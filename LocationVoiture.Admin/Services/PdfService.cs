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
        public PdfService()
        {
            // Licence communautaire gratuite requise pour QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenererBonReservation(int idLocation, string nomClient, string voiture, DateTime debut, DateTime fin, decimal prix)
        {
            // 1. GÉNÉRER LE QR CODE
            byte[] qrCodeImage = GenererQrCode($"LOC-{idLocation}-{nomClient}");

            // 2. GÉNÉRER LE PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // En-tête
                    page.Header()
                        .Text($"BON DE RÉSERVATION #{idLocation}")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium);

                    // Contenu
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            x.Item().Text($"Date d'émission : {DateTime.Now:dd/MM/yyyy}");
                            x.Item().Text($"Client : {nomClient}").Bold();

                            // Tableau des détails
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

                                    // CORRECTION ICI : Utilisation de DefaultTextStyle pour appliquer le style au texte
                                    static IContainer CellStyle(IContainer container) =>
                                        container.BorderBottom(1).BorderColor("#E0E0E0").Padding(5).DefaultTextStyle(x => x.SemiBold());
                                });

                                table.Cell().Padding(5).Text("Véhicule");
                                table.Cell().Padding(5).Text(voiture);

                                table.Cell().Padding(5).Text("Date de début");
                                table.Cell().Padding(5).Text(debut.ToString("dd/MM/yyyy"));

                                table.Cell().Padding(5).Text("Date de fin");
                                table.Cell().Padding(5).Text(fin.ToString("dd/MM/yyyy"));

                                table.Cell().Padding(5).Text("PRIX TOTAL");
                                table.Cell().Padding(5).Text($"{prix} DH").Bold().FontColor(Colors.Green.Medium);
                            });

                            // Affichage du QR Code
                            x.Item().AlignRight().Element(c => c.Height(100).Width(100).Image(qrCodeImage));
                            x.Item().AlignRight().Text("Scannez ce code à l'agence").FontSize(10).Italic();
                        });

                    // Pied de page
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
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