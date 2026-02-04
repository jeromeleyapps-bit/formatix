@echo off
REM Script de Déploiement FormatiX - Version Batch
REM Crée un package prêt à être transporté vers un autre PC

echo ========================================
echo   FormatiX - Script de Deploiement
echo ========================================
echo.

REM Vérifier PowerShell
powershell -Command "Get-Host" >nul 2>&1
if errorlevel 1 (
    echo ERREUR : PowerShell n'est pas disponible
    pause
    exit /b 1
)

REM Exécuter le script PowerShell
powershell -ExecutionPolicy Bypass -File "%~dp0deploy-package.ps1" %*

pause
