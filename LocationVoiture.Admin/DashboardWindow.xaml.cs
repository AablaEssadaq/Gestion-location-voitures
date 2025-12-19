using System;
using System.Windows;
using LocationVoiture.Data;
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
            if (App.CurrentRole != "Admin")
            {
                if (btnGestionEquipe != null) btnGestionEquipe.Visibility = Visibility.Collapsed;
            }
        }

        private void ChargerStatistiques()
        {
            try
            {
                object resLoc = db.ExecuteScalar("SELECT COUNT(*) FROM Locations WHERE Statut = 'En Attente'");
                txtLocAttente.Text = resLoc?.ToString() ?? "0";

                object resVoit = db.ExecuteScalar("SELECT COUNT(*) FROM Voitures WHERE EstDisponible = 1");
                txtVehiculesDispo.Text = resVoit?.ToString() ?? "0";

                object resClient = db.ExecuteScalar("SELECT COUNT(*) FROM Clients");
                txtTotalClients.Text = resClient?.ToString() ?? "0";
            }
            catch (Exception)
            {
                txtLocAttente.Text = "-";
                txtVehiculesDispo.Text = "-";
                txtTotalClients.Text = "-";
            }
        }


        private void BtnVoitures_Click(object sender, RoutedEventArgs e)
        {
            MainWindow win = new MainWindow();
            win.Closed += (s, args) => ChargerStatistiques(); 
            win.Show(); 
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            GestionClientsWindow win = new GestionClientsWindow();
            win.Closed += (s, args) => ChargerStatistiques();
            win.Show();
        }

        private void BtnLocations_Click(object sender, RoutedEventArgs e)
        {
            GestionLocationsWindow win = new GestionLocationsWindow();
            win.Closed += (s, args) => ChargerStatistiques();
            win.Show();
        }

        private void BtnEmployes_Click(object sender, RoutedEventArgs e)
        {
            GestionUtilisateursWindow win = new GestionUtilisateursWindow();
            win.Show();
        }

        private void BtnPaiements_Click(object sender, RoutedEventArgs e)
        {
            GestionPaiementsWindow win = new GestionPaiementsWindow();
            win.Closed += (s, args) => ChargerStatistiques();
            win.Show();
        }

        private void BtnEntretiens_Click(object sender, RoutedEventArgs e)
        {
            GestionEntretiensWindow win = new GestionEntretiensWindow();
            win.Closed += (s, args) => ChargerStatistiques();
            win.Show();
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