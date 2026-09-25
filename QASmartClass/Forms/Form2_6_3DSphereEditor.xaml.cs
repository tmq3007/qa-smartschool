using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_6_3DSphereEditor : Window
    {
        // Properties - Public for access from MainDashboard
        public double RotationX { get; private set; }
        public double RotationY { get; private set; }
        public double RotationZ { get; private set; }
        public double SphereRadius { get; private set; }
        public int LatitudeSegments { get; private set; }
        public int LongitudeSegments { get; private set; }
        public double EdgeThickness { get; private set; } = 1.5;
        public string DrawMode { get; private set; }
        public Color EdgeColor { get; private set; }
        public Color FaceColor { get; private set; }
        public bool IsConfirmed { get; private set; }
        public bool DisplayStatsMode { get; private set; } = false;

        // Tùy chọn vẽ nét đứt cho cạnh khuất
        public bool ShowHiddenEdges { get; private set; } = true;

        // Auto-rotate timer
        private DispatcherTimer autoRotateTimer;
        private bool isAutoRotating = false;

        // Direct Canvas Drag/Touch Rotation state
        private Point _lastInteractionPoint;
        private bool _isDragging = false;
        private int? _activeTouchId = null;

        public Form2_6_3DSphereEditor()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            InitializeAutoRotateTimer();

            // Initialize default values
            RotationX = 0;
            RotationY = 60;
            RotationZ = 0;
            SphereRadius = 100;
            LatitudeSegments = 16;
            LongitudeSegments = 16;
            DrawMode = "Wireframe";
            EdgeColor = Colors.Black;
            FaceColor = Colors.Transparent;
            IsConfirmed = false;

            // Initial draw
            Loaded += (s, e) => DrawSphere();
            SizeChanged += (s, e) => { if (IsLoaded) DrawSphere(); };
        }

        private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            RotationX = RotXSlider.Value;
            RotationY = RotYSlider.Value;
            RotationZ = RotZSlider.Value;

            RotXValue.Text = $"{RotationX:N0}°";
            RotYValue.Text = $"{RotationY:N0}°";
            RotZValue.Text = $"{RotationZ:N0}°";

            DrawSphere();
        }

        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            SphereRadius = RadiusSlider.Value;
            RadiusValue.Text = $"{SphereRadius:N0}";

            // Calculate surface area and volume
            double surfaceArea = 4 * Math.PI * SphereRadius * SphereRadius;
            double volume = (4.0 / 3.0) * Math.PI * Math.Pow(SphereRadius, 3);

            txtRadius.Text = $"{SphereRadius:N0}";
            txtSurfaceArea.Text = $"{surfaceArea:N0} đvdt";
            txtVolume.Text = $"{volume:N0} đvtt";

            DrawSphere();
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            EdgeThickness = ThicknessSlider.Value;
            ThicknessValue.Text = EdgeThickness.ToString("0.0");

            DrawSphere();
        }

        private void SegmentsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            LatitudeSegments = (int)LatSegmentsSlider.Value;
            LongitudeSegments = (int)LongSegmentsSlider.Value;

            LatSegmentsValue.Text = $"{LatitudeSegments}";
            LongSegmentsValue.Text = $"{LongitudeSegments}";

            txtLatSegments.Text = $"{LatitudeSegments}";
            txtLongSegments.Text = $"{LongitudeSegments}";

            DrawSphere();
        }

        private void DrawModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedItem = DrawModeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            DrawMode = selectedItem.Content.ToString() switch
            {
                "Khung dây (Wireframe)" => "Wireframe",
                "Tô màu mặt (Solid)" => "Solid",
                "Cả hai (Both)" => "Both",
                _ => "Wireframe"
            };

            DrawSphere();
        }

        private void chkShowHiddenEdges_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ShowHiddenEdges = chkShowHiddenEdges.IsChecked ?? true;
            DrawSphere();
        }

        private void EdgeColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedItem = EdgeColorComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string colorName = selectedItem.Content.ToString();
            EdgeColor = colorName switch
            {
                var s when s.Contains("Đen") || s.Contains("Black") => Colors.Black,
                var s when s.Contains("Trắng") || s.Contains("White") => Colors.White,
                var s when s.Contains("Đỏ") || s.Contains("Red") => Colors.Red,
                var s when s.Contains("Xanh lá") || s.Contains("Green") => Colors.Green,
                var s when s.Contains("Xanh dương") || s.Contains("Blue") => Colors.Blue,
                var s when s.Contains("Vàng") || s.Contains("Yellow") => Colors.Yellow,
                var s when s.Contains("Tím") || s.Contains("Purple") => Colors.Purple,
                var s when s.Contains("Cam") || s.Contains("Orange") => Colors.Orange,
                _ => Colors.Black
            };

            EdgeColorPreview.Background = new SolidColorBrush(EdgeColor);
            DrawSphere();
        }

        private void FaceColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedItem = FaceColorComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string colorName = selectedItem.Content.ToString();
            FaceColor = colorName switch
            {
                var s when s.Contains("Trong suốt") || s.Contains("Transparent") => Colors.Transparent,
                var s when s.Contains("Xanh lá") || s.Contains("Green") => Color.FromArgb(180, 100, 255, 100),
                var s when s.Contains("Xanh dương") || s.Contains("Blue") => Color.FromArgb(180, 100, 150, 255),
                var s when s.Contains("Đỏ") || s.Contains("Red") => Color.FromArgb(180, 255, 100, 100),
                var s when s.Contains("Vàng") || s.Contains("Yellow") => Color.FromArgb(180, 255, 255, 100),
                var s when s.Contains("Cam") || s.Contains("Orange") => Color.FromArgb(180, 255, 165, 0),
                var s when s.Contains("Tím") || s.Contains("Purple") => Color.FromArgb(180, 200, 100, 255),
                var s when s.Contains("Hồng") || s.Contains("Pink") => Color.FromArgb(180, 255, 150, 200),
                var s when s.Contains("Xám") || s.Contains("Gray") => Color.FromArgb(180, 150, 150, 150),
                _ => Colors.Transparent
            };

            // Update preview
            if (FaceColor == Colors.Transparent)
            {
                FaceColorPreview.Background = new SolidColorBrush(Colors.White);
                // Auto switch to Wireframe if selecting transparent
                if (DrawMode == "Solid")
                {
                    DrawModeComboBox.SelectedIndex = 0; // Wireframe
                }
            }
            else
            {
                FaceColorPreview.Background = new SolidColorBrush(FaceColor);
                // Auto switch to Both mode to show color
                if (DrawMode == "Wireframe")
                {
                    DrawModeComboBox.SelectedIndex = 2; // Both
                }
            }
            
            DrawSphere();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create RenderTargetBitmap
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    (int)Math.Max(1, SphereCanvas.ActualWidth),
                    (int)Math.Max(1, SphereCanvas.ActualHeight),
                    96d,
                    96d,
                    PixelFormats.Pbgra32);

                renderBitmap.Render(SphereCanvas);

                // Save to file
                string fileName = $"Sphere_3D_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string downloadsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    fileName);

                using (FileStream fs = new FileStream(downloadsPath, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    encoder.Save(fs);
                }

                MessageBox.Show(
                    $"Đã lưu hình ảnh thành công!\n\nĐường dẫn:\n{downloadsPath}",
                    "Tải xuống thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu hình ảnh:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset all values to default
            RotXSlider.Value = 0;
            RotYSlider.Value = 60;
            RotZSlider.Value = 0;
            RadiusSlider.Value = 100;
            LatSegmentsSlider.Value = 16;
            LongSegmentsSlider.Value = 16;
            DrawModeComboBox.SelectedIndex = 0; // Wireframe
            EdgeColorComboBox.SelectedIndex = 0; // Đen
            FaceColorComboBox.SelectedIndex = 0; // Không màu
            if (chkAutoRotate.IsChecked == true)
            {
                chkAutoRotate.IsChecked = false;
            }
        }

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

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.DialogResult = true;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.DialogResult = false;
            this.Close();
        }

        #region Canvas Direct Interaction (Touch & Mouse Drag)

        private void SphereCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _lastInteractionPoint = e.GetPosition(SphereCanvas);
                SphereCanvas.CaptureMouse();
            }
        }

        private void SphereCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(SphereCanvas);
                double deltaX = currentPoint.X - _lastInteractionPoint.X;
                double deltaY = currentPoint.Y - _lastInteractionPoint.Y;

                UpdateRotationFromDrag(deltaX, deltaY);
                _lastInteractionPoint = currentPoint;
            }
        }

        private void SphereCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                SphereCanvas.ReleaseMouseCapture();
            }
        }

        private void SphereCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                SphereCanvas.ReleaseMouseCapture();
            }
        }

        private void SphereCanvas_TouchDown(object sender, TouchEventArgs e)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _activeTouchId = e.TouchDevice.Id;
                _lastInteractionPoint = e.GetTouchPoint(SphereCanvas).Position;
                SphereCanvas.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void SphereCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            if (_isDragging && _activeTouchId == e.TouchDevice.Id)
            {
                Point currentPoint = e.GetTouchPoint(SphereCanvas).Position;
                double deltaX = currentPoint.X - _lastInteractionPoint.X;
                double deltaY = currentPoint.Y - _lastInteractionPoint.Y;

                UpdateRotationFromDrag(deltaX, deltaY);
                _lastInteractionPoint = currentPoint;
                e.Handled = true;
            }
        }

        private void SphereCanvas_TouchUp(object sender, TouchEventArgs e)
        {
            if (_isDragging && _activeTouchId == e.TouchDevice.Id)
            {
                _isDragging = false;
                _activeTouchId = null;
                SphereCanvas.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void UpdateRotationFromDrag(double deltaX, double deltaY)
        {
            // Tạm dừng tự động xoay nếu người dùng đang chủ động tương tác
            if (isAutoRotating)
            {
                chkAutoRotate.IsChecked = false;
            }

            // Kéo ngang -> Xoay quanh trục Y (quay trái/phải)
            double newY = (RotYSlider.Value + deltaX * 0.6) % 360;
            if (newY < 0) newY += 360;

            // Kéo dọc -> Xoay quanh trục X (nghiêng lên/xuống)
            double newX = (RotXSlider.Value - deltaY * 0.6) % 360;
            if (newX < 0) newX += 360;

            RotYSlider.Value = Math.Round(newY);
            RotXSlider.Value = Math.Round(newX);
        }

        private void SphereCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double step = (e.Delta > 0) ? 10 : -10;
            double newValue = RadiusSlider.Value + step;
            if (newValue >= RadiusSlider.Minimum && newValue <= RadiusSlider.Maximum)
            {
                RadiusSlider.Value = newValue;
            }
        }

        #endregion

        #region Preset View Handlers

        private void BtnPresetPerspective_Click(object sender, RoutedEventArgs e)
        {
            RotXSlider.Value = 0;
            RotYSlider.Value = 60;
            RotZSlider.Value = 0;
        }

        private void BtnPresetFront_Click(object sender, RoutedEventArgs e)
        {
            RotXSlider.Value = 0;
            RotYSlider.Value = 0;
            RotZSlider.Value = 0;
        }

        private void BtnPresetTop_Click(object sender, RoutedEventArgs e)
        {
            RotXSlider.Value = 90;
            RotYSlider.Value = 0;
            RotZSlider.Value = 0;
        }

        private void BtnPresetIsometric_Click(object sender, RoutedEventArgs e)
        {
            RotXSlider.Value = 30;
            RotYSlider.Value = 45;
            RotZSlider.Value = 0;
        }

        #endregion

        #region 3D Rendering Methods

        /// <summary>
        /// Main drawing method for sphere
        /// </summary>
        private void DrawSphere()
        {
            if (SphereCanvas == null || SphereCanvas.ActualWidth == 0 || SphereCanvas.ActualHeight == 0)
                return;

            SphereCanvas.Children.Clear();

            double canvasWidth = SphereCanvas.ActualWidth;
            double canvasHeight = SphereCanvas.ActualHeight;
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;

            // Generate sphere vertices
            List<double[]> vertices = GenerateSphereVertices(SphereRadius, LatitudeSegments, LongitudeSegments);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateVertex(vertex, RotationX, RotationY, RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, canvasWidth, canvasHeight));
            }

            // Generate faces (quads from sphere grid)
            List<int[]> faces = GenerateSphereFaces(LatitudeSegments, LongitudeSegments);

            // Calculate face visibility and depth
            List<(int faceIndex, double avgZ, bool isVisible)> facesInfo = new List<(int, double, bool)>();

            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];

                // Calculate average Z for depth sorting
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;

                // Calculate face normal for backface culling
                double[] normal = CalculateFaceNormal(
                    rotatedVertices[face[0]],
                    rotatedVertices[face[1]],
                    rotatedVertices[face[2]]
                );

                // View vector (from camera at 0,0,1)
                double[] viewVector = new double[] { 0, 0, 1 };

                // Dot product
                double dotProduct = normal[0] * viewVector[0] +
                                   normal[1] * viewVector[1] +
                                   normal[2] * viewVector[2];

                bool isVisible = dotProduct < 0;

                facesInfo.Add((i, avgZ, isVisible));
            }

            // Sort faces by depth (far to near)
            facesInfo.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // DRAW FACES (Solid mode)
            if (DrawMode == "Solid" || DrawMode == "Both")
            {
                foreach (var (faceIndex, avgZ, isVisible) in facesInfo)
                {
                    // Draw all faces to show full color
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon
                    {
                        Fill = new SolidColorBrush(FaceColor),
                        Stroke = Brushes.Transparent,
                        StrokeThickness = 0
                    };

                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    SphereCanvas.Children.Add(polygon);
                }
            }

            // DRAW EDGES (Wireframe mode)
            if (DrawMode == "Wireframe" || DrawMode == "Both")
            {
                // Hướng nhìn: Từ ngoài vào (Z dương = gần, Z âm = xa/bị che)
                
                // Draw latitude circles
                for (int lat = 0; lat < LatitudeSegments; lat++)
                {
                    for (int lon = 0; lon < LongitudeSegments; lon++)
                    {
                        int current = lat * (LongitudeSegments + 1) + lon;
                        int next = lat * (LongitudeSegments + 1) + ((lon + 1) % (LongitudeSegments + 1));

                        // Độ sâu trung bình của cạnh
                        double avgZ = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2;
                        bool isHidden = avgZ >= 0;

                        Line line = new Line
                        {
                            X1 = projected2D[current].X,
                            Y1 = projected2D[current].Y,
                            X2 = projected2D[next].X,
                            Y2 = projected2D[next].Y,
                            Stroke = new SolidColorBrush(EdgeColor),
                            StrokeThickness = EdgeThickness
                        };

                        if (isHidden && ShowHiddenEdges)
                        {
                            line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                        }

                        SphereCanvas.Children.Add(line);
                    }
                }

                // Draw longitude circles
                for (int lon = 0; lon <= LongitudeSegments; lon++)
                {
                    for (int lat = 0; lat < LatitudeSegments; lat++)
                    {
                        int current = lat * (LongitudeSegments + 1) + lon;
                        int next = (lat + 1) * (LongitudeSegments + 1) + lon;

                        // Độ sâu trung bình của cạnh
                        double avgZ = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2;
                        bool isHidden = avgZ >= 0;

                        Line line = new Line
                        {
                            X1 = projected2D[current].X,
                            Y1 = projected2D[current].Y,
                            X2 = projected2D[next].X,
                            Y2 = projected2D[next].Y,
                            Stroke = new SolidColorBrush(EdgeColor),
                            StrokeThickness = EdgeThickness
                        };

                        if (isHidden && ShowHiddenEdges)
                        {
                            line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                        }

                        SphereCanvas.Children.Add(line);
                    }
                }
            }
        }

        /// <summary>
        /// Generate sphere vertices using latitude/longitude approach
        /// </summary>
        private List<double[]> GenerateSphereVertices(double radius, int latSegments, int longSegments)
        {
            List<double[]> vertices = new List<double[]>();

            for (int lat = 0; lat <= latSegments; lat++)
            {
                double theta = lat * Math.PI / latSegments; // 0 to PI
                double sinTheta = Math.Sin(theta);
                double cosTheta = Math.Cos(theta);

                for (int lon = 0; lon <= longSegments; lon++)
                {
                    double phi = lon * 2 * Math.PI / longSegments; // 0 to 2*PI
                    double sinPhi = Math.Sin(phi);
                    double cosPhi = Math.Cos(phi);

                    double x = radius * sinTheta * cosPhi;
                    double y = radius * cosTheta;
                    double z = radius * sinTheta * sinPhi;

                    vertices.Add(new double[] { x, y, z });
                }
            }

            return vertices;
        }

        /// <summary>
        /// Generate sphere faces (quads)
        /// </summary>
        private List<int[]> GenerateSphereFaces(int latSegments, int longSegments)
        {
            List<int[]> faces = new List<int[]>();

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < longSegments; lon++)
                {
                    int first = lat * (longSegments + 1) + lon;
                    int second = first + longSegments + 1;

                    // Create quad as 4-vertex face
                    faces.Add(new int[] {
                        first,
                        second,
                        second + 1,
                        first + 1
                    });
                }
            }

            return faces;
        }

        /// <summary>
        /// Rotate vertex around X, Y, Z axes
        /// </summary>
        private double[] RotateVertex(double[] vertex, double angleX, double angleY, double angleZ)
        {
            double x = vertex[0];
            double y = vertex[1];
            double z = vertex[2];

            // Convert angles to radians
            double radX = angleX * Math.PI / 180;
            double radY = angleY * Math.PI / 180;
            double radZ = angleZ * Math.PI / 180;

            // Rotate around X axis
            double y1 = y * Math.Cos(radX) - z * Math.Sin(radX);
            double z1 = y * Math.Sin(radX) + z * Math.Cos(radX);
            y = y1;
            z = z1;

            // Rotate around Y axis
            double x1 = x * Math.Cos(radY) + z * Math.Sin(radY);
            z1 = -x * Math.Sin(radY) + z * Math.Cos(radY);
            x = x1;
            z = z1;

            // Rotate around Z axis
            x1 = x * Math.Cos(radZ) - y * Math.Sin(radZ);
            y1 = x * Math.Sin(radZ) + y * Math.Cos(radZ);
            x = x1;
            y = y1;

            return new double[] { x, y, z };
        }

        /// <summary>
        /// Project 3D point to 2D canvas
        /// </summary>
        private Point Project3DTo2D(double[] vertex, double canvasWidth, double canvasHeight)
        {
            double distance = 600;
            double scale = distance / (distance - vertex[2]);

            double x = vertex[0] * scale + canvasWidth / 2;
            double y = -vertex[1] * scale + canvasHeight / 2;

            return new Point(x, y);
        }

        /// <summary>
        /// Calculate face normal vector
        /// </summary>
        private double[] CalculateFaceNormal(double[] v1, double[] v2, double[] v3)
        {
            // Two edges of the triangle
            double[] edge1 = new double[] { v2[0] - v1[0], v2[1] - v1[1], v2[2] - v1[2] };
            double[] edge2 = new double[] { v3[0] - v1[0], v3[1] - v1[1], v3[2] - v1[2] };

            // Cross product
            double[] normal = new double[]
            {
                edge1[1] * edge2[2] - edge1[2] * edge2[1],
                edge1[2] * edge2[0] - edge1[0] * edge2[2],
                edge1[0] * edge2[1] - edge1[1] * edge2[0]
            };

            return normal;
        }

        #region Auto Rotate

        /// <summary>
        /// Initialize the auto-rotate timer
        /// </summary>
        private void InitializeAutoRotateTimer()
        {
            autoRotateTimer = new DispatcherTimer();
            autoRotateTimer.Interval = TimeSpan.FromMilliseconds(30);
            autoRotateTimer.Tick += AutoRotateTimer_Tick;
        }

        /// <summary>
        /// Timer tick event - rotates the sphere automatically
        /// </summary>
        private void AutoRotateTimer_Tick(object? sender, EventArgs e)
        {
            RotationY += 1;
            if (RotationY >= 360) RotationY = 0;
            RotYSlider.Value = RotationY;
            DrawSphere();
        }

        /// <summary>
        /// Handle auto-rotate checkbox state change
        /// </summary>
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

        #endregion

        #endregion
    }
}
