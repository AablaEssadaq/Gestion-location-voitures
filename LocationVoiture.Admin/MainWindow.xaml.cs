using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper db;

        // Variables pour la pagination
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRecords = 0;
        private bool _isLoaded = false; // Sécurité pour éviter les appels trop tôt

        public MainWindow()
        {
            InitializeComponent();

            // CORRECTION : Instancier db EN PREMIER
            db = new DatabaseHelper();

            ChargerFiltresCategories();

            // On marque que le chargement initial est fini
            _isLoaded = true;

            ChargerVoitures();
        }

        private void ChargerFiltresCategories()
        {
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT Id, Libelle FROM Categories");
                DataRow row = dt.NewRow();
                row["Id"] = 0;
                row["Libelle"] = "Toutes les catégories";
                dt.Rows.InsertAt(row, 0);

                cbFiltreCategorie.ItemsSource = dt.DefaultView;
                cbFiltreCategorie.SelectedIndex = 0;
            }
            catch { }
        }

        private void ChargerVoitures()
        {
            // Sécurité : Si la fenêtre n'est pas encore prête, on ne fait rien
            if (!_isLoaded) return;

            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (v.Marque LIKE @search OR v.Modele LIKE @search OR v.Matricule LIKE @search)";
                    parameters.Add(new MySqlParameter("@search", "%" + txtRecherche.Text + "%"));
                }

                if (cbFiltreCategorie.SelectedValue != null && Convert.ToInt32(cbFiltreCategorie.SelectedValue) > 0)
                {
                    condition += " AND v.CategorieId = @catId";
                    parameters.Add(new MySqlParameter("@catId", cbFiltreCategorie.SelectedValue));
                }

                string orderBy = "v.Id DESC";
                if (cbTri.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    orderBy = item.Tag.ToString();
                }

                string countQuery = $"SELECT COUNT(*) FROM Voitures v {condition}";
                object countResult = db.ExecuteScalar(countQuery, parameters.ToArray());
                _totalRecords = Convert.ToInt32(countResult);

                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;

                if (_currentPage > totalPages) _currentPage = totalPages;
                if (_currentPage < 1) _currentPage = 1;

                lblPaginationInfo.Text = $"Page {_currentPage} sur {totalPages} ({_totalRecords} résultats)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                int offset = (_currentPage - 1) * _pageSize;

                string query = $@"
                    SELECT v.Id, v.Matricule, v.Marque, v.Modele, c.Libelle as Categorie, 
                           v.PrixParJour, v.EstDisponible, 
                           v.KilometrageActuel, v.KmDernierEntretien
                    FROM Voitures v
                    INNER JOIN Categories c ON v.CategorieId = c.Id
                    {condition}
                    ORDER BY {orderBy}
                    LIMIT {_pageSize} OFFSET {offset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());

                dt.Columns.Add("NomProchainEntretien", typeof(string));
                dt.Columns.Add("CouleurAlerte", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    CalculerEntretien(row);
                }

                MyDataGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message);
            }
        }

        private void CalculerEntretien(DataRow row)
        {
            int vId = Convert.ToInt32(row["Id"]);
            int kmActuel = Convert.ToInt32(row["KilometrageActuel"]);

            int lastVidange = GetDernierKmFait(vId, "Vidange");
            int lastFreins = GetDernierKmFait(vId, "Plaquettes");
            int lastCourroie = GetDernierKmFait(vId, "Courroie");

            int seuilVidange = 10000;
            int seuilFreins = 30000;
            int seuilCourroie = 100000;

            int resteVidange = seuilVidange - (kmActuel - lastVidange);
            int resteFreins = seuilFreins - (kmActuel - lastFreins);
            int resteCourroie = seuilCourroie - (kmActuel - lastCourroie);

            int minReste = Math.Min(resteVidange, Math.Min(resteFreins, resteCourroie));

            string prochainEntretien = "À jour";
            string couleur = "#E6FFFA";

            if (minReste == resteVidange) prochainEntretien = "Vidange";
            else if (minReste == resteFreins) prochainEntretien = "Plaquettes";
            else if (minReste == resteCourroie) prochainEntretien = "Courroie";

            if (minReste <= 500) { couleur = "#FFCCCC"; prochainEntretien += " (URGENT)"; }
            else if (minReste <= 2000) { couleur = "#FFF4CC"; }

            if (minReste > 9000) { prochainEntretien = "À jour"; couleur = "Transparent"; }

            row["NomProchainEntretien"] = prochainEntretien;
            row["CouleurAlerte"] = couleur;
        }

        private int GetDernierKmFait(int voitureId, string type)
        {
            try
            {
                object res = db.ExecuteScalar("SELECT MAX(Kilometrage) FROM Entretiens WHERE VoitureId = @id AND TypeEntretien LIKE @type",
                    new MySqlParameter[] { new MySqlParameter("@id", voitureId), new MySqlParameter("@type", "%" + type + "%") });
                return (res != DBNull.Value && res != null) ? Convert.ToInt32(res) : 0;
            }
            catch { return 0; }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return; // Sécurité
            _currentPage = 1;
            ChargerVoitures();
        }

        private void CbFiltre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return; // Sécurité
            _currentPage = 1;
            ChargerVoitures();
        }

        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return; // Sécurité
            ChargerVoitures();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtRecherche.Text = "";
            if (cbFiltreCategorie.Items.Count > 0) cbFiltreCategorie.SelectedIndex = 0;
            if (cbTri.Items.Count > 0) cbTri.SelectedIndex = 0;
            _currentPage = 1;
            ChargerVoitures();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; ChargerVoitures(); }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            ChargerVoitures();
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterVoitureWindow w = new AjouterVoitureWindow();
            w.ShowDialog();
            ChargerVoitures();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(((Button)sender).Tag);
            new AjouterVoitureWindow(id).ShowDialog();
            ChargerVoitures();
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Supprimer ?", "Confirmer", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try { db.ExecuteNonQuery("DELETE FROM Voitures WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", Convert.ToInt32(((Button)sender).Tag)) }); ChargerVoitures(); }
                catch { MessageBox.Show("Erreur suppression."); }
            }
        }
    }
}