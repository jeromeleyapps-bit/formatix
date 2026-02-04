using System.Text.Json;
using FormationManager.Infrastructure.Exceptions;
using Polly;
using Polly.Retry;

namespace FormationManager.Infrastructure.Sync
{
    public interface ISyncService
    {
        Task<SyncResult> SyncToCentralAsync(string siteId, SyncData data);
        Task<SyncData> SyncFromCentralAsync(string siteId);
        Task<bool> TestConnectionAsync(string centralUrl);
        Task<SyncStatus> GetSyncStatusAsync(string siteId);
    }

    public class SyncService : ISyncService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SyncService> _logger;
        private readonly IConfiguration _configuration;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly string _centralUrl;
        private readonly string _apiKey;
        private readonly int _retryAttempts;

        public SyncService(
            IHttpClientFactory httpClientFactory,
            ILogger<SyncService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            
            _centralUrl = configuration["Sync:CentralUrl"] ?? "https://localhost:5001";
            _apiKey = configuration["Sync:ApiKey"] ?? "";
            _retryAttempts = configuration.GetValue<int>("Sync:RetryAttempts", 3);

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: _retryAttempts,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Tentative sync {RetryCount}/{TotalAttempts} après {Delay}s",
                            retryCount, _retryAttempts, timespan.TotalSeconds);
                    });
        }

        public async Task<SyncResult> SyncToCentralAsync(string siteId, SyncData data)
        {
            try
            {
                _logger.LogInformation("Démarrage synchronisation vers central pour site {SiteId}", siteId);

                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogWarning("Clé API non configurée pour la synchronisation");
                    return new SyncResult
                    {
                        Success = false,
                        Message = "Clé API non configurée",
                        Errors = new List<string> { "Configuration manquante" }
                    };
                }

                using var client = CreateHttpClient(siteId);
                
                data.SiteId = siteId;
                data.SyncTimestamp = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await client.PostAsync($"{_centralUrl}/api/sync/upload", content);
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erreur sync : {StatusCode} - {Error}", response.StatusCode, errorContent);
                    throw new SyncException(siteId, $"Erreur HTTP {response.StatusCode}: {errorContent}");
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SyncResult>(resultJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    throw new SyncException(siteId, "Réponse invalide du serveur central");
                }

                _logger.LogInformation(
                    "Synchronisation réussie : {EntitiesCount} entités synchronisées",
                    result.SyncedEntities);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la synchronisation vers central");
                throw new SyncException(siteId, "Erreur de synchronisation", ex);
            }
        }

        public async Task<SyncData> SyncFromCentralAsync(string siteId)
        {
            try
            {
                _logger.LogInformation("Récupération données depuis central pour site {SiteId}", siteId);

                using var client = CreateHttpClient(siteId);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await client.GetAsync($"{_centralUrl}/api/sync/download?siteId={siteId}");
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erreur récupération : {StatusCode} - {Error}", response.StatusCode, errorContent);
                    throw new SyncException(siteId, $"Erreur HTTP {response.StatusCode}: {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<SyncData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    throw new SyncException(siteId, "Données invalides reçues du serveur central");
                }

                _logger.LogInformation(
                    "Données récupérées : {EntitiesCount} entités",
                    data.Entities?.Count ?? 0);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération depuis central");
                throw new SyncException(siteId, "Erreur de récupération", ex);
            }
        }

        public async Task<bool> TestConnectionAsync(string centralUrl)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetAsync($"{centralUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Test de connexion échoué pour {Url}", centralUrl);
                return false;
            }
        }

        public async Task<SyncStatus> GetSyncStatusAsync(string siteId)
        {
            try
            {
                using var client = CreateHttpClient(siteId);

                var response = await client.GetAsync($"{_centralUrl}/api/sync/status?siteId={siteId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return new SyncStatus
                    {
                        IsConnected = false,
                        LastSyncTime = null,
                        Message = $"Erreur {response.StatusCode}"
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<SyncStatus>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return status ?? new SyncStatus { IsConnected = false, Message = "Réponse invalide" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du statut de synchronisation");
                return new SyncStatus
                {
                    IsConnected = false,
                    Message = ex.Message
                };
            }
        }

        private HttpClient CreateHttpClient(string siteId)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Site-Id", siteId);
            client.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            client.Timeout = TimeSpan.FromMinutes(5);
            return client;
        }
    }

    public class SyncData
    {
        public List<object> Entities { get; set; } = new();
        public DateTime SyncTimestamp { get; set; } = DateTime.UtcNow;
        public string SiteId { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class SyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SyncedEntities { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime SyncTime { get; set; } = DateTime.UtcNow;
    }

    public class SyncStatus
    {
        public bool IsConnected { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PendingEntities { get; set; }
    }
}