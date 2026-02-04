using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormationManager.Infrastructure.Sync;
using FormationManager.Infrastructure.Exceptions;

namespace FormationManager.Controllers.Sync
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SyncController : ControllerBase
    {
        private readonly ISyncService _syncService;
        private readonly ILogger<SyncController> _logger;
        private readonly IConfiguration _configuration;

        public SyncController(
            ISyncService syncService,
            ILogger<SyncController> logger,
            IConfiguration configuration)
        {
            _syncService = syncService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Upload des données locales vers le serveur central
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromBody] SyncData data)
        {
            try
            {
                // Récupération du SiteId depuis la requête ou la configuration
                var siteId = Request.Headers["X-Site-Id"].FirstOrDefault() 
                    ?? _configuration["Sync:SiteId"] 
                    ?? "UNKNOWN";

                _logger.LogInformation("Upload de données depuis site {SiteId}", siteId);

                // Validation de la clé API
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                var configuredApiKey = _configuration["Sync:ApiKey"];

                if (string.IsNullOrEmpty(configuredApiKey) || apiKey != configuredApiKey)
                {
                    _logger.LogWarning("Tentative d'upload avec clé API invalide");
                    return Unauthorized(new { message = "Clé API invalide" });
                }

                if (!string.IsNullOrWhiteSpace(data.SiteId) && data.SiteId != siteId)
                {
                    return BadRequest(new { message = "SiteId incohérent", expected = siteId, received = data.SiteId });
                }

                data.SiteId = siteId;
                var result = await _syncService.SyncToCentralAsync(siteId, data);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (SyncException ex)
            {
                _logger.LogError(ex, "Erreur de synchronisation");
                return StatusCode(503, new { message = ex.Message, siteId = ex.SiteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'upload");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Download des données depuis le serveur central
        /// </summary>
        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery] string? siteId)
        {
            try
            {
                siteId ??= Request.Headers["X-Site-Id"].FirstOrDefault() 
                    ?? _configuration["Sync:SiteId"] 
                    ?? "UNKNOWN";

                _logger.LogInformation("Download de données pour site {SiteId}", siteId);

                // Validation de la clé API
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                var configuredApiKey = _configuration["Sync:ApiKey"];

                if (string.IsNullOrEmpty(configuredApiKey) || apiKey != configuredApiKey)
                {
                    _logger.LogWarning("Tentative de download avec clé API invalide");
                    return Unauthorized(new { message = "Clé API invalide" });
                }

                var data = await _syncService.SyncFromCentralAsync(siteId);
                if (!string.IsNullOrWhiteSpace(data.SiteId) && data.SiteId != siteId)
                {
                    return StatusCode(502, new { message = "SiteId incohérent dans la réponse", expected = siteId, received = data.SiteId });
                }

                return Ok(data);
            }
            catch (SyncException ex)
            {
                _logger.LogError(ex, "Erreur de récupération");
                return StatusCode(503, new { message = ex.Message, siteId = ex.SiteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors du download");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Statut de la synchronisation
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> Status([FromQuery] string? siteId)
        {
            try
            {
                siteId ??= Request.Headers["X-Site-Id"].FirstOrDefault() 
                    ?? _configuration["Sync:SiteId"] 
                    ?? "UNKNOWN";

                var status = await _syncService.GetSyncStatusAsync(siteId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du statut");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Test de connexion au serveur central
        /// </summary>
        [HttpGet("test-connection")]
        [AllowAnonymous]
        public async Task<IActionResult> TestConnection([FromQuery] string? centralUrl)
        {
            try
            {
                centralUrl ??= _configuration["Sync:CentralUrl"] ?? "https://localhost:5001";
                
                var isConnected = await _syncService.TestConnectionAsync(centralUrl);
                
                return Ok(new
                {
                    url = centralUrl,
                    connected = isConnected,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test de connexion");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}