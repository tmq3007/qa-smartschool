using System.Windows;
using System.Windows.Controls;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class KioskModeView : UserControl
    {
        private BarcodeScannerService? _scannerService;
        private KioskModeViewModel? _viewModel;

        public KioskModeView()
        {
            InitializeComponent();
        }

        private Window? _parentWindow;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as KioskModeViewModel;
            
            _scannerService = new BarcodeScannerService();
            _scannerService.BarcodeScanned += ScannerService_BarcodeScanned;
            
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _scannerService.HookScanner(_parentWindow);
                _parentWindow.Focus();
            }

            this.MouseMove += KioskView_MouseMove;
            this.PreviewMouseWheel += KioskView_PreviewMouseWheel;
            this.PreviewMouseDown += KioskView_PreviewMouseDown;
        }

        private void KioskView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) => ResetKioskTimer();
        private void KioskView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) => ResetKioskTimer();
        private void KioskView_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => ResetKioskTimer();

        private void ResetKioskTimer()
        {
            if (DataContext is KioskModeViewModel vm && vm.KioskState != "IDLE")
            {
                vm.ResetIdleTimer();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.Cleanup(); // FIX MEMORY LEAK: Dừng IdleTimer

            if (_scannerService != null)
            {
                _scannerService.BarcodeScanned -= ScannerService_BarcodeScanned;
                if (_parentWindow != null)
                {
                    _scannerService.UnhookScanner(_parentWindow);
                    _parentWindow = null;
                }
            }

            this.MouseMove -= KioskView_MouseMove;
            this.PreviewMouseWheel -= KioskView_PreviewMouseWheel;
            this.PreviewMouseDown -= KioskView_PreviewMouseDown;
        }

        private void ScannerService_BarcodeScanned(object? sender, string barcode)
        {
            _viewModel?.HandleBarcodeScanned(barcode);
        }
    }
}
