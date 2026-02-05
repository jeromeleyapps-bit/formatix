using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;
using QRCoder;

namespace FormationManager.Controllers
{
    [AllowAnonymous]
    public class InscriptionController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly IQualiopiAutoProofService _qualiopiAutoProofService;
        private readonly IInscriptionLinkService _inscriptionLinkService;

        private static readonly string[] StatutsOuverts = { "Programmée", "En cours" };

        public InscriptionController(
            FormationDbContext context,
            IQualiopiAutoProofService qualiopiAutoProofService,
            IInscriptionLinkService inscriptionLinkService)
        {
            _context = context;
            _qualiopiAutoProofService = qualiopiAutoProofService;
            _inscriptionLinkService = inscriptionLinkService;
        }

        [HttpGet]
        public async Task<IActionResult> Session(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .Include(s => s.Stagiaires)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return View("Erreur", (object)"Cette session n'existe pas.");
            }

            var (ok, message) = await CanAcceptInscriptionAsync(session);
            if (!ok)
            {
                return View("Erreur", (object)message);
            }

            ViewBag.Session = session;
            ViewBag.PlacesRestantes = session.NombreMaxStagiaires - session.Stagiaires!.Count;
            return View("Formulaire");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inscription(
            int sessionId,
            string nom,
            string prenom,
            string? email,
            string? telephone,
            string? poste)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Stagiaires)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                return View("Erreur", (object)"Cette session n'existe pas.");
            }

            var (ok, message) = await CanAcceptInscriptionAsync(session);
            if (!ok)
            {
                return View("Erreur", (object)message);
            }

            nom = (nom ?? "").Trim();
            prenom = (prenom ?? "").Trim();
            void SetFormBag()
            {
                ViewBag.Session = session;
                ViewBag.PlacesRestantes = session.NombreMaxStagiaires - session.Stagiaires!.Count;
                ViewBag.Nom = nom;
                ViewBag.Prenom = prenom;
                ViewBag.Email = email ?? "";
                ViewBag.Telephone = telephone ?? "";
                ViewBag.Poste = poste ?? "";
            }

            if (string.IsNullOrWhiteSpace(nom))
            {
                SetFormBag();
                ViewBag.Erreur = "Le nom est requis.";
                return View("Formulaire");
            }
            if (string.IsNullOrWhiteSpace(prenom))
            {
                SetFormBag();
                ViewBag.Erreur = "Le prénom est requis.";
                return View("Formulaire");
            }

            email = (email ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var dejaInscrit = await _context.Stagiaires
                    .AnyAsync(s => s.SessionId == sessionId && s.Email == email);
                if (dejaInscrit)
                {
                    SetFormBag();
                    ViewBag.Erreur = "Un stagiaire avec cette adresse email est déjà inscrit à cette session.";
                    return View("Formulaire");
                }
            }

            var siteId = session.SiteId ?? string.Empty;
            var independant = await _context.Clients
                .FirstOrDefaultAsync(c =>
                    (c.Nom == "Indépendant" || c.Nom == "Particulier") && c.SiteId == siteId);

            if (independant == null)
            {
                independant = new Client
                {
                    Nom = "Indépendant",
                    TypeClient = TypeClient.Particulier,
                    SiteId = siteId,
                    DateCreation = DateTime.Now,
                    DateModification = DateTime.Now,
                    CreePar = "inscription-en-ligne",
                    ModifiePar = "inscription-en-ligne"
                };
                _context.Clients.Add(independant);
                await _context.SaveChangesAsync();
            }

            var stagiaire = new Stagiaire
            {
                ClientId = independant.Id,
                SessionId = sessionId,
                Nom = nom,
                Prenom = prenom,
                Email = email ?? string.Empty,
                Telephone = (telephone ?? "").Trim(),
                Poste = (poste ?? "").Trim(),
                StatutInscription = "Inscrit",
                SiteId = siteId,
                DateCreation = DateTime.Now,
                DateModification = DateTime.Now,
                CreePar = "inscription-en-ligne",
                ModifiePar = "inscription-en-ligne"
            };

            _context.Stagiaires.Add(stagiaire);
            await _context.SaveChangesAsync();

            await _qualiopiAutoProofService.AutoCreatePreuvesForStagiaireAsync(stagiaire.Id);

            TempData["InscriptionTitre"] = session.Formation?.Titre;
            TempData["InscriptionLieu"] = session.Lieu;
            TempData["InscriptionDateDebut"] = session.DateDebut.ToString("dd/MM/yyyy");
            TempData["InscriptionDateFin"] = session.DateFin.ToString("dd/MM/yyyy");
            return RedirectToAction(nameof(Merci));
        }

        [HttpGet]
        public IActionResult Merci()
        {
            ViewBag.Titre = TempData["InscriptionTitre"];
            ViewBag.Lieu = TempData["InscriptionLieu"];
            ViewBag.DateDebut = TempData["InscriptionDateDebut"];
            ViewBag.DateFin = TempData["InscriptionDateFin"];
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> QR(int id)
        {
            var session = await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
                return NotFound();

            var absUrl = _inscriptionLinkService.GetInscriptionUrl(id);

            using var qr = new QRCodeGenerator();
            var data = qr.CreateQrCode(absUrl, QRCodeGenerator.ECCLevel.Q);
            using var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(20);
            return File(bytes, "image/png");
        }

        private async Task<(bool Ok, string Message)> CanAcceptInscriptionAsync(Models.Session session)
        {
            // Ne plus bloquer sur EstPublique : on veut pouvoir utiliser un lien direct
            // même pour des sessions "privées". Le paramètre EstPublique est plutôt
            // utilisé pour l'affichage dans les catalogues / listes.
            if (!StatutsOuverts.Contains(session.Statut ?? ""))
                return (false, "Cette session n'accepte plus d'inscriptions (statut : " + (session.Statut ?? "—") + ").");

            var count = session.Stagiaires?.Count ?? await _context.Stagiaires.CountAsync(s => s.SessionId == session.Id);
            if (count >= session.NombreMaxStagiaires)
                return (false, "Cette session est complète.");

            return (true, "");
        }
    }
}
