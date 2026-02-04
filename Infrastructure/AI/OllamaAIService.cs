using System.Net.Http.Json;
using System.Text.Json;
using FormationManager.Infrastructure.Exceptions;
using Polly;
using Polly.Retry;

namespace FormationManager.Infrastructure.AI
{
    public interface IAIService
    {
        Task<DocumentAnalysis> AnalyzeDocumentAsync(string text, DocumentType documentType);
        Task<string> ClassifyQualiopiAsync(string documentText);
        Task<List<string>> ExtractKeywordsAsync(string text);
        Task<bool> IsServiceAvailableAsync();
    }

    public class OllamaAIService : IAIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OllamaAIService> _logger;
        private readonly string _ollamaBaseUrl;
        private readonly string _modelName;
        private readonly int _timeoutSeconds;
        private readonly AsyncRetryPolicy _retryPolicy;

        public OllamaAIService(
            IHttpClientFactory httpClientFactory,
            ILogger<OllamaAIService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            _modelName = configuration["Ollama:Model"] ?? "mistral";
            _timeoutSeconds = configuration.GetValue<int>("Ollama:TimeoutSeconds", 120);

            // Policy de retry avec exponential backoff
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Tentative {RetryCount} après {Delay}s : {Exception}",
                            retryCount, timespan.TotalSeconds, exception?.Message);
                    });
        }

        public async Task<DocumentAnalysis> AnalyzeDocumentAsync(string text, DocumentType documentType)
        {
            try
            {
                _logger.LogInformation("Analyse document de type {DocumentType}", documentType);

                var prompt = BuildAnalysisPrompt(text, documentType);
                var response = await CallOllamaAsync(prompt);

                var analysis = new DocumentAnalysis
                {
                    DocumentType = documentType,
                    Summary = ExtractSummary(response),
                    QualiopiCriteria = ExtractQualiopiCriteria(response),
                    Keywords = await ExtractKeywordsAsync(text),
                    Confidence = ExtractConfidence(response),
                    AnalyzedAt = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "Analyse terminée : {CriteriaCount} critères Qualiopi identifiés, Confiance: {Confidence}%",
                    analysis.QualiopiCriteria.Count, analysis.Confidence);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'analyse document");
                throw new AIException($"Erreur analyse IA : {ex.Message}", ex);
            }
        }

        public async Task<string> ClassifyQualiopiAsync(string documentText)
        {
            try
            {
                var prompt = $@"Analyse ce document de formation et identifie les critères Qualiopi pertinents (1 à 7).

Document :
{documentText}

Réponds uniquement avec les numéros de critères séparés par des virgules, par exemple : 1,3,5";

                var response = await CallOllamaAsync(prompt);
                return response.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la classification Qualiopi");
                throw new AIException("Erreur classification Qualiopi", ex);
            }
        }

        public async Task<List<string>> ExtractKeywordsAsync(string text)
        {
            try
            {
                var prompt = $@"Extrait les mots-clés importants de ce texte (maximum 10).

Texte :
{text}

Réponds uniquement avec les mots-clés séparés par des virgules.";

                var response = await CallOllamaAsync(prompt);
                return response.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction de mots-clés");
                return new List<string>();
            }
        }

        public async Task<bool> IsServiceAvailableAsync()
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetAsync($"{_ollamaBaseUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ollama n'est pas disponible");
                return false;
            }
        }

        private async Task<string> CallOllamaAsync(string prompt)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

                var request = new
                {
                    model = _modelName,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        top_p = 0.9
                    }
                };

                var response = await client.PostAsJsonAsync(
                    $"{_ollamaBaseUrl}/api/generate",
                    request);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                return result?.response ?? string.Empty;
            });
        }

        private string BuildAnalysisPrompt(string text, DocumentType documentType)
        {
            var documentTypeName = documentType switch
            {
                DocumentType.Emargement => "feuille d'émargement",
                DocumentType.Programme => "programme de formation",
                DocumentType.Evaluation => "évaluation",
                DocumentType.Convention => "convention de formation",
                DocumentType.Attestation => "attestation",
                _ => "document"
            };

            return $@"Tu es un expert en certification Qualiopi pour la formation professionnelle.

Analyse ce document de type {documentTypeName} et fournis :
1. Un résumé concis (2-3 phrases)
2. Les critères Qualiopi pertinents (numéros de 1 à 7)
3. Un niveau de confiance dans l'analyse (0-100%)

Document :
{text}

Réponds au format suivant :
Résumé: [résumé du document]
Critères: [numéros séparés par des virgules]
Confiance: [pourcentage]%";
        }

        private string ExtractSummary(string response)
        {
            // Extraction du résumé depuis la réponse
            var summaryMatch = System.Text.RegularExpressions.Regex.Match(
                response, 
                @"Résumé[:\s]+(.*?)(?:\n|Critères|$)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            
            if (summaryMatch.Success && summaryMatch.Groups.Count > 1)
            {
                return summaryMatch.Groups[1].Value.Trim();
            }

            // Fallback : première ligne significative
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return lines.FirstOrDefault(l => l.Length > 20) ?? lines.FirstOrDefault() ?? string.Empty;
        }

        private List<int> ExtractQualiopiCriteria(string response)
        {
            var criteria = new List<int>();
            
            // Extraction depuis "Critères: 1,3,5"
            var criteriaMatch = System.Text.RegularExpressions.Regex.Match(
                response,
                @"Critères[:\s]+([0-9,\s]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (criteriaMatch.Success && criteriaMatch.Groups.Count > 1)
            {
                var criteriaStr = criteriaMatch.Groups[1].Value;
                foreach (var numStr in criteriaStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(numStr.Trim(), out var criterion) && criterion >= 1 && criterion <= 7)
                    {
                        criteria.Add(criterion);
                    }
                }
            }
            else
            {
                // Fallback : recherche directe des numéros 1-7
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    response, @"\b([1-7])\b");
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (int.TryParse(match.Value, out var criterion))
                    {
                        criteria.Add(criterion);
                    }
                }
            }

            return criteria.Distinct().OrderBy(c => c).ToList();
        }

        private int ExtractConfidence(string response)
        {
            // Extraction depuis "Confiance: 85%"
            var confidenceMatch = System.Text.RegularExpressions.Regex.Match(
                response,
                @"Confiance[:\s]+(\d+)%",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (confidenceMatch.Success && confidenceMatch.Groups.Count > 1)
            {
                if (int.TryParse(confidenceMatch.Groups[1].Value, out var confidence))
                {
                    return Math.Clamp(confidence, 0, 100);
                }
            }

            // Fallback : recherche d'un pourcentage dans le texte
            var percentageMatch = System.Text.RegularExpressions.Regex.Match(
                response,
                @"(\d+)%");
            
            if (percentageMatch.Success && int.TryParse(percentageMatch.Groups[1].Value, out var percentage))
            {
                return Math.Clamp(percentage, 0, 100);
            }

            return 50; // Valeur par défaut
        }
    }

    public class DocumentAnalysis
    {
        public DocumentType DocumentType { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<int> QualiopiCriteria { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public int Confidence { get; set; }
        public DateTime AnalyzedAt { get; set; }
    }

    public enum DocumentType
    {
        Emargement,
        Programme,
        Evaluation,
        Convention,
        Attestation,
        Autre
    }

    // Classes pour désérialisation Ollama
    internal class OllamaResponse
    {
        public string response { get; set; } = string.Empty;
        public bool done { get; set; }
    }
}