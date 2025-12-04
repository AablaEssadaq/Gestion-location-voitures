using System;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class GestionClientsWindow : Window
    {
        private DatabaseHelper db;

        public GestionClientsWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            ChargerClients();
        }

        private void ChargerClients()
        {
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT * FROM Clients ORDER BY Id DESC");
                ClientsGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

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
                    db.ExecuteNonQuery("DELETE FROM Clients WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                    ChargerClients();
                }
                catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
            }
        }
    }
}