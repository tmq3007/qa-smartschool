using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.IO;

namespace QASmartTouch.Forms
{
    public partial class Form2_13_ScatterChartEditor : Window
    {
        private List<ScatterDataPoint> dataPoints;
        private Dictionary<string, CategoryInfo> categories;
        private Form2_MainDashboard? _mainDashboard;

        public bool IsConfirmed { get; private set; }

        // Configuration Properties (public for MainDashboard access)
        public string ChartTitle { get; private set; } = "Mối quan hệ giữa Chiều cao và Cân nặng";
        public string XAxisLabel { get; private set; } = "Chiều cao (cm)";
        public string YAxisLabel { get; private set; } = "Cân nặng (kg)";
        public List<ScatterDataPoint> DataPoints { get; private set; } = new List<ScatterDataPoint>();
        public Dictionary<string, CategoryInfo> Categories { get; private set; } = new Dictionary<string, CategoryInfo>();
        public string DefaultPointShape { get; private set; } = "circle";
        public double DefaultPointSize { get; private set; } = 5;
        public double PointOpacity { get; private set; } = 0.7;
        public bool ShowGrid { get; private set; } = true;
        public bool ShowAxes { get; private set; } = true;
        public bool ShowLabels { get; private set; } = false;
        public bool ShowTrendLine { get; private set; } = true;
        public bool ShowCorrelation { get; private set; } = true;
        public bool ShowLegend { get; private set; } = true;

        public Form2_13_ScatterChartEditor(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            _mainDashboard = mainDashboard;
            dataPoints = new List<ScatterDataPoint>();
            categories = new Dictionary<string, CategoryInfo>();
            InitializeDefaultData();
            Loaded += (s, e) => UpdateChart();
        }

        private void InitializeDefaultData()
        {
            dataPoints.Clear();
            categories.Clear();

            // Định nghĩa categories
            categories["Nam"] = new CategoryInfo 
            { 
                Color = Color.FromRgb(74, 144, 226), 
                Shape = "circle" 
            };
            categories["Nữ"] = new CategoryInfo 
            { 
                Color = Color.FromRgb(233, 75, 60), 
                Shape = "triangle" 
            };

            // Dữ liệu mẫu: Chiều cao - Cân nặng
            var sampleData = new[]
            {
                (160, 55, "A", "Nữ", 5), (165, 60, "B", "Nữ", 6), (170, 65, "C", "Nam", 7),
                (155, 50, "D", "Nữ", 5), (175, 70, "E", "Nam", 8), (162, 58, "F", "Nữ", 6),
                (168, 63, "G", "Nam", 6), (172, 68, "H", "Nam", 7), (158, 53, "I", "Nữ", 5),
                (178, 75, "J", "Nam", 8), (163, 59, "K", "Nữ", 6), (169, 64, "L", "Nam", 7),
                (156, 52, "M", "Nữ", 5), (174, 69, "N", "Nam", 7), (161, 57, "O", "Nữ", 6),
                (171, 66, "P", "Nam", 7), (159, 54, "Q", "Nữ", 5), (176, 72, "R", "Nam", 8),
                (164, 61, "S", "Nữ", 6), (173, 67, "T", "Nam", 7)
            };

            foreach (var (x, y, label, category, size) in sampleData)
            {
                dataPoints.Add(new ScatterDataPoint
                {
                    X = x,
                    Y = y,
                    Label = label,
                    Category = category,
                    Size = size
                });
            }

            RefreshStats();
        }

        private void LoadPreset(string presetName)
        {
            dataPoints.Clear();
            categories.Clear();

            switch (presetName)
            {
                case "height-weight":
                    TxtChartTitle.Text = "Mối quan hệ giữa Chiều cao và Cân nặng";
                    TxtXAxisLabel.Text = "Chiều cao (cm)";
                    TxtYAxisLabel.Text = "Cân nặng (kg)";
                    categories["Nam"] = new CategoryInfo { Color = Color.FromRgb(74, 144, 226), Shape = "circle" };
                    categories["Nữ"] = new CategoryInfo { Color = Color.FromRgb(233, 75, 60), Shape = "triangle" };
                    var heightWeightData = new[]
                    {
                        (160, 55, "A", "Nữ", 5), (165, 60, "B", "Nữ", 6), (170, 65, "C", "Nam", 7),
                        (155, 50, "D", "Nữ", 5), (175, 70, "E", "Nam", 8), (162, 58, "F", "Nữ", 6),
                        (168, 63, "G", "Nam", 6), (172, 68, "H", "Nam", 7), (158, 53, "I", "Nữ", 5),
                        (178, 75, "J", "Nam", 8), (163, 59, "K", "Nữ", 6), (169, 64, "L", "Nam", 7)
                    };
                    foreach (var (x, y, label, category, size) in heightWeightData)
                    {
                        dataPoints.Add(new ScatterDataPoint { X = x, Y = y, Label = label, Category = category, Size = size });
                    }
                    break;

                case "study-score":
                    TxtChartTitle.Text = "Mối quan hệ giữa Giờ học và Điểm số";
                    TxtXAxisLabel.Text = "Giờ học mỗi tuần";
                    TxtYAxisLabel.Text = "Điểm trung bình";
                    categories["Nhóm A"] = new CategoryInfo { Color = Color.FromRgb(52, 168, 83), Shape = "circle" };
                    categories["Nhóm B"] = new CategoryInfo { Color = Color.FromRgb(251, 188, 5), Shape = "square" };
                    var studyScoreData = new[]
                    {
                        (5, 55, "SV1", "Nhóm B", 5), (10, 65, "SV2", "Nhóm B", 6), (15, 75, "SV3", "Nhóm A", 7),
                        (8, 60, "SV4", "Nhóm B", 5), (20, 85, "SV5", "Nhóm A", 8), (12, 70, "SV6", "Nhóm A", 6),
                        (18, 80, "SV7", "Nhóm A", 7), (7, 58, "SV8", "Nhóm B", 5), (25, 90, "SV9", "Nhóm A", 8),
                        (14, 72, "SV10", "Nhóm A", 7), (9, 62, "SV11", "Nhóm B", 6), (22, 88, "SV12", "Nhóm A", 8)
                    };
                    foreach (var (x, y, label, category, size) in studyScoreData)
                    {
                        dataPoints.Add(new ScatterDataPoint { X = x, Y = y, Label = label, Category = category, Size = size });
                    }
                    break;

                case "age-income":
                    TxtChartTitle.Text = "Mối quan hệ giữa Tuổi và Thu nhập";
                    TxtXAxisLabel.Text = "Tuổi";
                    TxtYAxisLabel.Text = "Thu nhập (triệu VNĐ)";
                    categories["Nam"] = new CategoryInfo { Color = Color.FromRgb(66, 133, 244), Shape = "diamond" };
                    categories["Nữ"] = new CategoryInfo { Color = Color.FromRgb(234, 67, 53), Shape = "circle" };
                    var ageIncomeData = new[]
                    {
                        (25, 12, "P1", "Nam", 5), (30, 18, "P2", "Nam", 6), (35, 25, "P3", "Nam", 7),
                        (28, 15, "P4", "Nữ", 5), (40, 32, "P5", "Nam", 8), (32, 20, "P6", "Nữ", 6),
                        (38, 28, "P7", "Nam", 7), (27, 14, "P8", "Nữ", 5), (45, 38, "P9", "Nam", 8),
                        (33, 22, "P10", "Nữ", 7), (29, 16, "P11", "Nữ", 6), (42, 35, "P12", "Nam", 8)
                    };
                    foreach (var (x, y, label, category, size) in ageIncomeData)
                    {
                        dataPoints.Add(new ScatterDataPoint { X = x, Y = y, Label = label, Category = category, Size = size });
                    }
                    break;

                case "temperature-sales":
                    TxtChartTitle.Text = "Mối quan hệ giữa Nhiệt độ và Doanh số";
                    TxtXAxisLabel.Text = "Nhiệt độ (°C)";
                    TxtYAxisLabel.Text = "Doanh số (triệu)";
                    categories["Đồ uống"] = new CategoryInfo { Color = Color.FromRgb(255, 152, 0), Shape = "star" };
                    var temperatureSalesData = new[]
                    {
                        (15, 25, "T1", "Đồ uống", 5), (20, 35, "T2", "Đồ uống", 6), (25, 45, "T3", "Đồ uống", 7),
                        (18, 30, "T4", "Đồ uống", 5), (30, 60, "T5", "Đồ uống", 8), (22, 40, "T6", "Đồ uống", 6),
                        (28, 55, "T7", "Đồ uống", 7), (17, 28, "T8", "Đồ uống", 5), (32, 65, "T9", "Đồ uống", 8),
                        (24, 42, "T10", "Đồ uống", 7), (19, 32, "T11", "Đồ uống", 6), (35, 70, "T12", "Đồ uống", 8)
                    };
                    foreach (var (x, y, label, category, size) in temperatureSalesData)
                    {
                        dataPoints.Add(new ScatterDataPoint { X = x, Y = y, Label = label, Category = category, Size = size });
                    }
                    break;
            }

            RefreshStats();
            UpdateChart();
        }

        private void RefreshStats()
        {
            TxtStatPoints.Text = dataPoints.Count.ToString();

            if (dataPoints.Count < 2)
            {
                TxtStatCorrelation.Text = "N/A";
                TxtStatEquation.Text = "-";
                return;
            }

            // Tính hệ số tương quan
            var (correlation, slope, intercept) = CalculateCorrelation(dataPoints);
            TxtStatCorrelation.Text = correlation.ToString("F3");
            TxtStatEquation.Text = $"y = {slope:F2}x + {intercept:F2}";
        }

        private (double correlation, double slope, double intercept) CalculateCorrelation(List<ScatterDataPoint> points)
        {
            if (points.Count < 2) return (0, 0, 0);

            int n = points.Count;
            double sumX = points.Sum(p => p.X);
            double sumY = points.Sum(p => p.Y);
            double sumXY = points.Sum(p => p.X * p.Y);
            double sumX2 = points.Sum(p => p.X * p.X);
            double sumY2 = points.Sum(p => p.Y * p.Y);

            double numerator = (n * sumXY) - (sumX * sumY);
            double denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

            double correlation = denominator == 0 ? 0 : numerator / denominator;

            // Linear regression: y = slope * x + intercept
            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            return (correlation, slope, intercept);
        }

        private void UpdateChart()
        {
            ChartCanvas.Children.Clear();

            if (dataPoints == null || dataPoints.Count == 0)
                return;

            // Update properties from UI
            ChartTitle = TxtChartTitle.Text;
            XAxisLabel = TxtXAxisLabel.Text;
            YAxisLabel = TxtYAxisLabel.Text;
            DataPoints = dataPoints;
            Categories = categories;

            var shapeIndex = CmbPointShape.SelectedIndex;
            DefaultPointShape = shapeIndex switch 
            { 
                0 => "circle", 
                1 => "square", 
                2 => "triangle", 
                3 => "diamond", 
                4 => "cross", 
                5 => "star", 
                _ => "circle" 
            };

            DefaultPointSize = SliderPointSize.Value;
            PointOpacity = SliderPointOpacity.Value;
            ShowGrid = ChkShowGrid.IsChecked == true;
            ShowAxes = ChkShowAxes.IsChecked == true;
            ShowLabels = ChkShowLabels.IsChecked == true;
            ShowTrendLine = ChkShowTrendLine.IsChecked == true;
            ShowCorrelation = ChkShowCorrelation.IsChecked == true;
            ShowLegend = ChkShowLegend.IsChecked == true;

            // Draw chart
            DrawScatterChart();
        }

        private void DrawScatterChart()
        {
            double canvasWidth = ChartCanvas.Width;
            double canvasHeight = ChartCanvas.Height;
            double padding = 80;
            double chartWidth = canvasWidth - 2 * padding;
            double chartHeight = canvasHeight - 2 * padding;

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
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            Canvas.SetLeft(titleText, (canvasWidth - titleText.Text.Length * 9) / 2);
            Canvas.SetTop(titleText, 20);
            ChartCanvas.Children.Add(titleText);

            // Find min/max for scaling
            double minX = dataPoints.Min(p => p.X);
            double maxX = dataPoints.Max(p => p.X);
            double minY = dataPoints.Min(p => p.Y);
            double maxY = dataPoints.Max(p => p.Y);

            double rangeX = maxX - minX;
            double rangeY = maxY - minY;

            // Add 10% padding to ranges
            minX -= rangeX * 0.1;
            maxX += rangeX * 0.1;
            minY -= rangeY * 0.1;
            maxY += rangeY * 0.1;
            rangeX = maxX - minX;
            rangeY = maxY - minY;

            // Draw grid
            if (ShowGrid)
            {
                for (int i = 0; i <= 5; i++)
                {
                    double y = padding + (chartHeight / 5) * i;
                    var gridLine = new Line
                    {
                        X1 = padding,
                        Y1 = y,
                        X2 = padding + chartWidth,
                        Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                        StrokeThickness = 1
                    };
                    ChartCanvas.Children.Add(gridLine);

                    double value = maxY - (rangeY / 5) * i;
                    var valueText = new TextBlock
                    {
                        Text = value.ToString("F1"),
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                    };
                    Canvas.SetLeft(valueText, padding - 40);
                    Canvas.SetTop(valueText, y - 8);
                    ChartCanvas.Children.Add(valueText);
                }

                for (int i = 0; i <= 5; i++)
                {
                    double x = padding + (chartWidth / 5) * i;
                    var gridLine = new Line
                    {
                        X1 = x,
                        Y1 = padding,
                        X2 = x,
                        Y2 = padding + chartHeight,
                        Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                        StrokeThickness = 1
                    };
                    ChartCanvas.Children.Add(gridLine);

                    double value = minX + (rangeX / 5) * i;
                    var valueText = new TextBlock
                    {
                        Text = value.ToString("F1"),
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                    };
                    Canvas.SetLeft(valueText, x - 15);
                    Canvas.SetTop(valueText, padding + chartHeight + 10);
                    ChartCanvas.Children.Add(valueText);
                }
            }

            // Draw axes
            if (ShowAxes)
            {
                var xAxis = new Line
                {
                    X1 = padding,
                    Y1 = padding + chartHeight,
                    X2 = padding + chartWidth,
                    Y2 = padding + chartHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                ChartCanvas.Children.Add(xAxis);

                var yAxis = new Line
                {
                    X1 = padding,
                    Y1 = padding,
                    X2 = padding,
                    Y2 = padding + chartHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                ChartCanvas.Children.Add(yAxis);

                // Axis labels
                var xLabel = new TextBlock
                {
                    Text = XAxisLabel,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60))
                };
                Canvas.SetLeft(xLabel, padding + chartWidth / 2 - xLabel.Text.Length * 3);
                Canvas.SetTop(xLabel, padding + chartHeight + 35);
                ChartCanvas.Children.Add(xLabel);

                var yLabel = new TextBlock
                {
                    Text = YAxisLabel,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    RenderTransform = new RotateTransform(-90)
                };
                Canvas.SetLeft(yLabel, padding - 60);
                Canvas.SetTop(yLabel, padding + chartHeight / 2);
                ChartCanvas.Children.Add(yLabel);
            }

            // Draw trend line
            if (ShowTrendLine && dataPoints.Count >= 2)
            {
                var (correlation, slope, intercept) = CalculateCorrelation(dataPoints);

                double x1Canvas = padding;
                double x2Canvas = padding + chartWidth;
                double x1Value = minX;
                double x2Value = maxX;
                double y1Value = slope * x1Value + intercept;
                double y2Value = slope * x2Value + intercept;

                double y1Canvas = padding + chartHeight - ((y1Value - minY) / rangeY) * chartHeight;
                double y2Canvas = padding + chartHeight - ((y2Value - minY) / rangeY) * chartHeight;

                var trendLine = new Line
                {
                    X1 = x1Canvas,
                    Y1 = y1Canvas,
                    X2 = x2Canvas,
                    Y2 = y2Canvas,
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 3 },
                    Opacity = 0.7
                };
                ChartCanvas.Children.Add(trendLine);
            }

            // Draw data points
            foreach (var point in dataPoints)
            {
                double x = padding + ((point.X - minX) / rangeX) * chartWidth;
                double y = padding + chartHeight - ((point.Y - minY) / rangeY) * chartHeight;

                var categoryInfo = categories.ContainsKey(point.Category) 
                    ? categories[point.Category] 
                    : new CategoryInfo { Color = Color.FromRgb(102, 126, 234), Shape = DefaultPointShape };

                var pointColor = categoryInfo.Color;
                var pointShape = categoryInfo.Shape;
                var pointSize = point.Size > 0 ? point.Size : DefaultPointSize;

                DrawPoint(x, y, pointSize, pointColor, pointShape, PointOpacity);

                // Draw label
                if (ShowLabels && !string.IsNullOrEmpty(point.Label))
                {
                    var labelText = new TextBlock
                    {
                        Text = point.Label,
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60))
                    };
                    Canvas.SetLeft(labelText, x + pointSize + 3);
                    Canvas.SetTop(labelText, y - pointSize - 3);
                    ChartCanvas.Children.Add(labelText);
                }
            }

            // Draw correlation info
            if (ShowCorrelation && dataPoints.Count >= 2)
            {
                var (correlation, _, _) = CalculateCorrelation(dataPoints);
                var corrText = new TextBlock
                {
                    Text = $"r = {correlation:F3}",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(102, 126, 234))
                };
                Canvas.SetLeft(corrText, padding + chartWidth - 80);
                Canvas.SetTop(corrText, padding + 10);
                ChartCanvas.Children.Add(corrText);
            }

            // Draw legend
            if (ShowLegend && categories.Count > 0)
            {
                double legendX = padding + chartWidth - 150;
                double legendY = padding + 40;
                int index = 0;

                foreach (var kvp in categories)
                {
                    var categoryName = kvp.Key;
                    var categoryInfo = kvp.Value;

                    DrawPoint(legendX, legendY + index * 25, 5, categoryInfo.Color, categoryInfo.Shape, PointOpacity);

                    var legendText = new TextBlock
                    {
                        Text = categoryName,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60))
                    };
                    Canvas.SetLeft(legendText, legendX + 15);
                    Canvas.SetTop(legendText, legendY + index * 25 - 6);
                    ChartCanvas.Children.Add(legendText);

                    index++;
                }
            }
        }

        private void DrawPoint(double x, double y, double size, Color color, string shape, double opacity)
        {
            var brush = new SolidColorBrush(color) { Opacity = opacity };

            switch (shape)
            {
                case "circle":
                    var circle = new Ellipse
                    {
                        Width = size * 2,
                        Height = size * 2,
                        Fill = brush,
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(circle, x - size);
                    Canvas.SetTop(circle, y - size);
                    ChartCanvas.Children.Add(circle);
                    break;

                case "square":
                    var square = new System.Windows.Shapes.Rectangle
                    {
                        Width = size * 2,
                        Height = size * 2,
                        Fill = brush,
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(square, x - size);
                    Canvas.SetTop(square, y - size);
                    ChartCanvas.Children.Add(square);
                    break;

                case "triangle":
                    var triangle = new Polygon
                    {
                        Points = new PointCollection
                        {
                            new Point(0, -size),
                            new Point(-size, size),
                            new Point(size, size)
                        },
                        Fill = brush,
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(triangle, x);
                    Canvas.SetTop(triangle, y);
                    ChartCanvas.Children.Add(triangle);
                    break;

                case "diamond":
                    var diamond = new Polygon
                    {
                        Points = new PointCollection
                        {
                            new Point(0, -size),
                            new Point(size, 0),
                            new Point(0, size),
                            new Point(-size, 0)
                        },
                        Fill = brush,
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(diamond, x);
                    Canvas.SetTop(diamond, y);
                    ChartCanvas.Children.Add(diamond);
                    break;

                case "cross":
                    var crossLine1 = new Line
                    {
                        X1 = x - size,
                        Y1 = y,
                        X2 = x + size,
                        Y2 = y,
                        Stroke = new SolidColorBrush(color) { Opacity = opacity },
                        StrokeThickness = 2
                    };
                    var crossLine2 = new Line
                    {
                        X1 = x,
                        Y1 = y - size,
                        X2 = x,
                        Y2 = y + size,
                        Stroke = new SolidColorBrush(color) { Opacity = opacity },
                        StrokeThickness = 2
                    };
                    ChartCanvas.Children.Add(crossLine1);
                    ChartCanvas.Children.Add(crossLine2);
                    break;

                case "star":
                    var star = new Polygon
                    {
                        Points = CreateStarPoints(size),
                        Fill = brush,
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(star, x);
                    Canvas.SetTop(star, y);
                    ChartCanvas.Children.Add(star);
                    break;

                default:
                    var defaultCircle = new Ellipse
                    {
                        Width = size * 2,
                        Height = size * 2,
                        Fill = brush,
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(defaultCircle, x - size);
                    Canvas.SetTop(defaultCircle, y - size);
                    ChartCanvas.Children.Add(defaultCircle);
                    break;
            }
        }

        private PointCollection CreateStarPoints(double size)
        {
            var points = new PointCollection();
            double outerRadius = size;
            double innerRadius = size * 0.4;

            for (int i = 0; i < 10; i++)
            {
                double angle = (i * 36 - 90) * Math.PI / 180;
                double radius = i % 2 == 0 ? outerRadius : innerRadius;
                points.Add(new Point(radius * Math.Cos(angle), radius * Math.Sin(angle)));
            }

            return points;
        }

        private void BtnOpenDataTable_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng bảng nhập dữ liệu sẽ được triển khai trong phiên bản tiếp theo.\n\nHiện tại hãy sử dụng các mẫu preset có sẵn.", 
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string presetName)
            {
                LoadPreset(presetName);
            }
        }

        private void OnShapeChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChart();
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender == SliderPointSize && TxtPointSize != null)
            {
                TxtPointSize.Text = $"{SliderPointSize.Value:F0} px";
            }
            else if (sender == SliderPointOpacity && TxtPointOpacity != null)
            {
                TxtPointOpacity.Text = $"{SliderPointOpacity.Value:F1}";
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
            IsConfirmed = true;
            this.Close();
        }

        private void BtnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = "ScatterChart.png"
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
            TxtChartTitle.Text = "Mối quan hệ giữa Chiều cao và Cân nặng";
            TxtXAxisLabel.Text = "Chiều cao (cm)";
            TxtYAxisLabel.Text = "Cân nặng (kg)";
            CmbPointShape.SelectedIndex = 0;
            SliderPointSize.Value = 5;
            SliderPointOpacity.Value = 0.7;
            ChkShowGrid.IsChecked = true;
            ChkShowAxes.IsChecked = true;
            ChkShowLabels.IsChecked = false;
            ChkShowTrendLine.IsChecked = true;
            ChkShowCorrelation.IsChecked = true;
            ChkShowLegend.IsChecked = true;

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

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class ScatterDataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Label { get; set; } = "";
        public string Category { get; set; } = "Default";
        public double Size { get; set; } = 5;
    }

    public class CategoryInfo
    {
        public Color Color { get; set; }
        public string Shape { get; set; } = "circle";
    }
}
