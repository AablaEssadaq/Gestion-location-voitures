using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using LocationVoiture.Data;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.Security.Cryptography;
using System.Text;

namespace LocationVoiture.Admin
{
    public partial class GestionUtilisateursWindow : Window
    {
        private DatabaseHelper db;
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalRecords = 0;
        private bool _isLoaded = false;

        public GestionUtilisateursWindow()
        {
            InitializeComponent();
            try { db = new DatabaseHelper(); }
            catch (Exception ex) { MessageBox.Show("Erreur BDD : " + ex.Message); return; }
            _isLoaded = true;
            ChargerUtilisateurs();
        }

        private string HasherMotDePasse(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void ImportUtilisateursFromXlsx(string filePath)
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
                            string nom = row.Cell(1).GetString().Trim();
                            string prenom = row.Cell(2).GetString().Trim();
                            string email = row.Cell(3).GetString().Trim();
                            string role = row.Cell(4).GetString().Trim();

                            string mdpClair = "123456";

                            string mdpHache = HasherMotDePasse(mdpClair);

                            string query = "INSERT INTO Utilisateurs (Nom, Prenom, Email, Role, MotDePasse) VALUES (@n, @p, @e, @r, @m)";
                            MySqlParameter[] ps = {
                                new MySqlParameter("@n", nom),
                                new MySqlParameter("@p", prenom),
                                new MySqlParameter("@e", email),
                                new MySqlParameter("@r", role),
                                new MySqlParameter("@m", mdpHache)
                            };

                            db.ExecuteNonQuery(query, ps);
                            success++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Erreur ligne : " + ex.Message);
                            fail++;
                        }
                    }
                }
                MessageBox.Show($"Import terminé : {success} membres ajoutés (mots de passe hachés), {fail} échecs.");
                ChargerUtilisateurs();
            }
            catch (Exception ex) { MessageBox.Show("Erreur critique : " + ex.Message); }
        }

        private void ChargerUtilisateurs(bool loadAll = false)
        {
            if (!_isLoaded || db == null) return;
            try
            {
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();
                if (!string.IsNullOrWhiteSpace(txtRecherche.Text))
                {
                    condition += " AND (Nom LIKE @s OR Prenom LIKE @s OR Email LIKE @s)";
                    parameters.Add(new MySqlParameter("@s", "%" + txtRecherche.Text + "%"));
                }
                if (cbFiltreRole.SelectedIndex > 0)
                {
                    string role = (cbFiltreRole.SelectedItem as ComboBoxItem).Content.ToString();
                    condition += " AND Role = @role";
                    parameters.Add(new MySqlParameter("@role", role));
                }
                _totalRecords = Convert.ToInt32(db.ExecuteScalar($"SELECT COUNT(*) FROM Utilisateurs {condition}", parameters.ToArray()));
                int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (totalPages == 0) totalPages = 1;
                if (_currentPage > totalPages) _currentPage = totalPages;
                lblPaginationInfo.Text = $"Page {_currentPage} / {totalPages} ({_totalRecords})";
                btnPrev.IsEnabled = _currentPage > 1;
                btnNext.IsEnabled = _currentPage < totalPages;

                string limitOffset = loadAll ? "" : $"LIMIT {_pageSize} OFFSET {(_currentPage - 1) * _pageSize}";
                string query = $"SELECT Id, Nom, Prenom, Email, Role FROM Utilisateurs {condition} ORDER BY Id DESC {limitOffset}";
                DataTable dt = db.ExecuteQuery(query, parameters.ToArray());

                if (!loadAll) UsersGrid.ItemsSource = dt.DefaultView;
                else ExportUtilisateursToXlsx(dt);
            }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private void ExportUtilisateursToXlsx(DataTable dt)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "Equipe_" + DateTime.Now.ToString("yyyyMMdd") };
                if (sfd.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Utilisateurs");
                        string[] columns = { "Nom", "Prenom", "Email", "Role" };
                        for (int i = 0; i < columns.Length; i++) worksheet.Cell(1, i + 1).Value = columns[i];
                        for (int r = 0; r < dt.Rows.Count; r++)
                        {
                            for (int c = 0; c < columns.Length; c++)
                                worksheet.Cell(r + 2, c + 1).Value = dt.Rows[r][columns[c]].ToString();
                        }
                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }
                    MessageBox.Show("Exportation réussie !");
                }
            }
            catch (Exception ex) { MessageBox.Show("Erreur export : " + ex.Message); }
        }

        private void BtnExporter_Click(object sender, RoutedEventArgs e) => ChargerUtilisateurs(true);
        private void BtnImporter_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
            if (ofd.ShowDialog() == true)
            {
                if (MessageBox.Show("Importer avec mdp '123456' haché ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    ImportUtilisateursFromXlsx(ofd.FileName);
            }
        }
        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e) { _currentPage = 1; ChargerUtilisateurs(); }
        private void CbFiltreRole_SelectionChanged(object sender, SelectionChangedEventArgs e) { _currentPage = 1; ChargerUtilisateurs(); }
        private void BtnReset_Click(object sender, RoutedEventArgs e) { txtRecherche.Text = ""; cbFiltreRole.SelectedIndex = 0; _currentPage = 1; ChargerUtilisateurs(); }
        private void BtnPrev_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; ChargerUtilisateurs(); } }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { _currentPage++; ChargerUtilisateurs(); }
        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            AjouterUtilisateurWindow win = new AjouterUtilisateurWindow();
            if (win.ShowDialog() == true)
            {
                ChargerUtilisateurs(); 
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int userId = Convert.ToInt32(btn.Tag);

                AjouterUtilisateurWindow win = new AjouterUtilisateurWindow(userId);

                if (win.ShowDialog() == true)
                {
                    ChargerUtilisateurs(); 
                }
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int id = Convert.ToInt32(btn.Tag);
                if (MessageBox.Show("Supprimer cet utilisateur ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        db.ExecuteNonQuery("DELETE FROM Utilisateurs WHERE Id = @id", new MySqlParameter[] { new MySqlParameter("@id", id) });
                        ChargerUtilisateurs();
                    }
                    catch (Exception ex) { MessageBox.Show("Erreur suppression : " + ex.Message); }
                }
            }
        }
    }
}