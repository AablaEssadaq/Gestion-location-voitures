using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Globalization; 

using ClosedXML.Excel;

namespace LocationVoiture.Admin
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper db;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public MainWindow()
        {
            InitializeComponent();

            db = new DatabaseHelper();

            ChargerFiltresCategories();

            _isLoaded = true;

            ChargerVoitures();
        }

        private void ChargerFiltresCategories()
        {
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT Id, Libelle FROM Categories");
                DataRow row = dt.NewRow();
                row["Id"] = 0;
                row["Libelle"] = "Toutes les catégories";
                dt.Rows.InsertAt(row, 0);

                cbFiltreCategorie.ItemsSource = dt.DefaultView;
                cbFiltreCategorie.SelectedIndex = 0;
            }
            catch { }
        }

        private void ChargerVoitures(bool loadAll = false)
        {
            if (!_isLoaded) return;

            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (v.Marque LIKE @search OR v.Modele LIKE @search OR v.Matricule LIKE @search)";
                    parameters.Add(new MySqlParameter("@search", "%" + txtRecherche.Text + "%"));
                }

                if (cbFiltreCategorie.SelectedValue != null && Convert.ToInt32(cbFiltreCategorie.SelectedValue) > 0)
                {
                    condition += " AND v.CategorieId = @catId";
                    parameters.Add(new MySqlParameter("@catId", cbFiltreCategorie.SelectedValue));
                }

                string orderBy = "v.Id DESC";
                if (cbTri.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    orderBy = item.Tag.ToString();
                }

                string countQuery = $"SELECT COUNT(*) FROM Voitures v {condition}";
                object countResult = db.ExecuteScalar(countQuery, parameters.ToArray());
                _totalRecords = Convert.ToInt32(countResult);

                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;

                if (_currentPage > totalPages) _currentPage = totalPages;
                if (_currentPage < 1) _currentPage = 1;

                lblPaginationInfo.Text = $"Page {_currentPage} sur {totalPages} ({_totalRecords} résultats)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                string limitOffset = loadAll ? "" : $"LIMIT {_pageSize} OFFSET {(_currentPage - 1) * _pageSize}";

                string query = $@"
                    SELECT v.Id, v.Matricule, v.Marque, v.Modele, c.Libelle as Categorie, 
                            v.PrixParJour, v.EstDisponible, 
                            v.KilometrageActuel, v.KmDernierEntretien,
                            v.CategorieId -- Ajout pour l'import si besoin de l'ID
                    FROM Voitures v
                    INNER JOIN Categories c ON v.CategorieId = c.Id
                    {condition}
                    ORDER BY {orderBy}
                    {limitOffset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());

                dt.Columns.Add("NomProchainEntretien", typeof(string));
                dt.Columns.Add("CouleurAlerte", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    CalculerEntretien(row);
                }

                if (!loadAll)
                {
                    MyDataGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    ExportToXlsx(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement : " + ex.Message);
            }
        }

        private void CalculerEntretien(DataRow row)
        {
            int vId = Convert.ToInt32(row["Id"]);
            int kmActuel = Convert.ToInt32(row["KilometrageActuel"]);

            int lastVidange = GetDernierKmFait(vId, "Vidange");
            int lastFreins = GetDernierKmFait(vId, "Plaquettes");
            int lastCourroie = GetDernierKmFait(vId, "Courroie");

            int seuilVidange = 10000;
            int seuilFreins = 30000;
            int seuilCourroie = 100000;

            int resteVidange = seuilVidange - (kmActuel - lastVidange);
            int resteFreins = seuilFreins - (kmActuel - lastFreins);
            int resteCourroie = seuilCourroie - (kmActuel - lastCourroie);

            int minReste = Math.Min(resteVidange, Math.Min(resteFreins, resteCourroie));

            string prochainEntretien = "À jour";
            string couleur = "Transparent";

            if (minReste == resteVidange) prochainEntretien = "Vidange";
            else if (minReste == resteFreins) prochainEntretien = "Plaquettes";
            else if (minReste == resteCourroie) prochainEntretien = "Courroie";

            if (minReste <= 500) { couleur = "#f21b1b"; prochainEntretien += " (URGENT)"; }
            else if (minReste <= 2000) { couleur = "#ebd234"; }

            if (minReste > 9000) { prochainEntretien = "À jour"; couleur = "#069425"; }

            row["NomProchainEntretien"] = prochainEntretien;
            row["CouleurAlerte"] = couleur;
        }

        private int GetDernierKmFait(int voitureId, string type)
        {
            try
            {
                object res = db.ExecuteScalar("SELECT MAX(Kilometrage) FROM Entretiens WHERE VoitureId = @id AND TypeEntretien LIKE @type",
                    new MySqlParameter[] { new MySqlParameter("@id", voitureId), new MySqlParameter("@type", "%" + type + "%") });
                return (res != DBNull.Value && res != null) ? Convert.ToInt32(res) : 0;
            }
            catch { return 0; }
        }


        private void BtnExporter_Click(object sender, RoutedEventArgs e)
        {
            ChargerVoitures(true);
        }

        private void ExportToXlsx(DataTable dt)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
                saveFileDialog.FileName = "export_vehicules_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        DataTable dtExport = new DataTable("Voitures");
                        string[] exportColumns = { "Matricule", "Marque", "Modele", "Categorie", "PrixParJour", "EstDisponible", "KilometrageActuel", "KmDernierEntretien" };

                        foreach (string colName in exportColumns)
                        {
                            dtExport.Columns.Add(colName, dt.Columns[colName].DataType);
                        }

                        foreach (DataRow row in dt.Rows)
                        {
                            DataRow newRow = dtExport.NewRow();
                            foreach (string colName in exportColumns)
                            {
                                newRow[colName] = row[colName];
                            }
                            dtExport.Rows.Add(newRow);
                        }

                        var worksheet = workbook.Worksheets.Add(dtExport, "Liste des Véhicules");

                        worksheet.Range(1, 1, 1, exportColumns.Length).Style.Font.Bold = true;
                        worksheet.Range(1, 1, 1, exportColumns.Length).Style.Fill.BackgroundColor = XLColor.LightGray;

                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show("Exportation réussie !");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'exportation XLSX : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnImporter_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";

            if (openFileDialog.ShowDialog() == true)
            {
                if (MessageBox.Show("Attention: L'importation va ajouter de nouveaux véhicules à la base de données. Continuer ?", "Confirmation d'Importation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    ImportFromXlsx(openFileDialog.FileName);
                }
            }
        }

        private void ImportFromXlsx(string filePath)
        {
            int importedCount = 0;
            int failedCount = 0;

            List<string> errorDetails = new List<string>();

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);

                    for (int row = 2; row <= worksheet.LastRowUsed().RowNumber(); row++)
                    {
                        int currentRow = row;
                        string currentMatricule = "N/A";

                        try
                        {

                            string matricule = worksheet.Cell(currentRow, 1).GetString().Trim();
                            currentMatricule = matricule;

                            string marque = worksheet.Cell(currentRow, 2).GetString().Trim();
                            string modele = worksheet.Cell(currentRow, 3).GetString().Trim();
                            string libelleCategorie = worksheet.Cell(currentRow, 4).GetString().Trim();

                            string prixStr = worksheet.Cell(currentRow, 5).GetString().Replace(',', '.').Trim();
                            double prixParJour = 0;
                            if (!string.IsNullOrWhiteSpace(prixStr))
                            {
                                double.TryParse(prixStr, NumberStyles.Any, CultureInfo.InvariantCulture, out prixParJour);
                            }

                            string dispoStr = worksheet.Cell(currentRow, 6).GetString().Trim();
                            bool estDisponible = dispoStr.Equals("true", StringComparison.OrdinalIgnoreCase) || dispoStr == "1" || dispoStr.Equals("oui", StringComparison.OrdinalIgnoreCase);

                            string kmStr = worksheet.Cell(currentRow, 7).GetString().Trim();
                            int kilometrageActuel = 0;
                            if (!string.IsNullOrWhiteSpace(kmStr))
                            {
                                int.TryParse(kmStr, out kilometrageActuel);
                            }


                            object catIdObj = db.ExecuteScalar("SELECT Id FROM Categories WHERE Libelle = @libelle",
                                new MySqlParameter[] { new MySqlParameter("@libelle", libelleCategorie) });

                            if (catIdObj == null || catIdObj == DBNull.Value)
                            {
                                failedCount++;
                                errorDetails.Add($"Ligne {currentRow} ({matricule}): Catégorie non trouvée dans la BDD pour '{libelleCategorie}'. Vérifiez l'orthographe.");
                                continue;
                            }
                            int categorieId = Convert.ToInt32(catIdObj);

                            string query = @"INSERT INTO Voitures 
                                (Matricule, Marque, Modele, CategorieId, PrixParJour, EstDisponible, KilometrageActuel, KmDernierEntretien)
                                VALUES (@mat, @mar, @mod, @catId, @prix, @dispo, @kmActuel, @kmActuel)";

                            MySqlParameter[] parameters = new MySqlParameter[]
                            {
                                new MySqlParameter("@mat", matricule),
                                new MySqlParameter("@mar", marque),
                                new MySqlParameter("@mod", modele),
                                new MySqlParameter("@catId", categorieId),
                                new MySqlParameter("@prix", prixParJour),
                                new MySqlParameter("@dispo", estDisponible),
                                new MySqlParameter("@kmActuel", kilometrageActuel)
                            };

                            db.ExecuteNonQuery(query, parameters);
                            importedCount++;
                        }
                        catch (Exception innerEx)
                        {
                            failedCount++;
                            errorDetails.Add($"Ligne {currentRow} ({currentMatricule}): Erreur de format de données ou de base de données. Détail : {innerEx.Message}");
                        }
                    }
                }

                string finalMessage = $"Importation terminée : {importedCount} véhicules ajoutés, {failedCount} lignes ignorées.";

                if (failedCount > 0)
                {
                    finalMessage += "\n\n❌ Détails des échecs :\n" + string.Join("\n", errorDetails);
                    MessageBox.Show(finalMessage, "Importation XLSX - Avec Erreurs", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(finalMessage, "Importation XLSX", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ChargerVoitures();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur critique lors de la lecture du fichier XLSX : " + ex.Message, "Erreur Importation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            _currentPage = 1;
            ChargerVoitures();
        }

        private void CbFiltre_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            _currentPage = 1;
            ChargerVoitures();
        }

        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            ChargerVoitures();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtRecherche.Text = "";
            if (cbFiltreCategorie.Items.Count > 0) cbFiltreCategorie.SelectedIndex = 0;
            if (cbTri.Items.Count > 0) cbTri.SelectedIndex = 0;
            _currentPage = 1;
            ChargerVoitures();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; ChargerVoitures(); }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            ChargerVoitures();
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
           
            AjouterVoitureWindow fenetreAjout = new AjouterVoitureWindow();

            if (fenetreAjout.ShowDialog() == true)
            {
                ChargerVoitures();
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int voitureId = Convert.ToInt32(btn.Tag);

                AjouterVoitureWindow fenetreModif = new AjouterVoitureWindow(voitureId);

                if (fenetreModif.ShowDialog() == true)
                {
                    ChargerVoitures();
                }
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Supprimer ?", "Confirmer", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try { db.ExecuteNonQuery("DELETE FROM Voitures WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", Convert.ToInt32(((Button)sender).Tag)) }); ChargerVoitures(); }
                catch { MessageBox.Show("Erreur suppression."); }
            }
        }
    }
}