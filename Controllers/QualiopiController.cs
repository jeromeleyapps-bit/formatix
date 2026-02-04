using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Services;
using FormationManager.Data;
using FormationManager.Models;

namespace FormationManager.Controllers
{
    [Authorize]
    public class QualiopiUiController : Controller
    {
        private readonly IQualiopiService _qualiopiService;
        private readonly IQualiopiAutoProofService _qualiopiAutoProofService;
        private readonly FormationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IExportService _exportService;
        private readonly ISiteContext _siteContext;
        private readonly ICritereSuggestionService _critereSuggestionService;

        public QualiopiUiController(
            IQualiopiService qualiopiService,
            IQualiopiAutoProofService qualiopiAutoProofService,
            FormationDbContext context,
            IWebHostEnvironment environment,
            IExportService exportService,
            ISiteContext siteContext,
            ICritereSuggestionService critereSuggestionService)
        {
            _qualiopiService = qualiopiService;
            _qualiopiAutoProofService = qualiopiAutoProofService;
            _context = context;
            _environment = environment;
            _exportService = exportService;
            _siteContext = siteContext;
            _critereSuggestionService = critereSuggestionService;
        }

        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Index()
        {
            var indicateursQuery = _context.IndicateursQualiopi.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                indicateursQuery = indicateursQuery.Where(i => string.IsNullOrEmpty(i.SiteId) || i.SiteId == _siteContext.CurrentSiteId);
            }

            var indicateursRaw = await indicateursQuery.ToListAsync();
            var indicateurs = indicateursRaw
                .GroupBy(i => i.CodeIndicateur)
                .Select(g => g.First())
                .OrderBy(i => i.Critere)
                .ThenBy(i => ParseCode(i.CodeIndicateur))
                .ToList();

            var preuvesQuery = _context.PreuvesQualiopi
                .Include(p => p.Indicateur)
                .Where(p => p.EstValide);
            if (!_siteContext.IsAdmin)
            {
                preuvesQuery = preuvesQuery.Where(p => string.IsNullOrEmpty(p.SiteId) || p.SiteId == _siteContext.CurrentSiteId);
            }

            var preuvesValides = await preuvesQuery.ToListAsync();
            var indicateursValidesFromPreuves = preuvesValides
                .Select(p => p.Indicateur?.CodeIndicateur)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!)
                .Distinct()
                .ToList();

            // Pour le critère 6, inclure aussi les validations veille RSS
            var validationsVeilleQuery = _context.VeilleValidations
                .Include(v => v.Indicateur)
                .Where(v => v.Indicateur.Critere == 6);
            if (!_siteContext.IsAdmin)
            {
                validationsVeilleQuery = validationsVeilleQuery.Where(v => v.SiteId == _siteContext.CurrentSiteId);
            }
            var validationsVeille = await validationsVeilleQuery.ToListAsync();
            var indicateursValidesFromVeille = validationsVeille
                .Select(v => v.Indicateur?.CodeIndicateur)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!)
                .Distinct()
                .ToList();

            // Fusionner les deux listes (preuves + veille)
            var indicateursValides = indicateursValidesFromPreuves
                .Union(indicateursValidesFromVeille)
                .ToList();

            var criteres = indicateurs
                .GroupBy(i => i.Critere)
                .Select(g =>
                {
                    var total = g.Count();
                    var valides = g.Count(i => indicateursValides.Contains(i.CodeIndicateur));
                    var taux = total == 0 ? 0 : Math.Round(100m * valides / total, 1);
                    return new QualiopiCritereStat
                    {
                        Critere = g.Key,
                        TotalIndicateurs = total,
                        IndicateursValides = valides,
                        TauxValidite = taux
                    };
                })
                .OrderBy(c => c.Critere)
                .ToList();

            var totalIndicateurs = indicateurs.Count;
            var totalValides = indicateurs.Count(i => indicateursValides.Contains(i.CodeIndicateur));
            var tauxGlobal = totalIndicateurs == 0 ? 0 : Math.Round(100m * totalValides / totalIndicateurs, 1);

            var viewModel = new QualiopiIndexViewModel
            {
                Indicateurs = indicateurs,
                Criteres = criteres,
                IndicateursValides = indicateursValides,
                TotalIndicateurs = totalIndicateurs,
                IndicateursValidesCount = totalValides,
                TauxValidite = tauxGlobal
            };

            return View(viewModel);
        }

        private static int ParseCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return int.MaxValue;
            }

            var digits = new string(code.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out var value) ? value : int.MaxValue;
        }

        public async Task<IActionResult> Preuves()
        {
            var query = _context.PreuvesQualiopi.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(p => string.IsNullOrEmpty(p.SiteId) || p.SiteId == _siteContext.CurrentSiteId);
            }

            var preuves = await query
                .Include(p => p.Indicateur)
                .Include(p => p.Session)
                    .ThenInclude(s => s!.Formation)
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync();

            var sessionsQuery = _context.Sessions.Include(s => s.Formation).AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                sessionsQuery = sessionsQuery.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Sessions = await sessionsQuery
                .OrderByDescending(s => s.DateDebut)
                .ToListAsync();

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            return View(preuves);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePreuve(int? sessionId = null, int? documentId = null)
        {
            var sessionsQuery = _context.Sessions.Include(s => s.Formation).AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                sessionsQuery = sessionsQuery.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Sessions = await sessionsQuery
                .OrderByDescending(s => s.DateDebut)
                .ToListAsync();

            var indicateursRaw = await _context.IndicateursQualiopi
                .OrderBy(i => i.Critere)
                .ThenBy(i => i.CodeIndicateur)
                .ToListAsync();
            // Un seul indicateur par (Critere, CodeIndicateur) : évite les doublons liés aux sites
            ViewBag.Indicateurs = indicateursRaw
                .GroupBy(i => new { i.Critere, i.CodeIndicateur })
                .Select(g => g.First())
                .OrderBy(i => i.Critere)
                .ThenBy(i => int.TryParse(i.CodeIndicateur, out var n) ? n : 999)
                .ToList();

            // Si un document est fourni, pré-remplir les informations
            if (documentId.HasValue)
            {
                var document = await _context.Documents
                    .Include(d => d.Session)
                    .FirstOrDefaultAsync(d => d.Id == documentId.Value);

                if (document != null)
                {
                    ViewBag.Document = document;
                    ViewBag.DocumentId = documentId.Value;
                    if (document.SessionId.HasValue)
                    {
                        sessionId = document.SessionId;
                    }
                }
            }

            ViewBag.SessionId = sessionId;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentsBySession(int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(session.SiteId) && session.SiteId != _siteContext.CurrentSiteId))
            {
                return Json(Array.Empty<object>());
            }

            var query = _context.Documents
                .AsNoTracking()
                .Where(d => d.SessionId == sessionId);
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(d => string.IsNullOrEmpty(d.SiteId) || d.SiteId == _siteContext.CurrentSiteId);
            }

            var items = await query
                .OrderByDescending(d => d.DateCreation)
                .Select(d => new { id = d.Id, nomFichier = d.NomFichier, typeDocument = d.TypeDocument.ToString(), dateCreation = d.DateCreation })
                .ToListAsync();

            return Json(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePreuve(
            int sessionId,
            [FromForm] IEnumerable<int>? indicateurIds,
            string titre,
            string description,
            PreuveQualiopi.TypePreuve typePreuve,
            IFormFile? fichier,
            int? documentId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(session.SiteId) && session.SiteId != _siteContext.CurrentSiteId))
            {
                return NotFound();
            }

            var ids = (indicateurIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (ids.Count == 0)
            {
                TempData["Error"] = "Sélectionnez au moins un indicateur Qualiopi.";
                return RedirectToAction(nameof(CreatePreuve), new { sessionId, documentId });
            }

            var uploadsPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "preuves");
            Directory.CreateDirectory(uploadsPath);

            string? cheminFichier = null;
            string? titreFinal = titre;

            if (fichier != null && fichier.Length > 0)
            {
                var fileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fichier.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await fichier.CopyToAsync(stream);
                cheminFichier = $"/uploads/preuves/{fileName}";
            }
            else if (documentId.HasValue)
            {
                var docQuery = _context.Documents
                    .AsNoTracking()
                    .Where(d => d.Id == documentId.Value && d.SessionId == sessionId);
                if (!_siteContext.IsAdmin)
                {
                    docQuery = docQuery.Where(d => string.IsNullOrEmpty(d.SiteId) || d.SiteId == _siteContext.CurrentSiteId);
                }
                var doc = await docQuery.FirstOrDefaultAsync();
                if (doc != null)
                {
                    cheminFichier = doc.CheminFichier ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(titreFinal) && !string.IsNullOrEmpty(doc.NomFichier))
                    {
                        titreFinal = doc.NomFichier;
                    }
                }
            }

            var now = DateTime.Now;
            var user = User.Identity?.Name ?? "system";
            foreach (var indicateurId in ids)
            {
                var preuve = new PreuveQualiopi
                {
                    SessionId = sessionId,
                    IndicateurQualiopiId = indicateurId,
                    Titre = titreFinal ?? titre,
                    Description = description ?? string.Empty,
                    Type = typePreuve,
                    CheminFichier = cheminFichier ?? string.Empty,
                    SiteId = session.SiteId,
                    EstValide = true, // Preuve ajoutée manuellement = considérée valide pour le taux de conformité
                    DateCreation = now,
                    DateModification = now,
                    CreePar = user,
                    ModifiePar = user
                };
                await _qualiopiService.AjouterPreuveAsync(preuve);
            }

            TempData["Success"] = ids.Count == 1
                ? "Une preuve a été créée."
                : $"{ids.Count} preuves ont été créées pour ce document.";
            return RedirectToAction(nameof(Preuves));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValiderPreuve(int id, string commentaire)
        {
            await _qualiopiService.ValiderPreuveAsync(id, commentaire);
            return RedirectToAction(nameof(Preuves));
        }

        [HttpGet]
        public async Task<IActionResult> RapportConformite(int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return NotFound();
            }

            var pdf = await _qualiopiService.GenerateRapportConformiteAsync(sessionId);
            return File(pdf, "application/pdf", $"rapport_qualiopi_session_{sessionId}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ExportJson(int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return NotFound();
            }

            var json = await _exportService.ExportQualiopiJSONAsync(sessionId);
            return File(json, "application/json", $"qualiopi_session_{sessionId}.json");
        }

        public async Task<IActionResult> Guide()
        {
            var guide = await _qualiopiAutoProofService.GetQualiopiGuideAsync();
            ViewBag.Guide = guide;
            return View();
        }

        /// <summary>
        /// Endpoint pour obtenir des suggestions de critères basées sur le nom de fichier et la session
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCritereSuggestions(int? sessionId, string? fileName)
        {
            try
            {
                var suggestions = await _critereSuggestionService.GetSuggestionsAsync(
                    sessionId,
                    fileName,
                    null); // Type document déterminé côté serveur si nécessaire

                return Json(suggestions);
            }
            catch (Exception)
            {
                // Logger l'erreur mais retourner une liste vide pour ne pas bloquer l'utilisateur
                return Json(new List<object>());
            }
        }
    }
}
