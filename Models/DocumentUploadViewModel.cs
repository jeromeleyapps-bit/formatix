using System.ComponentModel.DataAnnotations;

namespace FormationManager.Models
{
    public class DocumentUploadViewModel
    {
        [Required]
        public IFormFile? File { get; set; }

        public int? SessionId { get; set; }

        [Required]
        public TypeDocument TypeDocument { get; set; }

        public string SiteId { get; set; } = string.Empty;

        /// <summary>
        /// Indicateurs Qualiopi que ce document valide (checklist à l'import).
        /// Une preuve est créée par indicateur coché si une session est liée.
        /// </summary>
        public List<int> IndicateurIds { get; set; } = new();
    }
}
