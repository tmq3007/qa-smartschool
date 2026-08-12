using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using QASmartClass.Data;
using Serilog;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentChatPage : Page
    {
        internal static readonly object _dbLock = new object();

        private static readonly string[] ForbiddenFileExtensions = new[]
        {
            ".exe", ".msi", ".bat", ".cmd", ".vbs", ".js", ".jse", 
            ".wsf", ".wsh", ".ps1", ".psm1", ".scr", ".pif", ".lnk", ".reg"
        };

        private static readonly string[] BadWords = new[]
        {
            "đm", "dm", "vcl", "vkl", "cl", "clm", "đéo", "cứt", "chó", "chó chết", "fuck", "ngu thế", "dốt thế"
        };

        private string FilterProfanity(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            string filtered = input;
            foreach (var badWord in BadWords)
            {
                string pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(badWord) + @"\b";
                filtered = System.Text.RegularExpressions.Regex.Replace(
                    filtered, 
                    pattern, 
                    new string('*', badWord.Length), 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }
            return filtered;
        }

        // ─── Data models (matching teacher side) ─────────
        internal class ChatGroup
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Emoji { get; set; } = "👥";
            public List<string> MemberCodes { get; set; } = new();
        }

        // ─── State ──────────────────────────────────────────
        private readonly ObservableCollection<StudentChannel> _channels = new();
        private readonly List<StudentChatMsg> _allMessages = new();
        private List<ChatGroup> _customGroups = new();
        private string _activeChannel = "ALL";
        private string _myCode = "Student";
        private string _myName = "Học sinh";
        private DispatcherTimer _unreadTimer;
        private int _lastKnownDbCount = 0;
        private bool _isMessageReceiverWired = false;
        private bool _isMuted = false;
        private DateTime _lastFeedbackTime = DateTime.MinValue;
        private bool _isAnonymousFeedback = false;
        private System.Windows.Threading.DispatcherTimer? _cooldownTimer;
        private int _cooldownSecondsRemaining = 0;
        private Button? _activeCooldownButton;

        public StudentChatPage()
        {
            InitializeComponent();
            ResolveStudentIdentity();
            LoadCustomGroupsFromDB();
            BuildChannels();
            LoadChatHistoryFromDB();
            WireMessageReceiver();
            StartUnreadTimer();

            Loaded += (_, _) =>
            {
                // Select first channel
                if (channelList.SelectedIndex < 0 && channelList.Items.Count > 0)
                    channelList.SelectedIndex = 0;

                if (_unreadTimer != null && !_unreadTimer.IsEnabled)
                    _unreadTimer.Start();

                chatScroll.ScrollToEnd();
            };
            Unloaded += (_, _) =>
            {
                _unreadTimer?.Stop();
                _cooldownTimer?.Stop();
            };
        }

        // ═══════════════════════════════════════════════════
        //  IDENTITY
        // ═══════════════════════════════════════════════════

        private void ResolveStudentIdentity()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var client = app.StudentNetwork;
                if (!string.IsNullOrEmpty(client?.StudentCode))
                    _myCode = client.StudentCode;
                if (!string.IsNullOrEmpty(client?.StudentName))
                    _myName = client.StudentName;
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════
        //  LOAD CUSTOM GROUPS FROM DB (same as teacher side)
        // ═══════════════════════════════════════════════════

        private void LoadCustomGroupsFromDB()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var log = app.Database?.EventLogs
                    .Where(e => e.EventType == "CHAT_GROUPS")
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefault();

                if (log != null && !string.IsNullOrEmpty(log.Details))
                {
                    _customGroups = System.Text.Json.JsonSerializer.Deserialize<List<ChatGroup>>(log.Details) ?? new();
                }
            }
            catch (Exception ex) { Log.Warning("LoadGroups error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════
        //  BUILD CHANNELS (sidebar)
        // ═══════════════════════════════════════════════════

        private void BuildChannels()
        {
            _channels.Clear();

            // ── "Cả lớp" (ALL) ──
            _channels.Add(new StudentChannel
            {
                Code = "ALL",
                DisplayName = "Cả lớp",
                AvatarText = "🏫",
                AvatarBg = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Subtitle = "🏫 Nhắn tin chung với toàn bộ lớp",
                IsGroup = true
            });

            // ── Custom groups from teacher (GROUP_xxx) ──
            foreach (var grp in _customGroups)
            {
                // Check if this student belongs to the group
                bool isMember = grp.MemberCodes.Contains(_myCode);
                _channels.Add(new StudentChannel
                {
                    Code = $"GROUP_{grp.Id}",
                    DisplayName = $"{grp.Emoji} {grp.Name}",
                    AvatarText = grp.Emoji,
                    AvatarBg = GetBrushForCode(grp.Id),
                    Subtitle = $"📋 {grp.MemberCodes.Count} thành viên" + (isMember ? " · Bạn là TV" : ""),
                    IsGroup = true
                });
            }

            // ── Study groups from App.CurrentGroups (STUDY_x) ──
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app.CurrentGroups?.Count > 0)
                {
                    foreach (var sg in app.CurrentGroups)
                    {
                        bool isMember = sg.Members.Any(m =>
                            m.Contains(_myCode) || m.Contains(_myName));

                        _channels.Add(new StudentChannel
                        {
                            Code = $"STUDY_{sg.Index}",
                            DisplayName = $"{sg.Emoji} {sg.Name}",
                            AvatarText = sg.Emoji,
                            AvatarBg = new SolidColorBrush(sg.HeaderColor),
                            Subtitle = $"📚 Nhóm học tập · {sg.Members.Count} HS" + (isMember ? " · Bạn là TV" : ""),
                            IsGroup = true
                        });
                    }
                }
            }
            catch { }

            // ── "GV Riêng" (TEACHER) ──
            _channels.Add(new StudentChannel
            {
                Code = "TEACHER",
                DisplayName = "👨‍🏫 Giáo viên",
                AvatarText = "GV",
                AvatarBg = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                Subtitle = "🔒 Tin nhắn riêng với giáo viên",
                IsGroup = false
            });

            channelList.ItemsSource = _channels;
            if (panelEmptyState != null)
            {
                panelEmptyState.Visibility = _channels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ═══════════════════════════════════════════════════
        //  LOAD HISTORY FROM DB
        // ═══════════════════════════════════════════════════

        private void LoadChatHistoryFromDB()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                var chatEvents = app.Database.EventLogs
                    .Where(e => e.EventType == "CHAT"
                             || e.EventType == "TEACHER_CHAT"
                             || e.EventType == "PRIVATE_CHAT"
                             || e.EventType == "GROUP_CHAT")
                    .OrderBy(e => e.Timestamp)
                    .ToList()
                    .TakeLast(100)
                    .ToList();

                foreach (var ev in chatEvents)
                {
                    bool isTeacher = ev.Actor == "GV" || ev.EventType == "TEACHER_CHAT" || ev.EventType == "GROUP_CHAT";
                    string rawDetails = ev.Details;

                    // Parse channel from [CH:xxx] tag
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
                        // Private messages: teacher→student has channel = student code
                        // For student side: map to "TEACHER" if it's about me
                        if (ev.Actor == _myCode || isTeacher)
                            channel = "TEACHER";
                    }

                    // Map student-code channels to TEACHER for private msgs
                    // (Both teacher→student and student→teacher use _myCode in DB)
                    if (channel == _myCode)
                        channel = "TEACHER";

                    // Clean text
                    string text = rawDetails;
                    if (text.StartsWith("Tin nhắn: "))
                        text = text.Substring("Tin nhắn: ".Length);

                    // Remove [CH:xxx]
                    var tagIdx = text.IndexOf(" [CH:");
                    if (tagIdx >= 0)
                    {
                        var tagEnd = text.IndexOf("]", tagIdx);
                        if (tagEnd > tagIdx)
                            text = text.Substring(0, tagIdx);
                    }

                    // Remove identity bracket
                    var bracketIdx = text.LastIndexOf(" [");
                    if (bracketIdx > 0 && text.EndsWith("]"))
                        text = text.Substring(0, bracketIdx);

                    // Determine if this is "me"
                    bool isMe = !isTeacher && ev.Actor == _myCode;

                    _allMessages.Add(new StudentChatMsg
                    {
                        Author = isTeacher ? "GV" : (ev.Actor ?? "HS"),
                        Text = text,
                        Time = ev.Timestamp,
                        IsTeacher = isTeacher,
                        IsMe = isMe,
                        Channel = channel
                    });
                }

                Log.Information("StudentChat: Loaded {Count} messages from DB", chatEvents.Count);
            }
            catch (Exception ex) { Log.Warning("LoadChatHistory error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════
        //  NETWORK EVENTS (real-time)
        // ═══════════════════════════════════════════════════

        public void AppendNewMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendNewMessage(msg));
                return;
            }
            try
            {
                string channel = "ALL";
                string text = msg;

                if (msg.StartsWith("📨 ["))
                {
                    // Custom group: 📨 [Nhóm 1] GV: text
                    channel = DetectGroupChannel(msg);
                    // Clean text
                    var colIdx = msg.IndexOf("] GV: ");
                    text = colIdx > 0 ? msg.Substring(colIdx + 6) : msg;
                }
                else if (msg.StartsWith("📚 ["))
                {
                    // Study group: 📚 [Sư Tử] GV: text
                    channel = DetectStudyGroupChannel(msg);
                    var colIdx = msg.IndexOf("] GV: ");
                    text = colIdx > 0 ? msg.Substring(colIdx + 6) : msg;
                }
                else if (msg.StartsWith("🔒"))
                {
                    // Private: 🔒 GV Name: text
                    channel = "TEACHER";
                    var colonIdx = msg.IndexOf(": ");
                    text = colonIdx > 0 ? msg.Substring(colonIdx + 2) : msg;
                }
                else if (msg.StartsWith("MSG_HISTORY|"))
                {
                    var parts = msg.Split('|', 3);
                    if (parts.Length >= 3) text = parts[2];
                    channel = "ALL";
                    // Clean broadcast prefix from history
                    if (text.StartsWith("📢 GV: ")) text = text.Substring("📢 GV: ".Length);
                }
                else if (msg.StartsWith("📢 GV: "))
                {
                    // Broadcast: 📢 GV: text
                    channel = "ALL";
                    text = msg.Substring("📢 GV: ".Length);
                }
                else if (msg.StartsWith("💬 "))
                {
                    channel = "ALL";
                    text = msg.Substring(3);
                }
                else if (msg.StartsWith("💬"))
                {
                    channel = "ALL";
                    text = msg.Substring(2);
                }
                else
                {
                    // Fallback: show as teacher message in ALL
                    channel = "ALL";
                }

                if (string.IsNullOrWhiteSpace(text)) return;

                var chatMsg = new StudentChatMsg
                {
                    Author = "GV",
                    Text = text,
                    Time = DateTime.Now,
                    IsTeacher = true,
                    IsMe = false,
                    Channel = channel
                };

                // Check for duplicates
                bool exists = _allMessages.Any(m => m.Channel == channel && m.Text == text && Math.Abs((m.Time - chatMsg.Time).TotalSeconds) < 2);
                if (!exists)
                {
                    _allMessages.Add(chatMsg);

                    // Increment unread if not viewing this channel
                    if (channel != _activeChannel)
                    {
                        var ch = _channels.FirstOrDefault(c => c.Code == channel);
                        if (ch != null)
                            ch.UnreadCount++;
                    }

                    // Update last message preview
                    var chPreview = _channels.FirstOrDefault(c => c.Code == channel);
                    if (chPreview != null)
                        chPreview.LastMessage = text.Length > 25 ? text.Substring(0, 25) + "..." : text;

                    RenderMessages();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("AppendNewMessage error: {Err}", ex.Message);
            }
        }

        private void WireMessageReceiver()
        {
            // Empty. Messages are handled globally in StudentShell and delegated here.
        }

        private string DetectGroupChannel(string msg)
        {
            // Match against custom groups loaded from DB
            foreach (var grp in _customGroups)
            {
                if (msg.Contains($"[{grp.Name}]"))
                    return $"GROUP_{grp.Id}";
            }
            // If not found, create a dynamic channel from the group name
            var start = msg.IndexOf("[");
            var end = msg.IndexOf("]");
            if (start >= 0 && end > start)
            {
                string grpName = msg.Substring(start + 1, end - start - 1);
                // Add as new channel if not exists
                EnsureDynamicGroupChannel(grpName);
                return $"GROUP_DYN_{grpName}";
            }
            return "ALL"; // Fallback
        }

        private string DetectStudyGroupChannel(string msg)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app.CurrentGroups != null)
                {
                    foreach (var sg in app.CurrentGroups)
                    {
                        if (msg.Contains($"[{sg.Name}]"))
                            return $"STUDY_{sg.Index}";
                    }
                }
            }
            catch { }
            return "ALL";
        }

        private void EnsureDynamicGroupChannel(string groupName)
        {
            // Check if already exists
            if (_channels.Any(c => c.DisplayName.Contains(groupName)))
                return;

            _channels.Insert(_channels.Count - 1, new StudentChannel
            {
                Code = $"GROUP_DYN_{groupName}",
                DisplayName = $"📋 {groupName}",
                AvatarText = "📋",
                AvatarBg = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                Subtitle = "Nhóm do GV tạo",
                IsGroup = true
            });
        }

        // ═══════════════════════════════════════════════════
        //  CHANNEL SELECTION
        // ═══════════════════════════════════════════════════

        private void Channel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (channelList.SelectedItem is StudentChannel ch)
            {
                _activeChannel = ch.Code;

                // Reset unread count for this channel
                ch.UnreadCount = 0;

                // Update header
                chatAvatarText.Text = ch.AvatarText;
                chatAvatar.Background = ch.AvatarBg;
                txtChatTitle.Text = ch.DisplayName;
                txtChatSubtitle.Text = ch.Subtitle;

                RenderMessages();
            }
        }
        // ═══════════════════════════════════════════════════
        //  SEARCH, FILTER & SORT
        // ═══════════════════════════════════════════════════

        private string _filterTab = "ALL"; // ALL, GROUP, PRIVATE

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (searchPlaceholder != null)
                searchPlaceholder.Visibility = string.IsNullOrEmpty(txtSearch.Text)
                    ? Visibility.Visible : Visibility.Collapsed;

            ApplyChannelFilter();
        }

        private void TabAll_Click(object sender, RoutedEventArgs e)
        {
            _filterTab = "ALL";
            SetActiveTab(tabAll, tabGroup, tabPrivate);
            ApplyChannelFilter();
        }

        private void TabGroup_Click(object sender, RoutedEventArgs e)
        {
            _filterTab = "GROUP";
            SetActiveTab(tabGroup, tabAll, tabPrivate);
            ApplyChannelFilter();
        }

        private void TabPrivate_Click(object sender, RoutedEventArgs e)
        {
            _filterTab = "PRIVATE";
            SetActiveTab(tabPrivate, tabAll, tabGroup);
            ApplyChannelFilter();
        }

        private void SetActiveTab(Button active, Button b1, Button b2)
        {
            active.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            active.Foreground = Brushes.White;
            b1.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            b1.Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97));
            b2.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            b2.Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97));
        }

        private void SortOnline_Click(object sender, RoutedEventArgs e) => ApplyChannelFilter();
        private void SortName_Click(object sender, RoutedEventArgs e) => SortChannels("NAME");
        private void SortRecent_Click(object sender, RoutedEventArgs e) => SortChannels("RECENT");
        private void SortUnread_Click(object sender, RoutedEventArgs e) => SortChannels("UNREAD");

        private void SortChannels(string mode)
        {
            var items = _channels.ToList();
            switch (mode)
            {
                case "NAME":
                    items = items.OrderBy(c => c.DisplayName).ToList();
                    break;
                case "RECENT":
                    items = items.OrderByDescending(c => c.LastMessage?.Length > 0 ? 1 : 0).ToList();
                    break;
                case "UNREAD":
                    items = items.OrderByDescending(c => c.UnreadCount).ToList();
                    break;
            }
            _channels.Clear();
            foreach (var c in items) _channels.Add(c);
        }

        private void ApplyChannelFilter()
        {
            string search = txtSearch?.Text?.Trim().ToLower() ?? "";
            var all = new List<StudentChannel>();

            // Rebuild from scratch
            BuildChannelsInternal(all);

            // Apply tab filter
            var filtered = all.Where(c =>
            {
                if (_filterTab == "GROUP")
                    return c.Code == "ALL" || c.Code.StartsWith("GROUP_") || c.Code.StartsWith("STUDY_");
                if (_filterTab == "PRIVATE")
                    return c.Code == "TEACHER";
                return true; // ALL
            });

            // Apply search
            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(c =>
                    c.DisplayName.ToLower().Contains(search) ||
                    c.Subtitle.ToLower().Contains(search));

            _channels.Clear();
            foreach (var c in filtered) _channels.Add(c);

            if (panelEmptyState != null)
            {
                panelEmptyState.Visibility = _channels.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void BuildChannelsInternal(List<StudentChannel> target)
        {
            target.Add(new StudentChannel
            {
                Code = "ALL",
                DisplayName = "Cả lớp",
                AvatarText = "🏫",
                AvatarBg = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Subtitle = "🏫 Nhắn tin chung với toàn bộ lớp",
                IsGroup = true
            });

            foreach (var grp in _customGroups)
            {
                bool isMember = grp.MemberCodes.Contains(_myCode);
                target.Add(new StudentChannel
                {
                    Code = $"GROUP_{grp.Id}",
                    DisplayName = $"{grp.Emoji} {grp.Name}",
                    AvatarText = grp.Emoji,
                    AvatarBg = GetBrushForCode(grp.Id),
                    Subtitle = $"📋 {grp.MemberCodes.Count} thành viên" + (isMember ? " · Bạn là TV" : ""),
                    IsGroup = true
                });
            }

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app.CurrentGroups?.Count > 0)
                {
                    foreach (var sg in app.CurrentGroups)
                    {
                        bool isMember = sg.Members.Any(m =>
                            m.Contains(_myCode) || m.Contains(_myName));
                        target.Add(new StudentChannel
                        {
                            Code = $"STUDY_{sg.Index}",
                            DisplayName = $"{sg.Emoji} {sg.Name}",
                            AvatarText = sg.Emoji,
                            AvatarBg = new SolidColorBrush(sg.HeaderColor),
                            Subtitle = $"📚 Nhóm HT · {sg.Members.Count} HS" + (isMember ? " · TV" : ""),
                            IsGroup = true
                        });
                    }
                }
            }
            catch { }

            target.Add(new StudentChannel
            {
                Code = "TEACHER",
                DisplayName = "👨‍🏫 Giáo viên",
                AvatarText = "GV",
                AvatarBg = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                Subtitle = "🔒 Tin nhắn riêng với giáo viên",
                IsGroup = false
            });
        }

        // ═══════════════════════════════════════════════════
        //  RENDER FILTERED MESSAGES
        // ═══════════════════════════════════════════════════

        private void RenderMessages()
        {
            messagesPanel.Children.Clear();

            // Filter messages for active channel
            var filtered = new List<StudentChatMsg>();
            foreach (var msg in _allMessages)
            {
                bool include = false;

                if (_activeChannel == "ALL")
                {
                    // Broadcast messages
                    include = msg.Channel == "ALL";
                }
                else if (_activeChannel == "TEACHER")
                {
                    // Private with GV: both teacher and student messages in TEACHER channel
                    include = msg.Channel == "TEACHER";
                }
                else if (_activeChannel.StartsWith("GROUP_"))
                {
                    // Custom group messages
                    include = msg.Channel == _activeChannel;
                }
                else if (_activeChannel.StartsWith("STUDY_"))
                {
                    // Study group messages
                    include = msg.Channel == _activeChannel;
                }

                if (include) filtered.Add(msg);
            }

            // Add welcome if empty
            if (filtered.Count == 0)
            {
                var channelName = _channels.FirstOrDefault(c => c.Code == _activeChannel)?.DisplayName ?? _activeChannel;
                AddSystemBubble($"💬 Chưa có tin nhắn nào trong \"{channelName}\". Hãy bắt đầu cuộc trò chuyện!");
            }

            // Render with date separators
            DateTime? lastDate = null;
            int msgCount = 0;

            foreach (var msg in filtered)
            {
                var msgDate = msg.Time.Date;
                if (lastDate == null || msgDate != lastDate.Value)
                {
                    string dateLabel;
                    if (msgDate == DateTime.Today) dateLabel = "Hôm nay";
                    else if (msgDate == DateTime.Today.AddDays(-1)) dateLabel = "Hôm qua";
                    else dateLabel = msgDate.ToString("ddd, dd/MM/yyyy");

                    AddDateSeparator(dateLabel);
                    lastDate = msgDate;
                }

                if (msg.IsMe)
                    AddStudentBubble(msg.Text, msg.Time, msg.IsFailed);
                else if (msg.IsTeacher)
                    AddTeacherBubble(msg.Text, msg.Time);
                else
                    AddOtherStudentBubble(msg.Author, msg.Text, msg.Time);

                msgCount++;
            }

            // Update stats
            txtStats.Text = $"{msgCount} tin nhắn";
            var activeCh = _channels.FirstOrDefault(c => c.Code == _activeChannel);
            txtMembers.Text = activeCh?.IsGroup == true
                ? $"👥 Cả nhóm" : $"👤 GV + Em";

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                () => chatScroll.ScrollToEnd());
        }

        // ═══════════════════════════════════════════════════
        //  SEND MESSAGE
        // ═══════════════════════════════════════════════════

        private void SendMessage_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Return) && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void TxtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (inputPlaceholder != null)
                inputPlaceholder.Visibility = string.IsNullOrEmpty(txtMessage.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        private void QuickLike_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text = "👍";
            SendMessage();
        }

        private void btnToggleReactionPopover_Click(object sender, RoutedEventArgs e)
        {
            popQuickReactions.IsOpen = !popQuickReactions.IsOpen;
        }

        private void ReactionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string reactionText = btn.Content.ToString() ?? "";
                if (this.Resources["VibrateStickerAnim"] is Storyboard sb)
                {
                    Storyboard.SetTarget(sb, btn);
                    EventHandler? handler = null;
                    handler = (s, ev) =>
                    {
                        sb.Completed -= handler;
                        popQuickReactions.IsOpen = false;
                        txtMessage.Text = reactionText;
                        SendMessage();
                        TriggerReactionFeedback(reactionText);
                    };
                    sb.Completed += handler;
                    sb.Begin();
                }
                else
                {
                    popQuickReactions.IsOpen = false;
                    txtMessage.Text = reactionText;
                    SendMessage();
                    TriggerReactionFeedback(reactionText);
                }
            }
        }

        private void TriggerReactionFeedback(string reactionText)
        {
            try
            {
                string emoji = "";
                Button? targetEmojiBtn = null;

                if (reactionText.Contains("Đã hiểu"))
                {
                    emoji = "💡";
                    targetEmojiBtn = btnEmojiLight;
                }
                else if (reactionText.Contains("Chưa hiểu"))
                {
                    emoji = "❓";
                    targetEmojiBtn = btnEmojiQuestion;
                }
                else if (reactionText.Contains("trợ giúp"))
                {
                    emoji = "🙋";
                    targetEmojiBtn = btnEmojiRaise;
                }
                else if (reactionText.Contains("Tuyệt vời"))
                {
                    emoji = "💡";
                    targetEmojiBtn = btnEmojiLight;
                }

                if (!string.IsNullOrEmpty(emoji) && targetEmojiBtn != null)
                {
                    // Check cooldown (3 seconds)
                    if (DateTime.Now - _lastFeedbackTime >= TimeSpan.FromSeconds(3))
                    {
                        _lastFeedbackTime = DateTime.Now;
                        var app = (QASmartTouch.App)Application.Current;
                        if (app?.StudentNetwork != null && app.StudentNetwork.IsConnected)
                        {
                            string studentCode = app.StudentNetwork.StudentCode ?? _myCode;
                            string msg = $"STUDENT_FEEDBACK|{studentCode}|{emoji}";
                            app.StudentNetwork.SendAsync(msg);
                            
                            AddSystemBubble(_isAnonymousFeedback 
                                ? $"💡 Bạn đã gửi phản hồi (ẩn danh): {emoji}"
                                : $"💡 Bạn đã gửi phản hồi: {emoji}");

                            StartVisualCooldown(targetEmojiBtn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("TriggerReactionFeedback error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════
        //  CHAT INPUT BAR — Icon feature handlers
        // ═══════════════════════════════════════════════════

        private void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Đính kèm tệp tài liệu học tập",
                Filter = "Tài liệu học tập (*.pdf;*.docx;*.xlsx;*.pptx)|*.pdf;*.docx;*.xlsx;*.pptx|Ảnh (*.png;*.jpg;*.gif)|*.png;*.jpg;*.gif",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                    if (ForbiddenFileExtensions.Contains(ext))
                    {
                        MessageBox.Show("⚠️ Định dạng tệp tin này không được phép đính kèm vì lý do an toàn bảo mật lớp học!", 
                                        "Cảnh báo Bảo mật", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var fileInfo = new System.IO.FileInfo(dlg.FileName);
                    if (fileInfo.Length > 10 * 1024 * 1024)
                    {
                        MessageBox.Show("⚠️ Dung lượng tệp đính kèm không được vượt quá 10MB để bảo đảm băng thông lớp học!", 
                                        "Tệp quá lớn", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch { }

                string fileName = System.IO.Path.GetFileName(dlg.FileName);
                txtMessage.Text = $"📄 [File: {fileName}]";
                txtMessage.Focus();
                txtMessage.CaretIndex = txtMessage.Text.Length;
            }
        }

        private void AttachMedia_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Đính kèm ảnh / media",
                Filter = "Ảnh & Video (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.mp3)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.mp3",
                Multiselect = false
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                    if (ForbiddenFileExtensions.Contains(ext))
                    {
                        MessageBox.Show("⚠️ Định dạng tệp tin này không được phép đính kèm vì lý do an toàn bảo mật lớp học!", 
                                        "Cảnh báo Bảo mật", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var fileInfo = new System.IO.FileInfo(dlg.FileName);
                    if (fileInfo.Length > 10 * 1024 * 1024)
                    {
                        MessageBox.Show("⚠️ Dung lượng tệp đính kèm không được vượt quá 10MB để bảo đảm băng thông lớp học!", 
                                        "Tệp quá lớn", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch { }

                string fileName = System.IO.Path.GetFileName(dlg.FileName);
                string extStr = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                string icon = extStr switch
                {
                    ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "🖼️",
                    ".mp4" or ".avi" or ".mov" => "🎬",
                    ".mp3" or ".wav" or ".ogg" => "🎵",
                    _ => "📎"
                };
                txtMessage.Text = $"{icon} [Media: {fileName}]";
                txtMessage.Focus();
                txtMessage.CaretIndex = txtMessage.Text.Length;
            }
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
            mainStack.Children.Add(new TextBlock
            {
                Text = "😊 Biểu cảm nhanh",
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                Margin = new Thickness(4, 0, 0, 6)
            });

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
                        txtMessage.Text += em;
                        txtMessage.Focus();
                        txtMessage.CaretIndex = txtMessage.Text.Length;
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

            var stickers = new (string label, string[] items)[]
            {
                ("Chào hỏi", new[] { "👋😊", "🙋‍♂️", "🙋‍♀️", "🤗", "🫡👋", "✋😄" }),
                ("Tuyệt vời", new[] { "🎉🥳", "💯🔥", "⭐✨", "🏆👏", "💪😎", "🚀🌟" }),
                ("Đáng yêu", new[] { "🐱💕", "🐶❤️", "🐰🌸", "🦊💛", "🐻🧡", "🐼💚" }),
                ("Học tập", new[] { "📚✍️", "🎓📝", "💡🤔", "📖👀", "🧠💫", "✅👍" }),
                ("Cảm xúc", new[] { "😂🤣", "😭💔", "😱😨", "😤🔥", "🥺🥹", "😴💤" })
            };

            foreach (var group in stickers)
            {
                var groupStack = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
                groupStack.Children.Add(new TextBlock
                {
                    Text = group.label, FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
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
                        txtMessage.Text = sticker;
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

        private async void SendMessage()
        {
            if (_isMuted)
            {
                MessageBox.Show("⚠️ Giáo viên đã tạm khóa chat lớp học. Bạn không thể gửi tin nhắn lúc này!", "Đã khóa chat", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var text = txtMessage.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // CẢI TIẾN: Thực hiện lọc từ ngữ thô tục bảo vệ môi trường sư phạm
            string filteredText = FilterProfanity(text);

            // Sanitize message text to prevent HTML injection/XSS issues
            string cleanText = PathHelper.SanitizeInput(filteredText, 500);
            if (string.IsNullOrEmpty(cleanText)) return;

            // Channel = the active channel the student is viewing
            // This ensures the message appears in the correct channel filter
            string msgChannel = _activeChannel;

            var chatMsg = new StudentChatMsg
            {
                Author = _myName,
                Text = cleanText,
                Time = DateTime.Now,
                IsTeacher = false,
                IsMe = true,
                Channel = msgChannel
            };
            _allMessages.Add(chatMsg);

            // Update last message preview on sidebar
            var activeChannelObj = _channels.FirstOrDefault(c => c.Code == msgChannel);
            if (activeChannelObj != null)
                activeChannelObj.LastMessage = $"Bạn: {(cleanText.Length > 20 ? cleanText.Substring(0, 20) + "..." : cleanText)}";

            RenderMessages();

            // Clear input immediately
            txtMessage.Clear();
            txtMessage.Focus();

            bool sendSuccess = false;
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var client = app.StudentNetwork;
                if (client != null && client.IsConnected)
                {
                    await client.SendChat(cleanText, _activeChannel);
                    sendSuccess = true;
                }
                else
                {
                    var net = app.NetworkService;
                    if (net != null && net.IsBroadcasting)
                    {
                        await net.BroadcastMessage($"💬 HS: {cleanText}");
                        sendSuccess = true;
                    }
                }

                // CẢI TIẾN: Thực hiện ghi DB không chặn bằng Task.Run (Background thread)
                if (app?.Database != null)
                {
                    string actorName = _myCode;
                    string identityStr = "";
                    try
                    {
                        var student = app.Database.Students.FirstOrDefault(s => s.StudentCode == _myCode);
                        if (student != null)
                            identityStr = $" [{student.ChatIdentity}]";
                    }
                    catch { }

                    string finalIdentityStr = identityStr;
                    string dbChannel = msgChannel == "TEACHER" ? _myCode : msgChannel;

                    _ = Task.Run(async () =>
                    {
                        await QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.WaitAsync();
                        try
                        {
                            using (var dbContext = new QASmartClass.Data.AppDbContext())
                            {
                                dbContext.EventLogs.Add(new EventLog
                                {
                                    EventType = msgChannel == "TEACHER" ? "PRIVATE_CHAT" : "CHAT",
                                    Actor = actorName,
                                    Details = $"Tin nhắn: {cleanText}{finalIdentityStr} [CH:{dbChannel}]",
                                    Timestamp = DateTime.Now
                                });
                                dbContext.SaveChanges();
                            }
                        }
                        catch (Exception exDb)
                        {
                            Log.Warning("Failed to save sent message to DB: {Err}", exDb.Message);
                        }
                        finally
                        {
                            QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.Release();
                        }
                    });
                }
            }
            catch (Exception ex) 
            { 
                Log.Warning("Chat send error: {Err}", ex.Message); 
                sendSuccess = false;
            }

            if (!sendSuccess)
            {
                chatMsg.IsFailed = true;
                RenderMessages();
            }
        }

        // ═══════════════════════════════════════════════════
        //  BUBBLE BUILDERS
        // ═══════════════════════════════════════════════════

        private void AddDateSeparator(string label)
        {
            var container = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 6)
            };
            var inner = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 3, 16, 3)
            };
            inner.Child = new TextBlock
            {
                Text = label, FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                FontWeight = FontWeights.SemiBold
            };
            container.Child = inner;
            messagesPanel.Children.Add(container);
        }

        private void AddSystemBubble(string text)
        {
            var container = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 8)
            };
            var inner = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(232, 234, 246)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 5, 14, 5)
            };
            inner.Child = new TextBlock
            {
                Text = text, FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(92, 107, 192)),
                FontStyle = FontStyles.Italic
            };
            container.Child = inner;
            messagesPanel.Children.Add(container);
        }

        private void AddStudentBubble(string text, DateTime? time = null, bool isFailed = false)
        {
            var ts = time ?? DateTime.Now;
            var bubble = new StackPanel { Margin = new Thickness(100, 2, 0, 2), HorizontalAlignment = HorizontalAlignment.Right };
            var border = new Border
            {
                CornerRadius = new CornerRadius(16, 4, 16, 16),
                Padding = new Thickness(14, 9, 14, 9),
                MaxWidth = 500 // CẢI TIẾN: Giới hạn chiều rộng tối đa bong bóng chat
            };
            
            if (isFailed)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(254, 237, 238)); // Hồng nhạt
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 83, 80));   // Viền đỏ
                border.BorderThickness = new Thickness(1);
            }
            else
            {
                border.Background = new LinearGradientBrush(
                    Color.FromRgb(25, 118, 210), Color.FromRgb(21, 101, 192), 0);
            }

            var innerStack = new StackPanel();
            innerStack.Children.Add(new TextBlock 
            { 
                Text = text, 
                FontSize = 15.5, // CẢI TIẾN: Nâng cỡ chữ đạt chuẩn sư phạm tiếng Việt
                Foreground = isFailed ? new SolidColorBrush(Color.FromRgb(198, 40, 40)) : Brushes.White, 
                TextWrapping = TextWrapping.Wrap 
            });

            string timeLabel = isFailed ? $"{ts:HH:mm}  ⚠️ Gửi lỗi (Không có kết nối)" : ts.ToString("HH:mm");
            innerStack.Children.Add(new TextBlock
            {
                Text = timeLabel, FontSize = 9,
                Foreground = isFailed ? new SolidColorBrush(Color.FromRgb(239, 83, 80)) : new SolidColorBrush(Color.FromRgb(187, 222, 251)),
                HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 2, 0, 0)
            });
            border.Child = innerStack;
            bubble.Children.Add(border);
            messagesPanel.Children.Add(bubble);
        }

        private void AddTeacherBubble(string text, DateTime? time = null)
        {
            var ts = time ?? DateTime.Now;
            var outer = new StackPanel { Margin = new Thickness(0, 2, 100, 2), HorizontalAlignment = HorizontalAlignment.Left };
            var dock = new DockPanel();

            // Avatar
            var avatar = new Border
            {
                Width = 34, Height = 34, CornerRadius = new CornerRadius(17),
                VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, 10, 0)
            };
            avatar.Background = new LinearGradientBrush(
                Color.FromRgb(46, 125, 50), Color.FromRgb(102, 187, 106), 0);
            avatar.Child = new TextBlock
            {
                Text = "GV", FontSize = 11, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(avatar, Dock.Left);
            dock.Children.Add(avatar);

            var contentStack = new StackPanel();
            var headerSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };
            headerSp.Children.Add(new TextBlock
            {
                Text = "Giáo viên", FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                Cursor = Cursors.Hand
            });
            headerSp.Children.Add(new TextBlock
            {
                Text = ts.ToString("HH:mm"), FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                Margin = new Thickness(8, 2, 0, 0), VerticalAlignment = VerticalAlignment.Bottom
            });
            contentStack.Children.Add(headerSp);

            var msgBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(4, 16, 16, 16),
                Padding = new Thickness(14, 9, 14, 9),
                MaxWidth = 500, // CẢI TIẾN: Giới hạn chiều rộng tối đa bong bóng chat
                HorizontalAlignment = HorizontalAlignment.Left,
                BorderBrush = new SolidColorBrush(Color.FromRgb(232, 232, 232)),
                BorderThickness = new Thickness(1)
            };
            msgBorder.Child = new TextBlock
            {
                Text = text, 
                FontSize = 15.5, // CẢI TIẾN: Nâng cỡ chữ đạt chuẩn sư phạm tiếng Việt
                Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                TextWrapping = TextWrapping.Wrap
            };
            contentStack.Children.Add(msgBorder);

            dock.Children.Add(contentStack);
            outer.Children.Add(dock);
            messagesPanel.Children.Add(outer);
        }

        private void AddOtherStudentBubble(string author, string text, DateTime? time = null)
        {
            var ts = time ?? DateTime.Now;
            var outer = new StackPanel { Margin = new Thickness(0, 2, 100, 2), HorizontalAlignment = HorizontalAlignment.Left };
            var dock = new DockPanel();

            var avatar = new Border
            {
                Width = 34, Height = 34, CornerRadius = new CornerRadius(17),
                Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, 10, 0)
            };
            var initials = author.Length >= 2 ? author.Substring(author.Length - 2) : author;
            avatar.Child = new TextBlock
            {
                Text = initials, FontSize = 11, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(avatar, Dock.Left);
            dock.Children.Add(avatar);

            var contentStack = new StackPanel();
            var headerSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };
            headerSp.Children.Add(new TextBlock
            {
                Text = author, FontSize = 12, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                Cursor = Cursors.Hand
            });
            headerSp.Children.Add(new TextBlock
            {
                Text = ts.ToString("HH:mm"), FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                Margin = new Thickness(8, 2, 0, 0), VerticalAlignment = VerticalAlignment.Bottom
            });
            contentStack.Children.Add(headerSp);

            var msgBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                CornerRadius = new CornerRadius(4, 16, 16, 16),
                Padding = new Thickness(14, 9, 14, 9),
                MaxWidth = 500, // CẢI TIẾN: Giới hạn chiều rộng tối đa bong bóng chat
                HorizontalAlignment = HorizontalAlignment.Left,
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 224, 178)),
                BorderThickness = new Thickness(1)
            };
            msgBorder.Child = new TextBlock
            {
                Text = text, 
                FontSize = 15.5, // CẢI TIẾN: Nâng cỡ chữ đạt chuẩn sư phạm tiếng Việt
                Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                TextWrapping = TextWrapping.Wrap
            };
            contentStack.Children.Add(msgBorder);

            dock.Children.Add(contentStack);
            outer.Children.Add(dock);
            messagesPanel.Children.Add(outer);
        }

        // ─── ClearChat ──────────────────────────────────
        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            var channelName = _channels.FirstOrDefault(c => c.Code == _activeChannel)?.DisplayName ?? _activeChannel;
            var result = MessageBox.Show(
                $"Xóa tất cả tin nhắn trong \"{channelName}\" khỏi màn hình?\n\n(Dữ liệu trong CSDL không bị xóa)",
                "Xóa tin nhắn",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _allMessages.RemoveAll(m => m.Channel == _activeChannel);
                RenderMessages();
            }
        }

        // ─── Helper ──────────────────────────────────────
        private Brush GetBrushForCode(string code)
        {
            string[] colors = { "#E91E63", "#9C27B0", "#673AB7", "#3F51B5",
                "#009688", "#FF5722", "#795548", "#607D8B" };
            int hash = code.GetHashCode();
            var color = colors[Math.Abs(hash) % colors.Length];
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        // ═══════════════════════════════════════════════════
        //  UNREAD TIMER — polls DB every 1 second
        // ═══════════════════════════════════════════════════

        private void StartUnreadTimer()
        {
            _lastKnownDbCount = _allMessages.Count;
            _unreadTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _unreadTimer.Tick += UnreadTimer_Tick;
            _unreadTimer.Start();
        }

        private async void UnreadTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                // CẢI TIẾN: Thực hiện đếm số dòng DB trên luồng nền (Task.Run) để tránh nghẽn luồng UI
                int currentDbCount = await Task.Run(async () =>
                {
                    await QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.WaitAsync();
                    try
                    {
                        using (var dbContext = new QASmartClass.Data.AppDbContext())
                        {
                            return dbContext.EventLogs
                                .Count(ev => ev.EventType == "CHAT"
                                          || ev.EventType == "TEACHER_CHAT"
                                          || ev.EventType == "PRIVATE_CHAT"
                                          || ev.EventType == "GROUP_CHAT");
                        }
                    }
                    finally
                    {
                        QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.Release();
                    }
                });

                if (currentDbCount <= _lastKnownDbCount) return;

                // Tin nhắn mới xuất hiện — lấy phần chênh lệch (delta) trên luồng nền
                var delta = currentDbCount - _lastKnownDbCount;
                _lastKnownDbCount = currentDbCount;

                var latestEvents = await Task.Run(async () =>
                {
                    await QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.WaitAsync();
                    try
                    {
                        using (var dbContext = new QASmartClass.Data.AppDbContext())
                        {
                            return dbContext.EventLogs
                                .Where(ev => ev.EventType == "CHAT"
                                          || ev.EventType == "TEACHER_CHAT"
                                          || ev.EventType == "PRIVATE_CHAT"
                                          || ev.EventType == "GROUP_CHAT")
                                .OrderByDescending(ev => ev.Timestamp)
                                .Take(delta)
                                .ToList();
                        }
                    }
                    finally
                    {
                        QASmartClass.Data.AppDbContext.BackgroundDbSemaphore.Release();
                    }
                });

                bool hasNewMessages = false;
                foreach (var ev in latestEvents)
                {
                    // Lọc trùng tin nhắn (nếu đã thêm cục bộ)
                    if (_allMessages.Any(m => Math.Abs((m.Time - ev.Timestamp).TotalSeconds) < 1
                        && m.Text.Length > 0 && ev.Details.Contains(m.Text.Substring(0, Math.Min(m.Text.Length, 10)))))
                        continue;

                    bool isTeacher = ev.Actor == "GV" || ev.EventType == "TEACHER_CHAT" || ev.EventType == "GROUP_CHAT";
                    string rawDetails = ev.Details;

                    // Parse kênh nhận tin
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
                        channel = "TEACHER";
                    }

                    if (channel == _myCode)
                        channel = "TEACHER";

                    // Bỏ qua tin nhắn do chính mình gửi (đã được render lập tức)
                    if (ev.Actor == _myCode) continue;

                    // Chuẩn hóa chuỗi hiển thị
                    string text = rawDetails;
                    if (text.StartsWith("Tin nhắn: "))
                        text = text.Substring("Tin nhắn: ".Length);
                    var tagIdx = text.IndexOf(" [CH:");
                    if (tagIdx >= 0)
                    {
                        var tagEnd = text.IndexOf("]", tagIdx);
                        if (tagEnd > tagIdx) text = text.Substring(0, tagIdx);
                    }
                    var bracketIdx = text.LastIndexOf(" [");
                    if (bracketIdx > 0 && text.EndsWith("]"))
                        text = text.Substring(0, bracketIdx);

                    if (string.IsNullOrWhiteSpace(text)) continue;

                    _allMessages.Add(new StudentChatMsg
                    {
                        Author = isTeacher ? "GV" : (ev.Actor ?? "HS"),
                        Text = text,
                        Time = ev.Timestamp,
                        IsTeacher = isTeacher,
                        IsMe = false,
                        Channel = channel
                    });

                    // Cập nhật số tin nhắn chưa đọc
                    if (channel != _activeChannel)
                    {
                        var ch = _channels.FirstOrDefault(c => c.Code == channel);
                        if (ch != null) ch.UnreadCount++;
                    }

                    // Cập nhật dòng preview tin nhắn mới nhất
                    var chP = _channels.FirstOrDefault(c => c.Code == channel);
                    if (chP != null)
                        chP.LastMessage = text.Length > 25 ? text.Substring(0, 25) + "..." : text;

                    hasNewMessages = true;
                }

                // Render lại nếu có tin nhắn mới thuộc kênh đang mở
                if (hasNewMessages)
                    RenderMessages();
            }
            catch (Exception ex) { Log.Warning("UnreadTimer error: {Err}", ex.Message); }
        }

        public void SetChatMuteStatus(bool isMuted)
        {
            try
            {
                _isMuted = isMuted;
                txtMessage.IsEnabled = !isMuted;
                if (btnSendMessage != null)
                {
                    btnSendMessage.IsEnabled = !isMuted;
                }
                
                if (inputPlaceholder != null)
                {
                    inputPlaceholder.Text = isMuted 
                        ? "🔒 Giáo viên đã tạm khóa chat lớp học" 
                        : "Nhập nội dung tin nhắn và nhấn Enter để gửi...";
                }

                if (txtMessageBorder != null)
                {
                    txtMessageBorder.Background = isMuted 
                        ? new SolidColorBrush(Color.FromRgb(250, 235, 235)) 
                        : new SolidColorBrush(Color.FromRgb(245, 247, 250));
                    txtMessageBorder.BorderBrush = isMuted 
                        ? new SolidColorBrush(Color.FromRgb(239, 83, 80)) 
                        : Brushes.Transparent;
                    txtMessageBorder.BorderThickness = isMuted ? new Thickness(1) : new Thickness(0);
                }

                if (isMuted)
                {
                    txtMessage.Clear();
                }
            }
            catch { }
        }

        public void SetIndividualChatMuteStatus(string studentCode, bool isMuted)
        {
            try
            {
                if (string.Equals(studentCode, _myCode, StringComparison.OrdinalIgnoreCase))
                {
                    SetChatMuteStatus(isMuted);
                }
            }
            catch { }
        }

        public void SetAnonymousFeedbackStatus(bool isAnonymous)
        {
            try
            {
                _isAnonymousFeedback = isAnonymous;
                AddSystemBubble(isAnonymous 
                    ? "🔒 Giáo viên đã bật chế độ phản hồi ẩn danh cho cảm xúc học tập."
                    : "🔓 Giáo viên đã tắt chế độ phản hồi ẩn danh.");
            }
            catch { }
        }

        private void FeedbackEmoji_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn)
                {
                    string emoji = btn.Content.ToString();
                    if (string.IsNullOrEmpty(emoji)) return;

                    // Rate-limiting check: 3 seconds cooldown
                    if (DateTime.Now - _lastFeedbackTime < TimeSpan.FromSeconds(3))
                    {
                        MessageBox.Show("⏳ Vui lòng đợi một chút trước khi gửi lại phản hồi cảm xúc!", "Chậm lại một chút", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _lastFeedbackTime = DateTime.Now;

                    // Send STUDENT_FEEDBACK command via network
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.StudentNetwork != null && app.StudentNetwork.IsConnected)
                    {
                        string studentCode = app.StudentNetwork.StudentCode ?? _myCode;
                        string msg = $"STUDENT_FEEDBACK|{studentCode}|{emoji}";
                        app.StudentNetwork.SendAsync(msg);
                        
                        // Show visual confirm
                        AddSystemBubble(_isAnonymousFeedback 
                            ? $"💡 Bạn đã gửi phản hồi (ẩn danh): {emoji}"
                            : $"💡 Bạn đã gửi phản hồi: {emoji}");

                        // Start visual cooldown feedback
                        StartVisualCooldown(btn);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("FeedbackEmoji_Click error: {Err}", ex.Message);
            }
        }

        private void StartVisualCooldown(Button btn)
        {
            try
            {
                SetEmojiButtonsEnabled(false);
                _activeCooldownButton = btn;
                _cooldownSecondsRemaining = 3;
                
                _cooldownTimer?.Stop();
                _cooldownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _cooldownTimer.Tick += CooldownTimer_Tick;
                _cooldownTimer.Start();

                string sym = btn.Content.ToString() ?? "";
                btn.Content = $"{sym} (3s)";
            }
            catch { }
        }

        private void CooldownTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                _cooldownSecondsRemaining--;
                if (_cooldownSecondsRemaining <= 0)
                {
                    _cooldownTimer?.Stop();
                    
                    if (_activeCooldownButton != null)
                    {
                        if (_activeCooldownButton == btnEmojiLight) _activeCooldownButton.Content = "💡";
                        else if (_activeCooldownButton == btnEmojiQuestion) _activeCooldownButton.Content = "❓";
                        else if (_activeCooldownButton == btnEmojiRaise) _activeCooldownButton.Content = "🙋";
                        else if (_activeCooldownButton == btnEmojiWait) _activeCooldownButton.Content = "⏳";
                    }

                    SetEmojiButtonsEnabled(true);
                    _activeCooldownButton = null;
                }
                else
                {
                    if (_activeCooldownButton != null)
                    {
                        string sym = "💡";
                        if (_activeCooldownButton == btnEmojiLight) sym = "💡";
                        else if (_activeCooldownButton == btnEmojiQuestion) sym = "❓";
                        else if (_activeCooldownButton == btnEmojiRaise) sym = "🙋";
                        else if (_activeCooldownButton == btnEmojiWait) sym = "⏳";
                        
                        _activeCooldownButton.Content = $"{sym} ({_cooldownSecondsRemaining}s)";
                    }
                }
            }
            catch { }
        }

        private void SetEmojiButtonsEnabled(bool isEnabled)
        {
            try
            {
                if (btnEmojiLight != null) btnEmojiLight.IsEnabled = isEnabled;
                if (btnEmojiQuestion != null) btnEmojiQuestion.IsEnabled = isEnabled;
                if (btnEmojiRaise != null) btnEmojiRaise.IsEnabled = isEnabled;
                if (btnEmojiWait != null) btnEmojiWait.IsEnabled = isEnabled;
            }
            catch { }
        }
    }

    // ═══════════════════════════════════════════════════
    //  DATA MODELS
    // ═══════════════════════════════════════════════════

    public class StudentChannel : INotifyPropertyChanged
    {
        public string Code { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string AvatarText { get; set; } = "?";
        public Brush AvatarBg { get; set; } = Brushes.Gray;
        public string Subtitle { get; set; } = "";
        public bool IsGroup { get; set; }

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set { _unreadCount = value; OnPropertyChanged(nameof(UnreadCount)); }
        }

        private string _lastMessage = "";
        public string LastMessage
        {
            get => _lastMessage;
            set
            {
                _lastMessage = value;
                LastTimeStr = DateTime.Now.ToString("HH:mm");
                OnPropertyChanged(nameof(LastMessage));
            }
        }

        private string _lastTimeStr = "";
        public string LastTimeStr
        {
            get => _lastTimeStr;
            set { _lastTimeStr = value; OnPropertyChanged(nameof(LastTimeStr)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class StudentChatMsg
    {
        public string Author { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Time { get; set; }
        public bool IsTeacher { get; set; }
        public bool IsMe { get; set; }
        public string Channel { get; set; } = "ALL";
        public bool IsFailed { get; set; } = false;
    }
}






