using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly ISiteContext _siteContext;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public AdminController(
            FormationDbContext context,
            ISiteContext siteContext,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _siteContext = siteContext;
            _environment = environment;
            _configuration = configuration;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            try
            {
                var sites = _siteContext.GetSites();

            var formations = await _context.Formations
                .GroupBy(f => f.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();
            var sessions = await _context.Sessions
                .GroupBy(s => s.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();
            var stagiaires = await _context.Stagiaires
                .GroupBy(s => s.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();
            var documents = await _context.Documents
                .GroupBy(d => d.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();
            var preuves = await _context.PreuvesQualiopi
                .GroupBy(p => p.SiteId)
                .Select(g => new { SiteId = g.Key, Count = g.Count() })
                .ToListAsync();

            var formationsList = await _context.Formations
                .ToListAsync();

            var sessionsList = await _context.Sessions
                .Include(s => s.Formation)
                .ToListAsync();

            var documentsBySession = await _context.Documents
                .Where(d => d.SessionId.HasValue)
                .GroupBy(d => d.SessionId!.Value)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync();

            var preuvesBySession = await _context.PreuvesQualiopi
                .GroupBy(p => p.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync();

            var evaluationDocsBySession = await _context.Documents
                .Where(d => d.SessionId.HasValue && d.TypeDocument == TypeDocument.Evaluation)
                .GroupBy(d => d.SessionId!.Value)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync();

            var stagiairesMissingEvaluations = await _context.Stagiaires
                .Where(s => s.SessionId.HasValue && (s.EvaluationAChaud == null || s.EvaluationAFroid == null))
                .Join(_context.Sessions,
                    stagiaire => stagiaire.SessionId,
                    session => session.Id,
                    (stagiaire, session) => new { session.FormationId, stagiaire.Id })
                .GroupBy(x => x.FormationId)
                .Select(g => new { FormationId = g.Key, Count = g.Count() })
                .ToListAsync();

            var trainerSessions = await _context.Sessions
                .Include(s => s.Formateur)
                .Where(s => s.Formateur != null)
                .GroupBy(s => new { s.FormateurId, s.SiteId, Nom = s.Formateur!.Nom, Prenom = s.Formateur!.Prenom })
                .Select(g => new
                {
                    g.Key.FormateurId,
                    g.Key.SiteId,
                    g.Key.Nom,
                    g.Key.Prenom,
                    Sessions = g.Count(),
                    DerniereSession = g.Max(x => x.DateDebut)
                })
                .ToListAsync();

            var trainerStagiaires = await _context.Stagiaires
                .Where(s => s.SessionId.HasValue)
                .Join(_context.Sessions,
                    stagiaire => stagiaire.SessionId!.Value,
                    session => session.Id,
                    (stagiaire, session) => new { session.FormateurId, stagiaire.Id })
                .GroupBy(x => x.FormateurId)
                .Select(g => new { FormateurId = g.Key, Count = g.Count() })
                .ToListAsync();

            var trainerDocuments = await _context.Documents
                .Where(d => d.SessionId.HasValue)
                .Join(_context.Sessions,
                    document => document.SessionId!.Value,
                    session => session.Id,
                    (document, session) => new { session.FormateurId, document.Id })
                .GroupBy(x => x.FormateurId)
                .Select(g => new { FormateurId = g.Key, Count = g.Count() })
                .ToListAsync();

            var trainerPreuves = await _context.PreuvesQualiopi
                .Join(_context.Sessions,
                    preuve => preuve.SessionId,
                    session => session.Id,
                    (preuve, session) => new { session.FormateurId, preuve.Id })
                .GroupBy(x => x.FormateurId)
                .Select(g => new { FormateurId = g.Key, Count = g.Count() })
                .ToListAsync();

            var pendingDocuments = await _context.Documents
                .Include(d => d.Session)
                    .ThenInclude(s => s!.Formation)
                .Where(d => d.StatutValidation == "En attente")
                .OrderByDescending(d => d.DateCreation)
                .Take(20)
                .ToListAsync();

            var pendingPreuves = await _context.PreuvesQualiopi
                .Include(p => p.Indicateur)
                .Include(p => p.Session)
                    .ThenInclude(s => s!.Formation)
                .Where(p => !p.EstValide)
                .OrderByDescending(p => p.DateCreation)
                .Take(20)
                .ToListAsync();

            var veilleSince = DateTime.UtcNow.AddDays(-30);
            var veilleFluxCount = await _context.RssFeeds.CountAsync(f => f.IsActive);
            var veilleActualitesCount = await _context.RssItems.CountAsync(i =>
                (i.PublishedUtc != null && i.PublishedUtc >= veilleSince) || i.FetchedAt >= veilleSince);
            var veilleValidationsCount = await _context.VeilleValidations.CountAsync(v => v.ValidatedAt >= veilleSince);

            // Statistiques Qualiopi globales (tous sites confondus)
            var indicateursRaw = await _context.IndicateursQualiopi
                .AsNoTracking()
                .ToListAsync();
            // Un seul indicateur par code pour éviter les doublons liés aux sites
            var indicateurs = indicateursRaw
                .GroupBy(i => i.CodeIndicateur)
                .Select(g => g.First())
                .ToList();

            var preuvesValides = await _context.PreuvesQualiopi
                .Include(p => p.Indicateur)
                .Where(p => p.EstValide)
                .ToListAsync();
            var indicateursValidesFromPreuves = preuvesValides
                .Select(p => p.Indicateur?.CodeIndicateur)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!)
                .Distinct()
                .ToList();

            // Pour le critère 6, inclure aussi les validations de veille
            var validationsVeille = await _context.VeilleValidations
                .Include(v => v.Indicateur)
                .Where(v => v.Indicateur.Critere == 6)
                .ToListAsync();
            var indicateursValidesFromVeille = validationsVeille
                .Select(v => v.Indicateur?.CodeIndicateur)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!)
                .Distinct()
                .ToList();

            var indicateursValides = indicateursValidesFromPreuves
                .Union(indicateursValidesFromVeille)
                .ToList();

            var qualiopiCriteres = indicateurs
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

            var qualiopiTotalIndicateurs = indicateurs.Count;
            var qualiopiIndicateursValides = indicateurs.Count(i => indicateursValides.Contains(i.CodeIndicateur));
            var qualiopiTauxGlobal = qualiopiTotalIndicateurs == 0
                ? 0
                : Math.Round(100m * qualiopiIndicateursValides / qualiopiTotalIndicateurs, 1);

            // Sessions à risque Qualiopi : sans documents ou sans preuves
            var sessionsAtRisk = sessionsList
                .Where(s => !documentsBySession.Any(d => d.SessionId == s.Id) || !preuvesBySession.Any(p => p.SessionId == s.Id))
                .OrderByDescending(s => s.DateDebut)
                .Take(20)
                .Select(s => new SessionRiskItem
                {
                    SessionId = s.Id,
                    FormationTitre = s.Formation?.Titre ?? "-",
                    SiteId = s.SiteId,
                    SiteName = sites.FirstOrDefault(site => site.SiteId == s.SiteId)?.Name ?? s.SiteId,
                    DateDebut = s.DateDebut,
                    MissingDocuments = !documentsBySession.Any(d => d.SessionId == s.Id),
                    MissingPreuves = !preuvesBySession.Any(p => p.SessionId == s.Id)
                })
                .ToList();

            var viewModel = new AdminDashboardViewModel
            {
                Sites = sites.Select(site => new SiteDashboardItem
                {
                    SiteId = site.SiteId,
                    SiteName = site.Name,
                    Formations = formations.FirstOrDefault(f => f.SiteId == site.SiteId)?.Count ?? 0,
                    Sessions = sessions.FirstOrDefault(s => s.SiteId == site.SiteId)?.Count ?? 0,
                    Stagiaires = stagiaires.FirstOrDefault(s => s.SiteId == site.SiteId)?.Count ?? 0,
                    Documents = documents.FirstOrDefault(d => d.SiteId == site.SiteId)?.Count ?? 0,
                    Preuves = preuves.FirstOrDefault(p => p.SiteId == site.SiteId)?.Count ?? 0
                }).ToList(),
                Trainers = trainerSessions.Select(t => new TrainerDashboardItem
                {
                    FormateurId = t.FormateurId,
                    Nom = t.Nom,
                    Prenom = t.Prenom,
                    SiteId = t.SiteId,
                    SiteName = sites.FirstOrDefault(s => s.SiteId == t.SiteId)?.Name ?? t.SiteId,
                    Sessions = t.Sessions,
                    DerniereSession = t.DerniereSession,
                    Stagiaires = trainerStagiaires.FirstOrDefault(s => s.FormateurId == t.FormateurId)?.Count ?? 0,
                    Documents = trainerDocuments.FirstOrDefault(d => d.FormateurId == t.FormateurId)?.Count ?? 0,
                    Preuves = trainerPreuves.FirstOrDefault(p => p.FormateurId == t.FormateurId)?.Count ?? 0,
                    SessionsMissingDocuments = sessionsList.Count(s =>
                        s.FormateurId == t.FormateurId &&
                        !documentsBySession.Any(d => d.SessionId == s.Id)),
                    SessionsMissingPreuves = sessionsList.Count(s =>
                        s.FormateurId == t.FormateurId &&
                        !preuvesBySession.Any(p => p.SessionId == s.Id))
                }).ToList(),
                FormationsFollowup = formationsList.Select(f =>
                {
                    var formationSessions = sessionsList
                        .Where(s => s.FormationId == f.Id)
                        .ToList();
                    var sessionsWithoutEvaluationDocs = formationSessions.Count(s =>
                        !evaluationDocsBySession.Any(e => e.SessionId == s.Id));
                    return new FormationFollowupItem
                    {
                        FormationId = f.Id,
                        Titre = f.Titre,
                        SiteId = f.SiteId,
                        SiteName = sites.FirstOrDefault(s => s.SiteId == f.SiteId)?.Name ?? f.SiteId,
                        Sessions = formationSessions.Count,
                        SessionsWithoutEvaluationDocs = sessionsWithoutEvaluationDocs,
                        StagiairesWithoutEvaluation = stagiairesMissingEvaluations
                            .FirstOrDefault(x => x.FormationId == f.Id)?.Count ?? 0,
                        DerniereSession = formationSessions
                            .OrderByDescending(s => s.DateDebut)
                            .Select(s => (DateTime?)s.DateDebut)
                            .FirstOrDefault()
                    };
                })
                .OrderByDescending(f => f.SessionsWithoutEvaluationDocs + f.StagiairesWithoutEvaluation)
                .ToList(),
                PendingDocuments = pendingDocuments.Select(d => new PendingDocumentItem
                {
                    Id = d.Id,
                    TypeDocument = d.TypeDocument.ToString(),
                    SessionTitle = d.Session?.Formation?.Titre ?? "-",
                    SiteName = sites.FirstOrDefault(s => s.SiteId == d.SiteId)?.Name ?? d.SiteId,
                    DateCreation = d.DateCreation
                }).ToList(),
                PendingPreuves = pendingPreuves.Select(p => new PendingPreuveItem
                {
                    Id = p.Id,
                    Indicateur = p.Indicateur?.CodeIndicateur ?? "-",
                    SessionTitle = p.Session?.Formation?.Titre ?? "-",
                    SiteName = sites.FirstOrDefault(s => s.SiteId == p.SiteId)?.Name ?? p.SiteId,
                    DateCreation = p.DateCreation
                }).ToList(),
                SessionsAtRisk = sessionsAtRisk,
                QualiopiCriteres = qualiopiCriteres,
                QualiopiTotalIndicateurs = qualiopiTotalIndicateurs,
                QualiopiIndicateursValides = qualiopiIndicateursValides,
                QualiopiTauxGlobal = qualiopiTauxGlobal
            };

            viewModel.TotalFormations = viewModel.Sites.Sum(s => s.Formations);
            viewModel.TotalSessions = viewModel.Sites.Sum(s => s.Sessions);
            viewModel.TotalStagiaires = viewModel.Sites.Sum(s => s.Stagiaires);
            viewModel.TotalDocuments = viewModel.Sites.Sum(s => s.Documents);
            viewModel.TotalPreuves = viewModel.Sites.Sum(s => s.Preuves);
            viewModel.SessionsMissingDocuments = sessionsList.Count(s => !documentsBySession.Any(d => d.SessionId == s.Id));
            viewModel.SessionsMissingPreuves = sessionsList.Count(s => !preuvesBySession.Any(p => p.SessionId == s.Id));
            viewModel.PendingDocumentsCount = pendingDocuments.Count;
            viewModel.PendingPreuvesCount = pendingPreuves.Count;
            viewModel.VeilleFluxCount = veilleFluxCount;
            viewModel.VeilleActualitesCount = veilleActualitesCount;
            viewModel.VeilleValidationsCount = veilleValidationsCount;

            return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur Dashboard: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                
                // Retourner une vue d'erreur ou rediriger
                TempData["Error"] = $"Erreur lors du chargement du dashboard: {ex.Message}";
                return View(new AdminDashboardViewModel());
            }
        }

        public async Task<IActionResult> Diagnostics()
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var model = new SystemDiagnosticsViewModel();

            // Content root et disque
            model.ContentRootPath = _environment.ContentRootPath;
            try
            {
                var root = System.IO.Path.GetPathRoot(_environment.ContentRootPath);
                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new System.IO.DriveInfo(root);
                    model.ContentRootFreeSpaceBytes = drive.AvailableFreeSpace;
                    if (drive.AvailableFreeSpace < 1L * 1024 * 1024 * 1024)
                    {
                        model.Warnings.Add("Espace disque faible (< 1 Go) sur le disque de l'application.");
                    }
                }
            }
            catch (Exception ex)
            {
                model.Warnings.Add($"Impossible de déterminer l'espace disque disponible : {ex.Message}");
            }

            // Base de données (SQLite)
            try
            {
                model.DatabaseCanConnect = await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                model.DatabaseCanConnect = false;
                model.DatabaseError = ex.Message;
                model.Warnings.Add("La connexion à la base de données a échoué. Vérifiez le fichier SQLite et les droits d'accès.");
            }

            try
            {
                var connString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
                var dbPath = connString;
                const string prefix = "Data Source=";
                if (connString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    dbPath = connString.Substring(prefix.Length).Trim();
                }

                if (!System.IO.Path.IsPathRooted(dbPath))
                {
                    dbPath = System.IO.Path.Combine(_environment.ContentRootPath, dbPath);
                }

                model.DatabasePath = dbPath;
                model.DatabaseFileExists = System.IO.File.Exists(dbPath);
                if (!model.DatabaseFileExists)
                {
                    model.Warnings.Add($"Le fichier de base de données SQLite est introuvable : {dbPath}.");
                }
                else
                {
                    var info = new System.IO.FileInfo(dbPath);
                    model.DatabaseFileSizeBytes = info.Length;
                }
            }
            catch (Exception ex)
            {
                model.Warnings.Add($"Erreur lors de la vérification du fichier SQLite : {ex.Message}");
            }

            // Dossiers fichiers
            void CheckFolder(string label, string path, Action<string, bool> assign)
            {
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                    assign(path, true);
                }
                catch (Exception ex)
                {
                    assign(path, false);
                    model.Warnings.Add($"Dossier {label} inaccessible ({path}) : {ex.Message}");
                }
            }

            CheckFolder("uploads", System.IO.Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads"),
                (p, ok) => { model.UploadsPath = p; model.UploadsFolderOk = ok; });
            CheckFolder("examples", System.IO.Path.Combine(_environment.ContentRootPath, "wwwroot", "examples"),
                (p, ok) => { model.ExamplesPath = p; model.ExamplesFolderOk = ok; });
            CheckFolder("generated", System.IO.Path.Combine(_environment.ContentRootPath, "wwwroot", "generated"),
                (p, ok) => { model.GeneratedPath = p; model.GeneratedFolderOk = ok; });

            // BaseUrlInscription
            model.BaseUrlInscription = _configuration["AppSettings:BaseUrlInscription"] ?? string.Empty;
            model.BaseUrlInscriptionConfigured = !string.IsNullOrWhiteSpace(model.BaseUrlInscription);
            if (!model.BaseUrlInscriptionConfigured)
            {
                model.Warnings.Add("AppSettings:BaseUrlInscription n'est pas configuré. Les liens d'inscription distants utiliseront l'URL actuelle du serveur.");
            }
            else
            {
                model.BaseUrlInscriptionLooksPublic =
                    model.BaseUrlInscription.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    model.BaseUrlInscription.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                if (!model.BaseUrlInscriptionLooksPublic)
                {
                    model.Warnings.Add($"BaseUrlInscription ne ressemble pas à une URL publique valide : {model.BaseUrlInscription}.");
                }
            }

            // Sync / SiteId
            model.SyncSiteId = _configuration["Sync:SiteId"] ?? string.Empty;
            model.SyncSiteIdDefined = !string.IsNullOrWhiteSpace(model.SyncSiteId);
            if (!model.SyncSiteIdDefined)
            {
                model.Warnings.Add("Sync:SiteId n'est pas défini. Le filtrage par site ne fonctionnera pas correctement.");
            }
            else
            {
                var sites = _siteContext.GetSites();
                model.SyncSiteIdKnownInSites = sites.Any(s => s.SiteId == model.SyncSiteId);
                if (!model.SyncSiteIdKnownInSites)
                {
                    model.Warnings.Add($"Sync:SiteId='{model.SyncSiteId}' n'existe pas dans la configuration des sites.");
                }
            }

            return View(model);
        }
    }
}
