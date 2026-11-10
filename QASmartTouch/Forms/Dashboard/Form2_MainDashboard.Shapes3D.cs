using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Handlers;
using QASmartTouch.Managers;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using System.Windows.Input;
using System.Windows.Ink;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using QASmartTouch.Services;
using QASmartTouch.Helpers;
using QASmartTouch.Models;
using QASmartTouch.Controls;
using QASmartTouch.Controllers;
using QASmartTouch.Shared;
using QASmartTouch.Properties;
using System.Windows.Documents;
using QASmartTouch.Services.Tools;
using QASmartTouch.WhiteboardCore.History;
// ══════════════════════════════════════════════════════════════════
// Form2_MainDashboard.Shapes3D.cs — Partial Class
// Quản lý hộp thoại thiết lập và dựng khối Hình học Không gian 3D
// kèm bảng thông số công thức toán học (Diện tích / Thể tích).
// ══════════════════════════════════════════════════════════════════

namespace QASmartTouch.Forms
{
    public partial class Form2_MainDashboard
    {
        #region 3D Shape Editors & Canvas Builders

        private void Open3DCubeEditor()
        {
            var cubeEditor = new Form2_6_3DCubeEditor(_currentPenColor);
            
            if (WindowHelper.ShowChildDialog(cubeEditor, this) == true && cubeEditor.IsConfirmed)
            {
                // Check if "Display Stats" button was clicked
                if (cubeEditor.DisplayStatsMode)
                {
                    Apply3DCubeWithStatsToCanvas(cubeEditor);
                }
                else
                {
                    Apply3DCubeToCanvas(cubeEditor);
                }
            }
        }

        private void Open3DSphereEditor()
        {
            var sphereEditor = new Form2_6_3DSphereEditor(_currentPenColor);
            
            if (WindowHelper.ShowChildDialog(sphereEditor, this) == true && sphereEditor.IsConfirmed)
            {
                // Check if "Display Stats" button was clicked
                if (sphereEditor.DisplayStatsMode)
                {
                    Apply3DSphereWithStatsToCanvas(sphereEditor);
                }
                else
                {
                    Apply3DSphereToCanvas(sphereEditor);
                }
            }
        }

        private void Open3DCylinderEditor()
        {
            var cylinderEditor = new Form2_6_3DCylinderEditor(_currentPenColor);
            
            if (WindowHelper.ShowChildDialog(cylinderEditor, this) == true && cylinderEditor.IsConfirmed)
            {
                // Check if DisplayStats mode was activated
                if (cylinderEditor.DisplayStatsMode)
                {
                    Apply3DCylinderWithStatsToCanvas(cylinderEditor);
                }
                else
                {
                    // Regular confirm - just copy shape
                    Apply3DCylinderToCanvas(cylinderEditor);
                }
            }
        }

        private void Open3DConeEditor()
        {
            System.Diagnostics.Debug.WriteLine("🔍 DASHBOARD: Open3DConeEditor được gọi!");
            
            var coneEditor = new Form2_6_3DConeEditor(_currentPenColor);
            
            System.Diagnostics.Debug.WriteLine("🔍 DASHBOARD: Đang mở ConeEditor dialog...");
            var dialogResult = WindowHelper.ShowChildDialog(coneEditor, this);
            
            System.Diagnostics.Debug.WriteLine($"🔍 DASHBOARD: Dialog đã đóng. DialogResult = {dialogResult}");
            System.Diagnostics.Debug.WriteLine($"   IsConfirmed = {coneEditor.IsConfirmed}");
            System.Diagnostics.Debug.WriteLine($"   DisplayStatsMode = {coneEditor.DisplayStatsMode}");
            
            if (dialogResult == true && coneEditor.IsConfirmed)
            {
                // Check if "Display Stats" button was clicked
                if (coneEditor.DisplayStatsMode)
                {
                    System.Diagnostics.Debug.WriteLine("🔍 DASHBOARD: Gọi Apply3DConeWithStatsToCanvas...");
                    Apply3DConeWithStatsToCanvas(coneEditor);
                    System.Diagnostics.Debug.WriteLine("✅ DASHBOARD: Apply3DConeWithStatsToCanvas hoàn thành!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("🔍 DASHBOARD: Gọi Apply3DConeToCanvas (regular mode)...");
                    Apply3DConeToCanvas(coneEditor);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ DASHBOARD: Dialog bị hủy hoặc không confirm!");
            }
        }

        private void Open3DPyramidEditor()
        {
            var pyramidEditor = new Form2_6_3DPyramidEditor(_currentPenColor);
            
            if (WindowHelper.ShowChildDialog(pyramidEditor, this) == true && pyramidEditor.IsConfirmed)
            {
                // Check if "Display Stats" button was clicked
                if (pyramidEditor.DisplayStatsMode)
                {
                    Apply3DPyramidWithStatsToCanvas(pyramidEditor);
                }
                else
                {
                    Apply3DPyramidToCanvas(pyramidEditor);
                }
            }
        }

        private void Open3DPrismEditor()
        {
            var prismEditor = new Form2_6_3DPrismEditor(_currentPenColor);
            
            if (WindowHelper.ShowChildDialog(prismEditor, this) == true && prismEditor.IsConfirmed)
            {
                // User confirmed - apply the prism to canvas
                Apply3DPrismToCanvas(prismEditor);
            }
        }

        /// <summary>
        /// Open Prism Editor and handle "Display Stats" button - copy shape + educational stats to canvas
        /// </summary>
        public void Open3DPrismEditorWithStats()
        {
            var prismEditor = new Form2_6_3DPrismEditor(_currentPenColor);
            
            if (WindowHelper.ShowChildDialog(prismEditor, this) == true && prismEditor.IsConfirmed)
            {
                // Check if DisplayStats mode was activated
                if (prismEditor.DisplayStatsMode)
                {
                    Apply3DPrismWithStatsToCanvas(prismEditor);
                }
                else
                {
                    // Regular confirm - just copy shape
                    Apply3DPrismToCanvas(prismEditor);
                }
            }
        }

        /// <summary>
        /// Open Line Chart Editor and handle both regular and DisplayStats mode
        /// </summary>
        private void OpenLineChartEditor()
        {
            var lineChartEditor = new Form2_9_LineChartEditor();
            
            if (WindowHelper.ShowChildDialog(lineChartEditor, this) == true && lineChartEditor.IsConfirmed)
            {
                // Check if DisplayStats mode was activated
                if (lineChartEditor.DisplayStatsMode)
                {
                    ApplyLineChartWithStatsToCanvas(lineChartEditor);
                }
                else
                {
                    // Regular confirm - just copy chart
                    ApplyLineChartToCanvas(lineChartEditor);
                }
            }
        }

        private void Apply3DCubeToCanvas(Form2_6_3DCubeEditor editor)
        {
            // Create a canvas to hold the 3D cube
            Canvas cubeContainer = new Canvas
            {
                Width = 400,
                Height = 400,
                Background = Brushes.Transparent
            };

            // Get cube vertices with applied rotations
            double size = editor.CubeSize;
            double[,] vertices3D = GetCubeVertices3D(size);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            for (int i = 0; i < 8; i++)
            {
                double[] vertex = new double[] { vertices3D[i, 0], vertices3D[i, 1], vertices3D[i, 2] };
                vertex = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(vertex);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, 200, 200));
            }

            // Define faces
            int[][] faces = new int[][]
            {
                new int[] { 0, 3, 2, 1 }, // Front (normal toward -z, camera)
                new int[] { 4, 5, 6, 7 }, // Back  (normal toward +z, away)
                new int[] { 0, 1, 5, 4 }, // Bottom (normal toward -y)
                new int[] { 3, 7, 6, 2 }, // Top    (normal toward +y)
                new int[] { 0, 4, 7, 3 }, // Left   (normal toward -x)
                new int[] { 1, 2, 6, 5 }  // Right  (normal toward +x)
            };

            // Calculate visibility and depth
            List<(int faceIndex, double avgZ)> facesInfo = new List<(int, double)>();
            for (int i = 0; i < faces.Length; i++)
            {
                int[] face = faces[i];
                double avgZ = (rotatedVertices[face[0]][2] + rotatedVertices[face[1]][2] + 
                              rotatedVertices[face[2]][2] + rotatedVertices[face[3]][2]) / 4.0;
                facesInfo.Add((i, avgZ));
            }

            facesInfo.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (Solid mode)
            if (editor.DrawMode == "Solid" || editor.DrawMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesInfo)
                {
                    int[] face = faces[faceIndex];
                    Polygon polygon = new Polygon
                    {
                        Fill = new SolidColorBrush(editor.FaceColor),
                        Stroke = Brushes.Transparent,
                        StrokeThickness = 0
                    };

                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    cubeContainer.Children.Add(polygon);
                }
            }

            // Draw edges - Phân biệt nét đứt / nét liền theo tùy chọn ShowHiddenEdges
            bool[] isFaceBack = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                int[] face = faces[i];
                double[] normal = CalculateFaceNormal(rotatedVertices[face[0]], rotatedVertices[face[1]], rotatedVertices[face[2]]);
                isFaceBack[i] = normal[2] >= 0;
            }

            int[][] edges = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 2, 3 }, new int[] { 3, 0 },
                new int[] { 4, 5 }, new int[] { 5, 6 }, new int[] { 6, 7 }, new int[] { 7, 4 },
                new int[] { 0, 4 }, new int[] { 1, 5 }, new int[] { 2, 6 }, new int[] { 3, 7 }
            };
            
            foreach (var edge in edges)
            {
                List<int> sharingFaces = new List<int>();
                for (int i = 0; i < 6; i++)
                {
                    int[] f = faces[i];
                    bool hasU = f[0] == edge[0] || f[1] == edge[0] || f[2] == edge[0] || f[3] == edge[0];
                    bool hasV = f[0] == edge[1] || f[1] == edge[1] || f[2] == edge[1] || f[3] == edge[1];
                    if (hasU && hasV) sharingFaces.Add(i);
                }

                bool isHidden = sharingFaces.Count == 2 && isFaceBack[sharingFaces[0]] && isFaceBack[sharingFaces[1]];

                Line line = new Line
                {
                    X1 = projected2D[edge[0]].X,
                    Y1 = projected2D[edge[0]].Y,
                    X2 = projected2D[edge[1]].X,
                    Y2 = projected2D[edge[1]].Y,
                    Stroke = new SolidColorBrush(editor.EdgeColor),
                    StrokeThickness = editor.EdgeThickness
                };

                if (isHidden && editor.ShowHiddenEdges)
                {
                    line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                }

                cubeContainer.Children.Add(line);
            }

            // Draw vertices and labels - Chỉ vẽ khi người dùng bật ShowVertexLabels
            if (editor.ShowVertexLabels)
            {
                if (editor.DrawMode == "Wireframe" || editor.DrawMode == "Both")
                {
                    foreach (var point in projected2D)
                    {
                        Ellipse dot = new Ellipse
                        {
                            Width = 6,
                            Height = 6,
                            Fill = new SolidColorBrush(editor.EdgeColor)
                        };
                        Canvas.SetLeft(dot, point.X - 3);
                        Canvas.SetTop(dot, point.Y - 3);
                        cubeContainer.Children.Add(dot);
                    }
                }

                // Vẽ nhãn đỉnh (A, B, C, D, A', B', C', D')
                string[] vertexNames = { "A", "B", "C", "D", "A'", "B'", "C'", "D'" };
                for (int i = 0; i < 8; i++)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = vertexNames[i],
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, projected2D[i].X + 8);
                    Canvas.SetTop(label, projected2D[i].Y - 16);
                    cubeContainer.Children.Add(label);
                }
            }

            // Trim canvas to square based on cube diagonal (side = space diagonal / √2)
            TrimCanvasToCubeDiagonal(cubeContainer, editor.CubeSize, 10);

            // Position cube container in center of canvas
            Canvas.SetLeft(cubeContainer, (MainInteractiveBoard.ActualWidth - cubeContainer.Width) / 2);
            Canvas.SetTop(cubeContainer, (MainInteractiveBoard.ActualHeight - cubeContainer.Height) / 2);

            // Enable drag & drop
            Enable3DShapeDragging(cubeContainer);

            // Add to main canvas
            MainInteractiveBoard.Children.Add(cubeContainer);
            
            // Add to undo stack
            RecordAddAction(cubeContainer, "Draw");
        }

        /// <summary>
        /// Apply 3D Cube to canvas WITH educational stats panel (for "Display Stats" button)
        /// Creates TWO INDEPENDENT objects: 3D Shape + Stats Panel (can be moved and deleted separately)
        /// </summary>
        private void Apply3DCubeWithStatsToCanvas(Form2_6_3DCubeEditor editor)
        {
            // ========== 1. CREATE 3D CUBE SHAPE (INDEPENDENT OBJECT) ==========
            Canvas cubeContainer = new Canvas
            {
                Width = 400,
                Height = 400,
                Background = Brushes.Transparent
            };

            // Get cube vertices with applied rotations
            double size = editor.CubeSize;
            double[,] vertices3D = GetCubeVertices3D(size);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            for (int i = 0; i < 8; i++)
            {
                double[] vertex = new double[] { vertices3D[i, 0], vertices3D[i, 1], vertices3D[i, 2] };
                vertex = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(vertex);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, 200, 200));
            }

            // Define faces
            int[][] faces = new int[][]
            {
                new int[] { 0, 3, 2, 1 }, // Front (normal toward -z, camera)
                new int[] { 4, 5, 6, 7 }, // Back  (normal toward +z, away)
                new int[] { 0, 1, 5, 4 }, // Bottom (normal toward -y)
                new int[] { 3, 7, 6, 2 }, // Top    (normal toward +y)
                new int[] { 0, 4, 7, 3 }, // Left   (normal toward -x)
                new int[] { 1, 2, 6, 5 }  // Right  (normal toward +x)
            };

            // Calculate visibility and depth
            List<(int faceIndex, double avgZ)> facesInfo = new List<(int, double)>();
            for (int i = 0; i < faces.Length; i++)
            {
                int[] face = faces[i];
                double avgZ = (rotatedVertices[face[0]][2] + rotatedVertices[face[1]][2] + 
                              rotatedVertices[face[2]][2] + rotatedVertices[face[3]][2]) / 4.0;
                facesInfo.Add((i, avgZ));
            }

            facesInfo.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (Solid mode)
            if (editor.DrawMode == "Solid" || editor.DrawMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesInfo)
                {
                    int[] face = faces[faceIndex];
                    Polygon polygon = new Polygon
                    {
                        Fill = new SolidColorBrush(editor.FaceColor),
                        Stroke = Brushes.Transparent,
                        StrokeThickness = 0
                    };

                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    cubeContainer.Children.Add(polygon);
                }
            }

            // Draw edges - Phân biệt nét đứt / nét liền theo tùy chọn ShowHiddenEdges
            bool[] isFaceBack = new bool[6];
            for (int i = 0; i < 6; i++)
            {
                int[] face = faces[i];
                double[] normal = CalculateFaceNormal(rotatedVertices[face[0]], rotatedVertices[face[1]], rotatedVertices[face[2]]);
                isFaceBack[i] = normal[2] >= 0;
            }

            int[][] edges = new int[][]
            {
                new int[] { 0, 1 }, new int[] { 1, 2 }, new int[] { 2, 3 }, new int[] { 3, 0 },
                new int[] { 4, 5 }, new int[] { 5, 6 }, new int[] { 6, 7 }, new int[] { 7, 4 },
                new int[] { 0, 4 }, new int[] { 1, 5 }, new int[] { 2, 6 }, new int[] { 3, 7 }
            };
            
            foreach (var edge in edges)
            {
                List<int> sharingFaces = new List<int>();
                for (int i = 0; i < 6; i++)
                {
                    int[] f = faces[i];
                    bool hasU = f[0] == edge[0] || f[1] == edge[0] || f[2] == edge[0] || f[3] == edge[0];
                    bool hasV = f[0] == edge[1] || f[1] == edge[1] || f[2] == edge[1] || f[3] == edge[1];
                    if (hasU && hasV) sharingFaces.Add(i);
                }

                bool isHidden = sharingFaces.Count == 2 && isFaceBack[sharingFaces[0]] && isFaceBack[sharingFaces[1]];

                Line line = new Line
                {
                    X1 = projected2D[edge[0]].X,
                    Y1 = projected2D[edge[0]].Y,
                    X2 = projected2D[edge[1]].X,
                    Y2 = projected2D[edge[1]].Y,
                    Stroke = new SolidColorBrush(editor.EdgeColor),
                    StrokeThickness = editor.EdgeThickness
                };

                if (isHidden && editor.ShowHiddenEdges)
                {
                    line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                }

                cubeContainer.Children.Add(line);
            }

            // Draw vertices and labels - Chỉ vẽ khi người dùng bật ShowVertexLabels
            if (editor.ShowVertexLabels)
            {
                if (editor.DrawMode == "Wireframe" || editor.DrawMode == "Both")
                {
                    foreach (var point in projected2D)
                    {
                        Ellipse dot = new Ellipse
                        {
                            Width = 6,
                            Height = 6,
                            Fill = new SolidColorBrush(editor.EdgeColor)
                        };
                        Canvas.SetLeft(dot, point.X - 3);
                        Canvas.SetTop(dot, point.Y - 3);
                        cubeContainer.Children.Add(dot);
                    }
                }

                // Vẽ nhãn đỉnh (A, B, C, D, A', B', C', D')
                string[] vertexNames = { "A", "B", "C", "D", "A'", "B'", "C'", "D'" };
                for (int i = 0; i < 8; i++)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = vertexNames[i],
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, projected2D[i].X + 8);
                    Canvas.SetTop(label, projected2D[i].Y - 16);
                    cubeContainer.Children.Add(label);
                }
            }

            TrimCanvasToCubeDiagonal(cubeContainer, editor.CubeSize, 10);

            // Position cube shape in center-left area of canvas
            Canvas.SetLeft(cubeContainer, (MainInteractiveBoard.ActualWidth / 2) - cubeContainer.Width - 20);
            Canvas.SetTop(cubeContainer, (MainInteractiveBoard.ActualHeight - cubeContainer.Height) / 2);

            // Enable drag & drop for cube shape (independent)
            Enable3DShapeDragging(cubeContainer);

            // Add cube to main canvas
            MainInteractiveBoard.Children.Add(cubeContainer);
            
            // Add to undo stack
            RecordAddAction(cubeContainer, "Create 3D object");

            // ========== 2. CREATE EDUCATIONAL STATS PANEL (INDEPENDENT OBJECT) ==========
            // Calculate ACTUAL dimensions on canvas (after 3D projection)
            const double DPI = 96.0;
            const double MM_PER_INCH = 25.4;
            double pixelsToMm = MM_PER_INCH / DPI;

            // Calculate edge length (distance between two adjacent vertices)
            double actualEdgeLengthPx = 0;
            if (projected2D.Count >= 2)
            {
                Point vertex0 = projected2D[0];
                Point vertex1 = projected2D[1];
                actualEdgeLengthPx = Math.Sqrt(Math.Pow(vertex1.X - vertex0.X, 2) + Math.Pow(vertex1.Y - vertex0.Y, 2));
            }
            double actualEdgeLengthMm = actualEdgeLengthPx * pixelsToMm;

            // Calculate face diagonal
            double actualFaceDiagonalMm = actualEdgeLengthMm * Math.Sqrt(2);
            
            // Calculate space diagonal
            double actualSpaceDiagonalMm = actualEdgeLengthMm * Math.Sqrt(3);

            // Calculate surface area and volume based on ACTUAL mm dimensions
            double actualSurfaceAreaMm2 = 6 * actualEdgeLengthMm * actualEdgeLengthMm;
            double actualVolumeMm3 = Math.Pow(actualEdgeLengthMm, 3);

            var statsPanel = new Border
            {
                Width = 380,
                Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15)
            };

            var stackPanel = new StackPanel();

            // Title
            AddStatsTextBlock(stackPanel, "📐 HÌNH LẬP PHƯƠNG (CUBE)", 16, true, "#667EEA");
            AddStatsSeparator(stackPanel);

            // Dimensions - Show ACTUAL measured size in mm
            AddStatsTextBlock(stackPanel, "📏 KÍCH THƯỚC THỰC TẾ (trên màn hình):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Cạnh (a): {actualEdgeLengthMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Đường chéo mặt: {actualFaceDiagonalMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Đường chéo khối: {actualSpaceDiagonalMm:N1} mm", 11, false, "#555");
            AddStatsSeparator(stackPanel);

            // Formulas - Based on ACTUAL mm dimensions
            AddStatsTextBlock(stackPanel, "📐 CÔNG THỨC TÍNH (theo kích thước thực):", 13, true, "#2C3E50");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích toàn phần (S):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S = 6 × a²", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualSurfaceAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Thể tích (V):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     V = a³", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualVolumeMm3:N2} mm³", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Đường chéo mặt (d_mặt):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     d = a × √2", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualFaceDiagonalMm:N2} mm", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Đường chéo khối (d_khối):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     d = a × √3", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualSpaceDiagonalMm:N2} mm", 11, false, "#E91E63");
            AddStatsSeparator(stackPanel);

            // Topology
            AddStatsTextBlock(stackPanel, "🔢 ĐẶC ĐIỂM HÌNH HỌC:", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Khối đa diện đều (Platonic solid)", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Số đỉnh (V): 8", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Số cạnh (E): 12", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Số mặt (F): 6 (hình vuông)", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Công thức Euler: V - E + F = 2", 11, false, "#9C27B0");
            AddStatsTextBlock(stackPanel, $"     8 - 12 + 6 = 2 âœ“", 11, false, "#9C27B0");

            statsPanel.Child = stackPanel;

            // Wrap stats panel in a Canvas container for drag & drop
            Canvas statsPanelContainer = new Canvas
            {
                Width = statsPanel.Width + 10,
                Height = stackPanel.ActualHeight > 0 ? stackPanel.ActualHeight + 30 : 600,
                Background = Brushes.Transparent
            };
            
            Canvas.SetLeft(statsPanel, 0);
            Canvas.SetTop(statsPanel, 0);
            statsPanelContainer.Children.Add(statsPanel);

            // Position stats panel to the right of cube shape
            Canvas.SetLeft(statsPanelContainer, (MainInteractiveBoard.ActualWidth - 850) / 2 + 430);
            Canvas.SetTop(statsPanelContainer, (MainInteractiveBoard.ActualHeight - 400) / 2 + 10);

            // Enable drag & drop for stats panel (independent)
            Enable3DShapeDragging(statsPanelContainer);

            // Add stats panel to main canvas
            MainInteractiveBoard.Children.Add(statsPanelContainer);
            
            // Add to undo stack
            RecordAddAction(statsPanelContainer, "Draw");
        }

        private void Apply3DSphereToCanvas(Form2_6_3DSphereEditor editor)
        {
            // Create a canvas to hold the 3D sphere
            Canvas sphereContainer = new Canvas
            {
                Width = 500,
                Height = 500,
                Background = Brushes.Transparent
            };

            double centerX = 250;
            double centerY = 250;

            // Generate sphere vertices
            List<double[]> vertices = GenerateSphereVertices(
                editor.SphereRadius,
                editor.LatitudeSegments,
                editor.LongitudeSegments);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GenerateSphereFaces(editor.LatitudeSegments, editor.LongitudeSegments);

            // Calculate face visibility and depth
            List<(int faceIndex, double avgZ, bool isVisible)> facesInfo = new List<(int, double, bool)>();

            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;

                double[] normal = CalculateFaceNormal(
                    rotatedVertices[face[0]],
                    rotatedVertices[face[1]],
                    rotatedVertices[face[2]]);

                double[] viewVector = new double[] { 0, 0, 1 };
                double dotProduct = normal[0] * viewVector[0] +
                                   normal[1] * viewVector[1] +
                                   normal[2] * viewVector[2];

                bool isVisible = dotProduct < 0;
                facesInfo.Add((i, avgZ, isVisible));
            }

            facesInfo.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawMode == "Solid" || editor.DrawMode == "Both")
            {
                foreach (var (faceIndex, avgZ, isVisible) in facesInfo)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon
                    {
                        Fill = new SolidColorBrush(editor.FaceColor),
                        Stroke = Brushes.Transparent,
                        StrokeThickness = 0
                    };

                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    sphereContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawMode == "Wireframe" || editor.DrawMode == "Both")
            {
                // Draw latitude lines
                for (int lat = 0; lat < editor.LatitudeSegments; lat++)
                {
                    for (int lon = 0; lon < editor.LongitudeSegments; lon++)
                    {
                        int current = lat * (editor.LongitudeSegments + 1) + lon;
                        int next = lat * (editor.LongitudeSegments + 1) + ((lon + 1) % (editor.LongitudeSegments + 1));

                        // Độ sâu trung bình của cạnh để xác định ẩn/hiện
                        double avgZ = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2;
                        bool isHidden = avgZ >= 0;

                        Line line = new Line
                        {
                            X1 = projected2D[current].X,
                            Y1 = projected2D[current].Y,
                            X2 = projected2D[next].X,
                            Y2 = projected2D[next].Y,
                            Stroke = new SolidColorBrush(editor.EdgeColor),
                            StrokeThickness = editor.EdgeThickness
                        };

                        if (isHidden && editor.ShowHiddenEdges)
                        {
                            line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                        }

                        sphereContainer.Children.Add(line);
                    }
                }

                // Draw longitude lines
                for (int lon = 0; lon <= editor.LongitudeSegments; lon++)
                {
                    for (int lat = 0; lat < editor.LatitudeSegments; lat++)
                    {
                        int current = lat * (editor.LongitudeSegments + 1) + lon;
                        int next = (lat + 1) * (editor.LongitudeSegments + 1) + lon;

                        // Độ sâu trung bình của cạnh để xác định ẩn/hiện
                        double avgZ = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2;
                        bool isHidden = avgZ >= 0;

                        Line line = new Line
                        {
                            X1 = projected2D[current].X,
                            Y1 = projected2D[current].Y,
                            X2 = projected2D[next].X,
                            Y2 = projected2D[next].Y,
                            Stroke = new SolidColorBrush(editor.EdgeColor),
                            StrokeThickness = editor.EdgeThickness
                        };

                        if (isHidden && editor.ShowHiddenEdges)
                        {
                            line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                        }

                        sphereContainer.Children.Add(line);
                    }
                }
            }

            // Trim canvas to perfect square (sphere-specific algorithm)
            TrimCanvasToSquare(sphereContainer, 10);

            // Position sphere in center of canvas
            var mainCanvas = this.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas == null) return;

            Canvas.SetLeft(sphereContainer, (mainCanvas.ActualWidth - sphereContainer.Width) / 2);
            Canvas.SetTop(sphereContainer, (mainCanvas.ActualHeight - sphereContainer.Height) / 2);

            // Enable drag & drop
            Enable3DShapeDragging(sphereContainer);

            // Add to main canvas
            mainCanvas.Children.Add(sphereContainer);
            
            // Add to undo stack
            RecordAddAction(sphereContainer, "Draw");
        }

        // Helper method for sphere vertices generation
        private List<double[]> GenerateSphereVertices(double radius, int latSegments, int longSegments)
        {
            List<double[]> vertices = new List<double[]>();

            for (int lat = 0; lat <= latSegments; lat++)
            {
                double theta = lat * Math.PI / latSegments;
                double sinTheta = Math.Sin(theta);
                double cosTheta = Math.Cos(theta);

                for (int lon = 0; lon <= longSegments; lon++)
                {
                    double phi = lon * 2 * Math.PI / longSegments;
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

        private List<int[]> GenerateSphereFaces(int latSegments, int longSegments)
        {
            List<int[]> faces = new List<int[]>();

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < longSegments; lon++)
                {
                    int first = lat * (longSegments + 1) + lon;
                    int second = first + longSegments + 1;

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
        /// Apply 3D Sphere to canvas WITH educational stats panel (for "Display Stats" button)
        /// Creates TWO INDEPENDENT objects: 3D Shape + Stats Panel (can be moved and deleted separately)
        /// </summary>
        private void Apply3DSphereWithStatsToCanvas(Form2_6_3DSphereEditor editor)
        {
            // ========== 1. CREATE 3D SPHERE SHAPE (INDEPENDENT OBJECT) ==========
            Canvas sphereContainer = new Canvas
            {
                Width = 500,
                Height = 500,
                Background = Brushes.Transparent
            };

            double centerX = 250;
            double centerY = 250;

            // Generate sphere vertices
            List<double[]> vertices = GenerateSphereVertices(
                editor.SphereRadius,
                editor.LatitudeSegments,
                editor.LongitudeSegments);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GenerateSphereFaces(editor.LatitudeSegments, editor.LongitudeSegments);

            // Calculate face visibility and depth
            List<(int faceIndex, double avgZ, bool isVisible)> facesInfo = new List<(int, double, bool)>();

            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;

                double[] normal = CalculateFaceNormal(
                    rotatedVertices[face[0]],
                    rotatedVertices[face[1]],
                    rotatedVertices[face[2]]);

                double[] viewVector = new double[] { 0, 0, 1 };
                double dotProduct = normal[0] * viewVector[0] +
                                   normal[1] * viewVector[1] +
                                   normal[2] * viewVector[2];

                bool isVisible = dotProduct < 0;
                facesInfo.Add((i, avgZ, isVisible));
            }

            facesInfo.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawMode == "Solid" || editor.DrawMode == "Both")
            {
                foreach (var (faceIndex, avgZ, isVisible) in facesInfo)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon
                    {
                        Fill = new SolidColorBrush(editor.FaceColor),
                        Stroke = Brushes.Transparent,
                        StrokeThickness = 0
                    };

                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    sphereContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawMode == "Wireframe" || editor.DrawMode == "Both")
            {
                // Draw latitude lines
                for (int lat = 0; lat < editor.LatitudeSegments; lat++)
                {
                    for (int lon = 0; lon < editor.LongitudeSegments; lon++)
                    {
                        int current = lat * (editor.LongitudeSegments + 1) + lon;
                        int next = lat * (editor.LongitudeSegments + 1) + ((lon + 1) % (editor.LongitudeSegments + 1));

                        // Độ sâu trung bình của cạnh để xác định ẩn/hiện
                        double avgZ = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2;
                        bool isHidden = avgZ >= 0;

                        Line line = new Line
                        {
                            X1 = projected2D[current].X,
                            Y1 = projected2D[current].Y,
                            X2 = projected2D[next].X,
                            Y2 = projected2D[next].Y,
                            Stroke = new SolidColorBrush(editor.EdgeColor),
                            StrokeThickness = editor.EdgeThickness
                        };

                        if (isHidden && editor.ShowHiddenEdges)
                        {
                            line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                        }

                        sphereContainer.Children.Add(line);
                    }
                }

                // Draw longitude lines
                for (int lon = 0; lon <= editor.LongitudeSegments; lon++)
                {
                    for (int lat = 0; lat < editor.LatitudeSegments; lat++)
                    {
                        int current = lat * (editor.LongitudeSegments + 1) + lon;
                        int next = (lat + 1) * (editor.LongitudeSegments + 1) + lon;

                        // Độ sâu trung bình của cạnh để xác định ẩn/hiện
                        double avgZ = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2;
                        bool isHidden = avgZ >= 0;

                        Line line = new Line
                        {
                            X1 = projected2D[current].X,
                            Y1 = projected2D[current].Y,
                            X2 = projected2D[next].X,
                            Y2 = projected2D[next].Y,
                            Stroke = new SolidColorBrush(editor.EdgeColor),
                            StrokeThickness = editor.EdgeThickness
                        };

                        if (isHidden && editor.ShowHiddenEdges)
                        {
                            line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                        }

                        sphereContainer.Children.Add(line);
                    }
                }
            }

            // Trim canvas to perfect square (sphere-specific algorithm)
            TrimCanvasToSquare(sphereContainer, 10);

            // Position sphere shape in center-left area of canvas
            Canvas.SetLeft(sphereContainer, (MainInteractiveBoard.ActualWidth / 2) - sphereContainer.Width - 20);
            Canvas.SetTop(sphereContainer, (MainInteractiveBoard.ActualHeight - sphereContainer.Height) / 2);

            // Enable drag & drop for sphere shape (independent)
            Enable3DShapeDragging(sphereContainer);

            // Add sphere to main canvas
            MainInteractiveBoard.Children.Add(sphereContainer);
            
            // Add to undo stack
            RecordAddAction(sphereContainer, "Create 3D object");

            // ========== 2. CREATE EDUCATIONAL STATS PANEL (INDEPENDENT OBJECT) ==========
            // Calculate ACTUAL dimensions on canvas (after 3D projection)
            const double DPI = 96.0;
            const double MM_PER_INCH = 25.4;
            double pixelsToMm = MM_PER_INCH / DPI;

            // Calculate radius (distance from center to any edge point - use equator point)
            double actualRadiusPx = 0;
            if (projected2D.Count > 0)
            {
                // Use middle latitude (equator) point for most accurate measurement
                int equatorIndex = (editor.LatitudeSegments / 2) * (editor.LongitudeSegments + 1);
                Point centerPoint = new Point(centerX, centerY);
                Point edgePoint = projected2D[equatorIndex];
                actualRadiusPx = Math.Sqrt(Math.Pow(edgePoint.X - centerPoint.X, 2) + Math.Pow(edgePoint.Y - centerPoint.Y, 2));
            }
            double actualRadiusMm = actualRadiusPx * pixelsToMm;

            // Calculate surface area and volume based on ACTUAL mm dimensions
            double actualSurfaceAreaMm2 = 4 * Math.PI * actualRadiusMm * actualRadiusMm;
            double actualVolumeMm3 = (4.0 / 3.0) * Math.PI * Math.Pow(actualRadiusMm, 3);

            var statsPanel = new Border
            {
                Width = 380,
                Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15)
            };

            var stackPanel = new StackPanel();

            // Title
            AddStatsTextBlock(stackPanel, "📐 HÌNH CẦU (SPHERE)", 16, true, "#667EEA");
            AddStatsSeparator(stackPanel);

            // Dimensions - Show ACTUAL measured size in mm
            AddStatsTextBlock(stackPanel, "📏 KÍCH THƯỚC THỰC TẾ (trên màn hình):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Bán kính (r): {actualRadiusMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Đường kính (d): {actualRadiusMm * 2:N1} mm", 11, false, "#555");
            AddStatsSeparator(stackPanel);

            // Formulas - Based on ACTUAL mm dimensions
            AddStatsTextBlock(stackPanel, "📐 CÔNG THỨC TÍNH (theo kích thước thực):", 13, true, "#2C3E50");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích bề mặt (S):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S = 4 × π × r²", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualSurfaceAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Thể tích (V):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     V = (4/3) × π × r³", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualVolumeMm3:N2} mm³", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Chu vi đường tròn lớn (C):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     C = 2 × π × r", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {2 * Math.PI * actualRadiusMm:N2} mm", 11, false, "#E91E63");
            AddStatsSeparator(stackPanel);

            // Topology
            AddStatsTextBlock(stackPanel, "🔢 ĐẶC ĐIỂM HÌNH HỌC:", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Hình cầu hoàn hảo", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Mọi điểm trên bề mặt cách tâm = r", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Không có đỉnh, cạnh hay mặt phẳng", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Tỷ lệ thể tích/diện tích tối đa", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Cấu trúc lưới: {editor.LatitudeSegments} vĩ độ × {editor.LongitudeSegments} kinh độ", 11, false, "#9C27B0");

            statsPanel.Child = stackPanel;

            // Wrap stats panel in a Canvas container for drag & drop
            Canvas statsPanelContainer = new Canvas
            {
                Width = statsPanel.Width + 10,
                Height = stackPanel.ActualHeight > 0 ? stackPanel.ActualHeight + 30 : 550,
                Background = Brushes.Transparent
            };
            
            Canvas.SetLeft(statsPanel, 0);
            Canvas.SetTop(statsPanel, 0);
            statsPanelContainer.Children.Add(statsPanel);

            // Position stats panel to the right of sphere shape
            Canvas.SetLeft(statsPanelContainer, (MainInteractiveBoard.ActualWidth - 950) / 2 + 530);
            Canvas.SetTop(statsPanelContainer, (MainInteractiveBoard.ActualHeight - 500) / 2 + 10);

            // Enable drag & drop for stats panel (independent)
            Enable3DShapeDragging(statsPanelContainer);

            // Add stats panel to main canvas
            MainInteractiveBoard.Children.Add(statsPanelContainer);
            
            // Add to undo stack
            RecordAddAction(statsPanelContainer, "Draw");
        }

        private void Apply3DCylinderToCanvas(Form2_6_3DCylinderEditor editor)
        {
            // Create a canvas to hold the 3D cylinder
            Canvas cylinderContainer = new Canvas
            {
                Width = 600,
                Height = 600,
                Background = Brushes.Transparent
            };

            double centerX = 300;
            double centerY = 300;

            // Generate cylinder vertices
            List<double[]> vertices = GenerateCylinderVertices(
                editor.Radius,
                editor.Height,
                editor.Segments);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GenerateCylinderFaces(editor.Segments);

            // Sort faces by depth for proper rendering
            List<(int faceIndex, double avgZ)> facesWithDepth = new List<(int, double)>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;
                facesWithDepth.Add((i, avgZ));
            }
            facesWithDepth.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawingMode == "Solid" || editor.DrawingMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesWithDepth)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon();
                    
                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    // Apply face color with opacity
                    if (editor.FaceColor != Brushes.Transparent)
                    {
                        var color = ((SolidColorBrush)editor.FaceColor).Color;
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(editor.OpacityValue * 255),
                            color.R, color.G, color.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    cylinderContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawingMode == "Wireframe" || editor.DrawingMode == "Both")
            {
                int segments = editor.Segments;

                // Xác định độ sâu trung bình để quyết định cạnh nào bị khuất
                double avgTopZ = 0, avgBottomZ = 0;
                for (int i = 0; i < segments; i++)
                {
                    avgTopZ += rotatedVertices[i][2];
                    avgBottomZ += rotatedVertices[segments + i][2];
                }
                avgTopZ /= segments;
                avgBottomZ /= segments;

                // Draw top circle
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    bool isHidden = (rotatedVertices[i][2] + rotatedVertices[next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[i].X,
                        Y1 = projected2D[i].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    cylinderContainer.Children.Add(line);
                }

                // Draw bottom circle
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    bool isHidden = (rotatedVertices[segments + i][2] + rotatedVertices[segments + next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[segments + i].X,
                        Y1 = projected2D[segments + i].Y,
                        X2 = projected2D[segments + next].X,
                        Y2 = projected2D[segments + next].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    cylinderContainer.Children.Add(line);
                }

                // Draw vertical edges
                for (int i = 0; i < segments; i++)
                {
                    bool isHidden = (rotatedVertices[i][2] + rotatedVertices[segments + i][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[i].X,
                        Y1 = projected2D[i].Y,
                        X2 = projected2D[segments + i].X,
                        Y2 = projected2D[segments + i].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    cylinderContainer.Children.Add(line);
                }
            }

            // Vẽ nhãn tâm đáy (O, O')
            if (editor.ShowVertexLabels)
            {
                Point topCenter = new Point(
                    projected2D.Take(editor.Segments).Average(p => p.X),
                    projected2D.Take(editor.Segments).Average(p => p.Y)
                );
                TextBlock labelO = new TextBlock
                {
                    Text = "O",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(labelO, topCenter.X + 6);
                Canvas.SetTop(labelO, topCenter.Y - 15);
                cylinderContainer.Children.Add(labelO);

                Point bottomCenter = new Point(
                    projected2D.Skip(editor.Segments).Take(editor.Segments).Average(p => p.X),
                    projected2D.Skip(editor.Segments).Take(editor.Segments).Average(p => p.Y)
                );
                TextBlock labelOPrime = new TextBlock
                {
                    Text = "O'",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(labelOPrime, bottomCenter.X + 6);
                Canvas.SetTop(labelOPrime, bottomCenter.Y - 15);
                cylinderContainer.Children.Add(labelOPrime);
            }

            // Trim canvas to rectangle for cylinder (width = diameter, height = cylinder height)
            TrimCanvasToCylinderRectangle(cylinderContainer, editor.Radius, editor.Height, 10);

            // Position cylinder in center of canvas
            Canvas.SetLeft(cylinderContainer, (MainInteractiveBoard.ActualWidth - cylinderContainer.Width) / 2);
            Canvas.SetTop(cylinderContainer, (MainInteractiveBoard.ActualHeight - cylinderContainer.Height) / 2);

            // Enable drag & drop
            Enable3DShapeDragging(cylinderContainer);

            // Add to main canvas
            MainInteractiveBoard.Children.Add(cylinderContainer);
            
            // Add to undo stack
            RecordAddAction(cylinderContainer, "Draw");
        }

        /// <summary>
        /// Apply 3D Cylinder to canvas WITH educational stats panel (for "Display Stats" button)
        /// Creates TWO INDEPENDENT objects: 3D Shape + Stats Panel (can be moved and deleted separately)
        /// </summary>
        private void Apply3DCylinderWithStatsToCanvas(Form2_6_3DCylinderEditor editor)
        {
            // ========== 1. CREATE 3D CYLINDER SHAPE (INDEPENDENT OBJECT) ==========
            Canvas cylinderContainer = new Canvas
            {
                Width = 600,
                Height = 600,
                Background = Brushes.Transparent
            };

            double centerX = 300;
            double centerY = 300;

            // Generate cylinder vertices
            List<double[]> vertices = GenerateCylinderVertices(
                editor.Radius,
                editor.Height,
                editor.Segments);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GenerateCylinderFaces(editor.Segments);

            // Sort faces by depth for proper rendering
            List<(int faceIndex, double avgZ)> facesWithDepth = new List<(int, double)>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;
                facesWithDepth.Add((i, avgZ));
            }
            facesWithDepth.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawingMode == "Solid" || editor.DrawingMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesWithDepth)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon();
                    
                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    if (editor.FaceColor != Brushes.Transparent)
                    {
                        var color = ((SolidColorBrush)editor.FaceColor).Color;
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(editor.OpacityValue * 255),
                            color.R, color.G, color.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    cylinderContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawingMode == "Wireframe" || editor.DrawingMode == "Both")
            {
                int segments = editor.Segments;

                // Draw top circle
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    bool isHidden = (rotatedVertices[i][2] + rotatedVertices[next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[i].X,
                        Y1 = projected2D[i].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    cylinderContainer.Children.Add(line);
                }

                // Draw bottom circle
                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    bool isHidden = (rotatedVertices[segments + i][2] + rotatedVertices[segments + next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[segments + i].X,
                        Y1 = projected2D[segments + i].Y,
                        X2 = projected2D[segments + next].X,
                        Y2 = projected2D[segments + next].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    cylinderContainer.Children.Add(line);
                }

                // Draw vertical edges
                for (int i = 0; i < segments; i++)
                {
                    bool isHidden = (rotatedVertices[i][2] + rotatedVertices[segments + i][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[i].X,
                        Y1 = projected2D[i].Y,
                        X2 = projected2D[segments + i].X,
                        Y2 = projected2D[segments + i].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    cylinderContainer.Children.Add(line);
                }
            }

            // Vẽ nhãn tâm đáy (O, O')
            if (editor.ShowVertexLabels)
            {
                Point topCenter = new Point(
                    projected2D.Take(editor.Segments).Average(p => p.X),
                    projected2D.Take(editor.Segments).Average(p => p.Y)
                );
                TextBlock labelO = new TextBlock
                {
                    Text = "O",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(labelO, topCenter.X + 6);
                Canvas.SetTop(labelO, topCenter.Y - 15);
                cylinderContainer.Children.Add(labelO);

                Point bottomCenter = new Point(
                    projected2D.Skip(editor.Segments).Take(editor.Segments).Average(p => p.X),
                    projected2D.Skip(editor.Segments).Take(editor.Segments).Average(p => p.Y)
                );
                TextBlock labelOPrime = new TextBlock
                {
                    Text = "O'",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(labelOPrime, bottomCenter.X + 6);
                Canvas.SetTop(labelOPrime, bottomCenter.Y - 15);
                cylinderContainer.Children.Add(labelOPrime);
            }

            // Trim canvas to rectangle for cylinder (width = diameter, height = cylinder height)
            TrimCanvasToCylinderRectangle(cylinderContainer, editor.Radius, editor.Height, 10);

            // Position cylinder shape in center-left area of canvas
            Canvas.SetLeft(cylinderContainer, (MainInteractiveBoard.ActualWidth / 2) - cylinderContainer.Width - 20);
            Canvas.SetTop(cylinderContainer, (MainInteractiveBoard.ActualHeight - cylinderContainer.Height) / 2);

            // Enable drag & drop for cylinder shape (independent)
            Enable3DShapeDragging(cylinderContainer);

            // Add cylinder to main canvas
            MainInteractiveBoard.Children.Add(cylinderContainer);
            
            // Add to undo stack
            RecordAddAction(cylinderContainer, "Create 3D object");

            // ========== 2. CREATE EDUCATIONAL STATS PANEL (INDEPENDENT OBJECT) ==========
            // Calculate ACTUAL dimensions on canvas (after 3D projection)
            const double DPI = 96.0;
            const double MM_PER_INCH = 25.4;
            double pixelsToMm = MM_PER_INCH / DPI;

            // Calculate radius (distance from center to edge point)
            double actualRadiusPx = 0;
            if (projected2D.Count >= 1)
            {
                Point center = new Point(centerX, centerY);
                Point edgePoint = projected2D[0];
                actualRadiusPx = Math.Sqrt(Math.Pow(edgePoint.X - center.X, 2) + Math.Pow(edgePoint.Y - center.Y, 2));
            }
            double actualRadiusMm = actualRadiusPx * pixelsToMm;

            // Calculate height (distance between corresponding vertices on top and bottom)
            double actualHeightPx = 0;
            if (projected2D.Count >= editor.Segments + 1)
            {
                Point topVertex = projected2D[0];
                Point bottomVertex = projected2D[editor.Segments];
                actualHeightPx = Math.Sqrt(Math.Pow(bottomVertex.X - topVertex.X, 2) + Math.Pow(bottomVertex.Y - topVertex.Y, 2));
            }
            double actualHeightMm = actualHeightPx * pixelsToMm;

            // Calculate areas and volume based on ACTUAL mm dimensions
            double actualBaseAreaMm2 = Math.PI * actualRadiusMm * actualRadiusMm;
            double actualLateralAreaMm2 = 2 * Math.PI * actualRadiusMm * actualHeightMm;
            double actualTotalAreaMm2 = 2 * actualBaseAreaMm2 + actualLateralAreaMm2;
            double actualVolumeMm3 = actualBaseAreaMm2 * actualHeightMm;

            var statsPanel = new Border
            {
                Width = 380,
                Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15)
            };

            var stackPanel = new StackPanel();

            // Title
            AddStatsTextBlock(stackPanel, "📐 HÌNH TRỤ (CYLINDER)", 16, true, "#667EEA");
            AddStatsSeparator(stackPanel);

            // Dimensions - Show ACTUAL measured size in mm
            AddStatsTextBlock(stackPanel, "📏 KÍCH THƯỚC THỰC TẾ (trên màn hình):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Bán kính (r): {actualRadiusMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Chiều cao (h): {actualHeightMm:N1} mm", 11, false, "#555");
            AddStatsSeparator(stackPanel);

            // Formulas - Based on ACTUAL mm dimensions
            AddStatsTextBlock(stackPanel, "📐 CÔNG THỨC TÍNH (theo kích thước thực):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Diện tích đáy (S_đáy):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S = π × r²", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualBaseAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích xung quanh (S_xq):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S_xq = 2 × π × r × h", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualLateralAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích toàn phần (S_tp):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S_tp = 2 × S_đáy + S_xq", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualTotalAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Thể tích (V):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     V = S_đáy × h = π × r² × h", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualVolumeMm3:N2} mm³", 11, false, "#E91E63");
            AddStatsSeparator(stackPanel);

            // Topology
            AddStatsTextBlock(stackPanel, "🔢 ĐẶC ĐIỂM HÌNH HỌC:", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Hình trụ tròn đều", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • 2 đáy tròn song song", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Mặt xung quanh: hình chữ nhật", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Chu vi đáy: 2πr = {2 * Math.PI * actualRadiusMm:N2} mm", 11, false, "#9C27B0");

            statsPanel.Child = stackPanel;

            // Wrap stats panel in a Canvas container for drag & drop
            Canvas statsPanelContainer = new Canvas
            {
                Width = statsPanel.Width + 10,
                Height = stackPanel.ActualHeight > 0 ? stackPanel.ActualHeight + 30 : 600,
                Background = Brushes.Transparent
            };
            
            Canvas.SetLeft(statsPanel, 0);
            Canvas.SetTop(statsPanel, 0);
            statsPanelContainer.Children.Add(statsPanel);

            // Position stats panel to the right of cylinder shape
            Canvas.SetLeft(statsPanelContainer, (MainInteractiveBoard.ActualWidth - 1050) / 2 + 630);
            Canvas.SetTop(statsPanelContainer, (MainInteractiveBoard.ActualHeight - 600) / 2 + 10);

            // Enable drag & drop for stats panel (independent)
            Enable3DShapeDragging(statsPanelContainer);

            // Add stats panel to main canvas
            MainInteractiveBoard.Children.Add(statsPanelContainer);
            
            // Add to undo stack
            RecordAddAction(statsPanelContainer, "Draw");
        }

        // Helper method for cylinder vertices generation
        private List<double[]> GenerateCylinderVertices(double radius, double height, int segments)
        {
            List<double[]> vertices = new List<double[]>();
            double angleStep = 2 * Math.PI / segments;

            // Top circle vertices
            for (int i = 0; i < segments; i++)
            {
                double angle = i * angleStep;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                vertices.Add(new double[] { x, height / 2, z });
            }

            // Bottom circle vertices
            for (int i = 0; i < segments; i++)
            {
                double angle = i * angleStep;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                vertices.Add(new double[] { x, -height / 2, z });
            }

            // Center points
            vertices.Add(new double[] { 0, height / 2, 0 });  // Top center
            vertices.Add(new double[] { 0, -height / 2, 0 }); // Bottom center

            return vertices;
        }

        private List<int[]> GenerateCylinderFaces(int segments)
        {
            List<int[]> faces = new List<int[]>();

            // Lateral faces (quads)
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                faces.Add(new int[] {
                    i,              // Top current
                    next,           // Top next
                    segments + next, // Bottom next
                    segments + i    // Bottom current
                });
            }

            // Top cap (triangles)
            int topCenterIndex = segments * 2;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                faces.Add(new int[] { topCenterIndex, i, next });
            }

            // Bottom cap (triangles)
            int bottomCenterIndex = segments * 2 + 1;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                faces.Add(new int[] { bottomCenterIndex, segments + next, segments + i });
            }

            return faces;
        }

        private void Apply3DConeToCanvas(Form2_6_3DConeEditor editor)
        {
            // Create a canvas to hold the 3D cone
            Canvas coneContainer = new Canvas
            {
                Width = 600,
                Height = 600,
                Background = Brushes.Transparent
            };

            double centerX = 300;
            double centerY = 300;

            // Generate cone vertices
            List<double[]> vertices = GenerateConeVertices(
                editor.BaseRadius,
                editor.ConeHeight,
                editor.Segments);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GenerateConeFaces(editor.Segments);

            // Sort faces by depth for proper rendering
            List<(int faceIndex, double avgZ)> facesWithDepth = new List<(int, double)>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;
                facesWithDepth.Add((i, avgZ));
            }
            facesWithDepth.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawingMode == "Solid" || editor.DrawingMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesWithDepth)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon();
                    
                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    // Apply face color with opacity
                    if (editor.FaceColor != Brushes.Transparent)
                    {
                        var color = ((SolidColorBrush)editor.FaceColor).Color;
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(editor.OpacityValue * 255),
                            color.R, color.G, color.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    coneContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawingMode == "Wireframe" || editor.DrawingMode == "Both")
            {
                int segments = editor.Segments;
                int apexIndex = 0;

                // Tính độ sâu của apex và base
                double apexZ = rotatedVertices[apexIndex][2];
                double avgBaseZ = 0;
                for (int i = 1; i <= segments; i++)
                {
                    avgBaseZ += rotatedVertices[i][2];
                }
                avgBaseZ /= segments;

                // Draw base circle
                bool baseHidden = avgBaseZ < apexZ;
                for (int i = 0; i < segments; i++)
                {
                    int current = i + 1;
                    int next = (i + 1) % segments + 1;
                    bool isHidden = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[current].X,
                        Y1 = projected2D[current].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    coneContainer.Children.Add(line);
                }

                // Draw lateral edges from apex to base
                for (int i = 0; i < segments; i++)
                {
                    int baseVertex = i + 1;
                    bool isHidden = (rotatedVertices[apexIndex][2] + rotatedVertices[baseVertex][2]) / 2 >= 0;
                    
                    Line line = new Line
                    {
                        X1 = projected2D[apexIndex].X,
                        Y1 = projected2D[apexIndex].Y,
                        X2 = projected2D[baseVertex].X,
                        Y2 = projected2D[baseVertex].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    coneContainer.Children.Add(line);
                }
            }

            // Vẽ nhãn đỉnh (S = đỉnh chóp hình nón)
            if (editor.ShowVertexLabels)
            {
                TextBlock apexLabel = new TextBlock
                {
                    Text = "S",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(apexLabel, projected2D[0].X + 8);
                Canvas.SetTop(apexLabel, projected2D[0].Y - 20);
                coneContainer.Children.Add(apexLabel);
            }

            // Trim canvas to remove white space
            TrimCanvasToContent(coneContainer, 10);

            // Position cone in center of canvas
            double leftPos = (MainInteractiveBoard.ActualWidth - coneContainer.Width) / 2;
            double topPos = (MainInteractiveBoard.ActualHeight - coneContainer.Height) / 2;

            if (double.IsNaN(leftPos) || double.IsInfinity(leftPos)) leftPos = (1920 - coneContainer.Width) / 2;
            if (double.IsNaN(topPos) || double.IsInfinity(topPos)) topPos = (1080 - coneContainer.Height) / 2;
            if (double.IsNaN(leftPos) || double.IsInfinity(leftPos)) leftPos = 660;
            if (double.IsNaN(topPos) || double.IsInfinity(topPos)) topPos = 240;

            Canvas.SetLeft(coneContainer, leftPos);
            Canvas.SetTop(coneContainer, topPos);

            // Enable drag & drop
            Enable3DShapeDragging(coneContainer);

            // Add to main canvas
            MainInteractiveBoard.Children.Add(coneContainer);
            
            // Add to undo stack
            RecordAddAction(coneContainer, "Draw");
        }

        // Helper method for cone vertices generation
        private List<double[]> GenerateConeVertices(double radius, double height, int segments)
        {
            List<double[]> vertices = new List<double[]>();
            double angleStep = 2 * Math.PI / segments;

            // Apex (top point) - Flipped to show apex at top when rendered
            vertices.Add(new double[] { 0, -height / 2, 0 });

            // Base circle vertices
            for (int i = 0; i < segments; i++)
            {
                double angle = i * angleStep;
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                vertices.Add(new double[] { x, height / 2, z });
            }

            // Base center point
            vertices.Add(new double[] { 0, height / 2, 0 });

            return vertices;
        }

        private List<int[]> GenerateConeFaces(int segments)
        {
            List<int[]> faces = new List<int[]>();
            int apexIndex = 0;
            int baseCenterIndex = segments + 1;

            // Lateral triangular faces
            for (int i = 0; i < segments; i++)
            {
                int current = i + 1;
                int next = (i + 1) % segments + 1;
                faces.Add(new int[] { apexIndex, next, current });
            }

            // Base triangular faces
            for (int i = 0; i < segments; i++)
            {
                int current = i + 1;
                int next = (i + 1) % segments + 1;
                faces.Add(new int[] { baseCenterIndex, current, next });
            }

            return faces;
        }

        /// <summary>
        /// Apply 3D Cone to canvas WITH educational stats panel (for "Display Stats" button)
        /// Creates TWO INDEPENDENT objects: 3D Shape + Stats Panel (can be moved and deleted separately)
        /// </summary>
        private void Apply3DConeWithStatsToCanvas(Form2_6_3DConeEditor editor)
        {
            // Debug: Confirm this function is called
            System.Diagnostics.Debug.WriteLine("🔍 Apply3DConeWithStatsToCanvas được gọi!");
            
            // ========== 1. CREATE 3D CONE SHAPE (INDEPENDENT OBJECT) ==========
            Canvas coneContainer = new Canvas
            {
                Width = 600,
                Height = 600,
                Background = Brushes.Transparent
            };

            double centerX = 300;
            double centerY = 300;

            // Generate cone vertices
            List<double[]> vertices = GenerateConeVertices(
                editor.BaseRadius,
                editor.ConeHeight,
                editor.Segments);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GenerateConeFaces(editor.Segments);

            // Sort faces by depth for proper rendering
            List<(int faceIndex, double avgZ)> facesWithDepth = new List<(int, double)>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;
                facesWithDepth.Add((i, avgZ));
            }
            facesWithDepth.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawingMode == "Solid" || editor.DrawingMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesWithDepth)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon();
                    
                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    if (editor.FaceColor != Brushes.Transparent)
                    {
                        var color = ((SolidColorBrush)editor.FaceColor).Color;
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(editor.OpacityValue * 255),
                            color.R, color.G, color.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    coneContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawingMode == "Wireframe" || editor.DrawingMode == "Both")
            {
                int segments = editor.Segments;
                int apexIndex = 0;

                double apexZ = rotatedVertices[apexIndex][2];
                double avgBaseZ = 0;
                for (int i = 1; i <= segments; i++)
                {
                    avgBaseZ += rotatedVertices[i][2];
                }
                avgBaseZ /= segments;

                // Draw base circle
                for (int i = 0; i < segments; i++)
                {
                    int current = i + 1;
                    int next = (i + 1) % segments + 1;
                    bool isHidden = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[current].X,
                        Y1 = projected2D[current].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    coneContainer.Children.Add(line);
                }

                // Draw lateral edges from apex to base
                for (int i = 0; i < segments; i++)
                {
                    int baseVertex = i + 1;
                    bool isHidden = (rotatedVertices[apexIndex][2] + rotatedVertices[baseVertex][2]) / 2 >= 0;
                    
                    Line line = new Line
                    {
                        X1 = projected2D[apexIndex].X,
                        Y1 = projected2D[apexIndex].Y,
                        X2 = projected2D[baseVertex].X,
                        Y2 = projected2D[baseVertex].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    coneContainer.Children.Add(line);
                }
            }

            // Vẽ nhãn đỉnh (S = đỉnh chóp hình nón)
            if (editor.ShowVertexLabels)
            {
                TextBlock apexLabel = new TextBlock
                {
                    Text = "S",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(apexLabel, projected2D[0].X + 8);
                Canvas.SetTop(apexLabel, projected2D[0].Y - 20);
                coneContainer.Children.Add(apexLabel);
            }

            // Trim canvas to remove white space
            TrimCanvasToContent(coneContainer, 10);

            // Position cone shape in center-left area of canvas
            double leftPosStats = (MainInteractiveBoard.ActualWidth / 2) - coneContainer.Width - 20;
            double topPosStats = (MainInteractiveBoard.ActualHeight - coneContainer.Height) / 2;

            if (double.IsNaN(leftPosStats) || double.IsInfinity(leftPosStats)) leftPosStats = (1920 / 2) - coneContainer.Width - 20;
            if (double.IsNaN(topPosStats) || double.IsInfinity(topPosStats)) topPosStats = (1080 - coneContainer.Height) / 2;
            if (double.IsNaN(leftPosStats) || double.IsInfinity(leftPosStats)) leftPosStats = 400;
            if (double.IsNaN(topPosStats) || double.IsInfinity(topPosStats)) topPosStats = 240;

            Canvas.SetLeft(coneContainer, leftPosStats);
            Canvas.SetTop(coneContainer, topPosStats);

            // Enable drag & drop for cone shape (independent)
            Enable3DShapeDragging(coneContainer);

            // Add cone to main canvas
            MainInteractiveBoard.Children.Add(coneContainer);
            System.Diagnostics.Debug.WriteLine($"✅ Đã thêm CONE container vào canvas. Children count: {MainInteractiveBoard.Children.Count}");
            
            // Add to undo stack
            RecordAddAction(coneContainer, "Create 3D object");

            // ========== 2. CREATE EDUCATIONAL STATS PANEL (INDEPENDENT OBJECT) ==========
            // Calculate ACTUAL dimensions on canvas (after 3D projection)
            const double DPI = 96.0;
            const double MM_PER_INCH = 25.4;
            double pixelsToMm = MM_PER_INCH / DPI;

            // Calculate base radius (distance from center to base edge point)
            double actualBaseRadiusPx = 0;
            if (projected2D.Count >= 2)
            {
                Point baseCenter = new Point(centerX, centerY);
                Point baseEdgePoint = projected2D[1];
                actualBaseRadiusPx = Math.Sqrt(Math.Pow(baseEdgePoint.X - baseCenter.X, 2) + Math.Pow(baseEdgePoint.Y - baseCenter.Y, 2));
            }
            double actualBaseRadiusMm = actualBaseRadiusPx * pixelsToMm;

            // Calculate height (distance from apex to base center)
            double actualHeightPx = 0;
            if (projected2D.Count >= 1)
            {
                Point apexPoint = projected2D[0];
                int baseCenterIndex = editor.Segments + 1;
                if (projected2D.Count > baseCenterIndex)
                {
                    Point baseCenter = projected2D[baseCenterIndex];
                    actualHeightPx = Math.Sqrt(Math.Pow(baseCenter.X - apexPoint.X, 2) + Math.Pow(baseCenter.Y - apexPoint.Y, 2));
                }
            }
            double actualHeightMm = actualHeightPx * pixelsToMm;

            // Calculate slant height, areas and volume based on ACTUAL mm dimensions
            double actualSlantHeightMm = Math.Sqrt(actualBaseRadiusMm * actualBaseRadiusMm + actualHeightMm * actualHeightMm);
            double actualBaseAreaMm2 = Math.PI * actualBaseRadiusMm * actualBaseRadiusMm;
            double actualLateralAreaMm2 = Math.PI * actualBaseRadiusMm * actualSlantHeightMm;
            double actualTotalAreaMm2 = actualBaseAreaMm2 + actualLateralAreaMm2;
            double actualVolumeMm3 = (1.0 / 3.0) * actualBaseAreaMm2 * actualHeightMm;

            var statsPanel = new Border
            {
                Width = 380,
                Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15)
            };

            var stackPanel = new StackPanel();

            // Title
            AddStatsTextBlock(stackPanel, "📐 HÌNH NÓN (CONE)", 16, true, "#667EEA");
            AddStatsSeparator(stackPanel);

            // Dimensions - Show ACTUAL measured size in mm
            AddStatsTextBlock(stackPanel, "📏 KÍCH THƯỚC THỰC TẾ (trên màn hình):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Bán kính đáy (r): {actualBaseRadiusMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Chiều cao (h): {actualHeightMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Đường sinh (l): {actualSlantHeightMm:N1} mm", 11, false, "#555");
            AddStatsSeparator(stackPanel);

            // Formulas - Based on ACTUAL mm dimensions
            AddStatsTextBlock(stackPanel, "📐 CÔNG THỨC TÍNH (theo kích thước thực):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Đường sinh (l):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     l = √(r² + h²)", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualSlantHeightMm:N2} mm", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích đáy (S_đáy):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S = π × r²", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualBaseAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích xung quanh (S_xq):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S_xq = π × r × l", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualLateralAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích toàn phần (S_tp):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S_tp = S_đáy + S_xq", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualTotalAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Thể tích (V):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     V = (1/3) × π × r² × h", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualVolumeMm3:N2} mm³", 11, false, "#E91E63");
            AddStatsSeparator(stackPanel);

            // Topology
            AddStatsTextBlock(stackPanel, "🔢 ĐẶC ĐIỂM HÌNH HỌC:", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Hình nón tròn đều", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • 1 đỉnh (apex)", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • 1 đáy tròn", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Mặt xung quanh: hình quạt tròn", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Chu vi đáy: 2πr = {2 * Math.PI * actualBaseRadiusMm:N2} mm", 11, false, "#9C27B0");

            statsPanel.Child = stackPanel;

            // Add shadow effect
            statsPanel.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 315,
                ShadowDepth = 5,
                BlurRadius = 15,
                Opacity = 0.3
            };

            // Wrap stats panel in a Canvas container for drag & drop
            Canvas statsPanelContainer = new Canvas
            {
                Width = statsPanel.Width + 10,
                Height = 700, // Fixed height to ensure visibility
                Background = Brushes.Transparent
            };
            
            Canvas.SetLeft(statsPanel, 0);
            Canvas.SetTop(statsPanel, 0);
            statsPanelContainer.Children.Add(statsPanel);

            // Position stats panel to the right of cone shape
            Canvas.SetLeft(statsPanelContainer, (MainInteractiveBoard.ActualWidth - 1050) / 2 + 630);
            Canvas.SetTop(statsPanelContainer, (MainInteractiveBoard.ActualHeight - 600) / 2);

            // Enable drag & drop for stats panel (independent)
            Enable3DShapeDragging(statsPanelContainer);

            // Add stats panel to main canvas
            MainInteractiveBoard.Children.Add(statsPanelContainer);
            System.Diagnostics.Debug.WriteLine($"✅ Đã thêm STATS PANEL vào canvas. Children count: {MainInteractiveBoard.Children.Count}");
            
            // Add to undo stack
            RecordAddAction(statsPanelContainer, "Draw");
            
            System.Diagnostics.Debug.WriteLine("🎉 Apply3DConeWithStatsToCanvas HOÀN THÀNH - Đã tạo 2 đối tượng độc lập!");
        }

        private void Apply3DPyramidToCanvas(Form2_6_3DPyramidEditor editor)
        {
            // Create a canvas to hold the 3D pyramid
            Canvas pyramidContainer = new Canvas
            {
                Width = 650,
                Height = 650,
                Background = Brushes.Transparent
            };

            double centerX = 325;
            double centerY = 325;

            // Generate pyramid vertices
            List<double[]> vertices = GeneratePyramidVertices(
                editor.BaseSize,
                editor.PyramidHeight,
                editor.BaseType);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GeneratePyramidFaces(editor.BaseType);

            // Sort faces by depth for proper rendering
            List<(int faceIndex, double avgZ)> facesWithDepth = new List<(int, double)>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;
                facesWithDepth.Add((i, avgZ));
            }
            facesWithDepth.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawingMode == "Solid" || editor.DrawingMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesWithDepth)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon();
                    
                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    // Apply face color with opacity
                    if (editor.FaceColor != Brushes.Transparent)
                    {
                        var color = ((SolidColorBrush)editor.FaceColor).Color;
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(editor.OpacityValue * 255),
                            color.R, color.G, color.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    pyramidContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawingMode == "Wireframe" || editor.DrawingMode == "Both")
            {
                int sides = GetPyramidSides(editor.BaseType);
                int apexIndex = 0;

                // Tính xem mặt nào là mặt sau
                bool[] isFaceBack = new bool[faces.Count];
                for (int i = 0; i < faces.Count; i++)
                {
                    int[] f = faces[i];
                    if (f.Length >= 3)
                    {
                        double[] v1 = rotatedVertices[f[0]];
                        double[] v2 = rotatedVertices[f[1]];
                        double[] v3 = rotatedVertices[f[2]];
                        double[] normal = CalculateFaceNormal(v1, v2, v3);
                        isFaceBack[i] = normal[2] >= 0;
                    }
                }

                // Hàm kiểm tra cạnh khuất (cả 2 mặt chia sẻ đều là mặt sau)
                bool IsEdgeHidden(int u, int v)
                {
                    List<int> sharingFaces = new List<int>();
                    for (int i = 0; i < faces.Count; i++)
                    {
                        bool hasU = false, hasV = false;
                        foreach (int idx in faces[i])
                        {
                            if (idx == u) hasU = true;
                            if (idx == v) hasV = true;
                        }
                        if (hasU && hasV)
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
                    bool isHidden = IsEdgeHidden(current, next);

                    Line line = new Line
                    {
                        X1 = projected2D[current].X,
                        Y1 = projected2D[current].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };

                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }

                    pyramidContainer.Children.Add(line);
                }

                // Draw lateral edges from apex to base vertices
                for (int i = 0; i < sides; i++)
                {
                    int baseVertex = i + 1;
                    bool isHidden = IsEdgeHidden(apexIndex, baseVertex);

                    Line line = new Line
                    {
                        X1 = projected2D[apexIndex].X,
                        Y1 = projected2D[apexIndex].Y,
                        X2 = projected2D[baseVertex].X,
                        Y2 = projected2D[baseVertex].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };

                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }

                    pyramidContainer.Children.Add(line);
                }
            }

            // Vẽ nhãn đỉnh (S = đỉnh chóp, A,B,C... = đáy)
            if (editor.ShowVertexLabels)
            {
                int labelSides = GetPyramidSides(editor.BaseType);
                string[] baseNames = labelSides switch
                {
                    3 => new[] { "A", "B", "C" },
                    4 => new[] { "A", "B", "C", "D" },
                    5 => new[] { "A", "B", "C", "D", "E" },
                    6 => new[] { "A", "B", "C", "D", "E", "F" },
                    _ => new[] { "A", "B", "C", "D" }
                };

                // Apex label (S) - vertex 0
                TextBlock apexLabel = new TextBlock
                {
                    Text = "S",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(apexLabel, projected2D[0].X + 8);
                Canvas.SetTop(apexLabel, projected2D[0].Y - 20);
                pyramidContainer.Children.Add(apexLabel);

                // Base vertex labels (A, B, C, D...) - vertices 1..n
                for (int i = 0; i < labelSides; i++)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = baseNames[i],
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, projected2D[i + 1].X + 8);
                    Canvas.SetTop(label, projected2D[i + 1].Y - 16);
                    pyramidContainer.Children.Add(label);
                }
            }

            // Trim canvas to rectangle for pyramid (width = base, height = pyramid height)
            TrimCanvasToPyramidRectangle(pyramidContainer, editor.BaseSize, editor.PyramidHeight, 10);

            // Position pyramid in center of canvas
            Canvas.SetLeft(pyramidContainer, (MainInteractiveBoard.ActualWidth - pyramidContainer.Width) / 2);
            Canvas.SetTop(pyramidContainer, (MainInteractiveBoard.ActualHeight - pyramidContainer.Height) / 2);

            // Enable drag & drop
            Enable3DShapeDragging(pyramidContainer);

            // Add to main canvas
            MainInteractiveBoard.Children.Add(pyramidContainer);
            
            // Add to undo stack
            RecordAddAction(pyramidContainer, "Draw");
        }

        // Helper methods for pyramid generation
        private List<double[]> GeneratePyramidVertices(double size, double height, string baseType)
        {
            List<double[]> vertices = new List<double[]>();

            // Apex (top point) - Flipped to show apex at top when rendered
            vertices.Add(new double[] { 0, -height / 2, 0 });

            // Generate base polygon vertices
            int sides = GetPyramidSides(baseType);
            double angleStep = 2 * Math.PI / sides;
            double radius = GetPyramidCircumradius(size, sides);

            for (int i = 0; i < sides; i++)
            {
                double angle = i * angleStep - Math.PI / 2;
                if (baseType == "Square")
                {
                    angle += Math.PI / 4;
                }
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                vertices.Add(new double[] { x, height / 2, z });
            }

            // Base center point
            vertices.Add(new double[] { 0, height / 2, 0 });

            return vertices;
        }

        private List<int[]> GeneratePyramidFaces(string baseType)
        {
            List<int[]> faces = new List<int[]>();
            int sides = GetPyramidSides(baseType);
            int apexIndex = 0;

            // Lateral triangular faces
            for (int i = 0; i < sides; i++)
            {
                int current = i + 1;
                int next = (i + 1) % sides + 1;
                faces.Add(new int[] { apexIndex, next, current });
            }

            // Base face (polygon)
            var baseFace = new int[sides];
            for (int i = 0; i < sides; i++)
            {
                baseFace[i] = i + 1;
            }
            faces.Add(baseFace);

            return faces;
        }

        private int GetPyramidSides(string baseType)
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

        private double GetPyramidCircumradius(double sideLength, int sides)
        {
            // Circumradius formula: R = a / (2 * sin(π/n))
            return sideLength / (2 * Math.Sin(Math.PI / sides));
        }

        /// <summary>
        /// Apply 3D Pyramid to canvas WITH educational stats panel (for "Display Stats" button)
        /// Creates TWO INDEPENDENT objects: 3D Shape + Stats Panel (can be moved and deleted separately)
        /// </summary>
        private void Apply3DPyramidWithStatsToCanvas(Form2_6_3DPyramidEditor editor)
        {
            // ========== 1. CREATE 3D PYRAMID SHAPE (INDEPENDENT OBJECT) ==========
            Canvas pyramidContainer = new Canvas
            {
                Width = 650,
                Height = 650,
                Background = Brushes.Transparent
            };

            double centerX = 325;
            double centerY = 325;

            // Generate pyramid vertices
            List<double[]> vertices = GeneratePyramidVertices(
                editor.BaseSize,
                editor.PyramidHeight,
                editor.BaseType);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GeneratePyramidFaces(editor.BaseType);

            // Sort faces by depth for proper rendering
            List<(int faceIndex, double avgZ)> facesWithDepth = new List<(int, double)>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;
                facesWithDepth.Add((i, avgZ));
            }
            facesWithDepth.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawingMode == "Solid" || editor.DrawingMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesWithDepth)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon();
                    
                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    if (editor.FaceColor != Brushes.Transparent)
                    {
                        var color = ((SolidColorBrush)editor.FaceColor).Color;
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(editor.OpacityValue * 255),
                            color.R, color.G, color.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    pyramidContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawingMode == "Wireframe" || editor.DrawingMode == "Both")
            {
                int wireSideCount = GetPyramidSides(editor.BaseType);
                int apexIndex = 0;

                // Tính xem mặt nào là mặt sau
                bool[] isFaceBack = new bool[faces.Count];
                for (int i = 0; i < faces.Count; i++)
                {
                    int[] f = faces[i];
                    if (f.Length >= 3)
                    {
                        double[] v1 = rotatedVertices[f[0]];
                        double[] v2 = rotatedVertices[f[1]];
                        double[] v3 = rotatedVertices[f[2]];
                        double[] normal = CalculateFaceNormal(v1, v2, v3);
                        isFaceBack[i] = normal[2] >= 0;
                    }
                }

                // Hàm kiểm tra cạnh khuất (cả 2 mặt chia sẻ đều là mặt sau)
                bool IsEdgeHidden(int u, int v)
                {
                    List<int> sharingFaces = new List<int>();
                    for (int i = 0; i < faces.Count; i++)
                    {
                        bool hasU = false, hasV = false;
                        foreach (int idx in faces[i])
                        {
                            if (idx == u) hasU = true;
                            if (idx == v) hasV = true;
                        }
                        if (hasU && hasV)
                        {
                            sharingFaces.Add(i);
                        }
                    }
                    return sharingFaces.Count == 2 && isFaceBack[sharingFaces[0]] && isFaceBack[sharingFaces[1]];
                }

                // Draw base polygon edges
                for (int i = 0; i < wireSideCount; i++)
                {
                    int current = i + 1;
                    int next = (i + 1) % wireSideCount + 1;
                    bool isHidden = IsEdgeHidden(current, next);

                    Line line = new Line
                    {
                        X1 = projected2D[current].X,
                        Y1 = projected2D[current].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };

                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }

                    pyramidContainer.Children.Add(line);
                }

                // Draw lateral edges from apex to base vertices
                for (int i = 0; i < wireSideCount; i++)
                {
                    int baseVertex = i + 1;
                    bool isHidden = IsEdgeHidden(apexIndex, baseVertex);

                    Line line = new Line
                    {
                        X1 = projected2D[apexIndex].X,
                        Y1 = projected2D[apexIndex].Y,
                        X2 = projected2D[baseVertex].X,
                        Y2 = projected2D[baseVertex].Y,
                        Stroke = (SolidColorBrush)editor.EdgeColor,
                        StrokeThickness = editor.EdgeThickness
                    };

                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }

                    pyramidContainer.Children.Add(line);
                }
            }

            // Vẽ nhãn đỉnh (S = đỉnh chóp, A,B,C... = đáy)
            if (editor.ShowVertexLabels)
            {
                int labelSides = GetPyramidSides(editor.BaseType);
                string[] baseNames = labelSides switch
                {
                    3 => new[] { "A", "B", "C" },
                    4 => new[] { "A", "B", "C", "D" },
                    5 => new[] { "A", "B", "C", "D", "E" },
                    6 => new[] { "A", "B", "C", "D", "E", "F" },
                    _ => new[] { "A", "B", "C", "D" }
                };

                TextBlock apexLabel = new TextBlock
                {
                    Text = "S",
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                };
                Canvas.SetLeft(apexLabel, projected2D[0].X + 8);
                Canvas.SetTop(apexLabel, projected2D[0].Y - 20);
                pyramidContainer.Children.Add(apexLabel);

                for (int i = 0; i < labelSides; i++)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = baseNames[i],
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, projected2D[i + 1].X + 8);
                    Canvas.SetTop(label, projected2D[i + 1].Y - 16);
                    pyramidContainer.Children.Add(label);
                }
            }

            // Trim canvas to rectangle for pyramid (width = base, height = pyramid height)
            TrimCanvasToPyramidRectangle(pyramidContainer, editor.BaseSize, editor.PyramidHeight, 10);

            // Position pyramid shape in center-left area of canvas
            Canvas.SetLeft(pyramidContainer, (MainInteractiveBoard.ActualWidth / 2) - pyramidContainer.Width - 20);
            Canvas.SetTop(pyramidContainer, (MainInteractiveBoard.ActualHeight - pyramidContainer.Height) / 2);

            // Enable drag & drop for pyramid shape (independent)
            Enable3DShapeDragging(pyramidContainer);

            // Add pyramid to main canvas
            MainInteractiveBoard.Children.Add(pyramidContainer);
            
            // Add to undo stack
            RecordAddAction(pyramidContainer, "Create 3D object");

            // ========== 2. CREATE EDUCATIONAL STATS PANEL (INDEPENDENT OBJECT) ==========
            // Calculate ACTUAL dimensions on canvas (after 3D projection)
            const double DPI = 96.0;
            const double MM_PER_INCH = 25.4;
            double pixelsToMm = MM_PER_INCH / DPI;

            // Calculate base side length (distance between two adjacent base vertices)
            int sides = GetPyramidSides(editor.BaseType);
            double actualBaseSidePx = 0;
            if (projected2D.Count >= sides + 1)
            {
                Point vertex1 = projected2D[1];
                Point vertex2 = projected2D[2];
                actualBaseSidePx = Math.Sqrt(Math.Pow(vertex2.X - vertex1.X, 2) + Math.Pow(vertex2.Y - vertex1.Y, 2));
            }
            double actualBaseSideMm = actualBaseSidePx * pixelsToMm;

            // Calculate height (distance from apex to base center)
            double actualHeightPx = 0;
            if (projected2D.Count >= 1)
            {
                Point apexPoint = projected2D[0];
                int baseCenterIndex = sides + 1;
                if (projected2D.Count > baseCenterIndex)
                {
                    Point baseCenter = projected2D[baseCenterIndex];
                    actualHeightPx = Math.Sqrt(Math.Pow(baseCenter.X - apexPoint.X, 2) + Math.Pow(baseCenter.Y - apexPoint.Y, 2));
                }
            }
            double actualHeightMm = actualHeightPx * pixelsToMm;

            // Calculate base area based on base type and ACTUAL mm dimensions
            double actualBaseAreaMm2 = sides switch
            {
                3 => (Math.Sqrt(3) / 4) * actualBaseSideMm * actualBaseSideMm, // Triangle
                4 => actualBaseSideMm * actualBaseSideMm, // Square
                5 => (1.0 / 4.0) * Math.Sqrt(5 * (5 + 2 * Math.Sqrt(5))) * actualBaseSideMm * actualBaseSideMm, // Pentagon
                6 => (3 * Math.Sqrt(3) / 2) * actualBaseSideMm * actualBaseSideMm, // Hexagon
                _ => actualBaseSideMm * actualBaseSideMm
            };

            // Calculate apothem (perpendicular distance from center to mid-side)
            double apothemMm = actualBaseSideMm / (2 * Math.Tan(Math.PI / sides));
            
            // Calculate slant height (from apex to mid-side of base)
            double slantHeightMm = Math.Sqrt(apothemMm * apothemMm + actualHeightMm * actualHeightMm);
            
            // Calculate areas and volume
            double perimeterMm = sides * actualBaseSideMm;
            double actualLateralAreaMm2 = (perimeterMm * slantHeightMm) / 2;
            double actualTotalAreaMm2 = actualBaseAreaMm2 + actualLateralAreaMm2;
            double actualVolumeMm3 = (1.0 / 3.0) * actualBaseAreaMm2 * actualHeightMm;

            string baseTypeName = editor.BaseType switch
            {
                "Triangle" => "Tam giác",
                "Square" => "Hình vuông",
                "Pentagon" => "Ngũ giác",
                "Hexagon" => "Lục giác",
                _ => "Hình vuông"
            };

            var statsPanel = new Border
            {
                Width = 390,
                Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15)
            };

            var stackPanel = new StackPanel();

            // Title
            AddStatsTextBlock(stackPanel, $"📐 HÌNH CHÓP (PYRAMID)", 16, true, "#667EEA");
            AddStatsSeparator(stackPanel);

            // Dimensions - Show ACTUAL measured size in mm
            AddStatsTextBlock(stackPanel, "📏 KÍCH THƯỚC THỰC TẾ (trên màn hình):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Loại đáy: {baseTypeName} ({sides} cạnh)", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Cạnh đáy (a): {actualBaseSideMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Chiều cao (h): {actualHeightMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Đường sinh (l): {slantHeightMm:N1} mm", 11, false, "#555");
            AddStatsSeparator(stackPanel);

            // Formulas - Based on ACTUAL mm dimensions
            AddStatsTextBlock(stackPanel, "📐 CÔNG THỨC TÍNH (theo kích thước thực):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Diện tích đáy (S_đáy):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     Phụ thuộc vào loại đáy {baseTypeName}", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualBaseAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích xung quanh (S_xq):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S_xq = (P × l) / 2", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     P = chu vi đáy = {perimeterMm:N2} mm", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualLateralAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích toàn phần (S_tp):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S_tp = S_đáy + S_xq", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualTotalAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Thể tích (V):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     V = (1/3) × S_đáy × h", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualVolumeMm3:N2} mm³", 11, false, "#E91E63");
            AddStatsSeparator(stackPanel);

            // Topology
            AddStatsTextBlock(stackPanel, "🔢 ĐẶC ĐIỂM HÌNH HỌC:", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Đa giác đáy: {baseTypeName}", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Số đỉnh: {sides + 1} (1 đỉnh + {sides} đỉnh đáy)", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Số cạnh: {sides * 2} ({sides} cạnh đáy + {sides} cạnh bên)", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Số mặt: {sides + 1} (1 đáy + {sides} mặt tam giác)", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Công thức Euler: V - E + F = 2", 11, false, "#9C27B0");
            AddStatsTextBlock(stackPanel, $"     {sides + 1} - {sides * 2} + {sides + 1} = 2 âœ“", 11, false, "#9C27B0");

            statsPanel.Child = stackPanel;

            // Wrap stats panel in a Canvas container for drag & drop
            Canvas statsPanelContainer = new Canvas
            {
                Width = statsPanel.Width + 10,
                Height = stackPanel.ActualHeight > 0 ? stackPanel.ActualHeight + 30 : 700,
                Background = Brushes.Transparent
            };
            
            Canvas.SetLeft(statsPanel, 0);
            Canvas.SetTop(statsPanel, 0);
            statsPanelContainer.Children.Add(statsPanel);

            // Position stats panel to the right of pyramid shape
            Canvas.SetLeft(statsPanelContainer, (MainInteractiveBoard.ActualWidth - 1100) / 2 + 680);
            Canvas.SetTop(statsPanelContainer, (MainInteractiveBoard.ActualHeight - 650) / 2 + 10);

            // Enable drag & drop for stats panel (independent)
            Enable3DShapeDragging(statsPanelContainer);

            // Add stats panel to main canvas
            MainInteractiveBoard.Children.Add(statsPanelContainer);
            
            // Add to undo stack
            RecordAddAction(statsPanelContainer, "Draw");
        }

        private void Apply3DPrismToCanvas(Form2_6_3DPrismEditor editor)
        {
            // Create a canvas to hold the 3D prism
            Canvas prismContainer = new Canvas
            {
                Width = 650,
                Height = 650,
                Background = Brushes.Transparent
            };

            double centerX = 325;
            double centerY = 325;

            // Generate prism vertices
            List<double[]> vertices = GeneratePrismVertices(
                editor.BaseSize,
                editor.Height,
                editor.BaseType);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GeneratePrismFaces(editor.BaseType);

            // Sort faces by depth for proper rendering
            List<(int faceIndex, double avgZ)> facesWithDepth = new List<(int, double)>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;
                facesWithDepth.Add((i, avgZ));
            }
            facesWithDepth.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawingMode == "Solid" || editor.DrawingMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesWithDepth)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon();
                    
                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    // Apply face color with opacity
                    if (editor.FaceColor != Colors.Transparent)
                    {
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(editor.Opacity * 255),
                            editor.FaceColor.R, editor.FaceColor.G, editor.FaceColor.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    prismContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawingMode == "Wireframe" || editor.DrawingMode == "Both")
            {
                int sides = GetPyramidSides(editor.BaseType); // Reuse pyramid sides helper

                // Tính độ sâu trung bình của top và bottom
                double avgTopZ = 0, avgBottomZ = 0;
                for (int i = 0; i < sides; i++)
                {
                    avgTopZ += rotatedVertices[i][2];
                    avgBottomZ += rotatedVertices[i + sides][2];
                }
                avgTopZ /= sides;
                avgBottomZ /= sides;

                // Draw top polygon edges
                for (int i = 0; i < sides; i++)
                {
                    int current = i;
                    int next = (i + 1) % sides;
                    bool isHidden = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[current].X,
                        Y1 = projected2D[current].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = new SolidColorBrush(editor.EdgeColor),
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    prismContainer.Children.Add(line);
                }

                // Draw bottom polygon edges
                for (int i = 0; i < sides; i++)
                {
                    int current = i + sides;
                    int next = (i + 1) % sides + sides;
                    bool isHidden = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[current].X,
                        Y1 = projected2D[current].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = new SolidColorBrush(editor.EdgeColor),
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    prismContainer.Children.Add(line);
                }

                // Draw vertical edges connecting top and bottom
                for (int i = 0; i < sides; i++)
                {
                    int topVertex = i;
                    int bottomVertex = i + sides;
                    bool isHidden = (rotatedVertices[topVertex][2] + rotatedVertices[bottomVertex][2]) / 2 >= 0;
                    
                    Line line = new Line
                    {
                        X1 = projected2D[topVertex].X,
                        Y1 = projected2D[topVertex].Y,
                        X2 = projected2D[bottomVertex].X,
                        Y2 = projected2D[bottomVertex].Y,
                        Stroke = new SolidColorBrush(editor.EdgeColor),
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    prismContainer.Children.Add(line);
                }
            }

            // Vẽ nhãn đỉnh (đáy dưới A,B,C + đáy trên A',B',C')
            if (editor.ShowVertexLabels)
            {
                int labelSides = GetPyramidSides(editor.BaseType);
                string[] bottomNames = labelSides switch
                {
                    3 => new[] { "A", "B", "C" },
                    4 => new[] { "A", "B", "C", "D" },
                    5 => new[] { "A", "B", "C", "D", "E" },
                    6 => new[] { "A", "B", "C", "D", "E", "F" },
                    _ => new[] { "A", "B", "C" }
                };

                // Top face labels (vertices 0..n-1)
                for (int i = 0; i < labelSides; i++)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = bottomNames[i],
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, projected2D[i].X + 8);
                    Canvas.SetTop(label, projected2D[i].Y - 16);
                    prismContainer.Children.Add(label);
                }
                // Bottom face labels (vertices n..2n-1)
                for (int i = 0; i < labelSides; i++)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = bottomNames[i] + "'",
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, projected2D[labelSides + i].X + 8);
                    Canvas.SetTop(label, projected2D[labelSides + i].Y - 16);
                    prismContainer.Children.Add(label);
                }
            }

            // Trim canvas to remove white space
            TrimCanvasToPrismRectangle(prismContainer, editor.BaseSize, editor.Height, editor.BaseType, 10);

            // Position prism in center of canvas
            Canvas.SetLeft(prismContainer, (MainInteractiveBoard.ActualWidth - prismContainer.Width) / 2);
            Canvas.SetTop(prismContainer, (MainInteractiveBoard.ActualHeight - prismContainer.Height) / 2);

            // Enable drag & drop
            Enable3DShapeDragging(prismContainer);

            // Add to main canvas
            MainInteractiveBoard.Children.Add(prismContainer);
            
            // Add to undo stack
            RecordAddAction(prismContainer, "Draw");
        }

        // Helper methods for prism generation
        private List<double[]> GeneratePrismVertices(double size, double height, string baseType)
        {
            List<double[]> vertices = new List<double[]>();
            int sides = GetPyramidSides(baseType); // Reuse pyramid sides helper
            double angleStep = 2 * Math.PI / sides;
            double radius = GetPyramidCircumradius(size, sides); // Reuse circumradius helper

            // Top polygon vertices
            for (int i = 0; i < sides; i++)
            {
                double angle = i * angleStep - Math.PI / 2;
                if (baseType == "Square")
                {
                    angle += Math.PI / 4;
                }
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                vertices.Add(new double[] { x, -height / 2, z });
            }

            // Bottom polygon vertices
            for (int i = 0; i < sides; i++)
            {
                double angle = i * angleStep - Math.PI / 2;
                if (baseType == "Square")
                {
                    angle += Math.PI / 4;
                }
                double x = radius * Math.Cos(angle);
                double z = radius * Math.Sin(angle);
                vertices.Add(new double[] { x, height / 2, z });
            }

            return vertices;
        }

        private List<int[]> GeneratePrismFaces(string baseType)
        {
            List<int[]> faces = new List<int[]>();
            int sides = GetPyramidSides(baseType); // Reuse pyramid sides helper

            // Top face (polygon)
            var topFace = new int[sides];
            for (int i = 0; i < sides; i++)
            {
                topFace[i] = i;
            }
            faces.Add(topFace);

            // Bottom face (polygon - reversed for correct normal)
            var bottomFace = new int[sides];
            for (int i = 0; i < sides; i++)
            {
                bottomFace[sides - 1 - i] = sides + i;
            }
            faces.Add(bottomFace);

            // Lateral faces (quads)
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                int[] lateralFace = new int[] 
                { 
                    i,           // Top current
                    next,        // Top next
                    next + sides,// Bottom next
                    i + sides    // Bottom current
                };
                faces.Add(lateralFace);
            }

            return faces;
        }

        /// <summary>
        /// Apply 3D Prism to canvas WITH educational stats panel (for "Display Stats" button)
        /// Creates TWO INDEPENDENT objects: 3D Shape + Stats Panel (can be moved and deleted separately)
        /// </summary>
        private void Apply3DPrismWithStatsToCanvas(Form2_6_3DPrismEditor editor)
        {
            // ========== 1. CREATE 3D PRISM SHAPE (INDEPENDENT OBJECT) ==========
            Canvas prismContainer = new Canvas
            {
                Width = 650,
                Height = 650,
                Background = Brushes.Transparent
            };

            double centerX = 325;
            double centerY = 325;

            // Generate prism vertices
            List<double[]> vertices = GeneratePrismVertices(
                editor.BaseSize,
                editor.Height,
                editor.BaseType);

            // Apply rotations
            List<double[]> rotatedVertices = new List<double[]>();
            foreach (var vertex in vertices)
            {
                double[] rotated = RotateXYZ(vertex, editor.RotationX, editor.RotationY, editor.RotationZ);
                rotatedVertices.Add(rotated);
            }

            // Project to 2D
            List<Point> projected2D = new List<Point>();
            foreach (var vertex in rotatedVertices)
            {
                projected2D.Add(Project3DTo2D(vertex, centerX, centerY));
            }

            // Generate faces
            List<int[]> faces = GeneratePrismFaces(editor.BaseType);

            // Sort faces by depth for proper rendering
            List<(int faceIndex, double avgZ)> facesWithDepth = new List<(int, double)>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                double avgZ = 0;
                foreach (int vertexIndex in face)
                {
                    avgZ += rotatedVertices[vertexIndex][2];
                }
                avgZ /= face.Length;
                facesWithDepth.Add((i, avgZ));
            }
            facesWithDepth.Sort((a, b) => b.avgZ.CompareTo(a.avgZ));

            // Draw faces (if Solid/Both mode)
            if (editor.DrawingMode == "Solid" || editor.DrawingMode == "Both")
            {
                foreach (var (faceIndex, avgZ) in facesWithDepth)
                {
                    int[] face = faces[faceIndex];

                    Polygon polygon = new Polygon();
                    
                    foreach (int vertexIndex in face)
                    {
                        polygon.Points.Add(projected2D[vertexIndex]);
                    }

                    if (editor.FaceColor != Colors.Transparent)
                    {
                        polygon.Fill = new SolidColorBrush(Color.FromArgb(
                            (byte)(editor.Opacity * 255),
                            editor.FaceColor.R, editor.FaceColor.G, editor.FaceColor.B));
                    }
                    else
                    {
                        polygon.Fill = Brushes.Transparent;
                    }

                    polygon.Stroke = Brushes.Transparent;
                    polygon.StrokeThickness = 0;

                    prismContainer.Children.Add(polygon);
                }
            }

            // Draw edges (if Wireframe/Both mode)
            if (editor.DrawingMode == "Wireframe" || editor.DrawingMode == "Both")
            {
                int sides = GetPyramidSides(editor.BaseType);

                // Draw top polygon edges
                for (int i = 0; i < sides; i++)
                {
                    int current = i;
                    int next = (i + 1) % sides;
                    bool isHidden = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[current].X,
                        Y1 = projected2D[current].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = new SolidColorBrush(editor.EdgeColor),
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    prismContainer.Children.Add(line);
                }

                // Draw bottom polygon edges
                for (int i = 0; i < sides; i++)
                {
                    int current = i + sides;
                    int next = (i + 1) % sides + sides;
                    bool isHidden = (rotatedVertices[current][2] + rotatedVertices[next][2]) / 2 >= 0;
                    Line line = new Line
                    {
                        X1 = projected2D[current].X,
                        Y1 = projected2D[current].Y,
                        X2 = projected2D[next].X,
                        Y2 = projected2D[next].Y,
                        Stroke = new SolidColorBrush(editor.EdgeColor),
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    prismContainer.Children.Add(line);
                }

                // Draw vertical edges
                for (int i = 0; i < sides; i++)
                {
                    int topVertex = i;
                    int bottomVertex = i + sides;
                    bool isHidden = (rotatedVertices[topVertex][2] + rotatedVertices[bottomVertex][2]) / 2 >= 0;
                    
                    Line line = new Line
                    {
                        X1 = projected2D[topVertex].X,
                        Y1 = projected2D[topVertex].Y,
                        X2 = projected2D[bottomVertex].X,
                        Y2 = projected2D[bottomVertex].Y,
                        Stroke = new SolidColorBrush(editor.EdgeColor),
                        StrokeThickness = editor.EdgeThickness
                    };
                    if (isHidden && editor.ShowHiddenEdges)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 4, 4 };
                    }
                    prismContainer.Children.Add(line);
                }
            }

            // Vẽ nhãn đỉnh (đáy dưới A,B,C + đáy trên A',B',C')
            if (editor.ShowVertexLabels)
            {
                int labelSides = GetPyramidSides(editor.BaseType);
                string[] bottomNames = labelSides switch
                {
                    3 => new[] { "A", "B", "C" },
                    4 => new[] { "A", "B", "C", "D" },
                    5 => new[] { "A", "B", "C", "D", "E" },
                    6 => new[] { "A", "B", "C", "D", "E", "F" },
                    _ => new[] { "A", "B", "C" }
                };

                for (int i = 0; i < labelSides; i++)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = bottomNames[i],
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, projected2D[i].X + 8);
                    Canvas.SetTop(label, projected2D[i].Y - 16);
                    prismContainer.Children.Add(label);
                }
                for (int i = 0; i < labelSides; i++)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = bottomNames[i] + "'",
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    };
                    Canvas.SetLeft(label, projected2D[labelSides + i].X + 8);
                    Canvas.SetTop(label, projected2D[labelSides + i].Y - 16);
                    prismContainer.Children.Add(label);
                }
            }

            // Trim canvas to remove white space
            TrimCanvasToPrismRectangle(prismContainer, editor.BaseSize, editor.Height, editor.BaseType, 10);

            // Position prism shape in center-left area of canvas
            Canvas.SetLeft(prismContainer, (MainInteractiveBoard.ActualWidth / 2) - prismContainer.Width - 20);
            Canvas.SetTop(prismContainer, (MainInteractiveBoard.ActualHeight - prismContainer.Height) / 2);

            // Enable drag & drop for prism shape (independent)
            Enable3DShapeDragging(prismContainer);

            // Add prism to main canvas
            MainInteractiveBoard.Children.Add(prismContainer);
            
            // Add to undo stack
            RecordAddAction(prismContainer, "Create 3D object");

            // ========== 2. CREATE EDUCATIONAL STATS PANEL (INDEPENDENT OBJECT) ==========
            string baseTypeName = editor.BaseType switch
            {
                "Triangle" => "Tam Giác",
                "Square" => "Tứ Giác (Vuông)",
                "Pentagon" => "Ngũ Giác",
                "Hexagon" => "Lục Giác",
                _ => editor.BaseType
            };

            int numSides = editor.BaseType switch
            {
                "Triangle" => 3,
                "Square" => 4,
                "Pentagon" => 5,
                "Hexagon" => 6,
                _ => 4
            };

            int numVertices = numSides * 2;
            int numEdges = numSides * 3;
            int numFaces = numSides + 2;

            // Calculate ACTUAL dimensions on canvas (after 3D projection)
            // Convert pixels to mm (assuming 96 DPI standard Windows display)
            const double DPI = 96.0;
            const double MM_PER_INCH = 25.4;
            double pixelsToMm = MM_PER_INCH / DPI; // 1 pixel = 0.264583 mm

            // Calculate base edge length (distance between two adjacent vertices on top base)
            double actualBaseEdgeLengthPx = 0;
            if (projected2D.Count >= 2)
            {
                Point p1 = projected2D[0];
                Point p2 = projected2D[1];
                actualBaseEdgeLengthPx = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            }
            double actualBaseEdgeMm = actualBaseEdgeLengthPx * pixelsToMm;

            // Calculate height (distance between corresponding vertices on top and bottom)
            double actualHeightPx = 0;
            if (projected2D.Count >= numSides + 1)
            {
                Point topVertex = projected2D[0];
                Point bottomVertex = projected2D[numSides];
                actualHeightPx = Math.Sqrt(Math.Pow(bottomVertex.X - topVertex.X, 2) + Math.Pow(bottomVertex.Y - topVertex.Y, 2));
            }
            double actualHeightMm = actualHeightPx * pixelsToMm;

            // Calculate areas and volume based on ACTUAL mm dimensions
            double actualBaseAreaMm2 = CalculatePrismBaseArea(editor.BaseType, actualBaseEdgeMm);
            double actualLateralAreaMm2 = numSides * actualBaseEdgeMm * actualHeightMm;
            double actualTotalAreaMm2 = 2 * actualBaseAreaMm2 + actualLateralAreaMm2;
            double actualVolumeMm3 = actualBaseAreaMm2 * actualHeightMm;

            // Keep original calculations for reference (based on setup values)
            double baseArea = CalculatePrismBaseArea(editor.BaseType, editor.BaseSize);
            double lateralArea = numSides * editor.BaseSize * editor.Height;
            double totalArea = 2 * baseArea + lateralArea;
            double volume = baseArea * editor.Height;

            var statsPanel = new Border
            {
                Width = 380,
                Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(102, 126, 234)),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15)
            };

            var stackPanel = new StackPanel();

            // Title
            AddStatsTextBlock(stackPanel, $"📐 LĂNG TRỤ ĐÁY {baseTypeName.ToUpper()}", 16, true, "#667EEA");
            AddStatsSeparator(stackPanel);

            // Dimensions - Show ACTUAL measured size in mm
            AddStatsTextBlock(stackPanel, "📏 KÍCH THƯỚC THỰC TẾ (trên màn hình):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Chiều dài cạnh đáy: {actualBaseEdgeMm:N1} mm", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Chiều cao (h): {actualHeightMm:N1} mm", 11, false, "#555");
            AddStatsSeparator(stackPanel);

            // Formulas - Based on ACTUAL mm dimensions
            AddStatsTextBlock(stackPanel, "📐 CÔNG THỨC TÍNH (theo kích thước thực):", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Diện tích đáy (S_đáy):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     {GetPrismBaseAreaFormula(editor.BaseType)}", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualBaseAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích xung quanh (S_xq):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S_xq = {numSides} × cạnh × h", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualLateralAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Diện tích toàn phần (S_tp):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     S_tp = 2 × S_đáy + S_xq", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualTotalAreaMm2:N2} mm²", 11, false, "#E91E63");
            
            AddStatsTextBlock(stackPanel, $"   • Thể tích (V):", 11, true, "#667EEA");
            AddStatsTextBlock(stackPanel, $"     V = S_đáy × h", 10, false, "#666");
            AddStatsTextBlock(stackPanel, $"     = {actualVolumeMm3:N2} mm³", 11, false, "#E91E63");
            AddStatsSeparator(stackPanel);

            // Topology
            AddStatsTextBlock(stackPanel, "🔢 ĐẶC ĐIỂM HÌNH HỌC:", 13, true, "#2C3E50");
            AddStatsTextBlock(stackPanel, $"   • Số đỉnh: {numVertices}", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Số cạnh: {numEdges}", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Số mặt: {numFaces} ({numSides} mặt bên + 2 đáy)", 11, false, "#555");
            AddStatsTextBlock(stackPanel, $"   • Định lý Euler: Đ - C + M = 2", 11, false, "#9C27B0");
            AddStatsTextBlock(stackPanel, $"     {numVertices} - {numEdges} + {numFaces} = 2 âœ“", 10, false, "#4CAF50");

            statsPanel.Child = stackPanel;

            // Wrap stats panel in a Canvas container for drag & drop
            Canvas statsPanelContainer = new Canvas
            {
                Width = statsPanel.Width + 10,
                Height = stackPanel.ActualHeight > 0 ? stackPanel.ActualHeight + 30 : 600,
                Background = Brushes.Transparent
            };
            
            Canvas.SetLeft(statsPanel, 0);
            Canvas.SetTop(statsPanel, 0);
            statsPanelContainer.Children.Add(statsPanel);

            // Position stats panel to the right of prism shape
            Canvas.SetLeft(statsPanelContainer, (MainInteractiveBoard.ActualWidth - 1100) / 2 + 680);
            Canvas.SetTop(statsPanelContainer, (MainInteractiveBoard.ActualHeight - 650) / 2 + 10);

            // Enable drag & drop for stats panel (independent)
            Enable3DShapeDragging(statsPanelContainer);

            // Add stats panel to main canvas
            MainInteractiveBoard.Children.Add(statsPanelContainer);
            
            // Add to undo stack
            RecordAddAction(statsPanelContainer, "Draw");
        }

        // Helper methods for educational stats panel
        private void AddStatsTextBlock(StackPanel parent, string text, double fontSize, bool bold, string colorHex)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Margin = new Thickness(0, 3, 0, 3),
                TextWrapping = TextWrapping.Wrap
            };
            parent.Children.Add(textBlock);
        }

        private void AddStatsSeparator(StackPanel parent)
        {
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Margin = new Thickness(0, 8, 0, 8)
            };
            parent.Children.Add(separator);
        }

        private double CalculatePrismBaseArea(string baseType, double size)
        {
            return baseType switch
            {
                "Triangle" => (Math.Sqrt(3) / 4) * size * size,
                "Square" => size * size,
                "Pentagon" => (1.0 / 4.0) * Math.Sqrt(5 * (5 + 2 * Math.Sqrt(5))) * size * size,
                "Hexagon" => (3 * Math.Sqrt(3) / 2) * size * size,
                _ => size * size
            };
        }

        private string GetPrismBaseAreaFormula(string baseType)
        {
            return baseType switch
            {
                "Triangle" => "S = (√3/4) × cạnh²",
                "Square" => "S = cạnh²",
                "Pentagon" => "S = (1/4)√[5(5+2√5)] × cạnh²",
                "Hexagon" => "S = (3√3/2) × cạnh²",
                _ => "S = cạnh²"
            };
        }

        // Helper methods for 3D cube rendering
        private double[,] GetCubeVertices3D(double size)
        {
            double s = size / 2;
            return new double[,]
            {
                { -s, -s, -s }, { s, -s, -s }, { s, s, -s }, { -s, s, -s },
                { -s, -s, s }, { s, -s, s }, { s, s, s }, { -s, s, s }
            };
        }

        private double[] RotateXYZ(double[] point, double angleX, double angleY, double angleZ)
        {
            // Rotate X
            double radX = angleX * Math.PI / 180;
            double cosX = Math.Cos(radX), sinX = Math.Sin(radX);
            double y1 = point[1] * cosX - point[2] * sinX;
            double z1 = point[1] * sinX + point[2] * cosX;
            
            // Rotate Y
            double radY = angleY * Math.PI / 180;
            double cosY = Math.Cos(radY), sinY = Math.Sin(radY);
            double x2 = point[0] * cosY + z1 * sinY;
            double z2 = -point[0] * sinY + z1 * cosY;
            
            // Rotate Z
            double radZ = angleZ * Math.PI / 180;
            double cosZ = Math.Cos(radZ), sinZ = Math.Sin(radZ);
            double x3 = x2 * cosZ - y1 * sinZ;
            double y3 = x2 * sinZ + y1 * cosZ;
            
            return new double[] { x3, y3, z2 };
        }

        private Point Project3DTo2D(double[] point3D, double centerX, double centerY)
        {
            double distance = 600;
            double z = point3D[2] + distance;
            if (z < 1) z = 1;
            double scale = distance / z;
            return new Point(point3D[0] * scale + centerX, point3D[1] * scale + centerY);
        }

        private double[] CalculateFaceNormal(double[] v1, double[] v2, double[] v3)
        {
            double[] edge1 = { v2[0] - v1[0], v2[1] - v1[1], v2[2] - v1[2] };
            double[] edge2 = { v3[0] - v1[0], v3[1] - v1[1], v3[2] - v1[2] };
            return new double[]
            {
                edge1[1] * edge2[2] - edge1[2] * edge2[1],
                edge1[2] * edge2[0] - edge1[0] * edge2[2],
                edge1[0] * edge2[1] - edge1[1] * edge2[0]
            };
        }

        #endregion
    }
}
