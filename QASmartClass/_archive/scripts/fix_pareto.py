import os

filepath = r'd:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\LearningTools\Views\Workplace\ParetoTool.xaml.cs'

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

# I will replace the block from `// Draw Axes` to `// Draw Points` with the new logic.

old_logic = """            // Draw Axes
            var axisStroke = new SolidColorBrush(Color.FromRgb(189, 189, 189));
            chartCanvas.Children.Add(new Line { X1 = padding, Y1 = padding + chartH, X2 = padding + chartW, Y2 = padding + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // X
            chartCanvas.Children.Add(new Line { X1 = padding, Y1 = padding, X2 = padding, Y2 = padding + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // Y1
            chartCanvas.Children.Add(new Line { X1 = padding + chartW, Y1 = padding, X2 = padding + chartW, Y2 = padding + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // Y2

            // Draw 80% Line
            double y80 = padding + chartH - (80.0 / 100.0) * chartH;
            var line80 = new Line
            {
                X1 = padding, Y1 = y80, X2 = padding + chartW, Y2 = y80,
                Stroke = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                StrokeThickness = 1, StrokeDashArray = new DoubleCollection(new[] { 4.0, 4.0 })
            };
            chartCanvas.Children.Add(line80);

            var lbl80 = new TextBlock { Text = "80%", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)) };
            Canvas.SetLeft(lbl80, padding + chartW + 5);
            Canvas.SetTop(lbl80, y80 - 6);
            chartCanvas.Children.Add(lbl80);

            // Draw Bars & Line Points
            var points = new PointCollection();
            points.Add(new Point(padding, padding + chartH)); // start from 0

            var barColor = new SolidColorBrush(Color.FromRgb(128, 203, 196)); // Teal light
            var lineColor = new SolidColorBrush(Color.FromRgb(0, 121, 107)); // Teal dark

            for (int i = 0; i < n; i++)
            {
                var item = _currentData[i];
                double xCenter = padding + (i + 0.5) * spacing;
                
                // Bar
                double barH = (item.Value / maxVal) * chartH;
                var rect = new Rectangle
                {
                    Width = colWidth, Height = barH, Fill = barColor,
                    ToolTip = $"{item.Category}: {item.Value} ({item.CumPercent:F1}%)"
                };
                Canvas.SetLeft(rect, xCenter - colWidth / 2);
                Canvas.SetTop(rect, padding + chartH - barH);
                chartCanvas.Children.Add(rect);

                // Point for Line
                double pointY = padding + chartH - (item.CumPercent / 100.0) * chartH;
                points.Add(new Point(xCenter, pointY));

                // Label X
                var lblX = new TextBlock
                {
                    Text = item.Category, FontSize = 10, MaxWidth = spacing - 2, TextTrimming = TextTrimming.CharacterEllipsis,
                    TextAlignment = TextAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97))
                };
                lblX.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(lblX, xCenter - lblX.DesiredSize.Width / 2);
                Canvas.SetTop(lblX, padding + chartH + 5);
                chartCanvas.Children.Add(lblX);
            }

            // Draw Line
            var polyline = new Polyline
            {
                Points = points, Stroke = lineColor, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round
            };
            chartCanvas.Children.Add(polyline);

            // Draw Points
            foreach (var pt in points.Skip(1)) // skip 0
            {
                var ell = new Ellipse { Width = 6, Height = 6, Fill = lineColor };
                Canvas.SetLeft(ell, pt.X - 3);
                Canvas.SetTop(ell, pt.Y - 3);
                chartCanvas.Children.Add(ell);
            }"""

new_logic = """            // Draw Axes
            var axisStroke = new SolidColorBrush(Color.FromRgb(189, 189, 189));
            chartCanvas.Children.Add(new Line { X1 = padding, Y1 = padding + chartH, X2 = padding + chartW, Y2 = padding + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // X
            chartCanvas.Children.Add(new Line { X1 = padding, Y1 = padding, X2 = padding, Y2 = padding + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // Y1
            chartCanvas.Children.Add(new Line { X1 = padding + chartW, Y1 = padding, X2 = padding + chartW, Y2 = padding + chartH, Stroke = axisStroke, StrokeThickness = 1 }); // Y2

            // Draw Gridlines and Ticks
            int ticks = 5;
            for (int i = 0; i <= ticks; i++)
            {
                double tickY = padding + chartH - (i * chartH / ticks);
                double tickVal = i * maxVal / ticks;
                double tickPct = i * 100.0 / ticks;

                // Horizontal grid line
                if (i > 0)
                {
                    chartCanvas.Children.Add(new Line
                    {
                        X1 = padding, Y1 = tickY, X2 = padding + chartW, Y2 = tickY,
                        Stroke = new SolidColorBrush(Color.FromRgb(240, 240, 240)), StrokeThickness = 1
                    });
                }

                // Y1 Tick (Frequency)
                chartCanvas.Children.Add(new Line { X1 = padding - 4, Y1 = tickY, X2 = padding, Y2 = tickY, Stroke = axisStroke, StrokeThickness = 1 });
                var lblTick1 = new TextBlock { Text = tickVal.ToString("F0"), FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)) };
                lblTick1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(lblTick1, padding - lblTick1.DesiredSize.Width - 6);
                Canvas.SetTop(lblTick1, tickY - 7);
                chartCanvas.Children.Add(lblTick1);

                // Y2 Tick (Percentage)
                chartCanvas.Children.Add(new Line { X1 = padding + chartW, Y1 = tickY, X2 = padding + chartW + 4, Y2 = tickY, Stroke = axisStroke, StrokeThickness = 1 });
                var lblTick2 = new TextBlock { Text = $"{tickPct:F0}%", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)) };
                lblTick2.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(lblTick2, padding + chartW + 6);
                Canvas.SetTop(lblTick2, tickY - 7);
                chartCanvas.Children.Add(lblTick2);
            }

            // Draw 80% Line
            double y80 = padding + chartH - (80.0 / 100.0) * chartH;
            var line80 = new Line
            {
                X1 = padding, Y1 = y80, X2 = padding + chartW, Y2 = y80,
                Stroke = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                StrokeThickness = 1, StrokeDashArray = new DoubleCollection(new[] { 4.0, 4.0 })
            };
            chartCanvas.Children.Add(line80);

            var lbl80 = new TextBlock { Text = "80%", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)) };
            Canvas.SetLeft(lbl80, padding + chartW + 35);
            Canvas.SetTop(lbl80, y80 - 6);
            chartCanvas.Children.Add(lbl80);

            // Draw Bars & Line Points
            var points = new PointCollection();
            points.Add(new Point(padding, padding + chartH)); // start from 0

            var barColor = new SolidColorBrush(Color.FromRgb(128, 203, 196)); // Teal light
            var lineColor = new SolidColorBrush(Color.FromRgb(0, 121, 107)); // Teal dark

            for (int i = 0; i < n; i++)
            {
                var item = _currentData[i];
                double xCenter = padding + (i + 0.5) * spacing;
                
                // Bar
                double barH = (item.Value / maxVal) * chartH;
                var rect = new Rectangle
                {
                    Width = colWidth, Height = barH, Fill = barColor,
                    ToolTip = $"{item.Category}: {item.Value} ({item.CumPercent:F1}%)"
                };
                Canvas.SetLeft(rect, xCenter - colWidth / 2);
                Canvas.SetTop(rect, padding + chartH - barH);
                chartCanvas.Children.Add(rect);

                // Label Value on Bar
                var lblVal = new TextBlock
                {
                    Text = item.Value.ToString(), FontSize = 11, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 121, 107))
                };
                lblVal.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(lblVal, xCenter - lblVal.DesiredSize.Width / 2);
                Canvas.SetTop(lblVal, padding + chartH - barH - 16);
                chartCanvas.Children.Add(lblVal);

                // Point for Line
                double pointY = padding + chartH - (item.CumPercent / 100.0) * chartH;
                points.Add(new Point(xCenter, pointY));

                // Label X
                var lblX = new TextBlock
                {
                    Text = item.Category, FontSize = 10, MaxWidth = spacing - 2, TextTrimming = TextTrimming.CharacterEllipsis,
                    TextAlignment = TextAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97))
                };
                lblX.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(lblX, xCenter - lblX.DesiredSize.Width / 2);
                Canvas.SetTop(lblX, padding + chartH + 5);
                chartCanvas.Children.Add(lblX);
            }

            // Draw Line
            var polyline = new Polyline
            {
                Points = points, Stroke = lineColor, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round
            };
            chartCanvas.Children.Add(polyline);

            // Draw Points
            int pointIdx = 0;
            foreach (var pt in points.Skip(1)) // skip 0
            {
                var ell = new Ellipse { Width = 6, Height = 6, Fill = lineColor };
                Canvas.SetLeft(ell, pt.X - 3);
                Canvas.SetTop(ell, pt.Y - 3);
                chartCanvas.Children.Add(ell);

                var item = _currentData[pointIdx];
                var ptLabel = new TextBlock
                {
                    Text = $"{item.CumPercent:F0}%", FontSize = 10, Foreground = lineColor, FontWeight = FontWeights.Medium
                };
                ptLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(ptLabel, pt.X - ptLabel.DesiredSize.Width / 2);
                Canvas.SetTop(ptLabel, pt.Y - 16);
                chartCanvas.Children.Add(ptLabel);

                pointIdx++;
            }"""

if old_logic in content:
    content = content.replace(old_logic, new_logic)
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Fixed ParetoTool!")
else:
    print("Could not find the block to replace.")
