using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// Helper quản lý cấu hình và môi trường WebView2 cho toàn bộ hệ sinh thái QA SmartSchool.
    /// Khắc phục triệt để lỗi phân quyền E_ACCESSDENIED (0x80070005) khi cài đặt ứng dụng vào C:\Program Files.
    /// </summary>
    public static class WebView2Helper
    {
        private static string? _userDataFolder;
        private static CoreWebView2Environment? _sharedEnvironment;
        private static readonly object _lock = new();

        /// <summary>
        /// Đường dẫn thư mục User Data Folder an toàn trong LocalAppData (%LocalAppData%\QASmartClass\WebView2)
        /// </summary>
        public static string UserDataFolder
        {
            get
            {
                if (_userDataFolder == null)
                {
                    lock (_lock)
                    {
                        if (_userDataFolder == null)
                        {
                            try
                            {
                                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                                _userDataFolder = Path.Combine(localAppData, "QASmartClass", "WebView2");
                                if (!Directory.Exists(_userDataFolder))
                                {
                                    Directory.CreateDirectory(_userDataFolder);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("[WebView2Helper] Không thể tạo thư mục trong LocalAppData: {Error}", ex.Message);
                                _userDataFolder = Path.Combine(Path.GetTempPath(), "QASmartClass_WebView2");
                                Directory.CreateDirectory(_userDataFolder);
                            }
                        }
                    }
                }
                return _userDataFolder;
            }
        }

        /// <summary>
        /// Cấu hình biến môi trường WEBVIEW2_USER_DATA_FOLDER toàn cục ngay khi ứng dụng khởi động.
        /// Mọi thành phần WebView2 khi gọi EnsureCoreWebView2Async(null) hoặc EnsureCoreWebView2Async()
        /// đều sẽ tự động lưu cache/profile vào thư mục an toàn này mà không bị lỗi phân quyền NTFS.
        /// </summary>
        public static void InitializeEnvironment()
        {
            try
            {
                string folder = UserDataFolder;
                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", folder);
                Log.Information("[WebView2Helper] Đã cấu hình WEBVIEW2_USER_DATA_FOLDER: {Folder}", folder);
            }
            catch (Exception ex)
            {
                Log.Warning("[WebView2Helper] Lỗi khi cấu hình biến môi trường WebView2: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Khởi tạo an toàn cho một đối tượng WebView2 bất kỳ.
        /// Bằng cách truyền null, WebView2 sẽ tự động nhận diện biến môi trường WEBVIEW2_USER_DATA_FOLDER
        /// đã được thiết lập ở InitializeEnvironment(), tránh lỗi xung đột (already initialized with different env)
        /// khi có các WebView2 được gán thuộc tính Source trực tiếp trong XAML.
        /// </summary>
        public static async Task EnsureInitializedAsync(Microsoft.Web.WebView2.Wpf.WebView2 webView)
        {
            if (webView == null || webView.CoreWebView2 != null) return;
            await webView.EnsureCoreWebView2Async(null);
        }
    }
}
