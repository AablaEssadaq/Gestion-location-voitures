using System;
using System.Data;
using MySql.Data.MySqlClient; // Nécessite l'installation du package NuGet 'MySql.Data'

namespace LocationVoiture.Data
{
    public class DatabaseHelper
    {
        // =========================================================================
        // 1. CONFIGURATION DE LA CONNEXION
        // =========================================================================

        // Si vous utilisez XAMPP ou WAMP par défaut :
        // Server=localhost | Uid=root | Pwd= (vide)
        // Si vous avez un mot de passe, écrivez-le après "Pwd="
        private string _connectionString = "Server=localhost;Database=GestionLocationDB;Uid=root;Pwd=;";

        // =========================================================================
        // 2. MÉTHODES D'ACCÈS AUX DONNÉES
        // =========================================================================

        /// <summary>
        /// Exécute une requête de LECTURE (SELECT) et retourne un tableau de résultats.
        /// </summary>
        public DataTable ExecuteQuery(string query, MySqlParameter[] parameters = null)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    // Ajout des paramètres (ex: @id, @nom) pour éviter les injections SQL
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    try
                    {
                        // Le DataAdapter ouvre et ferme la connexion automatiquement
                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        return table;
                    }
                    catch (Exception ex)
                    {
                        // On remonte l'erreur pour pouvoir l'afficher dans l'application
                        throw new Exception("Erreur MySQL (SELECT) : " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Exécute une requête d'ÉCRITURE (INSERT, UPDATE, DELETE).
        /// Retourne le nombre de lignes modifiées.
        /// </summary>
        public int ExecuteNonQuery(string query, MySqlParameter[] parameters = null)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    try
                    {
                        conn.Open(); // Il faut ouvrir manuellement pour ExecuteNonQuery
                        return cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erreur MySQL (INSERT/UPDATE) : " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Exécute une requête qui retourne UNE SEULE VALEUR (ex: COUNT(*), SUM(Prix)...).
        /// </summary>
        public object ExecuteScalar(string query, MySqlParameter[] parameters = null)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    try
                    {
                        conn.Open();
                        return cmd.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erreur MySQL (SCALAR) : " + ex.Message);
                    }
                }
            }
        }
    }


}