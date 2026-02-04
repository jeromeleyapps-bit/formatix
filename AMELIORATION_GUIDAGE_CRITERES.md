# Amélioration : Guidage Intelligent pour la Sélection des Critères

## 🎯 Objectif

Ajouter un **système de guidage intelligent** pour aider l'utilisateur à choisir le bon critère Qualiopi, **sans imposer** de choix.

---

## ✨ Fonctionnalités du Guidage

### 1. Suggestions Basées sur le Nom de Fichier

**Exemples :**
- `programme_formation.pdf` → Suggère **Critère 6** (Contenus) et **Critère 1** (Information)
- `emargement_session.pdf` → Suggère **Critère 3** (Conditions de déroulement)
- `convention_formation.pdf` → Suggère **Critère 2** (Objectifs)
- `evaluation_stagiaires.pdf` → Suggère **Critère 7** (Recueil des appréciations)
- `cv_formateur.pdf` → Suggère **Critère 5** (Moyens humains)

### 2. Suggestions Basées sur le Type de Document

**Mapping automatique :**
- **Programme** → Critère 6 (Contenus)
- **Convention** → Critère 2 (Objectifs)
- **Émargement** → Critère 3 (Conditions)
- **Attestation** → Critère 7 (Appréciations)
- **Évaluation** → Critère 7 (Appréciations)

### 3. Détection des Critères Manquants

**Alerte visuelle :** Si un critère n'a pas encore de preuve validée pour la session sélectionnée, il est suggéré avec un badge "⚠️ Manquant".

### 4. Historique des Preuves

**Suggestion basée sur l'usage :** Si un critère a déjà été utilisé pour cette session, il est suggéré avec la mention "Déjà utilisé X fois".

### 5. Descriptions Contextuelles

**Aide en temps réel :** Quand l'utilisateur sélectionne un critère, une description détaillée s'affiche automatiquement.

---

## 🎨 Interface Utilisateur

### Affichage des Suggestions

```
┌─────────────────────────────────────────────────────────┐
│  💡 Suggestions de Critères                              │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  ⚠️ Critère 3 - Conditions de déroulement (70%)          │
│     Raison : ⚠️ Critère manquant pour cette session      │
│     [Sélectionner]                                       │
│                                                           │
│  • Critère 6 - Contenus et modalités (80%)              │
│     Raison : Le nom du fichier contient 'programme'      │
│     [Sélectionner]                                       │
│                                                           │
│  • Critère 1 - Information du public (70%)               │
│     Raison : Le nom du fichier contient 'programme'      │
│     [Sélectionner]                                       │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

### Description Contextuelle

```
┌─────────────────────────────────────────────────────────┐
│  🎯 Critère Qualiopi *                                   │
│  [Critère 6 - Contenus et modalités ▼]                  │
│                                                           │
│  ┌───────────────────────────────────────────────────┐ │
│  │ Description :                                      │ │
│  │ Les contenus de formation et les modalités        │ │
│  │ pédagogiques doivent être clairement définis et    │ │
│  │ communiqués aux stagiaires.                        │ │
│  └───────────────────────────────────────────────────┘ │
│                                                           │
│  [ℹ️ Aide : Comment choisir le bon critère ?]           │
└─────────────────────────────────────────────────────────┘
```

---

## 🔧 Implémentation Technique

### Nouveau Service

**Fichier :** `Services/CritereSuggestionService.cs`

```csharp
public interface ICritereSuggestionService
{
    Task<List<CritereSuggestion>> GetSuggestionsAsync(
        int? sessionId,
        string? fileName,
        TypeDocument? documentType);
}
```

### Nouveau Endpoint

**Contrôleur :** `QualiopiUiController`

```csharp
[HttpGet]
public async Task<IActionResult> GetCritereSuggestions(
    int? sessionId,
    string? fileName)
{
    var suggestions = await _critereSuggestionService.GetSuggestionsAsync(
        sessionId,
        fileName,
        null);
    return Json(suggestions);
}
```

### JavaScript Dynamique

**Fichier :** `Views/QualiopiUi/CreatePreuve.cshtml`

- Chargement automatique des suggestions quand :
  - Une session est sélectionnée
  - Un fichier est uploadé
- Affichage des suggestions avec boutons "Sélectionner"
- Description contextuelle au changement de critère

---

## ✅ Avantages

1. **Réduction des erreurs** : -50% d'erreurs de sélection de critère
2. **Gain de temps** : -30% de temps de réflexion
3. **Apprentissage** : L'utilisateur apprend en voyant les suggestions
4. **Détection des manques** : Alerte sur les critères non couverts
5. **Flexibilité** : L'utilisateur garde toujours le contrôle

---

## 📊 Métriques Attendues

- **Taux d'adoption des suggestions** : >60%
- **Réduction des erreurs** : -50%
- **Temps de sélection** : -30%
- **Satisfaction utilisateur** : +20%

---

## 🚀 Intégration dans le Plan de Simplification

### Modifications au Plan

**Phase 3.2 : QualiopiController**
- ✅ Ajouter endpoint `GetCritereSuggestions`
- ✅ Modifier `CreatePreuve` (GET) pour inclure suggestions

**Phase 3.3 : Nouveau Service**
- ✅ Créer `CritereSuggestionService`
- ✅ Implémenter toutes les stratégies de guidage
- ✅ Enregistrer dans `Program.cs`

**Phase 4.1 : CreatePreuve.cshtml**
- ✅ Section suggestions (affichage dynamique)
- ✅ Descriptions contextuelles
- ✅ Aide "Comment choisir le bon critère ?"
- ✅ JavaScript pour chargement dynamique

**Phase 5.3 : Tests**
- ✅ Tests unitaires `CritereSuggestionService`
- ✅ Tests d'intégration suggestions
- ✅ Tests utilisateurs

---

## 📝 Exemple d'Utilisation

### Scénario : Upload "programme_excel_2024.pdf"

1. **Utilisateur sélectionne la session** "Formation Excel - 2024-01"
2. **Utilisateur upload le fichier** "programme_excel_2024.pdf"
3. **Suggestions affichées automatiquement :**
   ```
   💡 Suggestions de Critères
   
   • Critère 6 - Contenus (80%)
     Raison : Le nom du fichier contient 'programme'
     [Sélectionner]
   
   • Critère 1 - Information du public (70%)
     Raison : Le nom du fichier contient 'programme'
     [Sélectionner]
   ```
4. **Utilisateur clique sur "Sélectionner" pour Critère 6**
5. **Le critère est pré-sélectionné automatiquement**
6. **Description du critère s'affiche**
7. **Utilisateur valide ou modifie si nécessaire**

**Temps total : <15 secondes** (vs 30 secondes sans guidage)

---

## 🎓 Formation Utilisateurs

### Message à Communiquer

> "L'application vous **suggère** automatiquement les critères les plus pertinents basés sur :
> - Le nom de votre fichier
> - Le type de document
> - Les critères déjà utilisés pour cette session
> - Les critères manquants
> 
> **Vous gardez toujours le contrôle** : vous pouvez accepter la suggestion ou choisir un autre critère."

---

**Ce système guide intelligemment sans imposer, réduisant les erreurs tout en gardant la simplicité !** 🎯
