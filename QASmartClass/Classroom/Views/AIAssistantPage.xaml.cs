using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class AIAssistantPage : Page
    {
        private List<(string Role, string Content)> _chatHistory = new();
        private const string PlaceholderText = "Nhập câu hỏi hoặc chọn công cụ nhanh bên trái...";

        // AI response templates (offline mode — replace with API later)
        private static readonly Dictionary<string, string> Templates = new()
        {
            ["quiz"] = "📝 **Câu hỏi trắc nghiệm mẫu**\n\n" +
                       "Câu 1: Phương trình bậc 2 có dạng tổng quát là?\n" +
                       "  A. ax + b = 0\n  B. ax² + bx + c = 0 ✅\n  C. ax³ + bx² + c = 0\n  D. ax⁴ = 0\n\n" +
                       "Câu 2: Biệt thức Δ được tính bằng công thức nào?\n" +
                       "  A. Δ = b² - 4ac ✅\n  B. Δ = b² + 4ac\n  C. Δ = 2b - ac\n  D. Δ = a² - 4bc\n\n" +
                       "💡 Bạn muốn tôi tạo thêm câu hỏi về chủ đề nào?",

            ["summary"] = "📋 **Tóm tắt bài giảng**\n\n" +
                          "🎯 Mục tiêu: Học sinh nắm được khái niệm và vận dụng giải bài tập\n\n" +
                          "📌 Nội dung chính:\n" +
                          "• Định nghĩa và tính chất cơ bản\n" +
                          "• Các công thức quan trọng\n" +
                          "• Phương pháp giải bài tập\n" +
                          "• Ứng dụng thực tế\n\n" +
                          "💡 Hãy cung cấp nội dung bài giảng cụ thể để tôi tóm tắt chi tiết hơn!",

            ["openq"] = "💡 **Gợi ý câu hỏi mở rộng**\n\n" +
                        "1. Em hãy giải thích tại sao phương pháp này lại hiệu quả?\n" +
                        "2. Nêu 3 ví dụ thực tế ứng dụng kiến thức này.\n" +
                        "3. So sánh ưu nhược điểm giữa 2 phương pháp đã học.\n" +
                        "4. Nếu thay đổi điều kiện ban đầu, kết quả sẽ thay đổi như thế nào?\n" +
                        "5. Em có thể đề xuất phương pháp giải khác không?\n\n" +
                        "🎓 Những câu hỏi này giúp học sinh phát triển tư duy phản biện.",

            ["explain"] = "📖 **Giải thích khái niệm**\n\n" +
                         "Hãy nhập khái niệm bạn muốn giải thích, ví dụ:\n" +
                         "• \"Giải thích hàm số bậc 2\"\n" +
                         "• \"Thế nào là phản ứng oxi hóa-khử\"\n" +
                         "• \"Cấu trúc câu bị động trong tiếng Anh\"\n\n" +
                         "🤖 Tôi sẽ giải thích dễ hiểu, phù hợp cho học sinh THPT.",

            ["objective"] = "🎯 **Mục tiêu bài học mẫu**\n\n" +
                           "📚 Kiến thức:\n• HS trình bày được khái niệm, tính chất cơ bản\n• HS phân biệt được các trường hợp\n\n" +
                           "🛠️ Kỹ năng:\n• Vận dụng giải bài tập từ cơ bản đến nâng cao\n• Phân tích, tổng hợp, so sánh\n\n" +
                           "💚 Thái độ:\n• Tích cực, chủ động, sáng tạo\n• Hợp tác nhóm hiệu quả",

            ["rubric"] = "📊 **Rubric đánh giá mẫu**\n\n" +
                        "| Tiêu chí | Giỏi (9-10) | Khá (7-8) | TB (5-6) | Yếu (<5) |\n" +
                        "|----------|-------------|-----------|----------|----------|\n" +
                        "| Hiểu bài | Hiểu sâu, vận dụng tốt | Hiểu, vận dụng được | Hiểu cơ bản | Chưa hiểu |\n" +
                        "| Trình bày | Rõ ràng, logic | Tương đối rõ | Còn sai sót | Chưa logic |\n" +
                        "| Sáng tạo | Có cách giải hay | Đúng phương pháp | Theo mẫu | Không làm được |\n\n" +
                        "💡 Cung cấp chủ đề cụ thể để tôi tạo rubric chi tiết hơn."
        };

        public AIAssistantPage()
        {
            InitializeComponent();
            txtChatInput.Text = PlaceholderText;
            txtChatInput.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate #94A3B8

            Loaded += (_, _) => {
                if (_chatHistory.Count == 0)
                {
                    AddWelcome();
                }
            };
        }

        private void AddWelcome()
        {
            AddMessage("ai", "🤖 Xin chào thầy/cô! Tôi là AI Trợ giảng.\n\n" +
                "Tôi có thể giúp bạn:\n" +
                "• 📝 Tạo câu hỏi trắc nghiệm / tự luận\n" +
                "• 📋 Tóm tắt bài giảng\n" +
                "• 💡 Gợi ý hoạt động dạy học\n" +
                "• 📖 Giải thích khái niệm cho HS\n" +
                "• 🎯 Soạn mục tiêu, rubric đánh giá\n\n" +
                "👈 Chọn công cụ nhanh bên trái hoặc nhập câu hỏi bên dưới!");
        }

        private async void QuickTool_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;

            string prompt = tag switch
            {
                "quiz" => "Tạo câu hỏi trắc nghiệm",
                "summary" => "Tóm tắt bài giảng",
                "openq" => "Gợi ý câu hỏi mở",
                "explain" => "Giải thích khái niệm",
                "objective" => "Tạo mục tiêu bài học",
                "rubric" => "Tạo rubric đánh giá",
                _ => tag
            };

            AddMessage("user", prompt);

            btn.IsEnabled = false;
            await System.Threading.Tasks.Task.Delay(800);
            btn.IsEnabled = true;

            string response = Templates.GetValueOrDefault(tag, "🤖 Tôi chưa hiểu yêu cầu. Bạn có thể mô tả chi tiết hơn không?");
            AddMessage("ai", response);
        }

        private void SendChat_Click(object sender, RoutedEventArgs e) => ProcessInput();
        private void ChatInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) ProcessInput(); }

        private async void ProcessInput()
        {
            string input = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(input) || input == PlaceholderText) return;

            AddMessage("user", input);
            txtChatInput.Text = "";

            txtChatInput.IsEnabled = false;
            await System.Threading.Tasks.Task.Delay(800);
            txtChatInput.IsEnabled = true;
            txtChatInput.Focus();

            // Look for keyword matches
            string response = GenerateResponse(input);
            AddMessage("ai", response);
        }

        private void TxtChatInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtChatInput.Text == PlaceholderText)
            {
                txtChatInput.Text = "";
                txtChatInput.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // Dark Slate
            }
        }

        private void TxtChatInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtChatInput.Text))
            {
                txtChatInput.Text = PlaceholderText;
                txtChatInput.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate #94A3B8
            }
        }

        private string GenerateResponse(string input)
        {
            string lower = input.ToLower();

            if (lower.Contains("trắc nghiệm") || lower.Contains("quiz") || lower.Contains("câu hỏi"))
                return Templates["quiz"];
            if (lower.Contains("tóm tắt") || lower.Contains("summary"))
                return Templates["summary"];
            if (lower.Contains("mục tiêu") || lower.Contains("objective"))
                return Templates["objective"];
            if (lower.Contains("rubric") || lower.Contains("đánh giá"))
                return Templates["rubric"];
            if (lower.Contains("giải thích") || lower.Contains("explain"))
                return Templates["explain"];
            if (lower.Contains("gợi ý") || lower.Contains("câu hỏi mở"))
                return Templates["openq"];

            return "🤖 Cảm ơn câu hỏi! Trong phiên bản hiện tại, AI hoạt động ở chế độ offline với các mẫu có sẵn.\n\n" +
                   "💡 Hãy thử:\n" +
                   "• \"Tạo câu hỏi trắc nghiệm về ...\"\n" +
                   "• \"Tóm tắt bài giảng\"\n" +
                   "• \"Gợi ý câu hỏi mở\"\n" +
                   "• \"Tạo mục tiêu bài học\"\n\n" +
                   "🔮 Phiên bản tiếp theo sẽ tích hợp GPT/Gemini API để phản hồi thông minh hơn!";
        }

        private void AddMessage(string role, string content)
        {
            _chatHistory.Add((role, content));

            bool isAI = role == "ai";
            var border = new Border
            {
                Background = isAI
                    ? new SolidColorBrush(Color.FromRgb(245, 247, 250))
                    : new SolidColorBrush(Color.FromRgb(227, 242, 253)),
                CornerRadius = new CornerRadius(isAI ? 12 : 12, isAI ? 12 : 12, isAI ? 12 : 0, isAI ? 0 : 12),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(isAI ? 0 : 60, 4, isAI ? 60 : 0, 4),
                HorizontalAlignment = isAI ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                MaxWidth = 600
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = isAI ? "🤖 AI Trợ giảng" : "👨‍🏫 Bạn",
                FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = isAI ? new SolidColorBrush(Color.FromRgb(123, 31, 162)) : new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                Margin = new Thickness(0, 0, 0, 4)
            });
            sp.Children.Add(new TextBlock
            {
                Text = content, FontSize = 15,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                LineHeight = 20
            });

            border.Child = sp;
            chatList.Children.Add(border);
            chatScroll.ScrollToEnd();
        }

        public void PrepopulateWithText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            // Xóa lịch sử cũ và nạp dữ liệu OCR/văn bản quét chọn từ bảng vào context
            _chatHistory.Clear();
            chatList.Children.Clear();
            
            _chatHistory.Add(("user", $"Văn bản trích xuất từ bảng: \"{text}\"\n\nHãy giải thích và hướng dẫn chi tiết về nội dung này."));
            _chatHistory.Add(("assistant", $"Chào thầy/cô! Tôi đã nhận được nội dung trích xuất từ bảng chọn:\n\n> **{text}**\n\nThầy/cô muốn tôi hỗ trợ hoạt động sư phạm nào?\n1. 📖 **Giải thích khái niệm** này chi tiết để giảng bài.\n2. 📝 **Tạo bộ câu hỏi trắc nghiệm** liên quan trực tiếp.\n3. 🎯 **Gợi ý mục tiêu bài học** & tiến trình giảng dạy."));
            
            foreach (var chat in _chatHistory)
            {
                AddMessage(chat.Role, chat.Content);
            }
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            _chatHistory.Clear();
            chatList.Children.Clear();
            AddWelcome();
        }

        private void ShowPromptsGuide_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "HƯỚNG DẪN CÁCH ĐẶT CÂU LỆNH (PROMPTS) CHO AI TRỢ GIẢNG\n\n" +
                "Để AI hỗ trợ soạn bài và thiết kế bài tập tối ưu nhất, thầy/cô nên đặt câu hỏi theo cấu trúc 3 bước sau:\n\n" +
                "• Bước 1 (Bối cảnh): Nêu rõ môn học và cấp lớp.\n" +
                "   Ví dụ: 'Với vai trò là giáo viên môn Toán lớp 10...'\n\n" +
                "• Bước 2 (Yêu cầu): Mô tả chi tiết chủ đề hoặc tài liệu cần xử lý.\n" +
                "   Ví dụ: '...hãy soạn một giáo án 5E cho chủ đề Hàm số bậc hai...'\n\n" +
                "• Bước 3 (Kết quả): Xác định rõ cấu trúc dữ liệu đầu ra.\n" +
                "   Ví dụ: '...yêu cầu có đầy đủ mục tiêu, hoạt động khởi động và 3 câu hỏi trắc nghiệm kèm đáp án.'\n\n" +
                "💡 Các câu lệnh gợi ý nhanh:\n" +
                "1. 'Tạo bộ câu hỏi trắc nghiệm chủ đề phản ứng oxi hóa-khử Hóa học 10.'\n" +
                "2. 'Tóm tắt bài thơ Sóng của Xuân Quỳnh ngữ văn lớp 12.'\n" +
                "3. 'Gợi ý rubric đánh giá hoạt động thuyết trình nhóm môn Vật lý.'",
                "Chỉ dẫn sử dụng prompts từng bước",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
