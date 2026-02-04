namespace FormationManager.Models
{
    public class AdminDashboardViewModel
    {
        public List<SiteDashboardItem> Sites { get; set; } = new();
        public List<TrainerDashboardItem> Trainers { get; set; } = new();
        public List<FormationFollowupItem> FormationsFollowup { get; set; } = new();
        public List<SessionRiskItem> SessionsAtRisk { get; set; } = new();
        public List<PendingDocumentItem> PendingDocuments { get; set; } = new();
        public List<PendingPreuveItem> PendingPreuves { get; set; } = new();
        public int TotalFormations { get; set; }
        public int TotalSessions { get; set; }
        public int TotalStagiaires { get; set; }
        public int TotalDocuments { get; set; }
        public int TotalPreuves { get; set; }
        public int SessionsMissingDocuments { get; set; }
        public int SessionsMissingPreuves { get; set; }
        public int PendingDocumentsCount { get; set; }
        public int PendingPreuvesCount { get; set; }
        /// <summary>Nombre de flux RSS configurés (veille critère 6).</summary>
        public int VeilleFluxCount { get; set; }
        /// <summary>Nombre d'actualités RSS récentes.</summary>
        public int VeilleActualitesCount { get; set; }
        /// <summary>Nombre de validations de lecture (veille) sur la période.</summary>
        public int VeilleValidationsCount { get; set; }
        /// <summary>Statistiques Qualiopi par critère (tous sites confondus).</summary>
        public List<QualiopiCritereStat> QualiopiCriteres { get; set; } = new();
        public int QualiopiTotalIndicateurs { get; set; }
        public int QualiopiIndicateursValides { get; set; }
        public decimal QualiopiTauxGlobal { get; set; }
    }

    public class SiteDashboardItem
    {
        public string SiteId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public int Formations { get; set; }
        public int Sessions { get; set; }
        public int Stagiaires { get; set; }
        public int Documents { get; set; }
        public int Preuves { get; set; }
    }

    public class TrainerDashboardItem
    {
        public int FormateurId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int Stagiaires { get; set; }
        public int Documents { get; set; }
        public int Preuves { get; set; }
        public int SessionsMissingDocuments { get; set; }
        public int SessionsMissingPreuves { get; set; }
        public DateTime? DerniereSession { get; set; }
    }

    public class SessionRiskItem
    {
        public int SessionId { get; set; }
        public string FormationTitre { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public DateTime DateDebut { get; set; }
        public bool MissingDocuments { get; set; }
        public bool MissingPreuves { get; set; }
    }

    public class FormationFollowupItem
    {
        public int FormationId { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int SessionsWithoutEvaluationDocs { get; set; }
        public int StagiairesWithoutEvaluation { get; set; }
        public DateTime? DerniereSession { get; set; }
    }

    public class PendingDocumentItem
    {
        public int Id { get; set; }
        public string TypeDocument { get; set; } = string.Empty;
        public string SessionTitle { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
    }

    public class PendingPreuveItem
    {
        public int Id { get; set; }
        public string Indicateur { get; set; } = string.Empty;
        public string SessionTitle { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
    }
}
