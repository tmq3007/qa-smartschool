using System.Windows.Controls;
using QASmartClass.Staff.ViewModels;

namespace QASmartClass.Staff.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            DataContext = new ReportsViewModel();
        }
    }
}
