using FluentAssertions;
using FormationManager.Infrastructure.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FormationManager.Tests.Unit
{
    public class SyncServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<SyncService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public SyncServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<SyncService>>();
            _configurationMock = new Mock<IConfiguration>();
            
            _configurationMock.Setup(c => c["Sync:CentralUrl"]).Returns("https://localhost:5001");
            _configurationMock.Setup(c => c["Sync:ApiKey"]).Returns("test-api-key");
            _configurationMock.Setup(c => c["Sync:RetryAttempts"]).Returns("3");
        }

        [Fact]
        public void SyncService_Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var service = new SyncService(
                _httpClientFactoryMock.Object,
                _loggerMock.Object,
                _configurationMock.Object);

            // Assert
            service.Should().NotBeNull();
        }

        [Fact]
        public async Task TestConnectionAsync_WithInvalidUrl_ShouldReturnFalse()
        {
            // Arrange
            var service = new SyncService(
                _httpClientFactoryMock.Object,
                _loggerMock.Object,
                _configurationMock.Object);

            // Act
            var result = await service.TestConnectionAsync("https://invalid-url-test");

            // Assert
            result.Should().BeFalse();
        }
    }
}