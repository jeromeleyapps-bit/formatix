# Script de test OCR
param(
    [string]$PdfPath = "C:\Users\j_ley\Documents\Convention-signée-Charline_Leyssard.pdf"
)

Write-Host "Test OCR avec le fichier: $PdfPath" -ForegroundColor Cyan

if (-not (Test-Path $PdfPath)) {
    Write-Host "ERREUR: Le fichier n'existe pas: $PdfPath" -ForegroundColor Red
    exit 1
}

$logFile = Get-ChildItem "C:\AI\Opagax\logs\*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($logFile) {
    Write-Host "`nDerniers logs avant le test:" -ForegroundColor Yellow
    Get-Content $logFile.FullName -Tail 20
    Write-Host "`n" -ForegroundColor Yellow
}

Write-Host "En attente de l'upload du fichier..." -ForegroundColor Green
Write-Host "Une fois l'upload terminé, appuyez sur Entrée pour analyser les logs..." -ForegroundColor Green
Read-Host

if ($logFile) {
    Write-Host "`nNouveaux logs après le test:" -ForegroundColor Yellow
    Get-Content $logFile.FullName -Tail 50 | Select-String -Pattern "OCR|Tesseract|Upload|error|Error|ERROR|exception|Exception" -Context 2,2
}
