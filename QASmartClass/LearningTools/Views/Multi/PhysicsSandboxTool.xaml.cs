using System;
using System.Collections.Generic;
using System.Linq;
using QASmartClass.LearningTools.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Multi
{
    using Math = System.Math;

    /// <summary>
    /// Phòng Thí Nghiệm Vật Lý ảo — Module Quang Học.
    /// Cho phép đặt nguồn sáng Laser, Gương phẳng, Thấu kính hội tụ, Lăng kính
    /// lên mặt phẳng 2D và tự động vẽ tia sáng (Raycasting) kèm phản xạ/khúc xạ.
    /// </summary>
    public partial class PhysicsSandboxTool : BaseToolControl, IDisposable
    {
        // ═══════════════════════════════════════════════════════════
        //  DATA MODEL
        // ═══════════════════════════════════════════════════════════

        private enum ObjectType { Laser, Mirror, Lens, Prism }

        private class PhysicsObject
        {
            public ObjectType Type { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Angle { get; set; } // degrees (for mirror/laser direction)
            public FrameworkElement? Visual { get; set; }
        }

        private readonly List<PhysicsObject> _objects = new();
        private string _selectedTool = ""; // "" = none, "laser", "mirror", "lens", "prism"
        private PhysicsObject? _selectedObject;
        private readonly Dictionary<PhysicsObject, TextBlock> _objectLabels = new();

        // Drag state
        private bool _isDragging = false;
        private PhysicsObject? _dragTarget;
        private Point _dragOffset;

        // Ray drawing
        private readonly List<Line> _rayLines = new();

        public PhysicsSandboxTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                menuTextGuide.Text = isVN ? "Hướng dẫn thao tác" : "Operations Guide";
                menuTextExperiments.Text = isVN ? "Thí nghiệm mẫu & Câu hỏi" : "Sample Experiments";
                menuTextPractice.Text = isVN ? "Mô phỏng vật lý" : "Physics Sandbox";
                menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";
                
                sideMenu.SelectedIndex = 0;
                LoadPracticalApps();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  TOOLBOX CLICK
        // ═══════════════════════════════════════════════════════════

        private void ToolboxItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tool)
            {
                _selectedTool = tool;
                txtInfo.Text = $"🎯 Đã chọn: {GetToolName(tool)} — Nhấp vào không gian để đặt.";
                txtInfo.Foreground = new SolidColorBrush(Color.FromRgb(124, 77, 255)); // Purple accent
            }
        }

        private static string GetToolName(string tool) => tool switch
        {
            "laser" => "Nguồn Laser 🔦",
            "mirror" => "Gương phẳng 🪞",
            "lens" => "Thấu kính hội tụ 🔍",
            "prism" => "Lăng kính 🔺",
            _ => tool
        };

        // ═══════════════════════════════════════════════════════════
        //  SELECTION HIGHLIGHT & CONTROLS
        // ═══════════════════════════════════════════════════════════

        private void ApplyHighlight(PhysicsObject obj, bool highlight)
        {
            if (obj.Visual == null) return;

            if (highlight)
            {
                obj.Visual.Effect = new DropShadowEffect
                {
                    BlurRadius = 16,
                    ShadowDepth = 0,
                    Opacity = 1.0,
                    Color = Colors.Gold
                };
            }
            else
            {
                // Restore default effects
                switch (obj.Type)
                {
                    case ObjectType.Laser:
                        obj.Visual.Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 0, Opacity = 0.6, Color = Colors.Red };
                        break;
                    case ObjectType.Mirror:
                        obj.Visual.Effect = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 0, Opacity = 0.5, Color = Colors.LightBlue };
                        break;
                    case ObjectType.Lens:
                        obj.Visual.Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 0, Opacity = 0.4, Color = Colors.CornflowerBlue };
                        break;
                    case ObjectType.Prism:
                        obj.Visual.Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 0, Opacity = 0.5, Color = Colors.MediumPurple };
                        break;
                }
            }
        }

        private void UpdateSelectedControls()
        {
            if (_selectedObject != null)
            {
                txtInfo.Visibility = Visibility.Collapsed;
                pnlSelectedControls.Visibility = Visibility.Visible;
                txtSelectedName.Text = $"Đang chọn: {GetTypeName(_selectedObject.Type)} ({_selectedObject.Angle:0}°)";
            }
            else
            {
                txtInfo.Visibility = Visibility.Visible;
                pnlSelectedControls.Visibility = Visibility.Collapsed;
            }
        }

        private static string GetTypeName(ObjectType type) => type switch
        {
            ObjectType.Laser => "Laser 🔦",
            ObjectType.Mirror => "Gương phẳng 🪞",
            ObjectType.Lens => "Thấu kính 🔍",
            ObjectType.Prism => "Lăng kính 🔺",
            _ => "Dụng cụ"
        };

        // ═══════════════════════════════════════════════════════════
        //  WORKSPACE EVENTS
        // ═══════════════════════════════════════════════════════════

        private void Workspace_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(WorkspaceCanvas);

            // Check if clicking on existing object for drag and selection
            PhysicsObject? clickedObj = null;
            foreach (var obj in _objects)
            {
                if (obj.Visual != null)
                {
                    double left = Canvas.GetLeft(obj.Visual);
                    double top = Canvas.GetTop(obj.Visual);
                    double w = obj.Visual.ActualWidth > 0 ? obj.Visual.ActualWidth : 50;
                    double h = obj.Visual.ActualHeight > 0 ? obj.Visual.ActualHeight : 50;

                    if (pos.X >= left && pos.X <= left + w && pos.Y >= top && pos.Y <= top + h)
                    {
                        clickedObj = obj;
                        break;
                    }
                }
            }

            if (clickedObj != null)
            {
                // Select and start drag
                if (_selectedObject != null && _selectedObject != clickedObj)
                {
                    ApplyHighlight(_selectedObject, false);
                }
                _selectedObject = clickedObj;
                ApplyHighlight(_selectedObject, true);

                _isDragging = true;
                _dragTarget = clickedObj;
                double left = Canvas.GetLeft(clickedObj.Visual);
                double top = Canvas.GetTop(clickedObj.Visual);
                _dragOffset = new Point(pos.X - left, pos.Y - top);
                WorkspaceCanvas.CaptureMouse();
                UpdateSelectedControls();
                e.Handled = true;
                return;
            }

            // Clicked empty space: deselect current
            if (_selectedObject != null)
            {
                ApplyHighlight(_selectedObject, false);
                _selectedObject = null;
                UpdateSelectedControls();
            }

            // Place new object if tool selected
            if (!string.IsNullOrEmpty(_selectedTool))
            {
                PlaceObject(_selectedTool, pos.X, pos.Y);
                _selectedTool = "";
                txtInfo.Text = "💡 Click chọn dụng cụ ở cột trái, rồi click vào vùng tối để đặt.";
                txtInfo.Foreground = new SolidColorBrush(Color.FromRgb(176, 190, 197));
                RedrawRays();
            }
        }

        private void Workspace_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _dragTarget?.Visual != null)
            {
                var pos = e.GetPosition(WorkspaceCanvas);
                double newX = pos.X - _dragOffset.X;
                double newY = pos.Y - _dragOffset.Y;

                double canvasW = WorkspaceCanvas.ActualWidth > 0 ? WorkspaceCanvas.ActualWidth : 1200;
                double canvasH = WorkspaceCanvas.ActualHeight > 0 ? WorkspaceCanvas.ActualHeight : 600;
                double objW = _dragTarget.Visual.ActualWidth > 0 ? _dragTarget.Visual.ActualWidth : 50;
                double objH = _dragTarget.Visual.ActualHeight > 0 ? _dragTarget.Visual.ActualHeight : 50;
                if (objW == 50 && objH == 50)
                {
                    switch (_dragTarget.Type)
                    {
                        case ObjectType.Laser: objW = 50; objH = 20; break;
                        case ObjectType.Mirror: objW = 8; objH = 60; break;
                        case ObjectType.Lens: objW = 30; objH = 60; break;
                        case ObjectType.Prism: objW = 50; objH = 50; break;
                    }
                }

                // Giới hạn biên canvas (Clamping)
                newX = Math.Max(0, Math.Min(canvasW - objW, newX));
                newY = Math.Max(0, Math.Min(canvasH - objH, newY));

                // Bắt lưới Grid 10px (Snapping)
                newX = Math.Round(newX / 10.0) * 10.0;
                newY = Math.Round(newY / 10.0) * 10.0;

                if (_dragTarget.X != newX || _dragTarget.Y != newY)
                {
                    Canvas.SetLeft(_dragTarget.Visual, newX);
                    Canvas.SetTop(_dragTarget.Visual, newY);
                    _dragTarget.X = newX;
                    _dragTarget.Y = newY;

                    UpdateLabelPosition(_dragTarget);
                    RedrawRays();
                }
            }
        }

        private void Workspace_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _dragTarget = null;
                WorkspaceCanvas.ReleaseMouseCapture();
            }
        }

        public void Workspace_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_selectedObject != null)
            {
                bool shiftPressed = false;
                try
                {
                    shiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                }
                catch
                {
                    // Fallback for non-interactive test threads
                }

                double step = shiftPressed ? 15 : 5;
                double delta = e.Delta > 0 ? step : -step;

                if (shiftPressed)
                {
                    double currentAngle = _selectedObject.Angle;
                    double targetAngle = e.Delta > 0 
                        ? Math.Ceiling((currentAngle + 0.1) / 15.0) * 15.0 
                        : Math.Floor((currentAngle - 0.1) / 15.0) * 15.0;
                    RotateObject(_selectedObject, targetAngle - currentAngle);
                }
                else
                {
                    RotateObject(_selectedObject, delta);
                }
                e.Handled = true;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  PLACE & ROTATE & DELETE OBJECTS
        // ═══════════════════════════════════════════════════════════

        private void PlaceObject(string tool, double x, double y)
        {
            var objType = tool switch
            {
                "laser" => ObjectType.Laser,
                "mirror" => ObjectType.Mirror,
                "lens" => ObjectType.Lens,
                "prism" => ObjectType.Prism,
                _ => ObjectType.Laser
            };

            double w = 50, h = 50;
            switch (objType)
            {
                case ObjectType.Laser: w = 50; h = 20; break;
                case ObjectType.Mirror: w = 8; h = 60; break;
                case ObjectType.Lens: w = 30; h = 60; break;
                case ObjectType.Prism: w = 50; h = 50; break;
            }

            var obj = new PhysicsObject { Type = objType, X = x - w / 2, Y = y - h / 2, Angle = 0 };
            FrameworkElement visual;

            switch (objType)
            {
                case ObjectType.Laser:
                    visual = CreateLaserVisual();
                    break;
                case ObjectType.Mirror:
                    visual = CreateMirrorVisual();
                    break;
                case ObjectType.Lens:
                    visual = CreateLensVisual();
                    break;
                case ObjectType.Prism:
                    visual = CreatePrismVisual();
                    break;
                default:
                    visual = CreateLaserVisual();
                    break;
            }

            Canvas.SetLeft(visual, obj.X);
            Canvas.SetTop(visual, obj.Y);

            // Register rotation transform
            visual.RenderTransformOrigin = new Point(0.5, 0.5);
            visual.RenderTransform = new RotateTransform(obj.Angle);

            WorkspaceCanvas.Children.Add(visual);

            obj.Visual = visual;
            _objects.Add(obj);

            // Create on-canvas numeric label
            var label = new TextBlock
            {
                Text = $"{obj.Angle:0}°",
                Width = 60,
                TextAlignment = TextAlignment.Center,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Gold),
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"),
                IsHitTestVisible = false
            };
            _objectLabels[obj] = label;
            WorkspaceCanvas.Children.Add(label);
            UpdateLabelPosition(obj);

            txtObjectCount.Text = $"Đối tượng: {_objects.Count}";
        }

        private void RotateObject(PhysicsObject obj, double angleDelta)
        {
            obj.Angle = (obj.Angle + angleDelta) % 360;
            if (obj.Angle < 0) obj.Angle += 360;

            if (obj.Visual != null)
            {
                if (obj.Visual.RenderTransform is RotateTransform rt)
                {
                    rt.Angle = obj.Angle;
                }
                else
                {
                    obj.Visual.RenderTransformOrigin = new Point(0.5, 0.5);
                    obj.Visual.RenderTransform = new RotateTransform(obj.Angle);
                }
            }

            if (_objectLabels.TryGetValue(obj, out var label))
            {
                label.Text = $"{obj.Angle:0}°";
                UpdateLabelPosition(obj);
            }

            RedrawRays();
            UpdateSelectedControls();
        }

        private void UpdateLabelPosition(PhysicsObject obj)
        {
            if (_objectLabels.TryGetValue(obj, out var label))
            {
                Point center = GetObjectCenter(obj);
                Canvas.SetLeft(label, center.X - 30);
                Canvas.SetTop(label, center.Y + 35);
            }
        }

        private FrameworkElement CreateLaserVisual()
        {
            var grid = new Grid { Width = 50, Height = 20 };
            var body = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                CornerRadius = new CornerRadius(4),
                Width = 50, Height = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 0, Opacity = 0.6, Color = Colors.Red }
            };
            var label = new TextBlock
            {
                Text = "🔦", FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.Children.Add(body);
            grid.Children.Add(label);
            return grid;
        }

        private FrameworkElement CreateMirrorVisual()
        {
            var rect = new Border
            {
                Width = 8, Height = 60,
                Background = new LinearGradientBrush(
                    Color.FromRgb(200, 200, 220),
                    Color.FromRgb(120, 120, 160),
                    90),
                CornerRadius = new CornerRadius(2),
                Effect = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 0, Opacity = 0.5, Color = Colors.LightBlue }
            };
            return rect;
        }

        private FrameworkElement CreateLensVisual()
        {
            var canvas = new Canvas { Width = 30, Height = 60 };
            var ellipse = new Ellipse
            {
                Width = 30, Height = 60,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Color.FromArgb(40, 100, 181, 246)),
                Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 0, Opacity = 0.4, Color = Colors.CornflowerBlue }
            };
            canvas.Children.Add(ellipse);
            return canvas;
        }

        private FrameworkElement CreatePrismVisual()
        {
            var canvas = new Canvas { Width = 50, Height = 50 };
            var triangle = new Polygon
            {
                Points = new PointCollection(new[]
                {
                    new Point(25, 0),
                    new Point(50, 50),
                    new Point(0, 50)
                }),
                Stroke = new SolidColorBrush(Color.FromRgb(179, 136, 255)),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 179, 136, 255)),
                Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 0, Opacity = 0.5, Color = Colors.MediumPurple }
            };
            canvas.Children.Add(triangle);
            return canvas;
        }

        // ═══════════════════════════════════════════════════════════
        //  RAY TRACING (Precise Geometric Raycasting)
        // ═══════════════════════════════════════════════════════════

        private Point GetObjectCenter(PhysicsObject obj)
        {
            double w = 50, h = 50;
            switch (obj.Type)
            {
                case ObjectType.Laser: w = 50; h = 20; break;
                case ObjectType.Mirror: w = 8; h = 60; break;
                case ObjectType.Lens: w = 30; h = 60; break;
                case ObjectType.Prism: w = 50; h = 50; break;
            }
            return new Point(obj.X + w / 2, obj.Y + h / 2);
        }

        private Point GetRotatedPoint(Point p, Point center, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            double rx = center.X + (p.X - center.X) * cos - (p.Y - center.Y) * sin;
            double ry = center.Y + (p.X - center.X) * sin + (p.Y - center.Y) * cos;
            return new Point(rx, ry);
        }

        private bool GetRaySegmentIntersection(
            Point rayStart, Point rayDir, 
            Point segStart, Point segEnd, 
            out Point intersectionPoint, out double t)
        {
            intersectionPoint = new Point();
            t = 0;

            double dx = rayDir.X;
            double dy = rayDir.Y;

            double vx = segEnd.X - segStart.X;
            double vy = segEnd.Y - segStart.Y;

            double wx = rayStart.X - segStart.X;
            double wy = rayStart.Y - segStart.Y;

            double det = dx * vy - dy * vx;
            if (Math.Abs(det) < 1e-6)
            {
                return false; // Parallel
            }

            double u = (dx * wy - dy * wx) / det;
            t = (vx * wy - vy * wx) / det;

            if (t > 0.001 && u >= 0 && u <= 1)
            {
                intersectionPoint = new Point(rayStart.X + t * dx, rayStart.Y + t * dy);
                return true;
            }

            return false;
        }

        private void RedrawRays()
        {
            // Clear previous rays
            foreach (var line in _rayLines)
                WorkspaceCanvas.Children.Remove(line);
            _rayLines.Clear();

            // Find all lasers and cast rays
            foreach (var obj in _objects)
            {
                if (obj.Type == ObjectType.Laser)
                {
                    double rad = obj.Angle * Math.PI / 180.0;
                    Point center = GetObjectCenter(obj);
                    // Start nozzle shifted 25px along direction
                    double startX = center.X + 25 * Math.Cos(rad);
                    double startY = center.Y + 25 * Math.Sin(rad);

                    CastRay(startX, startY, obj.Angle, Colors.Red, 0, null);
                }
            }
        }

        /// <summary>
        /// Cast a ray from (startX, startY) at angle (degrees from horizontal-right).
        /// </summary>
        private void CastRay(double startX, double startY, double angleDeg, Color color, int bounceCount, PhysicsObject? lastHitObject = null)
        {
            if (bounceCount > 5) return; // Prevent infinite bounces

            double rad = angleDeg * System.Math.PI / 180.0;
            double dx = System.Math.Cos(rad);
            double dy = System.Math.Sin(rad);

            double canvasW = WorkspaceCanvas.ActualWidth > 0 ? WorkspaceCanvas.ActualWidth : 1200;
            double canvasH = WorkspaceCanvas.ActualHeight > 0 ? WorkspaceCanvas.ActualHeight : 600;

            double maxDist = 2000;
            double endX = startX + dx * maxDist;
            double endY = startY + dy * maxDist;

            PhysicsObject? hitObject = null;
            double closestDist = maxDist;
            Point hitPoint = new Point();

            foreach (var obj in _objects)
            {
                if (obj.Type == ObjectType.Laser) continue;
                if (obj == lastHitObject) continue; // Skip last hit object to prevent self-collision

                Point center = GetObjectCenter(obj);

                if (obj.Type == ObjectType.Mirror || obj.Type == ObjectType.Lens)
                {
                    double angleRad = (90 + obj.Angle) * Math.PI / 180.0;
                    Point p1 = new Point(center.X - 30 * Math.Cos(angleRad), center.Y - 30 * Math.Sin(angleRad));
                    Point p2 = new Point(center.X + 30 * Math.Cos(angleRad), center.Y + 30 * Math.Sin(angleRad));

                    if (GetRaySegmentIntersection(new Point(startX, startY), new Point(dx, dy), p1, p2, out Point ip, out double t))
                    {
                        if (t < closestDist)
                        {
                            closestDist = t;
                            hitObject = obj;
                            hitPoint = ip;
                        }
                    }
                }
                else if (obj.Type == ObjectType.Prism)
                {
                    Point pc = new Point(obj.X + 25, obj.Y + 25);
                    Point pA = GetRotatedPoint(new Point(obj.X + 25, obj.Y + 0), pc, obj.Angle);
                    Point pB = GetRotatedPoint(new Point(obj.X + 50, obj.Y + 50), pc, obj.Angle);
                    Point pC = GetRotatedPoint(new Point(obj.X + 0, obj.Y + 50), pc, obj.Angle);

                    Point[] sides = { pA, pB, pC, pA };
                    for (int i = 0; i < 3; i++)
                    {
                        if (GetRaySegmentIntersection(new Point(startX, startY), new Point(dx, dy), sides[i], sides[i+1], out Point ip, out double t))
                        {
                            if (t < closestDist)
                            {
                                closestDist = t;
                                hitObject = obj;
                                hitPoint = ip;
                            }
                        }
                    }
                }
            }

            if (hitObject != null)
            {
                endX = hitPoint.X;
                endY = hitPoint.Y;
            }
            else
            {
                // Clamp to canvas borders
                if (endX < 0) { endX = 0; endY = startY + dy * ((0 - startX) / dx); }
                if (endX > canvasW) { endX = canvasW; endY = startY + dy * ((canvasW - startX) / dx); }
                if (endY < 0) { endY = 0; endX = startX + dx * ((0 - startY) / dy); }
                if (endY > canvasH) { endY = canvasH; endX = startX + dx * ((canvasH - startY) / dy); }
            }

            // Draw the ray
            var rayLine = new Line
            {
                X1 = startX, Y1 = startY,
                X2 = endX, Y2 = endY,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 3,
                Opacity = 0.85,
                Effect = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 0, Opacity = 0.5, Color = color }
            };
            WorkspaceCanvas.Children.Add(rayLine);
            _rayLines.Add(rayLine);

            // Handle reflections/refractions at hit object
            if (hitObject != null)
            {
                switch (hitObject.Type)
                {
                    case ObjectType.Mirror:
                        // Reflect relative to mirror normal angle
                        double reflectAngle = 2 * (90 + hitObject.Angle) - angleDeg;
                        CastRay(endX, endY, reflectAngle, Colors.OrangeRed, bounceCount + 1, hitObject);
                        break;

                    case ObjectType.Lens:
                        Point lensCenter = GetObjectCenter(hitObject);
                        double lensAngleRad = (90 + hitObject.Angle) * Math.PI / 180.0;
                        Point lensDir = new Point(Math.Cos(lensAngleRad), Math.Sin(lensAngleRad));
                        // Signed offset distance along lens plane
                        double offset = (endX - lensCenter.X) * lensDir.X + (endY - lensCenter.Y) * lensDir.Y;
                        // Converging lens: bends ray inwards towards focal line
                        double refractAngle = angleDeg - offset * 0.3;
                        CastRay(endX + dx * 2, endY + dy * 2, refractAngle, Colors.DodgerBlue, bounceCount + 1, hitObject);
                        break;

                    case ObjectType.Prism:
                        // Dispersion: split into spectrum, rotated along prism orientation
                        var spectrum = new[] {
                            (Colors.Red, -8.0), (Colors.Orange, -4.0), (Colors.Yellow, 0.0),
                            (Colors.Lime, 4.0), (Colors.Cyan, 8.0), (Colors.Blue, 12.0), (Colors.Violet, 16.0)
                        };
                        foreach (var (c, a) in spectrum)
                        {
                            double outAngle = angleDeg + 20 + hitObject.Angle + a;
                            double radOut = outAngle * Math.PI / 180.0;
                            double odx = Math.Cos(radOut);
                            double ody = Math.Sin(radOut);
                            CastRay(endX + odx * 2, endY + ody * 2, outAngle, c, bounceCount + 1, hitObject);
                        }
                        break;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  BUTTON CLICK HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            WorkspaceCanvas.Children.Clear();
            _objects.Clear();
            _rayLines.Clear();
            _objectLabels.Clear();
            _selectedObject = null;
            UpdateSelectedControls();
            txtObjectCount.Text = "Đối tượng: 0";
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            sideMenu.SelectedIndex = 1;
        }
 
        private void BtnRedraw_Click(object sender, RoutedEventArgs e)
        {
            RedrawRays();
        }

        public void BtnRotateLeft_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject != null)
            {
                RotateObject(_selectedObject, -15);
            }
        }

        public void BtnRotateRight_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject != null)
            {
                RotateObject(_selectedObject, 15);
            }
        }

        public void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject != null)
            {
                if (_selectedObject.Visual != null)
                {
                    WorkspaceCanvas.Children.Remove(_selectedObject.Visual);
                }
                if (_objectLabels.TryGetValue(_selectedObject, out var label))
                {
                    WorkspaceCanvas.Children.Remove(label);
                    _objectLabels.Remove(_selectedObject);
                }
                _objects.Remove(_selectedObject);
                _selectedObject = null;
                UpdateSelectedControls();
                txtObjectCount.Text = $"Đối tượng: {_objects.Count}";
                RedrawRays();
            }
        }

        public void Dispose()
        {
            WorkspaceCanvas.Children.Clear();
            _objects.Clear();
            _rayLines.Clear();
            _objectLabels.Clear();
        }



        private void BtnCloseHelp_Click(object sender, RoutedEventArgs e)
        {
            sideMenu.SelectedIndex = 1;
        }

        private void TabHelpControls_Click(object sender, RoutedEventArgs e)
        {
            sideMenu.SelectedIndex = 1;
        }

        private void TabHelpExperiments_Click(object sender, RoutedEventArgs e)
        {
            sideMenu.SelectedIndex = 1;
        }

        private void BtnPresetDispersion_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset(1);
        }

        private void BtnPresetFocus_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset(2);
        }

        private void BtnPresetReflection_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset(3);
        }

        private void BtnPresetPeriscope_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset(4);
        }

        private void BtnPresetCombo_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset(5);
        }

        private void LoadPreset(int presetType)
        {
            // Clear all objects and rays
            BtnClear_Click(this, new RoutedEventArgs());

            double canvasW = WorkspaceCanvas.ActualWidth > 0 ? WorkspaceCanvas.ActualWidth : 1200;
            double canvasH = WorkspaceCanvas.ActualHeight > 0 ? WorkspaceCanvas.ActualHeight : 600;

            // Calculate center dynamically
            double cx = canvasW / 2;
            double cy = canvasH / 2;

            if (presetType == 1) // Tán sắc (Dispersion)
            {
                // Place Laser chéo
                double lx = cx - 200;
                double ly = cy - 100;
                PlaceObject("laser", lx, ly);
                var laser = _objects.Last();
                RotateObject(laser, 20); // Laser xoay 20 độ chiếu nghiêng xuống lăng kính

                // Place Prism ở giữa
                PlaceObject("prism", cx, cy);
                var prism = _objects.Last();
                RotateObject(prism, 15); // Lăng kính xoay 15 độ
            }
            else if (presetType == 2) // Hội tụ (Focus)
            {
                // Place Laser bắn ngang
                double lx = cx - 200;
                double ly = cy;
                PlaceObject("laser", lx, ly);

                // Place Lens ở giữa
                PlaceObject("lens", cx, cy);
            }
            else if (presetType == 3) // Phản xạ (Reflection)
            {
                // Place Laser bắn ngang hơi chéo lên
                double lx = cx - 220;
                double ly = cy + 60;
                PlaceObject("laser", lx, ly);
                var laser = _objects.Last();
                RotateObject(laser, 335); // Bắn góc chéo lên

                // Place Gương 1 ở trên
                double mx1 = cx;
                double my1 = cy - 80;
                PlaceObject("mirror", mx1, my1);
                var mirror1 = _objects.Last();
                RotateObject(mirror1, 100); // Gương nghiêng 100 độ

                // Place Gương 2 ở dưới phải
                double mx2 = cx + 180;
                double my2 = cy + 100;
                PlaceObject("mirror", mx2, my2);
                var mirror2 = _objects.Last();
                RotateObject(mirror2, 45); // Gương nghiêng 45 độ
            }
            else if (presetType == 4) // Kính tiềm vọng (Periscope)
            {
                // Place Laser bắn ngang
                double lx = cx - 250;
                double ly = cy + 120;
                PlaceObject("laser", lx, ly);
                var laser = _objects.Last();
                RotateObject(laser, 0); // Bắn ngang sang phải

                // Place Gương 1 ở dưới phải chéo 45 độ
                double mx1 = cx;
                double my1 = cy + 120;
                PlaceObject("mirror", mx1, my1);
                var mirror1 = _objects.Last();
                RotateObject(mirror1, 45); // Xoay 45 độ

                // Place Gương 2 ở trên trái/giữa chéo 45 độ
                double mx2 = cx;
                double my2 = cy - 120;
                PlaceObject("mirror", mx2, my2);
                var mirror2 = _objects.Last();
                RotateObject(mirror2, 45); // Xoay 45 độ
            }
            else if (presetType == 5) // Hội tụ & Phản xạ (Combo)
            {
                // Laser 1 ở trên
                double lx1 = cx - 250;
                double ly1 = cy - 100;
                PlaceObject("laser", lx1, ly1);

                // Laser 2 ở dưới
                double lx2 = cx - 250;
                double ly2 = cy - 60;
                PlaceObject("laser", lx2, ly2);

                // Lens ở giữa
                double lensX = cx - 100;
                double lensY = cy - 80;
                PlaceObject("lens", lensX, lensY);

                // Mirror ở tiêu điểm chéo phản xạ xuống dưới
                double mx = cx + 90;
                double my = cy - 80;
                PlaceObject("mirror", mx, my);
                var mirror = _objects.Last();
                RotateObject(mirror, 120); // Góc phản xạ chéo đẹp
            }

            RedrawRays();
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🪞",
                    Title = isVN ? "Khảo Sát Định Luật Phản Xạ & Gương Phẳng" : "Reflection Law & Periscope Design",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_physics_sandbox_1_{suffix}.png",
                    Description = isVN 
                        ? "Ứng dụng mô phỏng đường đi của tia laser phản xạ qua gương phẳng, giúp học sinh chứng minh định luật phản xạ ánh sáng (i = i') và tự thiết kế mô hình kính tiềm vọng quang học." 
                        : "Simulate laser pathways reflecting off plane mirrors to verify the law of reflection (i = i') and design functional optical periscope models."
                },
                new PracticalAppItem
                {
                    Icon = "🔺",
                    Title = isVN ? "Hiện Tượng Khúc Xạ & Tán Sắc Lăng Kính" : "Prism Refraction & Dispersion",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_physics_sandbox_2_{suffix}.png",
                    Description = isVN 
                        ? "Mô tả cơ chế khúc xạ ánh sáng đi qua lăng kính thủy tinh. Chùm sáng trắng bị phân tách thành dải màu cầu vồng để giải thích cơ chế hình thành cầu vồng trong tự nhiên." 
                        : "Model light refraction through a glass prism. Separate white light into a rainbow spectrum to explain the natural mechanism of rainbows."
                },
                new PracticalAppItem
                {
                    Icon = "🔍",
                    Title = isVN ? "Thấu Kính Hội Tụ & Dụng Cụ Quang Học" : "Convex Lens & Optical Instruments",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_physics_sandbox_3_{suffix}.png",
                    Description = isVN 
                        ? "Khảo sát sự hội tụ của chùm tia song song qua thấu kính. Dùng để chế tạo mô hình kính lúp phóng to, kính hiển vi hoặc hệ kính thiên văn ngắm sao." 
                        : "Explore parallel beam convergence through a convex lens. Apply it to design magnifying glasses, microscope setups, or astronomical telescope optics."
                }
            };

            try
            {
                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for PhysicsSandboxTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || viewGuide == null || viewExperiments == null || viewPractice == null || viewPractical == null) return;
            
            viewGuide.Visibility = Visibility.Collapsed;
            viewExperiments.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;
            
            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                viewGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                viewExperiments.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractice.Visibility = Visibility.Visible;
            }
            else if (index == 3)
            {
                viewPractical.Visibility = Visibility.Visible;
            }
        }
    }
}