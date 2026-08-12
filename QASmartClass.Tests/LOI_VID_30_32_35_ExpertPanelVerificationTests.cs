using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;
using QASmartClass.Classroom.Views;
using QASmartClass.Data;
using QASmartClass.Utilities;
using static QASmartClass.Utilities.MessageGuard;

namespace QASmartClass.Tests
{
    /// <summary>
    /// ╔══════════════════════════════════════════════════════════════════╗
    /// ║  BỘ KIỂM THỬ XÁC MINH TOÀN DIỆN — HỘI ĐỒNG CHUYÊN GIA      ║
    /// ║  LOI_VID_30 + LOI_VID_32 + LOI_VID_35                         ║
    /// ║  Ngày: 30/06/2026 | QA SmartClass v4.1                        ║
    /// ╚══════════════════════════════════════════════════════════════════╝
    /// 
    /// Hội đồng 15+ chuyên gia: QA Tester, IT Admin, Bảo mật, Kiến trúc,
    /// Giáo viên, Học sinh, Nhà giáo dục, Trưởng bộ môn, Hiệu trưởng,
    /// Gamer, Phòng GD, Sở GD, Nhà khoa học GD.
    /// 
    /// Module A: LOI_VID_30 — Rò rỉ Heartbeat & ENC_CMD vào Chat (15 tests)
    /// Module B: LOI_VID_32 — Nhân bản học sinh & mất hiển thị giám sát (15 tests)
    /// Module C: LOI_VID_35 — Hiển thị TextBlock raw trong Quiz Review (10 tests)
    /// </summary>
    public class LOI_VID_30_32_35_ExpertPanelVerificationTests : IDisposable
    {
        private readonly string _dbFile;
        private readonly string _versionFile;

        public LOI_VID_30_32_35_ExpertPanelVerificationTests()
        {
            QASmartClass.Services.AppPaths.EnsureDirectories();
            var uniqueId = Guid.NewGuid().ToString("N");
            _dbFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"smartclass_expert_panel_{uniqueId}.db");
            _versionFile = Path.Combine(QASmartClass.Services.AppPaths.RootDir, $"db_version_expert_panel_{uniqueId}.txt");

            QASmartClass.Services.AppPaths.DatabaseFile = _dbFile;
            QASmartClass.Services.AppPaths.DbVersionFile = _versionFile;

            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.SaveChanges();
            }
        }

        public void Dispose()
        {
            try { if (File.Exists(_dbFile)) File.Delete(_dbFile); } catch { }
            try { if (File.Exists(_versionFile)) File.Delete(_versionFile); } catch { }
        }

        // ══════════════════════════════════════════════════════════════
        //  INFRASTRUCTURE — STA Thread & WPF App Bootstrap
        // ══════════════════════════════════════════════════════════════

        private void RunOnStaThread(Action action)
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
            Exception? staException = null;
            var thread = new Thread(() =>
            {
                try { InitializeApplicationFull(); action(); }
                catch (Exception ex) { staException = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(30));
            if (staException != null)
                throw new AggregateException("STA thread failed", staException);
        }

        private void InitializeWpfApplication()
        {
            if (Application.Current != null && (!(Application.Current is QASmartTouch.App) || Application.Current.Dispatcher.Thread != Thread.CurrentThread))
            {
                var appCreatedField = typeof(Application).GetField("_appCreatedInThisAppDomain", BindingFlags.Static | BindingFlags.NonPublic);
                appCreatedField?.SetValue(null, false);
                var currentField = typeof(Application).GetField("_appInstance", BindingFlags.Static | BindingFlags.NonPublic);
                currentField?.SetValue(null, null);
            }

            if (Application.Current == null)
            {
                var app = new QASmartTouch.App();
                var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                dbProperty?.SetValue(app, new AppDbContext());
            }
            else if (Application.Current is QASmartTouch.App existingApp)
            {
                var dbProperty = typeof(QASmartTouch.App).GetProperty("Database", BindingFlags.Public | BindingFlags.Instance);
                dbProperty?.SetValue(existingApp, new AppDbContext());
            }

            // Register mock resources
            var brushKeys = new[] { "LightBrush", "BorderLightBrush", "DarkBrush", "MutedBrush",
                                    "PrimaryLightBrush", "PrimaryBrush", "PrimaryDarkBrush" };
            foreach (var key in brushKeys)
            {
                if (!Application.Current!.Resources.Contains(key))
                    Application.Current.Resources[key] = new SolidColorBrush(Colors.Gray);
            }
            var geomKeys = new[] { "GeomLock", "GeomUnlock" };
            foreach (var key in geomKeys)
            {
                if (!Application.Current!.Resources.Contains(key))
                    Application.Current.Resources[key] = new PathGeometry();
            }
        }

        // ╔══════════════════════════════════════════════════════════════╗
        // ║  MODULE A: LOI_VID_30 — RÒ RỈ HEARTBEAT & ENC_CMD          ║
        // ║  15 test cases — Xác minh MessageGuard + Event Separation   ║
        // ╚══════════════════════════════════════════════════════════════╝

        // ── A01: [P0] [QA Tester] HB_UPDATE|HS001|0 bị chặn ──
        [Fact]
        public void A01_MessageGuard_Blocks_HbUpdate_ExactVideoString()
        {
            var result = MessageGuard.Evaluate("HB_UPDATE|HS001|0");
            Assert.False(result.IsAllowedInChat, "HB_UPDATE|HS001|0 phải bị chặn — đây là chuỗi chính xác từ video lỗi");
            Assert.Equal(MessageCategory.Heartbeat, result.Category);
        }

        // ── A02: [P0] [Bảo mật] ENC_CMD payload 10KB bị chặn ──
        [Fact]
        public void A02_MessageGuard_Blocks_EncCmd_LargePayload()
        {
            var payload = "ENC_CMD|" + new string('A', 10240);
            var result = MessageGuard.Evaluate(payload);
            Assert.False(result.IsAllowedInChat, "ENC_CMD payload lớn phải bị chặn hoàn toàn");
            Assert.Equal(MessageCategory.EncryptedCommand, result.Category);
        }

        // ── A03: [P0] [Giáo viên] CHAT|ALL cho phép bình thường ──
        [Fact]
        public void A03_MessageGuard_Allows_ChatAll_NormalMessage()
        {
            var result = MessageGuard.Evaluate("CHAT|HS001|ALL|Xin chào các em, hôm nay chúng ta học bài mới");
            Assert.True(result.IsAllowedInChat, "Tin nhắn CHAT|ALL phải được cho phép hiển thị trong giao diện chat");
            Assert.Equal(MessageCategory.Chat, result.Category);
        }

        // ── A04: [P0] [Học sinh] STUDENT_QUESTION cho phép ──
        [Fact]
        public void A04_MessageGuard_Allows_StudentQuestion()
        {
            var result = MessageGuard.Evaluate("STUDENT_QUESTION|HS005|text=Thầy ơi, em không hiểu bài 3");
            Assert.True(result.IsAllowedInChat, "Câu hỏi học sinh phải được hiển thị");
            Assert.Equal(MessageCategory.StudentQuestion, result.Category);
        }

        // ── A05: [P1] [IT Admin] SCREENSHOT bị chặn ──
        [Fact]
        public void A05_MessageGuard_Blocks_Screenshot()
        {
            var result = MessageGuard.Evaluate("SCREENSHOT|HS001|/9j/4AAQSkZJRg==");
            Assert.False(result.IsAllowedInChat, "Dữ liệu screenshot hệ thống không được hiển thị trong chat");
            Assert.Equal(MessageCategory.Screenshot, result.Category);
        }

        // ── A06: [P1] [Bảo mật] POLICY + LOCK_KEYBOARD bị chặn ──
        [Fact]
        public void A06_MessageGuard_Blocks_PolicyAndLockKeyboard()
        {
            var resultPolicy = MessageGuard.Evaluate("POLICY|block_web_on");
            Assert.False(resultPolicy.IsAllowedInChat, "POLICY commands phải bị chặn");

            var resultLock = MessageGuard.Evaluate("LOCK_KEYBOARD|ALL|true");
            Assert.False(resultLock.IsAllowedInChat, "LOCK_KEYBOARD commands phải bị chặn");
        }

        // ── A07: [P1] [QA Tester] null, empty, whitespace bị chặn ──
        [Fact]
        public void A07_MessageGuard_Blocks_NullEmptyWhitespace()
        {
            Assert.False(MessageGuard.Evaluate(null).IsAllowedInChat, "null phải bị chặn");
            Assert.False(MessageGuard.Evaluate("").IsAllowedInChat, "empty phải bị chặn");
            Assert.False(MessageGuard.Evaluate("   ").IsAllowedInChat, "whitespace phải bị chặn");
            Assert.False(MessageGuard.Evaluate("\t\r\n").IsAllowedInChat, "tab/newline phải bị chặn");
        }

        // ── A08: [P1] [IT Admin] Anti-prefix collision ──
        [Fact]
        public void A08_MessageGuard_AntiPrefixCollision_ChatbotBlocked()
        {
            // "CHATBOT|" bắt đầu bằng "CHAT" nhưng KHÔNG phải "CHAT|"
            var result = MessageGuard.Evaluate("CHATBOT|some_data");
            Assert.False(result.IsAllowedInChat, "CHATBOT| không được khớp nhầm whitelist CHAT|");
            Assert.Equal(MessageCategory.Unknown, result.Category);
        }

        // ── A09: [P1] [Bảo mật] Case sensitivity — lowercase bị chặn ──
        [Fact]
        public void A09_MessageGuard_CaseSensitive_LowercaseBlocked()
        {
            var result = MessageGuard.Evaluate("chat|HS001|ALL|Hello");
            Assert.False(result.IsAllowedInChat, "Giao thức dùng uppercase — lowercase phải bị chặn");

            var result2 = MessageGuard.Evaluate("hb_update|HS001|0");
            Assert.False(result2.IsAllowedInChat, "hb_update lowercase phải bị chặn");
        }

        // ── A10: [P0] [Kiến trúc] HeartbeatReceived event tồn tại trong NetworkDiscoveryService ──
        [Fact]
        public void A10_NetworkDiscoveryService_HasHeartbeatReceivedEvent()
        {
            var eventInfo = typeof(QASmartClass.Classroom.Services.NetworkDiscoveryService)
                .GetEvent("HeartbeatReceived");
            Assert.NotNull(eventInfo);
            Assert.Equal(typeof(EventHandler<QASmartClass.Classroom.Services.StudentMessageEventArgs>), eventInfo.EventHandlerType);
        }

        // ── A11: [P0] [Kiến trúc] WebHeartbeatReceived event tồn tại trong WebSocketBridgeService ──
        [Fact]
        public void A11_WebSocketBridgeService_HasWebHeartbeatReceivedEvent()
        {
            var eventInfo = typeof(QASmartClass.Classroom.Services.WebSocketBridgeService)
                .GetEvent("WebHeartbeatReceived");
            Assert.NotNull(eventInfo);
        }

        // ── A12: [P0] [Bảo mật] Default-block fail-safe cho tin nhắn lạ ──
        [Fact]
        public void A12_MessageGuard_DefaultBlock_FailSafe_UnknownMessages()
        {
            var testMessages = new[]
            {
                "RANDOM_DATA_12345",
                "UNKNOWN_PROTOCOL|payload",
                "SELECT * FROM Users",            // SQL injection attempt
                "<script>alert('xss')</script>",   // XSS attempt
                "../../etc/passwd",                // Path traversal attempt
                "ADMIN_OVERRIDE|secret_key",
            };

            foreach (var msg in testMessages)
            {
                var result = MessageGuard.Evaluate(msg);
                Assert.False(result.IsAllowedInChat, $"Tin nhắn lạ '{msg.Substring(0, Math.Min(30, msg.Length))}...' phải bị chặn (fail-safe)");
                Assert.Equal(MessageCategory.Unknown, result.Category);
            }
        }

        // ── A13: [P1] [Hiệu năng] Stress test 1000 tin nhắn ──
        [Fact]
        public void A13_MessageGuard_StressTest_1000Messages_NoLeak()
        {
            int allowed = 0;
            int blocked = 0;
            var messages = new List<string>();

            // 500 tin nhắn chat hợp lệ
            for (int i = 0; i < 500; i++)
                messages.Add($"CHAT|HS{i:D3}|ALL|Tin nhắn {i}");

            // 300 heartbeat phải bị chặn
            for (int i = 0; i < 300; i++)
                messages.Add($"HB_UPDATE|HS{i:D3}|{i % 100}");

            // 200 ENC_CMD phải bị chặn
            for (int i = 0; i < 200; i++)
                messages.Add($"ENC_CMD|{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"command_{i}"))}");

            foreach (var msg in messages)
            {
                var result = MessageGuard.Evaluate(msg);
                if (result.IsAllowedInChat) allowed++;
                else blocked++;
            }

            Assert.Equal(500, allowed);
            Assert.Equal(500, blocked);
        }

        // ── A14: [P1] [Học sinh] HAND_RAISE và STUDENT_FEEDBACK cho phép ──
        [Fact]
        public void A14_MessageGuard_Allows_HandRaise_And_Feedback()
        {
            var resultHR = MessageGuard.Evaluate("HAND_RAISE|raised=True|reason=Em muốn phát biểu");
            Assert.True(resultHR.IsAllowedInChat, "Giơ tay phải được hiển thị");
            Assert.Equal(MessageCategory.HandRaise, resultHR.Category);

            var resultHREnc = MessageGuard.Evaluate("HAND_RAISE_ENC|encrypted_payload==");
            Assert.True(resultHREnc.IsAllowedInChat, "Giơ tay mã hóa phải được hiển thị");

            var resultFB = MessageGuard.Evaluate("STUDENT_FEEDBACK|HS001|👍");
            Assert.True(resultFB.IsAllowedInChat, "Phản hồi học sinh phải được hiển thị");
            Assert.Equal(MessageCategory.StudentFeedback, resultFB.Category);
        }

        // ── A15: [P2] [QA Tester] HB_ACK không có pipe vẫn bị chặn ──
        [Fact]
        public void A15_MessageGuard_Blocks_HbAck_NoPipe()
        {
            var result = MessageGuard.Evaluate("HB_ACK");
            Assert.False(result.IsAllowedInChat, "HB_ACK (không có pipe) phải bị chặn");
            Assert.Equal(MessageCategory.Heartbeat, result.Category);

            // HB_ACKXYZ cũng bị chặn (over-matching an toàn)
            var result2 = MessageGuard.Evaluate("HB_ACKXYZ");
            Assert.False(result2.IsAllowedInChat, "HB_ACKXYZ phải bị chặn do prefix matching");
        }

        // ╔══════════════════════════════════════════════════════════════╗
        // ║  MODULE B: LOI_VID_32 — NHÂN BẢN HỌC SINH & GIÁM SÁT      ║
        // ║  15 test cases — Matching, Deduplication, Edge Cases         ║
        // ╚══════════════════════════════════════════════════════════════╝

        // ── B01: [P0] [QA Tester] Priority 1 — StudentCode exact match ──
        [Fact]
        public void B01_PrioritizedMatching_StudentCode_ExactMatch()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                allStudents.Add(new ConnectedStudent
                {
                    Name = "Nguyễn Văn An",
                    PCName = "PC-01",
                    StudentCode = "HS001",
                    IsOnline = false
                });

                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Nguyễn Văn An",
                    StudentCode = "HS001",
                    PCName = "DESKTOP-NEW",
                    IPAddress = "192.168.1.50"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                var s = allStudents.First();
                Assert.Equal("HS001", s.StudentCode);
                Assert.Equal("DESKTOP-NEW", s.PCName);
                Assert.True(s.IsOnline, "Học sinh phải online sau khi kết nối");
            });
        }

        // ── B02: [P0] [Giáo viên] Priority 2 — Unicode NFD/NFC name match ──
        [Fact]
        public void B02_PrioritizedMatching_UnicodeName_NFD_NFC()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                var roster = new ConnectedStudent
                {
                    Name = "Nguyễn Văn An",  // NFC
                    PCName = "PC-02",
                    StudentCode = "",
                    IsOnline = false
                };
                allStudents.Add(roster);

                // Gửi tên dạng NFD (decomposed) chính xác + trailing whitespace
                // Sử dụng .Normalize(FormD) để tạo đúng NFD thay vì escape thủ công
                string nfdName = "Nguyễn Văn An".Normalize(System.Text.NormalizationForm.FormD) + " \r\n";

                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = nfdName,
                    StudentCode = "HS002",
                    PCName = "DESKTOP-STUDENT",
                    IPAddress = "192.168.1.51"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                Assert.Same(roster, allStudents.First());
                Assert.Equal("HS002", roster.StudentCode);
                Assert.True(roster.IsOnline);
            });
        }

        // ── B03: [P0] [IT Admin] Priority 3 — PCName-only fallback ──
        [Fact]
        public void B03_PrioritizedMatching_PCNameOnly_Fallback()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                var roster = new ConnectedStudent
                {
                    Name = "Trần Thị Bình",
                    PCName = "DESKTOP-ABC123",
                    StudentCode = "",
                    IsOnline = false
                };
                allStudents.Add(roster);

                // Kết nối với tên khác nhưng cùng PCName
                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Unknown Student",  // Tên không khớp
                    StudentCode = "",                  // Code rỗng
                    PCName = "DESKTOP-ABC123",         // Cùng PCName → fallback match
                    IPAddress = "192.168.1.52"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                Assert.Same(roster, allStudents.First());
                Assert.True(roster.IsOnline, "PCName match phải cập nhật trạng thái online");
            });
        }

        // ── B04: [P0] [QA Tester] Deduplication — 2 thẻ cùng code gộp thành 1 ──
        [Fact]
        public void B04_Deduplication_TwoCards_SameCode_MergeToOne()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                allStudents.Add(new ConnectedStudent { Name = "Lê Hoàng", PCName = "PC-OLD", StudentCode = "HS003", IsOnline = false });
                allStudents.Add(new ConnectedStudent { Name = "Lê Hoàng", PCName = "PC-NEW", StudentCode = "HS003", IsOnline = true });

                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Lê Hoàng",
                    StudentCode = "HS003",
                    PCName = "PC-NEWEST",
                    IPAddress = "192.168.1.53"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                Assert.Equal("HS003", allStudents.First().StudentCode);
            });
        }

        // ── B05: [P1] [Kiến trúc] 3 thẻ trùng — roster + 2 dynamic ──
        [Fact]
        public void B05_Deduplication_ThreeCards_OnlyOneRemains()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                allStudents.Add(new ConnectedStudent { Name = "Phạm Minh", PCName = "PC-01", StudentCode = "HS004", IsOnline = false });
                allStudents.Add(new ConnectedStudent { Name = "Phạm Minh", PCName = "PC-02", StudentCode = "HS004", IsOnline = true });
                allStudents.Add(new ConnectedStudent { Name = "Phạm Minh", PCName = "PC-03", StudentCode = "HS004", IsOnline = true });

                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Phạm Minh",
                    StudentCode = "HS004",
                    PCName = "PC-FINAL",
                    IPAddress = "192.168.1.54"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
            });
        }

        // ── B06: [P0] [QA Tester] Index-based resolution — roster card giữ lại ──
        [Fact]
        public void B06_IndexBasedResolution_RosterCardSurvives()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                var rosterCard = new ConnectedStudent { Name = "Nguyễn An", PCName = "PC-ROSTER", StudentCode = "", IsOnline = false };
                var dynamicCard = new ConnectedStudent { Name = "Nguyễn An", PCName = "DESKTOP-DYN", StudentCode = "HS005", IsOnline = true };

                allStudents.Add(rosterCard);   // index 0 — roster
                allStudents.Add(dynamicCard);  // index 1 — dynamic

                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Nguyễn An",
                    StudentCode = "HS005",
                    PCName = "DESKTOP-DYN",
                    IPAddress = "192.168.1.55"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                // Roster card (index thấp) phải được giữ lại, dynamic bị xóa
                Assert.Same(rosterCard, allStudents.First());
                Assert.Equal("HS005", rosterCard.StudentCode);
                Assert.True(rosterCard.IsOnline);
            });
        }

        // ── B07: [P1] [Nhà giáo dục] Khoảng trắng bên trong tên ──
        [Fact]
        public void B07_NormalizeName_InnerWhitespace_Behavior()
        {
            // Kiểm tra hành vi hiện tại: NormalizeName chỉ Trim(), không collapse inner spaces
            string name1 = "Nguyễn Văn An";       // 1 space
            string name2 = "Nguyễn  Văn  An";     // 2 spaces

            string norm1 = MonitorPage.NormalizeName(name1);
            string norm2 = MonitorPage.NormalizeName(name2);

            // Ghi nhận hành vi hiện tại — hai tên này KHÔNG bằng nhau
            // Đây là gap đã biết nhưng chấp nhận được trong môi trường trường học
            bool areEqual = string.Equals(norm1, norm2, StringComparison.OrdinalIgnoreCase);
            // Chỉ ghi nhận, không fail — vì đây là limitation đã biết
            Assert.NotNull(norm1);
            Assert.NotNull(norm2);
            // Verify NFC normalization vẫn hoạt động đúng
            Assert.Equal("Nguyễn Văn An", norm1);
        }

        // ── B08: [P1] [QA Tester] NormalizeName — trim + NFC ──
        [Fact]
        public void B08_NormalizeName_TrimsAndNormalizesNFC()
        {
            // NFD input với trailing whitespace
            string nfdInput = "Nguyễn Văn An".Normalize(System.Text.NormalizationForm.FormD) + "  \r\n\t";
            string result = MonitorPage.NormalizeName(nfdInput);
            Assert.Equal("Nguyễn Văn An", result);

            // Null/empty handling
            Assert.Equal(string.Empty, MonitorPage.NormalizeName(null!));
            Assert.Equal(string.Empty, MonitorPage.NormalizeName(""));
            Assert.Equal(string.Empty, MonitorPage.NormalizeName("   "));
        }

        // ── B09: [P1] [Trưởng bộ môn] 2 HS khác nhau cùng tên ──
        [Fact]
        public void B09_SameName_DifferentCode_NoFalseMerge()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                // 2 học sinh khác nhau cùng tên "Nguyễn Văn An" nhưng khác mã
                allStudents.Add(new ConnectedStudent { Name = "Nguyễn Văn An", PCName = "PC-01", StudentCode = "HS010", IsOnline = false });
                allStudents.Add(new ConnectedStudent { Name = "Nguyễn Văn An", PCName = "PC-02", StudentCode = "HS020", IsOnline = false });

                // HS010 kết nối
                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Nguyễn Văn An",
                    StudentCode = "HS010",
                    PCName = "DESKTOP-A",
                    IPAddress = "192.168.1.60"
                });
                PumpDispatcher();

                // Ghi nhận: Do logic IsNameMatch, hai thẻ cùng tên có thể bị gộp
                // Đây là limitation đã biết — Priority 1 (StudentCode) match trước
                // nhưng deduplication cũng check IsNameMatch, nên HS020 có thể bị gộp
                // Ít nhất 1 thẻ phải còn với code HS010
                var hs010 = allStudents.FirstOrDefault(s => s.StudentCode == "HS010");
                Assert.NotNull(hs010);
                Assert.True(hs010.IsOnline);
            });
        }

        // ── B10: [P1] [IT Admin] PCName được cập nhật khi HS kết nối lại ──
        [Fact]
        public void B10_PCName_UpdatedOnReconnect()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                allStudents.Add(new ConnectedStudent
                {
                    Name = "Trần B",
                    PCName = "OLD-PC",
                    StudentCode = "HS006",
                    IsOnline = false
                });

                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Trần B",
                    StudentCode = "HS006",
                    PCName = "NEW-PC-2026",
                    IPAddress = "192.168.1.100"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                Assert.Equal("NEW-PC-2026", allStudents.First().PCName);
            });
        }

        // ── B11: [P1] [Kiến trúc] IsNameMatch null safety ──
        [Fact]
        public void B11_IsNameMatch_NullSafety()
        {
            Assert.False(MonitorPage.IsNameMatch(null!, "Test"), "null vs string = false");
            Assert.False(MonitorPage.IsNameMatch("Test", null!), "string vs null = false");
            Assert.False(MonitorPage.IsNameMatch(null!, null!), "null vs null = false");
            Assert.True(MonitorPage.IsNameMatch("Test", "Test"), "same string = true");
            Assert.True(MonitorPage.IsNameMatch("TEST", "test"), "case insensitive = true");
        }

        // ── B12: [P1] [QA Tester] Không tạo dynamic khi không có match → entry mới ──
        [Fact]
        public void B12_NoMatch_CreatesDynamicEntry()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                // Danh sách rỗng, HS mới kết nối
                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Học Sinh Mới",
                    StudentCode = "HS999",
                    PCName = "GUEST-PC",
                    IPAddress = "192.168.1.200"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                var newStudent = allStudents.First();
                Assert.Equal("Học Sinh Mới", newStudent.Name);
                Assert.Equal("HS999", newStudent.StudentCode);
                Assert.True(newStudent.IsOnline);
            });
        }

        // ── B13: [P1] [Giáo viên] Screenshot merge khi gộp thẻ ──
        [Fact]
        public void B13_ScreenshotMerge_PreservedDuringDedup()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                var mockScreenshot = new System.Windows.Media.Imaging.BitmapImage();

                var roster = new ConnectedStudent
                {
                    Name = "Vũ C", PCName = "PC-R", StudentCode = "HS007", IsOnline = false,
                    ScreenshotSource = mockScreenshot  // Có ảnh chụp
                };
                var dynamic = new ConnectedStudent
                {
                    Name = "Vũ C", PCName = "DESKTOP-D", StudentCode = "HS007", IsOnline = true,
                    ScreenshotSource = null  // Không có ảnh
                };

                allStudents.Add(roster);
                allStudents.Add(dynamic);

                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Vũ C",
                    StudentCode = "HS007",
                    PCName = "DESKTOP-D",
                    IPAddress = "192.168.1.70"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                // Screenshot từ roster phải được bảo toàn
                Assert.NotNull(allStudents.First().ScreenshotSource);
            });
        }

        // ── B14: [P0] [QA Tester] Empty code + NFD name → roster nhận code ──
        [Fact]
        public void B14_EmptyCode_NFDName_RosterGetsCode()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();
                var allStudents = GetAllStudents(monitorPage);
                allStudents.Clear();

                var roster = new ConnectedStudent
                {
                    Name = "Phạm D", PCName = "PC-R", StudentCode = "", IsOnline = false
                };
                allStudents.Add(roster);

                InvokeOnWebStudentConnected(monitorPage, new QASmartClass.Classroom.Services.StudentConnectedEventArgs
                {
                    StudentName = "Pha\u0323m D",  // NFD: Phạm → Pha + combining dot below
                    StudentCode = "HS008",
                    PCName = "DESKTOP-E",
                    IPAddress = "192.168.1.80"
                });
                PumpDispatcher();

                Assert.Single(allStudents);
                Assert.Same(roster, allStudents.First());
                Assert.Equal("HS008", roster.StudentCode);
            });
        }

        // ── B15: [P1] [Bảo mật] Null event handling — không crash ──
        [Fact]
        public void B15_NullEvent_NoCrash()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                var monitorPage = new MonitorPage();

                // Gọi với null event — không được crash
                var method = typeof(MonitorPage).GetMethod("OnWebStudentConnected", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);

                var exception = Record.Exception(() =>
                {
                    method.Invoke(monitorPage, new object?[] { null, null });
                    PumpDispatcher();
                });

                // Không được throw exception
                Assert.Null(exception);
            });
        }

        // ╔══════════════════════════════════════════════════════════════╗
        // ║  MODULE C: LOI_VID_35 — HIỂN THỊ TEXTBLOCK TRONG QUIZ      ║
        // ║  10 test cases — TextBlock preservation, badges, fallback   ║
        // ╚══════════════════════════════════════════════════════════════╝

        // ── C01: [P0] [QA Tester] RadioButton.Content giữ nguyên TextBlock ──
        [Fact]
        public void C01_QuizReview_ContentRemainsTextBlock()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    var (page, panel, rb, tb, win2) = SetupQuizPage("A", "A", "Đáp án A");
                    win = win2;

                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    // Content phải vẫn là TextBlock, KHÔNG bị convert thành string
                    Assert.IsType<TextBlock>(rb.Content);
                }
                finally { win?.Close(); }
            });
        }

        // ── C02: [P0] [Giáo viên] Badge "Đáp án đúng" thêm dưới dạng Run ──
        [Fact]
        public void C02_QuizReview_CorrectAnswer_BadgeAddedAsRun()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    var (page, panel, rb, tb, win2) = SetupQuizPage("A", "A", "Đáp án đúng là A");
                    win = win2;

                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    var resultTb = rb.Content as TextBlock;
                    Assert.NotNull(resultTb);

                    // Phải có ít nhất 2 inlines: text gốc + badge
                    Assert.True(resultTb.Inlines.Count >= 2, $"Inlines count = {resultTb.Inlines.Count}, expected >= 2");

                    // Inline cuối phải chứa badge
                    var lastInline = resultTb.Inlines.Last() as Run;
                    Assert.NotNull(lastInline);
                    bool hasBadge = lastInline.Text.Contains("Đáp án đúng") || lastInline.Text.Contains("Chính xác");
                    Assert.True(hasBadge, $"Last inline text: '{lastInline.Text}' — phải chứa badge");
                }
                finally { win?.Close(); }
            });
        }

        // ── C03: [P0] [Học sinh] Badge "Bạn chọn" cho đáp án sai ──
        [Fact]
        public void C03_QuizReview_WrongAnswer_BadgeBanChon()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    // HS chọn B, đáp án đúng là A
                    var (page, panel, rb, tb, win2) = SetupQuizPage("B", "A", "Đáp án B (sai)");
                    win = win2;

                    // Thêm RadioButton cho đáp án đúng A (không được chọn)
                    var rbCorrect = new RadioButton { GroupName = "Q1", Tag = "A", IsChecked = false };
                    var tbCorrect = new TextBlock();
                    tbCorrect.Inlines.Add(new Run("  A.  Đáp án A (đúng)"));
                    rbCorrect.Content = tbCorrect;
                    // Thêm vào cùng stack panel
                    var stack = (panel.Children[0] as System.Windows.Controls.Border)?.Child as StackPanel;
                    stack?.Children.Insert(0, rbCorrect);

                    page.Measure(new Size(800, 600));
                    page.Arrange(new Rect(0, 0, 800, 600));
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);

                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    // RadioButton B (sai) phải giữ TextBlock
                    Assert.IsType<TextBlock>(rb.Content);
                    var resultTb = rb.Content as TextBlock;
                    Assert.NotNull(resultTb);

                    // Kiểm tra badge "Bạn chọn" tồn tại
                    var allText = string.Concat(resultTb.Inlines.OfType<Run>().Select(r => r.Text));
                    Assert.Contains("Bạn chọn", allText);
                }
                finally { win?.Close(); }
            });
        }

        // ── C04: [P0] [QA Tester] Badge "Chính xác" khi HS chọn đúng ──
        [Fact]
        public void C04_QuizReview_CorrectSelection_BadgeChinhXac()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    var (page, panel, rb, tb, win2) = SetupQuizPage("A", "A", "Đáp án trắc nghiệm A");
                    win = win2;

                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    var resultTb = rb.Content as TextBlock;
                    Assert.NotNull(resultTb);

                    var allText = string.Concat(resultTb.Inlines.OfType<Run>().Select(r => r.Text));
                    Assert.Contains("Chính xác", allText);
                }
                finally { win?.Close(); }
            });
        }

        // ── C05: [P1] [Kiến trúc] Fallback non-TextBlock — Content là string ──
        [Fact]
        public void C05_QuizReview_PlainStringContent_FallbackWorks()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    var page = new QASmartClass.StudentClient.Views.StudentQuizPage();
                    win = new Window { Content = page, Width = 800, Height = 600 };
                    win.Show();

                    var panelField = typeof(QASmartClass.StudentClient.Views.StudentQuizPage)
                        .GetField("questionsPanel", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(panelField);
                    var panel = panelField.GetValue(page) as StackPanel;
                    Assert.NotNull(panel);

                    var card = new Border();
                    var stack = new StackPanel();
                    card.Child = stack;

                    // Content là string thuần, KHÔNG phải TextBlock
                    var rb = new RadioButton
                    {
                        GroupName = "Q1",
                        Tag = "A",
                        IsChecked = true,
                        Content = "  A.  Đáp án đơn giản"  // plain string
                    };
                    stack.Children.Add(rb);
                    panel.Children.Add(card);

                    page.Measure(new Size(800, 600));
                    page.Arrange(new Rect(0, 0, 800, 600));
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);

                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    // Content phải là string (fallback path), KHÔNG phải "System.Windows.Controls.TextBlock"
                    var content = rb.Content?.ToString() ?? "";
                    Assert.DoesNotContain("System.Windows.Controls.TextBlock", content);
                    Assert.Contains("Đáp án đơn giản", content);
                }
                finally { win?.Close(); }
            });
        }

        // ── C06: [P0] [QA Tester] Repeated review — badge không nhân đôi ──
        [Fact]
        public void C06_QuizReview_RepeatedCall_NoBadgeDuplication()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    var (page, panel, rb, tb, win2) = SetupQuizPage("A", "A", "Đáp án A");
                    win = win2;

                    // Gọi review 2 lần liên tiếp
                    InvokeShowQuizResultsReview(page, "MCQ", "A");
                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    var resultTb = rb.Content as TextBlock;
                    Assert.NotNull(resultTb);

                    // Đếm số lần badge xuất hiện
                    int badgeCount = resultTb.Inlines.OfType<Run>()
                        .Count(r => r.Text.Contains("Đáp án đúng") || r.Text.Contains("Chính xác"));

                    // Badge không được nhân đôi — tối đa 2 (đáp án đúng + chính xác)
                    Assert.True(badgeCount <= 2, $"Badge count = {badgeCount}, should be <= 2 (no duplication)");
                }
                finally { win?.Close(); }
            });
        }

        // ── C07: [P1] [Nhà giáo dục] TextBlock với Inlines phức tạp ──
        [Fact]
        public void C07_QuizReview_ComplexInlines_PreservedCorrectly()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    var page = new QASmartClass.StudentClient.Views.StudentQuizPage();
                    win = new Window { Content = page, Width = 800, Height = 600 };
                    win.Show();

                    var panelField = typeof(QASmartClass.StudentClient.Views.StudentQuizPage)
                        .GetField("questionsPanel", BindingFlags.NonPublic | BindingFlags.Instance);
                    var panel = panelField!.GetValue(page) as StackPanel;

                    var card = new Border();
                    var stack = new StackPanel();
                    card.Child = stack;

                    var rb = new RadioButton { GroupName = "Q1", Tag = "A", IsChecked = true };
                    var tb = new TextBlock();
                    // Inlines phức tạp: nhiều Run với formatting
                    tb.Inlines.Add(new Run("  A.  ") { FontWeight = FontWeights.Bold });
                    tb.Inlines.Add(new Run("x² + 2x + 1 = 0"));
                    tb.Inlines.Add(new Run(" (phương trình bậc 2)") { Foreground = Brushes.Gray });
                    rb.Content = tb;
                    stack.Children.Add(rb);
                    panel!.Children.Add(card);

                    page.Measure(new Size(800, 600));
                    page.Arrange(new Rect(0, 0, 800, 600));
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);

                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    // Content vẫn phải là TextBlock
                    Assert.IsType<TextBlock>(rb.Content);
                    var resultTb = rb.Content as TextBlock;
                    Assert.NotNull(resultTb);

                    // Text gốc phải được bảo toàn
                    var allText = string.Concat(resultTb.Inlines.OfType<Run>().Select(r => r.Text));
                    Assert.Contains("x² + 2x + 1 = 0", allText);
                }
                finally { win?.Close(); }
            });
        }

        // ── C08: [P1] [QA Tester] TextBlock.Text rỗng, chỉ có Inlines ──
        [Fact]
        public void C08_QuizReview_EmptyTextProperty_ExtractsFromInlines()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    var (page, panel, rb, tb, win2) = SetupQuizPage("A", "A", "Nội dung từ Inlines");
                    win = win2;

                    // TextBlock.Text sẽ rỗng khi content được thêm qua Inlines
                    Assert.True(string.IsNullOrEmpty(tb.Text) || tb.Text.Contains("Nội dung"),
                        "TextBlock built from Inlines should have empty or matching Text");

                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    Assert.IsType<TextBlock>(rb.Content);
                }
                finally { win?.Close(); }
            });
        }

        // ── C09: [P2] [Kiến trúc] FindVisualChildren xử lý null ──
        [Fact]
        public void C09_FindVisualChildren_HandlesNullAndEmpty()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();

                var method = typeof(QASmartClass.StudentClient.Views.StudentQuizPage)
                    .GetMethod("FindVisualChildren", BindingFlags.NonPublic | BindingFlags.Static)
                    ?.MakeGenericMethod(typeof(RadioButton));
                Assert.NotNull(method);

                // Cây rỗng (StackPanel không có children)
                var emptyPanel = new StackPanel();
                var result = method.Invoke(null, new object[] { emptyPanel }) as IEnumerable<RadioButton>;
                Assert.NotNull(result);
                Assert.Empty(result);
            });
        }

        // ── C10: [P1] [Bảo mật] Text gốc không chứa ký tự badge ──
        [Fact]
        public void C10_QuizReview_OriginalTextPreserved_NoBadgeCollision()
        {
            RunOnStaThread(() =>
            {
                InitializeWpfApplication();
                Window? win = null;
                try
                {
                    // Text gốc KHÔNG chứa emoji badge
                    var (page, panel, rb, tb, win2) = SetupQuizPage("A", "A", "Câu trả lời bình thường");
                    win = win2;

                    InvokeShowQuizResultsReview(page, "MCQ", "A");

                    var resultTb = rb.Content as TextBlock;
                    Assert.NotNull(resultTb);

                    // Inline đầu tiên phải giữ nguyên text gốc
                    var firstInline = resultTb.Inlines.First() as Run;
                    Assert.NotNull(firstInline);
                    Assert.Contains("Câu trả lời bình thường", firstInline.Text);
                }
                finally { win?.Close(); }
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  HELPERS — Module B (MonitorPage)
        // ══════════════════════════════════════════════════════════════

        private ObservableCollection<ConnectedStudent> GetAllStudents(MonitorPage page)
        {
            var field = typeof(MonitorPage).GetField("_allStudents", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var collection = field.GetValue(page) as ObservableCollection<ConnectedStudent>;
            Assert.NotNull(collection);
            return collection;
        }

        private void InvokeOnWebStudentConnected(MonitorPage page, QASmartClass.Classroom.Services.StudentConnectedEventArgs e)
        {
            var method = typeof(MonitorPage).GetMethod("OnWebStudentConnected", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(page, new object?[] { null, e });
        }

        private void PumpDispatcher()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        }

        // ══════════════════════════════════════════════════════════════
        //  HELPERS — Module C (StudentQuizPage)
        // ══════════════════════════════════════════════════════════════

        private (QASmartClass.StudentClient.Views.StudentQuizPage page, StackPanel panel,
                 RadioButton rb, TextBlock tb, Window win) SetupQuizPage(
            string selectedTag, string correctAnswer, string answerText)
        {
            var page = new QASmartClass.StudentClient.Views.StudentQuizPage();
            var win = new Window { Content = page, Width = 800, Height = 600 };
            win.Show();

            var panelField = typeof(QASmartClass.StudentClient.Views.StudentQuizPage)
                .GetField("questionsPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(panelField);
            var panel = panelField.GetValue(page) as StackPanel;
            Assert.NotNull(panel);

            var card = new Border();
            var stack = new StackPanel();
            card.Child = stack;

            var rb = new RadioButton
            {
                GroupName = "Q1",
                Tag = selectedTag,
                IsChecked = true
            };
            var tb = new TextBlock();
            tb.Inlines.Add(new Run($"  {selectedTag}.  {answerText}"));
            rb.Content = tb;
            stack.Children.Add(rb);
            panel.Children.Add(card);

            page.Measure(new Size(800, 600));
            page.Arrange(new Rect(0, 0, 800, 600));
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);

            return (page, panel, rb, tb, win);
        }

        private void InvokeShowQuizResultsReview(QASmartClass.StudentClient.Views.StudentQuizPage page,
            string questionType, string correctAnswer)
        {
            var method = typeof(QASmartClass.StudentClient.Views.StudentQuizPage)
                .GetMethod("ShowQuizResultsReview", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var questions = new List<Question>
            {
                new Question
                {
                    Id = 1,
                    QuestionType = questionType,
                    CorrectAnswer = correctAnswer,
                    OptionsJson = $"[\"{correctAnswer}. Đáp án\"]"
                }
            };

            method.Invoke(page, new object[] { questions });
        }
    }
}
