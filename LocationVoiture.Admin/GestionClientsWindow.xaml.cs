using System;
using System.Collections.Generic; // Pour List
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class GestionClientsWindow : Window
    {
        private DatabaseHelper db;

        // Variables Pagination
        private int _currentPage = 1;
        private int _pageSize = 12; // Nombre de clients par page
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public GestionClientsWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();

            _isLoaded = true;
            ChargerClients();
        }

        private void ChargerClients()
        {
            if (!_isLoaded) return;

            try
            {
                // 1. Construction de la condition WHERE
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                // Recherche (Nom, Prénom, Email, Tel, Permis)
                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (Nom LIKE @s OR Prenom LIKE @s OR Email LIKE @s OR Telephone LIKE @s OR NumPermis LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRecherche.Text + "%"));
                }

                // 2. Gestion du Tri
                string orderBy = "Id DESC";
                if (cbTri.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    orderBy = item.Tag.ToString();
                }

                // 3. Compter le total
                string countQuery = $"SELECT COUNT(*) FROM Clients {condition}";
                object countResult = db.ExecuteScalar(countQuery, parameters.ToArray());
                _totalRecords = Convert.ToInt32(countResult);

                // Calcul Pages
                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;

                if (_currentPage > totalPages) _currentPage = totalPages;
                if (_currentPage < 1) _currentPage = 1;

                // UI Pagination
                lblPaginationInfo.Text = $"Page {_currentPage} sur {totalPages} ({_totalRecords} clients)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                // 4. Requête Finale avec LIMIT
                int offset = (_currentPage - 1) * _pageSize;
                string query = $"SELECT * FROM Clients {condition} ORDER BY {orderBy} LIMIT {_pageSize} OFFSET {offset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());
                ClientsGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        // --- ÉVÉNEMENTS UI ---

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentPage = 1;
            ChargerClients();
        }

        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChargerClients();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtRecherche.Text = "";
            cbTri.SelectedIndex = 0;
            _currentPage = 1;
            ChargerClients();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; ChargerClients(); }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            ChargerClients();
        }

        // --- ACTIONS CRUD ---

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterClientWindow win = new AjouterClientWindow();
            win.ShowDialog();
            ChargerClients();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);
            AjouterClientWindow win = new AjouterClientWindow(id);
            win.ShowDialog();
            ChargerClients();
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);
            if (MessageBox.Show("Supprimer ce client ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // Attention : On ne peut pas supprimer un client s'il a des locations.
                    // Il faudrait gérer les contraintes SQL (clé étrangère).
                    db.ExecuteNonQuery("DELETE FROM Clients WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                    ChargerClients();
                }
                catch (Exception ex) { MessageBox.Show("Impossible de supprimer (Probablement lié à des locations). Détail : " + ex.Message); }
            }
        }
    }
}