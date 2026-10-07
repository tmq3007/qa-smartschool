using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Animation;
using System.Windows.Documents;
using WpfPath = System.Windows.Shapes.Path;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using QASmartTouch.Helpers;
using QASmartTouch.Shared;

namespace QASmartTouch.Forms
{
    public partial class Form2_10_PieChartEditor : Window
    {
        private List<PieDataEntry> chartData;
        private Form2_MainDashboard? _mainDashboard;

        public bool IsConfirmed { get; private set; }
        public bool DisplayStatsMode { get; private set; } = false;
        public PieChartConfiguration CurrentConfiguration { get; private set; }
        public double? TargetLeft { get; set; }
        public double? TargetTop { get; set; }

        // Initial chart type to set before Loaded event
        public string? InitialChartType { get; set; } = null;

        // Configuration Properties (public for MainDashboard access)
        public string ChartTitle { get; private set; } = "Thị phần sản phẩm năm 2024";
        public List<string> Labels { get; private set; } = new List<string>();
        public List<double> Values { get; private set; } = new List<double>();
        public List<Color> Colors { get; private set; } = new List<Color>();
        public string ChartType { get; private set; } = "Pie"; // Pie, Donut, Semi
        public string DisplayStyle { get; private set; } = "Normal"; // Normal, Exploded, 3D
        public double ChartSize { get; private set; } = 80;
        public double DonutHoleSize { get; private set; } = 50;
        public double StartAngle { get; private set; } = -90;
        public double ExplodeDistance { get; private set; } = 20;
        public bool ShowLabels { get; private set; } = true;
        public bool ShowPercentages { get; private set; } = true;
        public bool ShowValues { get; private set; } = false;
        public bool ShowLegend { get; private set; } = true;
        public bool ShowShadow { get; private set; } = true;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lấy kích thước màn hình hiện tại
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // Lấy kích thước cửa sổ
            var windowWidth = this.ActualWidth;
            var windowHeight = this.ActualHeight;

            // Căn giữa theo chiều ngang và sát cạnh dưới
            this.Left = (screenWidth - windowWidth) / 2;
            this.Top = screenHeight - windowHeight - 10; // chừa 10px cách mép dưới (nếu muốn sát hẳn thì để 0)
        }

        public Form2_10_PieChartEditor(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            TouchScrollHelper.AttachToAllScrollViewers(this);
            _mainDashboard = mainDashboard;
            InitializeDefaultData();
            TxtChartTitle.TextChanged += (s, e) =>
            {
                ChartTitle = TxtChartTitle.Text;
                if (IsLoaded) UpdateChart();
            };
            Loaded += (s, e) =>
            {
                // Apply initial chart type if set
                if (!string.IsNullOrEmpty(InitialChartType))
                {
                    switch (InitialChartType)
                    {
                        case "Pie":
                            CmbChartType.SelectedIndex = 0;
                            break;
                        case "Donut":
                            CmbChartType.SelectedIndex = 1;
                            break;
                        case "Semi":
                            CmbChartType.SelectedIndex = 2;
                            break;
                    }
                }
                UpdateChart();
            };
        }

        /// <summary>
        /// Set chart type from submenu selection
        /// </summary>
        public void SetChartType(int index)
        {
            if (CmbChartType != null)
            {
                CmbChartType.SelectedIndex = index;
            }
        }

        private void InitializeDefaultData()
        {
            chartData = new List<PieDataEntry>
            {
                new PieDataEntry { Label = "Sản phẩm A", Value = 35, Color = Color.FromRgb(255, 107, 107) },
                new PieDataEntry { Label = "Sản phẩm B", Value = 25, Color = Color.FromRgb(78, 205, 196) },
                new PieDataEntry { Label = "Sản phẩm C", Value = 20, Color = Color.FromRgb(69, 183, 209) },
                new PieDataEntry { Label = "Sản phẩm D", Value = 12, Color = Color.FromRgb(255, 160, 122) },
                new PieDataEntry { Label = "Sản phẩm E", Value = 8, Color = Color.FromRgb(152, 216, 200) }
            };

            RefreshDataEntries();
            UpdateStats();
        }

        private void RefreshDataEntries()
        {
            DataEntriesPanel.Children.Clear();

            foreach (var entry in chartData)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                // Color indicator button
                var colorBtn = new Button
                {
                    Width = 30,
                    Height = 30,
                    Background = new SolidColorBrush(entry.Color),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    Content = "",
                    Tag = entry
                };
                colorBtn.Click += ColorBtn_Click;
                Grid.SetColumn(colorBtn, 0);

                // Label textbox
                var txtLabel = new TextBox
                {
                    Text = entry.Label,
                    Padding = new Thickness(10),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    BorderThickness = new Thickness(2),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                txtLabel.TextChanged += (s, e) => { entry.Label = txtLabel.Text; UpdateStats(); if (IsLoaded) UpdateChart(); };
                Grid.SetColumn(txtLabel, 1);

                // Value textbox
                var txtValue = new TextBox
                {
                    Text = entry.Value.ToString(),
                    Padding = new Thickness(10),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    BorderThickness = new Thickness(2),
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                };
                txtValue.TextChanged += (s, e) =>
                {
                    if (double.TryParse(txtValue.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val) && val >= 0)
                    {
                        entry.Value = val;
                        UpdateStats();
                        if (IsLoaded) UpdateChart();
                    }
                };
                Grid.SetColumn(txtValue, 2);

                grid.Children.Add(colorBtn);
                grid.Children.Add(txtLabel);
                grid.Children.Add(txtValue);
                border.Child = grid;
                DataEntriesPanel.Children.Add(border);
            }
        }

        private void ColorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PieDataEntry entry)
            {
                // Simple color selection - cycle through preset colors
                var colors = new[] {
                    Color.FromRgb(255, 107, 107),
                    Color.FromRgb(78, 205, 196),
                    Color.FromRgb(69, 183, 209),
                    Color.FromRgb(255, 160, 122),
                    Color.FromRgb(152, 216, 200),
                    Color.FromRgb(221, 161, 94),
                    Color.FromRgb(66, 133, 244),
                    Color.FromRgb(255, 27, 45),
                    Color.FromRgb(0, 120, 215),
                    Color.FromRgb(255, 113, 57)
                };

                // Find current color index and move to next
                int currentIndex = -1;
                for (int i = 0; i < colors.Length; i++)
                {
                    if (entry.Color.R == colors[i].R && entry.Color.G == colors[i].G && entry.Color.B == colors[i].B)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                entry.Color = colors[(currentIndex + 1) % colors.Length];
                btn.Background = new SolidColorBrush(entry.Color);
                UpdateChart();
            }
        }

        private void UpdateStats()
        {
            double total = chartData.Sum(d => d.Value);
            TxtStatTotal.Text = total.ToString("N0");
            TxtStatCount.Text = chartData.Count.ToString();

            if (total > 0)
            {
                double maxPercent = chartData.Max(d => d.Value) / total * 100;
                double minPercent = chartData.Min(d => d.Value) / total * 100;
                TxtStatMax.Text = maxPercent.ToString("F1") + "%";
                TxtStatMin.Text = minPercent.ToString("F1") + "%";
            }
            else
            {
                TxtStatMax.Text = "0%";
                TxtStatMin.Text = "0%";
            }
        }

        private void UpdateChart()
        {
            ChartCanvas.Children.Clear();

            if (chartData.Count == 0 || chartData.Sum(d => d.Value) == 0)
                return;

            // Update properties from UI
            ChartTitle = TxtChartTitle.Text;
            ChartSize = SliderSize.Value;
            DonutHoleSize = SliderDonutHole.Value;
            StartAngle = SliderStartAngle.Value;
            ExplodeDistance = SliderExplodeDistance.Value;
            ShowLabels = ChkShowLabels.IsChecked == true;
            ShowPercentages = ChkShowPercentages.IsChecked == true;
            ShowValues = ChkShowValues.IsChecked == true;
            ShowLegend = ChkShowLegend.IsChecked == true;
            ShowShadow = ChkShowShadow.IsChecked == true;

            // Update public lists
            Labels = chartData.Select(d => d.Label).ToList();
            Values = chartData.Select(d => d.Value).ToList();
            Colors = chartData.Select(d => d.Color).ToList();

            // Get chart type
            var chartTypeIndex = CmbChartType.SelectedIndex;
            ChartType = chartTypeIndex switch
            {
                0 => "Pie",
                1 => "Donut",
                2 => "Semi",
                _ => "Pie"
            };

            // Get display style
            var displayStyleIndex = CmbDisplayStyle.SelectedIndex;
            DisplayStyle = displayStyleIndex switch
            {
                0 => "Normal",
                1 => "Exploded",
                2 => "3D",
                _ => "Normal"
            };

            // Use fixed canvas size (550x550 as defined in XAML)
            double canvasWidth = ChartCanvas.Width > 0 ? ChartCanvas.Width : 550;
            double canvasHeight = ChartCanvas.Height > 0 ? ChartCanvas.Height : 550;

            // Center the chart for balanced display - adjusted to prevent overflow
            double centerX = canvasWidth * 0.5;  // Centered for balanced layout
            double centerY = canvasHeight / 2;
            double radius = Math.Min(canvasWidth, canvasHeight) * (ChartSize / 100.0) * 0.24;  // Reduced from 0.28 to 0.24 to prevent overflow

            double total = chartData.Sum(d => d.Value);
            double startAngleRad = StartAngle * Math.PI / 180;

            // Draw title - reduced top margin from 20 to 10
            if (!string.IsNullOrWhiteSpace(ChartTitle))
            {
                var title = new TextBlock
                {
                    Text = ChartTitle,
                    FontSize = 18,  // Reduced from 20 to 18
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
                };
                Canvas.SetLeft(title, canvasWidth / 2 - 150);  // Center on canvas
                Canvas.SetTop(title, 10);  // Reduced from 20 to 10
                ChartCanvas.Children.Add(title);
            }

            // Adjust center for semi-circle
            if (ChartType == "Semi")
            {
                centerY = canvasHeight * 0.65;
                startAngleRad = Math.PI;
            }

            // Draw slices
            double currentAngle = startAngleRad;
            int index = 0;

            foreach (var entry in chartData)
            {
                double sweepAngle = ChartType == "Semi" 
                    ? (entry.Value / total) * Math.PI 
                    : (entry.Value / total) * 2 * Math.PI;

                // Calculate explode offset
                double explodeX = 0, explodeY = 0;
                if (DisplayStyle == "Exploded")
                {
                    double midAngle = currentAngle + sweepAngle / 2;
                    explodeX = Math.Cos(midAngle) * ExplodeDistance;
                    explodeY = Math.Sin(midAngle) * ExplodeDistance;
                }

                // Draw shadow for 3D effect
                if (DisplayStyle == "3D" || ShowShadow)
                {
                    DrawPieSlice(centerX + explodeX + 5, centerY + explodeY + 5, radius, 
                                currentAngle, sweepAngle, 
                                Color.FromArgb(50, 0, 0, 0), ChartType == "Donut" ? radius * (DonutHoleSize / 100.0) : 0);
                }

                // Draw main slice
                DrawPieSlice(centerX + explodeX, centerY + explodeY, radius, 
                            currentAngle, sweepAngle, 
                            entry.Color, ChartType == "Donut" ? radius * (DonutHoleSize / 100.0) : 0);

                // Draw labels
                if (ShowLabels || ShowPercentages || ShowValues)
                {
                    double midAngle = currentAngle + sweepAngle / 2;
                    double labelRadius = ChartType == "Donut" 
                        ? radius * (1 - DonutHoleSize / 200.0)
                        : radius * 0.7;
                    
                    double labelX = centerX + explodeX + Math.Cos(midAngle) * labelRadius;
                    double labelY = centerY + explodeY + Math.Sin(midAngle) * labelRadius;

                    string labelText = "";
                    if (ShowLabels) labelText += entry.Label;
                    if (ShowPercentages)
                    {
                        double percent = (entry.Value / total) * 100;
                        labelText += (labelText.Length > 0 ? "\n" : "") + percent.ToString("F1") + "%";
                    }
                    if (ShowValues)
                    {
                        labelText += (labelText.Length > 0 ? "\n" : "") + entry.Value.ToString("N0");
                    }

                    if (!string.IsNullOrWhiteSpace(labelText))
                    {
                        var label = new TextBlock
                        {
                            Text = labelText,
                            FontSize = 11,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.White,
                            TextAlignment = TextAlignment.Center,
                            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                            Padding = new Thickness(6, 3, 6, 3)
                        };

                        Canvas.SetLeft(label, labelX - 40);
                        Canvas.SetTop(label, labelY - 20);
                        ChartCanvas.Children.Add(label);
                    }
                }

                currentAngle += sweepAngle;
                index++;
            }

            // Draw legend
            if (ShowLegend)
            {
                DrawLegend(canvasWidth, canvasHeight);
            }
        }

        private void DrawPieSlice(double centerX, double centerY, double radius, 
                                  double startAngle, double sweepAngle, 
                                  Color color, double innerRadius = 0)
        {
            if (sweepAngle <= 0) return;

            // Limit sweep angle to avoid full circle rendering issues
            if (sweepAngle >= 2 * Math.PI - 0.001)
            {
                if (innerRadius > 0)
                {
                    // Draw donut with ellipse geometry
                    var outerEllipse = new EllipseGeometry(new Point(centerX, centerY), radius, radius);
                    var innerEllipse = new EllipseGeometry(new Point(centerX, centerY), innerRadius, innerRadius);
                    var combinedGeo = new CombinedGeometry(GeometryCombineMode.Exclude, outerEllipse, innerEllipse);
                    
                    var donutPath = new WpfPath
                    {
                        Fill = new SolidColorBrush(color),
                        Stroke = Brushes.White,
                        StrokeThickness = 2,
                        Data = combinedGeo
                    };
                    ChartCanvas.Children.Add(donutPath);
                }
                else
                {
                    // Draw full circle
                    var ellipse = new Ellipse
                    {
                        Width = radius * 2,
                        Height = radius * 2,
                        Fill = new SolidColorBrush(color),
                        Stroke = Brushes.White,
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(ellipse, centerX - radius);
                    Canvas.SetTop(ellipse, centerY - radius);
                    ChartCanvas.Children.Add(ellipse);
                }
                return;
            }

            double endAngle = startAngle + sweepAngle;

            // Outer arc points
            Point startPointOuter = new Point(
                centerX + radius * Math.Cos(startAngle),
                centerY + radius * Math.Sin(startAngle)
            );

            Point endPointOuter = new Point(
                centerX + radius * Math.Cos(endAngle),
                centerY + radius * Math.Sin(endAngle)
            );

            PathFigure figure = new PathFigure { StartPoint = startPointOuter };

            // Outer arc
            figure.Segments.Add(new ArcSegment
            {
                Point = endPointOuter,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = sweepAngle > Math.PI
            });

            if (innerRadius > 0)
            {
                // Inner arc points for donut
                Point endPointInner = new Point(
                    centerX + innerRadius * Math.Cos(endAngle),
                    centerY + innerRadius * Math.Sin(endAngle)
                );

                Point startPointInner = new Point(
                    centerX + innerRadius * Math.Cos(startAngle),
                    centerY + innerRadius * Math.Sin(startAngle)
                );

                figure.Segments.Add(new LineSegment { Point = endPointInner });
                figure.Segments.Add(new ArcSegment
                {
                    Point = startPointInner,
                    Size = new Size(innerRadius, innerRadius),
                    SweepDirection = SweepDirection.Counterclockwise,
                    IsLargeArc = sweepAngle > Math.PI
                });
                figure.Segments.Add(new LineSegment { Point = startPointOuter });
            }
            else
            {
                // Line to center for pie
                figure.Segments.Add(new LineSegment { Point = new Point(centerX, centerY) });
                figure.Segments.Add(new LineSegment { Point = startPointOuter });
            }

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            WpfPath pathShape = new WpfPath
            {
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Data = geometry
            };

            ChartCanvas.Children.Add(pathShape);
        }

        private void DrawLegend(double canvasWidth, double canvasHeight)
        {
            // Calculate total legend height (title + items)
            int totalItems = chartData.Count;
            double legendTitleHeight = 25;
            double legendItemsHeight = totalItems * 30;
            double totalLegendHeight = legendTitleHeight + legendItemsHeight;
            
            // Position legend at vertical center, 5mm (~19px) from left edge
            double legendX = 19;
            double legendY = (canvasHeight - totalLegendHeight) / 2;
            
            // Draw legend title "Chú thích:"
            var legendTitle = new TextBlock
            {
                Text = "Chú thích:",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40))
            };
            Canvas.SetLeft(legendTitle, legendX);
            Canvas.SetTop(legendTitle, legendY);
            ChartCanvas.Children.Add(legendTitle);
            
            // Draw legend items starting below the title
            double itemsStartY = legendY + legendTitleHeight;
            
            int index = 0;
            foreach (var entry in chartData)
            {
                var rect = new Rectangle
                {
                    Width = 20,
                    Height = 20,
                    Fill = new SolidColorBrush(entry.Color),
                    Stroke = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(rect, legendX);
                Canvas.SetTop(rect, itemsStartY + index * 30);
                ChartCanvas.Children.Add(rect);

                var text = new TextBlock
                {
                    Text = entry.Label,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
                };
                Canvas.SetLeft(text, legendX + 30);
                Canvas.SetTop(text, itemsStartY + index * 30 + 2);
                ChartCanvas.Children.Add(text);

                index++;
            }
        }

        private void OnChartPropertyChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            // Show/hide donut hole slider
            if (CmbChartType.SelectedIndex == 1) // Donut
            {
                DonutHoleGroup.Visibility = Visibility.Visible;
                SliderDonutHole.Visibility = Visibility.Visible;
            }
            else
            {
                DonutHoleGroup.Visibility = Visibility.Collapsed;
                SliderDonutHole.Visibility = Visibility.Collapsed;
            }

            // Show/hide explode distance slider
            if (CmbDisplayStyle.SelectedIndex == 1) // Exploded
            {
                ExplodeDistanceGroup.Visibility = Visibility.Visible;
                SliderExplodeDistance.Visibility = Visibility.Visible;
            }
            else
            {
                ExplodeDistanceGroup.Visibility = Visibility.Collapsed;
                SliderExplodeDistance.Visibility = Visibility.Collapsed;
            }

            UpdateChart();
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            if (sender == SliderSize)
                TxtSizeValue.Text = SliderSize.Value.ToString("F0") + "%";
            else if (sender == SliderDonutHole)
                TxtDonutHoleValue.Text = SliderDonutHole.Value.ToString("F0") + "%";
            else if (sender == SliderStartAngle)
                TxtStartAngleValue.Text = SliderStartAngle.Value.ToString("F0") + "°";
            else if (sender == SliderExplodeDistance)
                TxtExplodeDistanceValue.Text = SliderExplodeDistance.Value.ToString("F0") + "px";

            UpdateChart();
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateChart();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // STEP 1: Validate data
                if (chartData == null || chartData.Count == 0)
                {
                    MessageBox.Show("❌ Chưa có dữ liệu biểu đồ!", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var entry in chartData)
                {
                    if (string.IsNullOrWhiteSpace(entry.Label))
                    {
                        MessageBox.Show("❌ Vui lòng nhập tên cho tất cả các phần!", "Lỗi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (entry.Value <= 0)
                    {
                        MessageBox.Show("❌ Giá trị phải lớn hơn 0!", "Lỗi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // STEP 2: Save configuration
                SaveCurrentConfiguration();

                // Cập nhật và ép vẽ lại layout trước khi render bitmap
                UpdateChart();
                ChartBorder.UpdateLayout();

                // STEP 3: Confirm and display chart
                IsConfirmed = true;
                DialogResult = true;

                if (_mainDashboard != null)
                {
                    AddInteractiveChartToCanvas();
                }
                else
                {
                    MessageBox.Show("✅ Biểu đồ đã được tạo thành công!", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Đóng form mà không lưu
            IsConfirmed = false;
            this.Close();
        }

        private void BtnToggleKeyboard_Click(object sender, RoutedEventArgs e)
        {
            if (TouchKeyboardHelper.IsKeyboardVisible())
            {
                TouchKeyboardHelper.HideTouchKeyboard();
            }
            else
            {
                TouchKeyboardHelper.ShowTouchKeyboard();
            }
        }

        private void BtnDisplayWithStats_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DisplayStatsMode = true;
            DialogResult = true;
            Close();
        }

        private void BtnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = "PieChart_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var renderBitmap = new RenderTargetBitmap(
                        (int)ChartCanvas.ActualWidth,
                        (int)ChartCanvas.ActualHeight,
                        96, 96,
                        PixelFormats.Pbgra32
                    );

                    renderBitmap.Render(ChartCanvas);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (var stream = File.Create(saveDialog.FileName))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show("Đã lưu hình ảnh thành công!", "Thành công",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu hình ảnh: {ex.Message}", "Lỗi",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            InitializeDefaultData();
            TxtChartTitle.Text = "Thị phần sản phẩm năm 2024";
            CmbChartType.SelectedIndex = 0;
            CmbDisplayStyle.SelectedIndex = 0;
            SliderSize.Value = 80;
            SliderDonutHole.Value = 50;
            SliderStartAngle.Value = -90;
            SliderExplodeDistance.Value = 20;
            ChkShowLabels.IsChecked = true;
            ChkShowPercentages.IsChecked = true;
            ChkShowValues.IsChecked = false;
            ChkShowLegend.IsChecked = true;
            ChkShowShadow.IsChecked = true;
            UpdateChart();
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
            {
                UpdateChart();
            }
        }

        // Preset buttons
        private void BtnPresetMarket_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset("market");
        }

        private void BtnPresetExpense_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset("expense");
        }

        private void BtnPresetAge_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset("age");
        }

        private void BtnPresetBrowser_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset("browser");
        }

        private void LoadPreset(string presetName)
        {
            switch (presetName)
            {
                case "market":
                    TxtChartTitle.Text = "Thị phần sản phẩm năm 2024";
                    chartData = new List<PieDataEntry>
                    {
                        new PieDataEntry { Label = "Sản phẩm A", Value = 35, Color = Color.FromRgb(255, 107, 107) },
                        new PieDataEntry { Label = "Sản phẩm B", Value = 25, Color = Color.FromRgb(78, 205, 196) },
                        new PieDataEntry { Label = "Sản phẩm C", Value = 20, Color = Color.FromRgb(69, 183, 209) },
                        new PieDataEntry { Label = "Sản phẩm D", Value = 12, Color = Color.FromRgb(255, 160, 122) },
                        new PieDataEntry { Label = "Sản phẩm E", Value = 8, Color = Color.FromRgb(152, 216, 200) }
                    };
                    break;

                case "expense":
                    TxtChartTitle.Text = "Phân bổ chi tiêu hàng tháng";
                    chartData = new List<PieDataEntry>
                    {
                        new PieDataEntry { Label = "Ăn uống", Value = 30, Color = Color.FromRgb(255, 107, 107) },
                        new PieDataEntry { Label = "Nhà ở", Value = 35, Color = Color.FromRgb(78, 205, 196) },
                        new PieDataEntry { Label = "Đi lại", Value = 15, Color = Color.FromRgb(69, 183, 209) },
                        new PieDataEntry { Label = "Giải trí", Value = 10, Color = Color.FromRgb(255, 160, 122) },
                        new PieDataEntry { Label = "Tiết kiệm", Value = 7, Color = Color.FromRgb(152, 216, 200) },
                        new PieDataEntry { Label = "Khác", Value = 3, Color = Color.FromRgb(221, 161, 94) }
                    };
                    break;

                case "age":
                    TxtChartTitle.Text = "Phân bố độ tuổi khách hàng";
                    chartData = new List<PieDataEntry>
                    {
                        new PieDataEntry { Label = "18-25 tuổi", Value = 22, Color = Color.FromRgb(255, 107, 107) },
                        new PieDataEntry { Label = "26-35 tuổi", Value = 35, Color = Color.FromRgb(78, 205, 196) },
                        new PieDataEntry { Label = "36-45 tuổi", Value = 25, Color = Color.FromRgb(69, 183, 209) },
                        new PieDataEntry { Label = "46-55 tuổi", Value = 12, Color = Color.FromRgb(255, 160, 122) },
                        new PieDataEntry { Label = "Trên 55", Value = 6, Color = Color.FromRgb(152, 216, 200) }
                    };
                    break;

                case "browser":
                    TxtChartTitle.Text = "Thị phần trình duyệt web 2024";
                    chartData = new List<PieDataEntry>
                    {
                        new PieDataEntry { Label = "Chrome", Value = 65, Color = Color.FromRgb(66, 133, 244) },
                        new PieDataEntry { Label = "Safari", Value = 18, Color = Color.FromRgb(0, 0, 0) },
                        new PieDataEntry { Label = "Edge", Value = 8, Color = Color.FromRgb(0, 120, 215) },
                        new PieDataEntry { Label = "Firefox", Value = 5, Color = Color.FromRgb(255, 113, 57) },
                        new PieDataEntry { Label = "Opera", Value = 2, Color = Color.FromRgb(255, 27, 45) },
                        new PieDataEntry { Label = "Khác", Value = 2, Color = Color.FromRgb(149, 165, 166) }
                    };
                    break;
            }

            RefreshDataEntries();
            UpdateStats();
            UpdateChart();
        }

        private void SaveCurrentConfiguration()
        {
            try
            {
                CurrentConfiguration = new PieChartConfiguration
                {
                    ChartTitle = TxtChartTitle.Text,
                    ChartData = chartData.Select(d => new PieDataConfig
                    {
                        Label = d.Label,
                        Value = d.Value,
                        Color = d.Color
                    }).ToList(),
                    ShowPercentage = ChkShowPercentages.IsChecked ?? true,
                    ShowValues = ChkShowValues.IsChecked ?? false,
                    ShowLegend = ChkShowLegend.IsChecked ?? true
                };

                string appDataPath = QASmartClass.Services.AppPaths.RootDir;
                string configDir = IOPath.Combine(appDataPath, "ChartConfigs");
                
                if (!IODirectory.Exists(configDir))
                {
                    IODirectory.CreateDirectory(configDir);
                }

                string configFile = IOPath.Combine(configDir, $"PieChart_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                string jsonString = JsonSerializer.Serialize(CurrentConfiguration, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                IOFile.WriteAllText(configFile, jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi lưu cấu hình: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddInteractiveChartToCanvas()
        {
            if (_mainDashboard == null) return;

            // Render entire ChartBorder (includes background, shadow, and ChartCanvas)
            var renderBitmap = new RenderTargetBitmap(
                (int)ChartBorder.ActualWidth,
                (int)ChartBorder.ActualHeight,
                96, 96,
                PixelFormats.Pbgra32);

            renderBitmap.Render(ChartBorder);

            InteractiveChartHelper.CreateAndAddChartToCanvas(new InteractiveChartHelper.ChartContainerOptions
            {
                MainDashboard = _mainDashboard,
                ChartTitle = string.IsNullOrWhiteSpace(TxtChartTitle.Text) ? "Biểu đồ tròn" : $"Biểu đồ tròn: {TxtChartTitle.Text}",
                Icon = "🥧",
                Bitmap = renderBitmap,
                InitialWidth = ChartBorder.ActualWidth * 0.9,
                InitialHeight = ChartBorder.ActualHeight * 0.9,
                TargetLeft = TargetLeft,
                TargetTop = TargetTop,
                Configuration = CurrentConfiguration,
                OnEdit = (container) => EditChart(container),
                OnCopy = (container, bitmap) => CopyChart(container, bitmap),
                OnDelete = (container) => DeleteChart(container)
            });
        }

        private void CopyChart(Grid originalContainer, RenderTargetBitmap bitmap)
        {
            double originalLeft = Canvas.GetLeft(originalContainer);
            double originalTop = Canvas.GetTop(originalContainer);
            if (double.IsNaN(originalLeft)) originalLeft = 0;
            if (double.IsNaN(originalTop)) originalTop = 0;

            InteractiveChartHelper.CreateAndAddChartToCanvas(new InteractiveChartHelper.ChartContainerOptions
            {
                MainDashboard = _mainDashboard!,
                ChartTitle = string.IsNullOrWhiteSpace(TxtChartTitle.Text) ? "Biểu đồ tròn (Bản sao)" : $"Biểu đồ tròn: {TxtChartTitle.Text} (Bản sao)",
                Icon = "🥧",
                Bitmap = bitmap,
                InitialWidth = originalContainer.Width,
                InitialHeight = (originalContainer.Children.OfType<Border>().FirstOrDefault(b => Grid.GetRow(b) == 1)?.Child as Image)?.Height ?? (ChartBorder.ActualHeight * 0.9),
                TargetLeft = originalLeft + 30,
                TargetTop = originalTop + 30,
                Configuration = CurrentConfiguration,
                OnEdit = (container) => EditChart(container),
                OnCopy = (container, bmp) => CopyChart(container, bmp),
                OnDelete = (container) => DeleteChart(container)
            });
        }

        private void DeleteChart(Grid outerContainer)
        {
            var mainCanvas = _mainDashboard?.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas != null)
            {
                mainCanvas.Children.Remove(outerContainer);
                _mainDashboard?.RecordChartRemove(outerContainer, "Biểu đồ tròn");
            }
        }

        private void EditChart(Grid outerContainer)
        {
            if (CurrentConfiguration == null)
            {
                MessageBox.Show("❌ Không tìm thấy cấu hình biểu đồ!", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double originalLeft = Canvas.GetLeft(outerContainer);
            double originalTop = Canvas.GetTop(outerContainer);
            if (double.IsNaN(originalLeft)) originalLeft = 0;
            if (double.IsNaN(originalTop)) originalTop = 0;

            var editor = new Form2_10_PieChartEditor(_mainDashboard);
            editor.TargetLeft = originalLeft;
            editor.TargetTop = originalTop;
            editor.LoadConfiguration(CurrentConfiguration);
            bool? result = WindowHelper.ShowChildDialog(editor, _mainDashboard);
            if (result == true)
            {
                var mainCanvas = _mainDashboard?.FindName("MainInteractiveBoard") as Canvas;
                if (mainCanvas != null)
                {
                    mainCanvas.Children.Remove(outerContainer);
                    _mainDashboard?.RecordChartRemove(outerContainer, "Biểu đồ tròn");
                }
            }
        }

        public void LoadConfiguration(PieChartConfiguration config)
        {
            if (config == null) return;

            CurrentConfiguration = config;

            TxtChartTitle.Text = config.ChartTitle;
            
            chartData.Clear();
            foreach (var data in config.ChartData)
            {
                chartData.Add(new PieDataEntry
                {
                    Label = data.Label,
                    Value = data.Value,
                    Color = data.Color
                });
            }

            ChkShowPercentages.IsChecked = config.ShowPercentage;
            ChkShowValues.IsChecked = config.ShowValues;
            ChkShowLegend.IsChecked = config.ShowLegend;

            RefreshDataEntries();
            UpdateChart();
        }

        private RenderTargetBitmap CaptureChartAsImage()
        {
            var renderBitmap = new RenderTargetBitmap(
                (int)ChartCanvas.ActualWidth,
                (int)ChartCanvas.ActualHeight,
                96, 96,
                PixelFormats.Pbgra32);

            renderBitmap.Render(ChartCanvas);
            return renderBitmap;
        }

    }

    public class PieChartConfiguration
    {
        public string ChartTitle { get; set; }
        public List<PieDataConfig> ChartData { get; set; }
        public bool ShowPercentage { get; set; }
        public bool ShowValues { get; set; }
        public bool ShowLegend { get; set; }
    }

    public class PieDataConfig
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public Color Color { get; set; }
    }

    public class PieDataEntry
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
        public Color Color { get; set; }
    }
}

