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
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_6_3DCylinderEditor : Window
    {
        // 3D Vertices and Faces
        private List<Point3D> vertices = new List<Point3D>();
        private List<Face> faces = new List<Face>();

        // Configuration properties (PUBLIC for MainDashboard access)
        public double RotationX { get; private set; } = 0;
        public double RotationY { get; private set; } = 60;
        public double RotationZ { get; private set; } = 0;
        public double Radius { get; private set; } = 75;
        public double Height { get; private set; } = 150;
        public int Segments { get; private set; } = 20;
        public double EdgeThickness { get; private set; } = 1.5;
        public string DrawingMode { get; private set; } = "Wireframe";
        public Brush EdgeColor { get; private set; } = Brushes.Black;
        public Brush FaceColor { get; private set; } = Brushes.Transparent;
        public double OpacityValue { get; private set; } = 0.7;
        public bool ShowHiddenEdges { get; private set; } = true;
        public bool ShowVertexLabels { get; private set; } = true;
        public bool IsConfirmed { get; private set; } = false;
        
        // Display Stats Mode flag (true if "Display Stats" button was clicked)
        public bool DisplayStatsMode { get; private set; } = false;

        // Drag & Drop properties
        private double OffsetX = 0;
        private double OffsetY = 0;
        private bool isDragging = false;
        private Point dragStartPoint;

        // Auto-rotate timer
        private DispatcherTimer autoRotateTimer;
        private bool isAutoRotating = false;

        public Form2_6_3DCylinderEditor()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            InitializeAutoRotateTimer();
            this.Loaded += (s, e) =>
            {
                EnableDragDrop();
                DrawCylinder();
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
            if (isAutoRotating)
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
            sliderRotationY.Value = 60;
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

        // ===== 3D GEOMETRY GENERATION =====

        private List<Point3D> GenerateCylinderVertices(double radius, double height, int segments)
        {
            var verts = new List<Point3D>();
            double angleStep = 2 * Math.PI / segments;

            // Top circle vertices (y = height/2)
            for (int i = 0; i < segments; i++)
            {
                double angle = i * angleStep;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                verts.Add(new Point3D(x, height / 2, z));
            }

            // Bottom circle vertices (y = -height/2)
            for (int i = 0; i < segments; i++)
            {
                double angle = i * angleStep;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                verts.Add(new Point3D(x, -height / 2, z));
            }

            // Center points for top and bottom caps
            verts.Add(new Point3D(0, height / 2, 0));  // Top center
            verts.Add(new Point3D(0, -height / 2, 0)); // Bottom center

            return verts;
        }

        private List<Face> GenerateCylinderFaces(int segments)
        {
            var cylinderFaces = new List<Face>();

            // Lateral faces (sides) - quads connecting top and bottom circles
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                var face = new Face
                {
                    VertexIndices = new List<int> 
                    { 
                        i,              // Top current
                        next,           // Top next
                        segments + next, // Bottom next
                        segments + i    // Bottom current
                    }
                };
                cylinderFaces.Add(face);
            }

            // Top cap - triangular faces from center to edge
            int topCenterIndex = segments * 2;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                var face = new Face
                {
                    VertexIndices = new List<int> { topCenterIndex, i, next }
                };
                cylinderFaces.Add(face);
            }

            // Bottom cap - triangular faces from center to edge
            int bottomCenterIndex = segments * 2 + 1;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                var face = new Face
                {
                    VertexIndices = new List<int> 
                    { 
                        bottomCenterIndex, 
                        segments + next, 
                        segments + i 
                    }
                };
                cylinderFaces.Add(face);
            }

            return cylinderFaces;
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

        private void DrawCylinder()
        {
            if (canvas3D == null || canvas3D.ActualWidth == 0 || canvas3D.ActualHeight == 0) 
                return; // Safety check - wait for canvas to be rendered
            
            canvas3D.Children.Clear();

            // Generate geometry
            vertices = GenerateCylinderVertices(Radius, Height, Segments);
            faces = GenerateCylinderFaces(Segments);

            // Rotate vertices
            var rotatedVertices = vertices.Select(v => RotateVertex(v, RotationX, RotationY, RotationZ)).ToList();

            // Project to 2D
            var projectedVertices = rotatedVertices.Select(v => Project3DTo2D(v, canvas3D.ActualWidth, canvas3D.ActualHeight)).ToList();

            // Sort faces by depth (painter's algorithm) for proper rendering
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

                    // Calculate face normal for backface culling (optional - disabled for educational purposes)
                    var v1 = rotatedVertices[face.VertexIndices[0]];
                    var v2 = rotatedVertices[face.VertexIndices[1]];
                    var v3 = rotatedVertices[face.VertexIndices[2]];
                    var normal = CalculateFaceNormal(v1, v2, v3);

                    // For educational purposes, we show all faces (no backface culling)
                    // bool isVisible = normal.Z < 0; // Uncomment to enable backface culling

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

            // Draw edges (Wireframe or Both mode)
            if (DrawingMode == "Wireframe" || DrawingMode == "Both")
            {
                for (int i = 0; i < Segments; i++)
                {
                    int next = (i + 1) % Segments;

                    // Top circle edge
                    bool isTopHidden = (rotatedVertices[i].Z + rotatedVertices[next].Z) / 2 >= 0;
                    DrawLine(projectedVertices[i], projectedVertices[next], EdgeColor, isTopHidden);

                    // Bottom circle edge
                    bool isBottomHidden = (rotatedVertices[Segments + i].Z + rotatedVertices[Segments + next].Z) / 2 >= 0;
                    DrawLine(projectedVertices[Segments + i], projectedVertices[Segments + next], EdgeColor, isBottomHidden);

                    // Vertical edge
                    bool isVertHidden = (rotatedVertices[i].Z + rotatedVertices[Segments + i].Z) / 2 >= 0;
                    DrawLine(projectedVertices[i], projectedVertices[Segments + i], EdgeColor, isVertHidden);
                }
            }

            // Vẽ nhãn tâm đáy (O, O')
            if (ShowVertexLabels)
            {
                // Top center O
                Point topCenter = new Point(
                    projectedVertices.Take(Segments).Average(p => p.X),
                    projectedVertices.Take(Segments).Average(p => p.Y)
                );
                TextBlock labelO = new TextBlock
                {
                    Text = "O",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(labelO, topCenter.X + 6);
                Canvas.SetTop(labelO, topCenter.Y - 15);
                canvas3D.Children.Add(labelO);

                // Bottom center O'
                Point bottomCenter = new Point(
                    projectedVertices.Skip(Segments).Take(Segments).Average(p => p.X),
                    projectedVertices.Skip(Segments).Take(Segments).Average(p => p.Y)
                );
                TextBlock labelOPrime = new TextBlock
                {
                    Text = "O'",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(labelOPrime, bottomCenter.X + 6);
                Canvas.SetTop(labelOPrime, bottomCenter.Y - 15);
                canvas3D.Children.Add(labelOPrime);
            }

            // Update statistics
            UpdateStatistics();
        }

        private void DrawLine(Point p1, Point p2, Brush color, bool isDashed = false)
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

            if (isDashed && ShowHiddenEdges)
            {
                line.StrokeDashArray = new DoubleCollection() { 4, 4 };
            }
            
            canvas3D.Children.Add(line);
        }

        private void chkShowHiddenEdges_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowHiddenEdges == null) return;
            ShowHiddenEdges = chkShowHiddenEdges.IsChecked ?? true;
            DrawCylinder();
        }

        private void chkShowVertexLabels_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowVertexLabels == null) return;
            ShowVertexLabels = chkShowVertexLabels.IsChecked ?? true;
            DrawCylinder();
        }

        // ===== STATISTICS CALCULATION =====

        private void UpdateStatistics()
        {
            txtRadius.Text = $"Bán kính (r): {Radius:F0} mm";
            txtHeight.Text = $"Chiều cao (h): {Height:F0} mm";
            txtSegments.Text = $"Số cạnh: {Segments}";

            // Calculate volume: V = πr²h
            double volume = Math.PI * Radius * Radius * Height;
            txtVolume.Text = $"Thể tích: {volume:N0} mm³";

            // Calculate lateral surface area: A_lateral = 2πrh
            double lateralSurface = 2 * Math.PI * Radius * Height;
            txtSurfaceArea.Text = $"Diện tích xung quanh: {lateralSurface:N0} mm²";

            // Calculate total surface area: A_total = 2πr(r + h)
            double totalSurface = 2 * Math.PI * Radius * (Radius + Height);
            txtTotalSurface.Text = $"Diện tích toàn phần: {totalSurface:N0} mm²";
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
                // DrawCylinder() will be called by OnRotationChanged
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

        private void OnRotationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderRotationX == null || sliderRotationY == null || sliderRotationZ == null) return;

            RotationX = sliderRotationX.Value;
            RotationY = sliderRotationY.Value;
            RotationZ = sliderRotationZ.Value;

            DrawCylinder();
        }

        private void OnDimensionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderRadius == null || sliderHeight == null || sliderSegments == null) return;

            Radius = sliderRadius.Value;
            Height = sliderHeight.Value;
            Segments = (int)sliderSegments.Value;

            DrawCylinder();
        }

        private void OnThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderThickness == null) return;

            EdgeThickness = sliderThickness.Value;
            DrawCylinder();
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

            DrawCylinder();
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
            DrawCylinder();
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
            DrawCylinder();
        }

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderOpacity == null) return;

            OpacityValue = sliderOpacity.Value / 100.0;
            DrawCylinder();
        }

        // ===== ACTION BUTTONS =====

        private void btnCopyStats_Click(object sender, RoutedEventArgs e)
        {
            // Display Stats mode - copy shape + educational stats panel to dashboard
            IsConfirmed = true;
            DisplayStatsMode = true; // Flag to show stats panel on dashboard
            
            if (autoRotateTimer.IsEnabled)
            {
                autoRotateTimer.Stop();
            }
            
            this.DialogResult = true;
            this.Close();
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    Title = "Lưu hình ảnh Cylinder 3D",
                    FileName = $"Cylinder_3D_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                        (int)canvas3D.ActualWidth, (int)canvas3D.ActualHeight, 96, 96, PixelFormats.Pbgra32);
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
            sliderRotationY.Value = 60;
            sliderRotationZ.Value = 0;
            sliderRadius.Value = 75;
            sliderHeight.Value = 150;
            sliderSegments.Value = 20;
            sliderOpacity.Value = 70;
            
            rbWireframe.IsChecked = true;
            cmbEdgeColor.SelectedIndex = 0;
            cmbFaceColor.SelectedIndex = 0;
            chkAutoRotate.IsChecked = false;

            DrawCylinder();

            MessageBox.Show("✅ Đã đặt lại về giá trị mặc định!", "Thông báo", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.DialogResult = true;
            this.Close();
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

    // ===== HELPER CLASSES =====

    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class Vector3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class Face
    {
        public List<int> VertexIndices { get; set; } = new List<int>();
    }
}
