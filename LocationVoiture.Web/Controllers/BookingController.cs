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

        // ÉTAPE 1 : Afficher la page de confirmation avec choix des dates
        [HttpGet]
        public IActionResult Book(int carId)
        {
            // Sécurité : On vérifie si le client est connecté
            if (HttpContext.Session.GetString("ClientId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // On récupère les infos de la voiture pour les afficher
                string query = "SELECT * FROM Voitures WHERE Id = @id";
                MySqlParameter[] p = { new MySqlParameter("@id", carId) };
                DataTable dt = db.ExecuteQuery(query, p);

                if (dt.Rows.Count == 0) return NotFound();

                return View(dt.Rows[0]); // On envoie les infos de la voiture à la vue
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return RedirectToAction("Index", "Cars");
            }
        }

        // ÉTAPE 2 : Traiter la réservation (Clic sur "Confirmer")
        [HttpPost]
        public IActionResult ConfirmBooking(int carId, DateTime dateDebut, DateTime dateFin)
        {
            // --- CORRECTION SÉCURITÉ SESSION ---
            string clientIdStr = HttpContext.Session.GetString("ClientId");
            if (string.IsNullOrEmpty(clientIdStr))
            {
                // Si la session est perdue, on renvoie vers le login au lieu de crasher
                return RedirectToAction("Login", "Account");
            }
            int clientId = int.Parse(clientIdStr);
            // ------------------------------------

            if (dateDebut < DateTime.Today || dateFin <= dateDebut)
            {
                TempData["Error"] = "Dates invalides.";
                return RedirectToAction("Book", new { carId = carId });
            }

            try
            {
                // === NOUVEAU : VÉRIFICATION DE DISPONIBILITÉ ===
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

                // Convert.ToInt32 gère le retour de COUNT(*) qui est un Int64 (long)
                int conflit = Convert.ToInt32(db.ExecuteScalar(queryCheck, pCheck));

                if (conflit > 0)
                {
                    TempData["Error"] = "Désolé, ce véhicule n'est plus disponible pour ces dates.";
                    return RedirectToAction("Book", new { carId = carId });
                }
                // ===============================================

                // 1. Récupérer le prix de la voiture
                string queryVoiture = "SELECT PrixParJour FROM Voitures WHERE Id = @id";
                decimal prixParJour = Convert.ToDecimal(db.ExecuteScalar(queryVoiture, new MySqlParameter[] { new MySqlParameter("@id", carId) }));

                // 2. Calculer le prix total
                int jours = (dateFin - dateDebut).Days;
                if (jours == 0) jours = 1; // Minimum 1 jour
                decimal prixTotal = jours * prixParJour;

                // 3. Insérer la location
                string queryInsert = @"INSERT INTO Locations (DateDebut, DateFin, PrixTotal, Statut, ClientId, VoitureId) 
                                       VALUES (@debut, @fin, @total, 'En Attente', @client, @voiture)";

                MySqlParameter[] p = {
                    new MySqlParameter("@debut", dateDebut),
                    new MySqlParameter("@fin", dateFin),
                    new MySqlParameter("@total", prixTotal),
                    new MySqlParameter("@client", clientId),
                    new MySqlParameter("@voiture", carId)
                };

                db.ExecuteNonQuery(queryInsert, p);

                // 4. Rediriger vers la page de succès
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
    }
}