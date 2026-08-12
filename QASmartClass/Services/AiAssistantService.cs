using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class AiAssistantService
    {
        // Đây là STUB service - Dữ liệu trả về là giả lập.
        // Sau này sẽ được thay thế bằng API gọi đến các LLM thật (OpenAI, Gemini...)

        private readonly Data.AppDbContext? _db;
        private static readonly Random _rng = new();

        public AiAssistantService() { }
        public AiAssistantService(Data.AppDbContext db) { _db = db; }

        private static readonly string[][] LessonPlanTemplates = new[]
        {
            new[] { "5E", "I. MỤC TIÊU:\n- Hiểu khái niệm cốt lõi\n\nII. TIẾN TRÌNH 5E:\n1. Engage (5p): Câu hỏi kích thích\n2. Explore (10p): Thí nghiệm nhóm\n3. Explain (10p): Trình bày kết quả\n4. Elaborate (10p): Mở rộng\n5. Evaluate (10p): Đánh giá" },
            new[] { "Tích hợp", "I. MỤC TIÊU:\n- Tích hợp liên môn\n\nII. PHƯƠNG PHÁP:\n- Dạy học dự án (PBL)\n- Kết hợp CNTT\n\nIII. TIẾN TRÌNH:\n1. Khởi động (5p)\n2. Giao dự án (10p)\n3. Thực hiện nhóm (20p)\n4. Trình bày (10p)" },
            new[] { "Truyền thống", "I. MỤC TIÊU:\n- Nắm vững kiến thức cơ bản\n\nII. PHƯƠNG PHÁP:\n- Giảng giải, hỏi đáp\n\nIII. TIẾN TRÌNH:\n1. Ổn định (2p)\n2. Bài cũ (5p)\n3. Bài mới (30p)\n4. Củng cố (5p)\n5. Dặn dò (3p)" },
            new[] { "STEM", "I. MỤC TIÊU:\n- Phát triển tư duy STEM\n\nII. TIẾN TRÌNH:\n1. Xác định vấn đề (5p)\n2. Nghiên cứu (10p)\n3. Thiết kế giải pháp (10p)\n4. Chế tạo mẫu (15p)\n5. Kiểm tra & cải tiến (5p)" },
            new[] { "Flipped", "I. MỤC TIÊU:\n- Học sinh tự chủ\n\nII. CHUẨN BỊ Ở NHÀ:\n- Video bài giảng 10p\n- Phiếu tự học\n\nIII. TRÊN LỚP:\n1. Kiểm tra hiểu biết (5p)\n2. Thảo luận (15p)\n3. Bài tập nâng cao (20p)\n4. Phản hồi (5p)" }
        };

        private static readonly string[] ProgressTemplates = new[]
        {
            "📈 Xu hướng tích cực: Điểm TB tăng {0}% so với tháng trước. Điểm mạnh: {1}. Cần cải thiện: Kỹ năng thuyết trình.",
            "🌱 Tiến bộ đều đặn: HS đạt top {0}% lớp. Nổi bật ở {1}. Đề xuất: Tham gia CLB học thuật.",
            "🏆 Xuất sắc: Điểm trung bình đạt {0}/10. Thế mạnh rõ rệt tại {1}. Cần thử thách thêm ở cấp độ nâng cao.",
            "⚠️ Cần lưu ý: Điểm {1} giảm {0}%. Nguyên nhân có thể: thiếu tập trung. Đề xuất: Tăng bài tập cá nhân.",
            "📊 Ổn định: Duy trì mức điểm trên TB lớp. Điểm mạnh: {1}. Đề xuất: Tham gia Olympic cấp trường."
        };

        private static readonly string[] CareerTemplates = new[]
        {
            "Mã Holland: Nghiên cứu + Thực tế ➔ Đề xuất ngành: Kỹ sư CNTT, Bác sĩ, Kiến trúc sư.",
            "Mã Holland: Nghệ thuật + Xã hội ➔ Đề xuất ngành: Thiết kế đồ họa, Giáo viên, Nhà tâm lý.",
            "Mã Holland: Kiến tạo + Truyền thống ➔ Đề xuất ngành: Quản trị kinh doanh, Kế toán, Luật sư.",
            "Mã Holland: Thực tế + Truyền thống ➔ Đề xuất ngành: Kỹ sư cơ khí, Xây dựng, Điện tử.",
            "Mã Holland: Xã hội + Nghiên cứu ➔ Đề xuất ngành: Bác sĩ, Dược sĩ, Tư vấn viên."
        };

        public string SuggestLessonPlan(string subject, string grade)
        {
            Log.Information("AI: SuggestLessonPlan for {Subject} - {Grade}", subject, grade);
            var template = LessonPlanTemplates[_rng.Next(LessonPlanTemplates.Length)];
            return $"AI-Generated Draft [{template[0]}] — Môn: {subject}, Lớp: {grade}\n\n{template[1]}";
        }

        public string GetLessonPlanSuggestion(string subject, string grade, string topic)
        {
            return SuggestLessonPlan(subject, grade) + $"\n\nCHỦ ĐỀ: {topic}";
        }

        public string AnalyzeStudentProgress(int studentId)
        {
            Log.Information("AI: AnalyzeStudentProgress for student {Id}", studentId);

            // Query dữ liệu thật từ DB
            if (_db != null)
            {
                try
                {
                    var grades = _db.StudentGrades.Where(g => g.StudentId == studentId).ToList();
                    if (grades.Any())
                    {
                        double avg = Math.Round(grades.Average(g => g.Score), 1);
                        var best = grades.OrderByDescending(g => g.Score).First();
                        var worst = grades.OrderBy(g => g.Score).First();
                        int total = grades.Count;

                        return $"📊 Phân tích học sinh ID#{studentId}:\n" +
                               $"• Tổng số điểm: {total} bài kiểm tra\n" +
                               $"• Điểm TB chung: {avg}/10\n" +
                               $"• Điểm cao nhất: {best.Score}/10\n" +
                               $"• Điểm thấp nhất: {worst.Score}/10\n" +
                               $"• Xếp loại: {(avg >= 8 ? "🥇 Giỏi" : avg >= 6.5 ? "🥈 Khá" : avg >= 5 ? "🥉 Trung bình" : "🔴 Yếu")}\n" +
                               $"• Đề xuất: {(avg >= 8 ? "Tham gia CLB học thuật, đội tuyển" : avg >= 5 ? "Tăng cường luyện tập, phụ đạo thêm" : "Cần can thiệp sớm, gặp GVCN")}";
                    }
                }
                catch (Exception ex) { Log.Warning(ex, "AI: DB query failed, using template fallback"); }
            }

            // Fallback khi không có dữ liệu
            var tpl = ProgressTemplates[_rng.Next(ProgressTemplates.Length)];
            return string.Format(tpl, _rng.Next(5, 20), "Toán học");
        }

        public string CareerGuidance(List<string>? testAnswers)
        {
            testAnswers ??= new List<string>();
            Log.Information("AI: CareerGuidance called");
            return "[BẢN THỬ NGHIỆM OFFLINE - KẾT QUẢ NGẪU NHIÊN]\nKết quả trắc nghiệm RIASEC:\n\n• " + CareerTemplates[_rng.Next(CareerTemplates.Length)] +
                   "\n• Đề xuất: Tham gia CLB STEM, trải nghiệm thực tế.";
        }

        public string GetCareerGuidance(string? subject, double score, Dictionary<string, double>? grades)
        {
            return CareerGuidance(null);
        }

        // P3-10: Gợi ý nội dung bảng tin
        public string SuggestBulletinContent(DateTime targetDate, string eventType)
        {
            Log.Information("AI: SuggestBulletinContent for {Event} on {Date}", eventType, targetDate);

            // Giả lập logic AI dựa vào ngày lễ/sự kiện
            if (targetDate.Month == 11 && targetDate.Day == 20)
            {
                return "Kính gửi toàn thể Cán bộ, Giáo viên và Nhân viên nhà trường,\n\n" +
                       "Nhân ngày Nhà giáo Việt Nam 20/11, xin gửi lời tri ân sâu sắc nhất đến những 'người lái đò' thầm lặng. " +
                       "Kính chúc quý thầy cô luôn mạnh khỏe, hạnh phúc và gặt hái nhiều thành công trong sự nghiệp trồng người.\n\n" +
                       "Trân trọng,\nBan Giám Hiệu.";
            }
            
            if (eventType == "Holiday")
            {
                return $"Thông báo về việc nghỉ lễ sắp tới ({targetDate:dd/MM/yyyy}).\n\n" +
                       $"Đề nghị các lớp tổng vệ sinh lớp học trước ngày nghỉ. Các bộ phận liên quan đảm bảo công tác an ninh trật tự, an toàn trường học.\n\n" +
                       "Trân trọng.";
            }

            return $"Thông báo hoạt động ngoại khóa tháng {targetDate.Month}.\n\n" +
                   $"Trường tổ chức sự kiện chào mừng với nhiều hoạt động hấp dẫn. Yêu cầu học sinh tham gia đầy đủ, giáo viên đôn đốc nhắc nhở các em chuẩn bị tốt.\n\n" +
                   "Trân trọng.";
        }
    }
}

