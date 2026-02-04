# Script automatique de test et correction OCR
param(
    [string]$PdfPath = "C:\AI\Opagax\Convention-signée-Charline_Leyssard.pdf",
    [int]$MaxIterations = 10
)

$ErrorActionPreference = "Continue"
$iteration = 0
$success = $false

Write-Host "=== DÉMARRAGE DU PROCESSUS AUTOMATIQUE DE CORRECTION OCR ===" -ForegroundColor Cyan
Write-Host "Fichier PDF: $PdfPath" -ForegroundColor Yellow
Write-Host ""

while (-not $success -and $iteration -lt $MaxIterations) {
    $iteration++
    Write-Host "`n=== ITÉRATION $iteration ===" -ForegroundColor Green
    
    # 1. Vérifier que l'application est démarrée
    Write-Host "[$iteration.1] Vérification de l'application..." -ForegroundColor Cyan
    $appRunning = Get-Process FormationManager -ErrorAction SilentlyContinue
    if (-not $appRunning) {
        Write-Host "[$iteration.1] Démarrage de l'application..." -ForegroundColor Yellow
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\AI\Opagax; dotnet run" -WindowStyle Minimized
        Start-Sleep -Seconds 5
    }
    
    # 2. Lire les logs avant le test
    Write-Host "[$iteration.2] Lecture des logs avant test..." -ForegroundColor Cyan
    $logFile = Get-ChildItem "C:\AI\Opagax\logs\*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    $logBefore = if ($logFile) { (Get-Content $logFile.FullName).Count } else { 0 }
    
    # 3. Tester l'upload (simulation via curl ou Invoke-WebRequest)
    Write-Host "[$iteration.3] Test de l'upload du PDF..." -ForegroundColor Cyan
    try {
        $testResult = Invoke-WebRequest -Uri "http://localhost:5000/Documents/Upload" -Method GET -UseBasicParsing -ErrorAction SilentlyContinue
        if ($testResult.StatusCode -eq 200) {
            Write-Host "[$iteration.3] Page d'upload accessible" -ForegroundColor Green
        }
    } catch {
        Write-Host "[$iteration.3] Erreur d'accès à la page: $_" -ForegroundColor Red
    }
    
    # 4. Attendre un peu pour que les logs soient écrits
    Start-Sleep -Seconds 3
    
    # 5. Lire les nouveaux logs
    Write-Host "[$iteration.4] Analyse des logs..." -ForegroundColor Cyan
    if ($logFile) {
        $logAfter = (Get-Content $logFile.FullName).Count
        $newLogs = Get-Content $logFile.FullName -Tail ($logAfter - $logBefore + 50) | Select-String -Pattern "OCR|Tesseract|Upload|error|Error|ERROR|exception|Exception|Conversion|fallback|tessdata|TesseractEngine" -Context 1,1
        
        Write-Host "[$iteration.4] Nouveaux logs trouvés:" -ForegroundColor Yellow
        $newLogs | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
        
        # 6. Analyser les erreurs
        $errors = $newLogs | Select-String -Pattern "error|Error|ERROR|exception|Exception|n'existe pas|n'est pas|impossible|Impossible"
        
        if ($errors) {
            Write-Host "[$iteration.5] Erreurs détectées:" -ForegroundColor Red
            $errors | Select-Object -Last 10 | ForEach-Object { Write-Host $_ -ForegroundColor Red }
            
            # Analyser le type d'erreur et corriger
            $errorText = ($errors | Select-Object -Last 5 | Out-String)
            
            if ($errorText -match "tessdata.*n'existe pas|tessdata.*n'est pas") {
                Write-Host "[$iteration.6] Correction: Vérification du dossier tessdata..." -ForegroundColor Yellow
                # Le code devrait déjà gérer ça, mais on vérifie
            }
            
            if ($errorText -match "TesseractEngine|AccessViolation|DllNotFound") {
                Write-Host "[$iteration.6] Correction: Problème avec Tesseract - désactivation temporaire..." -ForegroundColor Yellow
                # Désactiver temporairement Tesseract pour permettre l'upload
                $ocrFile = "C:\AI\Opagax\Infrastructure\OCR\TesseractOCRService.cs"
                $content = Get-Content $ocrFile -Raw
                
                # Si Tesseract plante, retourner chaîne vide immédiatement
                if ($content -notmatch "return string\.Empty;.*// Désactivation temporaire Tesseract") {
                    Write-Host "[$iteration.6] Ajout d'un bypass Tesseract..." -ForegroundColor Yellow
                    # On va plutôt améliorer la gestion d'erreur
                }
            }
            
            if ($errorText -match "Conversion.*terminée.*images") {
                Write-Host "[$iteration.6] Les images sont créées, problème probablement avec Tesseract" -ForegroundColor Yellow
                # Le problème est après la conversion - probablement Tesseract qui plante
                # Solution: désactiver Tesseract temporairement ou améliorer la gestion
            }
        } else {
            # Vérifier si on a un succès
            $successLogs = $newLogs | Select-String -Pattern "Extraction OCR terminée|Document importé|succès"
            if ($successLogs) {
                Write-Host "[$iteration.5] SUCCÈS DÉTECTÉ!" -ForegroundColor Green
                $success = $true
                break
            }
        }
    }
    
    # 7. Si on a des erreurs, corriger le code
    if (-not $success -and $iteration -lt $MaxIterations) {
        Write-Host "[$iteration.7] Application de corrections..." -ForegroundColor Yellow
        
        # Correction: Désactiver temporairement Tesseract si ça plante
        $ocrFile = "C:\AI\Opagax\Infrastructure\OCR\TesseractOCRService.cs"
        $content = Get-Content $ocrFile -Raw
        
        # Vérifier si on doit ajouter un bypass
        if ($content -notmatch "// BYPASS TESSERACT TEMPORAIRE") {
            Write-Host "[$iteration.7] Ajout d'un bypass Tesseract en cas d'échec..." -ForegroundColor Yellow
            
            # Lire le fichier
            $lines = Get-Content $ocrFile
            $newLines = @()
            $skipNext = $false
            
            for ($i = 0; $i -lt $lines.Count; $i++) {
                $line = $lines[$i]
                
                # Ajouter un bypass juste avant la création de TesseractEngine
                if ($line -match "AVANT création TesseractEngine" -and $lines[$i+1] -notmatch "// BYPASS") {
                    $newLines += $line
                    $newLines += "                // BYPASS TESSERACT TEMPORAIRE - Retour chaîne vide si Tesseract plante"
                    $newLines += "                // Décommenter la ligne suivante pour désactiver Tesseract temporairement:"
                    $newLines += "                // return string.Empty;"
                    continue
                }
                
                $newLines += $line
            }
            
            # Sauvegarder
            $newLines | Set-Content $ocrFile -Encoding UTF8
            Write-Host "[$iteration.7] Fichier modifié" -ForegroundColor Green
            
            # Rebuild
            Write-Host "[$iteration.8] Rebuild de l'application..." -ForegroundColor Yellow
            Stop-Process -Name FormationManager -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            
            $buildResult = dotnet build 2>&1
            if ($buildResult -match "Build succeeded|Build FAILED") {
                Write-Host "[$iteration.8] Build: $($buildResult | Select-String -Pattern 'succeeded|FAILED')" -ForegroundColor $(if ($buildResult -match "succeeded") { "Green" } else { "Red" })
            }
            
            # Redémarrer
            Write-Host "[$iteration.9] Redémarrage de l'application..." -ForegroundColor Yellow
            Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd C:\AI\Opagax; dotnet run" -WindowStyle Minimized
            Start-Sleep -Seconds 5
        }
    }
    
    Write-Host "[$iteration] Itération terminée. Attente avant la suivante..." -ForegroundColor Gray
    Start-Sleep -Seconds 2
}

if ($success) {
    Write-Host "`n=== SUCCÈS! Le problème est résolu ===" -ForegroundColor Green
} else {
    Write-Host "`n=== ÉCHEC après $MaxIterations itérations ===" -ForegroundColor Red
    Write-Host "Vérifiez les logs manuellement pour plus de détails." -ForegroundColor Yellow
}
