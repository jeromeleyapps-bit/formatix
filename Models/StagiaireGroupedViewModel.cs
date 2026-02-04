using FormationManager.Models;

namespace FormationManager.Models
{
    public class StagiaireGroupedViewModel
    {
        public Stagiaire StagiairePrincipal { get; set; } = null!;
        public List<Stagiaire> ToutesLesSessions { get; set; } = new List<Stagiaire>();
        public int NombreFormations => ToutesLesSessions.Count;
    }
}
