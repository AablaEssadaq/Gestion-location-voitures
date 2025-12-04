using System;
using System.Data; // Pour DataTable
using System.Windows;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class AjouterUtilisateurWindow : Window
    {
        private DatabaseHelper db;
        private int? _userIdToEdit = null; // Stocke l'ID si on est en modification

        // Constructeur par défaut (Ajout)
        public AjouterUtilisateurWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            this.Title = "Nouvel Employé";
        }

        // Constructeur pour la modification (On passe l'ID)
        public AjouterUtilisateurWindow(int userId)
        {
            InitializeComponent();
            db = new DatabaseHelper();
            _userIdToEdit = userId;
            this.Title = "Modifier l'Employé";

            // On change le texte du bouton (optionnel si vous avez donné un nom au bouton en XAML)
            // btnEnregistrer.Content = "Modifier"; 

            ChargerDonneesUtilisateur(userId);
        }

        private void ChargerDonneesUtilisateur(int id)
        {
            try
            {
                string query = "SELECT Nom, Prenom, Email, MotDePasse, Role FROM Utilisateurs WHERE Id = @id";
                MySqlParameter[] param = { new MySqlParameter("@id", id) };

                DataTable dt = db.ExecuteQuery(query, param);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtNom.Text = row["Nom"].ToString();
                    txtPrenom.Text = row["Prenom"].ToString();
                    txtEmail.Text = row["Email"].ToString();
                    txtMdp.Text = row["MotDePasse"].ToString(); // Affiche le mot de passe actuel
                    cbRole.Text = row["Role"].ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message);
            }
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
                string query;
                MySqlParameter[] param;

                if (_userIdToEdit == null)
                {
                    // === CAS 1 : AJOUT (INSERT) ===
                    query = "INSERT INTO Utilisateurs (Nom, Prenom, Email, MotDePasse, Role) VALUES (@nom, @prenom, @email, @mdp, @role)";
                    param = new MySqlParameter[] {
                        new MySqlParameter("@nom", txtNom.Text),
                        new MySqlParameter("@prenom", txtPrenom.Text),
                        new MySqlParameter("@email", txtEmail.Text),
                        new MySqlParameter("@mdp", txtMdp.Text),
                        new MySqlParameter("@role", cbRole.Text)
                    };
                }
                else
                {
                    // === CAS 2 : MODIFICATION (UPDATE) ===
                    query = "UPDATE Utilisateurs SET Nom=@nom, Prenom=@prenom, Email=@email, MotDePasse=@mdp, Role=@role WHERE Id=@id";
                    param = new MySqlParameter[] {
                        new MySqlParameter("@nom", txtNom.Text),
                        new MySqlParameter("@prenom", txtPrenom.Text),
                        new MySqlParameter("@email", txtEmail.Text),
                        new MySqlParameter("@mdp", txtMdp.Text),
                        new MySqlParameter("@role", cbRole.Text),
                        new MySqlParameter("@id", _userIdToEdit)
                    };
                }

                db.ExecuteNonQuery(query, param);

                MessageBox.Show(_userIdToEdit == null ? "Utilisateur créé !" : "Utilisateur modifié !");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }
    }
}