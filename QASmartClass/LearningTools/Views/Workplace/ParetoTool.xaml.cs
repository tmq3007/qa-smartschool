using QASmartClass.LearningTools.Models;
using System;
using QASmartClass.LearningTools.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Workplace
{
    public partial class ParetoTool : UserControl
    {
        private class DataItem
        {
            public string Category { get; set; } = "";
            public double Value { get; set; }
            public double CumPercent { get; set; }
        }

        private List<DataItem> _currentData = new();

        public ParetoTool()
        {
            InitializeComponent();
            LoadPracticalApps();
            var toolInfo = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetToolInfo("pareto");
            if (txtToolMeaning != null) txtToolMeaning.Text = toolInfo.Meaning;
            
            Loaded += (_, _) =>
            {
                if (dataPanel.Children.Count == 0)
                {
                    // Auto-load the first template on start
                    var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("pareto");
                    if (templates.Count > 0)
                    {
                        var defaultTemplate = templates[0];
                        var data = (QASmartClass.LearningTools.Helpers.ParetoData)defaultTemplate.Data;
                        
                        txtInputTitle.Text = defaultTemplate.Name;
                        foreach (var item in data.Items)
                        {
                            AddDataRow(item.Name, item.Value.ToString());
                        }
                        // Draw immediately
                        DrawChartDirectly();
                    }
                    else
                    {
                        for (int i = 0; i < 5; i++) AddDataRow();
                    }
                }
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  DATA ENTRY
        // ═══════════════════════════════════════════════════════════

        private void AddRow_Click(object sender, RoutedEventArgs e) => AddDataRow();

        private void AddDataRow(string cat = "", string val = "")
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

            var txtCat = new TextBox
            {
                Text = cat, FontSize = 14, FontFamily = DS.FontPrimary,
                Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 8, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224))
            };
            TouchTextPad.Attach(txtCat, mode: "text");
            Grid.SetColumn(txtCat, 0);
            row.Children.Add(txtCat);

            var txtVal = new TextBox
            {
                Text = val, FontSize = 14, FontFamily = DS.FontPrimary,
                Padding = new Thickness(8, 6, 8, 6), TextAlignment = TextAlignment.Center,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224))
            };
            // Use TouchNumPad instead of TouchTextPad for numerical fields!
            TouchNumPad.Attach(txtVal, min: 0.0, allowDecimal: true, allowNegative: false, enableMathKeys: true);
            Grid.SetColumn(txtVal, 1);
            row.Children.Add(txtVal);

            var btnDel = new TextBlock
            {
                Text = "✕", FontSize = 16, Foreground = Brushes.Gray,
                Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnDel.MouseEnter += (_, _) => btnDel.Foreground = Brushes.Red;
            btnDel.MouseLeave += (_, _) => btnDel.Foreground = Brushes.Gray;
            btnDel.MouseLeftButtonDown += (_, _) => dataPanel.Children.Remove(row);
            Grid.SetColumn(btnDel, 2);
            row.Children.Add(btnDel);

            dataPanel.Children.Add(row);
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_currentData.Count > 0) DrawChart();
        }

        private bool IsDataPanelEmpty()
        {
            foreach (Grid row in dataPanel.Children)
            {
                if (row.Children.Count >= 2 && row.Children[0] is TextBox txtCat && row.Children[1] is TextBox txtVal)
                {
                    if (!string.IsNullOrWhiteSpace(txtCat.Text) || !string.IsNullOrWhiteSpace(txtVal.Text))
                        return false;
                }
            }
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        //  DRAW CHART
        // ═══════════════════════════════════════════════════════════

        private void Draw_Click(object sender, RoutedEventArgs e)
        {
            DrawChartDirectly();
        }

        private void DrawChartDirectly()
        {
            var rawData = new List<DataItem>();
            var errors = new List<string>();
            int rowIdx = 1;

            foreach (Grid row in dataPanel.Children)
            {
                if (row.Children.Count >= 2 && row.Children[0] is TextBox txtCat && row.Children[1] is TextBox txtVal)
                {
                    string cat = txtCat.Text.Trim();
                    string valStr = txtVal.Text.Trim();

                    bool hasCat = !string.IsNullOrEmpty(cat);
                    bool hasVal = !string.IsNullOrEmpty(valStr);

                    if (hasCat || hasVal)
                    {
                        if (!hasCat)
                        {
                            errors.Add($"Dòng {rowIdx}: Thiếu tên danh mục.");
                        }
                        else if (!hasVal)
                        {
                            errors.Add($"Dòng {rowIdx} ({cat}): Thiếu tần suất.");
                        }
                        else if (!ParsingHelper.TryParseDouble(valStr, out double val))
                        {
                            errors.Add($"Dòng {rowIdx} ({cat}): Tần suất '{valStr}' không phải là số hợp lệ.");
                        }
                        else if (val <= 0)
                        {
                            errors.Add($"Dòng {rowIdx} ({cat}): Tần suất phải lớn hơn 0.");
                        }
                        else
                        {
                            rawData.Add(new DataItem { Category = cat, Value = val });
                        }
                    }
                }
                rowIdx++;
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", errors), "Lỗi nhập dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (rawData.Count == 0)
            {
                MessageBox.Show("Vui lòng nhập ít nhất 1 dòng dữ liệu hợp lệ (Tần suất > 0).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Group duplicate categories (case-insensitive)
            var groupedData = rawData
                .GroupBy(d => d.Category, StringComparer.OrdinalIgnoreCase)
                .Select(g => new DataItem { Category = g.Key, Value = g.Sum(x => x.Value) })
                .OrderByDescending(d => d.Value)
                .ToList();

            // Group extra categories beyond top 7 to keep layout neat
            _currentData = new List<DataItem>();
            if (groupedData.Count > 8)
            {
                _currentData.AddRange(groupedData.Take(7));
                double restSum = groupedData.Skip(7).Sum(d => d.Value);
                if (restSum > 0)
                {
                    _currentData.Add(new DataItem { Category = "Khác", Value = restSum });
                }
            }
            else
            {
                _currentData = groupedData;
            }

            // Calculate cumulative %
            double total = _currentData.Sum(d => d.Value);
            double cum = 0;
            foreach (var item in _currentData)
            {
                cum += item.Value;
                item.CumPercent = (cum / total) * 100;
            }

            // Update title
            string titleText = string.IsNullOrWhiteSpace(txtInputTitle.Text) ? "BIỂU ĐỒ PARETO" : txtInputTitle.Text.Trim();
            txtChartTitle.Text = $"BIỂU ĐỒ PARETO: {titleText.ToUpper()}";

            DrawChart();
            GenerateConclusion();
        }

        private void DrawChart()
        {
            chartCanvas.Children.Clear();
            if (_currentData.Count == 0) return;

            double width = chartCanvas.ActualWidth;
            double height = chartCanvas.ActualHeight;
            if (width < 100 || height < 100) return;

            // Symmetric paddings to ensure enough room for labels
            double padLeft = 60;
            double padRight = 60;
            double padTop = 40;
            double padBottom = 60;

            double chartW = width - padLeft - padRight;
            double chartH = height - padTop - padBottom;

            double total = _currentData.Sum(d => d.Value);
            if (total <= 0) return;

            // Left axis maximum matches total for perfect synchronization!
            double maxVal = total;

            int n = _currentData.Count;
            double spacing = chartW / n;
            double colWidth = System.Math.Min(60, spacing * 0.8);

            // Draw Axes
            var axisStroke = new SolidColorBrush(Color.FromRgb(189, 189, 189)); // Gray-400
            chartCanvas.Children.Add(new Line { X1 = padLeft, Y1 = padTop + chartH, X2 = padLeft + chartW, Y2 = padTop + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // X
            chartCanvas.Children.Add(new Line { X1 = padLeft, Y1 = padTop, X2 = padLeft, Y2 = padTop + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // Y1 (Left)
            chartCanvas.Children.Add(new Line { X1 = padLeft + chartW, Y1 = padTop, X2 = padLeft + chartW, Y2 = padTop + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // Y2 (Right)

            // Dynamic number of ticks
            int ticks = total >= 5 ? 5 : (int)total;
            if (ticks == 0) ticks = 1;

            // Draw Gridlines and Ticks
            for (int i = 0; i <= ticks; i++)
            {
                double tickY = padTop + chartH - (i * chartH / ticks);
                double tickVal = i * maxVal / ticks;
                double tickPct = i * 100.0 / ticks;

                // Horizontal grid line
                if (i > 0)
                {
                    chartCanvas.Children.Add(new Line
                    {
                        X1 = padLeft, Y1 = tickY, X2 = padLeft + chartW, Y2 = tickY,
                        Stroke = new SolidColorBrush(Color.FromRgb(240, 240, 240)), StrokeThickness = 1
                    });
                }

                // Y1 Tick (Frequency)
                chartCanvas.Children.Add(new Line { X1 = padLeft - 4, Y1 = tickY, X2 = padLeft, Y2 = tickY, Stroke = axisStroke, StrokeThickness = 1 });
                var lblTick1 = new TextBlock
                {
                    Text = tickVal.ToString("F0"), FontSize = 12, FontFamily = DS.FontPrimary,
                    Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)) // Gray-600
                };
                lblTick1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(lblTick1, padLeft - lblTick1.DesiredSize.Width - 6);
                Canvas.SetTop(lblTick1, tickY - 7);
                chartCanvas.Children.Add(lblTick1);

                // Y2 Tick (Percentage)
                chartCanvas.Children.Add(new Line { X1 = padLeft + chartW, Y1 = tickY, X2 = padLeft + chartW + 4, Y2 = tickY, Stroke = axisStroke, StrokeThickness = 1 });
                var lblTick2 = new TextBlock
                {
                    Text = $"{tickPct:F0}%", FontSize = 12, FontFamily = DS.FontPrimary,
                    Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97))
                };
                lblTick2.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(lblTick2, padLeft + chartW + 6);
                Canvas.SetTop(lblTick2, tickY - 7);
                chartCanvas.Children.Add(lblTick2);
            }

            // Colors
            var colorPrimaryTeal = Color.FromRgb(0, 121, 107);      // Dark Teal
            var colorSecondaryTeal = Color.FromRgb(178, 223, 219);  // Light Teal
            var colorOrange = Color.FromRgb(230, 81, 0);            // Brand Accent Orange

            var brushPrimaryTeal = new SolidColorBrush(colorPrimaryTeal);
            var brushSecondaryTeal = new SolidColorBrush(colorSecondaryTeal);
            var brushOrange = new SolidColorBrush(colorOrange);

            // T Y80 position
            double currentThreshold = sliderThreshold != null ? sliderThreshold.Value : 80.0;
            double y80 = padTop + chartH - (currentThreshold / 100.0) * chartH;

            // Draw Bars & Line Points
            var points = new PointCollection();
            points.Add(new Point(padLeft, padTop + chartH)); // start from origin

            for (int i = 0; i < n; i++)
            {
                var item = _currentData[i];
                double xCenter = padLeft + (i + 0.5) * spacing;
                
                // Color bar based on Pareto Threshold:
                bool isVitalFew = (i == 0) || (_currentData[i - 1].CumPercent < currentThreshold);
                var barFill = isVitalFew ? brushPrimaryTeal : brushSecondaryTeal;

                // Bar height
                double barH = (item.Value / maxVal) * chartH;
                var rect = new Rectangle
                {
                    Width = colWidth, Height = 0, Fill = barFill,
                    ToolTip = null // Handled by hover card for cleaner look
                };
                Canvas.SetLeft(rect, xCenter - colWidth / 2);
                Canvas.SetTop(rect, padTop + chartH);
                chartCanvas.Children.Add(rect);

                // Setup interactive hover effect for the bar
                var currentItem = item;
                rect.MouseEnter += (s, e) =>
                {
                    rect.Opacity = 0.8;
                    ShowHoverCard(currentItem.Category, $"Tần suất: {currentItem.Value}", $"Chiếm tỷ lệ: {(currentItem.Value / total * 100):F1}% (Tích lũy: {currentItem.CumPercent:F1}%)");
                };
                rect.MouseMove += (s, e) => MoveHoverCard(e.GetPosition(chartCanvas));
                rect.MouseLeave += (s, e) =>
                {
                    rect.Opacity = 1.0;
                    HideHoverCard();
                };

                // Animate bar growth
                var heightAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0, To = barH,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                var topAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = padTop + chartH, To = padTop + chartH - barH,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                rect.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);
                rect.BeginAnimation(Canvas.TopProperty, topAnim);

                // Label Value on Bar (To avoid overlap with cumulative line point at index 0)
                var lblVal = new TextBlock
                {
                    Text = item.Value.ToString(), FontSize = 12, FontWeight = FontWeights.SemiBold,
                    FontFamily = DS.FontPrimary
                };
                lblVal.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                if (barH > 25)
                {
                    // Draw inside the bar
                    Canvas.SetTop(lblVal, padTop + chartH - barH + 4);
                    // Dynamically set foreground color for contrast
                    lblVal.Foreground = isVitalFew ? Brushes.White : new SolidColorBrush(Color.FromRgb(0, 77, 64)); // Dark Teal for light bg
                }
                else
                {
                    // Draw above the bar
                    Canvas.SetTop(lblVal, padTop + chartH - barH - 18);
                    lblVal.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // Gray-900
                }
                Canvas.SetLeft(lblVal, xCenter - lblVal.DesiredSize.Width / 2);
                chartCanvas.Children.Add(lblVal);

                // Fade in value labels
                lblVal.Opacity = 0;
                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 1.0, Duration = TimeSpan.FromMilliseconds(300),
                    BeginTime = TimeSpan.FromMilliseconds(400)
                };
                lblVal.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                // Point for Line
                double pointY = padTop + chartH - (item.CumPercent / 100.0) * chartH;
                points.Add(new Point(xCenter, pointY));

                // Label X
                var lblX = new TextBlock
                {
                    Text = item.Category, FontSize = 12, MaxWidth = spacing - 4,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextAlignment = TextAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    FontFamily = DS.FontPrimary, ToolTip = item.Category
                };
                lblX.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(lblX, xCenter - lblX.DesiredSize.Width / 2);
                Canvas.SetTop(lblX, padTop + chartH + 8);
                chartCanvas.Children.Add(lblX);
            }

            // Draw Line
            var polyline = new Polyline
            {
                Points = points, Stroke = brushOrange, StrokeThickness = 2.5, StrokeLineJoin = PenLineJoin.Round
            };
            chartCanvas.Children.Add(polyline);

            // Animate Polyline fade-in
            polyline.Opacity = 0;
            var lineFade = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 1.0, Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = TimeSpan.FromMilliseconds(200)
            };
            polyline.BeginAnimation(UIElement.OpacityProperty, lineFade);

            // Draw Points
            int pointIdx = 0;
            foreach (var pt in points.Skip(1)) // skip origin
            {
                var ell = new Ellipse { Width = 6, Height = 6, Fill = brushOrange };
                Canvas.SetLeft(ell, pt.X - 3);
                Canvas.SetTop(ell, pt.Y - 3);
                chartCanvas.Children.Add(ell);

                var item = _currentData[pointIdx];
                var ptLabel = new TextBlock
                {
                    Text = $"{item.CumPercent:F0}%", FontSize = 12, Foreground = brushOrange,
                    FontWeight = FontWeights.Medium, FontFamily = DS.FontPrimary
                };
                ptLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(ptLabel, pt.X - ptLabel.DesiredSize.Width / 2);
                Canvas.SetTop(ptLabel, pt.Y - 18);
                chartCanvas.Children.Add(ptLabel);

                // Setup interactive hover effect for the cumulative point
                var currentItem = item;
                ell.MouseEnter += (s, e) =>
                {
                    ell.Width = 10; ell.Height = 10;
                    Canvas.SetLeft(ell, pt.X - 5); Canvas.SetTop(ell, pt.Y - 5);
                    ShowHoverCard($"Điểm Tích Lũy: {currentItem.Category}", $"Giá trị tích lũy: {currentItem.Value}", $"Tỷ lệ tích lũy: {currentItem.CumPercent:F1}%");
                };
                ell.MouseMove += (s, e) => MoveHoverCard(e.GetPosition(chartCanvas));
                ell.MouseLeave += (s, e) =>
                {
                    ell.Width = 6; ell.Height = 6;
                    Canvas.SetLeft(ell, pt.X - 3); Canvas.SetTop(ell, pt.Y - 3);
                    HideHoverCard();
                };

                // Animate points fade-in with staggered delay
                ell.Opacity = 0;
                ptLabel.Opacity = 0;
                var dotFade = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 1.0, Duration = TimeSpan.FromMilliseconds(300),
                    BeginTime = TimeSpan.FromMilliseconds(300 + pointIdx * 80)
                };
                ell.BeginAnimation(UIElement.OpacityProperty, dotFade);
                ptLabel.BeginAnimation(UIElement.OpacityProperty, dotFade);

                pointIdx++;
            }

            // Draw 80% Intersection Lines
            double xIntersect = padLeft + chartW;
            bool foundIntersection = false;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Point pA = points[i];
                Point pB = points[i + 1];

                // Y goes down in WPF, so Y of 0% (padTop + chartH) > Y of 80% (y80) > Y of 100% (padTop).
                if (pA.Y >= y80 && pB.Y <= y80)
                {
                    if (System.Math.Abs(pB.Y - pA.Y) > 1e-5)
                    {
                        double t = (y80 - pA.Y) / (pB.Y - pA.Y);
                        xIntersect = pA.X + t * (pB.X - pA.X);
                    }
                    else
                    {
                        xIntersect = pB.X;
                    }
                    foundIntersection = true;
                    break;
                }
            }

            // Horizontal line from intersection to right axis (80%)
            var line80H = new Line
            {
                X1 = xIntersect, Y1 = y80, X2 = padLeft + chartW, Y2 = y80,
                Stroke = brushOrange, StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection(new[] { 4.0, 4.0 })
            };
            chartCanvas.Children.Add(line80H);

            if (foundIntersection)
            {
                // Vertical line from intersection down to X-axis
                var line80V = new Line
                {
                    X1 = xIntersect, Y1 = y80, X2 = xIntersect, Y2 = padTop + chartH,
                    Stroke = brushOrange, StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection(new[] { 4.0, 4.0 })
                };
                chartCanvas.Children.Add(line80V);

                // Orange Intersection Dot with white stroke
                var dot = new Ellipse
                {
                    Width = 8, Height = 8,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 109, 0)),
                    Stroke = Brushes.White, StrokeThickness = 1.5
                };
                Canvas.SetLeft(dot, xIntersect - 4);
                Canvas.SetTop(dot, y80 - 4);
                chartCanvas.Children.Add(dot);
            }
        }

        private void GenerateConclusion()
        {
            if (_currentData.Count == 0) return;
            
            double currentThreshold = sliderThreshold != null ? sliderThreshold.Value : 80.0;
            var importantCats = new List<string>();
            for (int i = 0; i < _currentData.Count; i++)
            {
                importantCats.Add(_currentData[i].Category);
                if (_currentData[i].CumPercent >= currentThreshold) break; // found the cutoff
            }

            conclusionBorder.Visibility = Visibility.Visible;
            if (importantCats.Count < _currentData.Count)
            {
                txtConclusion.Text = $"Nhóm nguyên nhân: {string.Join(", ", importantCats)} đóng góp khoảng {currentThreshold:F0}% vấn đề. Bạn nên tập trung nguồn lực giải quyết các mục này trước tiên.";
            }
            else
            {
                txtConclusion.Text = "Tất cả các nguyên nhân đều phân bổ khá đều, không có sự chênh lệch rõ ràng. Bạn có thể cần xem xét lại cách thu thập dữ liệu hoặc giải quyết từng mục theo khả năng.";
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TEMPLATES & EXPORT & CLEAR
        // ═══════════════════════════════════════════════════════════

        private void Template_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var templates = QASmartClass.LearningTools.Helpers.WorkplaceTemplateManager.GetTemplates("pareto");
            var groups = new Dictionary<string, MenuItem>();

            foreach (var t in templates)
            {
                if (!groups.TryGetValue(t.Category, out var parentItem))
                {
                    parentItem = new MenuItem { Header = t.Category, FontSize = 13, FontWeight = FontWeights.Bold };
                    groups[t.Category] = parentItem;
                    menu.Items.Add(parentItem);
                }

                var item = new MenuItem { Header = t.Name, FontSize = 13, FontWeight = FontWeights.Normal };
                var data = (QASmartClass.LearningTools.Helpers.ParetoData)t.Data;
                item.Click += (_, _) =>
                {
                    if (sideMenu != null && sideMenu.SelectedIndex != 1)
                    {
                        sideMenu.SelectedIndex = 0;
                    }
                    // Overwrite warning only if the data panel is not empty!
                    if (!IsDataPanelEmpty())
                    {
                        if (MessageBox.Show("Tải mẫu mới sẽ ghi đè lên dữ liệu hiện tại. Bạn có muốn tiếp tục?", "Xác nhận ghi đè", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }

                    // Reset quietly
                    dataPanel.Children.Clear();
                    chartCanvas.Children.Clear();
                    _currentData.Clear();
                    conclusionBorder.Visibility = Visibility.Collapsed;
                    
                    txtInputTitle.Text = t.Name;
                    foreach (var i in data.Items) AddDataRow(i.Name, i.Value.ToString());
                    DrawChartDirectly();
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
                var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"Pareto_{DateTime.Now:yyyyMMdd_HHmmss}.png" };
                if (dlg.ShowDialog() == true)
                {
                    var target = chartBorder;
                    var bounds = new Rect(target.RenderSize);
                    var rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);
                    var dv = new DrawingVisual();
                    using (var dc = dv.RenderOpen()) 
                    { 
                        // Vẽ màu nền trắng đục đè lên để tránh nền trong suốt trong PNG
                        dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), bounds.Size));
                        dc.DrawRectangle(new VisualBrush(target), null, new Rect(new Point(), bounds.Size)); 
                    }
                    rtb.Render(dv);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using var stream = File.Create(dlg.FileName);
                    encoder.Save(stream);
                    MessageBox.Show($"Đã lưu biểu đồ: {dlg.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (IsDataPanelEmpty())
            {
                // Reset quietly if already empty
                dataPanel.Children.Clear();
                chartCanvas.Children.Clear();
                _currentData.Clear();
                conclusionBorder.Visibility = Visibility.Collapsed;
                for (int i = 0; i < 5; i++) AddDataRow();
                return;
            }

            if (MessageBox.Show("Xóa toàn bộ dữ liệu hiện tại?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                dataPanel.Children.Clear();
                chartCanvas.Children.Clear();
                _currentData.Clear();
                conclusionBorder.Visibility = Visibility.Collapsed;
                for (int i = 0; i < 5; i++) AddDataRow();
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
                        Icon = "🐛",
                        Title = isVN ? "Lỗi Phần Mềm" : "Software Bug Fix Priority",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pareto_1_{suffix}.png",
                        Description = isVN 
                            ? "Trong công nghệ, 80% các vụ sập hệ thống hoặc lỗi người dùng bắt nguồn từ 20% các lỗi code (bugs) cốt lõi. Lập trình viên cần ưu tiên tìm và sửa các lỗi chính này." 
                            : "Identify and fix the 20% core database and memory bugs that cause 80% of application crashes."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📦",
                        Title = isVN ? "Quản Lý Kho Hàng" : "Warehouse Inventory Management",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pareto_2_{suffix}.png",
                        Description = isVN 
                            ? "80% doanh thu hoặc giá trị tồn kho tập trung ở 20% nhóm sản phẩm chủ lực (Phân loại kho ABC). Việc tối ưu kho giúp giảm đáng kể chi phí lãng phí." 
                            : "Focus storage security and optimal counts on the 20% high-value products that generate 80% of warehouse value."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🗣",
                        Title = isVN ? "Khiếu Nại Khách Hàng" : "Resolving Customer Complaints",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pareto_3_{suffix}.png",
                        Description = isVN 
                            ? "80% lời phàn nàn của khách hàng đến từ 20% các khiếm khuyết của dịch vụ/sản phẩm. Giải quyết triệt để 20% này sẽ giúp nâng cao sự hài lòng chung." 
                            : "Prioritize resolving the 20% service flaws that trigger 80% of negative reviews, boosting customer satisfaction."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📝",
                        Title = isVN ? "Lỗi Sai Bài Thi" : "Analyzing Exam Mistakes",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pareto_4_{suffix}.png",
                        Description = isVN 
                            ? "80% số điểm bị mất trong bài kiểm tra thường do 20% lỗi lặp đi lặp lại (ví dụ: tính nhầm dấu, đọc sai đề). Khắc phục hai lỗi này giúp cải thiện điểm nhanh chóng." 
                            : "Help students eliminate the 20% common error types (like reading signs wrong) that cause 80% of their lost points."
                    },
                    new PracticalAppItem
                    {
                        Icon = "💰",
                        Title = isVN ? "Chi Phí Hoạt Động" : "Cost Control & Budgets",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pareto_5_{suffix}.png",
                        Description = isVN 
                            ? "80% tổng ngân sách chi tiêu của câu lạc bộ hoặc cá nhân tập trung vào 20% hạng mục lớn (như thuê địa điểm, thiết bị). Kiểm soát tốt 20% này giúp cân đối tài chính." 
                            : "Audit and control the 20% largest expense items (e.g. rent, salaries) that make up 80% of total company spending."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🗑",
                        Title = isVN ? "Rác Thải Học Đường" : "School Waste Cleanup",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_pareto_6_{suffix}.png",
                        Description = isVN 
                            ? "80% khối lượng rác thải nhựa tại trường học bắt nguồn từ 20% nguồn phát thải chính (tin học, căn tin). Tập trung xử lý tại đây sẽ cải thiện môi trường đáng kể." 
                            : "Target waste reduction efforts at the 20% source areas (like the cafeteria) that produce 80% of school garbage."
                    }
            };

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for ParetoTool: {Err}", ex.Message);
            }
        }

        private void Threshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lblThresholdVal != null)
            {
                lblThresholdVal.Text = $"{e.NewValue:F0}%";
            }
            if (_currentData.Count > 0)
            {
                DrawChart();
                GenerateConclusion();
            }
        }

        private void ShowHoverCard(string title, string valLine, string cumLine)
        {
            if (hoverCard == null) return;
            lblHoverTitle.Text = title;
            lblHoverVal.Text = valLine;
            lblHoverCum.Text = cumLine;
            hoverCard.Visibility = Visibility.Visible;
        }

        private void MoveHoverCard(Point mousePos)
        {
            if (hoverCard == null) return;
            double left = mousePos.X + 15;
            double top = mousePos.Y + 15;

            if (left + hoverCard.ActualWidth > chartCanvas.ActualWidth)
            {
                left = mousePos.X - hoverCard.ActualWidth - 15;
            }
            if (top + hoverCard.ActualHeight > chartCanvas.ActualHeight)
            {
                top = mousePos.Y - hoverCard.ActualHeight - 15;
            }

            Canvas.SetLeft(hoverCard, System.Math.Max(0, left));
            Canvas.SetTop(hoverCard, System.Math.Max(0, top));
        }

        private void HideHoverCard()
        {
            if (hoverCard != null)
            {
                hoverCard.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataToSave = new SavedParetoData
                {
                    Title = txtInputTitle.Text,
                    Threshold = sliderThreshold.Value,
                    Rows = new List<SavedRow>()
                };

                foreach (Grid row in dataPanel.Children)
                {
                    if (row.Children.Count >= 2 && row.Children[0] is TextBox txtCat && row.Children[1] is TextBox txtVal)
                    {
                        dataToSave.Rows.Add(new SavedRow
                        {
                            Category = txtCat.Text,
                            Value = txtVal.Text
                        });
                    }
                }

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FileName = "ParetoData.json"
                };

                if (dlg.ShowDialog() == true)
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(dataToSave, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(dlg.FileName, json, System.Text.Encoding.UTF8);
                    MessageBox.Show("Lưu dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu tệp: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
                };

                if (dlg.ShowDialog() == true)
                {
                    string json = File.ReadAllText(dlg.FileName, System.Text.Encoding.UTF8);
                    
                    if (!json.Trim().StartsWith("{") || !json.Contains("Rows"))
                    {
                        throw new InvalidDataException("Định dạng dữ liệu tệp JSON không hợp lệ.");
                    }

                    var loadedData = Newtonsoft.Json.JsonConvert.DeserializeObject<SavedParetoData>(json);
                    if (loadedData == null || loadedData.Rows == null)
                    {
                        throw new InvalidDataException("Không thể đọc được dữ liệu từ tệp.");
                    }

                    if (!IsDataPanelEmpty())
                    {
                        if (MessageBox.Show("Mở tệp mới sẽ ghi đè lên dữ liệu hiện tại. Bạn có muốn tiếp tục?", "Xác nhận ghi đè", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }

                    txtInputTitle.Text = loadedData.Title ?? "";
                    sliderThreshold.Value = loadedData.Threshold >= 50 && loadedData.Threshold <= 100 ? loadedData.Threshold : 80;
                    
                    dataPanel.Children.Clear();
                    foreach (var row in loadedData.Rows)
                    {
                        AddDataRow(row.Category, row.Value);
                    }

                    DrawChartDirectly();
                    MessageBox.Show("Tải dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tệp tin không hợp lệ hoặc bị lỗi: {ex.Message}", "Lỗi tải tệp", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private class SavedRow
        {
            public string Category { get; set; } = "";
            public string Value { get; set; } = "";
        }

        private class SavedParetoData
        {
            public string Title { get; set; } = "";
            public double Threshold { get; set; } = 80;
            public List<SavedRow> Rows { get; set; } = new();
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