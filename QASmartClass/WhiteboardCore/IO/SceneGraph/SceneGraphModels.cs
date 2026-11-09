using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QASmartClass.WhiteboardCore.IO.SceneGraph
{
    /// <summary>
    /// Toàn bộ kịch bản bài giảng gồm nhiều trang bảng
    /// </summary>
    public class LectureSceneGraph
    {
        public string Version { get; set; } = "2.0";
        public string Title { get; set; } = "Bài giảng QA SmartClass";
        public string Author { get; set; } = "Giáo viên";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastModifiedAt { get; set; } = DateTime.Now;
        public int ActiveBoardIndex { get; set; } = 0;
        public List<BoardSceneModel> Boards { get; set; } = new();
    }

    /// <summary>
    /// Mô hình dữ liệu cho một trang bảng (Board / Slide)
    /// </summary>
    public class BoardSceneModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double CanvasWidth { get; set; } = 1920;
        public double CanvasHeight { get; set; } = 1080;
        public string? BackgroundColorHex { get; set; } = "#3D6D64";
        public string? BackgroundImagePath { get; set; }
        public string? BackgroundPattern { get; set; } = "grid";
        public int LineSpacing { get; set; } = 40;
        public int LineOpacity { get; set; } = 10;
        public string? ThumbnailFile { get; set; }
        public List<SceneElementNode> Elements { get; set; } = new();
    }

    /// <summary>
    /// <summary>
    /// Định danh loại phần tử trong Scene Graph
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SceneElementType
    {
        Stroke,         // Nét vẽ tự do (Polyline / Path)
        Shape,          // Hình học 2D (Rectangle, Ellipse, Line, Triangle, Arrow, Polygon)
        Image,          // Hình ảnh tĩnh hoặc khung ảnh tương tác chèn vào bảng
        Text,           // Hộp văn bản đơn giản (TextBlock / TextBox)
        Table,          // Bảng biểu dữ liệu ma trận (Hàng x Cột)
        RichText,       // Hộp văn bản định dạng phong phú (RichTextBox / FlowDocument)
        WebEmbed,       // YouTube, PhET Simulation, Google Maps, WebBrowser
        Shape3D,        // Khối hình học 3D SGK (Cube, Sphere, Cylinder, Cone, Prism...)
        Chart,          // Biểu đồ (Cột, Đường, Tròn, Miền, Radar, Phân tán)
        Container,      // Nhóm đối tượng lồng nhau (Canvas con)
        GenericXaml     // Fallback cho các UIElement XAML khác
    }

    /// <summary>
    /// Node đại diện cho một đối tượng bất kỳ trên bảng vẽ
    /// </summary>
    public class SceneElementNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public SceneElementType ElementType { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int ZIndex { get; set; } = 0;
        public double Opacity { get; set; } = 1.0;
        public double RotationAngle { get; set; } = 0.0;
        public bool IsLocked { get; set; } = false;
        public string? Tag { get; set; }

        // --- Cấu hình riêng cho Stroke (Bút viết) ---
        public List<Point2D>? StrokePoints { get; set; }
        public string? StrokeColorHex { get; set; }
        public double StrokeThickness { get; set; } = 2.0;
        public string? BrushType { get; set; } = "Normal"; // Normal, Highlighter, Laser, Calligraphy...

        // --- Cấu hình riêng cho 2D Shape ---
        public string? ShapeKind { get; set; } // Rectangle, Ellipse, Line, Polygon, Triangle...
        public string? FillColorHex { get; set; }
        public string? StrokeDashArray { get; set; }
        public List<Point2D>? PolygonPoints { get; set; }
        public string? GeometryData { get; set; } // Path Data SVG string

        // --- Cấu hình riêng cho Image ---
        public string? AssetKey { get; set; } // Đường dẫn file ảnh trong ZIP (assets/board_0_img_1.png)
        public string? StretchMode { get; set; } = "Uniform";
        public string? BorderColorHex { get; set; }
        public double BorderThickness { get; set; } = 0;

        // --- Cấu hình riêng cho Text ---
        public string? TextContent { get; set; }
        public double FontSize { get; set; } = 16.0;
        public string? FontFamily { get; set; } = "Segoe UI";
        public string? FontWeight { get; set; } = "Normal";
        public string? ForegroundColorHex { get; set; } = "#FFFFFF";

        // --- Cấu hình riêng cho Bảng biểu (Table) ---
        public int TableRowCount { get; set; }
        public int TableColumnCount { get; set; }
        public List<double>? TableRowHeights { get; set; }
        public List<double>? TableColumnWidths { get; set; }
        public List<TableCellDTO>? TableCells { get; set; }
        public string? TableGridColorHex { get; set; } = "#2563EB";
        public double TableGridThickness { get; set; } = 1.5;

        // --- Cấu hình riêng cho RichText ---
        public string? RichTextDocumentXaml { get; set; }
        public string? RichTextPlainText { get; set; }

        // --- Cấu hình riêng cho Web Embed (YouTube, PhET, Maps) ---
        public string? EmbedKind { get; set; } // YouTube, PhET, GoogleMaps, LocalVideo
        public string? EmbedUrl { get; set; }
        public string? EmbedTitle { get; set; }
        public string? VideoId { get; set; }

        // --- Cấu hình riêng cho 3D Models / Shapes ---
        public string? Model3DPath { get; set; }
        public string? Model3DType { get; set; } // Cube, Cylinder, Cone, Sphere...
        public string? VisibleGeometrySvg { get; set; } // SVG Path string cho cạnh nhìn thấy (Nét liền)
        public string? HiddenGeometrySvg { get; set; }  // SVG Path string cho cạnh khuất (Nét đứt)
        public double RotationX { get; set; }
        public double RotationY { get; set; }
        public double RotationZ { get; set; }

        // --- Nét vẽ ghi chú đè trên đối tượng (Container-Owned Relative Inking) ---
        public List<AnnotationStrokeDTO>? AnnotationStrokes { get; set; }

        // --- Cấu hình riêng cho Nested Container (Ví dụ Chart Canvas) ---
        public List<SceneElementNode>? Children { get; set; }

        // --- Cấu hình riêng cho Biểu đồ tương tác (Interactive Chart Container) ---
        public string? ChartTitle { get; set; }
        public string? ChartType { get; set; }
        public string? ChartIcon { get; set; }
        public string? ChartConfigJson { get; set; }
        public string? ChartConfigTypeName { get; set; }

        // --- Fallback XAML Content ---
        public string? FallbackXaml { get; set; }
    }

    /// <summary>
    /// Mô hình dữ liệu ô bảng (Table Cell)
    /// </summary>
    public class TableCellDTO
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 14.0;
        public string? FontWeight { get; set; } = "Normal";
        public string? TextColorHex { get; set; } = "#000000";
        public string? BackgroundColorHex { get; set; }
        public string? TextAlignment { get; set; } = "Left";
    }

    /// <summary>
    /// Nét vẽ ghi chú cục bộ đè trên thân ảnh hoặc biểu đồ (tính theo tỉ lệ tương đối 0.0 - 1.0)
    /// </summary>
    public class AnnotationStrokeDTO
    {
        public string? StrokeColorHex { get; set; } = "#FFFF00";
        public double StrokeThickness { get; set; } = 2.0;
        public List<Point2D> RelativePoints { get; set; } = new();
    }

    /// <summary>
    /// Tọa độ 2D độc lập với System.Windows.Point để dễ serialize và cross-platform
    /// </summary>
    public struct Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
