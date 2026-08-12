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
    /// Interface for Scatter Chart service
    /// </summary>
    public interface IScatterChartService
    {
        CanvasControl CreateChart(Form2_13_ScatterChartEditor editor, double width = 900, double height = 600);
        void DrawScatterChart(CanvasControl canvas, Form2_13_ScatterChartEditor editor);
    }

    /// <summary>
    /// Service for creating and rendering scatter charts
    /// </summary>
    public class ScatterChartService : IScatterChartService
    {
        public CanvasControl CreateChart(Form2_13_ScatterChartEditor editor, double width = 900, double height = 600)
        {
            var chartContainer = new CanvasControl
            {
                Width = width,
                Height = height,
                Background = Brushes.White
            };

            DrawScatterChart(chartContainer, editor);
            return chartContainer;
        }

        public void DrawScatterChart(CanvasControl canvas, Form2_13_ScatterChartEditor editor)
        {
            double canvasWidth = canvas.Width;
            double canvasHeight = canvas.Height;
            double padding = 80;

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
                Text = "Scatter Chart",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            CanvasControl.SetLeft(titleText, padding);
            CanvasControl.SetTop(titleText, 20);
            canvas.Children.Add(titleText);

            // Draw scatter points (simplified)
            System.Diagnostics.Debug.WriteLine("📊 ScatterChartService: Chart rendered");
        }
    }
}
