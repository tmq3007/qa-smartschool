using System;
using System.Threading.Tasks;

namespace QASmartClass.Services
{
    public class ChatbotService
    {
        // Gi? l?p g?i API tại OpenAI / Gemini
        public async Task<string> AskQuestionAsync(string question, string studentName)
        {
            await Task.Delay(1500); // Gi? l?p network delay

            string q = question.ToLower();

            if (q.Contains("toán") || q.Contains("phương trình") || q.Contains("đạo hàm"))
            {
                return $"Chào {studentName}, đây là một bài toán. Bạn thử áp dụng công thức đạo hàm cơ bản y' = nx^(n-1) xem sao nhé! Cần hỗ trợ thêm thì báo mình nha.";
            }
            if (q.Contains("tiếng anh") || q.Contains("ngữ pháp") || q.Contains("từ vựng"))
            {
                return $"Để học tốt từ vựng này, {studentName} hãy thử đặt một câu ví dụ nhé. Ví dụ: 'I am learning new vocabulary'.";
            }
            if (q.Contains("lịch học") || q.Contains("thời khóa biểu"))
            {
                return $"Lịch học của bạn đã được cập nhật ở tab 'Thời Khóa Biểu'. Ngày mai bạn có tiết Toán, Văn và Anh nhé.";
            }

            return $"Mình là Trợ lý AI QA SmartClass. Mình chưa hiểu rõ ý bạn, {studentName} có thể cung cấp thêm ngữ cảnh cho câu hỏi không?";
        }
    }
}

