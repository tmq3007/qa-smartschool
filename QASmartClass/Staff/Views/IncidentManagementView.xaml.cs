using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class IncidentManagementView : UserControl
    {
        public IncidentManagementView()
        {
            InitializeComponent();
            Loaded += async (s, e) => 
            {
                if (DataContext is ViewModels.IncidentManagementViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            };
            Unloaded += (s, e) =>
            {
                if (DataContext is System.IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }
    }
}

