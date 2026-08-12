using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using SmartLibrary.Desktop.Services;
using SmartLibrary.Desktop.ViewModels;

namespace SmartLibrary.Desktop.Views.Librarian
{
    public partial class CirculationView : UserControl
    {
        private BarcodeScannerService? _scannerService;
        private CirculationViewModel? _viewModel;

        public CirculationView()
        {
            InitializeComponent();
        }

        private Window? _parentWindow;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as CirculationViewModel;
            
            // Khởi tạo Barcode Scanner Hook
            _scannerService = new BarcodeScannerService();
            _scannerService.BarcodeScanned += ScannerService_BarcodeScanned;
            
            // Lấy Window cha để hook sự kiện bàn phím toàn cục
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
            {
                _scannerService.HookScanner(_parentWindow);
                _parentWindow.Focus(); // Đảm bảo window nhận phím
            }

            this.Focusable = true;
            this.Focus();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_scannerService != null)
            {
                _scannerService.BarcodeScanned -= ScannerService_BarcodeScanned;
                if (_parentWindow != null)
                {
                    _scannerService.UnhookScanner(_parentWindow);
                    _parentWindow = null;
                }
            }
        }

        private async void ScannerService_BarcodeScanned(object? sender, string barcode)
        {
            if (_viewModel != null)
            {
                // UI Thread cập nhật
                await Dispatcher.InvokeAsync(async () =>
                {
                    // Phát tiếng bíp báo hiệu quét thành công
                    SystemSounds.Beep.Play();
                    
                    await _viewModel.HandleBarcodeScannedAsync(barcode);
                });
            }
        }

        private void UserControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_viewModel != null)
            {
                if (e.Key == System.Windows.Input.Key.F2)
                {
                    _viewModel.SelectedBookCondition = "Good";
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.F3)
                {
                    _viewModel.SelectedBookCondition = "Damaged";
                    e.Handled = true;
                }
                else if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.M)
                {
                    _viewModel.SelectedBookCondition = "Lost";
                    e.Handled = true;
                }
                else if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Alt)
                {
                    if (e.SystemKey == System.Windows.Input.Key.M)
                    {
                        if (_viewModel.SwitchModeCommand.CanExecute("Borrow"))
                        {
                            _viewModel.SwitchModeCommand.Execute("Borrow");
                            e.Handled = true;
                        }
                    }
                    else if (e.SystemKey == System.Windows.Input.Key.T)
                    {
                        if (_viewModel.SwitchModeCommand.CanExecute("Return"))
                        {
                            _viewModel.SwitchModeCommand.Execute("Return");
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        private async void TxtManualReaderId_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && _viewModel != null)
            {
                e.Handled = true;
                string input = TxtManualReaderId.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    TxtManualReaderId.Text = "";
                    await _viewModel.HandleBarcodeScannedAsync(input);
                    TxtManualBookBarcode.Focus();
                }
            }
        }

        private async void BtnManualReaderId_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                string input = TxtManualReaderId.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    TxtManualReaderId.Text = "";
                    await _viewModel.HandleBarcodeScannedAsync(input);
                    TxtManualBookBarcode.Focus();
                }
            }
        }

        private async void TxtManualBookBarcode_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && _viewModel != null)
            {
                e.Handled = true;
                string input = TxtManualBookBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    TxtManualBookBarcode.Text = "";
                    await _viewModel.HandleBarcodeScannedAsync(input);
                }
            }
        }

        private async void BtnManualBookBarcode_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                string input = TxtManualBookBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    TxtManualBookBarcode.Text = "";
                    await _viewModel.HandleBarcodeScannedAsync(input);
                }
            }
        }

        private async void TxtManualReturnBarcode_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && _viewModel != null)
            {
                e.Handled = true;
                string input = TxtManualReturnBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    TxtManualReturnBarcode.Text = "";
                    await _viewModel.HandleBarcodeScannedAsync(input);
                }
            }
        }

        private async void BtnManualReturnBarcode_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                string input = TxtManualReturnBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    TxtManualReturnBarcode.Text = "";
                    await _viewModel.HandleBarcodeScannedAsync(input);
                }
            }
        }
    }
}
