using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using QASmartClass.Data;
using QASmartClass.Services;
using System.Threading.Tasks;

namespace QASmartClass.Staff.Views
{
    public partial class KitchenDashboardView : UserControl
    {
        public KitchenDashboardView()
        {
            InitializeComponent();
            Unloaded += (s, e) => 
            {
                if (DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };

            Loaded += async (s, e) =>
            {
                if (DataContext is ViewModels.KitchenDashboardViewModel vm)
                {
                    await vm.InitializeAsync();
                }
            };
        }
    }
}

