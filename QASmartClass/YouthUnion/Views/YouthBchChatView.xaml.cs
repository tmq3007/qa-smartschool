using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.YouthUnion.Views
{
    public partial class YouthBchChatView : Page
    {
        private AppDbContext _db;

        public YouthBchChatView()
        {
            InitializeComponent();
            
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_db == null)
            {
                _db = new AppDbContext();
            }
            await LoadMessagesAsync();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }


        private async Task LoadMessagesAsync()
        {
            try
            {
                var messages = await _db.YouthBchMessages
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();
                    
                IcMessages.ItemsSource = messages;
                
                // Scroll to bottom
                if (messages.Any())
                {
                    ScvMessages.UpdateLayout();
                    ScvMessages.ScrollToBottom();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading BCH messages");
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadMessagesAsync();
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            var text = TxtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                var msg = new YouthBchMessage
                {
                    SenderId = currentUser?.TeacherCode ?? "GV001",
                    SenderName = currentUser?.FullName ?? "Bí thư",
                    Content = text,
                    SentAt = DateTime.Now
                };

                _db.YouthBchMessages.Add(msg);
                await _db.SaveChangesAsync();

                TxtMessage.Clear();
                await LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending BCH message");
                MessageBox.Show("Không thể gửi tin nhắn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ChatBubbleAlignmentConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string senderId)
            {
                var currentTeacherCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV001";
                if (senderId == currentTeacherCode)
                {
                    return System.Windows.HorizontalAlignment.Right;
                }
            }
            return System.Windows.HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ChatBubbleColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string senderId)
            {
                var currentTeacherCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV001";
                if (senderId == currentTeacherCode)
                {
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(219, 234, 254)); // Light blue #DBEAFE
                }
            }
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)); // Slate 100 #F1F5F9
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
