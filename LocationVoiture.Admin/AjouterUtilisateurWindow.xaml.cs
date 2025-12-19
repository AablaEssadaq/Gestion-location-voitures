using System;
using System.Data;
using System.Windows;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;
using LocationVoiture.Admin.Utilities;

namespace LocationVoiture.Admin
{
    public partial class AjouterUtilisateurWindow : Window
    {
        private DatabaseHelper db;
        private int? _userIdToEdit = null;

        public AjouterUtilisateurWindow(int? userId = null)
        {
            InitializeComponent();
            db = new DatabaseHelper();

            if (userId.HasValue)
            {
                _userIdToEdit = userId;
                this.Title = "Modifier l'Employé";
                ChargerDonneesUtilisateur(userId.Value);
            }
        }

        private void ChargerDonneesUtilisateur(int id)
        {
            try
            {
                string query = "SELECT Nom, Prenom, Email, Role FROM Utilisateurs WHERE Id = @id";
                MySqlParameter[] param = { new MySqlParameter("@id", id) };

                DataTable dt = db.ExecuteQuery(query, param);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtNom.Text = row["Nom"].ToString();
                    txtPrenom.Text = row["Prenom"].ToString();
                    txtEmail.Text = row["Email"].ToString();
                    cbRole.Text = row["Role"].ToString();

                    txtMdp.Password = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message);
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Merci de remplir Nom et Email.");
                return;
            }

            if (_userIdToEdit == null && string.IsNullOrWhiteSpace(txtMdp.Password))
            {
                MessageBox.Show("Le mot de passe est obligatoire pour un nouvel utilisateur.");
                return;
            }

            try
            {
                string query;
                System.Collections.Generic.List<MySqlParameter> p = new System.Collections.Generic.List<MySqlParameter>
                {
                    new MySqlParameter("@nom", txtNom.Text),
                    new MySqlParameter("@prenom", txtPrenom.Text),
                    new MySqlParameter("@email", txtEmail.Text),
                    new MySqlParameter("@role", cbRole.Text)
                };

                if (_userIdToEdit == null)
                {
                    string mdpHache = PasswordHelper.HashPassword(txtMdp.Password);

                    query = "INSERT INTO Utilisateurs (Nom, Prenom, Email, MotDePasse, Role) VALUES (@nom, @prenom, @email, @mdp, @role)";
                    p.Add(new MySqlParameter("@mdp", mdpHache));
                }
                else
                {
                    p.Add(new MySqlParameter("@id", _userIdToEdit));

                    if (string.IsNullOrWhiteSpace(txtMdp.Password))
                    {
                        query = "UPDATE Utilisateurs SET Nom=@nom, Prenom=@prenom, Email=@email, Role=@role WHERE Id=@id";
                    }
                    else
                    {
                        string mdpHache = PasswordHelper.HashPassword(txtMdp.Password);
                        p.Add(new MySqlParameter("@mdp", mdpHache));
                        query = "UPDATE Utilisateurs SET Nom=@nom, Prenom=@prenom, Email=@email, Role=@role, MotDePasse=@mdp WHERE Id=@id";
                    }
                }

                db.ExecuteNonQuery(query, p.ToArray());

                MessageBox.Show("Utilisateur enregistré avec succès !");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }
    }
}