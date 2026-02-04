using System.ComponentModel.DataAnnotations;

namespace FormationManager.Models
{
    public class SettingsViewModel
    {
        public List<Site> Sites { get; set; } = new();
        public List<Utilisateur> Users { get; set; } = new();
        public string CurrentSiteId { get; set; } = string.Empty;
        public string CentralUrl { get; set; } = string.Empty;
        public int SyncIntervalMinutes { get; set; }
        public OrganizationSettings Organization { get; set; } = new();
        public QualiopiSettings Qualiopi { get; set; } = new();
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Nom { get; set; } = string.Empty;

        [Required]
        public string Prenom { get; set; } = string.Empty;

        [Required]
        public RoleUtilisateur Role { get; set; }

        public string SiteId { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }

    public class OrganizationSettings
    {
        public string BaseUrlInscription { get; set; } = string.Empty;
        public string NomOrganisme { get; set; } = string.Empty;
        public string SIRET { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string CodePostal { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
    }

    public class QualiopiSettings
    {
        public bool Certification { get; set; }
        public string NumeroCertification { get; set; } = string.Empty;
        public string DateCertification { get; set; } = string.Empty;
        public string DateProchaineEvaluation { get; set; } = string.Empty;
        public string OrganismeCertificateur { get; set; } = string.Empty;
    }
}
