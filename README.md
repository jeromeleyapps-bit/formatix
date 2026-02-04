# FormatiX

## 📚 Application de Gestion de Formations Qualiopi

**FormatiX** est une solution complète pour la gestion de formations certifiées Qualiopi avec synchronisation décentralisée, OCR et analyse IA.

## ✨ Fonctionnalités Principales

- 🎓 **Gestion de Formations** : Catalogue, sessions, apprenants
- 📄 **Génération de Documents** : Conventions, attestations, émargements, évaluations
- 🤖 **OCR avec Tesseract** : Extraction automatique de texte depuis PDF
- 🧠 **Analyse IA avec Ollama** : Classification Qualiopi automatique
- 🔄 **Synchronisation Décentralisée** : Multi-sites avec serveur central
- ✅ **Module Qualiopi** : 7 critères et indicateurs complets
- 📊 **Reporting** : BPF, exports CSV/JSON, rapports Qualiopi
- 🔍 **Monitoring** : Health checks, logs structurés avec Serilog

## 🚀 Démarrage Rapide

### Prérequis

- .NET 9 SDK
- Ollama (optionnel, pour l'IA)
- Fichiers Tesseract (optionnel, pour l'OCR)

### Installation

1. **Restaurer les packages** :
```bash
dotnet restore
```

2. **Configurer votre organisation** dans `appsettings.json` :
```json
"AppSettings": {
  "NomOrganisme": "VOTRE ORGANISME",
  "SIRET": "VOTRE_SIRET",
  ...
}
```

3. **Configurer la base de données** :
```bash
dotnet ef database update
```

4. **Lancer l'application** :
```bash
dotnet run
```

Pour plus de détails, voir [INSTALLATION.md](INSTALLATION.md)

## 📖 Documentation

- [Guide de Démarrage](GUIDE_DEMARRAGE.md) : Utilisation de l'application
- [Installation](INSTALLATION.md) : Guide d'installation complet
- [Architecture](ARCHITECTURE_COMPLETE.md) : Documentation technique complète

## 🔧 Configuration

L'application est entièrement configurable via `appsettings.json` :

- **Organisation** : Nom, SIRET, adresse, contact
- **Ollama** : Configuration de l'IA
- **Tesseract** : Configuration de l'OCR
- **Sync** : Configuration de la synchronisation
- **Logging** : Configuration des logs Serilog

## 🌐 API

L'API REST est documentée avec Swagger :
- **URL** : https://localhost:5001/swagger
- **Health Checks** : https://localhost:5001/health

## 🧪 Tests

```bash
# Tous les tests
dotnet test

# Tests avec couverture
dotnet test /p:CollectCoverage=true
```

## 📦 Structure du Projet

```
FormatiX/
├── Controllers/        # Contrôleurs MVC et API
├── Infrastructure/     # Services infrastructure (OCR, AI, Sync)
├── Services/          # Services métier
├── Models/            # Modèles de données
├── Data/              # Context EF Core
├── Views/             # Vues Razor
└── FormationManager.Tests/  # Tests
```

## 🏢 Affichage de l'Organisation

Le nom de votre organisation est visible partout dans l'application :
- Titre des pages
- Sidebar de navigation
- Page de connexion
- Footer

Configurez-le dans `appsettings.json` → `AppSettings:NomOrganisme`

## 🎯 Prochaines Étapes

1. ✅ Configurer le nom de votre organisation
2. ✅ Installer Ollama (pour l'IA)
3. ✅ Télécharger les fichiers Tesseract (pour l'OCR)
4. ✅ Lancer l'application et tester

## 📝 Licence

Ce projet est sous licence AGPL-3.0 - 100% open source et gratuit.

---

**FormatiX** - La solution complète pour votre certification Qualiopi 🎓