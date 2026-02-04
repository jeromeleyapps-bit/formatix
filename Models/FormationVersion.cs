using System.ComponentModel.DataAnnotations;

namespace FormationManager.Models
{
    /// <summary>
    /// Historique des versions d'une formation (traçabilité Qualiopi)
    /// </summary>
    public class FormationVersion : BaseEntity
    {
        public int FormationId { get; set; }
        public virtual Formation Formation { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string NumeroVersion { get; set; } = string.Empty; // Ex: "v1.0", "v2.1"

        [Required]
        public DateTime DateVersion { get; set; }

        [StringLength(200)]
        public string RaisonModification { get; set; } = string.Empty; // Ex: "Amélioration du programme", "Mise à jour réglementaire"

        // Contenu de la formation à cette version
        public string Titre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Programme { get; set; } = string.Empty;
        public string Prerequis { get; set; } = string.Empty;
        public string ModalitesPedagogiques { get; set; } = string.Empty;
        public string ModalitesEvaluation { get; set; } = string.Empty;
        public string ReferencesQualiopi { get; set; } = string.Empty;
        public decimal DureeHeures { get; set; }
        public decimal PrixIndicatif { get; set; }

        [StringLength(200)]
        public string ModifiePar { get; set; } = string.Empty; // Utilisateur qui a fait la modification
    }
}
