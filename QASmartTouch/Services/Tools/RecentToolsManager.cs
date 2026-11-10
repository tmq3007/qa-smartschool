using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace QASmartTouch.Services.Tools
{
    /// <summary>
    /// Đại diện cho một chức năng/công cụ vừa được người dùng chọn để hiển thị trên thanh Truy cập nhanh (tối đa 3 mục).
    /// </summary>
    public class RecentToolItem
    {
        public string Id { get; set; } = string.Empty;
        public string ShortTitle { get; set; } = string.Empty;
        public string FullTooltip { get; set; } = string.Empty;
        public string IconPathData { get; set; } = string.Empty;
        public Color IconColor { get; set; } = Color.FromRgb(0x2E, 0x86, 0xDE);
        public Action? ReplayAction { get; set; }
    }

    /// <summary>
    /// Quản lý danh sách tối đa 3 chức năng gần nhất (MRU - Most Recently Used) trên thanh công cụ chính.
    /// </summary>
    public class RecentToolsManager
    {
        public const int MaxRecentItems = 3;

        private readonly List<RecentToolItem> _items = new List<RecentToolItem>(MaxRecentItems);

        /// <summary>
        /// Sự kiện phát ra mỗi khi danh sách 3 chức năng gần nhất thay đổi.
        /// </summary>
        public event EventHandler? RecentToolsChanged;

        /// <summary>
        /// Danh sách hiện tại (tối đa 3 phần tử, phần tử [0] là chức năng vừa chọn gần nhất).
        /// </summary>
        public IReadOnlyList<RecentToolItem> Items => _items;

        /// <summary>
        /// Ghi nhận một chức năng vừa được chọn. Nếu trùng Id sẽ đẩy lên vị trí đầu tiên (#1).
        /// </summary>
        public void RecordTool(
            string id,
            string shortTitle,
            string iconPathData,
            Color iconColor,
            Action replayAction,
            string? fullTooltip = null)
        {
            if (string.IsNullOrWhiteSpace(id) || replayAction == null)
                return;

            // Xóa bản ghi cũ nếu trùng Id để đưa lên đầu danh sách
            _items.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

            var newItem = new RecentToolItem
            {
                Id = id,
                ShortTitle = string.IsNullOrWhiteSpace(shortTitle) ? "Công cụ" : shortTitle.Trim(),
                FullTooltip = string.IsNullOrWhiteSpace(fullTooltip)
                    ? $"Dùng lại nhanh: {shortTitle}"
                    : fullTooltip,
                IconPathData = string.IsNullOrWhiteSpace(iconPathData)
                    ? DefaultToolIconPath
                    : iconPathData,
                IconColor = iconColor,
                ReplayAction = replayAction
            };

            _items.Insert(0, newItem);

            // Chỉ giữ đúng 3 chức năng gần nhất
            while (_items.Count > MaxRecentItems)
            {
                _items.RemoveAt(_items.Count - 1);
            }

            RecentToolsChanged?.Invoke(this, EventArgs.Empty);
        }

        // ── Các hằng số Vector Path (24x24) chuẩn cho các nhóm chức năng ──
        public const string DefaultToolIconPath = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        public const string PenIconPath = "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.06,6.18L3,17.25Z";
        public const string ShapePenIconPath = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4Z";
        public const string HighlighterIconPath = "M18.5,1.15C17.97,1.15 17.46,1.34 17.07,1.73L11.26,7.55L16.91,13.2L22.73,7.39C23.5,6.61 23.5,5.35 22.73,4.56L19.89,1.73C19.5,1.34 19,1.15 18.5,1.15M10.3,8.5L4.34,14.46C3.56,15.24 3.56,16.5 4.34,17.27L6.73,19.66C7.5,20.44 8.76,20.44 9.54,19.66L15.5,13.7L10.3,8.5M2,20V22H9L7,20H2Z";
        public const string LaserPenIconPath = "M7.5,5.6L5,7L6.4,4.5L5,2L7.5,3.4L10,2L8.6,4.5L10,7L7.5,5.6M19.5,15.4L22,14L20.6,16.5L22,19L19.5,17.6L17,19L18.4,16.5L17,14L19.5,15.4M22,2L20.6,4.5L22,7L19.5,5.6L17,7L18.4,4.5L17,2L19.5,3.4L22,2M13.34,12.78L15.78,10.34L13.66,8.22L11.22,10.66L13.34,12.78M14.37,7.29L16.71,9.63C17.1,10.02 17.1,10.65 16.71,11.04L5.04,22.71C4.65,23.1 4.02,23.1 3.63,22.71L1.29,20.37C0.9,19.98 0.9,19.35 1.29,18.96L12.96,7.29C13.35,6.9 13.98,6.9 14.37,7.29Z";
        public const string EraserIconPath = "M16.24,3.56L21.19,8.5C21.97,9.29 21.97,10.55 21.19,11.34L12,20.53C10.44,22.09 7.91,22.09 6.34,20.53L2.81,17C2.03,16.21 2.03,14.95 2.81,14.16L13.41,3.56C14.2,2.78 15.46,2.78 16.24,3.56M4.22,15.58L7.76,19.11C8.54,19.9 9.8,19.9 10.59,19.11L14.12,15.58L9.17,10.63L4.22,15.58Z";
        public const string KeyboardIconPath = "M19,10H17V8H19M19,13H17V11H19M16,10H14V8H16M16,13H14V11H16M16,17H8V15H16M7,10H5V8H7M7,13H5V11H7M8,11H10V13H8M8,8H10V10H8M11,11H13V13H11M11,8H13V10H11M20,5H4C2.89,5 2,5.89 2,7V17A2,2 0 0,0 4,19H20A2,2 0 0,0 22,17V7C22,5.89 21.1,5 20,5Z";
        public const string ImageIconPath = "M8.5,13.5L11,16.5L14.5,12L19,18H5M21,19V5C21,3.89 20.1,3 19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19Z";
        public const string VideoIconPath = "M17,10.5V7A1,1 0 0,0 16,6H4A1,1 0 0,0 3,7V17A1,1 0 0,0 4,18H16A1,1 0 0,0 17,17V13.5L21,17.5V6.5L17,10.5Z";
        public const string BookIconPath = "M18,2H12V9L9.5,7.5L7,9V2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V4A2,2 0 0,0 18,2Z";
        public const string BarChartIconPath = "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z";
        public const string PieChartIconPath = "M11,2V22C5.9,21.5 2,17.2 2,12C2,6.8 5.9,2.5 11,2M13,2V11H22C21.5,6.2 17.8,2.5 13,2M13,13V22C17.7,21.5 21.5,17.8 22,13H13Z";
        public const string ShapeIconPath = "M3,3H11V11H3V3M13,3H21V11H13V3M3,13H11V21H3V13M18,13L22,21H14L18,13Z";
        public const string Cube3DIconPath = "M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5M12,4.15L6.04,7.5L12,10.85L17.96,7.5L12,4.15M5,15.91L11,19.29V12.58L5,9.21V15.91M19,15.91V9.21L13,12.58V19.29L19,15.91Z";
        public const string SelectRectIconPath = "M3,5H5V3C3.9,3 3,3.9 3,5M3,13H5V11H3V13M7,21H9V19H7V21M3,9H5V7H3V9M13,3H11V5H13V3M19,3V5H21C21,3.9 20.1,3 19,3M5,21V19H3C3,20.1 3.9,21 5,21M3,17H5V15H3V17M9,3H7V5H9V3M11,21H13V19H11V21M19,13H21V11H19V13M19,21C20.1,21 21,20.1 21,19H19V21M19,9H21V7H19V9M19,17H21V15H19V17M15,21H17V19H15V21M15,5H17V3H15V5Z";
        public const string SelectLassoIconPath = "M9,3C5.13,3 2,5.69 2,9C2,11.42 3.67,13.5 6.08,14.44C6.03,14.94 6,15.46 6,16A2,2 0 0,0 8,18H9V16H8C7.68,16 7.47,15.32 7.5,14.81C8,14.93 8.5,15 9,15C12.87,15 16,12.31 16,9C16,5.69 12.87,3 9,3M9,5C11.76,5 14,6.79 14,9C14,11.21 11.76,13 9,13C6.24,13 4,11.21 4,9C4,6.79 6.24,5 9,5Z";
        public const string MagicWandIconPath = "M7.5,5.6L5,7L6.4,4.5L5,2L7.5,3.4L10,2L8.6,4.5L10,7L7.5,5.6M19.5,15.4L22,14L20.6,16.5L22,19L19.5,17.6L17,19L18.4,16.5L17,14L19.5,15.4M22,2L20.6,4.5L22,7L19.5,5.6L17,7L18.4,4.5L17,2L19.5,3.4L22,2M13.34,12.78L15.78,10.34L13.66,8.22L11.22,10.66L13.34,12.78M14.37,7.29L16.71,9.63C17.1,10 17.1,10.65 16.71,11.04L5.04,22.71C4.65,23.1 4,23.1 3.63,22.71L1.29,20.37C0.9,20 0.9,19.35 1.29,18.96L12.96,7.29C13.35,6.9 14,6.9 14.37,7.29Z";
        public const string RulerIconPath = "M2,6H22V18H2V6M4,8V16H20V8H18V12H16V8H14V11H12V8H10V12H8V8H6V11H4V8Z";
        public const string ProtractorIconPath = "M12,5A9,9 0 0,0 3,14V19H21V14A9,9 0 0,0 12,5M12,7A7,7 0 0,1 19,14H15A3,3 0 0,0 12,11A3,3 0 0,0 9,14H5A7,7 0 0,1 12,7Z";
        public const string TriangleToolIconPath = "M2,21V3L20,21H2M5,18H12.5L5,10.5V18Z";
        public const string CompassIconPath = "M12,2A2,2 0 0,1 14,4C14,4.74 13.6,5.39 13,5.73L18.5,19L16.66,19.76L14.5,14.5H9.5L7.34,19.76L5.5,19L11,5.73C10.4,5.39 10,4.74 10,4A2,2 0 0,1 12,2M12,8.38L10.33,12.5H13.67L12,8.38Z";
        public const string TableIconPath = "M4,3H20A2,2 0 0,1 22,5V19A2,2 0 0,1 20,21H4A2,2 0 0,1 2,19V5A2,2 0 0,1 4,3M4,7V11H8V7H4M10,7V11H14V7H10M20,11V7H16V11H20M4,13V17H8V13H4M10,13V17H14V13H10M20,17V13H16V17H20Z";
        public const string NoteIconPath = "M14,10H19.5L14,4.5V10M5,3H15L21,9V19A2,2 0 0,1 19,21H5C3.89,21 3,20.1 3,19V5C3,3.89 3.89,3 5,3M5,12V14H19V12H5M5,16V18H14V16H5Z";
        public const string GraphIconPath = "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H22V21H2V3H4V17.54L9.5,8L16,11.78Z";
        public const string CalculatorIconPath = "M7,2H17A2,2 0 0,1 19,4V20A2,2 0 0,1 17,22H7A2,2 0 0,1 5,20V4A2,2 0 0,1 7,2M7,4V8H17V4H7M7,10V12H9V10H7M11,10V12H13V10H11M15,10V12H17V10H15M7,14V16H9V14H7M11,14V16H13V14H11M15,14V20H17V14H15M7,18V20H9V18H7M11,18V20H13V18H11Z";
        public const string OcrIconPath = "M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z";
        public const string SpotlightIconPath = "M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z";
        public const string PhysicsLabIconPath = "M13,6V11H18V7.75L22.25,12L18,16.25V13H13V18H16.25L12,22.25L7.75,18H11V13H6V16.25L1.75,12L6,7.75V11H11V6H7.75L12,1.75L16.25,6H13Z";
        public const string PeriodicTableIconPath = "M3,3H21V21H3V3M5,5V19H19V5H5M7,7H11V11H7V7M13,7H17V11H13V7M7,13H11V17H7V13M13,13H17V17H13V13Z";

        /// <summary>
        /// Tra cứu tên Tiếng Việt ngắn gọn và icon cho một hình học (2D / 3D / Toán-Lý-Hóa-Sinh).
        /// </summary>
        public static (string ShortTitle, string IconPath, Color AccentColor) GetShapeMetadata(string shapeName)
        {
            if (string.IsNullOrWhiteSpace(shapeName))
                return ("Hình học", ShapeIconPath, Color.FromRgb(0x2E, 0x86, 0xDE));

            // 1. Tra cứu trong danh mục học cụ Toán-Lý-Hóa-Sinh của SubjectShapeGeometryService
            foreach (var subj in SubjectShapeGeometryService.Subjects)
            {
                foreach (var cat in subj.Categories)
                {
                    var found = cat.Shapes.FirstOrDefault(s => string.Equals(s.Id, shapeName, StringComparison.OrdinalIgnoreCase));
                    if (found != null)
                    {
                        Color subjColor = subj.Id switch
                        {
                            "math" => Color.FromRgb(0x2E, 0x7D, 0x32),
                            "physics" => Color.FromRgb(0xE6, 0x51, 0x00),
                            "chem" => Color.FromRgb(0x00, 0x83, 0x8F),
                            "bio" => Color.FromRgb(0xAD, 0x14, 0x57),
                            _ => Color.FromRgb(0x2E, 0x86, 0xDE)
                        };
                        string is3DIcon = (cat.Id.Contains("3d", StringComparison.OrdinalIgnoreCase)) ? Cube3DIconPath : ShapeIconPath;
                        string shortName = TruncateShortTitle(found.Name);
                        return (shortName, is3DIcon, subjColor);
                    }
                }
            }

            // 2. Tra cứu các hình 2D / 3D chuẩn
            return shapeName switch
            {
                "StraightLine" => ("Đoạn thẳng", "M3,21L21,3L19.59,1.59L1.59,19.59L3,21Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "ArrowLine" => ("Mũi tên", "M4,11V13H16L10.5,18.5L11.92,19.92L19.84,12L11.92,4.08L10.5,5.5L16,11H4Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "DashedLine" => ("Nét đứt", "M3,11H7V13H3V11M10,11H14V13H10V11M17,11H21V13H17V11Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "DoubleArrowLine" => ("Mũi tên kép", "M8,11H16V8L20,12L16,16V13H8V16L4,12L8,8V11Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Circle" => ("Hình tròn", "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Triangle" => ("Tam giác", "M12,2L1,21H23L12,2M12,6L19.53,19H4.47L12,6Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "RightTriangle" => ("Tam giác vuông", "M2,21V3L20,21H2M5,18H12.5L5,10.5V18Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Square" => ("Hình vuông", "M3,3V21H21V3H3M5,5H19V19H5V5Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Rectangle" => ("Chữ nhật", "M2,4H22V20H2V4M4,6V18H20V6H4Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "RoundedRectangle" => ("CN bo góc", "M4,4H20A2,2 0 0,1 22,6V18A2,2 0 0,1 20,20H4A2,2 0 0,1 2,18V6A2,2 0 0,1 4,4M4,6V18H20V6H4Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Pentagon" => ("Ngũ giác", "M12,2.5L2,9.8L5.8,21.5H18.2L22,9.8L12,2.5Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Hexagon" => ("Lục giác", "M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Star" => ("Ngôi sao", "M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z", Color.FromRgb(0xF5, 0x9E, 0x0B)),
                "Heart" => ("Trái tim", "M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z", Color.FromRgb(0xEE, 0x5A, 0x6F)),
                "Diamond" => ("Hình thoi", "M12,2L22,12L12,22L2,12L12,2Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Parallelogram" => ("Bình hành", "M6,4H22L18,20H2L6,4Z", Color.FromRgb(0x2E, 0x86, 0xDE)),
                "Cube" => ("Lập phương", Cube3DIconPath, Color.FromRgb(0x7C, 0x3A, 0xED)),
                "Sphere" => ("Hình cầu", Cube3DIconPath, Color.FromRgb(0x7C, 0x3A, 0xED)),
                "Pyramid" => ("Hình chóp", Cube3DIconPath, Color.FromRgb(0x7C, 0x3A, 0xED)),
                "Tetrahedron" => ("Tứ diện", Cube3DIconPath, Color.FromRgb(0x7C, 0x3A, 0xED)),
                "Cylinder" => ("Hình trụ", Cube3DIconPath, Color.FromRgb(0x7C, 0x3A, 0xED)),
                "Cone" => ("Hình nón", Cube3DIconPath, Color.FromRgb(0x7C, 0x3A, 0xED)),
                "Prism" => ("Lăng trụ", Cube3DIconPath, Color.FromRgb(0x7C, 0x3A, 0xED)),
                _ => (TruncateShortTitle(shapeName), ShapeIconPath, Color.FromRgb(0x2E, 0x86, 0xDE))
            };
        }

        private static string TruncateShortTitle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Hình học";
            string trimmed = raw.Trim();
            return trimmed.Length <= 11 ? trimmed : trimmed.Substring(0, 10).TrimEnd() + "…";
        }
    }
}

