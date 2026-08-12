using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.StudentClient.Views;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_08 — HỘI ĐỒNG CHUYÊN GIA
    /// 
    /// Đánh giá các tính năng:
    ///   TC-01: Gửi lệnh TOOL_FOCUS kích hoạt hiển thị _toolFocusOverlay.
    ///   TC-02: Gửi lệnh TOOL_UNFOCUS xóa bỏ _toolFocusOverlay đồng thời tự động đóng mọi cửa sổ con.
    ///   TC-03: Đổi tiêu điểm công cụ (switch focus) tự động dọn dẹp các cửa sổ con của công cụ trước đó.
    /// </summary>
    public class LOI_VID_08_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        private static Thread? _staThread;
        private static System.Windows.Threading.Dispatcher? _dispatcher;
        private static readonly object _lock = new object();

        private static void EnsureStaThread()
        {
            lock (_lock)
            {
                if (_staThread == null)
                {
                    var readyEvent = new ManualResetEventSlim(false);
                    _staThread = new Thread(() =>
                    {
                        if (Application.Current == null)
                        {
                            var app = new QASmartTouch.App();
                            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                            var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                            if (dbProperty != null)
                            {
                                dbProperty.SetValue(app, new AppDbContext());
                            }

                            var resources = app.Resources;
                            try
                            {
                                var dict = new ResourceDictionary
                                {
                                    Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                                };
                                resources.MergedDictionaries.Add(dict);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to merge DesignTokens.xaml: {ex.Message}");
                            }

                            var white = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                            var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                            var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

                            string[] keys = new[] { "LightBrush", "BorderLightBrush", "DarkBrush", "MutedBrush", "PrimaryLightBrush", "PrimaryBrush", "PrimaryDarkBrush" };
                            foreach (var key in keys)
                            {
                                if (!resources.Contains(key))
                                {
                                    resources.Add(key, key.Contains("Dark") || key.Contains("Muted") ? gray : (key.Contains("Primary") ? blue : white));
                                }
                            }

                            if (!resources.Contains("Gray100"))
                            {
                                resources.Add("Gray100", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5")));
                            }
                            if (!resources.Contains("BrandAccent"))
                            {
                                resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E65100")));
                            }
                        }

                        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        readyEvent.Set();
                        System.Windows.Threading.Dispatcher.Run();
                    });
                    _staThread.SetApartmentState(ApartmentState.STA);
                    _staThread.Start();
                    readyEvent.Wait();
                }
            }
        }

        public LOI_VID_08_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_08_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_08_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // Initialize the database schema first to avoid SQLite connection issues.
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbFile)) File.Delete(_dbFile); } catch { }
            try { if (File.Exists(_versionFile)) File.Delete(_versionFile); } catch { }
            try { if (File.Exists(_dbFile + "-wal")) File.Delete(_dbFile + "-wal"); } catch { }
            try { if (File.Exists(_dbFile + "-shm")) File.Delete(_dbFile + "-shm"); } catch { }
        }

        private void SetupApp()
        {
            if (Application.Current != null)
            {
                if (Application.Current is QASmartTouch.App app)
                {
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                }
            }
        }

        private void RunOnSTA(Action action)
        {
            EnsureStaThread();
            Exception? threadEx = null;
            _dispatcher!.Invoke(() =>
            {
                try
                {
                    if (Application.Current != null && Application.Current.Dispatcher.Thread != Thread.CurrentThread)
                    {
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null)
                        {
                            appCreatedField.SetValue(null, false);
                        }
                        var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (currentField != null)
                        {
                            currentField.SetValue(null, null);
                        }
                    }

                    if (Application.Current == null)
                    {
                        var app = new QASmartTouch.App();
                        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                        var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                        if (dbProperty != null)
                        {
                            dbProperty.SetValue(app, new AppDbContext());
                        }

                        var resources = app.Resources;
                        try
                        {
                            var dict = new ResourceDictionary
                            {
                                Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                            };
                            resources.MergedDictionaries.Add(dict);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to merge DesignTokens.xaml: {ex.Message}");
                        }

                        var white = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                        var gray = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                        var blue = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

                        string[] keys = new[] { "LightBrush", "BorderLightBrush", "DarkBrush", "MutedBrush", "PrimaryLightBrush", "PrimaryBrush", "PrimaryDarkBrush" };
                        foreach (var key in keys)
                        {
                            if (!resources.Contains(key))
                            {
                                resources.Add(key, key.Contains("Dark") || key.Contains("Muted") ? gray : (key.Contains("Primary") ? blue : white));
                            }
                        }

                        if (!resources.Contains("Gray100"))
                        {
                            resources.Add("Gray100", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5")));
                        }
                        if (!resources.Contains("BrandAccent"))
                        {
                            resources.Add("BrandAccent", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E65100")));
                        }
                    }

                    QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
                    QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;
                    action();
                }
                catch (Exception ex) { threadEx = ex; }
            });
            Assert.Null(threadEx);
        }

        [Fact]
        public void TC01_ToolFocus_Command_ShowsOverlay()
        {
            RunOnSTA(() =>
            {
                SetupApp();
                StudentShell shell = null;
                try
                {
                    shell = new StudentShell();
                    shell.Show(); // Show window to initialize Dispatcher fully and avoid hangs
                    DoEvents();

                    var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(handleMethod);

                    // Send focus command for a math tool, e.g., prime_numbers (matching tool registry ID)
                    string focusPayload = "CMD|TOOL_FOCUS|prime_numbers";
                    handleMethod.Invoke(shell, new object[] { focusPayload });
                    DoEvents();

                    // Find overlay by reflection or inspecting shell.Content
                    var overlayField = typeof(StudentShell).GetField("_toolFocusOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(overlayField);
                    var overlay = overlayField.GetValue(shell) as Grid;
                    Assert.NotNull(overlay);
                    Assert.Equal("ToolFocusOverlay", overlay.Tag);

                    // Clean up focus before closing
                    string unfocusPayload = "CMD|TOOL_UNFOCUS";
                    handleMethod.Invoke(shell, new object[] { unfocusPayload });
                    DoEvents();
                }
                finally
                {
                    CloseShellSecurely(shell);
                }
            });
        }

        [Fact]
        public void TC02_ToolUnfocus_Command_ClosesOverlayAndAllChildWindows()
        {
            RunOnSTA(() =>
            {
                SetupApp();
                StudentShell shell = null;
                try
                {
                    shell = new StudentShell();
                    shell.Show(); // Make it part of Application.Current.Windows
                    DoEvents();

                    var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(handleMethod);

                    // Focus tool first
                    string focusPayload = "CMD|TOOL_FOCUS|prime_numbers";
                    handleMethod.Invoke(shell, new object[] { focusPayload });
                    DoEvents();

                    var overlayField = typeof(StudentShell).GetField("_toolFocusOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
                    var overlayBefore = overlayField.GetValue(shell) as Grid;
                    Assert.NotNull(overlayBefore);

                    // Create a dummy child window to represent a floating tool window
                    var childWin = new Window { Title = "DummyToolWindow" };
                    childWin.Show();
                    DoEvents();

                    // Verify the child window is in Application.Current.Windows
                    Assert.Contains(childWin, Application.Current.Windows.Cast<Window>());

                    // Send unfocus command
                    string unfocusPayload = "CMD|TOOL_UNFOCUS";
                    handleMethod.Invoke(shell, new object[] { unfocusPayload });
                    DoEvents(); // Pump events to allow window closing to process

                    // Verify overlay is removed
                    var overlayAfter = overlayField.GetValue(shell) as Grid;
                    Assert.Null(overlayAfter);

                    // Verify child window is closed
                    Assert.False(childWin.IsLoaded);
                    Assert.DoesNotContain(childWin, Application.Current.Windows.Cast<Window>());
                }
                finally
                {
                    CloseShellSecurely(shell);
                }
            });
        }

        [Fact]
        public void TC03_SwitchFocus_ClosesPreviousChildWindows()
        {
            RunOnSTA(() =>
            {
                SetupApp();
                StudentShell shell = null;
                try
                {
                    shell = new StudentShell();
                    shell.Show();
                    DoEvents();

                    var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherCommand",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(handleMethod);

                    // Focus first tool
                    string focusPayload1 = "CMD|TOOL_FOCUS|prime_numbers";
                    handleMethod.Invoke(shell, new object[] { focusPayload1 });
                    DoEvents();

                    // Open dummy child window for the first tool
                    var childWin = new Window { Title = "FirstToolWindow" };
                    childWin.Show();
                    DoEvents();
                    Assert.Contains(childWin, Application.Current.Windows.Cast<Window>());

                    // Focus second tool (fraction)
                    string focusPayload2 = "CMD|TOOL_FOCUS|fraction";
                    handleMethod.Invoke(shell, new object[] { focusPayload2 });
                    DoEvents(); // Pump events to process closing previous windows

                    // Verify child window of previous tool was closed automatically
                    Assert.False(childWin.IsLoaded);
                    Assert.DoesNotContain(childWin, Application.Current.Windows.Cast<Window>());

                    // Clean up focus before closing
                    string unfocusPayload = "CMD|TOOL_UNFOCUS";
                    handleMethod.Invoke(shell, new object[] { unfocusPayload });
                    DoEvents();
                }
                finally
                {
                    CloseShellSecurely(shell);
                }
            });
        }

        private void CloseShellSecurely(StudentShell shell)
        {
            if (shell != null)
            {
                try { shell.Close(); } catch { }
                DoEvents();
            }
            if (Application.Current != null)
            {
                try
                {
                    var windows = Application.Current.Windows.Cast<Window>().ToList();
                    foreach (var win in windows)
                    {
                        if (win != shell)
                        {
                            try { win.Close(); } catch { }
                        }
                    }
                    DoEvents();
                }
                catch { }
            }
        }

        private void DoEvents()
        {
            Thread.Sleep(50); // Small sleep to let asynchronous processes queue up on dispatcher
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new System.Windows.Threading.DispatcherOperationCallback(ExitFrame), frame);
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }

        private object? ExitFrame(object frame)
        {
            ((System.Windows.Threading.DispatcherFrame)frame).Continue = false;
            return null;
        }
    }
}
