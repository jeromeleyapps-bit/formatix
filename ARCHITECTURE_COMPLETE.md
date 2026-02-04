# Architecture Complète FormatiX - Documentation Technique

## Vue d'ensemble

FormatiX est une application de gestion de formations certifiée Qualiopi avec synchronisation décentralisée, OCR et analyse IA.

## Structure du Projet

```
FormatiX/
├── Controllers/
│   ├── Sync/              # Contrôleurs de synchronisation
│   ├── Documents/         # Contrôleurs documents (OCR, IA)
│   └── ...                # Contrôleurs existants
├── Infrastructure/
│   ├── Exceptions/        # Gestion globale des erreurs
│   ├── OCR/               # Service OCR Tesseract
│   ├── AI/                # Service IA Ollama
│   ├── Sync/              # Service de synchronisation
│   └── HealthChecks/      # Health checks personnalisés
├── Services/              # Services métier existants
├── Models/                # Modèles de données
├── Data/                  # Context EF Core
├── Views/                 # Vues Razor
└── FormationManager.Tests/ # Tests unitaires et intégration
```

## Composants Principaux

### 1. Gestion des Erreurs (Infrastructure/Exceptions)

- **GlobalExceptionHandler** : Handler global pour toutes les exceptions
- **BusinessException** : Exception métier personnalisée
- **SyncException** : Exception de synchronisation
- **OCRException** : Exception OCR
- **AIException** : Exception IA

### 2. Service OCR (Infrastructure/OCR)

**TesseractOCRService** :
- Extraction de texte depuis PDF
- Extraction données émargement (noms, dates, signatures)
- Validation qualité OCR
- Support multi-langues (français par défaut)

**Configuration** :
```json
"Tesseract": {
  "DataPath": "./tessdata",
  "Language": "fra"
}
```

**Dépendance** : Nécessite les fichiers `.traineddata` dans le dossier `tessdata` :
- `fra.traineddata` (français)
- `eng.traineddata` (anglais)

### 3. Service IA (Infrastructure/AI)

**OllamaAIService** :
- Analyse de documents avec Ollama/Mistral
- Classification Qualiopi automatique
- Extraction de mots-clés
- Support multi-modèles (mistral, llama2, etc.)

**Configuration** :
```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "mistral",
  "TimeoutSeconds": 120
}
```

**Installation Ollama** :
1. Télécharger depuis https://ollama.ai/download
2. Installer et démarrer Ollama
3. Télécharger un modèle : `ollama pull mistral`

### 4. Service de Synchronisation (Infrastructure/Sync)

**SyncService** :
- Synchronisation bidirectionnelle (Upload/Download)
- Retry automatique avec exponential backoff
- Authentification par clé API
- Gestion des conflits

**Configuration** :
```json
"Sync": {
  "CentralUrl": "https://localhost:5001",
  "ApiKey": "CHANGEZ_CETTE_CLE_SECRETE",
  "IntervalMinutes": 15,
  "RetryAttempts": 3,
  "SiteId": "SITE_01"
}
```

**Endpoints API** :
- `POST /api/sync/upload` : Upload données vers central
- `GET /api/sync/download` : Download données depuis central
- `GET /api/sync/status` : Statut de synchronisation
- `GET /api/sync/test-connection` : Test connexion

### 5. Health Checks (Infrastructure/HealthChecks)

**OllamaHealthCheck** :
- Vérification disponibilité Ollama
- Monitoring santé de l'IA

**Endpoint** : `GET /health`

### 6. Contrôleurs API

#### SyncController
- Gestion synchronisation sites ↔ central
- Authentification par clé API
- Logging complet

#### DocumentsController
- Upload et analyse de documents PDF
- OCR automatique
- Analyse IA
- Extraction données émargement

**Endpoints** :
- `POST /api/documents/upload` : Upload + OCR + IA
- `POST /api/documents/analyze-emargement` : Analyse feuille émargement
- `POST /api/documents/extract-ocr` : Extraction OCR simple

### 7. Logging (Serilog)

**Configuration** :
- Console : logging en temps réel
- Fichiers : logs rotatifs quotidiens (30 jours de rétention)
- Enrichissement : MachineName, ThreadId, EnvironmentName

**Fichiers de logs** : `logs/app-YYYY-MM-DD.log`

**Niveaux** :
- DEBUG : Détails développement
- INFORMATION : Événements importants
- WARNING : Problèmes potentiels
- ERROR : Erreurs avec stack trace
- FATAL : Erreurs critiques

### 8. Configuration (appsettings.json)

**Sections principales** :
- `ConnectionStrings` : Base de données
- `Ollama` : Configuration IA
- `Sync` : Configuration synchronisation
- `Tesseract` : Configuration OCR
- `Serilog` : Configuration logging
- `Qualiopi` : Configuration certification

## Tests

### Structure des tests

```
FormationManager.Tests/
├── Unit/                  # Tests unitaires
│   ├── OCRServiceTests.cs
│   └── SyncServiceTests.cs
└── Integration/           # Tests d'intégration
    └── SyncIntegrationTests.cs
```

### Exécution

```bash
# Tous les tests
dotnet test

# Tests unitaires uniquement
dotnet test --filter Category=Unit

# Tests avec couverture
dotnet test /p:CollectCoverage=true
```

## Déploiement

### Prérequis

1. **.NET 9 SDK** installé
2. **Tesseract** : Fichiers `.traineddata` dans `tessdata/`
3. **Ollama** (optionnel) : Pour l'analyse IA
4. **PostgreSQL** (optionnel) : Pour production multi-sites

### Installation locale

```bash
# Restaurer les packages
dotnet restore

# Construire
dotnet build

# Appliquer migrations
dotnet ef database update

# Lancer
dotnet run
```

### Déploiement Windows

```bash
# Publier auto-contenu
dotnet publish -c Release -r win-x64 --self-contained

# L'application sera dans bin/Release/net9.0/win-x64/publish/
```

## Développement

### Ajout d'un nouveau service

1. Créer l'interface dans `Infrastructure/XXX/`
2. Implémenter le service
3. Enregistrer dans `Program.cs` : `builder.Services.AddScoped<IXXXService, XXXService>();`
4. Créer les tests unitaires

### Ajout d'un endpoint API

1. Créer le contrôleur dans `Controllers/`
2. Ajouter `[ApiController]` et `[Route]`
3. Documenter avec Swagger
4. Gérer les erreurs avec exceptions personnalisées

### Logging

```csharp
_logger.LogInformation("Message informatif");
_logger.LogWarning("Avertissement");
_logger.LogError(exception, "Erreur : {Message}", message);
_logger.LogDebug("Détails debug");
```

## Monitoring et Débogage

### Health Checks

Endpoint : `GET /health`

Réponse :
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T12:00:00Z",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 5.2
    },
    {
      "name": "ollama",
      "status": "Healthy",
      "duration": 10.5
    }
  ]
}
```

### Swagger UI

URL : `https://localhost:5001/swagger`

Documentation interactive de l'API.

### Logs

- **Console** : Temps réel en développement
- **Fichiers** : `logs/app-YYYY-MM-DD.log`
- **Niveaux** : Configurable dans `appsettings.json`

## Sécurité

### Authentification

- ASP.NET Core Identity
- Cookies sécurisés
- Sessions de 8 heures

### Synchronisation

- Clé API dans headers : `X-API-Key`
- Identification site : `X-Site-Id`
- HTTPS recommandé en production

### Validation

- Validation des entrées
- Gestion des erreurs centralisée
- Logging sécurisé (pas de données sensibles)

## Prochaines étapes

### À implémenter

1. **Conversion PDF → Images** : Intégrer PdfSharp ou iTextSharp
2. **Dashboard Qualiopi** : Vue consolidée des 7 critères
3. **Workflow audit** : Préparation automatique d'audit
4. **Export pack audit** : Génération ZIP avec toutes les preuves
5. **Notifications** : Alertes automatiques (preuves expirées, etc.)

### Améliorations possibles

1. **Caching** : Redis pour performance
2. **Queue** : Background jobs pour synchronisation
3. **Multi-tenancy** : Support multi-organisations
4. **API GraphQL** : Alternative à REST
5. **SignalR** : Notifications en temps réel

## Support

Pour toute question ou problème :
1. Vérifier les logs : `logs/app-*.log`
2. Consulter Swagger : `/swagger`
3. Vérifier health checks : `/health`
4. Consulter la documentation code