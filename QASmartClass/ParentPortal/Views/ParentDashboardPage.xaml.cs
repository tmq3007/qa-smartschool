using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentDashboardPage : Page
    {
        private readonly AppDbContext _db;
        private readonly Student _student;
        private readonly NotificationService _notifService;

        public ParentDashboardPage(AppDbContext db, Student student, ParentAuthService authService)
        {
            InitializeComponent();
            _db = db;
            _student = student;
            _notifService = new NotificationService(db);

            txtGreeting.Text  = $"Xin chào, {student.ParentName} \U0001f44b";
            txtSubtitle.Text  = $"Con em: {student.FullName} \u2022 {student.ClassName} \u2022 {student.StudentCode}";

            var summary = authService.GetSummary(student.Id);

            txtAvgScore.Text   = summary.AverageScore > 0 ? summary.AverageScore.ToString("F1") : "\u2014";
            txtAttendance.Text = $"{(summary.PresentDays + summary.LateDays)}/{summary.TotalAttendanceDays}";
            txtConduct.Text    = summary.ConductScore.ToString();
            txtPending.Text    = summary.PendingAssignments.ToString();

            txtPresent.Text = summary.PresentDays.ToString();
            txtLate.Text    = summary.LateDays.ToString();
            txtAbsent.Text  = summary.AbsentDays.ToString();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadNotificationBadge();
            RunEarlyWarningCheck();
        }

        private void LoadNotificationBadge()
        {
            try
            {
                int unread = _notifService.GetUnreadCount(_student.Id);
                if (FindName("TxtNotifBadgeBorder") is Border badgeBorder && FindName("TxtNotifBadgeText") is TextBlock badgeText)
                {
                    badgeText.Text         = unread > 0 ? unread.ToString() : "";
                    badgeBorder.Visibility = unread > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                if (FindName("TxtNotifCount") is TextBlock count)
                    count.Text = unread > 0 ? $"{unread} thông báo chưa đọc" : "Không có thông báo mới";

                var inbox = _notifService.GetParentInbox(_student.Id, 5);
                if (FindName("LstNotifications") is ListBox lst)
                {
                    lst.ItemsSource = inbox.Select(m => new
                    {
                        m.Content,
                        TimeStr  = m.CreatedAt.ToString("dd/MM HH:mm"),
                        IsUnread = !m.IsRead
                    }).ToList();
                }
                Log.Information("[ParentDashboard] Loaded {Count} unread", unread);
            }
            catch (Exception ex) { Log.Warning("[ParentDashboard] Badge error: {Err}", ex.Message); }
        }

        private void RunEarlyWarningCheck()
        {
            try
            {
                var warning = new EarlyWarningService(_db);
                var atRisk  = warning.GetAtRiskStudents(DateTime.Now.Month, DateTime.Now.Year);
                var myKid   = atRisk.FirstOrDefault(r => r.StudentId == _student.Id);

                if (myKid != null && FindName("PanelWarning") is Border panel)
                {
                    panel.Visibility = Visibility.Visible;
                    if (FindName("TxtWarningDetail") is TextBlock txt)
                        txt.Text = $"Cảnh báo: {myKid.Reason}";

                    var alreadySent = _db.InboxMessages
                        .Any(m => m.ReceiverId == $"parent_{_student.Id}"
                               && m.ThreadId   == $"earlywarning_{_student.Id}"
                               && m.CreatedAt  >= DateTime.Today.AddDays(-7));
                    if (!alreadySent)
                        _notifService.SendToParent(_student.Id, "Cảnh báo học tập", myKid.Reason);
                }
            }
            catch (Exception ex) { Log.Warning("[ParentDashboard] EarlyWarning error: {Err}", ex.Message); }
        }

        private void BtnViewNotifs_Click(object sender, RoutedEventArgs e)
            => LoadNotificationBadge();
    }
}

