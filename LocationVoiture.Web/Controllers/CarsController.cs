using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Data;
using System.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace LocationVoiture.Web.Controllers
{
    public class CarsController : Controller
    {
        private DatabaseHelper db;

        public CarsController()
        {
            db = new DatabaseHelper();
        }

        // Action INDEX avec paramètre 'brand' ajouté
        public IActionResult Index(string searchString, int? categoryId, string brand, int page = 1)
        {
            int pageSize = 6;
            int totalRecords = 0;

            try
            {
                // 1. Charger les catégories (Dropdown 1)
                DataTable dtCats = db.ExecuteQuery("SELECT Id, Libelle FROM Categories");
                ViewBag.Categories = dtCats;

                // 2. Charger les Marques uniques (Dropdown 2 - NOUVEAU)
                DataTable dtBrands = db.ExecuteQuery("SELECT DISTINCT Marque FROM Voitures ORDER BY Marque");
                List<string> brandsList = new List<string>();
                foreach (DataRow row in dtBrands.Rows)
                {
                    brandsList.Add(row["Marque"].ToString());
                }
                ViewBag.Brands = brandsList;

                // Sauvegarde des filtres actuels pour la vue
                ViewBag.CurrentCategory = categoryId;
                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentBrand = brand; // NOUVEAU
                ViewBag.CurrentPage = page;

                // 3. Construction de la requête SQL dynamique
                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                // A. Recherche Texte
                if (!string.IsNullOrEmpty(searchString))
                {
                    condition += " AND (v.Marque LIKE @search OR v.Modele LIKE @search)";
                    parameters.Add(new MySqlParameter("@search", "%" + searchString + "%"));
                }

                // B. Filtre Catégorie
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    condition += " AND v.CategorieId = @catId";
                    parameters.Add(new MySqlParameter("@catId", categoryId.Value));
                }

                // C. Filtre Marque (NOUVEAU)
                if (!string.IsNullOrEmpty(brand))
                {
                    condition += " AND v.Marque = @brand";
                    parameters.Add(new MySqlParameter("@brand", brand));
                }

                // 4. Compter le total
                string countQuery = $"SELECT COUNT(*) FROM Voitures v {condition}";
                object countRes = db.ExecuteScalar(countQuery, parameters.ToArray());
                totalRecords = Convert.ToInt32(countRes);

                // 5. Pagination
                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                ViewBag.TotalPages = totalPages;

                int offset = (page - 1) * pageSize;

                string query = $@"
                    SELECT v.Id, v.Marque, v.Modele, v.PrixParJour, v.ImageChemin, v.Carburant, v.EstDisponible, 
                           c.Libelle as Categorie
                    FROM Voitures v
                    INNER JOIN Categories c ON v.CategorieId = c.Id
                    {condition}
                    ORDER BY v.EstDisponible DESC, v.Id DESC
                    LIMIT {pageSize} OFFSET {offset}";

                DataTable dtVoitures = db.ExecuteQuery(query, parameters.ToArray());

                return View(dtVoitures);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return View(new DataTable());
            }
        }

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

                return View(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Erreur : " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}