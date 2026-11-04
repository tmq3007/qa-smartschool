using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Services.Tools
{
    using Canvas = System.Windows.Controls.Canvas;
    using CanvasControl = System.Windows.Controls.Canvas;
    /// <summary>
    /// Lưu trữ siêu dữ liệu không gian 3D của đối tượng hình học trên bảng vẽ.
    /// Hỗ trợ xoay không gian 3D (Yaw / Pitch) mượt mà chuẩn NoteBoard.
    /// </summary>
    public class Shape3DData
    {
        public string ShapeType { get; set; } = "Cube";
        public double Yaw { get; set; } = -28.0;   // Góc xoay ngang (độ)
        public double Pitch { get; set; } = 22.0;  // Góc xoay dọc (độ)
        public Color StrokeColor { get; set; } = Color.FromRgb(0x1E, 0x51, 0x80);
        public double StrokeThickness { get; set; } = 2.0;
        public bool IsRotating3D { get; set; } = false;

        // Lưu trạng thái trước khi xoay để ghi nhận Undo/Redo chính xác
        public double InitialYaw { get; set; } = -28.0;
        public double InitialPitch { get; set; } = 22.0;

        public Shape3DData Clone()
        {
            return new Shape3DData
            {
                ShapeType = this.ShapeType,
                Yaw = this.Yaw,
                Pitch = this.Pitch,
                StrokeColor = this.StrokeColor,
                StrokeThickness = this.StrokeThickness,
                IsRotating3D = this.IsRotating3D,
                InitialYaw = this.InitialYaw,
                InitialPitch = this.InitialPitch
            };
        }
    }

    /// <summary>
    /// Dịch vụ tính toán hình học không gian, xoay 3D (Yaw & Pitch) và phân loại
    /// cạnh nhìn thấy (nét liền) / cạnh khuất (nét đứt SGK) theo chuẩn NoteBoard.
    /// </summary>
    public static class Spatial3DService
    {
        public static bool Is3DShapeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string n = name.Trim().ToLowerInvariant();
            return n is "cube" or "sphere" or "pyramid" or "tetrahedron" or "cylinder" or "cone" or "prism"
                or "mcube" or "mpyramid" or "mtetra" or "mprism" or "mprism4" or "mcylinder" or "mcone" or "mfrustum" or "msphere";
        }

        public static string NormalizeShapeType(string? shapeName)
        {
            if (string.IsNullOrWhiteSpace(shapeName)) return "Cube";
            string n = shapeName.Trim().ToLowerInvariant();
            return n switch
            {
                "mcube" or "cube" => "Cube",
                "mpyramid" or "pyramid" => "Pyramid",
                "mtetra" or "tetrahedron" => "Tetrahedron",
                "mprism" or "mprism4" or "prism" => "Prism",
                "mcylinder" or "cylinder" => "Cylinder",
                "mcone" or "mfrustum" or "cone" => "Cone",
                "msphere" or "sphere" => "Sphere",
                _ => "Cube"
            };
        }

        public static bool Is3DShape(FrameworkElement? element)
        {
            if (element == null) return false;
            if (element.Tag is Shape3DData) return true;
            if (element.Tag?.ToString() == "3DShape") return true;
            if (element.Tag is string tagStr && Is3DShapeName(tagStr)) return true;
            return false;
        }

        public static Shape3DData? GetShape3DData(FrameworkElement? element)
        {
            if (element == null) return null;
            if (element.Tag is Shape3DData data) return data;
            if (element.Tag?.ToString() == "3DShape")
            {
                var newData = new Shape3DData();
                element.Tag = newData;
                return newData;
            }
            if (element.Tag is string tagStr && Is3DShapeName(tagStr))
            {
                string norm = NormalizeShapeType(tagStr);
                var newData = new Shape3DData
                {
                    ShapeType = norm,
                    Yaw = norm == "Pyramid" ? 45 : (norm == "Prism" ? -35 : -28),
                    Pitch = 22
                };
                element.Tag = newData;
                return newData;
            }
            return null;
        }

        public static void ApplyRotationDelta(Shape3DData data, double deltaX, double deltaY)
        {
            if (data == null) return;
            // Áp dụng công thức NoteBoard: kéo ngang đổi Yaw, kéo dọc đổi Pitch
            data.Yaw = ((data.Yaw - deltaX * 0.45 + 540) % 360) - 180;
            data.Pitch = Math.Max(-80, Math.Min(80, data.Pitch + deltaY * 0.35));
        }

        private static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached("Is3DInteractionAttached", typeof(bool), typeof(Spatial3DService), new PropertyMetadata(false));

        /// <summary>
        /// Gắn bộ lắng nghe sự kiện xoay 3D trực tiếp lên Canvas khối hình
        /// Đảm bảo thao tác vuốt cảm ứng đa điểm hoặc kéo chuột trên thân hình luôn xoay mượt mà 60fps
        /// </summary>
        public static void AttachInteraction(Canvas container)
        {
            if (container == null) return;

            // Đảm bảo không gắn trùng lặp
            if (container.GetValue(IsAttachedProperty) is true) return;
            container.SetValue(IsAttachedProperty, true);

            Point startMouse = new Point();
            bool isMouseRotating = false;

            container.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var data = GetShape3DData(container);
                if (data != null && data.IsRotating3D)
                {
                    isMouseRotating = true;
                    startMouse = e.GetPosition(container);
                    container.CaptureMouse();
                    e.Handled = true;
                }
            };

            container.PreviewMouseMove += (s, e) =>
            {
                if (isMouseRotating)
                {
                    var data = GetShape3DData(container);
                    if (data != null && data.IsRotating3D)
                    {
                        Point current = e.GetPosition(container);
                        double dx = current.X - startMouse.X;
                        double dy = current.Y - startMouse.Y;
                        if (Math.Abs(dx) > 0.5 || Math.Abs(dy) > 0.5)
                        {
                            ApplyRotationDelta(data, dx, dy);
                            Render3DShape(container, data);
                            startMouse = current;
                        }
                        e.Handled = true;
                    }
                }
            };

            container.PreviewMouseLeftButtonUp += (s, e) =>
            {
                if (isMouseRotating)
                {
                    isMouseRotating = false;
                    if (container.IsMouseCaptured) container.ReleaseMouseCapture();
                    e.Handled = true;
                }
            };

            // Touch events cho màn hình cảm ứng tương tác phòng học 86"
            Point startTouch = new Point();
            int activeTouchId = -1;

            container.PreviewTouchDown += (s, e) =>
            {
                var data = GetShape3DData(container);
                if (data != null && data.IsRotating3D && activeTouchId == -1)
                {
                    activeTouchId = e.TouchDevice.Id;
                    startTouch = e.GetTouchPoint(container).Position;
                    container.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                }
            };

            container.PreviewTouchMove += (s, e) =>
            {
                if (activeTouchId == e.TouchDevice.Id)
                {
                    var data = GetShape3DData(container);
                    if (data != null && data.IsRotating3D)
                    {
                        Point current = e.GetTouchPoint(container).Position;
                        double dx = current.X - startTouch.X;
                        double dy = current.Y - startTouch.Y;
                        if (Math.Abs(dx) > 0.5 || Math.Abs(dy) > 0.5)
                        {
                            ApplyRotationDelta(data, dx, dy);
                            Render3DShape(container, data);
                            startTouch = current;
                        }
                        e.Handled = true;
                    }
                }
            };

            container.PreviewTouchUp += (s, e) =>
            {
                if (activeTouchId == e.TouchDevice.Id)
                {
                    activeTouchId = -1;
                    if (container.AreAnyTouchesCaptured) container.ReleaseTouchCapture(e.TouchDevice);
                    e.Handled = true;
                }
            };
        }

        /// <summary>
        /// Tái tạo và hiển thị hình học 3D thời gian thực lên Canvas
        /// </summary>
        public static void Render3DShape(Canvas container, Shape3DData data)
        {
            if (container == null || data == null) return;
            AttachInteraction(container);

            double width = container.Width;
            double height = container.Height;
            if (double.IsNaN(width) || width <= 0) width = container.ActualWidth > 0 ? container.ActualWidth : 300;
            if (double.IsNaN(height) || height <= 0) height = container.ActualHeight > 0 ? container.ActualHeight : 300;

            GeometryGroup visibleGroup = new GeometryGroup();
            GeometryGroup hiddenGroup = new GeometryGroup();

            Build3DGeometry(data.ShapeType, data.Yaw, data.Pitch, width, height, visibleGroup, hiddenGroup);

            // Tìm hoặc tạo Path nét đứt (cạnh khuất) và Path nét liền (cạnh nhìn thấy)
            Path? hiddenPath = null;
            Path? visiblePath = null;

            foreach (var child in container.Children)
            {
                if (child is Path p)
                {
                    if (p.StrokeDashArray != null && p.StrokeDashArray.Count > 0)
                    {
                        hiddenPath = p;
                    }
                    else
                    {
                        visiblePath = p;
                    }
                }
            }

            Brush strokeBrush = new SolidColorBrush(data.StrokeColor);

            if (hiddenPath == null)
            {
                hiddenPath = new Path
                {
                    Stroke = strokeBrush,
                    StrokeThickness = data.StrokeThickness,
                    StrokeDashArray = new DoubleCollection { 4, 3 }
                };
                container.Children.Add(hiddenPath);
            }
            else
            {
                hiddenPath.Stroke = strokeBrush;
                hiddenPath.StrokeThickness = data.StrokeThickness;
            }

            if (visiblePath == null)
            {
                visiblePath = new Path
                {
                    Stroke = strokeBrush,
                    StrokeThickness = data.StrokeThickness
                };
                container.Children.Add(visiblePath);
            }
            else
            {
                visiblePath.Stroke = strokeBrush;
                visiblePath.StrokeThickness = data.StrokeThickness;
            }

            hiddenPath.Data = hiddenGroup;
            visiblePath.Data = visibleGroup;
        }

        #region 3D Math & Projections

        private static void Build3DGeometry(string shapeType, double yawDeg, double pitchDeg, double w, double h,
            GeometryGroup visible, GeometryGroup hidden)
        {
            switch (shapeType?.ToLowerInvariant())
            {
                case "cube":
                    BuildPolyhedronGeometry(GetCubeModel(), yawDeg, pitchDeg, w, h, visible, hidden);
                    break;
                case "pyramid":
                    BuildPolyhedronGeometry(GetPyramidModel(), yawDeg, pitchDeg, w, h, visible, hidden);
                    break;
                case "tetrahedron":
                    BuildPolyhedronGeometry(GetTetrahedronModel(), yawDeg, pitchDeg, w, h, visible, hidden);
                    break;
                case "prism":
                    BuildPolyhedronGeometry(GetPrismModel(), yawDeg, pitchDeg, w, h, visible, hidden);
                    break;
                case "cylinder":
                    BuildCylinderGeometry(yawDeg, pitchDeg, w, h, visible, hidden);
                    break;
                case "cone":
                    BuildConeGeometry(yawDeg, pitchDeg, w, h, visible, hidden);
                    break;
                case "sphere":
                    BuildSphereGeometry(yawDeg, pitchDeg, w, h, visible, hidden);
                    break;
                default:
                    BuildPolyhedronGeometry(GetCubeModel(), yawDeg, pitchDeg, w, h, visible, hidden);
                    break;
            }
        }

        private struct PolyhedronModel
        {
            public double[][] Vertices;
            public int[][] Faces;
        }

        private static PolyhedronModel GetCubeModel()
        {
            double[][] v = new double[][]
            {
                new double[] { -1, -1, -1 }, // 0
                new double[] {  1, -1, -1 }, // 1
                new double[] {  1, -1,  1 }, // 2
                new double[] { -1, -1,  1 }, // 3
                new double[] { -1,  1, -1 }, // 4
                new double[] {  1,  1, -1 }, // 5
                new double[] {  1,  1,  1 }, // 6
                new double[] { -1,  1,  1 }  // 7
            };
            int[][] f = new int[][]
            {
                new int[] { 0, 3, 2, 1 }, // Bottom (y = -1)
                new int[] { 4, 5, 6, 7 }, // Top    (y =  1)
                new int[] { 0, 1, 5, 4 }, // Front  (z = -1)
                new int[] { 1, 2, 6, 5 }, // Right  (x =  1)
                new int[] { 2, 3, 7, 6 }, // Back   (z =  1)
                new int[] { 3, 0, 4, 7 }  // Left   (x = -1)
            };
            return new PolyhedronModel { Vertices = v, Faces = f };
        }

        private static PolyhedronModel GetPyramidModel()
        {
            double[][] v = new double[][]
            {
                new double[] { -1, -1, -1 }, // 0
                new double[] {  1, -1, -1 }, // 1
                new double[] {  1, -1,  1 }, // 2
                new double[] { -1, -1,  1 }, // 3
                new double[] {  0,  1.2, 0 } // 4: Apex S
            };
            int[][] f = new int[][]
            {
                new int[] { 0, 3, 2, 1 }, // Bottom
                new int[] { 0, 1, 4 },    // Face SAB
                new int[] { 1, 2, 4 },    // Face SBC
                new int[] { 2, 3, 4 },    // Face SCD
                new int[] { 3, 0, 4 }     // Face SDA
            };
            return new PolyhedronModel { Vertices = v, Faces = f };
        }

        private static PolyhedronModel GetTetrahedronModel()
        {
            double r = 1.2;
            double[][] v = new double[][]
            {
                new double[] {  r * Math.Cos(-Math.PI/2), -0.8, r * Math.Sin(-Math.PI/2) },
                new double[] {  r * Math.Cos(-Math.PI/2 + 2*Math.PI/3), -0.8, r * Math.Sin(-Math.PI/2 + 2*Math.PI/3) },
                new double[] {  r * Math.Cos(-Math.PI/2 + 4*Math.PI/3), -0.8, r * Math.Sin(-Math.PI/2 + 4*Math.PI/3) },
                new double[] {  0, 1.2, 0 } // Apex S
            };
            int[][] f = new int[][]
            {
                new int[] { 0, 2, 1 },
                new int[] { 0, 1, 3 },
                new int[] { 1, 2, 3 },
                new int[] { 2, 0, 3 }
            };
            return new PolyhedronModel { Vertices = v, Faces = f };
        }

        private static PolyhedronModel GetPrismModel()
        {
            double r = 1.1;
            double h = 1.1;
            double[][] v = new double[6][];
            for (int i = 0; i < 3; i++)
            {
                double angle = -Math.PI / 2 + i * 2 * Math.PI / 3;
                v[i] = new double[] { r * Math.Cos(angle), -h, r * Math.Sin(angle) };
                v[i + 3] = new double[] { r * Math.Cos(angle), h, r * Math.Sin(angle) };
            }
            int[][] f = new int[][]
            {
                new int[] { 0, 2, 1 },       // Bottom
                new int[] { 3, 4, 5 },       // Top
                new int[] { 0, 1, 4, 3 },    // Side 1
                new int[] { 1, 2, 5, 4 },    // Side 2
                new int[] { 2, 0, 3, 5 }     // Side 3
            };
            return new PolyhedronModel { Vertices = v, Faces = f };
        }

        private static void BuildPolyhedronGeometry(PolyhedronModel model, double yawDeg, double pitchDeg,
            double w, double h, GeometryGroup visible, GeometryGroup hidden)
        {
            double yawRad = yawDeg * Math.PI / 180.0;
            double pitchRad = pitchDeg * Math.PI / 180.0;

            double cy = Math.Cos(yawRad), sy = Math.Sin(yawRad);
            double cp = Math.Cos(pitchRad), sp = Math.Sin(pitchRad);

            // Vector góc nhìn (Camera View)
            double[] view = new double[] { sy * cp, sp, cy * cp };
            double[] right = new double[] { cy, 0, -sy };
            double[] up = new double[] { -sy * sp, cp, -cy * sp };

            double scale = Math.Min(w, h) * 0.38;
            double centerX = w / 2.0;
            double centerY = h / 2.0;

            // Chiếu các đỉnh ra tọa độ màn hình 2D
            Point[] p2d = new Point[model.Vertices.Length];
            for (int i = 0; i < model.Vertices.Length; i++)
            {
                double[] pt = model.Vertices[i];
                double camX = pt[0] * right[0] + pt[1] * right[1] + pt[2] * right[2];
                double camY = pt[0] * up[0] + pt[1] * up[1] + pt[2] * up[2];
                p2d[i] = new Point(centerX + camX * scale, centerY - camY * scale);
            }

            // Tính pháp tuyến và hướng nhìn của từng mặt
            bool[] isFaceFront = new bool[model.Faces.Length];
            for (int fi = 0; fi < model.Faces.Length; fi++)
            {
                int[] face = model.Faces[fi];
                double[] p0 = model.Vertices[face[0]];
                double[] p1 = model.Vertices[face[1]];
                double[] p2 = model.Vertices[face[2]];

                // Vector v1 = p1 - p0, v2 = p2 - p0
                double v1x = p1[0] - p0[0], v1y = p1[1] - p0[1], v1z = p1[2] - p0[2];
                double v2x = p2[0] - p0[0], v2y = p2[1] - p0[1], v2z = p2[2] - p0[2];

                // Tích có hướng: N = v1 x v2
                double nx = v1y * v2z - v1z * v2y;
                double ny = v1z * v2x - v1x * v2z;
                double nz = v1x * v2y - v1y * v2x;

                double dot = nx * view[0] + ny * view[1] + nz * view[2];
                isFaceFront[fi] = dot > 1e-5;
            }

            // Tập hợp tất cả các cạnh và danh sách các mặt liền kề
            var edgeToFaces = new Dictionary<string, (int u, int v, List<int> faces)>();
            for (int fi = 0; fi < model.Faces.Length; fi++)
            {
                int[] face = model.Faces[fi];
                int len = face.Length;
                for (int i = 0; i < len; i++)
                {
                    int u = face[i];
                    int v = face[(i + 1) % len];
                    int min = Math.Min(u, v);
                    int max = Math.Max(u, v);
                    string key = $"{min}_{max}";
                    if (!edgeToFaces.TryGetValue(key, out var val))
                    {
                        val = (min, max, new List<int>());
                        edgeToFaces[key] = val;
                    }
                    val.faces.Add(fi);
                }
            }

            // Phân loại cạnh: Nếu mọi mặt kề đều quay ra sau -> Cạnh khuất (nét đứt)
            // Nếu có ít nhất 1 mặt quay ra trước -> Cạnh nhìn thấy (nét liền)
            foreach (var kvp in edgeToFaces)
            {
                var edge = kvp.Value;
                bool isVisible = false;
                foreach (int fi in edge.faces)
                {
                    if (isFaceFront[fi])
                    {
                        isVisible = true;
                        break;
                    }
                }

                LineGeometry line = new LineGeometry(p2d[edge.u], p2d[edge.v]);
                if (isVisible)
                {
                    visible.Children.Add(line);
                }
                else
                {
                    hidden.Children.Add(line);
                }
            }
        }

        private static void BuildCylinderGeometry(double yawDeg, double pitchDeg, double w, double h,
            GeometryGroup visible, GeometryGroup hidden)
        {
            double pitchRad = pitchDeg * Math.PI / 180.0;
            double sp = Math.Sin(pitchRad);
            double cp = Math.Cos(pitchRad);

            double cx = w / 2.0;
            double cy = h / 2.0;
            double rx = Math.Min(w, h) * 0.35;
            double ry = Math.Max(12, rx * Math.Abs(sp) * 0.85);
            double halfH = Math.Min(w, h) * 0.30;

            double topCy = cy - halfH * cp;
            double botCy = cy + halfH * cp;

            // Đáy trên: Elip
            visible.Children.Add(new EllipseGeometry(new Point(cx, topCy), rx, ry));

            // Hai đường sinh biên trái phải
            visible.Children.Add(new LineGeometry(new Point(cx - rx, topCy), new Point(cx - rx, botCy)));
            visible.Children.Add(new LineGeometry(new Point(cx + rx, topCy), new Point(cx + rx, botCy)));

            // Đáy dưới: Cung trước (nhìn thấy, nét liền) và cung sau (khuất, nét đứt)
            // Nửa dưới (nhìn thấy)
            var visibleArc = new PathGeometry();
            var vFig = new PathFigure { StartPoint = new Point(cx - rx, botCy), IsFilled = false };
            vFig.Segments.Add(new ArcSegment(new Point(cx + rx, botCy), new Size(rx, ry), 0, false, SweepDirection.Counterclockwise, true));
            visibleArc.Figures.Add(vFig);
            visible.Children.Add(visibleArc);

            // Nửa trên của đáy dưới (khuất nét đứt nếu nhìn từ trên xuống)
            var hiddenArc = new PathGeometry();
            var hFig = new PathFigure { StartPoint = new Point(cx - rx, botCy), IsFilled = false };
            hFig.Segments.Add(new ArcSegment(new Point(cx + rx, botCy), new Size(rx, ry), 0, false, SweepDirection.Clockwise, true));
            hiddenArc.Figures.Add(hFig);
            hidden.Children.Add(hiddenArc);
        }

        private static void BuildConeGeometry(double yawDeg, double pitchDeg, double w, double h,
            GeometryGroup visible, GeometryGroup hidden)
        {
            double pitchRad = pitchDeg * Math.PI / 180.0;
            double sp = Math.Sin(pitchRad);
            double cp = Math.Cos(pitchRad);

            double cx = w / 2.0;
            double cy = h / 2.0;
            double rx = Math.Min(w, h) * 0.36;
            double ry = Math.Max(12, rx * Math.Abs(sp) * 0.85);
            double halfH = Math.Min(w, h) * 0.34;

            Point apex = new Point(cx, cy - halfH);
            double botCy = cy + halfH * 0.6;

            // Hai đường sinh từ đỉnh xuống đáy
            visible.Children.Add(new LineGeometry(apex, new Point(cx - rx, botCy)));
            visible.Children.Add(new LineGeometry(apex, new Point(cx + rx, botCy)));

            // Đáy: Nửa trước nhìn thấy
            var visibleArc = new PathGeometry();
            var vFig = new PathFigure { StartPoint = new Point(cx - rx, botCy), IsFilled = false };
            vFig.Segments.Add(new ArcSegment(new Point(cx + rx, botCy), new Size(rx, ry), 0, false, SweepDirection.Counterclockwise, true));
            visibleArc.Figures.Add(vFig);
            visible.Children.Add(visibleArc);

            // Đáy: Nửa sau khuất
            var hiddenArc = new PathGeometry();
            var hFig = new PathFigure { StartPoint = new Point(cx - rx, botCy), IsFilled = false };
            hFig.Segments.Add(new ArcSegment(new Point(cx + rx, botCy), new Size(rx, ry), 0, false, SweepDirection.Clockwise, true));
            hiddenArc.Figures.Add(hFig);
            hidden.Children.Add(hiddenArc);
        }

        private static void BuildSphereGeometry(double yawDeg, double pitchDeg, double w, double h,
            GeometryGroup visible, GeometryGroup hidden)
        {
            double pitchRad = pitchDeg * Math.PI / 180.0;
            double sp = Math.Sin(pitchRad);

            double cx = w / 2.0;
            double cy = h / 2.0;
            double r = Math.Min(w, h) * 0.38;
            double ry = Math.Max(8, r * Math.Abs(sp) * 0.75);

            // Vòng tròn đường bao ngoài (luôn nhìn thấy)
            visible.Children.Add(new EllipseGeometry(new Point(cx, cy), r, r));

            // Vòng xích đạo: Cung trước (nhìn thấy)
            var visibleArc = new PathGeometry();
            var vFig = new PathFigure { StartPoint = new Point(cx - r, cy), IsFilled = false };
            vFig.Segments.Add(new ArcSegment(new Point(cx + r, cy), new Size(r, ry), 0, false, SweepDirection.Counterclockwise, true));
            visibleArc.Figures.Add(vFig);
            visible.Children.Add(visibleArc);

            // Vòng xích đạo: Cung sau (khuất nét đứt)
            var hiddenArc = new PathGeometry();
            var hFig = new PathFigure { StartPoint = new Point(cx - r, cy), IsFilled = false };
            hFig.Segments.Add(new ArcSegment(new Point(cx + r, cy), new Size(r, ry), 0, false, SweepDirection.Clockwise, true));
            hiddenArc.Figures.Add(hFig);
            hidden.Children.Add(hiddenArc);
        }

        #endregion
    }
}
