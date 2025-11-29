using System;
using System.Windows;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class AjouterUtilisateurWindow : Window
    {
        private DatabaseHelper db;

        public AjouterUtilisateurWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtEmail.Text) || string.IsNullOrWhiteSpace(txtMdp.Text))
            {
                MessageBox.Show("Merci de remplir tous les champs.");
                return;
            }

            try
            {
                string query = "INSERT INTO Utilisateurs (Nom, Prenom, Email, MotDePasse, Role) VALUES (@nom, @prenom, @email, @mdp, @role)";

                MySqlParameter[] param = {
                    new MySqlParameter("@nom", txtNom.Text),
                    new MySqlParameter("@prenom", txtPrenom.Text),
                    new MySqlParameter("@email", txtEmail.Text),
                    new MySqlParameter("@mdp", txtMdp.Text), // Pensez à hacher le mot de passe dans un vrai projet !
                    new MySqlParameter("@role", cbRole.Text)
                };

                db.ExecuteNonQuery(query, param);

                MessageBox.Show("Utilisateur créé !");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }
    }
}