using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FormationManager.Data;
using FormationManager.Models;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class FormationsAuditController : Controller
    {
        private readonly FormationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ISiteContext _siteContext;

        public FormationsAuditController(
            FormationDbContext context,
            IWebHostEnvironment environment,
            ISiteContext siteContext)
        {
            _context = context;
            _environment = environment;
            _siteContext = siteContext;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocumentAudit(int formationId, IFormFile file, string nom, string typeDocument, string description, string critereQualiopi)
        {
            var formation = await _context.Formations.FindAsync(formationId);
            if (formation == null || (!_siteContext.IsAdmin && !string.IsNullOrEmpty(formation.SiteId) && formation.SiteId != _siteContext.CurrentSiteId))
                return NotFound();

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Veuillez sélectionner un fichier.";
                return RedirectToAction("Details", "Formations", new { id = formationId });
            }

            var uploadsPath = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "audit", formationId.ToString());
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var documentAudit = new DocumentAudit
            {
                FormationId = formationId,
                Nom = nom,
                TypeDocument = typeDocument,
                Description = description,
                CheminFichier = $"/uploads/audit/{formationId}/{fileName}",
                CritereQualiopi = critereQualiopi,
                Statut = "Actif",
                SiteId = formation.SiteId,
                DateCreation = DateTime.Now,
                DateModification = DateTime.Now,
                CreePar = User.Identity?.Name ?? "system",
                ModifiePar = User.Identity?.Name ?? "system"
            };

            _context.DocumentsAudit.Add(documentAudit);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Document d'audit ajouté avec succès.";
            return RedirectToAction("Details", "Formations", new { id = formationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocumentAudit(int id)
        {
            var document = await _context.DocumentsAudit
                .Include(d => d.Formation)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return NotFound();

            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(document.SiteId) && document.SiteId != _siteContext.CurrentSiteId)
                return Forbid();

            var formationId = document.FormationId;

            // Supprimer le fichier physique
            if (!string.IsNullOrEmpty(document.CheminFichier))
            {
                var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", document.CheminFichier.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.DocumentsAudit.Remove(document);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Document d'audit supprimé.";
            return RedirectToAction("Details", "Formations", new { id = formationId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocumentAudit(int id)
        {
            var document = await _context.DocumentsAudit
                .Include(d => d.Formation)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return NotFound();

            if (!_siteContext.IsAdmin && !string.IsNullOrEmpty(document.SiteId) && document.SiteId != _siteContext.CurrentSiteId)
                return Forbid();

            var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", document.CheminFichier.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", Path.GetFileName(document.CheminFichier));
        }
    }
}
