using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class GestionEntretiensWindow : Window
    {
        private DatabaseHelper db;
        private bool _isLoaded = false;

        // Variables Pagination - Alertes
        private int _pageAlertes = 1;
        private int _totalAlertes = 0;

        // Variables Pagination - Historique
        private int _pageHist = 1;
        private int _totalHist = 0;

        private int _pageSize = 12;

        public GestionEntretiensWindow()
        {
            InitializeComponent();

            try { db = new DatabaseHelper(); }
            catch (Exception ex) { MessageBox.Show("Erreur BDD : " + ex.Message); return; }

            _isLoaded = true;
            ChargerFiltresTypes();
            ChargerAlertes();
            ChargerHistorique();
        }

        private void ChargerFiltresTypes()
        {
            if (db == null) return;
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT DISTINCT Nom FROM TypeEntretien ORDER BY Nom");

                // Remplissage des deux combobox
                cbFiltreTypeAlerte.Items.Add("Tous les types");
                cbFiltreTypeHist.Items.Add("Tous les types");

                foreach (DataRow row in dt.Rows)
                {
                    string nom = row["Nom"].ToString();
                    cbFiltreTypeAlerte.Items.Add(nom);
                    cbFiltreTypeHist.Items.Add(nom);
                }

                cbFiltreTypeAlerte.SelectedIndex = 0;
                cbFiltreTypeHist.SelectedIndex = 0;
            }
            catch { }
        }

        // ==========================================
        // LOGIQUE ALERTES
        // ==========================================
        private void ChargerAlertes()
        {
            if (!_isLoaded || db == null) return;

            try
            {
                string condition = "WHERE (KilometrageActuel - KmDernierEntretien) >= 5000"; // Seuil exemple
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                // Recherche
                if (!string.IsNullOrWhiteSpace(txtRechercheAlertes.Text))
                {
                    condition += " AND (Marque LIKE @s OR Modele LIKE @s OR Matricule LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRechercheAlertes.Text + "%"));
                }

                // Filtre Type (Si on avait stocké le type d'entretien dans la voiture, ici on filtre sur NomProchainEntretien)
                if (cbFiltreTypeAlerte.SelectedItem != null && cbFiltreTypeAlerte.SelectedIndex > 0)
                {
                    string type = cbFiltreTypeAlerte.SelectedItem.ToString();
                    condition += " AND NomProchainEntretien = @type";
                    parameters.Add(new MySqlParameter("@type", type));
                }

                // Pagination
                string countQuery = $"SELECT COUNT(*) FROM Voitures {condition}";
                _totalAlertes = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalAlertes / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_pageAlertes > totalPages) _pageAlertes = totalPages;
                if (_pageAlertes < 1) _pageAlertes = 1;

                lblPaginationInfoAlertes.Text = $"Page {_pageAlertes} sur {totalPages} ({_totalAlertes})";
                btnPrevAlertes.IsEnabled = _pageAlertes > 1;
                btnNextAlertes.IsEnabled = _pageAlertes < totalPages;

                int offset = (_pageAlertes - 1) * _pageSize;
                string query = $@"
                    SELECT Id, CONCAT(Marque, ' ', Modele) AS Vehicule, Matricule, 
                           KilometrageActuel, KmDernierEntretien, NomProchainEntretien
                    FROM Voitures
                    {condition}
                    LIMIT {_pageSize} OFFSET {offset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());
                AlertesGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur Alertes : " + ex.Message); }
        }

        // Events UI - Alertes
        private void TxtRechercheAlertes_TextChanged(object sender, TextChangedEventArgs e) { _pageAlertes = 1; ChargerAlertes(); }
        private void CbFiltreTypeAlerte_SelectionChanged(object sender, SelectionChangedEventArgs e) { _pageAlertes = 1; ChargerAlertes(); }
        private void BtnResetAlertes_Click(object sender, RoutedEventArgs e) { txtRechercheAlertes.Text = ""; cbFiltreTypeAlerte.SelectedIndex = 0; _pageAlertes = 1; ChargerAlertes(); }
        private void BtnPrevAlertes_Click(object sender, RoutedEventArgs e) { if (_pageAlertes > 1) { _pageAlertes--; ChargerAlertes(); } }
        private void BtnNextAlertes_Click(object sender, RoutedEventArgs e) { _pageAlertes++; ChargerAlertes(); }


        // ==========================================
        // LOGIQUE HISTORIQUE
        // ==========================================
        private void ChargerHistorique()
        {
            if (!_isLoaded || db == null) return;

            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrWhiteSpace(txtRechercheHist.Text))
                {
                    condition += " AND (v.Marque LIKE @s OR v.Modele LIKE @s OR v.Matricule LIKE @s OR e.Description LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRechercheHist.Text + "%"));
                }

                if (cbFiltreTypeHist.SelectedItem != null && cbFiltreTypeHist.SelectedIndex > 0)
                {
                    string type = cbFiltreTypeHist.SelectedItem.ToString();
                    condition += " AND e.TypeEntretien = @type";
                    parameters.Add(new MySqlParameter("@type", type));
                }

                string orderBy = "e.DateEntretien DESC";
                if (cbTriHist.SelectedItem is ComboBoxItem item && item.Tag != null) orderBy = item.Tag.ToString();

                string countQuery = $"SELECT COUNT(*) FROM Entretiens e JOIN Voitures v ON e.VoitureId = v.Id {condition}";
                _totalHist = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalHist / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_pageHist > totalPages) _pageHist = totalPages;
                if (_pageHist < 1) _pageHist = 1;

                lblPaginationInfoHist.Text = $"Page {_pageHist} sur {totalPages} ({_totalHist})";
                btnPrevHist.IsEnabled = _pageHist > 1;
                btnNextHist.IsEnabled = _pageHist < totalPages;

                int offset = (_pageHist - 1) * _pageSize;
                string query = $@"
                    SELECT e.Id AS IdEntretien, e.DateEntretien, e.TypeEntretien, e.Kilometrage, e.Cout, e.Description,
                           CONCAT(v.Marque, ' ', v.Modele) AS Vehicule, v.Matricule
                    FROM Entretiens e
                    JOIN Voitures v ON e.VoitureId = v.Id
                    {condition}
                    ORDER BY {orderBy}
                    LIMIT {_pageSize} OFFSET {offset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());
                HistoriqueGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur Historique : " + ex.Message); }
        }

        // Events UI - Historique
        private void TxtRechercheHist_TextChanged(object sender, TextChangedEventArgs e) { _pageHist = 1; ChargerHistorique(); }
        private void CbFiltreTypeHist_SelectionChanged(object sender, SelectionChangedEventArgs e) { _pageHist = 1; ChargerHistorique(); }
        private void CbTriHist_SelectionChanged(object sender, SelectionChangedEventArgs e) { ChargerHistorique(); }
        private void BtnResetHist_Click(object sender, RoutedEventArgs e) { txtRechercheHist.Text = ""; cbFiltreTypeHist.SelectedIndex = 0; cbTriHist.SelectedIndex = 0; _pageHist = 1; ChargerHistorique(); }
        private void BtnPrevHist_Click(object sender, RoutedEventArgs e) { if (_pageHist > 1) { _pageHist--; ChargerHistorique(); } }
        private void BtnNextHist_Click(object sender, RoutedEventArgs e) { _pageHist++; ChargerHistorique(); }

        // Event Changement d'Onglet
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                // On recharge les données de l'onglet actif pour être à jour
                if (AlertesGrid.IsVisible) ChargerAlertes();
                else ChargerHistorique();
            }
        }

        // --- ACTIONS COMMUNES ---
        private void BtnAjouter_Click(object sender, RoutedEventArgs e) { new AjouterEntretienWindow().ShowDialog(); ChargerAlertes(); ChargerHistorique(); }
        private void BtnConfig_Click(object sender, RoutedEventArgs e) { new GestionTypesEntretienWindow().ShowDialog(); ChargerFiltresTypes(); }
        private void BtnFaireEntretien_Click(object sender, RoutedEventArgs e) { int id = Convert.ToInt32(((Button)sender).Tag); new AjouterEntretienWindow(id).ShowDialog(); ChargerAlertes(); ChargerHistorique(); }

        private void BtnSupprimerHist_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Supprimer ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try { db.ExecuteNonQuery("DELETE FROM Entretiens WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", Convert.ToInt32(((Button)sender).Tag)) }); ChargerHistorique(); }
                catch { MessageBox.Show("Erreur suppression."); }
            }
        }

        private void BtnModifierHist_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);
            new AjouterEntretienWindow(0, id).ShowDialog();
            ChargerHistorique();
        }
    }
}