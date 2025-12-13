using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class GestionTypesEntretienWindow : Window
    {
        private DatabaseHelper db;
        private int? _idModif = null;

        public GestionTypesEntretienWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            ChargerTypes();
        }

        private void ChargerTypes()
        {
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT * FROM TypeEntretien ORDER BY PeriodiciteKm ASC");
                TypesGrid.ItemsSource = dt.DefaultView;
            }
            catch { }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView; // Attention : Tag="{Binding}" dans le XAML pour récupérer tout l'objet
            if (row != null)
            {
                _idModif = Convert.ToInt32(row["Id"]);
                txtNom.Text = row["Nom"].ToString();
                txtKm.Text = row["PeriodiciteKm"].ToString();
                btnSave.Content = "Mettre à jour";
                btnSave.Background = System.Windows.Media.Brushes.Orange;
            }
        }

        // === NOUVEAU : SUPPRESSION ===
        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Supprimer ce type d'entretien ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    int id = Convert.ToInt32(((Button)sender).Tag);
                    db.ExecuteNonQuery("DELETE FROM TypeEntretien WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                    ChargerTypes();

                    // Reset formulaire si on était en train de modifier celui qu'on supprime
                    if (_idModif == id) { _idModif = null; txtNom.Text = ""; txtKm.Text = ""; btnSave.Content = "Ajouter"; }
                }
                catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtKm.Text)) return;

            try
            {
                string query;
                MySqlParameter[] p;

                if (_idModif == null)
                {
                    query = "INSERT INTO TypeEntretien (Nom, PeriodiciteKm) VALUES (@nom, @km)";
                    p = new MySqlParameter[] { new MySqlParameter("@nom", txtNom.Text), new MySqlParameter("@km", int.Parse(txtKm.Text)) };
                }
                else
                {
                    query = "UPDATE TypeEntretien SET Nom=@nom, PeriodiciteKm=@km WHERE Id=@id";
                    p = new MySqlParameter[] {
                        new MySqlParameter("@nom", txtNom.Text),
                        new MySqlParameter("@km", int.Parse(txtKm.Text)),
                        new MySqlParameter("@id", _idModif)
                    };
                }

                db.ExecuteNonQuery(query, p);

                _idModif = null;
                txtNom.Text = ""; txtKm.Text = "";
                btnSave.Content = "Ajouter";
                btnSave.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#2563EB");

                ChargerTypes();
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }
    }
}