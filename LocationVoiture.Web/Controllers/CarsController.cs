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

        public IActionResult Index(string searchString, int? categoryId, string brand, int page = 1)
        {
            int pageSize = 6;
            int totalRecords = 0;

            try
            {
                DataTable dtCats = db.ExecuteQuery("SELECT Id, Libelle FROM Categories");
                ViewBag.Categories = dtCats;

                DataTable dtBrands = db.ExecuteQuery("SELECT DISTINCT Marque FROM Voitures ORDER BY Marque");
                List<string> brandsList = new List<string>();
                foreach (DataRow row in dtBrands.Rows)
                {
                    brandsList.Add(row["Marque"].ToString());
                }
                ViewBag.Brands = brandsList;

                ViewBag.CurrentCategory = categoryId;
                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentBrand = brand;
                ViewBag.CurrentPage = page;

                string condition = "WHERE 1=1";
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                
                if (!string.IsNullOrEmpty(searchString))
                {
                    condition += " AND (v.Marque LIKE @search OR v.Modele LIKE @search)";
                    parameters.Add(new MySqlParameter("@search", "%" + searchString + "%"));
                }

                
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    condition += " AND v.CategorieId = @catId";
                    parameters.Add(new MySqlParameter("@catId", categoryId.Value));
                }

                
                if (!string.IsNullOrEmpty(brand))
                {
                    condition += " AND v.Marque = @brand";
                    parameters.Add(new MySqlParameter("@brand", brand));
                }

                string countQuery = $"SELECT COUNT(*) FROM Voitures v {condition}";
                object countRes = db.ExecuteScalar(countQuery, parameters.ToArray());
                totalRecords = Convert.ToInt32(countRes);

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