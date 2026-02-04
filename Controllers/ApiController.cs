using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Models;
using FormationManager.Services;
using FormationManager.Data;

namespace FormationManager.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormationsController : ControllerBase
    {
        private readonly FormationDbContext _context;
        private readonly IDocumentService _documentService;

        public FormationsController(FormationDbContext context, IDocumentService documentService)
        {
            _context = context;
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Formation>>> GetFormations()
        {
            return await _context.Formations
                .OrderBy(f => f.Titre)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Formation>> GetFormation(int id)
        {
            var formation = await _context.Formations.FindAsync(id);

            if (formation == null)
                return NotFound();

            return formation;
        }

        [HttpPost]
        public async Task<ActionResult<Formation>> CreateFormation(Formation formation)
        {
            formation.DateCreation = DateTime.Now;
            formation.DateModification = DateTime.Now;

            _context.Formations.Add(formation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFormation), new { id = formation.Id }, formation);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFormation(int id, Formation formation)
        {
            if (id != formation.Id)
                return BadRequest();

            formation.DateModification = DateTime.Now;

            _context.Entry(formation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FormationExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFormation(int id)
        {
            var formation = await _context.Formations.FindAsync(id);
            if (formation == null)
                return NotFound();

            _context.Formations.Remove(formation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/sessions")]
        public async Task<ActionResult<IEnumerable<Session>>> GetFormationSessions(int id)
        {
            var sessions = await _context.Sessions
                .Include(s => s.Formateur)
                .Where(s => s.FormationId == id)
                .OrderBy(s => s.DateDebut)
                .ToListAsync();

            return sessions;
        }

        private bool FormationExists(int id)
        {
            return _context.Formations.Any(e => e.Id == id);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly FormationDbContext _context;
        private readonly IDocumentService _documentService;

        public SessionsController(FormationDbContext context, IDocumentService documentService)
        {
            _context = context;
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
        {
            return await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .OrderBy(s => s.DateDebut)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Session>> GetSession(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Client)
                .Include(s => s.Stagiaires)
                    .ThenInclude(s => s.Client)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            return session;
        }

        [HttpPost]
        public async Task<ActionResult<Session>> CreateSession(Session session)
        {
            session.DateCreation = DateTime.Now;
            session.DateModification = DateTime.Now;

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSession(int id, Session session)
        {
            if (id != session.Id)
                return BadRequest();

            session.DateModification = DateTime.Now;

            _context.Entry(session).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SessionExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
                return NotFound();

            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/convention/{clientId}")]
        public async Task<IActionResult> GenerateConvention(int id, int clientId)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);

            var client = await _context.Clients.FindAsync(clientId);

            if (session == null || client == null)
                return NotFound();

            var pdf = _documentService.GenerateConvention(session, client);

            return File(pdf, "application/pdf", $"convention_{session.Id}_{client.Nom}.pdf");
        }

        [HttpGet("{id}/emargement")]
        public async Task<IActionResult> GenerateEmargement(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Stagiaires)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            var pdf = _documentService.GenerateEmargement(session);

            return File(pdf, "application/pdf", $"emargement_{session.Id}.pdf");
        }

        private bool SessionExists(int id)
        {
            return _context.Sessions.Any(e => e.Id == id);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly FormationDbContext _context;

        public ClientsController(FormationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetClients()
        {
            return await _context.Clients
                .OrderBy(c => c.Nom)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
                return NotFound();

            return client;
        }

        [HttpPost]
        public async Task<ActionResult<Client>> CreateClient(Client client)
        {
            client.DateCreation = DateTime.Now;
            client.DateModification = DateTime.Now;

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClient(int id, Client client)
        {
            if (id != client.Id)
                return BadRequest();

            client.DateModification = DateTime.Now;

            _context.Entry(client).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/stagiaires")]
        public async Task<ActionResult<IEnumerable<Stagiaire>>> GetClientStagiaires(int id)
        {
            var stagiaires = await _context.Stagiaires
                .Where(s => s.ClientId == id)
                .OrderBy(s => s.Nom)
                .ToListAsync();

            return stagiaires;
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.Id == id);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class StagiairesController : ControllerBase
    {
        private readonly FormationDbContext _context;
        private readonly IDocumentService _documentService;

        public StagiairesController(FormationDbContext context, IDocumentService documentService)
        {
            _context = context;
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Stagiaire>>> GetStagiaires()
        {
            return await _context.Stagiaires
                .Include(s => s.Client)
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .OrderBy(s => s.Nom)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Stagiaire>> GetStagiaire(int id)
        {
            var stagiaire = await _context.Stagiaires
                .Include(s => s.Client)
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stagiaire == null)
                return NotFound();

            return stagiaire;
        }

        [HttpPost]
        public async Task<ActionResult<Stagiaire>> CreateStagiaire(Stagiaire stagiaire)
        {
            stagiaire.DateCreation = DateTime.Now;
            stagiaire.DateModification = DateTime.Now;

            _context.Stagiaires.Add(stagiaire);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStagiaire), new { id = stagiaire.Id }, stagiaire);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStagiaire(int id, Stagiaire stagiaire)
        {
            if (id != stagiaire.Id)
                return BadRequest();

            stagiaire.DateModification = DateTime.Now;

            _context.Entry(stagiaire).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StagiaireExists(id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStagiaire(int id)
        {
            var stagiaire = await _context.Stagiaires.FindAsync(id);
            if (stagiaire == null)
                return NotFound();

            _context.Stagiaires.Remove(stagiaire);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/attestation")]
        public async Task<IActionResult> GenerateAttestation(int id)
        {
            var stagiaire = await _context.Stagiaires
                .Include(s => s.Client)
                .Include(s => s.Session)
                    .ThenInclude(s => s!.Formation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stagiaire?.Session == null)
                return NotFound();

            var pdf = _documentService.GenerateAttestation(stagiaire, stagiaire.Session);

            return File(pdf, "application/pdf", $"attestation_{stagiaire.Nom}_{stagiaire.Prenom}.pdf");
        }

        [HttpGet("{id}/evaluation")]
        public async Task<IActionResult> GenerateEvaluation(int id)
        {
            var stagiaire = await _context.Stagiaires
                .Include(s => s.Client)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stagiaire == null)
                return NotFound();

            var pdf = _documentService.GenerateEvaluation(stagiaire);

            return File(pdf, "application/pdf", $"evaluation_{stagiaire.Nom}_{stagiaire.Prenom}.pdf");
        }

        private bool StagiaireExists(int id)
        {
            return _context.Stagiaires.Any(e => e.Id == id);
        }
    }
}
