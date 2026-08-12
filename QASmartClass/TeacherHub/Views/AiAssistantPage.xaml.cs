using QASmartClass.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartClass.TeacherHub.Views
{
    public class ChatMessage
    {
        public string Sender { get; set; } = "AI"; // "User" or "AI"
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public partial class AiAssistantPage : Page
    {
        private readonly AiAssistantService _aiService;
        private const string PlaceholderText = "Nhập câu hỏi hoặc yêu cầu hỗ trợ...";

        // Static session chat history cache
        private static readonly List<ChatMessage> _conversationHistory = new()
        {
            new ChatMessage
            {
                Sender = "AI",
                Text = "Xin chào! Tôi là trợ lý AI trợ giảng. Hãy nhập câu hỏi hoặc chọn một chức năng bên trái để tôi hỗ trợ bạn."
            }
        };

        public AiAssistantPage()
        {
            InitializeComponent();
            _aiService = new AiAssistantService(QASmartClass.Services.AppServices.Database ?? QASmartClass.Services.AppServices.CreateDb());

            // Initialize placeholder text and style programmatically to ensure 100% string comparison consistency
            TxtInput.Text = PlaceholderText;
            TxtInput.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate #94A3B8

            Loaded += AiAssistantPage_Loaded;
        }

        private void AiAssistantPage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshChatDisplay();
            ScrollToBottom();
        }

        private void RefreshChatDisplay()
        {
            if (ChatHistoryPanel == null) return;
            ChatHistoryPanel.Children.Clear();

            foreach (var msg in _conversationHistory)
            {
                RenderMessageBubble(msg);
            }
        }

        private void AppendMessage(string sender, string text)
        {
            var msg = new ChatMessage { Sender = sender, Text = text };
            _conversationHistory.Add(msg);
            RenderMessageBubble(msg);
            ScrollToBottom();
        }

        private void RenderMessageBubble(ChatMessage msg)
        {
            if (ChatHistoryPanel == null) return;

            bool isUser = msg.Sender.Equals("User", StringComparison.OrdinalIgnoreCase);

            var bubble = new Border
            {
                CornerRadius = isUser ? new CornerRadius(12, 12, 0, 12) : new CornerRadius(12, 12, 12, 0),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = isUser ? new Thickness(60, 4, 4, 4) : new Thickness(4, 4, 60, 4),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Background = isUser
                    ? new SolidColorBrush(Color.FromRgb(37, 99, 235)) // Blue #2563EB
                    : new SolidColorBrush(Color.FromRgb(241, 245, 249)), // Light Grey #F1F5F9
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 4,
                    ShadowDepth = 1,
                    Opacity = 0.05,
                    Color = Colors.Black
                }
            };

            var textBlock = new TextBlock
            {
                Text = msg.Text,
                FontSize = 14,
                Foreground = isUser ? Brushes.White : new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate #334155
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                FontFamily = new FontFamily("Segoe UI")
            };

            bubble.Child = textBlock;
            ChatHistoryPanel.Children.Add(bubble);
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer?.UpdateLayout();
            ChatScrollViewer?.ScrollToBottom();
        }

        // Sidebar actions
        private async void BtnSuggestLesson_Click(object sender, RoutedEventArgs e)
        {
            AppendMessage("User", "Yêu cầu: Gợi ý Giáo án dạy học");
            BtnSuggestLesson.IsEnabled = false;
            await System.Threading.Tasks.Task.Delay(800);
            BtnSuggestLesson.IsEnabled = true;
            string reply = _aiService.SuggestLessonPlan("Toán", "10");
            AppendMessage("AI", reply);
        }

        private async void BtnAnalyzeStudent_Click(object sender, RoutedEventArgs e)
        {
            AppendMessage("User", "Yêu cầu: Phân tích học sinh ID#101");
            BtnAnalyzeStudent.IsEnabled = false;
            await System.Threading.Tasks.Task.Delay(800);
            BtnAnalyzeStudent.IsEnabled = true;
            string reply = _aiService.AnalyzeStudentProgress(101);
            AppendMessage("AI", reply);
        }

        private async void BtnCareerGuidance_Click(object sender, RoutedEventArgs e)
        {
            AppendMessage("User", "Yêu cầu: Hướng nghiệp RIASEC");
            BtnCareerGuidance.IsEnabled = false;
            await System.Threading.Tasks.Task.Delay(800);
            BtnCareerGuidance.IsEnabled = true;
            string reply = _aiService.CareerGuidance(null);
            AppendMessage("AI", reply);
        }

        // TextBox & Button Handlers
        private void TxtInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtInput.Text == PlaceholderText)
            {
                TxtInput.Text = "";
                TxtInput.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // Dark slate #1E293B
            }
        }

        private void TxtInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtInput.Text))
            {
                TxtInput.Text = PlaceholderText;
                TxtInput.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate #94A3B8
            }
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            string query = TxtInput.Text.Trim();
            if (string.IsNullOrEmpty(query) || query == PlaceholderText)
            {
                return;
            }

            AppendMessage("User", query);
            TxtInput.Text = "";

            TxtInput.IsEnabled = false;
            BtnSend.IsEnabled = false;

            await System.Threading.Tasks.Task.Delay(800);

            TxtInput.IsEnabled = true;
            BtnSend.IsEnabled = true;
            TxtInput.Focus();

            // AI responses based on keywords in offline mode stub
            string aiReply = GenerateAiResponse(query);
            AppendMessage("AI", aiReply);
        }

        private string GenerateAiResponse(string query)
        {
            string queryLower = query.ToLowerInvariant();

            if (queryLower.Contains("giáo án") || queryLower.Contains("lesson"))
            {
                string subject = "Môn học tự chọn";
                if (queryLower.Contains("toán")) subject = "Toán";
                else if (queryLower.Contains("văn") || queryLower.Contains("ngữ văn")) subject = "Ngữ văn";
                else if (queryLower.Contains("vật lý") || queryLower.Contains("vật lí") || (queryLower.Contains("lý") && !queryLower.Contains("quản lý") && !queryLower.Contains("trợ lý"))) subject = "Vật lý";
                else if (queryLower.Contains("hóa") || queryLower.Contains("hoá")) subject = "Hóa học";
                else if (queryLower.Contains("sinh học") || (queryLower.Contains("sinh") && !queryLower.Contains("học sinh"))) subject = "Sinh học";
                else if (queryLower.Contains("anh") || queryLower.Contains("tiếng anh")) subject = "Tiếng Anh";

                string grade = "10";
                if (queryLower.Contains("11")) grade = "11";
                else if (queryLower.Contains("12")) grade = "12";

                return _aiService.SuggestLessonPlan(subject, grade);
            }
            if (queryLower.Contains("học sinh") || queryLower.Contains("tiến trình") || queryLower.Contains("student"))
            {
                int studentId = 102; // Mặc định
                var matches = System.Text.RegularExpressions.Regex.Matches(query, @"\d+");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (int.TryParse(match.Value, out int parsedId))
                    {
                        // Chọn số khác với số lớp học 10, 11, 12 để làm Student ID
                        if (parsedId != 10 && parsedId != 11 && parsedId != 12)
                        {
                            studentId = parsedId;
                            break;
                        }
                    }
                }
                
                // Nếu chỉ tìm thấy một số duy nhất là 10, 11 hoặc 12, vẫn chấp nhận làm Student ID
                if (studentId == 102 && matches.Count == 1)
                {
                    if (int.TryParse(matches[0].Value, out int singleId))
                    {
                        studentId = singleId;
                    }
                }

                return _aiService.AnalyzeStudentProgress(studentId);
            }
            if (queryLower.Contains("hướng nghiệp") || queryLower.Contains("riasec"))
            {
                return _aiService.CareerGuidance(null);
            }

            return $"Cảm ơn bạn đã gửi câu hỏi về: \"{query}\"\n\nTôi sẵn sàng hỗ trợ bạn soạn giáo án, phân tích tiến trình học tập của học sinh hoặc hướng nghiệp. Hãy chọn các tính năng gợi ý bên trái để tôi hỗ trợ nhanh nhất, hoặc nhập từ khóa liên quan như 'giáo án', 'học sinh' hay 'hướng nghiệp'.";
        }
    }
}
