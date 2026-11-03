using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class CalculatorWindow : Window
    {
        private static CalculatorWindow? _sharedInstance;
        private Form2_MainDashboard? _mainDashboard;
        private bool _isInitialized = false;

        public static CalculatorWindow GetOrCreateInstance(Form2_MainDashboard? mainDashboard)
        {
            if (_sharedInstance == null || !_sharedInstance.IsLoaded)
            {
                _sharedInstance = new CalculatorWindow(mainDashboard);
            }
            else
            {
                if (mainDashboard != null)
                {
                    _sharedInstance.SetMainDashboard(mainDashboard);
                }
            }
            return _sharedInstance;
        }

        public CalculatorWindow()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            this.Loaded += CalculatorWindow_Loaded;
            this.PreviewKeyDown += CalculatorWindow_PreviewKeyDown;
        }

        public CalculatorWindow(Form2_MainDashboard? mainDashboard) : this()
        {
            _mainDashboard = mainDashboard;
        }

        public void SetMainDashboard(Form2_MainDashboard mainDashboard)
        {
            _mainDashboard = mainDashboard;
        }

        private async void CalculatorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                calcWebView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(29, 72, 80);
                await WebView2Helper.EnsureInitializedAsync(calcWebView);

                calcWebView.WebMessageReceived += CalcWebView_WebMessageReceived;

                string htmlPath = ResolveCalculatorHtmlPath();
                if (File.Exists(htmlPath))
                {
                    calcWebView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                {
                    calcWebView.NavigateToString("<html><body style='color:white;background:#163a40;font-family:sans-serif;padding:20px;'><h3>Không tìm thấy tệp QA Calculator</h3><p>" + htmlPath + "</p></body></html>");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CalculatorWindow] WebView2 initialization error: {ex.Message}");
            }
        }

        private string ResolveCalculatorHtmlPath()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string path1 = Path.Combine(basePath, "Assets", "Calculator", "qa-calculator.html");
            if (File.Exists(path1)) return path1;

            string path2 = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "QASmartClass", "Assets", "Calculator", "qa-calculator.html"));
            if (File.Exists(path2)) return path2;

            string path3 = Path.GetFullPath(Path.Combine(basePath, "..", "QASmartClass", "Assets", "Calculator", "qa-calculator.html"));
            if (File.Exists(path3)) return path3;

            return path1;
        }

        private void CalcWebView_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string rawJson = e.WebMessageAsJson;
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.String)
                {
                    using var innerDoc = JsonDocument.Parse(root.GetString() ?? "{}");
                    HandleMessagePayload(innerDoc.RootElement);
                }
                else
                {
                    HandleMessagePayload(root);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CalculatorWindow] WebMessageReceived parse error: {ex.Message}");
            }
        }

        private void HandleMessagePayload(JsonElement root)
        {
            string action = root.TryGetProperty("action", out var actProp) ? actProp.GetString() ?? "" : "";

            if (action == "insertResult")
            {
                string text = root.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? "" : "";
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_mainDashboard != null)
                        {
                            _mainDashboard.InsertCalculatorResult(text);
                        }
                    });
                }
            }
            else if (action == "close")
            {
                Dispatcher.Invoke(this.Hide);
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try { this.DragMove(); } catch { }
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void CalculatorWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Hide();
                e.Handled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Giữ lại cửa sổ và tiến trình WebView2 để lần sau mở tức thì 0ms, không load lại từ đầu
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
        }
    }
}
