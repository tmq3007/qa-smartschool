using QASmartClass.LearningTools.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.LearningTools.Views.Workplace
{
    public partial class FiveSTool : UserControl
    {
        private class Question
        {
            public string Text { get; set; } = "";
            public int Score { get; set; } = 0; // 0 means unrated, 1-5 rating
        }

        private Dictionary<int, List<Question>> _checklists = new();
        private int _currentTab = 1;
        private Button[] _tabs;
        private readonly List<ConfettiParticle> _particles = new();
        private System.Windows.Threading.DispatcherTimer? _confettiTimer;
        private readonly Random _rng = new();
        private bool _hasTriggeredConfetti = false;

        public FiveSTool()
        {
            InitializeComponent();
            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("five_s");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;
            _tabs = new[] { tab1, tab2, tab3, tab4, tab5 };

            // Nạp dữ liệu mặc định sớm nhất để tránh race condition khi Canvas vẽ kích thước trước sự kiện Loaded
            LoadDefaultChecklists();
            LoadPracticalApps();

            if (panelGuideText != null)
            {
                _currentFontSize = (double)panelGuideText.GetValue(TextElement.FontSizeProperty);
            }

            Loaded += (_, _) =>
            {
                TouchTextPad.Attach(txtSubject, mode: "text");
                LoadSavedState();
                SelectTab(1);
                UpdateChart();
            };
            Unloaded += (s, e) => SaveCurrentState();
        }

        private void LoadDefaultChecklists()
        {
            _checklists[1] = new List<Question> // Seiri
            {
                new Question { Text = "Chỉ giữ lại những dụng cụ học tập thực sự cần thiết trên bàn." },
                new Question { Text = "Đã vứt bỏ giấy nháp cũ, bút hết mực, vỏ chai nước." },
                new Question { Text = "Sách vở năm ngoái hoặc ít dùng được cất gọn vào trong tủ." }
            };
            _checklists[2] = new List<Question> // Seiton
            {
                new Question { Text = "Sách vở, tài liệu được xếp theo môn học hoặc thời khóa biểu." },
                new Question { Text = "Máy tính, thước, bút có vị trí cố định dễ lấy ra, cất vào." },
                new Question { Text = "Dây sạc điện thoại, laptop được cuộn gọn gàng không bị rối." }
            };
            _checklists[3] = new List<Question> // Seiso
            {
                new Question { Text = "Bàn học, màn hình máy tính luôn sạch sẽ, không dính bụi bẩn." },
                new Question { Text = "Sàn nhà quanh khu vực học tập được quét dọn thường xuyên." },
                new Question { Text = "Không để vương vãi đồ ăn vặt, rác ở góc học tập." }
            };
            _checklists[4] = new List<Question> // Seiketsu
            {
                new Question { Text = "Duy trì dọn dẹp (3S) mỗi ngày thay vì đợi dồn lại cuối tuần." },
                new Question { Text = "Có dán nội quy hoặc checklist tự nhắc nhở bản thân ở góc học tập." },
                new Question { Text = "Có thùng rác nhỏ ngay dưới chân bàn để vứt rác tiện lợi." }
            };
            _checklists[5] = new List<Question> // Shitsuke
            {
                new Question { Text = "Tự giác dọn bàn ngay sau khi học xong mà không cần ai nhắc nhở." },
                new Question { Text = "Sắp xếp sẵn sách vở vào cặp cho buổi sáng ngày mai từ tối hôm trước." },
                new Question { Text = "Luôn giữ ý thức: Không gian sạch sẽ giúp học tập tập trung hơn." }
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  UI & TABS
        // ═══════════════════════════════════════════════════════════

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag.ToString(), out int tabIndex))
            {
                SelectTab(tabIndex);
            }
        }

        private void SelectTab(int index)
        {
            _currentTab = index;

            // Style tabs
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].Style = (i + 1 == index)
                    ? (Style)FindResource("ActiveTabStyle")
                    : (Style)FindResource("InactiveTabStyle");
            }

            RenderChecklist();
        }

        private void RenderChecklist()
        {
            checklistPanel.Children.Clear();
            var list = _checklists[_currentTab];

            for (int i = 0; i < list.Count; i++)
            {
                var q = list[i];
                var row = new Border
                {
                    Background = i % 2 == 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                    Padding = new Thickness(12, 16, 12, 16),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Question text
                var txt = new TextBlock
                {
                    Text = $"{i + 1}. {q.Text}",
                    FontSize = 14, Foreground = new SolidColorBrush(DS.TextPrimary),
                    TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 16, 0)
                };
                Grid.SetColumn(txt, 0);
                grid.Children.Add(txt);

                // Rating stars/buttons
                var starPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                for (int s = 1; s <= 5; s++)
                {
                    var btn = new Button
                    {
                        Content = s.ToString(),
                        Style = (Style)FindResource("RatingButtonStyle"),
                        Tag = q.Score == s ? "Selected" : "Normal"
                    };
                    
                    int capturedScore = s;
                    var capturedQ = q;
                    btn.Click += (_, _) =>
                    {
                        capturedQ.Score = capturedScore;
                        RenderChecklist(); // Re-render to update colors
                        UpdateChart();
                        SaveCurrentState();
                    };
                    starPanel.Children.Add(btn);
                }
                
                Grid.SetColumn(starPanel, 1);
                grid.Children.Add(starPanel);

                row.Child = grid;
                checklistPanel.Children.Add(row);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  RADAR CHART & SCORING
        // ═══════════════════════════════════════════════════════════

        private void RadarCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateChart();
        }

        private void UpdateChart()
        {
            if (radarCanvas.ActualWidth == 0 || radarCanvas.ActualHeight == 0) return;

            // Tính điểm trung bình từng phần
            double[] scores = new double[5];
            double totalScore = 0;
            bool isComplete = true;

            for (int i = 1; i <= 5; i++)
            {
                if (!_checklists.ContainsKey(i)) return; // Guard clause
                var list = _checklists[i];
                if (list.Any(q => q.Score == 0)) isComplete = false;

                double avg = list.Count > 0 ? list.Average(q => q.Score > 0 ? q.Score : 0) : 0;
                scores[i - 1] = avg; // Mức từ 0-5
                totalScore += avg;
            }

            txtTotalScore.Text = $"{totalScore:F1} / 25";
            if (totalScore >= 22 && isComplete) { txtEvaluation.Text = "🌟 XUẤT SẮC"; txtEvaluation.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); }
            else if (totalScore >= 18 && isComplete) { txtEvaluation.Text = "👍 TỐT"; txtEvaluation.Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)); }
            else if (totalScore >= 12 && isComplete) { txtEvaluation.Text = "⚠️ CẦN CẢI THIỆN"; txtEvaluation.Foreground = new SolidColorBrush(Color.FromRgb(245, 127, 23)); }
            else if (isComplete) { txtEvaluation.Text = "❌ CHƯA ĐẠT"; txtEvaluation.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)); }
            else { txtEvaluation.Text = "Đang đánh giá..."; txtEvaluation.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)); }

            // Gợi ý cải tiến sư phạm động
            if (isComplete)
            {
                double minVal = scores.Min();
                if (minVal >= 5.0)
                {
                    txtAdvice.Text = "🎉 Chúc mừng! Góc học tập của bạn đã đạt tiêu chuẩn 5S xuất sắc.";
                }
                else
                {
                    int minIndex = Array.IndexOf(scores, minVal);
                    txtAdvice.Text = minIndex switch
                    {
                        0 => "💡 Gợi ý cải tiến: Hãy thanh lọc ngay đống giấy nháp cũ và bút hỏng để bàn học thoáng đãng hơn.",
                        1 => "💡 Gợi ý cải tiến: Sắp xếp lại sách vở vào khay riêng theo thời khóa biểu ngày mai.",
                        2 => "💡 Gợi ý cải tiến: Dành 3 phút quét sạch bụi dưới chân bàn học và lau sạch màn hình máy tính.",
                        3 => "💡 Gợi ý cải tiến: Hãy lập một bảng checklist dán tường nhỏ để nhắc nhở bản thân dọn dẹp hàng ngày.",
                        4 => "💡 Gợi ý cải tiến: Duy trì thói quen tự giác sắp xếp ngăn nắp mà không cần đợi người khác nhắc nhở.",
                        _ => ""
                    };
                }
            }
            else
            {
                txtAdvice.Text = "Hãy hoàn thành đánh giá để nhận gợi ý cải tiến.";
            }

            // Kích hoạt pháo hoa giấy chúc mừng khi đạt điểm tuyệt đối 25/25
            if (totalScore >= 25 && isComplete)
            {
                if (!_hasTriggeredConfetti)
                {
                    _hasTriggeredConfetti = true;
                    TriggerConfetti();
                }
            }
            else
            {
                _hasTriggeredConfetti = false;
            }

            DrawRadar(scores);
        }

        private void DrawRadar(double[] scores)
        {
            radarCanvas.Children.Clear();

            double width = radarCanvas.ActualWidth;
            double height = radarCanvas.ActualHeight;
            double cx = width / 2;
            double cy = height / 2;
            double radius = System.Math.Min(cx, cy) - 45; // padding cho label song ngữ

            if (radius <= 0) return;

            // Draw Web (5 pentagons)
            var axisStroke = new SolidColorBrush(Color.FromRgb(189, 189, 189));
            for (int r = 1; r <= 5; r++)
            {
                double ratio = r / 5.0;
                var poly = new Polygon { Stroke = axisStroke, StrokeThickness = 1 };
                for (int i = 0; i < 5; i++)
                {
                    double angle = i * 2 * System.Math.PI / 5 - System.Math.PI / 2; // -90 deg to start at top
                    poly.Points.Add(new Point(cx + radius * ratio * System.Math.Cos(angle), cy + radius * ratio * System.Math.Sin(angle)));
                }
                radarCanvas.Children.Add(poly);
            }

            // Draw Axes & Labels
            string[] labels = { 
                "1S: Sàng Lọc\n(Seiri)", 
                "2S: Sắp Xếp\n(Seiton)", 
                "3S: Sạch Sẽ\n(Seiso)", 
                "4S: Săn Sóc\n(Seiketsu)", 
                "5S: Sẵn Sàng\n(Shitsuke)" 
            };
            for (int i = 0; i < 5; i++)
            {
                double angle = i * 2 * System.Math.PI / 5 - System.Math.PI / 2;
                double endX = cx + radius * System.Math.Cos(angle);
                double endY = cy + radius * System.Math.Sin(angle);

                radarCanvas.Children.Add(new Line { X1 = cx, Y1 = cy, X2 = endX, Y2 = endY, Stroke = axisStroke, StrokeThickness = 1 });

                var lbl = new TextBlock
                {
                    Text = labels[i], FontSize = 12, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                    TextAlignment = TextAlignment.Center
                };
                lbl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double lx = cx + (radius + 15) * System.Math.Cos(angle) - lbl.DesiredSize.Width / 2;
                double ly = cy + (radius + 15) * System.Math.Sin(angle) - lbl.DesiredSize.Height / 2;
                Canvas.SetLeft(lbl, lx);
                Canvas.SetTop(lbl, ly);
                radarCanvas.Children.Add(lbl);
            }

            // Draw Data Polygon
            var dataColor = new SolidColorBrush(Color.FromArgb(100, 0, 121, 107)); // Teal transparent
            var dataStroke = new SolidColorBrush(Color.FromRgb(0, 121, 107));
            var dataPoly = new Polygon { Fill = dataColor, Stroke = dataStroke, StrokeThickness = 2 };

            for (int i = 0; i < 5; i++)
            {
                double angle = i * 2 * System.Math.PI / 5 - System.Math.PI / 2;
                double scoreRatio = scores[i] / 5.0; // scale 0-5
                double px = cx + radius * scoreRatio * System.Math.Cos(angle);
                double py = cy + radius * scoreRatio * System.Math.Sin(angle);
                dataPoly.Points.Add(new Point(px, py));
            }

            radarCanvas.Children.Add(dataPoly); // Add polygon first

            // Add point circles on top of polygon
            for (int i = 0; i < 5; i++)
            {
                double angle = i * 2 * System.Math.PI / 5 - System.Math.PI / 2;
                double scoreRatio = scores[i] / 5.0;
                double px = cx + radius * scoreRatio * System.Math.Cos(angle);
                double py = cy + radius * scoreRatio * System.Math.Sin(angle);

                if (scores[i] > 0)
                {
                    var ell = new Ellipse { Width = 6, Height = 6, Fill = dataStroke };
                    Canvas.SetLeft(ell, px - 3);
                    Canvas.SetTop(ell, py - 3);
                    radarCanvas.Children.Add(ell);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TEMPLATES & ACTIONS
        // ═══════════════════════════════════════════════════════════

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("five_s");
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
                var data = (QASmartClass.LearningTools.Helpers.FiveSData)t.Data;
                item.Click += (_, _) =>
                {
                    bool hasData = _checklists.Values.Any(list => list.Any(q => q.Score > 0)) || !string.IsNullOrWhiteSpace(txtSubject.Text);
                    if (hasData)
                    {
                        var result = MessageBox.Show(
                            "Đang có dữ liệu đánh giá. Bạn có muốn tải mẫu mới không? Dữ liệu hiện tại sẽ bị xóa.", 
                            "Xác nhận", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Warning);
                        if (result != MessageBoxResult.Yes) return;
                    }

                    txtSubject.Text = data.Subject;
                    LoadDefaultChecklists();

                    if (data.Scores != null && data.Scores.Length == 5)
                    {
                        for (int tIndex = 1; tIndex <= 5; tIndex++)
                        {
                            var tabScores = data.Scores[tIndex - 1];
                            if (_checklists.ContainsKey(tIndex) && tabScores != null)
                            {
                                var questions = _checklists[tIndex];
                                for (int qIndex = 0; qIndex < System.Math.Min(questions.Count, tabScores.Length); qIndex++)
                                {
                                    questions[qIndex].Score = tabScores[qIndex];
                                }
                            }
                        }
                    }

                    if (sideMenu != null && sideMenu.SelectedIndex != 1)
                    {
                        sideMenu.SelectedIndex = 0;
                    }
                    SelectTab(1);
                    UpdateChart();
                    SaveCurrentState();
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 1;
                // Chờ layout cập nhật xong mới thực hiện chụp hình xuất ảnh
                Dispatcher.InvokeAsync(() => RunExport(), System.Windows.Threading.DispatcherPriority.Background);
                return;
            }
            RunExport();
        }

        private void RunExport()
        {
            try
            {
                // Clean up and sanitize subject input
                if (!string.IsNullOrWhiteSpace(txtSubject.Text))
                {
                    txtSubject.Text = System.Text.RegularExpressions.Regex.Replace(txtSubject.Text, @"[^\p{L}\p{N}\s\-_.]", "").Trim();
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập tên Khu vực đánh giá trước khi xuất ảnh để thông tin báo cáo đầy đủ nhất.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"5S_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
                if (dlg.ShowDialog() == true)
                {
                    var target = chartBorder; // Chỉ export phần biểu đồ và kết quả cho đẹp
                    var bounds = new Rect(target.RenderSize);
                    double dpi = 192; // 192 DPI for high resolution print
                    var rtb = new RenderTargetBitmap(
                        (int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96),
                        dpi, dpi, PixelFormats.Pbgra32);
                    var dv = new DrawingVisual();
                    using (var dc = dv.RenderOpen())
                    {
                        // Draw opaque white background to prevent black background transparency issues
                        dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), bounds.Size));
                        dc.DrawRectangle(new VisualBrush(target), null, new Rect(new Point(), bounds.Size));
                    }
                    rtb.Render(dv);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using var stream = File.Create(dlg.FileName);
                    encoder.Save(stream);

                    // Lưu lịch sử thi đua vào cơ sở dữ liệu SQLite
                    try
                    {
                        double finalScore = 0;
                        for (int i = 1; i <= 5; i++)
                        {
                            finalScore += _checklists[i].Count > 0 ? _checklists[i].Average(q => q.Score > 0 ? q.Score : 0) : 0;
                        }
                        var stateScores = new int[5][];
                        for (int i = 1; i <= 5; i++)
                        {
                            stateScores[i - 1] = _checklists[i].Select(q => q.Score).ToArray();
                        }
                        string stateScoresJson = System.Text.Json.JsonSerializer.Serialize(stateScores);
                        DbManager.SaveFiveSHistory(txtSubject.Text.Trim(), finalScore, stateScoresJson);
                    }
                    catch { /* Swallowed */ }

                    MessageBox.Show($"Đã lưu: {dlg.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Xóa toàn bộ đánh giá?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                txtSubject.Text = "";
                LoadDefaultChecklists();
                SelectTab(1);
                UpdateChart();
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
                        Icon = "🔴",
                        Title = isVN ? "Sàng Lọc (Seiri)" : "Desktop & Workspace Organization",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_s_1_{suffix}.png",
                        Description = isVN 
                            ? "Phân loại và loại bỏ những đồ dùng học tập không cần thiết (như nháp cũ, bút hỏng). Giúp bàn học sạch thoáng, dễ tập trung." 
                            : "Sort out unused documents (Sort), arrange desk tools neatly (Set in order), and clean keyboard dirt (Shine)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔵",
                        Title = isVN ? "Sắp Xếp (Seiton)" : "Factory Floor Safety",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_s_2_{suffix}.png",
                        Description = isVN 
                            ? "Sách vở, tài liệu xếp theo môn học hoặc thời khóa biểu. Máy tính, bút thước để đúng nơi quy định, dễ lấy dễ cất." 
                            : "Clear pathways of clutter, mark safety hazard zones on floors, and organize tool boards to prevent accidents."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🟢",
                        Title = isVN ? "Sạch Sẽ (Seiso)" : "Digital File Management",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_s_3_{suffix}.png",
                        Description = isVN 
                            ? "Lau chùi góc học tập, màn hình máy tính và thiết bị sạch sẽ. Quét dọn bụi bẩn xung quanh để đảm bảo không gian vệ sinh." 
                            : "Delete duplicate downloads (Sort), organize folders by project (Set in order), and standardize file naming rules (Standardize)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🟡",
                        Title = isVN ? "Săn Sóc (Seiketsu)" : "Library Book Storage",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_s_4_{suffix}.png",
                        Description = isVN 
                            ? "Thiết lập các tiêu chuẩn tự kiểm tra (checklist dán tường) và duy trì 3S trên đều đặn mỗi ngày." 
                            : "Catalog books by genre and code, keep shelves dust-free, and run weekly audits to sustain the system (Sustain)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🟣",
                        Title = isVN ? "Sẵn Sàng (Shitsuke)" : "Reducing Operation Waste",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_s_5_{suffix}.png",
                        Description = isVN 
                            ? "Tự giác thực hiện 5S mọi lúc mọi nơi mà không cần nhắc nhở. Tạo thói quen kỷ luật tự thân, ngăn nắp bền vững." 
                            : "Eliminate wasted search time for tools by implementing shadow boards and clear labeling in workshops."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏫",
                        Title = isVN ? "Trực Nhật Lớp Học" : "Creating Discipline Habits",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_s_6_{suffix}.png",
                        Description = isVN 
                            ? "Áp dụng 5S vào trực nhật phòng học: lau bảng sạch, sắp xếp bàn ghế thẳng hàng, vệ sinh tủ lớp và thùng rác." 
                            : "Foster self-discipline and team pride by conducting regular 5S self-checklists and rewarding best zones."
                    }
            };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for FiveSTool: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  DATABASE STATE SYNC & HISTORIC LOGGING
        // ═══════════════════════════════════════════════════════════

        private class FiveSState
        {
            public string Subject { get; set; } = "";
            public int[][] Scores { get; set; } = null!;
        }

        private void Subject_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveCurrentState();
        }

        private void SaveCurrentState()
        {
            try
            {
                var scores = new int[5][];
                for (int i = 1; i <= 5; i++)
                {
                    scores[i - 1] = _checklists[i].Select(q => q.Score).ToArray();
                }

                var state = new FiveSState
                {
                    Subject = txtSubject.Text.Trim(),
                    Scores = scores
                };

                string json = System.Text.Json.JsonSerializer.Serialize(state);
                System.Threading.Tasks.Task.Run(() => DbManager.SaveWorkplaceState("five_s", json));
            }
            catch { /* Swallowed */ }
        }

        private void LoadSavedState()
        {
            try
            {
                string json = DbManager.LoadWorkplaceState("five_s");
                if (string.IsNullOrWhiteSpace(json)) return;

                var state = System.Text.Json.JsonSerializer.Deserialize<FiveSState>(json);
                if (state == null) return;

                txtSubject.Text = state.Subject;
                if (state.Scores != null && state.Scores.Length == 5)
                {
                    for (int tIndex = 1; tIndex <= 5; tIndex++)
                    {
                        var tabScores = state.Scores[tIndex - 1];
                        if (_checklists.ContainsKey(tIndex) && tabScores != null)
                        {
                            var questions = _checklists[tIndex];
                            for (int qIndex = 0; qIndex < System.Math.Min(questions.Count, tabScores.Length); qIndex++)
                            {
                                questions[qIndex].Score = tabScores[qIndex];
                            }
                        }
                    }
                }
            }
            catch { /* Swallowed */ }
        }

        // ═══════════════════════════════════════════════════════════
        //  ZOOM FONT SIZE HANDLERS FOR CLASSROOM PROJECTORS
        // ═══════════════════════════════════════════════════════════

        private double _currentFontSize = 14;

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFontSize < 20)
            {
                _currentFontSize += 1.5;
                panelGuideText?.SetValue(TextElement.FontSizeProperty, _currentFontSize);
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFontSize > 12)
            {
                _currentFontSize -= 1.5;
                panelGuideText?.SetValue(TextElement.FontSizeProperty, _currentFontSize);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CONFETTI CELEBRATION EFFECT (60 FPS DispatcherTimer)
        // ═══════════════════════════════════════════════════════════

        private void TriggerConfetti()
        {
            if (_confettiTimer == null)
            {
                _confettiTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                _confettiTimer.Tick += ConfettiTimer_Tick;
            }

            double width = confettiCanvas.ActualWidth;
            if (width <= 0) width = 900;

            var colors = new Brush[]
            {
                Brushes.Red, Brushes.Green, Brushes.Orange, Brushes.Teal,
                Brushes.Blue, Brushes.Purple, Brushes.DeepPink, Brushes.Gold
            };

            for (int i = 0; i < 70; i++)
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = _rng.Next(8, 14),
                    Height = _rng.Next(8, 14),
                    Fill = colors[_rng.Next(colors.Length)],
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                var transform = new TransformGroup();
                transform.Children.Add(new RotateTransform());
                transform.Children.Add(new TranslateTransform());
                rect.RenderTransform = transform;

                confettiCanvas.Children.Add(rect);

                _particles.Add(new ConfettiParticle
                {
                    Element = rect,
                    X = _rng.NextDouble() * width,
                    Y = _rng.NextDouble() * -80 - 20,
                    SpeedX = _rng.NextDouble() * 5 - 2.5,
                    SpeedY = _rng.NextDouble() * 6 + 4,
                    RotationSpeed = _rng.NextDouble() * 12 - 6,
                    Angle = _rng.NextDouble() * 360
                });
            }

            _confettiTimer.Start();
        }

        private void ConfettiTimer_Tick(object? sender, EventArgs e)
        {
            double height = confettiCanvas.ActualHeight;
            if (height <= 0) height = 600;

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X += p.SpeedX;
                p.Y += p.SpeedY;
                p.Angle += p.RotationSpeed;

                if (p.Y > height)
                {
                    confettiCanvas.Children.Remove(p.Element);
                    _particles.RemoveAt(i);
                }
                else
                {
                    var tg = (TransformGroup)p.Element.RenderTransform;
                    var rt = (RotateTransform)tg.Children[0];
                    var tt = (TranslateTransform)tg.Children[1];

                    rt.Angle = p.Angle;
                    tt.X = p.X;
                    tt.Y = p.Y;
                }
            }

            if (_particles.Count == 0)
            {
                _confettiTimer?.Stop();
            }
        }

        private class ConfettiParticle
        {
            public FrameworkElement Element { get; set; } = null!;
            public double X { get; set; }
            public double Y { get; set; }
            public double SpeedX { get; set; }
            public double SpeedY { get; set; }
            public double Angle { get; set; }
            public double RotationSpeed { get; set; }
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
}