// CarsController.cs
using Microsoft.AspNetCore.Mvc;
using LocationVoiture.Data;
using System.Data;

public class CarsController : Controller
{
    DatabaseHelper db = new DatabaseHelper();

    public IActionResult Index()
    {
        DataTable table = db.ExecuteQuery("SELECT * FROM Voitures WHERE EstDisponible = 1");
        // Idéalement, convertir DataTable en List<VoitureModel> ici
        return View(table);
    }
}