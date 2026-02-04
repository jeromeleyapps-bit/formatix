@echo off
echo ========================================
echo Réinitialisation complète FormatiX
echo ========================================
echo.

powershell.exe -ExecutionPolicy Bypass -File "%~dp0reset-fresh-install.ps1"

pause
