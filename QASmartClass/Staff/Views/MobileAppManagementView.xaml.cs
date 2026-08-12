using System;
using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class MobileAppManagementView : UserControl
    {
        public MobileAppManagementView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is ViewModels.MobileAppViewModel vm)
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

