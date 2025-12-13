using System;
using System.Data;
using System.Globalization;
using System.Windows;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class AjouterEntretienWindow : Window
    {
        private DatabaseHelper db;
        private int? _idEntretienModif = null;

        // Constructeur adapté : accepte un ID voiture (pour ajout) OU un ID entretien (pour modif)
        public AjouterEntretienWindow(int idVoiturePreselect = 0, int idEntretienModif = 0)
        {
            InitializeComponent();
            db = new DatabaseHelper();
            dpDate.SelectedDate = DateTime.Now;

            ChargerVoitures(idVoiturePreselect);
            ChargerTypesEntretien();

            if (idEntretienModif > 0)
            {
                _idEntretienModif = idEntretienModif;
                this.Title = "Modifier l'Entretien";
                ChargerEntretienExistant(idEntretienModif);
            }
        }

        private void ChargerVoitures(int idPreselect)
        {
            try
            {
                string query = "SELECT Id, CONCAT(Marque, ' ', Modele, ' - ', Matricule) AS NomComplet, KilometrageActuel FROM Voitures";
                DataTable dt = db.ExecuteQuery(query);
                cbVoiture.ItemsSource = dt.DefaultView;

                if (idPreselect > 0) cbVoiture.SelectedValue = idPreselect;
            }
            catch { }
        }

        private void ChargerTypesEntretien()
        {
            try
            {
                cbType.Items.Clear();
                DataTable dt = db.ExecuteQuery("SELECT Nom FROM TypeEntretien ORDER BY Nom");
                foreach (DataRow row in dt.Rows) cbType.Items.Add(row["Nom"].ToString());
            }
            catch { }
        }

        // Charge les données pour modification
        private void ChargerEntretienExistant(int id)
        {
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT * FROM Entretiens WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    cbVoiture.SelectedValue = r["VoitureId"];
                    cbType.Text = r["TypeEntretien"].ToString();
                    txtKm.Text = r["Kilometrage"].ToString();
                    txtCout.Text = r["Cout"].ToString();
                    dpDate.SelectedDate = Convert.ToDateTime(r["DateEntretien"]);
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        private void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            if (cbVoiture.SelectedValue == null || string.IsNullOrWhiteSpace(txtKm.Text)) return;

            try
            {
                int voitureId = Convert.ToInt32(cbVoiture.SelectedValue);
                int km = int.Parse(txtKm.Text);
                string coutStr = txtCout.Text.Replace(",", ".");
                decimal cout = 0; decimal.TryParse(coutStr, NumberStyles.Any, CultureInfo.InvariantCulture, out cout);
                string type = cbType.Text;
                DateTime date = dpDate.SelectedDate.Value;

                string query;
                MySqlParameter[] p;

                if (_idEntretienModif == null) // INSERT
                {
                    query = @"INSERT INTO Entretiens (DateEntretien, TypeEntretien, Kilometrage, Cout, VoitureId, Description) 
                              VALUES (@date, @type, @km, @cout, @vid, 'Entretien périodique')";
                    p = new MySqlParameter[] {
                        new MySqlParameter("@date", date), new MySqlParameter("@type", type),
                        new MySqlParameter("@km", km), new MySqlParameter("@cout", cout), new MySqlParameter("@vid", voitureId)
                    };

                    // On met à jour la voiture SEULEMENT si c'est un nouvel entretien
                    string queryUpdate = "UPDATE Voitures SET KmDernierEntretien = @km, KilometrageActuel = @km WHERE Id = @vid";
                    db.ExecuteNonQuery(queryUpdate, new MySqlParameter[] { new MySqlParameter("@km", km), new MySqlParameter("@vid", voitureId) });
                }
                else // UPDATE
                {
                    query = @"UPDATE Entretiens SET DateEntretien=@date, TypeEntretien=@type, Kilometrage=@km, Cout=@cout, VoitureId=@vid 
                              WHERE Id=@id";
                    p = new MySqlParameter[] {
                        new MySqlParameter("@date", date), new MySqlParameter("@type", type),
                        new MySqlParameter("@km", km), new MySqlParameter("@cout", cout), new MySqlParameter("@vid", voitureId),
                        new MySqlParameter("@id", _idEntretienModif)
                    };
                }

                db.ExecuteNonQuery(query, p);
                MessageBox.Show("Enregistré !");
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}