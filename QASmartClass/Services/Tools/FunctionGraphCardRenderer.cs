using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace QASmartClass.Services.Tools
{
    /// <summary>
    /// Trình vẽ Đồ Thị Hàm Số 2D độc lập trực tiếp lên Thẻ Toán Học (Math Card)
    /// Hệ trục tọa độ Oxy chuẩn giáo khoa Việt Nam (O, x, y, các vạch số, lưới tọa độ, đường cong hàm số)
    /// Tự động chèn lên bảng vẽ với cơ chế giống hệt Bảng biến thiên và QA Calculator
    /// </summary>
    public static class FunctionGraphCardRenderer
    {
        private static readonly FontFamily MathFont = new FontFamily("Cambria Math, Segoe UI, Consolas, sans-serif");
        private static readonly Brush PrimaryColor = new SolidColorBrush(Color.FromRgb(22, 139, 130)); // #168b82
        private static readonly Brush DarkText = new SolidColorBrush(Color.FromRgb(30, 41, 59));
        private static readonly Brush MutedText = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private static readonly Brush AxisBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));
        private static readonly Brush GridBrush = new SolidColorBrush(Color.FromArgb(50, 148, 163, 184)); // Lưới mờ
        private static readonly Brush CurveBrush = new SolidColorBrush(Color.FromRgb(22, 101, 192)); // Xanh đậm chuyên nghiệp

        /// <summary>
        /// Tạo thẻ đồ thị hàm số hoàn chỉnh tương tự Thẻ Bảng biến thiên
        /// </summary>
        public static Border CreateFunctionGraphCard(string expression, VariationModel? model = null) => CreateGraphCard(expression, model);

        public static Border CreateGraphCard(string expression, VariationModel? model = null)
        {
            if (model == null)
            {
                try { model = VariationAnalysisEngine.Analyze(expression); }
                catch { model = new VariationModel { FunctionFormula = "y = " + expression }; }
            }

            var cardBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(252, 255, 255, 255)),
                BorderBrush = PrimaryColor,
                BorderThickness = new Thickness(2.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18, 14, 18, 14),
                MinWidth = 550,
                MaxWidth = 850,
                Tag = "CalculatorMathCard",
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 4,
                    BlurRadius = 12,
                    Opacity = 0.22
                }
            };

            var mainStack = new StackPanel { Orientation = Orientation.Vertical };

            // 1. Header: Công thức hàm số và Badge ĐỒ THỊ HÀM SỐ
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Vertical };
            string displayFormula = model.FunctionFormula;
            if (string.IsNullOrWhiteSpace(displayFormula) || displayFormula.Contains("(("))
            {
                string cleanF = VariationAnalysisEngine.CleanExpression(expression);
                displayFormula = $"y = {cleanF}";
            }
            var formulaText = new TextBlock
            {
                Text = displayFormula,
                FontFamily = MathFont,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = DarkText
            };
            var domainText = new TextBlock
            {
                Text = $"{model.DomainDescription}   |   {model.DerivativeFormula}",
                FontFamily = MathFont,
                FontSize = 13.5,
                FontWeight = FontWeights.Normal,
                Foreground = MutedText,
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleStack.Children.Add(formulaText);
            titleStack.Children.Add(domainText);
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            var badgeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 22, 139, 130)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Top
            };
            badgeBorder.Child = new TextBlock
            {
                Text = "ĐỒ THỊ HÀM SỐ",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = PrimaryColor
            };
            Grid.SetColumn(badgeBorder, 1);
            headerGrid.Children.Add(badgeBorder);

            mainStack.Children.Add(headerGrid);

            // 2. Khung vẽ Đồ thị Oxy Vector
            var graphCanvas = BuildCoordinatePlotCanvas(expression, model);
            mainStack.Children.Add(graphCanvas);

            // 3. Tóm tắt đặc trưng hàm số
            if (model.AnalysisSummaryLines != null && model.AnalysisSummaryLines.Count > 0)
            {
                var summaryBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 10, 0, 0)
                };
                var summaryStack = new StackPanel();
                foreach (var line in model.AnalysisSummaryLines.Take(2))
                {
                    summaryStack.Children.Add(new TextBlock
                    {
                        Text = line,
                        FontFamily = MathFont,
                        FontSize = 13,
                        Foreground = DarkText,
                        Margin = new Thickness(0, 1, 0, 1)
                    });
                }
                summaryBorder.Child = summaryStack;
                mainStack.Children.Add(summaryBorder);
            }

            cardBorder.Child = mainStack;
            return cardBorder;
        }

        /// <summary>
        /// Dựng hệ trục Oxy và vẽ đường cong hàm số bằng vector Canvas
        /// </summary>
        public static FrameworkElement BuildCoordinatePlotCanvas(string expression, VariationModel model)
        {
            double width = 580;
            double height = 400;

            var canvas = new System.Windows.Controls.Canvas
            {
                Width = width,
                Height = height,
                Background = Brushes.White,
                ClipToBounds = true
            };

            // Khung viền ngoài
            var borderRect = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                StrokeThickness = 1.5
            };
            canvas.Children.Add(borderRect);

            // Tự động điều chỉnh khoảng [xMin, xMax], [yMin, yMax] dựa theo cực trị của hàm số
            double xMin = -6, xMax = 6;
            double yMin = -5, yMax = 5;

            var finiteX = model.Marks.Where(m => m.X.HasValue && !double.IsInfinity(m.X.Value)).Select(m => m.X.Value).ToList();
            var finiteY = model.Marks.Where(m => m.Value.HasValue && !double.IsInfinity(m.Value.Value)).Select(m => m.Value.Value).ToList();

            if (finiteX.Count > 0)
            {
                double minX = finiteX.Min();
                double maxX = finiteX.Max();
                double spanX = Math.Max(4, (maxX - minX) * 1.6);
                double midX = (minX + maxX) / 2;
                xMin = Math.Floor(midX - spanX / 2);
                xMax = Math.Ceiling(midX + spanX / 2);
            }

            if (finiteY.Count > 0)
            {
                double minY = finiteY.Min();
                double maxY = finiteY.Max();
                double spanY = Math.Max(4, (maxY - minY) * 1.6);
                double midY = (minY + maxY) / 2;
                yMin = Math.Floor(midY - spanY / 2);
                yMax = Math.Ceiling(midY + spanY / 2);
            }

            // Đảm bảo chứa được gốc tọa độ (0, 0)
            xMin = Math.Min(xMin, -3);
            xMax = Math.Max(xMax, 3);
            yMin = Math.Min(yMin, -3);
            yMax = Math.Max(yMax, 3);

            Func<double, double> mapX = (x) => (x - xMin) / (xMax - xMin) * width;
            Func<double, double> mapY = (y) => height - (y - yMin) / (yMax - yMin) * height;

            double origX = mapX(0);
            double origY = mapY(0);

            // 1. Vẽ lưới tọa độ ô vuông
            int stepX = (xMax - xMin) > 15 ? 2 : 1;
            int stepY = (yMax - yMin) > 15 ? 2 : 1;

            for (int xVal = (int)Math.Ceiling(xMin); xVal <= (int)Math.Floor(xMax); xVal += stepX)
            {
                if (xVal == 0) continue;
                double px = mapX(xVal);
                canvas.Children.Add(new Line { X1 = px, Y1 = 0, X2 = px, Y2 = height, Stroke = GridBrush, StrokeThickness = 1 });
            }

            for (int yVal = (int)Math.Ceiling(yMin); yVal <= (int)Math.Floor(yMax); yVal += stepY)
            {
                if (yVal == 0) continue;
                double py = mapY(yVal);
                canvas.Children.Add(new Line { X1 = 0, Y1 = py, X2 = width, Y2 = py, Stroke = GridBrush, StrokeThickness = 1 });
            }

            // 2. Vẽ 2 trục tọa độ Ox và Oy có mũi tên
            // Trục Ox
            canvas.Children.Add(new Line { X1 = 0, Y1 = origY, X2 = width, Y2 = origY, Stroke = AxisBrush, StrokeThickness = 1.8 });
            DrawArrowHead(canvas, width, origY, 0); // Mũi tên hướng sang phải
            AddText(canvas, "x", width - 16, origY + 6, 14, FontWeights.Bold, DarkText);

            // Trục Oy
            canvas.Children.Add(new Line { X1 = origX, Y1 = height, X2 = origX, Y2 = 0, Stroke = AxisBrush, StrokeThickness = 1.8 });
            DrawArrowHead(canvas, origX, 0, -Math.PI / 2); // Mũi tên hướng lên trên
            AddText(canvas, "y", origX + 8, 4, 14, FontWeights.Bold, DarkText);

            // Gốc tọa độ O
            AddText(canvas, "O", origX - 14, origY + 4, 13.5, FontWeights.SemiBold, DarkText);

            // Đánh số các vạch đơn vị
            for (int xVal = (int)Math.Ceiling(xMin); xVal <= (int)Math.Floor(xMax); xVal += stepX)
            {
                if (xVal == 0) continue;
                double px = mapX(xVal);
                canvas.Children.Add(new Line { X1 = px, Y1 = origY - 3, X2 = px, Y2 = origY + 3, Stroke = AxisBrush, StrokeThickness = 1.4 });
                AddText(canvas, xVal.ToString(), px - 5, origY + 5, 11.5, FontWeights.Normal, DarkText);
            }

            for (int yVal = (int)Math.Ceiling(yMin); yVal <= (int)Math.Floor(yMax); yVal += stepY)
            {
                if (yVal == 0) continue;
                double py = mapY(yVal);
                canvas.Children.Add(new Line { X1 = origX - 3, Y1 = py, X2 = origX + 3, Y2 = py, Stroke = AxisBrush, StrokeThickness = 1.4 });
                AddText(canvas, yVal.ToString(), origX - 16, py - 7, 11.5, FontWeights.Normal, DarkText);
            }

            // 3. Hàm tính Y từ biểu thức toán học
            Func<double, double?> calcFunctionY = (x) =>
            {
                try
                {
                    string cleaned = VariationAnalysisEngine.CleanExpression(expression);
                    var frac = VariationAnalysisEngine.SplitFraction(cleaned);
                    if (frac != null)
                    {
                        try
                        {
                            double[] numP = VariationAnalysisEngine.ParsePolynomial(frac.Item1);
                            double[] denP = VariationAnalysisEngine.ParsePolynomial(frac.Item2);
                            double denVal = VariationAnalysisEngine.EvaluatePoly(denP, x);
                            if (Math.Abs(denVal) < 1e-4) return null; // Tiệm cận đứng
                            return VariationAnalysisEngine.EvaluatePoly(numP, x) / denVal;
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            double[] p = VariationAnalysisEngine.ParsePolynomial(cleaned);
                            if (p.Length > 0 && !p.All(c => c == 0))
                            {
                                return VariationAnalysisEngine.EvaluatePoly(p, x);
                            }
                        }
                        catch { }
                    }

                    // Fallback sang bộ tính toán đại số tổng quát
                    return VariationAnalysisEngine.EvaluateMath(cleaned, x);
                }
                catch { return null; }
            };

            // 4. Vẽ đường cong đồ thị (Polyline)
            int numSamples = 600;
            double dx = (xMax - xMin) / numSamples;
            var currentPoints = new PointCollection();

            for (int i = 0; i <= numSamples; i++)
            {
                double x = xMin + i * dx;
                double? y = calcFunctionY(x);

                if (y.HasValue && !double.IsNaN(y.Value) && !double.IsInfinity(y.Value) && y.Value >= yMin - 15 && y.Value <= yMax + 15)
                {
                    currentPoints.Add(new Point(mapX(x), mapY(y.Value)));
                }
                else
                {
                    if (currentPoints.Count > 1)
                    {
                        canvas.Children.Add(new Polyline { Points = currentPoints, Stroke = CurveBrush, StrokeThickness = 2.4 });
                        currentPoints = new PointCollection();
                    }
                }
            }

            if (currentPoints.Count > 1)
            {
                canvas.Children.Add(new Polyline { Points = currentPoints, Stroke = CurveBrush, StrokeThickness = 2.4 });
            }

            // 5. Đánh dấu các điểm cực trị trên đồ thị
            foreach (var m in model.Marks)
            {
                if (m.Defined && m.X.HasValue && m.Value.HasValue)
                {
                    double cx = mapX(m.X.Value);
                    double cy = mapY(m.Value.Value);

                    if (cx >= 0 && cx <= width && cy >= 0 && cy <= height)
                    {
                        // Điểm tròn đỏ nhỏ
                        var dot = new Ellipse { Width = 7, Height = 7, Fill = Brushes.Red };
                        System.Windows.Controls.Canvas.SetLeft(dot, cx - 3.5);
                        System.Windows.Controls.Canvas.SetTop(dot, cy - 3.5);
                        canvas.Children.Add(dot);

                        // Đường gióng đứt nét về 2 trục
                        var dashX = new Line { X1 = cx, Y1 = cy, X2 = cx, Y2 = origY, Stroke = Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 2, 2 } };
                        var dashY = new Line { X1 = cx, Y1 = cy, X2 = origX, Y2 = cy, Stroke = Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 2, 2 } };
                        canvas.Children.Add(dashX);
                        canvas.Children.Add(dashY);
                    }
                }
            }

            return canvas;
        }

        private static void AddText(System.Windows.Controls.Canvas canvas, string text, double x, double y, double size, FontWeight weight, Brush brush)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontFamily = MathFont,
                FontSize = size,
                FontWeight = weight,
                Foreground = brush
            };
            System.Windows.Controls.Canvas.SetLeft(tb, x);
            System.Windows.Controls.Canvas.SetTop(tb, y);
            canvas.Children.Add(tb);
        }

        private static void DrawArrowHead(System.Windows.Controls.Canvas canvas, double tipX, double tipY, double angle)
        {
            double len = 8;
            double spread = Math.PI / 6;

            var p1 = new Point(tipX - len * Math.Cos(angle - spread), tipY - len * Math.Sin(angle - spread));
            var p2 = new Point(tipX - len * Math.Cos(angle + spread), tipY - len * Math.Sin(angle + spread));

            var arrow = new Polygon
            {
                Fill = AxisBrush,
                Points = new PointCollection { new Point(tipX, tipY), p1, p2 }
            };
            canvas.Children.Add(arrow);
        }
    }
}

