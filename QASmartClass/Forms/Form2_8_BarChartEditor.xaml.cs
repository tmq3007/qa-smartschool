using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using QASmartTouch.Helpers;
using QASmartTouch.Shared;

namespace QASmartTouch.Forms
{
    public partial class Form2_8_BarChartEditor : Window
    {
        private List<ChartDataEntry> chartData;
        private Color primaryColor = Color.FromRgb(102, 126, 234);
        private Color secondaryColor = Color.FromRgb(118, 75, 162);
        private Form2_MainDashboard? _mainDashboard;

        public bool IsConfirmed { get; private set; }
        public bool DisplayStatsMode { get; private set; } = false;

        // Configuration properties for saving/loading
        public BarChartConfiguration CurrentConfiguration { get; private set; }
        public double? TargetLeft { get; set; }
        public double? TargetTop { get; set; }

        public Form2_8_BarChartEditor(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            TouchScrollHelper.AttachToAllScrollViewers(this);
            _mainDashboard = mainDashboard;
            InitializeDefaultData();
            txtChartTitle.TextChanged += (s, e) => { if (IsLoaded) UpdateChart(); };
            txtYAxisLabel.TextChanged += (s, e) => { if (IsLoaded) UpdateChart(); };
            Loaded += (s, e) => UpdateChart();
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded) UpdateChart();
        }

        private void InitializeDefaultData()
        {
            chartData = new List<ChartDataEntry>
            {
                new ChartDataEntry { Label = "Tháng 1", Value = 120 },
                new ChartDataEntry { Label = "Tháng 2", Value = 190 },
                new ChartDataEntry { Label = "Tháng 3", Value = 150 },
                new ChartDataEntry { Label = "Tháng 4", Value = 220 },
                new ChartDataEntry { Label = "Tháng 5", Value = 180 },
                new ChartDataEntry { Label = "Tháng 6", Value = 240 }
            };

            RefreshDataEntries();
        }

        private void RefreshDataEntries()
        {
            DataEntriesPanel.Children.Clear();

            foreach (var entry in chartData)
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txtLabel = new TextBox
                {
                    Text = entry.Label,
                    Padding = new Thickness(10),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    BorderThickness = new Thickness(2),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                txtLabel.TextChanged += (s, e) => { entry.Label = txtLabel.Text; UpdateStats(); if (IsLoaded) UpdateChart(); };
                Grid.SetColumn(txtLabel, 0);

                var txtValue = new TextBox
                {
                    Text = entry.Value.ToString(),
                    Padding = new Thickness(10),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    BorderThickness = new Thickness(2),
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                txtValue.TextChanged += (s, e) =>
                {
                    if (double.TryParse(txtValue.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                    {
                        entry.Value = val;
                        UpdateStats();
                        if (IsLoaded) UpdateChart();
                    }
                };
                Grid.SetColumn(txtValue, 1);

                var btnRemove = new Button
                {
                    Content = "×",
                    Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(12, 8, 12, 8),
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btnRemove.Click += (s, e) =>
                {
                    chartData.Remove(entry);
                    RefreshDataEntries();
                    UpdateStats();
                    UpdateChart();
                };
                Grid.SetColumn(btnRemove, 2);

                grid.Children.Add(txtLabel);
                grid.Children.Add(txtValue);
                grid.Children.Add(btnRemove);

                DataEntriesPanel.Children.Add(grid);
            }

            UpdateStats();
        }

        private void UpdateStats()
        {
            if (chartData.Count == 0)
            {
                txtStatTotal.Text = "0";
                txtStatAvg.Text = "0";
                txtStatMax.Text = "0";
                txtStatMin.Text = "0";
                return;
            }

            var total = chartData.Sum(d => d.Value);
            var avg = chartData.Average(d => d.Value);
            var max = chartData.Max(d => d.Value);
            var min = chartData.Min(d => d.Value);

            txtStatTotal.Text = total.ToString("F0");
            txtStatAvg.Text = avg.ToString("F1");
            txtStatMax.Text = max.ToString("F0");
            txtStatMin.Text = min.ToString("F0");
        }

        private void AddDataEntry_Click(object sender, RoutedEventArgs e)
        {
            chartData.Add(new ChartDataEntry { Label = "Mới", Value = 0 });
            RefreshDataEntries();
            UpdateChart();
        }

        private void LoadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string preset)
            {
                chartData.Clear();

                switch (preset)
                {
                    case "sales":
                        txtChartTitle.Text = "Doanh thu bán hàng theo tháng";
                        txtYAxisLabel.Text = "Doanh thu (triệu VNĐ)";
                        chartData.AddRange(new[]
                        {
                            new ChartDataEntry { Label = "Tháng 1", Value = 120 },
                            new ChartDataEntry { Label = "Tháng 2", Value = 190 },
                            new ChartDataEntry { Label = "Tháng 3", Value = 150 },
                            new ChartDataEntry { Label = "Tháng 4", Value = 220 },
                            new ChartDataEntry { Label = "Tháng 5", Value = 180 },
                            new ChartDataEntry { Label = "Tháng 6", Value = 240 }
                        });
                        break;

                    case "students":
                        txtChartTitle.Text = "Số lượng học sinh theo lớp";
                        txtYAxisLabel.Text = "Số học sinh";
                        chartData.AddRange(new[]
                        {
                            new ChartDataEntry { Label = "Lớp 6", Value = 35 },
                            new ChartDataEntry { Label = "Lớp 7", Value = 42 },
                            new ChartDataEntry { Label = "Lớp 8", Value = 38 },
                            new ChartDataEntry { Label = "Lớp 9", Value = 45 }
                        });
                        break;

                    case "temperature":
                        txtChartTitle.Text = "Nhiệt độ trung bình theo tháng";
                        txtYAxisLabel.Text = "Nhiệt độ (°C)";
                        chartData.AddRange(new[]
                        {
                            new ChartDataEntry { Label = "T1", Value = 18 },
                            new ChartDataEntry { Label = "T2", Value = 20 },
                            new ChartDataEntry { Label = "T3", Value = 25 },
                            new ChartDataEntry { Label = "T4", Value = 28 },
                            new ChartDataEntry { Label = "T5", Value = 32 },
                            new ChartDataEntry { Label = "T6", Value = 35 }
                        });
                        break;

                    case "products":
                        txtChartTitle.Text = "Sản phẩm bán chạy nhất";
                        txtYAxisLabel.Text = "Số lượng bán";
                        chartData.AddRange(new[]
                        {
                            new ChartDataEntry { Label = "Laptop", Value = 45 },
                            new ChartDataEntry { Label = "Điện thoại", Value = 89 },
                            new ChartDataEntry { Label = "Tablet", Value = 32 },
                            new ChartDataEntry { Label = "Tai nghe", Value = 67 }
                        });
                        break;
                }

                RefreshDataEntries();
                UpdateChart();
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            txtBarWidth.Text = $"{(int)sliderBarWidth.Value}%";
            txtBorderRadius.Text = $"{(int)sliderBorderRadius.Value}px";
            UpdateChart();
        }

        private void ChartSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateChart();
        }

        private void PrimaryColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Simple color picker - cycle through preset colors
            var colors = new[]
            {
                Color.FromRgb(102, 126, 234), // Blue
                Color.FromRgb(40, 167, 69),   // Green
                Color.FromRgb(255, 107, 107), // Red
                Color.FromRgb(255, 159, 67),  // Orange
                Color.FromRgb(142, 68, 173),  // Purple
                Color.FromRgb(52, 152, 219),  // Light Blue
                Color.FromRgb(231, 76, 60)    // Dark Red
            };
            
            var currentIndex = Array.FindIndex(colors, c => c.Equals(primaryColor));
            primaryColor = colors[(currentIndex + 1) % colors.Length];
            PrimaryColorPreview.Background = new SolidColorBrush(primaryColor);
            UpdateChart();
        }

        private void SecondaryColor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Simple color picker - cycle through preset colors
            var colors = new[]
            {
                Color.FromRgb(118, 75, 162),  // Purple
                Color.FromRgb(32, 191, 107),  // Green
                Color.FromRgb(255, 141, 96),  // Orange
                Color.FromRgb(255, 107, 129), // Pink
                Color.FromRgb(106, 90, 205),  // Slate Blue
                Color.FromRgb(72, 219, 251),  // Cyan
                Color.FromRgb(253, 121, 168)  // Rose
            };
            
            var currentIndex = Array.FindIndex(colors, c => c.Equals(secondaryColor));
            secondaryColor = colors[(currentIndex + 1) % colors.Length];
            SecondaryColorPreview.Background = new SolidColorBrush(secondaryColor);
            UpdateChart();
        }

        private void UpdateChart()
        {
            if (!IsLoaded || chartData.Count == 0) return;

            ChartCanvas.Children.Clear();

            var isColumnChart = cmbChartType.SelectedIndex == 0;
            var colorMode = cmbColorMode.SelectedIndex;
            var barWidthPercent = sliderBarWidth.Value / 100.0;
            var cornerRadius = sliderBorderRadius.Value;
            var showValues = chkShowValues.IsChecked == true;
            var showGrid = chkShowGrid.IsChecked == true;
            var show3D = chkShow3D.IsChecked == true;
            var showShadow = chkShowShadow.IsChecked == true;

            var width  = ChartCanvas.ActualWidth  > 50 ? ChartCanvas.ActualWidth  : 800;
            var height = ChartCanvas.ActualHeight > 50 ? ChartCanvas.ActualHeight : 550;

            // padding: Left=nhãn Y, Top=tiêu đề, Right=khoảng trống, Bottom=nhãn X + label trục X
            var padding = new Thickness(80, 60, 30, 120);
            var chartWidth  = width  - padding.Left - padding.Right;
            var chartHeight = height - padding.Top  - padding.Bottom;

            // Draw title
            var title = new TextBlock
            {
                Text = txtChartTitle.Text,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            Canvas.SetLeft(title, width / 2 - 150);
            Canvas.SetTop(title, 20);
            ChartCanvas.Children.Add(title);

            var maxValue = chartData.Max(d => d.Value);
            var scale = chartHeight / (maxValue * 1.1);

            // Draw grid
            if (showGrid)
            {
                for (int i = 0; i <= 5; i++)
                {
                    var y = padding.Top + (chartHeight / 5) * i;
                    var gridLine = new Line
                    {
                        X1 = padding.Left,
                        Y1 = y,
                        X2 = padding.Left + chartWidth,
                        Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 5, 5 }
                    };
                    ChartCanvas.Children.Add(gridLine);

                    var valueLabel = new TextBlock
                    {
                        Text = ((maxValue * 1.1 / 5) * (5 - i)).ToString("F0"),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102))
                    };
                    Canvas.SetLeft(valueLabel, padding.Left - 40);
                    Canvas.SetTop(valueLabel, y - 8);
                    ChartCanvas.Children.Add(valueLabel);
                }
            }

            // Draw axes
            var xAxis = new Line
            {
                X1 = padding.Left,
                Y1 = padding.Top + chartHeight,
                X2 = padding.Left + chartWidth,
                Y2 = padding.Top + chartHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            ChartCanvas.Children.Add(xAxis);

            var yAxis = new Line
            {
                X1 = padding.Left,
                Y1 = padding.Top,
                X2 = padding.Left,
                Y2 = padding.Top + chartHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            ChartCanvas.Children.Add(yAxis);

            // Y-axis label (top of Y-axis)
            var yLabelText = txtYAxisLabel.Text;
            var yLabel = new TextBlock
            {
                Text = yLabelText,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(yLabel, 10);
            Canvas.SetTop(yLabel, padding.Top - 30);
            ChartCanvas.Children.Add(yLabel);

            // X-axis label (bottom, căn giữa trục X)
            var xLabel = new TextBlock
            {
                Text = "Thời gian / Danh mục",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(xLabel, padding.Left + chartWidth / 2 - 80);
            Canvas.SetTop(xLabel, padding.Top + chartHeight + 58); // bên dưới nhãn cột
            ChartCanvas.Children.Add(xLabel);

            // Draw bars
            var barCount = chartData.Count;
            var barSpacing = chartWidth / barCount;
            var barWidth = barSpacing * barWidthPercent;

            for (int i = 0; i < barCount; i++)
            {
                var data = chartData[i];
                var barHeight = data.Value * scale;
                var x = padding.Left + (i * barSpacing) + (barSpacing - barWidth) / 2;
                var y = padding.Top + chartHeight - barHeight;

                // Determine bar color
                Brush barBrush;
                if (colorMode == 0) // Single color
                {
                    barBrush = new SolidColorBrush(primaryColor);
                }
                else if (colorMode == 1) // Gradient
                {
                    barBrush = new LinearGradientBrush(primaryColor, secondaryColor, 90);
                }
                else // Multi-color
                {
                    var colors = new[] { 
                        Color.FromRgb(102, 126, 234), 
                        Color.FromRgb(118, 75, 162),
                        Color.FromRgb(237, 100, 166),
                        Color.FromRgb(255, 107, 107),
                        Color.FromRgb(255, 159, 67),
                        Color.FromRgb(72, 219, 251)
                    };
                    barBrush = new SolidColorBrush(colors[i % colors.Length]);
                }

                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    Fill = barBrush,
                    RadiusX = cornerRadius,
                    RadiusY = cornerRadius
                };

                if (showShadow)
                {
                    bar.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.Black,
                        Opacity = 0.3,
                        ShadowDepth = 4,
                        BlurRadius = 8
                    };
                }

                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                ChartCanvas.Children.Add(bar);

                // 3D effect
                if (show3D)
                {
                    var depth = 8;
                    var side = new Polygon
                    {
                        Points = new PointCollection
                        {
                            new Point(x + barWidth, y),
                            new Point(x + barWidth + depth, y - depth),
                            new Point(x + barWidth + depth, y + barHeight - depth),
                            new Point(x + barWidth, y + barHeight)
                        },
                        Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0))
                    };
                    ChartCanvas.Children.Add(side);

                    var top = new Polygon
                    {
                        Points = new PointCollection
                        {
                            new Point(x, y),
                            new Point(x + depth, y - depth),
                            new Point(x + barWidth + depth, y - depth),
                            new Point(x + barWidth, y)
                        },
                        Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
                    };
                    ChartCanvas.Children.Add(top);
                }

                // Value label on top of bar
                if (showValues)
                {
                    var valueText = new TextBlock
                    {
                        Text = data.Value.ToString("F0"),
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94))
                    };
                    Canvas.SetLeft(valueText, x + barWidth / 2 - 15);
                    Canvas.SetTop(valueText, y - 25);
                    ChartCanvas.Children.Add(valueText);
                }

                // X-axis label
                var categoryLabel = new TextBlock
                {
                    Text = data.Label,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                Canvas.SetLeft(categoryLabel, x + barWidth / 2 - 30);
                Canvas.SetTop(categoryLabel, padding.Top + chartHeight + 10);
                ChartCanvas.Children.Add(categoryLabel);
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            UpdateChart();
            MessageBox.Show("Biểu đồ đã được cập nhật!", "Thành công", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnDisplayStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set flags for "Display Stats" mode
                IsConfirmed = true;
                DisplayStatsMode = true;

                // Close dialog
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Complete_Click(object sender, RoutedEventArgs e)
        {
            // BƯỚC 1: Kiểm tra dữ liệu hợp lệ
            if (chartData == null || chartData.Count == 0)
            {
                MessageBox.Show("❌ Vui lòng thêm dữ liệu cho biểu đồ!", "Lỗi dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chartData.Any(d => string.IsNullOrWhiteSpace(d.Label)))
            {
                MessageBox.Show("❌ Vui lòng điền nhãn cho tất cả các cột!", "Lỗi dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chartData.Any(d => d.Value <= 0))
            {
                MessageBox.Show("❌ Giá trị phải lớn hơn 0!", "Lỗi dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // BƯỚC 2: Lưu cấu hình hiện tại
            SaveCurrentConfiguration();

            // Cập nhật và ép vẽ lại layout trước khi render bitmap
            UpdateChart();
            ChartCanvas.UpdateLayout();

            // BƯỚC 3: Chuyển sang chế độ trình chiếu
            IsConfirmed = true;
            DialogResult = true;

            if (_mainDashboard != null)
            {
                AddInteractiveChartToCanvas();
            }
            else
            {
                MessageBox.Show("✅ Biểu đồ đã được tạo thành công!", "Hoàn thành",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            this.Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Đóng form mà không lưu
            IsConfirmed = false;
            this.Close();
        }

        /// <summary>
        /// BƯỚC 2: Lưu cấu hình hiện tại vào bộ nhớ
        /// </summary>
        private void SaveCurrentConfiguration()
        {
            CurrentConfiguration = new BarChartConfiguration
            {
                Title = txtChartTitle.Text,
                YAxisLabel = txtYAxisLabel.Text,
                ChartData = chartData.Select(d => new ChartDataEntry 
                { 
                    Label = d.Label, 
                    Value = d.Value 
                }).ToList(),
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                ChartSize = 100, // Default
                StartAngle = 0, // Default
                BarWidth = sliderBarWidth.Value,
                ShowGrid = chkShowGrid.IsChecked == true,
                ShowValues = chkShowValues.IsChecked == true,
                ShowLegend = false, // Not available in current form
                Animated = false, // Not available in current form
                Style3D = chkShow3D.IsChecked == true
            };

            // Lưu vào file JSON (để có thể mở lại lần sau)
            try
            {
                string configPath = IOPath.Combine(
                    QASmartClass.Services.AppPaths.RootDir, "ChartConfigs");
                
                IODirectory.CreateDirectory(configPath);
                
                string fileName = $"BarChart_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string fullPath = IOPath.Combine(configPath, fileName);
                
                string json = JsonSerializer.Serialize(CurrentConfiguration, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                IOFile.WriteAllText(fullPath, json);
            }
            catch (Exception ex)
            {
                // Không hiển thị lỗi, chỉ ghi log
                System.Diagnostics.Debug.WriteLine($"Không thể lưu cấu hình: {ex.Message}");
            }
        }

        /// <summary>
        /// BƯỚC 3: Thêm biểu đồ tương tác lên canvas với Unified Container chuẩn kép
        /// </summary>
        private void AddInteractiveChartToCanvas()
        {
            if (_mainDashboard == null) return;

            var renderBitmap = new RenderTargetBitmap(
                (int)ChartCanvas.ActualWidth,
                (int)ChartCanvas.ActualHeight,
                96, 96,
                PixelFormats.Pbgra32);

            renderBitmap.Render(ChartCanvas);

            InteractiveChartHelper.CreateAndAddChartToCanvas(new InteractiveChartHelper.ChartContainerOptions
            {
                MainDashboard = _mainDashboard,
                ChartTitle = string.IsNullOrWhiteSpace(txtChartTitle.Text) ? "Biểu đồ cột" : $"Biểu đồ cột: {txtChartTitle.Text}",
                Icon = "📊",
                Bitmap = renderBitmap,
                InitialWidth = ChartCanvas.ActualWidth * 0.8,
                InitialHeight = ChartCanvas.ActualHeight * 0.8,
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
                ChartTitle = string.IsNullOrWhiteSpace(txtChartTitle.Text) ? "Biểu đồ cột (Bản sao)" : $"Biểu đồ cột: {txtChartTitle.Text} (Bản sao)",
                Icon = "📊",
                Bitmap = bitmap,
                InitialWidth = originalContainer.Width,
                InitialHeight = (originalContainer.Children.OfType<Border>().FirstOrDefault(b => Grid.GetRow(b) == 1)?.Child as Image)?.Height ?? (ChartCanvas.ActualHeight * 0.8),
                TargetLeft = originalLeft + 30,
                TargetTop = originalTop + 30,
                Configuration = CurrentConfiguration,
                OnEdit = (container) => EditChart(container),
                OnCopy = (container, bmp) => CopyChart(container, bmp),
                OnDelete = (container) => DeleteChart(container)
            });
        }

        private void DeleteChart(Grid container)
        {
            var mainCanvas = _mainDashboard?.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas != null)
            {
                mainCanvas.Children.Remove(container);
                _mainDashboard?.RecordChartRemove(container, "Biểu đồ cột");
            }
        }

        private void EditChart(Grid container)
        {
            if (CurrentConfiguration == null)
            {
                MessageBox.Show("❌ Không tìm thấy cấu hình biểu đồ!", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            double originalLeft = Canvas.GetLeft(container);
            double originalTop = Canvas.GetTop(container);
            if (double.IsNaN(originalLeft)) originalLeft = 0;
            if (double.IsNaN(originalTop)) originalTop = 0;

            var editor = new Form2_8_BarChartEditor(_mainDashboard);
            editor.TargetLeft = originalLeft;
            editor.TargetTop = originalTop;
            editor.LoadConfiguration(CurrentConfiguration);
            bool? result = WindowHelper.ShowChildDialog(editor, _mainDashboard);
            if (result == true)
            {
                var mainCanvas = _mainDashboard?.FindName("MainInteractiveBoard") as Canvas;
                if (mainCanvas != null)
                {
                    mainCanvas.Children.Remove(container);
                    _mainDashboard?.RecordChartRemove(container, "Biểu đồ cột");
                }
            }
        }

        /// <summary>
        /// Load cấu hình đã lưu vào form
        /// </summary>
        public void LoadConfiguration(BarChartConfiguration config)
        {
            if (config == null) return;

            CurrentConfiguration = config;

            // Restore all settings
            txtChartTitle.Text = config.Title;
            txtYAxisLabel.Text = config.YAxisLabel;
            chartData = config.ChartData.Select(d => new ChartDataEntry 
            { 
                Label = d.Label, 
                Value = d.Value 
            }).ToList();
            
            primaryColor = config.PrimaryColor;
            secondaryColor = config.SecondaryColor;
            
            sliderBarWidth.Value = config.BarWidth;
            
            chkShowGrid.IsChecked = config.ShowGrid;
            chkShowValues.IsChecked = config.ShowValues;
            chkShow3D.IsChecked = config.Style3D;

            RefreshDataEntries();
            UpdateChart();
        }

        private void AddChartToCanvas()
        {
            AddInteractiveChartToCanvas();
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = "BieuDoHinhCot.png"
            };

            if (dialog.ShowDialog() == true)
            {
                var renderBitmap = new RenderTargetBitmap(
                    (int)ChartCanvas.ActualWidth,
                    (int)ChartCanvas.ActualHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

                renderBitmap.Render(ChartCanvas);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                using (var stream = IOFile.Create(dialog.FileName))
                {
                    encoder.Save(stream);
                }

                MessageBox.Show($"Đã lưu hình ảnh thành công!\n{dialog.FileName}", 
                    "Lưu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            var data = new
            {
                Title = txtChartTitle.Text,
                YAxisLabel = txtYAxisLabel.Text,
                Data = chartData.Select(d => new { d.Label, d.Value })
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            var dialog = new SaveFileDialog
            {
                Filter = "JSON File|*.json",
                FileName = "chart_data.json"
            };

            if (dialog.ShowDialog() == true)
            {
                IOFile.WriteAllText(dialog.FileName, json);
                MessageBox.Show($"Đã xuất dữ liệu thành công!\n{dialog.FileName}", 
                    "Xuất dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn reset về mặc định?\nMọi thay đổi sẽ bị mất!",
                "Xác nhận Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                txtChartTitle.Text = "Doanh thu bán hàng theo tháng";
                txtYAxisLabel.Text = "Doanh thu (triệu VNĐ)";
                cmbChartType.SelectedIndex = 0;
                cmbColorMode.SelectedIndex = 0;
                sliderBarWidth.Value = 70;
                sliderBorderRadius.Value = 5;
                chkShowValues.IsChecked = true;
                chkShowGrid.IsChecked = true;
                chkShow3D.IsChecked = false;
                chkShowShadow.IsChecked = true;

                primaryColor = Color.FromRgb(102, 126, 234);
                secondaryColor = Color.FromRgb(118, 75, 162);
                PrimaryColorPreview.Background = new SolidColorBrush(primaryColor);
                SecondaryColorPreview.Background = new SolidColorBrush(secondaryColor);

                InitializeDefaultData();
                UpdateChart();

                MessageBox.Show("Đã reset về mặc định!", "Thành công", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Enable resizing for chart with visual border
        /// </summary>
        private void EnableChartResizing(Grid container, System.Windows.Controls.Image chartImage, Border resizeBorder)
        {
            bool isResizing = false;
            Point resizeStartPoint = new Point();
            double originalWidth = 0;
            double originalHeight = 0;
            string resizeDirection = "";

            // Create resize handles at corners and edges
            var handles = new List<Border>();
            var positions = new[]
            {
                (HorizontalAlignment.Left, VerticalAlignment.Top, "NW"),
                (HorizontalAlignment.Right, VerticalAlignment.Top, "NE"),
                (HorizontalAlignment.Left, VerticalAlignment.Bottom, "SW"),
                (HorizontalAlignment.Right, VerticalAlignment.Bottom, "SE"),
                (HorizontalAlignment.Center, VerticalAlignment.Top, "N"),
                (HorizontalAlignment.Center, VerticalAlignment.Bottom, "S"),
                (HorizontalAlignment.Left, VerticalAlignment.Center, "W"),
                (HorizontalAlignment.Right, VerticalAlignment.Center, "E")
            };

            foreach (var (hAlign, vAlign, direction) in positions)
            {
                var handle = new Border
                {
                    Width = 10,
                    Height = 10,
                    Background = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                    HorizontalAlignment = hAlign,
                    VerticalAlignment = vAlign,
                    Cursor = GetCursorForDirection(direction),
                    Visibility = Visibility.Collapsed,
                    Margin = new Thickness(-5)
                };
                Grid.SetRow(handle, 1);

                handle.MouseEnter += (s, e) => handle.Background = Brushes.DodgerBlue;
                handle.MouseLeave += (s, e) => handle.Background = new SolidColorBrush(Color.FromRgb(102, 126, 234));

                handle.MouseLeftButtonDown += (s, e) =>
                {
                    isResizing = true;
                    resizeStartPoint = e.GetPosition(null);
                    originalWidth = container.Width;
                    originalHeight = container.Height;
                    resizeDirection = direction;
                    handle.CaptureMouse();
                    e.Handled = true;
                };

                container.Children.Add(handle);
                handles.Add(handle);
            }

            // Double-click to show/hide resize border and handles (not single click)
            container.MouseLeftButtonDown += (s, e) =>
            {
                if (!isResizing && e.ClickCount == 2) // Double-click only
                {
                    bool isVisible = resizeBorder.Visibility == Visibility.Visible;
                    resizeBorder.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
                    
                    foreach (var handle in handles)
                    {
                        handle.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
                    }
                    
                    e.Handled = true; // Only handle double-click, allow single-click for drag
                }
            };

            // Mouse move for resizing
            container.MouseMove += (s, e) =>
            {
                if (isResizing)
                {
                    Point currentPoint = e.GetPosition(null);
                    double deltaX = currentPoint.X - resizeStartPoint.X;
                    double deltaY = currentPoint.Y - resizeStartPoint.Y;

                    double newWidth = originalWidth;
                    double newHeight = originalHeight;

                    if (resizeDirection.Contains("E")) newWidth = originalWidth + deltaX;
                    if (resizeDirection.Contains("W")) newWidth = originalWidth - deltaX;
                    if (resizeDirection.Contains("S")) newHeight = originalHeight + deltaY;
                    if (resizeDirection.Contains("N")) newHeight = originalHeight - deltaY;

                    newWidth = Math.Max(200, newWidth);
                    newHeight = Math.Max(150, newHeight);

                    container.Width = newWidth;
                    container.Height = newHeight;
                    chartImage.Width = newWidth - 10;
                    chartImage.Height = newHeight - 50;

                    if (resizeDirection.Contains("W"))
                    {
                        double currentLeft = Canvas.GetLeft(container);
                        Canvas.SetLeft(container, currentLeft - (newWidth - originalWidth));
                    }
                    if (resizeDirection.Contains("N"))
                    {
                        double currentTop = Canvas.GetTop(container);
                        Canvas.SetTop(container, currentTop - (newHeight - originalHeight));
                    }
                }
            };

            container.MouseLeftButtonUp += (s, e) =>
            {
                if (isResizing)
                {
                    isResizing = false;
                    foreach (var handle in handles)
                    {
                        handle.ReleaseMouseCapture();
                    }
                }
            };
        }

        private System.Windows.Input.Cursor GetCursorForDirection(string direction)
        {
            return direction switch
            {
                "NW" or "SE" => System.Windows.Input.Cursors.SizeNWSE,
                "NE" or "SW" => System.Windows.Input.Cursors.SizeNESW,
                "N" or "S" => System.Windows.Input.Cursors.SizeNS,
                "W" or "E" => System.Windows.Input.Cursors.SizeWE,
                _ => System.Windows.Input.Cursors.Arrow
            };
        }
    }

    /// <summary>
    /// Class để lưu trữ cấu hình biểu đồ cột
    /// </summary>
    public class BarChartConfiguration
    {
        public string Title { get; set; } = "";
        public string YAxisLabel { get; set; } = "";
        public List<ChartDataEntry> ChartData { get; set; } = new List<ChartDataEntry>();
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
        public double ChartSize { get; set; }
        public double StartAngle { get; set; }
        public double BarWidth { get; set; }
        public bool ShowGrid { get; set; }
        public bool ShowValues { get; set; }
        public bool ShowLegend { get; set; }
        public bool Animated { get; set; }
        public bool Style3D { get; set; }
    }

    public class ChartDataEntry
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
    }
}
