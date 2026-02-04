using System.ComponentModel.DataAnnotations;

namespace FormationManager.Models
{
    /// <summary>
    /// Documents d'audit Qualiopi associés à une formation
    /// </summary>
    public class DocumentAudit : BaseEntity
    {
        public int FormationId { get; set; }
        public virtual Formation Formation { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Nom { get; set; } = string.Empty; // Ex: "Programme de formation v2.1", "Fiche descriptive"

        [Required]
        [StringLength(100)]
        public string TypeDocument { get; set; } = string.Empty; // Ex: "Programme", "Fiche descriptive", "Référentiel", etc.

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        public string CheminFichier { get; set; } = string.Empty; // Chemin vers le fichier uploadé

        [StringLength(50)]
        public string CritereQualiopi { get; set; } = string.Empty; // Critères Qualiopi concernés (ex: "1,2,3")

        public DateTime? DateValidite { get; set; } // Date de validité du document

        [StringLength(50)]
        public string Statut { get; set; } = "Actif"; // Actif, Archivé, Obsolète

        [StringLength(200)]
        public string Commentaire { get; set; } = string.Empty;
    }
}
