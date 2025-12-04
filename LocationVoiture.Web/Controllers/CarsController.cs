using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;

namespace LocationVoiture.Web.Controllers
{
    public class CarsController : Controller
    {
        private DatabaseHelper db;

        public CarsController()
        {
            db = new DatabaseHelper();
        }

        // 1. Catalogue : Liste de toutes les voitures disponibles
        public IActionResult Index()
        {
            try
            {
                // On récupère les voitures DISPONIBLES (EstDisponible = 1)
                // On fait une jointure pour avoir le nom de la catégorie
                string query = @"
                    SELECT v.Id, v.Marque, v.Modele, v.PrixParJour, v.ImageChemin, v.Carburant, c.Libelle as Categorie
                    FROM Voitures v
                    INNER JOIN Categories c ON v.CategorieId = c.Id
                    WHERE v.EstDisponible = 1";

                DataTable dt = db.ExecuteQuery(query);

                // On passe la DataTable à la Vue (Catalog)
                return View(dt);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return View(new DataTable());
            }
        }

        // 2. Détail : Page d'une seule voiture
        public IActionResult Details(int id)
        {
            try
            {
                string query = @"
                    SELECT v.*, c.Libelle as Categorie
                    FROM Voitures v
                    INNER JOIN Categories c ON v.CategorieId = c.Id
                    WHERE v.Id = @id";

                MySqlParameter[] p = { new MySqlParameter("@id", id) };
                DataTable dt = db.ExecuteQuery(query, p);

                if (dt.Rows.Count == 0) return NotFound();

                // On convertit la 1ère ligne en un objet dynamique ou DataRow pour la vue
                DataRow row = dt.Rows[0];
                return View(row);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}