using System.Collections.Frozen;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using FormationManager.Data;
using FormationManager.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace FormationManager.Services;

public interface IVeilleRssService
{
    Task EnsureFeedsFromConfigAsync(string siteId, CancellationToken ct = default);
    Task<(int Added, List<string> Errors)> RefreshAllFeedsAsync(CancellationToken ct = default);
    Task<(int? IndicateurId, bool FromKeywords)> SuggestIndicateurAsync(string title, string description, int? defaultIndicateurId, CancellationToken ct = default);
    Task<VeilleValidation?> CreateValidationAsync(int rssItemId, int indicateurId, string validatedBy, string siteId, CancellationToken ct = default);
}

public class VeilleRssService : IVeilleRssService
{
    private static readonly FrozenDictionary<string, string[]> MotsClesParIndicateur = new Dictionary<string, string[]>
    {
        ["23"] = new[] { "loi", "décret", "réglementation", "obligation", "CNEFOP", "France Compétences", "Code du travail", "convention collective", "accord", "qualiopi", "ordonnance", "circulaire" },
        ["24"] = new[] { "métier", "emploi", "OPCO", "certification professionnelle", "référentiel", "branches", "orientations", "compétences", "RNCP" },
        ["25"] = new[] { "pédagogie", "formation", "digital", "numérique", "MOOC", "outil", "innovation", "modalités", "e-learning", "blended", "classe virtuelle" },
        ["26"] = new[] { "handicap", "accessibilité", "PCH", "RQTH", "inclusion", "aménagement", "travailleur handicapé", "Agefiph", "FIPHFP" },
        ["27"] = new[] { "sous-traitance", "prestataire", "externalisation", "sous-traitant" },
        ["28"] = new[] { "alternance", "FEST", "entreprise", "tutorat", "terrain", "situation de travail", "apprentissage" },
        ["29"] = new[] { "insertion", "accompagnement", "retour à l'emploi", "évolution", "reconversion", "France Travail", "Pôle emploi" }
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly FormationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<VeilleRssService> _logger;

    public VeilleRssService(
        FormationDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment env,
        ILogger<VeilleRssService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _env = env;
        _logger = logger;
    }

    public async Task EnsureFeedsFromConfigAsync(string siteId, CancellationToken ct = default)
    {
        var path = Path.Combine(_env.ContentRootPath ?? ".", "Config", "veille-rss-feeds.json");
        if (!File.Exists(path))
            return;

        var json = await File.ReadAllTextAsync(path, ct);
        var root = System.Text.Json.JsonSerializer.Deserialize<VeilleRssConfigRoot>(json);
        var feeds = root?.VeilleRssFeeds ?? [];
        if (feeds.Count == 0)
            return;

        var indicateurs = await _context.IndicateursQualiopi
            .Where(i => i.Critere == 6)
            .ToListAsync(ct);
        var byCode = indicateurs
            .GroupBy(i => i.CodeIndicateur)
            .ToDictionary(g => g.Key, g => g.First().Id);

        foreach (var f in feeds)
        {
            if (string.IsNullOrWhiteSpace(f.Url))
                continue;
            var exists = await _context.RssFeeds.AnyAsync(x => x.Url == f.Url.Trim(), ct);
            if (exists)
                continue;

            int? defaultId = null;
            if (!string.IsNullOrWhiteSpace(f.DefaultIndicateurCode) && byCode.TryGetValue(f.DefaultIndicateurCode.Trim(), out var id))
                defaultId = id;

            _context.RssFeeds.Add(new RssFeed
            {
                Name = f.Name ?? "Flux",
                Url = f.Url.Trim(),
                DefaultIndicateurId = defaultId,
                SiteId = siteId,
                IsActive = true,
                DateCreation = DateTime.UtcNow,
                DateModification = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<(int Added, List<string> Errors)> RefreshAllFeedsAsync(CancellationToken ct = default)
    {
        var feeds = await _context.RssFeeds
            .Where(f => f.IsActive)
            .ToListAsync(ct);
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept", "application/rss+xml, application/xml, text/xml, application/atom+xml, */*");
        var added = 0;
        var errors = new List<string>();

        foreach (var feed in feeds)
        {
            try
            {
                using var response = await client.GetAsync(feed.Url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    errors.Add($"{feed.Name}: HTTP {response.StatusCode}");
                    continue;
                }

                // Lire le contenu en respectant l'encodage
                byte[] contentBytes;
                using (var ms = new MemoryStream())
                {
                    await response.Content.CopyToAsync(ms, ct);
                    contentBytes = ms.ToArray();
                }

                if (contentBytes.Length == 0)
                {
                    errors.Add($"{feed.Name}: Contenu vide");
                    continue;
                }

                // Détecter l'encodage depuis le contenu ou utiliser UTF-8 par défaut
                string contentString;
                try
                {
                    // Essayer de détecter l'encodage depuis le BOM ou la déclaration XML
                    var encoding = Encoding.UTF8;
                    if (contentBytes.Length >= 3 && contentBytes[0] == 0xEF && contentBytes[1] == 0xBB && contentBytes[2] == 0xBF)
                    {
                        encoding = Encoding.UTF8;
                        contentBytes = contentBytes.Skip(3).ToArray();
                    }
                    else
                    {
                        // Chercher la déclaration d'encodage dans le XML
                        var header = Encoding.UTF8.GetString(contentBytes, 0, Math.Min(200, contentBytes.Length));
                        var encodingMatch = Regex.Match(header, @"encoding\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                        if (encodingMatch.Success)
                        {
                            try
                            {
                                var encodingName = encodingMatch.Groups[1].Value;
                                encoding = Encoding.GetEncoding(encodingName);
                            }
                            catch
                            {
                                // Si l'encodage n'est pas reconnu, utiliser UTF-8
                                encoding = Encoding.UTF8;
                            }
                        }
                    }
                    contentString = encoding.GetString(contentBytes);
                }
                catch (Exception encEx)
                {
                    _logger.LogWarning(encEx, "Erreur lors de la détection de l'encodage pour {FeedName}, utilisation de UTF-8", feed.Name);
                    contentString = Encoding.UTF8.GetString(contentBytes);
                }

                if (string.IsNullOrWhiteSpace(contentString))
                {
                    errors.Add($"{feed.Name}: Contenu vide après décodage");
                    continue;
                }

                // Vérifier que c'est du XML/RSS/Atom (vérification plus souple)
                var trimmed = contentString.TrimStart();
                var isRss = trimmed.Contains("<rss", StringComparison.OrdinalIgnoreCase);
                var isAtom = trimmed.Contains("<feed", StringComparison.OrdinalIgnoreCase);
                var isXml = trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("<", StringComparison.OrdinalIgnoreCase);
                
                if (!isXml || (!isRss && !isAtom))
                {
                    // Peut-être une page HTML de connexion ou autre
                    if (contentString.Contains("login", StringComparison.OrdinalIgnoreCase) || 
                        contentString.Contains("connexion", StringComparison.OrdinalIgnoreCase) ||
                        contentString.Contains("authentification", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"{feed.Name}: Le site demande une authentification");
                    }
                    else
                    {
                        errors.Add($"{feed.Name}: Le contenu n'est pas un flux RSS/Atom valide (pas de balise <rss> ou <feed>)");
                    }
                    continue;
                }

                // Parser le flux avec les bonnes options
                SyndicationFeed? syndication = null;
                try
                {
                    _logger.LogDebug("Tentative de parsing pour {FeedName} (URL: {Url}, taille: {Size} bytes)", feed.Name, feed.Url, contentBytes.Length);
                    // Essayer d'abord avec le contenu tel quel
                    using var stream = new MemoryStream(contentBytes);
                    using var reader = XmlReader.Create(stream, new XmlReaderSettings 
                    { 
                        Async = true, 
                        DtdProcessing = DtdProcessing.Ignore,
                        IgnoreWhitespace = true,
                        IgnoreComments = true,
                        CheckCharacters = false // Plus permissif pour certains flux
                    });
                    
                    syndication = SyndicationFeed.Load(reader);
                    _logger.LogDebug("Parsing réussi pour {FeedName}, {ItemCount} items trouvés", feed.Name, syndication?.Items?.Count() ?? 0);
                }
                catch (XmlException xmlEx)
                {
                    _logger.LogWarning(xmlEx, "Première tentative de parsing XML échouée pour {FeedName}: {Message}", feed.Name, xmlEx.Message);
                    
                    // Essayer une deuxième fois avec un pré-traitement du XML
                    try
                    {
                        // Corriger les problèmes de dates communes dans RSS
                        var correctedContent = contentString;
                        
                        // Corriger les formats de date non standard (ex: GMT+00:00 -> GMT)
                        correctedContent = Regex.Replace(correctedContent, 
                            @"(\w{3},\s+\d{1,2}\s+\w{3}\s+\d{4}\s+\d{2}:\d{2}:\d{2})\s+GMT\+00:00", 
                            "$1 GMT", 
                            RegexOptions.IgnoreCase);
                        
                        // Réessayer avec le contenu corrigé
                        var correctedBytes = Encoding.UTF8.GetBytes(correctedContent);
                        using var stream2 = new MemoryStream(correctedBytes);
                        using var reader2 = XmlReader.Create(stream2, new XmlReaderSettings 
                        { 
                            Async = true, 
                            DtdProcessing = DtdProcessing.Ignore,
                            IgnoreWhitespace = true,
                            IgnoreComments = true,
                            CheckCharacters = false
                        });
                        
                        syndication = SyndicationFeed.Load(reader2);
                        _logger.LogInformation("Parsing réussi après correction pour {FeedName}", feed.Name);
                    }
                    catch (Exception retryEx)
                    {
                        _logger.LogError(retryEx, "Échec du parsing même après correction pour {FeedName}: {Message}", feed.Name, retryEx.Message);
                        errors.Add($"{feed.Name}: Erreur de parsing XML - {xmlEx.Message} (tentative de correction échouée)");
                        continue;
                    }
                }
                catch (Exception parseEx)
                {
                    _logger.LogWarning(parseEx, "Erreur lors du parsing pour {FeedName}: {Message}", feed.Name, parseEx.Message);
                    errors.Add($"{feed.Name}: Erreur lors du parsing - {parseEx.GetType().Name}: {parseEx.Message}");
                    continue;
                }

                if (syndication == null)
                {
                    errors.Add($"{feed.Name}: Impossible de charger le flux");
                    continue;
                }

                var itemsAdded = 0;
                var itemsToAdd = new List<RssItem>();

                foreach (var item in syndication.Items)
                {
                    var externalId = item.Id ?? item.Links.FirstOrDefault()?.Uri?.ToString() ?? item.Title?.Text ?? Guid.NewGuid().ToString();
                    if (string.IsNullOrWhiteSpace(externalId))
                        externalId = Guid.NewGuid().ToString();
                    var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "";
                    var title = item.Title?.Text ?? "";
                    var description = item.Summary?.Text ?? "";
                    var published = item.PublishDate.UtcDateTime;

                    // Vérifier si l'item existe déjà
                    var exists = await _context.RssItems
                        .AnyAsync(x => x.RssFeedId == feed.Id && x.ExternalId == externalId, ct);
                    if (exists)
                        continue;

                    itemsToAdd.Add(new RssItem
                    {
                        RssFeedId = feed.Id,
                        ExternalId = externalId,
                        Title = title.Length > 500 ? title[..500] : title,
                        Link = link.Length > 1000 ? link[..1000] : link,
                        Description = description ?? "",
                        PublishedUtc = published,
                        FetchedAt = DateTime.UtcNow
                    });
                    itemsAdded++;
                }

                // Sauvegarder par batch pour éviter les timeouts
                if (itemsToAdd.Any())
                {
                    _context.RssItems.AddRange(itemsToAdd);
                    try
                    {
                        await _context.SaveChangesAsync(ct);
                        added += itemsToAdd.Count;
                    }
                    catch (Exception saveEx)
                    {
                        errors.Add($"{feed.Name}: Erreur lors de la sauvegarde - {saveEx.Message}");
                        // Annuler les changements pour ce flux
                        foreach (var item in itemsToAdd)
                        {
                            _context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }
                }

                if (itemsAdded == 0 && syndication.Items.Count() == 0)
                {
                    errors.Add($"{feed.Name}: Flux vide (aucun item)");
                }
            }
            catch (XmlException ex)
            {
                errors.Add($"{feed.Name}: Erreur XML - {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                errors.Add($"{feed.Name}: Erreur HTTP - {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                errors.Add($"{feed.Name}: Timeout (délai dépassé)");
            }
            catch (Exception ex)
            {
                errors.Add($"{feed.Name}: {ex.GetType().Name} - {ex.Message}");
            }
        }
        
        // Log des erreurs
        if (errors.Any())
        {
            foreach (var error in errors)
            {
                _logger.LogWarning("Erreur lors du rafraîchissement d'un flux RSS: {Error}", error);
            }
        }

        return (added, errors);
    }

    public async Task<(int? IndicateurId, bool FromKeywords)> SuggestIndicateurAsync(string title, string description, int? defaultIndicateurId, CancellationToken ct = default)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        var best = (Code: (string?)null, Score: 0);

        foreach (var (code, mots) in MotsClesParIndicateur)
        {
            var score = mots.Count(m => text.Contains(m.ToLowerInvariant()));
            if (score > best.Score)
                best = (code, score);
        }

        if (best.Score > 0 && best.Code != null)
        {
            var id = await _context.IndicateursQualiopi
                .Where(i => i.Critere == 6 && i.CodeIndicateur == best.Code)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync(ct);
            return (id ?? defaultIndicateurId, id != null);
        }

        return (defaultIndicateurId, false);
    }

    public async Task<VeilleValidation?> CreateValidationAsync(int rssItemId, int indicateurId, string validatedBy, string siteId, CancellationToken ct = default)
    {
        var item = await _context.RssItems
            .Include(i => i.Feed)
            .FirstOrDefaultAsync(i => i.Id == rssItemId, ct);
        if (item == null)
            return null;

        var ok = await _context.IndicateursQualiopi
            .AnyAsync(i => i.Id == indicateurId && i.Critere == 6, ct);
        if (!ok)
            return null;

        var v = new VeilleValidation
        {
            RssItemId = rssItemId,
            IndicateurQualiopiId = indicateurId,
            ValidatedBy = validatedBy.Length > 200 ? validatedBy[..200] : validatedBy,
            ValidatedAt = DateTime.UtcNow,
            SiteId = siteId
        };
        _context.VeilleValidations.Add(v);
        await _context.SaveChangesAsync(ct);
        return v;
    }

    private sealed class VeilleRssConfigRoot
    {
        [System.Text.Json.Serialization.JsonPropertyName("veilleRssFeeds")]
        public List<VeilleRssFeedEntry> VeilleRssFeeds { get; set; } = new();
    }

    private sealed class VeilleRssFeedEntry
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string? Url { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("defaultIndicateurCode")]
        public string? DefaultIndicateurCode { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("comment")]
        public string? Comment { get; set; }
    }
}
