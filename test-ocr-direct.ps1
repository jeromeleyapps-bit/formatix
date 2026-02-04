# Test direct du service OCR avec analyse automatique
param(
    [string]$PdfPath = "C:\AI\Opagax\Convention-signée-Charline_Leyssard.pdf"
)

Write-Host "=== TEST DIRECT OCR ===" -ForegroundColor Cyan

if (-not (Test-Path $PdfPath)) {
    Write-Host "ERREUR: Fichier introuvable: $PdfPath" -ForegroundColor Red
    exit 1
}

Write-Host "Fichier trouvé: $PdfPath" -ForegroundColor Green
Write-Host "Taille: $((Get-Item $PdfPath).Length) bytes" -ForegroundColor Gray

# Lire les logs avant
$logFile = Get-ChildItem "C:\AI\Opagax\logs\*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$logBefore = if ($logFile) { (Get-Content $logFile.FullName).Count } else { 0 }

Write-Host "`nLogs avant test: $logBefore lignes" -ForegroundColor Gray
Write-Host "`n=== UPLOAD VIA L'INTERFACE WEB ===" -ForegroundColor Yellow
Write-Host "1. Ouvre http://localhost:5000/Documents/Upload" -ForegroundColor White
Write-Host "2. Upload le fichier: $PdfPath" -ForegroundColor White
Write-Host "3. Appuie sur Entrée ici une fois l'upload terminé..." -ForegroundColor White
Read-Host

# Attendre un peu
Start-Sleep -Seconds 3

# Lire les nouveaux logs
if ($logFile) {
    $logAfter = (Get-Content $logFile.FullName).Count
    $newLogs = Get-Content $logFile.FullName -Tail ($logAfter - $logBefore + 100)
    
    Write-Host "`n=== ANALYSE DES LOGS ===" -ForegroundColor Cyan
    Write-Host "Nouveaux logs: $($logAfter - $logBefore) lignes" -ForegroundColor Gray
    
    # Chercher les erreurs
    $errors = $newLogs | Select-String -Pattern "error|Error|ERROR|exception|Exception|n'existe pas|impossible|Impossible|planté|crash"
    $success = $newLogs | Select-String -Pattern "Extraction OCR terminée|Document importé|succès|terminée.*caractères"
    
    if ($success) {
        Write-Host "`n✓ SUCCÈS!" -ForegroundColor Green
        $success | ForEach-Object { Write-Host $_ -ForegroundColor Green }
    } elseif ($errors) {
        Write-Host "`n✗ ERREURS DÉTECTÉES:" -ForegroundColor Red
        $errors | Select-Object -Last 10 | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        
        # Analyser le type d'erreur
        $errorText = ($errors | Select-String -Pattern "Tesseract|tessdata|Conversion|fallback" | Select-Object -Last 5 | Out-String)
        
        Write-Host "`n=== DIAGNOSTIC ===" -ForegroundColor Yellow
        if ($errorText -match "Conversion.*terminée.*images") {
            Write-Host "✓ Les images sont créées" -ForegroundColor Green
            Write-Host "✗ Problème probablement avec Tesseract" -ForegroundColor Red
        }
        if ($errorText -match "tessdata.*n'existe pas") {
            Write-Host "✗ Dossier tessdata manquant" -ForegroundColor Red
        }
        if ($errorText -match "TesseractEngine") {
            Write-Host "✗ Tesseract plante lors de l'initialisation" -ForegroundColor Red
        }
    } else {
        Write-Host "`n? Aucun log d'erreur ou de succès trouvé" -ForegroundColor Yellow
        Write-Host "Derniers logs:" -ForegroundColor Gray
        $newLogs | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
    }
}

Write-Host "`n=== FIN DE L'ANALYSE ===" -ForegroundColor Cyan
