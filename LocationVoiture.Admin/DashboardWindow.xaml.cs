using System;
using System.Windows;
using LocationVoiture.Data; // Nécessaire pour DatabaseHelper
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class DashboardWindow : Window
    {
        private DatabaseHelper db;

        public DashboardWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();

            ConfigurerAcces();
            ChargerStatistiques();
        }

        private void ConfigurerAcces()
        {
            // Si l'utilisateur n'est PAS Admin (c'est donc un Employé)
            if (App.CurrentRole != "Admin")
            {
                // On cache le bouton de gestion d'équipe
                btnGestionEquipe.Visibility = Visibility.Collapsed;
            }
        }

        private void ChargerStatistiques()
        {
            try
            {
                // 1. Locations en attente
                object resLoc = db.ExecuteScalar("SELECT COUNT(*) FROM Locations WHERE Statut = 'En Attente'");
                txtLocAttente.Text = resLoc.ToString();

                // 2. Véhicules Disponibles
                object resVoit = db.ExecuteScalar("SELECT COUNT(*) FROM Voitures WHERE EstDisponible = 1");
                txtVehiculesDispo.Text = resVoit.ToString();

                // 3. Clients Inscrits
                object resClient = db.ExecuteScalar("SELECT COUNT(*) FROM Clients");
                txtTotalClients.Text = resClient.ToString();
            }
            catch (Exception ex)
            {
                // En cas d'erreur (ex: base pas prête), on met "-"
                txtLocAttente.Text = "-";
                txtVehiculesDispo.Text = "-";
                txtTotalClients.Text = "-";
                // MessageBox.Show("Erreur stats : " + ex.Message); // Optionnel
            }
        }

        private void BtnVoitures_Click(object sender, RoutedEventArgs e)
        {
            MainWindow win = new MainWindow();
            win.ShowDialog();
            ChargerStatistiques(); // On rafraîchit les stats au retour (ex: si on a ajouté une voiture)
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            GestionClientsWindow win = new GestionClientsWindow();
            win.ShowDialog();
            ChargerStatistiques();
        }

        private void BtnLocations_Click(object sender, RoutedEventArgs e)
        {
            GestionLocationsWindow win = new GestionLocationsWindow();
            win.ShowDialog();
            ChargerStatistiques(); // Rafraîchir (si on a validé des locations)
        }

        private void BtnEmployes_Click(object sender, RoutedEventArgs e)
        {
            GestionUtilisateursWindow win = new GestionUtilisateursWindow();
            win.ShowDialog();
        }

        private void BtnPaiements_Click(object sender, RoutedEventArgs e)
        {
            GestionPaiementsWindow win = new GestionPaiementsWindow();
            win.ShowDialog();

            // Au retour, on peut rafraîchir les stats si besoin
            ChargerStatistiques();
        }
        private void BtnEntretiens_Click(object sender, RoutedEventArgs e)
        {
            GestionEntretiensWindow win = new GestionEntretiensWindow();
            win.ShowDialog();
        }
        private void BtnConfigEntretiens_Click(object sender, RoutedEventArgs e)
        {
            // Ouvre la fenêtre de configuration des types
            GestionTypesEntretienWindow win = new GestionTypesEntretienWindow();
            win.ShowDialog();
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentRole = "";
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }


    }
}