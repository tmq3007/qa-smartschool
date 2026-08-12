using QASmartClass.Data;
using System;
using System.Collections.Generic;

namespace QASmartClass.Services
{
    public static class AiCopilotService
    {
        // ─── F2.2: TỰ ĐỘNG GIAO BÀI TẬP ÔN TẬP (REMEDIAL TASKS) ──────────────
        /// <summary>
        /// Gọi hàm này sau khi học sinh nộp bài Quiz. Nếu điểm dưới 5.0, tự động sinh Task ôn tập.
        /// </summary>
        public static void EvaluateQuizAndAssignRemedialTask(AppDbContext db, int studentId, string quizTitle, double score)
        {
            if (score >= 5.0) return; // Pass

            string studentCode = db.Students.Find(studentId)?.StudentCode ?? "HS_UNKNOWN";

            string advice = score < 3.0 
                ? "Bạn cần liên hệ giáo viên bộ môn gấp để được hướng dẫn phụ đạo trực tiếp kiến thức căn bản." 
                : "Hệ thống khuyên bạn nên làm lại các bài luyện tập cũ, đọc lại SGK chương này để củng cố lại lý thuyết.";

            // Sinh ra một Task mới giao riêng cho học sinh này
            var task = new DailyTask
            {
                Title = $"[Ôn tập AI] Củng cố kiến thức: {quizTitle}",
                Notes = $"Bạn đạt {score}/10 điểm trong bài '{quizTitle}'. {advice}",
                AssignedTo = studentCode, // Assign specifically to this student
                AssignedBy = "System_AI",
                Status = "Pending",
                DueDate = DateTime.Now.AddDays(2)
            };

            db.DailyTasks.Add(task);
            db.SaveChanges();

            AuditHelper.Log(db, "AI_RemedialTask", "System_AI", $"Assigned remedial task to {studentCode} for scoring {score} on {quizTitle}");
        }

        // ─── F2.3: AI COPILOT TRỢ GIẢNG (SINH CÂU HỎI TỪ VĂN BẢN) ───────────
        /// <summary>
        /// Mô phỏng AI đọc một đoạn văn bản và tách ra thành danh sách các câu hỏi trắc nghiệm.
        /// </summary>
        public static List<QuestionBankItem> GenerateQuestionsFromText(string text, string subject)
        {
            var results = new List<QuestionBankItem>();
            if (string.IsNullOrWhiteSpace(text)) return results;

            // Trong môi trường thật, đoạn này sẽ call API OpenAI/Gemini.
            // Ở đây ta mô phỏng sinh ra 3 câu hỏi cố định dựa vào text đầu vào.
            
            string snippet = text.Length > 20 ? text.Substring(0, 20) + "..." : text;

            results.Add(new QuestionBankItem
            {
                Content = $"[AI Gen] Nội dung chính của đoạn văn bắt đầu bằng '{snippet}' là gì?",
                OptionsJson = "[\"Ý tưởng A\",\"Ý tưởng B\",\"Ý tưởng C\",\"Ý tưởng D\"]",
                CorrectAnswer = "Ý tưởng A",
                QuestionType = "MCQ",
                Difficulty = "Medium",
                Subject = subject,
                Points = 10,
                CreatedAt = DateTime.Now
            });

            results.Add(new QuestionBankItem
            {
                Content = $"[AI Gen] Nhân vật / Sự kiện nào đóng vai trò then chốt trong văn bản trên?",
                OptionsJson = "[\"Đáp án 1\",\"Đáp án 2\",\"Đáp án 3\",\"Đáp án 4\"]",
                CorrectAnswer = "Đáp án 2",
                QuestionType = "MCQ",
                Difficulty = "Medium",
                Subject = subject,
                Points = 10,
                CreatedAt = DateTime.Now
            });

            results.Add(new QuestionBankItem
            {
                Content = $"[AI Gen] Đâu là kết luận đúng nhất từ đoạn dữ liệu được cung cấp?",
                OptionsJson = "[\"Kết luận X\",\"Kết luận Y\",\"Kết luận Z\",\"Kết luận W\"]",
                CorrectAnswer = "Kết luận Z",
                QuestionType = "MCQ",
                Difficulty = "Hard",
                Subject = subject,
                Points = 10,
                CreatedAt = DateTime.Now
            });

            return results;
        }
    }
}

