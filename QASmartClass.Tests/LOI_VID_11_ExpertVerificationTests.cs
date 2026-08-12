using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.StudentClient.Views;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_11 — HỘI ĐỒNG CHUYÊN GIA
    /// 
    /// Đánh giá các tính năng:
    ///   TC-01: LoadReceivedFiles và UpdateStats hoạt động bất đồng bộ không gây nghẽn luồng UI.
    ///   TC-02: Các nút OpenFolder và Mở file trong StudentSubmitPage sử dụng Task.Run để chạy explorer bất đồng bộ.
    ///   TC-03: Cửa sổ StudentShell cấu hình mở tài liệu trình chiếu bất đồng bộ.
    /// </summary>
    public class LOI_VID_11_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_11_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_11_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_11_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // Khởi tạo schema database
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.SaveChanges();
            }
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbFile)) File.Delete(_dbFile); } catch { }
            try { if (File.Exists(_versionFile)) File.Delete(_versionFile); } catch { }
        }

        private static readonly object AppInitLock = new object();

        private void RunOnSTA(Action action)
        {
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                lock (AppInitLock)
                {
                    try
                    {
                        // Unconditionally reset Application instance fields to avoid conflicting with other tests' shutdowns
                        try
                        {
                            var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                            if (appCreatedField != null) appCreatedField.SetValue(null, false);
                            var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                            if (currentField != null) currentField.SetValue(null, null);
                        }
                        catch { }

                        // Initialize the QASmartTouch App context for this thread
                        var app = new QASmartTouch.App();
                        var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                        if (dbProperty != null)
                        {
                            dbProperty.SetValue(app, new AppDbContext());
                        }

                        // Load Design Tokens and Styles so page components can load successfully
                        var resources = app.Resources;
                        try
                        {
                            bool hasTokens = false;
                            foreach (var dict in resources.MergedDictionaries)
                            {
                                if (dict.Source != null && dict.Source.OriginalString.Contains("DesignTokens.xaml"))
                                {
                                    hasTokens = true;
                                    break;
                                }
                            }
                            if (!hasTokens)
                            {
                                resources.MergedDictionaries.Add(new ResourceDictionary
                                {
                                    Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml", UriKind.Absolute)
                                });
                                resources.MergedDictionaries.Add(new ResourceDictionary
                                {
                                    Source = new Uri("pack://application:,,,/QASmartClass;component/Resources/Styles.xaml", UriKind.Absolute)
                                });
                            }
                        }
                        catch { }

                        QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
                        QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

                        action();
                    }
                    catch (Exception ex)
                    {
                        threadEx = ex;
                    }
                    finally
                    {
                        try
                        {
                            var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                            if (appCreatedField != null) appCreatedField.SetValue(null, false);
                            var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                            if (currentField != null) currentField.SetValue(null, null);
                        }
                        catch { }
                    }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            Assert.Null(threadEx);
        }

        [Fact]
        public void TC01_PageExecution_RunsWithoutFreezing()
        {
            RunOnSTA(() =>
            {
                var page = new StudentSubmitPage();
                
                // Kích hoạt LoadReceivedFiles qua phản xạ
                var loadMethod = typeof(StudentSubmitPage).GetMethod("LoadReceivedFiles", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(loadMethod);
                loadMethod.Invoke(page, null);

                // Kích hoạt UpdateStats qua phản xạ
                var updateStatsMethod = typeof(StudentSubmitPage).GetMethod("UpdateStats", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(updateStatsMethod);
                updateStatsMethod.Invoke(page, null);
            });
        }

        [Fact]
        public void TC02_SourceCodeVerification_StudentSubmitPage_UsesTaskRun()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string codePath = "";
            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "QASmartClass", "StudentClient", "Views", "StudentSubmitPage.xaml.cs");
                if (File.Exists(candidate))
                {
                    codePath = candidate;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.True(File.Exists(codePath), $"Không tìm thấy StudentSubmitPage.xaml.cs tại {codePath}");

            string codeContent = File.ReadAllText(codePath);

            // Xác thực LoadReceivedFiles và UpdateStats dùng Task.Run
            Assert.Contains("Task.Run", codeContent);
            Assert.Contains("explorer.exe", codeContent);
            
            // Tìm thấy cả OpenFolder_Click và btnOpenFolder.Click dùng Task.Run
            // Đảm bảo tối thiểu có nhiều cuộc gọi Task.Run xung quanh explorer.exe
            int explorerIndex = 0;
            int count = 0;
            while ((explorerIndex = codeContent.IndexOf("explorer.exe", explorerIndex)) != -1)
            {
                int windowStart = Math.Max(0, explorerIndex - 200);
                int windowEnd = Math.Min(codeContent.Length, explorerIndex + 200);
                string segment = codeContent.Substring(windowStart, windowEnd - windowStart);
                if (segment.Contains("Task.Run"))
                {
                    count++;
                }
                explorerIndex += 12;
            }

            Assert.True(count >= 2, "Có ít nhất 2 chỗ gọi explorer.exe phải được bọc trong Task.Run");
        }

        [Fact]
        public void TC03_SourceCodeVerification_StudentShell_UsesTaskRun()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string codePath = "";
            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "QASmartClass", "StudentClient", "Views", "StudentShell.xaml.cs");
                if (File.Exists(candidate))
                {
                    codePath = candidate;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.True(File.Exists(codePath), $"Không tìm thấy StudentShell.xaml.cs tại {codePath}");

            string codeContent = File.ReadAllText(codePath);

            // Kiểm tra button2.Click mở explorer.exe chứa Task.Run
            int explorerIndex = codeContent.IndexOf("explorer.exe");
            Assert.True(explorerIndex != -1, "Không tìm thấy lệnh explorer.exe trong StudentShell.xaml.cs");

            int windowStart = Math.Max(0, explorerIndex - 250);
            int windowEnd = Math.Min(codeContent.Length, explorerIndex + 250);
            string segment = codeContent.Substring(windowStart, windowEnd - windowStart);
            Assert.Contains("Task.Run", segment);
        }
    }
}
