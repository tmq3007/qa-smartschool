using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Staff.Services;
using System.Windows;

namespace QASmartClass.Staff.ViewModels
{
    public partial class StaffOverviewViewModel : ObservableObject
    {
        [ObservableProperty] private int _totalLogs;
        [ObservableProperty] private int _totalStudents;
        [ObservableProperty] private string _welcomeMessage = "Xin chào, Nhân sự Nhà trường!";
        
        [ObservableProperty] private int _pendingRequests;
        [ObservableProperty] private int _activeStaffCount;
        [ObservableProperty] private string _todayRevenue = "0";

        [ObservableProperty] private ObservableCollection<RecentActivityDisplay> _recentActivities = new();

        // ═══ BGH Executive KPIs ═══
        [ObservableProperty] private int _attendanceOnTime;
        [ObservableProperty] private int _attendanceLate;
        [ObservableProperty] private int _attendanceAbsent;
        [ObservableProperty] private ObservableCollection<UpcomingEventItem> _upcomingEvents = new();
        [ObservableProperty] private string _alertSummary = "Không có cảnh báo";
        [ObservableProperty] private Visibility _noEventsVisibility = Visibility.Visible;
        [ObservableProperty] private ObservableCollection<AuditLogDisplay> _auditLogsTimeline = new();
        [ObservableProperty] private Visibility _noAuditLogsVisibility = Visibility.Visible;

        // --- Phase 2.3 Analytics ---
        private ObservableCollection<TopBulletinItem> _topBulletins = new ObservableCollection<TopBulletinItem>();
        public ObservableCollection<TopBulletinItem> TopBulletins
        {
            get => _topBulletins;
            set { _topBulletins = value; OnPropertyChanged(nameof(TopBulletins)); }
        }

        public StaffOverviewViewModel()
        {
            if (StaffSession.IsLoggedIn)
            {
                WelcomeMessage = $"Xin chào, {StaffSession.DisplayName}!";
            }
        }

        public async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        private RecentActivityDisplay MapToDisplay(EventLog log)
        {
            string details = "Không có mô tả chi tiết";
            string title = log.EventType;
            if (log.EventType == "WalletTransaction")
            {
                title = "Thanh toán Ví Canteen";
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                    var root = doc.RootElement;
                    double amount = root.GetProperty("Amount").GetDouble();
                    details = $"Trừ ví Canteen: -{amount:N0} đ (Mã HS: {log.Actor})";
                }
                catch { details = log.Details; }
            }
            else if (log.EventType == "Incident")
            {
                title = "🚨 Sự cố Trường học";
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                    var root = doc.RootElement;
                    string severity = root.GetProperty("Severity").GetString() ?? "";
                    string friendlySeverity = severity switch {
                        "Low" => "Nhẹ",
                        "Medium" => "Trung bình",
                        "High" => "Nghiêm trọng",
                        "Critical" => "Khẩn cấp",
                        _ => severity
                    };
                    details = $"[{friendlySeverity}] {root.GetProperty("Description").GetString()}";
                }
                catch { details = log.Details; }
            }
            else if (log.EventType == "GateCheckIn" || log.EventType == "GateCheckOut")
            {
                title = log.EventType == "GateCheckIn" ? "Học sinh Vào trường" : "Học sinh Ra về";
                details = $"Học sinh {log.Actor} {(log.EventType == "GateCheckIn" ? "vào trường" : "ra về")}";
            }
            return new RecentActivityDisplay { Title = title, FriendlyDetails = details, Timestamp = log.Timestamp };
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                using var db = new AppDbContext();
                TotalLogs = await db.EventLogs.CountAsync();
                TotalStudents = await db.Students.CountAsync(s => s.Status == "Active");

                PendingRequests = await db.DocumentRoutes.CountAsync(d => d.Status == "Pending")
                                + await db.StudentLeaveRequests.CountAsync(r => r.Status == "Pending");
                ActiveStaffCount = await db.TeacherProfiles.CountAsync(t => t.IsActive);
                
                var todayStart = DateTime.Today;
                var todayEnd = todayStart.AddDays(1);
                var txs = await db.EventLogs
                    .Where(l => l.EventType == "WalletTransaction" && l.Timestamp >= todayStart && l.Timestamp < todayEnd)
                    .ToListAsync();
                double totalAmount = 0;
                foreach (var t in txs)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(t.Details);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("Type", out var typeProp) && typeProp.GetString() == "Canteen_Deduction")
                        {
                            if (root.TryGetProperty("Amount", out var amtProp))
                            {
                                totalAmount += amtProp.GetDouble();
                            }
                        }
                    }
                    catch { }
                }
                TodayRevenue = totalAmount >= 1000 ? $"{totalAmount / 1000:N0}K" : $"{totalAmount:N0} đ";

                var activities = await db.EventLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(5).ToListAsync();
                RecentActivities = new ObservableCollection<RecentActivityDisplay>(activities.Select(MapToDisplay));

                // ═══ BGH Executive Data ═══
                // Chấm công: dựa trên dữ liệu có sẵn
                var today = DateTime.Today;
                var lateToday = await db.StaffAttendances.CountAsync(a => a.Date == today && a.Status == "Late");
                var leaveToday = await db.StaffAttendances.CountAsync(a => a.Date == today && a.Status == "Leave");
                var onTimeToday = await db.StaffAttendances.CountAsync(a => a.Date == today && a.Status == "Present");

                AttendanceAbsent = leaveToday;
                AttendanceLate = lateToday;
                AttendanceOnTime = onTimeToday;

                // Sự kiện sắp tới (từ bảng SchoolEvents thực tế)
                var events = await db.SchoolEvents
                    .Where(e => e.StartTime >= DateTime.Today && e.Status != "Cancelled")
                    .OrderBy(e => e.StartTime)
                    .Take(5).ToListAsync();
                
                if (events.Any())
                {
                    UpcomingEvents = new ObservableCollection<UpcomingEventItem>(
                        events.Select(e => new UpcomingEventItem
                        {
                            DateLabel = e.StartTime.ToString("dd/MM HH:mm"),
                            Title = $"{e.Title} ({e.Location})"
                        }));
                    NoEventsVisibility = Visibility.Collapsed;
                }
                else
                {
                    NoEventsVisibility = Visibility.Visible;
                }

                // Cảnh báo
                var alerts = new System.Collections.Generic.List<string>();
                if (PendingRequests > 0) alerts.Add($"📋 {PendingRequests} yêu cầu chờ xử lý");
                if (AttendanceAbsent > 3) alerts.Add($"🏥 {AttendanceAbsent} nhân sự nghỉ phép hôm nay");
                
                // Vòng 4: Cảnh báo rủi ro tâm lý SEL khẩn cấp
                var criticalCounselings = await db.StudentMentalHealthRecords.CountAsync(r => (r.RiskLevel == "High" || r.RiskLevel == "Critical") && !r.IsNotified);
                if (criticalCounselings > 0) alerts.Add($"🚨 {criticalCounselings} học sinh cần can thiệp SEL khẩn cấp!");

                AlertSummary = alerts.Count > 0 ? string.Join(" • ", alerts) : "✅ Không có cảnh báo nào";

                // --- PHASE 2.3 Analytics: Top 5 Bulletins ---
                var topB = await db.Bulletins
                    .Where(b => b.Status == "Published")
                    .OrderByDescending(b => b.ViewCount)
                    .Take(5)
                    .ToListAsync();

                TopBulletins = new ObservableCollection<TopBulletinItem>(
                    topB.Select(b => new TopBulletinItem
                    {
                        Title = b.Title,
                        ViewCount = b.ViewCount,
                        LikeCount = b.LikeCount,
                        Audience = b.Audience
                    })
                );

                // --- PHASE 2 Audit Log Timeline & Security Anomaly Flags ---
                var auditList = await db.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10).ToListAsync();

                var timeline = new ObservableCollection<AuditLogDisplay>();
                foreach (var log in auditList)
                {
                    bool isSuspicious = false;
                    string flagText = "";

                    // Rule 1: Access during off-hours (22:00 to 04:00)
                    if (log.Timestamp.Hour >= 22 || log.Timestamp.Hour < 4)
                    {
                        isSuspicious = true;
                        flagText = "Truy cập ngoài giờ";
                    }

                    // Rule 2: Suspicious IP/Actor Details
                    if (log.Details.Contains("IP lạ") || log.Details.Contains("IP_Unknown") || log.Details.Contains("192.168.99"))
                    {
                        isSuspicious = true;
                        flagText = "IP lạ";
                    }

                    timeline.Add(new AuditLogDisplay
                    {
                        Action = log.Action,
                        Details = log.Details,
                        ActorInfo = $"Người thực hiện: {log.ActorName}",
                        Timestamp = log.Timestamp,
                        IsSuspicious = isSuspicious,
                        FlagText = flagText
                    });
                }
                AuditLogsTimeline = timeline;
                NoAuditLogsVisibility = timeline.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[StaffOverview] Load data failed");
            }
        }
    }

    /// <summary>Item hiển thị sự kiện sắp tới</summary>
    public class UpcomingEventItem
    {
        public string DateLabel { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class TopBulletinItem
    {
        public string Title { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public string Audience { get; set; } = string.Empty;
    }

    public class RecentActivityDisplay
    {
        public string Title { get; set; } = string.Empty;
        public string FriendlyDetails { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class AuditLogDisplay
    {
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string ActorInfo { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimestampString => Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
        public bool IsSuspicious { get; set; }
        public string FlagText { get; set; } = string.Empty;
        public System.Windows.Visibility FlagVisibility => IsSuspicious ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Media.Brush FlagBackground => IsSuspicious ? System.Windows.Media.Brushes.OrangeRed : System.Windows.Media.Brushes.Transparent;
        public System.Windows.Media.Brush StatusBrush => IsSuspicious ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
    }
}

