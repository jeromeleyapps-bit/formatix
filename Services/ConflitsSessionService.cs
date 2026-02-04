using FormationManager.Data;
using FormationManager.Models;
using Microsoft.EntityFrameworkCore;

namespace FormationManager.Services
{
    public interface IConflitsSessionService
    {
        Task<List<(Session Session, string Raison)>> GetConflitsAsync(Session session, int? excludeSessionId, CancellationToken ct = default);
    }

    public class ConflitsSessionService : IConflitsSessionService
    {
        private readonly FormationDbContext _context;

        public ConflitsSessionService(FormationDbContext context)
        {
            _context = context;
        }

        public async Task<List<(Session Session, string Raison)>> GetConflitsAsync(Session session, int? excludeSessionId, CancellationToken ct = default)
        {
            var deb = session.DateDebut;
            var fin = session.DateFin;
            var formateurId = session.FormateurId;
            var salleId = session.SalleId;

            var query = _context.Sessions
                .Include(s => s.Formation)
                .Where(s => s.Id != excludeSessionId)
                .Where(s => s.DateDebut < fin && s.DateFin > deb);

            var others = await query.ToListAsync(ct);
            var conflits = new List<(Session, string)>();

            foreach (var s in others)
            {
                if (s.FormateurId == formateurId)
                    conflits.Add((s, "Même formateur"));
                else if (salleId.HasValue && s.SalleId == salleId.Value)
                    conflits.Add((s, "Même salle"));
            }

            return conflits;
        }
    }
}
