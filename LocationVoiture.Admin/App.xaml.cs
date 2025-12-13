using System.Windows;

namespace LocationVoiture.Admin
{
    public partial class App : Application
    {
        // Cette variable stockera le rôle ("Admin" ou "Employe")
        public static string CurrentRole { get; set; } = "";
    }
}