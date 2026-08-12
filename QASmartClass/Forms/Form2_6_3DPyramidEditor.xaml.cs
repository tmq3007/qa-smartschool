using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using Microsoft.Win32;

namespace QASmartTouch.Forms
{
    public partial class Form2_6_3DPyramidEditor : Window
    {
        // 3D Vertices and Faces
        private List<Point3D> vertices = new List<Point3D>();
        private List<Face> faces = new List<Face>();

        // Configuration properties (PUBLIC for MainDashboard access)
        public double RotationX { get; private set; } = 0;
        public double RotationY { get; private set; } = 45;
        public double RotationZ { get; private set; } = 0;
        public double BaseSize { get; private set; } = 120;
        public double PyramidHeight { get; private set; } = 180;
        public string BaseType { get; private set; } = "Square";
        public double EdgeThickness { get; private set; } = 1.5;
        public string DrawingMode { get; private set; } = "Wireframe";
        public Brush EdgeColor { get; private set; } = Brushes.Black;
        public Brush FaceColor { get; private set; } = Brushes.Transparent;
        public double OpacityValue { get; private set; } = 0.7;
        public bool IsConfirmed { get; private set; } = false;
        public bool DisplayStatsMode { get; private set; } = false;

        // Tùy chọn vẽ nét đứt cho cạnh khuất (Phương án 2)
        public bool ShowHiddenEdges { get; private set; } = true;

        public bool ShowVertexLabels { get; private set; } = true;

        // Drag & Drop properties
        private double OffsetX = 0;
        private double OffsetY = 0;
        private bool isDragging = false;
        private Point dragStartPoint;

        // Auto-rotate timer
        private DispatcherTimer autoRotateTimer;
        private bool isAutoRotating = false;

        public Form2_6_3DPyramidEditor()
        {
            InitializeComponent();
            InitializeAutoRotateTimer();
            this.Loaded += (s, e) =>
            {
                EnableDragDrop();
                DrawPyramid();
            };
        }

        private void EnableDragDrop()
        {
            canvas3D.MouseLeftButtonDown += Canvas3D_MouseLeftButtonDown;
            canvas3D.MouseMove += Canvas3D_MouseMove;
            canvas3D.MouseLeftButtonUp += Canvas3D_MouseLeftButtonUp;
            canvas3D.MouseLeave += Canvas3D_MouseLeave;
        }

        private void Canvas3D_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isDragging = true;
            dragStartPoint = e.GetPosition(canvas3D);
            canvas3D.CaptureMouse();
        }

        private void Canvas3D_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentPoint = e.GetPosition(canvas3D);
                OffsetX += currentPoint.X - dragStartPoint.X;
                OffsetY += currentPoint.Y - dragStartPoint.Y;
                dragStartPoint = currentPoint;
                DrawPyramid();
            }
        }

        private void Canvas3D_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        // ===== 3D GEOMETRY GENERATION =====

        private List<Point3D> GeneratePyramidVertices(double size, double height, string baseType)
        {
            var verts = new List<Point3D>();

            // Apex (top point) at (0, height/2, 0)
            verts.Add(new Point3D(0, height / 2, 0));

            // Generate base polygon vertices based on type
            int sides = GetBaseTypeSides(baseType);
            double angleStep = 2 * Math.PI / sides;
            double radius = GetCircumradius(size, sides);

            for (int i = 0; i < sides; i++)
            {
                double angle = i * angleStep - Math.PI / 2; // Start from top
                if (baseType == "Square")
                {
                    angle += Math.PI / 4; // Rotate 45° for square to align properly
                }
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                verts.Add(new Point3D(x, -height / 2, z));
            }

            // Base center point
            verts.Add(new Point3D(0, -height / 2, 0));

            return verts;
        }

        private List<Face> GeneratePyramidFaces(string baseType)
        {
            var pyramidFaces = new List<Face>();
            int sides = GetBaseTypeSides(baseType);
            int apexIndex = 0;
            int baseCenterIndex = sides + 1;

            // Lateral triangular faces (from apex to base edges)
            for (int i = 0; i < sides; i++)
            {
                int current = i + 1;
                int next = (i + 1) % sides + 1;
                
                var face = new Face
                {
                    VertexIndices = new List<int> { apexIndex, next, current }
                };
                pyramidFaces.Add(face);
            }

            // Base face (polygon - can be triangle, square, pentagon, or hexagon)
            var baseFace = new Face { VertexIndices = new List<int>() };
            for (int i = 0; i < sides; i++)
            {
                baseFace.VertexIndices.Add(i + 1);
            }
            pyramidFaces.Add(baseFace);

            return pyramidFaces;
        }

        // Helper methods for base type calculations
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
            // Circumradius formula: R = a / (2 * sin(π/n))
            return sideLength / (2 * Math.Sin(Math.PI / sides));
        }

        private double CalculateBaseArea(double size, string baseType)
        {
            int sides = GetBaseTypeSides(baseType);
            
            return baseType switch
            {
                "Triangle" => (Math.Sqrt(3) / 4) * size * size, // Equilateral triangle
                "Square" => size * size,
                "Pentagon" => (5 * size * size) / (4 * Math.Tan(Math.PI / 5)),
                "Hexagon" => (3 * Math.Sqrt(3) / 2) * size * size,
                _ => size * size
            };
        }

        private double CalculateSlantHeight(double size, double height, string baseType)
        {
            // Distance from apex to midpoint of base edge
            double apothem = size / (2 * Math.Tan(Math.PI / GetBaseTypeSides(baseType)));
            return Math.Sqrt(height * height + apothem * apothem);
        }

        // ===== 3D TRANSFORMATIONS =====

        private Point3D RotateVertex(Point3D vertex, double angleX, double angleY, double angleZ)
        {
            double radX = angleX * Math.PI / 180;
            double radY = angleY * Math.PI / 180;
            double radZ = angleZ * Math.PI / 180;

            double x = vertex.X, y = vertex.Y, z = vertex.Z;

            // Rotate around X axis
            double tempY = y * Math.Cos(radX) - z * Math.Sin(radX);
            double tempZ = y * Math.Sin(radX) + z * Math.Cos(radX);
            y = tempY;
            z = tempZ;

            // Rotate around Y axis
            double tempX = x * Math.Cos(radY) + z * Math.Sin(radY);
            tempZ = -x * Math.Sin(radY) + z * Math.Cos(radY);
            x = tempX;
            z = tempZ;

            // Rotate around Z axis
            tempX = x * Math.Cos(radZ) - y * Math.Sin(radZ);
            tempY = x * Math.Sin(radZ) + y * Math.Cos(radZ);
            x = tempX;
            y = tempY;

            return new Point3D(x, y, z);
        }

        private Point Project3DTo2D(Point3D point, double canvasWidth, double canvasHeight)
        {
            double distance = 600;
            double scale = distance / (distance + point.Z);
            double x2D = point.X * scale + canvasWidth / 2 + OffsetX;
            double y2D = -point.Y * scale + canvasHeight / 2 + OffsetY;
            return new Point(x2D, y2D);
        }

        private Vector3D CalculateFaceNormal(Point3D v1, Point3D v2, Point3D v3)
        {
            Vector3D edge1 = new Vector3D(v2.X - v1.X, v2.Y - v1.Y, v2.Z - v1.Z);
            Vector3D edge2 = new Vector3D(v3.X - v1.X, v3.Y - v1.Y, v3.Z - v1.Z);

            return new Vector3D(
                edge1.Y * edge2.Z - edge1.Z * edge2.Y,
                edge1.Z * edge2.X - edge1.X * edge2.Z,
                edge1.X * edge2.Y - edge1.Y * edge2.X
            );
        }

        // ===== MAIN DRAWING FUNCTION =====

        private void DrawPyramid()
        {
            if (canvas3D == null || canvas3D.ActualWidth == 0 || canvas3D.ActualHeight == 0) 
                return; // Safety check - wait for canvas to be rendered
            
            canvas3D.Children.Clear();

            // Generate geometry
            vertices = GeneratePyramidVertices(BaseSize, PyramidHeight, BaseType);
            faces = GeneratePyramidFaces(BaseType);

            // Rotate vertices
            var rotatedVertices = vertices.Select(v => RotateVertex(v, RotationX, RotationY, RotationZ)).ToList();

            // Project to 2D
            var projectedVertices = rotatedVertices.Select(v => Project3DTo2D(v, canvas3D.ActualWidth, canvas3D.ActualHeight)).ToList();

            // Sort faces by depth (painter's algorithm)
            var facesWithDepth = faces.Select(face =>
            {
                double avgZ = face.VertexIndices.Average(i => rotatedVertices[i].Z);
                return new { Face = face, Depth = avgZ };
            }).OrderBy(f => f.Depth).ToList();

            // Draw faces (Solid or Both mode)
            if (DrawingMode == "Solid" || DrawingMode == "Both")
            {
                foreach (var faceData in facesWithDepth)
                {
                    var face = faceData.Face;

                    var polygon = new Polygon();
                    var points = new PointCollection();

                    foreach (var index in face.VertexIndices)
                    {
                        points.Add(projectedVertices[index]);
                    }

                    polygon.Points = points;
                    
                    // Apply face color with opacity
                    if (FaceColor != Brushes.Transparent)
                    {
                        var color = ((SolidColorBrush)FaceColor).Color;
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(OpacityValue * 255), 
                            color.R, color.G, color.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    canvas3D.Children.Add(polygon);
                }
            }

            // Draw edges (Wireframe or Both mode) - PHÂN BIỆT NÉT LIỀN & NÉT ĐỨT
            if (DrawingMode == "Wireframe" || DrawingMode == "Both")
            {
                int sides = GetBaseTypeSides(BaseType);
                int apexIndex = 0;

                // Tính xem mặt nào là mặt sau
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

                // Hàm kiểm tra cạnh khuất (cả 2 mặt chia sẻ đều là mặt sau)
                bool IsEdgeHidden(int u, int v)
                {
                    List<int> sharingFaces = new List<int>();
                    for (int i = 0; i < faces.Count; i++)
                    {
                        if (faces[i].VertexIndices.Contains(u) && faces[i].VertexIndices.Contains(v))
                        {
                            sharingFaces.Add(i);
                        }
                    }
                    return sharingFaces.Count == 2 && isFaceBack[sharingFaces[0]] && isFaceBack[sharingFaces[1]];
                }
                
                // Draw base polygon edges
                for (int i = 0; i < sides; i++)
                {
                    int current = i + 1;
                    int next = (i + 1) % sides + 1;
                    DrawLine(projectedVertices[current], projectedVertices[next], EdgeColor, IsEdgeHidden(current, next));
                }

                // Draw lateral edges from apex to base vertices
                for (int i = 0; i < sides; i++)
                {
                    int baseVertex = i + 1;
                    DrawLine(projectedVertices[apexIndex], projectedVertices[baseVertex], EdgeColor, IsEdgeHidden(apexIndex, baseVertex));
                }

                // DỰNG ĐƯỜNG CAO HÌNH CHÓP SO (BẮT BUỘC NÉT KHUẤT)
                int centerIndex = sides + 1;
                DrawLine(projectedVertices[apexIndex], projectedVertices[centerIndex], EdgeColor, isHidden: true);
            }

            // Vẽ các nhãn đỉnh (A, B, C... và S, O)
            if (ShowVertexLabels && canvas3D != null)
            {
                int sides = GetBaseTypeSides(BaseType);
                
                // Nhãn đỉnh chóp S
                TextBlock labelS = new TextBlock
                {
                    Text = "S",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(labelS, projectedVertices[0].X + 6);
                Canvas.SetTop(labelS, projectedVertices[0].Y - 18);
                canvas3D.Children.Add(labelS);

                // Nhãn các đỉnh đáy (A, B, C...)
                for (int i = 1; i <= sides; i++)
                {
                    string name = ((char)('A' + i - 1)).ToString();
                    TextBlock labelBase = new TextBlock
                    {
                        Text = name,
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66))
                    };
                    Canvas.SetLeft(labelBase, projectedVertices[i].X + 6);
                    Canvas.SetTop(labelBase, projectedVertices[i].Y - 14);
                    canvas3D.Children.Add(labelBase);
                }

                // Nhãn tâm đáy O
                int centerIndex = sides + 1;
                TextBlock labelO = new TextBlock
                {
                    Text = "O",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0))
                };
                Canvas.SetLeft(labelO, projectedVertices[centerIndex].X + 6);
                Canvas.SetTop(labelO, projectedVertices[centerIndex].Y - 14);
                canvas3D.Children.Add(labelO);
            }

            // Update statistics
            UpdateStatistics();
        }

        private void DrawLine(Point p1, Point p2, Brush color, bool isHidden = false)
        {
            var line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = color,
                StrokeThickness = EdgeThickness
            };
            if (isHidden && ShowHiddenEdges)
            {
                line.StrokeDashArray = new DoubleCollection() { 4, 4 };
            }
            canvas3D.Children.Add(line);
        }

        // ===== STATISTICS CALCULATION =====

        private void UpdateStatistics()
        {
            string baseTypeName = BaseType switch
            {
                "Triangle" => "Tam giác",
                "Square" => "Hình vuông",
                "Pentagon" => "Ngũ giác",
                "Hexagon" => "Lục giác",
                _ => "Hình vuông"
            };

            int sides = GetBaseTypeSides(BaseType);
            int totalVertices = sides + 1; // base vertices + apex

            txtBaseType.Text = $"Loại đáy: {baseTypeName}";
            txtBaseSize.Text = $"Cạnh đáy: {BaseSize:F0} mm";
            txtHeight.Text = $"Chiều cao (h): {PyramidHeight:F0} mm";
            txtBaseEdges.Text = $"Số cạnh đáy: {sides}";
            txtVertices.Text = $"Số đỉnh: {totalVertices}";

            // Calculate base area
            double baseArea = CalculateBaseArea(BaseSize, BaseType);
            txtBaseArea.Text = $"Diện tích đáy: {baseArea:N0} mm²";

            // Calculate volume: V = (1/3) * base_area * height
            double volume = (1.0 / 3.0) * baseArea * PyramidHeight;
            txtVolume.Text = $"Thể tích: {volume:N0} mm³";

            // Calculate lateral surface area
            double slantHeight = CalculateSlantHeight(BaseSize, PyramidHeight, BaseType);
            double lateralArea = (sides * BaseSize * slantHeight) / 2;
            txtLateralArea.Text = $"Diện tích xung quanh: {lateralArea:N0} mm²";

            // Calculate total surface area
            double totalArea = baseArea + lateralArea;
            txtTotalArea.Text = $"Diện tích toàn phần: {totalArea:N0} mm²";
        }

        // ===== AUTO-ROTATE FEATURE =====

        private void InitializeAutoRotateTimer()
        {
            autoRotateTimer = new DispatcherTimer();
            autoRotateTimer.Interval = TimeSpan.FromMilliseconds(30);
            autoRotateTimer.Tick += AutoRotateTimer_Tick;
        }

        private void AutoRotateTimer_Tick(object? sender, EventArgs e)
        {
            if (isAutoRotating)
            {
                RotationY = (RotationY + 2) % 360;
                sliderRotationY.Value = RotationY;
                // DrawPyramid() will be called by OnRotationChanged
            }
        }

        private void OnAutoRotateChanged(object sender, RoutedEventArgs e)
        {
            isAutoRotating = chkAutoRotate.IsChecked == true;
            
            if (isAutoRotating)
            {
                autoRotateTimer.Start();
            }
            else
            {
                autoRotateTimer.Stop();
            }
        }

        // ===== EVENT HANDLERS =====

        private void OnBaseTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbBaseType == null || cmbBaseType.SelectedItem == null) return;

            var selectedItem = (ComboBoxItem)cmbBaseType.SelectedItem;
            BaseType = selectedItem.Tag.ToString() ?? "Square";

            DrawPyramid();
        }

        private void chkShowHiddenEdges_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ShowHiddenEdges = chkShowHiddenEdges.IsChecked ?? true;
            DrawPyramid();
        }

        private void chkShowVertexLabels_Click(object sender, RoutedEventArgs e)
        {
            ShowVertexLabels = chkShowVertexLabels.IsChecked ?? true;
            DrawPyramid();
        }

        private void OnRotationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderRotationX == null || sliderRotationY == null || sliderRotationZ == null) return;

            RotationX = sliderRotationX.Value;
            RotationY = sliderRotationY.Value;
            RotationZ = sliderRotationZ.Value;

            DrawPyramid();
        }

        private void OnDimensionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderBaseSize == null || sliderHeight == null) return;

            BaseSize = sliderBaseSize.Value;
            PyramidHeight = sliderHeight.Value;

            DrawPyramid();
        }

        private void OnThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderThickness == null) return;

            EdgeThickness = sliderThickness.Value;
            DrawPyramid();
        }

        private void OnDrawingModeChanged(object sender, RoutedEventArgs e)
        {
            if (rbWireframe == null) return;

            if (rbWireframe.IsChecked == true)
                DrawingMode = "Wireframe";
            else if (rbSolid.IsChecked == true)
                DrawingMode = "Solid";
            else if (rbBoth.IsChecked == true)
                DrawingMode = "Both";

            DrawPyramid();
        }

        private void OnEdgeColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEdgeColor.SelectedItem == null || EdgeColorPreview == null) return;

            var selectedItem = (ComboBoxItem)cmbEdgeColor.SelectedItem;
            string colorTag = selectedItem.Tag.ToString() ?? "Black";

            EdgeColor = colorTag switch
            {
                "Black" => Brushes.Black,
                "White" => Brushes.White,
                "Red" => Brushes.Red,
                "Green" => Brushes.Green,
                "Blue" => Brushes.Blue,
                "Yellow" => Brushes.Yellow,
                "Purple" => Brushes.Purple,
                "Orange" => Brushes.Orange,
                _ => Brushes.Black
            };

            EdgeColorPreview.Background = EdgeColor;
            DrawPyramid();
        }

        private void OnFaceColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFaceColor.SelectedItem == null || FaceColorPreview == null) return;

            var selectedItem = (ComboBoxItem)cmbFaceColor.SelectedItem;
            string colorTag = selectedItem.Tag.ToString() ?? "Transparent";

            // Auto-switch to appropriate drawing mode
            if (colorTag == "Transparent")
            {
                rbWireframe.IsChecked = true;
                FaceColor = Brushes.Transparent;
            }
            else
            {
                if (DrawingMode == "Wireframe")
                {
                    rbBoth.IsChecked = true;
                }

                FaceColor = colorTag switch
                {
                    "White" => Brushes.White,
                    "Red" => Brushes.Red,
                    "Green" => Brushes.Green,
                    "Blue" => Brushes.Blue,
                    "Yellow" => Brushes.Yellow,
                    "Purple" => Brushes.Purple,
                    "Orange" => Brushes.Orange,
                    "Brown" => Brushes.Brown,
                    _ => Brushes.Transparent
                };
            }

            FaceColorPreview.Background = FaceColor;
            DrawPyramid();
        }

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderOpacity == null) return;

            OpacityValue = sliderOpacity.Value / 100.0;
            DrawPyramid();
        }

        // ===== ACTION BUTTONS =====

        private void btnCopyStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set flags for "Display Stats" mode
                IsConfirmed = true;
                DisplayStatsMode = true;

                // Stop auto-rotate before closing
                if (autoRotateTimer != null && autoRotateTimer.IsEnabled)
                {
                    autoRotateTimer.Stop();
                }

                // Close dialog
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    Title = "Lưu hình ảnh Pyramid 3D",
                    FileName = $"Pyramid_{BaseType}_3D_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        (int)canvas3D.Width, (int)canvas3D.Height, 96, 96, PixelFormats.Pbgra32);
                    renderBitmap.Render(canvas3D);

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (FileStream file = File.Create(saveDialog.FileName))
                    {
                        encoder.Save(file);
                    }

                    MessageBox.Show($"✅ Đã lưu file thành công!\n{saveDialog.FileName}", 
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi khi lưu file: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            // Reset to default values
            sliderRotationX.Value = 0;
            sliderRotationY.Value = 45;
            sliderRotationZ.Value = 0;
            sliderBaseSize.Value = 120;
            sliderHeight.Value = 180;
            sliderOpacity.Value = 70;
            
            cmbBaseType.SelectedIndex = 1; // Square
            rbWireframe.IsChecked = true;
            cmbEdgeColor.SelectedIndex = 0;
            cmbFaceColor.SelectedIndex = 0;
            chkAutoRotate.IsChecked = false;

            DrawPyramid();

            MessageBox.Show("✅ Đã đặt lại về giá trị mặc định!", "Thông báo", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
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
            
            if (autoRotateTimer.IsEnabled)
            {
                autoRotateTimer.Stop();
            }
            
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (autoRotateTimer != null && autoRotateTimer.IsEnabled)
            {
                autoRotateTimer.Stop();
            }
            base.OnClosed(e);
        }
    }
}
