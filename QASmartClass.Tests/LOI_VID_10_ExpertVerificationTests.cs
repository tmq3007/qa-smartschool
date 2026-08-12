using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Classroom.Services;
using QASmartClass.Classroom.Views;
using QASmartClass.Data;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_10 — HỘI ĐỒNG CHUYÊN GIA
    /// 
    /// Đánh giá các tính năng:
    ///   TC-01: Nhận tín hiệu giơ tay (raised=True) cập nhật trạng thái IsHandRaised thành true.
    ///   TC-02: Nhận tín hiệu hạ tay (raised=False) cập nhật trạng thái IsHandRaised thành false.
    ///   TC-03: Xác thực ClassroomPage.xaml định nghĩa Storyboard nhấp nháy liên tục cho biểu tượng giơ tay.
    /// </summary>
    public class LOI_VID_10_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_10_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_10_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_10_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // Khởi tạo schema database
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.SaveChanges();
            }

            // Thiết lập Application Context và mock Database
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

        [Fact]
        public void TC01_ReceiveHandRaise_UpdatesIsHandRaisedState()
        {
            RunOnSTA(() =>
            {
                var app = (QASmartTouch.App)Application.Current;
                var session = app.ClassroomSession;
                
                // Chuẩn bị danh sách học sinh
                session.ConnectedStudents.Clear();
                var student = new QASmartClass.Classroom.Services.ConnectedStudent
                {
                    StudentCode = "HS-001",
                    Name = "Nguyễn Văn A",
                    IsOnline = true,
                    IsHandRaised = false
                };
                session.ConnectedStudents.Add(student);

                // Khởi tạo ClassroomShell
                var shell = new ClassroomShell();

                var net = app.NetworkService;
                
                // Sử dụng phản xạ để đăng ký sự kiện thủ công thay vì StartNetworkAsync (tránh chiếm port mạng)
                try
                {
                    // Gọi StartNetworkAsync để đăng ký MessageReceived nhưng tắt cổng mạng ngay nếu nó khởi chạy
                    var startNetworkMethod = typeof(ClassroomShell).GetMethod("StartNetworkAsync", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(startNetworkMethod);
                    var task = startNetworkMethod.Invoke(shell, null) as Task;
                    Assert.NotNull(task);
                    task.Wait();
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi socket bind nếu có xung đột, vì event MessageReceived vẫn được đăng ký trước hoặc trong quá trình
                }
                finally
                {
                    // Stop network để giải phóng cổng
                    net.Stop();
                }

                // Lấy sự kiện MessageReceived bằng phản xạ và kích hoạt
                var eventField = typeof(NetworkDiscoveryService).GetField("MessageReceived", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(eventField);
                
                // Phát tín hiệu giơ tay
                var args = new StudentMessageEventArgs
                {
                    StudentCode = "HS-001",
                    Message = "HAND_RAISE|raised=True|reason=Em muon phat bieu"
                };

                // Kích hoạt sự kiện
                var multicastDelegate = eventField.GetValue(net) as MulticastDelegate;
                Assert.NotNull(multicastDelegate);
                foreach (var handler in multicastDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { net, args });
                }

                // Xác thực trạng thái của học sinh đã đổi thành True
                Assert.True(student.IsHandRaised);
            });
        }

        [Fact]
        public void TC02_ReceiveHandLower_UpdatesIsHandRaisedState()
        {
            RunOnSTA(() =>
            {
                var app = (QASmartTouch.App)Application.Current;
                var session = app.ClassroomSession;
                
                // Chuẩn bị danh sách học sinh với IsHandRaised = true
                session.ConnectedStudents.Clear();
                var student = new QASmartClass.Classroom.Services.ConnectedStudent
                {
                    StudentCode = "HS-002",
                    Name = "Trần Thị B",
                    IsOnline = true,
                    IsHandRaised = true
                };
                session.ConnectedStudents.Add(student);

                // Khởi tạo ClassroomShell
                var shell = new ClassroomShell();

                var net = app.NetworkService;
                
                try
                {
                    var startNetworkMethod = typeof(ClassroomShell).GetMethod("StartNetworkAsync", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(startNetworkMethod);
                    var task = startNetworkMethod.Invoke(shell, null) as Task;
                    Assert.NotNull(task);
                    task.Wait();
                }
                catch (Exception)
                {
                }
                finally
                {
                    net.Stop();
                }

                var eventField = typeof(NetworkDiscoveryService).GetField("MessageReceived", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(eventField);

                // Phát tín hiệu hạ tay
                var args = new StudentMessageEventArgs
                {
                    StudentCode = "HS-002",
                    Message = "HAND_RAISE|raised=False"
                };

                // Kích hoạt sự kiện
                var multicastDelegate = eventField.GetValue(net) as MulticastDelegate;
                Assert.NotNull(multicastDelegate);
                foreach (var handler in multicastDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { net, args });
                }

                // Xác thực trạng thái của học sinh đã đổi thành False
                Assert.False(student.IsHandRaised);
            });
        }

        [Fact]
        public void TC03_HandRaiseIcon_StoryboardDefined()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string xamlPath = "";
            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "QASmartClass", "Classroom", "Views", "ClassroomPage.xaml");
                if (File.Exists(candidate))
                {
                    xamlPath = candidate;
                    break;
                }
                dir = dir.Parent;
            }

            Assert.True(File.Exists(xamlPath), $"Không tìm thấy ClassroomPage.xaml tại {xamlPath}");

            string xamlContent = File.ReadAllText(xamlPath);

            Assert.Contains("IsHandRaised", xamlContent);
            Assert.Contains("handRaiseIndicator", xamlContent);
            Assert.Contains("Storyboard", xamlContent);
            Assert.Contains("DoubleAnimation", xamlContent);
            Assert.Contains("Opacity", xamlContent);
            Assert.Contains("RepeatBehavior=\"Forever\"", xamlContent);
        }
    }
}
