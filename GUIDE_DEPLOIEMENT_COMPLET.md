# Guide de Déploiement Complet - FormatiX

Ce guide explique comment déployer FormatiX sur un nouvel ordinateur Windows, en incluant tous les prérequis et dépendances nécessaires.

## 📋 Table des Matières

1. [Prérequis Système](#prérequis-système)
2. [Installation des Programmes Tiers](#installation-des-programmes-tiers)
3. [Configuration de l'Application](#configuration-de-lapplication)
4. [Déploiement](#déploiement)
5. [Vérification](#vérification)
6. [Dépannage](#dépannage)

---

## 🖥️ Prérequis Système

### Système d'Exploitation
- **Windows 10/11** (64-bit)
- Connexion Internet pour le téléchargement des dépendances

### .NET SDK
- **.NET 9.0 SDK** (ou version supérieure)
- Téléchargement : https://dotnet.microsoft.com/download/dotnet/9.0
- Vérification : `dotnet --version` (doit afficher 9.0.x ou supérieur)

---

## 📦 Installation des Programmes Tiers

### 1. Tesseract OCR

**Nécessaire pour** : Extraction de texte depuis les documents PDF scannés

#### Installation
1. Télécharger depuis : https://github.com/UB-Mannheim/tesseract/wiki
2. Installer la version **Windows 64-bit** (ex: `tesseract-ocr-w64-setup-5.x.x.exe`)
3. **IMPORTANT** : Noter le chemin d'installation (par défaut : `C:\Program Files\Tesseract-OCR\`)

#### Fichiers de Langue
1. Télécharger `fra.traineddata` (français) depuis : https://github.com/tesseract-ocr/tessdata
2. Copier le fichier dans le dossier `tessdata` de l'application :
   - Chemin : `[REPERTOIRE_FORMATIX]\tessdata\fra.traineddata`
   - Si le dossier n'existe pas, le créer

#### Vérification
```powershell
tesseract --version
tesseract --list-langs
```
Doit afficher la version et la liste des langues (incluant `fra`)

---

### 2. Ghostscript

**Nécessaire pour** : Conversion PDF → Images pour l'OCR

#### Installation Automatique (Recommandé)
1. Dans le répertoire FormatiX, exécuter `install-ghostscript.bat` **en tant qu'administrateur**
2. Le script télécharge et installe automatiquement Ghostscript

#### Installation Manuelle
1. Télécharger depuis : https://github.com/ArtifexSoftware/ghostpdl-downloads/releases
2. Installer `gs10032w64.exe` (ou version plus récente)
3. Par défaut installé dans : `C:\Program Files\gs\gs10.03.2\bin\`

#### Vérification
```powershell
gswin64c --version
```

---

### 3. ImageMagick

**Nécessaire pour** : Conversion PDF → Images (fallback si Ghostscript échoue)

#### Installation
1. Télécharger depuis : https://imagemagick.org/script/download.php#windows
2. Installer la version **64-bit Q16-HDRI** (ex: `ImageMagick-7.1.2-Q16-HDRI-x64-dll.exe`)
3. **IMPORTANT** : Cocher "Install development headers and libraries for C and C++" (optionnel mais recommandé)
4. Par défaut installé dans : `C:\Program Files\ImageMagick-7.1.2-Q16-HDRI\`

#### Vérification
```powershell
magick -version
```

---

### 4. Ollama (Optionnel mais Recommandé)

**Nécessaire pour** : Analyse IA des documents et identification automatique des critères Qualiopi

#### Installation
1. Télécharger depuis : https://ollama.ai/download
2. Installer `OllamaSetup.exe`
3. Démarrer Ollama (il doit être en cours d'exécution pour que l'analyse IA fonctionne)
4. Télécharger un modèle (ex: `ollama pull llama3.2` ou `ollama pull mistral`)

#### Vérification
```powershell
ollama list
```

---

## ⚙️ Configuration de l'Application

### 1. Copier les Fichiers de l'Application

Copier tout le répertoire FormatiX sur le nouvel ordinateur :
```
C:\AI\Opagax\
├── appsettings.json
├── FormationManager.csproj
├── Program.cs
├── Controllers\
├── Models\
├── Views\
├── Infrastructure\
├── Services\
├── Data\
├── wwwroot\
├── tessdata\          ← IMPORTANT : Inclure ce dossier avec fra.traineddata
└── ...
```

### 2. Vérifier appsettings.json

Ouvrir `appsettings.json` et vérifier/corriger :

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=opagax.db"
  },
  "CreateDemoData": false,
  "TesseractDataPath": "tessdata",
  "OllamaApiUrl": "http://localhost:11434"
}
```

### 3. Créer les Dossiers Nécessaires

```powershell
# Dans le répertoire de l'application
New-Item -ItemType Directory -Path "logs" -Force
New-Item -ItemType Directory -Path "wwwroot\uploads" -Force
New-Item -ItemType Directory -Path "wwwroot\examples" -Force
New-Item -ItemType Directory -Path "tessdata" -Force
```

### 4. Vérifier le Fichier de Langue Tesseract

S'assurer que `tessdata\fra.traineddata` existe :
```powershell
Test-Path "tessdata\fra.traineddata"
```

Si absent, télécharger depuis : https://github.com/tesseract-ocr/tessdata/raw/main/fra.traineddata

---

## 🚀 Déploiement

### 1. Restaurer les Dépendances NuGet

```powershell
cd C:\AI\Opagax
dotnet restore
```

### 2. Créer la Base de Données

```powershell
dotnet ef database update
```

### 3. Compiler l'Application

```powershell
dotnet build --configuration Release
```

### 4. Démarrer l'Application

#### Mode Développement
```powershell
dotnet run
```

#### Mode Production (Recommandé)
```powershell
dotnet run --configuration Release
```

L'application sera accessible sur : `http://localhost:5000`

---

## ✅ Vérification

### 1. Vérifier que l'Application Démarre

- Ouvrir un navigateur : `http://localhost:5000`
- La page de connexion doit s'afficher

### 2. Créer un Compte Administrateur

Voir le guide : `GUIDE_CREATION_ADMIN.md`

### 3. Tester l'OCR

1. Se connecter en tant qu'administrateur
2. Aller dans **Documents** → **Importer un document**
3. Uploader un PDF scanné
4. Vérifier les logs pour confirmer que :
   - Ghostscript/ImageMagick convertit le PDF en images
   - Tesseract extrait le texte
   - L'analyse IA fonctionne (si Ollama est installé)

### 4. Vérifier les Logs

```powershell
Get-Content logs\*.log -Tail 50
```

Rechercher :
- ✅ "Ghostscript trouvé" ou "ImageMagick trouvé"
- ✅ "Tesseract CLI trouvé"
- ✅ "Extraction OCR terminée : X caractères extraits"
- ✅ "Analyse IA terminée" (si Ollama est installé)

---

## 🔧 Dépannage

### Problème : OCR retourne 0 caractères

**Causes possibles :**
1. Ghostscript non installé ou non trouvé
   - **Solution** : Installer Ghostscript et redémarrer l'application
2. ImageMagick non installé (fallback)
   - **Solution** : Installer ImageMagick
3. Fichier de langue Tesseract manquant
   - **Solution** : Vérifier que `tessdata\fra.traineddata` existe

**Vérification :**
```powershell
# Vérifier Ghostscript
gswin64c --version

# Vérifier ImageMagick
magick -version

# Vérifier Tesseract
tesseract --list-langs
```

### Problème : Analyse IA ne fonctionne pas

**Causes possibles :**
1. Ollama non installé
   - **Solution** : Installer Ollama et démarrer le service
2. Ollama non démarré
   - **Solution** : Démarrer Ollama (il doit être en cours d'exécution)
3. Modèle non téléchargé
   - **Solution** : `ollama pull llama3.2`

**Vérification :**
```powershell
# Vérifier que Ollama est en cours d'exécution
Get-Process ollama -ErrorAction SilentlyContinue

# Vérifier les modèles disponibles
ollama list
```

### Problème : Erreur "pdfium.dll not found"

**Cause :** PdfiumViewer nécessite des DLL natives qui ne sont pas toujours disponibles

**Solution :** C'est normal, l'application utilise automatiquement Ghostscript/ImageMagick en fallback

### Problème : Port 5000 déjà utilisé

**Solution :**
```powershell
# Trouver le processus utilisant le port
netstat -ano | findstr :5000

# Arrêter le processus (remplacer PID par le numéro trouvé)
taskkill /PID [PID] /F
```

Ou modifier le port dans `appsettings.json` :
```json
{
  "Urls": "http://localhost:5001"
}
```

---

## 📝 Checklist de Déploiement

- [ ] .NET 9.0 SDK installé
- [ ] Tesseract OCR installé
- [ ] Fichier `fra.traineddata` dans `tessdata\`
- [ ] Ghostscript installé
- [ ] ImageMagick installé (optionnel mais recommandé)
- [ ] Ollama installé et démarré (optionnel mais recommandé)
- [ ] Fichiers de l'application copiés
- [ ] `appsettings.json` configuré
- [ ] Dossiers `logs`, `wwwroot\uploads`, `wwwroot\examples` créés
- [ ] `dotnet restore` exécuté
- [ ] `dotnet ef database update` exécuté
- [ ] Application démarrée et accessible
- [ ] Compte administrateur créé
- [ ] Test OCR réussi
- [ ] Test analyse IA réussi (si Ollama installé)

---

## 🔗 Liens Utiles

- **Tesseract OCR** : https://github.com/UB-Mannheim/tesseract/wiki
- **Fichiers de langue Tesseract** : https://github.com/tesseract-ocr/tessdata
- **Ghostscript** : https://github.com/ArtifexSoftware/ghostpdl-downloads/releases
- **ImageMagick** : https://imagemagick.org/script/download.php#windows
- **Ollama** : https://ollama.ai/download
- **.NET SDK** : https://dotnet.microsoft.com/download/dotnet/9.0

---

## 📌 Notes Importantes

1. **Ordre d'Installation** : Installer d'abord Tesseract, puis Ghostscript, puis ImageMagick, puis Ollama
2. **Redémarrage** : Après l'installation de chaque programme, redémarrer l'application FormatiX
3. **Permissions** : Certaines installations nécessitent des droits administrateur
4. **PATH** : Les programmes doivent être dans le PATH système ou l'application les détectera automatiquement dans les emplacements standards
5. **Fichiers de Données** : Ne pas oublier de copier le dossier `tessdata\` avec `fra.traineddata`

---

## 🎯 Déploiement Multi-Sites

Pour déployer sur plusieurs sites distants :

1. **Site Central (Administrateur)** :
   - Installer tous les programmes tiers
   - Configurer `appsettings.json` avec `CreateDemoData: false`
   - Créer un compte administrateur
   - Configurer la synchronisation (voir `GUIDE_DEPLOIEMENT.md`)

2. **Sites Distants (Formateurs)** :
   - Installer Tesseract, Ghostscript, ImageMagick (Ollama optionnel)
   - Copier les fichiers de l'application
   - Configurer l'URL du serveur central dans `appsettings.json`
   - L'application synchronisera automatiquement avec le serveur central

---

**Dernière mise à jour** : 2026-01-23
