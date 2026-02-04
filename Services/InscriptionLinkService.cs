using Microsoft.AspNetCore.Http;

namespace FormationManager.Services
{
    public interface IInscriptionLinkService
    {
        string GetBaseUrl();
        string GetInscriptionUrl(int sessionId);
    }

    public class InscriptionLinkService : IInscriptionLinkService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public InscriptionLinkService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public string GetBaseUrl()
        {
            var baseUrl = _configuration["AppSettings:BaseUrlInscription"]?.Trim();
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                return baseUrl.TrimEnd('/');
            }

            var context = _httpContextAccessor.HttpContext;
            if (context?.Request != null)
            {
                return $"{context.Request.Scheme}://{context.Request.Host.Value}";
            }

            return "http://localhost:5000";
        }

        public string GetInscriptionUrl(int sessionId)
        {
            return $"{GetBaseUrl()}/Inscription/Session/{sessionId}";
        }
    }
}
