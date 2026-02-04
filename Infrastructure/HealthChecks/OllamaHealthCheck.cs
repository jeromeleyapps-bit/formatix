using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FormationManager.Infrastructure.HealthChecks
{
    public class OllamaHealthCheck : IHealthCheck
    {
        private readonly Infrastructure.AI.IAIService _aiService;
        private readonly ILogger<OllamaHealthCheck> _logger;

        public OllamaHealthCheck(
            Infrastructure.AI.IAIService aiService,
            ILogger<OllamaHealthCheck> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isAvailable = await _aiService.IsServiceAvailableAsync();
                
                if (isAvailable)
                {
                    return HealthCheckResult.Healthy("Ollama est disponible et répond");
                }
                
                return HealthCheckResult.Unhealthy(
                    "Ollama n'est pas disponible. Vérifiez que le service Ollama est démarré et accessible.",
                    data: new Dictionary<string, object>
                    {
                        ["service"] = "Ollama",
                        ["status"] = "unavailable"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de santé Ollama");
                return HealthCheckResult.Unhealthy(
                    "Erreur lors de la vérification Ollama",
                    ex,
                    new Dictionary<string, object>
                    {
                        ["error"] = ex.Message,
                        ["service"] = "Ollama"
                    });
            }
        }
    }
}