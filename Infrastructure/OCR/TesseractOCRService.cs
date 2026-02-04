using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using FormationManager.Infrastructure.Exceptions;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;

namespace FormationManager.Infrastructure.OCR
{
    public interface IOCRService
    {
        Task<string> ExtractTextAsync(byte[] pdfBytes, string language = "fra");
        Task<EmargementData> ExtractEmargementDataAsync(byte[] pdfBytes);
        Task<bool> ValidateOCRQualityAsync(byte[] pdfBytes);
        Task<List<string>> ExtractNamesFromTextAsync(string text);
        Task<List<DateTime>> ExtractDatesFromTextAsync(string text);
    }

    public class TesseractOCRService : IOCRService
    {
        private readonly ILogger<TesseractOCRService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _tesseractDataPath;
        private readonly string _defaultLanguage;

        public TesseractOCRService(
            ILogger<TesseractOCRService> logger, 
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _logger = logger;
            _environment = environment;
            
            // Configuration Tesseract
            var configPath = configuration["Tesseract:DataPath"] ?? "./tessdata";
            _tesseractDataPath = Path.IsPathRooted(configPath) 
                ? configPath 
                : Path.Combine(environment.ContentRootPath, configPath);
            
            _defaultLanguage = configuration["Tesseract:Language"] ?? "fra";

            // Création du dossier si nécessaire
            if (!Directory.Exists(_tesseractDataPath))
            {
                Directory.CreateDirectory(_tesseractDataPath);
                _logger.LogWarning(
                    "Dossier tessdata créé à {Path}. Assurez-vous d'y placer les fichiers .traineddata (fra.traineddata, eng.traineddata)",
                    _tesseractDataPath);
            }
        }

        public async Task<string> ExtractTextAsync(byte[] pdfBytes, string language = "fra")
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                _logger.LogInformation("Début extraction OCR pour langue {Language}, {Size} bytes", language, pdfBytes.Length);

                // Détecter le type de fichier (PDF, JPEG, PNG)
                var fileType = DetectFileType(pdfBytes);
                
                // Si c'est une image (JPEG/PNG), traitement direct
                if (fileType == FileType.Jpeg || fileType == FileType.Png)
                {
                    _logger.LogInformation("Fichier image détecté ({FileType}), extraction OCR directe", fileType);
                    var images = new List<byte[]> { pdfBytes };
                    var extractedText = await ExtractTextViaCliAsync(images, language);
                    
                    if (!string.IsNullOrWhiteSpace(extractedText))
                    {
                        _logger.LogInformation("Extraction OCR depuis image réussie : {TextLength} caractères", extractedText.Length);
                        return extractedText;
                    }
                    
                    _logger.LogWarning("Aucun texte extrait de l'image");
                    return string.Empty;
                }

                // Si c'est un PDF, traitement normal
                if (fileType == FileType.Pdf)
                {
                    // Tentative d'extraction directe depuis PDF
                    _logger.LogInformation("Fichier PDF détecté, tentative d'extraction OCR directe via Tesseract CLI");
                    var extractedText = await ExtractTextFromPdfDirectlyAsync(pdfBytes, language);
                    
                    if (!string.IsNullOrWhiteSpace(extractedText))
                    {
                        _logger.LogInformation("Extraction OCR directe réussie : {TextLength} caractères", extractedText.Length);
                        return extractedText;
                    }
                    
                    // Fallback : conversion PDF → images via ImageMagick/Ghostscript si disponible
                    _logger.LogWarning("Extraction directe échouée, tentative via conversion PDF → images");
                    List<byte[]> images;
                    try
                    {
                        images = await ConvertPdfToImagesAsync(pdfBytes);
                        if (images == null || images.Count == 0)
                        {
                            _logger.LogWarning("Aucune image extraite du PDF");
                            return string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ERREUR lors de la conversion PDF vers images : {Type} - {Message}", ex.GetType().Name, ex.Message);
                        return string.Empty;
                    }

                    _logger.LogInformation("Extraction OCR via Tesseract CLI sur images");
                    extractedText = await ExtractTextViaCliAsync(images, language);
                    
                    if (string.IsNullOrWhiteSpace(extractedText))
                    {
                        _logger.LogWarning("Aucun texte extrait par Tesseract CLI");
                        return string.Empty;
                    }
                    
                    _logger.LogInformation("Extraction OCR terminée : {TextLength} caractères extraits", extractedText.Length);
                    return extractedText;
                }

                // Type de fichier non reconnu
                _logger.LogWarning("Type de fichier non reconnu, tentative de traitement comme image");
                // Fallback : essayer comme image
                var fallbackImages = new List<byte[]> { pdfBytes };
                return await ExtractTextViaCliAsync(fallbackImages, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction OCR : {Message}", ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Détecte le type de fichier basé sur les magic bytes
        /// </summary>
        private FileType DetectFileType(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length < 4)
            {
                return FileType.Unknown;
            }

            // PDF : commence par %PDF
            if (fileBytes.Length >= 4 && 
                fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && 
                fileBytes[2] == 0x44 && fileBytes[3] == 0x46) // %PDF
            {
                return FileType.Pdf;
            }

            // JPEG : commence par FF D8 FF
            if (fileBytes.Length >= 3 && 
                fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
            {
                return FileType.Jpeg;
            }

            // PNG : commence par 89 50 4E 47 0D 0A 1A 0A
            if (fileBytes.Length >= 8 && 
                fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && 
                fileBytes[2] == 0x4E && fileBytes[3] == 0x47 &&
                fileBytes[4] == 0x0D && fileBytes[5] == 0x0A &&
                fileBytes[6] == 0x1A && fileBytes[7] == 0x0A)
            {
                return FileType.Png;
            }

            return FileType.Unknown;
        }

        private enum FileType
        {
            Unknown,
            Pdf,
            Jpeg,
            Png
        }
        
        private async Task<string> ExtractTextFromPdfDirectlyAsync(byte[] pdfBytes, string language)
        {
            var tesseractPath = FindTesseractExecutable();
            if (string.IsNullOrEmpty(tesseractPath))
            {
                _logger.LogWarning("Tesseract CLI non trouvé pour extraction directe PDF");
                return string.Empty;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), $"tesseract_pdf_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Sauvegarder le PDF temporairement
                var pdfPath = Path.Combine(tempDir, "input.pdf");
                await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);
                
                var outputPath = Path.Combine(tempDir, "output");
                
                // Utiliser le dossier tessdata local de l'application si disponible
                var localTessdata = Path.Combine(_environment.ContentRootPath, "tessdata");
                var tessdataToUse = Directory.Exists(localTessdata) && File.Exists(Path.Combine(localTessdata, $"{language}.traineddata"))
                    ? localTessdata
                    : Path.Combine(Path.GetDirectoryName(tesseractPath) ?? "", "tessdata");
                
                _logger.LogDebug("Extraction OCR directe depuis PDF : {PdfPath}, tessdata: {Tessdata}", pdfPath, tessdataToUse);
                
                // Tesseract peut lire les PDF directement (depuis v4.0)
                var arguments = $"\"{pdfPath}\" \"{outputPath}\" -l {language} --dpi 300";
                
                var psi = new ProcessStartInfo
                {
                    FileName = tesseractPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                };
                
                // Définir TESSDATA_PREFIX pour pointer vers le bon dossier
                psi.EnvironmentVariables["TESSDATA_PREFIX"] = tessdataToUse;
                _logger.LogDebug("TESSDATA_PREFIX defini a: {Path}", tessdataToUse);

                using var process = Process.Start(psi);
                if (process == null)
                {
                    _logger.LogError("Impossible de démarrer Tesseract pour extraction PDF directe");
                    return string.Empty;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("Tesseract a retourné une erreur (code {ExitCode}) pour extraction PDF directe: {Error}", 
                        process.ExitCode, error);
                    return string.Empty;
                }

                // Lire le fichier de sortie
                var resultFilePath = outputPath + ".txt";
                if (System.IO.File.Exists(resultFilePath))
                {
                    var text = await System.IO.File.ReadAllTextAsync(resultFilePath);
                    _logger.LogDebug("Extraction PDF directe réussie : {TextLength} caractères", text.Length);
                    return text;
                }
                else
                {
                    _logger.LogWarning("Fichier de sortie Tesseract non trouvé : {FilePath}", resultFilePath);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction OCR directe depuis PDF : {Message}", ex.Message);
                return string.Empty;
            }
            finally
            {
                // Nettoyer
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                        _logger.LogDebug("Dossier temporaire nettoyé");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors du nettoyage du dossier temporaire");
                    }
                }
            }
        }

        public async Task<EmargementData> ExtractEmargementDataAsync(byte[] pdfBytes)
        {
            try
            {
                _logger.LogInformation("Extraction données émargement depuis PDF");
                
                var text = await ExtractTextAsync(pdfBytes);
                
                var data = new EmargementData
                {
                    RawText = text,
                    ExtractedAt = DateTime.UtcNow
                };

                // Extraction noms
                data.Names = await ExtractNamesFromTextAsync(text);
                
                // Extraction dates
                data.Dates = await ExtractDatesFromTextAsync(text);
                
                // Détection signatures
                var signatureKeywords = new[] { "signature", "signé", "signer", "émargement", "présent", "absence" };
                data.HasSignatures = signatureKeywords.Any(kw => 
                    text.Contains(kw, StringComparison.OrdinalIgnoreCase));

                // Détection structure tableau
                var tablePatterns = new[] { "nom", "prénom", "date", "matin", "après-midi", "am", "pm" };
                data.HasTableStructure = tablePatterns.Any(pattern => 
                    text.Contains(pattern, StringComparison.OrdinalIgnoreCase));

                _logger.LogInformation(
                    "Données émargement extraites : {NamesCount} noms, {DatesCount} dates, Signatures: {HasSignatures}, Tableau: {HasTable}",
                    data.Names.Count, data.Dates.Count, data.HasSignatures, data.HasTableStructure);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction des données d'émargement");
                throw new OCRException("Erreur extraction données émargement", ex);
            }
        }

        public async Task<bool> ValidateOCRQualityAsync(byte[] pdfBytes)
        {
            try
            {
                var text = await ExtractTextAsync(pdfBytes);
                
                if (string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }

                // Validation qualité : ratio caractères alphanumériques
                var alphanumericCount = text.Count(char.IsLetterOrDigit);
                var totalChars = text.Replace(" ", "").Replace("\n", "").Length;
                var qualityRatio = totalChars > 0 ? (double)alphanumericCount / totalChars : 0;

                // Seuil minimum : 50% de caractères alphanumériques
                var isValid = qualityRatio >= 0.5;

                _logger.LogDebug(
                    "Validation qualité OCR : Ratio {QualityRatio:P2}, Valide: {IsValid}",
                    qualityRatio, isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation qualité OCR");
                return false;
            }
        }

        public Task<List<string>> ExtractNamesFromTextAsync(string text)
        {
            // Pattern pour noms français : Majuscule suivie de minuscules
            var namePattern = @"\b([A-ZÉÈÊËÀÁÂÃÄÅÆÇÌÍÎÏÑÒÓÔÕÖØÙÚÛÜÝ][a-zéèêëàáâãäåæçìíîïñòóôõöøùúûüý]+(?:\s+[A-ZÉÈÊËÀÁÂÃÄÅÆÇÌÍÎÏÑÒÓÔÕÖØÙÚÛÜÝ][a-zéèêëàáâãäåæçìíîïñòóôõöøùúûüý]+)*)\b";
            var nameMatches = Regex.Matches(text, namePattern);
            
            var names = nameMatches.Cast<Match>()
                .Select(m => m.Value.Trim())
                .Where(n => n.Length > 2 && !IsCommonWord(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogDebug("Extraction de {Count} noms depuis le texte", names.Count);
            return Task.FromResult(names);
        }

        public Task<List<DateTime>> ExtractDatesFromTextAsync(string text)
        {
            var dates = new List<DateTime>();
            
            // Pattern dates : DD/MM/YYYY ou DD-MM-YYYY
            var datePattern = @"(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})";
            var dateMatches = Regex.Matches(text, datePattern);

            foreach (Match match in dateMatches)
            {
                try
                {
                    var day = int.Parse(match.Groups[1].Value);
                    var month = int.Parse(match.Groups[2].Value);
                    var yearStr = match.Groups[3].Value;
                    var year = yearStr.Length == 2 
                        ? 2000 + int.Parse(yearStr) 
                        : int.Parse(yearStr);

                    var dateString = $"{day:D2}/{month:D2}/{year:D4}";
                    if (DateTime.TryParseExact(
                        dateString,
                        "dd/MM/yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date))
                    {
                        dates.Add(date);
                    }
                }
                catch
                {
                    // Ignorer les dates invalides
                }
            }

            _logger.LogDebug("Extraction de {Count} dates depuis le texte", dates.Count);
            return Task.FromResult(dates.Distinct().OrderBy(d => d).ToList());
        }

        /// <summary>
        /// Extrait le texte des images via Tesseract CLI (processus externe)
        /// Cette approche évite les problèmes de DLL natives qui causent des crashs
        /// </summary>
        private async Task<string> ExtractTextViaCliAsync(List<byte[]> images, string language)
        {
            if (images == null || images.Count == 0)
            {
                return string.Empty;
            }

            try
            {
                // Vérifier que Tesseract est installé
                var tesseractPath = FindTesseractExecutable();
                if (string.IsNullOrEmpty(tesseractPath))
                {
                    _logger.LogWarning("Tesseract CLI non trouvé. Vérifiez que Tesseract est installé et dans le PATH.");
                    return string.Empty;
                }

                _logger.LogInformation("Tesseract CLI trouvé : {Path}", tesseractPath);

                // Créer un dossier temporaire pour les images et le résultat
                var tempDir = Path.Combine(Path.GetTempPath(), $"tesseract_ocr_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    var fullText = new System.Text.StringBuilder();

                    // Traiter chaque page
                    for (int i = 0; i < images.Count; i++)
                    {
                        var imageBytes = images[i];
                        if (imageBytes == null || imageBytes.Length == 0)
                        {
                            continue;
                        }

                        // Sauvegarder l'image en TIFF (format optimal pour Tesseract)
                        var imagePath = Path.Combine(tempDir, $"page_{i + 1}.tiff");
                        var outputPath = Path.Combine(tempDir, $"output_{i + 1}");

                        // Convertir les bytes en image TIFF
                        await SaveImageAsTiffAsync(imageBytes, imagePath);

                        // Appeler Tesseract CLI
                        var text = await RunTesseractCliAsync(tesseractPath, imagePath, outputPath, language);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            fullText.AppendLine(text);
                        }
                    }

                    return fullText.ToString();
                }
                finally
                {
                    // Nettoyer le dossier temporaire
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossible de supprimer le dossier temporaire {Path}", tempDir);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction OCR via CLI : {Message}", ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Trouve l'exécutable Tesseract sur le système
        /// </summary>
        private string? FindTesseractExecutable()
        {
            // Chemins communs sur Windows (vérifier d'abord les chemins standards)
            var commonPaths = new[]
            {
                @"C:\Program Files\Tesseract-OCR\tesseract.exe",
                @"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
                Environment.GetEnvironmentVariable("TESSERACT_HOME") != null 
                    ? Path.Combine(Environment.GetEnvironmentVariable("TESSERACT_HOME")!, "tesseract.exe")
                    : null
            }.Where(p => p != null).Cast<string>();

            // Vérifier les chemins communs en premier (plus rapide)
            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogDebug("Tesseract trouvé dans le chemin standard : {Path}", path);
                    return path;
                }
            }

            // Vérifier dans le PATH (plus lent, donc en dernier)
            var executableNames = new[] { "tesseract.exe", "tesseract" };
            foreach (var exeName in executableNames)
            {
                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = exeName,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(processStartInfo);
                    if (process != null)
                    {
                        process.WaitForExit(2000);
                        if (process.ExitCode == 0)
                        {
                            _logger.LogDebug("Tesseract trouvé dans le PATH : {ExeName}", exeName);
                            return exeName; // Trouvé dans le PATH
                        }
                    }
                }
                catch
                {
                    // Ignorer et continuer
                }
            }

            _logger.LogWarning("Tesseract non trouvé. Vérifiez l'installation : https://github.com/UB-Mannheim/tesseract/wiki");
            return null;
        }

        /// <summary>
        /// Exécute Tesseract CLI et retourne le texte extrait
        /// </summary>
        private async Task<string> RunTesseractCliAsync(string tesseractPath, string imagePath, string outputPath, string language)
        {
            try
            {
                // Utiliser le dossier tessdata local de l'application si disponible
                var localTessdata = Path.Combine(_environment.ContentRootPath, "tessdata");
                var tessdataToUse = Directory.Exists(localTessdata) && File.Exists(Path.Combine(localTessdata, $"{language}.traineddata"))
                    ? localTessdata
                    : Path.Combine(Path.GetDirectoryName(tesseractPath) ?? "", "tessdata");
                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = tesseractPath,
                    Arguments = $"\"{imagePath}\" \"{outputPath}\" -l {language}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(imagePath)
                };
                
                // Définir TESSDATA_PREFIX
                processStartInfo.EnvironmentVariables["TESSDATA_PREFIX"] = tessdataToUse;

                _logger.LogDebug("Exécution Tesseract CLI : {Command} {Arguments}", processStartInfo.FileName, processStartInfo.Arguments);

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    _logger.LogError("Impossible de démarrer le processus Tesseract");
                    return string.Empty;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("Tesseract CLI a retourné le code {ExitCode}. Erreur : {Error}", process.ExitCode, error);
                    return string.Empty;
                }

                // Lire le fichier texte généré
                var textFilePath = $"{outputPath}.txt";
                if (File.Exists(textFilePath))
                {
                    var text = await File.ReadAllTextAsync(textFilePath);
                    _logger.LogDebug("Texte extrait : {Length} caractères", text.Length);
                    return text;
                }

                _logger.LogWarning("Fichier de sortie Tesseract non trouvé : {Path}", textFilePath);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution de Tesseract CLI : {Message}", ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Sauvegarde les bytes d'image en format TIFF (optimal pour Tesseract)
        /// </summary>
        private async Task SaveImageAsTiffAsync(byte[] imageBytes, string outputPath)
        {
            try
            {
                // Charger l'image depuis les bytes (format PNG depuis SkiaSharp)
                using var ms = new MemoryStream(imageBytes);
                using var bitmap = new Bitmap(ms);
                
                // Sauvegarder en TIFF avec compression LZW (optimal pour OCR)
                var tiffEncoder = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(e => e.FormatID == ImageFormat.Tiff.Guid);

                if (tiffEncoder != null)
                {
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
                    
                    bitmap.Save(outputPath, tiffEncoder, encoderParams);
                }
                else
                {
                    // Fallback : sauvegarder directement
                    bitmap.Save(outputPath, ImageFormat.Tiff);
                }

                _logger.LogDebug("Image sauvegardée en TIFF : {Path} ({Size} bytes)", outputPath, new FileInfo(outputPath).Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde de l'image en TIFF : {Message}", ex.Message);
                throw;
            }
        }

        private async Task<List<byte[]>> ConvertPdfToImagesAsync(byte[] pdfBytes)
        {
            try
            {
                _logger.LogInformation("Conversion PDF vers images : {Size} bytes", pdfBytes.Length);
                
                // PRIORITÉ 1 : Ghostscript directement (plus fiable pour PDF)
                var gsPath = FindGhostscriptExecutable();
                if (!string.IsNullOrEmpty(gsPath))
                {
                    _logger.LogInformation("Utilisation de Ghostscript pour la conversion PDF");
                    return await ConvertPdfToImagesWithGhostscriptAsync(pdfBytes, gsPath);
                }
                
                // PRIORITÉ 2 : ImageMagick (nécessite Ghostscript installé)
                var magickPath = FindImageMagickExecutable();
                if (!string.IsNullOrEmpty(magickPath))
                {
                    _logger.LogInformation("Utilisation d'ImageMagick pour la conversion PDF");
                    var result = await ConvertPdfToImagesWithImageMagickAsync(pdfBytes, magickPath);
                    if (result.Count > 0)
                    {
                        return result;
                    }
                    _logger.LogWarning("ImageMagick n'a pas généré d'images (Ghostscript manquant ?), fallback...");
                }
                
                // PRIORITÉ 3 : PdfiumViewer si disponible
                try
                {
                    return await ConvertPdfToImagesWithPdfiumAsync(pdfBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PdfiumViewer non disponible, utilisation du fallback");
                    return await ConvertPdfToImagesFallbackAsync(pdfBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la conversion PDF vers images");
                return new List<byte[]>();
            }
        }
        
        private string FindImageMagickExecutable()
        {
            // Vérifier les chemins courants (versions spécifiques)
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var commonPaths = new[]
            {
                Path.Combine(programFiles, "ImageMagick-7.1.2-Q16-HDRI", "magick.exe"),
                Path.Combine(programFiles, "ImageMagick-7.1.1-Q16-HDRI", "magick.exe"),
                Path.Combine(programFiles, "ImageMagick-7.1.0-Q16-HDRI", "magick.exe"),
                Path.Combine(programFiles, "ImageMagick-7.0.11-Q16-HDRI", "magick.exe"),
                Path.Combine(programFiles, "ImageMagick", "magick.exe")
            };
            
            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogDebug("ImageMagick trouvé : {Path}", path);
                    return path;
                }
            }
            
            // Recherche dynamique : chercher tous les dossiers ImageMagick-*
            try
            {
                if (Directory.Exists(programFiles))
                {
                    var imagemagickDirs = Directory.GetDirectories(programFiles, "ImageMagick-*");
                    foreach (var dir in imagemagickDirs)
                    {
                        var magickPath = Path.Combine(dir, "magick.exe");
                        if (File.Exists(magickPath))
                        {
                            _logger.LogDebug("ImageMagick trouvé (recherche dynamique) : {Path}", magickPath);
                            return magickPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Erreur lors de la recherche dynamique d'ImageMagick");
            }
            
            // Vérifier le PATH
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "magick",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(2000);
                    if (process.ExitCode == 0)
                    {
                        _logger.LogDebug("ImageMagick trouvé dans le PATH");
                        return "magick";
                    }
                }
            }
            catch
            {
                // Ignorer
            }
            
            return string.Empty;
        }
        
        private async Task<List<byte[]>> ConvertPdfToImagesWithImageMagickAsync(byte[] pdfBytes, string magickPath)
        {
            var images = new List<byte[]>();
            var tempDir = Path.Combine(Path.GetTempPath(), $"imagemagick_pdf_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Sauvegarder le PDF temporairement
                var pdfPath = Path.Combine(tempDir, "input.pdf");
                await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);
                
                _logger.LogDebug("PDF sauvegardé temporairement : {Path}", pdfPath);
                
                // ImageMagick peut convertir PDF → PNG directement
                // Syntaxe correcte : magick -density 300 input.pdf output-%02d.png
                var outputPattern = Path.Combine(tempDir, "page-%02d.png");
                var arguments = $"-density 300 \"{pdfPath}\" \"{outputPattern}\"";
                
                _logger.LogDebug("Conversion PDF via ImageMagick : {Command} {Args}", magickPath, arguments);
                
                var psi = new ProcessStartInfo
                {
                    FileName = magickPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir
                };
                
                using var process = Process.Start(psi);
                if (process == null)
                {
                    _logger.LogError("Impossible de démarrer ImageMagick");
                    return images;
                }
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("ImageMagick a retourné une erreur (code {ExitCode}): {Error}", process.ExitCode, error);
                    // Ne pas retourner immédiatement, vérifier quand même si des fichiers ont été créés
                }
                
                // Attendre un peu pour que les fichiers soient écrits
                await Task.Delay(500);
                
                // Lire les images générées (chercher les deux patterns possibles)
                var pageFiles1 = Directory.GetFiles(tempDir, "page-*.png").OrderBy(f => f).ToList();
                var pageFiles2 = Directory.GetFiles(tempDir, "page_*.png").OrderBy(f => f).ToList();
                var pageFiles = pageFiles1.Concat(pageFiles2).Distinct().OrderBy(f => f).ToList();
                
                _logger.LogInformation("ImageMagick a généré {Count} pages (pattern1: {Count1}, pattern2: {Count2})", 
                    pageFiles.Count, pageFiles1.Count, pageFiles2.Count);
                
                if (pageFiles.Count == 0 && !string.IsNullOrWhiteSpace(error))
                {
                    _logger.LogWarning("ImageMagick erreur détaillée : {Error}", error);
                }
                
                foreach (var pageFile in pageFiles)
                {
                    var imageBytes = await System.IO.File.ReadAllBytesAsync(pageFile);
                    images.Add(imageBytes);
                    _logger.LogDebug("Page convertie : {File} ({Size} bytes)", Path.GetFileName(pageFile), imageBytes.Length);
                }
                
                _logger.LogInformation("Conversion ImageMagick terminée : {ImageCount} images créées", images.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la conversion PDF via ImageMagick : {Message}", ex.Message);
            }
            finally
            {
                // Nettoyer
                if (Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, true);
                        _logger.LogDebug("Dossier temporaire ImageMagick nettoyé");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors du nettoyage du dossier temporaire ImageMagick");
                    }
                }
            }
            
            return images;
        }
        
        private async Task<List<byte[]>> ConvertPdfToImagesWithPdfiumAsync(byte[] pdfBytes)
        {
            var images = new List<byte[]>();
            
            try
            {
                // Utiliser PdfiumViewer pour le rendu complet
                using var pdfStream = new MemoryStream(pdfBytes);
                using var pdfDocument = PdfiumViewer.PdfDocument.Load(pdfStream);
                
                var pageCount = pdfDocument.PageCount;
                _logger.LogDebug("PDF ouvert avec PdfiumViewer : {PageCount} pages", pageCount);
                
                const int dpi = 300; // Résolution pour l'OCR
                
                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    try
                    {
                        // Récupérer les dimensions de la page
                        var size = pdfDocument.PageSizes[pageIndex];
                        var widthPoints = (double)size.Width;
                        var heightPoints = (double)size.Height;
                        
                        var width = (int)(widthPoints * dpi / 72.0);
                        var height = (int)(heightPoints * dpi / 72.0);
                        
                        // Rendre la page PDF en bitmap System.Drawing
                        using var bitmap = pdfDocument.Render(
                            pageIndex,
                            width,
                            height,
                            dpi,
                            dpi,
                            PdfiumViewer.PdfRenderFlags.Annotations) as Bitmap;
                        
                        if (bitmap == null)
                        {
                            _logger.LogWarning("Impossible de rendre la page {PageIndex}", pageIndex + 1);
                            continue;
                        }
                        
                        // Convertir System.Drawing.Bitmap en SkiaSharp SKBitmap
                        var skBitmap = ConvertToSkiaBitmap(bitmap);
                        
                        // Encoder en PNG
                        using var image = SKImage.FromBitmap(skBitmap);
                        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                        var imageBytes = data.ToArray();
                        
                        images.Add(imageBytes);
                        _logger.LogDebug("Page {PageIndex} convertie avec PdfiumViewer : {Width}x{Height} pixels, {Size} bytes", 
                            pageIndex + 1, width, height, imageBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erreur lors de la conversion de la page {PageIndex} avec PdfiumViewer", pageIndex + 1);
                    }
                }
                
                if (images.Count > 0)
                {
                    _logger.LogInformation("Conversion PdfiumViewer terminée : {ImageCount} images créées", images.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PdfiumViewer non disponible ou erreur : {Message}", ex.Message);
                throw; // Relancer pour déclencher le fallback
            }
            
            return images;
        }
        
        private async Task<List<byte[]>> ConvertPdfToImagesFallbackAsync(byte[] pdfBytes)
        {
            try
            {
                _logger.LogInformation("Tentative de conversion PDF via Ghostscript (CLI)");
                
                // Essayer d'abord Ghostscript (très stable, pas de DLL natives)
                var gsPath = FindGhostscriptExecutable();
                if (!string.IsNullOrEmpty(gsPath))
                {
                    _logger.LogInformation("Ghostscript trouvé : {Path}", gsPath);
                    return await ConvertPdfToImagesWithGhostscriptAsync(pdfBytes, gsPath);
                }
                
                // Fallback : PdfSharpCore + System.Drawing (sans SkiaSharp)
                _logger.LogWarning("Ghostscript non trouvé, utilisation du fallback PdfSharpCore + System.Drawing");
                return await ConvertPdfToImagesWithSystemDrawingAsync(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur critique lors du fallback PDF vers images : {Message}", ex.Message);
                return new List<byte[]>();
            }
        }
        
        private string FindGhostscriptExecutable()
        {
            // Vérifier les chemins courants
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            var commonPaths = new[]
            {
                Path.Combine(programFiles, "gs", "gs10.03.1", "bin", "gswin64c.exe"),
                Path.Combine(programFiles, "gs", "gs10.02.1", "bin", "gswin64c.exe"),
                Path.Combine(programFiles, "gs", "gs10.01.2", "bin", "gswin64c.exe"),
                Path.Combine(programFilesX86, "gs", "gs10.03.1", "bin", "gswin32c.exe"),
                Path.Combine(programFilesX86, "gs", "gs10.02.1", "bin", "gswin32c.exe"),
                Path.Combine(programFilesX86, "gs", "gs10.01.2", "bin", "gswin32c.exe")
            };
            
            foreach (var path in commonPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    return path;
                }
            }
            
            // Vérifier le PATH
            var values = Environment.GetEnvironmentVariable("PATH");
            if (values != null)
            {
                foreach (var path in values.Split(Path.PathSeparator))
                {
                    var fullPath64 = Path.Combine(path, "gswin64c.exe");
                    var fullPath32 = Path.Combine(path, "gswin32c.exe");
                    if (System.IO.File.Exists(fullPath64)) return fullPath64;
                    if (System.IO.File.Exists(fullPath32)) return fullPath32;
                }
            }
            
            return string.Empty;
        }
        
        private async Task<List<byte[]>> ConvertPdfToImagesWithGhostscriptAsync(byte[] pdfBytes, string gsPath)
        {
            var images = new List<byte[]>();
            var tempDir = Path.Combine(Path.GetTempPath(), $"gs_pdf_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Sauvegarder le PDF temporairement
                var pdfPath = Path.Combine(tempDir, "input.pdf");
                await System.IO.File.WriteAllBytesAsync(pdfPath, pdfBytes);
                
                _logger.LogDebug("PDF sauvegardé temporairement : {Path}", pdfPath);
                
                // Compter les pages avec Ghostscript
                var pageCount = await GetPdfPageCountAsync(gsPath, pdfPath);
                _logger.LogInformation("PDF contient {PageCount} pages", pageCount);
                
                // Convertir chaque page en PNG
                for (int pageIndex = 1; pageIndex <= pageCount; pageIndex++)
                {
                    try
                    {
                        var outputPath = Path.Combine(tempDir, $"page_{pageIndex}.png");
                        var arguments = $"-dNOPAUSE -dBATCH -sDEVICE=png16m -r300 -dFirstPage={pageIndex} -dLastPage={pageIndex} -sOutputFile=\"{outputPath}\" \"{pdfPath}\"";
                        
                        _logger.LogDebug("Conversion page {PageIndex} via Ghostscript", pageIndex);
                        
                        var psi = new ProcessStartInfo
                        {
                            FileName = gsPath,
                            Arguments = arguments,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using var process = Process.Start(psi);
                        if (process == null)
                        {
                            _logger.LogError("Impossible de démarrer Ghostscript pour la page {PageIndex}", pageIndex);
                            continue;
                        }
                        
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();
                        
                        if (process.ExitCode != 0)
                        {
                            _logger.LogWarning("Ghostscript a retourné une erreur (code {ExitCode}) pour la page {PageIndex}: {Error}", 
                                process.ExitCode, pageIndex, error);
                            continue;
                        }
                        
                        if (System.IO.File.Exists(outputPath))
                        {
                            var imageBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
                            images.Add(imageBytes);
                            _logger.LogDebug("Page {PageIndex} convertie : {Size} bytes", pageIndex, imageBytes.Length);
                            System.IO.File.Delete(outputPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de la conversion de la page {PageIndex} via Ghostscript", pageIndex);
                    }
                }
                
                _logger.LogInformation("Conversion Ghostscript terminée : {ImageCount} images créées", images.Count);
            }
            finally
            {
                // Nettoyer
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                    _logger.LogDebug("Dossier temporaire Ghostscript nettoyé");
                }
            }
            
            return images;
        }
        
        private async Task<int> GetPdfPageCountAsync(string gsPath, string pdfPath)
        {
            try
            {
                var arguments = $"-q -dNODISPLAY -c \"({pdfPath}) (r) file runpdfbegin pdfpagecount = quit\"";
                var psi = new ProcessStartInfo
                {
                    FileName = gsPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(psi);
                if (process == null) return 0;
                
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (int.TryParse(output.Trim(), out int count))
                {
                    return count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de compter les pages via Ghostscript, utilisation de PdfSharpCore");
            }
            
            // Fallback : utiliser PdfSharpCore pour compter
            try
            {
                using var pdfStream = new MemoryStream(await System.IO.File.ReadAllBytesAsync(pdfPath));
                var pdfDocument = PdfReader.Open(pdfStream, PdfDocumentOpenMode.ReadOnly);
                var count = pdfDocument?.PageCount ?? 0;
                pdfDocument?.Close();
                return count;
            }
            catch
            {
                return 0;
            }
        }
        
        private async Task<List<byte[]>> ConvertPdfToImagesWithSystemDrawingAsync(byte[] pdfBytes)
        {
            var images = new List<byte[]>();
            
            try
            {
                using var pdfStream = new MemoryStream(pdfBytes);
                var pdfDocument = PdfReader.Open(pdfStream, PdfDocumentOpenMode.ReadOnly);
                
                if (pdfDocument == null || pdfDocument.PageCount == 0)
                {
                    _logger.LogWarning("PDF vide ou invalide");
                    pdfDocument?.Close();
                    return images;
                }
                
                _logger.LogDebug("Conversion de {PageCount} pages avec System.Drawing", pdfDocument.PageCount);
                
                const double dpi = 200.0; // Réduire à 200 DPI pour éviter les problèmes de mémoire
                const double pointsPerInch = 72.0;
                
                for (int pageIndex = 0; pageIndex < pdfDocument.PageCount; pageIndex++)
                {
                    try
                    {
                        var page = pdfDocument.Pages[pageIndex];
                        var width = (int)(page.Width.Point * dpi / pointsPerInch);
                        var height = (int)(page.Height.Point * dpi / pointsPerInch);
                        
                        // Limiter à 2000px max pour éviter les crashes
                        if (width > 2000 || height > 2000)
                        {
                            var ratio = Math.Min(2000.0 / width, 2000.0 / height);
                            width = (int)(width * ratio);
                            height = (int)(height * ratio);
                        }
                        
                        _logger.LogDebug("Création image {Width}x{Height} pour page {PageIndex}", width, height, pageIndex + 1);
                        
                        // Utiliser System.Drawing au lieu de SkiaSharp
                        using var bitmap = new System.Drawing.Bitmap(width, height);
                        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
                        graphics.Clear(System.Drawing.Color.White);
                        
                        // Sauvegarder en PNG via MemoryStream
                        using var ms = new MemoryStream();
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var imageBytes = ms.ToArray();
                        
                        images.Add(imageBytes);
                        _logger.LogDebug("Page {PageIndex} convertie : {Size} bytes", pageIndex + 1, imageBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de la conversion de la page {PageIndex} : {Message}", pageIndex + 1, ex.Message);
                    }
                }
                
                pdfDocument.Close();
                _logger.LogInformation("Conversion System.Drawing terminée : {ImageCount} images créées", images.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la conversion PDF avec System.Drawing : {Message}", ex.Message);
            }
            
            return images;
        }
        
        private SKBitmap ConvertToSkiaBitmap(Bitmap bitmap)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            
            try
            {
                var ptr = bitmapData.Scan0;
                var bytes = new byte[bitmapData.Stride * height];
                System.Runtime.InteropServices.Marshal.Copy(ptr, bytes, 0, bytes.Length);
                
                // Copier les données dans SkiaSharp
                var skPixels = skBitmap.GetPixels();
                System.Runtime.InteropServices.Marshal.Copy(bytes, 0, skPixels, bytes.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            
            return skBitmap;
        }

        private static bool IsCommonWord(string word)
        {
            // Mots communs à exclure des noms
            var commonWords = new[]
            {
                "Le", "La", "Les", "De", "Du", "Des", "Et", "Ou", "Un", "Une",
                "Formation", "Session", "Date", "Nom", "Prénom", "Signature",
                "Matin", "Après", "Midi", "Jour", "Heure"
            };
            
            return commonWords.Contains(word, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class EmargementData
    {
        public string RawText { get; set; } = string.Empty;
        public List<string> Names { get; set; } = new();
        public List<DateTime> Dates { get; set; } = new();
        public bool HasSignatures { get; set; }
        public bool HasTableStructure { get; set; }
        public DateTime ExtractedAt { get; set; }
        public int Confidence { get; set; }
    }
}
