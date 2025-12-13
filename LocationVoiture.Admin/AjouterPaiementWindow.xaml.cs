using System;
using System.Windows;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;
using System.Globalization;

namespace LocationVoiture.Admin
{
    public partial class AjouterPaiementWindow : Window
    {
        private DatabaseHelper db;
        private int _locationId;

        public AjouterPaiementWindow(int locationId, decimal montantTotal)
        {
            InitializeComponent();
            db = new DatabaseHelper();
            _locationId = locationId;

            // Pré-remplissage
            lblInfo.Text = $"Règlement pour la location #{locationId}";
            txtMontant.Text = montantTotal.ToString("0.00").Replace(",", "."); // Format propre
            dpDate.SelectedDate = DateTime.Now;
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validation basique
            if (string.IsNullOrWhiteSpace(txtMontant.Text) || dpDate.SelectedDate == null)
            {
                MessageBox.Show("Veuillez remplir le montant et la date.");
                return;
            }

            
            string montantTexte = txtMontant.Text.Replace(",", ".");
            decimal montantFinal = 0;

            if (!decimal.TryParse(montantTexte, NumberStyles.Any, CultureInfo.InvariantCulture, out montantFinal))
            {
                MessageBox.Show("Le format du montant est incorrect. Exemple valide : 600.00");
                return;
            }

            try
            {
                string queryPaiement = @"INSERT INTO Paiements (Montant, DatePaiement, Methode, LocationId) 
                                 VALUES (@montant, @date, @methode, @locId)";

                MySqlParameter[] p = {
            new MySqlParameter("@montant", montantFinal), // Plus de decimal.Parse ici !
            new MySqlParameter("@date", dpDate.SelectedDate.Value),
            new MySqlParameter("@methode", cbMethode.Text),
            new MySqlParameter("@locId", _locationId)
        };

                db.ExecuteNonQuery(queryPaiement, p);

                string queryUpdateLoc = "UPDATE Locations SET EstPaye = 1 WHERE Id = @id";
                db.ExecuteNonQuery(queryUpdateLoc, new MySqlParameter[] { new MySqlParameter("@id", _locationId) });

                MessageBox.Show("Paiement enregistré avec succès !");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }
        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}