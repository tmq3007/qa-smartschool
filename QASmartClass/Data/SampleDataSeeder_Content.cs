using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace QASmartClass.Data
{
    // Partial class — phần mở rộng seed dữ liệu nội dung
    public static partial class SampleDataSeeder
    {
        // ═══════════════════════════════════════════════════════════
        //  3. THỜI KHÓA BIỂU + BÀI GIẢNG (Lesson)
        // ═══════════════════════════════════════════════════════════

        static void SeedTimetableAndLessons(AppDbContext db)
        {
            if (db.Lessons.Any()) return;

            // Tuần 31 (28/04 - 03/05/2026), HK2
            var lessons = new List<Lesson>();

            // ── 10A1 — Toán (GV Nguyễn Văn Hùng) ──
            AddLessons(lessons, "10A1", "Toán", "10", "Nguyễn Văn Hùng", new[]
            {
                ("Thứ 2", 2, "Bất phương trình bậc hai", "Giải BPT bậc hai bằng bảng xét dấu", "Chương 4,Lý thuyết"),
                ("Thứ 3", 3, "Bài tập BPT bậc hai", "Ôn luyện các dạng BPT", "Chương 4,Bài tập"),
                ("Thứ 4", 1, "Hệ bất phương trình", "Hệ BPT bậc nhất hai ẩn", "Chương 4,Lý thuyết"),
                ("Thứ 6", 4, "Luyện tập tổng hợp Chương 4", "Ôn tập chuẩn bị kiểm tra", "Chương 4,Ôn tập"),
            });

            // ── 10A2 — Vật lý (GV Trần Thị Mai) ──
            AddLessons(lessons, "10A2", "Vật lý", "10", "Trần Thị Mai", new[]
            {
                ("Thứ 2", 4, "Định luật bảo toàn động lượng", "Xung lượng, động lượng, va chạm", "Chương 5,Lý thuyết"),
                ("Thứ 4", 2, "Bài tập động lượng", "Các bài tập áp dụng ĐLBT động lượng", "Chương 5,Bài tập"),
                ("Thứ 5", 1, "Công và công suất", "Công cơ học, công suất", "Chương 5,Lý thuyết"),
            });

            // ── 11A1 — Hóa học (GV Lê Hoàng Nam) ──
            AddLessons(lessons, "11A1", "Hóa học", "11", "Lê Hoàng Nam", new[]
            {
                ("Thứ 3", 1, "Anđehit và xeton", "Cấu tạo, tính chất hóa học của anđehit", "Chương 8,Lý thuyết"),
                ("Thứ 4", 3, "Bài tập anđehit", "Phản ứng tráng bạc, oxi hóa", "Chương 8,Bài tập"),
                ("Thứ 6", 2, "Axit cacboxylic", "Cấu tạo, danh pháp, tính chất", "Chương 8,Lý thuyết"),
                ("Thứ 7", 1, "TN: Tính chất axit cacboxylic", "Thực hành phòng thí nghiệm", "Chương 8,Thực hành,STEAM"),
            });

            // ── 11A2 — Ngữ văn (GV Hoàng Đức Minh) ──
            AddLessons(lessons, "11A2", "Ngữ văn", "11", "Hoàng Đức Minh", new[]
            {
                ("Thứ 2", 1, "Chí Phèo — Nam Cao (P1)", "Tìm hiểu chung, bố cục tác phẩm", "Văn xuôi,Lý thuyết"),
                ("Thứ 3", 2, "Chí Phèo — Nam Cao (P2)", "Phân tích nhân vật Chí Phèo", "Văn xuôi,Phân tích"),
                ("Thứ 5", 3, "Nghị luận văn học", "Viết bài nghị luận về tác phẩm", "Làm văn,Bài tập"),
                ("Thứ 7", 2, "Đọc thêm: Đời thừa", "Đọc hiểu mở rộng — Nam Cao", "Văn xuôi,Đọc thêm"),
            });

            // ── 12A1 — Tiếng Anh (GV Vũ Thị Lan) ──
            AddLessons(lessons, "12A1", "Tiếng Anh", "12", "Vũ Thị Lan", new[]
            {
                ("Thứ 2", 3, "Unit 10: Endangered Species — Reading", "Đọc hiểu về các loài nguy cấp", "Unit 10,Reading"),
                ("Thứ 3", 4, "Unit 10: Listening & Speaking", "Luyện nghe + thảo luận nhóm", "Unit 10,Listening"),
                ("Thứ 5", 2, "Unit 10: Writing", "Viết đoạn văn nghị luận", "Unit 10,Writing"),
                ("Thứ 6", 1, "Unit 10: Language Focus", "Ngữ pháp — Mệnh đề quan hệ", "Unit 10,Grammar"),
            });

            // ── 12A2 — Tin học (GV Bùi Minh Đức) ──
            AddLessons(lessons, "12A2", "Tin học", "12", "Bùi Minh Đức", new[]
            {
                ("Thứ 2", 5, "Cơ sở dữ liệu quan hệ", "Khái niệm bảng, trường, bản ghi", "Chương 3,Lý thuyết"),
                ("Thứ 4", 4, "Thực hành: Tạo CSDL Access", "Tạo bảng, nhập dữ liệu, truy vấn", "Chương 3,Thực hành"),
                ("Thứ 6", 3, "Truy vấn SQL cơ bản", "SELECT, WHERE, ORDER BY", "Chương 3,Lý thuyết,STEAM"),
            });

            // ── Thêm bài giảng các môn phụ (chung nhiều lớp) ──
            AddLessons(lessons, "10A1", "Lịch sử", "10", "Đỗ Quang Trung", new[]
            {
                ("Thứ 5", 4, "Phong trào GPDT ở Đông Nam Á", "Giai đoạn 1918-1945", "Chương 6,Lý thuyết"),
            });
            AddLessons(lessons, "10A1", "Địa lý", "10", "Ngô Thị Hạnh", new[]
            {
                ("Thứ 2", 1, "Khí quyển — Nhiệt độ không khí", "Sự phân bố nhiệt trên Trái Đất", "Chương 3,Lý thuyết"),
                ("Thứ 4", 4, "TH: Phân tích biểu đồ khí hậu", "Đọc và nhận xét biểu đồ", "Chương 3,Thực hành"),
            });
            AddLessons(lessons, "10A1", "Sinh học", "10", "Phạm Thị Hương", new[]
            {
                ("Thứ 3", 2, "Enzim và vai trò của enzim", "Cấu trúc, cơ chế tác dụng", "Chương 3,Lý thuyết"),
                ("Thứ 5", 3, "TH: Ảnh hưởng nhiệt độ đến enzim", "Thí nghiệm catalase", "Chương 3,STEAM"),
            });
            AddLessons(lessons, "11A1", "Tiếng Anh", "11", "Vũ Thị Lan", new[]
            {
                ("Thứ 2", 3, "Unit 12: The Asian Games — Reading", "Đọc hiểu về ASIAD", "Unit 12,Reading"),
                ("Thứ 5", 1, "Unit 12: Writing", "Viết thư trang trọng", "Unit 12,Writing"),
            });
            AddLessons(lessons, "11A1", "Toán", "11", "Nguyễn Văn Hùng", new[]
            {
                ("Thứ 4", 2, "Giới hạn của dãy số", "Định nghĩa, quy tắc tính giới hạn", "Chương 4,Lý thuyết"),
                ("Thứ 6", 4, "Bài tập giới hạn dãy số", "Các dạng bài tập cơ bản và nâng cao", "Chương 4,Bài tập"),
            });

            // Thêm 1 bài cũ đã dạy
            lessons.Add(new Lesson
            {
                Title = "Ôn tập giữa HK2 — Toán 10",
                Subject = "Toán", Grade = "10", ClassName = "10A1",
                TeacherName = "Nguyễn Văn Hùng",
                DayOfWeek = "Thứ 6", Period = 4, WeekNumber = 28, Semester = "HK2",
                Status = "Taught", LessonType = "Normal", DurationMinutes = 45,
                Tags = "Ôn tập,Giữa kỳ", UseCount = 1,
                LastTaughtAt = new DateTime(2026, 4, 10, 9, 0, 0),
                HasHomework = true,
                HomeworkText = "Hoàn thành đề ôn tập trong SBT trang 85-88. Nộp trước thứ 4 tuần sau.",
                Description = "Ôn tập tổng hợp các chương 1-3",
                CreatedAt = new DateTime(2026, 4, 8), UpdatedAt = new DateTime(2026, 4, 10)
            });

            // ═══════════════════════════════════════════════════════════
            //  BÀI GIẢNG MẪU HOÀN CHỈNH — CÓ NỘI DUNG SLIDES
            //  Toán 10A1: "Bất phương trình bậc hai" (8 content blocks)
            // ═══════════════════════════════════════════════════════════
            var demoLesson = new Lesson
            {
                Title = "Bất phương trình bậc hai một ẩn",
                Subject = "Toán", Grade = "10", ClassName = "10A1",
                TeacherName = "Nguyễn Văn Hùng",
                DayOfWeek = "Thứ 2", Period = 2, WeekNumber = 31, Semester = "HK2",
                Status = "Approved", LessonType = "Normal", DurationMinutes = 45,
                Tags = "Chương 4,Lý thuyết,Có mô phỏng,Bài mẫu",
                HasHomework = true,
                HomeworkText = "Bài tập SGK trang 112: Bài 1, 2, 3, 5.\nBài tập SBT trang 89: Bài 4.1 → 4.5.\nNộp bài trước thứ 4 tuần này.",
                Description = "Bài giảng mẫu đầy đủ nội dung — Giải BPT bậc hai bằng bảng xét dấu tam thức bậc hai. Bao gồm lý thuyết, ví dụ minh họa, mô phỏng tương tác và bài tập vận dụng.",
                CreatedAt = new DateTime(2026, 4, 25),
                UpdatedAt = new DateTime(2026, 4, 28),
                Contents = new List<LessonContent>
                {
                    // ── Slide 1: Mục tiêu bài học ──
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 1,
                        Data = "MỤC TIÊU BÀI HỌC\n\n" +
                               "✔ Nắm vững định nghĩa bất phương trình bậc hai một ẩn.\n" +
                               "✔ Biết cách lập bảng xét dấu tam thức bậc hai f(x) = ax² + bx + c.\n" +
                               "✔ Vận dụng bảng xét dấu để giải BPT bậc hai.\n" +
                               "✔ Liên hệ đồ thị parabol với tập nghiệm BPT.\n\n" +
                               "→ Thời lượng: 45 phút\n" +
                               "→ Hình thức: Lý thuyết + Ví dụ minh họa + Mô phỏng tương tác"
                    },

                    // ── Slide 2: Nhắc lại kiến thức ──
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 2,
                        Data = "NHẮC LẠI: TAM THỨC BẬC HAI\n\n" +
                               "Cho tam thức bậc hai f(x) = ax² + bx + c (a ≠ 0)\n\n" +
                               "• Δ = b² − 4ac là biệt thức (discriminant)\n" +
                               "• Nếu Δ > 0: f(x) có 2 nghiệm phân biệt x₁, x₂\n" +
                               "• Nếu Δ = 0: f(x) có nghiệm kép x₀ = −b/(2a)\n" +
                               "• Nếu Δ < 0: f(x) vô nghiệm, f(x) cùng dấu với a ∀x\n\n" +
                               "BẢNG XÉT DẤU f(x) = ax² + bx + c (khi Δ > 0, a > 0):\n" +
                               "─────────────────────────────────────────\n" +
                               "  x    │  −∞      x₁      x₂      +∞\n" +
                               "─────────────────────────────────────────\n" +
                               " f(x)  │   +    0   −    0   +\n" +
                               "─────────────────────────────────────────\n\n" +
                               "→ f(x) trái dấu với a trong khoảng (x₁, x₂)\n" +
                               "→ f(x) cùng dấu với a ngoài khoảng (x₁, x₂)"
                    },

                    // ── Slide 3: Định lý dấu tam thức bậc hai ──
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 3,
                        Data = "ĐỊNH LÝ VỀ DẤU TAM THỨC BẬC HAI\n\n" +
                               "Cho f(x) = ax² + bx + c (a ≠ 0), Δ = b² − 4ac\n\n" +
                               "📌 Trường hợp 1: Δ < 0\n" +
                               "→ f(x) cùng dấu với a, ∀x ∈ ℝ\n" +
                               "→ VD: f(x) = x² + x + 1 > 0, ∀x (vì a=1>0, Δ=−3<0)\n\n" +
                               "📌 Trường hợp 2: Δ = 0\n" +
                               "→ f(x) cùng dấu với a, ∀x ≠ x₀ = −b/(2a)\n" +
                               "→ f(x₀) = 0\n\n" +
                               "📌 Trường hợp 3: Δ > 0 (x₁ < x₂)\n" +
                               "→ f(x) cùng dấu với a khi x < x₁ hoặc x > x₂\n" +
                               "→ f(x) trái dấu với a khi x₁ < x < x₂\n" +
                               "→ f(x₁) = f(x₂) = 0\n\n" +
                               "⚡ GHI NHỚ: \"Trong trái — ngoài cùng\" (trong khoảng 2 nghiệm → trái dấu a)"
                    },

                    // ── Slide 4: Ví dụ 1 ──
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 4,
                        Data = "VÍ DỤ 1: Giải BPT x² − 5x + 6 > 0\n\n" +
                               "Bước 1: Tìm nghiệm\n" +
                               "  Δ = 25 − 24 = 1 > 0\n" +
                               "  x₁ = (5−1)/2 = 2\n" +
                               "  x₂ = (5+1)/2 = 3\n\n" +
                               "Bước 2: Lập bảng xét dấu (a = 1 > 0)\n" +
                               "─────────────────────────────────────────\n" +
                               "  x    │  −∞       2        3       +∞\n" +
                               "─────────────────────────────────────────\n" +
                               " f(x)  │   +     0   −     0    +\n" +
                               "─────────────────────────────────────────\n\n" +
                               "Bước 3: Kết luận\n" +
                               "  f(x) > 0 khi f(x) dương\n" +
                               "  → Tập nghiệm: S = (−∞, 2) ∪ (3, +∞)\n\n" +
                               "✅ Kiểm tra: f(0) = 6 > 0 ✓  |  f(2.5) = −0.25 < 0 ✓"
                    },

                    // ── Slide 5: Ví dụ 2 ──
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 5,
                        Data = "VÍ DỤ 2: Giải BPT 2x² − 3x − 2 ≤ 0\n\n" +
                               "Bước 1: Tìm nghiệm\n" +
                               "  a = 2, b = −3, c = −2\n" +
                               "  Δ = 9 + 16 = 25 > 0\n" +
                               "  x₁ = (3−5)/4 = −1/2\n" +
                               "  x₂ = (3+5)/4 = 2\n\n" +
                               "Bước 2: Bảng xét dấu (a = 2 > 0)\n" +
                               "─────────────────────────────────────────\n" +
                               "  x    │  −∞      −1/2       2       +∞\n" +
                               "─────────────────────────────────────────\n" +
                               " f(x)  │   +     0    −     0    +\n" +
                               "─────────────────────────────────────────\n\n" +
                               "Bước 3: f(x) ≤ 0 → lấy phần âm + gốc\n" +
                               "  → Tập nghiệm: S = [−1/2, 2]\n\n" +
                               "📝 LƯU Ý: Dấu ≤ (có bằng) → lấy cả 2 nghiệm (ngoặc vuông)"
                    },

                    // ── Slide 6: Mô phỏng tương tác PhET ──
                    new LessonContent
                    {
                        ContentType = "Simulation", SortOrder = 6,
                        Data = "https://phet.colorado.edu/sims/html/graphing-quadratics/latest/graphing-quadratics_all.html"
                    },

                    // ── Slide 7: Bài tập tự luyện ──
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 7,
                        Data = "BÀI TẬP VẬN DỤNG — Làm tại lớp (5 phút)\n\n" +
                               "Bài 1: Giải BPT x² − 4 > 0\n" +
                               "  Đáp án: S = (−∞, −2) ∪ (2, +∞)\n\n" +
                               "Bài 2: Giải BPT −x² + 2x + 3 ≥ 0\n" +
                               "  Gợi ý: a = −1 < 0, nhân 2 vế với (−1) đổi dấu\n" +
                               "  Đáp án: S = [−1, 3]\n\n" +
                               "Bài 3: Tìm x để f(x) = x² + 1 < 0\n" +
                               "  Đáp án: Δ = −4 < 0, a = 1 > 0 → f(x) > 0 ∀x → S = ∅\n\n" +
                               "Bài 4 (Nâng cao): Tìm m để BPT x² − 2mx + m + 2 > 0, ∀x ∈ ℝ\n" +
                               "  Gợi ý: a = 1 > 0, cần Δ < 0\n" +
                               "  → 4m² − 4(m+2) < 0 → m² − m − 2 < 0 → (m−2)(m+1) < 0\n" +
                               "  Đáp án: m ∈ (−1, 2)"
                    },

                    // ── Slide 8: Quiz nhúng ──
                    new LessonContent
                    {
                        ContentType = "Quiz", SortOrder = 8,
                        Data = "Quiz: Kiểm tra 15p — BPT bậc hai"
                    },
                }
            };
            lessons.Add(demoLesson);

            // ═══════════════════════════════════════════════════════════
            //  BÀI GIẢNG MẪU 2 — Hóa 11A1: Anđehit và Xeton (6 blocks)
            // ═══════════════════════════════════════════════════════════
            var demoLesson2 = new Lesson
            {
                Title = "Anđehit và Xeton — Tính chất hóa học",
                Subject = "Hóa học", Grade = "11", ClassName = "11A1",
                TeacherName = "Lê Hoàng Nam",
                DayOfWeek = "Thứ 3", Period = 1, WeekNumber = 31, Semester = "HK2",
                Status = "Approved", LessonType = "Normal", DurationMinutes = 45,
                Tags = "Chương 8,Lý thuyết,Có mô phỏng,Bài mẫu",
                HasHomework = true,
                HomeworkText = "Bài tập SGK trang 203: Bài 1, 2, 4.\nBài tập SBT: 8.5 → 8.10.\nLàm thí nghiệm ảo trên PhET.\nNộp trước thứ 6.",
                Description = "Bài giảng mẫu — Tính chất hóa học của anđehit: phản ứng cộng H₂, phản ứng oxi hóa (tráng bạc), và so sánh với xeton.",
                CreatedAt = new DateTime(2026, 4, 25),
                UpdatedAt = new DateTime(2026, 4, 28),
                Contents = new List<LessonContent>
                {
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 1,
                        Data = "MỤC TIÊU BÀI HỌC — ANĐEHIT VÀ XETON\n\n" +
                               "✔ Biết cấu tạo, phân loại anđehit và xeton\n" +
                               "✔ Nắm vững tính chất hóa học: phản ứng cộng, phản ứng oxi hóa\n" +
                               "✔ Phân biệt anđehit với xeton bằng phản ứng tráng bạc\n" +
                               "✔ Viết và cân bằng phương trình phản ứng\n\n" +
                               "→ Thời lượng: 45 phút\n" +
                               "→ Dụng cụ: SGK, vở ghi, máy tính"
                    },
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 2,
                        Data = "CẤU TẠO ANĐEHIT VÀ XETON\n\n" +
                               "📌 ANĐEHIT (Aldehyde)\n" +
                               "• Nhóm chức: −CHO (nhóm cacbonyl liên kết với H)\n" +
                               "• Công thức tổng quát: R−CHO (R là H hoặc gốc hidrocacbon)\n" +
                               "• VD: HCHO (fomic), CH₃CHO (axetic), C₂H₅CHO (propionic)\n\n" +
                               "📌 XETON (Ketone)\n" +
                               "• Nhóm chức: >C=O (nhóm cacbonyl liên kết 2 gốc R)\n" +
                               "• Công thức: R−CO−R'\n" +
                               "• VD: CH₃COCH₃ (axeton), C₂H₅COCH₃ (metyletylxeton)\n\n" +
                               "⚡ KHÁC BIỆT QUAN TRỌNG:\n" +
                               "→ Anđehit có H liên kết C=O → tham gia phản ứng tráng bạc\n" +
                               "→ Xeton KHÔNG có H → KHÔNG tráng bạc"
                    },
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 3,
                        Data = "TÍNH CHẤT HÓA HỌC — ANĐEHIT\n\n" +
                               "I. PHẢN ỨNG CỘNG H₂ (khử)\n" +
                               "  CH₃CHO + H₂ →(Ni,t°) CH₃CH₂OH\n" +
                               "  (Anđehit axetic → Etanol)\n\n" +
                               "II. PHẢN ỨNG OXI HÓA\n\n" +
                               "  a) Tráng bạc (dùng AgNO₃/NH₃):\n" +
                               "  CH₃CHO + 2AgNO₃ + 3NH₃ + H₂O → CH₃COONH₄ + 2Ag↓ + 2NH₄NO₃\n" +
                               "  → Ag kết tủa tạo lớp gương bạc sáng bóng\n" +
                               "  → Tỉ lệ: 1 mol anđehit → 2 mol Ag\n" +
                               "  → ĐẶC BIỆT: HCHO → 4 mol Ag (vì có 2 nhóm −CHO)\n\n" +
                               "  b) Oxi hóa bằng O₂ (xúc tác):\n" +
                               "  2CH₃CHO + O₂ →(xt) 2CH₃COOH\n" +
                               "  (Anđehit axetic → Axit axetic)\n\n" +
                               "  c) Với Cu(OH)₂/NaOH (đun nóng):\n" +
                               "  CH₃CHO + 2Cu(OH)₂ →(t°) CH₃COOH + Cu₂O↓ (đỏ gạch) + 2H₂O"
                    },
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 4,
                        Data = "VÍ DỤ MINH HỌA\n\n" +
                               "Bài 1: Cho 4.4g CH₃CHO tác dụng hết với AgNO₃/NH₃ dư.\n" +
                               "Tính khối lượng Ag thu được?\n\n" +
                               "Giải:\n" +
                               "  n(CH₃CHO) = 4.4/44 = 0.1 mol\n" +
                               "  CH₃CHO → 2Ag\n" +
                               "  n(Ag) = 2 × 0.1 = 0.2 mol\n" +
                               "  m(Ag) = 0.2 × 108 = 21.6g\n\n" +
                               "  ✅ Đáp án: 21.6g Ag\n\n" +
                               "Bài 2: Cho 3g HCHO phản ứng tráng bạc. Tính m(Ag)?\n\n" +
                               "Giải:\n" +
                               "  n(HCHO) = 3/30 = 0.1 mol\n" +
                               "  HCHO → 4Ag (đặc biệt!)\n" +
                               "  n(Ag) = 4 × 0.1 = 0.4 mol\n" +
                               "  m(Ag) = 0.4 × 108 = 43.2g\n\n" +
                               "  ✅ Đáp án: 43.2g Ag"
                    },
                    new LessonContent
                    {
                        ContentType = "Simulation", SortOrder = 5,
                        Data = "https://phet.colorado.edu/sims/html/molecule-shapes/latest/molecule-shapes_all.html"
                    },
                    new LessonContent
                    {
                        ContentType = "Quiz", SortOrder = 6,
                        Data = "Quiz: Anđehit & Xeton — 5 câu — Competition"
                    },
                }
            };
            lessons.Add(demoLesson2);

            // ═══════════════════════════════════════════════════════════
            //  BÀI GIẢNG MẪU 3 — Tiếng Anh 12A1: Unit 10 Reading
            // ═══════════════════════════════════════════════════════════
            var demoLesson3 = new Lesson
            {
                Title = "Unit 10: Endangered Species — Reading",
                Subject = "Tiếng Anh", Grade = "12", ClassName = "12A1",
                TeacherName = "Vũ Thị Lan",
                DayOfWeek = "Thứ 2", Period = 3, WeekNumber = 31, Semester = "HK2",
                Status = "Approved", LessonType = "Normal", DurationMinutes = 45,
                Tags = "Unit 10,Reading,Có quiz,Bài mẫu",
                HasHomework = true,
                HomeworkText = "Learn new vocabulary (10 words).\nAnswer comprehension questions in workbook p.72.\nWrite a short paragraph about an endangered animal (80-100 words).",
                Description = "Reading comprehension lesson about endangered species with vocabulary building and discussion activities.",
                CreatedAt = new DateTime(2026, 4, 25),
                UpdatedAt = new DateTime(2026, 4, 28),
                Contents = new List<LessonContent>
                {
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 1,
                        Data = "UNIT 10: ENDANGERED SPECIES — READING\n\n" +
                               "LESSON OBJECTIVES\n" +
                               "✔ Read and understand a passage about endangered species\n" +
                               "✔ Learn 10+ vocabulary words related to wildlife conservation\n" +
                               "✔ Practice reading comprehension strategies (scanning, skimming)\n" +
                               "✔ Discuss causes and solutions for species extinction\n\n" +
                               "→ Duration: 45 minutes\n" +
                               "→ Materials: Textbook p.108-110, Workbook p.72"
                    },
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 2,
                        Data = "PRE-READING: VOCABULARY\n\n" +
                               "📌 KEY WORDS — Match the word with its meaning:\n\n" +
                               "1. endangered (adj) — at risk of becoming extinct\n" +
                               "2. extinction (n) — the complete disappearance of a species\n" +
                               "3. habitat (n) — the natural environment where an animal lives\n" +
                               "4. conservation (n) — the protection of natural resources\n" +
                               "5. poaching (n) — illegal hunting of wild animals\n" +
                               "6. biodiversity (n) — the variety of plant and animal life\n" +
                               "7. ecosystem (n) — a community of living organisms\n" +
                               "8. deforestation (n) — cutting down large areas of forest\n" +
                               "9. breeding program (n) — organized effort to produce offspring\n" +
                               "10. wildlife reserve (n) — protected area for wild animals\n\n" +
                               "→ TASK: Use 5 new words to make your own sentences."
                    },
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 3,
                        Data = "WHILE-READING: COMPREHENSION\n\n" +
                               "📖 Read the passage on page 108-110, then answer:\n\n" +
                               "Task 1 — TRUE or FALSE:\n" +
                               "a) There are about 41,000 endangered species worldwide. → ___\n" +
                               "b) Habitat loss is the biggest threat to wildlife. → ___\n" +
                               "c) All endangered species are large animals. → ___\n" +
                               "d) Zoos play no role in conservation. → ___\n\n" +
                               "Task 2 — Answer the questions:\n" +
                               "1. What are the main causes of species extinction?\n" +
                               "2. How do breeding programs help endangered animals?\n" +
                               "3. What can individuals do to protect wildlife?\n" +
                               "4. Why is biodiversity important for humans?\n\n" +
                               "Task 3 — Find words in the passage that mean:\n" +
                               "a) to die out completely = ___________\n" +
                               "b) animals living in the wild = ___________\n" +
                               "c) to make something less dangerous = ___________"
                    },
                    new LessonContent
                    {
                        ContentType = "Text", SortOrder = 4,
                        Data = "POST-READING: DISCUSSION\n\n" +
                               "🗣️ GROUP WORK (4 students/group, 5 minutes)\n\n" +
                               "Discuss and present:\n\n" +
                               "1. Name 3 endangered species in Vietnam.\n" +
                               "   → Hint: Saola, Javan Rhino, Indochinese Tiger...\n\n" +
                               "2. What should the Vietnamese government do to protect them?\n\n" +
                               "3. What can YOU do as a student?\n" +
                               "   → Ideas: Reduce plastic, plant trees, raise awareness...\n\n" +
                               "📝 WRITING TASK (Homework):\n" +
                               "Write a paragraph (80-100 words) about ONE endangered animal.\n" +
                               "Include: name, habitat, threats, conservation efforts."
                    },
                    new LessonContent
                    {
                        ContentType = "Quiz", SortOrder = 5,
                        Data = "Quiz: Vocabulary — Endangered Species"
                    },
                }
            };
            lessons.Add(demoLesson3);

            db.Lessons.AddRange(lessons);
            db.SaveChanges();
            Log.Information("[Seed] Đã tạo {Count} bài giảng / thời khóa biểu ({Demo} bài có nội dung mẫu)", lessons.Count, 3);
        }

        static void AddLessons(List<Lesson> list, string cls, string subject, string grade,
            string teacher, (string Day, int Period, string Title, string Desc, string Tags)[] items)
        {
            int week = 31; // tuần 31
            foreach (var (day, period, title, desc, tags) in items)
            {
                list.Add(new Lesson
                {
                    Title = title, Subject = subject, Grade = grade,
                    ClassName = cls, TeacherName = teacher,
                    DayOfWeek = day, Period = period, WeekNumber = week, Semester = "HK2",
                    Status = "Approved", LessonType = tags.Contains("STEAM") ? "STEAM" : "Normal",
                    DurationMinutes = tags.Contains("Thực hành") || tags.Contains("STEAM") ? 90 : 45,
                    Tags = tags, Description = desc,
                    CreatedAt = new DateTime(2026, 4, 25),
                    UpdatedAt = new DateTime(2026, 4, 28)
                });
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  4. QUIZ + CÂU HỎI
        // ═══════════════════════════════════════════════════════════

        static void SeedQuizzesAndQuestions(AppDbContext db)
        {
            if (db.Quizzes.Any()) return;

            // Quiz 1: Toán 10 — BPT bậc hai
            var q1 = new Quiz { Title = "Kiểm tra 15p — BPT bậc hai", QuizType = "Test", TimeLimitSeconds = 900, CreatedAt = new DateTime(2026, 4, 28) };
            q1.Questions = new List<Question>
            {
                MakeQ(1, "Tập nghiệm của BPT x² - 5x + 6 > 0 là?",
                    "[\"(-∞, 2) ∪ (3, +∞)\",\"(2, 3)\",\"[-∞, 2] ∪ [3, +∞]\",\"Vô nghiệm\"]", "A", 10, "Medium"),
                MakeQ(2, "BPT nào sau đây vô nghiệm?",
                    "[\"x² + 1 < 0\",\"x² - 4 > 0\",\"x² ≥ 0\",\"x² + x > -1\"]", "A", 10, "Easy"),
                MakeQ(3, "Giải BPT: 2x² - 3x - 2 ≤ 0",
                    "[\"[-1/2, 2]\",\"(-∞, -1/2] ∪ [2, +∞)\",\"(-1/2, 2)\",\"[1/2, 2]\"]", "A", 10, "Medium"),
                MakeQ(4, "Số nghiệm nguyên của BPT x² < 9 là?",
                    "[\"5\",\"4\",\"6\",\"3\"]", "A", 10, "Easy"),
                MakeQ(5, "Tìm m để BPT x² - 2mx + m + 2 > 0 ∀x?",
                    "[\"m ∈ (-1, 2)\",\"m > 2\",\"m < -1\",\"m ∈ [-1, 2]\"]", "A", 15, "Hard"),
            };
            db.Quizzes.Add(q1);

            // Quiz 2: Hóa 11 — Anđehit
            var q2 = new Quiz { Title = "Quiz nhanh — Anđehit & Xeton", QuizType = "Competition", TimeLimitSeconds = 300, CreatedAt = new DateTime(2026, 4, 29) };
            q2.Questions = new List<Question>
            {
                MakeQ(1, "CTPT của anđehit fomic là?",
                    "[\"HCHO\",\"CH₃CHO\",\"C₂H₅CHO\",\"HCOOH\"]", "A", 10, "Easy"),
                MakeQ(2, "Phản ứng tráng bạc dùng thuốc thử nào?",
                    "[\"AgNO₃/NH₃\",\"Cu(OH)₂\",\"NaOH\",\"HCl\"]", "A", 10, "Easy"),
                MakeQ(3, "Anđehit axetic tác dụng AgNO₃/NH₃ theo tỉ lệ mol?",
                    "[\"1:2\",\"1:1\",\"2:1\",\"1:4\"]", "A", 10, "Medium"),
                MakeQ(4, "Chất nào không có phản ứng tráng bạc?",
                    "[\"Axeton\",\"HCHO\",\"HCOOH\",\"Glucozơ\"]", "A", 10, "Medium"),
                MakeQ(5, "Oxi hóa ancol bậc 1 bằng CuO thu được?",
                    "[\"Anđehit\",\"Xeton\",\"Axit\",\"Este\"]", "A", 10, "Easy"),
            };
            db.Quizzes.Add(q2);

            // Quiz 3: Tiếng Anh 12 — Unit 10
            var q3 = new Quiz { Title = "Vocabulary Quiz — Endangered Species", QuizType = "Competition", TimeLimitSeconds = 300, CreatedAt = new DateTime(2026, 4, 28) };
            q3.Questions = new List<Question>
            {
                MakeQ(1, "\"Endangered\" means ___",
                    "[\"At risk of extinction\",\"Very dangerous\",\"Protected by law\",\"Common\"]", "A", 10, "Easy"),
                MakeQ(2, "Which animal is NOT endangered?",
                    "[\"Pigeon\",\"Giant Panda\",\"Javan Rhino\",\"Amur Leopard\"]", "A", 10, "Easy"),
                MakeQ(3, "The relative clause in: 'The tiger ___ lives in Asia is endangered'",
                    "[\"which\",\"who\",\"whom\",\"whose\"]", "A", 10, "Medium"),
                MakeQ(4, "'Conservation' is closest in meaning to ___",
                    "[\"Protection\",\"Destruction\",\"Pollution\",\"Evolution\"]", "A", 10, "Easy"),
                MakeQ(5, "Choose the correct sentence:",
                    "[\"The species that are endangered need protection.\",\"The species who are endangered need protection.\",\"The species which is endangered need protection.\",\"The species whom are endangered need protection.\"]",
                    "A", 10, "Medium"),
            };
            db.Quizzes.Add(q3);

            db.SaveChanges();
            Log.Information("[Seed] Đã tạo {Count} quiz", 3);
        }

        static Question MakeQ(int order, string content, string options, string answer, int points, string diff) => new()
        {
            Content = content, OptionsJson = options, CorrectAnswer = answer,
            Points = points, Difficulty = diff, SortOrder = order, QuestionType = "MultipleChoice"
        };

        // ═══════════════════════════════════════════════════════════
        //  5. NGÂN HÀNG CÂU HỎI
        // ═══════════════════════════════════════════════════════════

        static void SeedQuestionBank(AppDbContext db)
        {
            if (db.QuestionBankCategories.Any()) return;

            var cats = new (string Name, string Subject, string Grade, string Desc, (string Content, string Opts, string Ans, string Diff, string Tags)[] Qs)[]
            {
                ("Chương 4: BPT bậc hai", "Toán", "10", "Bất phương trình bậc nhất, bậc hai một ẩn", new[]
                {
                    ("Giải BPT: x² - 4x + 3 < 0", "[\"(1,3)\",\"(-∞,1)∪(3,+∞)\",\"[1,3]\",\"Vô nghiệm\"]", "A", "Easy", "BPT,Bậc hai"),
                    ("Tìm m để x²+2mx+m²-1>0 ∀x", "[\"Không tồn tại m\",\"m∈R\",\"m>1\",\"m<-1\"]", "A", "Hard", "Tham số,Nâng cao"),
                    ("Biểu diễn tập nghiệm BPT x²≤4 trên trục số", "[\"[-2,2]\",\"(-2,2)\",\"R\\{-2,2}\",\"(-∞,-2)∪(2,+∞)\"]", "A", "Easy", "Biểu diễn"),
                }),
                ("Chương 8: Anđehit — Axit cacboxylic", "Hóa học", "11", "Tính chất hóa học nhóm chức -CHO, -COOH", new[]
                {
                    ("Sản phẩm khi oxi hóa CH₃CHO bằng O₂ (xt) là?", "[\"CH₃COOH\",\"CO₂+H₂O\",\"HCHO\",\"C₂H₅OH\"]", "A", "Easy", "Anđehit,Oxi hóa"),
                    ("Axit axetic có thể phản ứng với chất nào?", "[\"NaOH, Na, C₂H₅OH\",\"NaCl, KOH\",\"Cu, HCl\",\"CO₂, H₂O\"]", "A", "Medium", "Axit,Phản ứng"),
                    ("Cho 0.1 mol HCHO tác dụng AgNO₃/NH₃ dư, khối lượng Ag thu được?", "[\"43.2g\",\"21.6g\",\"10.8g\",\"32.4g\"]", "A", "Hard", "Tính toán,Tráng bạc"),
                }),
                ("Unit 10: Endangered Species", "Tiếng Anh", "12", "Vocabulary, Reading, Grammar — Relative clauses", new[]
                {
                    ("'Habitat' means ___", "[\"Natural living place\",\"Food source\",\"Hunting area\",\"Migration route\"]", "A", "Easy", "Vocabulary"),
                    ("The ___ of many species is caused by deforestation", "[\"extinction\",\"evolution\",\"migration\",\"reproduction\"]", "A", "Medium", "Vocabulary"),
                    ("Choose correct: The park ___ was established in 1990 protects rare birds.", "[\"which\",\"who\",\"whom\",\"where\"]", "A", "Medium", "Grammar,Relative clause"),
                }),
                ("Chương 5: Động lượng — Công", "Vật lý", "10", "Định luật bảo toàn động lượng, công cơ học", new[]
                {
                    ("Đơn vị của động lượng là?", "[\"kg.m/s\",\"N.m\",\"J\",\"W\"]", "A", "Easy", "Đơn vị"),
                    ("Vật 2kg chuyển động 3m/s, động lượng bằng?", "[\"6 kg.m/s\",\"3 kg.m/s\",\"5 kg.m/s\",\"1.5 kg.m/s\"]", "A", "Easy", "Tính toán"),
                    ("ĐLBT động lượng áp dụng khi?", "[\"Hệ cô lập\",\"Có ngoại lực\",\"Mọi trường hợp\",\"Vật đứng yên\"]", "A", "Medium", "Lý thuyết"),
                }),
                ("Chương 3: CSDL quan hệ", "Tin học", "12", "Bảng, trường, bản ghi, truy vấn SQL", new[]
                {
                    ("Thành phần cơ bản của CSDL quan hệ là?", "[\"Bảng (Table)\",\"Form\",\"Report\",\"Module\"]", "A", "Easy", "Khái niệm"),
                    ("Lệnh SQL nào dùng để truy vấn dữ liệu?", "[\"SELECT\",\"INSERT\",\"UPDATE\",\"DELETE\"]", "A", "Easy", "SQL"),
                    ("Khóa chính (Primary Key) dùng để?", "[\"Xác định duy nhất bản ghi\",\"Sắp xếp dữ liệu\",\"Liên kết bảng\",\"Mã hóa dữ liệu\"]", "A", "Medium", "Khái niệm"),
                }),
            };

            foreach (var (name, subj, gr, desc, qs) in cats)
            {
                var cat = new QuestionBankCategory { Name = name, Subject = subj, Grade = gr, Description = desc };
                cat.Items = qs.Select((q, i) => new QuestionBankItem
                {
                    Content = q.Content, OptionsJson = q.Opts, CorrectAnswer = q.Ans,
                    Difficulty = q.Diff, Tags = q.Tags, Subject = subj, Grade = gr,
                    Points = q.Diff == "Hard" ? 15 : 10, TimeLimitSeconds = q.Diff == "Hard" ? 90 : 60,
                    QuestionType = "MCQ"
                }).ToList();
                db.QuestionBankCategories.Add(cat);
            }
            db.SaveChanges();
            Log.Information("[Seed] Đã tạo {Count} danh mục NHCH", cats.Length);
        }

        // ═══════════════════════════════════════════════════════════
        //  6. NHẬT KÝ SỰ KIỆN
        // ═══════════════════════════════════════════════════════════

        static void SeedEventLogs(AppDbContext db)
        {
            if (db.EventLogs.Any()) return;

            var now = DateTime.Now;
            var logs = new EventLog[]
            {
                new() { EventType = "SYSTEM", Actor = "System", Details = "Hệ thống khởi tạo dữ liệu mẫu THPT Quang Ân thành công", Timestamp = now.AddHours(-5) },
                new() { EventType = "LOGIN", Actor = "GV Nguyễn Văn Hùng", Details = "Đăng nhập — Phòng STEM, IP: 192.168.1.100", Timestamp = now.AddHours(-4) },
                new() { EventType = "ROSTER_SWITCH", Actor = "GV", Details = "Chuyển lớp: 10A1 - Toán → 11A1 - Hóa học", Timestamp = now.AddHours(-3.5) },
                new() { EventType = "ATTENDANCE", Actor = "GV", Details = "Điểm danh lớp 11A1: 36/38 có mặt, 1 trễ, 1 vắng (Châu Thị Ánh Nguyệt - P)", Timestamp = now.AddHours(-3) },
                new() { EventType = "BROADCAST_START", Actor = "GV", Details = "Chiếu màn hình bắt đầu — Bài: Anđehit và xeton — 38 HS", Timestamp = now.AddHours(-2.8) },
                new() { EventType = "LESSON_OPEN", Actor = "GV", Details = "Mở bài giảng: Anđehit và xeton (Hóa 11, Chương 8)", Timestamp = now.AddHours(-2.7) },
                new() { EventType = "QUIZ_START", Actor = "GV", Details = "Quiz: Anđehit & Xeton — 5 câu — Mode: Competition — 38 HS", Timestamp = now.AddHours(-2) },
                new() { EventType = "QUIZ_END", Actor = "GV", Details = "Quiz kết thúc — TB: 78% — Top: Trần Minh Quân (100%)", Timestamp = now.AddHours(-1.8) },
                new() { EventType = "FILE_TRANSFER", Actor = "GV", Details = "Phát bài tập: BT_Andehit_C8.pdf → 38 HS — Hoàn tất", Timestamp = now.AddHours(-1.5) },
                new() { EventType = "BROADCAST_STOP", Actor = "GV", Details = "Tắt chiếu màn hình — Thời gian: 45 phút", Timestamp = now.AddHours(-1.2) },
                new() { EventType = "ROSTER_SWITCH", Actor = "GV", Details = "Chuyển lớp: 11A1 - Hóa học → 12A1 - Tiếng Anh", Timestamp = now.AddHours(-1) },
                new() { EventType = "ATTENDANCE", Actor = "GV", Details = "Tự động điểm danh 12A1: 40/42 online, 2 offline (vắng)", Timestamp = now.AddHours(-0.9) },
                new() { EventType = "WEB_PUSH", Actor = "GV", Details = "Mở website: https://dictionary.cambridge.org trên 40 máy HS", Timestamp = now.AddHours(-0.5) },
                new() { EventType = "LOGOUT", Actor = "GV Nguyễn Văn Hùng", Details = "Đăng xuất — Tổng phiên: 4h — 3 lớp — 2 quiz", Timestamp = now },
            };

            db.EventLogs.AddRange(logs);
            db.SaveChanges();
            Log.Information("[Seed] Đã tạo {Count} event logs", logs.Length);
        }
    }
}
