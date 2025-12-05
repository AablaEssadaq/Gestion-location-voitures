using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Data;
using System.Data;

namespace LocationVoiture.Web.Controllers
{
    public class CarsController : Controller
    {
        private DatabaseHelper db;

        public CarsController()
        {
            db = new DatabaseHelper();
        }

        public IActionResult Index()
        {
            try
            {
                // MODIFICATION : J'ai retiré "WHERE v.EstDisponible = 1"
                // On récupère TOUT, y compris le champ EstDisponible pour l'utiliser dans la Vue
                string query = @"
                    SELECT v.Id, v.Marque, v.Modele, v.PrixParJour, v.ImageChemin, v.Carburant, v.EstDisponible, c.Libelle as Categorie
                    FROM Voitures v
                    INNER JOIN Categories c ON v.CategorieId = c.Id";

                DataTable dt = db.ExecuteQuery(query);
                return View(dt);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return View(new DataTable());
            }
        }

        public IActionResult Details(int id)
        {
            // Même chose pour le détail, on récupère le champ EstDisponible
            string query = @"SELECT v.*, c.Libelle as Categorie 
                             FROM Voitures v 
                             INNER JOIN Categories c ON v.CategorieId = c.Id 
                             WHERE v.Id = " + id; // (Simple pour l'exemple, préférez MySqlParameter)

            DataTable dt = db.ExecuteQuery(query);
            if (dt.Rows.Count == 0) return NotFound();
            return View(dt.Rows[0]);
        }
    }
}