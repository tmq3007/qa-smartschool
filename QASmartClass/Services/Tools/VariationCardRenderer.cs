using WpfCanvas = System.Windows.Controls.Canvas;
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
    /// Trình dựng Bảng Biến Thiên & Thẻ Toán Học chuẩn vector tương tác cho Form2_MainDashboard
    /// Thiết kế tương đồng với Thẻ Toán Học QA Calculator (khung bo viền ngọc #168b82, nền kem ngà #F8FBFB)
    /// </summary>
    public static class VariationCardRenderer
    {
        private static readonly FontFamily MathFont = new FontFamily("Cambria Math, Segoe UI, Consolas, sans-serif");
        private static readonly Brush PrimaryColor = new SolidColorBrush(Color.FromRgb(22, 139, 130)); // #168b82
        private static readonly Brush DarkText = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // Slate 800
        private static readonly Brush MutedText = new SolidColorBrush(Color.FromRgb(100, 116, 139)); // Slate 500
        private static readonly Brush TableBorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)); // Nét đen ghi đậm chuẩn bảng giáo khoa

        /// <summary>
        /// Tạo Border Thẻ Toán Học hoàn chỉnh chứa Bảng Biến Thiên và thông tin giải tích
        /// </summary>
        public static Border CreateVariationCard(VariationModel model)
        {
            var cardBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(250, 255, 255, 255)),
                BorderBrush = PrimaryColor,
                BorderThickness = new Thickness(2.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 16, 20, 16),
                MinWidth = 550,
                MaxWidth = 950,
                Tag = "CalculatorMathCard", // Đánh dấu cùng loại thẻ với QA Calculator
                DataContext = model,
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

            // 1. Header: Biểu thức hàm số & Badge loại hàm
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Vertical };
            var formulaText = new TextBlock
            {
                Text = model.FunctionFormula,
                FontFamily = MathFont,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = DarkText
            };
            var domainText = new TextBlock
            {
                Text = $"{model.DomainDescription}   |   {model.DerivativeFormula}",
                FontFamily = MathFont,
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                Foreground = MutedText,
                Margin = new Thickness(0, 3, 0, 0)
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
                Text = "BẢNG BIẾN THIÊN",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = PrimaryColor
            };
            Grid.SetColumn(badgeBorder, 1);
            headerGrid.Children.Add(badgeBorder);

            mainStack.Children.Add(headerGrid);

            // 2. Bảng Biến Thiên Trực Quan (Vector Canvas)
            var tableControl = BuildVariationTableCanvas(model);
            mainStack.Children.Add(tableControl);

            // 3. Phần kết luận tóm tắt cực trị / tính đơn điệu
            if (model.AnalysisSummaryLines != null && model.AnalysisSummaryLines.Count > 0)
            {
                var summaryBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 14, 0, 0)
                };
                var summaryStack = new StackPanel();
                foreach (var line in model.AnalysisSummaryLines)
                {
                    summaryStack.Children.Add(new TextBlock
                    {
                        Text = line,
                        FontFamily = MathFont,
                        FontSize = 13.5,
                        Foreground = DarkText,
                        Margin = new Thickness(0, 2, 0, 2)
                    });
                }
                summaryBorder.Child = summaryStack;
                mainStack.Children.Add(summaryBorder);
            }

            cardBorder.Child = mainStack;
            return cardBorder;
        }

        /// <summary>
        /// Nhân bản (Clone) toàn diện Thẻ Bảng Biến Thiên kèm đầy đủ lưới vector, mũi tên, nhãn và thuộc tính khung
        /// </summary>
        public static Border? TryCloneVariationCard(Border sourceBorder)
        {
            if (sourceBorder == null) return null;
            if (sourceBorder.DataContext is not VariationModel model) return null;

            var cloned = CreateVariationCard(model);
            cloned.Width = sourceBorder.Width;
            cloned.Height = sourceBorder.Height;
            cloned.MinWidth = sourceBorder.MinWidth;
            cloned.MaxWidth = sourceBorder.MaxWidth;
            cloned.Background = sourceBorder.Background?.Clone() ?? cloned.Background;
            cloned.BorderBrush = sourceBorder.BorderBrush?.Clone() ?? cloned.BorderBrush;
            cloned.BorderThickness = sourceBorder.BorderThickness;
            cloned.CornerRadius = sourceBorder.CornerRadius;
            cloned.Padding = sourceBorder.Padding;
            cloned.Opacity = sourceBorder.Opacity;
            if (sourceBorder.RenderTransform != null && sourceBorder.RenderTransform != Transform.Identity)
            {
                cloned.RenderTransform = sourceBorder.RenderTransform.Clone();
                cloned.RenderTransformOrigin = sourceBorder.RenderTransformOrigin;
            }
            return cloned;
        }

        /// <summary>
        /// Dựng toàn bộ lưới đồ họa vector của Bảng biến thiên (3 hàng: x, y', y)
        /// </summary>
        public static FrameworkElement BuildVariationTableCanvas(VariationModel model)
        {
            int n = model.Marks.Count;
            if (n < 2) return new WpfCanvas { Height = 50 };

            double headerColWidth = 60.0;
            double colWidth = Math.Max(110.0, 500.0 / (n - 1));
            double tableWidth = headerColWidth + (n - 1) * colWidth + 20;
            double tableHeight = 220.0;

            var canvas = new WpfCanvas
            {
                Width = tableWidth,
                Height = tableHeight,
                Background = Brushes.White,
                ClipToBounds = true
            };

            // Viền ngoài bảng
            var outerRect = new Rectangle
            {
                Width = tableWidth - 2,
                Height = tableHeight - 2,
                Stroke = TableBorderBrush,
                StrokeThickness = 1.8
            };
            System.Windows.Controls.Canvas.SetLeft(outerRect, 1);
            System.Windows.Controls.Canvas.SetTop(outerRect, 1);
            canvas.Children.Add(outerRect);

            // Đường dọc ngăn cách cột tiêu đề (x, y', y) và các cột giá trị
            var colLine = new Line
            {
                X1 = headerColWidth,
                Y1 = 1,
                X2 = headerColWidth,
                Y2 = tableHeight - 1,
                Stroke = TableBorderBrush,
                StrokeThickness = 1.8
            };
            canvas.Children.Add(colLine);

            // Các đường ngang phân chia 3 hàng:
            // Hàng 1 (x): Y = 0 đến 45
            // Hàng 2 (y'): Y = 45 đến 85
            // Hàng 3 (y): Y = 85 đến 220
            double row1Y = 45.0;
            double row2Y = 85.0;

            canvas.Children.Add(new Line { X1 = 1, Y1 = row1Y, X2 = tableWidth - 1, Y2 = row1Y, Stroke = TableBorderBrush, StrokeThickness = 1.5 });
            canvas.Children.Add(new Line { X1 = 1, Y1 = row2Y, X2 = tableWidth - 1, Y2 = row2Y, Stroke = TableBorderBrush, StrokeThickness = 1.5 });

            // Nhãn tiêu đề 3 hàng
            AddCenteredText(canvas, "x", headerColWidth / 2, row1Y / 2, 17, FontWeights.Bold, DarkText);
            AddCenteredText(canvas, "y'", headerColWidth / 2, (row1Y + row2Y) / 2, 17, FontWeights.Bold, DarkText);
            AddCenteredText(canvas, "y", headerColWidth / 2, (row2Y + tableHeight) / 2, 17, FontWeights.Bold, DarkText);

            // Tọa độ X của các mốc
            double[] markXPositions = new double[n];
            for (int i = 0; i < n; i++)
            {
                markXPositions[i] = headerColWidth + 10 + i * colWidth;
            }

            // Tính toán cao độ dòng y (hàm height)
            var finiteVals = model.Marks
                .SelectMany(m => new double?[] { m.Value, m.LeftLimit, m.RightLimit })
                .Where(v => v.HasValue && !double.IsInfinity(v.Value))
                .Select(v => v.Value)
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            double topYInRow3 = row2Y + 28;
            double botYInRow3 = tableHeight - 30;

            Func<double?, double> getYCoordinate = (val) =>
            {
                if (!val.HasValue) return (topYInRow3 + botYInRow3) / 2;
                if (double.IsPositiveInfinity(val.Value)) return topYInRow3;
                if (double.IsNegativeInfinity(val.Value)) return botYInRow3;
                if (finiteVals.Count < 2) return (topYInRow3 + botYInRow3) / 2;

                double minV = finiteVals.First();
                double maxV = finiteVals.Last();
                double ratio = (val.Value - minV) / (maxV - minV);
                return botYInRow3 - ratio * (botYInRow3 - topYInRow3);
            };

            // 1. Vẽ các khoảng (Dấu của y' và Mũi tên của y)
            for (int i = 0; i < model.Intervals.Count; i++)
            {
                var interval = model.Intervals[i];
                double xLeft = markXPositions[i];
                double xRight = markXPositions[i + 1];
                double xMid = (xLeft + xRight) / 2.0;

                // Dấu y'
                string signText = interval.Sign > 0 ? "+" : (interval.Sign < 0 ? "-" : "0");
                AddCenteredText(canvas, signText, xMid, (row1Y + row2Y) / 2, 18, FontWeights.Bold, interval.Sign > 0 ? Brushes.Red : Brushes.Blue);

                // Mũi tên biến thiên của dòng y
                var mLeft = model.Marks[i];
                var mRight = model.Marks[i + 1];

                double yStart = getYCoordinate(mLeft.RightLimit ?? mLeft.Value);
                double yEnd = getYCoordinate(mRight.LeftLimit ?? mRight.Value);

                // Vẽ mũi tên có đầu nhọn
                DrawArrow(canvas, xLeft + 16, yStart, xRight - 16, yEnd);
            }

            // 2. Vẽ các mốc Mark (Cột x, đạo hàm 0/||, và giá trị y)
            for (int i = 0; i < n; i++)
            {
                var mark = model.Marks[i];
                double xPos = markXPositions[i];

                // Dòng 1: Giá trị x
                AddCenteredText(canvas, mark.XLabel, xPos, row1Y / 2, 15, FontWeights.SemiBold, DarkText);

                // Dòng 2 & 3: Xử lý điểm gián đoạn (Nét đôi ||) hoặc điểm xác định (0)
                if (mark.Excluded && i > 0 && i < n - 1)
                {
                    // Nét đôi kéo từ dòng y' xuống hết bảng
                    var line1 = new Line { X1 = xPos - 2.5, Y1 = row1Y, X2 = xPos - 2.5, Y2 = tableHeight - 1, Stroke = TableBorderBrush, StrokeThickness = 1.4 };
                    var line2 = new Line { X1 = xPos + 2.5, Y1 = row1Y, X2 = xPos + 2.5, Y2 = tableHeight - 1, Stroke = TableBorderBrush, StrokeThickness = 1.4 };
                    canvas.Children.Add(line1);
                    canvas.Children.Add(line2);

                    // Nhãn giới hạn 2 bên tiệm cận
                    if (!string.IsNullOrEmpty(mark.LeftLimitLabel))
                    {
                        double yCoordLeft = getYCoordinate(mark.LeftLimit);
                        AddCenteredText(canvas, mark.LeftLimitLabel, xPos - 18, yCoordLeft, 13.5, FontWeights.Medium, DarkText);
                    }
                    if (!string.IsNullOrEmpty(mark.RightLimitLabel))
                    {
                        double yCoordRight = getYCoordinate(mark.RightLimit);
                        AddCenteredText(canvas, mark.RightLimitLabel, xPos + 18, yCoordRight, 13.5, FontWeights.Medium, DarkText);
                    }
                }
                else
                {
                    // Đạo hàm y' = 0
                    if (!string.IsNullOrEmpty(mark.DerivativeLabel))
                    {
                        AddCenteredText(canvas, mark.DerivativeLabel, xPos, (row1Y + row2Y) / 2, 15, FontWeights.Bold, DarkText);
                    }

                    // Giá trị dòng y
                    if (mark.Defined && !string.IsNullOrEmpty(mark.ValueLabel))
                    {
                        double yCoord = getYCoordinate(mark.Value);
                        // Đẩy nhãn lệch nhẹ lên/xuống tùy theo đỉnh hay đáy
                        bool isPeak = i > 0 && i < n - 1 && model.Intervals[i - 1].Sign > 0 && model.Intervals[i].Sign < 0;
                        double textY = isPeak ? yCoord - 14 : yCoord + 14;
                        AddCenteredText(canvas, mark.ValueLabel, xPos, textY, 14, FontWeights.Bold, DarkText);
                    }
                    else
                    {
                        // Giới hạn ở 2 đầu vô cực
                        if (i == 0 && !string.IsNullOrEmpty(mark.RightLimitLabel))
                        {
                            double yCoord = getYCoordinate(mark.RightLimit);
                            AddCenteredText(canvas, mark.RightLimitLabel, xPos + 6, yCoord, 14, FontWeights.SemiBold, DarkText);
                        }
                        else if (i == n - 1 && !string.IsNullOrEmpty(mark.LeftLimitLabel))
                        {
                            double yCoord = getYCoordinate(mark.LeftLimit);
                            AddCenteredText(canvas, mark.LeftLimitLabel, xPos - 6, yCoord, 14, FontWeights.SemiBold, DarkText);
                        }
                    }
                }
            }

            return canvas;
        }

        private static void AddCenteredText(WpfCanvas canvas, string text, double cx, double cy, double fontSize, FontWeight weight, Brush foreground)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontFamily = MathFont,
                FontSize = fontSize,
                FontWeight = weight,
                Foreground = foreground
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            System.Windows.Controls.Canvas.SetLeft(tb, cx - tb.DesiredSize.Width / 2);
            System.Windows.Controls.Canvas.SetTop(tb, cy - tb.DesiredSize.Height / 2);
            canvas.Children.Add(tb);
        }

        private static void DrawArrow(WpfCanvas canvas, double x1, double y1, double x2, double y2)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = DarkText,
                StrokeThickness = 2.0
            };
            canvas.Children.Add(line);

            // Mũi tên ở đầu (x2, y2)
            double angle = Math.Atan2(y2 - y1, x2 - x1);
            double arrowLen = 9.0;
            double arrowAngle = Math.PI / 6.0;

            var p1 = new Point(x2 - arrowLen * Math.Cos(angle - arrowAngle), y2 - arrowLen * Math.Sin(angle - arrowAngle));
            var p2 = new Point(x2 - arrowLen * Math.Cos(angle + arrowAngle), y2 - arrowLen * Math.Sin(angle + arrowAngle));

            var head = new Polygon
            {
                Fill = DarkText,
                Points = new PointCollection { new Point(x2, y2), p1, p2 }
            };
            canvas.Children.Add(head);
        }
    }
}
