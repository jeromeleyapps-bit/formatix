using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly UserManager<Utilisateur> _userManager;
        private readonly ISiteContext _siteContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            FormationDbContext context,
            UserManager<Utilisateur> userManager,
            ISiteContext siteContext,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<SettingsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _siteContext = siteContext;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var sites = await _context.Sites
                .OrderBy(s => s.SiteId)
                .ToListAsync();

            var users = await _context.Utilisateurs
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();

            var model = new SettingsViewModel
            {
                Sites = sites,
                Users = users,
                CurrentSiteId = _configuration["Sync:SiteId"] ?? "SITE_01",
                CentralUrl = _configuration["Sync:CentralUrl"] ?? "https://localhost:5001",
                SyncIntervalMinutes = _configuration.GetValue<int>("Sync:IntervalMinutes", 15),
                Organization = new OrganizationSettings
                {
                    BaseUrlInscription = _configuration["AppSettings:BaseUrlInscription"] ?? string.Empty,
                    NomOrganisme = _configuration["AppSettings:NomOrganisme"] ?? string.Empty,
                    SIRET = _configuration["AppSettings:SIRET"] ?? string.Empty,
                    Adresse = _configuration["AppSettings:Adresse"] ?? string.Empty,
                    CodePostal = _configuration["AppSettings:CodePostal"] ?? string.Empty,
                    Ville = _configuration["AppSettings:Ville"] ?? string.Empty,
                    Email = _configuration["AppSettings:Email"] ?? string.Empty,
                    Telephone = _configuration["AppSettings:Telephone"] ?? string.Empty
                },
                Qualiopi = new QualiopiSettings
                {
                    Certification = _configuration.GetValue<bool>("Qualiopi:Certification", false),
                    NumeroCertification = _configuration["Qualiopi:NumeroCertification"] ?? string.Empty,
                    DateCertification = _configuration["Qualiopi:DateCertification"] ?? string.Empty,
                    DateProchaineEvaluation = _configuration["Qualiopi:DateProchaineEvaluation"] ?? string.Empty,
                    OrganismeCertificateur = _configuration["Qualiopi:OrganismeCertificateur"] ?? string.Empty
                }
            };

            ViewBag.SiteNames = sites.ToDictionary(s => s.SiteId, s => s.Name);
            
            // Préparer les données de diagnostic pour l'onglet
            var diagnosticsModel = new SystemDiagnosticsViewModel();
            PrepareDiagnosticsModel(diagnosticsModel);
            ViewBag.DiagnosticsModel = diagnosticsModel;
            
            return View(model);
        }
        
        private void PrepareDiagnosticsModel(SystemDiagnosticsViewModel model)
        {
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
                model.DatabaseCanConnect = _context.Database.CanConnectAsync().Result;
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
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSite(string siteId, string name)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "SiteId et nom requis.";
                return RedirectToAction(nameof(Index));
            }

            var exists = await _context.Sites.AnyAsync(s => s.SiteId == siteId);
            if (exists)
            {
                TempData["Error"] = "Ce SiteId existe déjà.";
                return RedirectToAction(nameof(Index));
            }

            _context.Sites.Add(new Site
            {
                SiteId = siteId,
                Name = name,
                IsActive = true
            });
            await _context.SaveChangesAsync();

            // Créer les 32 indicateurs Qualiopi pour le nouveau site
            await CreateQualiopiIndicatorsForSiteAsync(siteId);

            TempData["Success"] = "Site ajouté avec les 32 indicateurs Qualiopi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameSite(string siteId, string name)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
            {
                return NotFound();
            }

            site.Name = name;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Site mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSite(string siteId)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
            {
                return NotFound();
            }

            site.IsActive = !site.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = site.IsActive ? "Site activé." : "Site désactivé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Formulaire utilisateur invalide.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Role != RoleUtilisateur.Administrateur && string.IsNullOrWhiteSpace(model.SiteId))
            {
                TempData["Error"] = "Un site est requis pour ce rôle.";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                TempData["Error"] = "Un utilisateur avec cet email existe déjà.";
                return RedirectToAction(nameof(Index));
            }

            var user = new Utilisateur
            {
                UserName = model.Email,
                Email = model.Email,
                Nom = model.Nom,
                Prenom = model.Prenom,
                Role = model.Role,
                SiteId = model.SiteId ?? string.Empty,
                Actif = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                // Traduction des erreurs Identity en français
                var erreursFr = result.Errors.Select(e => TraduireErreurIdentity(e.Code, e.Description));
                TempData["Error"] = string.Join(" | ", erreursFr);
                return RedirectToAction(nameof(Index));
            }

            var roleName = model.Role.ToString();
            await _userManager.AddToRoleAsync(user, roleName);

            TempData["Success"] = "Utilisateur créé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(int id, RoleUtilisateur role, string siteId, bool actif)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            if (role != RoleUtilisateur.Administrateur && string.IsNullOrWhiteSpace(siteId))
            {
                TempData["Error"] = "Un site est requis pour ce rôle.";
                return RedirectToAction(nameof(Index));
            }

            user.Role = role;
            user.SiteId = role == RoleUtilisateur.Administrateur ? string.Empty : siteId;
            user.Actif = actif;
            user.DateModification = DateTime.Now;

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
            }
            await _userManager.AddToRoleAsync(user, role.ToString());

            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Utilisateur mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(int id, string newPassword)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["Error"] = "Mot de passe invalide (min 8 caractères).";
                return RedirectToAction(nameof(Index));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                var erreursFr = result.Errors.Select(e => TraduireErreurIdentity(e.Code, e.Description));
                TempData["Error"] = string.Join(" | ", erreursFr);
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Mot de passe réinitialisé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            // Vérifier qu'il reste au moins un autre admin actif si on supprime un admin
            if (user.Role == RoleUtilisateur.Administrateur)
            {
                var autresAdminsActifs = await _context.Utilisateurs
                    .CountAsync(u => u.Role == RoleUtilisateur.Administrateur && u.Actif && u.Id != id);
                
                if (autresAdminsActifs == 0)
                {
                    TempData["Error"] = "Impossible de supprimer le dernier administrateur actif.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Utilisateur supprimé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSite(string siteId, string transferToSiteId)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(siteId))
            {
                TempData["Error"] = "Site source requis.";
                return RedirectToAction(nameof(Index));
            }

            var site = await _context.Sites.FindAsync(siteId);
            if (site == null)
            {
                return NotFound();
            }

            // Vérifier si le site a des données (exclure les indicateurs Qualiopi qui sont des données système)
            var hasData = await _context.Formations.AnyAsync(f => f.SiteId == siteId) ||
                         await _context.Sessions.AnyAsync(s => s.SiteId == siteId) ||
                         await _context.Clients.AnyAsync(c => c.SiteId == siteId) ||
                         await _context.Stagiaires.AnyAsync(s => s.SiteId == siteId) ||
                         await _context.Documents.AnyAsync(d => d.SiteId == siteId) ||
                         await _context.PreuvesQualiopi.AnyAsync(p => p.SiteId == siteId) ||
                         await _context.Utilisateurs.Where(u => u.SiteId == siteId && u.Role != RoleUtilisateur.Administrateur).AnyAsync();

            // Si le site a des données, un site de transfert est requis
            if (hasData)
            {
                if (string.IsNullOrWhiteSpace(transferToSiteId))
                {
                    TempData["Error"] = "Ce site contient des données. Veuillez sélectionner un site de transfert.";
                    return RedirectToAction(nameof(Index));
                }

                if (siteId == transferToSiteId)
                {
                    TempData["Error"] = "Le site de transfert doit être différent.";
                    return RedirectToAction(nameof(Index));
                }

                var target = await _context.Sites.FindAsync(transferToSiteId);
                if (target == null)
                {
                    TempData["Error"] = "Site de transfert introuvable.";
                    return RedirectToAction(nameof(Index));
                }

                await ReassignSiteDataAsync(siteId, transferToSiteId);
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Site supprimé et données transférées.";
            }
            else
            {
                // Site vide, suppression directe (supprimer aussi les indicateurs Qualiopi du site)
                await _context.IndicateursQualiopi.Where(i => i.SiteId == siteId)
                    .ExecuteDeleteAsync();
                
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Site supprimé.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateSyncSettings(string siteId, string centralUrl, int intervalMinutes)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(centralUrl))
            {
                TempData["Error"] = "Paramètres invalides.";
                return RedirectToAction(nameof(Index));
            }

            var appSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            var json = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(appSettingsPath));
            if (json == null)
            {
                TempData["Error"] = "Impossible de lire appsettings.json.";
                return RedirectToAction(nameof(Index));
            }

            json["Sync"] ??= new System.Text.Json.Nodes.JsonObject();
            json["Sync"]!["SiteId"] = siteId;
            json["Sync"]!["CentralUrl"] = centralUrl;
            json["Sync"]!["IntervalMinutes"] = intervalMinutes;

            System.IO.File.WriteAllText(appSettingsPath, json.ToJsonString(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }));

            TempData["Success"] = "Paramètres sync enregistrés. Redémarrage requis.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrganizationSettings(OrganizationSettings model)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var appSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            var json = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(appSettingsPath));
            if (json == null)
            {
                TempData["Error"] = "Impossible de lire appsettings.json.";
                return RedirectToAction(nameof(Index));
            }

            json["AppSettings"] ??= new System.Text.Json.Nodes.JsonObject();
            json["AppSettings"]!["BaseUrlInscription"] = model.BaseUrlInscription ?? string.Empty;
            json["AppSettings"]!["NomOrganisme"] = model.NomOrganisme;
            json["AppSettings"]!["SIRET"] = model.SIRET;
            json["AppSettings"]!["Adresse"] = model.Adresse;
            json["AppSettings"]!["CodePostal"] = model.CodePostal;
            json["AppSettings"]!["Ville"] = model.Ville;
            json["AppSettings"]!["Email"] = model.Email;
            json["AppSettings"]!["Telephone"] = model.Telephone;

            System.IO.File.WriteAllText(appSettingsPath, json.ToJsonString(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }));

            TempData["Success"] = "Paramètres organisme enregistrés. Redémarrage requis.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQualiopiSettings(QualiopiSettings model)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var appSettingsPath = Path.Combine(_environment.ContentRootPath, "appsettings.json");
            var json = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(appSettingsPath));
            if (json == null)
            {
                TempData["Error"] = "Impossible de lire appsettings.json.";
                return RedirectToAction(nameof(Index));
            }

            json["Qualiopi"] ??= new System.Text.Json.Nodes.JsonObject();
            json["Qualiopi"]!["Certification"] = model.Certification;
            json["Qualiopi"]!["NumeroCertification"] = model.NumeroCertification;
            json["Qualiopi"]!["DateCertification"] = model.DateCertification;
            json["Qualiopi"]!["DateProchaineEvaluation"] = model.DateProchaineEvaluation;
            json["Qualiopi"]!["OrganismeCertificateur"] = model.OrganismeCertificateur;

            System.IO.File.WriteAllText(appSettingsPath, json.ToJsonString(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }));

            TempData["Success"] = "Paramètres Qualiopi enregistrés. Redémarrage requis.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLogo(IFormFile logoFile)
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            if (logoFile == null || logoFile.Length == 0)
            {
                TempData["Error"] = "Aucun fichier sélectionné.";
                return RedirectToAction(nameof(Index));
            }

            // Vérifier l'extension
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
            var extension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Format de fichier non supporté. Utilisez PNG ou JPG.";
                return RedirectToAction(nameof(Index));
            }

            // Vérifier la taille (max 5MB)
            if (logoFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Le fichier est trop volumineux (max 5MB).";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Créer le dossier uploads s'il n'existe pas
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Supprimer l'ancien logo s'il existe
                var oldLogoPath = Path.Combine(uploadsDir, "logo.png");
                var oldLogoPathJpg = Path.Combine(uploadsDir, "logo.jpg");
                if (System.IO.File.Exists(oldLogoPath))
                {
                    System.IO.File.Delete(oldLogoPath);
                }
                if (System.IO.File.Exists(oldLogoPathJpg))
                {
                    System.IO.File.Delete(oldLogoPathJpg);
                }

                // Sauvegarder le nouveau logo (toujours en .png pour uniformité)
                var logoPath = Path.Combine(uploadsDir, "logo.png");
                using (var stream = new FileStream(logoPath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                TempData["Success"] = "Logo uploadé avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload du logo");
                TempData["Error"] = $"Erreur lors de l'upload : {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteLogo()
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            try
            {
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                var logoPath = Path.Combine(uploadsDir, "logo.png");
                var logoPathJpg = Path.Combine(uploadsDir, "logo.jpg");

                if (System.IO.File.Exists(logoPath))
                {
                    System.IO.File.Delete(logoPath);
                    TempData["Success"] = "Logo supprimé.";
                }
                else if (System.IO.File.Exists(logoPathJpg))
                {
                    System.IO.File.Delete(logoPathJpg);
                    TempData["Success"] = "Logo supprimé.";
                }
                else
                {
                    TempData["Error"] = "Aucun logo à supprimer.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du logo");
                TempData["Error"] = $"Erreur lors de la suppression : {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportSites()
        {
            if (!_siteContext.IsAdmin)
            {
                return Forbid();
            }

            var sites = await _context.Sites
                .OrderBy(s => s.SiteId)
                .ToListAsync();

            var lines = new List<string> { "SiteId,Name,IsActive" };
            lines.AddRange(sites.Select(s =>
                $"{s.SiteId},{s.Name.Replace(",", " ")},{s.IsActive}"));

            var csv = System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines));
            return File(csv, "text/csv", "sites.csv");
        }

        private async Task ReassignSiteDataAsync(string fromSiteId, string toSiteId)
        {
            await _context.Formations.Where(f => f.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
            await _context.Sessions.Where(s => s.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
            await _context.Clients.Where(c => c.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
            await _context.Stagiaires.Where(s => s.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
            await _context.Documents.Where(d => d.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
            await _context.PreuvesQualiopi.Where(p => p.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
            await _context.SessionClients.Where(sc => sc.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
            await _context.ActionsVeille.Where(a => a.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
            await _context.Utilisateurs.Where(u => u.SiteId == fromSiteId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SiteId, toSiteId));
        }

        private async Task CreateQualiopiIndicatorsForSiteAsync(string siteId)
        {
            // Vérifier si les indicateurs existent déjà
            var existing = await _context.IndicateursQualiopi
                .Where(i => i.SiteId == siteId)
                .AnyAsync();

            if (existing)
            {
                return; // Les indicateurs existent déjà
            }

            var definitions = new List<(string Code, string Label, int Critere)>
            {
                ("1", "Information du public", 1),
                ("2", "Indicateurs de résultats", 1),
                ("3", "Taux d'obtention des certifications", 1),
                ("4", "Analyse du besoin", 2),
                ("5", "Objectifs de la prestation", 2),
                ("6", "Contenus et modalités", 2),
                ("7", "Contenus et exigences", 2),
                ("8", "Positionnement à l'entrée", 2),
                ("9", "Conditions de déroulement", 3),
                ("10", "Adaptation de la prestation", 3),
                ("11", "Atteinte des objectifs", 3),
                ("12", "Engagement des bénéficiaires", 3),
                ("13", "Coordination des apprentis", 3),
                ("14", "Exercice de la citoyenneté", 3),
                ("15", "Droits et devoirs de l'apprenti", 3),
                ("16", "Présentation à la certification", 3),
                ("17", "Moyens humains et techniques", 4),
                ("18", "Coordination des acteurs", 4),
                ("19", "Ressources pédagogiques", 4),
                ("20", "Personnels dédiés", 4),
                ("21", "Compétences des acteurs", 5),
                ("22", "Gestion de la compétence", 5),
                ("23", "Veille légale et réglementaire", 6),
                ("24", "Veille des emplois et métiers", 6),
                ("25", "Veille pédagogique et technologique", 6),
                ("26", "Situation de handicap", 6),
                ("27", "Disposition sous-traitance", 6),
                ("28", "Formation en situation de travail", 6),
                ("29", "Insertion professionnelle", 6),
                ("30", "Recueil des appréciations", 7),
                ("31", "Traitement des réclamations", 7),
                ("32", "Amélioration continue", 7)
            };

            var now = DateTime.Now;
            var indicators = definitions.Select(def => new IndicateurQualiopi
            {
                CodeIndicateur = def.Code,
                Libelle = def.Label,
                Critere = def.Critere,
                NiveauPreuveRequis = IndicateurQualiopi.NiveauPreuve.Moyen,
                DateCreation = now,
                DateModification = now,
                CreePar = "system",
                ModifiePar = "system",
                SiteId = siteId
            }).ToList();

            _context.IndicateursQualiopi.AddRange(indicators);
            await _context.SaveChangesAsync();
        }

        private static string TraduireErreurIdentity(string code, string descriptionOriginale)
        {
            return code switch
            {
                "PasswordTooShort" => "Le mot de passe doit contenir au moins 8 caractères.",
                "PasswordRequiresDigit" => "Le mot de passe doit contenir au moins un chiffre (0-9).",
                "PasswordRequiresUpper" => "Le mot de passe doit contenir au moins une majuscule (A-Z).",
                "PasswordRequiresLower" => "Le mot de passe doit contenir au moins une minuscule (a-z).",
                "PasswordRequiresNonAlphanumeric" => "Le mot de passe doit contenir au moins un caractère spécial.",
                "PasswordRequiresUniqueChars" => "Le mot de passe doit contenir des caractères uniques.",
                "DuplicateUserName" => "Ce nom d'utilisateur est déjà utilisé.",
                "DuplicateEmail" => "Cette adresse email est déjà utilisée.",
                "InvalidEmail" => "L'adresse email est invalide.",
                "InvalidUserName" => "Le nom d'utilisateur est invalide.",
                "UserAlreadyHasPassword" => "L'utilisateur a déjà un mot de passe.",
                "UserLockoutNotEnabled" => "Le verrouillage n'est pas activé pour cet utilisateur.",
                "UserAlreadyInRole" => "L'utilisateur a déjà ce rôle.",
                "UserNotInRole" => "L'utilisateur n'a pas ce rôle.",
                "PasswordMismatch" => "Le mot de passe est incorrect.",
                "InvalidToken" => "Le token est invalide.",
                "LoginAlreadyAssociated" => "Ce login est déjà associé à un compte.",
                "DefaultError" => "Une erreur est survenue.",
                _ => descriptionOriginale // Retourner la description originale si pas de traduction
            };
        }
    }
}
