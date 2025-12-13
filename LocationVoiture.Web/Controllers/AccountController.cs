using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;
using LocationVoiture.Web.Utilities; // IMPORTANT : Pour utiliser PasswordHelper

namespace LocationVoiture.Web.Controllers
{
    public class AccountController : Controller
    {
        private DatabaseHelper db;

        public AccountController()
        {
            db = new DatabaseHelper();
        }

        // === INSCRIPTION ===
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Client client)
        {
            if (string.IsNullOrWhiteSpace(client.Nom) || string.IsNullOrWhiteSpace(client.Email) || string.IsNullOrWhiteSpace(client.MotDePasse))
            {
                ViewBag.Error = "Veuillez remplir les champs obligatoires.";
                return View(client);
            }

            try
            {
                // 1. Vérifier si l'email existe déjà
                string checkQuery = "SELECT COUNT(*) FROM Clients WHERE Email = @email";
                MySqlParameter[] pCheck = { new MySqlParameter("@email", client.Email) };
                int count = Convert.ToInt32(db.ExecuteScalar(checkQuery, pCheck));

                if (count > 0)
                {
                    ViewBag.Error = "Cet email est déjà utilisé.";
                    return View(client);
                }

                // 2. HACHER LE MOT DE PASSE
                string motDePasseHache = PasswordHelper.HashPassword(client.MotDePasse);

                // 3. Insertion avec le mot de passe haché
                string query = @"INSERT INTO Clients (Nom, Prenom, Email, Telephone, NumPermis, MotDePasse) 
                                 VALUES (@nom, @prenom, @email, @tel, @permis, @mdp)";

                MySqlParameter[] p = {
                    new MySqlParameter("@nom", client.Nom),
                    new MySqlParameter("@prenom", client.Prenom),
                    new MySqlParameter("@email", client.Email),
                    new MySqlParameter("@tel", client.Telephone),
                    new MySqlParameter("@permis", client.NumPermis),
                    new MySqlParameter("@mdp", motDePasseHache) // On envoie le hash, pas le clair
                };

                db.ExecuteNonQuery(query, p);

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur technique : " + ex.Message;
                return View(client);
            }
        }

        // === CONNEXION ===
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            try
            {
                // 1. On récupère l'utilisateur par son Email UNIQUEMENT
                string query = "SELECT * FROM Clients WHERE Email = @email";
                MySqlParameter[] p = { new MySqlParameter("@email", email) };
                DataTable dt = db.ExecuteQuery(query, p);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    string storedHash = row["MotDePasse"].ToString();

                    // 2. On vérifie si le mot de passe saisi correspond au hash stocké
                    if (PasswordHelper.VerifyPassword(password, storedHash))
                    {
                        // SUCCÈS
                        HttpContext.Session.SetString("ClientId", row["Id"].ToString());
                        HttpContext.Session.SetString("ClientNom", row["Prenom"].ToString() + " " + row["Nom"].ToString());
                        return RedirectToAction("Index", "Home");
                    }
                }

                // ECHEC (Soit email introuvable, soit mot de passe faux)
                ViewBag.Error = "Email ou mot de passe incorrect.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return View();
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}