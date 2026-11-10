using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using QASmartTouch.Forms;
using CanvasControl = System.Windows.Controls.Canvas;

namespace QASmartTouch.Services.Charts
{
    /// <summary>
    /// Interface for Line Chart service
    /// </summary>
    public interface ILineChartService
    {
        /// <summary>
        /// Create and render a line chart on canvas
        /// </summary>
        CanvasControl CreateChart(Form2_9_LineChartEditor editor, double width = 800, double height = 600);

        /// <summary>
        /// Create line chart with statistics panel
        /// </summary>
        (CanvasControl chart, StackPanel stats) CreateChartWithStats(Form2_9_LineChartEditor editor, double width = 800, double height = 600);

        /// <summary>
        /// Draw line chart on existing canvas
        /// </summary>
        void DrawLineChart(CanvasControl canvas, Form2_9_LineChartEditor editor);
    }

    /// <summary>
    /// Service for creating and rendering line charts
    /// Responsibilities:
    /// - Create line chart containers
    /// - Draw line chart elements (axes, grid, lines, points, legend)
    /// - Calculate data statistics
    /// - Generate stats panel for DisplayStats mode
    /// </summary>
    public class LineChartService : ILineChartService
    {
        #region Public Methods

        /// <summary>
        /// Create and render a line chart on canvas
        /// </summary>
        public CanvasControl CreateChart(Form2_9_LineChartEditor editor, double width = 800, double height = 600)
        {
            var chartContainer = new CanvasControl
            {
                Width = width,
                Height = height,
                Background = Brushes.White
            };

            DrawLineChart(chartContainer, editor);
            return chartContainer;
        }

        /// <summary>
        /// Create line chart with statistics panel
        /// </summary>
        public (CanvasControl chart, StackPanel stats) CreateChartWithStats(Form2_9_LineChartEditor editor, double width = 800, double height = 600)
        {
            const double pixelsToMm = 25.4 / 96.0;

            // Create chart
            var chartContainer = CreateChart(editor, width, height);

            // Add border
            var chartBorder = new Border
            {
                Width = width,
                Height = height,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5)
            };
            chartContainer.Children.Insert(0, chartBorder);

            // Calculate statistics
            var allDataPoints = ExtractDataPoints(editor);
            double chartWidthMm = width * pixelsToMm;
            double chartHeightMm = height * pixelsToMm;

            // Create stats panel
            var statsPanel = CreateStatsPanel(allDataPoints, chartWidthMm, chartHeightMm, width, height);

            return (chartContainer, statsPanel);
        }

        /// <summary>
        /// Draw line chart on existing canvas
        /// </summary>
        public void DrawLineChart(CanvasControl canvas, Form2_9_LineChartEditor editor)
        {
            double canvasWidth = canvas.Width;
            double canvasHeight = canvas.Height;
            double padding = 80;
            double chartWidth = canvasWidth - 2 * padding;
            double chartHeight = canvasHeight - 2 * padding;

            // Background
            var bgRect = new Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = Brushes.White
            };
            CanvasControl.SetLeft(bgRect, 0);
            CanvasControl.SetTop(bgRect, 0);
            canvas.Children.Add(bgRect);

            // Title
            var titleText = new TextBlock
            {
                Text = "Line Chart",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            CanvasControl.SetLeft(titleText, (canvasWidth - MeasureTextWidth(titleText.Text, 18, true)) / 2);
            CanvasControl.SetTop(titleText, 20);
            canvas.Children.Add(titleText);

            // Draw axes
            DrawAxes(canvas, padding, chartWidth, chartHeight, canvasHeight);

            // Draw grid lines (optional)
            // DrawGridLines(canvas, padding, chartWidth, chartHeight, canvasHeight);

            // Draw line series
            DrawLineSeries(canvas, editor, padding, chartWidth, chartHeight, canvasHeight);

            // Draw legend
            DrawLegend(canvas, editor, canvasWidth, padding);
        }

        #endregion

        #region Private Methods - Drawing

        private void DrawAxes(CanvasControl canvas, double padding, double chartWidth, double chartHeight, double canvasHeight)
        {
            // X-axis
            var xAxis = new Line
            {
                X1 = padding,
                Y1 = canvasHeight - padding,
                X2 = padding + chartWidth,
                Y2 = canvasHeight - padding,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvas.Children.Add(xAxis);

            // Y-axis
            var yAxis = new Line
            {
                X1 = padding,
                Y1 = padding,
                X2 = padding,
                Y2 = canvasHeight - padding,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvas.Children.Add(yAxis);
        }

        private void DrawLineSeries(CanvasControl canvas, Form2_9_LineChartEditor editor, double padding, double chartWidth, double chartHeight, double canvasHeight)
        {
            if (editor == null) return;

            // Access line series through reflection
            var seriesField = editor.GetType().GetField("lineSeries",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (seriesField == null) return;

            var series = seriesField.GetValue(editor) as System.Collections.IList;
            if (series == null || series.Count == 0) return;

            // Calculate scale
            var allDataPoints = ExtractDataPoints(editor);
            double minValue = allDataPoints.Any() ? allDataPoints.Min() : 0;
            double maxValue = allDataPoints.Any() ? allDataPoints.Max() : 100;
            double valueRange = maxValue - minValue;
            if (valueRange == 0) valueRange = 1;

            // Draw each series
            int seriesIndex = 0;
            foreach (var s in series)
            {
                var nameProperty = s.GetType().GetProperty("Name");
                var dataProperty = s.GetType().GetProperty("Data");
                var colorProperty = s.GetType().GetProperty("Color");

                if (dataProperty == null) continue;

                var data = dataProperty.GetValue(s) as List<double>;
                if (data == null || data.Count == 0) continue;

                var color = colorProperty?.GetValue(s) as Color? ?? Colors.Blue;
                
                DrawSingleLineSeries(canvas, data, color, minValue, valueRange, padding, chartWidth, chartHeight, canvasHeight);
                seriesIndex++;
            }
        }

        private void DrawSingleLineSeries(CanvasControl canvas, List<double> data, Color color, double minValue, double valueRange, 
            double padding, double chartWidth, double chartHeight, double canvasHeight)
        {
            if (data.Count < 2) return;

            double xStep = chartWidth / (data.Count - 1);

            for (int i = 0; i < data.Count - 1; i++)
            {
                double x1 = padding + i * xStep;
                double y1 = canvasHeight - padding - ((data[i] - minValue) / valueRange) * chartHeight;
                double x2 = padding + (i + 1) * xStep;
                double y2 = canvasHeight - padding - ((data[i + 1] - minValue) / valueRange) * chartHeight;

                var line = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2
                };
                canvas.Children.Add(line);

                // Draw point
                var point = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(color)
                };
                CanvasControl.SetLeft(point, x1 - 3);
                CanvasControl.SetTop(point, y1 - 3);
                canvas.Children.Add(point);
            }

            // Last point
            double xLast = padding + (data.Count - 1) * xStep;
            double yLast = canvasHeight - padding - ((data[data.Count - 1] - minValue) / valueRange) * chartHeight;
            var lastPoint = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(color)
            };
            CanvasControl.SetLeft(lastPoint, xLast - 3);
            CanvasControl.SetTop(lastPoint, yLast - 3);
            canvas.Children.Add(lastPoint);
        }

        private void DrawLegend(CanvasControl canvas, Form2_9_LineChartEditor editor, double canvasWidth, double padding)
        {
            if (editor == null) return;

            var seriesField = editor.GetType().GetField("lineSeries",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (seriesField == null) return;

            var series = seriesField.GetValue(editor) as System.Collections.IList;
            if (series == null || series.Count == 0) return;

            double legendX = canvasWidth - padding - 150;
            double legendY = padding;

            int index = 0;
            foreach (var s in series)
            {
                var nameProperty = s.GetType().GetProperty("Name");
                var colorProperty = s.GetType().GetProperty("Color");

                var name = nameProperty?.GetValue(s) as string ?? $"Series {index + 1}";
                var color = colorProperty?.GetValue(s) as Color? ?? Colors.Blue;

                // Legend color box
                var colorBox = new Rectangle
                {
                    Width = 20,
                    Height = 15,
                    Fill = new SolidColorBrush(color)
                };
                CanvasControl.SetLeft(colorBox, legendX);
                CanvasControl.SetTop(colorBox, legendY + index * 25);
                canvas.Children.Add(colorBox);

                // Legend text
                var legendText = new TextBlock
                {
                    Text = name,
                    FontSize = 12,
                    Foreground = Brushes.Black
                };
                CanvasControl.SetLeft(legendText, legendX + 25);
                CanvasControl.SetTop(legendText, legendY + index * 25);
                canvas.Children.Add(legendText);

                index++;
            }
        }

        #endregion

        #region Private Methods - Statistics

        private List<double> ExtractDataPoints(Form2_9_LineChartEditor editor)
        {
            var allDataPoints = new List<double>();

            if (editor == null) return allDataPoints;

            var seriesField = editor.GetType().GetField("lineSeries",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (seriesField == null) return allDataPoints;

            var series = seriesField.GetValue(editor) as System.Collections.IList;
            if (series == null) return allDataPoints;

            foreach (var s in series)
            {
                var dataProperty = s.GetType().GetProperty("Data");
                if (dataProperty != null)
                {
                    var data = dataProperty.GetValue(s) as List<double>;
                    if (data != null)
                    {
                        allDataPoints.AddRange(data);
                    }
                }
            }

            return allDataPoints;
        }

        private StackPanel CreateStatsPanel(List<double> allDataPoints, double chartWidthMm, double chartHeightMm, double width, double height)
        {
            int totalSeries = 1; // Simplified
            int totalDataPoints = allDataPoints.Count;
            double minValue = allDataPoints.Any() ? allDataPoints.Min() : 0;
            double maxValue = allDataPoints.Any() ? allDataPoints.Max() : 0;
            double avgValue = allDataPoints.Any() ? allDataPoints.Average() : 0;
            double totalValue = allDataPoints.Sum();
            double rangeValue = maxValue - minValue;

            var statsPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255))
            };

            // Title
            AddStatsTextBlock(statsPanel, "📈 THÔNG SỐ BIỂU ĐỒ ĐƯỜNG", 18, true, "#2C3E50");
            AddStatsSeparator(statsPanel);

            // Chart dimensions
            AddStatsTextBlock(statsPanel, "📐 KÍCH THƯỚC BIỂU ĐỒ", 14, true, "#34495E");
            AddStatsTextBlock(statsPanel, $"Chiều rộng: {chartWidthMm:N1} mm ({width} px)", 12, false, "#555555");
            AddStatsTextBlock(statsPanel, $"Chiều cao: {chartHeightMm:N1} mm ({height} px)", 12, false, "#555555");
            AddStatsTextBlock(statsPanel, $"Diện tích: {(chartWidthMm * chartHeightMm):N2} mm²", 12, false, "#555555");
            AddStatsSeparator(statsPanel);

            // Data statistics
            AddStatsTextBlock(statsPanel, "📊 THỐNG KÊ DỮ LIỆU", 14, true, "#34495E");
            AddStatsTextBlock(statsPanel, $"Tổng số điểm: {totalDataPoints:N0}", 12, false, "#555555");
            AddStatsTextBlock(statsPanel, $"Giá trị nhỏ nhất: {minValue:N2}", 12, false, "#E74C3C");
            AddStatsTextBlock(statsPanel, $"Giá trị lớn nhất: {maxValue:N2}", 12, false, "#27AE60");
            AddStatsTextBlock(statsPanel, $"Giá trị trung bình: {avgValue:N2}", 12, false, "#3498DB");
            AddStatsTextBlock(statsPanel, $"Khoảng biến thiên: {rangeValue:N2}", 12, false, "#F39C12");
            AddStatsSeparator(statsPanel);

            // Chart information
            AddStatsTextBlock(statsPanel, "📋 THÔNG TIN BIỂU ĐỒ", 14, true, "#34495E");
            AddStatsTextBlock(statsPanel, "Loại biểu đồ: Line Chart (Biểu đồ đường)", 12, false, "#555555");

            return statsPanel;
        }

        private void AddStatsTextBlock(StackPanel panel, string text, double fontSize, bool isBold, string colorHex)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Margin = new Thickness(10, 5, 10, 5)
            };
            panel.Children.Add(textBlock);
        }

        private void AddStatsSeparator(StackPanel panel)
        {
            var separator = new Rectangle
            {
                Height = 1,
                Fill = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(10, 10, 10, 10)
            };
            panel.Children.Add(separator);
        }

        #endregion

        #region Private Methods - Utilities

        private double MeasureTextWidth(string text, double fontSize, bool isBold)
        {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), 
                    isBold ? FontStyles.Normal : FontStyles.Normal,
                    isBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStretches.Normal),
                fontSize,
                Brushes.Black,
                96);

            return formattedText.Width;
        }

        #endregion
    }
}
