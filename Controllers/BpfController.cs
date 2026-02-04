using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormationManager.Services;

namespace FormationManager.Controllers
{
    [Authorize]
    public class BpfController : Controller
    {
        private readonly IBPFService _bpfService;
        private readonly IExportService _exportService;

        public BpfController(IBPFService bpfService, IExportService exportService)
        {
            _bpfService = bpfService;
            _exportService = exportService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Download(DateTime? debut, DateTime? fin)
        {
            var dateFin = fin ?? DateTime.Today;
            var dateDebut = debut ?? dateFin.AddYears(-1);

            if (dateDebut > dateFin)
            {
                return BadRequest("La date de début ne peut pas être après la date de fin.");
            }

            var pdf = await _bpfService.GenerateBPFAsync(dateDebut, dateFin);
            var fileName = $"bpf_{dateDebut:yyyyMMdd}_{dateFin:yyyyMMdd}.pdf";

            return File(pdf, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadJson(DateTime? debut, DateTime? fin)
        {
            var dateFin = fin ?? DateTime.Today;
            var dateDebut = debut ?? dateFin.AddYears(-1);

            if (dateDebut > dateFin)
            {
                return BadRequest("La date de début ne peut pas être après la date de fin.");
            }

            var json = await _exportService.ExportBPFJSONAsync(dateDebut, dateFin);
            var fileName = $"bpf_{dateDebut:yyyyMMdd}_{dateFin:yyyyMMdd}.json";

            return File(json, "application/json", fileName);
        }
    }
}
