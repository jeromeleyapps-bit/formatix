using FormationManager.Data;
using FormationManager.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfDocument = QuestPDF.Fluent.Document;

namespace FormationManager.Services
{
    public interface IFacturationService
    {
        string GetNextNumeroDevis();
        string GetNextNumeroFacture();
        byte[] GenerateDevisPdf(Devis devis);
        byte[] GenerateFacturePdf(Facture facture);
    }

    public class FacturationService : IFacturationService
    {
        private readonly FormationDbContext _context;
        private readonly IOrganizationService _org;
        private const string QualiopiLogoPath = @"C:\AI\Opagax\logoqualiopi.png";

        public FacturationService(FormationDbContext context, IOrganizationService org)
        {
            _context = context;
            _org = org;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string GetNextNumeroDevis()
        {
            var y = DateTime.Today.Year;
            var count = _context.Devis.Count(d => d.DateCreation.Year == y);
            return $"DEV-{y}-{(count + 1):D4}";
        }

        public string GetNextNumeroFacture()
        {
            var y = DateTime.Today.Year;
            var count = _context.Factures.Count(f => f.DateEmission.Year == y);
            return $"FAC-{y}-{(count + 1):D4}";
        }

        public byte[] GenerateDevisPdf(Devis devis)
        {
            var client = devis.Client ?? _context.Clients.Find(devis.ClientId);
            var session = devis.SessionId.HasValue ? (devis.Session ?? _context.Sessions.Include(s => s.Formation).FirstOrDefault(s => s.Id == devis.SessionId)) : null;
            var formation = session?.Formation?.Titre;
            var ttc = devis.MontantHT * (1 + devis.TauxTVA / 100m);
            var tva = ttc - devis.MontantHT;

            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Calibri));

                    page.Header().Text("DEVIS").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                    page.Content()
                        .Column(c =>
                        {
                            c.Spacing(12);
                            c.Item().Text(_org.GetOrganizationName()).Bold();
                            var s = _org.GetSIRET(); if (!string.IsNullOrWhiteSpace(s)) c.Item().Text($"SIRET: {s}");
                            var a = _org.GetAddress(); if (!string.IsNullOrWhiteSpace(a)) c.Item().Text(a);
                            c.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            c.Item().Text($"Numéro: {devis.Numero}").Bold();
                            c.Item().Text($"Date: {devis.DateCreation:dd/MM/yyyy}");
                            if (devis.DateValidite.HasValue) c.Item().Text($"Valide jusqu'au: {devis.DateValidite:dd/MM/yyyy}");
                            c.Item().PaddingTop(8).Text("Client:").Bold();
                            c.Item().Text(client?.Nom ?? "");
                            if (!string.IsNullOrWhiteSpace(client?.Adresse)) c.Item().Text(client.Adresse);
                            if (!string.IsNullOrWhiteSpace(client?.CodePostal) || !string.IsNullOrWhiteSpace(client?.Ville))
                                c.Item().Text($"{client?.CodePostal} {client?.Ville}".Trim());
                            c.Item().PaddingTop(12).Text("Désignation:").Bold();
                            c.Item().Text(string.IsNullOrWhiteSpace(devis.Designation) ? (formation ?? "Prestation de formation") : devis.Designation);
                            c.Item().PaddingTop(12)
                                .Row(r =>
                                {
                                    r.RelativeItem().Text("Montant HT");
                                    r.ConstantItem(80).AlignRight().Text(devis.MontantHT.ToString("N2") + " €");
                                });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"TVA ({devis.TauxTVA}%)");
                                r.ConstantItem(80).AlignRight().Text(tva.ToString("N2") + " €");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total TTC").Bold();
                                r.ConstantItem(80).AlignRight().Text(ttc.ToString("N2") + " €").Bold();
                            });
                        });
                    page.Footer().AlignCenter().Column(c =>
                    {
                        c.Spacing(4);
                        if (System.IO.File.Exists(QualiopiLogoPath))
                            c.Item().Height(24).Width(80).Image(QualiopiLogoPath);
                        c.Item().Text(_org.GetOrganizationName()).FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });
            return document.GeneratePdf();
        }

        public byte[] GenerateFacturePdf(Facture facture)
        {
            var client = facture.Client ?? _context.Clients.Find(facture.ClientId);
            var session = facture.SessionId.HasValue ? (facture.Session ?? _context.Sessions.Include(s => s.Formation).FirstOrDefault(s => s.Id == facture.SessionId)) : null;
            var formation = session?.Formation?.Titre;
            var ttc = facture.MontantHT * (1 + facture.TauxTVA / 100m);
            var tva = ttc - facture.MontantHT;

            var document = PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Calibri));

                    page.Header().Text("FACTURE").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                    page.Content()
                        .Column(c =>
                        {
                            c.Spacing(12);
                            c.Item().Text(_org.GetOrganizationName()).Bold();
                            var s = _org.GetSIRET(); if (!string.IsNullOrWhiteSpace(s)) c.Item().Text($"SIRET: {s}");
                            var a = _org.GetAddress(); if (!string.IsNullOrWhiteSpace(a)) c.Item().Text(a);
                            c.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            c.Item().Text($"Numéro: {facture.Numero}").Bold();
                            c.Item().Text($"Date d'émission: {facture.DateEmission:dd/MM/yyyy}");
                            if (facture.DevisId.HasValue) c.Item().Text($"Devis: {facture.Devis?.Numero ?? ("#" + facture.DevisId)}");
                            c.Item().PaddingTop(8).Text("Client:").Bold();
                            c.Item().Text(client?.Nom ?? "");
                            if (!string.IsNullOrWhiteSpace(client?.Adresse)) c.Item().Text(client.Adresse);
                            if (!string.IsNullOrWhiteSpace(client?.CodePostal) || !string.IsNullOrWhiteSpace(client?.Ville))
                                c.Item().Text($"{client?.CodePostal} {client?.Ville}".Trim());
                            c.Item().PaddingTop(12).Text("Désignation:").Bold();
                            c.Item().Text(string.IsNullOrWhiteSpace(facture.Designation) ? (formation ?? "Prestation de formation") : facture.Designation);
                            c.Item().PaddingTop(12)
                                .Row(r =>
                                {
                                    r.RelativeItem().Text("Montant HT");
                                    r.ConstantItem(80).AlignRight().Text(facture.MontantHT.ToString("N2") + " €");
                                });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"TVA ({facture.TauxTVA}%)");
                                r.ConstantItem(80).AlignRight().Text(tva.ToString("N2") + " €");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total TTC").Bold();
                                r.ConstantItem(80).AlignRight().Text(ttc.ToString("N2") + " €").Bold();
                            });
                        });
                    page.Footer().AlignCenter().Column(c =>
                    {
                        c.Spacing(4);
                        if (System.IO.File.Exists(QualiopiLogoPath))
                            c.Item().Height(24).Width(80).Image(QualiopiLogoPath);
                        c.Item().Text(_org.GetOrganizationName()).FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });
            return document.GeneratePdf();
        }
    }
}
