using System.Windows;
using LocationVoiture.Data;
using System.Data;
// CORRECTION 1 : On utilise MySQL au lieu de SQL Server
using MySql.Data.MySqlClient;

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

                // Note : Assurez-vous que votre table s'appelle bien "Utilisateurs" (comme dans le script MySQL)
                // et non "AdminUsers" (ancien script SQL Server).
                string query = "SELECT COUNT(*) FROM Utilisateurs WHERE Email = @user AND MotDePasse = @pass AND Role = 'Admin'";

                // CORRECTION 2 : On utilise MySqlParameter
                MySqlParameter[] paramsDb = new MySqlParameter[] {
                    new MySqlParameter("@user", user),
                    new MySqlParameter("@pass", pass)
                };

                DataTable result = db.ExecuteQuery(query, paramsDb);

                if (result.Rows.Count > 0 && Convert.ToInt32(result.Rows[0][0]) > 0)
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