using System.Windows;
using LocationVoiture.Data; // Pour utiliser DatabaseHelper
using System.Data;          // Pour utiliser DataTable




namespace LocationVoiture.Admin
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper db;

        public MainWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            ChargerVoitures();
        }

        private void ChargerVoitures()
        {
            // ... (votre code existant pour charger les voitures)
            try
            {
                string query = @"
                    SELECT v.Id, v.Matricule, v.Marque, v.Modele, c.Libelle as Categorie, v.PrixParJour, v.EstDisponible
                    FROM Voitures v
                    INNER JOIN Categories c ON v.CategorieId = c.Id";
                DataTable data = db.ExecuteQuery(query);
                MyDataGrid.ItemsSource = data.DefaultView;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message);
            }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterVoitureWindow fenetreAjout = new AjouterVoitureWindow();
            fenetreAjout.ShowDialog();
            ChargerVoitures();
        }

        // === NOUVEAU : GESTION DU BOUTON UTILISATEURS ===
        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            GestionUtilisateursWindow fenetreUsers = new GestionUtilisateursWindow();
            fenetreUsers.ShowDialog();
        }
    }
}