using FluentAssertions;
using FormationManager.Infrastructure.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FormationManager.Tests.Integration
{
    /// <summary>
    /// Tests d'intégration pour la synchronisation
    /// Ces tests nécessitent un serveur central en cours d'exécution
    /// </summary>
    public class SyncIntegrationTests : IDisposable
    {
        private readonly ISyncService? _syncService;
        private readonly Mock<ILogger<SyncService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly IHttpClientFactory _httpClientFactory;

        public SyncIntegrationTests()
        {
            _loggerMock = new Mock<ILogger<SyncService>>();
            _configurationMock = new Mock<IConfiguration>();
            _httpClientFactory = new HttpClientFactoryForTesting();

            _configurationMock.Setup(c => c["Sync:CentralUrl"]).Returns("https://localhost:5001");
            _configurationMock.Setup(c => c["Sync:ApiKey"]).Returns("test-api-key");
            _configurationMock.Setup(c => c["Sync:RetryAttempts"]).Returns("3");
            _configurationMock.Setup(c => c["Sync:SiteId"]).Returns("TEST_SITE_01");
        }

        [Fact(Skip = "Nécessite un serveur central en cours d'exécution")]
        public async Task SyncToCentralAsync_WithValidData_ShouldSucceed()
        {
            // Arrange
            var service = new SyncService(
                _httpClientFactory,
                _loggerMock.Object,
                _configurationMock.Object);

            var siteId = "TEST_SITE_01";
            var syncData = new SyncData
            {
                SiteId = siteId,
                Entities = new List<object>(),
                Metadata = new Dictionary<string, object>
                {
                    ["test"] = "integration"
                }
            };

            // Act
            var result = await service.SyncToCentralAsync(siteId, syncData);

            // Assert
            result.Should().NotBeNull();
            // Note: Le succès dépend du serveur central
        }

        public void Dispose()
        {
            // Cleanup si nécessaire
        }
    }

    // Factory HTTP client simple pour les tests
    internal class HttpClientFactoryForTesting : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}