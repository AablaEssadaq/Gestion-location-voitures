using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;
using LocationVoiture.Web.Utilities; 

namespace LocationVoiture.Web.Controllers
{
    public class ClientSpaceController : Controller
    {
        private DatabaseHelper db;

        public ClientSpaceController()
        {
            db = new DatabaseHelper();
        }

        public IActionResult Index()
        {
            string clientId = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");

            try
            {
                string query = @"
                    SELECT l.Id, l.DateDebut, l.DateFin, l.PrixTotal, l.Statut, 
                           v.Marque, v.Modele, v.ImageChemin
                    FROM Locations l
                    JOIN Voitures v ON l.VoitureId = v.Id
                    WHERE l.ClientId = @clientId
                    ORDER BY l.Id DESC";

                MySqlParameter[] p = { new MySqlParameter("@clientId", clientId) };
                DataTable dt = db.ExecuteQuery(query, p);
                return View(dt);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return View(new DataTable());
            }
        }

        public IActionResult Profile()
        {
            string clientId = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");

            try
            {
                string query = "SELECT * FROM Clients WHERE Id = @id";
                MySqlParameter[] p = { new MySqlParameter("@id", clientId) };
                DataTable dt = db.ExecuteQuery(query, p);

                if (dt.Rows.Count > 0) return View(dt.Rows[0]);
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return View(null);
            }
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            string clientId = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientId)) return RedirectToAction("Login", "Account");

            string query = "SELECT * FROM Clients WHERE Id = @id";
            DataTable dt = db.ExecuteQuery(query, new MySqlParameter[] { new MySqlParameter("@id", clientId) });

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                Client c = new Client
                {
                    Id = Convert.ToInt32(row["Id"]),
                    Nom = row["Nom"].ToString(),
                    Prenom = row["Prenom"].ToString(),
                    Email = row["Email"].ToString(),
                    Telephone = row["Telephone"].ToString(),
                    NumPermis = row["NumPermis"].ToString(),
                };
                return View(c);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult EditProfile(Client client)
        {
            string clientIdStr = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientIdStr)) return RedirectToAction("Login", "Account");

            try
            {
                string query;
                List<MySqlParameter> p = new List<MySqlParameter>
                {
                    new MySqlParameter("@nom", client.Nom),
                    new MySqlParameter("@prenom", client.Prenom),
                    new MySqlParameter("@email", client.Email),
                    new MySqlParameter("@tel", client.Telephone),
                    new MySqlParameter("@permis", client.NumPermis),
                    new MySqlParameter("@id", clientIdStr)
                };

                if (!string.IsNullOrWhiteSpace(client.MotDePasse))
                {
                    query = @"UPDATE Clients SET 
                              Nom=@nom, Prenom=@prenom, Email=@email, 
                              Telephone=@tel, NumPermis=@permis, MotDePasse=@mdp 
                              WHERE Id=@id";

                    string mdpHache = PasswordHelper.HashPassword(client.MotDePasse);
                    p.Add(new MySqlParameter("@mdp", mdpHache));
                }
                else
                {
                    query = @"UPDATE Clients SET 
                              Nom=@nom, Prenom=@prenom, Email=@email, 
                              Telephone=@tel, NumPermis=@permis 
                              WHERE Id=@id";
                }

                db.ExecuteNonQuery(query, p.ToArray());

                HttpContext.Session.SetString("ClientNom", client.Prenom + " " + client.Nom);
                TempData["Success"] = "Profil mis à jour avec succès !";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return View(client);
            }
        }
    }
}