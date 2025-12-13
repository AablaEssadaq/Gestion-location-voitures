using System.Windows;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;
using LocationVoiture.Admin.Utilities; // Pour PasswordHelper

namespace LocationVoiture.Admin
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;      // Ici c'est l'email ou le login
            string password = txtPassword.Password;  // Le mot de passe en clair

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Veuillez remplir tous les champs.");
                return;
            }

            // On vérifie
            string role = VerifierEtRecupererRole(username, password);

            if (role != null)
            {
                OuvrirApplication(role);
            }
            else
            {
                ShowError("Identifiant ou mot de passe incorrect.");
            }
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Nouvelle logique de vérification avec Hash
        private string VerifierEtRecupererRole(string user, string pass)
        {
            try
            {
                DatabaseHelper db = new DatabaseHelper();

                // 1. On cherche l'utilisateur par son Login/Email UNIQUEMENT
                // (On ne met plus le mot de passe dans le WHERE)
                string query = "SELECT MotDePasse, Role FROM Utilisateurs WHERE Email = @user";

                MySqlParameter[] paramsDb = new MySqlParameter[] {
                    new MySqlParameter("@user", user)
                };

                DataTable dt = db.ExecuteQuery(query, paramsDb);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    string storedHash = row["MotDePasse"].ToString();
                    string role = row["Role"].ToString();

                    // 2. On compare le mot de passe saisi avec le hash stocké
                    if (PasswordHelper.VerifyPassword(pass, storedHash))
                    {
                        return role; // C'est bon !
                    }
                }

                return null; // Pas trouvé ou mauvais mot de passe
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Erreur BDD : " + ex.Message);
                return null;
            }
        }

        private void OuvrirApplication(string role)
        {
            App.CurrentRole = role;
            DashboardWindow dash = new DashboardWindow();
            dash.Show();
            this.Close();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visibility = Visibility.Visible;
        }
    }
}