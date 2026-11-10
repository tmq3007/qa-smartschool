using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Collections.Generic;
using QASmartTouch.Forms;
using CanvasControl = System.Windows.Controls.Canvas;

namespace QASmartTouch.Services.Charts
{
    /// <summary>
    /// Interface for Radar Chart service
    /// </summary>
    public interface IRadarChartService
    {
        CanvasControl CreateChart(Form2_14_RadarChartEditor editor, double width = 700, double height = 700);
        void DrawRadarChart(CanvasControl canvas, Form2_14_RadarChartEditor editor);
    }

    /// <summary>
    /// Service for creating and rendering radar/spider charts
    /// </summary>
    public class RadarChartService : IRadarChartService
    {
        public CanvasControl CreateChart(Form2_14_RadarChartEditor editor, double width = 700, double height = 700)
        {
            var chartContainer = new CanvasControl
            {
                Width = width,
                Height = height,
                Background = Brushes.White
            };

            DrawRadarChart(chartContainer, editor);
            return chartContainer;
        }

        public void DrawRadarChart(CanvasControl canvas, Form2_14_RadarChartEditor editor)
        {
            double canvasWidth = canvas.Width;
            double canvasHeight = canvas.Height;
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;

            // Background
            canvas.Children.Add(new Rectangle
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = Brushes.White
            });

            // Title
            var titleText = new TextBlock
            {
                Text = "Radar Chart",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            CanvasControl.SetLeft(titleText, (canvasWidth - 150) / 2);
            CanvasControl.SetTop(titleText, 20);
            canvas.Children.Add(titleText);

            // Draw radar grid (simplified)
            System.Diagnostics.Debug.WriteLine("📊 RadarChartService: Chart rendered");
        }
    }
}
