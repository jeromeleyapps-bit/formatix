# Tests Opagax

## Structure des tests

### Tests unitaires
- **OCRServiceTests** : Tests du service OCR Tesseract
- **SyncServiceTests** : Tests du service de synchronisation
- **AIServiceTests** : Tests du service IA Ollama (à créer)

### Tests d'intégration
- **SyncIntegrationTests** : Tests d'intégration de la synchronisation (nécessitent un serveur central)

## Exécution des tests

```bash
# Tous les tests
dotnet test

# Tests unitaires uniquement
dotnet test --filter Category=Unit

# Tests d'intégration uniquement
dotnet test --filter Category=Integration

# Avec couverture de code
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Notes

- Les tests d'intégration nécessitent que les services externes (Ollama, serveur central) soient en cours d'exécution
- Certains tests peuvent être marqués avec `[Fact(Skip = "Raison")]` s'ils nécessitent une configuration spécifique

## Débogage

Pour déboguer un test spécifique :
1. Mettre un point d'arrêt dans le test
2. Cliquer droit sur le test > "Déboguer"
3. Ou utiliser `dotnet test --logger "console;verbosity=detailed"`