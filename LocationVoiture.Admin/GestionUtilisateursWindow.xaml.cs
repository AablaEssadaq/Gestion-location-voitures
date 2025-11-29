using System;
using System.Windows;
using System.Windows.Controls; // Pour Button
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
                // On récupère tout sauf le mot de passe pour des raisons de sécurité
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
            // Ouvre la fenêtre d'ajout (qu'on va créer juste après)
            AjouterUtilisateurWindow fenetre = new AjouterUtilisateurWindow();
            fenetre.ShowDialog();
            ChargerUtilisateurs(); // Rafraîchir la liste au retour
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            // Récupérer l'ID via la propriété Tag du bouton cliqué
            Button btn = (Button)sender;
            int idToDelete = Convert.ToInt32(btn.Tag);

            if (MessageBox.Show("Voulez-vous vraiment supprimer cet utilisateur ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    string query = "DELETE FROM Utilisateurs WHERE Id = @id";
                    MySqlParameter[] param = { new MySqlParameter("@id", idToDelete) };
                    db.ExecuteNonQuery(query, param);

                    ChargerUtilisateurs(); // Rafraîchir
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur suppression : " + ex.Message);
                }
            }
        }
    }

}