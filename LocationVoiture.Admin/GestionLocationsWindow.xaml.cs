using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using LocationVoiture.Admin.Services;
using MySql.Data.MySqlClient;
using System.IO;
using System.Diagnostics;

namespace LocationVoiture.Admin
{
    public partial class GestionLocationsWindow : Window
    {
        private DatabaseHelper db;
        private EmailService emailService;
        private PdfService pdfService;

        public GestionLocationsWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            emailService = new EmailService();
            pdfService = new PdfService();
            ChargerLocations();
        }

        private void ChargerLocations()
        {
            try
            {
                string query = @"
                    SELECT l.Id, l.DateDebut, l.DateFin, l.PrixTotal, l.Statut,
                           CONCAT(c.Prenom, ' ', c.Nom) AS NomClient, c.Email AS EmailClient,
                           CONCAT(v.Marque, ' ', v.Modele) AS ModeleVoiture
                    FROM Locations l
                    JOIN Clients c ON l.ClientId = c.Id
                    JOIN Voitures v ON l.VoitureId = v.Id
                    ORDER BY l.Id DESC";

                DataTable dt = db.ExecuteQuery(query);
                LocationsGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        private async void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView;
            if (row == null) return;

            int idLoc = Convert.ToInt32(row["Id"]);

            if (row["Statut"].ToString() == "Confirmée")
            {
                MessageBox.Show("Cette réservation est déjà confirmée.");
                return;
            }

            string email = row["EmailClient"].ToString();
            string nom = row["NomClient"].ToString();
            string voiture = row["ModeleVoiture"].ToString();
            DateTime debut = Convert.ToDateTime(row["DateDebut"]);
            DateTime fin = Convert.ToDateTime(row["DateFin"]);
            decimal prix = Convert.ToDecimal(row["PrixTotal"]);
            string dates = $"{debut:dd/MM} au {fin:dd/MM/yyyy}";

            // 1. Demande de confirmation simple
            if (MessageBox.Show("Confirmer la réservation, envoyer l'email et générer le bon ?", "Validation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // A. GÉNÉRER LE PDF
                    byte[] pdfBytes = pdfService.GenererBonReservation(idLoc, nom, voiture, debut, fin, prix);

                    // B. STOCKER EN BASE DE DONNÉES
                    string queryUpdate = "UPDATE Locations SET Statut = 'Confirmée', BonReservation = @pdf WHERE Id = @id";
                    MySqlParameter[] p = {
                        new MySqlParameter("@id", idLoc),
                        new MySqlParameter("@pdf", pdfBytes)
                    };
                    db.ExecuteNonQuery(queryUpdate, p);

                    // C. ENVOYER L'EMAIL
                    await emailService.EnvoyerEmailConfirmation(email, nom, voiture, dates, prix, pdfBytes);

                    // D. OUVRIR LE PDF AUTOMATIQUEMENT
                    string tempFile = Path.Combine(Path.GetTempPath(), $"Bon_Reservation_{idLoc}.pdf");
                    File.WriteAllBytes(tempFile, pdfBytes);
                    Process.Start(new ProcessStartInfo { FileName = tempFile, UseShellExecute = true });

                    MessageBox.Show("Opération terminée avec succès !");
                    ChargerLocations();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message);
                }
            }
        }

        // === BOUTON POUR VOIR LE REÇU (PDF) ===
        private void BtnVoirRecu_Click(object sender, RoutedEventArgs e)
        {
            int idLoc = Convert.ToInt32(((Button)sender).Tag);

            try
            {
                // 1. On récupère le fichier PDF stocké en BLOB dans la base de données
                string query = "SELECT BonReservation FROM Locations WHERE Id = @id";
                MySqlParameter[] p = { new MySqlParameter("@id", idLoc) };

                object result = db.ExecuteScalar(query, p);

                if (result == DBNull.Value || result == null)
                {
                    MessageBox.Show("Aucun reçu de réservation disponible pour cette location.");
                    return;
                }

                byte[] pdfBytes = (byte[])result;

                // 2. On le sauvegarde temporairement sur le disque
                string tempFile = Path.Combine(Path.GetTempPath(), $"Recu_Reservation_{idLoc}.pdf");
                File.WriteAllBytes(tempFile, pdfBytes);

                // 3. On l'ouvre avec le lecteur PDF par défaut
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir le reçu : " + ex.Message);
            }
        }

        private void BtnRefuser_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView;
            int id = Convert.ToInt32(row["Id"]);
            if (MessageBox.Show("Refuser cette demande ?", "Annulation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                db.ExecuteNonQuery("UPDATE Locations SET Statut = 'Annulée', BonReservation = NULL WHERE Id = @id",
                    new MySqlParameter[] { new MySqlParameter("@id", id) });
                ChargerLocations();
            }
        }
    }
}