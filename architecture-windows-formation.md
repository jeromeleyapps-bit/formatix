# Application de Gestion de Formation avec Certification Qualiopi
## Architecture Technique pour Windows

### Stack Technologique Recommandé

#### Backend
- **Framework** : .NET 8 (C#) - Idéal pour applications Windows desktop
- **Base de données** : SQLite (pour version standalone) ou SQL Server (pour multi-utilisateurs)
- **ORM** : Entity Framework Core
- **API REST** : ASP.NET Core Web API

#### Frontend
- **Desktop** : WPF ou WinUI 3 pour application Windows native
- **Alternative** : Electron avec React/Vue.js pour cross-platform

#### Génération PDF
- **iTextSharp** ou **PdfSharp** - Alternative .NET à dompdf

#### Architecture
```
├── FormationManager.Desktop (WPF/WinUI)
├── FormationManager.API (ASP.NET Core)
├── FormationManager.Core (Business Logic)
├── FormationManager.Data (Entity Framework)
├── FormationManager.Models (Entities)
└── FormationManager.Services (PDF, Export, etc.)
```

### Modules Principaux

#### 1. Gestion des Formations
- Création de fiches de formation
- Programmation de sessions
- Gestion des plannings

#### 2. Gestion des Clients
- CRM intégré
- Suivi financier
- Gestion des contrats

#### 3. Gestion des Stagiaires
- Inscriptions
- Suivi de présence
- Évaluations

#### 4. Documents Administratifs
- Templates personnalisables
- Génération PDF
- Validation workflow

#### 5. Module Qualiopi
- Suivi des 32 indicateurs
- Collecte des preuves
- Tableau de bord conformité

#### 6. Bilan Pédagogique et Financier
- Agrégation des données
- Export CSV/XML
- Visualisations

### Base de Données - Schéma Principal

```sql
-- Formations
CREATE TABLE Formations (
    Id INT PRIMARY KEY,
    Titre NVARCHAR(200) NOT NULL,
    Description TEXT,
    DureeHeures DECIMAL(5,2),
    PrixIndicatif DECIMAL(10,2),
    EstPublique BIT DEFAULT 1
);

-- Sessions
CREATE TABLE Sessions (
    Id INT PRIMARY KEY,
    FormationId INT FOREIGN KEY REFERENCES Formations(Id),
    DateDebut DATETIME2,
    DateFin DATETIME2,
    Lieu NVARCHAR(200),
    EstPublique BIT DEFAULT 1,
    Statut NVARCHAR(50)
);

-- Clients
CREATE TABLE Clients (
    Id INT PRIMARY KEY,
    Nom NVARCHAR(200) NOT NULL,
    TypeClient NVARCHAR(50), -- 'Particulier', 'Entreprise', 'OF'
    Email NVARCHAR(200),
    Telephone NVARCHAR(50)
);

-- Stagiaires
CREATE TABLE Stagiaires (
    Id INT PRIMARY KEY,
    ClientId INT FOREIGN KEY REFERENCES Clients(Id),
    Nom NVARCHAR(200) NOT NULL,
    Prenom NVARCHAR(200),
    Email NVARCHAR(200)
);

-- Documents
CREATE TABLE Documents (
    Id INT PRIMARY KEY,
    TypeDocument NVARCHAR(100),
    TemplateId INT,
    Donnees JSON,
    StatutValidation NVARCHAR(50),
    DateCreation DATETIME2
);
```

### Déploiement Windows

#### Options
1. **Application Desktop** : Installation via MSI
2. **Application Web** : Déploiement sur IIS ou Docker
3. **Hybride** : Desktop + API locale

#### Installation
- .NET 8 Runtime requis
- Base de données SQLite incluse (pas d'installation nécessaire)
- Mises à jour via ClickOnce ou Squirrel

### Migration depuis OPAGA

#### Points d'Attention
- **Rôles utilisateurs** : Implémentation native sans dépendance
- **Templates PDF** : Conversion des templates PHP vers .NET
- **Base de données** : Migration des données WordPress vers SQL
- **Fonctionnalités** : Maintien de la logique métier Qualiopi

#### Avantages de l'Architecture .NET
- Performance supérieure
- Déploiement simplifié sur Windows
- Intégration native avec l'écosystème Microsoft
- Maintenance à long terme assurée

### Roadmap de Développement

#### Phase 1 - Core (2-3 mois)
- Architecture de base
- Gestion des formations
- Gestion des sessions
- Base de données

#### Phase 2 - Gestion Client (1-2 mois)
- Module clients
- Module stagiaires
- Documents de base

#### Phase 3 - Qualiopi (2 mois)
- Module conformité
- Tableau de bord
- Collecte preuves

#### Phase 4 - Advanced (1-2 mois)
- BPF automatisé
- Export/Import
- Multi-utilisateurs

### Coûts et Ressources

#### Développement
- 1 développeur .NET senior (6-8 mois)
- 1 expert formation/Qualiopi (consultant)

#### Infrastructure
- Azure DevOps ou GitHub Actions (CI/CD)
- Azure SQL Database (optionnel)
- Certificat de signature de code

#### Maintenance
- Mises à jour .NET annuelles
- Évolutions réglementaires Qualiopi
- Support utilisateur
