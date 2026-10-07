using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_6_3DPrismEditor : Window
    {
        // 3D Vertices and Faces
        private List<Point3D> vertices = new List<Point3D>();
        private List<Face> faces = new List<Face>();

        // Configuration Properties (public for MainDashboard access)
        public string BaseType { get; private set; } = "Square";
        public double BaseSize { get; private set; } = 100;
        public double Height { get; private set; } = 200;
        public double RotationX { get; private set; } = 0;
        public double RotationY { get; private set; } = 30;
        public double RotationZ { get; private set; } = 0;
        public double EdgeThickness { get; private set; } = 1.5;
        public string DrawingMode { get; private set; } = "Wireframe";
        public Color EdgeColor { get; private set; } = Colors.Black;
        public Color FaceColor { get; private set; } = Colors.Transparent;
        public double Opacity { get; private set; } = 0.7;
        public bool IsAutoRotate { get; private set; } = false;

        // Confirmation flag
        public bool IsConfirmed { get; private set; } = false;
        
        // Display Stats Mode flag (true if "Display Stats" button was clicked)
        public bool DisplayStatsMode { get; private set; } = false;
        
        public bool ShowHiddenEdges { get; private set; } = true;
        public bool ShowVertexLabels { get; private set; } = false;

        // Drag & Drop properties
        private double OffsetX = 0;
        private double OffsetY = 0;
        private bool isDragging = false;
        private Point dragStartPoint;

        // Auto-rotate timer
        private DispatcherTimer? autoRotateTimer;

        public Form2_6_3DPrismEditor()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            InitializePrism();
            this.Loaded += (s, e) =>
            {
                EnableDragDrop();
                DrawPrism();
            };
        }

        private void EnableDragDrop()
        {
            canvas3D.MouseDown += Canvas3D_MouseDown;
            canvas3D.MouseMove += Canvas3D_MouseMove;
            canvas3D.MouseUp += Canvas3D_MouseUp;
            canvas3D.MouseLeave += Canvas3D_MouseLeave;
            canvas3D.TouchDown += Canvas3D_TouchDown;
            canvas3D.TouchMove += Canvas3D_TouchMove;
            canvas3D.TouchUp += Canvas3D_TouchUp;
            canvas3D.TouchLeave += Canvas3D_TouchUp;
            canvas3D.MouseWheel += Canvas3D_MouseWheel;
        }

        private int? _activeTouchId = null;

        private void Canvas3D_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                isDragging = true;
                dragStartPoint = e.GetPosition(canvas3D);
                canvas3D.CaptureMouse();
            }
        }

        private void Canvas3D_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(canvas3D);
                double deltaX = currentPoint.X - dragStartPoint.X;
                double deltaY = currentPoint.Y - dragStartPoint.Y;

                UpdateRotationFromDrag(deltaX, deltaY);
                dragStartPoint = currentPoint;
            }
        }

        private void Canvas3D_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                canvas3D.ReleaseMouseCapture();
            }
        }

        private void Canvas3D_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                canvas3D.ReleaseMouseCapture();
            }
        }

        private void Canvas3D_TouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (!isDragging)
            {
                isDragging = true;
                _activeTouchId = e.TouchDevice.Id;
                dragStartPoint = e.GetTouchPoint(canvas3D).Position;
                canvas3D.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void Canvas3D_TouchMove(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (isDragging && _activeTouchId == e.TouchDevice.Id)
            {
                Point currentPoint = e.GetTouchPoint(canvas3D).Position;
                double deltaX = currentPoint.X - dragStartPoint.X;
                double deltaY = currentPoint.Y - dragStartPoint.Y;

                UpdateRotationFromDrag(deltaX, deltaY);
                dragStartPoint = currentPoint;
                e.Handled = true;
            }
        }

        private void Canvas3D_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (isDragging && _activeTouchId == e.TouchDevice.Id)
            {
                isDragging = false;
                _activeTouchId = null;
                canvas3D.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void UpdateRotationFromDrag(double deltaX, double deltaY)
        {
            if (IsAutoRotate)
            {
                chkAutoRotate.IsChecked = false;
            }

            double newY = (sliderRotationY.Value + deltaX * 0.6) % 360;
            if (newY < 0) newY += 360;

            double newX = (sliderRotationX.Value - deltaY * 0.6) % 360;
            if (newX < 0) newX += 360;

            sliderRotationY.Value = Math.Round(newY);
            sliderRotationX.Value = Math.Round(newX);
        }

        private void Canvas3D_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double step = (e.Delta > 0) ? 10 : -10;
            double newValue = sliderHeight.Value + step;
            if (newValue >= sliderHeight.Minimum && newValue <= sliderHeight.Maximum)
            {
                sliderHeight.Value = newValue;
            }
        }

        #region Preset View Handlers

        private void BtnPresetPerspective_Click(object sender, RoutedEventArgs e)
        {
            sliderRotationX.Value = 0;
            sliderRotationY.Value = 30;
            sliderRotationZ.Value = 0;
        }

        private void BtnPresetFront_Click(object sender, RoutedEventArgs e)
        {
            sliderRotationX.Value = 0;
            sliderRotationY.Value = 0;
            sliderRotationZ.Value = 0;
        }

        private void BtnPresetTop_Click(object sender, RoutedEventArgs e)
        {
            sliderRotationX.Value = 90;
            sliderRotationY.Value = 0;
            sliderRotationZ.Value = 0;
        }

        private void BtnPresetIsometric_Click(object sender, RoutedEventArgs e)
        {
            sliderRotationX.Value = 30;
            sliderRotationY.Value = 45;
            sliderRotationZ.Value = 0;
        }

        #endregion

        // 3D Point structure
        private struct Point3D
        {
            public double X, Y, Z;
            public Point3D(double x, double y, double z)
            {
                X = x; Y = y; Z = z;
            }
        }

        // Face structure
        private class Face
        {
            public List<int> VertexIndices { get; set; } = new List<int>();
            public double AverageZ { get; set; }
        }

        private void InitializePrism()
        {
            vertices = GeneratePrismVertices(BaseSize, Height, BaseType);
            faces = GeneratePrismFaces(BaseType);
            UpdateStats();
        }

        private List<Point3D> GeneratePrismVertices(double size, double height, string baseType)
        {
            var verts = new List<Point3D>();
            int sides = GetBaseTypeSides(baseType);

            // For square, use direct coordinates instead of circumradius
            if (baseType == "Square")
            {
                double halfSize = size / 2;
                
                // Top square vertices (clockwise from top-right)
                verts.Add(new Point3D(halfSize, -height / 2, halfSize));   // Top-right
                verts.Add(new Point3D(-halfSize, -height / 2, halfSize));  // Top-left
                verts.Add(new Point3D(-halfSize, -height / 2, -halfSize)); // Bottom-left
                verts.Add(new Point3D(halfSize, -height / 2, -halfSize));  // Bottom-right

                // Bottom square vertices (same order)
                verts.Add(new Point3D(halfSize, height / 2, halfSize));    // Top-right
                verts.Add(new Point3D(-halfSize, height / 2, halfSize));   // Top-left
                verts.Add(new Point3D(-halfSize, height / 2, -halfSize));  // Bottom-left
                verts.Add(new Point3D(halfSize, height / 2, -halfSize));   // Bottom-right
            }
            else
            {
                // For other shapes, use circumradius approach
                double radius = GetCircumradius(size, sides);

                // Top polygon vertices
                for (int i = 0; i < sides; i++)
                {
                    double angle = 2 * Math.PI * i / sides - Math.PI / 2; // Start from top
                    double x = radius * Math.Cos(angle);
                    double z = radius * Math.Sin(angle);
                    verts.Add(new Point3D(x, -height / 2, z));
                }

                // Bottom polygon vertices
                for (int i = 0; i < sides; i++)
                {
                    double angle = 2 * Math.PI * i / sides - Math.PI / 2;
                    double x = radius * Math.Cos(angle);
                    double z = radius * Math.Sin(angle);
                    verts.Add(new Point3D(x, height / 2, z));
                }
            }

            return verts;
        }

        private List<Face> GeneratePrismFaces(string baseType)
        {
            var faceList = new List<Face>();
            int sides = GetBaseTypeSides(baseType);

            // Top face (polygon)
            var topFace = new Face();
            for (int i = 0; i < sides; i++)
            {
                topFace.VertexIndices.Add(i);
            }
            faceList.Add(topFace);

            // Bottom face (polygon - reversed for correct normal)
            var bottomFace = new Face();
            for (int i = sides * 2 - 1; i >= sides; i--)
            {
                bottomFace.VertexIndices.Add(i);
            }
            faceList.Add(bottomFace);

            // Lateral faces (quads)
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                var lateralFace = new Face();
                lateralFace.VertexIndices.Add(i);
                lateralFace.VertexIndices.Add(next);
                lateralFace.VertexIndices.Add(next + sides);
                lateralFace.VertexIndices.Add(i + sides);
                faceList.Add(lateralFace);
            }

            return faceList;
        }

        private int GetBaseTypeSides(string baseType)
        {
            return baseType switch
            {
                "Triangle" => 3,
                "Square" => 4,
                "Pentagon" => 5,
                "Hexagon" => 6,
                _ => 4
            };
        }

        private double GetCircumradius(double sideLength, int sides)
        {
            return sideLength / (2 * Math.Sin(Math.PI / sides));
        }

        private double CalculateBaseArea(double size, string baseType)
        {
            return baseType switch
            {
                "Triangle" => (Math.Sqrt(3) / 4) * size * size,
                "Square" => size * size,
                "Pentagon" => (5 * size * size) / (4 * Math.Tan(Math.PI / 5)),
                "Hexagon" => (3 * Math.Sqrt(3) / 2) * size * size,
                _ => size * size
            };
        }

        private double CalculateVolume(double size, double height, string baseType)
        {
            double baseArea = CalculateBaseArea(size, baseType);
            return baseArea * height;
        }

        private double CalculateLateralArea(double size, double height, string baseType)
        {
            int sides = GetBaseTypeSides(baseType);
            return sides * size * height;
        }

        private void UpdateStats()
        {
            int sides = GetBaseTypeSides(BaseType);
            int vertexCount = sides * 2;
            double baseArea = CalculateBaseArea(BaseSize, BaseType);
            double volume = CalculateVolume(BaseSize, Height, BaseType);
            double lateralArea = CalculateLateralArea(BaseSize, Height, BaseType);
            double totalArea = 2 * baseArea + lateralArea;

            string baseTypeName = BaseType switch
            {
                "Triangle" => "Tam giác",
                "Square" => "Hình vuông",
                "Pentagon" => "Ngũ giác",
                "Hexagon" => "Lục giác",
                _ => "Hình vuông"
            };

            txtBaseType.Text = $"Loại đáy: {baseTypeName}";
            txtBaseSize.Text = $"Cạnh đáy: {BaseSize:N0} mm";
            txtHeight.Text = $"Chiều cao (h): {Height:N0} mm";
            txtBaseEdges.Text = $"Số cạnh đáy: {sides}";
            txtVertices.Text = $"Số đỉnh: {vertexCount}";
            txtVolume.Text = $"Thể tích: {volume:N0} mm³";
            txtBaseArea.Text = $"Diện tích đáy: {baseArea:N2} mm²";
            txtLateralArea.Text = $"Diện tích xung quanh: {lateralArea:N2} mm²";
            txtTotalArea.Text = $"Diện tích toàn phần: {totalArea:N2} mm²";
        }

        private void DrawPrism()
        {
            if (canvas3D == null || canvas3D.ActualWidth == 0 || canvas3D.ActualHeight == 0) 
                return; // Safety check - wait for canvas to be rendered
            
            canvas3D.Children.Clear();

            var rotatedVertices = vertices.Select(v => RotateVertex(v, RotationX, RotationY, RotationZ)).ToList();

            // Calculate face depths for sorting
            foreach (var face in faces)
            {
                double sumZ = 0;
                foreach (int idx in face.VertexIndices)
                {
                    sumZ += rotatedVertices[idx].Z;
                }
                face.AverageZ = sumZ / face.VertexIndices.Count;
            }

            var sortedFaces = faces.OrderBy(f => f.AverageZ).ToList();

            // Draw faces (Solid mode)
            if (DrawingMode == "Solid" || DrawingMode == "Both")
            {
                foreach (var face in sortedFaces)
                {
                    var points = face.VertexIndices.Select(idx => Project3DTo2D(rotatedVertices[idx])).ToList();
                    
                    var polygon = new Polygon
                    {
                        Points = new PointCollection(points),
                        Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(Opacity * 255),
                            FaceColor.R,
                            FaceColor.G,
                            FaceColor.B))
                    };

                    canvas3D.Children.Add(polygon);
                }
            }

            // Draw edges (Wireframe mode)
            if (DrawingMode == "Wireframe" || DrawingMode == "Both")
            {
                // Calculate face normals for hidden edge detection
                bool[] isFaceBack = new bool[faces.Count];
                for (int i = 0; i < faces.Count; i++)
                {
                    var f = faces[i];
                    if (f.VertexIndices.Count >= 3)
                    {
                        var v1 = rotatedVertices[f.VertexIndices[0]];
                        var v2 = rotatedVertices[f.VertexIndices[1]];
                        var v3 = rotatedVertices[f.VertexIndices[2]];
                        var normal = CalculateFaceNormal(v1, v2, v3);
                        isFaceBack[i] = normal.Z >= 0;
                    }
                }

                var drawnEdges = new HashSet<string>();

                foreach (var face in faces)
                {
                    for (int i = 0; i < face.VertexIndices.Count; i++)
                    {
                        int v1 = face.VertexIndices[i];
                        int v2 = face.VertexIndices[(i + 1) % face.VertexIndices.Count];

                        string edgeKey = v1 < v2 ? $"{v1}-{v2}" : $"{v2}-{v1}";
                        if (drawnEdges.Contains(edgeKey)) continue;
                        drawnEdges.Add(edgeKey);

                        // Find all faces sharing this edge
                        var sharingFaces = new List<int>();
                        for (int fi = 0; fi < faces.Count; fi++)
                        {
                            if (faces[fi].VertexIndices.Contains(v1) && faces[fi].VertexIndices.Contains(v2))
                            {
                                sharingFaces.Add(fi);
                            }
                        }

                        bool isHidden = sharingFaces.Count > 0 && sharingFaces.All(fi => isFaceBack[fi]);

                        var p1 = Project3DTo2D(rotatedVertices[v1]);
                        var p2 = Project3DTo2D(rotatedVertices[v2]);

                        var line = new Line
                        {
                            X1 = p1.X,
                            Y1 = p1.Y,
                            X2 = p2.X,
                            Y2 = p2.Y,
                            Stroke = new SolidColorBrush(EdgeColor),
                            StrokeThickness = EdgeThickness
                        };

                        if (isHidden && ShowHiddenEdges)
                        {
                            line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                        }

                        canvas3D.Children.Add(line);
                    }
                }
            }

            // Vẽ nhãn đỉnh
            if (ShowVertexLabels)
            {
                int sides = GetBaseTypeSides(BaseType);
                string[] bottomNames = sides switch
                {
                    3 => new[] { "A", "B", "C" },
                    4 => new[] { "A", "B", "C", "D" },
                    5 => new[] { "A", "B", "C", "D", "E" },
                    6 => new[] { "A", "B", "C", "D", "E", "F" },
                    _ => new[] { "A", "B", "C", "D" }
                };
                string[] topNames = bottomNames.Select(n => n + "'").ToArray();
                
                // Top face labels
                for (int i = 0; i < sides; i++)
                {
                    var pt = Project3DTo2D(rotatedVertices[i]);
                    TextBlock label = new TextBlock
                    {
                        Text = topNames[i],
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, pt.X + 8);
                    Canvas.SetTop(label, pt.Y - 16);
                    canvas3D.Children.Add(label);
                }
                // Bottom face labels
                for (int i = 0; i < sides; i++)
                {
                    var pt = Project3DTo2D(rotatedVertices[sides + i]);
                    TextBlock label = new TextBlock
                    {
                        Text = bottomNames[i],
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, pt.X + 8);
                    Canvas.SetTop(label, pt.Y - 16);
                    canvas3D.Children.Add(label);
                }
            }
        }

        private Point3D RotateVertex(Point3D vertex, double angleX, double angleY, double angleZ)
        {
            double radX = angleX * Math.PI / 180;
            double radY = angleY * Math.PI / 180;
            double radZ = angleZ * Math.PI / 180;

            // Rotate around X axis
            double y1 = vertex.Y * Math.Cos(radX) - vertex.Z * Math.Sin(radX);
            double z1 = vertex.Y * Math.Sin(radX) + vertex.Z * Math.Cos(radX);

            // Rotate around Y axis
            double x2 = vertex.X * Math.Cos(radY) + z1 * Math.Sin(radY);
            double z2 = -vertex.X * Math.Sin(radY) + z1 * Math.Cos(radY);

            // Rotate around Z axis
            double x3 = x2 * Math.Cos(radZ) - y1 * Math.Sin(radZ);
            double y3 = x2 * Math.Sin(radZ) + y1 * Math.Cos(radZ);

            return new Point3D(x3, y3, z2);
        }

        private Point Project3DTo2D(Point3D point)
        {
            double distance = 600;
            double scale = distance / (distance + point.Z);
            double x2D = point.X * scale + canvas3D.ActualWidth / 2 + OffsetX;
            double y2D = point.Y * scale + canvas3D.ActualHeight / 2 + OffsetY;
            return new Point(x2D, y2D);
        }

        // Event Handlers
        private void OnBaseTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBaseType.SelectedItem is ComboBoxItem item && item.Tag is string baseType)
            {
                BaseType = baseType;
                InitializePrism();
                DrawPrism();
            }
        }

        private void OnRotationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderRotationX == null || sliderRotationY == null || sliderRotationZ == null) return;
            
            RotationX = sliderRotationX.Value;
            RotationY = sliderRotationY.Value;
            RotationZ = sliderRotationZ.Value;
            DrawPrism();
        }

        private void OnDimensionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderBaseSize == null || sliderHeight == null) return;
            
            BaseSize = sliderBaseSize.Value;
            Height = sliderHeight.Value;
            InitializePrism();
            DrawPrism();
        }

        private void OnThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderThickness == null) return;

            EdgeThickness = sliderThickness.Value;
            DrawPrism();
        }

        private void OnDrawingModeChanged(object sender, RoutedEventArgs e)
        {
            if (rbWireframe.IsChecked == true)
                DrawingMode = "Wireframe";
            else if (rbSolid.IsChecked == true)
                DrawingMode = "Solid";
            else if (rbBoth.IsChecked == true)
                DrawingMode = "Both";

            DrawPrism();
        }

        private void OnEdgeColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEdgeColor == null || EdgeColorPreview == null) return;
            
            if (cmbEdgeColor.SelectedItem is ComboBoxItem item && item.Tag is string colorName)
            {
                EdgeColor = (Color)ColorConverter.ConvertFromString(colorName);
                EdgeColorPreview.Background = new SolidColorBrush(EdgeColor);
                DrawPrism();
            }
        }

        private void OnFaceColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFaceColor == null || FaceColorPreview == null || rbBoth == null) return;
            
            if (cmbFaceColor.SelectedItem is ComboBoxItem item && item.Tag is string colorName)
            {
                FaceColor = (Color)ColorConverter.ConvertFromString(colorName);
                FaceColorPreview.Background = new SolidColorBrush(FaceColor);
                DrawPrism();

                // Auto-switch to Solid or Both mode when face color is selected
                if (FaceColor != Colors.Transparent && DrawingMode == "Wireframe")
                {
                    rbBoth.IsChecked = true;
                }
            }
        }

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderOpacity == null) return;
            
            Opacity = sliderOpacity.Value / 100.0;
            DrawPrism();
        }

        private void OnAutoRotateChanged(object sender, RoutedEventArgs e)
        {
            if (chkAutoRotate == null || sliderRotationY == null) return;
            
            IsAutoRotate = chkAutoRotate.IsChecked == true;

            if (IsAutoRotate)
            {
                autoRotateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(30)
                };
                autoRotateTimer.Tick += (s, args) =>
                {
                    sliderRotationY.Value = (sliderRotationY.Value + 2) % 360;
                };
                autoRotateTimer.Start();
            }
            else
            {
                autoRotateTimer?.Stop();
                autoRotateTimer = null;
            }
        }

        private Point3D CalculateFaceNormal(Point3D v1, Point3D v2, Point3D v3)
        {
            double ax = v2.X - v1.X;
            double ay = v2.Y - v1.Y;
            double az = v2.Z - v1.Z;

            double bx = v3.X - v1.X;
            double by = v3.Y - v1.Y;
            double bz = v3.Z - v1.Z;

            return new Point3D(
                ay * bz - az * by,
                az * bx - ax * bz,
                ax * by - ay * bx
            );
        }

        private void chkShowHiddenEdges_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowHiddenEdges == null) return;
            ShowHiddenEdges = chkShowHiddenEdges.IsChecked ?? true;
            DrawPrism();
        }

        private void chkShowVertexLabels_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowVertexLabels == null) return;
            ShowVertexLabels = chkShowVertexLabels.IsChecked ?? true;
            DrawPrism();
        }

        private void btnCopyStats_Click(object sender, RoutedEventArgs e)
        {
            int sides = GetBaseTypeSides(BaseType);
            double baseArea = CalculateBaseArea(BaseSize, BaseType);
            double volume = CalculateVolume(BaseSize, Height, BaseType);
            double lateralArea = CalculateLateralArea(BaseSize, Height, BaseType);
            double totalArea = 2 * baseArea + lateralArea;

            string baseTypeName = BaseType switch
            {
                "Triangle" => "Tam giác",
                "Square" => "Hình vuông",
                "Pentagon" => "Ngũ giác",
                "Hexagon" => "Lục giác",
                _ => "Hình vuông"
            };

            string stats = $"=== THÔNG SỐ LĂNG TRỤ 3D ===\n\n" +
                          $"Loại đáy: {baseTypeName}\n" +
                          $"Cạnh đáy: {BaseSize:N0} mm\n" +
                          $"Chiều cao: {Height:N0} mm\n" +
                          $"Số cạnh đáy: {sides}\n" +
                          $"Số đỉnh: {sides * 2}\n\n" +
                          $"--- Tính toán ---\n" +
                          $"Thể tích: {volume:N2} mm³\n" +
                          $"Diện tích đáy: {baseArea:N2} mm²\n" +
                          $"Diện tích xung quanh: {lateralArea:N2} mm²\n" +
                          $"Diện tích toàn phần: {totalArea:N2} mm²\n\n" +
                          $"Xoay: X={RotationX:N0}°, Y={RotationY:N0}°, Z={RotationZ:N0}°";

            Clipboard.SetText(stats);
            MessageBox.Show("Đã sao chép thông số vào clipboard!", "Thành công", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var renderBitmap = new RenderTargetBitmap(
                    (int)canvas3D.ActualWidth,
                    (int)canvas3D.ActualHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

                renderBitmap.Render(canvas3D);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                string fileName = $"Prism3D_{BaseType}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    fileName);

                using (var fileStream = System.IO.File.Create(filePath))
                {
                    encoder.Save(fileStream);
                }

                MessageBox.Show($"Đã lưu ảnh thành công!\n{filePath}", "Thành công",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu ảnh: {ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            // Reset all controls to default
            cmbBaseType.SelectedIndex = 1; // Square
            sliderBaseSize.Value = 100;
            sliderHeight.Value = 200;
            sliderRotationX.Value = 0;
            sliderRotationY.Value = 30;
            sliderRotationZ.Value = 0;
            rbWireframe.IsChecked = true;
            cmbEdgeColor.SelectedIndex = 0; // Black
            cmbFaceColor.SelectedIndex = 0; // Transparent
            sliderOpacity.Value = 70;
            chkAutoRotate.IsChecked = false;

            MessageBox.Show("Đã đặt lại tất cả tham số!", "Thông báo",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            autoRotateTimer?.Stop();
            this.DialogResult = true;
            this.Close();
        }

        // Display stats button - Confirm and show shape with annotations on main board
        private void btnDisplayStats_Click(object sender, RoutedEventArgs e)
        {
            // Display Stats mode - copy shape + educational stats panel to dashboard
            IsConfirmed = true;
            DisplayStatsMode = true; // Flag to show stats panel on dashboard
            autoRotateTimer?.Stop();
            this.DialogResult = true;
            this.Close();
        }

        // Header close button
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            btnClose_Click(sender, e);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            autoRotateTimer?.Stop();
            this.Close();
        }
    }
}
