using Microsoft.EntityFrameworkCore;
using FormationManager.Models;
using FormationManager.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Configuration;
using PdfDocument = QuestPDF.Fluent.Document;

namespace FormationManager.Services
{
    public interface IBPFService
    {
        Task<byte[]> GenerateBPFAsync(DateTime debut, DateTime fin);
        Task<Dictionary<string, object>> GetStatistiquesAsync(DateTime debut, DateTime fin);
        Task<List<Session>> GetSessionsPeriodeAsync(DateTime debut, DateTime fin);
    }

    public class BPFService : IBPFService
    {
        private readonly FormationDbContext _context;
        private readonly ILogger<BPFService> _logger;
        private readonly ISiteContext _siteContext;
        private readonly IOrganizationService _organizationService;
        private readonly IConfiguration _configuration;

        // Logo Qualiopi fourni par l'utilisateur
        private const string QualiopiLogoPath = @"C:\AI\Opagax\logoqualiopi.png";

        public BPFService(FormationDbContext context, ILogger<BPFService> logger, ISiteContext siteContext, IOrganizationService organizationService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _siteContext = siteContext;
            _organizationService = organizationService;
            _configuration = configuration;
        }

        public async Task<byte[]> GenerateBPFAsync(DateTime debut, DateTime fin)
        {
            var sessions = await GetSessionsPeriodeAsync(debut, fin);
            var statistiques = await GetStatistiquesAsync(debut, fin);

            var sessionDetails = new List<(Session Session, int StagiairesCount, decimal ChiffreAffaires)>();
            foreach (var session in sessions)
            {
                var stagiairesCount = await _context.Stagiaires.CountAsync(s => s.SessionId == session.Id);
                var ca = await _context.SessionClients
                    .Where(sc => sc.SessionId == session.Id)
                    .SumAsync(sc => sc.TarifNegocie * sc.NombrePlaces);

                sessionDetails.Add((session, stagiairesCount, ca));
            }

            // Informations de l'organisation
            var orgInfo = _organizationService.GetOrganizationInfo();
            var currentYear = DateTime.Now.Year;
            var qualiopiCertified = _configuration["Qualiopi:Certification"] == "true";
            var qualiopiCertNumber = _configuration["Qualiopi:NumeroCertification"] ?? string.Empty;
            var logoPath = _organizationService.GetLogoPath();

            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginVertical(1.2f, Unit.Centimetre);
                    page.MarginHorizontal(1.8f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Calibri));

                    // En-tête de page
                    page.Header()
                        .Text($"Bilan Pédagogique et Financier {currentYear}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium)
                        .AlignCenter();

                    page.Content()
                        .Column(column =>
                        {
                            // PAGE DE GARDE - Design inspiré du catalogue
                            column.Item().PageBreak();
                            column.Item()
                                .Column(coverColumn =>
                            {
                                coverColumn.Spacing(15);
                                
                                // Zone supérieure avec logo et titre (fond bleu foncé)
                                coverColumn.Item()
                                    .Background(Colors.Blue.Darken2)
                                    .Padding(30)
                                    .Column(headerZone =>
                                    {
                                        headerZone.Spacing(15);
                                        
                                        // Logo centré
                                        if (!string.IsNullOrEmpty(logoPath) && System.IO.File.Exists(logoPath))
                                        {
                                            headerZone.Item()
                                                .AlignCenter()
                                                .MaxHeight(70)
                                                .Image(logoPath)
                                                .FitArea();
                                        }
                                        
                                        // Nom de l'organisation (si pas de logo)
                                        if (string.IsNullOrEmpty(logoPath) || !System.IO.File.Exists(logoPath))
                                        {
                                            if (!string.IsNullOrEmpty(orgInfo.OrganizationName))
                                            {
                                                headerZone.Item()
                                                    .AlignCenter()
                                                    .Text(orgInfo.OrganizationName)
                                                    .FontSize(20)
                                                    .Bold()
                                                    .FontColor(Colors.White);
                                            }
                                        }
                                        
                                        // Séparateur visuel
                                        headerZone.Item()
                                            .PaddingVertical(10)
                                            .LineHorizontal(1)
                                            .LineColor(Colors.Grey.Lighten1);
                                        
                                        // Titre principal
                                        headerZone.Item()
                                            .AlignCenter()
                                            .Text("Bilan Pédagogique et Financier")
                                            .FontSize(28)
                                            .FontColor(Colors.White)
                                            .Bold();
                                        
                                        headerZone.Item()
                                            .AlignCenter()
                                            .Text($"Période : {debut:dd/MM/yyyy} au {fin:dd/MM/yyyy}")
                                            .FontSize(14)
                                            .FontColor(Colors.Grey.Lighten1);
                                    });
                                
                                // Zone centrale - Statistiques
                                coverColumn.Item()
                                    .PaddingTop(25)
                                    .PaddingHorizontal(20)
                                    .Column(statsZone =>
                                    {
                                        statsZone.Spacing(12);
                                        
                                        statsZone.Item()
                                            .AlignCenter()
                                            .Text("Synthèse de l'activité de formation")
                                            .FontSize(16)
                                            .Bold()
                                            .FontColor(Colors.Blue.Darken2);
                                        
                                        // Statistiques en grille
                                        statsZone.Item()
                                            .PaddingTop(20)
                                            .Row(statsRow =>
                                            {
                                                statsRow.RelativeItem()
                                                    .Background(Colors.Grey.Lighten5)
                                                    .Padding(15)
                                                    .BorderLeft(4)
                                                    .BorderColor(Colors.Blue.Medium)
                                                    .Column(statCol =>
                                                    {
                                                        statCol.Item()
                                                            .AlignCenter()
                                                            .Text(statistiques["nombre_sessions"].ToString())
                                                            .FontSize(24)
                                                            .Bold()
                                                            .FontColor(Colors.Blue.Medium);
                                                        statCol.Item()
                                                            .AlignCenter()
                                                            .Text("Sessions")
                                                            .FontSize(10)
                                                            .FontColor(Colors.Grey.Darken1);
                                                    });
                                                
                                                statsRow.RelativeItem()
                                                    .Background(Colors.Grey.Lighten5)
                                                    .Padding(15)
                                                    .BorderLeft(4)
                                                    .BorderColor(Colors.Green.Medium)
                                                    .Column(statCol =>
                                                    {
                                                        statCol.Item()
                                                            .AlignCenter()
                                                            .Text(statistiques["nombre_stagiaires"].ToString())
                                                            .FontSize(24)
                                                            .Bold()
                                                            .FontColor(Colors.Green.Medium);
                                                        statCol.Item()
                                                            .AlignCenter()
                                                            .Text("Stagiaires")
                                                            .FontSize(10)
                                                            .FontColor(Colors.Grey.Darken1);
                                                    });
                                                
                                                statsRow.RelativeItem()
                                                    .Background(Colors.Grey.Lighten5)
                                                    .Padding(15)
                                                    .BorderLeft(4)
                                                    .BorderColor(Colors.Orange.Medium)
                                                    .Column(statCol =>
                                                    {
                                                        statCol.Item()
                                                            .AlignCenter()
                                                            .Text($"{statistiques["total_heures"]}h")
                                                            .FontSize(24)
                                                            .Bold()
                                                            .FontColor(Colors.Orange.Medium);
                                                        statCol.Item()
                                                            .AlignCenter()
                                                            .Text("Heures")
                                                            .FontSize(10)
                                                            .FontColor(Colors.Grey.Darken1);
                                                    });
                                                
                                                statsRow.RelativeItem()
                                                    .Background(Colors.Grey.Lighten5)
                                                    .Padding(15)
                                                    .BorderLeft(4)
                                                    .BorderColor(Colors.Purple.Medium)
                                                    .Column(statCol =>
                                                    {
                                                        statCol.Item()
                                                            .AlignCenter()
                                                            .Text($"{(decimal)statistiques["chiffre_affaires"]:C0}")
                                                            .FontSize(20)
                                                            .Bold()
                                                            .FontColor(Colors.Purple.Medium);
                                                        statCol.Item()
                                                            .AlignCenter()
                                                            .Text("Chiffre d'affaires")
                                                            .FontSize(10)
                                                            .FontColor(Colors.Grey.Darken1);
                                                    });
                                            });
                                    });
                                
                                // Zone inférieure - Informations pratiques
                                coverColumn.Item()
                                    .PaddingTop(30)
                                    .PaddingHorizontal(20)
                                    .Column(infoZone =>
                                    {
                                        infoZone.Spacing(8);
                                        
                                        infoZone.Item()
                                            .LineHorizontal(1)
                                            .LineColor(Colors.Grey.Lighten2);
                                        
                                        infoZone.Item()
                                            .PaddingTop(15)
                                            .Text("Informations pratiques")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(Colors.Blue.Darken2);
                                        
                                        infoZone.Item()
                                            .PaddingTop(10)
                                            .Column(orgInfoCol =>
                                            {
                                                orgInfoCol.Spacing(6);
                                                
                                                if (!string.IsNullOrEmpty(orgInfo.OrganizationName))
                                                {
                                                    orgInfoCol.Item()
                                                        .Row(row =>
                                                        {
                                                            row.ConstantItem(80)
                                                                .Text("Organisme :")
                                                                .FontSize(10)
                                                                .FontColor(Colors.Grey.Darken1);
                                                            row.RelativeItem()
                                                                .Text(orgInfo.OrganizationName)
                                                                .FontSize(10)
                                                                .Bold();
                                                        });
                                                }
                                                
                                                if (!string.IsNullOrEmpty(orgInfo.SIRET))
                                                {
                                                    orgInfoCol.Item()
                                                        .Row(row =>
                                                        {
                                                            row.ConstantItem(80)
                                                                .Text("SIRET :")
                                                                .FontSize(10)
                                                                .FontColor(Colors.Grey.Darken1);
                                                            row.RelativeItem()
                                                                .Text(orgInfo.SIRET)
                                                                .FontSize(10);
                                                        });
                                                }
                                                
                                                if (!string.IsNullOrEmpty(orgInfo.Address))
                                                {
                                                    orgInfoCol.Item()
                                                        .Row(row =>
                                                        {
                                                            row.ConstantItem(80)
                                                                .Text("Adresse :")
                                                                .FontSize(10)
                                                                .FontColor(Colors.Grey.Darken1);
                                                            row.RelativeItem()
                                                                .Text(orgInfo.Address)
                                                                .FontSize(10);
                                                        });
                                                }
                                            });
                                        
                                        // Badge Qualiopi (si certifié)
                                        if (qualiopiCertified)
                                        {
                                            infoZone.Item()
                                                .PaddingTop(15)
                                                .Background(Colors.Green.Lighten5)
                                                .Padding(10)
                                                .BorderLeft(3)
                                                .BorderColor(Colors.Green.Medium)
                                                .Row(qualiopiRow =>
                                                {
                                                    qualiopiRow.RelativeItem()
                                                        .Column(qualiopiCol =>
                                                        {
                                                            qualiopiCol.Item()
                                                                .Text("✓ Organisme certifié Qualiopi")
                                                                .FontSize(10)
                                                                .Bold()
                                                                .FontColor(Colors.Green.Darken2);
                                                            
                                                            if (!string.IsNullOrEmpty(qualiopiCertNumber))
                                                            {
                                                                qualiopiCol.Item()
                                                                    .Text($"N° de certificat : {qualiopiCertNumber}")
                                                                    .FontSize(9)
                                                                    .FontColor(Colors.Grey.Darken1);
                                                            }
                                                        });
                                                });
                                        }
                                        
                                        // Date de génération
                                        infoZone.Item()
                                            .PaddingTop(20)
                                            .AlignCenter()
                                            .Text($"Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm}")
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Medium);
                                });
                            });

                            // CONTENU DÉTAILLÉ
                            column.Item().PageBreak();
                            column.Item()
                                .Column(contentColumn =>
                            {
                                contentColumn.Spacing(15);

                                // Détail des sessions - style professionnel
                                contentColumn.Item()
                                    .Background(Colors.Blue.Darken2)
                                    .Padding(15)
                                    .Text("DÉTAIL DES SESSIONS")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                contentColumn.Item().PaddingTop(10);

                                contentColumn.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(70); // Date
                                    columns.RelativeColumn(); // Formation
                                    columns.ConstantColumn(60); // Stagiaires
                                    columns.ConstantColumn(60); // Heures
                                    columns.ConstantColumn(80); // CA
                                });

                                table.Header(header =>
                                {
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Medium).Padding(8)).Text("Date").Bold().FontColor(Colors.White);
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Medium).Padding(8)).Text("Formation").Bold().FontColor(Colors.White);
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Medium).Padding(8)).Text("Stagiaires").Bold().FontColor(Colors.White);
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Medium).Padding(8)).Text("Heures").Bold().FontColor(Colors.White);
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Medium).Padding(8)).Text("CA").Bold().FontColor(Colors.White);
                                });

                                foreach (var detail in sessionDetails)
                                {
                                        table.Cell().Padding(5).Text(detail.Session.DateDebut.ToString("dd/MM/yyyy"));
                                        table.Cell().Padding(5).Text(detail.Session.Formation.Titre);
                                        table.Cell().Padding(5).AlignCenter().Text(detail.StagiairesCount.ToString());
                                        table.Cell().Padding(5).AlignCenter().Text($"{detail.Session.Formation.DureeHeures}h");
                                        table.Cell().Padding(5).AlignRight().Text($"{detail.ChiffreAffaires:C2}");
                                    }
                                });

                                contentColumn.Item().PaddingTop(20);

                                // Répartition par type de client - style professionnel
                                contentColumn.Item()
                                    .Background(Colors.Blue.Darken2)
                                    .Padding(15)
                                    .Text("RÉPARTITION PAR TYPE DE CLIENT")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.White);

                                contentColumn.Item().PaddingTop(10);

                            var repartition = statistiques["repartition_clients"] as Dictionary<TypeClient, int>
                                ?? new Dictionary<TypeClient, int>();

                                contentColumn.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(); // Type
                                    columns.ConstantColumn(80); // Nombre
                                    columns.ConstantColumn(80); // Pourcentage
                                });

                                table.Header(header =>
                                {
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Medium).Padding(8)).Text("Type de client").Bold().FontColor(Colors.White);
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Medium).Padding(8)).Text("Nombre").Bold().FontColor(Colors.White);
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Medium).Padding(8)).Text("%").Bold().FontColor(Colors.White);
                                });

                                var total = repartition.Values.Sum();
                                foreach (var kvp in repartition)
                                {
                                    var pourcentage = total > 0 ? (kvp.Value * 100.0 / total) : 0;
                                        table.Cell().Padding(5).Text(kvp.Key.ToString());
                                        table.Cell().Padding(5).AlignCenter().Text(kvp.Value.ToString());
                                        table.Cell().Padding(5).AlignCenter().Text($"{pourcentage:F1}%");
                                    }
                                });
                            });
                        });

                    page.Footer()
                        .PaddingTop(5)
                        .Column(col =>
                        {
                            col.Spacing(2);

                            // Logo organisation si disponible
                            if (!string.IsNullOrEmpty(logoPath) && System.IO.File.Exists(logoPath))
                            {
                                col.Item()
                                    .AlignCenter()
                                    .Height(20)
                                    .Image(logoPath);
                            }

                            // Logo Qualiopi si disponible
                            if (System.IO.File.Exists(QualiopiLogoPath))
                            {
                                col.Item()
                                    .AlignCenter()
                                    .Height(20)
                                    .Image(QualiopiLogoPath);
                            }

                            col.Item()
                                .AlignCenter()
                                .Text(_organizationService.GetOrganizationName())
                                .FontSize(8)
                                .SemiBold();

                            col.Item()
                                .AlignCenter()
                                .Text(text =>
                                {
                                    text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                                    text.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                                });
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<Dictionary<string, object>> GetStatistiquesAsync(DateTime debut, DateTime fin)
        {
            var sessions = await GetSessionsPeriodeAsync(debut, fin);
            
            var nombreSessions = sessions.Count;
            var sessionIds = sessions.Select(s => s.Id).ToList();

            var stagiairesQuery = _context.Stagiaires.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                stagiairesQuery = stagiairesQuery.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            }

            var nombreStagiaires = await stagiairesQuery
                .Where(s => s.SessionId.HasValue && sessionIds.Contains(s.SessionId.Value))
                .CountAsync();

            var totalHeures = sessions.Sum(s => s.Formation.DureeHeures);
            
            var sessionClientsQuery = _context.SessionClients.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                sessionClientsQuery = sessionClientsQuery.Where(sc => string.IsNullOrEmpty(sc.SiteId) || sc.SiteId == _siteContext.CurrentSiteId);
            }

            var chiffreAffaires = await sessionClientsQuery
                .Where(sc => sessionIds.Contains(sc.SessionId))
                .SumAsync(sc => sc.TarifNegocie * sc.NombrePlaces);

            // Répartition par type de client
            var repartitionClients = new Dictionary<TypeClient, int>();
            var clientsSessions = await sessionClientsQuery
                .Include(sc => sc.Client)
                .Where(sc => sessionIds.Contains(sc.SessionId))
                .ToListAsync();

            foreach (var sc in clientsSessions)
            {
                if (!repartitionClients.ContainsKey(sc.Client.TypeClient))
                    repartitionClients[sc.Client.TypeClient] = 0;
                repartitionClients[sc.Client.TypeClient] += sc.NombrePlaces;
            }

            return new Dictionary<string, object>
            {
                ["nombre_sessions"] = nombreSessions,
                ["nombre_stagiaires"] = nombreStagiaires,
                ["total_heures"] = totalHeures,
                ["chiffre_affaires"] = chiffreAffaires,
                ["repartition_clients"] = repartitionClients
            };
        }

        public async Task<List<Session>> GetSessionsPeriodeAsync(DateTime debut, DateTime fin)
        {
            var query = _context.Sessions.AsQueryable();
            if (!_siteContext.IsAdmin)
            {
                query = query.Where(s => string.IsNullOrEmpty(s.SiteId) || s.SiteId == _siteContext.CurrentSiteId);
            }

            return await query
                .Include(s => s.Formation)
                .Where(s => s.DateDebut >= debut && s.DateFin <= fin)
                .OrderBy(s => s.DateDebut)
                .ToListAsync();
        }
    }
}
