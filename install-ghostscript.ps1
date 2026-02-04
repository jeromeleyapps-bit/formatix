# Script d'installation de Ghostscript pour FormatiX
# Nécessite des droits administrateur

Write-Host "`n=== INSTALLATION GHOSTSCRIPT POUR FORMATIX ===" -ForegroundColor Cyan
Write-Host "Ce script va installer Ghostscript necessaire pour l'OCR PDF" -ForegroundColor Yellow
Write-Host ""

# Vérifier les droits administrateur
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERREUR: Ce script necessite des droits administrateur!" -ForegroundColor Red
    Write-Host "Relancez PowerShell en tant qu'administrateur et reexecutez ce script." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Ou installez manuellement:" -ForegroundColor Yellow
    Write-Host "1. Telechargez: https://github.com/ArtifexSoftware/ghostpdl-downloads/releases" -ForegroundColor Cyan
    Write-Host "2. Installez gs10032w64.exe (ou version plus recente)" -ForegroundColor Cyan
    Write-Host "3. Redemarrez l'application FormatiX" -ForegroundColor Cyan
    exit 1
}

# URL de téléchargement Ghostscript
$url = "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/download/gs10032/gs10032w64.exe"
$installer = "$env:TEMP\gs10032w64.exe"

Write-Host "Telechargement de Ghostscript..." -ForegroundColor Yellow
try {
    # Télécharger avec une barre de progression
    $ProgressPreference = 'Continue'
    Invoke-WebRequest -Uri $url -OutFile $installer -UseBasicParsing -ErrorAction Stop
    Write-Host "Telechargement reussi!" -ForegroundColor Green
} catch {
    Write-Host "ERREUR lors du telechargement: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Installation manuelle requise:" -ForegroundColor Yellow
    Write-Host "1. Telechargez: $url" -ForegroundColor Cyan
    Write-Host "2. Executez l'installateur" -ForegroundColor Cyan
    Write-Host "3. Redemarrez l'application FormatiX" -ForegroundColor Cyan
    exit 1
}

Write-Host ""
Write-Host "Installation de Ghostscript (mode silencieux)..." -ForegroundColor Yellow
try {
    Start-Process -FilePath $installer -ArgumentList "/S" -Wait -NoNewWindow
    Write-Host "Installation terminee!" -ForegroundColor Green
} catch {
    Write-Host "ERREUR lors de l'installation: $_" -ForegroundColor Red
    Write-Host "Essayez d'executer l'installateur manuellement: $installer" -ForegroundColor Yellow
    exit 1
}

# Nettoyer
Remove-Item $installer -ErrorAction SilentlyContinue

# Vérifier l'installation
Write-Host ""
Write-Host "Verification de l'installation..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

$found = $false
$locations = @("C:\Program Files\gs", "C:\Program Files (x86)\gs")
foreach ($base in $locations) {
    if (Test-Path $base) {
        $gsDirs = Get-ChildItem $base -Directory -Filter "gs*" -ErrorAction SilentlyContinue
        foreach ($gsDir in $gsDirs) {
            $exe = Join-Path $gsDir "bin\gswin64c.exe"
            if (Test-Path $exe) {
                Write-Host "SUCCES: Ghostscript installe: $exe" -ForegroundColor Green
                $found = $true
                break
            }
        }
    }
}

if ($found) {
    Write-Host ""
    Write-Host "=== INSTALLATION REUSSIE ===" -ForegroundColor Green
    Write-Host "Ghostscript est maintenant installe et pret a etre utilise!" -ForegroundColor Green
    Write-Host "Redemarrez l'application FormatiX pour que les changements prennent effet." -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "ATTENTION: Ghostscript n'a pas ete detecte apres l'installation." -ForegroundColor Yellow
    Write-Host "Il peut etre necessaire de redemarrer l'ordinateur ou de mettre a jour le PATH." -ForegroundColor Yellow
    Write-Host "Verifiez manuellement dans: C:\Program Files\gs\" -ForegroundColor Cyan
}
