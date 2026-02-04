using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;
using Microsoft.Extensions.Logging;

namespace FormationManager.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly ISiteContext _siteContext;

        public HomeController(FormationDbContext context, ISiteContext siteContext)
        {
            _context = context;
            _siteContext = siteContext;
        }

        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Index()
        {
            var queryFormations = _context.Formations.AsQueryable();
            var querySessions = _context.Sessions.AsQueryable();
            var queryStagiaires = _context.Stagiaires.AsQueryable();
            var queryClients = _context.Clients.AsQueryable();

            if (!_siteContext.IsAdmin)
            {
                var siteId = _siteContext.CurrentSiteId;
                queryFormations = queryFormations.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == siteId);
                querySessions = querySessions.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == siteId);
                queryStagiaires = queryStagiaires.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == siteId);
                queryClients = queryClients.Where(c => string.IsNullOrEmpty(c.SiteId) || c.SiteId == siteId);
            }

            var now = DateTime.Now;
            var anneeEnCours = new DateTime(now.Year, 1, 1);

            // Identifier l'utilisateur courant et son éventuel profil formateur
            Utilisateur? currentUser = null;
            int? currentFormateurId = null;
            try
            {
                var userName = User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    currentUser = await _context.Utilisateurs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == userName || u.UserName == userName);

                    if (currentUser != null)
                    {
                        currentFormateurId = await _context.Formateurs
                            .AsNoTracking()
                            .Where(f => f.Actif && f.UtilisateurId == currentUser.Id)
                            .Select(f => (int?)f.Id)
                            .FirstOrDefaultAsync();
                    }
                }
            }
            catch
            {
                // En cas de problème de résolution, on ne filtre simplement pas par formateur
            }

            var totalFormations = await queryFormations.CountAsync();
            var totalSessions = await querySessions.CountAsync();
            var totalStagiaires = await queryStagiaires.CountAsync();
            
            // Calcul du CA annuel
            var caAnnee = await _context.SessionClients
                .Where(sc => sc.Session.DateDebut >= anneeEnCours)
                .Where(sc => _siteContext.IsAdmin || string.IsNullOrEmpty(sc.Session.SiteId) || sc.Session.SiteId == _siteContext.CurrentSiteId)
                .SumAsync(sc => (decimal?)(sc.TarifNegocie * sc.NombrePlaces)) ?? 0;

            // Sessions à venir (prochaines 5)
            var sessionsQuery = querySessions;
            // Pour un formateur identifié (non admin), on privilégie ses propres sessions
            if (!_siteContext.IsAdmin && currentFormateurId.HasValue)
            {
                sessionsQuery = sessionsQuery.Where(s => s.FormateurId == currentFormateurId.Value);
            }

            var sessionsAVenir = await sessionsQuery
                .Where(s => s.DateDebut > now)
                .Include(s => s.Formation)
                .Include(s => s.Stagiaires)
                .OrderBy(s => s.DateDebut)
                .Take(5)
                .Select(s => new
                {
                    FormationTitre = s.Formation.Titre,
                    DateDebut = s.DateDebut,
                    NombreStagiaires = s.Stagiaires.Count,
                    NombreMax = s.NombreMaxStagiaires,
                    Statut = s.Statut
                })
                .ToListAsync();

            // Documents en attente de validation
            var documentsEnAttente = await _context.Documents
                .Where(d => d.StatutValidation == "En attente")
                .Where(d => _siteContext.IsAdmin || string.IsNullOrEmpty(d.SiteId) || d.SiteId == _siteContext.CurrentSiteId)
                .Include(d => d.Session)
                    .ThenInclude(s => s!.Formation)
                .OrderByDescending(d => d.DateCreation)
                .Take(3)
                .ToListAsync();

            // Conformité Qualiopi (même logique que QualiopiUi : par CodeIndicateur + validations veille critère 6)
            var indicateursQualiopiQuery = _context.IndicateursQualiopi.AsQueryable();
            if (!_siteContext.IsAdmin)
                indicateursQualiopiQuery = indicateursQualiopiQuery.Where(i => string.IsNullOrEmpty(i.SiteId) || i.SiteId == _siteContext.CurrentSiteId);
            var indicateursList = await indicateursQualiopiQuery.ToListAsync();
            var indicateursUniques = indicateursList
                .GroupBy(i => i.CodeIndicateur)
                .Select(g => g.First())
                .ToList();
            var totalIndicateurs = indicateursUniques.Count;

            var preuvesValidesQuery = _context.PreuvesQualiopi
                .Include(p => p.Indicateur)
                .Where(p => p.EstValide);
            if (!_siteContext.IsAdmin)
                preuvesValidesQuery = preuvesValidesQuery.Where(p => string.IsNullOrEmpty(p.SiteId) || p.SiteId == _siteContext.CurrentSiteId);
            var codesValidesPreuves = (await preuvesValidesQuery.ToListAsync())
                .Select(p => p.Indicateur?.CodeIndicateur)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .Distinct()
                .ToHashSet();

            var validationsVeilleQuery = _context.VeilleValidations
                .Include(v => v.Indicateur)
                .Where(v => v.Indicateur.Critere == 6);
            if (!_siteContext.IsAdmin)
                validationsVeilleQuery = validationsVeilleQuery.Where(v => v.SiteId == _siteContext.CurrentSiteId);
            var codesValidesVeille = (await validationsVeilleQuery.ToListAsync())
                .Select(v => v.Indicateur?.CodeIndicateur)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .ToHashSet();

            var indicateursValides = indicateursUniques.Count(i => codesValidesPreuves.Contains(i.CodeIndicateur) || codesValidesVeille.Contains(i.CodeIndicateur));
            var tauxConformite = totalIndicateurs > 0 ? (int)Math.Round((double)indicateursValides / totalIndicateurs * 100) : 0;
            var actionsRequises = totalIndicateurs - indicateursValides;

            ViewBag.TotalFormations = totalFormations;
            ViewBag.TotalSessions = totalSessions;
            ViewBag.TotalStagiaires = totalStagiaires;
            ViewBag.IsAdmin = _siteContext.IsAdmin;
            ViewBag.IsFormateur = !_siteContext.IsAdmin && currentFormateurId.HasValue;
            ViewBag.CAAnnee = caAnnee;
            ViewBag.SessionsAVenir = sessionsAVenir;
            ViewBag.DocumentsEnAttente = documentsEnAttente;
            ViewBag.TauxConformite = tauxConformite;
            ViewBag.IndicateursValides = indicateursValides;
            ViewBag.TotalIndicateurs = totalIndicateurs;
            ViewBag.ActionsRequises = actionsRequises;

            return View();
        }
    }

    [Authorize]
    public class FormationsController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly ISiteContext _siteContext;
        private readonly IExportService _exportService;

        public FormationsController(
            FormationDbContext context, 
            ISiteContext siteContext,
            IExportService exportService)
        {
            _context = context;
            _siteContext = siteContext;
            _exportService = exportService;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Formations.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            var formations = await query
                .Include(f => f.Sessions)
                .OrderBy(f => f.Titre)
                .ToListAsync();

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            return View(formations);
        }

        public async Task<IActionResult> Details(int id)
        {
            var formation = await _context.Formations
                .Include(f => f.Sessions)
                    .ThenInclude(s => s.Formateur)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (formation == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(formation.SiteId) && formation.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            // Récupérer les versions de la formation
            var versions = await _context.FormationVersions
                .Where(v => v.FormationId == id)
                .OrderByDescending(v => v.DateVersion)
                .ToListAsync();

            // Récupérer les documents d'audit
            var documentsAudit = await _context.DocumentsAudit
                .Where(d => d.FormationId == id)
                .OrderByDescending(d => d.DateCreation)
                .ToListAsync();

            ViewBag.Versions = versions;
            ViewBag.DocumentsAudit = documentsAudit;
            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            return View(formation);
        }

        public IActionResult Create()
        {
            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Formation formation)
        {
            if (ModelState.IsValid)
            {
                formation.DateCreation = DateTime.Now;
                formation.DateModification = DateTime.Now;
                formation.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(formation.SiteId)
                    ? formation.SiteId
                    : _siteContext.CurrentSiteId;

                _context.Add(formation);
                await _context.SaveChangesAsync();

                // Générer automatiquement les preuves Qualiopi pour la formation
                var qualiopiAutoProofService = HttpContext.RequestServices.GetRequiredService<IQualiopiAutoProofService>();
                await qualiopiAutoProofService.AutoCreatePreuvesForFormationAsync(formation.Id);

                return RedirectToAction(nameof(Details), new { id = formation.Id });
            }

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }
            return View(formation);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var formation = await _context.Formations.FindAsync(id);
            if (formation == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(formation.SiteId) && formation.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }
            return View(formation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Formation formation, string? raisonModification)
        {
            if (id != formation.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Récupérer la formation actuelle pour comparer
                var formationActuelle = await _context.Formations.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (formationActuelle != null)
                {
                    // Vérifier si des modifications importantes ont été faites
                    bool hasChanges = formation.Titre != formationActuelle.Titre ||
                                     formation.Programme != formationActuelle.Programme ||
                                     formation.ModalitesPedagogiques != formationActuelle.ModalitesPedagogiques ||
                                     formation.ModalitesEvaluation != formationActuelle.ModalitesEvaluation ||
                                     formation.Prerequis != formationActuelle.Prerequis ||
                                     formation.DureeHeures != formationActuelle.DureeHeures;

                    if (hasChanges)
                    {
                        // Créer une version de la formation avant modification
                        var derniereVersion = await _context.FormationVersions
                            .Where(v => v.FormationId == id)
                            .OrderByDescending(v => v.DateVersion)
                            .FirstOrDefaultAsync();

                        string numeroVersion = "v1.0";
                        if (derniereVersion != null)
                        {
                            // Incrémenter la version
                            var parts = derniereVersion.NumeroVersion.Split('.');
                            if (parts.Length == 2 && int.TryParse(parts[0].Substring(1), out int major) &&
                                int.TryParse(parts[1], out int minor))
                            {
                                if (formation.Titre != formationActuelle.Titre ||
                                    formation.Programme != formationActuelle.Programme ||
                                    formation.ModalitesPedagogiques != formationActuelle.ModalitesPedagogiques)
                                {
                                    // Changement majeur
                                    numeroVersion = $"v{major + 1}.0";
                                }
                                else
                                {
                                    // Changement mineur
                                    numeroVersion = $"v{major}.{minor + 1}";
                                }
                            }
                        }

                        var version = new FormationVersion
                        {
                            FormationId = id,
                            NumeroVersion = numeroVersion,
                            DateVersion = DateTime.Now,
                            RaisonModification = raisonModification ?? "Modification de la formation",
                            Titre = formationActuelle.Titre,
                            Description = formationActuelle.Description,
                            Programme = formationActuelle.Programme,
                            Prerequis = formationActuelle.Prerequis,
                            ModalitesPedagogiques = formationActuelle.ModalitesPedagogiques,
                            ModalitesEvaluation = formationActuelle.ModalitesEvaluation,
                            ReferencesQualiopi = formationActuelle.ReferencesQualiopi,
                            DureeHeures = formationActuelle.DureeHeures,
                            PrixIndicatif = formationActuelle.PrixIndicatif,
                            SiteId = formationActuelle.SiteId,
                            ModifiePar = User.Identity?.Name ?? "system",
                            DateCreation = formationActuelle.DateCreation,
                            DateModification = formationActuelle.DateModification,
                            CreePar = formationActuelle.CreePar
                        };

                        _context.FormationVersions.Add(version);
                    }
                }

                formation.DateModification = DateTime.Now;
                formation.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(formation.SiteId)
                    ? formation.SiteId
                    : _siteContext.CurrentSiteId;

                _context.Update(formation);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Formation modifiée avec succès. Une nouvelle version a été créée dans l'historique.";
                return RedirectToAction(nameof(Details), new { id = formation.Id });
            }

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }
            return View(formation);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var formation = await _context.Formations.FindAsync(id);
            if (formation == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(formation.SiteId) && formation.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            return View(formation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var formation = await _context.Formations.FindAsync(id);
            if (formation != null && (_siteContext.IsAdmin || string.IsNullOrEmpty(formation.SiteId) || formation.SiteId == _siteContext.CurrentSiteId))
            {
                _context.Formations.Remove(formation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportCataloguePDF()
        {
            try
            {
                // Utiliser le SiteId du contexte pour filtrer les formations
                var siteId = _siteContext.IsAdmin ? null : _siteContext.CurrentSiteId;
                
                var pdf = await _exportService.ExportCataloguePDFAsync(siteId);
                
                var fileName = $"catalogue_formations_{_siteContext.CurrentSiteId ?? "all"}_{DateTime.Now:yyyy}.pdf";
                
                return File(pdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors de l'export : {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }

    [Authorize]
    public class SessionsController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly IDocumentService _documentService;
        private readonly IWebHostEnvironment _environment;
        private readonly ISiteContext _siteContext;
        private readonly ILogger<SessionsController> _logger;
        private readonly IInscriptionLinkService _inscriptionLinkService;
        private readonly IConflitsSessionService _conflitsService;

        public SessionsController(
            FormationDbContext context,
            IDocumentService documentService,
            IWebHostEnvironment environment,
            ISiteContext siteContext,
            ILogger<SessionsController> logger,
            IInscriptionLinkService inscriptionLinkService,
            IConflitsSessionService conflitsService)
        {
            _context = context;
            _documentService = documentService;
            _environment = environment;
            _siteContext = siteContext;
            _logger = logger;
            _inscriptionLinkService = inscriptionLinkService;
            _conflitsService = conflitsService;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Sessions.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            }

            var sessions = await query
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .OrderByDescending(s => s.DateDebut)
                .ToListAsync();

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            return View(sessions);
        }

        public async Task<IActionResult> Details(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .Include(s => s.Salle)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Client)
                .Include(s => s.Stagiaires)
                    .ThenInclude(s => s.Client)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(session.SiteId) && session.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            var inscriptionsOuvertes = session.EstPublique && (session.Statut == "Programmée" || session.Statut == "En cours");
            ViewBag.InscriptionUrl = inscriptionsOuvertes ? _inscriptionLinkService.GetInscriptionUrl(session.Id) : null;

            return View(session);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateEmargement(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(session.SiteId) && session.SiteId != _siteContext.CurrentSiteId)
                return Forbid();

            var pdf = _documentService.GenerateEmargement(session);
            var fileName = $"emargement_session_{session.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            await SaveGeneratedDocumentAsync(TypeDocument.Emargement, fileName, pdf, session.SiteId, sessionId: session.Id);

            return File(pdf, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> GenerateConvention(int id, int clientId)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Client)
                .FirstOrDefaultAsync(s => s.Id == id);

            var client = await _context.Clients.FindAsync(clientId);

            if (session == null || client == null)
                return NotFound();
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(session.SiteId) && session.SiteId != _siteContext.CurrentSiteId)
                return Forbid();

            var pdf = _documentService.GenerateConvention(session, client);
            var fileName = $"convention_session_{session.Id}_{client.Nom}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            await SaveGeneratedDocumentAsync(TypeDocument.Convention, fileName, pdf, session.SiteId, sessionId: session.Id, clientId: client.Id);

            return File(pdf, "application/pdf", fileName);
        }

        public async Task<IActionResult> Create()
        {
            var formationsQuery = _context.Formations.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                formationsQuery = formationsQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            var formations = await formationsQuery
                .OrderBy(f => f.Titre)
                .ToListAsync();
            ViewBag.Formations = formations;
            
            // Récupérer le formateur de la dernière session de chaque formation pour pré-sélection
            Dictionary<int, int> formationFormateurs = new Dictionary<int, int>();
            if (formations.Any())
            {
                var formationIds = formations.Select(f => f.Id).ToList();
                var derniereSessions = await _context.Sessions
                    .Where(s => formationIds.Contains(s.FormationId))
                    .GroupBy(s => s.FormationId)
                    .Select(g => new { FormationId = g.Key, FormateurId = g.OrderByDescending(s => s.DateDebut).First().FormateurId })
                    .ToListAsync();
                
                formationFormateurs = derniereSessions
                    .Where(s => s.FormateurId > 0)
                    .ToDictionary(s => s.FormationId, s => s.FormateurId);
            }
            
            ViewBag.FormationFormateurs = formationFormateurs;
            
            // Utiliser le pool de formateurs au lieu des utilisateurs
            var formateursQuery = _context.Formateurs
                .Where(f => f.Actif)
                .AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                formateursQuery = formateursQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Formateurs = await formateursQuery
                .OrderBy(f => f.Nom)
                .ThenBy(f => f.Prenom)
                .ToListAsync();

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }

            var sallesQuery = _context.Salles.AsQueryable();
            if (!_siteContext.IsAdmin)
                sallesQuery = sallesQuery.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            ViewBag.Salles = await sallesQuery.OrderBy(s => s.Nom).ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Session session)
        {
            // Logs détaillés pour déboguer
            _logger.LogInformation("=== DÉBUT CRÉATION SESSION ===");
            _logger.LogInformation($"FormationId depuis modèle: {session.FormationId}");
            _logger.LogInformation($"FormateurId depuis modèle: {session.FormateurId}");
            _logger.LogInformation($"DateDebut: {session.DateDebut}");
            _logger.LogInformation($"DateFin: {session.DateFin}");
            
            // Afficher toutes les clés du formulaire
            _logger.LogInformation("Clés du formulaire reçues:");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation($"  {key} = {Request.Form[key]}");
            }
            
            // Essayer de récupérer les valeurs depuis Request.Form si elles ne sont pas dans le modèle
            // AVANT de supprimer les erreurs de ModelState
            if (Request.Form.ContainsKey("FormationId"))
            {
                var formationIdStr = Request.Form["FormationId"].ToString();
                _logger.LogInformation($"FormationId depuis Request.Form (string): '{formationIdStr}'");
                if (int.TryParse(formationIdStr, out int formationId) && formationId > 0)
                {
                    session.FormationId = formationId;
                    _logger.LogInformation($"FormationId récupéré depuis Request.Form: {formationId}");
                }
                else
                {
                    _logger.LogWarning($"Impossible de parser FormationId: '{formationIdStr}'");
                }
            }
            else
            {
                _logger.LogWarning("FormationId n'est pas présent dans Request.Form");
            }
            
            if (Request.Form.ContainsKey("FormateurId"))
            {
                var formateurIdStr = Request.Form["FormateurId"].ToString();
                _logger.LogInformation($"FormateurId depuis Request.Form (string): '{formateurIdStr}'");
                if (int.TryParse(formateurIdStr, out int formateurId) && formateurId > 0)
                {
                    session.FormateurId = formateurId;
                    _logger.LogInformation($"FormateurId récupéré depuis Request.Form: {formateurId}");
                }
                else
                {
                    _logger.LogWarning($"Impossible de parser FormateurId: '{formateurIdStr}'");
                }
            }
            else
            {
                _logger.LogWarning("FormateurId n'est pas présent dans Request.Form");
            }

            if (Request.Form.ContainsKey("SalleId"))
            {
                var s = Request.Form["SalleId"].ToString();
                if (int.TryParse(s, out int salleId) && salleId > 0)
                    session.SalleId = salleId;
                else
                    session.SalleId = null;
            }
            
            _logger.LogInformation($"FormationId final: {session.FormationId}");
            _logger.LogInformation($"FormateurId final: {session.FormateurId}");
            
            // Afficher les erreurs de ModelState
            _logger.LogInformation("Erreurs ModelState avant suppression:");
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key]?.Errors;
                if (errors != null && errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        _logger.LogWarning($"  {key}: {error.ErrorMessage}");
                    }
                }
            }
            
            // Supprimer d'abord les erreurs de validation automatiques pour ces champs
            // pour permettre notre validation personnalisée
            // Supprimer les erreurs pour les IDs ET les objets de navigation
            ModelState.Remove("FormationId");
            ModelState.Remove("FormateurId");
            ModelState.Remove("Formation");  // Objet de navigation
            ModelState.Remove("Formateur");  // Objet de navigation
            
            // Valider manuellement les champs requis
            if (session.FormationId <= 0)
            {
                ModelState.AddModelError("FormationId", "La formation est requise.");
            }
            else
            {
                // Vérifier que la formation existe
                var formationExists = await _context.Formations.AnyAsync(f => f.Id == session.FormationId);
                if (!formationExists)
                {
                    ModelState.AddModelError("FormationId", "La formation sélectionnée n'existe pas.");
                }
            }
            
            if (session.FormateurId <= 0)
            {
                ModelState.AddModelError("FormateurId", "Le formateur est requis.");
            }
            else
            {
                // Vérifier que le formateur existe
                var formateurExists = await _context.Formateurs.AnyAsync(f => f.Id == session.FormateurId);
                if (!formateurExists)
                {
                    ModelState.AddModelError("FormateurId", "Le formateur sélectionné n'existe pas.");
                }
            }
            if (session.DateDebut == default(DateTime))
                ModelState.AddModelError("DateDebut", "La date de début est requise.");
            if (session.DateFin == default(DateTime))
                ModelState.AddModelError("DateFin", "La date de fin est requise.");
            if (session.DateFin < session.DateDebut)
                ModelState.AddModelError("DateFin", "La date de fin doit être postérieure à la date de début.");

            // Supprimer les erreurs de validation pour les champs optionnels
            ModelState.Remove("Statut");
            ModelState.Remove("Lieu");
            ModelState.Remove("SalleId");
            ModelState.Remove("Salle");
            
            // S'assurer que le statut a une valeur par défaut
            if (string.IsNullOrWhiteSpace(session.Statut))
                session.Statut = "Programmée";

            if (ModelState.IsValid)
            {
                try
                {
                    session.DateCreation = DateTime.Now;
                    session.DateModification = DateTime.Now;
                    session.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(session.SiteId)
                        ? session.SiteId
                        : _siteContext.CurrentSiteId;

                    // S'assurer que les champs optionnels ne sont pas null
                    session.Lieu = session.Lieu ?? string.Empty;
                    session.Statut = session.Statut ?? "Programmée";
                    session.CreePar = session.CreePar ?? "system";
                    session.ModifiePar = session.ModifiePar ?? "system";

                    var conflits = await _conflitsService.GetConflitsAsync(session, null);
                    if (conflits.Count > 0)
                    {
                        var msg = string.Join(" ; ", conflits.Select(c => $"{c.Raison}: {c.Session.Formation?.Titre ?? "Session"} ({c.Session.DateDebut:dd/MM/yyyy})"));
                        TempData["ConflitsWarning"] = "Conflit(s) détecté(s) : " + msg;
                    }

                    _context.Add(session);
                    await _context.SaveChangesAsync();

                    // Générer automatiquement les preuves Qualiopi pour la session
                    var qualiopiAutoProofService = HttpContext.RequestServices.GetRequiredService<IQualiopiAutoProofService>();
                    await qualiopiAutoProofService.AutoCreatePreuvesForSessionAsync(session.Id);

                    TempData["Success"] = "Session créée avec succès.";
                    return RedirectToAction(nameof(Details), new { id = session.Id });
                }
                catch (Exception ex)
                {
                    // Afficher l'exception interne pour plus de détails
                    var errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" | Détails : {ex.InnerException.Message}";
                    }
                    
                    // Vérifier si c'est une erreur de contrainte SQLite
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY constraint"))
                    {
                        if (ex.InnerException.Message.Contains("FormationId"))
                            ModelState.AddModelError("FormationId", "La formation sélectionnée n'existe pas.");
                        else if (ex.InnerException.Message.Contains("FormateurId"))
                            ModelState.AddModelError("FormateurId", "Le formateur sélectionné n'existe pas.");
                        else
                            ModelState.AddModelError("", "Une erreur de référence s'est produite. Vérifiez les relations.");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Erreur lors de la création : {errorMessage}");
                    }
                    
                    // Log complet pour déboguer
                    System.Diagnostics.Debug.WriteLine($"Erreur complète : {ex}");
                }
            }

            var formationsQuery = _context.Formations.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                formationsQuery = formationsQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Formations = await formationsQuery.OrderBy(f => f.Titre).ToListAsync();
            
            // Utiliser le pool de formateurs au lieu des utilisateurs
            var formateursQuery = _context.Formateurs
                .Where(f => f.Actif)
                .AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                formateursQuery = formateursQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Formateurs = await formateursQuery
                .OrderBy(f => f.Nom)
                .ThenBy(f => f.Prenom)
                .ToListAsync();

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }

            var sallesQueryRet = _context.Salles.AsQueryable();
            if (!_siteContext.IsAdmin)
                sallesQueryRet = sallesQueryRet.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            ViewBag.Salles = await sallesQueryRet.OrderBy(s => s.Nom).ToListAsync();

            return View(session);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(session.SiteId) && session.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            var formationsQuery = _context.Formations.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                formationsQuery = formationsQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Formations = await formationsQuery.OrderBy(f => f.Titre).ToListAsync();
            
            // Utiliser le pool de formateurs au lieu des utilisateurs
            var formateursQuery = _context.Formateurs
                .Where(f => f.Actif)
                .AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                formateursQuery = formateursQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Formateurs = await formateursQuery
                .OrderBy(f => f.Nom)
                .ThenBy(f => f.Prenom)
                .ToListAsync();

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }

            var sallesQueryEdit = _context.Salles.AsQueryable();
            if (!_siteContext.IsAdmin)
                sallesQueryEdit = sallesQueryEdit.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            ViewBag.Salles = await sallesQueryEdit.OrderBy(s => s.Nom).ToListAsync();

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Session session)
        {
            if (id != session.Id)
                return NotFound();

            // Supprimer les erreurs de validation pour les objets de navigation
            ModelState.Remove("Formation");
            ModelState.Remove("Formateur");
            ModelState.Remove("Salle");
            ModelState.Remove("SalleId");
            
            // Valider manuellement FormationId et FormateurId
            if (session.FormationId <= 0)
            {
                ModelState.AddModelError("FormationId", "La formation est requise.");
            }
            else
            {
                var formationExists = await _context.Formations.AnyAsync(f => f.Id == session.FormationId);
                if (!formationExists)
                {
                    ModelState.AddModelError("FormationId", "La formation sélectionnée n'existe pas.");
                }
            }
            
            if (session.FormateurId <= 0)
            {
                ModelState.AddModelError("FormateurId", "Le formateur est requis.");
            }
            else
            {
                var formateurExists = await _context.Formateurs.AnyAsync(f => f.Id == session.FormateurId);
                if (!formateurExists)
                {
                    ModelState.AddModelError("FormateurId", "Le formateur sélectionné n'existe pas.");
                }
            }

            if (ModelState.IsValid)
            {
                session.DateModification = DateTime.Now;
                session.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(session.SiteId)
                    ? session.SiteId
                    : _siteContext.CurrentSiteId;

                var conflits = await _conflitsService.GetConflitsAsync(session, session.Id);
                if (conflits.Count > 0)
                {
                    var msg = string.Join(" ; ", conflits.Select(c => $"{c.Raison}: {c.Session.Formation?.Titre ?? "Session"} ({c.Session.DateDebut:dd/MM/yyyy})"));
                    TempData["ConflitsWarning"] = "Conflit(s) détecté(s) : " + msg;
                }

                _context.Update(session);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = session.Id });
            }

            var formationsQuery = _context.Formations.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                formationsQuery = formationsQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Formations = await formationsQuery.OrderBy(f => f.Titre).ToListAsync();
            
            var formateursQuery = _context.Formateurs
                .Where(f => f.Actif)
                .AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                formateursQuery = formateursQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            ViewBag.Formateurs = await formateursQuery
                .OrderBy(f => f.Nom)
                .ThenBy(f => f.Prenom)
                .ToListAsync();

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }

            var sallesQueryEditRet = _context.Salles.AsQueryable();
            if (!_siteContext.IsAdmin)
                sallesQueryEditRet = sallesQueryEditRet.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            ViewBag.Salles = await sallesQueryEditRet.OrderBy(s => s.Nom).ToListAsync();

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cloturer(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(session.SiteId) && session.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            session.Statut = "Terminée";
            session.DateModification = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "La session a été clôturée.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(session.SiteId) && session.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            return View(session);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session != null && (_siteContext.IsAdmin || string.IsNullOrEmpty(session.SiteId) || session.SiteId == _siteContext.CurrentSiteId))
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task SaveGeneratedDocumentAsync(
            TypeDocument typeDocument,
            string fileName,
            byte[] content,
            string siteId,
            int? sessionId = null,
            int? clientId = null,
            int? stagiaireId = null)
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
                    Source = "Session"
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
