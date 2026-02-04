@echo off
echo ========================================
echo INSTALLATION GHOSTSCRIPT POUR FORMATIX
echo ========================================
echo.
echo Ce script va installer Ghostscript necessaire pour l'OCR PDF
echo.
echo IMPORTANT: Ce script necessite des droits administrateur!
echo.
pause

powershell -ExecutionPolicy Bypass -File "%~dp0install-ghostscript.ps1"

pause
