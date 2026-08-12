using System;
using System.Windows;
using System.Windows.Controls;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class TopBarControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(TopBarControl), new PropertyMetadata(""));
        public static readonly DependencyProperty ActionButtonTextProperty = DependencyProperty.Register(
            "ActionButtonText", 
            typeof(string), 
            typeof(TopBarControl), 
            new PropertyMetadata("", OnActionButtonTextChanged));
        public static readonly DependencyProperty HasActionButtonProperty = DependencyProperty.Register("HasActionButton", typeof(bool), typeof(TopBarControl), new PropertyMetadata(false));

        public event RoutedEventHandler? OnActionClick;

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public string ActionButtonText
        {
            get { return (string)GetValue(ActionButtonTextProperty); }
            set { SetValue(ActionButtonTextProperty, value); }
        }

        private static void OnActionButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TopBarControl control)
            {
                control.HasActionButton = !string.IsNullOrEmpty(e.NewValue as string);
            }
        }

        public bool HasActionButton
        {
            get { return (bool)GetValue(HasActionButtonProperty); }
            private set { SetValue(HasActionButtonProperty, value); }
        }

        public static event Action? OnBarcodeScanSuccess;
        public static event Action? OnRfidScanSuccess;

        private System.Windows.Threading.DispatcherTimer? _heartbeatTimer;
        private bool _oldCamOk = true;
        private bool _oldBarOk = true;
        private bool _oldRfidOk = true;
        private bool _oldPrnOk = true;

        public static void TriggerBarcodeFlash() => OnBarcodeScanSuccess?.Invoke();
        public static void TriggerRfidFlash() => OnRfidScanSuccess?.Invoke();

        public TopBarControl()
        {
            InitializeComponent();
            BtnToggleTheme.Content = SmartLibrary.Desktop.Services.ThemeService.IsDarkTheme ? "☀️" : "🌙";
            OnBarcodeScanSuccess += FlashBarcodeLed;
            OnRfidScanSuccess += FlashRfidLed;
            Unloaded += TopBarControl_Unloaded;

            _heartbeatTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _heartbeatTimer.Tick += HeartbeatTimer_Tick;
            _heartbeatTimer.Start();
            UpdateHardwareLeds();
        }

        private void BtnToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            SmartLibrary.Desktop.Services.ThemeService.ToggleTheme();
            BtnToggleTheme.Content = SmartLibrary.Desktop.Services.ThemeService.IsDarkTheme ? "☀️" : "🌙";
        }

        private void TopBarControl_Unloaded(object sender, RoutedEventArgs e)
        {
            OnBarcodeScanSuccess -= FlashBarcodeLed;
            OnRfidScanSuccess -= FlashRfidLed;
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Stop();
                _heartbeatTimer.Tick -= HeartbeatTimer_Tick;
            }
        }

        private void HeartbeatTimer_Tick(object? sender, EventArgs e)
        {
            UpdateHardwareLeds();
        }

        private void UpdateHardwareLeds()
        {
            try
            {
                var baseDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary");
                if (!System.IO.Directory.Exists(baseDir))
                {
                    System.IO.Directory.CreateDirectory(baseDir);
                }

                bool camError = System.IO.File.Exists(System.IO.Path.Combine(baseDir, "simulate_camera_error.txt"));
                bool camOk = !camError;
                UpdateLedState(LedCamera, camOk, "Camera QR: Kết nối tốt (Giả lập)", "Camera QR: Lỗi kết nối! Vui lòng kiểm tra lại thiết bị.");
                if (_oldCamOk && !camOk) SmartLibrary.Desktop.Services.ToastService.ShowWarning("⚠️ Mất kết nối Camera QR! Vui lòng kiểm tra lại thiết bị.");
                _oldCamOk = camOk;

                bool barError = System.IO.File.Exists(System.IO.Path.Combine(baseDir, "simulate_barcode_error.txt"));
                bool barOk = !barError;
                UpdateLedState(LedBarcode, barOk, "Súng quét mã vạch: Kết nối tốt (Giả lập)", "Súng quét mã vạch: Lỗi kết nối! Vui lòng kiểm tra lại thiết bị.");
                if (_oldBarOk && !barOk) SmartLibrary.Desktop.Services.ToastService.ShowWarning("⚠️ Mất kết nối Súng quét mã vạch! Vui lòng kiểm tra lại thiết bị.");
                _oldBarOk = barOk;

                bool rfidError = System.IO.File.Exists(System.IO.Path.Combine(baseDir, "simulate_rfid_error.txt")) || SmartLibrary.Desktop.Services.RfidReaderService.HasHardwareError;
                bool rfidOk = !rfidError;
                UpdateLedState(LedRfid, rfidOk, "Đầu đọc RFID: Kết nối tốt", "Đầu đọc RFID: Lỗi kết nối! Vui lòng kiểm tra lại thiết bị.");
                if (_oldRfidOk && !rfidOk) SmartLibrary.Desktop.Services.ToastService.ShowWarning("⚠️ Mất kết nối Đầu đọc RFID! Vui lòng kiểm tra lại thiết bị.");
                _oldRfidOk = rfidOk;

                bool prnError = System.IO.File.Exists(System.IO.Path.Combine(baseDir, "simulate_printer_error.txt"));
                bool prnOk = !prnError;
                UpdateLedState(LedPrinter, prnOk, "Máy in hóa đơn Zebra: Kết nối tốt (Giả lập)", "Máy in hóa đơn Zebra: Lỗi kết nối! Vui lòng kiểm tra lại thiết bị.");
                if (_oldPrnOk && !prnOk) SmartLibrary.Desktop.Services.ToastService.ShowWarning("⚠️ Mất kết nối Máy in hóa đơn Zebra! Vui lòng kiểm tra lại thiết bị.");
                _oldPrnOk = prnOk;
            }
            catch {}
        }

        private void UpdateLedState(System.Windows.Shapes.Ellipse led, bool isOk, string okTooltip, string errorTooltip)
        {
            if (led == null) return;
            if (isOk)
            {
                led.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
                led.ToolTip = okTooltip;
            }
            else
            {
                led.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                led.ToolTip = errorTooltip;
            }
        }

        private void FlashBarcodeLed()
        {
            FlashLed(LedBarcode);
        }

        private void FlashRfidLed()
        {
            FlashLed(LedRfid);
        }

        private void FlashLed(System.Windows.Shapes.Ellipse led)
        {
            if (led == null) return;
            // Chuyển sang xanh sáng nhạt (#34D399) và chớp tắt nhẹ
            led.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 211, 153));
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                // Check if currently error or ok after flash finishes
                var baseDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary");
                bool hasErr = false;
                if (led == LedBarcode) hasErr = System.IO.File.Exists(System.IO.Path.Combine(baseDir, "simulate_barcode_error.txt"));
                else if (led == LedRfid) hasErr = System.IO.File.Exists(System.IO.Path.Combine(baseDir, "simulate_rfid_error.txt"));

                if (hasErr)
                {
                    led.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                }
                else
                {
                    led.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
                }
            };
            timer.Start();
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            OnActionClick?.Invoke(this, e);
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new XpRulesDialog
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }
    }
}
