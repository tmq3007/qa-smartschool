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
using WpfPath = System.Windows.Shapes.Path;
using QASmartTouch.Helpers;
using QASmartTouch.Shared;
using System.Windows.Input;

namespace QASmartTouch.Forms
{
    public partial class Form2_12_AreaChartEditor : Window
    {
        private List<AreaChartSeries> chartSeries;
        private Form2_MainDashboard? _mainDashboard;

        public bool IsConfirmed { get; private set; }
        public AreaChartConfiguration? CurrentConfiguration { get; private set; }
        public double? TargetLeft { get; set; }
        public double? TargetTop { get; set; }

        // Configuration Properties (public for MainDashboard access)
        public string ChartTitle { get; private set; } = "Doanh thu theo tháng năm 2024";
        public List<string> Labels { get; private set; } = new List<string>();
        public List<AreaChartSeries> Series { get; private set; } = new List<AreaChartSeries>();
        public string LineStyle { get; private set; } = "straight";
        public string AreaType { get; private set; } = "normal";
        public double LineWidth { get; private set; } = 2;
        public double PointSize { get; private set; } = 4;
        public bool ShowGrid { get; private set; } = true;
        public bool ShowAxes { get; private set; } = true;
        public bool ShowPoints { get; private set; } = true;
        public bool ShowValues { get; private set; } = false;
        public bool ShowLegend { get; private set; } = true;
        public bool UseGradient { get; private set; } = true;

        public Form2_12_AreaChartEditor(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            TouchScrollHelper.AttachToAllScrollViewers(this);
            _mainDashboard = mainDashboard;
            chartSeries = new List<AreaChartSeries>();
            InitializeDefaultData();
            TxtChartTitle.TextChanged += (s, e) => { ChartTitle = TxtChartTitle.Text; if (IsLoaded) UpdateChart(); };
            TxtLabels.TextChanged += (s, e) => { if (IsLoaded) UpdateChart(); };
            Loaded += (s, e) => UpdateChart();
        }

        private void InitializeDefaultData()
        {
            chartSeries.Clear();
            chartSeries.Add(new AreaChartSeries 
            { 
                Name = "Doanh thu trực tiếp", 
                Data = new List<double> { 120, 150, 180, 140, 200, 220 }, 
                Color = Color.FromRgb(255, 107, 107),
                Opacity = 0.4
            });
            chartSeries.Add(new AreaChartSeries 
            { 
                Name = "Doanh thu online", 
                Data = new List<double> { 80, 95, 110, 100, 130, 145 }, 
                Color = Color.FromRgb(78, 205, 196),
                Opacity = 0.4
            });
            chartSeries.Add(new AreaChartSeries 
            { 
                Name = "Doanh thu đối tác", 
                Data = new List<double> { 50, 60, 70, 65, 85, 95 }, 
                Color = Color.FromRgb(69, 183, 209),
                Opacity = 0.4
            });

            RefreshSeriesUI();
        }

        private void RefreshSeriesUI()
        {
            SeriesPanel.Children.Clear();

            for (int i = 0; i < chartSeries.Count; i++)
            {
                var series = chartSeries[i];
                var index = i;

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var stackPanel = new StackPanel();

                // Header với color picker
                var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Color indicator
                var colorBorder = new Border
                {
                    Width = 30,
                    Height = 30,
                    Background = new SolidColorBrush(series.Color),
                    CornerRadius = new CornerRadius(6),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    BorderThickness = new Thickness(2),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = index
                };
                colorBorder.MouseLeftButtonDown += ColorIndicator_Click;
                Grid.SetColumn(colorBorder, 0);

                // Series name
                var nameBox = new TextBox
                {
                    Text = series.Name,
                    Margin = new Thickness(10, 0, 10, 0),
                    Padding = new Thickness(8),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Tag = index
                };
                nameBox.TextChanged += (s, e) =>
                {
                    var idx = (int)((TextBox)s).Tag;
                    chartSeries[idx].Name = ((TextBox)s).Text;
                    if (IsLoaded) UpdateChart();
                };
                Grid.SetColumn(nameBox, 1);

                // Remove button
                var removeBtn = new Button
                {
                    Content = "🗑️",
                    Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 4, 8, 4),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = index,
                    FontSize = 14
                };
                removeBtn.Click += (s, e) =>
                {
                    var idx = (int)((Button)s).Tag;
                    chartSeries.RemoveAt(idx);
                    RefreshSeriesUI();
                    UpdateChart();
                };
                Grid.SetColumn(removeBtn, 2);

                headerGrid.Children.Add(colorBorder);
                headerGrid.Children.Add(nameBox);
                headerGrid.Children.Add(removeBtn);
                stackPanel.Children.Add(headerGrid);

                // Opacity slider
                var opacityGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                opacityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                opacityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                opacityGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

                var opacityLabel = new TextBlock { Text = "Độ mờ:", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)), VerticalAlignment = VerticalAlignment.Center };
                var opacitySlider = new Slider { Minimum = 0, Maximum = 1, Value = series.Opacity, TickFrequency = 0.1, IsSnapToTickEnabled = true, Margin = new Thickness(10, 0, 10, 0), Tag = index };
                var opacityValue = new TextBlock { Text = series.Opacity.ToString("F1"), FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(102, 126, 234)), TextAlignment = TextAlignment.Right };

                opacitySlider.ValueChanged += (s, e) =>
                {
                    var idx = (int)((Slider)s).Tag;
                    chartSeries[idx].Opacity = e.NewValue;
                    opacityValue.Text = e.NewValue.ToString("F1");
                    UpdateChart();
                };

                Grid.SetColumn(opacityLabel, 0);
                Grid.SetColumn(opacitySlider, 1);
                Grid.SetColumn(opacityValue, 2);

                opacityGrid.Children.Add(opacityLabel);
                opacityGrid.Children.Add(opacitySlider);
                opacityGrid.Children.Add(opacityValue);
                stackPanel.Children.Add(opacityGrid);

                // Data points
                var dataLabel = new TextBlock { Text = "Dữ liệu (phân cách bằng dấu phẩy):", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 5) };
                stackPanel.Children.Add(dataLabel);

                var dataBox = new TextBox
                {
                    Text = string.Join(", ", series.Data),
                    Padding = new Thickness(8),
                    FontSize = 12,
                    Tag = index
                };
                dataBox.TextChanged += (s, e) =>
                {
                    var idx = (int)((TextBox)s).Tag;
                    var text = ((TextBox)s).Text;
                    var values = text.Split(',')
                        .Select(v => v.Trim())
                        .Where(v => !string.IsNullOrEmpty(v))
                        .Select(v => double.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result) ? result : 0)
                        .ToList();
                    chartSeries[idx].Data = values;
                    if (IsLoaded) UpdateChart();
                };
                stackPanel.Children.Add(dataBox);

                border.Child = stackPanel;
                SeriesPanel.Children.Add(border);
            }
        }

        private void ColorIndicator_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            var index = (int)border.Tag;

            // Simple color selection - cycle through preset colors
            var colors = new[] {
                Color.FromRgb(255, 107, 107),
                Color.FromRgb(78, 205, 196),
                Color.FromRgb(69, 183, 209),
                Color.FromRgb(255, 160, 122),
                Color.FromRgb(152, 216, 200),
                Color.FromRgb(255, 193, 7),
                Color.FromRgb(156, 39, 176),
                Color.FromRgb(76, 175, 80),
                Color.FromRgb(255, 87, 34),
                Color.FromRgb(3, 169, 244)
            };

            // Find current color index and move to next
            int currentIndex = -1;
            for (int i = 0; i < colors.Length; i++)
            {
                if (chartSeries[index].Color.R == colors[i].R && 
                    chartSeries[index].Color.G == colors[i].G && 
                    chartSeries[index].Color.B == colors[i].B)
                {
                    currentIndex = i;
                    break;
                }
            }

            var newColor = colors[(currentIndex + 1) % colors.Length];
            chartSeries[index].Color = newColor;
            border.Background = new SolidColorBrush(newColor);
            UpdateChart();
        }

        private void BtnAddSeries_Click(object sender, RoutedEventArgs e)
        {
            var newSeries = new AreaChartSeries
            {
                Name = $"Chuỗi dữ liệu {chartSeries.Count + 1}",
                Data = new List<double> { 50, 60, 70, 80, 90, 100 },
                Color = GetNextColor(),
                Opacity = 0.4
            };

            chartSeries.Add(newSeries);
            RefreshSeriesUI();
            UpdateChart();
        }

        private Color GetNextColor()
        {
            var colors = new Color[]
            {
                Color.FromRgb(255, 107, 107),
                Color.FromRgb(78, 205, 196),
                Color.FromRgb(69, 183, 209),
                Color.FromRgb(255, 160, 122),
                Color.FromRgb(152, 216, 200),
                Color.FromRgb(255, 193, 7),
                Color.FromRgb(156, 39, 176),
                Color.FromRgb(76, 175, 80),
                Color.FromRgb(255, 87, 34),
                Color.FromRgb(3, 169, 244)
            };

            return colors[chartSeries.Count % colors.Length];
        }

        private void LoadPreset(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var preset = button.Tag.ToString();

            switch (preset)
            {
                case "revenue":
                    TxtChartTitle.Text = "Doanh thu theo tháng năm 2024";
                    TxtLabels.Text = "T1, T2, T3, T4, T5, T6";
                    chartSeries = new List<AreaChartSeries>
                    {
                        new AreaChartSeries { Name = "Doanh thu trực tiếp", Data = new List<double> { 120, 150, 180, 140, 200, 220 }, Color = Color.FromRgb(255, 107, 107), Opacity = 0.4 },
                        new AreaChartSeries { Name = "Doanh thu online", Data = new List<double> { 80, 95, 110, 100, 130, 145 }, Color = Color.FromRgb(78, 205, 196), Opacity = 0.4 },
                        new AreaChartSeries { Name = "Doanh thu đối tác", Data = new List<double> { 50, 60, 70, 65, 85, 95 }, Color = Color.FromRgb(69, 183, 209), Opacity = 0.4 }
                    };
                    break;

                case "traffic":
                    TxtChartTitle.Text = "Lưu lượng truy cập website theo tuần";
                    TxtLabels.Text = "T2, T3, T4, T5, T6, T7, CN";
                    chartSeries = new List<AreaChartSeries>
                    {
                        new AreaChartSeries { Name = "Desktop", Data = new List<double> { 2500, 2800, 3100, 2900, 3300, 2200, 1800 }, Color = Color.FromRgb(255, 107, 107), Opacity = 0.5 },
                        new AreaChartSeries { Name = "Mobile", Data = new List<double> { 1800, 2100, 2300, 2200, 2600, 1700, 1400 }, Color = Color.FromRgb(78, 205, 196), Opacity = 0.5 },
                        new AreaChartSeries { Name = "Tablet", Data = new List<double> { 800, 950, 1050, 980, 1200, 750, 600 }, Color = Color.FromRgb(69, 183, 209), Opacity = 0.5 }
                    };
                    break;

                case "energy":
                    TxtChartTitle.Text = "Tiêu thụ năng lượng trong ngày";
                    TxtLabels.Text = "0h, 4h, 8h, 12h, 16h, 20h, 24h";
                    chartSeries = new List<AreaChartSeries>
                    {
                        new AreaChartSeries { Name = "Điện", Data = new List<double> { 30, 25, 50, 80, 90, 95, 60 }, Color = Color.FromRgb(255, 193, 7), Opacity = 0.4 },
                        new AreaChartSeries { Name = "Nước", Data = new List<double> { 15, 12, 30, 45, 50, 48, 35 }, Color = Color.FromRgb(3, 169, 244), Opacity = 0.4 }
                    };
                    break;

                case "sales":
                    TxtChartTitle.Text = "Doanh số bán hàng theo quý";
                    TxtLabels.Text = "Q1, Q2, Q3, Q4";
                    chartSeries = new List<AreaChartSeries>
                    {
                        new AreaChartSeries { Name = "Sản phẩm A", Data = new List<double> { 150, 180, 220, 250 }, Color = Color.FromRgb(255, 107, 107), Opacity = 0.5 },
                        new AreaChartSeries { Name = "Sản phẩm B", Data = new List<double> { 120, 140, 160, 190 }, Color = Color.FromRgb(78, 205, 196), Opacity = 0.5 },
                        new AreaChartSeries { Name = "Sản phẩm C", Data = new List<double> { 80, 95, 110, 130 }, Color = Color.FromRgb(69, 183, 209), Opacity = 0.5 }
                    };
                    break;
            }

            RefreshSeriesUI();
            UpdateChart();
        }

        private void UpdateChart()
        {
            ChartCanvas.Children.Clear();

            if (chartSeries == null || chartSeries.Count == 0)
                return;

            // Update properties from UI
            ChartTitle = TxtChartTitle.Text;
            Labels = TxtLabels.Text.Split(',').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
            Series = chartSeries;
            
            var lineStyleIndex = CmbLineStyle.SelectedIndex;
            LineStyle = lineStyleIndex switch { 0 => "straight", 1 => "curved", 2 => "stepped", _ => "straight" };
            
            var areaTypeIndex = CmbAreaType.SelectedIndex;
            AreaType = areaTypeIndex switch { 0 => "normal", 1 => "stacked", 2 => "percentage", _ => "normal" };
            
            LineWidth = SliderLineWidth.Value;
            PointSize = SliderPointSize.Value;
            ShowGrid = ChkShowGrid.IsChecked == true;
            ShowAxes = ChkShowAxes.IsChecked == true;
            ShowPoints = ChkShowPoints.IsChecked == true;
            ShowValues = ChkShowValues.IsChecked == true;
            ShowLegend = ChkShowLegend.IsChecked == true;
            UseGradient = ChkUseGradient.IsChecked == true;

            double canvasWidth = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : 900;
            double canvasHeight = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight : 550;

            double margin = 60;
            double chartWidth = canvasWidth - 2 * margin;
            double chartHeight = canvasHeight - 2 * margin - (ShowLegend ? 40 : 0);

            // Draw background
            var bgRect = new Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = Brushes.White
            };
            ChartCanvas.Children.Add(bgRect);

            // Draw title
            var title = new TextBlock
            {
                Text = ChartTitle,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            Canvas.SetLeft(title, (canvasWidth - title.ActualWidth) / 2);
            Canvas.SetTop(title, 15);
            ChartCanvas.Children.Add(title);

            // Calculate max value
            double maxValue = 0;
            if (AreaType == "normal")
            {
                maxValue = chartSeries.SelectMany(s => s.Data).DefaultIfEmpty(0).Max();
            }
            else if (AreaType == "stacked")
            {
                for (int i = 0; i < Labels.Count; i++)
                {
                    double sum = chartSeries.Sum(s => i < s.Data.Count ? s.Data[i] : 0);
                    maxValue = Math.Max(maxValue, sum);
                }
            }
            else // percentage
            {
                maxValue = 100;
            }

            maxValue = Math.Ceiling(maxValue / 10) * 10;
            if (maxValue == 0) maxValue = 100;

            // Draw grid
            if (ShowGrid)
            {
                for (int i = 0; i <= 5; i++)
                {
                    double y = margin + (chartHeight / 5) * i;
                    var line = new Line
                    {
                        X1 = margin,
                        Y1 = y,
                        X2 = margin + chartWidth,
                        Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                        StrokeThickness = 1
                    };
                    ChartCanvas.Children.Add(line);

                    var valueLabel = new TextBlock
                    {
                        Text = ((maxValue / 5) * (5 - i)).ToString("F0"),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120))
                    };
                    Canvas.SetLeft(valueLabel, margin - 35);
                    Canvas.SetTop(valueLabel, y - 7);
                    ChartCanvas.Children.Add(valueLabel);
                }
            }

            // Draw axes
            if (ShowAxes)
            {
                var xAxis = new Line
                {
                    X1 = margin,
                    Y1 = margin + chartHeight,
                    X2 = margin + chartWidth,
                    Y2 = margin + chartHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    StrokeThickness = 2
                };
                ChartCanvas.Children.Add(xAxis);

                var yAxis = new Line
                {
                    X1 = margin,
                    Y1 = margin,
                    X2 = margin,
                    Y2 = margin + chartHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    StrokeThickness = 2
                };
                ChartCanvas.Children.Add(yAxis);
            }

            // Draw labels
            for (int i = 0; i < Labels.Count; i++)
            {
                double x = margin + (chartWidth / (Labels.Count - 1)) * i;
                var label = new TextBlock
                {
                    Text = Labels[i],
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80))
                };
                Canvas.SetLeft(label, x - 15);
                Canvas.SetTop(label, margin + chartHeight + 10);
                ChartCanvas.Children.Add(label);
            }

            // Draw area charts
            double[] stackedValues = new double[Labels.Count];

            for (int seriesIndex = chartSeries.Count - 1; seriesIndex >= 0; seriesIndex--)
            {
                var series = chartSeries[seriesIndex];
                if (series.Data.Count == 0) continue;

                var points = new PointCollection();
                var linePoints = new PointCollection();

                for (int i = 0; i < Labels.Count && i < series.Data.Count; i++)
                {
                    double x = margin + (chartWidth / (Labels.Count - 1)) * i;
                    double value = series.Data[i];

                    if (AreaType == "percentage")
                    {
                        double total = chartSeries.Sum(s => i < s.Data.Count ? s.Data[i] : 0);
                        value = total > 0 ? (value / total) * 100 : 0;
                    }

                    double y;
                    if (AreaType == "stacked")
                    {
                        y = margin + chartHeight - ((stackedValues[i] + value) / maxValue * chartHeight);
                        stackedValues[i] += value;
                    }
                    else
                    {
                        y = margin + chartHeight - (value / maxValue * chartHeight);
                    }

                    points.Add(new Point(x, y));
                    linePoints.Add(new Point(x, y));
                }

                // Close the area polygon
                if (AreaType == "stacked" && seriesIndex < chartSeries.Count - 1)
                {
                    for (int i = Labels.Count - 1; i >= 0; i--)
                    {
                        double x = margin + (chartWidth / (Labels.Count - 1)) * i;
                        double value = series.Data[i < series.Data.Count ? i : series.Data.Count - 1];
                        if (AreaType == "percentage")
                        {
                            double total = chartSeries.Sum(s => i < s.Data.Count ? s.Data[i] : 0);
                            value = total > 0 ? (value / total) * 100 : 0;
                        }
                        double y = margin + chartHeight - ((stackedValues[i] - value) / maxValue * chartHeight);
                        points.Add(new Point(x, y));
                    }
                }
                else
                {
                    points.Add(new Point(margin + chartWidth, margin + chartHeight));
                    points.Add(new Point(margin, margin + chartHeight));
                }

                // Draw area
                var polygon = new Polygon
                {
                    Points = points,
                    Fill = UseGradient ? CreateGradientBrush(series.Color, series.Opacity) : new SolidColorBrush(Color.FromArgb((byte)(series.Opacity * 255), series.Color.R, series.Color.G, series.Color.B)),
                    Stroke = new SolidColorBrush(series.Color),
                    StrokeThickness = LineWidth
                };
                ChartCanvas.Children.Add(polygon);

                // Draw points
                if (ShowPoints && PointSize > 0)
                {
                    foreach (var point in linePoints)
                    {
                        var ellipse = new Ellipse
                        {
                            Width = PointSize * 2,
                            Height = PointSize * 2,
                            Fill = new SolidColorBrush(series.Color),
                            Stroke = Brushes.White,
                            StrokeThickness = 2
                        };
                        Canvas.SetLeft(ellipse, point.X - PointSize);
                        Canvas.SetTop(ellipse, point.Y - PointSize);
                        ChartCanvas.Children.Add(ellipse);
                    }
                }

                // Draw values
                if (ShowValues)
                {
                    for (int i = 0; i < linePoints.Count && i < series.Data.Count; i++)
                    {
                        var value = series.Data[i];
                        var valueLabel = new TextBlock
                        {
                            Text = value.ToString("F0"),
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(series.Color)
                        };
                        Canvas.SetLeft(valueLabel, linePoints[i].X - 12);
                        Canvas.SetTop(valueLabel, linePoints[i].Y - 20);
                        ChartCanvas.Children.Add(valueLabel);
                    }
                }
            }

            // Draw legend
            if (ShowLegend)
            {
                double legendY = margin + chartHeight + 50;
                double legendX = margin;

                for (int i = 0; i < chartSeries.Count; i++)
                {
                    var series = chartSeries[i];

                    var legendRect = new Rectangle
                    {
                        Width = 20,
                        Height = 15,
                        Fill = new SolidColorBrush(series.Color)
                    };
                    Canvas.SetLeft(legendRect, legendX);
                    Canvas.SetTop(legendRect, legendY);
                    ChartCanvas.Children.Add(legendRect);

                    var legendText = new TextBlock
                    {
                        Text = series.Name,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80))
                    };
                    Canvas.SetLeft(legendText, legendX + 25);
                    Canvas.SetTop(legendText, legendY);
                    ChartCanvas.Children.Add(legendText);

                    legendX += 25 + legendText.Text.Length * 7 + 20;
                }
            }
        }

        private Brush CreateGradientBrush(Color color, double opacity)
        {
            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(0, 1);
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B), 0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(opacity * 128), color.R, color.G, color.B), 0.5));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(20, color.R, color.G, color.B), 1));
            return gradient;
        }

        private void OnChartPropertyChanged(object sender, RoutedEventArgs e)
        {
            UpdateChart();
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = (Slider)sender;
            if (slider.Name == "SliderLineWidth")
                TxtLineWidth.Text = slider.Value.ToString("F0") + "px";
            else if (slider.Name == "SliderPointSize")
                TxtPointSize.Text = slider.Value.ToString("F0") + "px";

            UpdateChart();
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateChart();
        }

        private void SaveCurrentConfiguration()
        {
            CurrentConfiguration = new AreaChartConfiguration
            {
                Title = TxtChartTitle.Text,
                RawLabels = TxtLabels.Text,
                Series = chartSeries.Select(s => new AreaChartSeriesData
                {
                    Name = s.Name,
                    Color = s.Color,
                    Opacity = s.Opacity,
                    Data = new List<double>(s.Data)
                }).ToList(),
                LineStyleIndex = CmbLineStyle.SelectedIndex,
                AreaTypeIndex = CmbAreaType.SelectedIndex,
                LineWidth = SliderLineWidth.Value,
                PointSize = SliderPointSize.Value,
                ShowGrid = ChkShowGrid.IsChecked ?? true,
                ShowAxes = ChkShowAxes.IsChecked ?? true,
                ShowPoints = ChkShowPoints.IsChecked ?? true,
                ShowValues = ChkShowValues.IsChecked ?? false,
                ShowLegend = ChkShowLegend.IsChecked ?? true,
                UseGradient = ChkUseGradient.IsChecked ?? true
            };
        }

        public void LoadConfiguration(AreaChartConfiguration config)
        {
            if (config == null) return;
            CurrentConfiguration = config;

            TxtChartTitle.Text = config.Title;
            ChartTitle = config.Title;
            TxtLabels.Text = config.RawLabels;

            chartSeries.Clear();
            foreach (var s in config.Series)
            {
                chartSeries.Add(new AreaChartSeries
                {
                    Name = s.Name,
                    Color = s.Color,
                    Opacity = s.Opacity,
                    Data = new List<double>(s.Data)
                });
            }

            if (config.LineStyleIndex >= 0 && config.LineStyleIndex < CmbLineStyle.Items.Count)
                CmbLineStyle.SelectedIndex = config.LineStyleIndex;
            if (config.AreaTypeIndex >= 0 && config.AreaTypeIndex < CmbAreaType.Items.Count)
                CmbAreaType.SelectedIndex = config.AreaTypeIndex;

            SliderLineWidth.Value = config.LineWidth;
            SliderPointSize.Value = config.PointSize;
            ChkShowGrid.IsChecked = config.ShowGrid;
            ChkShowAxes.IsChecked = config.ShowAxes;
            ChkShowPoints.IsChecked = config.ShowPoints;
            ChkShowValues.IsChecked = config.ShowValues;
            ChkShowLegend.IsChecked = config.ShowLegend;
            ChkUseGradient.IsChecked = config.UseGradient;

            RefreshSeriesUI();
            UpdateChart();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentConfiguration();
            UpdateChart();
            ChartCanvas.UpdateLayout();

            IsConfirmed = true;
            DialogResult = true;
            if (_mainDashboard != null)
            {
                AddInteractiveChartToCanvas();
            }
            this.Close();
        }

        /// <summary>
        /// Thêm biểu đồ vùng tương tác lên canvas MainInteractiveBoard
        /// </summary>
        private void AddInteractiveChartToCanvas()
        {
            if (_mainDashboard == null) return;

            // Render ChartCanvas
            double renderWidth = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : 800;
            double renderHeight = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight : 600;

            var renderBitmap = new RenderTargetBitmap(
                (int)renderWidth,
                (int)renderHeight,
                96, 96,
                PixelFormats.Pbgra32);

            renderBitmap.Render(ChartCanvas);

            InteractiveChartHelper.CreateAndAddChartToCanvas(new InteractiveChartHelper.ChartContainerOptions
            {
                MainDashboard = _mainDashboard,
                ChartTitle = string.IsNullOrWhiteSpace(TxtChartTitle.Text) ? "Biểu đồ vùng" : $"Biểu đồ vùng: {TxtChartTitle.Text}",
                Icon = "🏔️",
                Bitmap = renderBitmap,
                InitialWidth = renderWidth * 0.8,
                InitialHeight = renderHeight * 0.8,
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
            if (_mainDashboard == null) return;
            double originalLeft = Canvas.GetLeft(originalContainer);
            double originalTop = Canvas.GetTop(originalContainer);
            if (double.IsNaN(originalLeft)) originalLeft = 0;
            if (double.IsNaN(originalTop)) originalTop = 0;

            InteractiveChartHelper.CreateAndAddChartToCanvas(new InteractiveChartHelper.ChartContainerOptions
            {
                MainDashboard = _mainDashboard,
                ChartTitle = string.IsNullOrWhiteSpace(TxtChartTitle.Text) ? "Biểu đồ vùng (Bản sao)" : $"Biểu đồ vùng: {TxtChartTitle.Text} (Bản sao)",
                Icon = "🏔️",
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
            if (_mainDashboard == null) return;
            var mainCanvas = _mainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas != null)
            {
                mainCanvas.Children.Remove(container);
                _mainDashboard.RecordChartRemove(container, "Biểu đồ vùng");
            }
        }

        private void EditChart(Grid container)
        {
            double originalLeft = Canvas.GetLeft(container);
            double originalTop = Canvas.GetTop(container);
            if (double.IsNaN(originalLeft)) originalLeft = 0;
            if (double.IsNaN(originalTop)) originalTop = 0;

            var editor = new Form2_12_AreaChartEditor(_mainDashboard);
            editor.TargetLeft = originalLeft;
            editor.TargetTop = originalTop;
            if (CurrentConfiguration != null)
            {
                editor.LoadConfiguration(CurrentConfiguration);
            }
            bool? result = WindowHelper.ShowChildDialog(editor, _mainDashboard);
            if (result == true)
            {
                var mainCanvas = _mainDashboard?.FindName("MainInteractiveBoard") as Canvas;
                if (mainCanvas != null)
                {
                    mainCanvas.Children.Remove(container);
                    _mainDashboard?.RecordChartRemove(container, "Biểu đồ vùng");
                }
            }
        }

        private void BtnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    Title = "Lưu biểu đồ miền",
                    FileName = $"area_chart_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var renderBitmap = new RenderTargetBitmap(
                        (int)ChartCanvas.ActualWidth,
                        (int)ChartCanvas.ActualHeight,
                        96, 96, PixelFormats.Pbgra32);

                    renderBitmap.Render(ChartCanvas);

                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (var fileStream = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }

                    MessageBox.Show("Đã lưu hình ảnh thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu hình ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            TxtChartTitle.Text = "Doanh thu theo tháng năm 2024";
            TxtLabels.Text = "T1, T2, T3, T4, T5, T6";
            CmbLineStyle.SelectedIndex = 0;
            CmbAreaType.SelectedIndex = 0;
            SliderLineWidth.Value = 2;
            SliderPointSize.Value = 4;
            ChkShowGrid.IsChecked = true;
            ChkShowAxes.IsChecked = true;
            ChkShowPoints.IsChecked = true;
            ChkShowValues.IsChecked = false;
            ChkShowLegend.IsChecked = true;
            ChkUseGradient.IsChecked = true;

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
    }

    public class AreaChartSeries
    {
        public string Name { get; set; } = "";
        public List<double> Data { get; set; } = new List<double>();
        public Color Color { get; set; }
        public double Opacity { get; set; } = 0.4;
    }

    public class AreaChartConfiguration
    {
        public string Title { get; set; } = "Doanh thu theo tháng năm 2024";
        public string RawLabels { get; set; } = "T1, T2, T3, T4, T5, T6";
        public List<AreaChartSeriesData> Series { get; set; } = new List<AreaChartSeriesData>();
        public int LineStyleIndex { get; set; } = 0;
        public int AreaTypeIndex { get; set; } = 0;
        public double LineWidth { get; set; } = 2;
        public double PointSize { get; set; } = 4;
        public bool ShowGrid { get; set; } = true;
        public bool ShowAxes { get; set; } = true;
        public bool ShowPoints { get; set; } = true;
        public bool ShowValues { get; set; } = false;
        public bool ShowLegend { get; set; } = true;
        public bool UseGradient { get; set; } = true;
    }

    public class AreaChartSeriesData
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; }
        public double Opacity { get; set; } = 0.5;
        public List<double> Data { get; set; } = new List<double>();
    }
}
