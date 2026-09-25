using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;
using System.Text.Json;
using System.Windows.Media.Animation;
using System.Windows.Documents;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_9_LineChartEditor : Window
    {
        private readonly Form2_MainDashboard? _mainDashboard;
        
        public bool IsConfirmed { get; private set; } = false;
        public bool DisplayStatsMode { get; private set; } = false;
        public LineChartConfiguration CurrentConfiguration { get; private set; }

        // Chart data
        private List<string> labels = new List<string>();
        private List<LineSeries> lineSeries = new List<LineSeries>();
        
        // Chart settings - increased margins to prevent clipping
        private double chartWidth = 800;
        private double chartHeight = 550;
        private double marginLeft = 80;
        private double marginRight = 100;  // Increased from 50 to prevent right-side clipping
        private double marginTop = 80;
        private double marginBottom = 100;

        public class LineSeries
        {
            public string Name { get; set; } = "Line";
            public List<double> Data { get; set; } = new List<double>();
            public Color Color { get; set; } = Colors.Red;
            public Button ColorButton { get; set; }
        }

        public Form2_9_LineChartEditor(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            TouchScrollHelper.AttachToAllScrollViewers(this);
            _mainDashboard = mainDashboard;
            InitializeDefaultData();
            CreateLineSeriesUI();
            DrawChart();
        }

        private void InitializeDefaultData()
        {
            labels = new List<string> { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
            
            lineSeries = new List<LineSeries>
            {
                new LineSeries 
                { 
                    Name = "Doanh thu 2024",
                    Data = new List<double> { 120, 150, 180, 220, 250, 280, 300, 320, 290, 310, 340, 380 },
                    Color = Color.FromRgb(255, 107, 107)
                },
                new LineSeries 
                { 
                    Name = "Doanh thu 2023",
                    Data = new List<double> { 100, 130, 160, 190, 210, 240, 260, 270, 250, 280, 300, 330 },
                    Color = Color.FromRgb(78, 205, 196)
                }
            };
            // ✅ Thêm axis labels mặc định
            if (string.IsNullOrWhiteSpace(txtXAxisLabel.Text))
                txtXAxisLabel.Text = "Tháng";

            if (string.IsNullOrWhiteSpace(txtYAxisLabel.Text))
                txtYAxisLabel.Text = "Doanh thu (triệu đồng)";
        }

        private void CreateLineSeriesUI()
        {
            LineSeriesPanel.Children.Clear();

            for (int i = 0; i < lineSeries.Count; i++)
            {
                var series = lineSeries[i];
                var seriesIndex = i;

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

                // Header with color and name
                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                if (lineSeries.Count > 1)
                {
                    headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                }

                // Color button
                var colorBtn = new Button
                {
                    Background = new SolidColorBrush(series.Color),
                    Width = 30,
                    Height = 30,
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = seriesIndex
                };
                colorBtn.Click += ColorButton_Click;
                series.ColorButton = colorBtn;
                Grid.SetColumn(colorBtn, 0);

                // Name textbox
                var nameBox = new TextBox
                {
                    Text = series.Name,
                    Margin = new Thickness(10, 0, 0, 0),
                    Height = 30,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Tag = seriesIndex
                };
                nameBox.TextChanged += (s, e) =>
                {
                    var idx = (int)((TextBox)s).Tag;
                    lineSeries[idx].Name = ((TextBox)s).Text;
                    DrawChart();
                };
                Grid.SetColumn(nameBox, 1);

                headerGrid.Children.Add(colorBtn);
                headerGrid.Children.Add(nameBox);

                // Remove button
                if (lineSeries.Count > 1)
                {
                    var removeBtn = new Button
                    {
                        Content = "×",
                        Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                        Foreground = Brushes.White,
                        Width = 30,
                        Height = 30,
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(10, 0, 0, 0),
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Tag = seriesIndex
                    };
                    removeBtn.Click += RemoveLineSeries_Click;
                    Grid.SetColumn(removeBtn, 2);
                    headerGrid.Children.Add(removeBtn);
                }

                stackPanel.Children.Add(headerGrid);

                // Data inputs
                var dataWrap = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
                for (int j = 0; j < series.Data.Count; j++)
                {
                    var dataBox = new TextBox
                    {
                        Text = series.Data[j].ToString(),
                        Width = 60,
                        Height = 30,
                        Margin = new Thickness(0, 0, 8, 8),
                        TextAlignment = TextAlignment.Center,
                        Tag = new Tuple<int, int>(seriesIndex, j)
                    };
                    dataBox.TextChanged += DataBox_TextChanged;
                    dataWrap.Children.Add(dataBox);
                }
                stackPanel.Children.Add(dataWrap);

                border.Child = stackPanel;
                LineSeriesPanel.Children.Add(border);
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var index = (int)btn.Tag;

            // Create a simple color picker window
            var colorPicker = new Window
            {
                Title = "Chọn màu",
                Width = 280,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new WrapPanel { Margin = new Thickness(10) };
            
            // Preset colors (from HTML template)
            Color[] presetColors = new[]
            {
                Color.FromRgb(54, 162, 235),   // Blue
                Color.FromRgb(255, 99, 132),   // Red
                Color.FromRgb(75, 192, 192),   // Teal
                Color.FromRgb(255, 159, 64),   // Orange
                Color.FromRgb(153, 102, 255),  // Purple
                Color.FromRgb(255, 206, 86),   // Yellow
                Color.FromRgb(76, 175, 80),    // Green
                Color.FromRgb(158, 158, 158),  // Gray
                Color.FromRgb(255, 87, 34),    // Deep Orange
                Color.FromRgb(0, 188, 212),    // Cyan
                Color.FromRgb(233, 30, 99),    // Pink
                Color.FromRgb(121, 85, 72)     // Brown
            };

            Color? selectedColor = null;

            foreach (var color in presetColors)
            {
                var colorBtn = new Button
                {
                    Width = 50,
                    Height = 50,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(color),
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Gray
                };
                
                colorBtn.Click += (s, ev) =>
                {
                    selectedColor = color;
                    colorPicker.DialogResult = true;
                    colorPicker.Close();
                };
                
                panel.Children.Add(colorBtn);
            }

            colorPicker.Content = panel;

            if (colorPicker.ShowDialog() == true && selectedColor.HasValue)
            {
                lineSeries[index].Color = selectedColor.Value;
                btn.Background = new SolidColorBrush(selectedColor.Value);
                DrawChart();
            }
        }

        private void DataBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox)sender;
            var indices = (Tuple<int, int>)box.Tag;
            
            if (double.TryParse(box.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                lineSeries[indices.Item1].Data[indices.Item2] = value;
                DrawChart();
            }
        }

        private void RemoveLineSeries_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var index = (int)btn.Tag;
            
            if (lineSeries.Count > 1)
            {
                lineSeries.RemoveAt(index);
                CreateLineSeriesUI();
                DrawChart();
            }
        }

        private void AddLineSeries_Click(object sender, RoutedEventArgs e)
        {
            var random = new Random();
            var newSeries = new LineSeries
            {
                Name = $"Đường {lineSeries.Count + 1}",
                Data = Enumerable.Range(0, labels.Count).Select(_ => random.Next(50, 400)).Select(x => (double)x).ToList(),
                Color = Color.FromRgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256))
            };
            
            lineSeries.Add(newSeries);
            CreateLineSeriesUI();
            DrawChart();
        }

        private void DrawChart()
        {
            ChartCanvas.Children.Clear();

            if (labels.Count == 0 || lineSeries.Count == 0) return;

            // Canvas size is set in XAML (700x450)
            // No need to set here

            // Get all data points
            var allData = lineSeries.SelectMany(s => s.Data).ToList();
            if (allData.Count == 0) return;

            double minValue = Math.Min(0, allData.Min());
            double maxValue = allData.Max();
            double range = maxValue - minValue;
            if (range == 0) range = 1;

            // Use fixed canvas size (700x450 as defined in XAML)
            double canvasWidth = ChartCanvas.Width > 0 ? ChartCanvas.Width : 800;
            double canvasHeight = ChartCanvas.Height > 0 ? ChartCanvas.Height : 650;
            
            double plotWidth = canvasWidth - marginLeft - marginRight;
            double plotHeight = canvasHeight - marginTop - marginBottom;

            // Draw title - centered horizontally
            var title = new TextBlock
            {
                Text = txtTitle.Text,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            
            // Measure the text to get actual width
            title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double titleWidth = title.DesiredSize.Width;
            
            // Center the title horizontally in the plot area
            Canvas.SetLeft(title, marginLeft + (plotWidth - titleWidth) / 2);
            Canvas.SetTop(title, 20);
            ChartCanvas.Children.Add(title);

            // Draw grid
            if (chkShowGrid.IsChecked == true)
            {
                DrawGrid(plotWidth, plotHeight, minValue, maxValue);
            }

            // Draw axes
            if (chkShowAxes.IsChecked == true)
            {
                DrawAxes(plotWidth, plotHeight);
            }

            // Draw Y-axis labels
            DrawYAxisLabels(plotHeight, minValue, maxValue);

            // Draw X-axis labels
            DrawXAxisLabels(plotWidth, plotHeight);

            // Draw lines
            DrawLines(plotWidth, plotHeight, minValue, range);

            // Draw legend
            if (chkShowLegend.IsChecked == true)
            {
                DrawLegend();
            }

            // Draw axis labels
            DrawAxisLabels(plotWidth, plotHeight);

        }


        private void DrawTitle(double plotWidth)
        {
            var title = new TextBlock
            {
                Text = txtTitle.Text,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };

            title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double titleWidth = title.DesiredSize.Width;

            // ✅ Căn giữa trong toàn bộ canvas width
            Canvas.SetLeft(title, (chartWidth - titleWidth) / 2);
            Canvas.SetTop(title, 20);
            ChartCanvas.Children.Add(title);
        }


        private void DrawGrid(double plotWidth, double plotHeight, double minValue, double maxValue)
        {
            int gridLines = 10;
            
            for (int i = 0; i <= gridLines; i++)
            {
                // Horizontal lines
                double y = marginTop + (plotHeight * i / gridLines);
                var hLine = new Line
                {
                    X1 = marginLeft,
                    Y1 = y,
                    X2 = marginLeft + plotWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(hLine);

                // Vertical lines
                if (i < labels.Count)
                {
                    double x = marginLeft + (plotWidth * i / (labels.Count - 1));
                    var vLine = new Line
                    {
                        X1 = x,
                        Y1 = marginTop,
                        X2 = x,
                        Y2 = marginTop + plotHeight,
                        Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                        StrokeThickness = 1
                    };
                    ChartCanvas.Children.Add(vLine);
                }
            }
        }

        private void DrawAxes(double plotWidth, double plotHeight)
        {
            // X-axis
            var xAxis = new Line
            {
                X1 = marginLeft,
                Y1 = marginTop + plotHeight,
                X2 = marginLeft + plotWidth,
                Y2 = marginTop + plotHeight,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 2
            };
            ChartCanvas.Children.Add(xAxis);

            // Y-axis
            var yAxis = new Line
            {
                X1 = marginLeft,
                Y1 = marginTop,
                X2 = marginLeft,
                Y2 = marginTop + plotHeight,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 2
            };
            ChartCanvas.Children.Add(yAxis);
        }

        private void DrawYAxisLabels(double plotHeight, double minValue, double maxValue)
        {
            int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
            
                double value = maxValue - (maxValue - minValue) * i / steps;
                double y = marginTop + (plotHeight * i / steps);

                var label = new TextBlock
                {
                    Text = value.ToString("N0"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };

               

                Canvas.SetLeft(label, marginLeft - 60);
                Canvas.SetTop(label, y - 10);
                ChartCanvas.Children.Add(label);
            }
        }

        private void DrawXAxisLabels(double plotWidth, double plotHeight)
        {
            for (int i = 0; i < labels.Count; i++)
            {
                double x = marginLeft + (plotWidth * i / (labels.Count - 1));
        
                var label = new TextBlock
                {
                    Text = labels[i],
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };

                // ✅ Measure để lấy chiều rộng thực tế
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double labelWidth = label.DesiredSize.Width;

                // ✅ Căn giữa label với điểm dữ liệu
                double labelLeft = x - (labelWidth / 2);

                // ✅ Giới hạn không vượt quá canvas
                labelLeft = Math.Max(5, Math.Min(labelLeft, chartWidth - labelWidth - 5));


                Canvas.SetLeft(label, x - 15);
                Canvas.SetTop(label, marginTop + plotHeight + 10);
                ChartCanvas.Children.Add(label);
            }
        }

        private void DrawAxisLabels(double plotWidth, double plotHeight)
        {
            // ✅ X-axis label (bên phải T12, cùng dòng với trục X)
            if (!string.IsNullOrWhiteSpace(txtXAxisLabel.Text))
            {
                var xLabel = new TextBlock
                {
                    Text = txtXAxisLabel.Text,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 70))
                };

                xLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double xLabelWidth = xLabel.DesiredSize.Width;

                // ✅ Vị trí: Bên phải trục X, cùng dòng với baseline
                double xLabelLeft = marginLeft + plotWidth + 15;
                double xLabelTop = marginTop + plotHeight - 8; // -8 để căn giữa với trục X

                
                Canvas.SetLeft(xLabel, xLabelLeft);
                Canvas.SetTop(xLabel, xLabelTop);
                ChartCanvas.Children.Add(xLabel);
            }

            // ✅ Y-axis label (phía trên, căn giữa với trục Y)
            if (!string.IsNullOrWhiteSpace(txtYAxisLabel.Text))
            {
                var yLabel = new TextBlock
                {
                    Text = txtYAxisLabel.Text,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                    TextAlignment = TextAlignment.Center
                };

                yLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double textWidth = yLabel.DesiredSize.Width;

                // ✅ Căn giữa với trục Y, phía trên cùng
                Canvas.SetLeft(yLabel, marginLeft - textWidth / 2);
                Canvas.SetTop(yLabel, 10);
                ChartCanvas.Children.Add(yLabel);
            }



        }

        private void DrawAxisLabels_Back_12112025(double plotWidth, double plotHeight)
        {
            // X-axis label (right side, after T12, same line as X-axis)
            if (!string.IsNullOrWhiteSpace(txtXAxisLabel.Text))
            {
                var xLabel = new TextBlock
                {
                    Text = txtXAxisLabel.Text,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 70))
                };

                // ✅ Đảm bảo không vượt quá canvas
                double xLabelLeft = marginLeft + plotWidth + 15;
                if (xLabelLeft + 100 > chartWidth)
                {
                    xLabelLeft = chartWidth - 120; // Đặt lại vị trí an toàn
                }

                // Position after T12, aligned with X-axis line
                Canvas.SetLeft(xLabel, marginLeft + plotWidth + 15);
                Canvas.SetTop(xLabel, marginTop + plotHeight - 10);
                ChartCanvas.Children.Add(xLabel);
            }

            // Y-axis label (top, horizontal, centered with Y-axis)
            if (!string.IsNullOrWhiteSpace(txtYAxisLabel.Text))
            {
                var yLabel = new TextBlock
                {
                    Text = txtYAxisLabel.Text,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                    TextAlignment = TextAlignment.Center
                };
                
                // Measure text width to center it properly
                yLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double textWidth = yLabel.DesiredSize.Width;
                
                // Position at top, centered horizontally with Y-axis
                Canvas.SetLeft(yLabel, marginLeft - textWidth / 2);
                Canvas.SetTop(yLabel, 10);
                ChartCanvas.Children.Add(yLabel);
            }
        }

        private void DrawLines(double plotWidth, double plotHeight, double minValue, double range)
        {
            double lineWidth = sliderLineWidth.Value;
            double pointSize = sliderPointSize.Value;
            bool showPoints = chkShowPoints.IsChecked == true;
            bool showValues = chkShowValues.IsChecked == true;
            bool fillArea = chkFillArea.IsChecked == true;
            string lineStyle = ((ComboBoxItem)cmbLineStyle.SelectedItem).Content.ToString();

            foreach (var series in lineSeries)
            {
                if (series.Data.Count != labels.Count) continue;

                var points = new List<Point>();
                
                for (int i = 0; i < series.Data.Count; i++)
                {
                    double x = marginLeft + (plotWidth * i / (labels.Count - 1));
                    double normalizedValue = (series.Data[i] - minValue) / range;
                    double y = marginTop + plotHeight - (normalizedValue * plotHeight);
                    points.Add(new Point(x, y));
                }

                // Fill area
                if (fillArea)
                {
                    var fillPath = new System.Windows.Shapes.Path
                    {
                        Stroke = null,
                        Fill = new SolidColorBrush(Color.FromArgb(50, series.Color.R, series.Color.G, series.Color.B))
                    };

                    var fillGeometry = new PathGeometry();
                    var fillFigure = new PathFigure { StartPoint = new Point(points[0].X, marginTop + plotHeight) };
                    
                    fillFigure.Segments.Add(new LineSegment(points[0], false));
                    for (int i = 1; i < points.Count; i++)
                    {
                        fillFigure.Segments.Add(new LineSegment(points[i], true));
                    }
                    fillFigure.Segments.Add(new LineSegment(new Point(points.Last().X, marginTop + plotHeight), true));
                    fillFigure.IsClosed = true;
                    
                    fillGeometry.Figures.Add(fillFigure);
                    fillPath.Data = fillGeometry;
                    ChartCanvas.Children.Add(fillPath);
                }

                // Draw line
                var path = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(series.Color),
                    StrokeThickness = lineWidth,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };

                var geometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = points[0] };

                if (lineStyle == "Đường cong mượt")
                {
                    // Bezier curve
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var p0 = points[i];
                        var p1 = points[i + 1];
                        var controlPoint1 = new Point(p0.X + (p1.X - p0.X) / 3, p0.Y);
                        var controlPoint2 = new Point(p0.X + 2 * (p1.X - p0.X) / 3, p1.Y);
                        figure.Segments.Add(new BezierSegment(controlPoint1, controlPoint2, p1, true));
                    }
                }
                else if (lineStyle == "Đường bậc thang")
                {
                    // Step line
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        figure.Segments.Add(new LineSegment(new Point(points[i + 1].X, points[i].Y), true));
                        figure.Segments.Add(new LineSegment(points[i + 1], true));
                    }
                }
                else
                {
                    // Straight line
                    for (int i = 1; i < points.Count; i++)
                    {
                        figure.Segments.Add(new LineSegment(points[i], true));
                    }
                }

                geometry.Figures.Add(figure);
                path.Data = geometry;
                ChartCanvas.Children.Add(path);

                // Draw points and values
                for (int i = 0; i < points.Count; i++)
                {
                    if (showPoints && pointSize > 0)
                    {
                        var point = new Ellipse
                        {
                            Width = pointSize * 2,
                            Height = pointSize * 2,
                            Fill = new SolidColorBrush(series.Color),
                            Stroke = Brushes.White,
                            StrokeThickness = 2
                        };
                        Canvas.SetLeft(point, points[i].X - pointSize);
                        Canvas.SetTop(point, points[i].Y - pointSize);
                        ChartCanvas.Children.Add(point);
                    }

                    if (showValues)
                    {
                        var valueText = new TextBlock
                        {
                            Text = series.Data[i].ToString("N0"),
                            FontSize = 10,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(series.Color)
                        };
                        Canvas.SetLeft(valueText, points[i].X - 15);
                        Canvas.SetTop(valueText, points[i].Y - 25);
                        ChartCanvas.Children.Add(valueText);
                    }
                }
            }
        }

        private void DrawLegend()
        {
            // Use fixed canvas size
            double canvasWidth = ChartCanvas.Width > 0 ? ChartCanvas.Width : 800;
            double canvasHeight = ChartCanvas.Height > 0 ? ChartCanvas.Height : 650;
            double plotWidth = canvasWidth - marginLeft - marginRight;
            double plotHeight = canvasHeight - marginTop - marginBottom;
            
            // Position legend at bottom, horizontal layout
            double lineLength = 30;
            double itemSpacing = 150; // Horizontal spacing between legend items
            double legendY = marginTop + plotHeight + 50; // Below X-axis labels

            // Kiểm tra không vượt quá canvas height
            if (legendY + 30 > chartHeight)
            {
                legendY = chartHeight - 40; // Đặt ở vị trí an toàn
            }

            // Calculate total width to center the legend
            double totalLegendWidth = (lineSeries.Count * itemSpacing) - itemSpacing + lineLength;
            double startX = marginLeft + (plotWidth - totalLegendWidth) / 2;

            for (int i = 0; i < lineSeries.Count; i++)
            {
                var series = lineSeries[i];
                double x = startX + (i * itemSpacing);

                // Legend line
                var line = new Line
                {
                    X1 = x,
                    Y1 = legendY,
                    X2 = x + lineLength,
                    Y2 = legendY,
                    Stroke = new SolidColorBrush(series.Color),
                    StrokeThickness = 3
                };
                ChartCanvas.Children.Add(line);

                // Legend text
                var text = new TextBlock
                {
                    Text = series.Name,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
                };
                Canvas.SetLeft(text, x + lineLength + 10);
                Canvas.SetTop(text, legendY - 8);
                ChartCanvas.Children.Add(text);
            }
        }

        private void OnSettingsChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                ParseLabels();
                DrawChart();
            }
        }

        private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            if (sender == sliderLineWidth)
                lblLineWidth.Text = $"{(int)sliderLineWidth.Value}px";
            else if (sender == sliderPointSize)
                lblPointSize.Text = $"{(int)sliderPointSize.Value}px";

            DrawChart();
        }

        private void ParseLabels()
        {
            var labelText = txtLabels.Text;
            labels = labelText.Split(',').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
            
            // Adjust data arrays to match label count
            foreach (var series in lineSeries)
            {
                while (series.Data.Count < labels.Count)
                    series.Data.Add(0);
                while (series.Data.Count > labels.Count)
                    series.Data.RemoveAt(series.Data.Count - 1);
            }
        }

        private void LoadPreset_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var preset = btn.Tag.ToString();

            switch (preset)
            {
                case "revenue":
                    txtTitle.Text = "Doanh thu theo tháng năm 2024";
                    txtLabels.Text = "T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12";
                    lineSeries = new List<LineSeries>
                    {
                        new LineSeries { Name = "Doanh thu 2024", Data = new List<double> { 120, 150, 180, 220, 250, 280, 300, 320, 290, 310, 340, 380 }, Color = Color.FromRgb(255, 107, 107) },
                        new LineSeries { Name = "Doanh thu 2023", Data = new List<double> { 100, 130, 160, 190, 210, 240, 260, 270, 250, 280, 300, 330 }, Color = Color.FromRgb(78, 205, 196) }
                    };
                    break;

                case "temperature":
                    txtTitle.Text = "Nhiệt độ trung bình theo tháng";
                    txtLabels.Text = "T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12";
                    lineSeries = new List<LineSeries>
                    {
                        new LineSeries { Name = "Hà Nội", Data = new List<double> { 17, 18, 22, 25, 29, 31, 32, 31, 29, 26, 22, 18 }, Color = Color.FromRgb(255, 107, 107) },
                        new LineSeries { Name = "TP.HCM", Data = new List<double> { 26, 27, 29, 30, 29, 28, 28, 28, 28, 28, 27, 26 }, Color = Color.FromRgb(78, 205, 196) },
                        new LineSeries { Name = "Đà Nẵng", Data = new List<double> { 21, 22, 24, 27, 29, 30, 30, 30, 28, 26, 24, 22 }, Color = Color.FromRgb(69, 183, 209) }
                    };
                    break;

                case "stock":
                    txtTitle.Text = "Giá cổ phiếu theo tuần";
                    txtLabels.Text = "T1, T2, T3, T4, T5, T6, T7, T8";
                    lineSeries = new List<LineSeries>
                    {
                        new LineSeries { Name = "Cổ phiếu A", Data = new List<double> { 100, 105, 102, 108, 115, 112, 118, 125 }, Color = Color.FromRgb(255, 107, 107) },
                        new LineSeries { Name = "Cổ phiếu B", Data = new List<double> { 80, 82, 85, 83, 88, 92, 90, 95 }, Color = Color.FromRgb(78, 205, 196) }
                    };
                    break;

                case "website":
                    txtTitle.Text = "Lưu lượng truy cập website";
                    txtLabels.Text = "CN, T2, T3, T4, T5, T6, T7";
                    lineSeries = new List<LineSeries>
                    {
                        new LineSeries { Name = "Tuần này", Data = new List<double> { 1200, 2500, 3200, 3800, 4200, 3500, 1800 }, Color = Color.FromRgb(255, 107, 107) },
                        new LineSeries { Name = "Tuần trước", Data = new List<double> { 1000, 2200, 2800, 3200, 3600, 3000, 1500 }, Color = Color.FromRgb(78, 205, 196) }
                    };
                    break;
            }

            ParseLabels();
            CreateLineSeriesUI();
            DrawChart();
        }

        private void UpdateChart_Click(object sender, RoutedEventArgs e)
        {
            ParseLabels();
            CreateLineSeriesUI();
            DrawChart();
            MessageBox.Show("Biểu đồ đã được cập nhật!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = "LineChart.png"
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

                    using (var stream = File.Create(dialog.FileName))
                    {
                        encoder.Save(stream);
                    }

                    MessageBox.Show("Đã lưu hình ảnh thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu hình ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = new
                {
                    title = txtTitle.Text,
                    labels = labels,
                    series = lineSeries.Select(s => new
                    {
                        name = s.Name,
                        data = s.Data,
                        color = $"#{s.Color.R:X2}{s.Color.G:X2}{s.Color.B:X2}"
                    })
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON File|*.json",
                    FileName = "chartdata.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("Đã xuất dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            InitializeDefaultData();
            txtTitle.Text = "Doanh thu theo tháng năm 2024";
            txtLabels.Text = "T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12";
            cmbLineStyle.SelectedIndex = 0;
            sliderLineWidth.Value = 3;
            sliderPointSize.Value = 5;
            chkShowGrid.IsChecked = true;
            chkShowAxes.IsChecked = true;
            chkShowPoints.IsChecked = true;
            chkShowValues.IsChecked = false;
            chkShowLegend.IsChecked = true;
            chkFillArea.IsChecked = false;
            
            CreateLineSeriesUI();
            DrawChart();
            
            MessageBox.Show("Đã reset về mặc định!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
            {
                DrawChart();
            }
        }

        private void Complete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // STEP 1: Validate data
                if (lineSeries == null || lineSeries.Count == 0)
                {
                    MessageBox.Show("❌ Chưa có dữ liệu biểu đồ!", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (labels == null || labels.Count == 0)
                {
                    MessageBox.Show("❌ Chưa có nhãn dữ liệu!", "Lỗi", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var series in lineSeries)
                {
                    if (string.IsNullOrWhiteSpace(series.Name))
                    {
                        MessageBox.Show("❌ Vui lòng nhập tên cho tất cả các đường!", "Lỗi", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // STEP 2: Save configuration
                SaveCurrentConfiguration();

                // STEP 3: Confirm and display chart
                IsConfirmed = true;

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

        /// <summary>
        /// Lưu cấu hình hiện tại vào file JSON
        /// </summary>
        private void SaveCurrentConfiguration()
        {
            try
            {
                CurrentConfiguration = new LineChartConfiguration
                {
                    ChartTitle = txtTitle.Text,
                    Labels = new List<string>(labels),
                    LineSeries = lineSeries.Select(s => new LineSeriesData
                    {
                        Name = s.Name,
                        Data = new List<double>(s.Data),
                        Color = s.Color
                    }).ToList(),
                    ShowGrid = chkShowGrid.IsChecked ?? true,
                    ShowLegend = chkShowLegend.IsChecked ?? true,
                    ShowPoints = chkShowPoints.IsChecked ?? false,
                    LineThickness = 2.0
                };

                // Save to JSON file
                string appDataPath = QASmartClass.Services.AppPaths.RootDir;
                string configDir = IOPath.Combine(appDataPath, "ChartConfigs");
                
                if (!IODirectory.Exists(configDir))
                {
                    IODirectory.CreateDirectory(configDir);
                }

                string configFile = IOPath.Combine(configDir, $"LineChart_{DateTime.Now:yyyyMMdd_HHmmss}.json");
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

        /// <summary>
        /// Thêm biểu đồ tương tác lên canvas MainDashboard
        /// </summary>
        private void AddInteractiveChartToCanvas()
        {
            if (_mainDashboard == null) return;

            // Render ChartCanvas only (without border padding) for accurate content display
            var renderBitmap = new RenderTargetBitmap(
                (int)ChartCanvas.ActualWidth,
                (int)ChartCanvas.ActualHeight,
                96, 96,
                PixelFormats.Pbgra32);

            renderBitmap.Render(ChartCanvas);

            // Create Image control with 80% size 
            var chartImage = new System.Windows.Controls.Image
            {
                Source = renderBitmap,
                Width = ChartCanvas.ActualWidth * 1.2,
                Height = ChartCanvas.ActualHeight * 1,
                Stretch = Stretch.Uniform
            };

            // Create container with action buttons
            var chartContainer = CreateInteractiveChartContainer(chartImage, renderBitmap);

            // Get main canvas
            var mainCanvas = _mainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas == null) return;

            // Position in center with smart offset to avoid overlapping
            double left = (mainCanvas.ActualWidth - chartImage.Width) / 2;
            double top = (mainCanvas.ActualHeight - chartImage.Height - 50) / 2; // -50 for button panel
            
            // Smart positioning: offset if there are existing charts
            int existingCharts = mainCanvas.Children.OfType<Grid>().Count();
            if (existingCharts > 0)
            {
                // Offset by 30px for each existing chart to cascade effect
                left += (existingCharts % 5) * 30;
                top += (existingCharts % 5) * 30;
            }
            
            Canvas.SetLeft(chartContainer, left);
            Canvas.SetTop(chartContainer, top);

            // Add to canvas
            mainCanvas.Children.Add(chartContainer);
        }

        /// <summary>
        /// Tạo container với 3 nút: Copy, Delete, Edit
        /// </summary>
        private Grid CreateInteractiveChartContainer(System.Windows.Controls.Image chartImage, RenderTargetBitmap bitmap)
        {
            // Tạo container wrapper để chứa buttons bên ngoài chart
            var outerContainer = new Grid();

            // Define 2 rows: Row 0 for buttons (auto height), Row 1 for chart
            outerContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outerContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var container = new Grid
            {
                Width = chartImage.Width,
                Height = chartImage.Height
            };
            Grid.SetRow(container, 1); // Place chart in row 1

            // Button panel (hidden by default, shown on hover) - moved outside chart area
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5),
                Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                Opacity = 0,
                Height = 50
            };
            Grid.SetRow(buttonPanel, 0); // Place buttons in row 0 (above chart)

            // Nút Phóng to
            var btnZoomIn = CreateActionButton("➕", Colors.Purple);
            btnZoomIn.Click += (s, e) => ZoomInChart(outerContainer, container, chartImage);

            // Nút Thu nhỏ
            var btnZoomOut = CreateActionButton("➖", Colors.Orange);
            btnZoomOut.Click += (s, e) => ZoomOutChart(outerContainer, container, chartImage);

            // Nút Copy
            var btnCopy = CreateActionButton("📋", Colors.Green);
            btnCopy.Click += (s, e) => CopyChart(outerContainer, chartImage, bitmap);

            // Nút Xóa
            var btnDelete = CreateActionButton("🗑️", Colors.Red);
            btnDelete.Click += (s, e) => DeleteChart(outerContainer);

            // Nút Chỉnh sửa
            var btnEdit = CreateActionButton("✏️", Colors.Blue);
            btnEdit.Click += (s, e) => EditChart(outerContainer);

            buttonPanel.Children.Add(btnZoomIn);
            buttonPanel.Children.Add(btnZoomOut);
            buttonPanel.Children.Add(btnCopy);
            buttonPanel.Children.Add(btnDelete);
            buttonPanel.Children.Add(btnEdit);

            // Chart border with image
            var chartBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Child = chartImage,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    Direction = 315,
                    ShadowDepth = 5,
                    Opacity = 0.5
                }
            };

            // Resize border (hidden by default)
            var resizeBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                BorderThickness = new Thickness(3),
                Visibility = Visibility.Collapsed
            };

            container.Children.Add(chartBorder);
            container.Children.Add(resizeBorder);

            outerContainer.Children.Add(buttonPanel);
            outerContainer.Children.Add(container);

            // Hover animations - moved to outerContainer
            outerContainer.MouseEnter += (s, e) =>
            {
                var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
                buttonPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };

            outerContainer.MouseLeave += (s, e) =>
            {
                var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
                buttonPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            };

            // Enable dragging on outerContainer (the element added to canvas)
            var mainCanvas = _mainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas != null)
            {
                EnableChartDragging(outerContainer, mainCanvas);
            }

            // Enable resizing on innerContainer
            EnableChartResizing(container, chartImage, resizeBorder);

            return outerContainer;
        }

        /// <summary>
        /// Tạo nút action với style đồng nhất
        /// </summary>
        private Button CreateActionButton(string content, Color color)
        {
            var button = new Button
            {
                Content = content,
                Width = 42,
                Height = 42,
                Margin = new Thickness(3, 0, 3, 0),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.BackgroundProperty, new SolidColorBrush(color));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            factory.SetValue(Border.PaddingProperty, new Thickness(5));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(TextElement.ForegroundProperty, Brushes.White);

            factory.AppendChild(contentPresenter);
            template.VisualTree = factory;
            button.Template = template;

            return button;
        }

        /// <summary>
        /// Chức năng Phóng to: Tăng kích thước biểu đồ 20%
        /// </summary>
        private void ZoomInChart(Grid outerContainer, Grid innerContainer, System.Windows.Controls.Image chartImage)
        {
            double newWidth = chartImage.Width * 1.2;
            double newHeight = chartImage.Height * 1.2;

            chartImage.Width = newWidth;
            chartImage.Height = newHeight;
            innerContainer.Width = newWidth;
            innerContainer.Height = newHeight;
            // outerContainer will auto-resize based on its children
        }

        /// <summary>
        /// Chức năng Thu nhỏ: Giảm kích thước biểu đồ 20%
        /// </summary>
        private void ZoomOutChart(Grid outerContainer, Grid innerContainer, System.Windows.Controls.Image chartImage)
        {
            double newWidth = chartImage.Width * 0.8;
            double newHeight = chartImage.Height * 0.8;

            // Minimum size constraint
            if (newWidth < 200 || newHeight < 150) return;

            chartImage.Width = newWidth;
            chartImage.Height = newHeight;
            innerContainer.Width = newWidth;
            innerContainer.Height = newHeight;
            // outerContainer will auto-resize based on its children
        }

        /// <summary>
        /// Chức năng Copy: Tạo bản sao của biểu đồ
        /// </summary>
        private void CopyChart(Grid outerContainer, System.Windows.Controls.Image originalImage, RenderTargetBitmap bitmap)
        {
            var mainCanvas = _mainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas == null) return;

            var newImage = new System.Windows.Controls.Image
            {
                Source = bitmap,
                Width = originalImage.Width,
                Height = originalImage.Height,
                Stretch = Stretch.Uniform
            };

            var newContainer = CreateInteractiveChartContainer(newImage, bitmap);

            double originalLeft = Canvas.GetLeft(outerContainer);
            double originalTop = Canvas.GetTop(outerContainer);
            
            Canvas.SetLeft(newContainer, originalLeft + 20);
            Canvas.SetTop(newContainer, originalTop + 20);

            mainCanvas.Children.Add(newContainer);
        }

        /// <summary>
        /// Chức năng Xóa: Xóa biểu đồ khỏi canvas
        /// </summary>
        private void DeleteChart(Grid outerContainer)
        {
            var mainCanvas = _mainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas != null)
            {
                mainCanvas.Children.Remove(outerContainer);
            }
        }

        /// <summary>
        /// Chức năng Chỉnh sửa: Mở lại form với cấu hình đã lưu và xóa biểu đồ cũ
        /// </summary>
        private void EditChart(Grid outerContainer)
        {
            if (CurrentConfiguration == null)
            {
                MessageBox.Show("❌ Không tìm thấy cấu hình biểu đồ!", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var mainCanvas = _mainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas != null)
            {
                mainCanvas.Children.Remove(outerContainer);
            }

            var editor = new Form2_9_LineChartEditor(_mainDashboard);
            editor.LoadConfiguration(CurrentConfiguration);
            editor.ShowDialog();
        }

        /// <summary>
        /// Load cấu hình đã lưu vào form
        /// </summary>
        public void LoadConfiguration(LineChartConfiguration config)
        {
            if (config == null) return;

            CurrentConfiguration = config;

            txtTitle.Text = config.ChartTitle;
            labels = new List<string>(config.Labels);
            
            lineSeries.Clear();
            foreach (var seriesData in config.LineSeries)
            {
                lineSeries.Add(new LineSeries
                {
                    Name = seriesData.Name,
                    Data = new List<double>(seriesData.Data),
                    Color = seriesData.Color
                });
            }

            chkShowGrid.IsChecked = config.ShowGrid;
            chkShowLegend.IsChecked = config.ShowLegend;
            chkShowPoints.IsChecked = config.ShowPoints;

            CreateLineSeriesUI();
            DrawChart();
        }

        /// <summary>
        /// Capture chart canvas as bitmap
        /// </summary>
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

        /// <summary>
        /// Enable drag and drop for chart container
        /// </summary>
        /// <summary>
        /// Enable dragging for chart element on canvas
        /// </summary>
        private void EnableChartDragging(UIElement chartElement, Canvas mainCanvas)
        {
            // Use local variables for each chart element to avoid conflicts
            bool isDragging = false;
            Point dragStartPoint = new Point();
            double originalLeft = 0;
            double originalTop = 0;

            chartElement.MouseLeftButtonDown += (s, e) =>
            {
                isDragging = true;
                dragStartPoint = e.GetPosition(mainCanvas);
                originalLeft = Canvas.GetLeft(chartElement);
                originalTop = Canvas.GetTop(chartElement);
                
                if (double.IsNaN(originalLeft)) originalLeft = 0;
                if (double.IsNaN(originalTop)) originalTop = 0;
                
                chartElement.CaptureMouse();
                e.Handled = true;
            };

            chartElement.MouseMove += (s, e) =>
            {
                if (isDragging && chartElement.IsMouseCaptured)
                {
                    Point currentPoint = e.GetPosition(mainCanvas);
                    double deltaX = currentPoint.X - dragStartPoint.X;
                    double deltaY = currentPoint.Y - dragStartPoint.Y;

                    double newLeft = originalLeft + deltaX;
                    double newTop = originalTop + deltaY;

                    // Get element size
                    double elementWidth = (chartElement as FrameworkElement)?.ActualWidth ?? 0;
                    double elementHeight = (chartElement as FrameworkElement)?.ActualHeight ?? 0;

                    // Constrain to canvas bounds
                    newLeft = Math.Max(0, Math.Min(newLeft, mainCanvas.ActualWidth - elementWidth));
                    newTop = Math.Max(0, Math.Min(newTop, mainCanvas.ActualHeight - elementHeight));

                    Canvas.SetLeft(chartElement, newLeft);
                    Canvas.SetTop(chartElement, newTop);
                }
            };

            chartElement.MouseLeftButtonUp += (s, e) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    chartElement.ReleaseMouseCapture();
                }
            };
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

                    // Calculate new size based on direction
                    if (resizeDirection.Contains("E")) newWidth = originalWidth + deltaX;
                    if (resizeDirection.Contains("W")) newWidth = originalWidth - deltaX;
                    if (resizeDirection.Contains("S")) newHeight = originalHeight + deltaY;
                    if (resizeDirection.Contains("N")) newHeight = originalHeight - deltaY;

                    // Apply minimum size constraints
                    newWidth = Math.Max(200, newWidth);
                    newHeight = Math.Max(150, newHeight);

                    // Update container and image size
                    container.Width = newWidth;
                    container.Height = newHeight;
                    chartImage.Width = newWidth;
                    chartImage.Height = newHeight;

                    // Adjust position for NW, N, W directions
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

            // Mouse up to stop resizing
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

        /// <summary>
        /// Get appropriate cursor for resize direction
        /// </summary>
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

    // Configuration classes
    public class LineChartConfiguration
    {
        public string ChartTitle { get; set; }
        public List<string> Labels { get; set; }
        public List<LineSeriesData> LineSeries { get; set; }
        public bool ShowGrid { get; set; }
        public bool ShowLegend { get; set; }
        public bool ShowPoints { get; set; }
        public double LineThickness { get; set; }
    }

    public class LineSeriesData
    {
        public string Name { get; set; }
        public List<double> Data { get; set; }
        public Color Color { get; set; }
    }
}

