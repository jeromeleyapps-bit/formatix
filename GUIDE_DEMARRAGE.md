# Guide de Démarrage Rapide - FormatiX

## Installation

### 1. Prérequis

- **.NET 9 SDK** : https://dotnet.microsoft.com/download/dotnet/9.0
- **Ollama** (optionnel pour IA) : https://ollama.ai/download
- **Tesseract** (optionnel pour OCR) : Fichiers `.traineddata` dans `tessdata/`

### 2. Installation des packages

```bash
# Restaurer tous les packages NuGet
dotnet restore

# Vérifier que tout compile
dotnet build
```

### 3. Configuration

Éditer `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=opagax.db"
  },
  "Sync": {
    "CentralUrl": "https://localhost:5001",
    "ApiKey": "CHANGEZ_CETTE_CLE_SECRETE_EN_PRODUCTION",
    "SiteId": "SITE_01"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "mistral"
  }
}
```

### 4. Configuration Ollama (pour l'IA)

```bash
# Installer Ollama depuis https://ollama.ai/download

# Télécharger un modèle
ollama pull mistral

# Vérifier que Ollama fonctionne
curl http://localhost:11434/api/tags
```

### 5. Configuration Tesseract (pour l'OCR)

1. Créer le dossier `tessdata/` à la racine du projet
2. Télécharger les fichiers `.traineddata` :
   - `fra.traineddata` (français) : https://github.com/tesseract-ocr/tessdata
   - `eng.traineddata` (anglais)
3. Placer les fichiers dans `tessdata/`

### 6. Base de données

```bash
# Appliquer les migrations
dotnet ef database update

# Ou créer la base manuellement (si nécessaire)
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 7. Lancer l'application

```bash
dotnet run
```

L'application sera accessible sur :
- **Web** : https://localhost:5001 ou http://localhost:5000
- **Swagger** : https://localhost:5001/swagger
- **Health Checks** : https://localhost:5001/health

## Utilisation

### 1. Test de l'API

#### Health Check

```bash
curl https://localhost:5001/health
```

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

#### Test Synchronisation

```bash
curl https://localhost:5001/api/sync/test-connection
```

#### Upload Document avec OCR + IA

```bash
curl -X POST https://localhost:5001/api/documents/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@document.pdf" \
  -F "sessionId=1"
```

#### Analyse Feuille d'Émargement

```bash
curl -X POST https://localhost:5001/api/documents/analyze-emargement \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@emargement.pdf" \
  -F "sessionId=1"
```

### 2. Synchronisation

#### Upload vers Central

```bash
curl -X POST https://localhost:5001/api/sync/upload \
  -H "X-Site-Id: SITE_01" \
  -H "X-API-Key: YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "entities": [],
    "metadata": {}
  }'
```

#### Download depuis Central

```bash
curl -X GET "https://localhost:5001/api/sync/download?siteId=SITE_01" \
  -H "X-API-Key: YOUR_API_KEY"
```

### 3. Logs

Les logs sont disponibles dans :
- **Console** : Temps réel en développement
- **Fichiers** : `logs/app-YYYY-MM-DD.log`

Exemple :
```bash
tail -f logs/app-2024-01-01.log
```

### 4. Swagger UI

Accéder à Swagger :
- URL : https://localhost:5001/swagger
- Tester tous les endpoints directement
- Documentation interactive complète

## Tests

### Exécuter les tests

```bash
# Tous les tests
dotnet test

# Tests unitaires uniquement
dotnet test --filter Category=Unit

# Tests avec détails
dotnet test --logger "console;verbosity=detailed"
```

### Couverture de code

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Débogage

### 1. Vérifier les logs

```bash
# Logs en temps réel
tail -f logs/app-*.log

# Rechercher des erreurs
grep -i error logs/app-*.log
```

### 2. Health Checks

```bash
# Vérifier la santé de l'application
curl https://localhost:5001/health | jq
```

### 3. Vérifier Ollama

```bash
# Tester Ollama
curl http://localhost:11434/api/tags

# Tester génération
curl http://localhost:11434/api/generate -d '{
  "model": "mistral",
  "prompt": "Bonjour",
  "stream": false
}'
```

### 4. Vérifier la base de données

```bash
# Ouvrir la base SQLite
sqlite3 opagax.db

# Lister les tables
.tables

# Vérifier les données
SELECT * FROM Formations;
```

## Production

### 1. Configuration Production

Dans `appsettings.Production.json` :

```json
{
  "Sync": {
    "CentralUrl": "https://central.votre-domaine.com",
    "ApiKey": "CLE_SECRETE_PRODUCTION"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=opagax_prod.db"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"
    }
  }
}
```

### 2. Variables d'environnement

```bash
export ASPNETCORE_ENVIRONMENT=Production
export Sync__ApiKey=PRODUCTION_KEY
export ConnectionStrings__DefaultConnection=Data Source=opagax_prod.db
```

### 3. HTTPS

Configurer HTTPS avec certificat SSL valide.

### 4. Sécurité

- **Changer la clé API** : Modifier `Sync:ApiKey` dans `appsettings.json`
- **HTTPS obligatoire** : En production, forcer HTTPS
- **Logs sécurisés** : Ne pas logger de données sensibles
- **Authentification** : Utiliser des tokens JWT pour l'API

## Problèmes Courants

### Ollama n'est pas disponible

```bash
# Vérifier que Ollama est démarré
ollama serve

# Vérifier les modèles disponibles
ollama list

# Télécharger mistral si nécessaire
ollama pull mistral
```

### Tesseract erreurs

- Vérifier que les fichiers `.traineddata` sont dans `tessdata/`
- Vérifier les permissions d'accès au dossier
- Vérifier la configuration dans `appsettings.json`

### Erreurs de synchronisation

- Vérifier la clé API : `Sync:ApiKey`
- Vérifier l'URL centrale : `Sync:CentralUrl`
- Vérifier la connectivité réseau
- Consulter les logs : `logs/app-*.log`

### Erreurs de base de données

```bash
# Réinitialiser la base
rm opagax.db
dotnet ef database update
```

## Support

- **Documentation** : Voir `ARCHITECTURE_COMPLETE.md`
- **API** : Swagger UI sur `/swagger`
- **Logs** : `logs/app-*.log`
- **Health Checks** : `/health`

## Prochaines Étapes

1. Configurer Ollama et télécharger un modèle
2. Placer les fichiers Tesseract dans `tessdata/`
3. Configurer la clé API de synchronisation
4. Tester l'upload d'un document avec OCR + IA
5. Tester la synchronisation avec un autre site