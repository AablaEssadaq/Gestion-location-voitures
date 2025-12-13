using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class GestionUtilisateursWindow : Window
    {
        private DatabaseHelper db;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public GestionUtilisateursWindow()
        {
            InitializeComponent();

            try { db = new DatabaseHelper(); }
            catch (Exception ex) { MessageBox.Show("Erreur BDD : " + ex.Message); return; }

            _isLoaded = true;
            ChargerUtilisateurs();
        }

        private void ChargerUtilisateurs()
        {
            if (!_isLoaded || db == null) return;

            try
            {
                // 1. WHERE Dynamique
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                // Recherche
                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (Nom LIKE @s OR Prenom LIKE @s OR Email LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRecherche.Text + "%"));
                }

                // Filtre Rôle
                if (cbFiltreRole.SelectedIndex > 0)
                {
                    string role = (cbFiltreRole.SelectedItem as ComboBoxItem).Content.ToString();
                    condition += " AND Role = @role";
                    parameters.Add(new MySqlParameter("@role", role));
                }

                // 2. Pagination
                string countQuery = $"SELECT COUNT(*) FROM Utilisateurs {condition}";
                _totalRecords = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_currentPage > totalPages) _currentPage = totalPages;
                if (_currentPage < 1) _currentPage = 1;

                lblPaginationInfo.Text = $"Page {_currentPage} sur {totalPages} ({_totalRecords} utilisateurs)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                // 3. Requête Finale
                int offset = (_currentPage - 1) * _pageSize;
                string query = $"SELECT Id, Nom, Prenom, Email, Role FROM Utilisateurs {condition} ORDER BY Id DESC LIMIT {_pageSize} OFFSET {offset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());
                UsersGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        // --- EVENTS UI ---
        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) { _currentPage = 1; ChargerUtilisateurs(); }
        private void CbFiltreRole_SelectionChanged(object sender, SelectionChangedEventArgs e) { _currentPage = 1; ChargerUtilisateurs(); }
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtRecherche.Text = ""; cbFiltreRole.SelectedIndex = 0; _currentPage = 1; ChargerUtilisateurs(); }
        private void BtnPrev_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; ChargerUtilisateurs(); } }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { _currentPage++; ChargerUtilisateurs(); }

        // --- ACTIONS CRUD ---
        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterUtilisateurWindow win = new AjouterUtilisateurWindow();
            win.ShowDialog();
            ChargerUtilisateurs();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);
            AjouterUtilisateurWindow win = new AjouterUtilisateurWindow(id);
            win.ShowDialog();
            ChargerUtilisateurs();
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);
            if (MessageBox.Show("Supprimer cet utilisateur ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    db.ExecuteNonQuery("DELETE FROM Utilisateurs WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                    ChargerUtilisateurs();
                }
                catch (Exception ex) { MessageBox.Show("Erreur suppression : " + ex.Message); }
            }
        }
    }
}