using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class AgendaController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly ISiteContext _siteContext;

        public AgendaController(FormationDbContext context, ISiteContext siteContext)
        {
            _context = context;
            _siteContext = siteContext;
        }

        public IActionResult Index()
        {
            ViewBag.ShowSite = _siteContext.IsAdmin;
            ViewBag.SiteNames = _siteContext.GetSites().ToDictionary(s => s.SiteId, s => s.Name);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Events(DateTime? start, DateTime? end)
        {
            var query = _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .AsQueryable();

            if (!_siteContext.IsAdmin)
                query = query.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);

            var deb = start ?? DateTime.Today.AddMonths(-1);
            var fin = end ?? DateTime.Today.AddMonths(2);
            var sessions = await query
                .Where(s => s.DateDebut.Date <= fin && s.DateFin.Date >= deb)
                .OrderBy(s => s.DateDebut)
                .ToListAsync();

            var events = new List<object>();
            foreach (var s in sessions)
            {
                var titre = s.Formation?.Titre ?? "Session";
                var formateur = s.Formateur != null ? $"{s.Formateur.Prenom} {s.Formateur.Nom}" : "";
                var lieu = string.IsNullOrWhiteSpace(s.Lieu) ? "" : $" • {s.Lieu}";
                var url = Url.Action("Details", "Sessions", new { id = s.Id });
                events.Add(new
                {
                    id = s.Id,
                    title = $"{titre}{(!string.IsNullOrEmpty(formateur) ? $" ({formateur})" : "")}{lieu}",
                    start = s.DateDebut.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = s.DateFin.ToString("yyyy-MM-ddTHH:mm:ss"),
                    url,
                    backgroundColor = StatutColor(s.Statut),
                    borderColor = StatutColor(s.Statut),
                    extendedProps = new
                    {
                        formationTitre = s.Formation?.Titre,
                        formateurNom = formateur,
                        lieu = s.Lieu,
                        statut = s.Statut,
                        siteId = s.SiteId
                    }
                });
            }

            return Json(events);
        }

        private static string StatutColor(string? statut)
        {
            return statut switch
            {
                "Programmée" => "#0d6efd",
                "En cours" => "#ffc107",
                "Terminée" => "#6c757d",
                "Annulée" => "#dc3545",
                _ => "#6c757d"
            };
        }
    }
}
