using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_3DTetrahedronEditor : Window
    {
        // Properties
        public double EdgeLength { get; set; }
        public double Scale { get; set; }
        public double RotationX { get; set; }
        public double RotationY { get; set; }
        public double RotationZ { get; set; }
        public string DrawMode { get; set; }
        public string ColorMode { get; set; }
        public Color FaceColor1 { get; set; }
        public Color FaceColor2 { get; set; }
        public Color FaceColor3 { get; set; }
        public Color FaceColor4 { get; set; }
        public double Opacity { get; set; }
        public bool EnableLighting { get; set; }
        public bool EnableBackfaceCulling { get; set; }
        public bool ShowEdges { get; set; }
        public bool ShowVertices { get; set; }
        public bool ShowLabels { get; set; }
        public bool ShowAxis { get; set; }
        public bool ShowFaceEdges { get; set; }
        public bool ShowNormals { get; set; }
        public double EdgeWidth { get; set; }
        public double VertexSize { get; set; }
        public double AutoSpeed { get; set; }
        public bool IsConfirmed { get; set; }
        public bool DisplayStatsMode { get; set; } = false;

        private string currentType = "regular";
        private DispatcherTimer autoRotateTimer;
        private bool isAutoRotating = false;

        // Interaction state for Touch & Mouse Drag
        private bool _isDragging = false;
        private Point _lastInteractionPoint;
        private int? _activeTouchId = null;

        // Tetrahedron vertices in cube coordinates
        private struct Point3D
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public Point3D(double x, double y, double z)
            {
                X = x; Y = y; Z = z;
            }
        }

        public Form2_7_3DTetrahedronEditor()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);

            // Initialize default values
            EdgeLength = 150;
            Scale = 1.0;
            RotationX = -20;
            RotationY = 30;
            RotationZ = 0;
            DrawMode = "Both";
            ColorMode = "Multi";
            FaceColor1 = (Color)ColorConverter.ConvertFromString("#FF6B6B");
            FaceColor2 = (Color)ColorConverter.ConvertFromString("#4ECDC4");
            FaceColor3 = (Color)ColorConverter.ConvertFromString("#FFD93D");
            FaceColor4 = (Color)ColorConverter.ConvertFromString("#95E1D3");
            Opacity = 90;
            EnableLighting = true;
            EnableBackfaceCulling = true;
            ShowEdges = true;
            ShowVertices = false;
            ShowLabels = false;
            ShowAxis = false;
            ShowFaceEdges = false;
            ShowNormals = false;
            EdgeWidth = 2;
            VertexSize = 6;
            AutoSpeed = 1.0;
            IsConfirmed = false;

            // Initialize auto-rotate timer
            autoRotateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            autoRotateTimer.Tick += AutoRotateTimer_Tick;

            // Set active type button
            UpdateTypeButtonStyles();

            // Draw initial tetrahedron
            DrawTetrahedron();
            UpdateInfoPanel();
        }

        private void AutoRotateTimer_Tick(object sender, EventArgs e)
        {
            RotationY += AutoSpeed;
            if (RotationY >= 360) RotationY -= 360;

            RotYSlider.Value = RotationY;
            lblRotYValue.Text = $"{RotationY:N0}°";

            DrawTetrahedron();
        }

        private void TypeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                currentType = btn.Tag.ToString();
                UpdateTypeButtonStyles();
                
                // Update type label
                txtType.Text = currentType switch
                {
                    "regular" => "Tứ diện đều",
                    "right" => "Tứ diện vuông",
                    "isosceles" => "Tứ diện cân",
                    "custom" => "Tùy chỉnh",
                    _ => "Tứ diện đều"
                };

                DrawTetrahedron();
                UpdateInfoPanel();
            }
        }

        private void UpdateTypeButtonStyles()
        {
            // Reset all buttons
            btnRegular.Background = Brushes.White;
            btnRegular.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
            btnRight.Background = Brushes.White;
            btnRight.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
            btnIsosceles.Background = Brushes.White;
            btnIsosceles.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
            btnCustom.Background = Brushes.White;
            btnCustom.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));

            // Set active button
            Button activeBtn = currentType switch
            {
                "regular" => btnRegular,
                "right" => btnRight,
                "isosceles" => btnIsosceles,
                "custom" => btnCustom,
                _ => btnRegular
            };

            LinearGradientBrush gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#667eea"), 0));
            gradientBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#764ba2"), 1));
            
            activeBtn.Background = gradientBrush;
            activeBtn.Foreground = Brushes.White;
            activeBtn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#667eea"));
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            EdgeLength = EdgeLengthSlider.Value;
            Scale = ScaleSlider.Value;

            lblEdgeLengthValue.Text = EdgeLength.ToString("N0");
            lblScaleValue.Text = $"{Scale:F1}x";

            txtEdgeLength.Text = $"{EdgeLength:N0} đơn vị";

            DrawTetrahedron();
            UpdateInfoPanel();
        }

        private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            RotationX = RotXSlider.Value;
            RotationY = RotYSlider.Value;
            RotationZ = RotZSlider.Value;

            lblRotXValue.Text = $"{RotationX:N0}°";
            lblRotYValue.Text = $"{RotationY:N0}°";
            lblRotZValue.Text = $"{RotationZ:N0}°";

            DrawTetrahedron();
        }

        private void AutoRotate_Changed(object sender, RoutedEventArgs e)
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

        private void AutoSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            AutoSpeed = AutoSpeedSlider.Value;
            lblAutoSpeedValue.Text = $"{AutoSpeed:F1}x";
        }

        private void DrawMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedItem = cboDrawMode.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                DrawMode = selectedItem.Tag.ToString();
                DrawTetrahedron();
            }
        }

        private void ColorMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var selectedItem = cboColorMode.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                ColorMode = selectedItem.Tag.ToString();
                
                // Show/hide multi-color panel
                pnlMultiColor.Visibility = ColorMode == "Multi" ? Visibility.Visible : Visibility.Collapsed;
                
                DrawTetrahedron();
            }
        }

        private void FaceColor_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var combo = sender as ComboBox;
            var selectedItem = combo?.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                var colorString = selectedItem.Tag.ToString();
                var color = (Color)ColorConverter.ConvertFromString(colorString);

                if (combo == cboColor1) FaceColor1 = color;
                else if (combo == cboColor2) FaceColor2 = color;
                else if (combo == cboColor3) FaceColor3 = color;
                else if (combo == cboColor4) FaceColor4 = color;

                DrawTetrahedron();
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            Opacity = OpacitySlider.Value;
            lblOpacityValue.Text = $"{Opacity:N0}%";

            DrawTetrahedron();
        }

        private void Lighting_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            EnableLighting = chkEnableLighting.IsChecked == true;
            DrawTetrahedron();
        }

        private void BackfaceCulling_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            EnableBackfaceCulling = chkBackfaceCulling.IsChecked == true;
            DrawTetrahedron();
        }

        private void DisplayOption_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            ShowEdges = chkShowEdges.IsChecked == true;
            ShowVertices = chkShowVertices.IsChecked == true;
            ShowLabels = chkShowLabels.IsChecked == true;
            ShowAxis = chkShowAxis.IsChecked == true;
            ShowFaceEdges = chkShowFaceEdges.IsChecked == true;
            ShowNormals = chkShowNormals.IsChecked == true;

            DrawTetrahedron();
        }

        private void EdgeWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            EdgeWidth = EdgeWidthSlider.Value;
            lblEdgeWidthValue.Text = $"{EdgeWidth:F1}px";

            DrawTetrahedron();
        }

        private void VertexSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            VertexSize = VertexSizeSlider.Value;
            lblVertexSizeValue.Text = $"{VertexSize:N0}px";

            DrawTetrahedron();
        }

        private void DrawTetrahedron()
        {
            TetrahedronCanvas.Children.Clear();

            // Get tetrahedron vertices based on type
            var vertices = GetTetrahedronVertices();

            // Apply scale
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Point3D(
                    vertices[i].X * Scale,
                    vertices[i].Y * Scale,
                    vertices[i].Z * Scale
                );
            }

            // Rotate vertices
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = RotatePoint(vertices[i], RotationX, RotationY, RotationZ);
            }

            // Project to 2D
            Point[] projectedPoints = new Point[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                projectedPoints[i] = Project3DTo2D(vertices[i]);
            }

            // Define faces (4 triangular faces)
            int[][] faces = new int[][]
            {
                new int[] { 0, 2, 1 }, // Base (ABC)
                new int[] { 0, 1, 3 }, // Side 1 (ABD)
                new int[] { 1, 2, 3 }, // Side 2 (BCD)
                new int[] { 2, 0, 3 }  // Side 3 (CAD)
            };

            // Calculate face normals for backface culling and lighting
            double[] faceDepths = new double[faces.Length];
            bool[] faceVisible = new bool[faces.Length];

            for (int i = 0; i < faces.Length; i++)
            {
                var v1 = vertices[faces[i][0]];
                var v2 = vertices[faces[i][1]];
                var v3 = vertices[faces[i][2]];

                // Calculate average Z depth
                faceDepths[i] = (v1.Z + v2.Z + v3.Z) / 3;

                // Calculate normal for backface culling
                var normal = CalculateFaceNormal(v1, v2, v3);
                faceVisible[i] = !EnableBackfaceCulling || normal.Z < 0;
            }

            // Sort faces by depth (painter's algorithm)
            var faceIndices = Enumerable.Range(0, faces.Length).ToList();
            faceIndices.Sort((a, b) => faceDepths[a].CompareTo(faceDepths[b]));

            // Draw solid faces
            if (DrawMode == "Solid" || DrawMode == "Both")
            {
                foreach (int faceIndex in faceIndices)
                {
                    if (!faceVisible[faceIndex]) continue;

                    var face = faces[faceIndex];
                    var polygon = new Polygon();

                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projectedPoints[vertexIndex]);
                    }

                    // Get face color
                    Color baseColor = GetFaceColor(faceIndex);

                    // Apply lighting
                    if (EnableLighting)
                    {
                        var v1 = vertices[face[0]];
                        var v2 = vertices[face[1]];
                        var v3 = vertices[face[2]];
                        var normal = CalculateFaceNormal(v1, v2, v3);

                        // Simple directional lighting
                        double lightIntensity = Math.Max(0, -normal.Z) * 0.6 + 0.4;
                        baseColor = Color.FromRgb(
                            (byte)(baseColor.R * lightIntensity),
                            (byte)(baseColor.G * lightIntensity),
                            (byte)(baseColor.B * lightIntensity)
                        );
                    }

                    // Apply opacity
                    baseColor.A = (byte)(255 * Opacity / 100);

                    polygon.Fill = new SolidColorBrush(baseColor);
                    polygon.Stroke = ShowFaceEdges ? Brushes.Black : null;
                    polygon.StrokeThickness = ShowFaceEdges ? 1 : 0;

                    TetrahedronCanvas.Children.Add(polygon);
                }
            }

            // Draw wireframe edges
            if ((DrawMode == "Wireframe" || DrawMode == "Both") && ShowEdges)
            {
                int[][] edges = new int[][]
                {
                    new int[] { 0, 1 }, new int[] { 0, 2 }, new int[] { 0, 3 },
                    new int[] { 1, 2 }, new int[] { 1, 3 }, new int[] { 2, 3 }
                };

                foreach (var edge in edges)
                {
                    var line = new Line
                    {
                        X1 = projectedPoints[edge[0]].X,
                        Y1 = projectedPoints[edge[0]].Y,
                        X2 = projectedPoints[edge[1]].X,
                        Y2 = projectedPoints[edge[1]].Y,
                        Stroke = Brushes.Black,
                        StrokeThickness = EdgeWidth
                    };

                    TetrahedronCanvas.Children.Add(line);
                }
            }

            // Draw vertices
            if (ShowVertices)
            {
                for (int i = 0; i < projectedPoints.Length; i++)
                {
                    var ellipse = new Ellipse
                    {
                        Width = VertexSize,
                        Height = VertexSize,
                        Fill = Brushes.Red,
                        Stroke = Brushes.White,
                        StrokeThickness = 1
                    };

                    Canvas.SetLeft(ellipse, projectedPoints[i].X - VertexSize / 2);
                    Canvas.SetTop(ellipse, projectedPoints[i].Y - VertexSize / 2);

                    TetrahedronCanvas.Children.Add(ellipse);
                }
            }

            // Draw labels
            if (ShowLabels)
            {
                string[] labels = { "A", "B", "C", "D" };
                for (int i = 0; i < projectedPoints.Length; i++)
                {
                    var textBlock = new TextBlock
                    {
                        Text = labels[i],
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Foreground = Brushes.Blue
                    };

                    Canvas.SetLeft(textBlock, projectedPoints[i].X + 10);
                    Canvas.SetTop(textBlock, projectedPoints[i].Y - 10);

                    TetrahedronCanvas.Children.Add(textBlock);
                }
            }

            // Draw coordinate axes
            if (ShowAxis)
            {
                DrawAxis();
            }

            // Draw normals
            if (ShowNormals)
            {
                foreach (int faceIndex in faceIndices)
                {
                    if (!faceVisible[faceIndex]) continue;

                    var face = faces[faceIndex];
                    var v1 = vertices[face[0]];
                    var v2 = vertices[face[1]];
                    var v3 = vertices[face[2]];

                    // Calculate face center
                    Point3D center = new Point3D(
                        (v1.X + v2.X + v3.X) / 3,
                        (v1.Y + v2.Y + v3.Y) / 3,
                        (v1.Z + v2.Z + v3.Z) / 3
                    );

                    var normal = CalculateFaceNormal(v1, v2, v3);
                    double normalLength = 30;

                    Point3D normalEnd = new Point3D(
                        center.X + normal.X * normalLength,
                        center.Y + normal.Y * normalLength,
                        center.Z + normal.Z * normalLength
                    );

                    Point centerProj = Project3DTo2D(center);
                    Point normalEndProj = Project3DTo2D(normalEnd);

                    var normalLine = new Line
                    {
                        X1 = centerProj.X,
                        Y1 = centerProj.Y,
                        X2 = normalEndProj.X,
                        Y2 = normalEndProj.Y,
                        Stroke = Brushes.Green,
                        StrokeThickness = 2
                    };

                    TetrahedronCanvas.Children.Add(normalLine);
                }
            }
        }

        private Point3D[] GetTetrahedronVertices()
        {
            double a = EdgeLength;
            double k = a / Math.Sqrt(2);

            switch (currentType)
            {
                case "regular":
                    // Regular tetrahedron - vertices in cube
                    return new Point3D[]
                    {
                        new Point3D(k, k, k),      // A
                        new Point3D(-k, -k, k),    // B
                        new Point3D(-k, k, -k),    // C
                        new Point3D(k, -k, -k)     // D
                    };

                case "right":
                    // Right tetrahedron - right angles at origin
                    return new Point3D[]
                    {
                        new Point3D(0, 0, 0),      // A (origin)
                        new Point3D(a, 0, 0),      // B (along X)
                        new Point3D(0, a, 0),      // C (along Y)
                        new Point3D(0, 0, a)       // D (along Z)
                    };

                case "isosceles":
                    // Isosceles tetrahedron - base is equilateral triangle
                    double h = a * Math.Sqrt(6) / 3; // Height
                    double r = a / Math.Sqrt(3);     // Radius of base circle
                    return new Point3D[]
                    {
                        new Point3D(r, 0, -h/3),                    // A (base)
                        new Point3D(-r/2, r*Math.Sqrt(3)/2, -h/3),  // B (base)
                        new Point3D(-r/2, -r*Math.Sqrt(3)/2, -h/3), // C (base)
                        new Point3D(0, 0, 2*h/3)                     // D (apex)
                    };

                case "custom":
                default:
                    // Custom - slightly irregular
                    return new Point3D[]
                    {
                        new Point3D(a*0.6, a*0.5, a*0.4),
                        new Point3D(-a*0.5, -a*0.6, a*0.5),
                        new Point3D(-a*0.4, a*0.6, -a*0.5),
                        new Point3D(a*0.5, -a*0.5, -a*0.6)
                    };
            }
        }

        private Color GetFaceColor(int faceIndex)
        {
            if (ColorMode == "Solid")
            {
                return FaceColor1;
            }
            else if (ColorMode == "Multi")
            {
                return faceIndex switch
                {
                    0 => FaceColor1,
                    1 => FaceColor2,
                    2 => FaceColor3,
                    3 => FaceColor4,
                    _ => FaceColor1
                };
            }
            else // Gradient
            {
                // Simple gradient based on face index
                double factor = faceIndex / 3.0;
                byte r = (byte)(FaceColor1.R * (1 - factor) + FaceColor2.R * factor);
                byte g = (byte)(FaceColor1.G * (1 - factor) + FaceColor2.G * factor);
                byte b = (byte)(FaceColor1.B * (1 - factor) + FaceColor2.B * factor);
                return Color.FromRgb(r, g, b);
            }
        }

        private Point3D RotatePoint(Point3D p, double rx, double ry, double rz)
        {
            // Convert to radians
            double radX = rx * Math.PI / 180;
            double radY = ry * Math.PI / 180;
            double radZ = rz * Math.PI / 180;

            // Rotate around X axis
            double y1 = p.Y * Math.Cos(radX) - p.Z * Math.Sin(radX);
            double z1 = p.Y * Math.Sin(radX) + p.Z * Math.Cos(radX);

            // Rotate around Y axis
            double x2 = p.X * Math.Cos(radY) + z1 * Math.Sin(radY);
            double z2 = -p.X * Math.Sin(radY) + z1 * Math.Cos(radY);

            // Rotate around Z axis
            double x3 = x2 * Math.Cos(radZ) - y1 * Math.Sin(radZ);
            double y3 = x2 * Math.Sin(radZ) + y1 * Math.Cos(radZ);

            return new Point3D(x3, y3, z2);
        }

        private Point Project3DTo2D(Point3D p)
        {
            double distance = 600;
            double scale = distance / (distance + p.Z);

            double x2D = p.X * scale + TetrahedronCanvas.Width / 2;
            double y2D = -p.Y * scale + TetrahedronCanvas.Height / 2;

            return new Point(x2D, y2D);
        }

        private Point3D CalculateFaceNormal(Point3D v1, Point3D v2, Point3D v3)
        {
            // Calculate two edge vectors
            double ax = v2.X - v1.X, ay = v2.Y - v1.Y, az = v2.Z - v1.Z;
            double bx = v3.X - v1.X, by = v3.Y - v1.Y, bz = v3.Z - v1.Z;

            // Cross product
            double nx = ay * bz - az * by;
            double ny = az * bx - ax * bz;
            double nz = ax * by - ay * bx;

            // Normalize
            double length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (length > 0)
            {
                nx /= length;
                ny /= length;
                nz /= length;
            }

            return new Point3D(nx, ny, nz);
        }

        private void DrawAxis()
        {
            double axisLength = 100;

            // X axis (Red)
            Point3D xEnd = RotatePoint(new Point3D(axisLength, 0, 0), RotationX, RotationY, RotationZ);
            Point xEndProj = Project3DTo2D(xEnd);
            Point origin = Project3DTo2D(new Point3D(0, 0, 0));

            var xLine = new Line
            {
                X1 = origin.X, Y1 = origin.Y,
                X2 = xEndProj.X, Y2 = xEndProj.Y,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };
            TetrahedronCanvas.Children.Add(xLine);

            // Y axis (Green)
            Point3D yEnd = RotatePoint(new Point3D(0, axisLength, 0), RotationX, RotationY, RotationZ);
            Point yEndProj = Project3DTo2D(yEnd);

            var yLine = new Line
            {
                X1 = origin.X, Y1 = origin.Y,
                X2 = yEndProj.X, Y2 = yEndProj.Y,
                Stroke = Brushes.Green,
                StrokeThickness = 2
            };
            TetrahedronCanvas.Children.Add(yLine);

            // Z axis (Blue)
            Point3D zEnd = RotatePoint(new Point3D(0, 0, axisLength), RotationX, RotationY, RotationZ);
            Point zEndProj = Project3DTo2D(zEnd);

            var zLine = new Line
            {
                X1 = origin.X, Y1 = origin.Y,
                X2 = zEndProj.X, Y2 = zEndProj.Y,
                Stroke = Brushes.Blue,
                StrokeThickness = 2
            };
            TetrahedronCanvas.Children.Add(zLine);
        }

        private void UpdateInfoPanel()
        {
            double a = EdgeLength;

            // Calculate geometric properties based on type
            double height, outerRadius, innerRadius, faceArea, totalArea, volume;

            if (currentType == "regular")
            {
                // Regular tetrahedron formulas
                height = a * Math.Sqrt(2.0 / 3.0);
                outerRadius = a * Math.Sqrt(6) / 4;
                innerRadius = outerRadius / 3;
                faceArea = a * a * Math.Sqrt(3) / 4;
                totalArea = a * a * Math.Sqrt(3);
                volume = a * a * a / (6 * Math.Sqrt(2));
            }
            else if (currentType == "right")
            {
                // Right tetrahedron (right-angled at origin)
                height = a * Math.Sqrt(3) / Math.Sqrt(3);
                outerRadius = a * Math.Sqrt(3) / 2;
                innerRadius = a / 6;
                faceArea = a * a / 2; // Right triangle
                totalArea = a * a * (1 + Math.Sqrt(3)) / 2;
                volume = a * a * a / 6;
            }
            else if (currentType == "isosceles")
            {
                // Isosceles tetrahedron
                height = a * Math.Sqrt(6) / 3;
                outerRadius = a * Math.Sqrt(6) / 4;
                innerRadius = a * Math.Sqrt(6) / 12;
                faceArea = a * a * Math.Sqrt(3) / 4;
                totalArea = a * a * Math.Sqrt(3);
                volume = a * a * a * Math.Sqrt(2) / 12;
            }
            else // custom
            {
                // Approximate values for custom
                height = a * 0.8;
                outerRadius = a * 0.6;
                innerRadius = a * 0.2;
                faceArea = a * a * 0.43;
                totalArea = faceArea * 4;
                volume = a * a * a * 0.12;
            }

            txtHeight.Text = $"{height:F2} đơn vị";
            txtOuterRadius.Text = $"{outerRadius:F2} đơn vị";
            txtInnerRadius.Text = $"{innerRadius:F2} đơn vị";
            txtFaceArea.Text = $"{faceArea:F2} đơn vị²";
            txtTotalArea.Text = $"{totalArea:F2} đơn vị²";
            txtVolume.Text = $"{volume:F2} đơn vị³";
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            
            if (autoRotateTimer != null && autoRotateTimer.IsEnabled)
            {
                autoRotateTimer.Stop();
            }
            
            this.Close();
        }

        private void btnDisplayStats_Click(object sender, RoutedEventArgs e)
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
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Touch and Mouse Drag Rotation

        private void TetrahedronCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                _isDragging = true;
                _lastInteractionPoint = e.GetPosition(TetrahedronCanvas);
                TetrahedronCanvas.CaptureMouse();
                e.Handled = true;
            }
        }

        private void TetrahedronCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(TetrahedronCanvas);
                double deltaX = currentPoint.X - _lastInteractionPoint.X;
                double deltaY = currentPoint.Y - _lastInteractionPoint.Y;

                UpdateRotationFromDrag(deltaX, deltaY);

                _lastInteractionPoint = currentPoint;
                e.Handled = true;
            }
        }

        private void TetrahedronCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                TetrahedronCanvas.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void TetrahedronCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                TetrahedronCanvas.ReleaseMouseCapture();
            }
        }

        private void TetrahedronCanvas_TouchDown(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _activeTouchId = e.TouchDevice.Id;
                _lastInteractionPoint = e.GetTouchPoint(TetrahedronCanvas).Position;
                TetrahedronCanvas.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void TetrahedronCanvas_TouchMove(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (_isDragging && _activeTouchId == e.TouchDevice.Id)
            {
                Point currentPoint = e.GetTouchPoint(TetrahedronCanvas).Position;
                double deltaX = currentPoint.X - _lastInteractionPoint.X;
                double deltaY = currentPoint.Y - _lastInteractionPoint.Y;

                UpdateRotationFromDrag(deltaX, deltaY);

                _lastInteractionPoint = currentPoint;
                e.Handled = true;
            }
        }

        private void TetrahedronCanvas_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            if (_isDragging && _activeTouchId == e.TouchDevice.Id)
            {
                _isDragging = false;
                _activeTouchId = null;
                TetrahedronCanvas.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
            }
        }

        private void UpdateRotationFromDrag(double deltaX, double deltaY)
        {
            if (isAutoRotating)
            {
                chkAutoRotate.IsChecked = false;
            }

            double newY = RotYSlider.Value + deltaX * 0.6;
            while (newY > 180) newY -= 360;
            while (newY < -180) newY += 360;

            double newX = RotXSlider.Value - deltaY * 0.6;
            while (newX > 180) newX -= 360;
            while (newX < -180) newX += 360;

            RotYSlider.Value = Math.Round(newY);
            RotXSlider.Value = Math.Round(newX);
        }

        private void TetrahedronCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double step = (e.Delta > 0) ? 10 : -10;
            double newValue = EdgeLengthSlider.Value + step;
            if (newValue >= EdgeLengthSlider.Minimum && newValue <= EdgeLengthSlider.Maximum)
            {
                EdgeLengthSlider.Value = newValue;
            }
        }

        #endregion

        #region Preset Views and Actions

        private void BtnPresetPerspective_Click(object sender, RoutedEventArgs e)
        {
            RotXSlider.Value = -20;
            RotYSlider.Value = 30;
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

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            EdgeLengthSlider.Value = 150;
            ScaleSlider.Value = 1.0;
            RotXSlider.Value = -20;
            RotYSlider.Value = 30;
            RotZSlider.Value = 0;
            chkAutoRotate.IsChecked = false;
            cboDrawMode.SelectedIndex = 2; // Both
            cboColorMode.SelectedIndex = 1; // Multi
            OpacitySlider.Value = 90;
            chkEnableLighting.IsChecked = true;
            chkBackfaceCulling.IsChecked = true;
            chkShowEdges.IsChecked = true;
            chkShowVertices.IsChecked = false;
            chkShowLabels.IsChecked = false;
            chkShowAxis.IsChecked = false;
            chkShowFaceEdges.IsChecked = false;
            chkShowNormals.IsChecked = false;
            EdgeWidthSlider.Value = 2;
            VertexSizeSlider.Value = 6;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            if (autoRotateTimer != null && autoRotateTimer.IsEnabled)
            {
                autoRotateTimer.Stop();
            }
            this.Close();
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image (*.png)|*.png",
                    FileName = $"Tetrahedron3D_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)TetrahedronCanvas.ActualWidth,
                        (int)TetrahedronCanvas.ActualHeight,
                        96, 96, PixelFormats.Pbgra32);
                    renderBitmap.Render(TetrahedronCanvas);

                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderBitmap));

                    using var stream = new System.IO.FileStream(saveFileDialog.FileName, System.IO.FileMode.Create);
                    encoder.Save(stream);

                    MessageBox.Show("✅ Đã lưu hình tứ diện 3D thành công!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Lỗi lưu ảnh: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
