using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Workplace
{
    /// <summary>
    /// 🎯 SWOT Analysis: Phân tích Điểm mạnh – Điểm yếu – Cơ hội – Thách thức
    /// </summary>
    public partial class SwotTool : UserControl
    {
        public SwotTool()
        {
            InitializeComponent();
            UpdatePlaceholders();
            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("swot");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;
            Loaded += (_, _) =>
            {
                TouchTextPad.Attach(txtSubject, mode: "text");
                LoadPracticalApps();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  ADD ITEMS — Thêm mục vào từng ô SWOT
        // ═══════════════════════════════════════════════════════════

        private void AddS_Click(object sender, RoutedEventArgs e) => AddSwotItem(panelS, "#2E7D32", "#E8F5E9");
        private void AddW_Click(object sender, RoutedEventArgs e) => AddSwotItem(panelW, "#F57F17", "#FFF8E1");
        private void AddO_Click(object sender, RoutedEventArgs e) => AddSwotItem(panelO, "#1565C0", "#E3F2FD");
        private void AddT_Click(object sender, RoutedEventArgs e) => AddSwotItem(panelT, "#C62828", "#FFEBEE");

        private void AddSwotItem(StackPanel panel, string fgHex, string bgHex)
        {
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);

            var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Bullet
            var bullet = new TextBlock
            {
                Text = "•",
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(bullet, 0);
            row.Children.Add(bullet);

            // TextBox
            var txt = new TextBox
            {
                FontSize = 14, FontFamily = DS.FontPrimary,
                Padding = new Thickness(8, 5, 8, 5),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, fg.R, fg.G, fg.B)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Foreground = new SolidColorBrush(DS.TextPrimary),
                TextWrapping = TextWrapping.Wrap
            };
            TouchTextPad.Attach(txt, mode: "text");
            Grid.SetColumn(txt, 1);
            row.Children.Add(txt);

            // Tự động cập nhật Placeholders khi học sinh bắt đầu gõ
            txt.TextChanged += (s, ev) =>
            {
                UpdatePlaceholders();
            };

            // Phím tắt Enter để tự động tạo thêm mục mới (Chỉ tạo nếu ô hiện tại không trống)
            txt.KeyDown += (s, ev) =>
            {
                if (ev.Key == Key.Enter)
                {
                    ev.Handled = true;
                    if (!string.IsNullOrWhiteSpace(txt.Text))
                    {
                        if (panel == panelS) AddS_Click(null!, null!);
                        else if (panel == panelW) AddW_Click(null!, null!);
                        else if (panel == panelO) AddO_Click(null!, null!);
                        else if (panel == panelT) AddT_Click(null!, null!);
                    }
                }
            };

            // Delete button
            var del = new TextBlock
            {
                Text = "✕",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0),
                ToolTip = "Xóa mục này"
            };
            del.MouseEnter += (_, _) => del.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
            del.MouseLeave += (_, _) => del.Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189));
            var capturedRow = row;
            del.MouseLeftButtonDown += (_, _) => {
                panel.Children.Remove(capturedRow);
                UpdatePlaceholders();
            };
            Grid.SetColumn(del, 2);
            row.Children.Add(del);

            panel.Children.Add(row);
            UpdatePlaceholders();

            // Focus vào ô vừa tạo
            txt.Loaded += (_, _) => txt.Focus();
        }

        // ═══════════════════════════════════════════════════════════
        //  STRATEGY — Gợi ý chiến lược SO/WO/ST/WT
        // ═══════════════════════════════════════════════════════════

        private string FormatItemsForStrategy(List<string> items)
        {
            if (items.Count <= 3)
                return string.Join(", ", items);
            return string.Join(", ", items.Take(3)) + " và các yếu tố khác";
        }

        private void Strategy_Click(object sender, RoutedEventArgs e)
        {
            var sItems = GetItemTexts(panelS);
            var wItems = GetItemTexts(panelW);
            var oItems = GetItemTexts(panelO);
            var tItems = GetItemTexts(panelT);

            // Yêu cầu nhập ít nhất 1 mục vào bất kỳ ô nào trước khi hiển thị khung gợi ý chiến lược
            if (sItems.Count == 0 && wItems.Count == 0 && oItems.Count == 0 && tItems.Count == 0)
            {
                MessageBox.Show("Hãy nhập ít nhất 1 mục vào bất kỳ ô SWOT nào trước khi xem gợi ý chiến lược.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            strategySection.Visibility = Visibility.Visible;
            strategyContent.Children.Clear();

            // SO Strategy
            if (sItems.Count > 0 && oItems.Count > 0)
            {
                AddStrategyBlock("🚀 SO — Chiến lược Tận dụng",
                    $"Sử dụng điểm mạnh ({FormatItemsForStrategy(sItems)}) để nắm bắt cơ hội ({FormatItemsForStrategy(oItems)})",
                    "#2E7D32", "#E8F5E9");
            }
            else
            {
                AddStrategyBlock("🚀 SO — Chiến lược Tận dụng (Chưa đủ thông tin)",
                    "Kết hợp Điểm mạnh (S) để tận dụng Cơ hội (O). Hãy nhập thêm cả Điểm mạnh và Cơ hội để nhận gợi ý cụ thể cho bạn.",
                    "#757575", "#F5F5F5");
            }

            // WO Strategy
            if (wItems.Count > 0 && oItems.Count > 0)
            {
                AddStrategyBlock("🔧 WO — Chiến lược Khắc phục",
                    $"Khắc phục điểm yếu ({FormatItemsForStrategy(wItems)}) bằng cách tận dụng cơ hội ({FormatItemsForStrategy(oItems)})",
                    "#1565C0", "#E3F2FD");
            }
            else
            {
                AddStrategyBlock("🔧 WO — Chiến lược Khắc phục (Chưa đủ thông tin)",
                    "Khắc phục Điểm yếu (W) bằng cách tận dụng Cơ hội (O). Hãy nhập thêm cả Điểm yếu và Cơ hội để nhận gợi ý cụ thể cho bạn.",
                    "#757575", "#F5F5F5");
            }

            // ST Strategy
            if (sItems.Count > 0 && tItems.Count > 0)
            {
                AddStrategyBlock("🛡️ ST — Chiến lược Đối phó",
                    $"Dùng điểm mạnh ({FormatItemsForStrategy(sItems)}) để đối phó thách thức ({FormatItemsForStrategy(tItems)})",
                    "#E65100", "#FFF3E0");
            }
            else
            {
                AddStrategyBlock("🛡️ ST — Chiến lược Đối phó (Chưa đủ thông tin)",
                    "Sử dụng Điểm mạnh (S) để đối phó và hạn chế Thách thức (T). Hãy nhập thêm cả Điểm mạnh và Thách thức để nhận gợi ý cụ thể cho bạn.",
                    "#757575", "#F5F5F5");
            }

            // WT Strategy
            if (wItems.Count > 0 && tItems.Count > 0)
            {
                AddStrategyBlock("⚠️ WT — Chiến lược Phòng thủ",
                    $"Giảm thiểu điểm yếu ({FormatItemsForStrategy(wItems)}) để tránh thách thức ({FormatItemsForStrategy(tItems)})",
                    "#C62828", "#FFEBEE");
            }
            else
            {
                AddStrategyBlock("⚠️ WT — Chiến lược Phòng thủ (Chưa đủ thông tin)",
                    "Tối thiểu hóa Điểm yếu (W) để tránh và giảm thiểu ảnh hưởng của Thách thức (T). Hãy nhập thêm cả Điểm yếu và Thách thức để nhận gợi ý cụ thể cho bạn.",
                    "#757575", "#F5F5F5");
            }
        }

        private void AddStrategyBlock(string title, string description, string fgHex, string bgHex)
        {
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);

            var block = new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, fg.R, fg.G, fg.B)),
                BorderThickness = new Thickness(1)
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg),
                Margin = new Thickness(0, 0, 0, 4)
            });
            sp.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 16, FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(DS.TextPrimary),
                TextWrapping = TextWrapping.Wrap
            });

            block.Child = sp;
            strategyContent.Children.Add(block);
        }

        private List<string> GetItemTexts(StackPanel panel)
        {
            var items = new List<string>();
            foreach (var child in panel.Children)
            {
                if (child is Grid grid)
                {
                    foreach (var gc in grid.Children)
                    {
                        if (gc is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
                        {
                            items.Add(tb.Text.Trim());
                        }
                    }
                }
            }
            return items;
        }

        // ═══════════════════════════════════════════════════════════
        //  TEMPLATES
        // ═══════════════════════════════════════════════════════════

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("swot");
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
                var data = (QASmartClass.LearningTools.Helpers.SwotData)t.Data;
                item.Click += (_, _) =>
                {
                    ApplyTemplate(t.Name, data.S, data.W, data.O, data.T);
                };
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private bool IsSwotEmpty()
        {
            if (!string.IsNullOrWhiteSpace(txtSubject.Text)) return false;
            if (panelS.Children.Count > 0) return false;
            if (panelW.Children.Count > 0) return false;
            if (panelO.Children.Count > 0) return false;
            if (panelT.Children.Count > 0) return false;
            return true;
        }

        private void ApplyTemplate(string subject, string[] s, string[] w, string[] o, string[] t)
        {
            if (!IsSwotEmpty())
            {
                var result = MessageBox.Show(
                    "Bạn đang có dữ liệu phân tích SWOT hiện tại. Áp dụng template mới sẽ xóa toàn bộ dữ liệu này. Bạn có chắc chắn muốn tiếp tục không?",
                    "Xác nhận ghi đè dữ liệu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            ClearAll();
            txtSubject.Text = subject;

            foreach (var item in s) { AddSwotItem(panelS, "#2E7D32", "#E8F5E9"); SetLastItemText(panelS, item); }
            foreach (var item in w) { AddSwotItem(panelW, "#F57F17", "#FFF8E1"); SetLastItemText(panelW, item); }
            foreach (var item in o) { AddSwotItem(panelO, "#1565C0", "#E3F2FD"); SetLastItemText(panelO, item); }
            foreach (var item in t) { AddSwotItem(panelT, "#C62828", "#FFEBEE"); SetLastItemText(panelT, item); }
        }

        private void SetLastItemText(StackPanel panel, string text)
        {
            if (panel.Children.Count == 0) return;
            var lastRow = panel.Children[panel.Children.Count - 1] as Grid;
            if (lastRow == null) return;
            foreach (var child in lastRow.Children)
            {
                if (child is TextBox tb) { tb.Text = text; break; }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ACTIONS — Export, Clear
        // ═══════════════════════════════════════════════════════════

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = $"SWOT_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };
                if (dlg.ShowDialog() == true)
                {
                    var target = exportArea;
                    var bounds = new Rect(target.RenderSize);
                    double dpi = 192;
                    var rtb = new RenderTargetBitmap(
                        (int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96),
                        dpi, dpi, PixelFormats.Pbgra32);
                    var dv = new DrawingVisual();
                    using (var dc = dv.RenderOpen())
                    {
                        dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), bounds.Size));
                        dc.DrawRectangle(new VisualBrush(target), null, new Rect(new Point(), bounds.Size));
                    }
                    rtb.Render(dv);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using var stream = File.Create(dlg.FileName);
                    encoder.Save(stream);
                    MessageBox.Show($"Đã lưu: {dlg.FileName}", "Export thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi export: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Xóa toàn bộ phân tích SWOT?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                ClearAll();
        }

        private void ClearAll()
        {
            txtSubject.Text = "";
            panelS.Children.Clear();
            panelW.Children.Clear();
            panelO.Children.Clear();
            panelT.Children.Clear();
            strategySection.Visibility = Visibility.Collapsed;
            strategyContent.Children.Clear();
            UpdatePlaceholders();
        }

        private void UpdatePlaceholders()
        {
            if (lblPlaceholderS != null) lblPlaceholderS.Visibility = GetItemTexts(panelS).Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (lblPlaceholderW != null) lblPlaceholderW.Visibility = GetItemTexts(panelW).Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (lblPlaceholderO != null) lblPlaceholderO.Visibility = GetItemTexts(panelO).Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (lblPlaceholderT != null) lblPlaceholderT.Visibility = GetItemTexts(panelT).Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🎯",
                    Title = isVN ? "Hoạch định học tập & Bản thân" : "Academic & Personal SWOT",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_swot_1_{suffix}.png",
                    Description = isVN 
                        ? "Học sinh tự phân tích điểm mạnh (môn học thế mạnh, sự tập trung) và điểm yếu (lười biếng, hổng kiến thức) để thiết lập lộ trình học tập, luyện thi đại học và định hướng nghề nghiệp phù hợp." 
                        : "Students analyze their strengths (strong subjects, concentration) and weaknesses (procrastination, knowledge gaps) to set a customized study roadmap, prepare for college, and select career paths."
                },
                new PracticalAppItem
                {
                    Icon = "💼",
                    Title = isVN ? "Phát triển dự án khởi nghiệp" : "Startup Project SWOT",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_swot_2_{suffix}.png",
                    Description = isVN 
                        ? "Áp dụng SWOT để phân tích các ý tưởng dự án khởi nghiệp hoặc kinh doanh nhỏ của học sinh, nhận diện cơ hội thị trường và các thách thức từ đối thủ cạnh tranh trước khi thực thi." 
                        : "Apply SWOT to evaluate students' startup ideas or micro-businesses, identifying market opportunities and potential threats from competitors before execution."
                },
                new PracticalAppItem
                {
                    Icon = "👥",
                    Title = isVN ? "Quản lý hoạt động Câu lạc bộ" : "Club & Group Management",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_swot_3_{suffix}.png",
                    Description = isVN 
                        ? "Đánh giá nội bộ và môi trường bên ngoài khi vận hành đội nhóm, các câu lạc bộ thể thao học đường hoặc các chiến dịch tình nguyện để tối ưu nhân sự và đạt mục tiêu dự án." 
                        : "Assess internal capabilities and external environments when running student teams, school sports clubs, or volunteering campaigns to optimize staffing and reach project goals."
                }
            };

            try
            {
                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for SwotTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            // Ẩn tất cả
            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            // Hiện đúng tab tương ứng
            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPractice.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
