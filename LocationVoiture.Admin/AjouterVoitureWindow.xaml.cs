using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Admin
{
    public partial class AjouterVoitureWindow : Window
    {
        private string _cheminImageSource = null;
        private DatabaseHelper db;

        public AjouterVoitureWindow()
        {
            InitializeComponent();
            try
            {
                db = new DatabaseHelper();
                ChargerCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur à l'initialisation : " + ex.Message);
            }
        }

        private void ChargerCategories()
        {
            try
            {
                string query = "SELECT Id, Libelle FROM Categories";
                DataTable dt = db.ExecuteQuery(query);
                cbCategorie.ItemsSource = dt.DefaultView;

                if (cbCategorie.Items.Count > 0) cbCategorie.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement catégories : " + ex.Message);
            }
        }

        private void BtnParcourir_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Sélectionner une image";
            op.Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (op.ShowDialog() == true)
            {
                _cheminImageSource = op.FileName;
                lblCheminImage.Text = Path.GetFileName(_cheminImageSource);
                imgApercu.Source = new BitmapImage(new Uri(_cheminImageSource));
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // Validation des champs (ajout de txtKm)
            if (string.IsNullOrWhiteSpace(txtMarque.Text) ||
                string.IsNullOrWhiteSpace(txtMatricule.Text) ||
                string.IsNullOrWhiteSpace(txtPrix.Text) ||
                string.IsNullOrWhiteSpace(txtKm.Text) || // Vérification KM
                cbCategorie.SelectedValue == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs obligatoires.");
                return;
            }

            try
            {
                string cheminRelatifWeb = null;

                if (_cheminImageSource != null)
                {
                    string dossierWeb = GetCheminImagesWeb();
                    string extension = Path.GetExtension(_cheminImageSource);
                    string nomFichierUnique = Guid.NewGuid().ToString() + extension;
                    string cheminDestination = Path.Combine(dossierWeb, nomFichierUnique);

                    File.Copy(_cheminImageSource, cheminDestination, true);
                    cheminRelatifWeb = "/images/" + nomFichierUnique;
                }

                // MISE A JOUR DE LA REQUETE SQL
                string query = @"INSERT INTO Voitures 
                                (Marque, Modele, Matricule, CategorieId, Carburant, PrixParJour, ImageChemin, EstDisponible, KilometrageActuel) 
                                VALUES 
                                (@marque, @modele, @matricule, @catId, @carbu, @prix, @img, 1, @km)";

                MySqlParameter[] parametres = {
                    new MySqlParameter("@marque", txtMarque.Text),
                    new MySqlParameter("@modele", txtModele.Text),
                    new MySqlParameter("@matricule", txtMatricule.Text),
                    new MySqlParameter("@catId", cbCategorie.SelectedValue),
                    new MySqlParameter("@carbu", cbCarburant.Text),
                    new MySqlParameter("@prix", decimal.Parse(txtPrix.Text)),
                    new MySqlParameter("@img", (object)cheminRelatifWeb ?? DBNull.Value),
                    // Ajout du paramètre KM
                    new MySqlParameter("@km", int.Parse(txtKm.Text))
                };

                db.ExecuteNonQuery(query, parametres);

                MessageBox.Show("Véhicule ajouté avec succès !");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private string GetCheminImagesWeb()
        {
            string dossierExecution = AppDomain.CurrentDomain.BaseDirectory;
            string cheminRelatif = @"..\..\..\..\LocationVoiture.Web\wwwroot\images";
            string cheminFinal = Path.GetFullPath(Path.Combine(dossierExecution, cheminRelatif));

            if (!Directory.Exists(cheminFinal))
            {
                Directory.CreateDirectory(cheminFinal);
            }
            return cheminFinal;
        }
    }
}