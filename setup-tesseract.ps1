# Script PowerShell pour télécharger les fichiers Tesseract (ASCII only)

Write-Host "FormatiX - Installation des fichiers Tesseract OCR" -ForegroundColor Cyan
Write-Host ""

$tessdataPath = Join-Path $PSScriptRoot "tessdata"

# Creer le dossier si necessaire
if (-not (Test-Path $tessdataPath)) {
    New-Item -ItemType Directory -Path $tessdataPath -Force | Out-Null
    Write-Host "Dossier tessdata cree" -ForegroundColor Green
}

Write-Host "Telechargement des fichiers Tesseract..." -ForegroundColor Yellow
Write-Host ""

# URLs des fichiers
$files = @{
    "fra.traineddata" = "https://github.com/tesseract-ocr/tessdata/raw/main/fra.traineddata";
    "eng.traineddata" = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
}

foreach ($file in $files.GetEnumerator()) {
    $filePath = Join-Path $tessdataPath $file.Key

    if (Test-Path $filePath) {
        Write-Host ("OK - " + $file.Key + " existe deja") -ForegroundColor Green
    } else {
        Write-Host ("Telechargement de " + $file.Key + "...") -ForegroundColor Yellow
        try {
            Invoke-WebRequest -Uri $file.Value -OutFile $filePath -UseBasicParsing
            Write-Host ("OK - " + $file.Key + " telecharge") -ForegroundColor Green
        } catch {
            Write-Host ("ERREUR - " + $file.Key + " : " + $_.Exception.Message) -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Installation Tesseract terminee." -ForegroundColor Cyan
Write-Host ("Les fichiers sont dans : " + $tessdataPath) -ForegroundColor Gray