# Guide d'Installation FormatiX

## Étapes d'Installation

### 1. Prérequis

✅ **.NET 9 SDK** installé
✅ **Visual Studio 2022** ou **VS Code** (optionnel)
✅ **Ollama** pour l'IA (optionnel mais recommandé)
✅ **Fichiers Tesseract** pour l'OCR (optionnel)

### 2. Restaurer les packages NuGet

```bash
dotnet restore
```

### 3. Configuration de l'Organisation

Éditer `appsettings.json` et modifier la section `AppSettings` :

```json
"AppSettings": {
  "NomOrganisme": "VOTRE NOM D'ORGANISME",
  "SIRET": "VOTRE_SIRET",
  "Adresse": "Votre adresse",
  "CodePostal": "Code postal",
  "Ville": "Votre ville",
  "Email": "contact@votre-organisme.fr",
  "Telephone": "Votre téléphone"
}
```

**Note** : Le nom de l'organisation sera visible dans toute l'application FormatiX.

### 4. Configuration Tesseract (OCR)

1. Les dossiers `tessdata/` et `logs/` ont été créés automatiquement
2. Télécharger les fichiers `.traineddata` :
   - **Français** : [fra.traineddata](https://github.com/tesseract-ocr/tessdata/blob/main/fra.traineddata)
   - **Anglais** : [eng.traineddata](https://github.com/tesseract-ocr/tessdata/blob/main/eng.traineddata)
3. Placer les fichiers dans le dossier `tessdata/`

### 5. Configuration Ollama (IA)

#### Installation Ollama

**Windows** :
1. Télécharger depuis : https://ollama.ai/download
2. Installer et démarrer Ollama
3. Vérifier : `ollama --version`

**Linux/Mac** :
```bash
curl -fsSL https://ollama.ai/install.sh | sh
```

#### Télécharger un modèle

```bash
# Modèle Mistral (recommandé, ~7B)
ollama pull mistral

# Ou LLaMA 2 (alternative)
ollama pull llama2

# Vérifier les modèles installés
ollama list
```

#### Vérifier que Ollama fonctionne

```bash
# Test simple
curl http://localhost:11434/api/tags

# Test de génération
ollama run mistral "Bonjour, comment allez-vous ?"
```

### 6. Configuration de la Base de Données

```bash
# Créer la base de données
dotnet ef database update

# Si erreur, créer d'abord la migration
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 7. Configuration de la Synchronisation (optionnel)

Éditer `appsettings.json` :

```json
"Sync": {
  "CentralUrl": "https://votre-serveur-central.com",
  "ApiKey": "VOTRE_CLE_API_SECRETE",
  "IntervalMinutes": 15,
  "RetryAttempts": 3,
  "SiteId": "SITE_01"
}
```

### 8. Lancer l'Application

```bash
# En mode développement
dotnet run

# Ou en mode Release
dotnet run --configuration Release
```

L'application sera accessible sur :
- **Interface Web** : https://localhost:5001 ou http://localhost:5000
- **Swagger API** : https://localhost:5001/swagger
- **Health Checks** : https://localhost:5001/health

### 9. Comptes de Démonstration

Par défaut, l'application crée des comptes de test :

| Rôle | Email | Mot de passe |
|------|-------|--------------|
| Administrateur | admin@formationmanager.com | Admin123! |
| Responsable | responsable@formationmanager.com | Responsable123! |
| Formateur | formateur1@formationmanager.com | Formateur123! |

**⚠️ Important** : Changer ces mots de passe en production !

## Vérification de l'Installation

### 1. Health Checks

```bash
curl https://localhost:5001/health
```

Vérifier que :
- ✅ Database : Healthy
- ✅ Ollama : Healthy (si configuré)

### 2. Test Swagger

1. Ouvrir https://localhost:5001/swagger
2. Tester un endpoint simple
3. Vérifier que l'API répond

### 3. Test OCR (si Tesseract configuré)

```bash
# Upload un PDF via Swagger ou curl
curl -X POST https://localhost:5001/api/documents/extract-ocr \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@document.pdf"
```

### 4. Test IA (si Ollama configuré)

Le health check devrait indiquer Ollama comme "Healthy".

## Dépannage

### Ollama n'est pas disponible

```bash
# Vérifier que Ollama est démarré
ollama serve

# Vérifier les modèles
ollama list

# Télécharger mistral si nécessaire
ollama pull mistral
```

### Erreurs Tesseract

- Vérifier que `tessdata/fra.traineddata` existe
- Vérifier les permissions du dossier
- Consulter les logs : `logs/app-*.log`

### Erreurs de base de données

```bash
# Réinitialiser la base
rm opagax.db
dotnet ef database update
```

### Logs

Les logs sont disponibles dans :
- **Console** : Temps réel en développement
- **Fichiers** : `logs/app-YYYY-MM-DD.log`

```bash
# Voir les logs en temps réel
tail -f logs/app-*.log

# Rechercher des erreurs
grep -i error logs/app-*.log
```

## Prochaines Étapes

1. ✅ Configurer le nom de votre organisation dans `appsettings.json`
2. ✅ Télécharger les fichiers Tesseract dans `tessdata/`
3. ✅ Installer et configurer Ollama
4. ✅ Lancer l'application
5. ✅ Tester l'upload d'un document avec OCR + IA
6. ✅ Configurer la synchronisation si nécessaire

## Support

- **Documentation** : Voir `ARCHITECTURE_COMPLETE.md`
- **Guide démarrage** : Voir `GUIDE_DEMARRAGE.md`
- **API** : Swagger UI sur `/swagger`
- **Logs** : `logs/app-*.log`

---

**FormatiX** - Solution complète pour la gestion de formations Qualiopi