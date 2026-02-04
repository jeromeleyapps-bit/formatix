using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormationManager.Controllers;

[Authorize]
public class VeilleController : Controller
{
    private readonly FormationDbContext _context;
    private readonly IVeilleRssService _veilleRss;
    private readonly ISiteContext _siteContext;

    public VeilleController(FormationDbContext context, IVeilleRssService veilleRss, ISiteContext siteContext)
    {
        _context = context;
        _veilleRss = veilleRss;
        _siteContext = siteContext;
    }

    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var siteId = _siteContext.CurrentSiteId;
        await _veilleRss.EnsureFeedsFromConfigAsync(siteId, ct);

        var depuis = DateTime.UtcNow.AddDays(-90);
        // Les feeds sont chargés en arrière-plan mais ne sont plus affichés dans l'interface

        var items = await _context.RssItems
            .Include(i => i.Feed)
            .Where(i => i.PublishedUtc >= depuis || i.FetchedAt >= depuis)
            .OrderByDescending(i => i.PublishedUtc ?? i.FetchedAt)
            .Take(200)
            .ToListAsync(ct);

        var validatedIds = await _context.VeilleValidations
            .Where(v => v.SiteId == siteId)
            .Select(v => v.RssItemId)
            .Distinct()
            .ToListAsync(ct);
        var validatedSet = validatedIds.ToHashSet();

        var itemVms = new List<RssItemViewModel>();
        foreach (var i in items)
        {
            var (suggestedId, _) = await _veilleRss.SuggestIndicateurAsync(
                i.Title, i.Description, i.Feed?.DefaultIndicateurId, ct);
            var indic = suggestedId.HasValue
                ? await _context.IndicateursQualiopi
                    .Where(x => x.Id == suggestedId.Value)
                    .Select(x => new { x.CodeIndicateur })
                    .FirstOrDefaultAsync(ct)
                : null;

            itemVms.Add(new RssItemViewModel
            {
                Id = i.Id,
                Title = i.Title,
                Link = i.Link,
                Description = i.Description.Length > 500 ? i.Description[..500] + "…" : i.Description,
                PublishedUtc = i.PublishedUtc,
                FeedName = i.Feed?.Name ?? "",
                SuggestedIndicateurId = suggestedId,
                SuggestedIndicateurCode = indic?.CodeIndicateur,
                IsValidated = validatedSet.Contains(i.Id)
            });
        }

        var validations = await _context.VeilleValidations
            .Include(v => v.RssItem)
            .Include(v => v.Indicateur)
            .Where(v => v.SiteId == siteId && v.ValidatedAt >= depuis)
            .OrderByDescending(v => v.ValidatedAt)
            .Take(100)
            .Select(v => new VeilleValidationViewModel
            {
                Id = v.Id,
                RssItemId = v.RssItemId,
                ItemTitle = v.RssItem != null ? v.RssItem.Title : "",
                ItemLink = v.RssItem != null ? v.RssItem.Link : "",
                IndicateurCode = v.Indicateur != null ? v.Indicateur.CodeIndicateur : "",
                IndicateurLibelle = v.Indicateur != null ? v.Indicateur.Libelle : "",
                ValidatedBy = v.ValidatedBy,
                ValidatedAt = v.ValidatedAt
            })
            .ToListAsync(ct);

        var indicateurs = await _context.IndicateursQualiopi
            .Where(i => i.Critere == 6)
            .OrderBy(i => i.CodeIndicateur)
            .Select(i => new { i.Id, i.CodeIndicateur, i.Libelle })
            .ToListAsync(ct);

        var vm = new VeilleIndexViewModel { Feeds = new List<RssFeedViewModel>(), Items = itemVms, Validations = validations };
        ViewBag.Indicateurs = indicateurs;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualiser(CancellationToken ct = default)
    {
        var (added, errors) = await _veilleRss.RefreshAllFeedsAsync(ct);
        
        if (errors.Any())
        {
            var errorMsg = string.Join("; ", errors.Take(5));
            if (errors.Count > 5)
                errorMsg += $" (+ {errors.Count - 5} autre(s))";
            TempData["Error"] = $"Erreurs sur certains flux : {errorMsg}";
        }
        
        if (added > 0)
        {
            TempData["Success"] = $"{added} nouvelle(s) actualité(s) récupérée(s).";
        }
        else if (!errors.Any())
        {
            TempData["Success"] = "Actualisation terminée. Aucune nouvelle actualité.";
        }
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Valider(int rssItemId, int indicateurId, CancellationToken ct = default)
    {
        var user = User.Identity?.Name ?? "?";
        var siteId = _siteContext.CurrentSiteId;
        var v = await _veilleRss.CreateValidationAsync(rssItemId, indicateurId, user, siteId, ct);
        if (v == null)
        {
            TempData["Error"] = "Validation impossible (actualité ou indicateur introuvable).";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Actualité validée pour l'indicateur choisi.";
        return RedirectToAction(nameof(Index));
    }
}
