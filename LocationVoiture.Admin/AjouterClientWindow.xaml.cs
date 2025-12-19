using System;
using System.Data;
using System.Windows;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;
using System.Security.Cryptography; 
using System.Text; 

namespace LocationVoiture.Admin
{
    public partial class AjouterClientWindow : Window
    {
        private DatabaseHelper db;
        private int? _idClient = null;

        public AjouterClientWindow(int? id = null)
        {
            InitializeComponent();
            db = new DatabaseHelper();
            _idClient = id;

            if (_idClient != null)
            {
                lblTitre.Text = "Modifier Client";
                btnSave.Content = "Mettre à jour";
                ChargerClient((int)_idClient);
            }
        }

        private string HasherMotDePasse(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";

            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Conversion de la chaîne en tableau d'octets
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Conversion du tableau d'octets en chaîne hexadécimale
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        private void ChargerClient(int id)
        {
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT * FROM Clients WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    txtNom.Text = r["Nom"].ToString();
                    txtPrenom.Text = r["Prenom"].ToString();
                    txtEmail.Text = r["Email"].ToString();
                    txtTel.Text = r["Telephone"].ToString();
                    txtPermis.Text = r["NumPermis"].ToString();
                    txtMdp.Text = r["MotDePasse"].ToString();
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Nom et Email obligatoires.");
                return;
            }

            try
            {
                string mdpSaisi = txtMdp.Text;
                string mdpFinal;


                if (_idClient == null || !string.IsNullOrWhiteSpace(mdpSaisi))
                {
                    mdpFinal = HasherMotDePasse(mdpSaisi);
                }
                else
                {
                    mdpFinal = mdpSaisi;
                }

                string query;
                MySqlParameter[] p = {
            new MySqlParameter("@nom", txtNom.Text),
            new MySqlParameter("@prenom", txtPrenom.Text),
            new MySqlParameter("@email", txtEmail.Text),
            new MySqlParameter("@tel", txtTel.Text),
            new MySqlParameter("@permis", txtPermis.Text),
            new MySqlParameter("@mdp", mdpFinal), 
            new MySqlParameter("@id", _idClient)
        };

                if (_idClient == null)
                {
                    query = "INSERT INTO Clients (Nom, Prenom, Email, Telephone, NumPermis, MotDePasse) VALUES (@nom, @prenom, @email, @tel, @permis, @mdp)";
                }
                else
                {
                    query = "UPDATE Clients SET Nom=@nom, Prenom=@prenom, Email=@email, Telephone=@tel, NumPermis=@permis, MotDePasse=@mdp WHERE Id=@id";
                }

                db.ExecuteNonQuery(query, p);
                MessageBox.Show("Enregistré avec succès !");

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }
    }
}