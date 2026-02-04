using FluentAssertions;
using FormationManager.Infrastructure.OCR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FormationManager.Tests.Unit
{
    public class OCRServiceTests
    {
        private readonly Mock<ILogger<TesseractOCRService>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public OCRServiceTests()
        {
            _loggerMock = new Mock<ILogger<TesseractOCRService>>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _environmentMock.Setup(e => e.ContentRootPath).Returns(Environment.CurrentDirectory);

            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["Tesseract:DataPath"]).Returns("./tessdata");
            _configurationMock.Setup(c => c["Tesseract:Language"]).Returns("fra");
        }

        [Fact]
        public async Task ExtractTextAsync_WithEmptyBytes_ShouldThrowArgumentException()
        {
            // Arrange
            var service = new TesseractOCRService(
                _loggerMock.Object,
                _environmentMock.Object,
                _configurationMock.Object);
            var emptyBytes = Array.Empty<byte>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.ExtractTextAsync(emptyBytes));
        }

        [Fact]
        public async Task ExtractNamesFromTextAsync_WithValidText_ShouldExtractNames()
        {
            // Arrange
            var service = new TesseractOCRService(
                _loggerMock.Object,
                _environmentMock.Object,
                _configurationMock.Object);
            
            var text = "Formation : Communication\nStagiaires :\nJean Dupont\nMarie Martin\nPierre Durand";

            // Act
            var names = await service.ExtractNamesFromTextAsync(text);

            // Assert
            names.Should().NotBeEmpty();
            names.Should().Contain("Jean Dupont");
            names.Should().Contain("Marie Martin");
            names.Should().Contain("Pierre Durand");
        }

        [Fact]
        public async Task ExtractDatesFromTextAsync_WithValidText_ShouldExtractDates()
        {
            // Arrange
            var service = new TesseractOCRService(
                _loggerMock.Object,
                _environmentMock.Object,
                _configurationMock.Object);
            
            var text = "Session du 15/01/2024 au 20/01/2024\nDate de fin : 25-12-2024";

            // Act
            var dates = await service.ExtractDatesFromTextAsync(text);

            // Assert
            dates.Should().NotBeEmpty();
            dates.Should().HaveCountGreaterOrEqualTo(2);
        }
    }
}