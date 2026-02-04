@echo off
echo ========================================
echo   Formation Manager - Deploiement
echo ========================================
echo.

echo [1/3] Verification du SDK .NET 8...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERREUR: SDK .NET 8 non trouve!
    pause
    exit /b 1
)

echo SDK .NET 8 trouve: 
dotnet --version
echo.

echo [2/3] Publication de l'application...
echo Publication pour Windows x64...
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if %errorlevel% neq 0 (
    echo ERREUR lors de la publication
    pause
    exit /b 1
)

echo Publication reussie.
echo.

echo [3/3] Creation du package d'installation...
if not exist "dist" mkdir dist
xcopy "bin\Release\net8.0\win-x64\publish\*" "dist\" /E /Y /Q

echo.
echo ========================================
echo   DEPLOIEMENT TERMINE
echo ========================================
echo.
echo L'application a ete deployee dans le dossier "dist\"
echo.
echo Pour lancer l'application:
echo   1. Naviguez dans le dossier "dist\"
echo   2. Executez "FormationManager.exe"
echo.
echo Le port par defaut sera: https://localhost:5001
echo.

pause
