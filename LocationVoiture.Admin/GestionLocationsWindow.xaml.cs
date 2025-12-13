using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using LocationVoiture.Admin.Services;
using MySql.Data.MySqlClient;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace LocationVoiture.Admin
{
    public partial class GestionLocationsWindow : Window
    {
        private DatabaseHelper db;
        private EmailService emailService;
        private PdfService pdfService;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public GestionLocationsWindow()
        {
            InitializeComponent();

            try
            {
                db = new DatabaseHelper();
                emailService = new EmailService();
                pdfService = new PdfService();
            }
            catch (Exception ex) { MessageBox.Show("Erreur Init : " + ex.Message); return; }

            _isLoaded = true;
            ChargerLocations();
        }

        private void ChargerLocations()
        {
            if (!_isLoaded || db == null) return;

            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                // A. Recherche (Nom Client, Marque Voiture)
                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (c.Nom LIKE @s OR c.Prenom LIKE @s OR v.Marque LIKE @s OR v.Modele LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRecherche.Text + "%"));
                }

                // B. Filtre Statut
                if (cbFiltreStatut.SelectedIndex > 0)
                {
                    string statut = (cbFiltreStatut.SelectedItem as ComboBoxItem).Content.ToString();
                    condition += " AND l.Statut = @statut";
                    parameters.Add(new MySqlParameter("@statut", statut));
                }

                // C. Filtre Paiement
                if (cbFiltrePaiement.SelectedItem is ComboBoxItem itemPay && itemPay.Tag.ToString() != "-1")
                {
                    condition += " AND l.EstPaye = @paye";
                    parameters.Add(new MySqlParameter("@paye", int.Parse(itemPay.Tag.ToString())));
                }

                // D. Tri
                string orderBy = "l.Id DESC";
                if (cbTri.SelectedItem is ComboBoxItem itemSort && itemSort.Tag != null)
                {
                    orderBy = itemSort.Tag.ToString();
                }

                // E. Pagination (Count)
                string countQuery = $@"SELECT COUNT(*) FROM Locations l 
                                       JOIN Clients c ON l.ClientId = c.Id 
                                       JOIN Voitures v ON l.VoitureId = v.Id 
                                       {condition}";
                _totalRecords = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_currentPage > totalPages) _currentPage = totalPages;
                if (_currentPage < 1) _currentPage = 1;

                lblPaginationInfo.Text = $"Page {_currentPage} sur {totalPages} ({_totalRecords} locations)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                // F. Requête Finale
                int offset = (_currentPage - 1) * _pageSize;
                string query = $@"
                    SELECT l.Id, l.DateDebut, l.DateFin, l.PrixTotal, l.Statut, l.EstPaye,
                           CONCAT(c.Prenom, ' ', c.Nom) AS NomClient, c.Email AS EmailClient,
                           CONCAT(v.Marque, ' ', v.Modele) AS ModeleVoiture
                    FROM Locations l
                    JOIN Clients c ON l.ClientId = c.Id
                    JOIN Voitures v ON l.VoitureId = v.Id
                    {condition}
                    ORDER BY {orderBy}
                    LIMIT {_pageSize} OFFSET {offset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());
                LocationsGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        // --- EVENTS UI ---
        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) { _currentPage = 1; ChargerLocations(); }
        private void CbFiltre_SelectionChanged(object sender, SelectionChangedEventArgs e) { _currentPage = 1; ChargerLocations(); }
        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e) { ChargerLocations(); }
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtRecherche.Text = ""; cbFiltreStatut.SelectedIndex = 0; cbFiltrePaiement.SelectedIndex = 0; cbTri.SelectedIndex = 0; _currentPage = 1; ChargerLocations(); }
        private void BtnPrev_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; ChargerLocations(); } }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { _currentPage++; ChargerLocations(); }


        // --- ACTIONS EXISTANTES (Payer, Valider, Reçu, Refuser) ---
        // (Copiez ici vos méthodes BtnPayer_Click, BtnValider_Click, etc. telles quelles)
        // Je ne les répète pas pour ne pas surcharger, elles restent identiques.

        private void BtnPayer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int idLoc = Convert.ToInt32(((Button)sender).Tag);
                string queryPrix = "SELECT PrixTotal FROM Locations WHERE Id = @id";
                object result = db.ExecuteScalar(queryPrix, new MySqlParameter[] { new MySqlParameter("@id", idLoc) });

                decimal prixTotal = 0;
                if (result != null)
                {
                    string prixStr = result.ToString().Replace(",", ".");
                    decimal.TryParse(prixStr, NumberStyles.Any, CultureInfo.InvariantCulture, out prixTotal);
                }

                AjouterPaiementWindow winPaiement = new AjouterPaiementWindow(idLoc, prixTotal);
                winPaiement.ShowDialog();
                ChargerLocations();
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private async void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView;
            if (row == null) return;

            int idLoc = Convert.ToInt32(row["Id"]);
            if (row["Statut"].ToString() == "Confirmée") { MessageBox.Show("Déjà confirmée."); return; }

            string email = row["EmailClient"].ToString();
            string nom = row["NomClient"].ToString();
            string voiture = row["ModeleVoiture"].ToString();
            DateTime debut = Convert.ToDateTime(row["DateDebut"]);
            DateTime fin = Convert.ToDateTime(row["DateFin"]);

            string prixStr = row["PrixTotal"].ToString().Replace(",", ".");
            decimal prix = 0; decimal.TryParse(prixStr, NumberStyles.Any, CultureInfo.InvariantCulture, out prix);
            string dates = $"{debut:dd/MM} au {fin:dd/MM/yyyy}";

            if (MessageBox.Show("Confirmer la réservation ?", "Validation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    byte[] pdfBytes = pdfService.GenererBonReservation(idLoc, nom, voiture, debut, fin, prix);

                    db.ExecuteNonQuery("UPDATE Locations SET Statut = 'Confirmée', BonReservation = @pdf WHERE Id = @id",
                        new MySqlParameter[] { new MySqlParameter("@id", idLoc), new MySqlParameter("@pdf", pdfBytes) });

                    await emailService.EnvoyerEmailConfirmation(email, nom, voiture, dates, prix, pdfBytes);

                    string tempFile = Path.Combine(Path.GetTempPath(), $"Bon_{idLoc}.pdf");
                    File.WriteAllBytes(tempFile, pdfBytes);
                    Process.Start(new ProcessStartInfo { FileName = tempFile, UseShellExecute = true });

                    MessageBox.Show("Validé !");
                    ChargerLocations();
                }
                catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
            }
        }

        private void BtnVoirRecu_Click(object sender, RoutedEventArgs e)
        {
            int idLoc = Convert.ToInt32(((Button)sender).Tag);
            try
            {
                object res = db.ExecuteScalar("SELECT BonReservation FROM Locations WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", idLoc) });
                if (res == DBNull.Value || res == null) { MessageBox.Show("Pas de reçu."); return; }

                byte[] pdfBytes = (byte[])res;
                string tempFile = Path.Combine(Path.GetTempPath(), $"Recu_{idLoc}.pdf");
                File.WriteAllBytes(tempFile, pdfBytes);
                Process.Start(new ProcessStartInfo { FileName = tempFile, UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private void BtnRefuser_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView;
            if (row == null) return;
            int id = Convert.ToInt32(row["Id"]);

            if (MessageBox.Show("Annuler ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                db.ExecuteNonQuery("UPDATE Locations SET Statut = 'Annulée', BonReservation = NULL WHERE Id = @id",
                    new MySqlParameter[] { new MySqlParameter("@id", id) });
                ChargerLocations();
            }
        }
    }
}