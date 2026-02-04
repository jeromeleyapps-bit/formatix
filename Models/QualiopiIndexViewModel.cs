using System.Collections.Generic;

namespace FormationManager.Models
{
    public class QualiopiIndexViewModel
    {
        public List<IndicateurQualiopi> Indicateurs { get; set; } = new();
        public List<QualiopiCritereStat> Criteres { get; set; } = new();
        public List<string> IndicateursValides { get; set; } = new();
        public int TotalIndicateurs { get; set; }
        public int IndicateursValidesCount { get; set; }
        public decimal TauxValidite { get; set; }
    }

    public class QualiopiCritereStat
    {
        public int Critere { get; set; }
        public int TotalIndicateurs { get; set; }
        public int IndicateursValides { get; set; }
        public decimal TauxValidite { get; set; }
    }
}
