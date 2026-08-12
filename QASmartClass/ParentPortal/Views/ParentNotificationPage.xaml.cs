using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentNotificationPage : Page
    {
        private readonly AppDbContext _db;
        private readonly int _studentId;
        private readonly NotificationService _notifService;
        public Action? OnNotificationRead { get; set; }

        public ParentNotificationPage(AppDbContext db, int studentId)
        {
            InitializeComponent();
            _db = db;
            _studentId = studentId;
            _notifService = new NotificationService(_db);
            Loaded += (_, __) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                var msgs = _notifService.GetParentInbox(_studentId);
                LvNotifications.ItemsSource = msgs.Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.CreatedAt,
                    m.IsRead,
                    BgColor = m.IsRead ? new SolidColorBrush(Color.FromRgb(248, 250, 252)) : new SolidColorBrush(Color.FromRgb(224, 242, 254)),
                    FontWeight = m.IsRead ? FontWeights.Normal : FontWeights.Bold,
                    StatusIcon = m.IsRead ? "✔ Đã đọc" : "🔔 Chưa đọc"
                }).ToList();
            }
            catch (Exception ex) { Log.Warning("Notification Load error: {Err}", ex.Message); }
        }

        private void LvNotifications_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LvNotifications.SelectedItem == null) return;

            dynamic selected = LvNotifications.SelectedItem;
            int id = selected.Id;
            bool isRead = selected.IsRead;

            if (!isRead)
            {
                _notifService.MarkAsRead(id);
                LoadData();
                OnNotificationRead?.Invoke(); // Trigger parent shell to update badge
            }

            LvNotifications.SelectedItem = null;
        }
    }
}

