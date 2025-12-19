using System;
using System.Data;
using MySql.Data.MySqlClient; 

namespace LocationVoiture.Data
{
    public class DatabaseHelper
    {

        private string _connectionString = "Server=localhost;Database=GestionLocationDB;Uid=root;Pwd=;";


        public DataTable ExecuteQuery(string query, MySqlParameter[] parameters = null)
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
                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable table = new DataTable();
                        adapter.Fill(table);
                        return table;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erreur MySQL (SELECT) : " + ex.Message);
                    }
                }
            }
        }


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
                        conn.Open(); 
                        return cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Erreur MySQL (INSERT/UPDATE) : " + ex.Message);
                    }
                }
            }
        }


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