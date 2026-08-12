using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.LearningTools.Views.Workplace
{
    public partial class KanbanTool : UserControl
    {
        private Border? _draggedCard = null;
        private bool _isVN = true;
        private bool _isLoadingState = false;

        // Pomodoro state
        private System.Windows.Threading.DispatcherTimer? _pomoTimer;
        private int _pomoSecondsRemaining = 25 * 60;
        private bool _isPomoRunning = false;

        public KanbanTool()
        {
            InitializeComponent();
            _isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";

            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("kanban");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;
            
            Loaded += (_, _) =>
            {
                TouchTextPad.Attach(txtQuickAdd, mode: "text");
                LocalizeUI();
                LoadPracticalApps();
                LoadSavedState();
            };

            Unloaded += (_, _) =>
            {
                SaveCurrentState();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ADD CARDS
        // ═══════════════════════════════════════════════════════════

        private void QuickAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) QuickAdd_Click(sender, e);
        }

        private void QuickAdd_Click(object sender, RoutedEventArgs e)
        {
            string title = txtQuickAdd.Text.Trim();
            if (string.IsNullOrEmpty(title)) return;

            AddCard(colTodo, title, "Chi tiết công việc...", "#E3F2FD", "#1976D2");
            txtQuickAdd.Text = "";
        }

        private void AddCard(StackPanel column, string title, string desc, string bgHex, string borderHex)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var borderColor = (Color)ColorConverter.ConvertFromString(borderHex);

            var card = new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(4, 1, 1, 1),
                BorderBrush = new SolidColorBrush(borderColor),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };

            // Shadow effect via border trick
            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black, BlurRadius = 4, ShadowDepth = 1, Opacity = 0.1
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Title & Del
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1: Description
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 2: Navigation

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 0: Drag Handle
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 1: Content
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 2: Delete Button

            // Drag Handle
            var dragHandle = new TextBlock
            {
                Text = "⋮⋮",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.LightGray,
                Cursor = Cursors.SizeAll,
                Margin = new Thickness(4, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            dragHandle.MouseEnter += (_, _) => dragHandle.Foreground = Brushes.DarkGray;
            dragHandle.MouseLeave += (_, _) => dragHandle.Foreground = Brushes.LightGray;
            dragHandle.MouseLeftButtonDown += (sender, e) =>
            {
                _draggedCard = card;
                DragDrop.DoDragDrop(card, card, DragDropEffects.Move);
            };
            Grid.SetRow(dragHandle, 0);
            Grid.SetRowSpan(dragHandle, 2);
            Grid.SetColumn(dragHandle, 0);
            grid.Children.Add(dragHandle);

            // Title
            var txtTitle = new TextBox
            {
                Text = title, FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(DS.TextPrimary),
                BorderThickness = new Thickness(0), Background = Brushes.Transparent,
                TextWrapping = TextWrapping.Wrap,
                MaxLength = 100,
                AcceptsReturn = false
            };
            txtTitle.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    Keyboard.ClearFocus();
                }
            };
            txtTitle.LostFocus += (s, e) =>
            {
                if (!_isLoadingState) SaveCurrentState();
            };
            TouchTextPad.Attach(txtTitle, mode: "text");
            Grid.SetRow(txtTitle, 0); Grid.SetColumn(txtTitle, 1);
            grid.Children.Add(txtTitle);

            // Delete Button
            var btnDel = new TextBlock
            {
                Text = "✕", FontSize = 12, Foreground = Brushes.LightGray,
                Cursor = Cursors.Hand, Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            btnDel.MouseEnter += (_, _) => btnDel.Foreground = Brushes.Red;
            btnDel.MouseLeave += (_, _) => btnDel.Foreground = Brushes.LightGray;
            btnDel.MouseLeftButtonDown += (_, _) =>
            {
                if (card.Parent is StackPanel parent)
                {
                    parent.Children.Remove(card);
                    UpdateCounts();
                    if (!_isLoadingState) SaveCurrentState();
                }
            };
            Grid.SetRow(btnDel, 0); Grid.SetColumn(btnDel, 2);
            grid.Children.Add(btnDel);

            // Description
            var txtDesc = new TextBox
            {
                Text = desc, FontSize = 12, FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                BorderThickness = new Thickness(0), Background = Brushes.Transparent,
                TextWrapping = TextWrapping.Wrap, AcceptsReturn = true,
                Margin = new Thickness(0, 6, 0, 0),
                MaxLength = 500
            };
            txtDesc.LostFocus += (s, e) =>
            {
                if (!_isLoadingState) SaveCurrentState();
            };
            TouchTextPad.Attach(txtDesc, mode: "text");
            Grid.SetRow(txtDesc, 1); Grid.SetColumn(txtDesc, 1); Grid.SetColumnSpan(txtDesc, 2);
            grid.Children.Add(txtDesc);

            // Navigation panel (populated by UpdateCardState)
            var navPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(navPanel, 2);
            Grid.SetColumn(navPanel, 1);
            Grid.SetColumnSpan(navPanel, 2);
            grid.Children.Add(navPanel);

            card.Child = grid;

            column.Children.Add(card);
            UpdateCardState(card);
            UpdateCounts();

            if (!_isLoadingState) SaveCurrentState();
        }

        // ═══════════════════════════════════════════════════════════
        //  DRAG & DROP LOGIC
        // ═══════════════════════════════════════════════════════════

        private void Column_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Border)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Column_Drop(object sender, DragEventArgs e)
        {
            if (_draggedCard != null && sender is StackPanel targetPanel)
            {
                var sourcePanel = _draggedCard.Parent as StackPanel;
                if (sourcePanel != null && sourcePanel != targetPanel)
                {
                    sourcePanel.Children.Remove(_draggedCard);
                    targetPanel.Children.Add(_draggedCard);
                    UpdateCardState(_draggedCard);
                    UpdateCounts();
                    AnimateCard(_draggedCard);

                    if (targetPanel == colDone)
                    {
                        PlayDoneSound();
                    }

                    SaveCurrentState();
                }
                _draggedCard = null;
            }
        }

        private void UpdateCounts()
        {
            countTodo.Text = colTodo.Children.Count.ToString();
            countDoing.Text = colDoing.Children.Count.ToString();
            countDone.Text = colDone.Children.Count.ToString();
        }

        private void UpdateCardState(Border card)
        {
            if (!(card.Parent is StackPanel column)) return;

            // 1. Determine colors based on the column
            string bgHex, borderHex;
            if (column == colTodo)
            {
                bgHex = "#E3F2FD"; // Light blue
                borderHex = "#1976D2"; // Blue
            }
            else if (column == colDoing)
            {
                bgHex = "#FFF3E0"; // Light orange
                borderHex = "#F57F17"; // Orange
            }
            else // colDone
            {
                bgHex = "#E8F5E9"; // Light green
                borderHex = "#388E3C"; // Green
            }

            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var borderColor = (Color)ColorConverter.ConvertFromString(borderHex);
            card.Background = new SolidColorBrush(bg);
            card.BorderBrush = new SolidColorBrush(borderColor);

            // 2. Find and update the navigation panel children
            if (card.Child is Grid grid)
            {
                StackPanel? navPanel = null;
                foreach (UIElement child in grid.Children)
                {
                    if (Grid.GetRow(child) == 2 && child is StackPanel panel)
                    {
                        navPanel = panel;
                        break;
                    }
                }

                if (navPanel != null)
                {
                    navPanel.Children.Clear();

                    // Left button (move to previous column)
                    if (column == colDoing || column == colDone)
                    {
                        var btnLeft = new Button
                        {
                            Content = "◀",
                            Padding = new Thickness(8, 2, 8, 2),
                            Margin = new Thickness(0, 0, 6, 0),
                            FontSize = 11,
                            FontWeight = FontWeights.Bold,
                            Background = Brushes.White,
                            BorderBrush = new SolidColorBrush(borderColor),
                            Foreground = new SolidColorBrush(borderColor),
                            Cursor = Cursors.Hand,
                            Height = 24
                        };
                        btnLeft.Click += (s, e) =>
                        {
                            MoveCard(card, false);
                            e.Handled = true;
                        };
                        navPanel.Children.Add(btnLeft);
                    }

                    // Right button (move to next column)
                    if (column == colTodo || column == colDoing)
                    {
                        var btnRight = new Button
                        {
                            Content = "▶",
                            Padding = new Thickness(8, 2, 8, 2),
                            FontSize = 11,
                            FontWeight = FontWeights.Bold,
                            Background = Brushes.White,
                            BorderBrush = new SolidColorBrush(borderColor),
                            Foreground = new SolidColorBrush(borderColor),
                            Cursor = Cursors.Hand,
                            Height = 24
                        };
                        btnRight.Click += (s, e) =>
                        {
                            MoveCard(card, true);
                            e.Handled = true;
                        };
                        navPanel.Children.Add(btnRight);
                    }
                }
            }
        }

        private void MoveCard(Border card, bool moveRight)
        {
            if (!(card.Parent is StackPanel sourcePanel)) return;

            StackPanel? targetPanel = null;
            if (sourcePanel == colTodo)
            {
                if (moveRight) targetPanel = colDoing;
            }
            else if (sourcePanel == colDoing)
            {
                targetPanel = moveRight ? colDone : colTodo;
            }
            else if (sourcePanel == colDone)
            {
                if (!moveRight) targetPanel = colDoing;
            }

            if (targetPanel != null)
            {
                sourcePanel.Children.Remove(card);
                targetPanel.Children.Add(card);
                UpdateCardState(card);
                UpdateCounts();
                AnimateCard(card);

                if (targetPanel == colDone)
                {
                    PlayDoneSound();
                }

                SaveCurrentState();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TEMPLATES & ACTIONS
        // ═══════════════════════════════════════════════════════════

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("kanban");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 12, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 12, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.KanbanData)t.Data;
                item.Click += (_, _) =>
                {
                    if (sideMenu != null && sideMenu.SelectedIndex != 1)
                    {
                        sideMenu.SelectedIndex = 0;
                    }
                    bool hasCards = colTodo.Children.Count > 0 || colDoing.Children.Count > 0 || colDone.Children.Count > 0;
                    if (!hasCards || MessageBox.Show("Xóa toàn bộ các thẻ trên bảng để tải mẫu?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        colTodo.Children.Clear();
                        colDoing.Children.Clear();
                        colDone.Children.Clear();
                        foreach (var todo in data.Todo) AddCard(colTodo, todo.Title, todo.Desc, "#E3F2FD", "#1976D2");
                        foreach (var doing in data.Doing) AddCard(colDoing, doing.Title, doing.Desc, "#FFF3E0", "#F57F17");
                        foreach (var done in data.Done) AddCard(colDone, done.Title, done.Desc, "#E8F5E9", "#388E3C");
                        UpdateCounts();
                        SaveCurrentState();
                    }
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            bool hasCards = colTodo.Children.Count > 0 || colDoing.Children.Count > 0 || colDone.Children.Count > 0;
            if (!hasCards) return;

            if (MessageBox.Show("Xóa toàn bộ các thẻ trên bảng?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                colTodo.Children.Clear();
                colDoing.Children.Clear();
                colDone.Children.Clear();
                UpdateCounts();
                SaveCurrentState();
            }
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
new PracticalAppItem
                    {
                        Icon = "🔬",
                        Title = isVN ? "Kế hoạch Ôn thi Tốt nghiệp THPT" : "Software Development Sprint",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_kanban_1_{suffix}.png",
                        Description = isVN 
                            ? "Quản lý tiến độ học tập các môn thi tốt nghiệp cá nhân.&#x0a;• To Do: Giải 10 đề Toán, Học thuộc 5 bài thơ môn Văn, Xem lại ngữ pháp tiếng Anh.&#x0a;• Doing: Làm sơ đồ tư duy môn Lịch sử chương 2.&#x0a;• Done: Học xong lý thuyết Địa lý tự nhiên Việt Nam." 
                            : "Track user stories moving from Backlog to In Progress, Testing, and Done columns, preventing task blockages."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💻",
                        Title = isVN ? "Dự án Viết Phần mềm môn Tin học" : "Personal Homework Tracker",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_kanban_2_{suffix}.png",
                        Description = isVN 
                            ? "Quản lý tiến độ lập trình nhóm phát triển ứng dụng di động.&#x0a;• To Do: Viết báo cáo Word, Thiết kế cơ sở dữ liệu SQLite.&#x0a;• Doing: Lập trình giao diện trang chủ bằng XML/WPF (Phụ trách: Nam).&#x0a;• Done: Lên ý tưởng và phân chia công việc cho thành viên." 
                            : "Organize upcoming assignments in columns, limit active tasks to 2, and celebrate finished homework cards."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎭",
                        Title = isVN ? "Chuẩn bị Tiết mục Văn nghệ ngày 20/11" : "Marketing Campaign Tasks",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_kanban_3_{suffix}.png",
                        Description = isVN 
                            ? "Ban văn thể mỹ lớp theo dõi chuẩn bị biểu diễn chào mừng ngày Nhà giáo.&#x0a;• To Do: Thuê trang phục biểu diễn, Mua đạo cụ (hoa, nón).&#x0a;• Doing: Tập múa bài Việt Nam quê hương tôi (Cả đội văn nghệ).&#x0a;• Done: Chọn bài hát và tuyển chọn thành viên tham gia." 
                            : "Track progress of writing, graphic design, and approvals to launch advertisement campaigns on schedule."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔬",
                        Title = isVN ? "Đề tài Nghiên cứu Khoa học Kỹ thuật" : "Restaurant Kitchen Orders",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_kanban_4_{suffix}.png",
                        Description = isVN 
                            ? "Dự án chế tạo robot mini dọn rác tự động của nhóm STEM.&#x0a;• To Do: Viết báo cáo khoa học nộp ban giám khảo, Chỉnh sửa thông số cảm biến siêu âm.&#x0a;• Doing: Lắp ráp khung xe robot và hàn mạch driver động cơ.&#x0a;• Done: Mua linh kiện điện tử và lên sơ đồ khối mạch." 
                            : "Display food tickets in columns (Queue, Cooking, Plating, Served) to coordinate chef tasks and reduce wait times."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌸",
                        Title = isVN ? "Trang trí Lớp học chào đón Tết Nguyên Đán" : "HR Hiring Pipelines",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_kanban_5_{suffix}.png",
                        Description = isVN 
                            ? "Ban cán sự lớp tổ chức dọn dẹp và trang trí không khí Tết.&#x0a;• To Do: Treo đèn lồng màu đỏ, Vẽ bảng phấn hoa đào hoa mai.&#x0a;• Doing: Làm cây hoa mai giả bằng giấy ở góc lớp (Tổ 1 & Tổ 2).&#x0a;• Done: Gom quỹ lớp để mua phụ kiện trang trí." 
                            : "Move applicant profiles through hiring stages (Resume Screen, Phone Screen, Interview, Offer, Hired)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎂",
                        Title = isVN ? "Tổ chức Sinh nhật tập thể trong tháng" : "Inventory Restocking Controls",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_kanban_6_{suffix}.png",
                        Description = isVN 
                            ? "Ban chấp hành chi đoàn chuẩn bị tiệc sinh nhật cho các bạn sinh tháng 6.&#x0a;• To Do: Mua bánh kem và nến, Chuẩn bị trò chơi minigame.&#x0a;• Doing: Viết thiệp chúc mừng viết tay cho từng bạn (Bí thư Linh).&#x0a;• Done: Lên danh sách các bạn sinh nhật trong tháng." 
                            : "Use cards (Kanban) to trigger parts ordering automatically as warehouse stock levels fall below thresholds."
                    }
            };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for KanbanTool: {Err}", ex.Message);
            }
        }

        private void LocalizeUI()
        {
            if (!_isVN)
            {
                // Tab Headers
                if (menuTextGuide != null) menuTextGuide.Text = "Guide & Workflow";
                if (menuTextWorkspace != null) menuTextWorkspace.Text = "Kanban Board";
                if (menuTextPractical != null) menuTextPractical.Text = "Real-world Applications";
                if (txtStepTitle != null) txtStepTitle.Text = "🗺️ SIMPLE 4-STEP WORKFLOW";
                if (txtStep1Header != null) txtStep1Header.Text = "1. 📝 Add New Card";
                if (txtStep1Desc != null) txtStep1Desc.Text = "Type a task in the quick add box and press Enter to create a card in To Do.";
                if (txtStep2Header != null) txtStep2Header.Text = "2. ✏️ Fill Details";
                if (txtStep2Desc != null) txtStep2Desc.Text = "Click directly on the card to edit the title, assignee, deadline, or notes.";
                if (txtStep3Header != null) txtStep3Header.Text = "3. ⏱️ Focus (Pomodoro)";
                if (txtStep3Desc != null) txtStep3Desc.Text = "Move card to Doing, click ▶ to start a 25-minute focused study session.";
                if (txtStep4Header != null) txtStep4Header.Text = "4. 🏆 Complete (Done)";
                if (txtStep4Desc != null) txtStep4Desc.Text = "Move card to Done (click ▶ or drag & drop) to accumulate accomplishments.";
                if (txtOriginHeader != null) txtOriginHeader.Text = "💡 ORIGIN & PEDAGOGICAL MEANING";
                if (txtAppHeader != null) txtAppHeader.Text = "🌍 REAL-WORLD APPLICATION";
                if (txtGuidelineText != null) 
                {
                    txtGuidelineText.Text = "• TO DO: Contains all tasks not yet started. Break down large tasks into smaller items.\n" +
                                           "• DOING: Tasks in progress. WIP Rule: Limit to 3 items at a time to prevent distraction.\n" +
                                           "• DONE: Visual reward! Cards scale up smoothly and play a cheerful sound when dropped here.\n" +
                                           "• Auto-save: State is saved to SQLite automatically so you won't lose work when exiting.";
                }
                if (txtSuggestionsHeader != null) txtSuggestionsHeader.Text = "🏫 SUGGESTED SCHOOL SCENARIOS";
                if (txtSuggestionsDesc != null)
                {
                    txtSuggestionsDesc.Text = "👉 Math Class: Solve 3 mock exams (To Do) ➔ Work on Exam 3 (Doing) ➔ Solved Exam 1 & 2 (Done).\n\n" +
                                              "👉 STEM Project: Draw block diagram (To Do) ➔ Solder motor circuit (Doing) ➔ Purchased components (Done).\n\n" +
                                              "👉 Personal: Learn 20 English words (To Do) ➔ Mindmap History (Doing) ➔ Learned Geography theory (Done).";
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  STATE PERSISTENCE (AUTO SAVE / LOAD)
        // ═══════════════════════════════════════════════════════════

        private void SaveCurrentState()
        {
            try
            {
                var state = new KanbanStateData();

                foreach (var child in colTodo.Children)
                {
                    if (child is Border card && GetCardData(card, out var title, out var desc))
                    {
                        state.Todo.Add(new KanbanCardStateData { Title = title, Desc = desc });
                    }
                }
                foreach (var child in colDoing.Children)
                {
                    if (child is Border card && GetCardData(card, out var title, out var desc))
                    {
                        state.Doing.Add(new KanbanCardStateData { Title = title, Desc = desc });
                    }
                }
                foreach (var child in colDone.Children)
                {
                    if (child is Border card && GetCardData(card, out var title, out var desc))
                    {
                        state.Done.Add(new KanbanCardStateData { Title = title, Desc = desc });
                    }
                }

                string json = System.Text.Json.JsonSerializer.Serialize(state);
                System.Threading.Tasks.Task.Run(() => DbManager.SaveWorkplaceState("kanban", json));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error saving kanban state: " + ex.Message);
            }
        }

        private void LoadSavedState()
        {
            _isLoadingState = true;
            try
            {
                string json = DbManager.LoadWorkplaceState("kanban");
                if (string.IsNullOrWhiteSpace(json)) return;

                var state = System.Text.Json.JsonSerializer.Deserialize<KanbanStateData>(json);
                if (state == null) return;

                colTodo.Children.Clear();
                colDoing.Children.Clear();
                colDone.Children.Clear();

                if (state.Todo != null)
                {
                    foreach (var card in state.Todo)
                    {
                        AddCard(colTodo, card.Title, card.Desc, "#E3F2FD", "#1976D2");
                    }
                }
                if (state.Doing != null)
                {
                    foreach (var card in state.Doing)
                    {
                        AddCard(colDoing, card.Title, card.Desc, "#FFF3E0", "#F57F17");
                    }
                }
                if (state.Done != null)
                {
                    foreach (var card in state.Done)
                    {
                        AddCard(colDone, card.Title, card.Desc, "#E8F5E9", "#388E3C");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading kanban state: " + ex.Message);
            }
            finally
            {
                _isLoadingState = false;
                UpdateCounts();
            }
        }

        private bool GetCardData(Border card, out string title, out string desc)
        {
            title = "";
            desc = "";
            if (card.Child is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is TextBox textBox)
                    {
                        int row = Grid.GetRow(textBox);
                        int col = Grid.GetColumn(textBox);
                        if (row == 0 && col == 1)
                        {
                            title = textBox.Text;
                        }
                        else if (row == 1 && col == 1)
                        {
                            desc = textBox.Text;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        // ═══════════════════════════════════════════════════════════
        //  GAMIFICATION EFFECTS (SOUND & POP ANIMATION)
        // ═══════════════════════════════════════════════════════════

        private void AnimateCard(Border card)
        {
            try
            {
                var scale = new ScaleTransform(1.0, 1.0);
                card.RenderTransform = scale;
                card.RenderTransformOrigin = new Point(0.5, 0.5);

                var anim = new System.Windows.Media.Animation.DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(200));
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Animation error: " + ex.Message);
            }
        }

        private void PlayDoneSound()
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Sound play error: " + ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  POMODORO TIMER LOGIC
        // ═══════════════════════════════════════════════════════════

        private void PomoStart_Click(object sender, RoutedEventArgs e)
        {
            if (_pomoTimer == null)
            {
                _pomoTimer = new System.Windows.Threading.DispatcherTimer();
                _pomoTimer.Interval = TimeSpan.FromSeconds(1);
                _pomoTimer.Tick += PomoTimer_Tick;
            }

            if (_isPomoRunning)
            {
                _pomoTimer.Stop();
                _isPomoRunning = false;
                btnPomoStart.Content = "▶";
            }
            else
            {
                _pomoTimer.Start();
                _isPomoRunning = true;
                btnPomoStart.Content = "⏸";
            }
        }

        private void PomoReset_Click(object sender, RoutedEventArgs e)
        {
            if (_pomoTimer != null)
            {
                _pomoTimer.Stop();
            }
            _isPomoRunning = false;
            _pomoSecondsRemaining = 25 * 60;
            btnPomoStart.Content = "▶";
            txtPomoTime.Text = "25:00";
        }

        private void PomoTimer_Tick(object? sender, EventArgs e)
        {
            if (_pomoSecondsRemaining > 0)
            {
                _pomoSecondsRemaining--;
                int minutes = _pomoSecondsRemaining / 60;
                int seconds = _pomoSecondsRemaining % 60;
                txtPomoTime.Text = $"{minutes:D2}:{seconds:D2}";
            }
            else
            {
                if (_pomoTimer != null)
                {
                    _pomoTimer.Stop();
                }
                _isPomoRunning = false;
                btnPomoStart.Content = "▶";
                _pomoSecondsRemaining = 25 * 60;
                txtPomoTime.Text = "25:00";

                PlayDoneSound();
                MessageBox.Show("🔔 Đã hoàn thành 25 phút tập trung Pomodoro! Hãy nghỉ ngơi 5 phút nhé.", "Pomodoro Timer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewWorkspace == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewWorkspace.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewWorkspace.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }

    // State classes for JSON persistence
    public class KanbanCardStateData
    {
        public string Title { get; set; } = "";
        public string Desc { get; set; } = "";
    }

    public class KanbanStateData
    {
        public List<KanbanCardStateData> Todo { get; set; } = new();
        public List<KanbanCardStateData> Doing { get; set; } = new();
        public List<KanbanCardStateData> Done { get; set; } = new();
    }
}