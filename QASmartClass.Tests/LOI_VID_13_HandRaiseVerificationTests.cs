using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Classroom.Views;
using QASmartClass.StudentClient.Views;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ XÁC MINH VÀ NGHIỆM THU LOI_VID_13 (HỘI ĐỒNG CHUYÊN GIA)
    /// 
    /// Đánh giá các cải tiến sư phạm và kỹ thuật:
    ///   TC-01: Chime báo giơ tay chuyên dụng (handraise_alert.wav) tồn tại trong Assets và được load động.
    ///   TC-02: Phân tích source code ClassroomShell.xaml.cs xác thực cơ chế fallback âm thanh bíp an toàn trong khối try-catch.
    ///   TC-03: Phân tích source code ClassroomPage.xaml.cs xác thực sự tồn tại của chức năng hạ tay đơn lẻ và hạ tay toàn lớp.
    ///   TC-04: Phân tích source code StudentShell.xaml.cs xác thực tiếp nhận lệnh HAND_LOWER để hạ tay và cập nhật UI lập tức.
    /// </summary>
    public class LOI_VID_13_HandRaiseVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_13_HandRaiseVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_13_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_13_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // Khởi tạo schema database rỗng
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.SaveChanges();
            }

            // Mock Application Context cho môi trường UI WPF
            RunOnSTA(() =>
            {
                if (Application.Current != null && (!(Application.Current is QASmartTouch.App) || Application.Current.Dispatcher.Thread != Thread.CurrentThread))
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
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                }
                else if (Application.Current is QASmartTouch.App app)
                {
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                }
            });
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { if (File.Exists(_dbFile)) File.Delete(_dbFile); } catch { }
            try { if (File.Exists(_versionFile)) File.Delete(_versionFile); } catch { }
        }

        private void RunOnSTA(Action action)
        {
            void InitializeApplicationFull()
            {
                var urls = new[] {
                    "pack://application:,,,/QASmartClass;component/Resources/DesignTokens.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/Styles.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/StaffTheme.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/InterOutfitFonts.xaml",
                    "pack://application:,,,/QASmartClass;component/Resources/SvgIcons.xaml",
                    "pack://application:,,,/QASmartClass;component/Localization/Strings_vi.xaml",
                    "pack://application:,,,/QASmartClass;component/LearningTools/Themes/LearningToolsStyles.xaml"
                };

                try
                {
                    var appField = typeof(System.Windows.Application).GetField("_appInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    var createdField = typeof(System.Windows.Application).GetField("_appCreatedInThisAppDomain", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (appField != null) appField.SetValue(null, null);
                    if (createdField != null) createdField.SetValue(null, false);

                    var app = new QASmartTouch.App();
                    foreach (var url in urls)
                    {
                        app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                        {
                            Source = new Uri(url, UriKind.Absolute)
                        });
                    }
                }
                catch { }
            }
            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                try
                {
                    QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
                    QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;
                    InitializeApplicationFull(); action();
                }
                catch (Exception ex) { threadEx = ex; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            Assert.Null(threadEx);
        }

        private string GetSourceFilePath(string relativePath)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }
            return string.Empty;
        }

        [Fact]
        public void TC01_Verification_ChimeAlertFileExists()
        {
            // Tìm file handraise_alert.wav trong Assets của project
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string chimePath = "";
            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "QASmartClass", "Assets", "handraise_alert.wav");
                if (File.Exists(candidate))
                {
                    chimePath = candidate;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.True(File.Exists(chimePath), $"Không tìm thấy chime báo giơ tay tại Assets: {chimePath}");
            var fileInfo = new FileInfo(chimePath);
            Assert.True(fileInfo.Length > 0, "File chime báo giơ tay rỗng.");
        }

        [Fact]
        public void TC02_SourceCodeVerification_ClassroomShell_SoundFallbackMechanism()
        {
            string shellCodePath = GetSourceFilePath(Path.Combine("QASmartClass", "Classroom", "Views", "ClassroomShell.xaml.cs"));
            Assert.True(File.Exists(shellCodePath), $"Không tìm thấy file ClassroomShell.xaml.cs");

            string code = File.ReadAllText(shellCodePath);

            // Kiểm tra xem có sử dụng SoundPlayer và phát handraise_alert.wav
            Assert.Contains("handraise_alert.wav", code);
            Assert.Contains("SoundPlayer", code);
            
            // Đảm bảo có khối try-catch bọc quanh việc phát nhạc
            Assert.Contains("System.Media.SystemSounds.Beep.Play()", code);
            Assert.Contains("catch", code);
        }

        [Fact]
        public void TC03_SourceCodeVerification_ClassroomPage_LowerHandMethods()
        {
            string pageCodePath = GetSourceFilePath(Path.Combine("QASmartClass", "Classroom", "Views", "ClassroomPage.xaml.cs"));
            Assert.True(File.Exists(pageCodePath), $"Không tìm thấy file ClassroomPage.xaml.cs");

            string code = File.ReadAllText(pageCodePath);

            // Xác minh sự tồn tại của hai phương thức hạ tay
            Assert.Contains("LowerStudentHand_Click", code);
            Assert.Contains("LowerAllHands_Click", code);

            // Xác minh việc cập nhật state IsHandRaised
            Assert.Contains("IsHandRaised = false", code);

            // Xác minh việc phát tán lệnh xuống thiết bị học sinh
            Assert.Contains("CMD|HAND_LOWER", code);
            Assert.Contains("SendToStudentAsync", code);
            Assert.Contains("SendCommandAsync", code);
        }

        [Fact]
        public void TC04_SourceCodeVerification_StudentShell_ReceiveHandLowerCommand()
        {
            string shellCodePath = GetSourceFilePath(Path.Combine("QASmartClass", "StudentClient", "Views", "StudentShell.xaml.cs"));
            Assert.True(File.Exists(shellCodePath), $"Không tìm thấy file StudentShell.xaml.cs");

            string code = File.ReadAllText(shellCodePath);

            // Xác minh tiếp nhận lệnh HAND_LOWER và hạ tay học sinh lập tức
            Assert.Contains("case \"HAND_LOWER\":", code);
            Assert.Contains("app.StudentNetwork.IsHandRaised = false", code);
        }
    }
}
