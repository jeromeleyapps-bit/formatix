namespace FormationManager.Models;

public class VeilleIndexViewModel
{
    public List<RssFeedViewModel> Feeds { get; set; } = new();
    public List<RssItemViewModel> Items { get; set; } = new();
    public List<VeilleValidationViewModel> Validations { get; set; } = new();
}

public class RssFeedViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? DefaultIndicateurCode { get; set; }
    public bool IsActive { get; set; }
}

public class RssItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? PublishedUtc { get; set; }
    public string FeedName { get; set; } = string.Empty;
    public int? SuggestedIndicateurId { get; set; }
    public string? SuggestedIndicateurCode { get; set; }
    public bool IsValidated { get; set; }
}

public class VeilleValidationViewModel
{
    public int Id { get; set; }
    public int RssItemId { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public string ItemLink { get; set; } = string.Empty;
    public string IndicateurCode { get; set; } = string.Empty;
    public string IndicateurLibelle { get; set; } = string.Empty;
    public string ValidatedBy { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; }
}
