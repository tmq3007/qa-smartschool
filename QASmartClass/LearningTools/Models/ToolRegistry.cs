using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.LearningTools.Models
{
    /// <summary>
    /// Registry đăng ký tất cả 72 công cụ học tập
    /// Singleton pattern — dùng ToolRegistry.AllTools để lấy danh sách
    /// </summary>
    public static class ToolRegistry
    {
        public static List<ToolDefinition> AllTools { get; } = new()
        {
            // ══════════════════════════════════════════════════════
            //  🔢 TOÁN HỌC (18 tools)
            // ══════════════════════════════════════════════════════
            new() { Id = "basic_math", Name = "Toán Tiểu Học", Icon = "🔢",
                    Description = "Luyện tập cộng trừ, so sánh với chế độ thi đấu nhiều người",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 1-5",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "tiểu học", "cộng trừ", "thi đấu", "so sánh" } },
            new() { Id = "multiplication", Name = "Bảng Cửu Chương", Icon = "🔢",
                    Description = "Bảng nhân 1-20, flash card, quiz tốc độ",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 1-5",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "nhân", "cửu chương", "tiểu học" } },

            new() { Id = "trigonometry", Name = "Bảng Lượng Giác", Icon = "📐",
                    Description = "sin, cos, tan — bảng giá trị & đường tròn lượng giác",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 10-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#F3E5F5",
                    Tags = new[] { "lượng giác", "sin", "cos", "tan", "THPT" } },

            new() { Id = "geometry", Name = "Hình Học Calculator", Icon = "📐",
                    Description = "Tính diện tích, chu vi, thể tích — 12+ hình dạng",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "hình học", "diện tích", "thể tích", "chu vi" } },

            new() { Id = "calculator", Name = "Máy Tính Khoa Học", Icon = "🧮",
                    Description = "Calculator khoa học nâng cao, lịch sử tính toán",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF3E0",
                    Tags = new[] { "máy tính", "calculator", "khoa học" } },

            new() { Id = "quadratic", Name = "Giải PT Bậc 2", Icon = "📊",
                    Description = "ax² + bx + c = 0 → Δ, nghiệm, phân tích nhân tử, đỉnh Parabol, lý thuyết, đồ thị",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 9-10",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FCE4EC",
                    Tags = new[] { "phương trình", "bậc 2", "delta", "parabol", "Vieta", "Graph" } },

            new() { Id = "linear_system", Name = "Giải Hệ PT Bậc Nhất", Icon = "📐",
                    Description = "Hệ 2 PT bậc nhất 2 ẩn — Cramer, thế, cộng — đồ thị giao điểm",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 9-10",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "hệ phương trình", "bậc nhất", "Cramer", "2 ẩn", "Graph" } },

            new() { Id = "trig_equation", Name = "Giải PT Lượng Giác", Icon = "🔄",
                    Description = "sin x, cos x, tan x, cot x = a — nghiệm tổng quát, đồ thị",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 10-11",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#F3E5F5",
                    Tags = new[] { "lượng giác", "sin", "cos", "tan", "cot", "trigonometry", "Graph" } },

            new() { Id = "logarithm", Name = "Logarithm & Lũy thừa", Icon = "📈",
                    Description = "Bảng log, ln, e^x — tra cứu & tính toán",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 11-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "logarit", "lũy thừa", "giải tích" } },

            new() { Id = "prime_numbers", Name = "Số Nguyên Tố", Icon = "📐",
                    Description = "Kiểm tra, phân tích thừa số, sàng Eratosthenes",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 6-9",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8EAF6",
                    Tags = new[] { "nguyên tố", "ước số", "ƯCLN", "BCNN" } },

            new() { Id = "number_base", Name = "Chuyển Đổi Hệ Số", Icon = "💻",
                    Description = "Nhị phân, Bát phân, Hex — converter & ASCII",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 10-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF3E0",
                    Tags = new[] { "nhị phân", "hex", "binary", "ASCII", "tin học" } },

            new() { Id = "identities", Name = "Hằng Đẳng Thức Đáng Nhớ", Icon = "📐",
                    Description = "7 HĐT: bình phương, lập phương, kiểm chứng bằng số",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 8-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8EAF6",
                    Tags = new[] { "hằng đẳng thức", "bình phương", "lập phương", "khai triển", "đại số" } },

            new() { Id = "inequality", Name = "Giải Bất Phương Trình", Icon = "📐",
                    Description = "BPT bậc 1, bậc 2 — tập nghiệm, trục số, Graph",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 9-10",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8EAF6",
                    Tags = new[] { "bất phương trình", "BPT", "bậc 1", "bậc 2", "tập nghiệm", "Graph" } },

            new() { Id = "sequence", Name = "Cấp Số Cộng & Nhân", Icon = "🔢",
                    Description = "CSC (d), CSN (q) — Uₙ, Sₙ, dãy số, Graph",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 11",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "cấp số cộng", "cấp số nhân", "dãy số", "Uₙ", "Sₙ", "Graph" } },

            new() { Id = "cubic", Name = "Giải PT Bậc 3", Icon = "📊",
                    Description = "ax³+bx²+cx+d=0 — Horner, nghiệm hữu tỉ, Cardano, Graph",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FCE4EC",
                    Tags = new[] { "phương trình", "bậc 3", "cubic", "Cardano", "Horner", "Graph" } },

            new() { Id = "coordinate", Name = "Tọa Độ Mặt Phẳng", Icon = "📐",
                    Description = "Khoảng cách, trung điểm, PT đường thẳng, góc — Graph",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 10",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8EAF6",
                    Tags = new[] { "tọa độ", "khoảng cách", "trung điểm", "đường thẳng", "hệ số góc", "Graph" } },

            new() { Id = "combinatorics", Name = "Tổ Hợp — Chỉnh Hợp", Icon = "🎲",
                    Description = "P(n), A(n,k), C(n,k), Nhị thức Newton, Tam giác Pascal",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 11",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "tổ hợp", "chỉnh hợp", "hoán vị", "Newton", "Pascal", "n!", "GDPT 2018" } },

            new() { Id = "probability", Name = "Xác Suất", Icon = "🎯",
                    Description = "P(A), Bernoulli, mô phỏng xúc xắc/đồng xu, Graph",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 10-11",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "xác suất", "probability", "Bernoulli", "mô phỏng", "biến cố", "GDPT 2018" } },

            new() { Id = "derivative", Name = "Đạo Hàm", Icon = "📈",
                    Description = "Quy tắc đạo hàm, đa thức, lượng giác, mũ-log, tiếp tuyến, cực trị",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "đạo hàm", "derivative", "cực trị", "tiếp tuyến", "giải tích", "THPT", "Graph" } },

            new() { Id = "integral", Name = "Nguyên Hàm & Tích Phân", Icon = "∫",
                    Description = "Nguyên hàm cơ bản, tích phân xác định Newton-Leibniz, diện tích",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "tích phân", "nguyên hàm", "integral", "Newton-Leibniz", "diện tích", "THPT", "Graph" } },

            new() { Id = "complex_number", Name = "Số Phức", Icon = "ℂ",
                    Description = "Phép toán số phức, module, argument, liên hợp, mặt phẳng phức",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#F3E5F5",
                    Tags = new[] { "số phức", "complex", "module", "argument", "liên hợp", "THPT", "Graph" } },

            new() { Id = "limit", Name = "Giới Hạn", Icon = "∞",
                    Description = "Giới hạn hàm phân thức, dãy số, L'Hôpital, giới hạn đặc biệt",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 11",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF3E0",
                    Tags = new[] { "giới hạn", "limit", "L'Hôpital", "vô định", "dãy số", "THPT", "Graph" } },

            new() { Id = "vector", Name = "Vectơ", Icon = "🏹",
                    Description = "Phép toán vectơ, tích vô hướng, góc, song song, vuông góc",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 10",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "vectơ", "vector", "tích vô hướng", "dot product", "góc", "THPT", "Graph" } },

            new() { Id = "conic_section", Name = "Đường Conic", Icon = "🔵",
                    Description = "Elip, Hyperbol, Parabol — tiêu điểm, tâm sai, tiệm cận",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 10",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FCE4EC",
                    Tags = new[] { "elip", "hyperbol", "parabol", "conic", "tiêu điểm", "tâm sai", "THPT", "Graph" } },

            new() { Id = "solid_geometry", Name = "Hình Học Không Gian", Icon = "🧊",
                    Description = "Thể tích, diện tích — hộp, lập phương, chóp, lăng trụ, trụ, nón, cầu",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 11-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#ECEFF1",
                    Tags = new[] { "hình không gian", "thể tích", "diện tích", "chóp", "lăng trụ", "hình trụ", "hình nón", "hình cầu", "THPT" } },

            new() { Id = "fraction", Name = "Phân Số", Icon = "🕐",
                    Description = "Rút gọn, quy đồng, cộng trừ nhân chia, so sánh, hỗn số — minh họa trực quan",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 4-6",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF8E1",
                    Tags = new[] { "phân số", "fraction", "rút gọn", "quy đồng", "hỗn số", "cộng trừ", "tiểu học" } },

            new() { Id = "math_curriculum", Name = "Chuyến Tàu Toán Học", Icon = "🚂",
                    Description = "Toàn bộ chương trình Toán Lớp 1-12: 50 chương, 737+ bài tập, đề thi thử — theo GDPT 2018",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 1-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8EAF6",
                    Tags = new[] { "chương trình", "toán THPT", "đề thi", "lý thuyết", "GDPT 2018", "curriculum" } },

            new() { Id = "literature_curriculum", Name = "Chuyến Tàu Ngữ Văn", Icon = "🚂",
                    Description = "Hành trình cảm thụ Văn học Lớp 1-12: Văn bản gốc, phân tích, sơ đồ tư duy, trắc nghiệm",
                    Category = ToolCategory.Language, GradeLevel = "Lớp 1-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FCE4EC",
                    Tags = new[] { "chương trình", "ngữ văn", "văn học", "phân tích", "đọc hiểu", "curriculum" } },

            // ══════════════════════════════════════════════════════
            //  🔬 KHOA HỌC (7 tools)
            // ══════════════════════════════════════════════════════
            new() { Id = "periodic_table", Name = "Bảng Tuần Hoàn", Icon = "🧪",
                    Description = "Bảng tuần hoàn 118 nguyên tố, so sánh, mô phỏng phản ứng, biểu đồ hòa tan",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 8-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "bảng tuần hoàn", "nguyên tố", "hóa học", "periodic table", "chemistry", "phản ứng", "tuần hoàn" } },

            new() { Id = "constants", Name = "Hằng Số Vật Lý", Icon = "⚡",
                    Description = "30+ hằng số: Vật lý, Hóa học, Hạt nhân, Thiên văn, Toán học — click copy",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 10-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF8E1",
                    Tags = new[] { "vật lý", "hằng số", "Planck", "Newton", "Avogadro" } },

            new() { Id = "unit_converter", Name = "Đổi Đơn Vị", Icon = "🔄",
                    Description = "10 loại: Chiều dài, Khối lượng, Nhiệt độ, Diện tích, Thể tích, Tốc độ, Áp suất, Dữ liệu...",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F7FA",
                    Tags = new[] { "đổi đơn vị", "converter", "m", "kg", "nhiệt độ", "tốc độ" } },

            new() { Id = "ph_scale", Name = "Thang pH", Icon = "🧪",
                    Description = "Axit → Kiềm, ví dụ chất, tính pH từ [H⁺]",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 10-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#F3E5F5",
                    Tags = new[] { "pH", "axit", "kiềm", "hóa học" } },

            new() { Id = "density", Name = "Khối Lượng Riêng", Icon = "⚖️",
                    Description = "300+ chất, tính m/V, vật nổi-chìm",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#EFEBE9",
                    Tags = new[] { "khối lượng riêng", "mật độ", "nổi chìm" } },

            new() { Id = "wave_speed", Name = "Tốc Độ Sóng", Icon = "📊",
                    Description = "Tốc độ âm/ánh sáng, Doppler, bước sóng",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 11-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "sóng", "âm thanh", "ánh sáng", "Doppler" } },

            new() { Id = "boiling_freezing", Name = "Nhiệt Độ Sôi/Đông", Icon = "🌡️",
                    Description = "Nhiệt độ sôi, đông, chuyển đổi °C/K/°F",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 10-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FBE9E7",
                    Tags = new[] { "nhiệt độ", "sôi", "đông đặc" } },

            new() { Id = "circuit", Name = "Mạch Điện — Ohm", Icon = "⚡",
                    Description = "U=IR, P=UI, nối tiếp/song song, đồ thị V-I Graph",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 11",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF8E1",
                    Tags = new[] { "mạch điện", "Ohm", "điện trở", "nối tiếp", "song song", "Graph" } },

            new() { Id = "lens", Name = "Thấu Kính Quang Học", Icon = "🔭",
                    Description = "1/f=1/d+1/d', ảnh thật/ảo, phóng đại, Graph",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 11",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F7FA",
                    Tags = new[] { "thấu kính", "quang học", "tiêu cự", "ảnh", "Graph" } },

            new() { Id = "electron_config", Name = "Cấu Hình Electron", Icon = "⚛️",
                    Description = "Nhập Z → cấu hình e, lớp vỏ, chu kỳ, kim loại/phi kim",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 10",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#EDE7F6",
                    Tags = new[] { "electron", "cấu hình", "nguyên tử", "hóa học", "lớp vỏ" } },

            new() { Id = "genetics", Name = "Bảng Di Truyền Punnett", Icon = "🧬",
                    Description = "Mendel 1-2 gen, kiểu gen, kiểu hình, tỉ lệ",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "di truyền", "Mendel", "Punnett", "kiểu gen" } },
            new() { Id = "molecular_genetics", Name = "Di Truyền Phân Tử & Đột Biến", Icon = "🧬",
                    Description = "Mô phỏng cơ chế phiên mã, dịch mã và phân tích các dạng đột biến gen.",
                    Category = ToolCategory.Science, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "sinh học", "di truyền", "phiên mã", "dịch mã", "đột biến" } },

            new() { Id = "statistics", Name = "Thống Kê Cơ Bản", Icon = "📊",
                    Description = "Mean, Median, Mode, phương sai, độ lệch chuẩn, Graph",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 10",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "thống kê", "trung bình", "trung vị", "phương sai", "Graph" } },

            // ══════════════════════════════════════════════════════
            //  🗣️ NGÔN NGỮ (4 tools)
            // ══════════════════════════════════════════════════════
            new() { Id = "irregular_verbs", Name = "Động Từ Bất Quy Tắc", Icon = "🔤",
                    Description = "120+ động từ: base → past → past participle",
                    Category = ToolCategory.Language, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "tiếng Anh", "động từ", "irregular", "verbs" } },

            new() { Id = "vocabulary", Name = "Từ Vựng Theo Chủ Đề", Icon = "📚",
                    Description = "20+ chủ đề, hình ảnh, flashcard, quiz",
                    Category = ToolCategory.Language, GradeLevel = "Lớp 1-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#F3E5F5",
                    Tags = new[] { "từ vựng", "vocabulary", "chủ đề" } },

            new() { Id = "grammar", Name = "Ngữ Pháp Tiếng Anh", Icon = "📐",
                    Description = "Quy tắc ngữ pháp, ví dụ, bài tập",
                    Category = ToolCategory.Language, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF3E0",
                    Tags = new[] { "ngữ pháp", "grammar", "thì", "tenses" } },

            new() { Id = "ipa", Name = "Bảng Phiên Âm IPA", Icon = "🗣️",
                    Description = "Nguyên âm, phụ âm, minimal pairs, phát âm",
                    Category = ToolCategory.Language, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "phiên âm", "IPA", "pronunciation" } },

            // ══════════════════════════════════════════════════════
            //  🌐 ĐA MÔN (7 tools)
            // ══════════════════════════════════════════════════════
            new() { Id = "countries", Name = "Nước & Thủ Đô", Icon = "🌍",
                    Description = "195 nước, lá cờ, thủ đô, quiz bản đồ",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 6-9",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "địa lý", "nước", "thủ đô", "quốc gia" } },

            new() { Id = "planets", Name = "Hành Tinh & Vệ Tinh", Icon = "🪐",
                    Description = "8 hành tinh, so sánh kích thước, quỹ đạo",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8EAF6",
                    Tags = new[] { "thiên văn", "hành tinh", "planets" } },

            new() { Id = "math_symbols", Name = "Ký Hiệu Toán Học", Icon = "∑",
                    Description = "120+ ký hiệu toán, logic, tập hợp — click copy",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF3E0",
                    Tags = new[] { "ký hiệu", "toán", "logic", "symbols" } },

            new() { Id = "literature", Name = "Tác Phẩm Văn Học", Icon = "📖",
                    Description = "500+ tác phẩm, tóm tắt, nhân vật, sơ đồ tư duy",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FCE4EC",
                    Tags = new[] { "văn học", "tác phẩm", "nhân vật" } },

            new() { Id = "dynasties", Name = "Triều Đại Lịch Sử", Icon = "🏛️",
                    Description = "Timeline triều đại VN & Thế giới",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#EFEBE9",
                    Tags = new[] { "lịch sử", "triều đại", "timeline" } },

            new() { Id = "formulas", Name = "Bảng Công Thức", Icon = "📋",
                    Description = "50+ công thức Vật lý, Hóa học, Toán học — phân loại theo chủ đề, click copy",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 5-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF8E1",
                    Tags = new[] { "công thức", "vật lý", "hóa học", "toán học", "formula" } },

            new() { Id = "textbooks", Name = "Sách Giáo Khoa Điện Tử", Icon = "📚",
                    Description = "SGK Lớp 1-12, 3 bộ sách GDPT 2018: KNTT, CTST, Cánh Diều",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 1-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "sách giáo khoa", "SGK", "textbook", "KNTT", "Cánh Diều", "Chân Trời", "GDPT 2018" } },

            new() { Id = "notebook", Name = "Sổ Tay Mở Rộng", Icon = "📓",
                    Description = "Sổ tay ghi chú đa phong cách (Cute, Thanh lịch), viết tay thông minh",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF9C4",
                    Tags = new[] { "sổ tay", "ghi chú", "notebook", "viết", "notes", "nháp" } },

            new() { Id = "focus_timer", Name = "Đồng Hồ Tập Trung", Icon = "⏰",
                    Description = "Pomodoro Timer — đếm ngược tập trung, nghỉ giải lao, âm thanh nền thư giãn",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "pomodoro", "focus", "timer", "đồng hồ", "tập trung", "nghỉ", "lofi" } },

            new() { Id = "brainstorm", Name = "Bức Tường Ý Tưởng", Icon = "📌",
                    Description = "Sticky Note Brainstorming — kéo thả giấy nhớ, gom ý kiến cả lớp",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF9C4",
                    Tags = new[] { "brainstorm", "sticky note", "ý tưởng", "giấy nhớ", "nhóm", "thảo luận" } },

            new() { Id = "noise_monitor", Name = "Radar Tiếng Ồn", Icon = "🚦",
                    Description = "Quản lý trật tự lớp học, tự động cảnh báo khi lớp quá ồn",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#BBDEFB",
                    Tags = new[] { "radar", "noise", "ồn", "trật tự", "quản lý lớp", "âm thanh" } },

            new() { Id = "history_timeline", Name = "Dải Thời Gian Lịch Sử", Icon = "⏳",
                    Description = "Timeline tương tác — khám phá lịch sử Việt Nam và Thế giới theo trục thời gian",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 4-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8EAF6",
                    Tags = new[] { "timeline", "lịch sử", "sự kiện", "thời gian", "history", "năm" } },



            new() { Id = "physics_sandbox", Name = "Phòng TN Vật Lý", Icon = "⚛️",
                    Description = "Mô phỏng quang học 2D — Laser, Gương phản xạ, Thấu kính, Lăng kính tán sắc",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Lớp 7-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#EDE7F6",
                    Tags = new[] { "vật lý", "quang học", "laser", "gương", "thấu kính", "physics", "optics" } },

            // ══════════════════════════════════════════════════════
            //  🧠 TƯ DUY & IQ (2 tools)
            // ══════════════════════════════════════════════════════
            new() { Id = "iq_quiz", Name = "Luyện IQ & Logic", Icon = "🧠",
                    Description = "30 ngày luyện IQ: dãy số, ma trận, suy luận",
                    Category = ToolCategory.Thinking, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FCE4EC",
                    Tags = new[] { "IQ", "logic", "tư duy", "Raven" } },

            new() { Id = "mental_math", Name = "Tính Nhẩm Nhanh", Icon = "⚡",
                    Description = "Luyện tính nhẩm 5 phút/ngày, thi tốc độ",
                    Category = ToolCategory.Thinking, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "tính nhẩm", "mental math", "tốc độ" } },

            new() { Id = "sudoku", Name = "Sudoku Logic", Icon = "🧩",
                    Description = "4×4, 6×6, 9×9 — gợi ý, kiểm tra",
                    Category = ToolCategory.Thinking, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FCE4EC",
                    Tags = new[] { "sudoku", "logic", "puzzle", "tư duy" } },

            new() { Id = "memory_game", Name = "Lật Thẻ Nhớ", Icon = "🃏",
                    Description = "4 chủ đề: phép tính, nguyên tố, từ vựng, công thức",
                    Category = ToolCategory.Thinking, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E8F5E9",
                    Tags = new[] { "memory", "lật thẻ", "nhớ", "game" } },

            new() { Id = "chess_game", Name = "Cờ Vua Chiến Thuật", Icon = "♟️",
                    Description = "Thi đấu 2 người, Đấu với Máy (3 độ khó), giải thế cờ & rèn tư duy chiến lược",
                    Category = ToolCategory.Thinking, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FCE4EC",
                    Tags = new[] { "cờ vua", "chess", "tư duy", "chiến thuật", "AI", "logic" } },

            new() { Id = "caro_game", Name = "Cờ Caro & Gomoku Học Đường", Icon = "⚪",
                    Description = "Thi đấu Caro 15x15, Luật VN & Gomoku, Đấu AI Minimax, Giải đấu Trường học",
                    Category = ToolCategory.Thinking, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "cờ caro", "caro", "gomoku", "tư duy", "chiến thuật", "AI", "logic" } },

            // ═══════════════════════════════════════════════════════
            //  🔢 TOÁN HỌC — Bổ sung mới
            // ═══════════════════════════════════════════════════════
            new() { Id = "matrix", Name = "Ma Trận & Định Thức", Icon = "🔢",
                    Description = "Nhập ma trận 2×2, 3×3, 4×4 — Det, A⁻¹, Aᵀ, A+B, A×B, kA — hiển thị bước giải",
                    Category = ToolCategory.Math, GradeLevel = "Lớp 11-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E3F2FD",
                    Tags = new[] { "ma trận", "matrix", "định thức", "determinant", "nghịch đảo", "inverse", "GDPT 2018" } },

            // ═══════════════════════════════════════════════════════
            //  🌐 ĐA MÔN — Bổ sung mới
            // ═══════════════════════════════════════════════════════
            new() { Id = "mindmap", Name = "Sơ Đồ Tư Duy", Icon = "🧠",
                    Description = "Mindmap kéo thả — tạo nhánh, đổi màu, export PNG — template Văn học, STEM",
                    Category = ToolCategory.MultiSubject, GradeLevel = "Mọi cấp",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#FFF3E0",
                    Tags = new[] { "sơ đồ tư duy", "mindmap", "brainstorm", "tóm tắt", "ý tưởng", "kéo thả" } },

            // ═══════════════════════════════════════════════════════
            //  💼 KỸ NĂNG NGHỀ NGHIỆP (Workplace Tools)
            // ═══════════════════════════════════════════════════════
            new() { Id = "five_why", Name = "5 Why – 1 How", Icon = "🔍",
                    Description = "Tìm nguyên nhân gốc rễ bằng chuỗi 5 câu hỏi Tại sao — 4 template mẫu",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "5 why", "1 how", "root cause", "nguyên nhân", "phân tích", "kaizen" } },
            new() { Id = "swot", Name = "Phân Tích SWOT", Icon = "🎯",
                    Description = "Ma trận Điểm mạnh – Điểm yếu – Cơ hội – Thách thức — gợi ý chiến lược SO/WO/ST/WT",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "SWOT", "strengths", "weaknesses", "opportunities", "threats", "chiến lược" } },
            new() { Id = "eisenhower", Name = "Ma Trận Eisenhower", Icon = "⏰",
                    Description = "Phân loại Khẩn cấp × Quan trọng — quản lý thời gian hiệu quả",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "eisenhower", "ưu tiên", "thời gian", "urgent", "important", "ma trận" } },
            new() { Id = "fishbone", Name = "Biểu Đồ Xương Cá", Icon = "🐟",
                    Description = "Ishikawa 6M — phân loại nguyên nhân gốc rễ theo nhóm (Man, Machine, Material...)",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "ishikawa", "xương cá", "fishbone", "6M", "nguyên nhân", "phân tích" } },
            new() { Id = "pareto", Name = "Biểu Đồ Pareto", Icon = "📊",
                    Description = "Nguyên tắc 80/20 — tìm 20% nguyên nhân gây 80% vấn đề",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "pareto", "80/20", "biểu đồ", "tần suất", "ưu tiên" } },
            new() { Id = "pdca", Name = "Chu Trình PDCA", Icon = "🔄",
                    Description = "Plan-Do-Check-Act — Cải tiến liên tục theo bánh xe Deming",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "PDCA", "Deming", "cải tiến", "cycle", "plan" } },
            new() { Id = "five_s", Name = "Phương Pháp 5S", Icon = "🏠",
                    Description = "Sàng lọc - Sắp xếp - Sạch sẽ - Săn sóc - Sẵn sàng — Checklist đánh giá",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "5S", "seiri", "seiton", "tổ chức", "kỷ luật", "đánh giá" } },
            new() { Id = "kanban", Name = "Kanban Board", Icon = "📌",
                    Description = "Bảng quản lý công việc kéo thả — To Do, Doing, Done",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "kanban", "board", "task", "kéo thả", "agile", "tiến độ" } },
            new() { Id = "kpi_okr", Name = "KPI & OKR", Icon = "📈",
                    Description = "Dashboard đo lường hiệu suất — KPI target vs actual, OKR progress",
                    Category = ToolCategory.Workplace, GradeLevel = "Lớp 9-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F2F1",
                    Tags = new[] { "KPI", "OKR", "mục tiêu", "đo lường", "hiệu suất", "dashboard" } },
            new() { Id = "phet_sim", Name = "Mô phỏng PhET", Icon = "🌍",
                    Description = "Hệ thống mô phỏng tương tác Vật lý, Hóa học, Sinh học, Toán học",
                    Category = ToolCategory.PracticalApps, GradeLevel = "Lớp 1-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F7FA",
                    NavigateFormId = "F34",
                    Tags = new[] { "phet", "mô phỏng", "vật lý", "hóa học", "sinh học", "toán" } },
            new() { Id = "desmos_graph", Name = "Đồ thị Desmos", Icon = "📈",
                    Description = "Máy tính vẽ đồ thị hàm số 2D và mô hình toán học trực quan",
                    Category = ToolCategory.PracticalApps, GradeLevel = "Lớp 6-12",
                    IsInteractive = true, IsAvailable = true, ColorAccent = "#E0F7FA",
                    NavigateFormId = "F34",
                    Tags = new[] { "desmos", "đồ thị", "hàm số", "máy tính", "toán học" } },
        };

        /// <summary>Lấy tools theo category</summary>
        public static List<ToolDefinition> GetByCategory(ToolCategory category)
            => AllTools.Where(t => t.Category == category).ToList();

        /// <summary>Lấy tool theo ID</summary>
        public static ToolDefinition? GetById(string id)
            => AllTools.FirstOrDefault(t => t.Id == id);

        /// <summary>Tìm kiếm theo tên hoặc tag</summary>
        public static List<ToolDefinition> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return AllTools;
            var q = query.ToLowerInvariant();
            return AllTools.Where(t =>
                t.Name.ToLowerInvariant().Contains(q) ||
                t.Description.ToLowerInvariant().Contains(q) ||
                t.Tags.Any(tag => tag.Contains(q))
            ).ToList();
        }

        /// <summary>Đếm tools available theo category</summary>
        public static (int total, int available) CountByCategory(ToolCategory category)
        {
            var items = GetByCategory(category);
            return (items.Count, items.Count(t => t.IsAvailable));
        }
    }
}

