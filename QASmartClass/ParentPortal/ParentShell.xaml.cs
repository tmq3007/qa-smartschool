using System;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;

namespace QASmartClass.ParentPortal
{
    public partial class ParentShell : Window
    {
        private readonly AppDbContext _db;
        private readonly Student _student;
        private readonly ParentAuthService _authService;
        private System.Windows.Threading.DispatcherTimer? _badgeTimer;

        public ParentShell(AppDbContext db, Student student)
        {
            InitializeComponent();
            _db = db;
            _student = student;
            _authService = new ParentAuthService(db);

            // Populate sidebar
            txtStudentName.Text = $"🎓 {student.FullName}";
            txtClassName.Text = $"Lớp {student.ClassName} • {student.StudentCode}";
            txtParentName.Text = $"👨‍👩‍👧 PH: {student.ParentName}";

            UpdateBadge();

            // Setup real-time polling timer for unread badges
            _badgeTimer = new System.Windows.Threading.DispatcherTimer();
            _badgeTimer.Interval = TimeSpan.FromSeconds(20);
            _badgeTimer.Tick += async (s, e) =>
            {
                try
                {
                    int totalUnread = await System.Threading.Tasks.Task.Run(() =>
                    {
                        using (var dbLocal = new AppDbContext())
                        {
                            var notifService = new NotificationService(dbLocal);
                            int unreadNotifs = notifService.GetUnreadCount(_student.Id);

                            string parentChatId = $"PH_{_student.StudentCode}";
                            int unreadChats = dbLocal.InboxMessages.Count(m => m.ReceiverId == parentChatId && !m.IsRead);

                            return unreadNotifs + unreadChats;
                        }
                    });

                    Dispatcher.Invoke(() =>
                    {
                        if (totalUnread > 0)
                        {
                            BadgeNotif.Visibility = Visibility.Visible;
                            TxtBadgeCount.Text = totalUnread > 99 ? "99+" : totalUnread.ToString();
                        }
                        else
                        {
                            BadgeNotif.Visibility = Visibility.Collapsed;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Warning("ParentShell background UpdateBadge error: {Err}", ex.Message);
                }
            };
            _badgeTimer.Start();

            Closed += (s, e) =>
            {
                _badgeTimer?.Stop();
                _badgeTimer = null;
            };

            // Navigate to dashboard
            NavigateTo("dashboard");
        }

        private void UpdateBadge()
        {
            try
            {
                var notifService = new NotificationService(_db);
                int unreadNotifs = notifService.GetUnreadCount(_student.Id);

                string parentChatId = $"PH_{_student.StudentCode}";
                int unreadChats = _db.InboxMessages.Count(m => m.ReceiverId == parentChatId && !m.IsRead);

                int totalUnread = unreadNotifs + unreadChats;

                if (totalUnread > 0)
                {
                    BadgeNotif.Visibility = Visibility.Visible;
                    TxtBadgeCount.Text = totalUnread > 99 ? "99+" : totalUnread.ToString();
                }
                else
                {
                    BadgeNotif.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("ParentShell UpdateBadge error: {Err}", ex.Message);
            }
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
                NavigateTo(tag);
        }

        private void NavigateTo(string tag)
        {
            try
            {
                Page? page = tag switch
                {
                    "dashboard" => new Views.ParentDashboardPage(_db, _student, _authService),
                    "grades" => new Views.ParentGradesPage(_db, _student),
                    "messages" => new Views.ParentMessagesPage(_db, _student),
                    "attendance" => new Views.ParentAttendancePage(_student.Id),
                    "timetable" => new Views.ParentTimetablePage(_db, _student),
                    "bulletin" => new Views.ParentBulletinPage(),
                    "tuition" => new Views.ParentTuitionPage(_db, _student),
                    "notification" => new Views.ParentNotificationPage(_db, _student.Id) { OnNotificationRead = UpdateBadge },
                    "familygame" => new Views.FamilyGamePage(_db, _student.Id, _student.Id),
                    _ => null
                };

                if (page != null)
                    MainFrame.Navigate(page);
            }
            catch (Exception ex)
            {
                Log.Error("ParentShell navigate error: {Err}", ex.Message);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("ParentPortal: Logout for {Code}", _student.StudentCode);
            Close();
        }
    }
}

