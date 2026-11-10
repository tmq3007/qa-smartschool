using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using QASmartTouch.Forms;
using CanvasControl = System.Windows.Controls.Canvas;

namespace QASmartTouch.Services.Charts
{
    /// <summary>
    /// Interface for Pie Chart service
    /// </summary>
    public interface IPieChartService
    {
        /// <summary>
        /// Create and render a pie chart on canvas
        /// </summary>
        CanvasControl CreateChart(Form2_10_PieChartEditor editor, double width = 900, double height = 700);

        /// <summary>
        /// Create pie chart with statistics panel
        /// </summary>
        (CanvasControl chart, StackPanel stats) CreateChartWithStats(Form2_10_PieChartEditor editor, double width = 900, double height = 700);

        /// <summary>
        /// Draw pie chart on existing canvas
        /// </summary>
        void DrawPieChart(CanvasControl canvas, Form2_10_PieChartEditor editor);
    }

    /// <summary>
    /// Service for creating and rendering pie charts
    /// Responsibilities:
    /// - Create pie chart containers
    /// - Draw pie slices with colors
    /// - Draw chart legend
    /// - Calculate percentages and statistics
    /// - Generate stats panel for DisplayStats mode
    /// </summary>
    public class PieChartService : IPieChartService
    {
        #region Public Methods

        /// <summary>
        /// Create and render a pie chart on canvas
        /// </summary>
        public CanvasControl CreateChart(Form2_10_PieChartEditor editor, double width = 900, double height = 700)
        {
            var chartContainer = new CanvasControl
            {
                Width = width,
                Height = height,
                Background = Brushes.White
            };

            DrawPieChart(chartContainer, editor);
            return chartContainer;
        }

        /// <summary>
        /// Create pie chart with statistics panel
        /// </summary>
        public (CanvasControl chart, StackPanel stats) CreateChartWithStats(Form2_10_PieChartEditor editor, double width = 900, double height = 700)
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
            var dataPoints = ExtractDataPoints(editor);
            double chartWidthMm = width * pixelsToMm;
            double chartHeightMm = height * pixelsToMm;

            // Create stats panel
            var statsPanel = CreateStatsPanel(dataPoints, chartWidthMm, chartHeightMm, width, height);

            return (chartContainer, statsPanel);
        }

        /// <summary>
        /// Draw pie chart on existing canvas
        /// </summary>
        public void DrawPieChart(CanvasControl canvas, Form2_10_PieChartEditor editor)
        {
            double canvasWidth = canvas.Width;
            double canvasHeight = canvas.Height;
            double centerX = canvasWidth * 0.35; // Left-biased for legend space
            double centerY = canvasHeight / 2;
            double radius = Math.Min(canvasWidth, canvasHeight) * 0.28;

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
                Text = "Pie Chart",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            CanvasControl.SetLeft(titleText, (canvasWidth - MeasureTextWidth(titleText.Text, 18, true)) / 2);
            CanvasControl.SetTop(titleText, 20);
            canvas.Children.Add(titleText);

            // Get data
            var dataItems = ExtractDataItems(editor);
            if (dataItems == null || dataItems.Count == 0) return;

            double total = dataItems.Sum(item => item.value);
            if (total <= 0) return;

            // Draw pie slices
            double startAngle = 0;
            for (int i = 0; i < dataItems.Count; i++)
            {
                var (label, value, color) = dataItems[i];
                double percentage = (value / total) * 100;
                double sweepAngle = (value / total) * 360;

                // Draw slice
                DrawPieSlice(canvas, centerX, centerY, radius, startAngle, sweepAngle, color);

                startAngle += sweepAngle;
            }

            // Draw legend
            DrawPieChartLegend(canvas, dataItems, total, canvasWidth, canvasHeight);
        }

        #endregion

        #region Private Methods - Drawing

        private void DrawPieSlice(CanvasControl canvas, double centerX, double centerY, double radius,
                                  double startAngle, double sweepAngle, Color color)
        {
            if (sweepAngle <= 0) return;

            double startRad = (startAngle - 90) * Math.PI / 180;
            double endRad = (startAngle + sweepAngle - 90) * Math.PI / 180;

            Point start = new Point(centerX + radius * Math.Cos(startRad), centerY + radius * Math.Sin(startRad));
            Point end = new Point(centerX + radius * Math.Cos(endRad), centerY + radius * Math.Sin(endRad));

            bool isLargeArc = sweepAngle > 180;

            var path = new Path
            {
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };

            var figure = new PathFigure { StartPoint = new Point(centerX, centerY) };
            figure.Segments.Add(new LineSegment(start, true));
            figure.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });
            figure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            path.Data = geometry;

            canvas.Children.Add(path);
        }

        private void DrawPieChartLegend(CanvasControl canvas, List<(string label, double value, Color color)> dataItems, 
            double total, double canvasWidth, double canvasHeight)
        {
            double legendX = canvasWidth * 0.65;
            double legendY = 100;
            double lineHeight = 35;

            for (int i = 0; i < dataItems.Count; i++)
            {
                var (label, value, color) = dataItems[i];
                double percentage = (value / total) * 100;

                // Color box
                var colorBox = new Rectangle
                {
                    Width = 25,
                    Height = 20,
                    Fill = new SolidColorBrush(color),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                CanvasControl.SetLeft(colorBox, legendX);
                CanvasControl.SetTop(colorBox, legendY + i * lineHeight);
                canvas.Children.Add(colorBox);

                // Label text
                var legendText = new TextBlock
                {
                    Text = $"{label}: {value:N0} ({percentage:N1}%)",
                    FontSize = 14,
                    Foreground = Brushes.Black
                };
                CanvasControl.SetLeft(legendText, legendX + 35);
                CanvasControl.SetTop(legendText, legendY + i * lineHeight);
                canvas.Children.Add(legendText);
            }
        }

        #endregion

        #region Private Methods - Data Extraction

        private List<(string label, double value, Color color)> ExtractDataItems(Form2_10_PieChartEditor editor)
        {
            var dataItems = new List<(string label, double value, Color color)>();

            if (editor == null) return dataItems;

            // Access pie data through reflection
            var dataField = editor.GetType().GetField("pieData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (dataField == null) return dataItems;

            var data = dataField.GetValue(editor) as System.Collections.IList;
            if (data == null) return dataItems;

            foreach (var item in data)
            {
                var labelProperty = item.GetType().GetProperty("Label");
                var valueProperty = item.GetType().GetProperty("Value");
                var colorProperty = item.GetType().GetProperty("Color");

                var label = labelProperty?.GetValue(item) as string ?? "Unknown";
                var value = valueProperty?.GetValue(item) as double? ?? 0;
                var color = colorProperty?.GetValue(item) as Color? ?? Colors.Gray;

                dataItems.Add((label, value, color));
            }

            return dataItems;
        }

        private List<double> ExtractDataPoints(Form2_10_PieChartEditor editor)
        {
            var dataItems = ExtractDataItems(editor);
            return dataItems.Select(item => item.value).ToList();
        }

        #endregion

        #region Private Methods - Statistics

        private StackPanel CreateStatsPanel(List<double> dataPoints, double chartWidthMm, double chartHeightMm, double width, double height)
        {
            int totalSegments = dataPoints.Count;
            double totalValue = dataPoints.Sum();
            double minValue = dataPoints.Any() ? dataPoints.Min() : 0;
            double maxValue = dataPoints.Any() ? dataPoints.Max() : 0;
            double avgValue = dataPoints.Any() ? dataPoints.Average() : 0;

            var statsPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255))
            };

            // Title
            AddStatsTextBlock(statsPanel, "🥧 THÔNG SỐ BIỂU ĐỒ TRÒN", 18, true, "#2C3E50");
            AddStatsSeparator(statsPanel);

            // Chart dimensions
            AddStatsTextBlock(statsPanel, "📐 KÍCH THƯỚC BIỂU ĐỒ", 14, true, "#34495E");
            AddStatsTextBlock(statsPanel, $"Chiều rộng: {chartWidthMm:N1} mm ({width} px)", 12, false, "#555555");
            AddStatsTextBlock(statsPanel, $"Chiều cao: {chartHeightMm:N1} mm ({height} px)", 12, false, "#555555");
            AddStatsSeparator(statsPanel);

            // Data statistics
            AddStatsTextBlock(statsPanel, "📊 THỐNG KÊ DỮ LIỆU", 14, true, "#34495E");
            AddStatsTextBlock(statsPanel, $"Số phần: {totalSegments:N0}", 12, false, "#555555");
            AddStatsTextBlock(statsPanel, $"Tổng giá trị: {totalValue:N2}", 12, false, "#9B59B6");
            AddStatsTextBlock(statsPanel, $"Giá trị nhỏ nhất: {minValue:N2}", 12, false, "#E74C3C");
            AddStatsTextBlock(statsPanel, $"Giá trị lớn nhất: {maxValue:N2}", 12, false, "#27AE60");
            AddStatsTextBlock(statsPanel, $"Giá trị trung bình: {avgValue:N2}", 12, false, "#3498DB");
            AddStatsSeparator(statsPanel);

            // Chart information
            AddStatsTextBlock(statsPanel, "📋 THÔNG TIN BIỂU ĐỒ", 14, true, "#34495E");
            AddStatsTextBlock(statsPanel, "Loại biểu đồ: Pie Chart (Biểu đồ tròn)", 12, false, "#555555");

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
                    FontStyles.Normal,
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
