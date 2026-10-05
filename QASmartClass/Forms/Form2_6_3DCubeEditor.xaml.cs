using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Windows.Threading;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_6_3DCubeEditor : Window
    {
        // Thuộc tính cho Cube configuration
        public double RotationX { get; private set; }
        public double RotationY { get; private set; }
        public double RotationZ { get; private set; }
        public double CubeSize { get; private set; }
        public double EdgeThickness { get; private set; } = 1.5;
        public string DrawMode { get; private set; }
        public Color EdgeColor { get; private set; }
        public bool IsConfirmed { get; private set; }
        public bool DisplayStatsMode { get; private set; } = false;
        
        // Tùy chọn vẽ nét đứt cho cạnh khuất (Phương án 2)
        public bool ShowHiddenEdges { get; private set; } = true;

        // Tùy chọn hiển thị nhãn đỉnh
        public bool ShowVertexLabels { get; private set; } = true;

        // Màu cho các mặt của Cube (mặc định: màu xanh lá đơn sắc cho tất cả các mặt)
        public Color FaceColor { get; private set; }

        // Auto-rotate timer
        private DispatcherTimer autoRotateTimer;
        private bool isAutoRotating = false;

        // Thao tác cảm ứng / chuột kéo thả xoay trực tiếp
        private bool _isDragging = false;
        private Point _lastInteractionPoint;
        private int? _activeTouchId = null;

        public Form2_6_3DCubeEditor()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            InitializeAutoRotateTimer();
            
            // Khởi tạo giá trị mặc định - Khối lập phương đứng thẳng
            RotationX = 0;      // Không xoay trục X
            RotationY = 60;     // Xoay 60° để nhìn góc
            RotationZ = 0;      // Không xoay trục Z
            CubeSize = 150;
            DrawMode = "Wireframe"; // Mặc định là Khung dây
            EdgeColor = Colors.Black;
            FaceColor = Colors.Transparent; // Mặc định không màu
            IsConfirmed = false;

            // Vẽ lần đầu
            Loaded += (s, e) => DrawCube();
        }

        #region Event Handlers

        private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            RotationX = RotXSlider.Value;
            RotationY = RotYSlider.Value;
            RotationZ = RotZSlider.Value;

            RotXValue.Text = $"{(int)RotationX}°";
            RotYValue.Text = $"{(int)RotationY}°";
            RotZValue.Text = $"{(int)RotationZ}°";

            DrawCube();
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            CubeSize = SizeSlider.Value;
            SizeValue.Text = ((int)CubeSize).ToString();

            // Cập nhật thể tích
            double volume = Math.Pow(CubeSize, 3);
            VolumeText.Text = volume.ToString("N0");

            DrawCube();
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            EdgeThickness = ThicknessSlider.Value;
            ThicknessValue.Text = EdgeThickness.ToString("0.0");

            DrawCube();
        }

        private void DrawModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedItem = DrawModeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string content = selectedItem.Content.ToString();
            if (content.Contains("Wireframe"))
                DrawMode = "Wireframe";
            else if (content.Contains("Solid"))
                DrawMode = "Solid";
            else
                DrawMode = "Both";

            DrawCube();
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
            DrawCube();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            
            // Không hiển thị MessageBox nữa, chỉ đóng cửa sổ và apply
            this.DialogResult = true;
            this.Close();
        }

        private void chkShowHiddenEdges_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            ShowHiddenEdges = chkShowHiddenEdges.IsChecked ?? true;
            DrawCube();
        }

        private void chkShowVertexLabels_Click(object sender, RoutedEventArgs e)
        {
            ShowVertexLabels = chkShowVertexLabels.IsChecked ?? true;
            DrawCube();
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

            // Cập nhật preview - hiển thị màu trắng với viền cho transparent
            if (FaceColor == Colors.Transparent)
            {
                FaceColorPreview.Background = new SolidColorBrush(Colors.White);
                // Khi chọn "Không màu", chuyển về chế độ Wireframe nếu đang ở Solid
                if (DrawMode == "Solid")
                {
                    DrawModeComboBox.SelectedIndex = 0; // Wireframe
                }
            }
            else
            {
                FaceColorPreview.Background = new SolidColorBrush(FaceColor);
                // Khi chọn màu mặt, tự động chuyển sang chế độ "Cả hai" để hiển thị màu
                if (DrawMode == "Wireframe")
                {
                    DrawModeComboBox.SelectedIndex = 2; // Both (Cả hai)
                }
            }
            
            DrawCube();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Tạo RenderTargetBitmap
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                    (int)CubeCanvas.ActualWidth,
                    (int)CubeCanvas.ActualHeight,
                    96d, 96d,
                    PixelFormats.Pbgra32
                );

                renderBitmap.Render(CubeCanvas);

                // Mở SaveFileDialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = $"Cube-3D-{DateTime.Now:yyyyMMdd-HHmmss}.png",
                    Title = "Lưu hình ảnh khối lập phương"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (FileStream stream = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                        encoder.Save(stream);
                    }

                    MessageBox.Show(
                        $"💾 Đã lưu hình ảnh thành công!\n\nĐường dẫn: {saveDialog.FileName}",
                        "Tải ảnh thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ Lỗi khi lưu ảnh:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset về giá trị mặc định - Khối đứng thẳng
            RotXSlider.Value = 0;
            RotYSlider.Value = 60;
            RotZSlider.Value = 0;
            SizeSlider.Value = 150;
            DrawModeComboBox.SelectedIndex = 0; // Wireframe
            EdgeColorComboBox.SelectedIndex = 0; // Đen
            FaceColorComboBox.SelectedIndex = 0; // Không màu
            if (chkAutoRotate.IsChecked == true)
            {
                chkAutoRotate.IsChecked = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.DialogResult = false;
            this.Close();
        }

        #region Canvas Direct Interaction (Touch & Mouse Drag)

        private void CubeCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                _isDragging = true;
                _lastInteractionPoint = e.GetPosition(CubeCanvas);
                CubeCanvas.CaptureMouse();
            }
        }

        private void CubeCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(CubeCanvas);
                double deltaX = currentPoint.X - _lastInteractionPoint.X;
                double deltaY = currentPoint.Y - _lastInteractionPoint.Y;

                UpdateRotationFromDrag(deltaX, deltaY);
                _lastInteractionPoint = currentPoint;
            }
        }

        private void CubeCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                CubeCanvas.ReleaseMouseCapture();
            }
        }

        private void CubeCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                CubeCanvas.ReleaseMouseCapture();
            }
        }

        private void CubeCanvas_TouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _activeTouchId = e.TouchDevice.Id;
                _lastInteractionPoint = e.GetTouchPoint(CubeCanvas).Position;
                CubeCanvas.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void CubeCanvas_TouchMove(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (_isDragging && _activeTouchId == e.TouchDevice.Id)
            {
                Point currentPoint = e.GetTouchPoint(CubeCanvas).Position;
                double deltaX = currentPoint.X - _lastInteractionPoint.X;
                double deltaY = currentPoint.Y - _lastInteractionPoint.Y;

                UpdateRotationFromDrag(deltaX, deltaY);
                _lastInteractionPoint = currentPoint;
                e.Handled = true;
            }
        }

        private void CubeCanvas_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (_isDragging && _activeTouchId == e.TouchDevice.Id)
            {
                _isDragging = false;
                _activeTouchId = null;
                CubeCanvas.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void UpdateRotationFromDrag(double deltaX, double deltaY)
        {
            if (isAutoRotating)
            {
                chkAutoRotate.IsChecked = false;
            }

            double newY = (RotYSlider.Value + deltaX * 0.6) % 360;
            if (newY < 0) newY += 360;

            double newX = (RotXSlider.Value - deltaY * 0.6) % 360;
            if (newX < 0) newX += 360;

            RotYSlider.Value = Math.Round(newY);
            RotXSlider.Value = Math.Round(newX);
        }

        private void CubeCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double step = (e.Delta > 0) ? 10 : -10;
            double newValue = SizeSlider.Value + step;
            if (newValue >= SizeSlider.Minimum && newValue <= SizeSlider.Maximum)
            {
                SizeSlider.Value = newValue;
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

        #endregion

        #region 3D Drawing Logic

        /// <summary>
        /// Định nghĩa 8 đỉnh của khối lập phương trong không gian 3D
        /// </summary>
        private double[,] GetCubeVertices3D(double size)
        {
            double s = size / 2;
            return new double[,]
            {
                { -s, -s, -s }, // 0: Trước-Dưới-Trái
                {  s, -s, -s }, // 1: Trước-Dưới-Phải
                {  s,  s, -s }, // 2: Trước-Trên-Phải
                { -s,  s, -s }, // 3: Trước-Trên-Trái
                { -s, -s,  s }, // 4: Sau-Dưới-Trái
                {  s, -s,  s }, // 5: Sau-Dưới-Phải
                {  s,  s,  s }, // 6: Sau-Trên-Phải
                { -s,  s,  s }  // 7: Sau-Trên-Trái
            };
        }

        /// <summary>
        /// Xoay điểm quanh trục X
        /// </summary>
        private double[] RotateX(double[] point, double angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            return new double[]
            {
                point[0],
                point[1] * cos - point[2] * sin,
                point[1] * sin + point[2] * cos
            };
        }

        /// <summary>
        /// Xoay điểm quanh trục Y
        /// </summary>
        private double[] RotateY(double[] point, double angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            return new double[]
            {
                point[0] * cos + point[2] * sin,
                point[1],
                -point[0] * sin + point[2] * cos
            };
        }

        /// <summary>
        /// Xoay điểm quanh trục Z
        /// </summary>
        private double[] RotateZ(double[] point, double angleDegrees)
        {
            double rad = angleDegrees * Math.PI / 180;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            return new double[]
            {
                point[0] * cos - point[1] * sin,
                point[0] * sin + point[1] * cos,
                point[2]
            };
        }

        /// <summary>
        /// Chiếu điểm 3D sang 2D với phối cảnh (perspective projection)
        /// </summary>
        private Point Project3DTo2D(double[] point3D, double canvasWidth, double canvasHeight)
        {
            // Khoảng cách từ camera đến màn hình
            double distance = 600;
            double z = point3D[2] + distance;
            
            // Tránh chia cho 0
            if (z < 1) z = 1;

            double scale = distance / z;
            
            double x2D = point3D[0] * scale + canvasWidth / 2;
            double y2D = point3D[1] * scale + canvasHeight / 2;

            return new Point(x2D, y2D);
        }

        /// <summary>
        /// Tính vector pháp tuyến của một mặt (face normal)
        /// </summary>
        private double[] CalculateFaceNormal(double[] v1, double[] v2, double[] v3)
        {
            // Vector cạnh 1
            double[] edge1 = new double[]
            {
                v2[0] - v1[0],
                v2[1] - v1[1],
                v2[2] - v1[2]
            };

            // Vector cạnh 2
            double[] edge2 = new double[]
            {
                v3[0] - v1[0],
                v3[1] - v1[1],
                v3[2] - v1[2]
            };

            // Tích có hướng (cross product)
            return new double[]
            {
                edge1[1] * edge2[2] - edge1[2] * edge2[1],
                edge1[2] * edge2[0] - edge1[0] * edge2[2],
                edge1[0] * edge2[1] - edge1[1] * edge2[0]
            };
        }

        /// <summary>
        /// Vẽ khối lập phương 3D
        /// </summary>
        private void DrawCube()
        {
            if (CubeCanvas == null || CubeCanvas.ActualWidth == 0 || CubeCanvas.ActualHeight == 0)
                return;

            CubeCanvas.Children.Clear();

            double canvasWidth = CubeCanvas.ActualWidth;
            double canvasHeight = CubeCanvas.ActualHeight;

            // Lấy 8 đỉnh 3D
            double[,] vertices3D = GetCubeVertices3D(CubeSize);

            // Áp dụng rotation cho từng đỉnh
            List<double[]> rotatedVertices = new List<double[]>();
            for (int i = 0; i < 8; i++)
            {
                double[] vertex = new double[] { vertices3D[i, 0], vertices3D[i, 1], vertices3D[i, 2] };
                
                // Áp dụng xoay theo thứ tự X -> Y -> Z
                vertex = RotateX(vertex, RotationX);
                vertex = RotateY(vertex, RotationY);
                vertex = RotateZ(vertex, RotationZ);
                
                rotatedVertices.Add(vertex);
            }

            // Chiếu sang 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, canvasWidth, canvasHeight));
            }

            // Định nghĩa 6 mặt (faces) - mỗi mặt có 4 đỉnh
            int[][] faces = new int[][]
            {
                new int[] { 0, 3, 2, 1 }, // Mặt trước (pháp tuyến hướng -z, về phía camera)
                new int[] { 4, 5, 6, 7 }, // Mặt sau   (pháp tuyến hướng +z, xa camera)
                new int[] { 0, 1, 5, 4 }, // Mặt dưới  (pháp tuyến hướng -y)
                new int[] { 3, 7, 6, 2 }, // Mặt trên  (pháp tuyến hướng +y)
                new int[] { 0, 4, 7, 3 }, // Mặt trái  (pháp tuyến hướng -x)
                new int[] { 1, 2, 6, 5 }  // Mặt phải  (pháp tuyến hướng +x)
            };

            // Tính độ sâu trung bình của mỗi mặt để sắp xếp
            List<(int faceIndex, double avgZ)> facesInfo = new List<(int, double)>();
            
            for (int i = 0; i < faces.Length; i++)
            {
                int[] face = faces[i];
                
                // Tính độ sâu trung bình
                double avgZ = (rotatedVertices[face[0]][2] + 
                              rotatedVertices[face[1]][2] + 
                              rotatedVertices[face[2]][2] + 
                              rotatedVertices[face[3]][2]) / 4.0;

                facesInfo.Add((i, avgZ));
            }

            // Sắp xếp mặt theo độ sâu (vẽ mặt xa trước, mặt gần sau)
            facesInfo.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // VẼ CÁC MẶT (SOLID MODE)
            if (DrawMode == "Solid" || DrawMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesInfo)
                {
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

                    CubeCanvas.Children.Add(polygon);
                }
            }

            // Tính xem mặt nào là mặt sau dựa trên pháp tuyến rotated (Z > 0 hướng đi xa camera)
            bool[] isFaceBack = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                int[] face = faces[i];
                double[] normal = CalculateFaceNormal(rotatedVertices[face[0]], rotatedVertices[face[1]], rotatedVertices[face[2]]);
                isFaceBack[i] = normal[2] >= 0;
            }

            // VẼ CÁC CẠNH - PHÂN BIỆT NÉT LIỀN & NÉT ĐỨT
            int[][] edges = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 2, 3 }, new int[] { 3, 0 }, // Mặt trước
                new int[] { 4, 5 }, new int[] { 5, 6 }, new int[] { 6, 7 }, new int[] { 7, 4 }, // Mặt sau
                new int[] { 0, 4 }, new int[] { 1, 5 }, new int[] { 2, 6 }, new int[] { 3, 7 }  // Cạnh nối
            };
            
            foreach (var edge in edges)
            {
                // Tìm các mặt chứa cạnh này (mỗi mặt f có 4 đỉnh)
                List<int> sharingFaces = new List<int>();
                for (int i = 0; i < 6; i++)
                {
                    int[] f = faces[i];
                    bool hasU = f[0] == edge[0] || f[1] == edge[0] || f[2] == edge[0] || f[3] == edge[0];
                    bool hasV = f[0] == edge[1] || f[1] == edge[1] || f[2] == edge[1] || f[3] == edge[1];
                    if (hasU && hasV)
                    {
                        sharingFaces.Add(i);
                    }
                }

                // Nếu cả hai mặt chia sẻ đều là mặt sau, vẽ nét đứt (cạnh khuất)
                bool isHidden = sharingFaces.Count == 2 && isFaceBack[sharingFaces[0]] && isFaceBack[sharingFaces[1]];

                Line line = new Line
                {
                    X1 = projected2D[edge[0]].X,
                    Y1 = projected2D[edge[0]].Y,
                    X2 = projected2D[edge[1]].X,
                    Y2 = projected2D[edge[1]].Y,
                    Stroke = new SolidColorBrush(EdgeColor),
                    StrokeThickness = EdgeThickness
                };

                if (isHidden && ShowHiddenEdges)
                {
                    line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                }

                CubeCanvas.Children.Add(line);
            }

            // Vẽ các đỉnh và nhãn đỉnh
            string[] vertexNames = { "A", "B", "C", "D", "A'", "B'", "C'", "D'" };
            for (int i = 0; i < 8; i++)
            {
                var point = projected2D[i];

                if (DrawMode == "Wireframe" || DrawMode == "Both")
                {
                    Ellipse dot = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush(EdgeColor)
                    };
                    Canvas.SetLeft(dot, point.X - 3);
                    Canvas.SetTop(dot, point.Y - 3);
                    CubeCanvas.Children.Add(dot);
                }

                // Nhãn chữ tên đỉnh
                if (ShowVertexLabels)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = vertexNames[i],
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)) // Màu BrandPrimaryColor
                    };
                    Canvas.SetLeft(label, point.X + 6);
                    Canvas.SetTop(label, point.Y - 15);
                    CubeCanvas.Children.Add(label);
                }
            }
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
            RotationY += 1;
            if (RotationY >= 360) RotationY = 0;
            
            RotYSlider.Value = RotationY;
            DrawCube();
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

        #endregion
    }
}
