using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Serilog;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Global Exception Handler — Bắt mọi exception chưa xử lý,
    /// log ra file, hiển thị thông báo thân thiện cho người dùng.
    /// Ngăn app crash đột ngột.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static bool _isInitialized;

        /// <summary>
        /// Khởi tạo global exception handlers. Gọi 1 lần duy nhất trong App.OnStartup.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // WPF UI thread exceptions
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Task exceptions
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            Log.Information("[ExceptionHandler] Global exception handlers initialized");
        }

        /// <summary>
        /// Exception trên UI thread (WPF Dispatcher)
        /// </summary>
        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                LogException("UI Thread", e.Exception);
                ShowErrorDialog(e.Exception);
                e.Handled = true; // Ngăn app crash
            }
            catch
            {
                // Last resort — don't let the handler itself crash
            }
        }

        /// <summary>
        /// Exception trên các thread khác (non-UI)
        /// </summary>
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                LogException("Background Thread", ex);

                if (e.IsTerminating)
                {
                    Log.Fatal("APPLICATION TERMINATING due to unhandled exception");
                    WriteCrashReport(ex);
                }
            }
            catch
            {
                // Last resort
            }
        }

        /// <summary>
        /// Exception trong async Task không được await
        /// </summary>
        private static void OnUnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                LogException("Async Task", e.Exception?.InnerException ?? e.Exception);
                e.SetObserved(); // Ngăn app crash
            }
            catch
            {
                // Last resort
            }
        }

        /// <summary>
        /// Ghi log exception chi tiết
        /// </summary>
        private static void LogException(string source, Exception? ex)
        {
            if (ex == null) return;

            Log.Error(ex, "[CRASH] Unhandled exception on {Source}: {Message}",
                source, ex.Message);
            Log.Error("StackTrace: {StackTrace}", ex.StackTrace);

            if (ex.InnerException != null)
            {
                Log.Error("InnerException: {Inner}", ex.InnerException.Message);
            }
        }

        /// <summary>
        /// Hiển thị dialog lỗi thân thiện cho người dùng
        /// </summary>
        private static void ShowErrorDialog(Exception? ex)
        {
            try
            {
                string userMessage =
                    "Đã xảy ra lỗi không mong muốn.\n\n" +
                    "Ứng dụng sẽ cố gắng tiếp tục hoạt động.\n" +
                    "Nếu lỗi tiếp tục, vui lòng khởi động lại ứng dụng.\n\n" +
                    $"Chi tiết: {ex?.Message ?? "Không rõ"}\n\n" +
                    "Bạn có muốn sao chép thông tin lỗi?";

                var result = MessageBox.Show(
                    userMessage,
                    "QA SmartTouch — Lỗi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes && ex != null)
                {
                    string clipboardText =
                        $"QA SmartTouch Error Report\n" +
                        $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                        $"Error: {ex.Message}\n" +
                        $"Type: {ex.GetType().FullName}\n" +
                        $"Stack: {ex.StackTrace}";

                    Clipboard.SetText(clipboardText);
                    MessageBox.Show("Đã sao chép thông tin lỗi vào clipboard.",
                        "Đã sao chép", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                // If dialog itself fails, just swallow
            }
        }

        /// <summary>
        /// Ghi crash report ra file (cho trường hợp app sắp terminate)
        /// </summary>
        private static void WriteCrashReport(Exception? ex)
        {
            try
            {
                var crashDir = QASmartClass.Services.AppPaths.CrashesDir;
                Directory.CreateDirectory(crashDir);

                var crashFile = Path.Combine(crashDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var report =
                    $"QA SmartTouch Crash Report\n" +
                    $"=========================\n" +
                    $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                    $"OS: {Environment.OSVersion}\n" +
                    $".NET: {Environment.Version}\n" +
                    $"64-bit: {Environment.Is64BitProcess}\n\n" +
                    $"Exception: {ex?.GetType().FullName}\n" +
                    $"Message: {ex?.Message}\n\n" +
                    $"Stack Trace:\n{ex?.StackTrace}\n\n" +
                    $"Inner Exception:\n{ex?.InnerException?.Message}\n{ex?.InnerException?.StackTrace}";

                File.WriteAllText(crashFile, report);
                Log.Information("Crash report saved to: {Path}", crashFile);
            }
            catch
            {
                // Cannot save crash report, nothing more we can do
            }
        }
    }
}
