using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;

namespace FormationManager.Services
{
    public interface ICritereSuggestionService
    {
        Task<List<CritereSuggestion>> GetSuggestionsAsync(
            int? sessionId,
            string? fileName,
            TypeDocument? documentType);
    }

    public class CritereSuggestionService : ICritereSuggestionService
    {
        private readonly FormationDbContext _context;
        private readonly ILogger<CritereSuggestionService> _logger;

        // Mapping mots-clés → critères suggérés
        private static readonly Dictionary<string, List<int>> KeywordToCriteres = new()
        {
            // Critère 1 - Information du public
            { "programme", new List<int> { 6, 1 } },
            { "fiche", new List<int> { 1 } },
            { "descriptif", new List<int> { 1 } },
            { "information", new List<int> { 1 } },
            { "catalogue", new List<int> { 1 } },
            { "brochure", new List<int> { 1 } },
            { "presentation", new List<int> { 1 } },
            
            // Critère 2 - Objectifs
            { "convention", new List<int> { 2 } },
            { "contrat", new List<int> { 2 } },
            { "objectif", new List<int> { 2 } },
            { "engagement", new List<int> { 2 } },
            { "accord", new List<int> { 2 } },
            
            // Critère 3 - Conditions de déroulement
            { "emargement", new List<int> { 3 } },
            { "émargement", new List<int> { 3 } },
            { "presence", new List<int> { 3 } },
            { "présence", new List<int> { 3 } },
            { "planning", new List<int> { 3 } },
            { "horaires", new List<int> { 3 } },
            { "lieu", new List<int> { 3 } },
            { "local", new List<int> { 3 } },
            { "salle", new List<int> { 3 } },
            
            // Critère 4 - Analyse du besoin
            { "besoin", new List<int> { 4 } },
            { "prerequis", new List<int> { 4 } },
            { "prérequis", new List<int> { 4 } },
            { "positionnement", new List<int> { 4, 8 } },
            { "diagnostic", new List<int> { 4 } },
            { "analyse", new List<int> { 4 } },
            
            // Critère 5 - Moyens humains
            { "formateur", new List<int> { 5, 17, 21 } },
            { "intervenant", new List<int> { 5, 17 } },
            { "cv", new List<int> { 17, 21 } },
            { "competence", new List<int> { 21 } },
            { "compétence", new List<int> { 21 } },
            { "diplome", new List<int> { 21 } },
            { "diplôme", new List<int> { 21 } },
            { "qualification", new List<int> { 21 } },
            
            // Critère 6 - Contenus
            { "contenu", new List<int> { 6 } },
            { "pedagogique", new List<int> { 6 } },
            { "pédagogique", new List<int> { 6 } },
            { "modalite", new List<int> { 6 } },
            { "modalité", new List<int> { 6 } },
            { "methode", new List<int> { 6 } },
            { "méthode", new List<int> { 6 } },
            { "support", new List<int> { 6 } },
            
            // Critère 7 - Recueil des appréciations
            { "evaluation", new List<int> { 7, 30 } },
            { "évaluation", new List<int> { 7, 30 } },
            { "attestation", new List<int> { 7 } },
            { "satisfaction", new List<int> { 7 } },
            { "appreciation", new List<int> { 7 } },
            { "appréciation", new List<int> { 7 } },
            { "questionnaire", new List<int> { 7 } },
            { "avis", new List<int> { 7 } },
            { "retour", new List<int> { 7 } }
        };

        public CritereSuggestionService(
            FormationDbContext context,
            ILogger<CritereSuggestionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CritereSuggestion>> GetSuggestionsAsync(
            int? sessionId,
            string? fileName,
            TypeDocument? documentType)
        {
            var allSuggestions = new List<CritereSuggestion>();

            // 1. Suggestions depuis nom de fichier
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var fileNameSuggestions = SuggestCriteresFromFileName(fileName);
                allSuggestions.AddRange(fileNameSuggestions);
                _logger.LogDebug("Suggestions depuis nom de fichier '{FileName}': {Count}", fileName, fileNameSuggestions.Count);
            }

            // 2. Suggestions depuis type de document
            if (documentType.HasValue)
            {
                var typeSuggestions = SuggestCriteresFromDocumentType(documentType.Value);
                allSuggestions.AddRange(typeSuggestions);
                _logger.LogDebug("Suggestions depuis type de document '{Type}': {Count}", documentType.Value, typeSuggestions.Count);
            }

            // 3. Suggestions depuis historique
            if (sessionId.HasValue)
            {
                var historySuggestions = await SuggestCriteresFromHistoryAsync(sessionId.Value);
                allSuggestions.AddRange(historySuggestions);
                _logger.LogDebug("Suggestions depuis historique session {SessionId}: {Count}", sessionId.Value, historySuggestions.Count);
            }

            // 4. Suggestions critères manquants
            if (sessionId.HasValue)
            {
                var missingSuggestions = await SuggestMissingCriteresAsync(sessionId.Value);
                allSuggestions.AddRange(missingSuggestions);
                _logger.LogDebug("Suggestions critères manquants session {SessionId}: {Count}", sessionId.Value, missingSuggestions.Count);
            }

            // Fusionner et trier par confiance
            var mergedSuggestions = allSuggestions
                .GroupBy(s => s.Critere)
                .Select(g => new CritereSuggestion
                {
                    Critere = g.Key,
                    Confidence = g.Max(s => s.Confidence),
                    Reason = string.Join(" | ", g.Select(s => s.Reason).Distinct()),
                    IsMissing = g.Any(s => s.IsMissing)
                })
                .OrderByDescending(s => s.IsMissing) // Critères manquants en premier
                .ThenByDescending(s => s.Confidence)
                .ToList();

            _logger.LogInformation("Total suggestions générées: {Count}", mergedSuggestions.Count);
            return mergedSuggestions;
        }

        private List<CritereSuggestion> SuggestCriteresFromFileName(string fileName)
        {
            var suggestions = new List<CritereSuggestion>();
            var lowerFileName = fileName.ToLowerInvariant();

            foreach (var (keyword, criteres) in KeywordToCriteres)
            {
                if (lowerFileName.Contains(keyword))
                {
                    foreach (var critere in criteres)
                    {
                        suggestions.Add(new CritereSuggestion
                        {
                            Critere = critere,
                            Confidence = 0.8, // Confiance élevée si mot-clé trouvé
                            Reason = $"Le nom du fichier contient '{keyword}'"
                        });
                    }
                }
            }

            return suggestions;
        }

        private List<CritereSuggestion> SuggestCriteresFromDocumentType(TypeDocument typeDocument)
        {
            return typeDocument switch
            {
                TypeDocument.PreuveQualiopi => new List<CritereSuggestion>
                {
                    new() { Critere = 6, Confidence = 0.9, Reason = "Un document de preuve Qualiopi correspond généralement au Critère 6 (Contenus)" },
                    new() { Critere = 1, Confidence = 0.7, Reason = "Peut aussi servir pour le Critère 1 (Information du public)" }
                },
                
                TypeDocument.Convention => new List<CritereSuggestion>
                {
                    new() { Critere = 2, Confidence = 0.95, Reason = "Une convention correspond au Critère 2 (Objectifs)" }
                },
                
                TypeDocument.Emargement => new List<CritereSuggestion>
                {
                    new() { Critere = 3, Confidence = 0.95, Reason = "Une feuille d'émargement correspond au Critère 3 (Conditions de déroulement)" }
                },
                
                TypeDocument.Attestation => new List<CritereSuggestion>
                {
                    new() { Critere = 7, Confidence = 0.9, Reason = "Une attestation correspond au Critère 7 (Recueil des appréciations)" }
                },
                
                TypeDocument.Evaluation => new List<CritereSuggestion>
                {
                    new() { Critere = 7, Confidence = 0.9, Reason = "Une évaluation correspond au Critère 7 (Recueil des appréciations)" },
                    new() { Critere = 3, Confidence = 0.6, Reason = "Peut aussi servir pour le Critère 3 (Atteinte des objectifs)" }
                },
                
                _ => new List<CritereSuggestion>()
            };
        }

        private async Task<List<CritereSuggestion>> SuggestCriteresFromHistoryAsync(int sessionId)
        {
            var existingPreuves = await _context.PreuvesQualiopi
                .Where(p => p.SessionId == sessionId)
                .Include(p => p.Indicateur)
                .GroupBy(p => p.Indicateur.Critere)
                .Select(g => new
                {
                    Critere = g.Key,
                    Count = g.Count(),
                    LastUsed = g.Max(p => p.DateCreation)
                })
                .ToListAsync();

            return existingPreuves.Select(e => new CritereSuggestion
            {
                Critere = e.Critere,
                Confidence = 0.6,
                Reason = $"Déjà utilisé {e.Count} fois pour cette session (dernière fois le {e.LastUsed:dd/MM/yyyy})"
            }).ToList();
        }

        private async Task<List<CritereSuggestion>> SuggestMissingCriteresAsync(int sessionId)
        {
            var allCriteres = await _context.IndicateursQualiopi
                .Select(i => i.Critere)
                .Distinct()
                .ToListAsync();

            var validatedCriteres = await _context.PreuvesQualiopi
                .Where(p => p.SessionId == sessionId && p.EstValide)
                .Include(p => p.Indicateur)
                .Select(p => p.Indicateur.Critere)
                .Distinct()
                .ToListAsync();

            var missingCriteres = allCriteres.Except(validatedCriteres).ToList();

            return missingCriteres.Select(c => new CritereSuggestion
            {
                Critere = c,
                Confidence = 0.7,
                Reason = "⚠️ Critère manquant pour cette session (aucune preuve validée)",
                IsMissing = true
            }).ToList();
        }
    }

    public class CritereSuggestion
    {
        public int Critere { get; set; }
        public double Confidence { get; set; } // 0.0 à 1.0
        public string Reason { get; set; } = string.Empty;
        public bool IsMissing { get; set; } // Critère manquant pour la session
    }

}
