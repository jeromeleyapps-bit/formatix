using Microsoft.EntityFrameworkCore;
using FormationManager.Models;
using FormationManager.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfDocument = QuestPDF.Fluent.Document;

namespace FormationManager.Services
{
    public interface IQualiopiService
    {
        Task<List<IndicateurQualiopi>> GetAllIndicateursAsync();
        Task<List<PreuveQualiopi>> GetPreuvesBySessionAsync(int sessionId);
        Task<Dictionary<int, bool>> GetConformiteBySessionAsync(int sessionId);
        Task<PreuveQualiopi> AjouterPreuveAsync(PreuveQualiopi preuve);
        Task<bool> ValiderPreuveAsync(int preuveId, string commentaire);
        Task<byte[]> GenerateRapportConformiteAsync(int sessionId);
    }

    public class QualiopiService : IQualiopiService
    {
        private readonly FormationDbContext _context;
        private readonly ILogger<QualiopiService> _logger;
        private readonly IOrganizationService _organizationService;

        // Logo Qualiopi fourni par l'utilisateur
        private const string QualiopiLogoPath = @"C:\AI\Opagax\logoqualiopi.png";

        public QualiopiService(FormationDbContext context, ILogger<QualiopiService> logger, IOrganizationService organizationService)
        {
            _context = context;
            _logger = logger;
            _organizationService = organizationService;
        }

        public async Task<List<IndicateurQualiopi>> GetAllIndicateursAsync()
        {
            return await _context.IndicateursQualiopi
                .OrderBy(i => i.Critere)
                .ThenBy(i => i.CodeIndicateur)
                .ToListAsync();
        }

        public async Task<List<PreuveQualiopi>> GetPreuvesBySessionAsync(int sessionId)
        {
            return await _context.PreuvesQualiopi
                .Include(p => p.Indicateur)
                .Where(p => p.SessionId == sessionId)
                .OrderBy(p => p.Indicateur.Critere)
                .ThenBy(p => p.Indicateur.CodeIndicateur)
                .ToListAsync();
        }

        public async Task<Dictionary<int, bool>> GetConformiteBySessionAsync(int sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return new Dictionary<int, bool>();
            }

            var indicateurs = await _context.IndicateursQualiopi
                .Where(i => string.IsNullOrEmpty(i.SiteId) || i.SiteId == session.SiteId)
                .ToListAsync();
            var preuves = await _context.PreuvesQualiopi
                .Where(p => p.SessionId == sessionId && p.EstValide)
                .ToListAsync();

            // Pour le critère 6, inclure aussi les VeilleValidation (veille RSS)
            var validationsVeille = await _context.VeilleValidations
                .Include(v => v.Indicateur)
                .Where(v => v.SiteId == session.SiteId && v.Indicateur.Critere == 6)
                .ToListAsync();

            var conformite = new Dictionary<int, bool>();

            foreach (var critere in indicateurs.GroupBy(i => i.Critere))
            {
                var indicateursCritere = critere.ToList();
                var preuvesCritere = preuves.Where(p => indicateursCritere.Any(i => i.Id == p.IndicateurQualiopiId)).ToList();
                
                // Pour le critère 6, ajouter les validations veille
                if (critere.Key == 6)
                {
                    var validationsCritere6 = validationsVeille
                        .Where(v => indicateursCritere.Any(i => i.Id == v.IndicateurQualiopiId))
                        .ToList();
                    // Un indicateur du critère 6 est couvert s'il a une preuve session OU une validation veille
                    conformite[critere.Key] = indicateursCritere.All(i => 
                        preuvesCritere.Any(p => p.IndicateurQualiopiId == i.Id) ||
                        validationsCritere6.Any(v => v.IndicateurQualiopiId == i.Id));
                }
                else
                {
                    // Un critère est conforme si tous les indicateurs requis ont des preuves valides
                    conformite[critere.Key] = indicateursCritere.All(i => 
                        preuvesCritere.Any(p => p.IndicateurQualiopiId == i.Id));
                }
            }

            return conformite;
        }

        public async Task<PreuveQualiopi> AjouterPreuveAsync(PreuveQualiopi preuve)
        {
            preuve.DateCreation = DateTime.Now;
            // Ne pas écraser EstValide : les preuves créées via l'interface (CreatePreuve) sont créées avec EstValide = true pour être prises en compte dans le taux

            _context.PreuvesQualiopi.Add(preuve);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Preuve ajoutée pour l'indicateur {preuve.IndicateurQualiopiId} de la session {preuve.SessionId}");

            return preuve;
        }

        public async Task<bool> ValiderPreuveAsync(int preuveId, string commentaire)
        {
            var preuve = await _context.PreuvesQualiopi.FindAsync(preuveId);
            if (preuve == null) return false;

            preuve.EstValide = true;
            preuve.DateValidation = DateTime.Now;
            preuve.CommentaireValidation = commentaire;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Preuve {preuveId} validée");

            return true;
        }

        public async Task<byte[]> GenerateRapportConformiteAsync(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                throw new ArgumentException("Session non trouvée");

            var conformite = await GetConformiteBySessionAsync(sessionId);
            var preuves = await GetPreuvesBySessionAsync(sessionId);
            
            // Récupérer les validations veille pour le critère 6
            var validationsVeille = await _context.VeilleValidations
                .Where(v => v.SiteId == session.SiteId)
                .Include(v => v.RssItem)
                .Include(v => v.Indicateur)
                .Where(v => v.Indicateur.Critere == 6)
                .OrderByDescending(v => v.ValidatedAt)
                .ToListAsync();

            // Génération du rapport PDF avec QuestPDF
            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Calibri));

                    page.Header()
                        .Text("RAPPORT DE CONFORMITÉ QUALIOPI")
                        .FontSize(18)
                        .FontColor(Colors.Blue.Medium)
                        .Bold()
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            // Informations session
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Session :").Bold();
                                row.RelativeItem().Text($"{session.Formation.Titre} ({session.DateDebut:dd/MM/yyyy} - {session.DateFin:dd/MM/yyyy})");
                            });

                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            column.Item().PaddingTop(10);

                            // Synthèse par critère
                            column.Item().Text("SYNTHÈSE PAR CRITÈRE").Bold().FontSize(14);
                            column.Item().PaddingTop(10);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(80); // Critère
                                    columns.ConstantColumn(100); // Conformité
                                    columns.RelativeColumn(); // Commentaires
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(cell => cell.Background(Colors.Grey.Lighten3)).Text("Critère").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Grey.Lighten3)).Text("Conformité").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Grey.Lighten3)).Text("Commentaires").Bold();
                                });

                                for (int critere = 1; critere <= 7; critere++)
                                {
                                    var estConforme = conformite.ContainsKey(critere) && conformite[critere];
                                    var couleur = estConforme ? Colors.Green.Medium : Colors.Red.Medium;

                                    table.Cell().Element(cell => cell.Background(Colors.Grey.Lighten4))
                                        .Text($"Critère {critere}");
                                    table.Cell().Element(cell => cell.Background(Colors.Grey.Lighten4))
                                        .Text(estConforme ? "✅ Conforme" : "❌ Non conforme").FontColor(couleur);
                                    table.Cell().Element(cell => cell.Background(Colors.Grey.Lighten4))
                                        .Text(estConforme ? "Tous les indicateurs sont couverts" : "Des preuves manquent");
                                }
                            });

                            column.Item().PaddingTop(20);

                            // Détail des preuves
                            column.Item().Text("DÉTAIL DES PREUVES").Bold().FontSize(14);
                            column.Item().PaddingTop(10);

                            var preuvesParIndicateur = preuves.GroupBy(p => p.Indicateur.CodeIndicateur);

                            foreach (var groupe in preuvesParIndicateur)
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"{groupe.Key} :").Bold();
                                    row.RelativeItem().Text($"{groupe.Count()} preuve(s)");
                                });

                                foreach (var preuve in groupe)
                                {
                                    var statut = preuve.EstValide ? "✅" : "⏳";
                                    column.Item().Row(row =>
                                    {
                                        row.ConstantItem(30).Text(statut);
                                        row.RelativeItem().Text(preuve.Titre).FontSize(9);
                                    });
                                }
                                column.Item().PaddingTop(5);
                            }

                            // Section Veille critère 6 (si validations existent)
                            if (validationsVeille.Any())
                            {
                                column.Item().PaddingTop(20);
                                column.Item().Text("VEILLE CRITÈRE 6 (VALIDATIONS RSS)").Bold().FontSize(14);
                                column.Item().PaddingTop(10);

                                var validationsParIndicateur = validationsVeille.GroupBy(v => v.Indicateur.CodeIndicateur);

                                foreach (var groupe in validationsParIndicateur)
                                {
                                    column.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text($"I{groupe.Key} :").Bold();
                                        row.RelativeItem().Text($"{groupe.Count()} validation(s)");
                                    });

                                    foreach (var validation in groupe)
                                    {
                                        column.Item().Row(row =>
                                        {
                                            row.ConstantItem(30).Text("📰");
                                            row.RelativeItem().Column(col =>
                                            {
                                                col.Item().Text(validation.RssItem?.Title ?? "Actualité RSS").FontSize(9);
                                                col.Item().Text($"Validé le {validation.ValidatedAt:dd/MM/yyyy} par {validation.ValidatedBy}").FontSize(8).FontColor(Colors.Grey.Medium);
                                            });
                                        });
                                    }
                                    column.Item().PaddingTop(5);
                                }
                            }
                        });

                    page.Footer()
                        .PaddingTop(5)
                        .Column(col =>
                        {
                            col.Spacing(2);

                            if (System.IO.File.Exists(QualiopiLogoPath))
                            {
                                col.Item()
                                    .AlignCenter()
                                    .Height(25)
                                    .Image(QualiopiLogoPath);
                            }

                            col.Item()
                                .AlignCenter()
                                .Text(_organizationService.GetOrganizationName())
                                .FontSize(9)
                                .SemiBold();

                            col.Item()
                                .AlignCenter()
                                .Text($"Généré le {DateTime.Now:dd/MM/yyyy HH:mm}")
                                .FontSize(8);
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
