using LocationVoiture.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocationVoiture.Admin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DatabaseHelper db = new DatabaseHelper();
        public MainWindow()
        {
            InitializeComponent();
            ChargerVoitures();
        }

        private void ChargerVoitures()
        {
            var data = db.ExecuteQuery("SELECT * FROM Voitures");
            MyDataGrid.ItemsSource = data.DefaultView;
        }
    }
}