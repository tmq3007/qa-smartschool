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
    /// BỘ KIỂM THỬ NGHIỆM THU LOI_VID_09 — HỘI ĐỒNG CHUYÊN GIA
    /// 
    /// Đánh giá các tính năng:
    ///   TC-01: Tin nhắn từ giáo viên (Broadcast, Nhóm, Riêng) nhận được lưu chính xác vào SQLite.
    ///   TC-02: Lịch sử chat tải đúng tất cả tin nhắn đã nhận từ DB.
    ///   TC-03: Chặn tin nhắn trống/chỉ chứa khoảng trắng không lưu DB và không hiển thị.
    ///   TC-04: UnreadTimer đồng bộ tin nhắn mới từ DB hoạt động ổn định và lọc trùng tốt.
    /// </summary>
    public class LOI_VID_09_ExpertVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_09_ExpertVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_09_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_09_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            // Initialize the database schema
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                // Set up default online student for profile queries
                db.Students.Add(new Student
                {
                    Id = 1,
                    StudentCode = "HS00001",
                    FullName = "Nguyễn Văn Học Sinh",
                    IsOnline = true,
                    ClassroomId = 1
                });
                db.SaveChanges();
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
                    try
                    {
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null) appCreatedField.SetValue(null, false);
                        var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (currentField != null) currentField.SetValue(null, null);
                    }
                    catch { }

                    QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
                    QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

                    var app = new QASmartTouch.App();
                    var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                    if (dbProperty != null)
                    {
                        dbProperty.SetValue(app, new AppDbContext());
                    }
                    if (app.StudentNetwork != null)
                    {
                        app.StudentNetwork.StudentCode = "HS00001";
                    }

                    var resources = app.Resources;
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

                    InitializeApplicationFull(); action();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
                finally
                {
                    try
                    {
                        if (Application.Current != null)
                        {
                            Application.Current.Shutdown();
                        }
                    }
                    catch { }
                    try
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                    }
                    catch { }
                    try
                    {
                        var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                        if (appCreatedField != null) appCreatedField.SetValue(null, false);
                        var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                        if (currentField != null) currentField.SetValue(null, null);
                    }
                    catch { }
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            bool completed = t.Join(TimeSpan.FromSeconds(15));
            Assert.True(completed, "STA thread timed out after 15 seconds");
            Assert.Null(threadEx);
        }

        [Fact]
        public void TC01_ReceiveMessage_SavesToDatabase()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                // 1. Send Broadcast Message
                handleMethod.Invoke(shell, new object[] { "📢 GV: Chào các em học sinh!" });

                // 2. Send Custom Group Message
                handleMethod.Invoke(shell, new object[] { "📨 [Nhóm 1] GV: Hãy thảo luận cùng nhóm." });

                // 3. Send Private Message
                handleMethod.Invoke(shell, new object[] { "🔒 GV: Em hãy tập trung học nhé." });

                // Give background Task.Run time to persist to DB
                Thread.Sleep(500);

                using (var db = new AppDbContext())
                {
                    var logs = db.EventLogs.ToList();
                    
                    // Verify Broadcast Message
                    var broadcastLog = logs.FirstOrDefault(l => l.EventType == "TEACHER_CHAT");
                    Assert.NotNull(broadcastLog);
                    Assert.Contains("Chào các em học sinh!", broadcastLog.Details);
                    Assert.Contains("[CH:ALL]", broadcastLog.Details);
                    Assert.Equal("GV", broadcastLog.Actor);

                    // Verify Group Message
                    var groupLog = logs.FirstOrDefault(l => l.EventType == "GROUP_CHAT");
                    Assert.NotNull(groupLog);
                    Assert.Contains("Hãy thảo luận cùng nhóm.", groupLog.Details);
                    Assert.Contains("[CH:GROUP_DYN_Nhóm 1]", groupLog.Details);
                    Assert.Equal("GV", groupLog.Actor);

                    // Verify Private Message
                    var privateLog = logs.FirstOrDefault(l => l.EventType == "PRIVATE_CHAT");
                    Assert.NotNull(privateLog);
                    Assert.Contains("Em hãy tập trung học nhé.", privateLog.Details);
                    Assert.Contains("[CH:HS00001]", privateLog.Details);
                    Assert.Equal("GV", privateLog.Actor);
                }
            });
        }

        [Fact]
        public void TC02_LoadHistory_RetrievesReceivedMessages()
        {
            RunOnSTA(() =>
            {
                // Pre-populate Database with Chat History from Teacher
                using (var db = new AppDbContext())
                {
                    db.EventLogs.Add(new EventLog
                    {
                        EventType = "TEACHER_CHAT",
                        Actor = "GV",
                        Details = "Tin nhắn: Tin nhắn lịch sử broadcast [CH:ALL]",
                        Timestamp = DateTime.Now.AddMinutes(-5)
                    });
                    db.EventLogs.Add(new EventLog
                    {
                        EventType = "GROUP_CHAT",
                        Actor = "GV",
                        Details = "Tin nhắn: Thảo luận nhóm toán [CH:GROUP_DYN_Nhóm Toán]",
                        Timestamp = DateTime.Now.AddMinutes(-4)
                    });
                    db.EventLogs.Add(new EventLog
                    {
                        EventType = "PRIVATE_CHAT",
                        Actor = "GV",
                        Details = "Tin nhắn: Nhắc nhở riêng [CH:HS00001]",
                        Timestamp = DateTime.Now.AddMinutes(-3)
                    });
                    db.SaveChanges();
                }

                // Instantiate StudentChatPage, which triggers LoadChatHistoryFromDB in constructor
                var chatPage = new StudentChatPage();

                var allMessagesField = typeof(StudentChatPage).GetField("_allMessages", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(allMessagesField);
                var messagesList = allMessagesField.GetValue(chatPage) as IEnumerable<object>;
                Assert.NotNull(messagesList);

                var list = messagesList.ToList();
                Assert.Equal(3, list.Count);

                // Check contents
                dynamic m1 = list[0];
                Assert.Equal("GV", m1.Author);
                Assert.Equal("Tin nhắn lịch sử broadcast", m1.Text);
                Assert.Equal("ALL", m1.Channel);

                dynamic m2 = list[1];
                Assert.Equal("GV", m2.Author);
                Assert.Equal("Thảo luận nhóm toán", m2.Text);
                Assert.Equal("GROUP_DYN_Nhóm Toán", m2.Channel);

                dynamic m3 = list[2];
                Assert.Equal("GV", m3.Author);
                Assert.Equal("Nhắc nhở riêng", m3.Text);
                Assert.Equal("TEACHER", m3.Channel);
            });
        }

        [Fact]
        public void TC03_EmptyOrWhitespaceMessage_IsRejected()
        {
            RunOnSTA(() =>
            {
                var shell = new StudentShell();
                var handleMethod = typeof(StudentShell).GetMethod("HandleTeacherMessage", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(handleMethod);

                // Reset DB
                using (var db = new AppDbContext())
                {
                    db.EventLogs.RemoveRange(db.EventLogs);
                    db.SaveChanges();
                }

                // Send empty/whitespace messages
                handleMethod.Invoke(shell, new object[] { "📢 GV: " });
                handleMethod.Invoke(shell, new object[] { "🔒 GV:    " });
                handleMethod.Invoke(shell, new object[] { "" });

                Thread.Sleep(300);

                // Verify DB is still empty of chat logs
                using (var db = new AppDbContext())
                {
                    var logsCount = db.EventLogs.Count(ev => ev.EventType == "CHAT"
                                                          || ev.EventType == "TEACHER_CHAT"
                                                          || ev.EventType == "PRIVATE_CHAT"
                                                          || ev.EventType == "GROUP_CHAT");
                    Assert.Equal(0, logsCount);
                }

                // Test AppendNewMessage direct call
                var chatPage = new StudentChatPage();
                var allMessagesField = typeof(StudentChatPage).GetField("_allMessages", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(allMessagesField);
                var messagesList = allMessagesField.GetValue(chatPage) as System.Collections.IList;
                Assert.NotNull(messagesList);

                int countBefore = messagesList.Count;
                chatPage.AppendNewMessage("📢 GV:  ");
                chatPage.AppendNewMessage("");
                int countAfter = messagesList.Count;
                Assert.Equal(countBefore, countAfter);
            });
        }

        [Fact]
        public void TC04_UnreadTimer_SyncsNewMessagesFromDB()
        {
            RunOnSTA(() =>
            {
                var chatPage = new StudentChatPage();

                var allMessagesField = typeof(StudentChatPage).GetField("_allMessages", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(allMessagesField);
                var messagesList = allMessagesField.GetValue(chatPage) as System.Collections.IList;
                Assert.NotNull(messagesList);

                // Insert a new message directly to DB simulating background arrival
                using (var db = new AppDbContext())
                {
                    db.EventLogs.Add(new EventLog
                    {
                        EventType = "TEACHER_CHAT",
                        Actor = "GV",
                        Details = "Tin nhắn: Tin nhắn mới xuất hiện khi tắt page [CH:ALL]",
                        Timestamp = DateTime.Now
                    });
                    db.SaveChanges();
                }

                // Get timer tick method via reflection
                var tickMethod = typeof(StudentChatPage).GetMethod("UnreadTimer_Tick", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(tickMethod);

                // Invoke timer tick (passes sender and EventArgs)
                tickMethod.Invoke(chatPage, new object[] { this, EventArgs.Empty });

                // Allow any background tasks running inside the tick method (like Task.Run) to finish
                Thread.Sleep(500);

                // Verify the message was parsed and added to _allMessages list
                bool found = false;
                foreach (dynamic m in messagesList)
                {
                    if (m.Text == "Tin nhắn mới xuất hiện khi tắt page")
                    {
                        found = true;
                        break;
                    }
                }
                Assert.True(found);

                // Tick again, verify no duplicates are added
                int countBefore = messagesList.Count;
                tickMethod.Invoke(chatPage, new object[] { this, EventArgs.Empty });
                Thread.Sleep(300);
                int countAfter = messagesList.Count;
                Assert.Equal(countBefore, countAfter);
            });
        }
    }
}
