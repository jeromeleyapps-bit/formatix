using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormationManager.Infrastructure.OCR;
using FormationManager.Infrastructure.AI;
using FormationManager.Infrastructure.Exceptions;
using FormationManager.Data;
using FormationManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FormationManager.Controllers.Documents
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IOCRService _ocrService;
        private readonly IAIService _aiService;
        private readonly FormationDbContext _context;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IOCRService ocrService,
            IAIService aiService,
            FormationDbContext context,
            ILogger<DocumentsController> logger)
        {
            _ocrService = ocrService;
            _aiService = aiService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Upload et analyse d'un document (PDF, JPEG, PNG) avec OCR + IA
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)] // 50MB max
        public async Task<IActionResult> UploadDocument(IFormFile file, [FromQuery] int? sessionId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Aucun fichier fourni" });
                }

                // Accepter PDF, JPEG et PNG
                var contentType = file.ContentType.ToLowerInvariant();
                var fileName = file.FileName.ToLowerInvariant();
                var isPdf = contentType.Contains("pdf") || fileName.EndsWith(".pdf");
                var isJpeg = contentType.Contains("jpeg") || contentType.Contains("jpg") || 
                            fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg");
                var isPng = contentType.Contains("png") || fileName.EndsWith(".png");

                if (!isPdf && !isJpeg && !isPng)
                {
                    return BadRequest(new { message = "Seuls les fichiers PDF, JPEG et PNG sont acceptés" });
                }

                _logger.LogInformation("Upload document : {FileName}, {Size} bytes", file.FileName, file.Length);

                // Lecture du fichier
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Validation qualité OCR
                var isValidQuality = await _ocrService.ValidateOCRQualityAsync(fileBytes);
                if (!isValidQuality)
                {
                    _logger.LogWarning("Qualité OCR insuffisante pour {FileName}", file.FileName);
                }

                // Extraction texte OCR
                var extractedText = await _ocrService.ExtractTextAsync(fileBytes);

                // Analyse IA
                var documentType = DetermineDocumentType(file.FileName);
                var analysis = await _aiService.AnalyzeDocumentAsync(extractedText, documentType);

                // Sauvegarde du document
                var document = new Models.Document
                {
                    TypeDocument = MapDocumentType(documentType),
                    NomFichier = file.FileName,
                    CheminFichier = $"documents/{Guid.NewGuid()}_{file.FileName}",
                    StatutValidation = "En attente",
                    DateCreation = DateTime.UtcNow,
                    SessionId = sessionId
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Document analysé : {DocumentId}, Type: {DocumentType}, Critères Qualiopi: {CriteriaCount}",
                    document.Id, documentType, analysis.QualiopiCriteria.Count);

                return Ok(new
                {
                    documentId = document.Id,
                    fileName = file.FileName,
                    size = file.Length,
                    ocrQuality = isValidQuality,
                    extractedTextLength = extractedText.Length,
                    analysis = new
                    {
                        documentType = documentType.ToString(),
                        summary = analysis.Summary,
                        qualiopiCriteria = analysis.QualiopiCriteria,
                        keywords = analysis.Keywords,
                        confidence = analysis.Confidence
                    }
                });
            }
            catch (OCRException ex)
            {
                _logger.LogError(ex, "Erreur OCR lors de l'upload");
                return StatusCode(422, new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (AIException ex)
            {
                _logger.LogError(ex, "Erreur IA lors de l'upload");
                return StatusCode(422, new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'upload");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Analyse d'une feuille d'émargement
        /// </summary>
        [HttpPost("analyze-emargement")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> AnalyzeEmargement(IFormFile file, [FromQuery] int sessionId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Aucun fichier fourni" });
                }

                _logger.LogInformation("Analyse feuille d'émargement pour session {SessionId}", sessionId);

                // Vérification session
                var session = await _context.Sessions.FindAsync(sessionId);
                if (session == null)
                {
                    return NotFound(new { message = "Session non trouvée" });
                }

                // Lecture du fichier
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var pdfBytes = memoryStream.ToArray();

                // Extraction données émargement
                var emargementData = await _ocrService.ExtractEmargementDataAsync(pdfBytes);

                // Analyse IA
                var analysis = await _aiService.AnalyzeDocumentAsync(emargementData.RawText, DocumentType.Emargement);

                return Ok(new
                {
                    sessionId = sessionId,
                    extractedData = new
                    {
                        names = emargementData.Names,
                        dates = emargementData.Dates,
                        hasSignatures = emargementData.HasSignatures,
                        hasTableStructure = emargementData.HasTableStructure,
                        confidence = emargementData.Confidence
                    },
                    analysis = new
                    {
                        summary = analysis.Summary,
                        qualiopiCriteria = analysis.QualiopiCriteria,
                        confidence = analysis.Confidence
                    }
                });
            }
            catch (OCRException ex)
            {
                _logger.LogError(ex, "Erreur OCR lors de l'analyse émargement");
                return StatusCode(422, new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'analyse émargement");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Extraction OCR d'un document (PDF, JPEG, PNG)
        /// </summary>
        [HttpPost("extract-ocr")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> ExtractOCR(IFormFile file, [FromQuery] string language = "fra")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Aucun fichier fourni" });
                }

                // Accepter PDF, JPEG et PNG
                var contentType = file.ContentType.ToLowerInvariant();
                var fileName = file.FileName.ToLowerInvariant();
                var isPdf = contentType.Contains("pdf") || fileName.EndsWith(".pdf");
                var isJpeg = contentType.Contains("jpeg") || contentType.Contains("jpg") || 
                            fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg");
                var isPng = contentType.Contains("png") || fileName.EndsWith(".png");

                if (!isPdf && !isJpeg && !isPng)
                {
                    return BadRequest(new { message = "Seuls les fichiers PDF, JPEG et PNG sont acceptés" });
                }

                _logger.LogInformation("Extraction OCR : {FileName}", file.FileName);

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var text = await _ocrService.ExtractTextAsync(fileBytes, language);

                return Ok(new
                {
                    fileName = file.FileName,
                    extractedText = text,
                    textLength = text.Length,
                    language = language
                });
            }
            catch (OCRException ex)
            {
                _logger.LogError(ex, "Erreur OCR");
                return StatusCode(422, new { message = ex.Message, errorCode = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction OCR");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        private DocumentType DetermineDocumentType(string fileName)
        {
            var lowerFileName = fileName.ToLowerInvariant();
            
            if (lowerFileName.Contains("émargement") || lowerFileName.Contains("emargement"))
                return DocumentType.Emargement;
            if (lowerFileName.Contains("programme"))
                return DocumentType.Programme;
            if (lowerFileName.Contains("évaluation") || lowerFileName.Contains("evaluation"))
                return DocumentType.Evaluation;
            if (lowerFileName.Contains("convention"))
                return DocumentType.Convention;
            if (lowerFileName.Contains("attestation"))
                return DocumentType.Attestation;
            
            return DocumentType.Autre;
        }

        private TypeDocument MapDocumentType(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.Emargement => TypeDocument.Emargement,
                DocumentType.Programme => TypeDocument.PreuveQualiopi,
                DocumentType.Evaluation => TypeDocument.Evaluation,
                DocumentType.Convention => TypeDocument.Convention,
                DocumentType.Attestation => TypeDocument.Attestation,
                _ => TypeDocument.PreuveQualiopi
            };
        }
    }
}