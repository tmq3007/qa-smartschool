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
    public partial class Form2_6_3DConeEditor : Window
    {
        // 3D Vertices and Faces
        private List<Point3D> vertices = new List<Point3D>();
        private List<Face> faces = new List<Face>();

        // Configuration properties (PUBLIC for MainDashboard access)
        public double RotationX { get; private set; } = 0;
        public double RotationY { get; private set; } = 60;
        public double RotationZ { get; private set; } = 0;
        public double BaseRadius { get; private set; } = 80;
        public double ConeHeight { get; private set; } = 160;
        public int Segments { get; private set; } = 20;
        public double EdgeThickness { get; private set; } = 1.5;
        public string DrawingMode { get; private set; } = "Wireframe";
        public Brush EdgeColor { get; private set; } = Brushes.Black;
        public Brush FaceColor { get; private set; } = Brushes.Transparent;
        public double OpacityValue { get; private set; } = 0.7;
        public bool ShowHiddenEdges { get; private set; } = true;
        public bool ShowVertexLabels { get; private set; } = true;
        public bool IsConfirmed { get; private set; } = false;
        public bool DisplayStatsMode { get; private set; } = false;

        // Drag & Drop properties
        private double OffsetX = 0;
        private double OffsetY = 0;
        private bool isDragging = false;
        private Point dragStartPoint;

        // Auto-rotate timer
        private DispatcherTimer autoRotateTimer;
        private bool isAutoRotating = false;

        public Form2_6_3DConeEditor()
        {
            InitializeComponent();
            InitializeAutoRotateTimer();
            this.Loaded += (s, e) =>
            {
                EnableDragDrop();
                DrawCone();
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
                DrawCone();
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

        private List<Point3D> GenerateConeVertices(double radius, double height, int segments)
        {
            var verts = new List<Point3D>();
            double angleStep = 2 * Math.PI / segments;

            // Apex (top point) at (0, height/2, 0)
            verts.Add(new Point3D(0, height / 2, 0));

            // Base circle vertices at y = -height/2
            for (int i = 0; i < segments; i++)
            {
                double angle = i * angleStep;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                verts.Add(new Point3D(x, -height / 2, z));
            }

            // Base center point
            verts.Add(new Point3D(0, -height / 2, 0));

            return verts;
        }

        private List<Face> GenerateConeFaces(int segments)
        {
            var coneFaces = new List<Face>();
            int apexIndex = 0;
            int baseCenterIndex = segments + 1;

            // Lateral triangular faces (from apex to base edge)
            for (int i = 0; i < segments; i++)
            {
                int current = i + 1;
                int next = (i + 1) % segments + 1;
                
                var face = new Face
                {
                    VertexIndices = new List<int> { apexIndex, next, current }
                };
                coneFaces.Add(face);
            }

            // Base triangular faces (from base center to edge)
            for (int i = 0; i < segments; i++)
            {
                int current = i + 1;
                int next = (i + 1) % segments + 1;
                
                var face = new Face
                {
                    VertexIndices = new List<int> { baseCenterIndex, current, next }
                };
                coneFaces.Add(face);
            }

            return coneFaces;
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

        private void DrawCone()
        {
            if (canvas3D == null || canvas3D.ActualWidth == 0 || canvas3D.ActualHeight == 0) 
                return; // Safety check - wait for canvas to be rendered
            
            canvas3D.Children.Clear();

            // Generate geometry
            vertices = GenerateConeVertices(BaseRadius, ConeHeight, Segments);
            faces = GenerateConeFaces(Segments);

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

            // Draw edges (Wireframe or Both mode)
            if (DrawingMode == "Wireframe" || DrawingMode == "Both")
            {
                int apexIndex = 0;
                
                // Draw base circle
                for (int i = 0; i < Segments; i++)
                {
                    int current = i + 1;
                    int next = (i + 1) % Segments + 1;
                    bool isHidden = (rotatedVertices[current].Z + rotatedVertices[next].Z) / 2 >= 0;
                    DrawLine(projectedVertices[current], projectedVertices[next], EdgeColor, isHidden);
                }

                // Draw lateral edges from apex to base
                for (int i = 0; i < Segments; i++)
                {
                    int baseVertex = i + 1;
                    bool isHidden = (rotatedVertices[apexIndex].Z + rotatedVertices[baseVertex].Z) / 2 >= 0;
                    DrawLine(projectedVertices[apexIndex], projectedVertices[baseVertex], EdgeColor, isHidden);
                }
            }

            // Vẽ nhãn đỉnh
            if (ShowVertexLabels)
            {
                // Apex label (S)
                TextBlock apexLabel = new TextBlock
                {
                    Text = "S",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(apexLabel, projectedVertices[0].X + 8);
                Canvas.SetTop(apexLabel, projectedVertices[0].Y - 20);
                canvas3D.Children.Add(apexLabel);
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

        // ===== STATISTICS CALCULATION =====

        private void UpdateStatistics()
        {
            txtBaseRadius.Text = $"Bán kính đáy (r): {BaseRadius:F0} mm";
            txtHeight.Text = $"Chiều cao (h): {ConeHeight:F0} mm";
            txtSegments.Text = $"Số cạnh: {Segments}";

            // Calculate slant height: l = sqrt(r² + h²)
            double slantHeight = Math.Sqrt(BaseRadius * BaseRadius + ConeHeight * ConeHeight);
            txtSlantHeight.Text = $"Đường sinh (l): {slantHeight:N2} mm";

            // Calculate volume: V = (1/3)πr²h
            double volume = (1.0 / 3.0) * Math.PI * BaseRadius * BaseRadius * ConeHeight;
            txtVolume.Text = $"Thể tích: {volume:N0} mm³";

            // Calculate lateral surface area: A_lateral = πrl
            double lateralArea = Math.PI * BaseRadius * slantHeight;
            txtLateralArea.Text = $"Diện tích xung quanh: {lateralArea:N0} mm²";

            // Calculate total surface area: A_total = πr(r + l)
            double totalArea = Math.PI * BaseRadius * (BaseRadius + slantHeight);
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
                // DrawCone() will be called by OnRotationChanged
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

        private void chkShowHiddenEdges_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowHiddenEdges == null) return;
            ShowHiddenEdges = chkShowHiddenEdges.IsChecked ?? true;
            DrawCone();
        }

        private void chkShowVertexLabels_Click(object sender, RoutedEventArgs e)
        {
            if (chkShowVertexLabels == null) return;
            ShowVertexLabels = chkShowVertexLabels.IsChecked ?? true;
            DrawCone();
        }

        // ===== EVENT HANDLERS =====

        private void OnRotationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderRotationX == null || sliderRotationY == null || sliderRotationZ == null) return;

            RotationX = sliderRotationX.Value;
            RotationY = sliderRotationY.Value;
            RotationZ = sliderRotationZ.Value;

            DrawCone();
        }

        private void OnDimensionChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderBaseRadius == null || sliderHeight == null || sliderSegments == null) return;

            BaseRadius = sliderBaseRadius.Value;
            ConeHeight = sliderHeight.Value;
            Segments = (int)sliderSegments.Value;

            DrawCone();
        }

        private void OnThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderThickness == null) return;

            EdgeThickness = sliderThickness.Value;
            DrawCone();
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

            DrawCone();
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
            DrawCone();
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
            DrawCone();
        }

        private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderOpacity == null) return;

            OpacityValue = sliderOpacity.Value / 100.0;
            DrawCone();
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

                // Close dialog with success
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
                    Title = "Lưu hình ảnh Cone 3D",
                    FileName = $"Cone_3D_{DateTime.Now:yyyyMMdd_HHmmss}.png"
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
            sliderRotationY.Value = 60;
            sliderRotationZ.Value = 0;
            sliderBaseRadius.Value = 80;
            sliderHeight.Value = 160;
            sliderSegments.Value = 20;
            sliderOpacity.Value = 70;
            
            rbWireframe.IsChecked = true;
            cmbEdgeColor.SelectedIndex = 0;
            cmbFaceColor.SelectedIndex = 0;
            chkAutoRotate.IsChecked = false;

            DrawCone();

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

    // Helper classes are already defined in Cylinder editor, reusing them here
}
