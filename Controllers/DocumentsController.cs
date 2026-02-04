using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Infrastructure.OCR;
using FormationManager.Infrastructure.AI;
using FormationManager.Infrastructure.Exceptions;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly IOCRService _ocrService;
        private readonly IAIService _aiService;
        private readonly IQualiopiService _qualiopiService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DocumentsController> _logger;
        private readonly ISiteContext _siteContext;

        public DocumentsController(
            FormationDbContext context,
            IOCRService ocrService,
            IAIService aiService,
            IQualiopiService qualiopiService,
            IWebHostEnvironment environment,
            ILogger<DocumentsController> logger,
            ISiteContext siteContext)
        {
            _context = context;
            _ocrService = ocrService;
            _aiService = aiService;
            _qualiopiService = qualiopiService;
            _environment = environment;
            _logger = logger;
            _siteContext = siteContext;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Documents.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(d => string.IsNullOrEmpty(d.SiteId) || d.SiteId == _siteContext.CurrentSiteId);
            }

            var documents = await query
                .Include(d => d.Template)
                .Include(d => d.Session)
                    .ThenInclude(s => s!.Formation)
                .Include(d => d.Client)
                .Include(d => d.Stagiaire)
                .OrderByDescending(d => d.DateCreation)
                .ToListAsync();

            var examples = await GetDocumentExamplesAsync();

            var viewModel = new DocumentsIndexViewModel
            {
                Documents = documents,
                Examples = examples
            };

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var sessionsQuery = _context.Sessions.Include(s => s.Formation).AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                sessionsQuery = sessionsQuery.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Sessions = await sessionsQuery
                .OrderByDescending(s => s.DateDebut)
                .ToListAsync();

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }

            var indicateursRaw = await _context.IndicateursQualiopi
                .OrderBy(i => i.Critere)
                .ThenBy(i => i.CodeIndicateur)
                .ToListAsync();
            ViewBag.Indicateurs = indicateursRaw
                .GroupBy(i => new { i.Critere, i.CodeIndicateur })
                .Select(g => g.First())
                .OrderBy(i => i.Critere)
                .ThenBy(i => int.TryParse(i.CodeIndicateur, out var n) ? n : 999)
                .ToList();

            return View(new DocumentUploadViewModel { TypeDocument = TypeDocument.Emargement });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(DocumentUploadViewModel model)
        {
            try
            {
                if (!ModelState.IsValid || model.File == null)
                {
                    return await Upload();
                }

                var uploadsPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsPath);

                var fileName = $"{Guid.NewGuid():N}_{Path.GetFileName(model.File.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                
                string ocrText = string.Empty;
                try
                {
                    _logger.LogInformation("Début extraction OCR pour fichier {FileName} ({Size} bytes)", 
                        model.File.FileName, fileBytes.Length);
                    ocrText = await _ocrService.ExtractTextAsync(fileBytes);
                    _logger.LogInformation("Extraction OCR terminée : {TextLength} caractères extraits", ocrText?.Length ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'extraction OCR : {Message}. Le document sera sauvegardé sans texte OCR.", ex.Message);
                    ocrText = string.Empty;
                }

                DocumentAnalysis? analysis = null;
                if (!string.IsNullOrWhiteSpace(ocrText))
                {
                    try
                    {
                        var aiType = MapDocumentType(model.TypeDocument);
                        _logger.LogInformation("Début analyse IA pour document de type {DocumentType}", aiType);
                        analysis = await _aiService.AnalyzeDocumentAsync(ocrText, aiType);
                        _logger.LogInformation("Analyse IA terminée : {CriteriaCount} critères Qualiopi identifiés", 
                            analysis?.QualiopiCriteria?.Count ?? 0);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Analyse IA indisponible, OCR seul sauvegardé.");
                    }
                }
                else
                {
                    _logger.LogWarning("Texte OCR vide, analyse IA ignorée");
                }

                var linkedSessionId = model.SessionId ?? await TryAutoLinkSessionAsync(ocrText);
                Session? linkedSession = null;
                if (linkedSessionId.HasValue)
                {
                    linkedSession = await _context.Sessions.FindAsync(linkedSessionId.Value);
                    if (linkedSession == null)
                    {
                        linkedSessionId = null;
                    }
                }

                if (!_siteContext.IsAdmin && linkedSession != null && !string.IsNullOrEmpty(linkedSession.SiteId) && linkedSession.SiteId != _siteContext.CurrentSiteId)
                {
                    return Forbid();
                }

                var templateId = await _context.TemplatesDocument
                    .Where(t => t.TypeDocument == model.TypeDocument)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync();

                if (templateId == 0)
                {
                    templateId = 1; // fallback
                }

                var now = DateTime.Now;
                var resolvedSiteId = linkedSession?.SiteId
                    ?? (_siteContext.IsAdmin && !string.IsNullOrWhiteSpace(model.SiteId)
                        ? model.SiteId
                        : _siteContext.CurrentSiteId);

                var document = new Document
                {
                    TypeDocument = model.TypeDocument,
                    TemplateId = templateId,
                    Donnees = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ocrText,
                        analysis
                    }),
                    StatutValidation = "En attente",
                    CheminFichier = $"/uploads/{fileName}",
                    NomFichier = model.File.FileName,
                    SessionId = linkedSessionId,
                    SiteId = resolvedSiteId,
                    DateCreation = now,
                    DateModification = now,
                    CreePar = User.Identity?.Name ?? "system",
                    ModifiePar = User.Identity?.Name ?? "system"
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                var messageParts = new List<string> { "Document importé avec succès." };
                
                // Feedback sur l'OCR
                if (!string.IsNullOrWhiteSpace(ocrText))
                {
                    messageParts.Add($"OCR : {ocrText.Length} caractères extraits.");
                }
                else
                {
                    messageParts.Add("⚠️ OCR : Aucun texte extrait.");
                }

                // Feedback sur l'analyse IA
                if (analysis != null)
                {
                    messageParts.Add($"Analyse IA : {analysis.QualiopiCriteria?.Count ?? 0} critères Qualiopi identifiés.");
                }
                else
                {
                    messageParts.Add("⚠️ Analyse IA : Ollama non disponible. Démarrez Ollama pour l'analyse automatique.");
                }

                // Feedback sur le linking
                if (linkedSessionId.HasValue && linkedSession != null)
                {
                    messageParts.Add($"✅ Lié automatiquement à la session : {linkedSession.Formation?.Titre ?? "Session"}");
                }
                else
                {
                    messageParts.Add("⚠️ Non lié à une session. Vous pouvez le lier manuellement.");
                }

                // Création des preuves Qualiopi : priorité à la checklist (coches utilisateur), sinon IA
                int preuvesCreees = 0;
                var indicateurIds = (model.IndicateurIds ?? new List<int>()).Distinct().ToList();
                var useChecklist = indicateurIds.Count > 0 && linkedSessionId.HasValue;

                if (useChecklist)
                {
                    try
                    {
                        preuvesCreees = await CreatePreuvesFromChecklistAsync(document, linkedSessionId!.Value, linkedSession!.SiteId ?? string.Empty, indicateurIds);
                        messageParts.Add($"✅ {preuvesCreees} preuve(s) Qualiopi créée(s) depuis la checklist.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors de la création des preuves depuis la checklist");
                        messageParts.Add("⚠️ Erreur lors de la création des preuves depuis la checklist.");
                    }
                }
                else if (analysis != null && linkedSessionId.HasValue && analysis.QualiopiCriteria.Count > 0)
                {
                    try
                    {
                        await AutoCreatePreuvesAsync(analysis, linkedSessionId.Value, document);
                        preuvesCreees = analysis.QualiopiCriteria.Count;
                        messageParts.Add($"✅ {preuvesCreees} preuve(s) Qualiopi créée(s) automatiquement (IA).");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors de la création automatique des preuves Qualiopi");
                        messageParts.Add("⚠️ Erreur lors de la création automatique des preuves Qualiopi.");
                    }
                }
                else if (linkedSessionId.HasValue)
                {
                    messageParts.Add("ℹ️ Aucune preuve créée. Cochez les critères à l'import ou créez des preuves manuellement (Qualiopi > Preuves).");
                }

                TempData["Success"] = string.Join(" ", messageParts);
                TempData["DocumentId"] = document.Id;
                TempData["LinkedSessionId"] = linkedSessionId;
                TempData["HasAnalysis"] = analysis != null;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur critique lors de l'upload du document : {Message}", ex.Message);
                TempData["Error"] = $"Erreur lors de l'import du document : {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExample(IFormFile? exampleFile)
        {
            if (exampleFile == null || exampleFile.Length == 0)
            {
                TempData["Error"] = "Veuillez sélectionner un fichier (PDF, DOCX, XLSX, XLS ou PPTX).";
                return Redirect(Url.Action(nameof(Index)) + "#document-types");
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".docx", ".xlsx", ".xls", ".pptx"
            };
            var extension = Path.GetExtension(exampleFile.FileName);
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Format non accepté. Utilisez PDF, DOCX, XLSX, XLS ou PPTX.";
                return Redirect(Url.Action(nameof(Index)) + "#document-types");
            }

            var examplesPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "examples", _siteContext.CurrentSiteId);
            Directory.CreateDirectory(examplesPath);

            var safeName = Path.GetFileName(exampleFile.FileName);
            var filePath = Path.Combine(examplesPath, safeName);
            if (System.IO.File.Exists(filePath))
            {
                var fileName = $"{Path.GetFileNameWithoutExtension(safeName)}_{Guid.NewGuid():N}{extension}";
                filePath = Path.Combine(examplesPath, fileName);
                safeName = fileName;
            }

            await using var stream = new FileStream(filePath, FileMode.Create);
            await exampleFile.CopyToAsync(stream);

            TempData["Success"] = "Document type ajouté. Il est disponible pour tous les formateurs et coordinateurs du site.";
            return Redirect(Url.Action(nameof(Index)) + "#document-types");
        }

        private static Infrastructure.AI.DocumentType MapDocumentType(TypeDocument typeDocument)
        {
            return typeDocument switch
            {
                TypeDocument.Emargement => Infrastructure.AI.DocumentType.Emargement,
                TypeDocument.Evaluation => Infrastructure.AI.DocumentType.Evaluation,
                TypeDocument.Convention => Infrastructure.AI.DocumentType.Convention,
                TypeDocument.Attestation => Infrastructure.AI.DocumentType.Attestation,
                _ => Infrastructure.AI.DocumentType.Autre
            };
        }

        private Task<List<DocumentExampleItem>> GetDocumentExamplesAsync()
        {
            var examplesPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "examples", _siteContext.CurrentSiteId);
            if (!Directory.Exists(examplesPath))
            {
                return Task.FromResult(new List<DocumentExampleItem>());
            }

            var items = Directory.GetFiles(examplesPath)
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new DocumentExampleItem
                    {
                        FileName = info.Name,
                        Url = $"/examples/{_siteContext.CurrentSiteId}/{info.Name}",
                        SizeKb = Math.Max(1, info.Length / 1024),
                        LastModified = info.LastWriteTime
                    };
                })
                .OrderByDescending(x => x.LastModified)
                .ToList();

            return Task.FromResult(items);
        }

        private async Task<int?> TryAutoLinkSessionAsync(string ocrText)
        {
            var sessions = await _context.Sessions
                .Include(s => s.Formation)
                .OrderByDescending(s => s.DateDebut)
                .ToListAsync();

            if (sessions.Count == 0 || string.IsNullOrWhiteSpace(ocrText))
            {
                return null;
            }

            var text = ocrText.ToLowerInvariant();
            var matches = sessions
                .Where(s => !string.IsNullOrWhiteSpace(s.Formation?.Titre) &&
                            text.Contains(s.Formation.Titre.ToLowerInvariant()))
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0].Id;
            }

            var dates = System.Text.RegularExpressions.Regex.Matches(
                    text,
                    @"\b\d{1,2}/\d{1,2}/\d{4}\b")
                .Select(m => DateTime.TryParse(m.Value, out var date) ? date.Date : (DateTime?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .Distinct()
                .ToList();

            if (dates.Count == 0)
            {
                return null;
            }

            var byDate = sessions
                .Where(s => dates.Any(d => d >= s.DateDebut.Date && d <= s.DateFin.Date))
                .ToList();

            return byDate.Count == 1 ? byDate[0].Id : null;
        }

        /// <summary>
        /// Crée les preuves Qualiopi à partir des indicateurs cochés dans la checklist à l'import.
        /// </summary>
        private async Task<int> CreatePreuvesFromChecklistAsync(Document document, int sessionId, string siteId, List<int> indicateurIds)
        {
            var indicateurs = await _context.IndicateursQualiopi
                .Where(i => indicateurIds.Contains(i.Id))
                .ToListAsync();

            if (indicateurs.Count == 0)
            {
                return 0;
            }

            var user = User.Identity?.Name ?? "system";
            var now = DateTime.Now;
            var titre = document.NomFichier ?? $"Document {document.TypeDocument}";
            var count = 0;

            foreach (var indicateur in indicateurs)
            {
                var exists = await _context.PreuvesQualiopi.AnyAsync(p =>
                    p.SessionId == sessionId &&
                    p.IndicateurQualiopiId == indicateur.Id &&
                    p.CheminFichier == document.CheminFichier);

                if (exists)
                {
                    continue;
                }

                var preuve = new PreuveQualiopi
                {
                    SessionId = sessionId,
                    IndicateurQualiopiId = indicateur.Id,
                    Titre = titre,
                    Description = string.Empty,
                    Type = PreuveQualiopi.TypePreuve.Document,
                    CheminFichier = document.CheminFichier ?? string.Empty,
                    SiteId = siteId,
                    DateCreation = now,
                    DateModification = now,
                    CreePar = user,
                    ModifiePar = user
                };

                await _qualiopiService.AjouterPreuveAsync(preuve);
                count++;
            }

            return count;
        }

        private async Task AutoCreatePreuvesAsync(DocumentAnalysis analysis, int sessionId, Document document)
        {
            var indicateurs = await _context.IndicateursQualiopi
                .Where(i => analysis.QualiopiCriteria.Contains(i.Critere))
                .ToListAsync();

            if (indicateurs.Count == 0)
            {
                return;
            }

            var title = $"Auto-import {document.TypeDocument} - {document.NomFichier}";
            foreach (var indicateur in indicateurs)
            {
                var exists = await _context.PreuvesQualiopi.AnyAsync(p =>
                    p.SessionId == sessionId &&
                    p.IndicateurQualiopiId == indicateur.Id &&
                    p.Titre == title);

                if (exists)
                {
                    continue;
                }

                var preuve = new PreuveQualiopi
                {
                    SessionId = sessionId,
                    IndicateurQualiopiId = indicateur.Id,
                    Titre = title,
                    Description = analysis.Summary,
                    Type = PreuveQualiopi.TypePreuve.Document,
                    CheminFichier = document.CheminFichier,
                    SiteId = document.SiteId,
                    DateCreation = DateTime.Now,
                    DateModification = DateTime.Now,
                    CreePar = User.Identity?.Name ?? "system",
                    ModifiePar = User.Identity?.Name ?? "system"
                };

                await _qualiopiService.AjouterPreuveAsync(preuve);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(d => d.Session)
                    .ThenInclude(s => s!.Formation)
                .Include(d => d.Client)
                .Include(d => d.Stagiaire)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            // Vérifier les permissions
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(document.SiteId) && document.SiteId != _siteContext.CurrentSiteId)
            {
                return Forbid();
            }

            return View(document);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.Documents
                .Include(d => d.Session)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                TempData["Error"] = "Document introuvable.";
                return RedirectToAction(nameof(Index));
            }

            // Vérifier les permissions
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(document.SiteId) && document.SiteId != _siteContext.CurrentSiteId)
            {
                TempData["Error"] = "Vous n'avez pas les permissions pour supprimer ce document.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Supprimer les preuves Qualiopi liées à ce document
                var preuves = await _context.PreuvesQualiopi
                    .Where(p => p.CheminFichier == document.CheminFichier)
                    .ToListAsync();

                if (preuves.Any())
                {
                    _context.PreuvesQualiopi.RemoveRange(preuves);
                    _logger.LogInformation("Suppression de {Count} preuves Qualiopi liées au document {DocumentId}", preuves.Count, id);
                }

                // Supprimer le fichier physique
                if (!string.IsNullOrEmpty(document.CheminFichier))
                {
                    var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", document.CheminFichier.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            System.IO.File.Delete(filePath);
                            _logger.LogInformation("Fichier physique supprimé : {FilePath}", filePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Impossible de supprimer le fichier physique : {FilePath}", filePath);
                            // Continuer même si le fichier ne peut pas être supprimé
                        }
                    }
                }

                // Supprimer l'entrée en base de données
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Document supprimé avec succès.";
                _logger.LogInformation("Document {DocumentId} supprimé par {User}", id, User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du document {DocumentId}", id);
                TempData["Error"] = $"Erreur lors de la suppression : {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
