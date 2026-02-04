using System.Diagnostics;

namespace FormationManager.Infrastructure.AI
{
    /// <summary>
    /// Optionally auto-starts Ollama ("ollama serve") on app startup (Windows).
    /// Safe by default: if Ollama is already running, it does nothing.
    /// </summary>
    public class OllamaAutoStartHostedService : IHostedService
    {
        private readonly ILogger<OllamaAutoStartHostedService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAIService _aiService;

        public OllamaAutoStartHostedService(
            ILogger<OllamaAutoStartHostedService> logger,
            IConfiguration configuration,
            IAIService aiService)
        {
            _logger = logger;
            _configuration = configuration;
            _aiService = aiService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var autoStart = _configuration.GetValue("Ollama:AutoStart", true);
            if (!autoStart)
            {
                _logger.LogInformation("Ollama AutoStart désactivé (Ollama:AutoStart=false).");
                return;
            }

            if (!OperatingSystem.IsWindows())
            {
                _logger.LogInformation("Ollama AutoStart ignoré (non-Windows).");
                return;
            }

            try
            {
                // If already up, nothing to do.
                if (await _aiService.IsServiceAvailableAsync())
                {
                    _logger.LogInformation("Ollama est déjà disponible (API OK).");
                    return;
                }

                var cliPath = _configuration["Ollama:CliPath"];
                if (string.IsNullOrWhiteSpace(cliPath))
                {
                    // rely on PATH
                    cliPath = "ollama";
                }

                _logger.LogWarning("Ollama n'est pas disponible. Tentative de démarrage automatique: {Cli} serve", cliPath);

                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = "serve",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = Process.Start(psi);
                if (process == null)
                {
                    _logger.LogWarning("Impossible de démarrer Ollama (Process.Start a retourné null).");
                    return;
                }

                // don't block forever; wait for API to come up
                var startupTimeoutSeconds = _configuration.GetValue("Ollama:StartupTimeoutSeconds", 20);
                var deadline = DateTime.UtcNow.AddSeconds(Math.Max(3, startupTimeoutSeconds));

                while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(750, cancellationToken);

                    if (await _aiService.IsServiceAvailableAsync())
                    {
                        _logger.LogInformation("Ollama démarré et disponible (API OK).");
                        return;
                    }
                }

                // If still not up, capture last stderr lines (best-effort)
                try
                {
                    var err = await process.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(err))
                    {
                        _logger.LogWarning("Ollama démarré mais API non disponible. stderr: {Err}", err.Length > 2000 ? err[..2000] : err);
                    }
                    else
                    {
                        _logger.LogWarning("Ollama démarré mais API non disponible après {Seconds}s.", startupTimeoutSeconds);
                    }
                }
                catch
                {
                    _logger.LogWarning("Ollama démarré mais API non disponible après {Seconds}s.", startupTimeoutSeconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AutoStart Ollama a échoué: {Message}", ex.Message);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Intentionally do NOT stop Ollama here:
            // - user might run it globally
            // - stopping it could disrupt other apps / sessions
            return Task.CompletedTask;
        }
    }
}

