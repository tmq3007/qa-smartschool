using System.Windows.Media;

namespace QASmartClass.LearningTools
{
    /// <summary>
    /// ══════════════════════════════════════════════════════════════════
    ///  QA SMART CLASS — DESIGN CONSTANTS
    ///  Tiêu chuẩn thiết kế thống nhất cho toàn bộ LearningTools.
    ///  
    ///  ⚠️ LUÔN SỬ DỤNG CÁC HẰNG SỐ NÀY THAY VÌ HARDCODE GIÁ TRỊ.
    ///  ⚠️ KHÔNG dùng font Consolas — gây lỗi tiếng Việt + emoji.
    /// ══════════════════════════════════════════════════════════════════
    /// </summary>
    public static class DS
    {
        // ═══════════════════════════════════════════════════════════
        //  🔤 FONTS — Tối ưu cho tiếng Việt + emoji + trình chiếu
        // ═══════════════════════════════════════════════════════════

        /// <summary>Font chính — dùng cho mọi nội dung text</summary>
        public static readonly FontFamily FontPrimary = new("pack://application:,,,/QASmartClass;component/Resources/Fonts/#Inter, Segoe UI, Arial");

        /// <summary>Font đậm — dùng cho tiêu đề, kết quả quan trọng</summary>
        public static readonly FontFamily FontBold = new("pack://application:,,,/QASmartClass;component/Resources/Fonts/#Outfit, Century Gothic, Segoe UI Semibold");

        /// <summary>Font công thức toán — dùng cho biểu thức, ký hiệu</summary>
        public static readonly FontFamily FontMath = new("pack://application:,,,/QASmartClass;component/Resources/Fonts/#Inter, Segoe UI, Arial");

        /// <summary>Font số liệu — dùng cho input số, bảng dữ liệu thuần số</summary>
        public static readonly FontFamily FontData = new("pack://application:,,,/QASmartClass;component/Resources/Fonts/#Inter, Segoe UI, Arial");

        // ═══════════════════════════════════════════════════════════
        //  📏 FONT SIZES — Tối ưu trình chiếu 55"-86"
        //  Quy tắc: Học sinh ngồi xa 3-6m vẫn đọc được
        // ═══════════════════════════════════════════════════════════

        /// <summary>Tiêu đề chính của tool (VD: "📊 Thống Kê Cơ Bản")</summary>
        public const double FontTitle = 24;

        /// <summary>Mô tả phụ dưới tiêu đề</summary>
        public const double FontSubtitle = 14;

        /// <summary>Label cho input field (VD: "Nhập a:")</summary>
        public const double FontLabel = 16;

        /// <summary>Giá trị trong ô input</summary>
        public const double FontInput = 20;

        /// <summary>Dòng kết quả chính</summary>
        public const double FontResult = 16;

        /// <summary>Tag/badge nhỏ (VD: "Lớp 10", "Interactive")</summary>
        public const double FontTag = 12;

        /// <summary>Công thức toán lớn hiển thị</summary>
        public const double FontFormula = 28;

        /// <summary>Nút preset / ví dụ nhanh</summary>
        public const double FontPreset = 15;

        /// <summary>Tooltip, ghi chú phụ</summary>
        public const double FontNote = 13;

        // ═══════════════════════════════════════════════════════════
        //  🎨 THƯƠNG HIỆU — QA Smart Class Brand Colors
        // ═══════════════════════════════════════════════════════════

        /// <summary>Màu chính thương hiệu — Blue</summary>
        public static readonly Color BrandPrimary = Color.FromRgb(21, 101, 192);      // #1565C0

        /// <summary>Màu phụ thương hiệu — Dark Blue</summary>
        public static readonly Color BrandSecondary = Color.FromRgb(13, 71, 161);      // #0D47A1

        /// <summary>Màu accent — Orange</summary>
        public static readonly Color BrandAccent = Color.FromRgb(230, 81, 0);          // #E65100

        // ═══════════════════════════════════════════════════════════
        //  🏷️ MÀU THEO NHÓM TOOL (Category Colors)
        // ═══════════════════════════════════════════════════════════

        /// <summary>Toán học — Blue</summary>
        public static readonly Color CatMath = Color.FromRgb(21, 101, 192);            // #1565C0
        public static readonly Color CatMathBg = Color.FromRgb(227, 242, 253);         // #E3F2FD

        /// <summary>Khoa học — Green</summary>
        public static readonly Color CatScience = Color.FromRgb(46, 125, 50);          // #2E7D32
        public static readonly Color CatScienceBg = Color.FromRgb(232, 245, 233);      // #E8F5E9

        /// <summary>Ngôn ngữ — Purple</summary>
        public static readonly Color CatLanguage = Color.FromRgb(106, 27, 154);        // #6A1B9A
        public static readonly Color CatLanguageBg = Color.FromRgb(243, 229, 245);     // #F3E5F5

        /// <summary>Đa môn — Orange</summary>
        public static readonly Color CatMulti = Color.FromRgb(230, 81, 0);             // #E65100
        public static readonly Color CatMultiBg = Color.FromRgb(255, 243, 224);        // #FFF3E0

        /// <summary>Tư duy — Pink</summary>
        public static readonly Color CatThinking = Color.FromRgb(173, 20, 87);         // #AD1457
        public static readonly Color CatThinkingBg = Color.FromRgb(252, 228, 236);     // #FCE4EC

        /// <summary>Kỹ năng nghề nghiệp — Teal</summary>
        public static readonly Color CatWorkplace = Color.FromRgb(0, 121, 107);        // #00796B
        public static readonly Color CatWorkplaceBg = Color.FromRgb(224, 242, 241);    // #E0F2F1

        // ═══════════════════════════════════════════════════════════
        //  📊 MÀU KẾT QUẢ (Result Row Colors)
        // ═══════════════════════════════════════════════════════════

        public static readonly Color ResultPrimary = Color.FromRgb(21, 101, 192);      // #1565C0 — Kết quả chính
        public static readonly Color ResultSuccess = Color.FromRgb(46, 125, 50);       // #2E7D32 — Đúng/tốt
        public static readonly Color ResultWarning = Color.FromRgb(230, 81, 0);        // #E65100 — Cảnh báo
        public static readonly Color ResultDanger = Color.FromRgb(198, 40, 40);        // #C62828 — Lỗi/sai
        public static readonly Color ResultInfo = Color.FromRgb(117, 117, 117);        // #757575 — Ghi chú
        public static readonly Color ResultSpecial = Color.FromRgb(123, 31, 162);      // #7B1FA2 — Đặc biệt
        public static readonly Color ResultDark = Color.FromRgb(40, 53, 147);          // #283593 — Nhấn mạnh

        // ═══════════════════════════════════════════════════════════
        //  🎨 UI TEXT COLORS
        // ═══════════════════════════════════════════════════════════

        public static readonly Color TextPrimary = Color.FromRgb(33, 33, 33);          // #212121
        public static readonly Color TextSecondary = Color.FromRgb(51, 51, 51);      // #333333
        public static readonly Color TextWhite = Colors.White;

        // ═══════════════════════════════════════════════════════════
        //  🎨 SHORT NAMES (for convenience in UI factory)
        // ═══════════════════════════════════════════════════════════

        public static Color Primary => BrandPrimary;
        public static Color Secondary => BrandSecondary;
        public static Color Accent => BrandAccent;
        public static Color Success => ResultSuccess;
        public static Color Warning => ResultWarning;
        public static Color Danger => ResultDanger;
        public static Color Info => ResultInfo;
        public static Color Error => ResultDanger;
        public static Color ResultError => ResultDanger;

        // ═══════════════════════════════════════════════════════════
        //  📐 SPACING — Khoảng cách chuẩn
        // ═══════════════════════════════════════════════════════════

        /// <summary>Padding bên trong card/border chính</summary>
        public const double PadCard = 20;

        /// <summary>Padding nhẹ cho chip/badge</summary>
        public const double PadChip = 12;

        /// <summary>Margin giữa các section</summary>
        public const double MarginSection = 16;

        /// <summary>Margin giữa các dòng kết quả</summary>
        public const double MarginResult = 4;

        /// <summary>Bo góc card</summary>
        public const double RadiusCard = 12;

        /// <summary>Bo góc chip/badge</summary>
        public const double RadiusChip = 8;

        /// <summary>Bo góc header</summary>
        public const double RadiusHeader = 10;

        // ═══════════════════════════════════════════════════════════
        //  👆 TOUCH — Kích thước tối thiểu cho màn hình cảm ứng
        // ═══════════════════════════════════════════════════════════

        /// <summary>Chiều cao tối thiểu nút bấm (48dp theo Material Design)</summary>
        public const double TouchMinHeight = 44;

        /// <summary>Chiều rộng tối thiểu nút bấm</summary>
        public const double TouchMinWidth = 44;

        /// <summary>Khoảng cách tối thiểu giữa 2 nút (tránh bấm nhầm)</summary>
        public const double TouchGap = 8;

        /// <summary>Max width cho nội dung tool (tránh kéo dãn quá rộng)</summary>
        public const double ContentMaxWidth = 800;

        // ═══════════════════════════════════════════════════════════
        //  🎯 HELPER: Tạo brush nhanh
        // ═══════════════════════════════════════════════════════════

        public static SolidColorBrush Brush(Color c) => new(c);
        public static SolidColorBrush BrushAlpha(Color c, byte alpha) => new(Color.FromArgb(alpha, c.R, c.G, c.B));

        /// <summary>Tạo màu nền nhạt từ foreground (alpha 20)</summary>
        public static SolidColorBrush LightBg(Color fg) => new(Color.FromArgb(20, fg.R, fg.G, fg.B));

        /// <summary>Tạo màu nền vừa từ foreground (alpha 40)</summary>
        public static SolidColorBrush MediumBg(Color fg) => new(Color.FromArgb(40, fg.R, fg.G, fg.B));
    }
}
