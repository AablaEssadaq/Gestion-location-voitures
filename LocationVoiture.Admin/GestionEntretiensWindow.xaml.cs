using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.Globalization;

namespace LocationVoiture.Admin
{
    public partial class GestionEntretiensWindow : Window
    {
        private DatabaseHelper db;
        private bool _isLoaded = false;

        private int _pageAlertes = 1;
        private int _totalAlertes = 0;

        private int _pageHist = 1;
        private int _totalHist = 0;

        private int _pageSize = 12;

        public GestionEntretiensWindow()
        {
            InitializeComponent();

            try { db = new DatabaseHelper(); }
            catch (Exception ex) { MessageBox.Show("Erreur BDD : " + ex.Message); return; }

            _isLoaded = true;
            ChargerFiltresTypes();
            ChargerAlertes();
            ChargerHistorique();
        }

        private void ChargerFiltresTypes()
        {
            if (db == null) return;
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT DISTINCT TypeEntretien FROM Entretiens WHERE TypeEntretien IS NOT NULL ORDER BY TypeEntretien");

                cbFiltreTypeAlerte.Items.Clear();
                cbFiltreTypeHist.Items.Clear();
                cbFiltreTypeAlerte.Items.Add("Tous les types");
                cbFiltreTypeHist.Items.Add("Tous les types");

                foreach (DataRow row in dt.Rows)
                {
                    string nom = row["TypeEntretien"].ToString();
                    cbFiltreTypeAlerte.Items.Add(nom);
                    cbFiltreTypeHist.Items.Add(nom);
                }

                cbFiltreTypeAlerte.SelectedIndex = 0;
                cbFiltreTypeHist.SelectedIndex = 0;
            }
            catch { }
        }


        private void ChargerAlertes()
        {
            if (!_isLoaded || db == null) return;

            try
            {
                string condition = "WHERE (KilometrageActuel - KmDernierEntretien) >= 5000";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrWhiteSpace(txtRechercheAlertes.Text))
                {
                    condition += " AND (Marque LIKE @s OR Modele LIKE @s OR Matricule LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRechercheAlertes.Text + "%"));
                }

                if (cbFiltreTypeAlerte.SelectedItem != null && cbFiltreTypeAlerte.SelectedIndex > 0)
                {
                    string type = cbFiltreTypeAlerte.SelectedItem.ToString();
                    condition += " AND NomProchainEntretien = @type";
                    parameters.Add(new MySqlParameter("@type", type));
                }

                string countQuery = $"SELECT COUNT(*) FROM Voitures {condition}";
                _totalAlertes = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalAlertes / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_pageAlertes > totalPages) _pageAlertes = totalPages;
                if (_pageAlertes < 1) _pageAlertes = 1;

                lblPaginationInfoAlertes.Text = $"Page {_pageAlertes} sur {totalPages} ({_totalAlertes})";
                btnPrevAlertes.IsEnabled = _pageAlertes > 1;
                btnNextAlertes.IsEnabled = _pageAlertes < totalPages;

                int offset = (_pageAlertes - 1) * _pageSize;
                string query = $@"
                    SELECT Id, CONCAT(Marque, ' ', Modele) AS Vehicule, Matricule, 
                            KilometrageActuel, KmDernierEntretien, NomProchainEntretien
                    FROM Voitures
                    {condition}
                    LIMIT {_pageSize} OFFSET {offset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());
                AlertesGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Erreur Alertes : " + ex.Message); }
        }

        private void TxtRechercheAlertes_TextChanged(object sender, TextChangedEventArgs e) { _pageAlertes = 1; ChargerAlertes(); }
        private void CbFiltreTypeAlerte_SelectionChanged(object sender, SelectionChangedEventArgs e) { _pageAlertes = 1; ChargerAlertes(); }
        private void BtnResetAlertes_Click(object sender, RoutedEventArgs e) { txtRechercheAlertes.Text = ""; cbFiltreTypeAlerte.SelectedIndex = 0; _pageAlertes = 1; ChargerAlertes(); }
        private void BtnPrevAlertes_Click(object sender, RoutedEventArgs e) { if (_pageAlertes > 1) { _pageAlertes--; ChargerAlertes(); } }
        private void BtnNextAlertes_Click(object sender, RoutedEventArgs e) { _pageAlertes++; ChargerAlertes(); }



        private void ChargerHistorique(bool loadAll = false)
        {
            if (!_isLoaded || db == null) return;

            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrWhiteSpace(txtRechercheHist.Text))
                {
                    condition += " AND (v.Marque LIKE @s OR v.Modele LIKE @s OR v.Matricule LIKE @s OR e.Description LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRechercheHist.Text + "%"));
                }

                if (cbFiltreTypeHist.SelectedItem != null && cbFiltreTypeHist.SelectedIndex > 0)
                {
                    string type = cbFiltreTypeHist.SelectedItem.ToString();
                    condition += " AND e.TypeEntretien = @type";
                    parameters.Add(new MySqlParameter("@type", type));
                }

                string orderBy = "e.DateEntretien DESC";
                if (cbTriHist.SelectedItem is ComboBoxItem item && item.Tag != null) orderBy = item.Tag.ToString();

                string countQuery = $"SELECT COUNT(*) FROM Entretiens e JOIN Voitures v ON e.VoitureId = v.Id {condition}";
                _totalHist = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalHist / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_pageHist > totalPages) _pageHist = totalPages;
                if (_pageHist < 1) _pageHist = 1;

                lblPaginationInfoHist.Text = $"Page {_pageHist} sur {totalPages} ({_totalHist})";
                btnPrevHist.IsEnabled = _pageHist > 1;
                btnNextHist.IsEnabled = _pageHist < totalPages;

                string limitOffset = loadAll ? "" : $"LIMIT {_pageSize} OFFSET {(_pageHist - 1) * _pageSize}";

                string query = $@"
                    SELECT e.Id AS IdEntretien, e.DateEntretien, e.TypeEntretien, e.Kilometrage, e.Cout, e.Description,
                            v.Matricule, v.Id AS VoitureId, CONCAT(v.Marque, ' ', v.Modele) AS Vehicule
                    FROM Entretiens e
                    JOIN Voitures v ON e.VoitureId = v.Id
                    {condition}
                    ORDER BY {orderBy}
                    {limitOffset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());

                if (!loadAll)
                {
                    HistoriqueGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    ExportHistoriqueToXlsx(dt);
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur Historique : " + ex.Message); }
        }

        private void TxtRechercheHist_TextChanged(object sender, TextChangedEventArgs e) { _pageHist = 1; ChargerHistorique(); }
        private void CbFiltreTypeHist_SelectionChanged(object sender, SelectionChangedEventArgs e) { _pageHist = 1; ChargerHistorique(); }
        private void CbTriHist_SelectionChanged(object sender, SelectionChangedEventArgs e) { ChargerHistorique(); }
        private void BtnResetHist_Click(object sender, RoutedEventArgs e) { txtRechercheHist.Text = ""; cbFiltreTypeHist.SelectedIndex = 0; cbTriHist.SelectedIndex = 0; _pageHist = 1; ChargerHistorique(); }
        private void BtnPrevHist_Click(object sender, RoutedEventArgs e) { if (_pageHist > 1) { _pageHist--; ChargerHistorique(); } }
        private void BtnNextHist_Click(object sender, RoutedEventArgs e) { _pageHist++; ChargerHistorique(); }




        private void BtnExporterHist_Click(object sender, RoutedEventArgs e)
        {
            ChargerHistorique(true);
        }

        private void ExportHistoriqueToXlsx(DataTable dt)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
                saveFileDialog.FileName = "export_historique_entretien_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        string[] exportColumns = { "Matricule", "DateEntretien", "TypeEntretien", "Kilometrage", "Cout", "Description" };

                        DataTable dtExport = new DataTable("HistoriqueEntretien");
                        foreach (string colName in exportColumns)
                        {
                            if (dt.Columns.Contains(colName))
                            {
                                Type columnType = dt.Columns[colName].DataType;
                                if (colName == "DateEntretien" && columnType != typeof(DateTime)) columnType = typeof(DateTime);
                                if (colName == "Cout" && columnType != typeof(double)) columnType = typeof(double);

                                dtExport.Columns.Add(colName, columnType);
                            }
                        }

                        foreach (DataRow row in dt.Rows)
                        {
                            DataRow newRow = dtExport.NewRow();
                            foreach (string colName in exportColumns)
                            {
                                if (dt.Columns.Contains(colName))
                                {
                                    newRow[colName] = row[colName];
                                }
                            }
                            dtExport.Rows.Add(newRow);
                        }

                        var worksheet = workbook.Worksheets.Add(dtExport, "Historique Entretien");

                        worksheet.Range(1, 1, 1, exportColumns.Length).Style.Font.Bold = true;
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show("Exportation réussie !");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'exportation de l'historique : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImporterHist_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";

            if (openFileDialog.ShowDialog() == true)
            {
                if (MessageBox.Show("Attention: L'importation va ajouter de nouvelles entrées d'entretien. Continuer ?", "Confirmation d'Importation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    ImportHistoriqueFromXlsx(openFileDialog.FileName);
                }
            }
        }

        private void ImportHistoriqueFromXlsx(string filePath)
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

                            DateTime dateEntretien = DateTime.Today;
                            string dateStr = worksheet.Cell(currentRow, 2).GetString().Trim();
                            if (!string.IsNullOrWhiteSpace(dateStr))
                            {
                                if (worksheet.Cell(currentRow, 2).DataType == XLDataType.DateTime)
                                    dateEntretien = worksheet.Cell(currentRow, 2).GetDateTime();
                                else if (!DateTime.TryParse(dateStr, out dateEntretien))
                                    throw new FormatException($"Date d'entretien '{dateStr}' non valide.");
                            }

                            string typeEntretien = worksheet.Cell(currentRow, 3).GetString().Trim();

                            string kmStr = worksheet.Cell(currentRow, 4).GetString().Trim();
                            int kilometrage = 0;
                            if (!string.IsNullOrWhiteSpace(kmStr))
                            {
                                int.TryParse(kmStr, out kilometrage);
                            }

                            string coutStr = worksheet.Cell(currentRow, 5).GetString().Replace(',', '.').Trim();
                            double cout = 0;
                            if (!string.IsNullOrWhiteSpace(coutStr))
                            {
                                double.TryParse(coutStr, NumberStyles.Any, CultureInfo.InvariantCulture, out cout);
                            }

                            string description = worksheet.Cell(currentRow, 6).GetString().Trim();

                            object voitureIdObj = db.ExecuteScalar("SELECT Id FROM Voitures WHERE Matricule = @mat",
                                new MySqlParameter[] { new MySqlParameter("@mat", matricule) });

                            if (voitureIdObj == null || voitureIdObj == DBNull.Value)
                            {
                                failedCount++;
                                errorDetails.Add($"Ligne {currentRow} ({matricule}): Véhicule non trouvé pour la matricule. Ligne ignorée.");
                                continue;
                            }
                            int voitureId = Convert.ToInt32(voitureIdObj);

                            string query = @"INSERT INTO Entretiens (VoitureId, DateEntretien, TypeEntretien, Kilometrage, Cout, Description)
                                             VALUES (@vId, @date, @type, @km, @cout, @desc)";

                            MySqlParameter[] parameters = new MySqlParameter[]
                            {
                                new MySqlParameter("@vId", voitureId),
                                new MySqlParameter("@date", dateEntretien),
                                new MySqlParameter("@type", typeEntretien),
                                new MySqlParameter("@km", kilometrage),
                                new MySqlParameter("@cout", cout),
                                new MySqlParameter("@desc", description)
                            };

                            db.ExecuteNonQuery(query, parameters);
                            importedCount++;

                            db.ExecuteNonQuery("UPDATE Voitures SET KmDernierEntretien = @km, KilometrageActuel = @km WHERE Id = @id AND KilometrageActuel < @km",
                                new MySqlParameter[] { new MySqlParameter("@km", kilometrage), new MySqlParameter("@id", voitureId) });

                        }
                        catch (Exception innerEx)
                        {
                            failedCount++;
                            errorDetails.Add($"Ligne {currentRow} (Matricule {currentMatricule}): Erreur format/BDD. Détail : {innerEx.Message}");
                        }
                    }
                }

                string finalMessage = $"Importation terminée : {importedCount} entretiens ajoutés, {failedCount} lignes ignorées.";
                if (failedCount > 0)
                {
                    finalMessage += "\n\n❌ Détails des échecs :\n" + string.Join("\n", errorDetails);
                    MessageBox.Show(finalMessage, "Importation XLSX - Avec Erreurs", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(finalMessage, "Importation XLSX", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ChargerHistorique();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur critique lors de la lecture du fichier XLSX : " + ex.Message, "Erreur Importation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (AlertesGrid.IsVisible) ChargerAlertes();
                else ChargerHistorique();
            }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterEntretienWindow win = new AjouterEntretienWindow();
            if (win.ShowDialog() == true)
            {
                ChargerAlertes();
                ChargerHistorique();
            }
        }

        private void BtnFaireEntretien_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int voitureId = Convert.ToInt32(btn.Tag);

                AjouterEntretienWindow win = new AjouterEntretienWindow(voitureId, 0);
                if (win.ShowDialog() == true)
                {
                    ChargerAlertes();
                    ChargerHistorique();
                }
            }
        }

        private void BtnModifierHist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int idEntretien = Convert.ToInt32(btn.Tag);

                AjouterEntretienWindow win = new AjouterEntretienWindow(0, idEntretien);
                if (win.ShowDialog() == true)
                {
                    ChargerHistorique();
                    ChargerAlertes();
                }
            }
        }

        private void BtnSupprimerHist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);
                if (MessageBox.Show("Supprimer cet historique ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        db.ExecuteNonQuery("DELETE FROM Entretiens WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                        ChargerHistorique();
                        ChargerAlertes();
                    }
                    catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
                }
            }
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            GestionTypesEntretienWindow win = new GestionTypesEntretienWindow();
            win.ShowDialog();
            ChargerFiltresTypes(); 
        }
    }
}