using System;
using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class StaffOverviewView : UserControl
    {
        public StaffOverviewView()
        {
            InitializeComponent();
            if (txtDate != null)
                txtDate.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy");

            Loaded += async (s, e) =>
            {
                if (DataContext is ViewModels.StaffOverviewViewModel vm)
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

        private void QuickIncident_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dashboard = System.Windows.Window.GetWindow(this) as StaffDashboardWindow;
            dashboard?.NavigateToFeature("Incidents");
        }

        private void QuickGate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dashboard = System.Windows.Window.GetWindow(this) as StaffDashboardWindow;
            dashboard?.NavigateToFeature("Gate");
        }

        private void QuickTask_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dashboard = System.Windows.Window.GetWindow(this) as StaffDashboardWindow;
            dashboard?.NavigateToFeature("task_management");
        }

        private void QuickExport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dashboard = System.Windows.Window.GetWindow(this) as StaffDashboardWindow;
            dashboard?.NavigateToFeature("data_exporter");
        }
    }
}

