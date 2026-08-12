using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Services;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentChatbotView : Page
    {
        private readonly ChatbotService _chatbotService;

        public StudentChatbotView()
        {
            InitializeComponent();
            _chatbotService = new ChatbotService();
            AddMessage("Xin chào! Mình là trợ lý AI. Bạn cần hỗ trợ môn Toán, Tiếng Anh hay Lịch học?", false);
        }

        private void AddMessage(string text, bool isUser)
        {
            var border = new Border
            {
                Background = isUser ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F2F5")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 450
            };

            var textBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Foreground = isUser ? Brushes.White : Brushes.Black,
                FontSize = 14
            };

            border.Child = textBlock;
            PnlChatHistory.Children.Add(border);
            SvChat.ScrollToBottom();
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void TxtInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                await SendMessage();
            }
        }

        private async Task SendMessage()
        {
            var text = TxtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AddMessage(text, true);
            TxtInput.Text = "";
            TxtInput.IsEnabled = false;
            BtnSend.IsEnabled = false;

            try
            {
                string currentStudentName = "Nguyễn Học Sinh";
                try
                {
                    using (var db = new QASmartClass.Data.AppDbContext())
                    {
                        var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                        var (_, _, sName) = identityService.GetCurrentStudent();
                        currentStudentName = sName;
                    }
                }
                catch { }
                var response = await _chatbotService.AskQuestionAsync(text, currentStudentName);
                AddMessage(response, false);
            }
            catch (Exception ex)
            {
                AddMessage($"Lỗi: {ex.Message}", false);
            }
            finally
            {
                TxtInput.IsEnabled = true;
                BtnSend.IsEnabled = true;
                TxtInput.Focus();
            }
        }
    }
}

