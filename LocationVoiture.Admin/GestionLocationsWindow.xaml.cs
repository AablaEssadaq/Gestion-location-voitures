using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using LocationVoiture.Admin.Services;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class GestionLocationsWindow : Window
    {
        private DatabaseHelper db;
        private EmailService emailService;
        private PdfService pdfService; // Nouveau service

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
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView;
            if (row == null) return;

            int idLoc = Convert.ToInt32(row["Id"]);
            string email = row["EmailClient"].ToString();
            string nom = row["NomClient"].ToString();
            string voiture = row["ModeleVoiture"].ToString();
            DateTime debut = Convert.ToDateTime(row["DateDebut"]);
            DateTime fin = Convert.ToDateTime(row["DateFin"]);
            decimal prix = Convert.ToDecimal(row["PrixTotal"]);
            string dates = $"{debut:dd/MM} au {fin:dd/MM/yyyy}";

            if (MessageBox.Show("Confirmer et envoyer le bon ?", "Validation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    // 1. UPDATE BDD
                    db.ExecuteNonQuery("UPDATE Locations SET Statut = 'Confirmée' WHERE Id = @id",
                        new MySqlParameter[] { new MySqlParameter("@id", idLoc) });

                    // 2. GÉNÉRER LE PDF
                    byte[] pdf = pdfService.GenererBonReservation(idLoc, nom, voiture, debut, fin, prix);

                    // 3. ENVOYER EMAIL AVEC PDF
                    await emailService.EnvoyerEmailConfirmation(email, nom, voiture, dates, prix, pdf);

                    MessageBox.Show("Succès ! Bon envoyé au client.");
                    ChargerLocations();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message);
                }
            }
        }

        private void BtnRefuser_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView;
            int id = Convert.ToInt32(row["Id"]);
            if (MessageBox.Show("Refuser ?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                db.ExecuteNonQuery("UPDATE Locations SET Statut = 'Annulée' WHERE Id = @id",
                    new MySqlParameter[] { new MySqlParameter("@id", id) });
                ChargerLocations();
            }
        }
    }
}