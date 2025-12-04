using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Data; // Accès à vos modèles (Client) et DatabaseHelper
using System.Data;
using MySql.Data.MySqlClient; // Pour MySQL

namespace LocationVoiture.Web.Controllers
{
    public class AccountController : Controller
    {
        private DatabaseHelper db;

        public AccountController()
        {
            db = new DatabaseHelper();
        }

        // ==========================================
        // PARTIE INSCRIPTION (REGISTER)
        // ==========================================

        // 1. Affiche la page d'inscription
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // 2. Reçoit les données du formulaire
        [HttpPost]
        public IActionResult Register(Client client)
        {
            // Vérification simple
            if (string.IsNullOrWhiteSpace(client.Nom) || string.IsNullOrWhiteSpace(client.Email) || string.IsNullOrWhiteSpace(client.MotDePasse))
            {
                ViewBag.Error = "Veuillez remplir les champs obligatoires (*).";
                return View(client);
            }

            try
            {
                // Vérifier si l'email existe déjà
                string checkQuery = "SELECT COUNT(*) FROM Clients WHERE Email = @email";
                MySqlParameter[] pCheck = { new MySqlParameter("@email", client.Email) };

                int count = Convert.ToInt32(db.ExecuteScalar(checkQuery, pCheck));

                if (count > 0)
                {
                    ViewBag.Error = "Cet email est déjà utilisé.";
                    return View(client);
                }

                // Insertion dans la base
                string query = @"INSERT INTO Clients (Nom, Prenom, Email, Telephone, NumPermis, MotDePasse) 
                                 VALUES (@nom, @prenom, @email, @tel, @permis, @mdp)";

                MySqlParameter[] p = {
                    new MySqlParameter("@nom", client.Nom),
                    new MySqlParameter("@prenom", client.Prenom),
                    new MySqlParameter("@email", client.Email),
                    new MySqlParameter("@tel", client.Telephone),
                    new MySqlParameter("@permis", client.NumPermis),
                    new MySqlParameter("@mdp", client.MotDePasse)
                };

                db.ExecuteNonQuery(query, p);

                // Succès : on redirige vers la page de connexion
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur technique : " + ex.Message;
                return View(client);
            }
        }

        // ==========================================
        // PARTIE CONNEXION (LOGIN)
        // ==========================================

        // 1. Affiche la page de connexion
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 2. Traite la connexion
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            try
            {
                // On cherche le client avec cet email et ce mot de passe
                string query = "SELECT * FROM Clients WHERE Email = @email AND MotDePasse = @mdp";
                MySqlParameter[] p = {
                    new MySqlParameter("@email", email),
                    new MySqlParameter("@mdp", password)
                };

                DataTable dt = db.ExecuteQuery(query, p);

                if (dt.Rows.Count > 0)
                {
                    // TROUVÉ ! On crée la session
                    DataRow row = dt.Rows[0];

                    // On stocke l'ID et le Nom dans le navigateur du client (Session)
                    HttpContext.Session.SetString("ClientId", row["Id"].ToString());
                    HttpContext.Session.SetString("ClientNom", row["Prenom"].ToString() + " " + row["Nom"].ToString());

                    // On renvoie vers l'accueil
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Error = "Email ou mot de passe incorrect.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return View();
            }
        }

        // ==========================================
        // DÉCONNEXION
        // ==========================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // On vide la session
            return RedirectToAction("Index", "Home");
        }
    }
}