using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FormationManager.Models;
using FormationManager.Data;
using Microsoft.EntityFrameworkCore;
using PdfDocument = QuestPDF.Fluent.Document;

namespace FormationManager.Services
{
    public interface IDocumentService
    {
        byte[] GenerateConvention(Session session, Client client);
        byte[] GenerateAttestation(Stagiaire stagiaire, Session session);
        byte[] GenerateEmargement(Session session);
        byte[] GenerateEvaluation(Stagiaire stagiaire);
    }

    public class DocumentService : IDocumentService
    {
        private readonly FormationDbContext _context;
        private readonly ILogger<DocumentService> _logger;
        private readonly IOrganizationService _organizationService;
        private readonly IConfiguration _configuration;

        // Logo Qualiopi fourni par l'utilisateur
        private const string QualiopiLogoPath = @"C:\AI\Opagax\logoqualiopi.png";
        
        public DocumentService(FormationDbContext context, ILogger<DocumentService> logger, IOrganizationService organizationService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _organizationService = organizationService;
            _configuration = configuration;
            
            // Configuration de QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateConvention(Session session, Client client)
        {
            var formation = session.Formation ?? _context.Formations.Find(session.FormationId);
            if (formation == null)
            {
                throw new InvalidOperationException("Formation introuvable pour la session.");
            }
            
            var logoPath = _organizationService.GetLogoPath();
            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Calibri));

                    page.Header()
                        .Text("CONVENTION DE FORMATION")
                        .FontSize(20)
                        .FontColor(Colors.Blue.Medium)
                        .Bold()
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Informations organisme
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("ORGANISME DE FORMATION").Bold();
                            });
                            column.Item().Text($"Nom: {_organizationService.GetOrganizationName()}");
                            var siret = _organizationService.GetSIRET();
                            if (!string.IsNullOrWhiteSpace(siret))
                                column.Item().Text($"SIRET: {siret}");
                            var adresse = _organizationService.GetAddress();
                            if (!string.IsNullOrWhiteSpace(adresse))
                                column.Item().Text($"Adresse: {adresse}");
                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            // Informations client
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("CLIENT").Bold();
                            });
                            column.Item().Text($"Nom: {client.Nom}");
                            column.Item().Text($"Type: {client.TypeClient}");
                            if (client.TypeClient == TypeClient.Entreprise)
                            {
                                column.Item().Text($"SIRET: {client.SIRET}");
                            }
                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            // Formation
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("FORMATION").Bold();
                            });
                            column.Item().Text($"Titre: {formation.Titre}");
                            column.Item().Text($"Durée: {formation.DureeHeures} heures");
                            column.Item().Text($"Prix: {session.SessionClients.FirstOrDefault()?.TarifNegocie:C2} €");
                            column.Item().Text($"Dates: {session.DateDebut:dd/MM/yyyy} au {session.DateFin:dd/MM/yyyy}");
                            column.Item().Text($"Lieu: {session.Lieu}");

                            // Programme
                            if (!string.IsNullOrEmpty(formation.Programme))
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("PROGRAMME").Bold();
                                });
                                column.Item().Text(formation.Programme);
                            }

                            // Modalités
                            if (!string.IsNullOrEmpty(formation.ModalitesPedagogiques))
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("MODALITÉS PÉDAGOGIQUES").Bold();
                                });
                                column.Item().Text(formation.ModalitesPedagogiques);
                            }

                            // Signature
                            column.Item().PaddingTop(50);
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Le client").FontSize(10);
                                    col.Item().Text(client.Nom).Bold();
                                    col.Item().Height(50);
                                    col.Item().Text("Signature").FontSize(10);
                                });
                                
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("L'organisme de formation").FontSize(10);
                                    col.Item().Text(_organizationService.GetOrganizationName()).Bold();
                                    col.Item().Height(50);
                                    col.Item().Text("Signature").FontSize(10);
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
                                .Text("Fait le " + DateTime.Now.ToString("dd/MM/yyyy"))
                                .FontSize(8);
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateAttestation(Stagiaire stagiaire, Session session)
        {
            var formation = session.Formation ?? _context.Formations.Find(session.FormationId);
            if (formation == null)
            {
                throw new InvalidOperationException("Formation introuvable pour la session.");
            }
            if (stagiaire.Client == null)
            {
                throw new InvalidOperationException("Client introuvable pour le stagiaire.");
            }
            
            var logoPath = _organizationService.GetLogoPath();
            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Calibri));

                    page.Header()
                        .Text("ATTESTATION DE FIN DE FORMATION")
                        .FontSize(18)
                        .FontColor(Colors.Blue.Medium)
                        .Bold()
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(2, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            column.Item().Text("Je soussigné(e), représentant(e) de l'organisme de formation, certifie que :");
                            
                            column.Item().PaddingTop(10);
                            column.Item().Text($"M./Mme {stagiaire.Prenom} {stagiaire.Nom}");
                            column.Item().Text($"Client: {stagiaire.Client.Nom}");
                            
                            column.Item().PaddingTop(20);
                            column.Item().Text("A suivi avec assiduité la formation :");
                            column.Item().Text($"Titre : {formation.Titre}");
                            column.Item().Text($"Durée : {formation.DureeHeures} heures");
                            column.Item().Text($"Dates : du {session.DateDebut:dd/MM/yyyy} au {session.DateFin:dd/MM/yyyy}");
                            column.Item().Text($"Lieu : {session.Lieu}");
                            
                            column.Item().PaddingTop(20);
                            column.Item().Text($"Nombre d'heures de présence : {stagiaire.HeuresPresence} heures");
                            
                            column.Item().PaddingTop(30);
                            column.Item().Text("Cette attestation est délivrée pour servir ce que de droit.");
                            
                            column.Item().PaddingTop(50);
                            column.Item().AlignCenter().Text("Fait à [Ville], le " + DateTime.Now.ToString("dd/MM/yyyy"));
                            
                            column.Item().PaddingTop(30);
                            column.Item().AlignCenter().Text(_organizationService.GetOrganizationName());
                            column.Item().AlignCenter().Text("Responsable de formation");
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
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateEmargement(Session session)
        {
            var formation = session.Formation ?? _context.Formations.Find(session.FormationId);
            if (formation == null)
            {
                throw new InvalidOperationException("Formation introuvable pour la session.");
            }
            var formateur = session.Formateur ?? _context.Formateurs.Find(session.FormateurId);
            var stagiaires = _context.Stagiaires.Where(s => s.SessionId == session.Id).ToList();
            var nomFormateur = formateur != null ? $"{formateur.Prenom} {formateur.Nom}" : "Formateur";

            // Hauteur minimale des cases signature (plus grandes pour signer)
            const float hauteurCaseSignature = 28f; // unités QuestPDF

            var logoPath = _organizationService.GetLogoPath();
            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));

                    page.Header()
                        .Text("FEUILLE D'ÉMARGEMENT")
                        .FontSize(16)
                        .FontColor(Colors.Blue.Medium)
                        .Bold()
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            // Informations session
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Formation: {formation.Titre}").Bold();
                            });
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Dates: {session.DateDebut:dd/MM/yyyy} - {session.DateFin:dd/MM/yyyy}");
                                row.ConstantItem(50).Text("");
                                row.RelativeItem().Text($"Lieu: {session.Lieu}").Bold();
                            });

                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            column.Item().PaddingTop(10);

                            // Tableau émargement : colonnes larges (cases signature confortables)
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(110);  // Date
                                    columns.ConstantColumn(200); // Nom & Prénom
                                    columns.ConstantColumn(280); // Signature matin
                                    columns.ConstantColumn(280); // Signature après-midi
                                });

                                // En-têtes
                                table.Header(header =>
                                {
                                    header.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(6)).Text("Date").Bold();
                                    header.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(6)).Text("Nom & Prénom").Bold();
                                    header.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(6)).Text("Signature Matin").Bold();
                                    header.Cell().Element(c => c.Background(Colors.Grey.Lighten3).Padding(6)).Text("Signature AM").Bold();
                                });

                                var jours = Enumerable.Range(0, (int)(session.DateFin - session.DateDebut).TotalDays + 1)
                                    .Select(i => session.DateDebut.AddDays(i));

                                foreach (var jour in jours)
                                {
                                    // Lignes stagiaires
                                    foreach (var stagiaire in stagiaires)
                                    {
                                        table.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5))
                                            .Text(jour.ToString("dd/MM/yyyy"));
                                        table.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5))
                                            .Text($"{stagiaire.Prenom} {stagiaire.Nom}");
                                        table.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Height(hauteurCaseSignature))
                                            .Text("");
                                        table.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Height(hauteurCaseSignature))
                                            .Text("");
                                    }
                                    // Ligne dédiée formateur (émargement du formateur)
                                    table.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Medium).Padding(5).Background(Colors.Grey.Lighten4))
                                        .Text(jour.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Medium).Padding(5).Background(Colors.Grey.Lighten4))
                                        .Text($"Formateur : {nomFormateur}").Bold();
                                    table.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Medium).Padding(8).Height(hauteurCaseSignature).Background(Colors.Grey.Lighten4))
                                        .Text("");
                                    table.Cell().Element(c => c.Border(1).BorderColor(Colors.Grey.Medium).Padding(8).Height(hauteurCaseSignature).Background(Colors.Grey.Lighten4))
                                        .Text("");
                                }
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
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateEvaluation(Stagiaire stagiaire)
        {
            // Récupérer la session et la formation pour afficher le nom
            Session? session = null;
            Formation? formation = null;
            
            if (stagiaire.Session != null)
            {
                session = stagiaire.Session;
                formation = session.Formation;
            }
            else if (stagiaire.SessionId.HasValue)
            {
                session = _context.Sessions
                    .Include(s => s.Formation)
                    .FirstOrDefault(s => s.Id == stagiaire.SessionId.Value);
                formation = session?.Formation;
            }
            
            if (formation == null && session?.FormationId > 0)
            {
                formation = _context.Formations.Find(session.FormationId);
            }

            var orgName = _organizationService.GetOrganizationName();
            var qualiopiCertified = _configuration.GetValue<bool>("Qualiopi:Certification", false);

            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    // Marges optimisées pour tenir sur 2 pages
                    page.Size(PageSizes.A4);
                    page.MarginVertical(1.2f, Unit.Centimetre);
                    page.MarginHorizontal(1.8f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Calibri));

                    // Header avec logo et nom organisation
                    var logoPath = _organizationService.GetLogoPath();
                    page.Header()
                        .Column(headerCol =>
                        {
                            headerCol.Spacing(5);
                            
                            // Logo organisation si disponible
                            if (!string.IsNullOrEmpty(logoPath) && System.IO.File.Exists(logoPath))
                            {
                                headerCol.Item()
                                    .AlignCenter()
                                    .MaxHeight(35)
                                    .Image(logoPath)
                                    .FitArea();
                            }
                            
                            // Logo Qualiopi si disponible (en plus du logo organisation)
                            if (System.IO.File.Exists(QualiopiLogoPath))
                            {
                                headerCol.Item()
                                    .AlignCenter()
                                    .MaxHeight(25)
                                    .Image(QualiopiLogoPath)
                                    .FitArea();
                            }
                            
                            // Nom organisation
                            headerCol.Item()
                                .AlignCenter()
                                .Text(orgName)
                                .FontSize(11)
                                .Bold();
                            
                            // Badge Qualiopi si certifié
                            if (qualiopiCertified)
                            {
                                headerCol.Item()
                                    .AlignCenter()
                                    .Text("Organisme certifié Qualiopi")
                                    .FontSize(7.5f)
                                    .FontColor(Colors.Grey.Medium);
                            }
                            
                            // Ligne de séparation
                            headerCol.Item()
                                .PaddingTop(3)
                                .LineHorizontal(1)
                                .LineColor(Colors.Blue.Medium);
                        });

                    page.Content()
                        .PaddingVertical(0.8f, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(8);

                            // Informations stagiaire et formation - Box compacte
                            column.Item()
                                .Background(Colors.Grey.Lighten5)
                                .Padding(8)
                                .Column(infoCol =>
                                {
                                    infoCol.Spacing(3);
                                    infoCol.Item().Text($"Stagiaire : {stagiaire.Prenom} {stagiaire.Nom}").FontSize(8.5f);
                                    infoCol.Item().Text($"Client : {stagiaire.Client?.Nom ?? "-"}").FontSize(8.5f);
                                    if (formation != null)
                                    {
                                        infoCol.Item().Text($"Formation : {formation.Titre}").FontSize(8.5f).Bold();
                                    }
                                    if (session != null)
                                    {
                                        infoCol.Item().Text($"Dates : {session.DateDebut:dd/MM/yyyy} au {session.DateFin:dd/MM/yyyy}").FontSize(8.5f);
                                    }
                                    infoCol.Item().Text($"Date de l'évaluation : {DateTime.Now:dd/MM/yyyy}").FontSize(8.5f);
                                });

                            column.Item().PaddingTop(6);

                            // ÉVALUATION À CHAUD (Modèle Kirkpatrick - Niveaux 1 & 2)
                            column.Item()
                                .AlignCenter()
                                .Text("ÉVALUATION À CHAUD (fin de session)")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);
                            
                            column.Item()
                                .AlignCenter()
                                .PaddingBottom(6)
                                .Text("Basée sur le modèle Kirkpatrick - Niveaux 1 (Réaction) et 2 (Apprentissage)")
                                .FontSize(7.5f)
                                .FontColor(Colors.Grey.Medium);
                            
                            column.Item().PaddingTop(4);
                            
                            // Niveau 1 : Réaction (Satisfaction)
                            column.Item()
                                .PaddingBottom(3)
                                .BorderBottom(1, Unit.Point)
                                .BorderColor(Colors.Blue.Medium)
                                .Text("1. SATISFACTION ET ENGAGEMENT (Niveau 1 - Réaction)")
                                .FontSize(11)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);
                            
                            column.Item()
                                .PaddingTop(3)
                                .Text("Dans quelle mesure êtes-vous satisfait(e) de cette formation ?")
                                .FontSize(9.5f);
                            
                            // Rating scale optimisé avec alignement parfait
                            column.Item()
                                .PaddingTop(2)
                                .Row(scaleRow =>
                                {
                                    scaleRow.ConstantItem(28)
                                        .Text("Très\ninsatisfait")
                                        .FontSize(6.5f)
                                        .AlignCenter()
                                        .LineHeight(1.15f);
                                    
                                    scaleRow.RelativeItem()
                                        .AlignCenter()
                                        .Row(ratingRow =>
                                        {
                                            for (int i = 1; i <= 5; i++)
                                            {
                                                ratingRow.ConstantItem(20)
                                                    .Column(ratingCol =>
                                                    {
                                                        ratingCol.Item()
                                                            .AlignCenter()
                                                            .Height(11)
                                                            .Width(11)
                                                            .Border(1, Unit.Point)
                                                            .BorderColor(Colors.Black);
                                                        ratingCol.Item()
                                                            .AlignCenter()
                                                            .PaddingTop(1)
                                                            .Text(i.ToString())
                                                            .FontSize(8.5f)
                                                            .Bold();
                                                    });
                                            }
                                        });
                                    
                                    scaleRow.ConstantItem(28)
                                        .Text("Très\nsatisfait")
                                        .FontSize(6.5f)
                                        .AlignCenter()
                                        .LineHeight(1.15f);
                                });
                            
                            column.Item()
                                .PaddingTop(3)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(50)
                                .Padding(5)
                                .Text("Commentaires sur la satisfaction :")
                                .FontSize(8.5f)
                                .FontColor(Colors.Grey.Medium);

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("L'organisation et le déroulement de la formation étaient-ils adaptés ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(50);

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Recommanderiez-vous cette formation à un collègue ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Row(row =>
                                {
                                    row.ConstantItem(50).Text("□ Oui").FontSize(9.5f);
                                    row.ConstantItem(50).Text("□ Non").FontSize(9.5f);
                                    row.ConstantItem(50).Text("□ Peut-être").FontSize(9.5f);
                                });

                            column.Item().PaddingTop(8);
                            
                            // Niveau 2 : Apprentissage
                            column.Item()
                                .PaddingBottom(3)
                                .BorderBottom(1, Unit.Point)
                                .BorderColor(Colors.Blue.Medium)
                                .Text("2. ACQUISITION DES CONNAISSANCES ET COMPÉTENCES (Niveau 2 - Apprentissage)")
                                .FontSize(11)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);
                            
                            column.Item()
                                .PaddingTop(3)
                                .Text("Évaluez votre niveau d'acquisition des compétences visées :")
                                .FontSize(9.5f);
                            
                            // Rating scale optimisé
                            column.Item()
                                .PaddingTop(2)
                                .Row(scaleRow =>
                                {
                                    scaleRow.ConstantItem(28)
                                        .Text("Aucune")
                                        .FontSize(6.5f)
                                        .AlignCenter();
                                    
                                    scaleRow.RelativeItem()
                                        .AlignCenter()
                                        .Row(ratingRow =>
                                        {
                                            for (int i = 1; i <= 5; i++)
                                            {
                                                ratingRow.ConstantItem(20)
                                                    .Column(ratingCol =>
                                                    {
                                                        ratingCol.Item()
                                                            .AlignCenter()
                                                            .Height(11)
                                                            .Width(11)
                                                            .Border(1, Unit.Point)
                                                            .BorderColor(Colors.Black);
                                                        ratingCol.Item()
                                                            .AlignCenter()
                                                            .PaddingTop(1)
                                                            .Text(i.ToString())
                                                            .FontSize(8.5f)
                                                            .Bold();
                                                    });
                                            }
                                        });
                                    
                                    scaleRow.ConstantItem(28)
                                        .Text("Maîtrisée")
                                        .FontSize(6.5f)
                                        .AlignCenter();
                                });
                            
                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Quels sont les 3 principaux apprentissages que vous retenez de cette formation ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(70);

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Y a-t-il des points que vous souhaiteriez approfondir ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(50);

                            column.Item().PaddingTop(8);
                            
                            // Section commentaires globaux (sur 2ème page si nécessaire)
                            column.Item()
                                .PageBreak();
                            
                            // ÉVALUATION À FROID (Modèle Kirkpatrick - Niveaux 3 & 4)
                            column.Item()
                                .AlignCenter()
                                .Text("ÉVALUATION À FROID (3 à 6 mois après la formation)")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);
                            
                            column.Item()
                                .AlignCenter()
                                .PaddingBottom(6)
                                .Text("Basée sur le modèle Kirkpatrick - Niveaux 3 (Comportement) et 4 (Résultats)")
                                .FontSize(7.5f)
                                .FontColor(Colors.Grey.Medium);
                            
                            column.Item()
                                .AlignCenter()
                                .Text("Date de remplissage : _______________")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);
                            
                            column.Item().PaddingTop(4);

                            // Niveau 3 : Comportement (Mise en pratique)
                            column.Item()
                                .PaddingBottom(3)
                                .BorderBottom(1, Unit.Point)
                                .BorderColor(Colors.Blue.Medium)
                                .Text("3. MISE EN PRATIQUE EN MILIEU PROFESSIONNEL (Niveau 3 - Comportement)")
                                .FontSize(11)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);
                            
                            column.Item()
                                .PaddingTop(3)
                                .Text("Avez-vous pu mettre en pratique les compétences acquises dans votre travail ?")
                                .FontSize(9.5f);
                            
                            column.Item()
                                .PaddingTop(2)
                                .Row(row =>
                                {
                                    row.ConstantItem(50).Text("□ Oui, régulièrement").FontSize(9.5f);
                                    row.ConstantItem(50).Text("□ Oui, occasionnellement").FontSize(9.5f);
                                    row.ConstantItem(50).Text("□ Non, pas encore").FontSize(9.5f);
                                    row.ConstantItem(50).Text("□ Non, pas applicable").FontSize(9.5f);
                                });

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Décrivez une situation concrète où vous avez appliqué ce que vous avez appris :")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(70);

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Quels obstacles avez-vous rencontrés pour mettre en pratique ces compétences ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(60);

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Quel soutien supplémentaire serait utile pour mieux appliquer ces compétences ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(50);

                            column.Item().PaddingTop(8);
                            
                            // Niveau 4 : Résultats (Impact)
                            column.Item()
                                .PaddingBottom(3)
                                .BorderBottom(1, Unit.Point)
                                .BorderColor(Colors.Blue.Medium)
                                .Text("4. IMPACT ET RÉSULTATS (Niveau 4 - Résultats)")
                                .FontSize(11)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);
                            
                            column.Item()
                                .PaddingTop(3)
                                .Text("Cette formation a-t-elle eu un impact mesurable sur votre performance professionnelle ?")
                                .FontSize(9.5f);
                            
                            column.Item()
                                .PaddingTop(2)
                                .Row(row =>
                                {
                                    row.ConstantItem(50).Text("□ Oui, impact significatif").FontSize(9.5f);
                                    row.ConstantItem(50).Text("□ Oui, impact modéré").FontSize(9.5f);
                                    row.ConstantItem(50).Text("□ Impact limité").FontSize(9.5f);
                                    row.ConstantItem(50).Text("□ Pas encore d'impact visible").FontSize(9.5f);
                                });

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Pouvez-vous citer des résultats concrets (amélioration de performance, gain de temps, qualité, etc.) ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(70);

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Cette formation a-t-elle contribué à atteindre vos objectifs professionnels ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(50);

                            column.Item().PaddingTop(8);
                            
                            // Suggestions d'amélioration
                            column.Item()
                                .PaddingBottom(3)
                                .BorderBottom(1, Unit.Point)
                                .BorderColor(Colors.Blue.Medium)
                                .Text("SUGGESTIONS D'AMÉLIORATION")
                                .FontSize(11)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);
                            
                            column.Item()
                                .PaddingTop(3)
                                .Text("Que pourrions-nous améliorer dans cette formation ?")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(60);

                            column.Item().PaddingTop(6);
                            column.Item()
                                .Text("Autres commentaires ou remarques :")
                                .FontSize(9.5f);
                            column.Item()
                                .PaddingTop(2)
                                .Border(1, Unit.Point)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Height(60);
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
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
