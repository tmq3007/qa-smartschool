using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace QASmartClass.Services.Tools
{
    public enum SmartNoteThemeType
    {
        White,
        Yellow,
        Mint,
        Sky,
        Peach,
        Purple,
        Pink,
        Dark
    }

    public class SmartNoteTheme
    {
        public SmartNoteThemeType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public Brush Background { get; set; } = Brushes.White;
        public Brush BorderBrush { get; set; } = Brushes.Teal;
        public Brush BadgeBg { get; set; } = Brushes.LightCyan;
        public Brush BadgeFg { get; set; } = Brushes.DarkCyan;
        public Brush TextColor { get; set; } = Brushes.Black;
        public Brush TitleColor { get; set; } = Brushes.DarkSlateGray;
        public Brush DividerBrush { get; set; } = Brushes.LightGray;

        private static readonly Dictionary<SmartNoteThemeType, SmartNoteTheme> Themes = new()
        {
            [SmartNoteThemeType.White] = new SmartNoteTheme
            {
                Type = SmartNoteThemeType.White,
                Name = "Trắng Tinh Khôi",
                Background = new SolidColorBrush(Color.FromArgb(250, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(22, 139, 130)), // #168B82 (MathCard Teal)
                BadgeBg = new SolidColorBrush(Color.FromArgb(35, 22, 139, 130)),
                BadgeFg = new SolidColorBrush(Color.FromRgb(22, 139, 130)),
                TextColor = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800
                TitleColor = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                DividerBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240))
            },
            [SmartNoteThemeType.Yellow] = new SmartNoteTheme
            {
                Type = SmartNoteThemeType.Yellow,
                Name = "Vàng Note",
                Background = new SolidColorBrush(Color.FromRgb(254, 249, 195)), // #FEF9C3
                BorderBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6)), // Amber #D97706
                BadgeBg = new SolidColorBrush(Color.FromRgb(254, 243, 199)),
                BadgeFg = new SolidColorBrush(Color.FromRgb(180, 83, 9)),
                TextColor = new SolidColorBrush(Color.FromRgb(69, 26, 3)),
                TitleColor = new SolidColorBrush(Color.FromRgb(120, 53, 15)),
                DividerBrush = new SolidColorBrush(Color.FromRgb(253, 230, 138))
            },
            [SmartNoteThemeType.Mint] = new SmartNoteTheme
            {
                Type = SmartNoteThemeType.Mint,
                Name = "Xanh Mint",
                Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)), // #DCFCE7
                BorderBrush = new SolidColorBrush(Color.FromRgb(22, 163, 74)), // Emerald #16A34A
                BadgeBg = new SolidColorBrush(Color.FromRgb(232, 245, 233)),
                BadgeFg = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                TextColor = new SolidColorBrush(Color.FromRgb(20, 83, 45)),
                TitleColor = new SolidColorBrush(Color.FromRgb(20, 83, 45)),
                DividerBrush = new SolidColorBrush(Color.FromRgb(187, 247, 208))
            },
            [SmartNoteThemeType.Sky] = new SmartNoteTheme
            {
                Type = SmartNoteThemeType.Sky,
                Name = "Xanh Da Trời",
                Background = new SolidColorBrush(Color.FromRgb(224, 242, 254)), // #E0F2FE
                BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199)), // Sky #0284C7
                BadgeBg = new SolidColorBrush(Color.FromRgb(225, 245, 254)),
                BadgeFg = new SolidColorBrush(Color.FromRgb(2, 119, 189)),
                TextColor = new SolidColorBrush(Color.FromRgb(12, 74, 110)),
                TitleColor = new SolidColorBrush(Color.FromRgb(12, 74, 110)),
                DividerBrush = new SolidColorBrush(Color.FromRgb(186, 230, 253))
            },
            [SmartNoteThemeType.Peach] = new SmartNoteTheme
            {
                Type = SmartNoteThemeType.Peach,
                Name = "Cam Phấn",
                Background = new SolidColorBrush(Color.FromRgb(255, 237, 213)), // #FFEDD5
                BorderBrush = new SolidColorBrush(Color.FromRgb(234, 88, 12)), // Orange #EA580C
                BadgeBg = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                BadgeFg = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                TextColor = new SolidColorBrush(Color.FromRgb(124, 45, 18)),
                TitleColor = new SolidColorBrush(Color.FromRgb(124, 45, 18)),
                DividerBrush = new SolidColorBrush(Color.FromRgb(254, 215, 170))
            },
            [SmartNoteThemeType.Purple] = new SmartNoteTheme
            {
                Type = SmartNoteThemeType.Purple,
                Name = "Tím Mộng Mơ",
                Background = new SolidColorBrush(Color.FromRgb(243, 232, 255)), // #F3E8FF
                BorderBrush = new SolidColorBrush(Color.FromRgb(147, 51, 234)), // Purple #9333EA
                BadgeBg = new SolidColorBrush(Color.FromRgb(243, 229, 245)),
                BadgeFg = new SolidColorBrush(Color.FromRgb(123, 31, 162)),
                TextColor = new SolidColorBrush(Color.FromRgb(88, 28, 135)),
                TitleColor = new SolidColorBrush(Color.FromRgb(88, 28, 135)),
                DividerBrush = new SolidColorBrush(Color.FromRgb(233, 213, 255))
            },
            [SmartNoteThemeType.Pink] = new SmartNoteTheme
            {
                Type = SmartNoteThemeType.Pink,
                Name = "Hồng Phấn",
                Background = new SolidColorBrush(Color.FromRgb(252, 231, 243)), // #FCE7F3
                BorderBrush = new SolidColorBrush(Color.FromRgb(219, 39, 119)), // Pink #DB2777
                BadgeBg = new SolidColorBrush(Color.FromRgb(252, 228, 236)),
                BadgeFg = new SolidColorBrush(Color.FromRgb(194, 24, 91)),
                TextColor = new SolidColorBrush(Color.FromRgb(131, 24, 67)),
                TitleColor = new SolidColorBrush(Color.FromRgb(131, 24, 67)),
                DividerBrush = new SolidColorBrush(Color.FromRgb(244, 114, 182))
            },
            [SmartNoteThemeType.Dark] = new SmartNoteTheme
            {
                Type = SmartNoteThemeType.Dark,
                Name = "Bảng Tối",
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // #1E293B
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139)), // #64748B
                BadgeBg = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BadgeFg = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                TextColor = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                TitleColor = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                DividerBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85))
            }
        };

        public static SmartNoteTheme Get(SmartNoteThemeType type)
        {
            return Themes.TryGetValue(type, out var theme) ? theme : Themes[SmartNoteThemeType.White];
        }

        public static IEnumerable<SmartNoteTheme> GetAll() => Themes.Values;
    }

    public class SmartNoteFontInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string FontFamilyName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public static readonly List<SmartNoteFontInfo> SupportedFonts = new()
        {
            // 1. Phông viết tay bo tròn (Canva)
            new SmartNoteFontInfo { DisplayName = "Baloo 2 (Bụng tròn)", FontFamilyName = "Baloo 2, Nunito, Segoe UI", Category = "Viết tay bo tròn" },
            new SmartNoteFontInfo { DisplayName = "Mali (Nắn nót)", FontFamilyName = "Mali, Comic Sans MS, Segoe UI", Category = "Viết tay bo tròn" },
            new SmartNoteFontInfo { DisplayName = "Itim (Phấn tròn)", FontFamilyName = "Itim, Mali, Segoe UI", Category = "Viết tay bo tròn" },
            new SmartNoteFontInfo { DisplayName = "Nunito (Tròn hiện đại)", FontFamilyName = "Nunito, Segoe UI, sans-serif", Category = "Viết tay bo tròn" },
            new SmartNoteFontInfo { DisplayName = "Comfortaa (Bo tròn)", FontFamilyName = "Comfortaa, Arial, sans-serif", Category = "Viết tay bo tròn" },

            // 2. Bảng phấn & Chữ viết thảo
            new SmartNoteFontInfo { DisplayName = "Patrick Hand (Bảng phấn)", FontFamilyName = "Patrick Hand, Segoe Print, Comic Sans MS", Category = "Bảng phấn & Thảo" },
            new SmartNoteFontInfo { DisplayName = "Segoe Print (Viết tay)", FontFamilyName = "Segoe Print, Comic Sans MS", Category = "Bảng phấn & Thảo" },
            new SmartNoteFontInfo { DisplayName = "Segoe Script (Viết mềm)", FontFamilyName = "Segoe Script, Segoe Print, cursive", Category = "Bảng phấn & Thảo" },

            // 3. Sách giáo khoa & Chuẩn in ấn
            new SmartNoteFontInfo { DisplayName = "Segoe UI (Chuẩn sắc nét)", FontFamilyName = "Segoe UI, sans-serif", Category = "Chuẩn SGK & In ấn" },
            new SmartNoteFontInfo { DisplayName = "Times New Roman (Chuẩn đề thi)", FontFamilyName = "Times New Roman, serif", Category = "Chuẩn SGK & In ấn" },
            new SmartNoteFontInfo { DisplayName = "Arial (Hiện đại)", FontFamilyName = "Arial, sans-serif", Category = "Chuẩn SGK & In ấn" },
            new SmartNoteFontInfo { DisplayName = "Calibri (Thanh lịch)", FontFamilyName = "Calibri, sans-serif", Category = "Chuẩn SGK & In ấn" },
            new SmartNoteFontInfo { DisplayName = "Cambria Math (Toán học)", FontFamilyName = "Cambria Math, Cambria, serif", Category = "Chuẩn SGK & In ấn" }
        };
    }

    public class SmartNoteModel
    {
        public string Title { get; set; } = "GHI CHÚ BÀI HỌC";
        public string BadgeText { get; set; } = "LƯU Ý";
        public string Content { get; set; } = "Nhập nội dung ghi chú tại đây...";
        public string FontFamily { get; set; } = "Segoe UI, sans-serif";
        public double FontSize { get; set; } = 24.0;
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public SmartNoteThemeType ThemeType { get; set; } = SmartNoteThemeType.White;
        public double Width { get; set; } = 420;
    }
}

