using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace LocationVoiture.Admin.Services
{
    public class PdfService
    {
        // ⚠️ CHANGEZ CE PORT SELON VOTRE CONFIGURATION (Voir barre d'adresse du navigateur quand le site est lancé)
        private const string BaseUrl = "https://localhost:56755";

        public PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenererBonReservation(int idLocation, string nomClient, string voiture, DateTime debut, DateTime fin, decimal prix)
        {
            // 1. GÉNÉRER L'URL POUR LE QR CODE
            // Cela crée un lien cliquable/scannable vers la page de détails
            string urlScan = $"{BaseUrl}/Booking/Details/{idLocation}";

            byte[] qrCodeImage = GenererQrCode(urlScan);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text($"BON DE RÉSERVATION #{idLocation}")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);
                            x.Item().Text($"Date d'émission : {DateTime.Now:dd/MM/yyyy}");
                            x.Item().Text($"Client : {nomClient}").Bold();

                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Désignation");
                                    header.Cell().Element(CellStyle).Text("Détail");
                                    static IContainer CellStyle(IContainer container) =>
                                        container.BorderBottom(1).BorderColor("#E0E0E0").Padding(5).DefaultTextStyle(t => t.SemiBold());
                                });

                                table.Cell().Padding(5).Text("Véhicule");
                                table.Cell().Padding(5).Text(voiture);

                                table.Cell().Padding(5).Text("Période");
                                table.Cell().Padding(5).Text($"Du {debut:dd/MM} au {fin:dd/MM/yyyy}");

                                table.Cell().Padding(5).Text("PRIX TOTAL");
                                table.Cell().Padding(5).Text($"{prix} DH").Bold().FontColor(Colors.Green.Medium);
                            });

                            // QR CODE AVEC INSTRUCTION
                            x.Item().AlignRight().Column(col =>
                            {
                                col.Item().Element(c => c.Height(100).Width(100).Image(qrCodeImage));
                                col.Item().Text("Scanner pour voir en ligne").FontSize(9).Italic();
                            });
                        });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private byte[] GenererQrCode(string contenu)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(contenu, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                return qrCode.GetGraphic(20);
            }
        }
    }
}