using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Serilog;
using System.Threading.Tasks;

namespace QASmartClass.Classroom.Views
{
    public partial class GroupPage : Page
    {
        private List<string> _studentNames = new();
        private int _groupCount = 3;
        private List<GroupVm> _groups = new();
        private List<string> _unassigned = new();

        // Drag state
        private string? _dragStudent;
        private int _dragFromGroup = -1; // -1 = unassigned pool

        private static readonly (Color header, Color border, string emoji)[] GroupStyles =
        {
            (Color.FromRgb(25,118,210),  Color.FromRgb(187,222,251), "🦁"),
            (Color.FromRgb(46,125,50),   Color.FromRgb(165,214,167), "🦊"),
            (Color.FromRgb(230,81,0),    Color.FromRgb(255,204,128), "🐬"),
            (Color.FromRgb(123,31,162),  Color.FromRgb(206,147,216), "🦅"),
            (Color.FromRgb(198,40,40),   Color.FromRgb(239,154,154), "🐺"),
            (Color.FromRgb(0,131,143),   Color.FromRgb(128,222,234), "🦋"),
        };

        private static readonly string[] DefaultNames =
            { "Sư Tử", "Cáo", "Cá Heo", "Đại Bàng", "Sói", "Bướm" };

        public GroupPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> names = null!;
                await Task.Run(() =>
                {
                    names = QASmartClass.Classroom.Services.RosterHelper.GetStudentNames();
                });

                _studentNames = names != null && names.Any() ? names : new List<string>
                {
                    "Nguyễn Văn An","Trần Thị Bình","Lê Hoàng Cường","Phạm Thị Dung",
                    "Hoàng Văn Em","Ngô Thị Phương","Đỗ Quang Hải","Vũ Thị Hoa",
                    "Bùi Đức Khang","Lý Thị Lan","Mai Văn Minh","Đinh Thị Ngọc",
                    "Trương Quốc Phong","Đặng Thị Quỳnh","Hà Sĩ Ren","Cao Thị Sen",
                    "Tô Văn Tuấn","Phan Thị Uyên","Lương Văn Vũ","Châu Thị Xuân"
                };

                await LoadExistingGroupsOrRandomAsync();
            }
            catch (Exception ex)
            {
                Log.Warning("Page_Loaded error: {Err}", ex.Message);
            }
        }

        private async System.Threading.Tasks.Task LoadExistingGroupsOrRandomAsync()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var activeRoster = app.ClassRoster.ActiveRoster;
                int rosterId = activeRoster?.Id ?? 0;
                
                if (rosterId == 0)
                {
                    DoRandomAssign();
                    return;
                }
                
                List<GroupVm>? loadedGroups = null;

                // 1. Kiểm tra trong bộ nhớ RAM
                if (app.CurrentGroups != null && app.CurrentGroups.Count > 0)
                {
                    var groupStudents = app.CurrentGroups.SelectMany(g => g.Members).ToList();
                    bool isSameRoster = groupStudents.Count > 0 && groupStudents.All(name => _studentNames.Contains(name));
                    if (isSameRoster)
                    {
                        loadedGroups = app.CurrentGroups;
                    }
                }

                // 2. Kiểm tra trong tệp tin đĩa cứng
                if (loadedGroups == null)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            var filePath = System.IO.Path.Combine(QASmartClass.Services.AppPaths.SettingsDir, $"groups_roster_{rosterId}.json");
                            if (System.IO.File.Exists(filePath))
                            {
                                var json = System.IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                                loadedGroups = System.Text.Json.JsonSerializer.Deserialize<List<GroupVm>>(json);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning("Failed to load groups from file: {Err}", ex.Message);
                        }
                    });
                }

                // 3. Khôi phục cấu trúc nhóm nếu tìm thấy dữ liệu hợp lệ
                if (loadedGroups != null && loadedGroups.Count > 0)
                {
                    _groups = loadedGroups.Select(g => 
                    {
                        var (hc, bc, _) = GroupStyles[g.Index % GroupStyles.Length];
                        var activeMembers = g.Members.Where(m => _studentNames.Contains(m)).ToList();
                        return new GroupVm
                        {
                            Index = g.Index,
                            Name = g.Name,
                            Emoji = g.Emoji,
                            Slogan = g.Slogan,
                            Leader = (activeMembers.Contains(g.Leader) && _studentNames.Contains(g.Leader)) ? g.Leader : "",
                            Task = g.Task,
                            Members = activeMembers,
                            HeaderColor = hc,
                            BorderColor = bc
                        };
                    }).ToList();
                    
                    _groupCount = _groups.Count;
                    
                    if (cmbGroupCount != null)
                    {
                        foreach (ComboBoxItem item in cmbGroupCount.Items)
                        {
                            if (int.TryParse(item.Content?.ToString(), out int val) && val == _groupCount)
                            {
                                cmbGroupCount.SelectionChanged -= GroupCount_Changed;
                                cmbGroupCount.SelectedItem = item;
                                cmbGroupCount.SelectionChanged += GroupCount_Changed;
                                break;
                            }
                        }
                    }

                    var allAssigned = _groups.SelectMany(g => g.Members).ToList();
                    _unassigned = _studentNames.Where(name => !allAssigned.Contains(name)).ToList();
                    
                    RenderAll();
                    Log.Information("Restored existing groups for roster: {RosterId}", rosterId);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error loading existing groups: {Err}", ex.Message);
            }

            // Fallback nếu không có dữ liệu cũ
            DoRandomAssign();
        }

        // ═══════════════════════════════════════════════════════════
        //  RANDOM ASSIGN
        // ═══════════════════════════════════════════════════════════

        private void DoRandomAssign()
        {
            var shuffled = _studentNames.OrderBy(_ => Guid.NewGuid()).ToList();
            _groups = new List<GroupVm>();
            _unassigned = new List<string>();

            for (int g = 0; g < _groupCount; g++)
            {
                int start = g * shuffled.Count / _groupCount;
                int end = (g + 1) * shuffled.Count / _groupCount;
                var members = shuffled.GetRange(start, end - start);
                var (hc, bc, emoji) = GroupStyles[g % GroupStyles.Length];

                _groups.Add(new GroupVm
                {
                    Index = g,
                    Name = DefaultNames[g % DefaultNames.Length],
                    Emoji = emoji,
                    Members = members,
                    HeaderColor = hc,
                    BorderColor = bc,
                    Leader = members.FirstOrDefault() ?? ""
                });
            }
            RenderAll();
        }

        // ═══════════════════════════════════════════════════════════
        //  RENDER ALL
        // ═══════════════════════════════════════════════════════════

        private void RenderAll()
        {
            RenderUnassigned();
            RenderGroups();
            UpdateSummary();
            SaveGroupsSilence();
        }

        private void UpdateSummary()
        {
            int assigned = _groups.Sum(g => g.Members.Count);
            if (txtGroupSummary != null)
            {
                txtGroupSummary.Text = $"{_groups.Count} nhóm · {assigned} HS đã phân · {_unassigned.Count} chưa phân · Tổng: {_studentNames.Count} HS";
            }

            // Sync groups to App for other pages (BroadcastPage etc.)
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                app.CurrentGroups = new List<GroupVm>(_groups);
            }
            catch { }
        }

        // ── Unassigned panel ──────────────────────────────────────

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

            foreach (char c in normalizedString)
            {
                System.Globalization.UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ' || c == 'Đ')
                    {
                        stringBuilder.Append(c == 'đ' ? 'd' : 'D');
                    }
                    else
                    {
                        stringBuilder.Append(c);
                    }
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private void RenderUnassigned()
        {
            if (unassignedPanel == null) return;
            unassignedPanel.Children.Clear();
            
            var filterText = tbSearchUnassigned != null && tbSearchUnassigned.Text != "🔍 Nhập tên để tìm..." 
                ? tbSearchUnassigned.Text.Trim() 
                : "";
                
            var normalizedSearch = RemoveDiacritics(filterText);
            var filtered = string.IsNullOrWhiteSpace(filterText)
                ? _unassigned
                : _unassigned.Where(name => RemoveDiacritics(name).IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (txtUnassignedCount != null)
            {
                txtUnassignedCount.Text = filtered.Count.ToString();
            }

            foreach (var name in filtered)
            {
                var card = MakeStudentChip(name, isPool: true);
                card.MouseLeftButtonDown += (s, e) =>
                {
                    _dragStudent = name;
                    _dragFromGroup = -1;
                    DragDrop.DoDragDrop(card, name, DragDropEffects.Move);
                };
                unassignedPanel.Children.Add(card);
            }
        }

        private void SearchUnassigned_TextChanged(object sender, TextChangedEventArgs e)
        {
            RenderUnassigned();
        }

        private void Search_GotFocus(object sender, RoutedEventArgs e)
        {
            if (tbSearchUnassigned != null && tbSearchUnassigned.Text == "🔍 Nhập tên để tìm...")
            {
                tbSearchUnassigned.Text = "";
                tbSearchUnassigned.Foreground = Brushes.Black;
            }
        }

        private void Search_LostFocus(object sender, RoutedEventArgs e)
        {
            if (tbSearchUnassigned != null && string.IsNullOrWhiteSpace(tbSearchUnassigned.Text))
            {
                tbSearchUnassigned.Text = "🔍 Nhập tên để tìm...";
                tbSearchUnassigned.Foreground = Brushes.Gray;
            }
        }

        // ── Groups panel ──────────────────────────────────────────

        private void RenderGroups()
        {
            if (groupsGrid == null) return;
            groupsGrid.Children.Clear();
            groupsGrid.ColumnDefinitions.Clear();
            groupsGrid.RowDefinitions.Clear();
            // Single star-height row so cards stretch vertically
            groupsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < _groups.Count; i++)
            {
                // Add group column
                groupsGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 180 });

                // Add splitter column (except after last group)
                if (i < _groups.Count - 1)
                    groupsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            int colIdx = 0;
            for (int i = 0; i < _groups.Count; i++)
            {
                var card = BuildGroupCard(_groups[i]);
                Grid.SetColumn(card, colIdx);
                groupsGrid.Children.Add(card);

                if (i < _groups.Count - 1)
                {
                    // Add GridSplitter between groups
                    var splitter = new GridSplitter
                    {
                        Width = 4, HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Background = new SolidColorBrush(Color.FromRgb(207, 216, 220)),
                        Cursor = Cursors.SizeWE, Margin = new Thickness(2, 0, 2, 0)
                    };
                    splitter.MouseEnter += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
                    splitter.MouseLeave += (s, e) => splitter.Background = new SolidColorBrush(Color.FromRgb(207, 216, 220));
                    Grid.SetColumn(splitter, colIdx + 1);
                    groupsGrid.Children.Add(splitter);
                }

                colIdx += 2; // skip splitter column
            }
        }

        private Border BuildGroupCard(GroupVm grp)
        {
            var (_, borderClr, _) = GroupStyles[grp.Index % GroupStyles.Length];
            var card = new Border
            {
                Margin = new Thickness(0, 0, 4, 0),
                CornerRadius = new CornerRadius(10),
                BorderBrush = new SolidColorBrush(borderClr), BorderThickness = new Thickness(1.5),
                Background = Brushes.White, AllowDrop = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 2, Opacity = 0.07 }
            };

            // Accept drop
            card.DragEnter += (s, e) => { card.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233)); e.Handled = true; };
            card.DragLeave += (s, e) => { card.Background = Brushes.White; };
            card.Drop += (s, e) =>
            {
                card.Background = Brushes.White;
                if (_dragStudent != null)
                {
                    // Remove from source
                    if (_dragFromGroup == -1) _unassigned.Remove(_dragStudent);
                    else _groups[_dragFromGroup].Members.Remove(_dragStudent);

                    // Add to this group
                    if (!grp.Members.Contains(_dragStudent))
                        grp.Members.Add(_dragStudent);

                    _dragStudent = null;
                    RenderAll();
                }
            };

            // Use Grid instead of StackPanel so members list can stretch to fill space
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // slogan+task+leader
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // members (fills remaining)

            var infoStack = new StackPanel(); // slogan + task + leader go here

            // ── Header (colored) ──
            var header = new Border
            {
                CornerRadius = new CornerRadius(10, 10, 0, 0), Padding = new Thickness(10, 7, 10, 7),
                Background = new SolidColorBrush(grp.HeaderColor)
            };
            var headerStack = new DockPanel();

            // Count badge
            var countBadge = new TextBlock
            {
                Text = $"{grp.Members.Count} HS", FontSize = 10, Foreground = Brushes.White,
                Opacity = 0.85, VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(countBadge, Dock.Right);
            headerStack.Children.Add(countBadge);

            // Edit name button
            var editBtn = new Button
            {
                Content = "✏️", FontSize = 10, Background = Brushes.Transparent,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand,
                Foreground = Brushes.White, Padding = new Thickness(2), Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Đổi tên / cài đặt nhóm"
            };
            editBtn.Click += (s, e) => EditGroupSettings(grp);
            DockPanel.SetDock(editBtn, Dock.Right);
            headerStack.Children.Add(editBtn);

            // Group name
            headerStack.Children.Add(new TextBlock
            {
                Text = $"{grp.Emoji} {grp.Name}",
                FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Child = headerStack;
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // ── Slogan ──
            if (!string.IsNullOrWhiteSpace(grp.Slogan))
            {
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"🏷️ \"{grp.Slogan}\"", FontSize = 12, FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                    Margin = new Thickness(10, 4, 10, 0), TextWrapping = TextWrapping.Wrap
                });
            }

            // ── Task if any ──
            if (!string.IsNullOrWhiteSpace(grp.Task))
            {
                var taskBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(255, 248, 225)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(8, 4, 8, 0)
                };
                taskBorder.Child = new TextBlock
                {
                    Text = $"📋 {grp.Task}", FontSize = 12, TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
                };
                infoStack.Children.Add(taskBorder);
            }

            // ── Leader badge ──
            if (!string.IsNullOrWhiteSpace(grp.Leader) && grp.Members.Contains(grp.Leader))
            {
                var leaderRow = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(8, 4, 8, 2)
                };
                leaderRow.Child = new TextBlock
                {
                    Text = $"⭐ Nhóm trưởng: {grp.Leader}",
                    FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192))
                };
                infoStack.Children.Add(leaderRow);
            }

            Grid.SetRow(infoStack, 1);
            mainGrid.Children.Add(infoStack);

            // ── Members list ──
            // Members scroll fills remaining vertical space (no MaxHeight cap)
            var memberScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var memberStack = new StackPanel { Margin = new Thickness(6, 4, 6, 6) };

            if (grp.Members.Count == 0)
            {
                var placeholder = new Border
                {
                    Background = Brushes.Transparent,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(207, 216, 220)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 10, 8, 10),
                    Margin = new Thickness(0, 4, 0, 4),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                placeholder.Child = new TextBlock
                {
                    Text = "➕ Thả học sinh vào đây",
                    FontSize = 11,
                    Foreground = Brushes.DarkGray,
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                memberStack.Children.Add(placeholder);
            }

            foreach (var name in grp.Members)
            {
                var chip = MakeStudentChip(name, isPool: false, isLeader: name == grp.Leader);
                int gIdx = grp.Index;

                // Drag to move out of this group + Double click to toggle leader
                chip.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        grp.Leader = grp.Leader == name ? "" : name;
                        RenderAll();
                        e.Handled = true;
                    }
                    else
                    {
                        _dragStudent = name;
                        _dragFromGroup = gIdx;
                        DragDrop.DoDragDrop(chip, name, DragDropEffects.Move);
                    }
                };

                // Right-click context menu
                var ctx = new ContextMenu();
                var menuLeader = new MenuItem { Header = name == grp.Leader ? "⭐ Bỏ nhóm trưởng" : "⭐ Chọn làm nhóm trưởng" };
                menuLeader.Click += (s, e2) =>
                {
                    grp.Leader = name == grp.Leader ? "" : name;
                    RenderAll();
                };
                ctx.Items.Add(menuLeader);

                var menuRemove = new MenuItem { Header = "↩️ Đưa về chưa phân nhóm" };
                menuRemove.Click += (s, e2) =>
                {
                    grp.Members.Remove(name);
                    _unassigned.Add(name);
                    RenderAll();
                };
                ctx.Items.Add(menuRemove);

                // Transfer submenu
                var menuTransfer = new MenuItem { Header = "➡️ Chuyển sang nhóm khác" };
                foreach (var otherGrp in _groups)
                {
                    if (otherGrp.Index == gIdx) continue;
                    var target = otherGrp;
                    var mi = new MenuItem { Header = $"{target.Emoji} {target.Name}" };
                    mi.Click += (s, e2) =>
                    {
                        grp.Members.Remove(name);
                        target.Members.Add(name);
                        RenderAll();
                    };
                    menuTransfer.Items.Add(mi);
                }
                ctx.Items.Add(menuTransfer);
                chip.ContextMenu = ctx;

                memberStack.Children.Add(chip);
            }
            memberScroll.Content = memberStack;
            Grid.SetRow(memberScroll, 2);
            mainGrid.Children.Add(memberScroll);

            card.Child = mainGrid;
            return card;
        }

        private Border MakeStudentChip(string name, bool isPool, bool isLeader = false)
        {
            var chip = new Border
            {
                Background = isLeader ? new SolidColorBrush(Color.FromRgb(255, 248, 225)) : Brushes.White,
                CornerRadius = new CornerRadius(5),
                BorderBrush = new SolidColorBrush(isLeader ? Color.FromRgb(255, 193, 7) : Color.FromRgb(238, 238, 238)),
                BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(8, 4, 8, 4), Cursor = Cursors.Hand
            };
            var dp = new DockPanel();

            // Avatar circle
            var avatar = new Border
            {
                Width = 22, Height = 22, CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromRgb(
                    (byte)(Math.Abs(name.GetHashCode()) % 100 + 100),
                    (byte)(Math.Abs(name.GetHashCode() >> 8) % 100 + 120),
                    (byte)(Math.Abs(name.GetHashCode() >> 16) % 100 + 140))),
                Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center
            };
            avatar.Child = new TextBlock
            {
                Text = name.Length > 0 ? name[0].ToString().ToUpper() : "?",
                FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(avatar, Dock.Left);
            dp.Children.Add(avatar);

            if (isLeader) dp.Children.Add(new TextBlock { Text = "⭐", FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 3, 0) });

            dp.Children.Add(new TextBlock
            {
                Text = name, FontSize = 12.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            chip.Child = dp;

            // Hover effect
            chip.MouseEnter += (s, e) => chip.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            chip.MouseLeave += (s, e) => chip.Background = isLeader ? new SolidColorBrush(Color.FromRgb(255, 248, 225)) : Brushes.White;

            return chip;
        }

        // ═══════════════════════════════════════════════════════════
        //  EDIT GROUP SETTINGS (rename, emoji, slogan, leader, task)
        // ═══════════════════════════════════════════════════════════

        private void EditGroupSettings(GroupVm grp)
        {
            var wnd = new Window
            {
                Title = $"⚙️ Cài đặt nhóm: {grp.Name}",
                Width = 420, Height = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            // Name
            sp.Children.Add(MakeLabel("📝 Tên nhóm:"));
            var tbName = new TextBox { Text = grp.Name, FontSize = 14, Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 0, 8) };
            sp.Children.Add(tbName);

            // Emoji (icon)
            sp.Children.Add(MakeLabel("🎨 Biểu tượng nhóm:"));
            var emojiPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            var emojis = new[] { "🦁", "🦊", "🐬", "🦅", "🐺", "🦋", "🐯", "🐼", "🦄", "🐉", "🦈", "🐧", "🦜", "🐝", "🌟", "⚡" };
            var selectedEmoji = grp.Emoji;
            foreach (var em in emojis)
            {
                var emBtn = new Button
                {
                    Content = em, FontSize = 18, Width = 36, Height = 36,
                    Background = em == grp.Emoji ? new SolidColorBrush(Color.FromRgb(227, 242, 253)) : Brushes.White,
                    BorderBrush = em == grp.Emoji ? new SolidColorBrush(Color.FromRgb(25, 118, 210)) : new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderThickness = new Thickness(em == grp.Emoji ? 2 : 1),
                    Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 3, 3), Padding = new Thickness(0)
                };
                emBtn.Click += (s, e) =>
                {
                    selectedEmoji = em;
                    foreach (Button b in emojiPanel.Children)
                    {
                        b.Background = b.Content.ToString() == em ? new SolidColorBrush(Color.FromRgb(227, 242, 253)) : Brushes.White;
                        b.BorderBrush = b.Content.ToString() == em ? new SolidColorBrush(Color.FromRgb(25, 118, 210)) : new SolidColorBrush(Color.FromRgb(230, 230, 230));
                        b.BorderThickness = new Thickness(b.Content.ToString() == em ? 2 : 1);
                    }
                };
                emojiPanel.Children.Add(emBtn);
            }
            sp.Children.Add(emojiPanel);

            // Slogan
            sp.Children.Add(MakeLabel("🏷️ Khẩu hiệu nhóm (tùy chọn):"));
            var tbSlogan = new TextBox { Text = grp.Slogan, FontSize = 12, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 8), FontStyle = FontStyles.Italic };
            sp.Children.Add(tbSlogan);

            // Leader selector
            sp.Children.Add(MakeLabel("⭐ Nhóm trưởng:"));
            var cmbLeader = new ComboBox { FontSize = 12, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 0, 0, 8) };
            cmbLeader.Items.Add(new ComboBoxItem { Content = "(Không chọn)", Tag = "" });
            foreach (var m in grp.Members)
            {
                var ci = new ComboBoxItem { Content = m, Tag = m };
                cmbLeader.Items.Add(ci);
                if (m == grp.Leader) cmbLeader.SelectedItem = ci;
            }
            if (cmbLeader.SelectedIndex < 0) cmbLeader.SelectedIndex = 0;
            sp.Children.Add(cmbLeader);

            // Task
            sp.Children.Add(MakeLabel("📋 Nhiệm vụ nhóm:"));
            var tbTask = new TextBox
            {
                Text = grp.Task, FontSize = 12, Padding = new Thickness(8, 6, 8, 6),
                AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 50,
                Margin = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(tbTask);

            // Save button
            var btnSave = new Button
            {
                Content = "✅ Lưu cài đặt", FontSize = 13, Padding = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            btnSave.MouseEnter += (s, e) => btnSave.Background = new SolidColorBrush(Color.FromRgb(0, 77, 64));
            btnSave.MouseLeave += (s, e) => btnSave.Background = new SolidColorBrush(Color.FromRgb(0, 105, 92));
            btnSave.Click += (s, e) =>
            {
                grp.Name = tbName.Text.Trim();
                grp.Emoji = selectedEmoji;
                grp.Slogan = tbSlogan.Text.Trim();
                grp.Leader = (cmbLeader.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
                grp.Task = tbTask.Text.Trim();
                wnd.Close();
                RenderAll();
            };
            sp.Children.Add(btnSave);

            wnd.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            wnd.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════
        //  ASSIGN TASK TO ALL GROUPS
        // ═══════════════════════════════════════════════════════════

        private void AssignGroupTask_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new Window
            {
                Title = "📋 Giao nhiệm vụ cho nhóm",
                Width = 520, Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this), ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "📋 Giao nhiệm vụ cho từng nhóm", FontSize = 15, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) });

            var taskBoxes = new List<(GroupVm grp, TextBox tb)>();
            foreach (var grp in _groups)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = $"{grp.Emoji} {grp.Name}:",
                    FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 2),
                    Foreground = new SolidColorBrush(grp.HeaderColor)
                });
                var tb = new TextBox
                {
                    Text = grp.Task, FontSize = 12, Padding = new Thickness(8, 5, 8, 5),
                    AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 35,
                    Margin = new Thickness(0, 0, 0, 6),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1)
                };
                sp.Children.Add(tb);
                taskBoxes.Add((grp, tb));
            }

            // Common task box
            sp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)), Margin = new Thickness(0, 6, 0, 6) });
            sp.Children.Add(new TextBlock { Text = "📌 Nhiệm vụ chung (áp cho tất cả nhóm):", FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 0, 0, 4) });
            var tbCommon = new TextBox { FontSize = 12, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 12) };
            sp.Children.Add(tbCommon);

            var btnSave = new Button
            {
                Content = "✅ Giao nhiệm vụ", FontSize = 13, Padding = new Thickness(0, 10, 0, 10),
                Background = new SolidColorBrush(Color.FromRgb(0, 105, 92)), Foreground = Brushes.White,
                BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            btnSave.MouseEnter += (s, e2) => btnSave.Background = new SolidColorBrush(Color.FromRgb(0, 77, 64));
            btnSave.MouseLeave += (s, e2) => btnSave.Background = new SolidColorBrush(Color.FromRgb(0, 105, 92));
            btnSave.Click += (s, e2) =>
            {
                // Apply common if provided
                string common = tbCommon.Text.Trim();
                foreach (var (grp, tb) in taskBoxes)
                {
                    grp.Task = !string.IsNullOrWhiteSpace(common) ? common : tb.Text.Trim();
                }
                wnd.Close();
                RenderAll();
                MessageBox.Show("✅ Đã giao nhiệm vụ cho tất cả các nhóm!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            sp.Children.Add(btnSave);

            wnd.Content = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            wnd.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void GroupCount_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGroupCount?.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content?.ToString(), out int n) &&
                groupsGrid != null)
            {
                _groupCount = n;
                DoRandomAssign();
            }
        }

        private void RandomAssign_Click(object sender, RoutedEventArgs e)
        {
            DoRandomAssign();
            Log.Information("Groups randomized: {Count} groups", _groupCount);
        }

        private void ResetGroups_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Xóa tất cả nhóm, đưa HS về danh sách chưa phân nhóm?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _unassigned = new List<string>(_studentNames);
                _groups.Clear();
                for (int g = 0; g < _groupCount; g++)
                {
                    var (hc, bc, emoji) = GroupStyles[g % GroupStyles.Length];
                    _groups.Add(new GroupVm
                    {
                        Index = g,
                        Name = DefaultNames[g % DefaultNames.Length],
                        Emoji = emoji,
                        Members = new List<string>(),
                        HeaderColor = hc, BorderColor = bc
                    });
                }
                RenderAll();
            }
        }

        private void SaveGroupsSilence()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var activeRoster = app.ClassRoster.ActiveRoster;
                int rosterId = activeRoster?.Id ?? 0;
                if (rosterId == 0) return;

                // 1. Đồng bộ trạng thái RAM toàn cục để các page khác đọc
                app.CurrentGroups = new List<GroupVm>(_groups);

                // 2. Chụp snapshot dữ liệu (sao chép sâu) tránh xung đột đa luồng
                var groupsCopy = _groups.Select(g => new GroupVm
                {
                    Index = g.Index,
                    Name = g.Name,
                    Emoji = g.Emoji,
                    Slogan = g.Slogan,
                    Leader = g.Leader,
                    Task = g.Task,
                    Members = new List<string>(g.Members),
                    HeaderColor = g.HeaderColor,
                    BorderColor = g.BorderColor
                }).ToList();

                // 3. Thực hiện lưu đĩa ngầm
                Task.Run(() =>
                {
                    try
                    {
                        var dir = QASmartClass.Services.AppPaths.SettingsDir;
                        if (!System.IO.Directory.Exists(dir))
                        {
                            System.IO.Directory.CreateDirectory(dir);
                        }
                        var filePath = System.IO.Path.Combine(dir, $"groups_roster_{rosterId}.json");
                        
                        var jsonOptions = new System.Text.Json.JsonSerializerOptions 
                        { 
                            WriteIndented = true 
                        };
                        var json = System.Text.Json.JsonSerializer.Serialize(groupsCopy, jsonOptions);
                        System.IO.File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to save groups to file (silent): {Err}", ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Warning("SaveGroupsSilence error: {Err}", ex.Message);
            }
        }

        private void SaveGroups_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveGroupsSilence();

                // Hiển thị thông báo trên UI thread
                var summary = string.Join("\n", _groups.Select(g => $"  {g.Emoji} {g.Name}: {g.Members.Count} HS" + (string.IsNullOrEmpty(g.Leader) ? "" : $" (Trưởng: {g.Leader})")));
                MessageBox.Show($"✅ Đã lưu {_groups.Count} nhóm học tập thành công và ghi nhận vào hệ thống!\n\n{summary}\n\nHọc sinh sẽ thấy nhóm của mình trên thiết bị.",
                    "Lưu nhóm", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                var activeRoster = ((QASmartTouch.App)Application.Current).ClassRoster.ActiveRoster;
                Log.Information("Groups saved manually for roster: {RosterId}", activeRoster?.Id ?? 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu nhóm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static TextBlock MakeLabel(string text) => new()
        {
            Text = text, FontSize = 12, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)), // Slate Gray #37474F
            Margin = new Thickness(0, 4, 0, 2)
        };
    }

    public class GroupVm
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Emoji { get; set; } = "🦁";
        public string Slogan { get; set; } = string.Empty;
        public string Leader { get; set; } = string.Empty;
        public string Task { get; set; } = string.Empty;
        public List<string> Members { get; set; } = new();
        [System.Text.Json.Serialization.JsonIgnore]
        public Color HeaderColor { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Color BorderColor { get; set; }
    }
}
