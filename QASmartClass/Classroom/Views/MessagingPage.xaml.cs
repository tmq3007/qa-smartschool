using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Classroom.Services;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class MessagingPage : Page
    {
        // ─── Data ──────────────────────────────────────────
        public ObservableCollection<ChatMessage> Messages { get; } = new();
        public ObservableCollection<ChatMessage> FilteredMessages { get; } = new();
        private readonly ObservableCollection<ChatContact> _allContacts = new();
        private string _myName = "Giáo viên";
        private string _activeChannel = "ALL"; // ALL, GROUP_xxx, or student code
        private string _currentTab = "ALL";    // ALL, GROUP, PRIVATE
        private string _currentSort = "ONLINE"; // ONLINE, NAME, RECENT, UNREAD
        private bool _isClassChatMuted = false;
        private System.Collections.Generic.Dictionary<string, string> _studentFeedbacks = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool _isFeedbackAnonymous = false;
        private System.Collections.Generic.Dictionary<string, DateTime> _studentLastFeedbackTime = new System.Collections.Generic.Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        // ─── Group data ─────────────────────────────────────
        private class StudentGroup
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Emoji { get; set; } = "👥";
            public System.Collections.Generic.List<string> MemberCodes { get; set; } = new();
        }
        private System.Collections.Generic.List<StudentGroup> _groups = new();

        // ─── Color palette for avatars ─────────────────────
        private static readonly string[] AvatarColors =
        {
            "#1976D2", "#388E3C", "#F57C00", "#7B1FA2",
            "#C62828", "#00838F", "#AD1457", "#4527A0",
            "#2E7D32", "#E65100", "#1565C0", "#00695C"
        };

        private static readonly string[] GroupEmojis = { "📘", "📗", "📙", "📕", "🔬", "🎵", "⚽", "🖥️", "📐", "🌍" };


        private static readonly string[] BadWords = new string[] 
        { 
            "đm", "dm", "vcl", "vkl", "cl", "clm", "đéo", "cứt", "chó", "chó chết", "fuck", "ngu thế", "dốt thế" 
        };

        public static string SanitizeBadWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string sanitized = text;
            foreach (var word in BadWords)
            {
                string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(word) + @"\b";
                sanitized = System.Text.RegularExpressions.Regex.Replace(
                    sanitized, 
                    pattern, 
                    new string('*', word.Length), 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }
            return sanitized;
        }

        private void WriteLogBackground(string eventType, string actor, string details)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        db.EventLogs.Add(new EventLog
                        {
                            EventType = eventType,
                            Actor = actor,
                            Details = details,
                            Timestamp = DateTime.Now
                        });
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("WriteLogBackground error: {Err}", ex.Message);
                }
            });
        }

        public MessagingPage()
        {
            InitializeComponent();
            DataContext = this;

            // Load teacher name
            try
            {
                using (var db = new AppDbContext())
                {
                    var teacher = db.TeacherProfiles?.FirstOrDefault();
                    if (teacher != null)
                        _myName = teacher.FullName;
                }
            }
            catch { }

            // Init
            LoadGroups();
            BuildContactList();
            LoadChatHistoryFromDB();
            WireNetworkEvents();
            SelectChannel("ALL");

            // Subscribe to active roster changes
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.ClassRoster != null)
                {
                    app.ClassRoster.ActiveRosterChanged += OnActiveRosterChanged;
                }
            }
            catch { }

            this.Unloaded += (s, e) =>
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.ClassRoster != null)
                    {
                        app.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                    }
                }
                catch { }
            };
        }

        private void LoadGroups()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var log = db.EventLogs
                        .Where(e => e.EventType == "CHAT_GROUPS")
                        .OrderByDescending(e => e.Timestamp)
                        .FirstOrDefault();

                    if (log != null && !string.IsNullOrEmpty(log.Details))
                    {
                        _groups = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<StudentGroup>>(log.Details) ?? new();
                    }
                }
            }
            catch (Exception ex) { Log.Warning("LoadGroups error: {Err}", ex.Message); }
        }

        private void SaveGroups()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_groups);
                using (var db = new AppDbContext())
                {
                    var old = db.EventLogs.Where(e => e.EventType == "CHAT_GROUPS").ToList();
                    db.EventLogs.RemoveRange(old);
                    db.EventLogs.Add(new EventLog
                    {
                        EventType = "CHAT_GROUPS", Actor = "GV",
                        Details = json, Timestamp = DateTime.Now
                    });
                    db.SaveChanges();
                }
            }
            catch (Exception ex) { Log.Warning("SaveGroups error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════
        //  CONTACT LIST
        // ═══════════════════════════════════════════════════

        private void BuildContactList()
        {
            System.Threading.Tasks.Task.Run(() => BuildContactListInternal());
        }

        private void BuildContactListInternal()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var contactsList = new List<ChatContact>();

                using (var db = new AppDbContext())
                {
                    // 1. Calculate unread count for ALL
                    int unreadAll = 0;
                    try
                    {
                        var lastReadLogAll = db.EventLogs
                            .Where(e => e.EventType == "CHAT_READ" && e.Details == "ALL")
                            .OrderByDescending(e => e.Timestamp)
                            .FirstOrDefault();
                        DateTime lastReadAll = lastReadLogAll?.Timestamp ?? DateTime.MinValue;
                        DateTime cutoffAll = lastReadAll > DateTime.Now.AddDays(-7) ? lastReadAll : DateTime.Now.AddDays(-7);

                        unreadAll = db.EventLogs
                            .Count(e => e.EventType == "CHAT"
                                        && e.Actor != "GV"
                                        && e.Actor != _myName
                                        && e.Timestamp > cutoffAll
                                        && e.Details != null
                                        && e.Details.Contains("[CH:ALL]"));
                    }
                    catch { }

                    contactsList.Add(new ChatContact
                    {
                        Code = "ALL",
                        DisplayName = "👥 Cả lớp",
                        AvatarText = "🏫",
                        AvatarBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")),
                        IsGroup = true,
                        LastMessage = "Nhắn tin chung với toàn bộ lớp",
                        UnreadCount = unreadAll
                    });

                    // 2. Add custom chat groups
                    for (int g = 0; g < _groups.Count; g++)
                    {
                        var grp = _groups[g];
                        var color = AvatarColors[(g + 3) % AvatarColors.Length];

                        int unreadGrp = 0;
                        try
                        {
                            var lastReadLogGrp = db.EventLogs
                                .Where(e => e.EventType == "CHAT_READ" && e.Details == $"GROUP_{grp.Id}")
                                .OrderByDescending(e => e.Timestamp)
                                .FirstOrDefault();
                            DateTime lastReadGrp = lastReadLogGrp?.Timestamp ?? DateTime.MinValue;
                            DateTime cutoffGrp = lastReadGrp > DateTime.Now.AddDays(-7) ? lastReadGrp : DateTime.Now.AddDays(-7);

                            unreadGrp = db.EventLogs
                                .Count(e => e.EventType == "CHAT"
                                            && e.Actor != "GV"
                                            && e.Actor != _myName
                                            && e.Timestamp > cutoffGrp
                                            && e.Details != null
                                            && e.Details.Contains($"[CH:GROUP_{grp.Id}]"));
                        }
                        catch { }

                        contactsList.Add(new ChatContact
                        {
                            Code = $"GROUP_{grp.Id}",
                            DisplayName = $"{grp.Emoji} {grp.Name}",
                            AvatarText = grp.Emoji,
                            AvatarBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                            IsGroup = true,
                            LastMessage = $"💬 {grp.MemberCodes.Count} thành viên",
                            UnreadCount = unreadGrp
                        });
                    }

                    // 3. Load study groups from Nhóm Học Tập (GroupPage)
                    List<GroupVm> currentGroups = null;
                    Dispatcher.Invoke(() => currentGroups = app.CurrentGroups?.ToList());
                    if (currentGroups?.Count > 0)
                    {
                        foreach (var sg in currentGroups)
                        {
                            var sgCode = $"STUDY_{sg.Index}";
                            if (contactsList.Any(c => c.Code == sgCode)) continue;

                            int unreadSg = 0;
                            try
                            {
                                var lastReadLogSg = db.EventLogs
                                    .Where(e => e.EventType == "CHAT_READ" && e.Details == sgCode)
                                    .OrderByDescending(e => e.Timestamp)
                                    .FirstOrDefault();
                                DateTime lastReadSg = lastReadLogSg?.Timestamp ?? DateTime.MinValue;
                                DateTime cutoffSg = lastReadSg > DateTime.Now.AddDays(-7) ? lastReadSg : DateTime.Now.AddDays(-7);

                                unreadSg = db.EventLogs
                                    .Count(e => e.EventType == "CHAT"
                                                && e.Actor != "GV"
                                                && e.Actor != _myName
                                                && e.Timestamp > cutoffSg
                                                && e.Details != null
                                                && e.Details.Contains($"[CH:{sgCode}]"));
                            }
                            catch { }

                            var colorHex = $"#{sg.HeaderColor.R:X2}{sg.HeaderColor.G:X2}{sg.HeaderColor.B:X2}";
                            contactsList.Add(new ChatContact
                            {
                                Code = sgCode,
                                DisplayName = $"{sg.Emoji} {sg.Name}",
                                AvatarText = sg.Emoji,
                                AvatarBg = new SolidColorBrush(sg.HeaderColor),
                                IsGroup = true,
                                LastMessage = $"📚 Nhóm học tập · {sg.Members.Count} HS",
                                UnreadCount = unreadSg
                            });
                        }
                    }

                    // 4. Get active online students from network
                    var networkOnlineCodes = new System.Collections.Generic.HashSet<string>();
                    try
                    {
                        var net = app.NetworkService;
                        if (net != null)
                        {
                            foreach (var client in net.GetConnectedStudents())
                            {
                                networkOnlineCodes.Add(client.Code);
                            }
                        }
                    }
                    catch { }

                    // Sync DB with actual network status
                    var allDbStudents = db.Students.ToList();
                    foreach (var s in allDbStudents)
                    {
                        bool actuallyOnline = networkOnlineCodes.Contains(s.StudentCode ?? "");
                        if (s.IsOnline != actuallyOnline)
                        {
                            s.IsOnline = actuallyOnline;
                        }
                    }
                    db.SaveChanges();

                    // Get roster students
                    List<Student> students = null;
                    Dispatcher.Invoke(() =>
                    {
                        var roster = app.ClassRoster.ActiveRoster;
                        if (roster != null)
                        {
                            students = app.ClassRoster.GetActiveStudents();
                        }
                    });

                    if (students == null || !students.Any())
                    {
                        // Fallback: load all from local DB
                        students = db.Students.ToList();
                    }

                    // Build contact list from sorted roster students
                    var sortedStudents = VietnameseNameHelper.SortByVietnameseName(students, s => s.FullName);
                    int i = 0;
                    foreach (var s in sortedStudents)
                    {
                        var color = AvatarColors[i % AvatarColors.Length];
                        var initials = GetInitials(s.FullName);
                        var roleBadge = s.Status == "Graduated" ? "🎓" : (s.Role == "PH" ? "👪" : "");

                        var studentCode = s.StudentCode ?? "";
                        var lastMsg = db.EventLogs
                            .Where(e => (e.EventType == "CHAT" || e.EventType == "TEACHER_CHAT" || e.EventType == "PRIVATE_CHAT")
                                        && (e.Actor == studentCode || (e.Details != null && e.Details.Contains($"[CH:{studentCode}]")))
                                        && (e.Details != null && !e.Details.Contains("[CH:ALL]") 
                                            && !e.Details.Contains("[CH:GROUP_") && !e.Details.Contains("[CH:STUDY_")))
                            .OrderByDescending(e => e.Timestamp)
                            .FirstOrDefault();

                        var lastReadLog = db.EventLogs
                            .Where(e => e.EventType == "CHAT_READ" && e.Details == studentCode)
                            .OrderByDescending(e => e.Timestamp)
                            .FirstOrDefault();
                        DateTime lastReadTime = lastReadLog?.Timestamp ?? DateTime.MinValue;
                        DateTime cutoff = lastReadTime > DateTime.Now.AddDays(-7) ? lastReadTime : DateTime.Now.AddDays(-7);

                        int unread = db.EventLogs
                            .Count(e => (e.EventType == "PRIVATE_CHAT" || e.EventType == "CHAT")
                                        && e.Actor == studentCode
                                        && e.Timestamp > cutoff
                                        && e.Details != null
                                        && e.Details.Contains($"[CH:{studentCode}]"));

                        contactsList.Add(new ChatContact
                        {
                            Code = s.StudentCode ?? $"HS{s.Id:D5}",
                            DisplayName = s.ShortIdentity,
                            FullIdentity = s.ChatIdentity,
                            RoleBadge = roleBadge,
                            ClassName = s.ClassName,
                            SchoolName = s.SchoolName,
                            AvatarText = initials,
                            AvatarBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                            IsOnline = s.IsOnline,
                            IsGroup = false,
                            LastMessage = lastMsg?.Details?.Replace("Tin nhắn: ", "") ?? "",
                            LastTime = lastMsg?.Timestamp ?? DateTime.MinValue,
                            UnreadCount = unread
                        });
                        i++;
                    }

                    // Add network-only students (not in DB)
                    var existingCodes = contactsList.Select(c => c.Code).ToHashSet();
                    try
                    {
                        var net = app.NetworkService;
                        if (net != null)
                        {
                            foreach (var client in net.GetConnectedStudents())
                            {
                                if (!existingCodes.Contains(client.Code))
                                {
                                    var color = AvatarColors[(i++) % AvatarColors.Length];
                                    contactsList.Add(new ChatContact
                                    {
                                        Code = client.Code,
                                        DisplayName = client.Name,
                                        AvatarText = GetInitials(client.Name),
                                        AvatarBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                                        IsOnline = true,
                                        IsGroup = false,
                                        LastMessage = "",
                                        UnreadCount = 0
                                    });
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Update UI Collection and view filters on UI Thread
                Dispatcher.Invoke(() =>
                {
                    _allContacts.Clear();
                    foreach (var c in contactsList)
                    {
                        _allContacts.Add(c);
                    }

                    // Refresh online count
                    int onlineCount = _allContacts.Count(cc => cc.IsOnline && !cc.IsGroup);
                    int total = _allContacts.Count(cc => !cc.IsGroup);
                    txtOnlineCount.Text = $"{onlineCount} học sinh online · {total} tổng";

                    UpdateTabLabels();
                    FilterContacts();
                });
            }
            catch (Exception ex)
            {
                Log.Error("BuildContactList error: {Err}", ex.Message);
                Dispatcher.Invoke(() => txtOnlineCount.Text = "Không thể tải danh sách");
            }
        }

        private void FilterContacts()
        {
            var search = txtSearch?.Text?.Trim().ToLower() ?? "";
            var filtered = _allContacts.Where(c =>
            {
                // Tab filter
                if (_currentTab == "GROUP" && !c.IsGroup) return false;
                if (_currentTab == "PRIVATE" && c.IsGroup) return false;

                // Search filter — search in name, code, class, identity
                if (!string.IsNullOrEmpty(search))
                    return c.DisplayName.ToLower().Contains(search)
                        || c.Code.ToLower().Contains(search)
                        || c.ClassName.ToLower().Contains(search)
                        || c.FullIdentity.ToLower().Contains(search);

                return true;
            });

            // Apply sort
            IOrderedEnumerable<ChatContact> sorted = _currentSort switch
            {
                "ONLINE" => filtered.OrderByDescending(c => c.IsGroup)      // Groups first
                                    .ThenByDescending(c => c.IsOnline)      // Online next
                                    .ThenByDescending(c => c.UnreadCount)   // Unread next
                                    .ThenBy(c => c.DisplayName),
                "NAME" => filtered.OrderByDescending(c => c.IsGroup)
                                  .ThenBy(c => c.DisplayName),
                "RECENT" => filtered.OrderByDescending(c => c.IsGroup)
                                    .ThenByDescending(c => c.LastTime)
                                    .ThenBy(c => c.DisplayName),
                "UNREAD" => filtered.OrderByDescending(c => c.IsGroup)
                                    .ThenByDescending(c => c.UnreadCount)
                                    .ThenByDescending(c => c.IsOnline)
                                    .ThenBy(c => c.DisplayName),
                _ => filtered.OrderByDescending(c => c.IsGroup)
                             .ThenByDescending(c => c.IsOnline)
                             .ThenBy(c => c.DisplayName)
            };

            var newList = sorted.ToList();
            contactList.SelectionChanged -= Contact_SelectionChanged;
            contactList.ItemsSource = newList;

            if (!string.IsNullOrEmpty(_activeChannel))
            {
                var selectItem = newList.FirstOrDefault(c => c.Code == _activeChannel);
                if (selectItem != null)
                {
                    contactList.SelectedItem = selectItem;
                    contactList.ScrollIntoView(selectItem);
                }
            }
            contactList.SelectionChanged += Contact_SelectionChanged;
        }

        // ═══════════════════════════════════════════════════
        //  CHAT HISTORY FROM DB
        // ═══════════════════════════════════════════════════

        private void LoadChatHistoryFromDB()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    List<EventLog> chatEvents;
                    Dictionary<string, Student> studentMap;
                    using (var db = new AppDbContext())
                    {
                        chatEvents = db.EventLogs
                            .Where(e => e.EventType == "CHAT"
                                     || e.EventType == "TEACHER_CHAT"
                                     || e.EventType == "PRIVATE_CHAT"
                                     || e.EventType == "HAND_RAISE"
                                     || e.EventType == "STUDENT_QUESTION")
                            .OrderBy(e => e.Timestamp)
                            .ToList()
                            .TakeLast(100)
                            .ToList();

                        studentMap = db.Students.ToDictionary(s => s.StudentCode ?? "", s => s);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        Messages.Clear();

                        if (chatEvents.Any())
                        {
                            AddSystemMessage($"📜 {chatEvents.Count} tin nhắn từ lịch sử");

                            foreach (var ev in chatEvents)
                            {
                                bool isTeacher = ev.Actor == "GV" || ev.Actor == _myName || ev.EventType == "TEACHER_CHAT" || ev.EventType == "GROUP_CHAT";
                                string rawDetails = ev.Details;

                                // Parse channel tag from raw Details FIRST (e.g. [CH:GROUP_abc12345])
                                string channel = "ALL";
                                var chIdx = rawDetails.IndexOf("[CH:");
                                if (chIdx >= 0)
                                {
                                    var chEnd = rawDetails.IndexOf("]", chIdx);
                                    if (chEnd > chIdx)
                                        channel = rawDetails.Substring(chIdx + 4, chEnd - chIdx - 4);
                                }
                                else if (ev.EventType == "PRIVATE_CHAT")
                                {
                                    channel = ev.Actor ?? "ALL";
                                }

                                // Clean up text
                                string text = rawDetails;
                                if (text.StartsWith("Tin nhắn: "))
                                    text = text.Substring("Tin nhắn: ".Length);

                                // Remove [CH:xxx] tag from display text
                                var chTagIdx = text.IndexOf(" [CH:");
                                if (chTagIdx >= 0)
                                {
                                    var chTagEnd = text.IndexOf("]", chTagIdx);
                                    if (chTagEnd > chTagIdx)
                                        text = text.Substring(0, chTagIdx);
                                }

                                // Remove identity bracket from text (e.g. " [THPT QA_Lớp10A_HS_K46_...]")
                                var bracketIdx = text.LastIndexOf(" [");
                                if (bracketIdx > 0 && text.EndsWith("]"))
                                    text = text.Substring(0, bracketIdx);

                                // Resolve author name using student lookup
                                string author;
                                if (isTeacher)
                                {
                                    author = _myName;
                                }
                                else if (ev.Actor != null && studentMap.TryGetValue(ev.Actor, out var student))
                                {
                                    author = student.ShortIdentity;
                                }
                                else
                                {
                                    author = ev.Actor == "Student" ? "Học sinh" : (ev.Actor ?? "?");
                                }

                                Messages.Add(new ChatMessage
                                {
                                    Author = author,
                                    Text = text,
                                    Time = ev.Timestamp,
                                    IsTeacher = isTeacher,
                                    StudentCode = isTeacher ? "" : (ev.Actor ?? ""),
                                    AvatarText = isTeacher ? "GV" : GetInitials(author),
                                    AvatarBg = isTeacher ? Brushes.Transparent : GetBrushForCode(ev.Actor ?? ""),
                                    Channel = channel
                                });
                            }

                            RefreshAndScroll();
                        }
                        else
                        {
                            AddSystemMessage("💬 Chào mừng đến hệ thống tin nhắn QA SmartClass!");
                            AddSystemMessage("📌 Chọn \"Cả lớp\" để nhắn nhóm, hoặc chọn HS cụ thể để nhắn riêng.");
                        }
                    });
                }
                catch (Exception ex) { Log.Warning("LoadChatHistory error: {Err}", ex.Message); }
            });
        }

        // ═══════════════════════════════════════════════════
        //  NETWORK EVENTS (real-time)
        // ═══════════════════════════════════════════════════

        private void WireNetworkEvents()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var net = app.NetworkService;
                if (net == null)
                {
                    Log.Warning("[MessagingPage] NetworkService is null. Skipping message events registration.");
                    return;
                }

                net.MessageReceived += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var msg = args.Message;

                        // ═══ LOI_VID_30 FIX: MessageGuard whitelist filter ═══
                        // Áp dụng ràng buộc: NET-001 (tách kênh), SEC-001 (không rò rỉ),
                        // SP-002 (giao diện HS an toàn), PERF-001 (< 0.1ms/call).
                        // Whitelist: CHAT|, STUDENT_QUESTION|, HAND_RAISE|, STUDENT_FEEDBACK|
                        // Blacklist: HB_UPDATE|, HB_ACK, SCREENSHOT|, ACK|, ENC_CMD|, CMD|, ...
                        // Default: Block unknown → fail-safe.
                        var guard = QASmartClass.Utilities.MessageGuard.Evaluate(msg);
                        if (!guard.IsAllowedInChat)
                        {
                            if (guard.Category == QASmartClass.Utilities.MessageGuard.MessageCategory.Unknown)
                            {
                                Log.Warning("[LOI_VID_30] MessageGuard blocked unknown message type in chat: {MsgPreview}",
                                    msg?.Length > 80 ? msg.Substring(0, 80) + "..." : msg);
                            }
                            // HB_UPDATE, SCREENSHOT, ACK, ENC_CMD → silently drop (không spam log)
                            return;
                        }
                        // ═══ END LOI_VID_30 FIX ═══
                        
                        // Decrypt encrypted student question (from fallback mode)
                        if (msg.StartsWith("STUDENT_QUESTION_ENC|"))
                        {
                            try
                            {
                                var dbApp = (QASmartTouch.App)Application.Current;
                                string classCode = dbApp?.ClassroomSession?.ClassCode ?? "DEFAULT_CLASS";
                                if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";
                                
                                string encryptedText = msg.Replace("STUDENT_QUESTION_ENC|text=", "");
                                string decrypted = QASmartClass.Utilities.CryptoHelper.Decrypt(encryptedText, classCode);
                                if (!string.IsNullOrEmpty(decrypted))
                                {
                                    long ts = 0;
                                    bool hasTs = false;
                                    var parts = decrypted.Split('|');
                                    var tsPart = parts.FirstOrDefault(p => p.StartsWith("ts="));
                                    if (tsPart != null && long.TryParse(tsPart.Substring(3), out ts))
                                    {
                                        hasTs = true;
                                    }

                                    if (hasTs)
                                    {
                                        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                        if (Math.Abs(now - ts) > 10)
                                        {
                                            Log.Warning("Security alert: Rejecting drifted/replayed student question in MessagingPage from {StudentCode}. Drift: {Drift}s", args.StudentCode, now - ts);
                                            return;
                                        }
                                    }

                                    string questionText = decrypted;
                                    if (hasTs && tsPart != null)
                                    {
                                        int idx = decrypted.LastIndexOf("|" + tsPart);
                                        if (idx >= 0)
                                        {
                                            questionText = decrypted.Substring(0, idx);
                                        }
                                    }
                                    if (questionText.StartsWith("text="))
                                    {
                                        questionText = questionText.Substring(5);
                                    }
                                    msg = $"STUDENT_QUESTION|text={questionText}";
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("Failed to decrypt fallback question in MessagingPage: {Err}", ex.Message);
                            }
                        }

                        // Decrypt encrypted student hand raise (from fallback mode)
                        if (msg.StartsWith("HAND_RAISE_ENC|"))
                        {
                            try
                            {
                                var dbApp = (QASmartTouch.App)Application.Current;
                                string classCode = dbApp?.ClassroomSession?.ClassCode ?? "DEFAULT_CLASS";
                                if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";
                                
                                string encryptedText = msg.Replace("HAND_RAISE_ENC|payload=", "");
                                string decrypted = QASmartClass.Utilities.CryptoHelper.Decrypt(encryptedText, classCode);
                                if (!string.IsNullOrEmpty(decrypted))
                                {
                                    long ts = 0;
                                    bool hasTs = false;
                                    var parts = decrypted.Split('|');
                                    var tsPart = parts.FirstOrDefault(p => p.StartsWith("ts="));
                                    if (tsPart != null && long.TryParse(tsPart.Substring(3), out ts))
                                    {
                                        hasTs = true;
                                    }

                                    if (hasTs)
                                    {
                                        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                        if (Math.Abs(now - ts) > 10)
                                        {
                                            Log.Warning("Security alert: Rejecting drifted/replayed hand raise in MessagingPage from {StudentCode}. Drift: {Drift}s", args.StudentCode, now - ts);
                                            return;
                                        }
                                    }

                                    string cleanPayload = decrypted;
                                    if (hasTs && tsPart != null)
                                    {
                                        int idx = decrypted.LastIndexOf("|" + tsPart);
                                        if (idx >= 0)
                                        {
                                            cleanPayload = decrypted.Substring(0, idx);
                                        }
                                    }
                                    msg = $"HAND_RAISE|{cleanPayload}";
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning("Failed to decrypt fallback hand raise in MessagingPage: {Err}", ex.Message);
                            }
                        }

                        if (msg.StartsWith("STUDENT_FEEDBACK|"))
                        {
                            var parts = msg.Split('|');
                            if (parts.Length >= 3)
                            {
                                string studentCode = parts[1];
                                string emoji = parts[2];
                                HandleStudentFeedback(studentCode, emoji);
                            }
                            return;
                        }
                        string author = args.StudentCode;
                        string text = SanitizeBadWords(msg);
                        string channel = "ALL";

                        // Get name
                        var clients = net.GetConnectedStudents();
                        foreach (var c in clients)
                        {
                            if (c.Code == args.StudentCode)
                            { author = c.Name; break; }
                        }

                        // Parse: new format = CHAT|code|channel|text, old format = CHAT|code|text
                        if (msg.StartsWith("CHAT|"))
                        {
                            var p = msg.Split('|', 4);
                            if (p.Length >= 4)
                            {
                                // New format: CHAT|code|channel|text
                                string studentChannel = p[2];
                                text = SanitizeBadWords(p[3]);

                                // Map student channel to teacher channel:
                                // "ALL" → "ALL" (broadcast, show in Cả lớp)
                                // "TEACHER" → student's code (private, show in student's contact)
                                // "GROUP_xxx" → "GROUP_xxx" (group chat)
                                // "STUDY_xxx" → "STUDY_xxx" (study group)
                                if (studentChannel == "ALL")
                                    channel = "ALL";
                                else if (studentChannel == "TEACHER")
                                    channel = args.StudentCode; // Private → route to student contact
                                else if (studentChannel.StartsWith("GROUP_") || studentChannel.StartsWith("STUDY_"))
                                    channel = studentChannel; // Group → keep as-is
                                else
                                    channel = args.StudentCode; // Fallback to private
                            }
                            else if (p.Length >= 3)
                            {
                                // Old format: CHAT|code|text (backward compat)
                                text = SanitizeBadWords(p[2]);
                                channel = args.StudentCode;
                            }
                        }
                        else if (msg.StartsWith("STUDENT_QUESTION|"))
                        {
                            text = "❓ " + SanitizeBadWords(msg.Replace("STUDENT_QUESTION|text=", ""));
                            channel = args.StudentCode;
                        }
                        else if (msg.StartsWith("HAND_RAISE|"))
                        {
                            bool raised = msg.Contains("raised=True");
                            string reason = "";
                            if (msg.Contains("reason="))
                            {
                                var reasonPart = msg.Split('|').FirstOrDefault(p => p.StartsWith("reason="));
                                if (reasonPart != null)
                                {
                                    reason = reasonPart.Replace("reason=", "").Trim();
                                }
                            }

                            if (raised)
                            {
                                string reasonDetail = !string.IsNullOrEmpty(reason) ? $" ({reason})" : "";
                                text = $"🖐️ Giơ tay xin phát biểu{reasonDetail}";
                            }
                            else
                            {
                                text = "✋ Đã hạ tay";
                            }
                            channel = args.StudentCode;
                        }

                        var chatMsg = new ChatMessage
                        {
                            Author = author,
                            Text = text,
                            Time = DateTime.Now,
                            IsTeacher = false,
                            StudentCode = args.StudentCode,
                            AvatarText = GetInitials(author),
                            AvatarBg = GetBrushForCode(args.StudentCode),
                            Channel = channel
                        };

                        Messages.Add(chatMsg);
                        RefreshAndScroll();

                        // Save student message to DB in background thread
                        WriteLogBackground("CHAT", args.StudentCode, $"Tin nhắn: {text} [CH:{channel}]");

                        // Update contact/channel last message + unread
                        bool shouldPlaySound = false;
                        if (channel == "ALL")
                        {
                            // Broadcast → update "Cả lớp" contact
                            var allContact = _allContacts.FirstOrDefault(c => c.Code == "ALL");
                            if (allContact != null)
                            {
                                allContact.LastMessage = $"{author}: {(text.Length > 25 ? text.Substring(0, 25) + "..." : text)}";
                                allContact.LastTime = DateTime.Now;
                                if (_activeChannel != "ALL")
                                {
                                    allContact.UnreadCount++;
                                    shouldPlaySound = true;
                                }
                            }
                        }
                        else if (channel.StartsWith("GROUP_") || channel.StartsWith("STUDY_"))
                        {
                            // Group/Study → update the GROUP/STUDY contact, NOT the student contact
                            var groupContact = _allContacts.FirstOrDefault(c => c.Code == channel);
                            if (groupContact != null)
                            {
                                groupContact.LastMessage = $"{author}: {(text.Length > 20 ? text.Substring(0, 20) + "..." : text)}";
                                groupContact.LastTime = DateTime.Now;
                                if (_activeChannel != channel)
                                {
                                    groupContact.UnreadCount++;
                                    shouldPlaySound = true;
                                }
                            }
                        }
                        else
                        {
                            // Private → update student contact
                            var contact = _allContacts.FirstOrDefault(c => c.Code == channel);
                            if (contact != null)
                            {
                                contact.LastMessage = text.Length > 30 ? text.Substring(0, 30) + "..." : text;
                                contact.LastTime = DateTime.Now;
                                if (_activeChannel != channel)
                                {
                                    contact.UnreadCount++;
                                    shouldPlaySound = true;
                                }
                            }
                        }

                        if (shouldPlaySound)
                        {
                            try
                            {
                                System.Media.SystemSounds.Asterisk.Play();
                            }
                            catch { }
                        }

                        Log.Information("MSG from {Student}: {Text}", author, text);
                        UpdateTabLabels();
                        FilterContacts();
                    });
                };

                net.StudentConnected += (s, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddSystemMessage($"🟢 {args.StudentName} ({args.StudentCode}) đã kết nối");

                        // Update DB
                        try
                        {
                            var a = (QASmartTouch.App)Application.Current;
                            using (var db = new AppDbContext())
                            {
                                var stu = db.Students.FirstOrDefault(st => st.StudentCode == args.StudentCode);
                                if (stu != null)
                                {
                                    stu.IsOnline = true;
                                    stu.LastSeen = DateTime.Now;
                                    db.SaveChanges();
                                }
                            }

                            // Send Mute status to newly connected student
                            _ = a.NetworkService?.SendToStudentAsync(args.StudentCode, $"CMD|CLASS_CHAT_MUTE|{_isClassChatMuted}");
                            
                            // Send Anonymous feedback status to newly connected student
                            _ = a.NetworkService?.SendToStudentAsync(args.StudentCode, $"CMD|CLASS_CHAT_ANONYMOUS|{_isFeedbackAnonymous}");

                            // Send recent message history to newly connected student
                            var recentMsgs = Messages
                                .Where(m => m.IsTeacher && m.Channel == "ALL")
                                .TakeLast(20)
                                .ToList();

                            if (recentMsgs.Count > 0)
                            {
                                foreach (var histMsg in recentMsgs)
                                {
                                    _ = a.NetworkService?.BroadcastMessage(
                                        $"MSG_HISTORY|{histMsg.Time:HH:mm}|{histMsg.Text}");
                                }
                                Log.Information("Sent {Count} history messages to {Student}", recentMsgs.Count, args.StudentCode);
                            }
                        }
                        catch { }

                        var c = _allContacts.FirstOrDefault(cc => cc.Code == args.StudentCode);
                        if (c != null)
                        {
                            c.IsOnline = true;
                            c.LastMessage = $"🟢 Online — {args.IPAddress}";

                            // Refresh online count
                            int onlineCount = _allContacts.Count(cc => cc.IsOnline && !cc.IsGroup);
                            int total = _allContacts.Count(cc => !cc.IsGroup);
                            txtOnlineCount.Text = $"{onlineCount} học sinh online · {total} tổng";
                            FilterContacts();
                        }
                        else
                        {
                            BuildContactList(); // Rebuild only if transient student
                        }
                    });
                };

                net.StudentDisconnected += (s, code) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddSystemMessage($"🔴 HS {code} đã ngắt kết nối");

                        // Update contact in memory
                        var c = _allContacts.FirstOrDefault(c => c.Code == code);
                        if (c != null) c.IsOnline = false;

                        // Update DB
                        try
                        {
                            using (var db = new AppDbContext())
                            {
                                var stu = db.Students.FirstOrDefault(st => st.StudentCode == code);
                                if (stu != null)
                                {
                                    stu.IsOnline = false;
                                    stu.LastSeen = DateTime.Now;
                                    db.SaveChanges();
                                }
                            }
                        }
                        catch { }

                        // Refresh online count
                        int onlineCount = _allContacts.Count(cc => cc.IsOnline && !cc.IsGroup);
                        int total = _allContacts.Count(cc => !cc.IsGroup);
                        txtOnlineCount.Text = $"{onlineCount} học sinh online · {total} tổng";
                        FilterContacts();
                    });
                };
            }
            catch (Exception ex) { Log.Warning("Wire network error: {Err}", ex.Message); }
        }

        private void MarkChannelAsRead(string channelCode)
        {
            var contact = _allContacts.FirstOrDefault(c => c.Code == channelCode);
            if (contact != null)
            {
                contact.UnreadCount = 0;
                UpdateTabLabels();
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        var oldLogs = db.EventLogs
                            .Where(e => e.EventType == "CHAT_READ" && e.Details == channelCode)
                            .ToList();
                        if (oldLogs.Count > 0)
                        {
                            db.EventLogs.RemoveRange(oldLogs);
                        }

                        db.EventLogs.Add(new EventLog
                        {
                            EventType = "CHAT_READ",
                            Actor = "GV",
                            Details = channelCode,
                            Timestamp = DateTime.Now
                        });
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("MarkChannelAsRead error: {Err}", ex.Message);
                }
            });
        }

        private void OnActiveRosterChanged(object? sender, Data.ClassRoster? roster)
        {
            Dispatcher.Invoke(() =>
            {
                BuildContactList();
                SelectChannel("ALL");
            });
        }

        private void SelectChannel(string channelCode)
        {
            _activeChannel = channelCode;

            MarkChannelAsRead(channelCode);

            if (channelCode == "ALL")
            {
                chatAvatarText.Text = "👥";
                chatAvatar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
                txtChatTitle.Text = "Cả lớp";
                txtChatSubtitle.Text = "Nhắn tin nhóm với toàn bộ học sinh";
            }
            else if (channelCode.StartsWith("GROUP_"))
            {
                var groupId = channelCode.Replace("GROUP_", "");
                var grp = _groups.FirstOrDefault(g => g.Id == groupId);
                if (grp != null)
                {
                    chatAvatarText.Text = grp.Emoji;
                    chatAvatar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#388E3C"));
                    txtChatTitle.Text = grp.Name;

                    // Get member names
                    var memberNames = new System.Collections.Generic.List<string>();
                    foreach (var code in grp.MemberCodes)
                    {
                        var contact = _allContacts.FirstOrDefault(c => c.Code == code);
                        memberNames.Add(contact?.DisplayName ?? code);
                    }
                    txtChatSubtitle.Text = $"👥 {grp.MemberCodes.Count} thành viên: {string.Join(", ", memberNames.Take(5))}{(memberNames.Count > 5 ? "..." : "")}";
                }
            }
            else if (channelCode.StartsWith("STUDY_"))
            {
                // Study group from Nhóm Học Tập
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (int.TryParse(channelCode.Replace("STUDY_", ""), out int sIdx)
                        && app.CurrentGroups?.Count > sIdx)
                    {
                        var sg = app.CurrentGroups[sIdx];
                        chatAvatarText.Text = sg.Emoji;
                        chatAvatar.Background = new SolidColorBrush(sg.HeaderColor);
                        txtChatTitle.Text = $"{sg.Emoji} {sg.Name}";
                        var names = sg.Members.Take(5);
                        txtChatSubtitle.Text = $"📚 Nhóm học tập · {sg.Members.Count} HS: {string.Join(", ", names)}{(sg.Members.Count > 5 ? "..." : "")}";
                    }
                }
                catch { }
            }
            else
            {
                var contact = _allContacts.FirstOrDefault(c => c.Code == channelCode);
                if (contact != null)
                {
                    chatAvatarText.Text = contact.AvatarText;
                    chatAvatar.Background = contact.AvatarBg;
                    txtChatTitle.Text = contact.DisplayName;

                    var status = contact.IsOnline ? "🟢 Online" : "⚪ Offline";
                    var identity = string.IsNullOrEmpty(contact.FullIdentity) ? "" : $" · {contact.FullIdentity}";
                    txtChatSubtitle.Text = $"{status}{identity}";

                    contact.UnreadCount = 0;
                    UpdateTabLabels();
                }
            }

            FilterMessagesForChannel();
        }

        private void FilterMessagesForChannel()
        {
            FilteredMessages.Clear();
            var filtered = new System.Collections.Generic.List<ChatMessage>();

            // Get study group member codes (for STUDY_ channels)
            var studyMemberNames = new System.Collections.Generic.HashSet<string>();
            if (_activeChannel.StartsWith("STUDY_"))
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (int.TryParse(_activeChannel.Replace("STUDY_", ""), out int sIdx)
                        && app.CurrentGroups?.Count > sIdx)
                    {
                        foreach (var name in app.CurrentGroups[sIdx].Members)
                            studyMemberNames.Add(name);
                    }
                }
                catch { }
            }

            // Get custom group member codes (for GROUP_ channels)
            var groupMemberCodes = new System.Collections.Generic.HashSet<string>();
            if (_activeChannel.StartsWith("GROUP_"))
            {
                var grpId = _activeChannel.Replace("GROUP_", "");
                var grp = _groups.FirstOrDefault(g => g.Id == grpId);
                if (grp != null)
                    foreach (var code in grp.MemberCodes)
                        groupMemberCodes.Add(code);
            }

            foreach (var msg in Messages)
            {
                bool include = false;

                if (_activeChannel == "ALL")
                {
                    // "Cả lớp" shows: teacher broadcast (channel=ALL), system msgs, student msgs sent to ALL
                    include = msg.IsSystem
                        || msg.Channel == "ALL";
                }
                else if (_activeChannel.StartsWith("GROUP_"))
                {
                    // Custom group: show teacher msgs to this group + student msgs from group members sent to this group
                    include = msg.Channel == _activeChannel;
                }
                else if (_activeChannel.StartsWith("STUDY_"))
                {
                    // Study group: show teacher msgs to this group + student msgs from study group members
                    include = msg.Channel == _activeChannel;
                }
                else
                {
                    // Private: show teacher msgs sent to this student + student's msgs explicitly to private
                    // Channel must match the student code (private channel), NOT just StudentCode
                    include = (msg.IsTeacher && msg.Channel == _activeChannel)
                        || (!msg.IsTeacher && !msg.IsSystem && msg.StudentCode == _activeChannel && msg.Channel == _activeChannel);
                }

                if (include)
                    filtered.Add(msg);
            }

            // Insert date separators between different days
            DateTime? lastDate = null;
            foreach (var msg in filtered)
            {
                if (msg.IsSystem) { FilteredMessages.Add(msg); continue; }

                var msgDate = msg.Time.Date;
                if (lastDate == null || msgDate != lastDate.Value)
                {
                    string dateLabel;
                    if (msgDate == DateTime.Today) dateLabel = "Hôm nay";
                    else if (msgDate == DateTime.Today.AddDays(-1)) dateLabel = "Hôm qua";
                    else dateLabel = msgDate.ToString("ddd, dd/MM/yyyy");

                    FilteredMessages.Add(new ChatMessage
                    {
                        Text = dateLabel,
                        Time = msg.Time,
                        IsDateSeparator = true
                    });
                    lastDate = msgDate;
                }
                FilteredMessages.Add(msg);
            }

            // Show placeholder if empty
            if (FilteredMessages.Count == 0)
            {
                string channelName = _activeChannel;
                var contact = _allContacts.FirstOrDefault(c => c.Code == _activeChannel);
                if (contact != null) channelName = contact.DisplayName;

                FilteredMessages.Add(new ChatMessage
                {
                    Author = "⚙️ Hệ thống",
                    Text = $"💬 Chưa có tin nhắn nào trong kênh \"{channelName}\". Hãy gửi tin nhắn đầu tiên!",
                    Time = DateTime.Now, IsSystem = true
                });
            }

            // Update stats in sub-header
            int msgCount = filtered.Count(m => !m.IsSystem);
            try
            {
                txtChannelStats.Text = $"{msgCount} tin nhắn";

                // Update member count
                if (_activeChannel == "ALL")
                {
                    int studentTotal = _allContacts.Count(c => !c.IsGroup);
                    txtTabMembers.Text = $"👥 {studentTotal} thành viên";
                }
                else if (_activeChannel.StartsWith("GROUP_"))
                {
                    var grpId = _activeChannel.Replace("GROUP_", "");
                    var grp = _groups.FirstOrDefault(g => g.Id == grpId);
                    txtTabMembers.Text = $"👥 {grp?.MemberCodes.Count ?? 0} thành viên";
                }
                else if (_activeChannel.StartsWith("STUDY_"))
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (int.TryParse(_activeChannel.Replace("STUDY_", ""), out int sIdx)
                        && app.CurrentGroups?.Count > sIdx)
                        txtTabMembers.Text = $"👥 {app.CurrentGroups[sIdx].Members.Count} thành viên";
                }
                else
                {
                    txtTabMembers.Text = $"👤 1 thành viên";
                }
            }
            catch { }

            // Scroll to end
            if (chatList.Items.Count > 0)
                chatList.ScrollIntoView(chatList.Items[chatList.Items.Count - 1]);
        }

        private void Contact_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (contactList.SelectedItem is ChatContact contact)
            {
                SelectChannel(contact.Code);
            }
        }

        // ═══════════════════════════════════════════════════
        //  TAB SWITCHING
        // ═══════════════════════════════════════════════════

        private void TabAll_Click(object sender, RoutedEventArgs e) { SetTab("ALL"); }
        private void TabGroup_Click(object sender, RoutedEventArgs e) { SetTab("GROUP"); }
        private void TabPrivate_Click(object sender, RoutedEventArgs e) { SetTab("PRIVATE"); }

        private void SetTab(string tab)
        {
            _currentTab = tab;

            // Update tab visuals
            var activeBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
            var inactiveBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            var activeFg = Brushes.White;
            var inactiveFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161"));

            tabAll.Background = tab == "ALL" ? activeBg : inactiveBg;
            tabAll.Foreground = tab == "ALL" ? activeFg : inactiveFg;
            tabGroup.Background = tab == "GROUP" ? activeBg : inactiveBg;
            tabGroup.Foreground = tab == "GROUP" ? activeFg : inactiveFg;
            tabPrivate.Background = tab == "PRIVATE" ? activeBg : inactiveBg;
            tabPrivate.Foreground = tab == "PRIVATE" ? activeFg : inactiveFg;

            FilterContacts();
        }

        // ═══════════════════════════════════════════════════
        //  SORT HANDLERS
        // ═══════════════════════════════════════════════════

        private void SortOnline_Click(object sender, RoutedEventArgs e) { SetSort("ONLINE"); }
        private void SortName_Click(object sender, RoutedEventArgs e) { SetSort("NAME"); }
        private void SortRecent_Click(object sender, RoutedEventArgs e) { SetSort("RECENT"); }
        private void SortUnread_Click(object sender, RoutedEventArgs e) { SetSort("UNREAD"); }

        private void SetSort(string sort)
        {
            _currentSort = sort;

            // Update sort button visuals
            var activeOnlineBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9"));
            var activeFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
            var activeBlueBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD"));
            var activeBlueFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0"));
            var activeRedBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE"));
            var activeRedFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
            var inactiveBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5"));
            var inactiveFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161"));

            sortOnline.Background = sort == "ONLINE" ? activeOnlineBg : inactiveBg;
            sortOnline.Foreground = sort == "ONLINE" ? activeFg : inactiveFg;
            sortName.Background = sort == "NAME" ? activeBlueBg : inactiveBg;
            sortName.Foreground = sort == "NAME" ? activeBlueFg : inactiveFg;
            sortRecent.Background = sort == "RECENT" ? activeBlueBg : inactiveBg;
            sortRecent.Foreground = sort == "RECENT" ? activeBlueFg : inactiveFg;
            sortUnread.Background = sort == "UNREAD" ? activeRedBg : inactiveBg;
            sortUnread.Foreground = sort == "UNREAD" ? activeRedFg : inactiveFg;

            FilterContacts();
        }

        // ═══════════════════════════════════════════════════
        //  SEND MESSAGE
        // ═══════════════════════════════════════════════════

        private void Send_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.None)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (inputPlaceholder != null)
                inputPlaceholder.Visibility = string.IsNullOrEmpty(txtInput.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchPlaceholder != null)
                searchPlaceholder.Visibility = string.IsNullOrEmpty(txtSearch.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
            FilterContacts();
        }

        // ═══════════════════════════════════════════════════
        //  CHAT INPUT BAR — Icon feature handlers
        // ═══════════════════════════════════════════════════

        private void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Đính kèm tệp",
                Filter = "Tất cả tệp (*.*)|*.*|Tài liệu (*.pdf;*.docx;*.xlsx;*.pptx)|*.pdf;*.docx;*.xlsx;*.pptx|Ảnh (*.png;*.jpg;*.gif)|*.png;*.jpg;*.gif",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                string fileName = System.IO.Path.GetFileName(dlg.FileName);
                txtInput.Text = $"📄 [File: {fileName}]";
                txtInput.Focus();
                txtInput.CaretIndex = txtInput.Text.Length;
            }
        }

        private void AttachMedia_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Đính kèm ảnh / media",
                Filter = "Ảnh & Video (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.mp3)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.mp3|Tất cả (*.*)|*.*",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                string fileName = System.IO.Path.GetFileName(dlg.FileName);
                string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                string icon = ext switch
                {
                    ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "🖼️",
                    ".mp4" or ".avi" or ".mov" => "🎬",
                    ".mp3" or ".wav" or ".ogg" => "🎵",
                    _ => "📎"
                };
                txtInput.Text = $"{icon} [Media: {fileName}]";
                txtInput.Focus();
                txtInput.CaretIndex = txtInput.Text.Length;
            }
        }

        private void QuickLike_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = "👍";
            SendMessage();
        }

        private System.Windows.Controls.Primitives.Popup? _emojiPopup;

        private void ShowEmoji_Click(object sender, RoutedEventArgs e)
        {
            if (_emojiPopup != null && _emojiPopup.IsOpen)
            { _emojiPopup.IsOpen = false; return; }

            _emojiPopup = new System.Windows.Controls.Primitives.Popup
            {
                StaysOpen = false,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                PlacementTarget = sender as Button,
                AllowsTransparency = true
            };

            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Width = 350,
                MaxHeight = 300,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12, Opacity = 0.15, ShadowDepth = 3
                }
            };

            var mainStack = new StackPanel();

            // Header
            mainStack.Children.Add(new TextBlock
            {
                Text = "😊 Biểu cảm nhanh",
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                Margin = new Thickness(4, 0, 0, 6)
            });

            // Category tabs
            var categories = new (string label, string[] emojis)[]
            {
                ("😊 Mặt",  new[] { "😀","😊","😂","🤣","😍","🥰","😎","🤗","🤔","😮","😢","😡","🥺","😱","🤮","😴","🤯","🥳","😏","🤓","😇","🤤","😬","🫡","🫣" }),
                ("👋 Tay",  new[] { "👍","👎","👋","✌️","🤞","👏","🙌","🤝","💪","☝️","👆","👇","👈","👉","🤟","🫶","✋","🤚","🖐️","🖖","🤙","✊","👊","🤜","🤛" }),
                ("🐱 Thú",  new[] { "🐶","🐱","🐭","🐰","🦊","🐻","🐼","🐻‍❄️","🐨","🐯","🦁","🐮","🐷","🐸","🐵","🐔","🦄","🐝","🐛","🦋","🐌","🐞","🐜","🪲","🐟" }),
                ("🍕 Đồ ăn", new[] { "🍕","🍔","🍟","🌭","🍿","🧁","🍰","🍩","🍪","🍫","🍬","🍭","🍮","☕","🧃","🥤","🧋","🍎","🍊","🍇","🍓","🍑","🥝","🍌","🍉" }),
                ("⭐ Biểu tượng", new[] { "❤️","🧡","💛","💚","💙","💜","🖤","💔","💯","✨","🌟","⭐","🔥","💥","💫","🎉","🎊","🎁","🏆","🥇","💎","🔔","💡","📌","🎯" })
            };

            var tabPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            var emojiGrid = new WrapPanel();

            void ShowCategory(int idx)
            {
                emojiGrid.Children.Clear();
                foreach (var em in categories[idx].emojis)
                {
                    var btn = new Button
                    {
                        Content = em, FontSize = 20, Width = 36, Height = 36,
                        FontFamily = new FontFamily("Segoe UI Emoji"),
                        Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand, Padding = new Thickness(0),
                        ToolTip = em
                    };
                    btn.Click += (_, __) =>
                    {
                        txtInput.Text += em;
                        txtInput.Focus();
                        txtInput.CaretIndex = txtInput.Text.Length;
                        _emojiPopup.IsOpen = false;
                    };
                    emojiGrid.Children.Add(btn);
                }
            }

            for (int i = 0; i < categories.Length; i++)
            {
                int idx = i;
                var tab = new Button
                {
                    Content = categories[i].label, FontSize = 10.5,
                    Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 3, 0),
                    Background = i == 0 ? new SolidColorBrush(Color.FromRgb(227, 242, 253))
                                        : new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    Foreground = i == 0 ? new SolidColorBrush(Color.FromRgb(25, 118, 210))
                                        : new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                    BorderThickness = new Thickness(0), Cursor = Cursors.Hand
                };
                tab.Click += (_, __) =>
                {
                    foreach (Button b in tabPanel.Children) {
                        b.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                        b.Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97));
                    }
                    tab.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    tab.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                    ShowCategory(idx);
                };
                tabPanel.Children.Add(tab);
            }

            mainStack.Children.Add(tabPanel);
            ShowCategory(0);

            var scroll = new ScrollViewer { MaxHeight = 200, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            scroll.Content = emojiGrid;
            mainStack.Children.Add(scroll);

            border.Child = mainStack;
            _emojiPopup.Child = border;
            _emojiPopup.IsOpen = true;
        }

        private void ShowSticker_Click(object sender, RoutedEventArgs e)
        {
            if (_emojiPopup != null && _emojiPopup.IsOpen)
            { _emojiPopup.IsOpen = false; return; }

            _emojiPopup = new System.Windows.Controls.Primitives.Popup
            {
                StaysOpen = false,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                PlacementTarget = sender as Button,
                AllowsTransparency = true
            };

            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Width = 300,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12, Opacity = 0.15, ShadowDepth = 3
                }
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = "😃 Sticker nhanh",
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                Margin = new Thickness(4, 0, 0, 8)
            });

            // Sticker groups
            var stickers = new (string label, string[] items)[]
            {
                ("Chào hỏi", new[] { "👋😊", "🙋‍♂️", "🙋‍♀️", "🤗", "🫡👋", "✋😄" }),
                ("Tuyệt vời", new[] { "🎉🥳", "💯🔥", "⭐✨", "🏆👏", "💪😎", "🚀🌟" }),
                ("Đáng yêu", new[] { "🐱💕", "🐶❤️", "🐰🌸", "🦊💛", "🐻🧡", "🐼💚" }),
                ("Học tập", new[] { "📚✍️", "🎓📝", "💡🤔", "📖👀", "🧠💫", "✅👍" }),
                ("Cảm xúc", new[] { "😂🤣", "😭💔", "😱😨", "😤🔥", "🥺🥹", "😴💤" })
            };

            var wrap = new WrapPanel();
            foreach (var group in stickers)
            {
                // Group label
                var groupStack = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
                groupStack.Children.Add(new TextBlock
                {
                    Text = group.label, FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    Margin = new Thickness(2, 4, 0, 2)
                });

                var groupWrap = new WrapPanel();
                foreach (var st in group.items)
                {
                    var sticker = st;
                    var btn = new Button
                    {
                        Content = sticker, FontSize = 22,
                        FontFamily = new FontFamily("Segoe UI Emoji"),
                        Width = 44, Height = 38,
                        Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                        BorderThickness = new Thickness(0),
                        Cursor = Cursors.Hand, Margin = new Thickness(1)
                    };
                    btn.Click += (_, __) =>
                    {
                        txtInput.Text = sticker;
                        SendMessage();
                        _emojiPopup.IsOpen = false;
                    };
                    groupWrap.Children.Add(btn);
                }
                groupStack.Children.Add(groupWrap);
                stack.Children.Add(groupStack);
            }

            border.Child = stack;
            _emojiPopup.Child = border;
            _emojiPopup.IsOpen = true;
        }

        private void VoiceRecord_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "🎤 Tính năng Ghi âm giọng nói sẽ được cập nhật trong phiên bản tiếp theo.\n\n" +
                "Bạn có thể:\n" +
                "• Gõ tin nhắn text\n" +
                "• Gửi biểu cảm 😊\n" +
                "• Đính kèm file 📎",
                "🎤 Ghi âm — Sắp ra mắt",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SendMessage()
        {
            var text = txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Determine display label
            string channelLabel = _activeChannel;
            if (_activeChannel.StartsWith("GROUP_"))
            {
                var grpId = _activeChannel.Replace("GROUP_", "");
                var grp = _groups.FirstOrDefault(g => g.Id == grpId);
                channelLabel = grp?.Name ?? _activeChannel;
            }
            else if (_activeChannel.StartsWith("STUDY_"))
            {
                try
                {
                    var a = (QASmartTouch.App)Application.Current;
                    if (int.TryParse(_activeChannel.Replace("STUDY_", ""), out int sIdx)
                        && a.CurrentGroups?.Count > sIdx)
                        channelLabel = a.CurrentGroups[sIdx].Name;
                }
                catch { }
            }

            var msg = new ChatMessage
            {
                Author = _myName,
                Text = text,
                Time = DateTime.Now,
                IsTeacher = true,
                Channel = _activeChannel,
                AvatarText = "GV"
            };
            Messages.Add(msg);
            txtInput.Text = "";
            RefreshAndScroll();

            // Send via network
            try
            {
                var app = (QASmartTouch.App)Application.Current;

                if (_activeChannel == "ALL")
                {
                    // Lấy danh sách học sinh đang online thực tế (TCP + Web)
                    var onlineStudents = new List<QASmartClass.Controls.NotificationWindow.FailedStudentItem>();
                    if (app.NetworkService != null)
                    {
                        foreach (var s in app.NetworkService.GetConnectedStudents())
                        {
                            onlineStudents.Add(new QASmartClass.Controls.NotificationWindow.FailedStudentItem { Code = s.Code, Name = s.Name });
                        }
                        if (app.NetworkService.WebBridge != null)
                        {
                            foreach (var w in app.NetworkService.WebBridge.GetConnectedWebStudents())
                            {
                                onlineStudents.Add(new QASmartClass.Controls.NotificationWindow.FailedStudentItem { Code = w.Code, Name = w.Name });
                            }
                        }
                    }

                    if (onlineStudents.Count > 0 && app.NetworkService != null)
                    {
                        // Khởi tạo Custom Notification Window ở trạng thái Processing
                        var notifyWin = new QASmartClass.Controls.NotificationWindow();
                        notifyWin.SetStatusProcessing("Đang gửi tin nhắn quảng bá...", $"Đang phát tới {onlineStudents.Count} học sinh online...");
                        notifyWin.Show();

                        // Ký và gán ID giao dịch
                        string rawCommand = $"MSG|📢 GV: {text}";
                        rawCommand = Classroom.Services.NetworkDiscoveryService.AppendCommandId(rawCommand);
                        string cmdId = Classroom.Services.NetworkDiscoveryService.ExtractCommandId(rawCommand);

                        var pendingAcks = new HashSet<string>(onlineStudents.Select(s => s.Code));
                        var receivedAcks = new HashSet<string>();
                        var lockObj = new object();

                        EventHandler<Classroom.Services.CommandAckEventArgs>? ackHandler = null;
                        System.Windows.Threading.DispatcherTimer? timeoutTimer = null;

                        Action cleanup = () =>
                        {
                            lock (lockObj)
                            {
                                if (app.NetworkService != null && ackHandler != null)
                                {
                                    app.NetworkService.CommandAckReceived -= ackHandler;
                                    ackHandler = null;
                                }
                                if (timeoutTimer != null)
                                {
                                    timeoutTimer.Stop();
                                    timeoutTimer = null;
                                }
                            }
                        };

                        notifyWin.Closed += (s, e) => { cleanup(); };

                        ackHandler = (sender, eArgs) =>
                        {
                            if (eArgs.CommandId == cmdId)
                            {
                                lock (lockObj)
                                {
                                    if (pendingAcks.Remove(eArgs.StudentCode))
                                    {
                                        receivedAcks.Add(eArgs.StudentCode);
                                        
                                        notifyWin.Dispatcher.Invoke(() =>
                                        {
                                            double percent = (double)receivedAcks.Count * 100.0 / onlineStudents.Count;
                                            notifyWin.UpdateProgress(percent);
                                            
                                            if (pendingAcks.Count == 0)
                                            {
                                                cleanup();
                                                notifyWin.SetStatusSuccess("Gửi tin nhắn thành công!", $"Đã gửi tới toàn bộ {onlineStudents.Count} học sinh online.");
                                            }
                                        });
                                    }
                                }
                            }
                        };

                        app.NetworkService.CommandAckReceived += ackHandler;

                        // Bắt đầu đếm ngược 3 giây
                        timeoutTimer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromSeconds(3)
                        };
                        timeoutTimer.Tick += (sTimer, eTimer) =>
                        {
                            cleanup();

                            var failedItems = new List<QASmartClass.Controls.NotificationWindow.FailedStudentItem>();
                            lock (lockObj)
                            {
                                foreach (var code in pendingAcks)
                                {
                                    var stud = onlineStudents.Find(s => s.Code == code);
                                    if (stud != null) failedItems.Add(stud);
                                }
                            }

                            if (failedItems.Count > 0)
                            {
                                // Ghi nhật ký lỗi kết nối mạng vào SQLite nền (v4.4 — di chuyển khỏi UI thread)
                                var failNames = string.Join(", ", failedItems.Select(f => $"{f.Name} ({f.Code})"));
                                WriteLogBackground("NETWORK_ERROR", "SYSTEM", $"[LỖI MẠNG] Gửi tin nhắn thất bại tới các học sinh: {failNames}");

                                string failDesc = $"Chỉ có {receivedAcks.Count}/{onlineStudents.Count} học sinh nhận được (Thất bại: {failedItems.Count}).";
                                notifyWin.SetStatusWarning("Gửi tin nhắn chưa hoàn tất!", failDesc, failedItems, (failedCode) =>
                                {
                                    // Logic gửi lại riêng lẻ (Resend)
                                    string resendCmd = $"MSG|🔒 {_myName} (Gửi lại): {text}";
                                    resendCmd = Classroom.Services.NetworkDiscoveryService.AppendCommandId(resendCmd);
                                    _ = app.NetworkService?.SendToStudentAsync(failedCode, resendCmd);
                                });

                                // Gửi lại hàng loạt cho toàn bộ danh sách máy lỗi (v4.3)
                                notifyWin.OnResendAllClicked = (failedCodes) =>
                                {
                                    foreach (var failedCode in failedCodes)
                                    {
                                        string resendCmd = $"MSG|🔒 {_myName} (Gửi lại): {text}";
                                        resendCmd = Classroom.Services.NetworkDiscoveryService.AppendCommandId(resendCmd);
                                        _ = app.NetworkService?.SendToStudentAsync(failedCode, resendCmd);
                                    }
                                };
                            }
                            else
                            {
                                notifyWin.SetStatusSuccess("Gửi tin nhắn thành công!", $"Đã gửi tới toàn bộ {onlineStudents.Count} học sinh online.");
                            }
                        };
                        timeoutTimer.Start();

                        // Thực hiện gửi lệnh đi
                        _ = app.NetworkService.SendCommandAsync(rawCommand);
                    }
                    else
                    {
                        // Không có học sinh online, phát bình thường
                        _ = app.NetworkService?.BroadcastMessage($"📢 GV: {text}");
                    }
                }
                else if (_activeChannel.StartsWith("GROUP_"))
                {
                    // Group → send only to group members
                    var grpId = _activeChannel.Replace("GROUP_", "");
                    var grp = _groups.FirstOrDefault(g => g.Id == grpId);
                    if (grp != null && grp.MemberCodes.Count > 0)
                        _ = app.NetworkService?.SendToStudentsAsync(grp.MemberCodes, $"MSG|📨 [{grp.Name}] GV: {text}");
                    else
                        _ = app.NetworkService?.BroadcastMessage($"📨 [{grp?.Name ?? "Nhóm"}] GV: {text}");
                }
                else if (_activeChannel.StartsWith("STUDY_"))
                {
                    // Study group → send only to study group members
                    try
                    {
                        var studyApp = (QASmartTouch.App)Application.Current;
                        if (int.TryParse(_activeChannel.Replace("STUDY_", ""), out int sIdx)
                            && studyApp.CurrentGroups?.Count > sIdx)
                        {
                            // Get student codes from study group member names
                            var memberNames = studyApp.CurrentGroups[sIdx].Members;
                            var memberCodes = new System.Collections.Generic.List<string>();
                            foreach (var name in memberNames)
                            {
                                var contact = _allContacts.FirstOrDefault(c =>
                                    !c.IsGroup && c.DisplayName.Contains(name));
                                if (contact != null) memberCodes.Add(contact.Code);
                            }
                            if (memberCodes.Count > 0)
                                _ = app.NetworkService?.SendToStudentsAsync(memberCodes, $"MSG|📚 [{channelLabel}] GV: {text}");
                            else
                                _ = app.NetworkService?.BroadcastMessage($"📚 [{channelLabel}] GV: {text}");
                        }
                        else
                        {
                            _ = app.NetworkService?.BroadcastMessage($"📚 [{channelLabel}] GV: {text}");
                        }
                    }
                    catch
                    {
                        _ = app.NetworkService?.BroadcastMessage($"📚 [{channelLabel}] GV: {text}");
                    }
                }
                else
                {
                    // Private → send ONLY to this specific student
                    _ = app.NetworkService?.SendToStudentAsync(_activeChannel, $"MSG|🔒 {_myName}: {text}");
                }

                // Save to DB in background thread
                string eventType = _activeChannel == "ALL" ? "TEACHER_CHAT"
                    : (_activeChannel.StartsWith("GROUP_") || _activeChannel.StartsWith("STUDY_")) ? "GROUP_CHAT"
                    : "PRIVATE_CHAT";
                WriteLogBackground(eventType, _myName, $"Tin nhắn: {text} [CH:{_activeChannel}]");
            }
            catch { }

            Log.Information("Sent to [{Channel}]: {Text}", _activeChannel, text);
        }

        // ═══════════════════════════════════════════════════
        //  CREATE GROUP
        // ═══════════════════════════════════════════════════

        private void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            ShowCreateGroupDialog();
        }

        private void ShowCreateGroupDialog()
        {
            var dlg = new Window
            {
                Title = "Tạo nhóm học sinh",
                Width = 560, Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            var rootBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(14),
                BorderBrush = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                    { BlurRadius = 24, ShadowDepth = 6, Opacity = 0.18, Color = Colors.Black }
            };

            var mainSp = new StackPanel();

            // ── HEADER — green gradient ──
            var header = new Border
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(27, 94, 50), Color.FromRgb(46, 125, 50), 0),
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Padding = new Thickness(24, 16, 24, 16)
            };
            var headerGrid = new DockPanel();
            var headerTextSp = new StackPanel();
            headerTextSp.Children.Add(new TextBlock
            {
                Text = "👥 Tạo Nhóm Học Sinh Mới",
                FontSize = 17, FontWeight = FontWeights.Bold, Foreground = Brushes.White
            });
            headerTextSp.Children.Add(new TextBlock
            {
                Text = "Tạo nhóm để gửi tin nhắn nhanh cho nhóm HS đã chọn",
                FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(200, 230, 200)),
                Margin = new Thickness(0, 4, 0, 0)
            });
            headerGrid.Children.Add(headerTextSp);

            var closeBtn = new TextBlock
            {
                Text = "✕", FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 230, 200)),
                Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top
            };
            closeBtn.MouseEnter += (_, _) => closeBtn.Foreground = Brushes.White;
            closeBtn.MouseLeave += (_, _) => closeBtn.Foreground = new SolidColorBrush(Color.FromRgb(200, 230, 200));
            closeBtn.MouseLeftButtonDown += (_, _) => { dlg.DialogResult = false; };
            DockPanel.SetDock(closeBtn, Dock.Right);
            headerGrid.Children.Add(closeBtn);
            header.Child = headerGrid;
            header.MouseLeftButtonDown += (_, _) => dlg.DragMove();
            mainSp.Children.Add(header);

            // ── CONTENT ──
            var content = new StackPanel { Margin = new Thickness(24, 18, 24, 8) };

            // ─ SECTION 1: Group name ─
            var nameLabelSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            nameLabelSp.Children.Add(new TextBlock
            {
                Text = "📝 Tên nhóm", FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55))
            });
            nameLabelSp.Children.Add(new TextBlock
            {
                Text = "  (bắt buộc)", FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(nameLabelSp);

            var txtGroupName = new TextBox
            {
                Text = $"Nhóm {_groups.Count + 1}", FontSize = 14,
                Padding = new Thickness(12, 10, 12, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 210, 220)),
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(252, 253, 255))
            };
            txtGroupName.GotFocus += (_, _) => txtGroupName.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            txtGroupName.LostFocus += (_, _) => txtGroupName.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 210, 220));
            content.Children.Add(txtGroupName);

            content.Children.Add(new TextBlock
            {
                Text = "💡 Gợi ý: Nhóm STEM, Nhóm Luyện thi, Nhóm Dự án...",
                FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                FontStyle = FontStyles.Italic, Margin = new Thickness(2, 4, 0, 0)
            });

            // ─ SECTION 2: Emoji picker ─
            content.Children.Add(new TextBlock
            {
                Text = "🎨 Chọn biểu tượng nhóm", FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                Margin = new Thickness(0, 14, 0, 6)
            });
            content.Children.Add(new TextBlock
            {
                Text = "Chọn màu đại diện cho nhóm, sẽ hiển thị trong sidebar",
                FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                FontStyle = FontStyles.Italic, Margin = new Thickness(2, 0, 0, 6)
            });

            var emojiContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var emojiPanel = new WrapPanel();
            string selectedEmoji = GroupEmojis[_groups.Count % GroupEmojis.Length];
            // Colored labels + background colors for each emoji
            string[] emojiLabels = { "Toán", "Văn", "Anh", "Sử", "STEM", "Nhạc", "TD", "Tin", "Vẽ", "Địa" };
            string[] emojiColors = { "#1565C0", "#C62828", "#F57C00", "#7B1FA2", "#00838F", "#AD1457", "#2E7D32", "#0277BD", "#EF6C00", "#4527A0" };
            string[] emojiBgColors = { "#E3F2FD", "#FFEBEE", "#FFF3E0", "#F3E5F5", "#E0F7FA", "#FCE4EC", "#E8F5E9", "#E1F5FE", "#FFF8E1", "#EDE7F6" };

            foreach (var (emoji, idx) in GroupEmojis.Select((e, i) => (e, i)))
            {
                var em = emoji;
                string label = idx < emojiLabels.Length ? emojiLabels[idx] : "";
                var colorStr = idx < emojiColors.Length ? emojiColors[idx] : "#616161";
                var bgStr = idx < emojiBgColors.Length ? emojiBgColors[idx] : "#F5F5F5";
                var itemColor = (Color)ColorConverter.ConvertFromString(colorStr);
                var itemBg = (Color)ColorConverter.ConvertFromString(bgStr);

                var btnSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                var isSelected = em == selectedEmoji;
                var btn = new Border
                {
                    Width = 44, Height = 44,
                    CornerRadius = new CornerRadius(10),
                    Background = isSelected
                        ? new SolidColorBrush(itemBg)
                        : new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                    BorderBrush = isSelected
                        ? new SolidColorBrush(itemColor)
                        : new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(isSelected ? 2.5 : 1),
                    Margin = new Thickness(2, 0, 2, 0),
                    Cursor = Cursors.Hand
                };
                btn.Child = new TextBlock
                {
                    Text = em, FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                btnSp.Children.Add(btn);
                btnSp.Children.Add(new TextBlock
                {
                    Text = label, FontSize = 9, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(itemColor),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                btn.MouseLeftButtonDown += (_, _) =>
                {
                    selectedEmoji = em;
                    // Reset all emoji buttons
                    int resetIdx = 0;
                    foreach (StackPanel sp in emojiPanel.Children)
                    {
                        if (sp.Children[0] is Border b)
                        {
                            b.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                            b.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                            b.BorderThickness = new Thickness(1);
                        }
                        resetIdx++;
                    }
                    btn.Background = new SolidColorBrush(itemBg);
                    btn.BorderBrush = new SolidColorBrush(itemColor);
                    btn.BorderThickness = new Thickness(2);
                };
                emojiPanel.Children.Add(btnSp);
            }
            emojiContainer.Child = emojiPanel;
            content.Children.Add(emojiContainer);

            // ─ SECTION 3: Student selection ─
            var memberLabelSp = new DockPanel { Margin = new Thickness(0, 14, 0, 6) };
            memberLabelSp.Children.Add(new TextBlock
            {
                Text = "👨‍🎓 Chọn thành viên", FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55))
            });
            var txtSelectedCount = new TextBlock
            {
                Text = "0 đã chọn", FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(txtSelectedCount, Dock.Right);
            memberLabelSp.Children.Add(txtSelectedCount);
            content.Children.Add(memberLabelSp);

            // 🔍 THÊM Ô TÌM KIẾM THÀNH VIÊN
            var searchMemberBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var searchGrid = new Grid();
            var placeholderText = new TextBlock
            {
                Text = "🔍 Nhập tên hoặc mã học sinh...",
                FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            var txtSearchMember = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0, 2, 0, 2)
            };
            
            searchGrid.Children.Add(placeholderText);
            searchGrid.Children.Add(txtSearchMember);
            searchMemberBorder.Child = searchGrid;
            content.Children.Add(searchMemberBorder);

            // Student checklist with colored rows
            var studentScroll = new ScrollViewer
            {
                Height = 180, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = new SolidColorBrush(Color.FromRgb(210, 220, 230)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(250, 252, 255))
            };
            var studentPanel = new StackPanel();
            var checkBoxes = new System.Collections.Generic.List<(Border row, CheckBox cb, string code, string name)>();

            // Xử lý sự kiện gõ ô tìm kiếm thành viên
            txtSearchMember.TextChanged += (s, ev) =>
            {
                placeholderText.Visibility = string.IsNullOrEmpty(txtSearchMember.Text) ? Visibility.Visible : Visibility.Collapsed;
                var query = txtSearchMember.Text.Trim().ToLower();
                foreach (var item in checkBoxes)
                {
                    bool matches = string.IsNullOrEmpty(query)
                        || item.name.ToLower().Contains(query)
                        || item.code.ToLower().Contains(query);
                    item.row.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
                }
            };

            var studentContacts = _allContacts.Where(c => !c.IsGroup).ToList();
            int rowIdx = 0;
            foreach (var sc in studentContacts)
            {
                var rowBg = rowIdx % 2 == 0
                    ? new SolidColorBrush(Color.FromRgb(255, 255, 255))
                    : new SolidColorBrush(Color.FromRgb(245, 248, 252));
                var hoverBg = new SolidColorBrush(Color.FromRgb(232, 245, 233));

                var rowBorder = new Border
                {
                    Background = rowBg, Padding = new Thickness(8, 6, 8, 6)
                };
                var rowPanel = new DockPanel();

                var statusDot = new Border
                {
                    Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
                    Background = sc.IsOnline
                        ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                        : new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                DockPanel.SetDock(statusDot, Dock.Right);

                var statusText = new TextBlock
                {
                    Text = sc.IsOnline ? "Online" : "Offline",
                    FontSize = 10,
                    Foreground = sc.IsOnline
                        ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                        : new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                DockPanel.SetDock(statusText, Dock.Right);

                var cb = new CheckBox
                {
                    Content = $"  {sc.DisplayName} ({(string.IsNullOrEmpty(sc.Code) ? "--" : sc.Code)})",
                    FontSize = 13,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                };

                // Update counter on check/uncheck
                cb.Checked += (_, _) =>
                {
                    int count = checkBoxes.Count(x => x.cb.IsChecked == true);
                    txtSelectedCount.Text = $"{count} đã chọn";
                    txtSelectedCount.Foreground = count > 0
                        ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                        : new SolidColorBrush(Color.FromRgb(158, 158, 158));
                    rowBorder.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                };
                cb.Unchecked += (_, _) =>
                {
                    int count = checkBoxes.Count(x => x.cb.IsChecked == true);
                    txtSelectedCount.Text = $"{count} đã chọn";
                    txtSelectedCount.Foreground = count > 0
                        ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                        : new SolidColorBrush(Color.FromRgb(158, 158, 158));
                    rowBorder.Background = rowBg;
                };

                rowPanel.Children.Add(statusDot);
                rowPanel.Children.Add(statusText);
                rowPanel.Children.Add(cb);
                rowBorder.Child = rowPanel;

                // Hover effect
                var rBg = rowBg;
                rowBorder.MouseEnter += (_, _) =>
                {
                    if (cb.IsChecked != true) rowBorder.Background = hoverBg;
                };
                rowBorder.MouseLeave += (_, _) =>
                {
                    if (cb.IsChecked != true) rowBorder.Background = rBg;
                };

                checkBoxes.Add((rowBorder, cb, sc.Code, sc.DisplayName));
                studentPanel.Children.Add(rowBorder);
                rowIdx++;
            }
            studentScroll.Content = studentPanel;
            content.Children.Add(studentScroll);

            // Select all / none bar
            var selectBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var selectPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var btnSelectAll = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 8, 0)
            };
            btnSelectAll.Child = new TextBlock
            {
                Text = "☑️ Chọn tất cả", FontSize = 11, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
            };
            btnSelectAll.MouseLeftButtonDown += (_, _) =>
            {
                foreach (var item in checkBoxes)
                {
                    if (item.row.Visibility == Visibility.Visible)
                        item.cb.IsChecked = true;
                }
            };

            var btnSelectNone = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = Cursors.Hand
            };
            btnSelectNone.Child = new TextBlock
            {
                Text = "⬜ Bỏ chọn tất cả", FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120))
            };
            btnSelectNone.MouseLeftButtonDown += (_, _) =>
            {
                foreach (var item in checkBoxes)
                {
                    if (item.row.Visibility == Visibility.Visible)
                        item.cb.IsChecked = false;
                }
            };

            selectPanel.Children.Add(btnSelectAll);
            selectPanel.Children.Add(btnSelectNone);
            selectBar.Child = selectPanel;
            content.Children.Add(selectBar);

            mainSp.Children.Add(content);

            // ── ACTION BUTTONS ──
            var btnPanel = new DockPanel { Margin = new Thickness(24, 14, 24, 22) };

            // Cancel (left)
            var btnCancel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 10, 18, 10),
                Cursor = Cursors.Hand
            };
            btnCancel.Child = new TextBlock
            {
                Text = "✕ Hủy", FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnCancel.MouseEnter += (_, _) => btnCancel.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            btnCancel.MouseLeave += (_, _) => btnCancel.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            btnCancel.MouseLeftButtonDown += (_, _) => { dlg.DialogResult = false; };

            // Create (right, green)
            var btnCreate = new Border
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(46, 125, 50), Color.FromRgb(56, 142, 60), 0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(24, 10, 24, 10),
                Cursor = Cursors.Hand
            };
            btnCreate.Child = new TextBlock
            {
                Text = "✅ Tạo nhóm", FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center
            };
            btnCreate.MouseEnter += (_, _) => btnCreate.Opacity = 0.85;
            btnCreate.MouseLeave += (_, _) => btnCreate.Opacity = 1.0;

            btnCreate.MouseLeftButtonDown += (_, _) =>
            {
                var selected = checkBoxes.Where(x => x.cb.IsChecked == true).Select(x => x.code).ToList();
                if (selected.Count == 0)
                {
                    MessageBox.Show("⚠️ Vui lòng chọn ít nhất 1 học sinh!", "Chưa chọn thành viên",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtGroupName.Text))
                {
                    MessageBox.Show("⚠️ Vui lòng nhập tên nhóm!", "Tên nhóm trống",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                dlg.DialogResult = true;
            };

            DockPanel.SetDock(btnCancel, Dock.Left);
            btnPanel.Children.Add(btnCancel);
            var rightP = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            rightP.Children.Add(btnCreate);
            btnPanel.Children.Add(rightP);
            mainSp.Children.Add(btnPanel);

            rootBorder.Child = mainSp;
            dlg.Content = rootBorder;

            if (dlg.ShowDialog() != true) return;

            // Create group
            var members = checkBoxes.Where(x => x.cb.IsChecked == true).Select(x => x.code).ToList();
            var newGroup = new StudentGroup
            {
                Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                Name = txtGroupName.Text.Trim(),
                Emoji = selectedEmoji,
                MemberCodes = members
            };
            _groups.Add(newGroup);
            SaveGroups();
            BuildContactList();
            SelectChannel($"GROUP_{newGroup.Id}");

            AddSystemMessage($"✅ Đã tạo nhóm \"{newGroup.Name}\" với {members.Count} thành viên");
        }

        // ═══════════════════════════════════════════════════
        //  ACTIONS
        // ═══════════════════════════════════════════════════

        private void BroadcastAnnounce_Click(object sender, RoutedEventArgs e)
        {
            var text = "📢 THÔNG BÁO: Bài kiểm tra bắt đầu sau 5 phút! Chuẩn bị sẵn sàng.";
            Messages.Add(new ChatMessage
            {
                Author = _myName, Text = text, Time = DateTime.Now, IsTeacher = true, AvatarText = "GV", Channel = "ALL"
            });
            RefreshAndScroll();

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                _ = app.NetworkService?.BroadcastMessage(text);
                WriteLogBackground("TEACHER_CHAT", _myName, $"Tin nhắn: {text}");
            }
            catch { }
        }

        private void ShowHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int count = 0;
                using (var db = new AppDbContext())
                {
                    count = db.EventLogs.Count(ev => ev.EventType == "CHAT" || ev.EventType == "TEACHER_CHAT");
                }

                MessageBox.Show(
                    $"📊 Thống kê tin nhắn:\n\n" +
                    $"💬 Tổng số tin nhắn: {count}\n" +
                    $"👥 Liên hệ: {_allContacts.Count - 1} học sinh\n" +
                    $"🟢 Đang online: {_allContacts.Count(c => c.IsOnline && !c.IsGroup)}\n\n" +
                    $"📂 Lịch sử được lưu trong EventLogs database.",
                    "Lịch sử tin nhắn", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show("Xóa toàn bộ tin nhắn trên màn hình?\n(Lịch sử trong DB vẫn giữ nguyên)",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                Messages.Clear();
                FilteredMessages.Clear();
                AddSystemMessage("🗑️ Đã xóa tin nhắn trên màn hình. Lịch sử DB vẫn còn.");
            }
        }

        // ═══════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════

        private void AddSystemMessage(string text)
        {
            Messages.Add(new ChatMessage
            {
                Author = "⚙️ Hệ thống", Text = text, Time = DateTime.Now, IsSystem = true, Channel = "ALL"
            });
            RefreshAndScroll();
        }

        private void RefreshAndScroll()
        {
            FilterMessagesForChannel();
        }

        private void ScrollToEnd()
        {
            if (chatList.Items.Count > 0)
                chatList.ScrollIntoView(chatList.Items[chatList.Items.Count - 1]);
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        private static Brush GetBrushForCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return Brushes.Gray;
            int hash = Math.Abs(code.GetHashCode());
            var color = AvatarColors[hash % AvatarColors.Length];
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private void TabMembers_Click(object sender, MouseButtonEventArgs e)
        {
            var membersToShow = new System.Collections.Generic.List<ChatContact>();

            if (_activeChannel == "ALL")
            {
                membersToShow = _allContacts.Where(c => !c.IsGroup).ToList();
            }
            else if (_activeChannel.StartsWith("GROUP_"))
            {
                var grpId = _activeChannel.Replace("GROUP_", "");
                var grp = _groups.FirstOrDefault(g => g.Id == grpId);
                if (grp != null)
                {
                    membersToShow = _allContacts.Where(c => !c.IsGroup && grp.MemberCodes.Contains(c.Code)).ToList();
                }
            }
            else if (_activeChannel.StartsWith("STUDY_"))
            {
                var app = (QASmartTouch.App)Application.Current;
                if (int.TryParse(_activeChannel.Replace("STUDY_", ""), out int sIdx)
                    && app.CurrentGroups?.Count > sIdx)
                {
                    var studyGroup = app.CurrentGroups[sIdx];
                    foreach (var name in studyGroup.Members)
                    {
                        var c = _allContacts.FirstOrDefault(contact => !contact.IsGroup && contact.DisplayName.Contains(name));
                        if (c != null) membersToShow.Add(c);
                    }
                }
            }
            else
            {
                // Riêng tư: 1 đối tác + Giáo viên
                var receiverContact = _allContacts.FirstOrDefault(c => c.Code == _activeChannel);
                if (receiverContact != null) membersToShow.Add(receiverContact);

                membersToShow.Add(new ChatContact
                {
                    DisplayName = _myName + " (Bạn)",
                    Code = "GV",
                    IsOnline = true,
                    RoleBadge = "👨‍🏫 GV"
                });
            }

            ShowGroupMembersDialog(membersToShow);
        }

        private void ShowGroupMembersDialog(System.Collections.Generic.List<ChatContact> members)
        {
            var dlg = new Window
            {
                Title = "Danh sách thành viên",
                Width = 460, Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            var rootBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(14),
                BorderBrush = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                    { BlurRadius = 24, ShadowDepth = 6, Opacity = 0.18, Color = Colors.Black }
            };

            var mainSp = new StackPanel();

            // ── HEADER — blue gradient ──
            var header = new Border
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(21, 101, 192), Color.FromRgb(30, 136, 229), 0),
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Padding = new Thickness(24, 16, 24, 16)
            };
            var headerGrid = new DockPanel();
            var headerTextSp = new StackPanel();
            headerTextSp.Children.Add(new TextBlock
            {
                Text = $"👥 Thành Viên Nhóm ({members.Count})",
                FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.White
            });
            headerTextSp.Children.Add(new TextBlock
            {
                Text = "Xem danh sách và trạng thái kết nối của các thành viên",
                FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                Margin = new Thickness(0, 4, 0, 0)
            });
            headerGrid.Children.Add(headerTextSp);

            var closeBtn = new TextBlock
            {
                Text = "✕", FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top
            };
            closeBtn.MouseEnter += (_, _) => closeBtn.Foreground = Brushes.White;
            closeBtn.MouseLeave += (_, _) => closeBtn.Foreground = new SolidColorBrush(Color.FromRgb(227, 242, 253));
            closeBtn.MouseLeftButtonDown += (_, _) => { dlg.DialogResult = false; };
            DockPanel.SetDock(closeBtn, Dock.Right);
            headerGrid.Children.Add(closeBtn);
            header.Child = headerGrid;
            header.MouseLeftButtonDown += (_, _) => dlg.DragMove();
            mainSp.Children.Add(header);

            // ── CONTENT ──
            var content = new StackPanel { Margin = new Thickness(20, 16, 20, 8) };

            // 🔍 Search Box
            var searchBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 247, 250)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var searchGrid = new Grid();
            var placeholderText = new TextBlock
            {
                Text = "🔍 Tìm kiếm thành viên theo tên hoặc mã...",
                FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            var txtSearch = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            searchGrid.Children.Add(placeholderText);
            searchGrid.Children.Add(txtSearch);
            searchBorder.Child = searchGrid;
            content.Children.Add(searchBorder);

            // Scrollable List
            var listScroll = new ScrollViewer
            {
                Height = 280, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 230, 240)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(250, 252, 255))
            };
            var listPanel = new StackPanel();
            var memberRows = new System.Collections.Generic.List<(Border row, string code, string name)>();

            // Handle searching
            txtSearch.TextChanged += (s, ev) =>
            {
                placeholderText.Visibility = string.IsNullOrEmpty(txtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
                var query = txtSearch.Text.Trim().ToLower();
                foreach (var item in memberRows)
                {
                    bool matches = string.IsNullOrEmpty(query)
                        || item.name.ToLower().Contains(query)
                        || item.code.ToLower().Contains(query);
                    item.row.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
                }
            };

            int rowIdx = 0;
            foreach (var m in members)
            {
                var rowBg = rowIdx % 2 == 0
                    ? new SolidColorBrush(Color.FromRgb(255, 255, 255))
                    : new SolidColorBrush(Color.FromRgb(245, 248, 252));

                var rowBorder = new Border
                {
                    Background = rowBg, Padding = new Thickness(10, 8, 10, 8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(235, 240, 248)),
                    BorderThickness = new Thickness(0, 0, 0, 0.5)
                };
                var rowPanel = new DockPanel();

                // Status Indicator
                var statusDot = new Border
                {
                    Width = 8, Height = 8, CornerRadius = new CornerRadius(4),
                    Background = m.IsOnline
                        ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                        : new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                DockPanel.SetDock(statusDot, Dock.Right);

                var statusText = new TextBlock
                {
                    Text = m.IsOnline ? "Online" : "Offline",
                    FontSize = 10,
                    Foreground = m.IsOnline
                        ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                        : new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                DockPanel.SetDock(statusText, Dock.Right);

                // Avatar
                var avatarBorder = new Border
                {
                    Width = 28, Height = 28, CornerRadius = new CornerRadius(14),
                    Background = m.AvatarBg ?? Brushes.Gray,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                avatarBorder.Child = new TextBlock
                {
                    Text = m.AvatarText ?? "?", FontSize = 11, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                };

                // Info Text
                var infoSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                infoSp.Children.Add(new TextBlock
                {
                    Text = m.DisplayName, FontSize = 12.5, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
                });
                if (!string.IsNullOrEmpty(m.Code) && m.Code != "GV")
                {
                    infoSp.Children.Add(new TextBlock
                    {
                        Text = $"@{m.Code}  •  {m.ClassName}", FontSize = 10.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 128))
                    });
                }
                else if (m.Code == "GV")
                {
                    infoSp.Children.Add(new TextBlock
                    {
                        Text = m.RoleBadge, FontSize = 10.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
                    });
                }

                rowPanel.Children.Add(statusDot);
                rowPanel.Children.Add(statusText);
                rowPanel.Children.Add(avatarBorder);
                rowPanel.Children.Add(infoSp);
                rowBorder.Child = rowPanel;

                listPanel.Children.Add(rowBorder);
                memberRows.Add((rowBorder, m.Code, m.DisplayName));
                rowIdx++;
            }

            listScroll.Content = listPanel;
            content.Children.Add(listScroll);
            mainSp.Children.Add(content);

            // ── FOOTER BUTTON ──
            var btnPanel = new DockPanel { Margin = new Thickness(20, 8, 20, 16) };
            var btnClose = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0, 10, 0, 10),
                Cursor = Cursors.Hand
            };
            btnClose.Child = new TextBlock
            {
                Text = "Đóng", FontSize = 12.5, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnClose.MouseEnter += (_, _) => btnClose.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            btnClose.MouseLeave += (_, _) => btnClose.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            btnClose.MouseLeftButtonDown += (_, _) => { dlg.DialogResult = false; };
            btnPanel.Children.Add(btnClose);
            mainSp.Children.Add(btnPanel);

            rootBorder.Child = mainSp;
            dlg.Content = rootBorder;
            dlg.ShowDialog();
        }

        private void ToggleMuteChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isClassChatMuted = !_isClassChatMuted;
                var app = (QASmartTouch.App)Application.Current;

                // Send broadcast command to all students
                app.NetworkService?.SendCommandAsync($"CMD|CLASS_CHAT_MUTE|{_isClassChatMuted}");

                // Update UI button state
                btnToggleMuteChat.Content = _isClassChatMuted ? "🔒 Mở khóa chat" : "🔓 Khóa chat";
                btnToggleMuteChat.Background = _isClassChatMuted ? new SolidColorBrush(Color.FromRgb(255, 235, 235)) : new SolidColorBrush(Color.FromRgb(224, 242, 241));
                btnToggleMuteChat.Foreground = _isClassChatMuted ? new SolidColorBrush(Color.FromRgb(211, 47, 47)) : new SolidColorBrush(Color.FromRgb(0, 121, 107));

                // Add system message feedback
                if (_isClassChatMuted)
                {
                    AddSystemMessage("🔒 Đã khóa chat của toàn bộ học sinh.");
                }
                else
                {
                    AddSystemMessage("🔓 Đã mở khóa chat của toàn bộ học sinh.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("ToggleMuteChat_Click error: {Err}", ex.Message);
            }
        }

        private void chkAnonymousFeedback_Checked(object sender, RoutedEventArgs e)
        {
            SetAnonymousFeedbackStatus(true);
        }

        private void chkAnonymousFeedback_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAnonymousFeedbackStatus(false);
        }

        private void SetAnonymousFeedbackStatus(bool isAnonymous)
        {
            try
            {
                _isFeedbackAnonymous = isAnonymous;
                var app = (QASmartTouch.App)Application.Current;
                
                // Broadcast anonymous command to all students
                app.NetworkService?.SendCommandAsync($"CMD|CLASS_CHAT_ANONYMOUS|{isAnonymous}");
                
                // Redraw or clear contact list feedback emojis
                foreach (var contact in _allContacts)
                {
                    if (isAnonymous)
                    {
                        contact.FeedbackEmoji = "";
                    }
                    else
                    {
                        if (_studentFeedbacks.TryGetValue(contact.Code, out var emoji))
                        {
                            contact.FeedbackEmoji = emoji;
                        }
                        else
                        {
                            contact.FeedbackEmoji = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("SetAnonymousFeedbackStatus error: {Err}", ex.Message);
            }
        }

        private void HandleStudentFeedback(string studentCode, string emoji)
        {
            try
            {
                if (string.IsNullOrEmpty(studentCode)) return;

                // Server-side rate limiting: 3 seconds cooldown per student
                var now = DateTime.Now;
                if (_studentLastFeedbackTime.TryGetValue(studentCode, out var lastTime))
                {
                    if (now - lastTime < TimeSpan.FromSeconds(3))
                    {
                        return; // Ignore spam
                    }
                }
                _studentLastFeedbackTime[studentCode] = now;

                // Save or update student feedback
                _studentFeedbacks[studentCode] = emoji;

                // Update contact's emoji (only if not anonymous)
                var contact = _allContacts.FirstOrDefault(c => c.Code.Equals(studentCode, StringComparison.OrdinalIgnoreCase));
                if (contact != null)
                {
                    contact.FeedbackEmoji = _isFeedbackAnonymous ? "" : emoji;
                }

                // Calculate statistics
                int cntLight = 0;
                int cntQuestion = 0;
                int cntRaise = 0;
                int cntWait = 0;

                foreach (var fb in _studentFeedbacks.Values)
                {
                    if (fb == "💡") cntLight++;
                    else if (fb == "❓") cntQuestion++;
                    else if (fb == "🙋") cntRaise++;
                    else if (fb == "⏳") cntWait++;
                }

                // Update stats UI
                txtFeedbackStats.Text = $"💡:{cntLight} | ❓:{cntQuestion} | 🙋:{cntRaise} | ⏳:{cntWait}";

                // Update ProgressBar understanding percentage
                int total = cntLight + cntQuestion + cntRaise + cntWait;
                if (total > 0 && barUnderstanding != null)
                {
                    double pct = (double)(cntLight + cntRaise) / total * 100;
                    barUnderstanding.Value = pct;
                    if (pct >= 75) barUnderstanding.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                    else if (pct >= 50) barUnderstanding.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                    else barUnderstanding.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                }
                else if (barUnderstanding != null)
                {
                    barUnderstanding.Value = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error in HandleStudentFeedback: {Err}", ex.Message);
            }
        }

        private void AllowStudentChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (contactList.SelectedItem is ChatContact contact && !contact.IsGroup)
                {
                    var app = (QASmartTouch.App)Application.Current;
                    _ = app.NetworkService?.SendToStudentAsync(contact.Code, $"CMD|CLASS_CHAT_MUTE_STUDENT|{contact.Code}|false");
                    _ = app.NetworkService?.SendToStudentAsync(contact.Code, $"CMD|TURN_GRANTED|{contact.Code}");
                    AddSystemMessage($"🔓 Đã mở khóa chat riêng và duyệt phát biểu cho học sinh {contact.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("AllowStudentChat_Click error: {Err}", ex.Message);
            }
        }

        private void MuteStudentChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (contactList.SelectedItem is ChatContact contact && !contact.IsGroup)
                {
                    var app = (QASmartTouch.App)Application.Current;
                    _ = app.NetworkService?.SendToStudentAsync(contact.Code, $"CMD|CLASS_CHAT_MUTE_STUDENT|{contact.Code}|true");
                    AddSystemMessage($"🔒 Đã khóa chat riêng học sinh {contact.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("MuteStudentChat_Click error: {Err}", ex.Message);
            }
        }

        private void UpdateTabLabels()
        {
            try
            {
                int unreadGroup = _allContacts.Where(c => c.IsGroup && c.Code != "ALL").Sum(c => c.UnreadCount);
                int unreadPrivate = _allContacts.Where(c => !c.IsGroup).Sum(c => c.UnreadCount);
                int unreadAll = _allContacts.FirstOrDefault(c => c.Code == "ALL")?.UnreadCount ?? 0;
                int totalUnread = unreadAll + unreadGroup + unreadPrivate;

                tabAll.Content = totalUnread > 0 ? $"📋 Tất cả ({totalUnread})" : "📋 Tất cả";
                tabGroup.Content = unreadGroup > 0 ? $"🏫 Nhóm ({unreadGroup})" : "🏫 Nhóm";
                tabPrivate.Content = unreadPrivate > 0 ? $"🔒 Riêng ({unreadPrivate})" : "🔒 Riêng";
            }
            catch (Exception ex)
            {
                Log.Warning("UpdateTabLabels error: {Err}", ex.Message);
            }
        }
    }

    // ═══════════════════════════════════════════════════
    //  DATA MODELS
    // ═══════════════════════════════════════════════════

    public class ChatMessage
    {
        public string Author      { get; set; } = string.Empty;
        public string Text        { get; set; } = string.Empty;
        public DateTime Time      { get; set; }
        public bool IsTeacher     { get; set; }
        public bool IsSystem      { get; set; }
        public bool IsDateSeparator { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string Channel     { get; set; } = "ALL";
        public string AvatarText  { get; set; } = "?";
        public Brush AvatarBg     { get; set; } = Brushes.Gray;
        public string TimeStr     => Time.ToString("HH:mm");
        public string StudentCodeDisplay => string.IsNullOrEmpty(StudentCode) ? "" : $"@{StudentCode}";
    }

    public class ChatContact : INotifyPropertyChanged
    {
        public string Code          { get; set; } = string.Empty;
        public string DisplayName   { get; set; } = string.Empty;
        /// <summary>Full identity: THPT QA_Lớp10A_HS_K46_Nguyễn Văn An</summary>
        public string FullIdentity  { get; set; } = string.Empty;
        public string RoleBadge     { get; set; } = string.Empty;  // 🎓 CựuHS, 👪 PH
        public string ClassName     { get; set; } = string.Empty;
        public string SchoolName    { get; set; } = string.Empty;
        public string AvatarText    { get; set; } = "?";
        public Brush AvatarBg       { get; set; } = Brushes.Gray;
        public bool IsGroup         { get; set; }
        
        private string _feedbackEmoji = "";
        public string FeedbackEmoji
        {
            get => _feedbackEmoji;
            set { _feedbackEmoji = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FeedbackEmoji))); }
        }

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOnline))); }
        }

        private string _lastMessage = "";
        public string LastMessage
        {
            get => _lastMessage;
            set { _lastMessage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessage))); }
        }

        private DateTime _lastTime;
        public DateTime LastTime
        {
            get => _lastTime;
            set { _lastTime = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastTimeStr))); }
        }

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set { _unreadCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnreadCount))); }
        }

        public string LastTimeStr => LastTime == DateTime.MinValue ? "" : LastTime.ToString("HH:mm");

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
