using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using LocationVoiture.Admin.Services;
using MySql.Data.MySqlClient;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;
using ClosedXML.Excel;

namespace LocationVoiture.Admin
{
    public partial class GestionLocationsWindow : Window
    {
        private DatabaseHelper db;
        private EmailService emailService;
        private PdfService pdfService;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public GestionLocationsWindow()
        {
            InitializeComponent();

            try
            {
                db = new DatabaseHelper();
                emailService = new EmailService();
                pdfService = new PdfService();
            }
            catch (Exception ex) { MessageBox.Show("Erreur Init : " + ex.Message); return; }

            _isLoaded = true;
            ChargerLocations();
        }

        private void ChargerLocations(bool loadAll = false)
        {
            if (!_isLoaded || db == null) return;

            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (c.Nom LIKE @s OR c.Prenom LIKE @s OR v.Marque LIKE @s OR v.Modele LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRecherche.Text + "%"));
                }

                if (cbFiltreStatut.SelectedIndex > 0)
                {
                    string statut = (cbFiltreStatut.SelectedItem as ComboBoxItem).Content.ToString();
                    condition += " AND l.Statut = @statut";
                    parameters.Add(new MySqlParameter("@statut", statut));
                }

                if (cbFiltrePaiement.SelectedItem is ComboBoxItem itemPay && itemPay.Tag.ToString() != "-1")
                {
                    condition += " AND l.EstPaye = @paye";
                    parameters.Add(new MySqlParameter("@paye", int.Parse(itemPay.Tag.ToString())));
                }

                string orderBy = "l.Id DESC";
                if (cbTri.SelectedItem is ComboBoxItem itemSort && itemSort.Tag != null)
                {
                    orderBy = itemSort.Tag.ToString();
                }

                string countQuery = $@"SELECT COUNT(*) FROM Locations l 
                                         JOIN Clients c ON l.ClientId = c.Id 
                                         JOIN Voitures v ON l.VoitureId = v.Id 
                                         {condition}";
                _totalRecords = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_currentPage > totalPages) _currentPage = totalPages;
                if (_currentPage < 1) _currentPage = 1;

                lblPaginationInfo.Text = $"Page {_currentPage} sur {totalPages} ({_totalRecords} locations)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                string limitOffset = loadAll ? "" : $"LIMIT {_pageSize} OFFSET {(_currentPage - 1) * _pageSize}";

                string query = $@"
                    SELECT l.Id, l.DateDebut, l.DateFin, l.PrixTotal, l.Statut, l.EstPaye,
                            c.Id AS ClientId, c.Email AS EmailClient, CONCAT(c.Prenom, ' ', c.Nom) AS NomClient,
                            v.Id AS VoitureId, v.Matricule AS MatriculeVoiture, CONCAT(v.Marque, ' ', v.Modele) AS ModeleVoiture
                    FROM Locations l
                    JOIN Clients c ON l.ClientId = c.Id
                    JOIN Voitures v ON l.VoitureId = v.Id
                    {condition}
                    ORDER BY {orderBy}
                    {limitOffset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());

                if (!loadAll)
                {
                    LocationsGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    ExportLocationsToXlsx(dt);
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }


        private void BtnExporter_Click(object sender, RoutedEventArgs e)
        {
            ChargerLocations(true);
        }

        private void BtnImporter_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";

            if (openFileDialog.ShowDialog() == true)
            {
                if (MessageBox.Show("Attention: L'importation va ajouter de nouvelles locations. Continuer ?", "Confirmation d'Importation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    ImportLocationsFromXlsx(openFileDialog.FileName);
                }
            }
        }

        private void ExportLocationsToXlsx(DataTable dt)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Fichier Excel (*.xlsx)|*.xlsx";
                saveFileDialog.FileName = "export_locations_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        string[] exportColumns = { "EmailClient", "MatriculeVoiture", "DateDebut", "DateFin", "PrixTotal", "Statut", "EstPaye" };

                        DataTable dtExport = new DataTable("Locations");
                        foreach (string colName in exportColumns)
                        {
                            if (dt.Columns.Contains(colName))
                            {
                                dtExport.Columns.Add(colName, dt.Columns[colName].DataType);
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

                        var worksheet = workbook.Worksheets.Add(dtExport, "Liste Locations");

                        worksheet.Range(1, 1, 1, exportColumns.Length).Style.Font.Bold = true;
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show("Exportation réussie !");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'exportation des locations : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportLocationsFromXlsx(string filePath)
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
                        string currentKey = "N/A"; 

                        try
                        {
                            string emailClient = worksheet.Cell(currentRow, 1).GetString().Trim();
                            string matriculeVoiture = worksheet.Cell(currentRow, 2).GetString().Trim();
                            currentKey = $"{emailClient} / {matriculeVoiture}";

                            if (string.IsNullOrWhiteSpace(emailClient) || string.IsNullOrWhiteSpace(matriculeVoiture))
                            {
                                throw new Exception("Email du client ou matricule de la voiture manquante.");
                            }

                            object clientIdObj = db.ExecuteScalar("SELECT Id FROM Clients WHERE Email = @email",
                                new MySqlParameter[] { new MySqlParameter("@email", emailClient) });

                            if (clientIdObj == null || clientIdObj == DBNull.Value)
                            {
                                throw new Exception($"Client non trouvé pour l'email '{emailClient}'.");
                            }
                            int clientId = Convert.ToInt32(clientIdObj);

                            object voitureIdObj = db.ExecuteScalar("SELECT Id FROM Voitures WHERE Matricule = @mat",
                                new MySqlParameter[] { new MySqlParameter("@mat", matriculeVoiture) });

                            if (voitureIdObj == null || voitureIdObj == DBNull.Value)
                            {
                                throw new Exception($"Voiture non trouvée pour la matricule '{matriculeVoiture}'.");
                            }
                            int voitureId = Convert.ToInt32(voitureIdObj);


                            DateTime dateDebut = DateTime.MinValue;
                            if (worksheet.Cell(currentRow, 3).DataType == XLDataType.DateTime)
                                dateDebut = worksheet.Cell(currentRow, 3).GetDateTime();
                            else if (!DateTime.TryParse(worksheet.Cell(currentRow, 3).GetString().Trim(), out dateDebut))
                                throw new FormatException($"Date de début '{worksheet.Cell(currentRow, 3).GetString()}' non valide.");

                            DateTime dateFin = DateTime.MinValue;
                            if (worksheet.Cell(currentRow, 4).DataType == XLDataType.DateTime)
                                dateFin = worksheet.Cell(currentRow, 4).GetDateTime();
                            else if (!DateTime.TryParse(worksheet.Cell(currentRow, 4).GetString().Trim(), out dateFin))
                                throw new FormatException($"Date de fin '{worksheet.Cell(currentRow, 4).GetString()}' non valide.");

                            string prixStr = worksheet.Cell(currentRow, 5).GetString().Replace(',', '.').Trim();
                            double prixTotal = 0;
                            if (!string.IsNullOrWhiteSpace(prixStr))
                            {
                                double.TryParse(prixStr, NumberStyles.Any, CultureInfo.InvariantCulture, out prixTotal);
                            }

                            string statut = worksheet.Cell(currentRow, 6).GetString().Trim();
                            string estPayeStr = worksheet.Cell(currentRow, 7).GetString().Trim();
                            bool estPaye = estPayeStr.Equals("true", StringComparison.OrdinalIgnoreCase) || estPayeStr == "1";


                            string query = @"INSERT INTO Locations (ClientId, VoitureId, DateDebut, DateFin, PrixTotal, Statut, EstPaye)
                                             VALUES (@cId, @vId, @dateD, @dateF, @prix, @statut, @paye)";

                            MySqlParameter[] parameters = new MySqlParameter[]
                            {
                                new MySqlParameter("@cId", clientId),
                                new MySqlParameter("@vId", voitureId),
                                new MySqlParameter("@dateD", dateDebut),
                                new MySqlParameter("@dateF", dateFin),
                                new MySqlParameter("@prix", prixTotal),
                                new MySqlParameter("@statut", statut),
                                new MySqlParameter("@paye", estPaye)
                            };

                            db.ExecuteNonQuery(query, parameters);
                            importedCount++;
                        }
                        catch (Exception innerEx)
                        {
                            failedCount++;
                            errorDetails.Add($"Ligne {currentRow} ({currentKey}): Erreur : {innerEx.Message}");
                        }
                    }
                }

                string finalMessage = $"Importation terminée : {importedCount} locations ajoutées, {failedCount} lignes ignorées.";
                if (failedCount > 0)
                {
                    finalMessage += "\n\n❌ Détails des échecs :\n" + string.Join("\n", errorDetails);
                    MessageBox.Show(finalMessage, "Importation XLSX - Avec Erreurs", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(finalMessage, "Importation XLSX", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ChargerLocations();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur critique lors de la lecture du fichier XLSX : " + ex.Message, "Erreur Importation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) { _currentPage = 1; ChargerLocations(); }
        private void CbFiltre_SelectionChanged(object sender, SelectionChangedEventArgs e) { _currentPage = 1; ChargerLocations(); }
        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e) { ChargerLocations(); }
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtRecherche.Text = ""; cbFiltreStatut.SelectedIndex = 0; cbFiltrePaiement.SelectedIndex = 0; cbTri.SelectedIndex = 0; _currentPage = 1; ChargerLocations(); }
        private void BtnPrev_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; ChargerLocations(); } }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { _currentPage++; ChargerLocations(); }



        private void BtnPayer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int idLoc = Convert.ToInt32(((Button)sender).Tag);

                string queryPrix = "SELECT PrixTotal FROM Locations WHERE Id = @id";
                object result = db.ExecuteScalar(queryPrix, new MySqlParameter[] { new MySqlParameter("@id", idLoc) });

                decimal prixTotal = 0;
                if (result != null && result != DBNull.Value)
                {
                    string prixStr = result.ToString().Replace(",", ".");
                    decimal.TryParse(prixStr, NumberStyles.Any, CultureInfo.InvariantCulture, out prixTotal);
                }

                AjouterPaiementWindow winPaiement = new AjouterPaiementWindow(idLoc, prixTotal);

                if (winPaiement.ShowDialog() == true)
                {
                    ChargerLocations();
                    MessageBox.Show("Paiement enregistré et location mise à jour !");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'encaissement : " + ex.Message);
            }
        }
        

        private async void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView;
            if (row == null) return;

            int idLoc = Convert.ToInt32(row["Id"]);
            if (row["Statut"].ToString() == "Confirmée") { MessageBox.Show("Déjà confirmée."); return; }

            string email = row["EmailClient"].ToString();
            string nom = row["NomClient"].ToString();
            string voiture = row["ModeleVoiture"].ToString();
            DateTime debut = Convert.ToDateTime(row["DateDebut"]);
            DateTime fin = Convert.ToDateTime(row["DateFin"]);

            string prixStr = row["PrixTotal"].ToString().Replace(",", ".");
            decimal prix = 0; decimal.TryParse(prixStr, NumberStyles.Any, CultureInfo.InvariantCulture, out prix);
            string dates = $"{debut:dd/MM} au {fin:dd/MM/yyyy}";

            if (MessageBox.Show("Confirmer la réservation ?", "Validation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    byte[] pdfBytes = pdfService.GenererBonReservation(idLoc, nom, voiture, debut, fin, prix);

                    if (pdfBytes == null || pdfBytes.Length == 0)
                    {
                        MessageBox.Show("Erreur : Le service PDF n'a pas pu générer le fichier.");
                        return;
                    }

                    db.ExecuteNonQuery("UPDATE Locations SET Statut = 'Confirmée', BonReservation = @pdf WHERE Id = @id",
                        new MySqlParameter[] {
            new MySqlParameter("@id", idLoc),
            new MySqlParameter("@pdf", pdfBytes)
                        });

                    await emailService.EnvoyerEmailConfirmation(email, nom, voiture, dates, prix, pdfBytes);

                    string tempFile = Path.Combine(Path.GetTempPath(), $"Bon_Reservation_{idLoc}.pdf");
                    File.WriteAllBytes(tempFile, pdfBytes);
                    Process.Start(new ProcessStartInfo { FileName = tempFile, UseShellExecute = true });

                    MessageBox.Show("Réservation confirmée et Email envoyé avec le bon de réservation !");
                    ChargerLocations();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors de la validation : " + ex.Message);
                }
            }
        }

        private void BtnVoirRecu_Click(object sender, RoutedEventArgs e)
        {
            int idLoc = Convert.ToInt32(((Button)sender).Tag);
            try
            {
                object res = db.ExecuteScalar("SELECT BonReservation FROM Locations WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", idLoc) });
                if (res == DBNull.Value || res == null) { MessageBox.Show("Pas de reçu."); return; }

                byte[] pdfBytes = (byte[])res;
                string tempFile = Path.Combine(Path.GetTempPath(), $"Recu_{idLoc}.pdf");
                File.WriteAllBytes(tempFile, pdfBytes);
                Process.Start(new ProcessStartInfo { FileName = tempFile, UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private void BtnRefuser_Click(object sender, RoutedEventArgs e)
        {
            var row = ((Button)sender).Tag as DataRowView;
            if (row == null) return;
            int id = Convert.ToInt32(row["Id"]);

            if (MessageBox.Show("Annuler ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                db.ExecuteNonQuery("UPDATE Locations SET Statut = 'Annulée', BonReservation = NULL WHERE Id = @id",
                    new MySqlParameter[] { new MySqlParameter("@id", id) });
                ChargerLocations();
            }
        }
    }
}