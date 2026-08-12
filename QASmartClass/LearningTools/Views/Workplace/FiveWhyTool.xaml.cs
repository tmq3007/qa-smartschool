using QASmartClass.LearningTools.Models;
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

namespace QASmartClass.LearningTools.Views.Workplace
{
    /// <summary>
    /// 🔍 5 Why – 1 How: Tìm nguyên nhân gốc rễ bằng chuỗi câu hỏi liên tiếp
    /// </summary>
    public partial class FiveWhyTool : UserControl
    {
        private readonly List<TextBox> _whyBoxes = new();
        private bool _isVN = true;

        // Màu gradient cho các cấp Why (từ nhạt → đậm)
        private static readonly string[] WhyColors = new[]
        {
            "#B2EBF2", "#80DEEA", "#4DD0E1", "#26C6DA", "#00BCD4",
            "#00ACC1", "#0097A7", "#00838F", "#006064"
        };
        private static readonly string[] WhyFgColors = new[]
        {
            "#006064", "#006064", "#004D40", "#004D40", "#FFFFFF",
            "#FFFFFF", "#FFFFFF", "#FFFFFF", "#FFFFFF"
        };

        public FiveWhyTool()
        {
            InitializeComponent();
            
            // Check language
            _isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";

            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("five_why");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            if (txtToolApplication != null) txtToolApplication.Text = toolInfo.RealWorldApplication;
            
            Loaded += (_, _) =>
            {
                // Thêm 5 ô Why mặc định
                for (int i = 0; i < 5; i++) AddWhyRow();

                // Gắn TouchTextPad
                TouchTextPad.Attach(txtProblem, mode: "text");
                TouchTextPad.Attach(txtHow, mode: "text");

                // Auto-update summary khi nội dung thay đổi
                txtProblem.TextChanged += (_, _) => UpdateSummary();
                txtHow.TextChanged += (_, _) => UpdateSummary();

                // Load ảnh và dịch giao diện
                LoadPracticalApps();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  WHY CHAIN — Tạo và quản lý chuỗi Why
        // ═══════════════════════════════════════════════════════════

        private void AddWhyRow()
        {
            int idx = _whyBoxes.Count + 1;
            int colorIdx = System.Math.Min(idx - 1, WhyColors.Length - 1);

            var bgColor = (Color)ColorConverter.ConvertFromString(WhyColors[colorIdx]);
            var fgColor = (Color)ColorConverter.ConvertFromString(WhyFgColors[colorIdx]);

            // Arrow connector
            if (idx > 1)
            {
                var arrow = new TextBlock
                {
                    Text = "  ↓",
                    FontSize = 20,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 121, 107)),
                    Margin = new Thickness(40, 4, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                whyChainPanel.Children.Add(arrow);
            }

            // Why card
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, bgColor.R, bgColor.G, bgColor.B)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 4),
                BorderBrush = new SolidColorBrush(bgColor),
                BorderThickness = new Thickness(1.5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Badge số thứ tự
            var badge = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(16),
                Width = 32, Height = 32,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            badge.Child = new TextBlock
            {
                Text = $"W{idx}",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fgColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(badge, 0);
            grid.Children.Add(badge);

            // Input area
            var inputPanel = new StackPanel();
            inputPanel.Children.Add(new TextBlock
            {
                Text = _isVN ? $"Why {idx}: Tại sao?" : $"Why {idx}: Why?",
                FontSize = DS.FontLabel, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 96, 100)),
                Margin = new Thickness(0, 0, 0, 4)
            });

            var txtWhy = new TextBox
            {
                FontSize = DS.FontInput, FontFamily = DS.FontPrimary,
                Padding = new Thickness(10, 8, 10, 8),
                AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                MinHeight = 40,
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, bgColor.R, bgColor.G, bgColor.B)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Foreground = new SolidColorBrush(DS.TextPrimary),
                Tag = idx
            };
            txtWhy.TextChanged += (_, _) => UpdateSummary();
            TouchTextPad.Attach(txtWhy, mode: "text");
            _whyBoxes.Add(txtWhy);
            inputPanel.Children.Add(txtWhy);

            Grid.SetColumn(inputPanel, 1);
            grid.Children.Add(inputPanel);

            // Delete button (luôn khởi tạo, ẩn/hiện dựa trên idx)
            var delBtn = new TextBlock
            {
                Text = "✕",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8, 2, 0, 0),
                ToolTip = "Xóa câu Why này",
                Visibility = idx > 5 ? Visibility.Visible : Visibility.Collapsed
            };
            var capturedCard = card;
            delBtn.MouseLeftButtonDown += (_, _) => RemoveWhyRow(capturedCard, txtWhy);
            Grid.SetColumn(delBtn, 2);
            grid.Children.Add(delBtn);

            card.Child = grid;
            whyChainPanel.Children.Add(card);
        }

        private void RemoveWhyRow(Border card, TextBox txtWhy)
        {
            // Tìm arrow trước card
            int cardIndex = whyChainPanel.Children.IndexOf(card);
            if (cardIndex > 0 && whyChainPanel.Children[cardIndex - 1] is TextBlock arrow && arrow.Text.Trim() == "↓")
            {
                whyChainPanel.Children.RemoveAt(cardIndex - 1);
                cardIndex--;
            }
            whyChainPanel.Children.Remove(card);
            _whyBoxes.Remove(txtWhy);
            RenumberWhyLabels();
            UpdateSummary();
        }

        private void RenumberWhyLabels()
        {
            int currentIdx = 1;
            foreach (var child in whyChainPanel.Children)
            {
                if (child is Border card)
                {
                    int idx = currentIdx;
                    int colorIdx = System.Math.Min(idx - 1, WhyColors.Length - 1);
                    var bgColor = (Color)ColorConverter.ConvertFromString(WhyColors[colorIdx]);
                    var fgColor = (Color)ColorConverter.ConvertFromString(WhyFgColors[colorIdx]);

                    // Cập nhật background và border của card
                    card.Background = new SolidColorBrush(Color.FromArgb(40, bgColor.R, bgColor.G, bgColor.B));
                    card.BorderBrush = new SolidColorBrush(bgColor);

                    if (card.Child is Grid grid)
                    {
                        // Cập nhật Badge (Cột 0)
                        var badge = grid.Children.OfType<Border>().FirstOrDefault(b => Grid.GetColumn(b) == 0);
                        if (badge != null)
                        {
                            badge.Background = new SolidColorBrush(bgColor);
                            if (badge.Child is TextBlock badgeText)
                            {
                                badgeText.Text = $"W{idx}";
                                badgeText.Foreground = new SolidColorBrush(fgColor);
                            }
                        }

                        // Cập nhật Label & TextBox (Cột 1)
                        var inputPanel = grid.Children.OfType<StackPanel>().FirstOrDefault(p => Grid.GetColumn(p) == 1);
                        if (inputPanel != null && inputPanel.Children.Count >= 2)
                        {
                            if (inputPanel.Children[0] is TextBlock labelText)
                            {
                                labelText.Text = _isVN ? $"Why {idx}: Tại sao?" : $"Why {idx}: Why?";
                            }
                            if (inputPanel.Children[1] is TextBox txtWhy)
                            {
                                txtWhy.Tag = idx;
                                txtWhy.BorderBrush = new SolidColorBrush(Color.FromArgb(80, bgColor.R, bgColor.G, bgColor.B));
                            }
                        }

                        // Cập nhật hiển thị nút xóa (Cột 2)
                        var delBtn = grid.Children.OfType<TextBlock>().FirstOrDefault(tb => Grid.GetColumn(tb) == 2);
                        if (delBtn != null)
                        {
                            delBtn.Visibility = idx > 5 ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                    currentIdx++;
                }
            }
        }

        private void AddWhy_Click(object sender, RoutedEventArgs e)
        {
            if (_whyBoxes.Count >= 9)
            {
                MessageBox.Show("Tối đa 9 cấp Why. Nếu chưa tìm được nguyên nhân gốc, hãy xem lại logic phân tích.",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            AddWhyRow();
        }

        // ═══════════════════════════════════════════════════════════
        //  SUMMARY — Tóm tắt phân tích
        // ═══════════════════════════════════════════════════════════

        private void UpdateSummary()
        {
            var filledWhys = _whyBoxes.Where(b => !string.IsNullOrWhiteSpace(b.Text)).ToList();
            bool hasProblem = !string.IsNullOrWhiteSpace(txtProblem.Text);
            bool hasHow = !string.IsNullOrWhiteSpace(txtHow.Text);

            if (!hasProblem && filledWhys.Count == 0)
            {
                summarySection.Visibility = Visibility.Collapsed;
                return;
            }

            summarySection.Visibility = Visibility.Visible;
            summaryContent.Children.Clear();

            // Problem
            if (hasProblem)
            {
                AddSummaryRow(_isVN ? "📋 Vấn đề:" : "📋 Problem:", txtProblem.Text.Trim(), "#C62828");
            }

            // Why chain
            for (int i = 0; i < filledWhys.Count; i++)
            {
                string prefix = i == filledWhys.Count - 1 ? (_isVN ? "🎯 Nguyên nhân gốc:" : "🎯 Root cause:") : $"Why {filledWhys[i].Tag}:";
                string color = i == filledWhys.Count - 1 ? "#E65100" : "#00796B";
                AddSummaryRow(prefix, filledWhys[i].Text.Trim(), color);

                // Arrow between
                if (i < filledWhys.Count - 1)
                {
                    summaryContent.Children.Add(new TextBlock
                    {
                        Text = "    ↓",
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 121, 107)),
                        Margin = new Thickness(0, 2, 0, 2)
                    });
                }
            }

            // How
            if (hasHow)
            {
                summaryContent.Children.Add(new Border
                {
                    Height = 1, Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    Margin = new Thickness(0, 8, 0, 8)
                });
                AddSummaryRow(_isVN ? "✅ Giải pháp:" : "✅ Solution:", txtHow.Text.Trim(), "#2E7D32");
            }

            // Completeness indicator (WPF responsive Grid stars)
            int totalSteps = _whyBoxes.Count + 2; // whys + problem + how
            int filledSteps = filledWhys.Count + (hasProblem ? 1 : 0) + (hasHow ? 1 : 0);
            double pct = (double)filledSteps / totalSteps * 100;

            var progressGrid = new Grid { Margin = new Thickness(0, 12, 0, 0), Height = 6 };
            if (pct <= 0)
            {
                progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
                progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            else if (pct >= 100)
            {
                progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
            }
            else
            {
                progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(pct, GridUnitType.Star) });
                progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - pct, GridUnitType.Star) });
            }

            var progressBar = new Border
            {
                Height = 6, CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            };
            Grid.SetColumnSpan(progressBar, 2);
            progressGrid.Children.Add(progressBar);

            var fill = new Border
            {
                Height = 6, CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(pct >= 80 ? Color.FromRgb(46, 125, 50) : Color.FromRgb(0, 121, 107)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Grid.SetColumn(fill, 0);
            progressGrid.Children.Add(fill);

            summaryContent.Children.Add(progressGrid);

            summaryContent.Children.Add(new TextBlock
            {
                Text = _isVN ? $"Hoàn thành: {filledSteps}/{totalSteps} bước ({pct:F0}%)" : $"Completed: {filledSteps}/{totalSteps} steps ({pct:F0}%)",
                FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        private void AddSummaryRow(string label, string value, string colorHex)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text = label,
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);

            var val = new TextBlock
            {
                Text = value,
                FontSize = 14, FontFamily = DS.FontPrimary,
                Foreground = new SolidColorBrush(DS.TextPrimary),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(val, 1);
            grid.Children.Add(val);

            summaryContent.Children.Add(grid);
        }

        // ═══════════════════════════════════════════════════════════
        //  TEMPLATES — Mẫu phân tích có sẵn
        // ═══════════════════════════════════════════════════════════

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("five_why");

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
                var data = (QASmartClass.LearningTools.Helpers.FiveWhyData)t.Data;
                item.Click += (_, _) => ApplyTemplate(data.Problem, new[] { data.Why1, data.Why2, data.Why3, data.Why4, data.Why5 }, data.How);
                parentItem.Items.Add(item);
            }

            menu.PlacementTarget = (Button)sender;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void ApplyTemplate(string problem, string[] whys, string how)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }

            // Xóa hiện tại
            ClearAll();

            txtProblem.Text = problem;

            // Đảm bảo đủ số ô Why
            while (_whyBoxes.Count < whys.Length)
                AddWhyRow();

            for (int i = 0; i < whys.Length && i < _whyBoxes.Count; i++)
                _whyBoxes[i].Text = whys[i];

            txtHow.Text = how;
            UpdateSummary();
        }

        // ═══════════════════════════════════════════════════════════
        //  ACTIONS — Export, Clear
        // ═══════════════════════════════════════════════════════════

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
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = $"5Why_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (dlg.ShowDialog() == true)
                {
                    // Render mainPanel to bitmap
                    var target = mainPanel;
                    var bounds = new Rect(target.RenderSize);
                    double dpi = 192;
                    var rtb = new RenderTargetBitmap(
                         (int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96),
                         dpi, dpi, PixelFormats.Pbgra32);

                    var dv = new DrawingVisual();
                    using (var dc = dv.RenderOpen())
                    {
                        // Draw solid white background first to avoid transparent image issues
                        dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), bounds.Size));
                        // Then draw target visual brush
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
                MessageBox.Show($"Lỗi export: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Xóa toàn bộ phân tích?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                ClearAll();
        }

        private void ClearAll()
        {
            txtProblem.Text = "";
            txtHow.Text = "";
            whyChainPanel.Children.Clear();
            _whyBoxes.Clear();
            summarySection.Visibility = Visibility.Collapsed;
            summaryContent.Children.Clear();

            // Tạo lại 5 ô Why mặc định
            for (int i = 0; i < 5; i++) AddWhyRow();
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                if (!isVN)
                {
                    if (menuTextGuide != null) menuTextGuide.Text = "Guide & Workflow";
                    if (menuTextWorkspace != null) menuTextWorkspace.Text = "Analysis & Workspace";
                    if (menuTextPractical != null) menuTextPractical.Text = "Real-world Applications";

                    if (btnTemplate != null)
                    {
                        btnTemplate.ToolTip = "Select a sample analysis template";
                        btnTemplate.Content = "🎯 Templates ▼";
                    }
                    if (btnExport != null) btnExport.ToolTip = "Export analysis as PNG image";
                    if (btnClear != null)
                    {
                        btnClear.ToolTip = "Clear entire analysis";
                        btnClear.Content = "🗑️ Clear all";
                    }

                    if (lblOriginTitle != null) lblOriginTitle.Text = "💡 ORIGIN & MEANING";
                    if (lblAppTitle != null) lblAppTitle.Text = "🌍 REAL-WORLD APPLICATION";
                    if (lblWorkflowTitle != null) lblWorkflowTitle.Text = "🛠️ VISUAL WORKFLOW WORKSTEP";

                    if (lblGuideExampleTitle != null) lblGuideExampleTitle.Text = "📝 VISUAL STUDY EXAMPLE (STUDYING)";
                    if (guideProblemLabel != null) guideProblemLabel.Text = "📋 Problem:";
                    if (guideProblemVal != null) guideProblemVal.Text = "Math semester exam grade dropped";
                    if (guideWhy1Label != null) guideWhy1Label.Text = "❓ Why 1:";
                    if (guideWhy1Val != null) guideWhy1Val.Text = "Did not study in time, only started studying right before exam day";
                    if (guideWhy2Label != null) guideWhy2Label.Text = "❓ Why 2:";
                    if (guideWhy2Val != null) guideWhy2Val.Text = "Procrastinated studying, prioritized video games";
                    if (guideWhy3Label != null) guideWhy3Label.Text = "❓ Why 3:";
                    if (guideWhy3Val != null) guideWhy3Val.Text = "Felt discouraged by the large volume of study material";
                    if (guideWhy4Label != null) guideWhy4Label.Text = "❓ Why 4:";
                    if (guideWhy4Val != null) guideWhy4Val.Text = "Did not make a daily study plan with small steps";
                    if (guideWhy5Label != null) guideWhy5Label.Text = "🎯 Why 5 (Root):";
                    if (guideWhy5Val != null) guideWhy5Val.Text = "Lacked personal goal management method (no mind map or plan used)";
                    if (guideHowLabel != null) guideHowLabel.Text = "✅ How Solution:";
                    if (guideHowVal != null) guideHowVal.Text = "Apply Pomodoro technique and break down study plan into 5 daily tasks";

                    if (step1Header != null) step1Header.Text = "Step 1";
                    if (step1Title != null) step1Title.Text = "📋 Problem";
                    if (step1Desc != null) step1Desc.Text = "Clearly state the specific situation encountered (e.g. poor grade, delay...).";

                    if (step2Header != null) step2Header.Text = "Step 2";
                    if (step2Title != null) step2Title.Text = "❓ 5 Why Chain";
                    if (step2Desc != null) step2Desc.Text = "Continuously ask 'Why?' for the previous answer.";

                    if (step3Header != null) step3Header.Text = "Step 3";
                    if (step3Title != null) step3Title.Text = "🎯 Root Cause";
                    if (step3Desc != null) step3Desc.Text = "Identify the core cause (usually at the 5th Why question).";

                    if (step4Header != null) step4Header.Text = "Step 4";
                    if (step4Title != null) step4Title.Text = "✅ How Solution";
                    if (step4Desc != null) step4Desc.Text = "Propose 1 specific solution to completely overcome the issue.";

                    if (lblProblemTitle != null) lblProblemTitle.Text = "📋 Problem to Analyze";
                    if (txtProblem != null) txtProblem.ToolTip = "Describe the problem you want to analyze";
                    if (lblHowTitle != null) lblHowTitle.Text = "✅ HOW — SOLUTION";
                    if (lblHowSubtitle != null) lblHowSubtitle.Text = "How to solve the root cause?";
                    if (lblSummaryTitle != null) lblSummaryTitle.Text = "📊 ANALYSIS SUMMARY";
                    if (btnAddWhy != null)
                    {
                        btnAddWhy.Content = "➕ Add Why Question";
                        btnAddWhy.ToolTip = "Add one more Why level";
                    }
                }

                var items = new List<PracticalAppItem>
                {
new PracticalAppItem
                    {
                        Icon = "🏭",
                        Title = isVN ? "Dây chuyền sản xuất Toyota dừng máy" : "Factory Line Shutdown",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_why_1_{suffix}.png",
                        Description = isVN 
                            ? "Vấn đề: Máy đột ngột dừng hoạt động.&#x0a;• Why 1: Cầu chì bị đứt do quá tải.&#x0a;• Why 2: Vòng bi bị kẹt do thiếu bôi trơn.&#x0a;• Why 3: Bơm bôi trơn không hoạt động tốt.&#x0a;• Why 4: Trục bơm bị mòn và rơ.&#x0a;• Why 5: Không có bộ lọc mạt kim loại lọt vào.&#x0a;=> How (Giải pháp): Lắp bộ lọc mạt bụi sắt cho bơm." 
                            : "Trace machine stops (Overloaded fuse -> Dry shaft -> Worn pump shaft -> Clogged oil filter -> Maintenance schedule missing)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⏰",
                        Title = isVN ? "Học sinh đi học muộn" : "Software Server Crash",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_why_2_{suffix}.png",
                        Description = isVN 
                            ? "Vấn đề: Học sinh đi học muộn.&#x0a;• Why 1: Ngủ dậy trễ so với giờ học.&#x0a;• Why 2: Chuông báo thức không reo.&#x0a;• Why 3: Đồng hồ báo thức bị hết pin.&#x0a;• Why 4: Quên mua pin mới dự phòng.&#x0a;• Why 5: Quên ghi pin vào danh sách mua sắm.&#x0a;=> How (Giải pháp): Đặt lịch nhắc nhở mua sắm định kỳ trên điện thoại." 
                            : "Trace server crash (Out of memory -> Large logs -> Debug mode enabled -> Human config mistake -> Config check missing)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📚",
                        Title = isVN ? "Tỷ lệ nộp bài tập về nhà thấp" : "Missed School Deadlines",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_why_3_{suffix}.png",
                        Description = isVN 
                            ? "Vấn đề: Học sinh không nộp bài tập đúng hạn.&#x0a;• Why 1: Nhiều học sinh không hiểu đề bài.&#x0a;• Why 2: Học sinh bỏ lỡ phần giải thích ở lớp.&#x0a;• Why 3: Giáo viên giải thích quá nhanh ở cuối giờ.&#x0a;• Why 4: Tiết học thường bị quá giờ.&#x0a;• Why 5: Phân bổ thời gian bài học chưa hợp lý.&#x0a;=> How (Giải pháp): Sắp xếp lại giáo án, dành riêng 5 phút cuối giờ để hướng dẫn bài tập." 
                            : "Trace late homework (Overslept -> Alarm failed -> Dead battery -> Charger broken -> Spare charger not prepared)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💰",
                        Title = isVN ? "Tiêu hết tiền tiêu vặt quá sớm" : "Poor Academic Scores",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_why_4_{suffix}.png",
                        Description = isVN 
                            ? "Vấn đề: Hết tiền tiêu vặt trước nửa tháng.&#x0a;• Why 1: Chi tiêu quá mức trong tuần đầu tiên.&#x0a;• Why 2: Mua phụ kiện chơi game đắt tiền.&#x0a;• Why 3: Bị thu hút bởi đợt giảm giá chớp nhoáng (Flash Sale).&#x0a;• Why 4: Không lập kế hoạch và theo dõi chi tiêu.&#x0a;• Why 5: Thiếu kiến thức về quản lý tài chính cá nhân.&#x0a;=> How (Giải pháp): Sử dụng ứng dụng quản lý chi tiêu và đặt hạn mức hàng tuần." 
                            : "Trace low math scores (Failed test -> Unprepared -> Didn't study -> Didn't understand -> Didn't ask for help early)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👁",
                        Title = isVN ? "Thị lực học sinh suy giảm nhanh" : "Customer Churn Spike",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_why_5_{suffix}.png",
                        Description = isVN 
                            ? "Vấn đề: Thị lực học sinh giảm sút nhanh chóng.&#x0a;• Why 1: Nhìn màn hình thiết bị quá lâu không nghỉ.&#x0a;• Why 2: Chơi game muộn vào ban đêm.&#x0a;• Why 3: Muốn cố gắng vượt qua các màn game trước khi ngủ.&#x0a;• Why 4: Lịch sinh hoạt giấc ngủ không điều độ.&#x0a;• Why 5: Nghiện game online trên điện thoại di động.&#x0a;=> How (Giải pháp): Cài đặt phần mềm giới hạn thời gian và áp dụng quy tắc mắt 20-20-20." 
                            : "Trace lost clients (Slow app load -> Heavy database query -> Missing index -> Code review skipped -> Release deadline rushed)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "👥",
                        Title = isVN ? "Trễ hạn nộp dự án học tập nhóm" : "Household Water Leak",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_five_why_6_{suffix}.png",
                        Description = isVN 
                            ? "Vấn đề: Bài tập nhóm bị nộp trễ hạn.&#x0a;• Why 1: Quá trình tổng hợp tài liệu của nhóm bị chậm.&#x0a;• Why 2: Các thành viên nộp phần việc cá nhân muộn.&#x0a;• Why 3: Công việc phân chia không rõ ràng từ đầu.&#x0a;• Why 4: Trưởng nhóm không giao mốc thời gian cụ thể (Milestones).&#x0a;• Why 5: Trưởng nhóm thiếu kỹ năng quản lý dự án học tập.&#x0a;=> How (Giải pháp): Lập lịch họp tuần và dùng bảng phân việc Trello có ghi hạn chót." 
                            : "Trace ceiling spot (Pipe leak -> Rusted joint -> High acidity -> Softener failed -> Maintenance interval skipped)."
                    }
            };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for FiveWhyTool: {Err}", ex.Message);
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
}