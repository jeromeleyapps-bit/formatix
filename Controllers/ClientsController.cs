using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly ISiteContext _siteContext;

        public ClientsController(FormationDbContext context, ISiteContext siteContext)
        {
            _context = context;
            _siteContext = siteContext;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Clients.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(c => string.IsNullOrEmpty(c.SiteId) || c.SiteId == _siteContext.CurrentSiteId);
            }

            var clients = await query
                .OrderBy(c => c.Nom)
                .ToListAsync();

            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);

            return View(clients);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (_siteContext.IsAdmin)
            {
                ViewBag.Sites = _siteContext.GetSites();
            }
            return View(new Client());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            if (!ModelState.IsValid)
            {
                return View(client);
            }

            var now = DateTime.Now;
            client.DateCreation = now;
            client.DateModification = now;
            client.CreePar = User.Identity?.Name ?? "system";
            client.ModifiePar = User.Identity?.Name ?? "system";
            client.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(client.SiteId)
                ? client.SiteId
                : _siteContext.CurrentSiteId;

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
