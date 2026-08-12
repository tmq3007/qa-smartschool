using System.Windows.Controls;

namespace QASmartClass.Staff.Views
{
    public partial class GateMonitorView : UserControl
    {
        private void PbLockdownPin_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.GateMonitorViewModel vm && sender is PasswordBox pb)
            {
                vm.LockdownPin = pb.Password;
            }
        }

        private void PbReleasePin_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.GateMonitorViewModel vm && sender is PasswordBox pb)
            {
                vm.LockdownPin = pb.Password;
            }
        }

        public GateMonitorView()
        {
            InitializeComponent();
            Unloaded += (s, e) => (DataContext as System.IDisposable)?.Dispose();
            
            Loaded += (s, e) =>
            {
                if (DataContext is ViewModels.GateMonitorViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(vm.ScannedLeavePassCode) && string.IsNullOrEmpty(vm.ScannedLeavePassCode))
                        {
                            txtScanInput.Focus();
                        }
                        if (args.PropertyName == nameof(vm.IsLockdownActive))
                        {
                            Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                PbLockdownPin.Clear();
                                PbReleasePin.Clear();
                            }));
                        }
                    };
                }
            };
        }
    }
}
