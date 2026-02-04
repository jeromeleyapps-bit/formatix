using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Services;
using FormationManager.Models;

namespace FormationManager.Controllers
{
    [Authorize]
    public class StagiairesController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly IDocumentService _documentService;
        private readonly IWebHostEnvironment _environment;
        private readonly ISiteContext _siteContext;

        public StagiairesController(
            FormationDbContext context,
            IDocumentService documentService,
            IWebHostEnvironment environment,
            ISiteContext siteContext)
        {
            _context = context;
            _documentService = documentService;
            _environment = environment;
            _siteContext = siteContext;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Stagiaires.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            }

            var stagiaires = await query
                .Include(s => s.Client)
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .OrderBy(s => s.Nom)
                .ThenBy(s => s.Prenom)
                .ToListAsync();

            // Vérifier quels documents ont déjà été générés pour chaque stagiaire/session
            var stagiaireIds = stagiaires.Select(s => s.Id).ToList();
            var documentsExistants = await _context.Documents
                .Where(d => stagiaireIds.Contains(d.StagiaireId ?? 0) && 
                           (d.TypeDocument == TypeDocument.Attestation || d.TypeDocument == TypeDocument.Evaluation))
                .ToListAsync();

            // Regrouper les stagiaires par identité (nom, prénom, email) pour éviter les doublons
            var stagiairesGroupes = stagiaires
                .GroupBy(s => new { s.Nom, s.Prenom, s.Email })
                .Select(g => new StagiaireGroupedViewModel
                {
                    StagiairePrincipal = g.OrderByDescending(s => s.Session?.DateDebut ?? DateTime.MinValue).First(), // Le plus récent comme référence
                    ToutesLesSessions = g.OrderByDescending(s => s.Session?.DateDebut ?? DateTime.MinValue).ToList()
                })
                .OrderBy(g => g.StagiairePrincipal.Nom)
                .ThenBy(g => g.StagiairePrincipal.Prenom)
                .ToList();

            // Créer un dictionnaire simple pour vérifier rapidement si un document existe
            // Clé: "StagiaireId_SessionId_TypeDocument"
            var documentsParStagiaire = documentsExistants
                .GroupBy(d => $"{d.StagiaireId}_{d.SessionId}_{d.TypeDocument}")
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(d => d.DateCreation).First()
                );

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);
            ViewBag.DocumentsExistants = documentsParStagiaire;

            return View(stagiairesGroupes);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var clientsQuery = _context.Clients.AsQueryable();
            var sessionsQuery = _context.Sessions.Include(s => s.Formation).AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                clientsQuery = clientsQuery.Where(c => string.IsNullOrEmpty(c.SiteId) || c.SiteId == _siteContext.CurrentSiteId);
                sessionsQuery = sessionsQuery.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            }

            // S'assurer que le client "Indépendant" existe
            var independantClient = await clientsQuery.FirstOrDefaultAsync(c => c.Nom == "Indépendant" || c.Nom == "Particulier");
            if (independantClient == null)
            {
                independantClient = new Client
                {
                    Nom = "Indépendant",
                    TypeClient = TypeClient.Particulier,
                    SiteId = _siteContext.CurrentSiteId ?? string.Empty,
                    DateCreation = DateTime.Now,
                    DateModification = DateTime.Now,
                    CreePar = User.Identity?.Name ?? "system",
                    ModifiePar = User.Identity?.Name ?? "system"
                };
                _context.Clients.Add(independantClient);
                await _context.SaveChangesAsync();
            }

            ViewBag.Clients = await clientsQuery.OrderBy(c => c.Nom).ToListAsync();
            ViewBag.Sessions = await sessionsQuery.ToListAsync();
            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }
            return View(new Stagiaire());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Stagiaire stagiaire)
        {
            // Récupérer les valeurs depuis Request.Form si elles ne sont pas dans le modèle
            if (Request.Form.ContainsKey("ClientId"))
            {
                var clientIdStr = Request.Form["ClientId"].ToString();
                if (!string.IsNullOrWhiteSpace(clientIdStr) && clientIdStr != "INDEPENDANT" && int.TryParse(clientIdStr, out int clientId) && clientId > 0)
                {
                    stagiaire.ClientId = clientId;
                }
                else if (clientIdStr == "INDEPENDANT" || string.IsNullOrWhiteSpace(clientIdStr))
                {
                    // Si "Indépendant" est sélectionné ou vide, chercher ou créer le client "Indépendant"
                    var independantClient = await _context.Clients
                        .FirstOrDefaultAsync(c => c.Nom == "Indépendant" || c.Nom == "Particulier");
                    
                    if (independantClient == null)
                    {
                        // Créer le client "Indépendant" s'il n'existe pas
                        independantClient = new Client
                        {
                            Nom = "Indépendant",
                            TypeClient = TypeClient.Particulier,
                            SiteId = _siteContext.CurrentSiteId ?? string.Empty,
                            DateCreation = DateTime.Now,
                            DateModification = DateTime.Now,
                            CreePar = User.Identity?.Name ?? "system",
                            ModifiePar = User.Identity?.Name ?? "system"
                        };
                        _context.Clients.Add(independantClient);
                        await _context.SaveChangesAsync();
                    }
                    stagiaire.ClientId = independantClient.Id;
                }
                else
                {
                    stagiaire.ClientId = null;
                }
            }
            
            if (Request.Form.ContainsKey("SessionId"))
            {
                var sessionIdStr = Request.Form["SessionId"].ToString();
                if (!string.IsNullOrWhiteSpace(sessionIdStr) && int.TryParse(sessionIdStr, out int sessionId) && sessionId > 0)
                {
                    stagiaire.SessionId = sessionId;
                }
                else
                {
                    stagiaire.SessionId = null;
                }
            }
            
            // Supprimer les erreurs de validation pour les objets de navigation
            ModelState.Remove("Client");
            ModelState.Remove("Session");
            ModelState.Remove("ClientId");
            ModelState.Remove("SessionId");
            
            // ClientId est maintenant optionnel, mais si fourni, vérifier qu'il existe
            if (stagiaire.ClientId.HasValue && stagiaire.ClientId.Value > 0)
            {
                var clientExists = await _context.Clients.AnyAsync(c => c.Id == stagiaire.ClientId.Value);
                if (!clientExists)
                {
                    ModelState.AddModelError("ClientId", "Le client sélectionné n'existe pas.");
                }
            }
            
            // SessionId est optionnel, mais si fourni, vérifier qu'elle existe
            if (stagiaire.SessionId.HasValue && stagiaire.SessionId.Value > 0)
            {
                var sessionExists = await _context.Sessions.AnyAsync(s => s.Id == stagiaire.SessionId.Value);
                if (!sessionExists)
                {
                    ModelState.AddModelError("SessionId", "La session sélectionnée n'existe pas.");
                }
            }
            
            // Valider les champs requis (seulement nom, prénom, email)
            if (string.IsNullOrWhiteSpace(stagiaire.Nom))
            {
                ModelState.AddModelError("Nom", "Le nom est requis.");
            }
            if (string.IsNullOrWhiteSpace(stagiaire.Prenom))
            {
                ModelState.AddModelError("Prenom", "Le prénom est requis.");
            }
            if (string.IsNullOrWhiteSpace(stagiaire.Email))
            {
                ModelState.AddModelError("Email", "L'email est requis.");
            }
            
            if (!ModelState.IsValid)
            {
                var clientsQuery = _context.Clients.AsQueryable();
                var sessionsQuery = _context.Sessions.Include(s => s.Formation).AsQueryable();
                if (!_siteContext.IsAdmin)
                {
                    clientsQuery = clientsQuery.Where(c => string.IsNullOrEmpty(c.SiteId) || c.SiteId == _siteContext.CurrentSiteId);
                    sessionsQuery = sessionsQuery.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
                }

                ViewBag.Clients = await clientsQuery.OrderBy(c => c.Nom).ToListAsync();
                ViewBag.Sessions = await sessionsQuery.ToListAsync();
                if (_siteContext.IsAdmin)
                {
                    ViewBag.Sites = _siteContext.GetSites();
                }
                return View(stagiaire);
            }

            var now = DateTime.Now;
            stagiaire.DateCreation = now;
            stagiaire.DateModification = now;
            stagiaire.CreePar = User.Identity?.Name ?? "system";
            stagiaire.ModifiePar = User.Identity?.Name ?? "system";
            stagiaire.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(stagiaire.SiteId)
                ? stagiaire.SiteId
                : _siteContext.CurrentSiteId;

            _context.Stagiaires.Add(stagiaire);
            await _context.SaveChangesAsync();

            // Générer automatiquement les preuves Qualiopi pour le stagiaire
            if (stagiaire.SessionId.HasValue)
            {
                var qualiopiAutoProofService = HttpContext.RequestServices.GetRequiredService<IQualiopiAutoProofService>();
                await qualiopiAutoProofService.AutoCreatePreuvesForStagiaireAsync(stagiaire.Id);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            var stagiaire = await _context.Stagiaires
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stagiaire == null)
                return NotFound();
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(stagiaire.SiteId) && stagiaire.SiteId != _siteContext.CurrentSiteId)
                return Forbid();

            ViewBag.ReturnUrl = returnUrl;
            return View(stagiaire);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl = null)
        {
            var stagiaire = await _context.Stagiaires.FindAsync(id);
            if (stagiaire == null)
                return NotFound();
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(stagiaire.SiteId) && stagiaire.SiteId != _siteContext.CurrentSiteId)
                return Forbid();

            var sessionId = stagiaire.SessionId;
            _context.Stagiaires.Remove(stagiaire);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GenerateAttestation(int id)
        {
            var stagiaire = await _context.Stagiaires
                .Include(s => s.Client)
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stagiaire?.Session == null)
                return NotFound();
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(stagiaire.SiteId) && stagiaire.SiteId != _siteContext.CurrentSiteId)
                return Forbid();

            var pdf = _documentService.GenerateAttestation(stagiaire, stagiaire.Session);
            
            // Nom de fichier incluant la formation pour éviter les conflits
            var formationNom = System.Text.RegularExpressions.Regex.Replace(
                stagiaire.Session.Formation?.Titre ?? "Formation", 
                @"[^a-zA-Z0-9]", "_");
            var fileName = $"attestation_{stagiaire.Nom}_{stagiaire.Prenom}_{formationNom}_{stagiaire.Session.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            await SaveGeneratedDocumentAsync(TypeDocument.Attestation, fileName, pdf, stagiaire.SiteId, stagiaire.Session.Id, stagiaire.ClientId, stagiaire.Id);

            return File(pdf, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateEvaluation(int id)
        {
            var stagiaire = await _context.Stagiaires
                .Include(s => s.Client)
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stagiaire == null || stagiaire.Session == null)
                return NotFound();
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(stagiaire.SiteId) && stagiaire.SiteId != _siteContext.CurrentSiteId)
                return Forbid();

            var pdf = _documentService.GenerateEvaluation(stagiaire);
            
            // Nom de fichier incluant la formation pour éviter les conflits
            var formationNom = System.Text.RegularExpressions.Regex.Replace(
                stagiaire.Session.Formation?.Titre ?? "Formation", 
                @"[^a-zA-Z0-9]", "_");
            var fileName = $"evaluation_{stagiaire.Nom}_{stagiaire.Prenom}_{formationNom}_{stagiaire.Session.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            await SaveGeneratedDocumentAsync(TypeDocument.Evaluation, fileName, pdf, stagiaire.SiteId, stagiaire.SessionId, stagiaire.ClientId, stagiaire.Id);

            return File(pdf, "application/pdf", fileName);
        }

        private async Task SaveGeneratedDocumentAsync(
            TypeDocument typeDocument,
            string fileName,
            byte[] content,
            string siteId,
            int? sessionId,
            int? clientId,
            int? stagiaireId)
        {
            var outputPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "generated");
            Directory.CreateDirectory(outputPath);

            var filePath = Path.Combine(outputPath, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, content);

            var templateId = await _context.TemplatesDocument
                .Where(t => t.TypeDocument == typeDocument)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            if (templateId == 0)
                templateId = 1;

            var now = DateTime.Now;
            _context.Documents.Add(new Document
            {
                TypeDocument = typeDocument,
                TemplateId = templateId,
                Donnees = System.Text.Json.JsonSerializer.Serialize(new
                {
                    GeneratedAt = now,
                    Source = "Stagiaire"
                }),
                StatutValidation = "Généré",
                CheminFichier = $"/generated/{fileName}",
                NomFichier = fileName,
                SessionId = sessionId,
                ClientId = clientId,
                StagiaireId = stagiaireId,
                SiteId = siteId,
                DateCreation = now,
                DateModification = now,
                CreePar = User.Identity?.Name ?? "system",
                ModifiePar = User.Identity?.Name ?? "system"
            });

            await _context.SaveChangesAsync();
        }
    }
}
