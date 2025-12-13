using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class GestionPaiementsWindow : Window
    {
        private DatabaseHelper db;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 15;
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public GestionPaiementsWindow()
        {
            InitializeComponent();

            try { db = new DatabaseHelper(); }
            catch (Exception ex) { MessageBox.Show("Erreur BDD : " + ex.Message); return; }

            _isLoaded = true;
            ChargerPaiements();
        }

        private void ChargerPaiements()
        {
            if (!_isLoaded || db == null) return;

            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                // A. Recherche (Client, Voiture)
                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (c.Nom LIKE @s OR c.Prenom LIKE @s OR v.Marque LIKE @s OR v.Modele LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRecherche.Text + "%"));
                }

                // B. Filtre Méthode
                if (cbFiltreMethode.SelectedIndex > 0)
                {
                    string methode = (cbFiltreMethode.SelectedItem as ComboBoxItem).Content.ToString();
                    condition += " AND p.Methode = @methode";
                    parameters.Add(new MySqlParameter("@methode", methode));
                }

                // C. Tri
                string orderBy = "p.DatePaiement DESC";
                if (cbTri.SelectedItem is ComboBoxItem itemSort && itemSort.Tag != null)
                {
                    orderBy = itemSort.Tag.ToString();
                }

                // D. Pagination (Count)
                string countQuery = $@"SELECT COUNT(*) FROM Paiements p 
                                       JOIN Locations l ON p.LocationId = l.Id
                                       JOIN Clients c ON l.ClientId = c.Id
                                       JOIN Voitures v ON l.VoitureId = v.Id
                                       {condition}";
                _totalRecords = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_currentPage > totalPages) _currentPage = totalPages;
                if (_currentPage < 1) _currentPage = 1;

                lblPaginationInfo.Text = $"Page {_currentPage} / {totalPages} ({_totalRecords} paiements)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                // E. Requête Finale
                int offset = (_currentPage - 1) * _pageSize;
                string query = $@"
                    SELECT p.Id, p.Montant, p.DatePaiement, p.Methode, p.LocationId,
                           CONCAT(c.Prenom, ' ', c.Nom) AS NomClient,
                           CONCAT(v.Marque, ' ', v.Modele) AS ModeleVoiture
                    FROM Paiements p
                    JOIN Locations l ON p.LocationId = l.Id
                    JOIN Clients c ON l.ClientId = c.Id
                    JOIN Voitures v ON l.VoitureId = v.Id
                    {condition}
                    ORDER BY {orderBy}
                    LIMIT {_pageSize} OFFSET {offset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());
                PaiementsGrid.ItemsSource = dt.DefaultView;

                // F. Calcul du Total (Sur la sélection filtrée, pas juste la page)
                // Pour avoir le vrai total filtré, on fait une petite requête SUM
                string sumQuery = $@"SELECT SUM(p.Montant) FROM Paiements p 
                                     JOIN Locations l ON p.LocationId = l.Id
                                     JOIN Clients c ON l.ClientId = c.Id
                                     JOIN Voitures v ON l.VoitureId = v.Id
                                     {condition}";

                object sumRes = db.ExecuteScalar(sumQuery, parameters.ToArray());
                decimal total = (sumRes != DBNull.Value && sumRes != null) ? Convert.ToDecimal(sumRes) : 0;

                txtTotalGeneral.Text = $"{total:N2} DH";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message);
            }
        }

        // --- EVENTS UI ---
        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) { _currentPage = 1; ChargerPaiements(); }
        private void CbFiltre_SelectionChanged(object sender, SelectionChangedEventArgs e) { _currentPage = 1; ChargerPaiements(); }
        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e) { ChargerPaiements(); }
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtRecherche.Text = ""; cbFiltreMethode.SelectedIndex = 0; cbTri.SelectedIndex = 0; _currentPage = 1; ChargerPaiements(); }
        private void BtnPrev_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; ChargerPaiements(); } }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { _currentPage++; ChargerPaiements(); }
    }
}