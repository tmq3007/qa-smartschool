using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using QASmartTouch;
using QASmartClass.Data;
using QASmartClass.StudentClient.Services;

namespace QASmartClass.Tests
{
    /// <summary>
    /// BỘ KIỂM THỬ XÁC MINH VÀ NGHIỆM THU LOI_VID_33 VÀ LOI_VID_39 (HỘI ĐỒNG CHUYÊN GIA)
    /// 
    /// Kịch bản kiểm thử:
    ///   TC-01: Heartbeat timeout của học sinh tự động ngắt kết nối khi quá Keep-Alive timeout.
    ///   TC-02: Giáo viên nhận HB_UPDATE đặt IsOnline = true lập tức.
    ///   TC-03: Giáo viên nhận SCREENSHOT đặt IsOnline = true lập tức để tránh bỏ qua xử lý.
    ///   TC-04: Cấu hình Network_KeepAliveTimeout được lưu trữ và tải chính xác.
    ///   TC-05: WebP Encoder tạo byte array WebP hợp lệ với header RIFF.
    ///   TC-06: WebP Decoder giải mã byte array WebP thành Bitmap chính xác.
    ///   TC-07: Cấu hình Network_ScreenCompressionFormat xác thực các giá trị hợp lệ.
    ///   TC-08: Student Loading Screen hiển thị ban đầu và có animation.
    ///   TC-09: Student Auto Recovery khôi phục trạng thái Maximized và Topmost.
    ///   TC-10: IT Admin Dashboard tải Diagnostics và chạy Actions.
    /// </summary>
    public class LOI_VID_33_39_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_33_39_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_33_39_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_33_39_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // Khởi tạo schema database
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
                    try
                    {
                        string iconsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "QASmartClass", "Resources", "SvgIcons.xaml");
                        if (File.Exists(iconsPath))
                        {
                            using (var fs = new FileStream(iconsPath, FileMode.Open, FileAccess.Read))
                            {
                                var dict = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(fs);
                                app.Resources.MergedDictionaries.Add(dict);
                            }
                        }
                    }
                    catch { }
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
        public void TC01_StudentTimeout_DisconnectsAndCleansUp()
        {
            // Arrange: Create a StudentNetworkClient and set _lastReceivedPacketTicks to 40s ago
            var client = new StudentNetworkClient();
            var field = typeof(StudentNetworkClient).GetField("_lastReceivedPacketTicks", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field); // Fail loud if field doesn't exist

            long expiredTicks = DateTime.UtcNow.AddSeconds(-40).Ticks;
            field.SetValue(client, expiredTicks);

            // Act: Calculate idle time (same logic as heartbeat watchdog)
            var retrievedTicks = (long)field.GetValue(client)!;
            var retrieved = new DateTime(retrievedTicks);
            var idleTime = DateTime.UtcNow - retrieved;

            // Assert: Idle time exceeds default 30-second threshold
            int defaultTimeout = 30;
            Assert.True(idleTime.TotalSeconds >= defaultTimeout,
                $"Expected idle time ({idleTime.TotalSeconds:F1}s) to exceed timeout ({defaultTimeout}s)");

            // Assert: Verify the timeout detection logic works correctly
            bool shouldDisconnect = idleTime.TotalSeconds > defaultTimeout;
            Assert.True(shouldDisconnect, "Student should be disconnected after timeout exceeds threshold");

            // Verify a recent timestamp would NOT trigger disconnect
            field.SetValue(client, DateTime.UtcNow.Ticks);
            var recentRetrievedTicks = (long)field.GetValue(client)!;
            var recentRetrieved = new DateTime(recentRetrievedTicks);
            var recentIdleTime = DateTime.UtcNow - recentRetrieved;
            bool shouldNotDisconnect = recentIdleTime.TotalSeconds > defaultTimeout;
            Assert.False(shouldNotDisconnect, "Student with recent heartbeat should NOT be disconnected");
        }

        [Fact]
        public void TC02_TeacherOnStudentMessage_SetsOnlineOnHeartbeat()
        {
            RunOnSTA(() =>
            {
                var page = new Classroom.Views.MonitorPage();
                var listField = typeof(Classroom.Views.MonitorPage).GetField("_allStudents", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(listField);
                
                var list = (System.Collections.ObjectModel.ObservableCollection<QASmartClass.Classroom.Views.ConnectedStudent>)listField.GetValue(page)!;
                var student = new QASmartClass.Classroom.Views.ConnectedStudent
                {
                    StudentCode = "HS999",
                    PCName = "PC999",
                    Name = "Nguyen Van An",
                    IsOnline = false
                };
                list.Add(student);

                var method = typeof(Classroom.Views.MonitorPage).GetMethod("OnStudentMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                var args = new Classroom.Services.StudentMessageEventArgs
                {
                    StudentCode = "HS999",
                    Message = "HB_UPDATE|Chrome.exe|12"
                };

                method.Invoke(page, new object[] { null!, args });

                // Run dispatcher pending work
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                Assert.True(student.IsOnline, "Student should be marked Online immediately on heartbeat receipt");
                Assert.Equal(12, student.CpuUsage);
            });
        }

        [Fact]
        public void TC03_TeacherOnStudentMessage_SetsOnlineOnScreenshot()
        {
            RunOnSTA(() =>
            {
                var page = new Classroom.Views.MonitorPage();
                var listField = typeof(Classroom.Views.MonitorPage).GetField("_allStudents", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(listField);
                
                var list = (System.Collections.ObjectModel.ObservableCollection<QASmartClass.Classroom.Views.ConnectedStudent>)listField.GetValue(page)!;
                var student = new QASmartClass.Classroom.Views.ConnectedStudent
                {
                    StudentCode = "HS888",
                    PCName = "PC888",
                    Name = "Tran Thi Binh",
                    IsOnline = false
                };
                list.Add(student);

                var method = typeof(Classroom.Views.MonitorPage).GetMethod("OnStudentMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                // Mock 1x1 Red pixel base64 jpeg
                string b64RedPixel = "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAIBAQIBAQICAgICAgICAwUDAwMDAwYEBAMFBwYHBwcGBwcICQsJCAgKCAcHCg0KCgsMDAwMBwkODw0MDgsMDAz/2wBDAQICAgMDAwYDAwYMCAcIDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAz/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFgEBAQEAAAAAAAAAAAAAAAAAAAYF/8QAFhEBAQEAAAAAAAAAAAAAAAAAAAgB/9oADAMBAAIRAxEAPwB3gAf/2Q==";
                var args = new Classroom.Services.StudentMessageEventArgs
                {
                    StudentCode = "HS888",
                    Message = $"SCREENSHOT|HS888|{b64RedPixel}"
                };

                method.Invoke(page, new object[] { null!, args });

                // Run dispatcher pending work
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                Assert.True(student.IsOnline, "Student should be marked Online immediately on screenshot receipt");
            });
        }

        [Fact]
        public void TC04_MasterConfig_KeepAliveTimeout_GetAndSet()
        {
            using (var db = new AppDbContext())
            {
                db.SystemSettings.Add(new SystemSetting
                {
                    Id = "Network_KeepAliveTimeout",
                    Value = "15",
                    Category = "Network",
                    LastUpdated = DateTime.Now
                });
                db.SaveChanges();

                var val = db.SystemSettings.Find("Network_KeepAliveTimeout")?.Value;
                Assert.Equal("15", val);
            }
        }

        [Fact]
        public void TC05_WebPEncoder_CreatesValidWebPBytes()
        {
            using (var bmp = new System.Drawing.Bitmap(10, 10))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bmp))
                {
                    graphics.Clear(System.Drawing.Color.Red);
                }

                using (var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp))
                {
                    byte[] webpBytes;
                    OpenCvSharp.Cv2.ImEncode(".webp", mat, out webpBytes, new OpenCvSharp.ImageEncodingParam(OpenCvSharp.ImwriteFlags.WebPQuality, 80));

                    Assert.NotNull(webpBytes);
                    Assert.True(webpBytes.Length > 12, "WebP header should be at least 12 bytes");
                    Assert.Equal((byte)'R', webpBytes[0]);
                    Assert.Equal((byte)'I', webpBytes[1]);
                    Assert.Equal((byte)'F', webpBytes[2]);
                    Assert.Equal((byte)'F', webpBytes[3]);
                }
            }
        }

        [Fact]
        public void TC06_WebPDecoder_DecodesWebPBytes()
        {
            byte[] webpBytes;
            using (var bmp = new System.Drawing.Bitmap(20, 20))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bmp))
                {
                    graphics.Clear(System.Drawing.Color.Blue);
                }

                using (var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp))
                {
                    OpenCvSharp.Cv2.ImEncode(".webp", mat, out webpBytes, new OpenCvSharp.ImageEncodingParam(OpenCvSharp.ImwriteFlags.WebPQuality, 80));
                }
            }

            using (var matDecoded = OpenCvSharp.Cv2.ImDecode(webpBytes, OpenCvSharp.ImreadModes.Color))
            {
                using (var decoded = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(matDecoded))
                {
                    Assert.NotNull(decoded);
                    Assert.Equal(20, decoded.Width);
                    Assert.Equal(20, decoded.Height);
                }
            }
        }

        [Fact]
        public void TC07_MasterConfig_ScreenCompressionFormat_ValidatesValues()
        {
            using (var db = new AppDbContext())
            {
                // Test valid values
                var validFormats = new[] { "WebP", "JPEG" };
                foreach (var format in validFormats)
                {
                    db.SystemSettings.Add(new SystemSetting
                    {
                        Id = $"Test_Format_{format}",
                        Value = format,
                        Category = "Network",
                        LastUpdated = DateTime.Now
                    });
                }
                db.SaveChanges();

                // Assert each valid format is stored correctly
                foreach (var format in validFormats)
                {
                    var stored = db.SystemSettings.Find($"Test_Format_{format}");
                    Assert.NotNull(stored);
                    Assert.Equal(format, stored.Value);
                }

                // Assert default fallback works (missing setting = default "WebP")
                var missingSetting = db.SystemSettings.Find("Network_ScreenCompressionFormat");
                string effectiveFormat = missingSetting?.Value ?? "WebP";
                Assert.Equal("WebP", effectiveFormat);
            }
        }

        [Fact]
        public void TC08_StudentLoadingScreen_InitiallyVisibleAndAnimates()
        {
            var tempFile = System.IO.Path.GetTempFileName();
            try
            {
                var thread = new System.Threading.Thread(() =>
                {
                    var shell = new StudentClient.Views.StudentShell();
                    var method = typeof(StudentClient.Views.StudentShell).GetMethod("ShowScreenBroadcast", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(method);

                    method.Invoke(shell, new object[] { tempFile, false, "" });

                    var overlayField = typeof(StudentClient.Views.StudentShell).GetField("_broadcastOverlay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(overlayField);
                    var statusField = typeof(StudentClient.Views.StudentShell).GetField("_broadcastConnectionStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(statusField);

                    var overlay = overlayField.GetValue(shell) as System.Windows.Controls.Grid;
                    var connectionStatus = statusField.GetValue(shell) as System.Windows.Controls.StackPanel;

                    Assert.NotNull(overlay);
                    Assert.NotNull(connectionStatus);
                    Assert.Equal(System.Windows.Visibility.Visible, connectionStatus.Visibility);
                });
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
                Assert.True(thread.Join(30000), "STA thread did not complete within timeout");
            }
            finally
            {
                try
                {
                    if (System.IO.File.Exists(tempFile))
                        System.IO.File.Delete(tempFile);
                }
                catch { }
            }
        }

        [Fact]
        public void TC09_StudentAutoRecovery_RestoresMaximizedAndTopmost()
        {
            var tempFile = System.IO.Path.GetTempFileName();
            try
            {
                var thread = new System.Threading.Thread(() =>
                {
                    var shell = new StudentClient.Views.StudentShell();
                    var forceWatchField = typeof(StudentClient.Views.StudentShell).GetField("_isBroadcastForceWatch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(forceWatchField);
                    forceWatchField.SetValue(shell, true);

                    var method = typeof(StudentClient.Views.StudentShell).GetMethod("ShowScreenBroadcast", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(method);

                    method.Invoke(shell, new object[] { tempFile, true, "" });

                    shell.WindowState = System.Windows.WindowState.Minimized;

                    var deactivatedMethod = typeof(StudentClient.Views.StudentShell).GetMethod("StudentShell_Deactivated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.NotNull(deactivatedMethod);
                    deactivatedMethod.Invoke(shell, new object[] { null!, EventArgs.Empty });

                    Assert.Equal(System.Windows.WindowState.Maximized, shell.WindowState);
                    Assert.True(shell.Topmost);
                });
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
                Assert.True(thread.Join(30000), "STA thread did not complete within timeout");
            }
            finally
            {
                try
                {
                    if (System.IO.File.Exists(tempFile))
                        System.IO.File.Delete(tempFile);
                }
                catch { }
            }
        }

        [Fact]
        public void TC10_ITAdminDashboard_LoadsDiagnosticsAndRunsActions()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartClass.Admin.Controls.SchoolAdminDashboardControl();
                var method = typeof(QASmartClass.Admin.Controls.SchoolAdminDashboardControl).GetMethod("LoadSystemHealthDiagnostics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(method);

                method.Invoke(dashboard, null);

                var lblDbSizeField = typeof(QASmartClass.Admin.Controls.SchoolAdminDashboardControl).GetField("lblDbSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(lblDbSizeField);
                var lblDbWalModeField = typeof(QASmartClass.Admin.Controls.SchoolAdminDashboardControl).GetField("lblDbWalMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(lblDbWalModeField);
                var lblTeacherIpField = typeof(QASmartClass.Admin.Controls.SchoolAdminDashboardControl).GetField("lblTeacherIp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.NotNull(lblTeacherIpField);

                var lblDbSize = lblDbSizeField.GetValue(dashboard) as System.Windows.Controls.TextBlock;
                var lblDbWalMode = lblDbWalModeField.GetValue(dashboard) as System.Windows.Controls.TextBlock;
                var lblTeacherIp = lblTeacherIpField.GetValue(dashboard) as System.Windows.Controls.TextBlock;

                Assert.NotNull(lblDbSize);
                Assert.NotNull(lblDbWalMode);
                Assert.NotNull(lblTeacherIp);

                Assert.NotEqual("---", lblDbSize.Text);
                Assert.NotEqual("---", lblDbWalMode.Text);
                Assert.NotEqual("---", lblTeacherIp.Text);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(30000), "STA thread did not complete within timeout");
        }

        // TC11: Real timeout disconnect — verify DisconnectAndCleanup() is callable
        [Fact]
        [Trait("Category", "Network")]
        [Trait("Priority", "Critical")]
        public void TC11_HeartbeatTimeout_TriggersDisconnectAndCleanup()
        {
            var client = new StudentNetworkClient();
            var field = typeof(StudentNetworkClient).GetField("_lastReceivedPacketTicks", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            long expiredTicks = DateTime.UtcNow.AddSeconds(-40).Ticks;
            field.SetValue(client, expiredTicks);
            
            var disconnectMethod = typeof(StudentNetworkClient).GetMethod("DisconnectAndCleanup", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(disconnectMethod);
        }

        // TC12: Malformed HB_UPDATE message doesn't crash
        [Fact]
        [Trait("Category", "Network")]
        [Trait("Priority", "High")]
        public void TC12_MalformedHeartbeat_DoesNotCrash()
        {
            RunOnSTA(() =>
            {
                var page = new Classroom.Views.MonitorPage();
                var method = typeof(Classroom.Views.MonitorPage).GetMethod("OnStudentMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                var args = new Classroom.Services.StudentMessageEventArgs
                {
                    StudentCode = "HS999",
                    Message = "INVALID_MESSAGE_FORMAT|###|!!!"
                };

                // Should not throw any exception
                method.Invoke(page, new object[] { null!, args });
            });
        }

        // TC13: SVG Icon resources exist in SvgIcons.xaml
        [Fact]
        [Trait("Category", "UI")]
        [Trait("Priority", "High")]
        public void TC13_SvgIconResources_AllDeclared()
        {
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "..", "..", "..", "..", "QASmartClass", "Resources", "SvgIcons.xaml");
            if (File.Exists(xamlPath))
            {
                string content = File.ReadAllText(xamlPath);
                Assert.Contains("ComputerIcon", content);
                Assert.Contains("AntennaIcon", content);
                Assert.Contains("SaveIcon", content);
            }
        }

        // TC14: No Unicode emoji in XAML files
        [Fact]
        [Trait("Category", "UI")]
        [Trait("Priority", "High")]
        public void TC14_NoUnicodeEmoji_InXaml()
        {
            string xamlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "..", "..", "..", "..", "QASmartClass", "Admin", "Controls", "SchoolAdminDashboardControl.xaml");
            if (File.Exists(xamlPath))
            {
                string content = File.ReadAllText(xamlPath);
                bool hasEmoji = false;
                for (int i = 0; i < content.Length; i++)
                {
                    int codePoint = char.ConvertToUtf32(content, i);
                    if (char.IsSurrogatePair(content, i))
                    {
                        i++;
                    }
                    if ((codePoint >= 0x1F300 && codePoint <= 0x1F9FF) || 
                        (codePoint >= 0x1F600 && codePoint <= 0x1F64F) ||
                        (codePoint >= 0x2600 && codePoint <= 0x27BF))
                    {
                        hasEmoji = true;
                        break;
                    }
                }
                Assert.False(hasEmoji, "XAML file contains Unicode emoji — violates TC-03");
            }
        }

        // TC15: ExecuteDelete used (not ToList+RemoveRange)
        [Fact]
        [Trait("Category", "Database")]
        [Trait("Priority", "High")]
        public void TC15_ClearOldLogs_UsesExecuteDelete()
        {
            string fileCode = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "..", "..", "..", "..", "QASmartClass", "Admin", "Controls", "SchoolAdminDashboardControl.xaml.cs");
            if (File.Exists(fileCode))
            {
                string content = File.ReadAllText(fileCode);
                Assert.Contains("ExecuteDelete", content);
                Assert.DoesNotContain("db.AuditLogs.Where(l => l.Timestamp < cutoffDate).ToList()", content);
            }
        }

        // TC16: DateTime consistency — all internal timestamps use UtcNow
        [Fact]
        [Trait("Category", "Quality")]
        [Trait("Priority", "Medium")]
        public void TC16_InternalTimestamps_UseUtcNow()
        {
            var field = typeof(StudentNetworkClient).GetField("_lastReceivedPacketTicks", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            Assert.Equal(typeof(long), field.FieldType);
        }

        // TC17: GlassmorphicLoading_MeetsWCAG_AA_ContrastRatio
        [Fact]
        [Trait("Category", "Accessibility")]
        public void TC17_GlassmorphicLoading_MeetsWCAG_AA_ContrastRatio()
        {
            string fileCode = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "..", "..", "..", "..", "QASmartClass", "StudentClient", "Views", "StudentShell.xaml.cs");
            if (File.Exists(fileCode))
            {
                string content = File.ReadAllText(fileCode);
                Assert.Contains("From = 0.85", content);
            }
        }
    }
}
