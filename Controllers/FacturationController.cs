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
    public class FacturationController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly ISiteContext _siteContext;
        private readonly IFacturationService _facturation;

        public FacturationController(FormationDbContext context, ISiteContext siteContext, IFacturationService facturation)
        {
            _context = context;
            _siteContext = siteContext;
            _facturation = facturation;
        }

        public async Task<IActionResult> Index()
        {
            var siteId = _siteContext.CurrentSiteId;
            var devisQuery = _context.Devis.Include(d => d.Client).Include(d => d.Session).ThenInclude(s => s!.Formation).AsQueryable();
            var factureQuery = _context.Factures.Include(f => f.Client).Include(f => f.Session).ThenInclude(s => s!.Formation).Include(f => f.Devis).AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                devisQuery = devisQuery.Where(d => string.IsNullOrEmpty(d.SiteId) || d.SiteId == siteId);
                factureQuery = factureQuery.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == siteId);
            }
            var devis = await devisQuery.OrderByDescending(d => d.DateCreation).ToListAsync();
            var factures = await factureQuery.OrderByDescending(f => f.DateEmission).ToListAsync();
            ViewBag.Devis = devis;
            ViewBag.Factures = factures;
            return View();
        }

        public async Task<IActionResult> CreateDevis()
        {
            await FillViewBag();
            var next = _facturation.GetNextNumeroDevis();
            ViewBag.NextNumero = next;
            return View(new Devis { Numero = next, DateCreation = DateTime.Today, TauxTVA = 20 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDevis(Devis devis)
        {
            devis.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(devis.SiteId) ? devis.SiteId : (_siteContext.CurrentSiteId ?? "");
            if (string.IsNullOrWhiteSpace(devis.Numero)) devis.Numero = _facturation.GetNextNumeroDevis();
            _context.Devis.Add(devis);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Devis créé.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CreateFacture()
        {
            await FillViewBag();
            var devisQuery = _context.Devis.AsQueryable();
            if (!_siteContext.IsAdmin) devisQuery = devisQuery.Where(d => string.IsNullOrEmpty(d.SiteId) || d.SiteId == _siteContext.CurrentSiteId);
            ViewBag.Devis = new SelectList(await devisQuery.OrderByDescending(d => d.DateCreation).ToListAsync(), "Id", "Numero");
            ViewBag.NextNumero = _facturation.GetNextNumeroFacture();
            return View(new Facture { Numero = _facturation.GetNextNumeroFacture(), DateEmission = DateTime.Today, TauxTVA = 20 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFacture(Facture facture)
        {
            facture.SiteId = _siteContext.IsAdmin && !string.IsNullOrWhiteSpace(facture.SiteId) ? facture.SiteId : (_siteContext.CurrentSiteId ?? "");
            if (string.IsNullOrWhiteSpace(facture.Numero)) facture.Numero = _facturation.GetNextNumeroFacture();
            _context.Factures.Add(facture);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Facture créée.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DevisPdf(int id)
        {
            var devis = await _context.Devis.Include(d => d.Client).Include(d => d.Session).ThenInclude(s => s!.Formation)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (devis == null) return NotFound();
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(devis.SiteId) && devis.SiteId != _siteContext.CurrentSiteId)
                return Forbid();
            var pdf = _facturation.GenerateDevisPdf(devis);
            return File(pdf, "application/pdf", $"devis_{devis.Numero}.pdf");
        }

        public async Task<IActionResult> FacturePdf(int id)
        {
            var facture = await _context.Factures.Include(f => f.Client).Include(f => f.Session).ThenInclude(s => s!.Formation).Include(f => f.Devis)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (facture == null) return NotFound();
            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(facture.SiteId) && facture.SiteId != _siteContext.CurrentSiteId)
                return Forbid();
            var pdf = _facturation.GenerateFacturePdf(facture);
            return File(pdf, "application/pdf", $"facture_{facture.Numero}.pdf");
        }

        private async Task FillViewBag()
        {
            var q = _context.Clients.AsQueryable();
            if (!_siteContext.IsAdmin) q = q.Where(c => string.IsNullOrEmpty(c.SiteId) || c.SiteId == _siteContext.CurrentSiteId);
            ViewBag.Clients = new SelectList(await q.OrderBy(c => c.Nom).ToListAsync(), "Id", "Nom");
            var s = _context.Sessions.Include(x => x.Formation).AsQueryable();
            if (!_siteContext.IsAdmin) s = s.Where(sess => string.IsNullOrEmpty(sess.SiteId) || sess.SiteId == _siteContext.CurrentSiteId);
            var sessions = await s.OrderByDescending(sess => sess.DateDebut).ToListAsync();
            ViewBag.Sessions = new SelectList(sessions.Select(sess => new { sess.Id, Lib = $"{sess.Formation?.Titre} ({sess.DateDebut:dd/MM/yy})" }), "Id", "Lib");
        }
    }
}
