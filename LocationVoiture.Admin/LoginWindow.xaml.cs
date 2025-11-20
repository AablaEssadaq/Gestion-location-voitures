using System.Windows;
using LocationVoiture.Data; // Assurez-vous que ce namespace est correct
using System.Data;
using System.Data.SqlClient;

namespace LocationVoiture.Admin
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Gestion du clic sur "Se Connecter"
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            // Validation simple des champs vides
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Veuillez remplir tous les champs.");
                return;
            }

            // --- TEST RAPIDE (Décommentez pour tester sans base de données) ---
            // if (username == "admin" && password == "admin")
            // {
            //     OuvrirApplication();
            //     return;
            // }

            // --- VRAIE VERIFICATION BDD ---
            if (VerifierBaseDeDonnees(username, password))
            {
                OuvrirApplication();
            }
            else
            {
                ShowError("Identifiant ou mot de passe incorrect.");
            }
        }

        // Gestion du clic sur "Quitter" ou la croix "X"
        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Méthode pour vérifier les identifiants en base
        private bool VerifierBaseDeDonnees(string user, string pass)
        {
            try
            {
                DatabaseHelper db = new DatabaseHelper();
                // Attention : Pensez à hasher les mots de passe en production !
                string query = "SELECT COUNT(*) FROM AdminUsers WHERE Username = @user AND PasswordHash = @pass";

                SqlParameter[] paramsDb = new SqlParameter[] {
                    new SqlParameter("@user", user),
                    new SqlParameter("@pass", pass)
                };

                DataTable result = db.ExecuteQuery(query, paramsDb);

                if (result.Rows.Count > 0 && int.Parse(result.Rows[0][0].ToString()) > 0)
                {
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Erreur BDD : " + ex.Message);
                return false;
            }
        }

        // Transition vers la fenêtre principale
        private void OuvrirApplication()
        {
            MainWindow main = new MainWindow();
            main.Show();
            this.Close();
        }

        // Affichage des erreurs sous le bouton
        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visibility = Visibility.Visible;
        }
    }
}