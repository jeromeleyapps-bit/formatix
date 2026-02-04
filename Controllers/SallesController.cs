using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class SallesController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly ISiteContext _siteContext;

        public SallesController(FormationDbContext context, ISiteContext siteContext)
        {
            _context = context;
            _siteContext = siteContext;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Salles.AsQueryable();
            if (!_siteContext.IsAdmin)
                query = query.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);

            var salles = await query.OrderBy(s => s.Nom).ToListAsync();
            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(x => x.SiteId, x => x.Name);
            return View(salles);
        }

        public async Task<IActionResult> Details(int id)
        {
            var salle = await _context.Salles
                .Include(s => s.Sessions)
                    .ThenInclude(s => s.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (salle == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(salle.SiteId) && salle.SiteId != _siteContext.CurrentSiteId))
                return NotFound();
            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(x => x.SiteId, x => x.Name);
            return View(salle);
        }

        public IActionResult Create()
        {
            if (_siteContext.IsAdmin)
                ViewBag.Sites = new SelectList(_siteContext.GetSites(), "SiteId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Salle salle)
        {
            salle.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(salle.SiteId) ? salle.SiteId : (_siteContext.CurrentSiteId ?? string.Empty);
            salle.DateCreation = salle.DateModification = DateTime.Now;
            salle.CreePar = salle.ModifiePar = User.Identity?.Name ?? "app";
            _context.Salles.Add(salle);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Salle créée.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var salle = await _context.Salles.FindAsync(id);
            if (salle == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(salle.SiteId) && salle.SiteId != _siteContext.CurrentSiteId))
                return NotFound();
            if (_siteContext.IsAdmin)
                ViewBag.Sites = new SelectList(_siteContext.GetSites(), "SiteId", "Name", salle.SiteId);
            return View(salle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Salle salle)
        {
            if (id != salle.Id) return NotFound();
            var existing = await _context.Salles.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(existing.SiteId) && existing.SiteId != _siteContext.CurrentSiteId))
                return NotFound();
            existing.Nom = salle.Nom;
            existing.Capacite = salle.Capacite;
            existing.Adresse = salle.Adresse ?? string.Empty;
            if (_siteContext.IsAdmin && !string.IsNullOrWhiteSpace(salle.SiteId))
                existing.SiteId = salle.SiteId;
            existing.DateModification = DateTime.Now;
            existing.ModifiePar = User.Identity?.Name ?? "app";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Salle mise à jour.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var salle = await _context.Salles
                .Include(s => s.Sessions)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (salle == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(salle.SiteId) && salle.SiteId != _siteContext.CurrentSiteId))
                return NotFound();
            if (salle.Sessions?.Count > 0)
            {
                TempData["Error"] = "Impossible de supprimer cette salle : des sessions y sont associées.";
                return RedirectToAction(nameof(Index));
            }
            return View(salle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var salle = await _context.Salles.Include(s => s.Sessions).FirstOrDefaultAsync(s => s.Id == id);
            if (salle == null) return NotFound();
            if (salle.Sessions?.Count > 0)
            {
                TempData["Error"] = "Impossible de supprimer cette salle : des sessions y sont associées.";
                return RedirectToAction(nameof(Index));
            }
            _context.Salles.Remove(salle);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Salle supprimée.";
            return RedirectToAction(nameof(Index));
        }
    }
}
