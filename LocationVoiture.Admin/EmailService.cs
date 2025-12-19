using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace LocationVoiture.Admin.Services
{
    public class EmailService
    {
        private string _smtpServer = "smtp.gmail.com";
        private int _port = 587;
        private string _senderEmail = "gestionlocationvoitures@gmail.com";
        private string _password = "llid wwro mrdg ypak";

        public async Task EnvoyerEmailConfirmation(string destinataire, string nomClient, string voiture, string dates, decimal prix, byte[] pdfBytes = null)
        {
            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress(_senderEmail, "Location Voiture Admin");
                mail.To.Add(destinataire);
                mail.Subject = "Confirmation de réservation - Bon inclus";

                mail.Body = $@"
                    Bonjour {nomClient},

                    Votre réservation pour la {voiture} est confirmée !
                    Veuillez trouver ci-joint votre bon de réservation avec QR Code.

                    Dates : {dates}
                    Prix : {prix} DH

                    Cordialement,
                    L'équipe.
                ";

                if (pdfBytes != null)
                {
                    mail.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), "BonReservation.pdf", "application/pdf"));
                }

                using (var client = new SmtpClient(_smtpServer, _port))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_senderEmail, _password);
                    await client.SendMailAsync(mail);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur Email : " + ex.Message);
            }
        }
    }
}