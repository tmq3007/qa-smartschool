using System;
using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class LeaveRequestManagementView : UserControl
    {
        public LeaveRequestManagementView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is ViewModels.LeaveRequestManagementViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            };
            Unloaded += (s, e) => 
            {
                if (DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }
    }
}

