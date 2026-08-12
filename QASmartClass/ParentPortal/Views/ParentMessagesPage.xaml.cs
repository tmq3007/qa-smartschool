using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentMessagesPage : Page
    {
        private readonly AppDbContext _db;
        private readonly Student _student;
        private readonly string _parentId;

        public ParentMessagesPage(AppDbContext db, Student student)
        {
            InitializeComponent();
            _db = db;
            _student = student;
            _parentId = $"PH_{student.StudentCode}";

            var homeroomTeacher = db.ClassRosters.FirstOrDefault(r => r.ClassName == student.ClassName && r.IsActive)?.TeacherName ?? "Giáo viên";
            txtInfo.Text = $"Phụ huynh: {student.ParentName} • HS: {student.FullName} • GVCN: {homeroomTeacher}";

            LoadMessages();
        }

        private string GetSetting(string id, string defaultValue)
        {
            var setting = _db.SystemSettings.FirstOrDefault(s => s.Id == id);
            if (setting == null)
            {
                setting = new SystemSetting { Id = id, Value = defaultValue, Category = "ParentPortal", LastUpdated = DateTime.Now };
                _db.SystemSettings.Add(setting);
                try { _db.SaveChanges(); } catch {}
            }
            return setting.Value;
        }

        private void LoadMessages()
        {
            try
            {
                var messages = _db.InboxMessages
                    .Where(m => m.SenderId == _parentId || m.ReceiverId == _parentId)
                    .OrderBy(m => m.CreatedAt)
                    .ToList();

                var isSentByMe = new Func<InboxMessage, bool>(m => m.SenderId == _parentId);

                listMessages.ItemsSource = messages.Select(m => new
                {
                    SenderLabel = isSentByMe(m) ? "Bạn" : m.SenderName,
                    m.Content,
                    TimeText = m.CreatedAt.ToString("HH:mm dd/MM"),
                    BgColor = isSentByMe(m)
                        ? new SolidColorBrush(Color.FromRgb(219, 234, 254))  // Light blue
                        : new SolidColorBrush(Color.FromRgb(241, 245, 249)), // Light gray
                    LabelColor = isSentByMe(m)
                        ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                        : new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Alignment = isSentByMe(m) ? HorizontalAlignment.Right : HorizontalAlignment.Left
                }).ToList();

                // Mark unread as read
                var unread = messages.Where(m => !m.IsRead && m.ReceiverId == _parentId).ToList();
                foreach (var msg in unread) msg.IsRead = true;
                if (unread.Any()) _db.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error("ParentMessages: Load error — {Err}", ex.Message);
            }
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            var content = txtMessage.Text?.Trim();
            if (string.IsNullOrWhiteSpace(content)) return;

            try
            {
                var routingSetting = GetSetting("ParentPortal_MessageRouting", "Homeroom");
                string receiverId = "GV";

                if (routingSetting.Equals("Homeroom", StringComparison.OrdinalIgnoreCase))
                {
                    var homeroomRoster = _db.ClassRosters.FirstOrDefault(r => r.ClassName == _student.ClassName && r.IsActive);
                    if (homeroomRoster != null)
                    {
                        receiverId = homeroomRoster.TeacherName;
                    }
                }

                _db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = _parentId,
                    SenderName = _student.ParentName,
                    ReceiverId = receiverId,
                    Content = content,
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    ThreadId = $"PH_{_student.StudentCode}"
                });
                _db.SaveChanges();

                txtMessage.Text = "";
                LoadMessages();

                Log.Information("ParentMessages: Sent message from {Parent} to {Receiver}", _parentId, receiverId);
            }
            catch (Exception ex)
            {
                Log.Error("ParentMessages: Send error — {Err}", ex.Message);
                MessageBox.Show("Không thể gửi tin nhắn. Vui lòng thử lại.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

