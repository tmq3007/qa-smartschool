using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Animation;
using System.Windows.Documents;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using QASmartTouch.Helpers;
using QASmartTouch.Shared;

namespace QASmartTouch.Forms
{
    public partial class Form2_14_RadarChartEditor : Window
    {
        private List<RadarAxis> axes;
        private List<RadarDataset> datasets;
        private Form2_MainDashboard? _mainDashboard;

        public bool IsConfirmed { get; private set; }
        public bool DisplayStatsMode { get; private set; } = false;
        public RadarChartConfiguration CurrentConfiguration { get; private set; }
        public double? TargetLeft { get; set; }
        public double? TargetTop { get; set; }

        // Configuration Properties (public for MainDashboard access)
        public string ChartTitle { get; private set; } = "So sánh kỹ năng lập trình viên";
        public List<RadarAxis> Axes { get; private set; } = new List<RadarAxis>();
        public List<RadarDataset> Datasets { get; private set; } = new List<RadarDataset>();
        public bool ShowGrid { get; private set; } = true;
        public int GridLevels { get; private set; } = 5;
        public bool ShowAxes { get; private set; } = true;
        public bool ShowLabels { get; private set; } = true;
        public bool ShowValues { get; private set; } = false;
        public bool ShowLegend { get; private set; } = true;
        public bool ShowGridLabels { get; private set; } = true;
        public double StartAngle { get; private set; } = -90;
        public bool Clockwise { get; private set; } = true;
        public double LineWidth { get; private set; } = 2;
        public double FillOpacity { get; private set; } = 0.3;
        public double PointSize { get; private set; } = 4;

        public Form2_14_RadarChartEditor(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            TouchScrollHelper.AttachToAllScrollViewers(this);
            _mainDashboard = mainDashboard;
            axes = new List<RadarAxis>();
            datasets = new List<RadarDataset>();
            InitializeDefaultData();
            Loaded += (s, e) => UpdateChart();
        }

        private void InitializeDefaultData()
        {
            axes.Clear();
            datasets.Clear();

            // Default axes
            axes.Add(new RadarAxis { Name = "JavaScript", Max = 100 });
            axes.Add(new RadarAxis { Name = "Python", Max = 100 });
            axes.Add(new RadarAxis { Name = "Java", Max = 100 });
            axes.Add(new RadarAxis { Name = "C++", Max = 100 });
            axes.Add(new RadarAxis { Name = "SQL", Max = 100 });
            axes.Add(new RadarAxis { Name = "HTML/CSS", Max = 100 });

            // Default datasets
            datasets.Add(new RadarDataset
            {
                Label = "Nguyễn Văn A",
                Data = new List<double> { 85, 70, 60, 55, 80, 90 },
                Color = Color.FromRgb(74, 144, 226)
            });
            datasets.Add(new RadarDataset
            {
                Label = "Trần Thị B",
                Data = new List<double> { 75, 85, 70, 65, 75, 80 },
                Color = Color.FromRgb(233, 75, 60)
            });
            datasets.Add(new RadarDataset
            {
                Label = "Lê Văn C",
                Data = new List<double> { 90, 65, 80, 70, 85, 75 },
                Color = Color.FromRgb(80, 200, 120)
            });
        }

        private void LoadPreset(string presetName)
        {
            axes.Clear();
            datasets.Clear();

            switch (presetName)
            {
                case "skills":
                    TxtChartTitle.Text = "So sánh kỹ năng lập trình viên";
                    axes.Add(new RadarAxis { Name = "JavaScript", Max = 100 });
                    axes.Add(new RadarAxis { Name = "Python", Max = 100 });
                    axes.Add(new RadarAxis { Name = "Java", Max = 100 });
                    axes.Add(new RadarAxis { Name = "C++", Max = 100 });
                    axes.Add(new RadarAxis { Name = "SQL", Max = 100 });
                    axes.Add(new RadarAxis { Name = "HTML/CSS", Max = 100 });
                    datasets.Add(new RadarDataset { Label = "Nguyễn Văn A", Data = new List<double> { 85, 70, 60, 55, 80, 90 }, Color = Color.FromRgb(74, 144, 226) });
                    datasets.Add(new RadarDataset { Label = "Trần Thị B", Data = new List<double> { 75, 85, 70, 65, 75, 80 }, Color = Color.FromRgb(233, 75, 60) });
                    datasets.Add(new RadarDataset { Label = "Lê Văn C", Data = new List<double> { 90, 65, 80, 70, 85, 75 }, Color = Color.FromRgb(80, 200, 120) });
                    break;

                case "student":
                    TxtChartTitle.Text = "Kết quả học tập học sinh";
                    axes.Add(new RadarAxis { Name = "Toán", Max = 10 });
                    axes.Add(new RadarAxis { Name = "Văn", Max = 10 });
                    axes.Add(new RadarAxis { Name = "Anh", Max = 10 });
                    axes.Add(new RadarAxis { Name = "Lý", Max = 10 });
                    axes.Add(new RadarAxis { Name = "Hóa", Max = 10 });
                    datasets.Add(new RadarDataset { Label = "Học kỳ 1", Data = new List<double> { 8.5, 7.5, 8.0, 7.0, 6.5 }, Color = Color.FromRgb(52, 168, 83) });
                    datasets.Add(new RadarDataset { Label = "Học kỳ 2", Data = new List<double> { 9.0, 8.0, 8.5, 7.5, 7.0 }, Color = Color.FromRgb(251, 188, 5) });
                    break;

                case "product":
                    TxtChartTitle.Text = "So sánh sản phẩm điện thoại";
                    axes.Add(new RadarAxis { Name = "Hiệu năng", Max = 100 });
                    axes.Add(new RadarAxis { Name = "Pin", Max = 100 });
                    axes.Add(new RadarAxis { Name = "Camera", Max = 100 });
                    axes.Add(new RadarAxis { Name = "Màn hình", Max = 100 });
                    axes.Add(new RadarAxis { Name = "Giá cả", Max = 100 });
                    datasets.Add(new RadarDataset { Label = "iPhone 15", Data = new List<double> { 95, 75, 90, 88, 60 }, Color = Color.FromRgb(0, 122, 255) });
                    datasets.Add(new RadarDataset { Label = "Samsung S24", Data = new List<double> { 92, 80, 92, 90, 70 }, Color = Color.FromRgb(20, 115, 230) });
                    datasets.Add(new RadarDataset { Label = "Xiaomi 14", Data = new List<double> { 88, 85, 85, 85, 85 }, Color = Color.FromRgb(255, 103, 0) });
                    break;

                case "employee":
                    TxtChartTitle.Text = "Đánh giá năng lực nhân viên";
                    axes.Add(new RadarAxis { Name = "Chuyên môn", Max = 10 });
                    axes.Add(new RadarAxis { Name = "Giao tiếp", Max = 10 });
                    axes.Add(new RadarAxis { Name = "Kỷ luật", Max = 10 });
                    axes.Add(new RadarAxis { Name = "Sáng tạo", Max = 10 });
                    axes.Add(new RadarAxis { Name = "Làm việc nhóm", Max = 10 });
                    datasets.Add(new RadarDataset { Label = "Quý 1", Data = new List<double> { 8, 7, 9, 7, 8 }, Color = Color.FromRgb(66, 133, 244) });
                    datasets.Add(new RadarDataset { Label = "Quý 2", Data = new List<double> { 8.5, 7.5, 9, 8, 8.5 }, Color = Color.FromRgb(234, 67, 53) });
                    break;
            }

            UpdateChart();
        }

        private void UpdateChart()
        {
            ChartCanvas.Children.Clear();

            if (axes == null || axes.Count < 3 || datasets == null || datasets.Count == 0)
                return;

            // Update properties from UI
            ChartTitle = TxtChartTitle.Text;
            Axes = axes;
            Datasets = datasets;
            GridLevels = (int)SliderGridLevels.Value;
            ShowGrid = ChkShowGrid.IsChecked == true;
            ShowAxes = ChkShowAxes.IsChecked == true;
            ShowLabels = ChkShowLabels.IsChecked == true;
            ShowValues = ChkShowValues.IsChecked == true;
            ShowLegend = ChkShowLegend.IsChecked == true;
            ShowGridLabels = ChkShowGridLabels.IsChecked == true;
            StartAngle = SliderStartAngle.Value;
            Clockwise = ChkClockwise.IsChecked == true;
            LineWidth = SliderLineWidth.Value;
            FillOpacity = SliderFillOpacity.Value;
            PointSize = SliderPointSize.Value;

            // Draw chart
            DrawRadarChart();
        }

        private void DrawRadarChart()
        {
            double canvasWidth = ChartCanvas.Width;
            double canvasHeight = ChartCanvas.Height;
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;
            double padding = 140;  // Increased from 120 to prevent clipping
            double maxRadius = Math.Min(canvasWidth, canvasHeight) / 2 - padding;

            // Background
            var bgRect = new System.Windows.Shapes.Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = Brushes.White
            };
            Canvas.SetLeft(bgRect, 0);
            Canvas.SetTop(bgRect, 0);
            ChartCanvas.Children.Add(bgRect);

            // Title
            var titleText = new TextBlock
            {
                Text = ChartTitle,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            Canvas.SetLeft(titleText, (canvasWidth - titleText.Text.Length * 10) / 2);
            Canvas.SetTop(titleText, 20);
            ChartCanvas.Children.Add(titleText);

            int axisCount = axes.Count;
            double angleStep = (2 * Math.PI) / axisCount;
            double startAngleRad = (StartAngle * Math.PI) / 180;

            // Calculate angle for each axis
            List<double> angles = new List<double>();
            for (int i = 0; i < axisCount; i++)
            {
                double angle = Clockwise
                    ? startAngleRad + (i * angleStep)
                    : startAngleRad - (i * angleStep);
                angles.Add(angle);
            }

            // Draw grid levels
            if (ShowGrid)
            {
                for (int level = 1; level <= GridLevels; level++)
                {
                    double radius = (maxRadius / GridLevels) * level;
                    var polygon = new Polygon
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                        StrokeThickness = 1,
                        Fill = Brushes.Transparent
                    };

                    var points = new PointCollection();
                    for (int i = 0; i < axisCount; i++)
                    {
                        double x = centerX + radius * Math.Cos(angles[i]);
                        double y = centerY + radius * Math.Sin(angles[i]);
                        points.Add(new Point(x, y));
                    }
                    polygon.Points = points;
                    ChartCanvas.Children.Add(polygon);

                    // Grid labels (percentage)
                    if (ShowGridLabels && level > 0)
                    {
                        double percentage = (100.0 / GridLevels) * level;
                        var labelText = new TextBlock
                        {
                            Text = $"{percentage:F0}%",
                            FontSize = 9,
                            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150))
                        };
                        Canvas.SetLeft(labelText, centerX + 5);
                        Canvas.SetTop(labelText, centerY - radius - 5);
                        ChartCanvas.Children.Add(labelText);
                    }
                }
            }

            // Draw axes
            if (ShowAxes)
            {
                for (int i = 0; i < axisCount; i++)
                {
                    double x = centerX + maxRadius * Math.Cos(angles[i]);
                    double y = centerY + maxRadius * Math.Sin(angles[i]);

                    var axisLine = new Line
                    {
                        X1 = centerX,
                        Y1 = centerY,
                        X2 = x,
                        Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                        StrokeThickness = 1
                    };
                    ChartCanvas.Children.Add(axisLine);
                }
            }

            // Draw labels
            if (ShowLabels)
            {
                for (int i = 0; i < axisCount; i++)
                {
                    double labelRadius = maxRadius + 20;
                    double x = centerX + labelRadius * Math.Cos(angles[i]);
                    double y = centerY + labelRadius * Math.Sin(angles[i]);

                    var labelText = new TextBlock
                    {
                        Text = axes[i].Name,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60))
                    };

                    // Adjust label position based on angle
                    double angleDeg = angles[i] * 180 / Math.PI;
                    if (angleDeg > -90 && angleDeg < 90)
                    {
                        Canvas.SetLeft(labelText, x + 5);
                    }
                    else
                    {
                        Canvas.SetLeft(labelText, x - labelText.Text.Length * 6 - 5);
                    }
                    Canvas.SetTop(labelText, y - 8);

                    ChartCanvas.Children.Add(labelText);
                }
            }

            // Draw datasets
            foreach (var dataset in datasets)
            {
                if (dataset.Data.Count != axisCount)
                {
                    // Adjust data count to match axes
                    while (dataset.Data.Count < axisCount)
                        dataset.Data.Add(0);
                    while (dataset.Data.Count > axisCount)
                        dataset.Data.RemoveAt(dataset.Data.Count - 1);
                }

                var polygon = new Polygon
                {
                    Stroke = new SolidColorBrush(dataset.Color),
                    StrokeThickness = LineWidth,
                    Fill = new SolidColorBrush(dataset.Color) { Opacity = FillOpacity }
                };

                var points = new PointCollection();
                for (int i = 0; i < axisCount; i++)
                {
                    double value = dataset.Data[i];
                    double maxValue = axes[i].Max;
                    double normalized = Math.Max(0, Math.Min(1, value / maxValue));
                    double radius = maxRadius * normalized;

                    double x = centerX + radius * Math.Cos(angles[i]);
                    double y = centerY + radius * Math.Sin(angles[i]);
                    points.Add(new Point(x, y));
                }
                polygon.Points = points;
                ChartCanvas.Children.Add(polygon);

                // Draw points
                for (int i = 0; i < axisCount; i++)
                {
                    double value = dataset.Data[i];
                    double maxValue = axes[i].Max;
                    double normalized = Math.Max(0, Math.Min(1, value / maxValue));
                    double radius = maxRadius * normalized;

                    double x = centerX + radius * Math.Cos(angles[i]);
                    double y = centerY + radius * Math.Sin(angles[i]);

                    var point = new Ellipse
                    {
                        Width = PointSize * 2,
                        Height = PointSize * 2,
                        Fill = new SolidColorBrush(dataset.Color),
                        Stroke = Brushes.White,
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(point, x - PointSize);
                    Canvas.SetTop(point, y - PointSize);
                    ChartCanvas.Children.Add(point);

                    // Draw values
                    if (ShowValues)
                    {
                        var valueText = new TextBlock
                        {
                            Text = value.ToString("F1"),
                            FontSize = 9,
                            Foreground = new SolidColorBrush(dataset.Color),
                            FontWeight = FontWeights.Bold
                        };
                        Canvas.SetLeft(valueText, x + PointSize + 3);
                        Canvas.SetTop(valueText, y - PointSize - 3);
                        ChartCanvas.Children.Add(valueText);
                    }
                }
            }

            // Draw legend
            if (ShowLegend)
            {
                // Calculate total legend height
                int totalLegendItems = datasets.Count;
                double legendTitleHeight = 20;
                double legendItemsHeight = totalLegendItems * 25;
                double totalLegendHeight = legendTitleHeight + legendItemsHeight;
                
                // Position legend at vertical center of canvas, 20px from left (0.5cm)
                double legendX = 20;
                double legendY = (canvasHeight - totalLegendHeight) / 2;
                
                // Legend title "Chú thích:"
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

                // Legend items start below the title
                double itemsStartY = legendY + 25;

                for (int i = 0; i < datasets.Count; i++)
                {
                    var dataset = datasets[i];

                    var colorBox = new System.Windows.Shapes.Rectangle
                    {
                        Width = 20,
                        Height = 15,
                        Fill = new SolidColorBrush(dataset.Color),
                        Stroke = new SolidColorBrush(dataset.Color),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(colorBox, legendX);
                    Canvas.SetTop(colorBox, itemsStartY + i * 25);
                    ChartCanvas.Children.Add(colorBox);

                    var legendText = new TextBlock
                    {
                        Text = dataset.Label,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60))
                    };
                    Canvas.SetLeft(legendText, legendX + 30);
                    Canvas.SetTop(legendText, itemsStartY + i * 25);
                    ChartCanvas.Children.Add(legendText);
                }
            }
        }

        private void BtnConfigAxes_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RadarAxesConfigDialog(axes);
            if (dialog.ShowDialog() == true)
            {
                axes = dialog.ConfiguredAxes;
                UpdateChart();
            }
        }

        private void BtnConfigDatasets_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RadarDatasetsConfigDialog(axes, datasets);
            if (dialog.ShowDialog() == true)
            {
                datasets = dialog.ConfiguredDatasets;
                UpdateChart();
            }
        }

        private void BtnPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string presetName)
            {
                LoadPreset(presetName);
            }
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender == SliderGridLevels && TxtGridLevels != null)
            {
                TxtGridLevels.Text = $"{SliderGridLevels.Value:F0}";
            }
            else if (sender == SliderStartAngle && TxtStartAngle != null)
            {
                TxtStartAngle.Text = $"{SliderStartAngle.Value:F0}°";
            }
            else if (sender == SliderLineWidth && TxtLineWidth != null)
            {
                TxtLineWidth.Text = $"{SliderLineWidth.Value:F1} px";
            }
            else if (sender == SliderFillOpacity && TxtFillOpacity != null)
            {
                TxtFillOpacity.Text = $"{SliderFillOpacity.Value:F1}";
            }
            else if (sender == SliderPointSize && TxtPointSize != null)
            {
                TxtPointSize.Text = $"{SliderPointSize.Value:F0} px";
            }
            UpdateChart();
        }

        private void OnCheckChanged(object sender, RoutedEventArgs e)
        {
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
                if (axes == null || axes.Count == 0)
                {
                    MessageBox.Show("❌ Chưa có trục dữ liệu!", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (datasets == null || datasets.Count == 0)
                {
                    MessageBox.Show("❌ Chưa có dataset!", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var axis in axes)
                {
                    if (string.IsNullOrWhiteSpace(axis.Name))
                    {
                        MessageBox.Show("❌ Vui lòng nhập tên cho tất cả các trục!", "Lỗi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // STEP 2: Save configuration
                SaveCurrentConfiguration();

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

        private void BtnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = "RadarChart.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var rtb = new RenderTargetBitmap((int)ChartCanvas.ActualWidth, (int)ChartCanvas.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(ChartCanvas);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                    using (var stream = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show("Đã lưu biểu đồ thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu hình ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TxtChartTitle.Text = "So sánh kỹ năng lập trình viên";
            SliderGridLevels.Value = 5;
            SliderStartAngle.Value = -90;
            SliderLineWidth.Value = 2;
            SliderFillOpacity.Value = 0.3;
            SliderPointSize.Value = 4;
            ChkShowGrid.IsChecked = true;
            ChkShowAxes.IsChecked = true;
            ChkShowLabels.IsChecked = true;
            ChkShowValues.IsChecked = false;
            ChkShowLegend.IsChecked = true;
            ChkShowGridLabels.IsChecked = true;
            ChkClockwise.IsChecked = true;

            InitializeDefaultData();
            UpdateChart();
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
            {
                UpdateChart();
            }
        }

        private void SaveCurrentConfiguration()
        {
            try
            {
                CurrentConfiguration = new RadarChartConfiguration
                {
                    ChartTitle = TxtChartTitle.Text,
                    Axes = axes.Select(a => new RadarAxisConfig
                    {
                        Label = a.Name,
                        MaxValue = a.Max
                    }).ToList(),
                    Datasets = datasets.Select(d => new RadarDatasetConfig
                    {
                        Label = d.Label,
                        Data = new List<double>(d.Data),
                        Color = d.Color,
                        Opacity = 0.3
                    }).ToList(),
                    ShowGrid = ChkShowGrid.IsChecked ?? true,
                    ShowAxes = ChkShowAxes.IsChecked ?? true,
                    ShowLabels = ChkShowLabels.IsChecked ?? true,
                    GridLevels = (int)SliderGridLevels.Value
                };

                string appDataPath = QASmartClass.Services.AppPaths.RootDir;
                string configDir = IOPath.Combine(appDataPath, "ChartConfigs");
                
                if (!IODirectory.Exists(configDir))
                {
                    IODirectory.CreateDirectory(configDir);
                }

                string configFile = IOPath.Combine(configDir, $"RadarChart_{DateTime.Now:yyyyMMdd_HHmmss}.json");
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
                ChartTitle = string.IsNullOrWhiteSpace(TxtChartTitle.Text) ? "Biểu đồ Radar" : $"Biểu đồ Radar: {TxtChartTitle.Text}",
                Icon = "🕸️",
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
                ChartTitle = string.IsNullOrWhiteSpace(TxtChartTitle.Text) ? "Biểu đồ Radar (Bản sao)" : $"Biểu đồ Radar: {TxtChartTitle.Text} (Bản sao)",
                Icon = "🕸️",
                Bitmap = bitmap,
                InitialWidth = originalContainer.Width,
                InitialHeight = originalContainer.Height - InteractiveChartHelper.HeaderBarHeight,
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
                _mainDashboard?.RecordChartRemove(outerContainer, "Biểu đồ Radar");
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

            var editor = new Form2_14_RadarChartEditor(_mainDashboard);
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
                    _mainDashboard?.RecordChartRemove(outerContainer, "Biểu đồ Radar");
                }
            }
        }

        public void LoadConfiguration(RadarChartConfiguration config)
        {
            if (config == null) return;

            CurrentConfiguration = config;

            TxtChartTitle.Text = config.ChartTitle;
            
            axes.Clear();
            foreach (var axisConfig in config.Axes)
            {
                axes.Add(new RadarAxis
                {
                    Name = axisConfig.Label,
                    Max = axisConfig.MaxValue
                });
            }

            datasets.Clear();
            foreach (var datasetConfig in config.Datasets)
            {
                datasets.Add(new RadarDataset
                {
                    Label = datasetConfig.Label,
                    Data = new List<double>(datasetConfig.Data),
                    Color = datasetConfig.Color
                });
            }

            ChkShowGrid.IsChecked = config.ShowGrid;
            ChkShowAxes.IsChecked = config.ShowAxes;
            ChkShowLabels.IsChecked = config.ShowLabels;
            SliderGridLevels.Value = config.GridLevels;

            UpdateChart();
        }



        private void btnDisplayStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsConfirmed = true;
                DisplayStatsMode = true;
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class RadarAxis
    {
        public string Name { get; set; } = "";
        public double Max { get; set; } = 100;
    }

    public class RadarDataset
    {
        public string Label { get; set; } = "";
        public List<double> Data { get; set; } = new List<double>();
        public Color Color { get; set; } = Colors.Blue;
    }

    // Simple config dialogs
    public class RadarAxesConfigDialog : Window
    {
        public List<RadarAxis> ConfiguredAxes { get; private set; }

        public RadarAxesConfigDialog(List<RadarAxis> currentAxes)
        {
            Title = "Cấu hình trục";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            ConfiguredAxes = new List<RadarAxis>(currentAxes.Select(a => new RadarAxis { Name = a.Name, Max = a.Max }));

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            var scrollViewer = new ScrollViewer { Content = stackPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

            foreach (var axis in ConfiguredAxes)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
                panel.Children.Add(new TextBlock { Text = "Tên trục:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
                var nameBox = new TextBox { Width = 150, Text = axis.Name, Margin = new Thickness(5, 0, 5, 0) };
                nameBox.TextChanged += (s, e) => axis.Name = nameBox.Text;
                panel.Children.Add(nameBox);

                panel.Children.Add(new TextBlock { Text = "Max:", Width = 40, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
                var maxBox = new TextBox { Width = 80, Text = axis.Max.ToString(), Margin = new Thickness(5, 0, 5, 0) };
                maxBox.TextChanged += (s, e) => { if (double.TryParse(maxBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val)) axis.Max = val; };
                panel.Children.Add(maxBox);

                stackPanel.Children.Add(panel);
            }

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) };
            var btnOK = new Button { Content = "OK", Width = 100, Padding = new Thickness(10), Margin = new Thickness(5) };
            btnOK.Click += (s, e) => { DialogResult = true; Close(); };
            btnPanel.Children.Add(btnOK);

            var mainPanel = new DockPanel();
            DockPanel.SetDock(btnPanel, Dock.Bottom);
            mainPanel.Children.Add(btnPanel);
            mainPanel.Children.Add(scrollViewer);

            Content = mainPanel;
        }
    }

    public class RadarDatasetsConfigDialog : Window
    {
        public List<RadarDataset> ConfiguredDatasets { get; private set; }

        public RadarDatasetsConfigDialog(List<RadarAxis> axes, List<RadarDataset> currentDatasets)
        {
            Title = "Cấu hình dữ liệu";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            ConfiguredDatasets = new List<RadarDataset>();
            foreach (var ds in currentDatasets)
            {
                ConfiguredDatasets.Add(new RadarDataset
                {
                    Label = ds.Label,
                    Data = new List<double>(ds.Data),
                    Color = ds.Color
                });
            }

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            var scrollViewer = new ScrollViewer { Content = stackPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

            foreach (var dataset in ConfiguredDatasets)
            {
                var groupBox = new GroupBox { Header = dataset.Label, Margin = new Thickness(0, 10, 0, 10), Padding = new Thickness(10) };
                var innerPanel = new StackPanel();

                var namePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
                namePanel.Children.Add(new TextBlock { Text = "Tên:", Width = 60 });
                var nameBox = new TextBox { Width = 200, Text = dataset.Label };
                nameBox.TextChanged += (s, e) => dataset.Label = nameBox.Text;
                namePanel.Children.Add(nameBox);
                innerPanel.Children.Add(namePanel);

                for (int i = 0; i < Math.Min(axes.Count, dataset.Data.Count); i++)
                {
                    var idx = i;
                    var dataPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
                    dataPanel.Children.Add(new TextBlock { Text = axes[i].Name + ":", Width = 100 });
                    var dataBox = new TextBox { Width = 80, Text = dataset.Data[i].ToString() };
                    dataBox.TextChanged += (s, e) => { if (double.TryParse(dataBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val)) dataset.Data[idx] = val; };
                    dataPanel.Children.Add(dataBox);
                    innerPanel.Children.Add(dataPanel);
                }

                groupBox.Content = innerPanel;
                stackPanel.Children.Add(groupBox);
            }

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) };
            var btnOK = new Button { Content = "OK", Width = 100, Padding = new Thickness(10), Margin = new Thickness(5) };
            btnOK.Click += (s, e) => { DialogResult = true; Close(); };
            btnPanel.Children.Add(btnOK);

            var mainPanel = new DockPanel();
            DockPanel.SetDock(btnPanel, Dock.Bottom);
            mainPanel.Children.Add(btnPanel);
            mainPanel.Children.Add(scrollViewer);

            Content = mainPanel;
        }
    }

    // Radar Chart Configuration
    public class RadarChartConfiguration
    {
        public string ChartTitle { get; set; }
        public List<RadarAxisConfig> Axes { get; set; }
        public List<RadarDatasetConfig> Datasets { get; set; }
        public bool ShowGrid { get; set; }
        public bool ShowAxes { get; set; }
        public bool ShowLabels { get; set; }
        public int GridLevels { get; set; }
    }

    public class RadarAxisConfig
    {
        public string Label { get; set; }
        public double MaxValue { get; set; }
    }

    public class RadarDatasetConfig
    {
        public string Label { get; set; }
        public List<double> Data { get; set; }
        public Color Color { get; set; }
        public double Opacity { get; set; }
    }
}

