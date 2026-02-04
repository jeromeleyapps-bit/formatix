# Système de Guidage Intelligent pour la Sélection des Critères Qualiopi

## 🎯 Objectif

Guider l'utilisateur dans la sélection du bon critère Qualiopi **sans imposer**, en utilisant :
- Analyse du nom de fichier
- Type de document
- Historique des preuves de la session
- Critères manquants
- Descriptions contextuelles

---

## 🧠 Stratégies de Guidage

### 1. Analyse du Nom de Fichier

**Détection de mots-clés dans le nom du fichier :**

```csharp
public class CritereSuggestionService
{
    // Mapping mots-clés → critères suggérés
    private static readonly Dictionary<string, List<int>> KeywordToCriteres = new()
    {
        // Critère 1 - Information du public
        { "programme", new List<int> { 6, 1 } },
        { "fiche", new List<int> { 1 } },
        { "descriptif", new List<int> { 1 } },
        { "information", new List<int> { 1 } },
        { "catalogue", new List<int> { 1 } },
        
        // Critère 2 - Objectifs
        { "convention", new List<int> { 2 } },
        { "contrat", new List<int> { 2 } },
        { "objectif", new List<int> { 2 } },
        { "engagement", new List<int> { 2 } },
        
        // Critère 3 - Conditions de déroulement
        { "emargement", new List<int> { 3 } },
        { "presence", new List<int> { 3 } },
        { "planning", new List<int> { 3 } },
        { "horaires", new List<int> { 3 } },
        { "lieu", new List<int> { 3 } },
        
        // Critère 4 - Analyse du besoin
        { "besoin", new List<int> { 4 } },
        { "prerequis", new List<int> { 4 } },
        { "positionnement", new List<int> { 4, 8 } },
        { "diagnostic", new List<int> { 4 } },
        
        // Critère 5 - Moyens humains
        { "formateur", new List<int> { 5, 17, 21 } },
        { "intervenant", new List<int> { 5, 17 } },
        { "cv", new List<int> { 17, 21 } },
        { "competence", new List<int> { 21 } },
        
        // Critère 6 - Contenus
        { "contenu", new List<int> { 6 } },
        { "pedagogique", new List<int> { 6 } },
        { "modalite", new List<int> { 6 } },
        { "methode", new List<int> { 6 } },
        
        // Critère 7 - Recueil des appréciations
        { "evaluation", new List<int> { 7, 30 } },
        { "attestation", new List<int> { 7 } },
        { "satisfaction", new List<int> { 7 } },
        { "appreciation", new List<int> { 7 } },
        { "questionnaire", new List<int> { 7 } }
    };

    public List<CritereSuggestion> SuggestCriteresFromFileName(string fileName)
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

        return suggestions
            .GroupBy(s => s.Critere)
            .Select(g => new CritereSuggestion
            {
                Critere = g.Key,
                Confidence = g.Max(s => s.Confidence),
                Reason = string.Join(", ", g.Select(s => s.Reason))
            })
            .OrderByDescending(s => s.Confidence)
            .ToList();
    }
}
```

### 2. Analyse du Type de Document

**Mapping type de document → critères :**

```csharp
public List<CritereSuggestion> SuggestCriteresFromDocumentType(TypeDocument typeDocument)
{
    return typeDocument switch
    {
        TypeDocument.Programme => new List<CritereSuggestion>
        {
            new() { Critere = 6, Confidence = 0.9, Reason = "Un programme correspond généralement au Critère 6 (Contenus)" },
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
```

### 3. Historique des Preuves de la Session

**Suggérer les critères déjà utilisés pour cette session :**

```csharp
public async Task<List<CritereSuggestion>> SuggestCriteresFromHistory(int sessionId)
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
```

### 4. Critères Manquants pour la Session

**Suggérer les critères qui n'ont pas encore de preuve validée :**

```csharp
public async Task<List<CritereSuggestion>> SuggestMissingCriteres(int sessionId)
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
```

### 5. Service Complet de Suggestion

```csharp
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

    public async Task<List<CritereSuggestion>> GetSuggestionsAsync(
        int? sessionId,
        string? fileName,
        TypeDocument? documentType)
    {
        var allSuggestions = new List<CritereSuggestion>();

        // 1. Suggestions depuis nom de fichier
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            allSuggestions.AddRange(SuggestCriteresFromFileName(fileName));
        }

        // 2. Suggestions depuis type de document
        if (documentType.HasValue)
        {
            allSuggestions.AddRange(SuggestCriteresFromDocumentType(documentType.Value));
        }

        // 3. Suggestions depuis historique
        if (sessionId.HasValue)
        {
            var historySuggestions = await SuggestCriteresFromHistory(sessionId.Value);
            allSuggestions.AddRange(historySuggestions);
        }

        // 4. Suggestions critères manquants
        if (sessionId.HasValue)
        {
            var missingSuggestions = await SuggestMissingCriteres(sessionId.Value);
            allSuggestions.AddRange(missingSuggestions);
        }

        // Fusionner et trier par confiance
        return allSuggestions
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
    }
}

public class CritereSuggestion
{
    public int Critere { get; set; }
    public double Confidence { get; set; } // 0.0 à 1.0
    public string Reason { get; set; } = string.Empty;
    public bool IsMissing { get; set; } // Critère manquant pour la session
}
```

---

## 🎨 Interface Utilisateur Améliorée

### Vue CreatePreuve avec Suggestions

```html
@model CreatePreuveViewModel

<div class="card">
    <div class="card-body">
        <form asp-action="CreatePreuve" method="post" enctype="multipart/form-data" id="createPreuveForm">
            
            <!-- Session -->
            <div class="mb-3">
                <label class="form-label">📋 Session de Formation *</label>
                <select name="sessionId" id="sessionSelect" class="form-select" required>
                    <option value="">-- Sélectionner une session --</option>
                    @foreach (var session in ViewBag.Sessions)
                    {
                        <option value="@session.Id">@session.Formation?.Titre - @session.DateDebut.ToString("dd/MM/yyyy")</option>
                    }
                </select>
            </div>

            <!-- Document Upload -->
            <div class="mb-3">
                <label class="form-label">📄 Document (optionnel)</label>
                <input type="file" name="fichier" id="fileInput" class="form-control" 
                       accept=".pdf,.jpg,.jpeg,.png" />
                <small class="form-text text-muted">
                    Formats acceptés : PDF, JPEG, PNG (max 50MB)
                </small>
            </div>

            <!-- Suggestions de Critères (affichées dynamiquement) -->
            <div id="critereSuggestions" class="mb-3" style="display: none;">
                <label class="form-label">💡 Suggestions de Critères</label>
                <div class="alert alert-info">
                    <strong>Critères suggérés basés sur :</strong>
                    <ul id="suggestionsList" class="mb-0 mt-2"></ul>
                </div>
            </div>

            <!-- Critère Qualiopi -->
            <div class="mb-3">
                <label class="form-label">🎯 Critère Qualiopi *</label>
                <select name="indicateurId" id="critereSelect" class="form-select" required>
                    <option value="">-- Sélectionner un critère --</option>
                    @foreach (var indicateur in ViewBag.Indicateurs)
                    {
                        <option value="@indicateur.Id" 
                                data-critere="@indicateur.Critere"
                                data-description="@indicateur.Description">
                            Critère @indicateur.Critere - @indicateur.Libelle
                        </option>
                    }
                </select>
                
                <!-- Description du critère sélectionné -->
                <div id="critereDescription" class="mt-2" style="display: none;">
                    <div class="alert alert-light border">
                        <strong>Description :</strong>
                        <p id="critereDescriptionText" class="mb-0"></p>
                    </div>
                </div>

                <!-- Aide contextuelle -->
                <div class="mt-2">
                    <button type="button" class="btn btn-sm btn-outline-info" 
                            data-bs-toggle="collapse" data-bs-target="#critereHelp">
                        ℹ️ Aide : Comment choisir le bon critère ?
                    </button>
                    <div id="critereHelp" class="collapse mt-2">
                        <div class="card card-body bg-light">
                            <ul class="mb-0">
                                <li><strong>Critère 1</strong> : Information du public (programmes, fiches descriptives)</li>
                                <li><strong>Critère 2</strong> : Objectifs de la prestation (conventions, contrats)</li>
                                <li><strong>Critère 3</strong> : Conditions de déroulement (émargements, planning)</li>
                                <li><strong>Critère 4</strong> : Analyse du besoin (prérequis, positionnement)</li>
                                <li><strong>Critère 5</strong> : Moyens humains (CV formateurs, compétences)</li>
                                <li><strong>Critère 6</strong> : Contenus et modalités (programmes détaillés)</li>
                                <li><strong>Critère 7</strong> : Recueil des appréciations (évaluations, attestations)</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Titre -->
            <div class="mb-3">
                <label class="form-label">📝 Titre de la Preuve *</label>
                <input name="titre" id="titreInput" class="form-control" required />
                <small class="form-text text-muted">
                    Auto-complété depuis le nom du fichier si un document est uploadé
                </small>
            </div>

            <!-- Description -->
            <div class="mb-3">
                <label class="form-label">📄 Description (optionnel)</label>
                <textarea name="description" class="form-control" rows="3"></textarea>
            </div>

            <!-- Boutons -->
            <div class="d-flex gap-2">
                <button type="submit" class="btn btn-primary">✅ Créer la Preuve</button>
                <a asp-action="Preuves" class="btn btn-outline-secondary">❌ Annuler</a>
            </div>
        </form>
    </div>
</div>

<script>
// JavaScript pour le guidage intelligent
document.addEventListener('DOMContentLoaded', function() {
    const sessionSelect = document.getElementById('sessionSelect');
    const fileInput = document.getElementById('fileInput');
    const critereSelect = document.getElementById('critereSelect');
    const titreInput = document.getElementById('titreInput');
    const suggestionsDiv = document.getElementById('critereSuggestions');
    const suggestionsList = document.getElementById('suggestionsList');
    const critereDescription = document.getElementById('critereDescription');
    const critereDescriptionText = document.getElementById('critereDescriptionText');

    // Auto-complétion titre depuis nom fichier
    fileInput.addEventListener('change', function() {
        if (this.files.length > 0) {
            const fileName = this.files[0].name;
            const nameWithoutExt = fileName.replace(/\.[^/.]+$/, "");
            if (!titreInput.value) {
                titreInput.value = nameWithoutExt;
            }
            // Charger suggestions
            loadSuggestions();
        }
    });

    // Charger suggestions quand session ou fichier change
    sessionSelect.addEventListener('change', loadSuggestions);
    fileInput.addEventListener('change', loadSuggestions);

    // Afficher description du critère sélectionné
    critereSelect.addEventListener('change', function() {
        const selectedOption = this.options[this.selectedIndex];
        if (selectedOption.value) {
            const description = selectedOption.getAttribute('data-description');
            if (description) {
                critereDescriptionText.textContent = description;
                critereDescription.style.display = 'block';
            } else {
                critereDescription.style.display = 'none';
            }
        } else {
            critereDescription.style.display = 'none';
        }
    });

    // Fonction pour charger les suggestions
    async function loadSuggestions() {
        const sessionId = sessionSelect.value;
        const fileName = fileInput.files.length > 0 ? fileInput.files[0].name : null;

        if (!sessionId && !fileName) {
            suggestionsDiv.style.display = 'none';
            return;
        }

        try {
            const response = await fetch(`/QualiopiUi/GetCritereSuggestions?sessionId=${sessionId || ''}&fileName=${encodeURIComponent(fileName || '')}`);
            const suggestions = await response.json();

            if (suggestions.length > 0) {
                suggestionsList.innerHTML = '';
                suggestions.forEach(suggestion => {
                    const li = document.createElement('li');
                    const critereOption = Array.from(critereSelect.options)
                        .find(opt => opt.getAttribute('data-critere') == suggestion.critere);
                    
                    if (critereOption) {
                        const badgeClass = suggestion.isMissing ? 'bg-warning' : 'bg-info';
                        li.innerHTML = `
                            <strong>Critère ${suggestion.critere}</strong> 
                            <span class="badge ${badgeClass}">${Math.round(suggestion.confidence * 100)}%</span>
                            <br>
                            <small class="text-muted">${suggestion.reason}</small>
                            <button type="button" class="btn btn-sm btn-outline-primary ms-2" 
                                    onclick="selectCritere(${critereOption.value})">
                                Sélectionner
                            </button>
                        `;
                        suggestionsList.appendChild(li);
                    }
                });
                suggestionsDiv.style.display = 'block';
            } else {
                suggestionsDiv.style.display = 'none';
            }
        } catch (error) {
            console.error('Erreur lors du chargement des suggestions:', error);
        }
    }

    // Fonction pour sélectionner un critère suggéré
    window.selectCritere = function(indicateurId) {
        critereSelect.value = indicateurId;
        critereSelect.dispatchEvent(new Event('change'));
        // Scroll vers le select
        critereSelect.scrollIntoView({ behavior: 'smooth', block: 'center' });
    };
});
</script>
```

---

## 🔧 Contrôleur avec Endpoint Suggestions

```csharp
[HttpGet]
public async Task<IActionResult> GetCritereSuggestions(
    int? sessionId,
    string? fileName)
{
    var suggestions = await _critereSuggestionService.GetSuggestionsAsync(
        sessionId,
        fileName,
        null); // Type document déterminé côté serveur si nécessaire

    return Json(suggestions);
}
```

---

## 📊 Exemples de Suggestions

### Exemple 1 : Upload "programme_formation_excel.pdf"

**Suggestions affichées :**
```
💡 Suggestions de Critères

• Critère 6 - Contenus et modalités (80% de confiance)
  Raison : Le nom du fichier contient 'programme'
  [Sélectionner]

• Critère 1 - Information du public (70% de confiance)
  Raison : Le nom du fichier contient 'programme'
  [Sélectionner]
```

### Exemple 2 : Session avec critères manquants

**Suggestions affichées :**
```
💡 Suggestions de Critères

⚠️ Critère 3 - Conditions de déroulement (70% de confiance)
  Raison : ⚠️ Critère manquant pour cette session (aucune preuve validée)
  [Sélectionner]

• Critère 6 - Contenus et modalités (60% de confiance)
  Raison : Déjà utilisé 2 fois pour cette session (dernière fois le 15/01/2024)
  [Sélectionner]
```

### Exemple 3 : Upload "emargement_session_2024.pdf"

**Suggestions affichées :**
```
💡 Suggestions de Critères

• Critère 3 - Conditions de déroulement (95% de confiance)
  Raison : Le nom du fichier contient 'emargement' | Type de document : Emargement
  [Sélectionner]
```

---

## ✅ Avantages du Système

1. **Guidage sans imposition** : L'utilisateur garde le contrôle total
2. **Réduction des erreurs** : Suggestions basées sur des règles métier
3. **Aide contextuelle** : Descriptions et exemples pour chaque critère
4. **Détection des manques** : Alerte sur les critères non couverts
5. **Apprentissage** : L'utilisateur apprend en voyant les suggestions
6. **Flexibilité** : L'utilisateur peut toujours choisir un autre critère

---

## 🚀 Implémentation

### Étape 1 : Créer le Service
- [ ] Créer `ICritereSuggestionService` et `CritereSuggestionService`
- [ ] Implémenter les méthodes de suggestion
- [ ] Enregistrer dans `Program.cs`

### Étape 2 : Modifier le Contrôleur
- [ ] Ajouter endpoint `GetCritereSuggestions`
- [ ] Modifier `CreatePreuve` (GET) pour inclure suggestions

### Étape 3 : Améliorer la Vue
- [ ] Ajouter section suggestions
- [ ] Ajouter JavaScript pour chargement dynamique
- [ ] Ajouter descriptions contextuelles

### Étape 4 : Tests
- [ ] Tester avec différents noms de fichiers
- [ ] Tester avec différentes sessions
- [ ] Valider avec utilisateurs

---

**Ce système guide l'utilisateur sans imposer, réduisant les erreurs tout en gardant la flexibilité !** 🎯
