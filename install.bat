@echo off
echo ========================================
echo   Formation Manager - Installation
echo ========================================
echo.

echo [1/4] Verification du SDK .NET 8+...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERREUR: SDK .NET 8 ou superieur non trouve!
    echo.
    echo Veuillez installer le SDK .NET 9 (recommande) ou .NET 8 depuis:
    echo https://dotnet.microsoft.com/download/dotnet/9.0
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo SDK .NET trouve: 
dotnet --version
echo.

echo [2/4] Restauration des packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERREUR lors de la restauration des packages
    pause
    exit /b 1
)
echo Packages restaures avec succes.
echo.

echo [3/4] Compilation de l'application...
dotnet build
if %errorlevel% neq 0 (
    echo ERREUR lors de la compilation
    pause
    exit /b 1
)
echo Compilation reussie.
echo.

echo [4/4] Lancement de l'application...
echo.
echo L'application va demarrer...
echo URL attendue: https://localhost:5001
echo.
echo Comptes de demonstration:
echo   Administrateur: admin@formationmanager.com / Admin123!
echo   Responsable:    responsable@formationmanager.com / Responsable123!
echo   Formateur:      formateur1@formationmanager.com / Formateur123!
echo.
echo Appuyez sur Ctrl+C pour arreter l'application.
echo.

dotnet run

pause
