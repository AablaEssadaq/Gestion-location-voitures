using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient; 

namespace LocationVoiture.Web.Controllers
{
    public class BookingController : Controller
    {
        private DatabaseHelper db;

        public BookingController()
        {
            db = new DatabaseHelper();
        }

        [HttpGet]
        public IActionResult Book(int carId)
        {
            if (HttpContext.Session.GetString("ClientId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                string query = "SELECT * FROM Voitures WHERE Id = @id";
                MySqlParameter[] p = { new MySqlParameter("@id", carId) };
                DataTable dt = db.ExecuteQuery(query, p);

                if (dt.Rows.Count == 0) return NotFound();

                return View(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return RedirectToAction("Index", "Cars");
            }
        }

        [HttpPost]
        public IActionResult ConfirmBooking(int carId, DateTime dateDebut, DateTime dateFin)
        {
            string clientIdStr = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientIdStr)) return RedirectToAction("Login", "Account");
            int clientId = int.Parse(clientIdStr);

            if (dateDebut < DateTime.Today || dateFin <= dateDebut)
            {
                TempData["Error"] = "Les dates sélectionnées sont invalides.";
                return RedirectToAction("Book", new { carId = carId });
            }

            try
            {
                string queryCheck = @"
                    SELECT COUNT(*) FROM Locations 
                    WHERE VoitureId = @id 
                    AND Statut = 'Confirmée' 
                    AND (DateDebut <= @fin AND DateFin >= @debut)";

                MySqlParameter[] pCheck = {
                    new MySqlParameter("@id", carId),
                    new MySqlParameter("@debut", dateDebut),
                    new MySqlParameter("@fin", dateFin)
                };

                int conflit = Convert.ToInt32(db.ExecuteScalar(queryCheck, pCheck));

                if (conflit > 0)
                {
                    TempData["Error"] = "Désolé, ce véhicule n'est plus disponible pour ces dates.";
                    return RedirectToAction("Book", new { carId = carId });
                }

                string queryVoiture = "SELECT PrixParJour FROM Voitures WHERE Id = @id";
                decimal prixParJour = Convert.ToDecimal(db.ExecuteScalar(queryVoiture, new MySqlParameter[] { new MySqlParameter("@id", carId) }));

                int jours = (dateFin - dateDebut).Days;
                if (jours == 0) jours = 1;
                decimal prixTotal = jours * prixParJour;

                string queryInsert = @"INSERT INTO Locations (DateDebut, DateFin, PrixTotal, Statut, ClientId, VoitureId, EstPaye) 
                                       VALUES (@debut, @fin, @total, 'En Attente', @client, @voiture, 0)";

                MySqlParameter[] p = {
                    new MySqlParameter("@debut", dateDebut),
                    new MySqlParameter("@fin", dateFin),
                    new MySqlParameter("@total", prixTotal),
                    new MySqlParameter("@client", clientId),
                    new MySqlParameter("@voiture", carId)
                };

                db.ExecuteNonQuery(queryInsert, p);

                return RedirectToAction("Confirmation");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Erreur technique : " + ex.Message;
                return RedirectToAction("Book", new { carId = carId });
            }
        }

        public IActionResult Confirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            try
            {
                string query = @"
                    SELECT l.Id, l.DateDebut, l.DateFin, l.PrixTotal, l.Statut, l.EstPaye,
                           v.Marque, v.Modele, v.ImageChemin, v.Matricule,
                           c.Nom, c.Prenom
                    FROM Locations l
                    JOIN Voitures v ON l.VoitureId = v.Id
                    JOIN Clients c ON l.ClientId = c.Id
                    WHERE l.Id = @id";

                MySqlParameter[] p = { new MySqlParameter("@id", id) };
                DataTable dt = db.ExecuteQuery(query, p);

                if (dt.Rows.Count == 0)
                {
                    return NotFound("Réservation introuvable.");
                }

                return View(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

    }
}