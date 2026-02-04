using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace FormationManager.Services
{
    public interface IOrganizationService
    {
        string GetApplicationName();
        string GetOrganizationName();
        string GetSIRET();
        string GetAddress();
        string GetEmail();
        string GetPhone();
        string? GetLogoPath();
        OrganizationInfo GetOrganizationInfo();
    }

    public class OrganizationService : IOrganizationService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public OrganizationService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public string GetApplicationName()
        {
            return "FormatiX";
        }

        public string GetOrganizationName()
        {
            return _configuration["AppSettings:NomOrganisme"] ?? "Organisme de Formation";
        }

        public string GetSIRET()
        {
            return _configuration["AppSettings:SIRET"] ?? string.Empty;
        }

        public string GetAddress()
        {
            var adresse = _configuration["AppSettings:Adresse"] ?? string.Empty;
            var codePostal = _configuration["AppSettings:CodePostal"] ?? string.Empty;
            var ville = _configuration["AppSettings:Ville"] ?? string.Empty;
            
            return string.Join(", ", new[] { adresse, codePostal, ville }.Where(s => !string.IsNullOrEmpty(s)));
        }

        public string GetEmail()
        {
            return _configuration["AppSettings:Email"] ?? string.Empty;
        }

        public string GetPhone()
        {
            return _configuration["AppSettings:Telephone"] ?? string.Empty;
        }

        public string? GetLogoPath()
        {
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
            var logoPath = Path.Combine(uploadsDir, "logo.png");
            var logoPathJpg = Path.Combine(uploadsDir, "logo.jpg");
            
            if (System.IO.File.Exists(logoPath))
            {
                return logoPath;
            }
            if (System.IO.File.Exists(logoPathJpg))
            {
                return logoPathJpg;
            }
            
            // Fallback sur icon.png si pas de logo uploadé
            var iconPath = Path.Combine(_environment.WebRootPath, "icon.png");
            if (System.IO.File.Exists(iconPath))
            {
                return iconPath;
            }
            
            return null;
        }

        public OrganizationInfo GetOrganizationInfo()
        {
            return new OrganizationInfo
            {
                ApplicationName = GetApplicationName(),
                OrganizationName = GetOrganizationName(),
                SIRET = GetSIRET(),
                Address = GetAddress(),
                Email = GetEmail(),
                Phone = GetPhone(),
                LogoPath = GetLogoPath()
            };
        }
    }

    public class OrganizationInfo
    {
        public string ApplicationName { get; set; } = "FormatiX";
        public string OrganizationName { get; set; } = string.Empty;
        public string SIRET { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
    }
}