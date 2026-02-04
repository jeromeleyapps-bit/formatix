# Formation Manager - Application Windows de Gestion de Formations avec Certification Qualiopi

## 🎯 Objectif

Créer une application Windows déployable pour la gestion administrative d'organismes de formation avec certification Qualiopi, basée sur l'architecture et les fonctionnalités du projet OPAGA.

## 🏗️ Architecture Technique

### Backend
- **Framework**: ASP.NET Core 8 MVC
- **Base de données**: SQLite avec Entity Framework Core
- **Authentification**: ASP.NET Core Identity
- **Architecture**: Clean Architecture avec services dédiés

### Frontend
- **Interface Web**: Bootstrap 5 + Font Awesome
- **Design**: Moderne et responsive
- **Navigation**: Sidebar avec menu contextuel

### Services Principaux
- **Génération PDF**: QuestPDF (conventions, attestations, émargements, évaluations)
- **Export**: CsvHelper pour CSV, System.Text.Json pour JSON
- **API REST**: Endpoints complets pour toutes les fonctionnalités

## 📋 Fonctionnalités Implémentées

### ✅ Authentification et Gestion des Rôles
- Système complet avec 3 rôles: Administrateur, ResponsableFormation, Formateur
- Connexion sécurisée avec cookies
- Gestion des profils utilisateurs
- Données de démonstration pré-configurées

### ✅ Gestion des Formations
- CRUD complet des formations
- Catalogue avec prix, durée, programme
- Prérequis et modalités pédagogiques
- Interface moderne avec cards

### ✅ Gestion des Sessions
- Programmation des sessions
- Gestion des formateurs et lieux
- Suivi des inscriptions et statuts

### ✅ API REST Complète
- `/api/formations` - Gestion des formations
- `/api/sessions` - Gestion des sessions
- `/api/clients` - Gestion des clients
- `/api/stagiaires` - Gestion des stagiaires
- `/api/qualiopi` - Conformité Qualiopi
- `/api/bpf` - Bilan Pédagogique et Financier
- `/api/export` - Export de données

### ✅ Génération de Documents PDF
- Conventions de formation
- Attestations de présence
- Feuilles d'émargement
- Évaluations stagiaires
- Rapports Qualiopi
- Bilans Pédagogiques et Financiers

### ✅ Export de Données
- Export CSV (sessions, stagiaires)
- Export JSON (BPF, Qualiopi)
- Formatage optimisé pour l'analyse

### ✅ Tableau de Bord
- Statistiques en temps réel
- Sessions à venir
- Tâches en attente
- Conformité Qualiopi

## 🗂️ Structure du Projet

```
c:\AI\Opagax\
├── Controllers\
│   ├── AccountController.cs          # Authentification
│   ├── ApiController.cs              # API REST
│   ├── MVCController.cs              # Contrôleurs MVC
│   └── SpecializedController.cs      # Services spécialisés
├── Data\
│   ├── FormationDbContext.cs         # Base de données
│   └── SeedData.cs                   # Données initiales
├── Models\
│   └── Entities.cs                   # Modèles de données
├── Services\
│   ├── DocumentService.cs            # Génération PDF
│   ├── QualiopiService.cs            # Gestion Qualiopi
│   ├── BPFService.cs                 # Bilan Pédagogique
│   └── ExportService.cs              # Export données
├── Views\
│   ├── Account\                      # Vues authentification
│   ├── Formations\                   # Vues formations
│   ├── Home\                         # Tableau de bord
│   └── Shared\                       # Layouts et partials
├── FormationManager.csproj           # Configuration projet
├── Program.cs                        # Point d'entrée
├── appsettings.json                  # Configuration
├── install.bat                       # Script d'installation
├── deploy.bat                        # Script de déploiement
└── README.md                         # Documentation
```

## 🚀 Installation et Lancement

### Prérequis
- SDK .NET 8 (https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation Automatique
```batch
install.bat
```

### Installation Manuelle
```powershell
dotnet restore
dotnet build
dotnet run
```

### Accès à l'Application
- URL: https://localhost:5001 ou http://localhost:5000

## 👥 Comptes de Démonstration

| Rôle | Email | Mot de passe |
|------|-------|--------------|
| Administrateur | admin@formationmanager.com | Admin123! |
| Responsable | responsable@formationmanager.com | Responsable123! |
| Formateur | formateur1@formationmanager.com | Formateur123! |

## 📊 Données de Démonstration

L'application inclut des données complètes pour tester toutes les fonctionnalités:
- 3 formations exemples (Communication Digitale, Gestion de Projet, Marketing Digital)
- 3 sessions programmées (Paris, Lyon, Nantes)
- 3 clients (entreprises et particulier)
- 4 stagiaires avec évaluations
- Actions de veille pédagogique
- Indicateurs Qualiopi complets

## 🔧 Déploiement

### Déploiement Windows
```batch
deploy.bat
```

L'application sera publiée dans le dossier `dist\` prête pour le déploiement.

### Déploiement Manuel
```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

## 🎨 Interface Utilisateur

### Design Moderne
- Interface responsive avec Bootstrap 5
- Sidebar navigation intuitive
- Cards modernes pour l'affichage des données
- Icônes Font Awesome
- Thème professionnel avec couleurs cohérentes

### Expérience Utilisateur
- Tableau de bord avec statistiques visuelles
- Navigation fluide entre les modules
- Formulaires optimisés
- Messages de confirmation et erreurs clairs

## 🔐 Sécurité

### Authentification
- ASP.NET Core Identity
- Hashage sécurisé des mots de passe
- Cookies sécurisés avec expiration
- Protection contre les attaques CSRF

### Autorisations
- Rôles granulaires (Administrateur, Responsable, Formateur)
- Contrôle d'accès par contrôleur
- Interface adaptée selon le rôle

## 📈 Qualiopi

### Gestion Complète
- 32 indicateurs répartis en 7 critères
- Suivi des preuves et conformité
- Rapport de conformité PDF
- Tableau de bord de suivi

### Fonctionnalités
- Ajout de preuves par session
- Validation des indicateurs
- Export des données Qualiopi
- Historique des modifications

## 📋 BPF (Bilan Pédagogique et Financier)

### Génération Automatique
- Calcul des statistiques (sessions, stagiaires, heures, CA)
- Rapport PDF détaillé
- Export JSON pour analyse
- Périodes personnalisables

## 🔄 API REST

### Endpoints Principaux
- **Formations**: CRUD complet + génération documents
- **Sessions**: Gestion + suivi stagiaires
- **Clients**: Gestion entreprises/particuliers
- **Stagiaires**: Inscriptions + évaluations
- **Qualiopi**: Indicateurs + preuves
- **BPF**: Statistiques + rapports
- **Export**: CSV + JSON

### Documentation
- Réponses JSON structurées
- Codes d'erreur standards
- Exemples d'utilisation

## 🎯 Prochaines Étapes

L'application est **fonctionnelle et testable** avec toutes les fonctionnalités principales. Pour une version finale:

1. **Tests complets** avec les différents comptes
2. **Interface WPF/WinUI** pour une application Windows native
3. **Déploiement en production** avec configuration HTTPS
4. **Sauvegarde/Restauration** de la base de données
5. **Notifications** et alertes automatiques

## 📞 Support

Pour toute question ou problème:
1. Consultez le README.md détaillé
2. Utilisez les scripts d'installation/déploiement
3. Testez avec les comptes de démonstration

---

**Application complète et fonctionnelle prête pour le déploiement et les tests!** 🚀
