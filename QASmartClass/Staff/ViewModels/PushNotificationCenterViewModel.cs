using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QASmartClass.Staff.ViewModels
{
    public partial class PushNotificationCenterViewModel : ObservableObject, IDisposable
    {
        private readonly AppDbContext _db;
        private readonly MobileApiService _mobileApiService;

        [ObservableProperty] private ObservableCollection<PushMessageLogDisplay> _history = new();
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _body = string.Empty;
        [ObservableProperty] private string _selectedType = "General";
        [ObservableProperty] private string _selectedTarget = "AllParents";

        [ObservableProperty] private ObservableCollection<string> _targetClassList = new();
        [ObservableProperty] private string _selectedTargetClass = string.Empty;
        [ObservableProperty] private ObservableCollection<string> _targetGradeList = new() { "Khối 10", "Khối 11", "Khối 12" };
        [ObservableProperty] private string _selectedTargetGrade = "Khối 10";

        [ObservableProperty] private Visibility _classSelectorVisibility = Visibility.Collapsed;
        [ObservableProperty] private Visibility _gradeSelectorVisibility = Visibility.Collapsed;

        public PushNotificationCenterViewModel()
        {
            _db = new AppDbContext();
            _mobileApiService = new MobileApiService(_db);
        }

        public async Task InitializeAsync()
        {
            try
            {
                var classes = await _db.Students
                    .Select(s => s.ClassName)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
                TargetClassList = new ObservableCollection<string>(classes);
                if (classes.Any())
                {
                    SelectedTargetClass = classes.First();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[PushNotification] Load classes failed");
            }

            await LoadPushHistoryAsync();
        }

        partial void OnSelectedTargetChanged(string value)
        {
            ClassSelectorVisibility = (value == "ParentClass" || value == "StudentClass") ? Visibility.Visible : Visibility.Collapsed;
            GradeSelectorVisibility = (value == "ParentGrade" || value == "StudentGrade") ? Visibility.Visible : Visibility.Collapsed;
        }

        [RelayCommand]
        private async Task LoadPushHistoryAsync()
        {
            try
            {
                var logs = await _db.PushMessageLogs.OrderByDescending(l => l.SentAt).Take(100).ToListAsync();
                var displayLogs = logs.Select(l => new PushMessageLogDisplay
                {
                    Id = l.Id,
                    RecipientId = l.RecipientId,
                    RecipientRole = l.RecipientRole,
                    Title = l.Title,
                    Body = l.Body,
                    Type = l.Type,
                    SentAt = l.SentAt,
                    Status = l.Status,
                    TargetClass = l.TargetClass,
                    TargetGrade = l.TargetGrade
                }).ToList();

                History = new ObservableCollection<PushMessageLogDisplay>(displayLogs);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[PushNotification] Load failed");
            }
        }

        [RelayCommand]
        private async Task SendPushAsync()
        {
            bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Body))
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập tiêu đề và nội dung thông báo.", "Lỗi");
                return;
            }

            try
            {
                string role = (SelectedTarget == "AllParents" || SelectedTarget == "ParentClass" || SelectedTarget == "ParentGrade") ? "Parent" : "Student";
                
                string targetClass = (SelectedTarget == "ParentClass" || SelectedTarget == "StudentClass") ? SelectedTargetClass : "";
                string targetGrade = (SelectedTarget == "ParentGrade" || SelectedTarget == "StudentGrade") ? SelectedTargetGrade : "";

                string finalBody = Body.Trim();
                if (SelectedType == "Emergency")
                {
                    string hotline = "1900-6789";
                    var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "EmergencyHotline");
                    if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                    {
                        hotline = setting.Value.Trim();
                    }
                    string footer = $"\n\n[Đường dây nóng hỗ trợ khẩn cấp của nhà trường: {hotline}]";
                    if (!finalBody.Contains(hotline))
                    {
                        finalBody += footer;
                    }
                }

                await _mobileApiService.SendPushNotificationAsync(0, role, Title.Trim(), finalBody, SelectedType, targetClass, targetGrade);
                string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
                QASmartClass.Services.AuditHelper.Log(_db, "Send_PushNotification", actor, $"Sent notification Title: {Title.Trim()} to {SelectedTarget} (Role: {role})");
                
                await AppServices.UIService.ShowInfoAsync($"Đã gửi thông báo đẩy (Push) thành công!", "Thành công");
                
                Title = string.Empty;
                Body = string.Empty;
                
                await LoadPushHistoryAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[PushNotification] Send failed");
                await AppServices.UIService.ShowInfoAsync("Lỗi khi gửi thông báo.", "Lỗi");
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class PushMessageLogDisplay
    {
        public int Id { get; set; }
        public int RecipientId { get; set; }
        public string RecipientRole { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TargetClass { get; set; } = string.Empty;
        public string TargetGrade { get; set; } = string.Empty;

        public string FriendlyRecipient
        {
            get
            {
                string role = RecipientRole switch
                {
                    "Parent" => "Phụ huynh",
                    "Student" => "Học sinh",
                    "AllParents" => "Tất cả Phụ huynh",
                    "AllStudents" => "Tất cả Học sinh",
                    _ => RecipientRole
                };
                if (!string.IsNullOrEmpty(TargetClass))
                    return $"{role} ({TargetClass})";
                if (!string.IsNullOrEmpty(TargetGrade))
                    return $"{role} ({TargetGrade})";
                return role;
            }
        }

        public string FriendlyType => Type switch
        {
            "General" => "Chung",
            "Academic" => "Học tập",
            "Payment" => "Học phí",
            "Emergency" => "Khẩn cấp",
            _ => Type
        };

        public string FriendlyStatus => Status switch
        {
            "Sent" => "Đã gửi",
            "Delivered" => "Đã nhận",
            "Read" => "Đã đọc",
            "Failed" => "Thất bại",
            _ => Status
        };
    }
}

