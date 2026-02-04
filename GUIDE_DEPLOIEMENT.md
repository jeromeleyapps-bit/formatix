# Guide de Déploiement - Transport de l'Application

## 📦 Transport de l'Application vers un Autre PC

Ce guide explique comment transporter l'application FormatiX de votre PC de développement vers votre PC de travail.

---

## ✅ Prérequis sur le PC de Destination

### 1. .NET 9.0 SDK
- Télécharger depuis : https://dotnet.microsoft.com/download/dotnet/9.0
- Vérifier l'installation : `dotnet --version` (doit afficher 9.0.x)

### 2. (Optionnel) Tesseract OCR
- Si vous utilisez l'OCR, installer Tesseract
- Windows : Télécharger depuis https://github.com/UB-Mannheim/tesseract/wiki
- Ou utiliser le script `setup-tesseract.ps1` fourni

### 3. (Optionnel) Ollama AI
- Si vous utilisez l'analyse IA, installer Ollama
- Télécharger depuis : https://ollama.ai/download
- Installer et démarrer le service

---

## 📋 Méthode 1 : Transport Complet (Recommandé)

### Étape 1 : Préparer le Package sur le PC de Développement

1. **Créer un dossier de transport** (ex: `C:\FormatiX_Deploy`)

2. **Copier les fichiers essentiels** :
   ```
   FormatiX_Deploy/
   ├── FormationManager.csproj
   ├── Program.cs
   ├── appsettings.json
   ├── Controllers/
   ├── Data/
   ├── Models/
   ├── Services/
   ├── Infrastructure/
   ├── Views/
   ├── Migrations/
   ├── tessdata/          (si vous utilisez OCR)
   ├── wwwroot/           (si vous avez des fichiers statiques)
   ├── opagax.db          (si vous voulez transporter les données)
   └── opagax.db-shm      (si présent)
   └── opagax.db-wal      (si présent)
   ```

3. **Exclure** (ne pas copier) :
   - `bin/` (sera régénéré)
   - `obj/` (sera régénéré)
   - `logs/` (sera recréé)
   - `wwwroot/uploads/` (optionnel, si vous voulez garder les fichiers uploadés)
   - `wwwroot/generated/` (optionnel)
   - `wwwroot/examples/` (optionnel)

### Étape 2 : Transporter vers le PC de Travail

1. **Copier le dossier** sur une clé USB, réseau partagé, ou cloud
2. **Coller** dans un dossier sur le PC de travail (ex: `C:\FormatiX`)

### Étape 3 : Configuration sur le PC de Travail

1. **Ouvrir un terminal** dans le dossier de l'application

2. **Restaurer les dépendances** :
   ```powershell
   dotnet restore
   ```

3. **Appliquer les migrations** (si base de données copiée) :
   ```powershell
   dotnet ef database update
   ```

4. **Configurer `appsettings.json`** :
   - Vérifier `ConnectionStrings` (chemin de la base de données)
   - Ajuster `Ollama.BaseUrl` si nécessaire
   - Configurer `Sync.CentralUrl` si vous utilisez la synchronisation
   - Vérifier `Tesseract.DataPath` (chemin relatif ou absolu)

5. **Lancer l'application** :
   ```powershell
   dotnet run
   ```

---

## 📋 Méthode 2 : Build et Déploiement (Production)

### Étape 1 : Créer un Build sur le PC de Développement

```powershell
# Build en mode Release
dotnet publish -c Release -o ./publish

# Cela crée un dossier "publish" avec tous les fichiers nécessaires
```

### Étape 2 : Copier le Dossier "publish"

Le dossier `publish/` contient :
- L'exécutable compilé
- Toutes les DLLs nécessaires
- Les fichiers de configuration
- Les vues Razor compilées

### Étape 3 : Sur le PC de Travail

1. **Copier le dossier `publish/`**
2. **Copier également** :
   - `tessdata/` (si OCR utilisé)
   - `opagax.db` (si vous voulez les données)
   - `appsettings.json` (vérifier la configuration)

3. **Lancer directement** :
   ```powershell
   cd publish
   .\FormationManager.exe
   ```

**Avantage** : Pas besoin de .NET SDK, seulement le Runtime .NET 9.0

---

## 📋 Méthode 3 : Utiliser le Script de Déploiement

Un script `deploy-package.ps1` est fourni pour automatiser le processus.

---

## ⚙️ Configuration Importante

### Fichier `appsettings.json`

Vérifier et ajuster :

1. **Base de données** :
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=opagax.db"
   }
   ```
   - Chemin relatif : `opagax.db` (dans le même dossier)
   - Chemin absolu : `C:\FormatiX\Data\opagax.db`

2. **Tesseract** (si utilisé) :
   ```json
   "Tesseract": {
     "DataPath": "./tessdata",  // Relatif au dossier de l'app
     "Language": "fra"
   }
   ```

3. **Ollama** (si utilisé) :
   ```json
   "Ollama": {
     "BaseUrl": "http://localhost:11434",
     "Model": "mistral"
   }
   ```

4. **Synchronisation** (si multi-sites) :
   ```json
   "Sync": {
     "SiteId": "AVI",  // Changer selon le site
     "CentralUrl": "https://votre-serveur-central.com"
   }
   ```

---

## 📁 Fichiers à Transporter

### Obligatoires :
- ✅ Tous les fichiers `.cs` (Controllers, Models, Services, etc.)
- ✅ Tous les fichiers `.cshtml` (Views)
- ✅ `FormationManager.csproj`
- ✅ `Program.cs`
- ✅ `appsettings.json`
- ✅ Dossier `Migrations/`
- ✅ `tessdata/` (si OCR utilisé)

### Optionnels (mais recommandés) :
- 📄 `opagax.db` (si vous voulez garder vos données)
- 📁 `wwwroot/uploads/` (documents uploadés)
- 📁 `wwwroot/generated/` (documents générés)
- 📁 `wwwroot/examples/` (documents exemples)

### À ne PAS transporter :
- ❌ `bin/` (sera régénéré)
- ❌ `obj/` (sera régénéré)
- ❌ `logs/` (sera recréé automatiquement)
- ❌ Fichiers temporaires Python (`temp_*.py`)

---

## 🔧 Première Installation sur le PC de Travail

### Si vous partez de zéro (sans base de données) :

1. **Restaurer les packages** :
   ```powershell
   dotnet restore
   ```

2. **Créer la base de données** :
   ```powershell
   dotnet ef database update
   ```

3. **Créer un compte admin** :
   - Via l'interface : Paramètres → Gestion des utilisateurs
   - Ou modifier `SeedData.cs` et relancer

4. **Configurer les sites** :
   - Via l'interface : Paramètres → Gestion des sites

### Si vous transportez la base de données existante :

1. **Copier** `opagax.db`, `opagax.db-shm`, `opagax.db-wal`
2. **Vérifier** que le chemin dans `appsettings.json` est correct
3. **Lancer** l'application directement

---

## 🚀 Démarrage Rapide

### Option A : Mode Développement
```powershell
dotnet run
```
- URL : `http://localhost:5000`
- Hot reload activé
- Logs détaillés

### Option B : Mode Production (Build)
```powershell
dotnet publish -c Release
cd bin/Release/net9.0/publish
.\FormationManager.exe
```

### Option C : Service Windows (IIS)
Voir le guide `INSTALLATION.md` pour l'installation en service Windows.

---

## ⚠️ Points d'Attention

### 1. Chemins Absolus vs Relatifs
- Les chemins dans `appsettings.json` peuvent être relatifs (`./tessdata`) ou absolus (`C:\FormatiX\tessdata`)
- Préférer les chemins relatifs pour la portabilité

### 2. Permissions
- L'application doit pouvoir :
  - Lire/écrire dans le dossier de l'application
  - Créer des fichiers dans `wwwroot/uploads/`
  - Créer des logs dans `logs/`

### 3. Ports
- Par défaut : `http://localhost:5000`
- Si le port est occupé, modifier dans `Program.cs` ou `appsettings.json`

### 4. Base de Données
- SQLite est portable : copier `opagax.db` suffit
- Si vous ne copiez pas la DB, elle sera recréée vide au premier lancement

### 5. Services Externes
- **Tesseract** : Optionnel, l'application fonctionne sans (mais l'OCR ne marchera pas)
- **Ollama** : Optionnel, l'application fonctionne sans (mais l'analyse IA ne marchera pas)

---

## 📦 Script de Déploiement Automatique

Un script PowerShell `deploy-package.ps1` est fourni pour automatiser le processus.

---

## ✅ Checklist de Transport

- [ ] .NET 9.0 SDK installé sur le PC de destination
- [ ] Tous les fichiers source copiés
- [ ] `appsettings.json` configuré pour le nouvel environnement
- [ ] Base de données copiée (si nécessaire)
- [ ] Dossier `tessdata/` copié (si OCR utilisé)
- [ ] Dossier `wwwroot/` copié (si fichiers statiques)
- [ ] `dotnet restore` exécuté
- [ ] `dotnet ef database update` exécuté (si migrations)
- [ ] Application testée et fonctionnelle

---

## 🆘 Dépannage

### Erreur : "Could not find a part of the path"
- Vérifier les chemins dans `appsettings.json`
- Utiliser des chemins relatifs plutôt qu'absolus

### Erreur : "Database locked"
- Fermer toutes les instances de l'application
- Supprimer `opagax.db-shm` et `opagax.db-wal` si nécessaire

### Erreur : "Tesseract not found"
- Installer Tesseract ou désactiver l'OCR dans la configuration

### Erreur : "Ollama connection failed"
- Vérifier qu'Ollama est démarré
- Vérifier l'URL dans `appsettings.json`

---

## 📞 Support

En cas de problème, vérifier les logs dans `logs/app-*.log`
