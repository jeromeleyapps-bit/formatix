using System.Text.Json;
using CsvHelper;
using System.Globalization;
using FormationManager.Models;
using FormationManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfDocument = QuestPDF.Fluent.Document;

namespace FormationManager.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportSessionsCSVAsync(DateTime debut, DateTime fin);
        Task<byte[]> ExportStagiairesCSVAsync(DateTime debut, DateTime fin);
        Task<byte[]> ExportBPFJSONAsync(DateTime debut, DateTime fin);
        Task<byte[]> ExportQualiopiJSONAsync(int sessionId);
        Task<byte[]> ExportCataloguePDFAsync(string? siteId = null);
    }

    public class ExportService : IExportService
    {
        private readonly FormationDbContext _context;
        private readonly ILogger<ExportService> _logger;
        private readonly IOrganizationService _organizationService;
        private readonly IConfiguration _configuration;

        public ExportService(
            FormationDbContext context, 
            ILogger<ExportService> logger,
            IOrganizationService organizationService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _organizationService = organizationService;
            _configuration = configuration;
            
            // Configuration QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> ExportSessionsCSVAsync(DateTime debut, DateTime fin)
        {
            var sessions = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.Formateur)
                .Where(s => s.DateDebut >= debut && s.DateFin <= fin)
                .OrderBy(s => s.DateDebut)
                .ToListAsync();

            using var output = new MemoryStream();
            using var writer = new StreamWriter(output, System.Text.Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // En-têtes
            csv.WriteField("ID Session");
            csv.WriteField("Formation");
            csv.WriteField("Date Début");
            csv.WriteField("Date Fin");
            csv.WriteField("Lieu");
            csv.WriteField("Statut");
            csv.WriteField("Formateur");
            csv.WriteField("Durée (heures)");
            csv.WriteField("Est Publique");
            csv.NextRecord();

            // Données
            foreach (var session in sessions)
            {
                csv.WriteField(session.Id);
                csv.WriteField(session.Formation.Titre);
                csv.WriteField(session.DateDebut.ToString("yyyy-MM-dd"));
                csv.WriteField(session.DateFin.ToString("yyyy-MM-dd"));
                csv.WriteField(session.Lieu ?? "");
                csv.WriteField(session.Statut);
                csv.WriteField($"{session.Formateur?.Prenom} {session.Formateur?.Nom}" ?? "");
                csv.WriteField(session.Formation.DureeHeures.ToString("F2"));
                csv.WriteField(session.EstPublique);
                csv.NextRecord();
            }

            await csv.FlushAsync();
            writer.Flush();
            output.Position = 0;

            return output.ToArray();
        }

        public async Task<byte[]> ExportStagiairesCSVAsync(DateTime debut, DateTime fin)
        {
            var sessions = await _context.Sessions
                .Where(s => s.DateDebut >= debut && s.DateFin <= fin)
                .Select(s => s.Id)
                .ToListAsync();

            var stagiaires = await _context.Stagiaires
                .Include(s => s.Client)
                .Include(s => s.Session)
                .ThenInclude(s => s!.Formation)
                .Where(s => sessions.Contains(s.SessionId ?? 0))
                .OrderBy(s => s.Nom)
                .ThenBy(s => s.Prenom)
                .ToListAsync();

            using var output = new MemoryStream();
            using var writer = new StreamWriter(output, System.Text.Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // En-têtes
            csv.WriteField("ID Stagiaire");
            csv.WriteField("Nom");
            csv.WriteField("Prénom");
            csv.WriteField("Email");
            csv.WriteField("Téléphone");
            csv.WriteField("Client");
            csv.WriteField("Type Client");
            csv.WriteField("Formation");
            csv.WriteField("Date Début");
            csv.WriteField("Heures Présence");
            csv.WriteField("Évaluation à Chaud");
            csv.WriteField("Évaluation à Froid");
            csv.WriteField("Attestation Générée");
            csv.NextRecord();

            // Données
            foreach (var stagiaire in stagiaires)
            {
                csv.WriteField(stagiaire.Id);
                csv.WriteField(stagiaire.Nom);
                csv.WriteField(stagiaire.Prenom);
                csv.WriteField(stagiaire.Email ?? "");
                csv.WriteField(stagiaire.Telephone ?? "");
                csv.WriteField(stagiaire.Client.Nom);
                csv.WriteField(stagiaire.Client.TypeClient.ToString());
                csv.WriteField(stagiaire.Session?.Formation.Titre ?? "");
                csv.WriteField(stagiaire.Session?.DateDebut.ToString("yyyy-MM-dd") ?? "");
                csv.WriteField(stagiaire.HeuresPresence.ToString("F2"));
                csv.WriteField(stagiaire.EvaluationAChaud?.ToString("F2") ?? "");
                csv.WriteField(stagiaire.EvaluationAFroid?.ToString("F2") ?? "");
                csv.WriteField(stagiaire.AttestationGeneree);
                csv.NextRecord();
            }

            await csv.FlushAsync();
            writer.Flush();
            output.Position = 0;

            return output.ToArray();
        }

        public async Task<byte[]> ExportBPFJSONAsync(DateTime debut, DateTime fin)
        {
            var sessions = await _context.Sessions
                .Include(s => s.Formation)
                .Include(s => s.SessionClients)
                    .ThenInclude(sc => sc.Client)
                .Where(s => s.DateDebut >= debut && s.DateFin <= fin)
                .ToListAsync();

            var stagiaires = await _context.Stagiaires
                .Include(s => s.Client)
                .Include(s => s.Session)
                .Where(s => sessions.Any(sess => sess.Id == s.SessionId))
                .ToListAsync();

            var bpfData = new
            {
                periode = new { debut = debut.ToString("yyyy-MM-dd"), fin = fin.ToString("yyyy-MM-dd") },
                synthese = new
                {
                    nombre_sessions = sessions.Count,
                    nombre_stagiaires = stagiaires.Count,
                    total_heures = sessions.Sum(s => s.Formation.DureeHeures),
                    chiffre_affaires = sessions.SumMany(s => s.SessionClients.Sum(sc => sc.TarifNegocie * sc.NombrePlaces))
                },
                sessions = sessions.Select(s => new
                {
                    id = s.Id,
                    formation = s.Formation.Titre,
                    date_debut = s.DateDebut.ToString("yyyy-MM-dd"),
                    date_fin = s.DateFin.ToString("yyyy-MM-dd"),
                    lieu = s.Lieu,
                    statut = s.Statut,
                    duree_heures = s.Formation.DureeHeures,
                    nombre_stagiaires = stagiaires.Count(st => st.SessionId == s.Id),
                    chiffre_affaires = s.SessionClients.Sum(sc => sc.TarifNegocie * sc.NombrePlaces),
                    clients = s.SessionClients.Select(sc => new
                    {
                        nom = sc.Client.Nom,
                        type = sc.Client.TypeClient.ToString(),
                        tarif = sc.TarifNegocie,
                        nombre_places = sc.NombrePlaces
                    }).ToList()
                }).ToList(),
                stagiaires = stagiaires.Select(s => new
                {
                    id = s.Id,
                    nom = s.Nom,
                    prenom = s.Prenom,
                    email = s.Email,
                    client = s.Client.Nom,
                    formation = s.Session?.Formation.Titre,
                    heures_presence = s.HeuresPresence,
                    evaluation_a_chaud = s.EvaluationAChaud,
                    evaluation_a_froid = s.EvaluationAFroid,
                    attestation_generee = s.AttestationGeneree
                }).ToList()
            };

            var json = JsonSerializer.Serialize(bpfData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public async Task<byte[]> ExportQualiopiJSONAsync(int sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.Formation)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                throw new ArgumentException("Session non trouvée");

            var indicateurs = await _context.IndicateursQualiopi
                .OrderBy(i => i.Critere)
                .ThenBy(i => i.CodeIndicateur)
                .ToListAsync();

            var preuves = await _context.PreuvesQualiopi
                .Include(p => p.Indicateur)
                .Where(p => p.SessionId == sessionId)
                .ToListAsync();

            // Récupérer les validations veille pour le critère 6
            var validationsVeille = await _context.VeilleValidations
                .Where(v => v.SiteId == session.SiteId)
                .Include(v => v.RssItem)
                .Include(v => v.Indicateur)
                .Where(v => v.Indicateur.Critere == 6)
                .ToListAsync();

            var qualiopiData = new
            {
                session = new
                {
                    id = session.Id,
                    formation = session.Formation.Titre,
                    date_debut = session.DateDebut.ToString("yyyy-MM-dd"),
                    date_fin = session.DateFin.ToString("yyyy-MM-dd"),
                    lieu = session.Lieu
                },
                indicateurs = indicateurs.Select(i => new
                {
                    id = i.Id,
                    code = i.CodeIndicateur,
                    libelle = i.Libelle,
                    critere = i.Critere,
                    niveau_preuve_requis = i.NiveauPreuveRequis.ToString(),
                    preuves = preuves.Where(p => p.IndicateurQualiopiId == i.Id).Select(p => new
                    {
                        id = p.Id,
                        titre = p.Titre,
                        description = p.Description,
                        type = p.Type.ToString(),
                        est_valide = p.EstValide,
                        date_validation = p.DateValidation?.ToString("yyyy-MM-dd"),
                        commentaire_validation = p.CommentaireValidation
                    }).ToList(),
                    validations_veille = i.Critere == 6 ? validationsVeille.Where(v => v.IndicateurQualiopiId == i.Id).Select(v => (object)new
                    {
                        id = v.Id,
                        actualite_titre = v.RssItem?.Title ?? "",
                        actualite_lien = v.RssItem?.Link ?? "",
                        valide_par = v.ValidatedBy,
                        valide_le = v.ValidatedAt.ToString("yyyy-MM-dd HH:mm")
                    }).ToList() : new List<object>()
                }).ToList(),
                conformite = new
                {
                    total_indicateurs = indicateurs.Count,
                    indicateurs_conformes = indicateurs.Count(i => 
                        preuves.Any(p => p.IndicateurQualiopiId == i.Id && p.EstValide) ||
                        (i.Critere == 6 && validationsVeille.Any(v => v.IndicateurQualiopiId == i.Id))),
                    taux_conformite = indicateurs.Count > 0 ? (indicateurs.Count(i => 
                        preuves.Any(p => p.IndicateurQualiopiId == i.Id && p.EstValide) ||
                        (i.Critere == 6 && validationsVeille.Any(v => v.IndicateurQualiopiId == i.Id))) * 100.0 / indicateurs.Count) : 0
                }
            };

            var json = JsonSerializer.Serialize(qualiopiData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public async Task<byte[]> ExportCataloguePDFAsync(string? siteId = null)
        {
            // Récupérer les formations (toutes si aucune n'est publique, sinon seulement publiques)
            var query = _context.Formations.AsQueryable();
            
            // Filtrer par site si spécifié
            if (!string.IsNullOrEmpty(siteId))
            {
                query = query.Where(f => string.IsNullOrEmpty(f.SiteId) || f.SiteId == siteId);
            }
            
            // Essayer d'abord les formations publiques, sinon prendre toutes
            var formationsPubliques = await query
                .Where(f => f.EstPublique)
                .OrderBy(f => f.Titre)
                .ToListAsync();
            
            var formations = formationsPubliques.Any() 
                ? formationsPubliques 
                : await query.OrderBy(f => f.Titre).ToListAsync();
            
            _logger.LogInformation($"Export catalogue : {formations.Count} formation(s) trouvée(s)");
            
            // Récupérer les images Qualiopi (preuves de type Photo)
            var qualiopiImages = new List<(PreuveQualiopi Preuve, IndicateurQualiopi Indicateur)>();
            try
            {
                var preuvesPhotos = await _context.PreuvesQualiopi
                    .Include(p => p.Indicateur)
                    .Where(p => p.Type == PreuveQualiopi.TypePreuve.Photo && 
                                p.EstValide && 
                                !string.IsNullOrEmpty(p.CheminFichier))
                    .Take(5) // Maximum 5 images
                    .ToListAsync();
                
                qualiopiImages = preuvesPhotos
                    .Select(p => (p, p.Indicateur))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de récupérer les images Qualiopi");
            }
            
            // Informations de l'organisation
            var orgInfo = _organizationService.GetOrganizationInfo();
            var currentYear = DateTime.Now.Year;
            var qualiopiCertified = _configuration["Qualiopi:Certification"] == "true";
            var qualiopiCertNumber = _configuration["Qualiopi:NumeroCertification"] ?? string.Empty;
            
            // Logo de l'organisation (si disponible)
            var logoPath = _organizationService.GetLogoPath();
            
            // Générer le PDF avec QuestPDF
            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Calibri));
                    
                    // En-tête de page
                    page.Header()
                        .Text($"Catalogue de Formations {currentYear}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Medium)
                        .AlignCenter();
                    
                    // Pied de page
                    page.Footer()
                        .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium))
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                    
                    page.Content()
                        .Column(column =>
                        {
                            // PAGE DE GARDE - Design inspiré des catalogues d'éducation populaire
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
                                        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                                        {
                                            headerZone.Item()
                                                .AlignCenter()
                                                .MaxHeight(70)
                                                .Image(logoPath)
                                                .FitArea();
                                        }
                                        
                                        // Nom de l'organisation (si pas de logo)
                                        if (string.IsNullOrEmpty(logoPath) || !File.Exists(logoPath))
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
                                        
                                        // Titre principal - style éducation populaire
                                        headerZone.Item()
                                            .AlignCenter()
                                            .Text("Catalogue de Formations")
                                            .FontSize(28)
                                            .FontColor(Colors.White)
                                            .Bold();
                                        
                                        headerZone.Item()
                                            .AlignCenter()
                                            .Text($"Année {currentYear}")
                                            .FontSize(14)
                                            .FontColor(Colors.Grey.Lighten1);
                                    });
                                
                                // Zone centrale - Mission et valeurs (style éducation populaire)
                                coverColumn.Item()
                                    .PaddingTop(25)
                                    .PaddingHorizontal(20)
                                    .Column(missionZone =>
                                    {
                                        missionZone.Spacing(12);
                                        
                                        // Message d'accueil / Mission
                                        missionZone.Item()
                                            .AlignCenter()
                                            .Text("Formation professionnelle et développement des compétences")
                                            .FontSize(16)
                                            .Bold()
                                            .FontColor(Colors.Blue.Darken2);
                                        
                                        missionZone.Item()
                                            .AlignCenter()
                                            .Text("Notre engagement : accompagner votre montée en compétences")
                                            .FontSize(12)
                                            .FontColor(Colors.Grey.Darken1)
                                            .Italic();
                                        
                                        // Statistiques rapides (si formations disponibles)
                                        if (formations.Any())
                                        {
                                            missionZone.Item()
                                                .PaddingTop(20)
                                                .Background(Colors.Grey.Lighten5)
                                                .Padding(15)
                                                .BorderLeft(4)
                                                .BorderColor(Colors.Blue.Medium)
                                                .Row(statsRow =>
                                                {
                                                    statsRow.RelativeItem()
                                                        .AlignCenter()
                                                        .Column(statCol =>
                                                        {
                                                            statCol.Item()
                                                                .Text(formations.Count.ToString())
                                                                .FontSize(24)
                                                                .Bold()
                                                                .FontColor(Colors.Blue.Medium);
                                                            statCol.Item()
                                                                .Text("Formations disponibles")
                                                                .FontSize(10)
                                                                .FontColor(Colors.Grey.Darken1);
                                                        });
                                                });
                                        }
                                    });
                                
                                // Zone inférieure - Informations pratiques
                                coverColumn.Item()
                                    .PaddingTop(30)
                                    .PaddingHorizontal(20)
                                    .Column(infoZone =>
                                    {
                                        infoZone.Spacing(8);
                                        
                                        // Ligne de séparation
                                        infoZone.Item()
                                            .LineHorizontal(1)
                                            .LineColor(Colors.Grey.Lighten2);
                                        
                                        infoZone.Item()
                                            .PaddingTop(15)
                                            .Text("Informations pratiques")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(Colors.Blue.Darken2);
                                        
                                        // Informations organisation en colonnes
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
                                                
                                                if (!string.IsNullOrEmpty(orgInfo.Phone))
                                                {
                                                    orgInfoCol.Item()
                                                        .Row(row =>
                                                        {
                                                            row.ConstantItem(80)
                                                                .Text("Téléphone :")
                                                                .FontSize(10)
                                                                .FontColor(Colors.Grey.Darken1);
                                                            row.RelativeItem()
                                                                .Text(orgInfo.Phone)
                                                                .FontSize(10);
                                                        });
                                                }
                                                
                                                if (!string.IsNullOrEmpty(orgInfo.Email))
                                                {
                                                    orgInfoCol.Item()
                                                        .Row(row =>
                                                        {
                                                            row.ConstantItem(80)
                                                                .Text("Email :")
                                                                .FontSize(10)
                                                                .FontColor(Colors.Grey.Darken1);
                                                            row.RelativeItem()
                                                                .Text(orgInfo.Email)
                                                                .FontSize(10);
                                                        });
                                                }
                                            });
                                        
                                        // Badge Qualiopi (si certifié) - style discret
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
                                        
                                        // Date de génération (discret en bas)
                                        infoZone.Item()
                                            .PaddingTop(20)
                                            .AlignCenter()
                                            .Text($"Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm}")
                                            .FontSize(9)
                                            .FontColor(Colors.Grey.Medium);
                                    });
                            });
                            
                            // TABLE DES MATIÈRES
                            column.Item().PageBreak();
                            column.Item().Column(tocColumn =>
                            {
                                tocColumn.Item()
                                    .PaddingBottom(20)
                                    .AlignCenter()
                                    .Text("TABLE DES MATIÈRES")
                                    .FontSize(20)
                                    .Bold()
                                    .FontColor(Colors.Blue.Medium);
                                
                                foreach (var formation in formations)
                                {
                                    tocColumn.Item()
                                        .BorderBottom(1, Unit.Point)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(5)
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text(formation.Titre)
                                                .FontColor(Colors.Blue.Medium)
                                                .Bold();
                                        });
                                }
                                
                                if (!formations.Any())
                                {
                                    tocColumn.Item()
                                        .Text("Aucune formation disponible")
                                        .Italic()
                                        .FontColor(Colors.Grey.Medium);
                                }
                            });
                            
                            // FORMATIONS
                            foreach (var formation in formations)
                            {
                                column.Item().PageBreak();
                                column.Item().Column(formationColumn =>
                                {
                                    // En-tête formation - style professionnel
                                    formationColumn.Item()
                                        .Background(Colors.Blue.Darken2)
                                        .Padding(15)
                                        .Column(headerColumn =>
                                        {
                                            headerColumn.Item()
                                                .Text(formation.Titre)
                                                .FontSize(18)
                                                .Bold()
                                                .FontColor(Colors.White);
                                            
                                            // Numéro de référence si disponible
                                            if (formation.Id > 0)
                                            {
                                                headerColumn.Item()
                                                    .PaddingTop(5)
                                                    .Text($"Réf. : FOR-{formation.Id:D4}")
                                                    .FontSize(10)
                                                    .FontColor(Colors.Grey.Lighten1);
                                            }
                                        });
                                    
                                    formationColumn.Item().PaddingTop(15);
                                    
                                    // Description
                                    if (!string.IsNullOrEmpty(formation.Description))
                                    {
                                        formationColumn.Item()
                                            .PaddingBottom(15)
                                            .Text(formation.Description)
                                            .FontSize(11)
                                            .LineHeight(1.6f);
                                    }
                                    
                                    // Détails
                                    formationColumn.Item()
                                        .Background(Colors.Grey.Lighten5)
                                        .Padding(10)
                                        .Grid(grid =>
                                        {
                                            grid.Columns(2);
                                            
                                            grid.Item()
                                                .Text($"Durée : {formation.DureeHeures:F2} heures")
                                                .FontSize(10);
                                            
                                            grid.Item()
                                                .Text($"Prix : {formation.PrixIndicatif:F2} €")
                                                .FontSize(10);
                                        });
                                    
                                    // Programme
                                    if (!string.IsNullOrEmpty(formation.Programme))
                                    {
                                        formationColumn.Item()
                                            .PaddingTop(15)
                                            .Column(programmeColumn =>
                                            {
                                                programmeColumn.Item()
                                                    .PaddingBottom(5)
                                                    .BorderBottom(2f, Unit.Point)
                                                    .BorderColor(Colors.Blue.Medium)
                                                    .Text("Programme")
                                                    .FontSize(13)
                                                    .Bold()
                                                    .FontColor(Colors.Blue.Medium);
                                                
                                                programmeColumn.Item()
                                                    .Text(formation.Programme)
                                                    .FontSize(10)
                                                    .LineHeight(1.6f);
                                            });
                                    }
                                    
                                    // Prérequis
                                    if (!string.IsNullOrEmpty(formation.Prerequis))
                                    {
                                        formationColumn.Item()
                                            .PaddingTop(15)
                                            .Column(prerequisColumn =>
                                            {
                                                prerequisColumn.Item()
                                                    .PaddingBottom(5)
                                                    .BorderBottom(2f, Unit.Point)
                                                    .BorderColor(Colors.Blue.Medium)
                                                    .Text("Prérequis")
                                                    .FontSize(13)
                                                    .Bold()
                                                    .FontColor(Colors.Blue.Medium);
                                                
                                                prerequisColumn.Item()
                                                    .Text(formation.Prerequis)
                                                    .FontSize(10)
                                                    .LineHeight(1.6f);
                                            });
                                    }
                                    
                                    // Modalités pédagogiques
                                    if (!string.IsNullOrEmpty(formation.ModalitesPedagogiques))
                                    {
                                        formationColumn.Item()
                                            .PaddingTop(15)
                                            .Column(modalitesColumn =>
                                            {
                                                modalitesColumn.Item()
                                                    .PaddingBottom(5)
                                                    .BorderBottom(2f, Unit.Point)
                                                    .BorderColor(Colors.Blue.Medium)
                                                    .Text("Modalités pédagogiques")
                                                    .FontSize(13)
                                                    .Bold()
                                                    .FontColor(Colors.Blue.Medium);
                                                
                                                modalitesColumn.Item()
                                                    .Text(formation.ModalitesPedagogiques)
                                                    .FontSize(10)
                                                    .LineHeight(1.6f);
                                            });
                                    }
                                    
                                    // Modalités d'évaluation
                                    if (!string.IsNullOrEmpty(formation.ModalitesEvaluation))
                                    {
                                        formationColumn.Item()
                                            .PaddingTop(15)
                                            .Column(evalColumn =>
                                            {
                                                evalColumn.Item()
                                                    .PaddingBottom(5)
                                                    .BorderBottom(2f, Unit.Point)
                                                    .BorderColor(Colors.Blue.Medium)
                                                    .Text("Modalités d'évaluation")
                                                    .FontSize(13)
                                                    .Bold()
                                                    .FontColor(Colors.Blue.Medium);
                                                
                                                evalColumn.Item()
                                                    .Text(formation.ModalitesEvaluation)
                                                    .FontSize(10)
                                                    .LineHeight(1.6f);
                                            });
                                    }
                                    
                                    // Prix
                                    formationColumn.Item()
                                        .PaddingTop(15)
                                        .Background(Colors.Yellow.Lighten4)
                                        .BorderLeft(4)
                                        .BorderColor(Colors.Yellow.Darken2)
                                        .Padding(10)
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text("Tarif :")
                                                .FontSize(12)
                                                .Bold()
                                                .FontColor(Colors.Yellow.Darken3);
                                            
                                            row.ConstantItem(100)
                                                .Text($"{formation.PrixIndicatif:F2} €")
                                                .FontSize(16)
                                                .Bold()
                                                .FontColor(Colors.Yellow.Darken3)
                                                .AlignRight();
                                        });
                                });
                            }
                            
                            // PAGE DE FIN
                            column.Item().PageBreak();
                            column.Item().Column(endColumn =>
                            {
                                endColumn.Item()
                                    .PaddingBottom(30)
                                    .AlignCenter()
                                    .Text("Merci de votre intérêt pour nos formations")
                                    .FontSize(14)
                                    .Bold()
                                    .FontColor(Colors.Grey.Darken1);
                                
                                endColumn.Item()
                                    .Background(Colors.Grey.Lighten5)
                                    .Padding(15)
                                    .Column(contactColumn =>
                                    {
                                        contactColumn.Item()
                                            .PaddingBottom(10)
                                            .Text("Contact")
                                            .FontSize(13)
                                            .Bold()
                                            .FontColor(Colors.Blue.Medium);
                                        
                                        if (!string.IsNullOrEmpty(orgInfo.OrganizationName))
                                        {
                                            contactColumn.Item()
                                                .Text(orgInfo.OrganizationName)
                                                .FontSize(11)
                                                .Bold();
                                        }
                                        
                                        if (!string.IsNullOrEmpty(orgInfo.Address))
                                        {
                                            contactColumn.Item()
                                                .Text(orgInfo.Address)
                                                .FontSize(10);
                                        }
                                        
                                        if (!string.IsNullOrEmpty(orgInfo.Phone))
                                        {
                                            contactColumn.Item()
                                                .Text($"Téléphone : {orgInfo.Phone}")
                                                .FontSize(10);
                                        }
                                        
                                        if (!string.IsNullOrEmpty(orgInfo.Email))
                                        {
                                            contactColumn.Item()
                                                .Text($"Email : {orgInfo.Email}")
                                                .FontSize(10);
                                        }
                                        
                                        if (qualiopiCertified)
                                        {
                                            contactColumn.Item()
                                                .PaddingTop(10)
                                                .Text("✓ Organisme certifié Qualiopi")
                                                .FontSize(10)
                                                .Bold()
                                                .FontColor(Colors.Blue.Medium);
                                        }
                                    });
                                
                                endColumn.Item()
                                    .PaddingTop(30)
                                    .AlignCenter()
                                    .Text($"Catalogue généré le {DateTime.Now:dd/MM/yyyy à HH:mm} - {formations.Count} formation{(formations.Count > 1 ? "s" : "")} disponible{(formations.Count > 1 ? "s" : "")}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Medium);
                            });
                        });
                });
            });
            
            return document.GeneratePdf();
        }
    }

    // Extension method pour SumMany
    public static class LinqExtensions
    {
        public static decimal SumMany<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select(selector).Sum();
        }
    }
}
