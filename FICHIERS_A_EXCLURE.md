# Fichiers à Exclure lors du Transport

## ❌ Ne PAS Copier (seront régénérés)

### Dossiers de Build
- `bin/` - Fichiers compilés (sera régénéré avec `dotnet restore` et `dotnet build`)
- `obj/` - Fichiers temporaires de compilation (sera régénéré)

### Logs
- `logs/` - Fichiers de logs (sera recréé automatiquement au démarrage)

### Fichiers Temporaires
- `*.db-shm` - Fichier temporaire SQLite (optionnel, peut être recréé)
- `*.db-wal` - Fichier temporaire SQLite (optionnel, peut être recréé)
- `temp_*.py` - Scripts Python temporaires de test

### Fichiers de Configuration IDE
- `.vs/` - Configuration Visual Studio
- `.vscode/` - Configuration VS Code (optionnel)
- `*.user` - Fichiers utilisateur Visual Studio

---

## ✅ À Copier (Obligatoires)

### Code Source
- `Controllers/` - Tous les contrôleurs
- `Models/` - Tous les modèles
- `Services/` - Tous les services
- `Infrastructure/` - Infrastructure (OCR, AI, Sync, etc.)
- `Data/` - DbContext et SeedData
- `Views/` - Toutes les vues Razor
- `Migrations/` - Migrations Entity Framework

### Configuration
- `FormationManager.csproj` - Fichier projet
- `Program.cs` - Point d'entrée
- `appsettings.json` - Configuration

### Ressources
- `tessdata/` - Données Tesseract OCR (si OCR utilisé)
- `wwwroot/icon.png` - Icône
- `wwwroot/favicon.ico` - Favicon

### Documentation
- `README.md`
- `GUIDE_DEPLOIEMENT.md`
- `INSTALLATION.md`
- `DOCUMENTS_QUALIOPI.md`
- `GUIDE_CREATION_ADMIN.md`

---

## 📦 Optionnels (selon vos besoins)

### Base de Données
- `opagax.db` - **Copier si vous voulez garder vos données**
- `opagax.db-shm` - Fichier temporaire (optionnel)
- `opagax.db-wal` - Fichier temporaire (optionnel)

### Fichiers Uploadés
- `wwwroot/uploads/` - Documents uploadés par les utilisateurs
- `wwwroot/generated/` - Documents PDF générés
- `wwwroot/examples/` - Documents exemples

**Note** : Si vous ne copiez pas ces dossiers, ils seront vides sur le nouveau PC.

---

## 📋 Résumé Rapide

### Transport Minimal (Code uniquement)
```
✅ Controllers/
✅ Models/
✅ Services/
✅ Infrastructure/
✅ Data/
✅ Views/
✅ Migrations/
✅ FormationManager.csproj
✅ Program.cs
✅ appsettings.json
✅ tessdata/
```

### Transport Complet (Avec données)
```
✅ Tout ce qui est ci-dessus
✅ opagax.db
✅ wwwroot/uploads/
✅ wwwroot/generated/
✅ wwwroot/examples/
```

### Utiliser le Script
Le script `deploy-package.ps1` fait automatiquement cette sélection :
```powershell
.\deploy-package.ps1                    # Transport minimal
.\deploy-package.ps1 -IncludeDatabase  # Avec base de données
.\deploy-package.ps1 -IncludeUploads   # Avec fichiers uploadés
.\deploy-package.ps1 -IncludeDatabase -IncludeUploads  # Tout
```
