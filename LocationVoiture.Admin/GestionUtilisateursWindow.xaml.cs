using System;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class GestionUtilisateursWindow : Window
    {
        private DatabaseHelper db;

        public GestionUtilisateursWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            ChargerUtilisateurs();
        }

        private void ChargerUtilisateurs()
        {
            try
            {
                // J'ajoute MotDePasse dans le SELECT au cas où, mais on ne l'affiche pas dans la grille
                string query = "SELECT Id, Nom, Prenom, Email, Role FROM Utilisateurs";
                DataTable dt = db.ExecuteQuery(query);
                UsersGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message);
            }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterUtilisateurWindow fenetre = new AjouterUtilisateurWindow();
            fenetre.ShowDialog();
            ChargerUtilisateurs();
        }

        // === NOUVEAU : CODE DU BOUTON MODIFIER ===
        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            // On récupère l'ID du bouton cliqué
            Button btn = (Button)sender;
            int idToEdit = Convert.ToInt32(btn.Tag);

            // On ouvre la fenêtre en mode "Modification" (en passant l'ID)
            AjouterUtilisateurWindow fenetre = new AjouterUtilisateurWindow(idToEdit);
            fenetre.ShowDialog();

            // On rafraîchit la liste quand la fenêtre se ferme
            ChargerUtilisateurs();
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int idToDelete = Convert.ToInt32(btn.Tag);

            if (MessageBox.Show("Voulez-vous vraiment supprimer cet utilisateur ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    string query = "DELETE FROM Utilisateurs WHERE Id = @id";
                    MySqlParameter[] param = { new MySqlParameter("@id", idToDelete) };
                    db.ExecuteNonQuery(query, param);

                    ChargerUtilisateurs();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur suppression : " + ex.Message);
                }
            }
        }
    }
}