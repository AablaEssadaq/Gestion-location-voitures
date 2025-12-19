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
    public partial class GestionClientsWindow : Window
    {
        private DatabaseHelper db;
        private int _currentPage = 1;
        private int _pageSize = 12;
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public GestionClientsWindow()
        {
            InitializeComponent();
            db = new DatabaseHelper();
            _isLoaded = true;
            ChargerClients();
        }

        private void ChargerClients(bool loadAll = false)
        {
            if (!_isLoaded) return;

            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (Nom LIKE @s OR Prenom LIKE @s OR Email LIKE @s OR Telephone LIKE @s OR NumPermis LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRecherche.Text + "%"));
                }

                string orderBy = "Id DESC";
                if (cbTri.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    orderBy = item.Tag.ToString();
                }

                _totalRecords = Convert.ToInt32(db.ExecuteScalar($"SELECT COUNT(*) FROM Clients {condition}", parameters.ToArray()));

                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_currentPage > totalPages) _currentPage = totalPages;

                lblPaginationInfo.Text = $"Page {_currentPage} sur {totalPages} ({_totalRecords} clients)";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                string limitOffset = loadAll ? "" : $"LIMIT {_pageSize} OFFSET {(_currentPage - 1) * _pageSize}";

                string query = $"SELECT Id, Nom, Prenom, Email, Telephone, NumPermis FROM Clients {condition} ORDER BY {orderBy} {limitOffset}";

                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());

                if (!loadAll)
                    ClientsGrid.ItemsSource = dt.DefaultView;
                else
                    ExportClientsToXlsx(dt);
            }
            catch (Exception ex) { MessageBox.Show("Erreur chargement : " + ex.Message); }
        }

        private void ExportClientsToXlsx(DataTable dt)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"Export_Clients_{DateTime.Now:yyyyMMdd}.xlsx" };
                if (sfd.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Clients");
                        string[] cols = { "Nom", "Prenom", "Email", "Telephone", "NumPermis" };

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

        private void ImportClientsFromXlsx(string filePath)
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
                            string query = "INSERT INTO Clients (Nom, Prenom, Email, Telephone, NumPermis) VALUES (@n, @p, @e, @t, @per)";
                            MySqlParameter[] ps = {
                                new MySqlParameter("@n", row.Cell(1).GetString()),
                                new MySqlParameter("@p", row.Cell(2).GetString()),
                                new MySqlParameter("@e", row.Cell(3).GetString()),
                                new MySqlParameter("@t", row.Cell(4).GetString()),
                                new MySqlParameter("@per", row.Cell(5).GetString())
                            };
                            db.ExecuteNonQuery(query, ps);
                            success++;
                        }
                        catch { fail++; }
                    }
                }
                MessageBox.Show($"Importation terminée : {success} succès, {fail} échecs.");
                ChargerClients();
            }
            catch (Exception ex) { MessageBox.Show("Erreur critique : " + ex.Message); }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) { _currentPage = 1; ChargerClients(); }
        private void CbTri_SelectionChanged(object sender, SelectionChangedEventArgs e) { ChargerClients(); }
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtRecherche.Text = ""; cbTri.SelectedIndex = 0; ChargerClients(); }
        private void BtnPrev_Click(object sender, RoutedEventArgs e) { _currentPage--; ChargerClients(); }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { _currentPage++; ChargerClients(); }
        private void BtnExporter_Click(object sender, RoutedEventArgs e) => ChargerClients(true);
        private void BtnImporter_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
            if (ofd.ShowDialog() == true) ImportClientsFromXlsx(ofd.FileName);
        }
        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterClientWindow win = new AjouterClientWindow();

            if (win.ShowDialog() == true)
            {
                ChargerClients(); 
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);

                AjouterClientWindow win = new AjouterClientWindow(id);

                if (win.ShowDialog() == true)
                {
                    ChargerClients(); 
                }
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);
                if (MessageBox.Show("Supprimer ce client ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        db.ExecuteNonQuery("DELETE FROM Clients WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                        ChargerClients(); 
                    }
                    catch (Exception ex) { MessageBox.Show("Erreur suppression : " + ex.Message); }
                }
            }
        }
    }
}