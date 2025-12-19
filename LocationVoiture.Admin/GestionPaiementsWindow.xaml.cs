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
    public partial class GestionPaiementsWindow : Window
    {
        private DatabaseHelper db;

        private int _currentPage = 1;
        private int _pageSize = 15;
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public GestionPaiementsWindow()
        {
            InitializeComponent();

            try { db = new DatabaseHelper(); }
            catch (Exception ex) { MessageBox.Show("Erreur BDD : " + ex.Message); return; }

            _isLoaded = true;
            ChargerPaiements();
        }

        private void ChargerPaiements(bool loadAll = false)
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

                if (cbFiltreMethode.SelectedIndex > 0)
                {
                    string methode = (cbFiltreMethode.SelectedItem as ComboBoxItem).Content.ToString();
                    condition += " AND p.Methode = @methode";
                    parameters.Add(new MySqlParameter("@methode", methode));
                }

                string orderBy = "p.DatePaiement DESC";
                if (cbTri.SelectedItem is ComboBoxItem itemSort && itemSort.Tag != null)
                {
                    orderBy = itemSort.Tag.ToString();
                }

                string countQuery = $@"SELECT COUNT(*) FROM Paiements p 
                                       JOIN Locations l ON p.LocationId = l.Id
                                       JOIN Clients c ON l.ClientId = c.Id
                                       JOIN Voitures v ON l.VoitureId = v.Id
                                       {condition}";
                _totalRecords = Convert.ToInt32(db.ExecuteScalar(countQuery, parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_currentPage > totalPages) _currentPage = totalPages;
                if (_currentPage < 1) _currentPage = 1;

                lblPaginationInfo.Text = $"Page {_currentPage} / {totalPages} ({_totalRecords} paiements)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                string limitOffset = loadAll ? "" : $"LIMIT {_pageSize} OFFSET {(_currentPage - 1) * _pageSize}";

                string query = $@"
                    SELECT p.Id, p.Montant, p.DatePaiement, p.Methode, p.LocationId,
                           CONCAT(c.Prenom, ' ', c.Nom) AS NomClient,
                           CONCAT(v.Marque, ' ', v.Modele) AS ModeleVoiture
                    FROM Paiements p
                    JOIN Locations l ON p.LocationId = l.Id
                    JOIN Clients c ON l.ClientId = c.Id
                    JOIN Voitures v ON l.VoitureId = v.Id
                    {condition}
                    ORDER BY {orderBy}
                    {limitOffset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());

                if (!loadAll)
                {
                    PaiementsGrid.ItemsSource = dt.DefaultView;
                }
                else
                {
                    ExportPaiementsToXlsx(dt);
                }

                string sumQuery = $@"SELECT SUM(p.Montant) FROM Paiements p 
                                     JOIN Locations l ON p.LocationId = l.Id
                                     JOIN Clients c ON l.ClientId = c.Id
                                     JOIN Voitures v ON l.VoitureId = v.Id
                                     {condition}";

                object sumRes = db.ExecuteScalar(sumQuery, parameters.ToArray());
                decimal total = (sumRes != DBNull.Value && sumRes != null) ? Convert.ToDecimal(sumRes) : 0;
                txtTotalGeneral.Text = $"{total:N2} DH";
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        private void BtnExporter_Click(object sender, RoutedEventArgs e) => ChargerPaiements(true);

        private void ExportPaiementsToXlsx(DataTable dt)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "Journal_Caisse_" + DateTime.Now.ToString("yyyyMMdd") };
                if (sfd.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Paiements");
                        string[] cols = { "LocationId", "Montant", "DatePaiement", "Methode", "NomClient", "ModeleVoiture" };

                        for (int i = 0; i < cols.Length; i++) worksheet.Cell(1, i + 1).Value = cols[i];

                        for (int r = 0; r < dt.Rows.Count; r++)
                        {
                            for (int c = 0; c < cols.Length; c++)
                                worksheet.Cell(r + 2, c + 1).Value = dt.Rows[r][cols[c]].ToString();
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }
                    MessageBox.Show("Exportation réussie !");
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur export : " + ex.Message); }
        }

        private void BtnImporter_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
            if (ofd.ShowDialog() == true) ImportPaiementsFromXlsx(ofd.FileName);
        }

        private void ImportPaiementsFromXlsx(string filePath)
        {
            int success = 0, fail = 0;
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var sheet = workbook.Worksheet(1);
                    var rows = sheet.RangeUsed().RowsUsed();

                    foreach (var row in rows)
                    {
                        if (row.RowNumber() == 1) continue;

                        try
                        {
                            int locId = int.Parse(row.Cell(1).GetString());
                            decimal montant = decimal.Parse(row.Cell(2).GetString().Replace(',', '.'), CultureInfo.InvariantCulture);
                            DateTime dateP = DateTime.Parse(row.Cell(3).GetString());
                            string methode = row.Cell(4).GetString();

                            string query = "INSERT INTO Paiements (LocationId, Montant, DatePaiement, Methode) VALUES (@id, @m, @d, @meth)";
                            MySqlParameter[] ps = {
                                new MySqlParameter("@id", locId),
                                new MySqlParameter("@m", montant),
                                new MySqlParameter("@d", dateP),
                                new MySqlParameter("@meth", methode)
                            };

                            db.ExecuteNonQuery(query, ps);
                            db.ExecuteNonQuery("UPDATE Locations SET EstPaye = 1 WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", locId) });

                            success++;
                        }
                        catch { fail++; }
                    }
                }
                MessageBox.Show($"Import terminé : {success} ajoutés, {fail} échecs.");
                ChargerPaiements();
            }
            catch (Exception ex) { MessageBox.Show("Erreur critique : " + ex.Message); }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) { _currentPage = 1; ChargerPaiements(); }
        private void CbFiltre_SelectionChanged(object sender, SelectionChangedEventArgs e) { _currentPage = 1; ChargerPaiements(); }
        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e) { ChargerPaiements(); }
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtRecherche.Text = ""; cbFiltreMethode.SelectedIndex = 0; cbTri.SelectedIndex = 0; _currentPage = 1; ChargerPaiements(); }
        private void BtnPrev_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; ChargerPaiements(); } }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { _currentPage++; ChargerPaiements(); }
    }
}