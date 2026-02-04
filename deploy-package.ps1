# Script de Déploiement FormatiX
# Crée un package prêt à être transporté vers un autre PC

param(
    [string]$OutputPath = ".\FormatiX_Package",
    [switch]$IncludeDatabase = $false,
    [switch]$IncludeUploads = $false,
    [switch]$BuildRelease = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  FormatiX - Script de Déploiement" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Vérifier que nous sommes dans le bon dossier
if (-not (Test-Path "FormationManager.csproj")) {
    Write-Host "ERREUR : Ce script doit être exécuté depuis le dossier racine du projet" -ForegroundColor Red
    exit 1
}

# Créer le dossier de sortie
if (Test-Path $OutputPath) {
    Write-Host "Suppression de l'ancien package..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

Write-Host "Création du package dans : $OutputPath" -ForegroundColor Green
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Fichiers et dossiers à copier
$itemsToCopy = @(
    "FormationManager.csproj",
    "Program.cs",
    "appsettings.json",
    "Controllers",
    "Data",
    "Models",
    "Services",
    "Infrastructure",
    "Views",
    "Migrations",
    "tessdata",
    "wwwroot\icon.png",
    "wwwroot\favicon.ico",
    "README.md",
    "GUIDE_DEPLOIEMENT.md",
    "INSTALLATION.md",
    "DOCUMENTS_QUALIOPI.md",
    "GUIDE_CREATION_ADMIN.md",
    "RESET_INSTALL.md"
)

Write-Host "`nCopie des fichiers..." -ForegroundColor Green
foreach ($item in $itemsToCopy) {
    if (Test-Path $item) {
        $destPath = Join-Path $OutputPath $item
        $destDir = Split-Path $destPath -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }
        Copy-Item -Path $item -Destination $destPath -Recurse -Force
        Write-Host "  ✓ $item" -ForegroundColor Gray
    } else {
        Write-Host "  ⚠ $item (non trouvé, ignoré)" -ForegroundColor Yellow
    }
}

# Base de données
if ($IncludeDatabase) {
    Write-Host "`nCopie de la base de données..." -ForegroundColor Green
    $dbFiles = @("opagax.db", "opagax.db-shm", "opagax.db-wal")
    foreach ($dbFile in $dbFiles) {
        if (Test-Path $dbFile) {
            Copy-Item -Path $dbFile -Destination $OutputPath -Force
            Write-Host "  ✓ $dbFile" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "`nBase de données non incluse (utilisez -IncludeDatabase pour l'inclure)" -ForegroundColor Yellow
}

# Fichiers uploadés
if ($IncludeUploads) {
    Write-Host "`nCopie des fichiers uploadés..." -ForegroundColor Green
    $uploadDirs = @("wwwroot\uploads", "wwwroot\generated", "wwwroot\examples")
    foreach ($uploadDir in $uploadDirs) {
        if (Test-Path $uploadDir) {
            $destPath = Join-Path $OutputPath $uploadDir
            Copy-Item -Path $uploadDir -Destination $destPath -Recurse -Force
            Write-Host "  ✓ $uploadDir" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "`nFichiers uploadés non inclus (utilisez -IncludeUploads pour les inclure)" -ForegroundColor Yellow
}

# Créer un fichier appsettings.json d'exemple si nécessaire
$exampleAppSettings = Join-Path $OutputPath "appsettings.example.json"
if (-not (Test-Path $exampleAppSettings)) {
    Copy-Item -Path "appsettings.json" -Destination $exampleAppSettings -Force
    Write-Host "`nCrée appsettings.example.json pour référence" -ForegroundColor Gray
}

# Créer un script de démarrage
$startScript = @'
@echo off
echo ========================================
echo   FormatiX - Demarrage
echo ========================================
echo.

REM Verifier .NET
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERREUR : .NET 9.0 SDK n'est pas installe
    echo Telechargez-le depuis : https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)

echo Restauration des packages...
dotnet restore
if errorlevel 1 (
    echo ERREUR lors de la restauration des packages
    pause
    exit /b 1
)

echo.
echo Application des migrations...
dotnet ef database update
if errorlevel 1 (
    echo ATTENTION : Erreur lors des migrations (peut etre normal si DB existe deja)
)

echo.
echo Demarrage de l'application...
echo URL : http://localhost:5000
echo.
dotnet run

pause
'@

$startScriptPath = Join-Path $OutputPath "start.bat"
Set-Content -Path $startScriptPath -Value $startScript -Encoding ASCII
Write-Host "`n  ✓ start.bat créé" -ForegroundColor Gray

# Créer un README de déploiement
$deployReadme = @'
# Package FormatiX - Guide de Déploiement

## Installation Rapide

1. **Vérifier les prérequis** :
   - .NET 9.0 SDK installé
   - (Optionnel) Tesseract OCR si vous utilisez l'OCR
   - (Optionnel) Ollama si vous utilisez l'IA

2. **Lancer le script de démarrage** :
   ```
   start.bat
   ```

   Ou manuellement :
   ```
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

3. **Accéder à l'application** :
   - URL : http://localhost:5000

## Configuration

Éditer `appsettings.json` pour :
- Configurer votre organisation
- Ajuster les chemins (Tesseract, Ollama)
- Configurer la synchronisation si multi-sites

## Première Connexion

Si la base de données est vide :
1. Créer un compte admin via : Paramètres → Gestion des utilisateurs
2. Ou modifier `Data\SeedData.cs` et relancer

## Documentation

- `GUIDE_DEPLOIEMENT.md` : Guide complet de déploiement
- `INSTALLATION.md` : Guide d'installation détaillé
- `README.md` : Documentation générale

## Support

En cas de problème, vérifier les logs dans `logs/app-*.log`
'@

$deployReadmePath = Join-Path $OutputPath "DEPLOIEMENT_README.md"
Set-Content -Path $deployReadmePath -Value $deployReadme -Encoding UTF8
Write-Host "  ✓ DEPLOIEMENT_README.md créé" -ForegroundColor Gray

# Build Release si demandé
if ($BuildRelease) {
    Write-Host "`nBuild en mode Release..." -ForegroundColor Green
    $publishPath = Join-Path $OutputPath "publish"
    dotnet publish -c Release -o $publishPath
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Build Release créé dans : $publishPath" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ Erreur lors du build Release" -ForegroundColor Yellow
    }
}

# Résumé
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Package créé avec succès !" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Emplacement : $((Resolve-Path $OutputPath).Path)" -ForegroundColor White
Write-Host ""
Write-Host "Prochaines étapes :" -ForegroundColor Yellow
Write-Host "1. Copier le dossier '$OutputPath' vers votre PC de travail" -ForegroundColor White
Write-Host "2. Sur le PC de travail, exécuter 'start.bat' ou suivre GUIDE_DEPLOIEMENT.md" -ForegroundColor White
Write-Host ""
Write-Host "Taille du package :" -ForegroundColor Cyan
$size = (Get-ChildItem -Path $OutputPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "  $([math]::Round($size, 2)) MB" -ForegroundColor White
Write-Host ""
