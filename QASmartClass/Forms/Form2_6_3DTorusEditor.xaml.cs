using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class Form2_6_3DTorusEditor : Window
    {
        // Properties
        public double MajorRadius { get; private set; }
        public double MinorRadius { get; private set; }
        public double RotationX { get; private set; }
        public double RotationY { get; private set; }
        public double RotationZ { get; private set; }
        public string DrawMode { get; private set; }
        public Color EdgeColor { get; private set; }
        public Color FaceColor { get; private set; }
        public bool IsConfirmed { get; private set; }
        public bool DisplayStatsMode { get; private set; } = false;

        private int majorSegments = 32;
        private int minorSegments = 16;
        private string currentPreset = "donut";

        // Auto-rotate timer
        private DispatcherTimer autoRotateTimer;
        private bool isAutoRotating = false;

        public Form2_6_3DTorusEditor()
        {
            InitializeComponent();
            InitializeAutoRotateTimer();
            
            // Initialize values
            MajorRadius = 100;
            MinorRadius = 40;
            RotationX = 20;
            RotationY = 45;
            RotationZ = 0;
            DrawMode = "Both";
            EdgeColor = Colors.Black;
            FaceColor = Colors.Transparent;
            IsConfirmed = false;

            // Draw initial torus
            DrawTorus();
            UpdateInfoPanel();
        }

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
            DrawTorus();
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

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string preset = btn.Tag?.ToString() ?? "donut";
                currentPreset = preset;

                switch (preset)
                {
                    case "donut":
                        MajorRadiusSlider.Value = 100;
                        MinorRadiusSlider.Value = 40;
                        txtPreset.Text = "Donut chuẩn";
                        break;
                    case "necklace":
                        MajorRadiusSlider.Value = 120;
                        MinorRadiusSlider.Value = 20;
                        txtPreset.Text = "Vòng cổ";
                        break;
                    case "tire":
                        MajorRadiusSlider.Value = 80;
                        MinorRadiusSlider.Value = 60;
                        txtPreset.Text = "Lốp xe";
                        break;
                    case "ring":
                        MajorRadiusSlider.Value = 150;
                        MinorRadiusSlider.Value = 30;
                        txtPreset.Text = "Nhẫn mỏng";
                        break;
                    case "bubble":
                        MajorRadiusSlider.Value = 90;
                        MinorRadiusSlider.Value = 85;
                        txtPreset.Text = "Bong bóng";
                        break;
                    case "custom":
                        txtPreset.Text = "Tùy chỉnh";
                        break;
                }
            }
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            MajorRadius = MajorRadiusSlider.Value;
            MinorRadius = MinorRadiusSlider.Value;

            MajorRadiusValue.Text = MajorRadius.ToString("F0");
            MinorRadiusValue.Text = MinorRadius.ToString("F0");

            // Check warning condition
            if (MinorRadius >= MajorRadius)
            {
                WarningPanel.Visibility = Visibility.Visible;
            }
            else
            {
                WarningPanel.Visibility = Visibility.Collapsed;
            }

            DrawTorus();
            UpdateInfoPanel();
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

            DrawTorus();
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

            DrawTorus();
        }

        private void OnEdgeColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedItem = cmbEdgeColor.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string tag = selectedItem.Tag?.ToString() ?? "Black";
            EdgeColor = tag switch
            {
                "Black" => Colors.Black,
                "White" => Colors.White,
                "Red" => Colors.Red,
                "Green" => Colors.Green,
                "Blue" => Colors.Blue,
                "Yellow" => Colors.Yellow,
                "Purple" => Colors.Purple,
                "Orange" => Colors.Orange,
                _ => Colors.Black
            };

            DrawTorus();
        }

        private void OnFaceColorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedItem = cmbFaceColor.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string tag = selectedItem.Tag?.ToString() ?? "Transparent";
            FaceColor = tag switch
            {
                "Transparent" => Colors.Transparent,
                "White" => Color.FromArgb(180, 255, 255, 255),
                "Red" => Color.FromArgb(180, 255, 100, 100),
                "Green" => Color.FromArgb(180, 100, 255, 100),
                "Blue" => Color.FromArgb(180, 100, 150, 255),
                "Yellow" => Color.FromArgb(180, 255, 255, 100),
                "Purple" => Color.FromArgb(180, 200, 100, 255),
                "Orange" => Color.FromArgb(180, 255, 165, 0),
                "Brown" => Color.FromArgb(180, 139, 69, 19),
                _ => Colors.Transparent
            };

            DrawTorus();
        }

        private void UpdateInfoPanel()
        {
            txtMajorRadius.Text = $"{MajorRadius:N0} đơn vị";
            txtMinorRadius.Text = $"{MinorRadius:N0} đơn vị";

            // Calculate surface area: 4π²Rr
            double surfaceArea = 4 * Math.PI * Math.PI * MajorRadius * MinorRadius;
            txtSurfaceArea.Text = $"{surfaceArea:N0} đvdt";

            // Calculate volume: 2π²Rr²
            double volume = 2 * Math.PI * Math.PI * MajorRadius * MinorRadius * MinorRadius;
            txtVolume.Text = $"{volume:N0} đvtt";
        }

        private void DrawTorus()
        {
            TorusCanvas.Children.Clear();

            // Generate torus vertices
            List<Point3D> vertices = new List<Point3D>();
            
            for (int i = 0; i <= majorSegments; i++)
            {
                double u = 2 * Math.PI * i / majorSegments;
                
                for (int j = 0; j <= minorSegments; j++)
                {
                    double v = 2 * Math.PI * j / minorSegments;
                    
                    // Parametric equations for torus
                    double x = (MajorRadius + MinorRadius * Math.Cos(v)) * Math.Cos(u);
                    double y = MinorRadius * Math.Sin(v);
                    double z = (MajorRadius + MinorRadius * Math.Cos(v)) * Math.Sin(u);
                    
                    vertices.Add(new Point3D(x, y, z));
                }
            }

            // Rotate vertices
            List<Point3D> rotatedVertices = new List<Point3D>();
            foreach (var vertex in vertices)
            {
                rotatedVertices.Add(RotatePoint(vertex));
            }

            // Draw faces
            if (DrawMode == "Solid" || DrawMode == "Both")
            {
                for (int i = 0; i < majorSegments; i++)
                {
                    for (int j = 0; j < minorSegments; j++)
                    {
                        int idx1 = i * (minorSegments + 1) + j;
                        int idx2 = i * (minorSegments + 1) + (j + 1);
                        int idx3 = (i + 1) * (minorSegments + 1) + (j + 1);
                        int idx4 = (i + 1) * (minorSegments + 1) + j;

                        // Calculate face normal for lighting
                        Point3D p1 = rotatedVertices[idx1];
                        Point3D p2 = rotatedVertices[idx2];
                        Point3D p3 = rotatedVertices[idx3];
                        
                        double[] normal = CalculateFaceNormal(
                            new double[] { p1.X, p1.Y, p1.Z },
                            new double[] { p2.X, p2.Y, p2.Z },
                            new double[] { p3.X, p3.Y, p3.Z }
                        );

                        // Simple lighting (facing towards viewer = brighter)
                        double lightIntensity = Math.Max(0, -normal[2]) * 0.5 + 0.5;
                        
                        byte alpha = FaceColor.A;
                        byte r = (byte)(FaceColor.R * lightIntensity);
                        byte g = (byte)(FaceColor.G * lightIntensity);
                        byte b = (byte)(FaceColor.B * lightIntensity);
                        
                        Color shadedColor = Color.FromArgb(alpha, r, g, b);

                        // Draw quad as polygon
                        Polygon face = new Polygon
                        {
                            Fill = new SolidColorBrush(shadedColor),
                            Stroke = DrawMode == "Both" ? new SolidColorBrush(EdgeColor) : null,
                            StrokeThickness = DrawMode == "Both" ? 0.5 : 0
                        };

                        face.Points.Add(Project3DTo2D(rotatedVertices[idx1]));
                        face.Points.Add(Project3DTo2D(rotatedVertices[idx2]));
                        face.Points.Add(Project3DTo2D(rotatedVertices[idx3]));
                        face.Points.Add(Project3DTo2D(rotatedVertices[idx4]));

                        TorusCanvas.Children.Add(face);
                    }
                }
            }

            // Draw wireframe
            if (DrawMode == "Wireframe" || DrawMode == "Both")
            {
                for (int i = 0; i <= majorSegments; i++)
                {
                    for (int j = 0; j < minorSegments; j++)
                    {
                        int idx1 = i * (minorSegments + 1) + j;
                        int idx2 = i * (minorSegments + 1) + (j + 1);

                        DrawLine(rotatedVertices[idx1], rotatedVertices[idx2], EdgeColor);
                    }
                }

                for (int j = 0; j <= minorSegments; j++)
                {
                    for (int i = 0; i < majorSegments; i++)
                    {
                        int idx1 = i * (minorSegments + 1) + j;
                        int idx2 = (i + 1) * (minorSegments + 1) + j;

                        DrawLine(rotatedVertices[idx1], rotatedVertices[idx2], EdgeColor);
                    }
                }
            }
        }

        private Point3D RotatePoint(Point3D point)
        {
            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            // Convert to radians
            double radX = RotationX * Math.PI / 180;
            double radY = RotationY * Math.PI / 180;
            double radZ = RotationZ * Math.PI / 180;

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

            return new Point3D(x1, y1, z1);
        }

        private Point Project3DTo2D(Point3D point)
        {
            double distance = 600;
            double scale = distance / (distance - point.Z);

            double x = point.X * scale + TorusCanvas.Width / 2;
            double y = -point.Y * scale + TorusCanvas.Height / 2;

            return new Point(x, y);
        }

        private void DrawLine(Point3D start, Point3D end, Color color)
        {
            Point p1 = Project3DTo2D(start);
            Point p2 = Project3DTo2D(end);

            Line line = new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1
            };

            TorusCanvas.Children.Add(line);
        }

        private double[] CalculateFaceNormal(double[] v1, double[] v2, double[] v3)
        {
            double[] edge1 = new double[] { v2[0] - v1[0], v2[1] - v1[1], v2[2] - v1[2] };
            double[] edge2 = new double[] { v3[0] - v1[0], v3[1] - v1[1], v3[2] - v1[2] };

            double[] normal = new double[]
            {
                edge1[1] * edge2[2] - edge1[2] * edge2[1],
                edge1[2] * edge2[0] - edge1[0] * edge2[2],
                edge1[0] * edge2[1] - edge1[1] * edge2[0]
            };

            // Normalize
            double length = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
            if (length > 0)
            {
                normal[0] /= length;
                normal[1] /= length;
                normal[2] /= length;
            }

            return normal;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.DialogResult = true;
            this.Close();
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

        // Helper struct for 3D points
        private struct Point3D
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
    }
}
