using System.Windows;
using System.Windows.Controls;

namespace QASmartTouch.Forms
{
    public partial class Form2_11_SubMenuPieChart : Window
    {
        private Form2_MainDashboard _mainDashboard;

        public Form2_11_SubMenuPieChart(Form2_MainDashboard mainDashboard)
        {
            InitializeComponent();
            _mainDashboard = mainDashboard;
        }

        private void btnPieChart_Click(object sender, RoutedEventArgs e)
        {
            // Open Pie Chart Editor with default Pie type
            if (_mainDashboard != null)
            {
                _mainDashboard.OpenPieChartEditorWithType("Pie");
                _mainDashboard.CloseActiveSubMenu();
            }
        }

        private void btnDonutChart_Click(object sender, RoutedEventArgs e)
        {
            // Open Pie Chart Editor with Donut type preset
            if (_mainDashboard != null)
            {
                _mainDashboard.OpenPieChartEditorWithType("Donut");
                _mainDashboard.CloseActiveSubMenu();
            }
        }

        private void btnSemiCircleChart_Click(object sender, RoutedEventArgs e)
        {
            // Open Pie Chart Editor with Semi-Circle type preset
            if (_mainDashboard != null)
            {
                _mainDashboard.OpenPieChartEditorWithType("Semi");
                _mainDashboard.CloseActiveSubMenu();
            }
        }
    }
}
