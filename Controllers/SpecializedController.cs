using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Models;
using FormationManager.Services;
using FormationManager.Data;

namespace FormationManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QualiopiController : ControllerBase
    {
        private readonly IQualiopiService _qualiopiService;

        public QualiopiController(IQualiopiService qualiopiService)
        {
            _qualiopiService = qualiopiService;
        }

        [HttpGet("indicateurs")]
        public async Task<ActionResult<IEnumerable<IndicateurQualiopi>>> GetIndicateurs()
        {
            var indicateurs = await _qualiopiService.GetAllIndicateursAsync();
            return Ok(indicateurs);
        }

        [HttpGet("sessions/{sessionId}/preuves")]
        public async Task<ActionResult<IEnumerable<PreuveQualiopi>>> GetPreuvesBySession(int sessionId)
        {
            var preuves = await _qualiopiService.GetPreuvesBySessionAsync(sessionId);
            return Ok(preuves);
        }

        [HttpGet("sessions/{sessionId}/conformite")]
        public async Task<ActionResult<Dictionary<int, bool>>> GetConformiteBySession(int sessionId)
        {
            var conformite = await _qualiopiService.GetConformiteBySessionAsync(sessionId);
            return Ok(conformite);
        }

        [HttpPost("preuves")]
        public async Task<ActionResult<PreuveQualiopi>> AjouterPreuve(PreuveQualiopi preuve)
        {
            var nouvellePreuve = await _qualiopiService.AjouterPreuveAsync(preuve);
            return CreatedAtAction(nameof(AjouterPreuve), new { id = nouvellePreuve.Id }, nouvellePreuve);
        }

        [HttpPost("preuves/{preuveId}/valider")]
        public async Task<IActionResult> ValiderPreuve(int preuveId, [FromBody] string commentaire)
        {
            var resultat = await _qualiopiService.ValiderPreuveAsync(preuveId, commentaire);
            if (!resultat)
                return NotFound();

            return NoContent();
        }

        [HttpGet("sessions/{sessionId}/rapport")]
        public async Task<IActionResult> GenerateRapportConformite(int sessionId)
        {
            var pdf = await _qualiopiService.GenerateRapportConformiteAsync(sessionId);
            return File(pdf, "application/pdf", $"rapport_qualiopi_session_{sessionId}.pdf");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class BPFController : ControllerBase
    {
        private readonly IBPFService _bpfService;

        public BPFController(IBPFService bpfService)
        {
            _bpfService = bpfService;
        }

        [HttpGet("statistiques")]
        public async Task<ActionResult> GetStatistiques([FromQuery] DateTime debut, [FromQuery] DateTime fin)
        {
            var statistiques = await _bpfService.GetStatistiquesAsync(debut, fin);
            return Ok(statistiques);
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<IEnumerable<Session>>> GetSessionsPeriode([FromQuery] DateTime debut, [FromQuery] DateTime fin)
        {
            var sessions = await _bpfService.GetSessionsPeriodeAsync(debut, fin);
            return Ok(sessions);
        }

        [HttpGet("export")]
        public async Task<IActionResult> GenerateBPF([FromQuery] DateTime debut, [FromQuery] DateTime fin)
        {
            var pdf = await _bpfService.GenerateBPFAsync(debut, fin);
            return File(pdf, "application/pdf", $"bpf_{debut:yyyyMMdd}_{fin:yyyyMMdd}.pdf");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;

        public ExportController(IExportService exportService)
        {
            _exportService = exportService;
        }

        [HttpGet("sessions/csv")]
        public async Task<IActionResult> ExportSessionsCSV([FromQuery] DateTime debut, [FromQuery] DateTime fin)
        {
            var csv = await _exportService.ExportSessionsCSVAsync(debut, fin);
            return File(csv, "text/csv", $"sessions_{debut:yyyyMMdd}_{fin:yyyyMMdd}.csv");
        }

        [HttpGet("stagiaires/csv")]
        public async Task<IActionResult> ExportStagiairesCSV([FromQuery] DateTime debut, [FromQuery] DateTime fin)
        {
            var csv = await _exportService.ExportStagiairesCSVAsync(debut, fin);
            return File(csv, "text/csv", $"stagiaires_{debut:yyyyMMdd}_{fin:yyyyMMdd}.csv");
        }

        [HttpGet("bpf/json")]
        public async Task<IActionResult> ExportBPFJSON([FromQuery] DateTime debut, [FromQuery] DateTime fin)
        {
            var json = await _exportService.ExportBPFJSONAsync(debut, fin);
            return File(json, "application/json", $"bpf_{debut:yyyyMMdd}_{fin:yyyyMMdd}.json");
        }

        [HttpGet("qualiopi/json")]
        public async Task<IActionResult> ExportQualiopiJSON([FromQuery] int sessionId)
        {
            var json = await _exportService.ExportQualiopiJSONAsync(sessionId);
            return File(json, "application/json", $"qualiopi_session_{sessionId}.json");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UtilisateursController : ControllerBase
    {
        private readonly FormationDbContext _context;

        public UtilisateursController(FormationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Utilisateur>>> GetUtilisateurs()
        {
            return await _context.Utilisateurs
                .OrderBy(u => u.Nom)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Utilisateur>> GetUtilisateur(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);

            if (utilisateur == null)
                return NotFound();

            return utilisateur;
        }

        [HttpPost]
        public async Task<ActionResult<Utilisateur>> CreateUtilisateur(Utilisateur utilisateur)
        {
            utilisateur.DateCreation = DateTime.Now;
            utilisateur.DateModification = DateTime.Now;

            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUtilisateur), new { id = utilisateur.Id }, utilisateur);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUtilisateur(int id, Utilisateur utilisateur)
        {
            if (id != utilisateur.Id)
                return BadRequest();

            utilisateur.DateModification = DateTime.Now;

            _context.Entry(utilisateur).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UtilisateurExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUtilisateur(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
                return NotFound();

            _context.Utilisateurs.Remove(utilisateur);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/desactiver")]
        public async Task<IActionResult> DesactiverUtilisateur(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
                return NotFound();

            utilisateur.Actif = false;
            utilisateur.DateModification = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id}/activer")]
        public async Task<IActionResult> ActiverUtilisateur(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
                return NotFound();

            utilisateur.Actif = true;
            utilisateur.DateModification = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UtilisateurExists(int id)
        {
            return _context.Utilisateurs.Any(e => e.Id == id);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly FormationDbContext _context;

        public DashboardController(FormationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetStats()
        {
            var now = DateTime.Now;
            var anneeEnCours = new DateTime(now.Year, 1, 1);
            var moisEnCours = new DateTime(now.Year, now.Month, 1);

            var stats = new
            {
                totalFormations = await _context.Formations.CountAsync(),
                totalSessions = await _context.Sessions.CountAsync(),
                totalClients = await _context.Clients.CountAsync(),
                totalStagiaires = await _context.Stagiaires.CountAsync(),
                sessionsAnneeEnCours = await _context.Sessions
                    .Where(s => s.DateDebut >= anneeEnCours)
                    .CountAsync(),
                sessionsMoisEnCours = await _context.Sessions
                    .Where(s => s.DateDebut >= moisEnCours)
                    .CountAsync(),
                sessionsAVenir = await _context.Sessions
                    .Where(s => s.DateDebut > now)
                    .CountAsync(),
                sessionsEnCours = await _context.Sessions
                    .Where(s => s.DateDebut <= now && s.DateFin >= now)
                    .CountAsync(),
                chiffreAffairesAnnee = await _context.SessionClients
                    .Where(sc => sc.Session.DateDebut >= anneeEnCours)
                    .SumAsync(sc => sc.TarifNegocie * sc.NombrePlaces),
                documentsEnAttente = await _context.Documents
                    .Where(d => d.StatutValidation == "En attente")
                    .CountAsync(),
                preuvesQualiopiEnAttente = await _context.PreuvesQualiopi
                    .Where(p => !p.EstValide)
                            .CountAsync()
            };

            return Ok(stats);
        }

        [HttpGet("sessions-recentes")]
        public async Task<ActionResult> GetSessionsRecentes()
        {
            var sessions = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .OrderByDescending(s => s.DateCreation)
                .Take(10)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("stagiaires-recentes")]
        public async Task<ActionResult> GetStagiairesRecents()
        {
            var stagiaires = await _context.Stagiaires
                .Include(s => s.Client)
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .OrderByDescending(s => s.DateCreation)
                .Take(10)
                .ToListAsync();

            return Ok(stagiaires);
        }

        [HttpGet("documents-en-attente")]
        public async Task<ActionResult> GetDocumentsEnAttente()
        {
            var documents = await _context.Documents
                .Include(d => d.Session)
                    .ThenInclude(s => s!.Formation)
                .Include(d => d.Client)
                .Include(d => d.Stagiaire)
                .Where(d => d.StatutValidation == "En attente")
                .OrderByDescending(d => d.DateCreation)
                .Take(10)
                .ToListAsync();

            return Ok(documents);
        }
    }
}
