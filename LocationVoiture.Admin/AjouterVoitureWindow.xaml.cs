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
        private int? _idVoitureModif = null;
        private string _ancienCheminImageWeb = null;

        public AjouterVoitureWindow(int idVoiture = 0)
        {
            InitializeComponent();
            db = new DatabaseHelper();
            ChargerCategories();

            if (idVoiture > 0)
            {
                _idVoitureModif = idVoiture;
                this.Title = "Modifier le véhicule";
                btnEnregistrer.Content = "Mettre à jour";
                ChargerVoitureExistante(idVoiture);
            }
        }

        private void ChargerCategories()
        {
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT Id, Libelle FROM Categories");
                cbCategorie.ItemsSource = dt.DefaultView;
                if (cbCategorie.Items.Count > 0) cbCategorie.SelectedIndex = 0;
            }
            catch { }
        }

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
                    txtKm.Text = row["KilometrageActuel"].ToString();
                    cbCategorie.SelectedValue = row["CategorieId"];
                    cbCarburant.Text = row["Carburant"].ToString();

                    bool estDispo = Convert.ToBoolean(row["EstDisponible"]);
                    chkDisponible.IsChecked = estDispo;

                    string cheminWeb = row["ImageChemin"].ToString();
                    if (!string.IsNullOrEmpty(cheminWeb))
                    {
                        _ancienCheminImageWeb = cheminWeb;
                        string cheminPhysique = Path.Combine(GetCheminImagesWeb(), Path.GetFileName(cheminWeb));
                        if (File.Exists(cheminPhysique)) imgApercu.Source = new BitmapImage(new Uri(cheminPhysique));
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMarque.Text) || string.IsNullOrWhiteSpace(txtMatricule.Text) || string.IsNullOrWhiteSpace(txtPrix.Text))
            {
                MessageBox.Show("Champs obligatoires manquants.");
                return;
            }

            try
            {
                string cheminFinal = _ancienCheminImageWeb;
                if (_cheminImageSource != null)
                {
                    string dossier = GetCheminImagesWeb();
                    string nom = Guid.NewGuid() + Path.GetExtension(_cheminImageSource);
                    File.Copy(_cheminImageSource, Path.Combine(dossier, nom), true);
                    cheminFinal = "/images/" + nom;
                }

                bool estDispo = chkDisponible.IsChecked == true; 

                string query;
                MySqlParameter[] p;

                if (_idVoitureModif == null)
                {
                    query = @"INSERT INTO Voitures (Marque, Modele, Matricule, CategorieId, Carburant, PrixParJour, ImageChemin, KilometrageActuel, EstDisponible) 
                              VALUES (@marque, @modele, @matricule, @catId, @carbu, @prix, @img, @km, @dispo)";

                    p = new MySqlParameter[] {
                        new MySqlParameter("@marque", txtMarque.Text),
                        new MySqlParameter("@modele", txtModele.Text),
                        new MySqlParameter("@matricule", txtMatricule.Text),
                        new MySqlParameter("@catId", cbCategorie.SelectedValue),
                        new MySqlParameter("@carbu", cbCarburant.Text),
                        new MySqlParameter("@prix", decimal.Parse(txtPrix.Text)),
                        new MySqlParameter("@img", (object)cheminFinal ?? DBNull.Value),
                        new MySqlParameter("@km", int.Parse(txtKm.Text)),
                        new MySqlParameter("@dispo", estDispo) 
                    };
                }
                else
                {
                    query = @"UPDATE Voitures SET 
                              Marque=@marque, Modele=@modele, Matricule=@matricule, 
                              CategorieId=@catId, Carburant=@carbu, PrixParJour=@prix, 
                              ImageChemin=@img, KilometrageActuel=@km, EstDisponible=@dispo
                              WHERE Id=@id";

                    p = new MySqlParameter[] {
                        new MySqlParameter("@marque", txtMarque.Text),
                        new MySqlParameter("@modele", txtModele.Text),
                        new MySqlParameter("@matricule", txtMatricule.Text),
                        new MySqlParameter("@catId", cbCategorie.SelectedValue),
                        new MySqlParameter("@carbu", cbCarburant.Text),
                        new MySqlParameter("@prix", decimal.Parse(txtPrix.Text)),
                        new MySqlParameter("@img", (object)cheminFinal ?? DBNull.Value),
                        new MySqlParameter("@km", int.Parse(txtKm.Text)),
                        new MySqlParameter("@dispo", estDispo),
                        new MySqlParameter("@id", _idVoitureModif)
                    };
                }

                db.ExecuteNonQuery(query, p);
                MessageBox.Show("Enregistré avec succès !");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private void BtnParcourir_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog { Filter = "Images (*.jpg;*.png)|*.jpg;*.png" };
            if (op.ShowDialog() == true)
            {
                _cheminImageSource = op.FileName;
                lblCheminImage.Text = Path.GetFileName(_cheminImageSource);
                imgApercu.Source = new BitmapImage(new Uri(_cheminImageSource));
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e) => this.Close();

        private string GetCheminImagesWeb()
        {
            string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\LocationVoiture.Web\wwwroot\images"));
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }
}