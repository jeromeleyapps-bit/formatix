# Script de réinitialisation complète pour test première installation
# Usage: .\reset-fresh-install.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Réinitialisation complète FormatiX" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Confirmation
$confirm = Read-Host "Cette action va SUPPRIMER toutes les données. Continuer ? (O/N)"
if ($confirm -ne "O" -and $confirm -ne "o") {
    Write-Host "Annulé." -ForegroundColor Yellow
    exit
}

Write-Host ""
Write-Host "1. Arrêt de l'application..." -ForegroundColor Yellow
Get-Process FormationManager -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "   Arrêt du processus FormationManager (PID: $($_.Id))" -ForegroundColor Gray
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 2

Write-Host "2. Suppression de la base de données SQLite..." -ForegroundColor Yellow
$dbFiles = @("opagax.db", "opagax.db-shm", "opagax.db-wal")
foreach ($file in $dbFiles) {
    if (Test-Path $file) {
        Remove-Item $file -Force -ErrorAction SilentlyContinue
        Write-Host "   Supprimé: $file" -ForegroundColor Green
    }
}

Write-Host "3. Suppression des fichiers de logs..." -ForegroundColor Yellow
if (Test-Path "logs") {
    Remove-Item "logs\*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "   Logs supprimés" -ForegroundColor Green
}

Write-Host "4. Suppression des fichiers uploads/generated..." -ForegroundColor Yellow
$uploadDirs = @("wwwroot\uploads", "wwwroot\generated", "wwwroot\examples")
foreach ($dir in $uploadDirs) {
    if (Test-Path $dir) {
        Remove-Item "$dir\*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "   Nettoyé: $dir" -ForegroundColor Green
    }
}

Write-Host "5. Suppression des fichiers temporaires..." -ForegroundColor Yellow
$tempFiles = @("temp_*.py", "temp_*.pdf", "cookies.txt", "login_response.txt", "sessions_response.txt", "login.html")
foreach ($pattern in $tempFiles) {
    Get-ChildItem -Path . -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
        Write-Host "   Supprimé: $($_.Name)" -ForegroundColor Green
    }
}

Write-Host "6. Désactivation des données de démonstration..." -ForegroundColor Yellow
$appsettingsPath = "appsettings.json"
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    if (-not $appsettings.AppSettings) {
        $appsettings | Add-Member -MemberType NoteProperty -Name "AppSettings" -Value @{}
    }
    if (-not $appsettings.AppSettings.PSObject.Properties['CreateDemoData']) {
        $appsettings.AppSettings | Add-Member -MemberType NoteProperty -Name "CreateDemoData" -Value $false
    } else {
        $appsettings.AppSettings.CreateDemoData = $false
    }
    $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
    Write-Host "   Configuration mise à jour: CreateDemoData = false" -ForegroundColor Green
}

Write-Host "7. Nettoyage des migrations (conservation des fichiers)..." -ForegroundColor Yellow

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Réinitialisation terminée !" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Prochaines étapes:" -ForegroundColor Yellow
Write-Host "1. Lancez l'application: dotnet run" -ForegroundColor White
Write-Host "2. La base de données sera recréée automatiquement" -ForegroundColor White
Write-Host "3. Seul l'utilisateur admin par défaut sera créé (pas de données de démo)" -ForegroundColor White
Write-Host "4. Connectez-vous avec: admin@formationmanager.com / Admin123!" -ForegroundColor White
Write-Host "5. Créez vos utilisateurs via Paramètres → Utilisateurs" -ForegroundColor White
Write-Host ""
$start = Read-Host "Voulez-vous démarrer l'application maintenant ? (O/N)"
if ($start -eq "O" -or $start -eq "o") {
    Write-Host ""
    Write-Host "Démarrage de l'application..." -ForegroundColor Green
    dotnet run
}
