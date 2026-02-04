using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class FormateursController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly ISiteContext _siteContext;

        public FormateursController(FormationDbContext context, ISiteContext siteContext)
        {
            _context = context;
            _siteContext = siteContext;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Formateurs.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == _siteContext.CurrentSiteId);
            }

            var formateurs = await query
                .Include(f => f.Utilisateur)
                .OrderBy(f => f.Nom)
                .ThenBy(f => f.Prenom)
                .ToListAsync();

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            return View(formateurs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var formateur = await _context.Formateurs
                .Include(f => f.Utilisateur)
                .Include(f => f.Sessions)
                    .ThenInclude(s => s.Formation)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (formateur == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(formateur.SiteId) && formateur.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            return View(formateur);
        }

        public async Task<IActionResult> Create()
        {
            // Récupérer les utilisateurs qui peuvent être formateurs (sans compte formateur existant)
            var utilisateursQuery = _context.Utilisateurs
                .Where(u => u.Actif)
                .AsQueryable();
            
            if (!_siteContext.IsAdmin)
            {
                utilisateursQuery = utilisateursQuery.Where(u => string.IsNullOrEmpty(u.SiteId) || u.SiteId == _siteContext.CurrentSiteId);
            }

            var utilisateurs = await utilisateursQuery
                .Where(u => !_context.Formateurs.Any(f => f.UtilisateurId == u.Id))
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();

            ViewBag.Utilisateurs = utilisateurs.Select(u => new { 
                Id = u.Id, 
                NomComplet = $"{u.Prenom} {u.Nom} ({u.Email})" 
            }).ToList();

            // Récupérer tous les sites pour l'antenne de rattachement
            var allSites = await _context.Sites
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            
            // Toujours passer les sites pour l'antenne
            ViewBag.SitesForAntenne = allSites;

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }
            else
            {
                ViewBag.Sites = allSites;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Formateur formateur)
        {
            // Si un utilisateur est lié, pré-remplir les données
            if (formateur.UtilisateurId.HasValue)
            {
                var utilisateur = await _context.Utilisateurs.FindAsync(formateur.UtilisateurId.Value);
                if (utilisateur != null)
                {
                    if (string.IsNullOrWhiteSpace(formateur.Nom))
                        formateur.Nom = utilisateur.Nom;
                    if (string.IsNullOrWhiteSpace(formateur.Prenom))
                        formateur.Prenom = utilisateur.Prenom;
                    if (string.IsNullOrWhiteSpace(formateur.Email))
                        formateur.Email = utilisateur.Email ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(formateur.Telephone))
                        formateur.Telephone = utilisateur.Telephone ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(formateur.SiteId))
                        formateur.SiteId = utilisateur.SiteId ?? string.Empty;
                }
            }

            // Valider manuellement les champs requis
            if (string.IsNullOrWhiteSpace(formateur.Nom))
                ModelState.AddModelError("Nom", "Le nom est requis.");
            if (string.IsNullOrWhiteSpace(formateur.Prenom))
                ModelState.AddModelError("Prenom", "Le prénom est requis.");

            // Supprimer les erreurs de validation pour SiteId et NumeroFormateur (optionnels)
            ModelState.Remove("SiteId");
            ModelState.Remove("NumeroFormateur");
            
            // Vérifier si un SiteId est fourni et s'il existe
            if (!string.IsNullOrWhiteSpace(formateur.SiteId) && _siteContext.IsAdmin)
            {
                var siteExists = await _context.Sites.AnyAsync(s => s.SiteId == formateur.SiteId);
                if (!siteExists)
                {
                    ModelState.AddModelError("SiteId", "Le site sélectionné n'existe pas.");
                }
            }

            // Log des erreurs de validation pour déboguer
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) })
                    .ToList();
                
                // Log pour déboguer
                foreach (var error in errors)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur validation {error.Field}: {string.Join(", ", error.Errors ?? new List<string>())}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    formateur.DateCreation = DateTime.Now;
                    formateur.DateModification = DateTime.Now;
                    
                    // SiteId est optionnel - utiliser le site courant si non spécifié
                    if (string.IsNullOrWhiteSpace(formateur.SiteId))
                    {
                        formateur.SiteId = _siteContext.CurrentSiteId ?? string.Empty;
                    }
                    else if (!_siteContext.IsAdmin)
                    {
                        // Si non-admin, forcer le site courant
                        formateur.SiteId = _siteContext.CurrentSiteId ?? string.Empty;
                    }

                    // Vérifier que le SiteId existe dans la table Sites si fourni
                    if (!string.IsNullOrWhiteSpace(formateur.SiteId))
                    {
                        var siteExists = await _context.Sites.AnyAsync(s => s.SiteId == formateur.SiteId);
                        if (!siteExists)
                        {
                            // Si le site n'existe pas, utiliser le site courant ou laisser vide
                            formateur.SiteId = _siteContext.CurrentSiteId ?? string.Empty;
                        }
                    }

                    // S'assurer que les champs optionnels ne sont pas null
                    formateur.Email = formateur.Email ?? string.Empty;
                    formateur.Telephone = formateur.Telephone ?? string.Empty;
                    formateur.StatutProfessionnel = formateur.StatutProfessionnel ?? string.Empty;
                    formateur.NumeroFormateur = formateur.NumeroFormateur ?? string.Empty;
                    formateur.AntenneRattachement = formateur.AntenneRattachement ?? string.Empty;
                    formateur.Biographie = formateur.Biographie ?? string.Empty;
                    formateur.Competences = formateur.Competences ?? string.Empty;
                    formateur.Experience = formateur.Experience ?? string.Empty;
                    formateur.Formations = formateur.Formations ?? string.Empty;
                    formateur.CreePar = formateur.CreePar ?? "system";
                    formateur.ModifiePar = formateur.ModifiePar ?? "system";

                    _context.Add(formateur);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Formateur créé avec succès.";
                    return RedirectToAction(nameof(Details), new { id = formateur.Id });
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
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("UNIQUE constraint"))
                    {
                        if (ex.InnerException.Message.Contains("Email"))
                            ModelState.AddModelError("Email", "Un formateur avec cet email existe déjà.");
                        else if (ex.InnerException.Message.Contains("NumeroFormateur"))
                            ModelState.AddModelError("NumeroFormateur", "Un formateur avec ce numéro existe déjà.");
                        else
                            ModelState.AddModelError("", "Une contrainte d'unicité est violée. Vérifiez les champs uniques (email, numéro formateur).");
                    }
                    else if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY constraint"))
                    {
                        ModelState.AddModelError("SiteId", "Le site sélectionné n'existe pas dans la base de données.");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Erreur lors de la création : {errorMessage}");
                    }
                    
                    // Log complet pour déboguer
                    System.Diagnostics.Debug.WriteLine($"Erreur complète : {ex}");
                }
            }

            // Recharger les données pour la vue
            var utilisateursQuery = _context.Utilisateurs
                .Where(u => u.Actif)
                .AsQueryable();
            
            if (!_siteContext.IsAdmin)
            {
                utilisateursQuery = utilisateursQuery.Where(u => string.IsNullOrEmpty(u.SiteId) || u.SiteId == _siteContext.CurrentSiteId);
            }

            var utilisateurs = await utilisateursQuery
                .Where(u => !_context.Formateurs.Any(f => f.UtilisateurId == u.Id))
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();

            ViewBag.Utilisateurs = utilisateurs.Select(u => new { 
                Id = u.Id, 
                NomComplet = $"{u.Prenom} {u.Nom} ({u.Email})" 
            }).ToList();

            // Récupérer tous les sites pour l'antenne de rattachement
            var allSites = await _context.Sites
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            
            // Toujours passer les sites pour l'antenne
            ViewBag.SitesForAntenne = allSites;

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }
            else
            {
                ViewBag.Sites = allSites;
            }

            return View(formateur);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var formateur = await _context.Formateurs
                .Include(f => f.Utilisateur)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (formateur == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(formateur.SiteId) && formateur.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            // Récupérer les utilisateurs disponibles
            var utilisateursQuery = _context.Utilisateurs
                .Where(u => u.Actif)
                .AsQueryable();
            
            if (!_siteContext.IsAdmin)
            {
                utilisateursQuery = utilisateursQuery.Where(u => string.IsNullOrEmpty(u.SiteId) || u.SiteId == _siteContext.CurrentSiteId);
            }

            var utilisateurs = await utilisateursQuery
                .Where(u => !_context.Formateurs.Any(f => f.UtilisateurId == u.Id && f.Id != id) || u.Id == formateur.UtilisateurId)
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();

            ViewBag.Utilisateurs = utilisateurs.Select(u => new { 
                Id = u.Id, 
                NomComplet = $"{u.Prenom} {u.Nom} ({u.Email})" 
            }).ToList();

            // Récupérer tous les sites pour l'antenne de rattachement
            var allSites = await _context.Sites
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            ViewBag.SitesForAntenne = allSites;

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }

            return View(formateur);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Formateur formateur)
        {
            if (id != formateur.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    formateur.DateModification = DateTime.Now;
                    formateur.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(formateur.SiteId)
                        ? formateur.SiteId
                        : _siteContext.CurrentSiteId;

                    _context.Update(formateur);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Formateurs.Any(e => e.Id == id))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Details), new { id = formateur.Id });
            }

            // Recharger les données pour la vue
            var utilisateursQuery = _context.Utilisateurs
                .Where(u => u.Actif)
                .AsQueryable();
            
            if (!_siteContext.IsAdmin)
            {
                utilisateursQuery = utilisateursQuery.Where(u => string.IsNullOrEmpty(u.SiteId) || u.SiteId == _siteContext.CurrentSiteId);
            }

            var utilisateurs = await utilisateursQuery
                .Where(u => !_context.Formateurs.Any(f => f.UtilisateurId == u.Id && f.Id != id) || u.Id == formateur.UtilisateurId)
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();

            ViewBag.Utilisateurs = utilisateurs.Select(u => new { 
                Id = u.Id, 
                NomComplet = $"{u.Prenom} {u.Nom} ({u.Email})" 
            }).ToList();

            // Récupérer tous les sites pour l'antenne de rattachement
            var allSites = await _context.Sites
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
            ViewBag.SitesForAntenne = allSites;

            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }

            return View(formateur);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var formateur = await _context.Formateurs
                .Include(f => f.Utilisateur)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (formateur == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(formateur.SiteId) && formateur.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            return View(formateur);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var formateur = await _context.Formateurs.FindAsync(id);
            if (formateur == null)
                return NotFound();

            // Vérifier s'il y a des sessions associées
            var hasSessions = await _context.Sessions.AnyAsync(s => s.FormateurId == id);
            if (hasSessions)
            {
                TempData["Error"] = "Impossible de supprimer ce formateur car il a des sessions associées.";
                return RedirectToAction(nameof(Index));
            }

            _context.Formateurs.Remove(formateur);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Formateur supprimé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUserInfo(int id)
        {
            var user = await _context.Utilisateurs.FindAsync(id);
            if (user == null)
                return NotFound();

            return Json(new
            {
                nom = user.Nom,
                prenom = user.Prenom,
                email = user.Email ?? string.Empty,
                telephone = user.Telephone ?? string.Empty
            });
        }
    }
}
