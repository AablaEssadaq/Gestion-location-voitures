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
        private int? _idVoitureModif = null; // Si null = Ajout, sinon = Modification
        private string _ancienCheminImageWeb = null; // Pour garder l'ancienne image si on n'en change pas

        // Constructeur Ajout
        public AjouterVoitureWindow()
        {
            InitializeComponent();
            InitWindow("Ajouter un véhicule");
        }

        // Constructeur Modification
        public AjouterVoitureWindow(int idVoiture)
        {
            InitializeComponent();
            _idVoitureModif = idVoiture;
            InitWindow("Modifier le véhicule");
            ChargerVoitureExistante(idVoiture);
        }

        private void InitWindow(string titre)
        {
            this.Title = titre;
            try
            {
                db = new DatabaseHelper();
                ChargerCategories();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ChargerCategories()
        {
            DataTable dt = db.ExecuteQuery("SELECT Id, Libelle FROM Categories");
            cbCategorie.ItemsSource = dt.DefaultView;
            if (cbCategorie.Items.Count > 0) cbCategorie.SelectedIndex = 0;
        }

        // Charge les données de la voiture à modifier
        private void ChargerVoitureExistante(int id)
        {
            try
            {
                string query = "SELECT * FROM Voitures WHERE Id = @id";
                MySqlParameter[] param = { new MySqlParameter("@id", id) };
                DataTable dt = db.ExecuteQuery(query, param);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    txtMarque.Text = row["Marque"].ToString();
                    txtModele.Text = row["Modele"].ToString();
                    txtMatricule.Text = row["Matricule"].ToString();
                    txtPrix.Text = row["PrixParJour"].ToString();
                    txtKm.Text = row["KilometrageActuel"].ToString(); // Charge le KM

                    cbCategorie.SelectedValue = row["CategorieId"];
                    cbCarburant.Text = row["Carburant"].ToString();
                    btnEnregistrer.Content = "Mettre à jour";

                    // Gestion Image
                    string cheminWeb = row["ImageChemin"].ToString();
                    if (!string.IsNullOrEmpty(cheminWeb))
                    {
                        _ancienCheminImageWeb = cheminWeb; // On garde le chemin en mémoire

                        // Pour afficher l'image, il faut retrouver le chemin physique complet
                        string dossierWeb = GetCheminImagesWeb();
                        string nomFichier = Path.GetFileName(cheminWeb); // juste "guid.jpg"
                        string cheminPhysique = Path.Combine(dossierWeb, nomFichier);

                        if (File.Exists(cheminPhysique))
                        {
                            imgApercu.Source = new BitmapImage(new Uri(cheminPhysique));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement voiture : " + ex.Message);
            }
        }

        private void BtnParcourir_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "Images (*.jpg;*.png)|*.jpg;*.png";
            if (op.ShowDialog() == true)
            {
                _cheminImageSource = op.FileName;
                lblCheminImage.Text = Path.GetFileName(_cheminImageSource);
                imgApercu.Source = new BitmapImage(new Uri(_cheminImageSource));
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtMarque.Text) || string.IsNullOrWhiteSpace(txtMatricule.Text) || string.IsNullOrWhiteSpace(txtPrix.Text))
            {
                MessageBox.Show("Champs obligatoires manquants.");
                return;
            }

            try
            {
                string cheminFinalPourBdd = _ancienCheminImageWeb; // Par défaut, on garde l'ancienne

                // Si une NOUVELLE image a été choisie, on l'upload
                if (_cheminImageSource != null)
                {
                    string dossierWeb = GetCheminImagesWeb();
                    string nomFichierUnique = Guid.NewGuid().ToString() + Path.GetExtension(_cheminImageSource);
                    string cheminDestination = Path.Combine(dossierWeb, nomFichierUnique);
                    File.Copy(_cheminImageSource, cheminDestination, true);

                    cheminFinalPourBdd = "/images/" + nomFichierUnique;
                }

                string query;
                MySqlParameter[] parametres;

                if (_idVoitureModif == null)
                {
                    // INSERT
                    query = @"INSERT INTO Voitures (Marque, Modele, Matricule, CategorieId, Carburant, PrixParJour, ImageChemin, KilometrageActuel, EstDisponible) 
                              VALUES (@marque, @modele, @matricule, @catId, @carbu, @prix, @img, @km, 1)";

                    parametres = new MySqlParameter[] {
                        new MySqlParameter("@marque", txtMarque.Text),
                        new MySqlParameter("@modele", txtModele.Text),
                        new MySqlParameter("@matricule", txtMatricule.Text),
                        new MySqlParameter("@catId", cbCategorie.SelectedValue),
                        new MySqlParameter("@carbu", cbCarburant.Text),
                        new MySqlParameter("@prix", decimal.Parse(txtPrix.Text)),
                        new MySqlParameter("@img", (object)cheminFinalPourBdd ?? DBNull.Value),
                        new MySqlParameter("@km", int.Parse(txtKm.Text))
                    };
                }
                else
                {
                    // UPDATE
                    query = @"UPDATE Voitures SET 
                              Marque=@marque, Modele=@modele, Matricule=@matricule, 
                              CategorieId=@catId, Carburant=@carbu, PrixParJour=@prix, 
                              ImageChemin=@img, KilometrageActuel=@km 
                              WHERE Id=@id";

                    parametres = new MySqlParameter[] {
                        new MySqlParameter("@marque", txtMarque.Text),
                        new MySqlParameter("@modele", txtModele.Text),
                        new MySqlParameter("@matricule", txtMatricule.Text),
                        new MySqlParameter("@catId", cbCategorie.SelectedValue),
                        new MySqlParameter("@carbu", cbCarburant.Text),
                        new MySqlParameter("@prix", decimal.Parse(txtPrix.Text)),
                        new MySqlParameter("@img", (object)cheminFinalPourBdd ?? DBNull.Value),
                        new MySqlParameter("@km", int.Parse(txtKm.Text)),
                        new MySqlParameter("@id", _idVoitureModif)
                    };
                }

                db.ExecuteNonQuery(query, parametres);
                MessageBox.Show("Enregistrement réussi !");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e) => this.Close();

        private string GetCheminImagesWeb()
        {
            string dossierExecution = AppDomain.CurrentDomain.BaseDirectory;
            string cheminRelatif = @"..\..\..\..\LocationVoiture.Web\wwwroot\images";
            string cheminFinal = Path.GetFullPath(Path.Combine(dossierExecution, cheminRelatif));
            if (!Directory.Exists(cheminFinal)) Directory.CreateDirectory(cheminFinal);
            return cheminFinal;
        }
    }
}