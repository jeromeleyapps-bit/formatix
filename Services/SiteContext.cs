using FormationManager.Data;
using FormationManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FormationManager.Services
{
    public interface ISiteContext
    {
        string CurrentSiteId { get; }
        bool IsAdmin { get; }
        IReadOnlyList<SiteInfo> GetSites();
        string GetSiteName(string siteId);
    }

    public class SiteContext : ISiteContext
    {
        private readonly IConfiguration _configuration;
        private readonly FormationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SiteContext(
            IConfiguration configuration,
            FormationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public string CurrentSiteId => _configuration["Sync:SiteId"] ?? "SITE_01";

        public bool IsAdmin
        {
            get
            {
                var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return false;
                }

                var user = _context.Utilisateurs.AsNoTracking()
                    .FirstOrDefault(u => u.Email == userName || u.UserName == userName);

                return user?.Role == RoleUtilisateur.Administrateur;
            }
        }

        public IReadOnlyList<SiteInfo> GetSites()
        {
            var dbSites = _context.Sites.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.SiteId)
                .ToList();
            if (dbSites.Count > 0)
            {
                return dbSites.Select(s => new SiteInfo
                {
                    SiteId = s.SiteId,
                    Name = s.Name
                }).ToList();
            }

            var sites = _configuration.GetSection("Sites").Get<List<SiteInfo>>();
            if (sites == null || sites.Count == 0)
            {
                return new List<SiteInfo>
                {
                    new SiteInfo { SiteId = CurrentSiteId, Name = CurrentSiteId }
                };
            }

            return sites;
        }

        public string GetSiteName(string siteId)
        {
            var site = GetSites().FirstOrDefault(s => s.SiteId == siteId);
            return site?.Name ?? siteId;
        }
    }

    public class SiteInfo
    {
        public string SiteId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
