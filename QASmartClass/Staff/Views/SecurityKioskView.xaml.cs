using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Threading.Tasks;

namespace QASmartClass.Staff.Views
{
    public partial class SecurityKioskView : UserControl
    {
        public SecurityKioskView()
        {
            InitializeComponent();
            Loaded += async (s, e) => 
            { 
                Dispatcher.BeginInvoke(new Action(() => TxtQrInput.Focus()), System.Windows.Threading.DispatcherPriority.Input);
                if (DataContext is ViewModels.SecurityKioskViewModel vm)
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

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is ViewModels.SecurityKioskViewModel vm)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.F1:
                        vm.SelectedEventType = "CheckIn";
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.F2:
                        vm.SelectedEventType = "CheckOut";
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.F3:
                        vm.SelectedEventType = "KeyExchange";
                        e.Handled = true;
                        break;
                    case System.Windows.Input.Key.F4:
                        vm.SelectedEventType = "Incident";
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}

