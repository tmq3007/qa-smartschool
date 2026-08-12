using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace QASmartTouch.Controls
{
    public partial class Graph3DControl : UserControl
    {
        private bool _isResizing = false;
        private Point _resizeStartPoint;
        private double _originalWidth;
        private double _originalHeight;

        public string GraphUrl { get; set; } = "https://www.Graph.com/3d";

        public event EventHandler? DeleteRequested;

        public Graph3DControl()
        {
            InitializeComponent();
            _ = InitializeWebView();
            this.Unloaded += Graph3DControl_Unloaded;
        }

        private void Graph3DControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (webView != null)
                {
                    webView.Source = null;
                    webView.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Graph3DControl WebView Cleanup Error] {ex.Message}");
            }
        }

        public Graph3DControl(string graphUrl) : this()
        {
            GraphUrl = graphUrl;
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                
                // Load the graph URL
                if (!string.IsNullOrEmpty(GraphUrl))
                {
                    webView.Source = new Uri(GraphUrl);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Graph3DControl WebView initialized: {GraphUrl}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Graph3DControl WebView init failed: {ex.Message}");
            }
        }

        // Title Bar Drag
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow parent to handle dragging
            e.Handled = false;
        }

        // Delete Button
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa đồ thị 3D này?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        // Resize Handle Events
        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizing = true;
            _resizeStartPoint = e.GetPosition(this.Parent as UIElement);
            _originalWidth = this.Width;
            _originalHeight = this.Height;
            
            resizeHandle.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                resizeHandle.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this.Parent as UIElement);
                
                double deltaX = currentPoint.X - _resizeStartPoint.X;
                double deltaY = currentPoint.Y - _resizeStartPoint.Y;

                double newWidth = Math.Max(300, _originalWidth + deltaX);
                double newHeight = Math.Max(200, _originalHeight + deltaY);

                this.Width = newWidth;
                this.Height = newHeight;

                e.Handled = true;
            }
        }

        /// <summary>
        /// Update the graph URL (for loading saved state)
        /// </summary>
        public void UpdateGraphUrl(string url)
        {
            GraphUrl = url;
            if (webView.CoreWebView2 != null)
            {
                webView.Source = new Uri(url);
            }
        }

        /// <summary>
        /// Get current graph state URL
        /// </summary>
        public async System.Threading.Tasks.Task<string> GetCurrentUrlAsync()
        {
            try
            {
                if (webView.CoreWebView2 != null)
                {
                    string script = "window.location.href";
                    string result = await webView.CoreWebView2.ExecuteScriptAsync(script);
                    return result?.Trim('"') ?? GraphUrl;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to get URL: {ex.Message}");
            }
            
            return GraphUrl;
        }
    }
}


