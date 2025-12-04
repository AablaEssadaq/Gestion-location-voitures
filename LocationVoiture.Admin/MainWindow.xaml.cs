using System;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper db;

        public MainWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            ChargerVoitures();
        }

        private void ChargerVoitures()
        {
            try
            {
                string query = @"
                    SELECT v.Id, v.Matricule, v.Marque, v.Modele, c.Libelle as Categorie, 
                           v.PrixParJour, v.EstDisponible, v.KilometrageActuel
                    FROM Voitures v
                    INNER JOIN Categories c ON v.CategorieId = c.Id
                    ORDER BY v.Id DESC";
                DataTable data = db.ExecuteQuery(query);
                MyDataGrid.ItemsSource = data.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterVoitureWindow fenetreAjout = new AjouterVoitureWindow();
            fenetreAjout.ShowDialog();
            ChargerVoitures();
        }

        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            GestionUtilisateursWindow fenetreUsers = new GestionUtilisateursWindow();
            fenetreUsers.ShowDialog();
        }

        // === NOUVEAU ===
        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            GestionClientsWindow fenetreClients = new GestionClientsWindow();
            fenetreClients.ShowDialog();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int idVoiture = Convert.ToInt32(btn.Tag);
            AjouterVoitureWindow fenetreModif = new AjouterVoitureWindow(idVoiture);
            fenetreModif.ShowDialog();
            ChargerVoitures();
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int idVoiture = Convert.ToInt32(btn.Tag);
            if (MessageBox.Show("Supprimer ce véhicule ?", "Confirmer", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    db.ExecuteNonQuery("DELETE FROM Voitures WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", idVoiture) });
                    ChargerVoitures();
                }
                catch (Exception ex) { MessageBox.Show("Impossible de supprimer : " + ex.Message); }
            }
        }

        private void BtnLocations_Click(object sender, RoutedEventArgs e)
        {
            GestionLocationsWindow win = new GestionLocationsWindow();
            win.ShowDialog();
        }
    }
}