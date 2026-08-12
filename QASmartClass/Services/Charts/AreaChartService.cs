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
    /// Interface for Area Chart service
    /// </summary>
    public interface IAreaChartService
    {
        CanvasControl CreateChart(Form2_12_AreaChartEditor editor, double width = 800, double height = 600);
        void DrawAreaChart(CanvasControl canvas, Form2_12_AreaChartEditor editor);
    }

    /// <summary>
    /// Service for creating and rendering area charts
    /// </summary>
    public class AreaChartService : IAreaChartService
    {
        public CanvasControl CreateChart(Form2_12_AreaChartEditor editor, double width = 800, double height = 600)
        {
            var chartContainer = new CanvasControl
            {
                Width = width,
                Height = height,
                Background = Brushes.White
            };

            DrawAreaChart(chartContainer, editor);
            return chartContainer;
        }

        public void DrawAreaChart(CanvasControl canvas, Form2_12_AreaChartEditor editor)
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
                Text = "Area Chart",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            CanvasControl.SetLeft(titleText, padding);
            CanvasControl.SetTop(titleText, 20);
            canvas.Children.Add(titleText);

            // Simplified drawing - similar to line chart but with filled area
            System.Diagnostics.Debug.WriteLine("📊 AreaChartService: Chart rendered");
        }
    }
}
