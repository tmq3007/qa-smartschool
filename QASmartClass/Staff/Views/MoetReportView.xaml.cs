using System.Windows.Controls;
using QASmartClass.Staff.ViewModels;

namespace QASmartClass.Staff.Views
{
    public partial class MoetReportView : UserControl
    {
        public MoetReportView()
        {
            InitializeComponent();
            var vm = new MoetReportViewModel();
            this.DataContext = vm;
        }
    }
}
