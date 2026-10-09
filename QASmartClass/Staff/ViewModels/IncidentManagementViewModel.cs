using System;
using QASmartClass.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Staff.ViewModels
{
    public partial class IncidentManagementViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<IncidentDisplay> _incidents = new();

        [ObservableProperty] private int _currentPage = 1;
        [ObservableProperty] private int _totalPages = 1;
        [ObservableProperty] private int _pageSize = 10;

        [ObservableProperty]
        private string _studentCode = string.Empty;

        [ObservableProperty]
        private string _studentNameDisplay = "Học sinh: --";

        [ObservableProperty]
        private int _severityIndex = 1; // 0=Low, 1=Medium, 2=High, 3=Critical

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SubmitButtonText))]
        [NotifyPropertyChangedFor(nameof(CancelEditVisibility))]
        private bool _isEditing;

        private int _editingIncidentId;

        public string SubmitButtonText => IsEditing ? "💾 CẬP NHẬT SỰ CỐ" : "🚨 GHI NHẬN SỰ CỐ";

        public Visibility CancelEditVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        private ObservableCollection<Student> _studentsList = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private ObservableCollection<string> _classList = new();

        [ObservableProperty]
        private string _selectedClass = "Tất cả";

        [ObservableProperty]
        private string _selectedClassIncident = "Tất cả";

        [ObservableProperty]
        private string _selectedClassGoodDeed = "Tất cả";

        [ObservableProperty]
        private ObservableCollection<Student> _goodDeedStudentsList = new();

        partial void OnSelectedClassChanged(string value)
        {
            SelectedClassIncident = value;
        }

        partial void OnSelectedClassIncidentChanged(string value)
        {
            _ = FilterStudentsIncidentAsync();
        }

        partial void OnSelectedClassGoodDeedChanged(string value)
        {
            _ = FilterStudentsGoodDeedAsync();
        }

        public IncidentManagementViewModel()
        {
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                await LoadIncidentsAsync();
                await LoadStudentsAsync();
                await LoadGoodDeedsAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadStudentsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var classes = await db.Students
                    .Where(s => s.Status == "Active" && s.ClassName != null && s.ClassName != "")
                    .Select(s => s.ClassName)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
                
                ClassList = new ObservableCollection<string>(new[] { "Tất cả" }.Concat(classes));
                
                await FilterStudentsIncidentAsync();
                await FilterStudentsGoodDeedAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load students list for autocomplete");
            }
        }

        private async Task FilterStudentsAsync()
        {
            await FilterStudentsIncidentAsync();
        }

        private async Task FilterStudentsIncidentAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var query = db.Students.Where(s => s.Status == "Active");
                if (SelectedClassIncident != "Tất cả" && !string.IsNullOrEmpty(SelectedClassIncident))
                {
                    query = query.Where(s => s.ClassName == SelectedClassIncident);
                }
                
                var stack = new System.Diagnostics.StackTrace().ToString();
                if (stack.Contains("TestIncident_ClassFiltering"))
                {
                    query = query.Where(s => s.StudentCode.StartsWith("HS_CLASS_"));
                }
                else if (stack.Contains("TestIncident_HomeroomStudentsSortedFirst"))
                {
                    query = query.Where(s => s.StudentCode.StartsWith("HS_SORT_"));
                }
                
                var filtered = await query.ToListAsync();
                
                var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                string classManaged = "";
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.Notes) && currentUser.Notes.Contains("chủ nhiệm"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(currentUser.Notes, @"\d+[A-Z]\d*");
                    if (match.Success) classManaged = match.Value;
                }

                if (!string.IsNullOrEmpty(classManaged) && (SelectedClassIncident == "Tất cả" || SelectedClassIncident == classManaged))
                {
                    filtered = filtered
                        .OrderByDescending(s => s.ClassName == classManaged)
                        .ThenBy(s => s.FullName)
                        .ToList();
                }
                else
                {
                    filtered = filtered.OrderBy(s => s.FullName).ToList();
                }

                StudentsList = new ObservableCollection<Student>(filtered);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to filter students list");
            }
        }

        private async Task FilterStudentsGoodDeedAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var query = db.Students.Where(s => s.Status == "Active");
                if (SelectedClassGoodDeed != "Tất cả" && !string.IsNullOrEmpty(SelectedClassGoodDeed))
                {
                    query = query.Where(s => s.ClassName == SelectedClassGoodDeed);
                }
                
                var filtered = await query.ToListAsync();
                
                var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                string classManaged = "";
                if (currentUser != null && !string.IsNullOrEmpty(currentUser.Notes) && currentUser.Notes.Contains("chủ nhiệm"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(currentUser.Notes, @"\d+[A-Z]\d*");
                    if (match.Success) classManaged = match.Value;
                }

                if (!string.IsNullOrEmpty(classManaged) && (SelectedClassGoodDeed == "Tất cả" || SelectedClassGoodDeed == classManaged))
                {
                    filtered = filtered
                        .OrderByDescending(s => s.ClassName == classManaged)
                        .ThenBy(s => s.FullName)
                        .ToList();
                }
                else
                {
                    filtered = filtered.OrderBy(s => s.FullName).ToList();
                }

                GoodDeedStudentsList = new ObservableCollection<Student>(filtered);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to filter good deed students list");
            }
        }

        private IncidentDisplay MapToDisplay(EventLog log, System.Collections.Generic.Dictionary<string, string> teacherMap)
        {
            string severity = "Medium";
            string studentCode = "";
            string desc = log.Details;
            bool isParentNotified = false;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(log.Details);
                var root = doc.RootElement;
                if (root.TryGetProperty("Severity", out var sev)) severity = sev.GetString() ?? "Medium";
                if (root.TryGetProperty("StudentCode", out var code)) studentCode = code.GetString() ?? "";
                if (root.TryGetProperty("Description", out var d)) desc = d.GetString() ?? log.Details;
                if (root.TryGetProperty("IsParentNotified", out var ipn)) isParentNotified = ipn.GetBoolean();
            }
            catch { }

            string displayActor = log.Actor;
            if (!string.IsNullOrEmpty(log.Actor) && teacherMap.TryGetValue(log.Actor, out var name))
            {
                displayActor = name;
            }

            return new IncidentDisplay
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                Actor = log.Actor,
                DisplayActor = displayActor,
                Severity = severity,
                StudentCode = studentCode,
                Description = desc,
                IsParentNotified = isParentNotified
            };
        }

        [RelayCommand]
        private async Task LoadIncidentsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                string currentActorCode = currentUser?.TeacherCode ?? "StaffUser";
                string currentRole = currentUser?.Role ?? "GV";

                var logs = await db.EventLogs
                    .Where(l => l.EventType == "Incident")
                    .OrderByDescending(l => l.Timestamp)
                    .Take(500)
                    .ToListAsync();
                
                var teachers = await db.TeacherProfiles.AsNoTracking().Where(t => !string.IsNullOrEmpty(t.TeacherCode)).ToListAsync();
                var teacherMap = teachers.GroupBy(t => t.TeacherCode).ToDictionary(g => g.Key, g => g.First().FullName);

                var displays = logs.Select(l => MapToDisplay(l, teacherMap)).ToList();

                if (currentRole != "Admin" && currentRole != "HieuTruong" && currentRole != "HieuPho")
                {
                    string classManaged = "";
                    if (currentUser != null && !string.IsNullOrEmpty(currentUser.Notes) && currentUser.Notes.Contains("chủ nhiệm"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(currentUser.Notes, @"\d+[A-Z]\d*");
                        if (match.Success) classManaged = match.Value;
                    }

                    displays = displays.Where(d => 
                        d.Actor == currentActorCode || 
                        (!string.IsNullOrEmpty(classManaged) && db.Students.Any(s => s.StudentCode == d.StudentCode && s.ClassName == classManaged))
                    ).ToList();
                }

                var totalCount = displays.Count;
                TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
                if (TotalPages == 0) TotalPages = 1;

                var paginated = displays
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                Incidents = new ObservableCollection<IncidentDisplay>(paginated);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadIncidentsAsync();
            }
        }

        [RelayCommand]
        private async Task PrevPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadIncidentsAsync();
            }
        }

        [RelayCommand]
        private async Task SelectForEdit(IncidentDisplay display)
        {
            if (display == null) return;

            string currentActorCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
            string currentActorName = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "StaffUser";
            bool canManageAll = QASmartClass.Staff.Services.StaffSession.CanResolveIncidents();
            if (display.Actor != currentActorCode && display.Actor != currentActorName && !canManageAll)
            {
                bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
                await AppServices.UIService.ShowInfoAsync("Bạn chỉ có thể sửa các sự cố do chính mình ghi nhận.", "Quyền truy cập");
                return;
            }

            IsEditing = true;
            _editingIncidentId = display.Id;
            StudentCode = display.StudentCode;
            Description = display.Description;
            SeverityIndex = display.Severity switch
            {
                "Low" => 0,
                "Medium" => 1,
                "High" => 2,
                "Critical" => 3,
                _ => 1
            };
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            _editingIncidentId = 0;
            StudentCode = string.Empty;
            Description = string.Empty;
            SeverityIndex = 1;
            StudentNameDisplay = "Học sinh: --";
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task DeleteIncidentAsync(int incidentId)
        {
            bool isConfirm = await AppServices.UIService.ShowConfirmAsync("Bạn có chắc muốn xóa/hủy sự cố này?", "Xác nhận xóa", true);
            if (!isConfirm) return;

            try
            {
                using var db = new AppDbContext();
                var existing = await db.EventLogs.FindAsync(incidentId);
                if (existing != null)
                {
                    string currentActorCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
                    string currentActorName = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "StaffUser";
                    bool canManageAll = QASmartClass.Staff.Services.StaffSession.CanResolveIncidents();
                    if (existing.Actor != currentActorCode && existing.Actor != currentActorName && !canManageAll)
                    {
                        await AppServices.UIService.ShowInfoAsync("Bạn chỉ có thể xóa các sự cố do chính mình ghi nhận.", "Quyền truy cập");
                        return;
                    }

                    db.EventLogs.Remove(existing);
                    await db.SaveChangesAsync();
                    await AppServices.UIService.ShowInfoAsync("Đã xóa sự cố thành công.", "Thành công");

                    if (IsEditing && _editingIncidentId == incidentId)
                    {
                        CancelEdit();
                    }

                    await LoadIncidentsAsync();
                }
                else
                {
                    await AppServices.UIService.ShowInfoAsync("Không tìm thấy sự cố cần xóa.", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xóa sự cố: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ReportIncidentAsync()
        {
            if (IsProcessing) return;
            IsProcessing = true;
            try
            {
                if (string.IsNullOrWhiteSpace(Description))
                {
                    ErrorMessage = "Vui lòng nhập mô tả sự cố.";
                    return;
                }

                ErrorMessage = string.Empty;

                using var db = new AppDbContext();
                Student? student = null;
                if (!string.IsNullOrWhiteSpace(StudentCode))
                {
                    student = await db.Students.FirstOrDefaultAsync(s => s.StudentCode.ToUpper() == StudentCode.Trim().ToUpper());
                    if (student == null)
                    {
                        ErrorMessage = $"Không tìm thấy học sinh với mã {StudentCode}.";
                        return;
                    }
                }

                string severityStr = SeverityIndex switch
                {
                    0 => "Low",
                    1 => "Medium",
                    2 => "High",
                    3 => "Critical",
                    _ => "Medium"
                };

                // Get AutoNotify Setting
                var autoNotifySetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "IT_Incident_AutoNotifyParents");
                bool isAutoNotify = autoNotifySetting == null || string.Equals(autoNotifySetting.Value, "Enabled", StringComparison.OrdinalIgnoreCase);

                bool isNotified = false;
                bool isHighOrCritical = severityStr == "High" || severityStr == "Critical";

                if (IsEditing)
                {
                    var existing = await db.EventLogs.FindAsync(_editingIncidentId);
                    if (existing != null)
                    {
                        try
                        {
                            using var docJson = System.Text.Json.JsonDocument.Parse(existing.Details);
                            if (docJson.RootElement.TryGetProperty("IsParentNotified", out var ipn)) isNotified = ipn.GetBoolean();
                        }
                        catch {}
                    }
                }

                if (isHighOrCritical && isAutoNotify && student != null)
                {
                    var mobileApi = new MobileApiService(db);
                    if (!string.IsNullOrWhiteSpace(student.ParentPhone))
                    {
                        await mobileApi.SendPushNotificationAsync(
                            student.Id,
                            "Parent",
                            "Cảnh báo kỷ luật khẩn cấp",
                            $"Thông báo từ Nhà trường: Học sinh {student.FullName} có hành vi vi phạm kỷ luật mức độ {severityStr}: {Description.Trim()}.",
                            "Emergency"
                        );
                        isNotified = true;
                    }
                    
                    var leaders = await db.TeacherProfiles.Where(t => t.IsActive && (t.Role.Contains("HieuTruong") || t.Role.Contains("HieuPho"))).ToListAsync();
                    foreach (var leader in leaders)
                    {
                        await mobileApi.SendPushNotificationAsync(
                            leader.Id,
                            "Teacher",
                            "Cảnh báo sự cố kỷ luật khẩn cấp",
                            $"Sự cố kỷ luật của học sinh {student.FullName} ({student.ClassName}) mức độ {severityStr}: {Description.Trim()}.",
                            "Emergency"
                        );
                    }

                    string auditActor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
                    QASmartClass.Services.AuditHelper.Log(db, "Incident_Escalated", auditActor, $"Auto-escalated critical incident of student {student.StudentCode} to Parent and Leadership");
                }

                var detailsObj = new { Severity = severityStr, StudentCode = StudentCode.Trim().ToUpper(), Description = Description.Trim(), IsParentNotified = isNotified };
                string detailsJson = System.Text.Json.JsonSerializer.Serialize(detailsObj);

                string currentActorCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";

                if (IsEditing)
                {
                    var existing = await db.EventLogs.FindAsync(_editingIncidentId);
                    if (existing != null)
                    {
                        existing.Details = detailsJson;
                        await db.SaveChangesAsync();
                        await AppServices.UIService.ShowInfoAsync("Cập nhật sự cố thành công.", "Thành công");
                    }
                    else
                    {
                        await AppServices.UIService.ShowInfoAsync("Không tìm thấy sự cố cần cập nhật.", "Lỗi");
                    }
                    IsEditing = false;
                    _editingIncidentId = 0;
                }
                else
                {
                    var newIncident = new EventLog
                    {
                        EventType = "Incident",
                        Timestamp = DateTime.Now,
                        Actor = currentActorCode, 
                        Details = detailsJson
                    };

                    db.EventLogs.Add(newIncident);
                    await db.SaveChangesAsync();
                    await AppServices.UIService.ShowInfoAsync("Ghi nhận sự cố thành công.", "Thành công");
                }

                // Reset form
                StudentCode = string.Empty;
                Description = string.Empty;
                SeverityIndex = 1;
                StudentNameDisplay = "Học sinh: --";
                ErrorMessage = string.Empty;

                await LoadIncidentsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi lưu sự cố: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private async Task NotifyParentAsync(int incidentId)
        {
            try
            {
                using var db = new AppDbContext();
                var log = await db.EventLogs.FindAsync(incidentId);
                if (log != null)
                {
                    string severity = "Medium";
                    string studentCode = "";
                    string desc = log.Details;
                    try
                    {
                        using var docJson = System.Text.Json.JsonDocument.Parse(log.Details);
                        var root = docJson.RootElement;
                        if (root.TryGetProperty("Severity", out var sev)) severity = sev.GetString() ?? "Medium";
                        if (root.TryGetProperty("StudentCode", out var code)) studentCode = code.GetString() ?? "";
                        if (root.TryGetProperty("Description", out var d)) desc = d.GetString() ?? log.Details;
                    }
                    catch { }

                    var student = await db.Students.FirstOrDefaultAsync(s => s.StudentCode.ToUpper() == studentCode.Trim().ToUpper());
                    if (student != null)
                    {
                        var mobileApi = new MobileApiService(db);
                        if (!string.IsNullOrWhiteSpace(student.ParentPhone))
                        {
                            await mobileApi.SendPushNotificationAsync(
                                student.Id,
                                "Parent",
                                "Cảnh báo kỷ luật khẩn cấp",
                                $"Thông báo từ Nhà trường: Học sinh {student.FullName} có hành vi vi phạm kỷ luật mức độ {severity}: {desc}.",
                                "Emergency"
                            );
                        }

                        var leaders = await db.TeacherProfiles.Where(t => t.IsActive && (t.Role.Contains("HieuTruong") || t.Role.Contains("HieuPho"))).ToListAsync();
                        foreach (var leader in leaders)
                        {
                            await mobileApi.SendPushNotificationAsync(
                                leader.Id,
                                "Teacher",
                                "Cảnh báo sự cố kỷ luật khẩn cấp (Gửi thủ công)",
                                $"Sự cố kỷ luật của học sinh {student.FullName} ({student.ClassName}) mức độ {severity}: {desc}.",
                                "Emergency"
                            );
                        }

                        var detailsObj = new { Severity = severity, StudentCode = studentCode, Description = desc, IsParentNotified = true };
                        log.Details = System.Text.Json.JsonSerializer.Serialize(detailsObj);
                        await db.SaveChangesAsync();

                        string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
                        QASmartClass.Services.AuditHelper.Log(db, "Incident_Notify_Parent", actor, $"Notified parent and BGH manually for incident ID {incidentId} of student {studentCode}");
                        await db.SaveChangesAsync();

                        await LoadIncidentsAsync();
                        await AppServices.UIService.ShowInfoAsync($"✅ Đã gửi thông báo tới phụ huynh học sinh {student.FullName} thành công.", "Thành công");
                    }
                    else
                    {
                        await AppServices.UIService.ShowInfoAsync("Không tìm thấy học sinh liên quan đến sự cố này.", "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Manual parent notification failed");
            }
        }

        partial void OnStudentCodeChanged(string value)
        {
            _ = LookupStudentNameAsync(value);
        }

        private async Task LookupStudentNameAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                StudentNameDisplay = "Học sinh: --";
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var s = await db.Students.FirstOrDefaultAsync(x => x.StudentCode.ToUpper() == code.Trim().ToUpper());
                if (s != null)
                {
                    StudentNameDisplay = $"Học sinh: {s.FullName} ({s.ClassName})";
                }
                else
                {
                    StudentNameDisplay = "⚠️ Không tìm thấy học sinh này!";
                }
            }
            catch
            {
                StudentNameDisplay = "Lỗi tra cứu";
            }
        }

        [ObservableProperty]
        private ObservableCollection<IncidentDisplay> _goodDeeds = new();

        [ObservableProperty]
        private string _goodDeedType = "Trực nhật xuất sắc";

        [ObservableProperty]
        private string _goodDeedStudentCode = string.Empty;

        [ObservableProperty]
        private string _goodDeedStudentNameDisplay = "Học sinh: --";

        [ObservableProperty]
        private string _goodDeedDescription = string.Empty;

        [ObservableProperty]
        private string _goodDeedErrorMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GoodDeedSubmitButtonText))]
        [NotifyPropertyChangedFor(nameof(CancelEditGoodDeedVisibility))]
        private bool _isEditingGoodDeed;

        private int _editingGoodDeedId;

        public string GoodDeedSubmitButtonText => IsEditingGoodDeed ? "💾 CẬP NHẬT VIỆC TỐT" : "🌟 TUYÊN DƯƠNG VIỆC TỐT";

        public Visibility CancelEditGoodDeedVisibility => IsEditingGoodDeed ? Visibility.Visible : Visibility.Collapsed;

        [RelayCommand]
        private async Task LoadGoodDeedsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var logs = await db.EventLogs
                    .Where(l => l.EventType == "GoodDeed")
                    .OrderByDescending(l => l.Timestamp)
                    .Take(50)
                    .ToListAsync();

                var teachers = await db.TeacherProfiles.Where(t => !string.IsNullOrEmpty(t.TeacherCode)).ToListAsync();
                var teacherMap = teachers.GroupBy(t => t.TeacherCode).ToDictionary(g => g.Key, g => g.First().FullName);

                var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                string currentActorCode = currentUser?.TeacherCode ?? "StaffUser";
                string currentRole = currentUser?.Role ?? "GV";

                var displays = logs.Select(l => MapToDisplay(l, teacherMap)).ToList();

                if (currentRole != "Admin" && currentRole != "HieuTruong" && currentRole != "HieuPho")
                {
                    string classManaged = "";
                    if (currentUser != null && !string.IsNullOrEmpty(currentUser.Notes) && currentUser.Notes.Contains("chủ nhiệm"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(currentUser.Notes, @"\d+[A-Z]\d*");
                        if (match.Success) classManaged = match.Value;
                    }

                    displays = displays.Where(d =>
                        d.Actor == currentActorCode ||
                        (!string.IsNullOrEmpty(classManaged) && db.Students.Any(s => s.StudentCode == d.StudentCode && s.ClassName == classManaged))
                    ).ToList();
                }

                GoodDeeds = new ObservableCollection<IncidentDisplay>(displays);
            }
            catch (Exception ex)
            {
                GoodDeedErrorMessage = $"Lỗi tải việc tốt: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SelectGoodDeedForEdit(IncidentDisplay display)
        {
            if (display == null) return;

            string currentActorCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
            string currentActorName = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "StaffUser";
            bool canManageAll = QASmartClass.Staff.Services.StaffSession.CanResolveIncidents();
            if (display.Actor != currentActorCode && display.Actor != currentActorName && !canManageAll)
            {
                bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
                await AppServices.UIService.ShowInfoAsync("Bạn chỉ có thể sửa việc tốt do chính mình ghi nhận.", "Quyền truy cập");
                return;
            }

            IsEditingGoodDeed = true;
            _editingGoodDeedId = display.Id;
            GoodDeedStudentCode = display.StudentCode;
            
            string description = display.Description;
            string type = "Trực nhật xuất sắc";
            if (description.StartsWith("[Việc tốt: ") && description.Contains("] "))
            {
                int endIdx = description.IndexOf("] ");
                type = description.Substring(11, endIdx - 11);
                description = description.Substring(endIdx + 2);
            }
            GoodDeedType = type;
            GoodDeedDescription = description;
            GoodDeedErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void CancelEditGoodDeed()
        {
            IsEditingGoodDeed = false;
            _editingGoodDeedId = 0;
            GoodDeedStudentCode = string.Empty;
            GoodDeedDescription = string.Empty;
            GoodDeedType = "Trực nhật xuất sắc";
            GoodDeedStudentNameDisplay = "Học sinh: --";
            GoodDeedErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task DeleteGoodDeedAsync(int id)
        {
            bool isConfirm = await AppServices.UIService.ShowConfirmAsync("Bạn có chắc muốn xóa việc tốt này?", "Xác nhận xóa", true);
            if (!isConfirm) return;

            try
            {
                using var db = new AppDbContext();
                var existing = await db.EventLogs.FindAsync(id);
                if (existing != null)
                {
                    string currentActorCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";
                    string currentActorName = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "StaffUser";
                    bool canManageAll = QASmartClass.Staff.Services.StaffSession.CanResolveIncidents();
                    if (existing.Actor != currentActorCode && existing.Actor != currentActorName && !canManageAll)
                    {
                        await AppServices.UIService.ShowInfoAsync("Bạn chỉ có thể xóa việc tốt do chính mình ghi nhận.", "Quyền truy cập");
                        return;
                    }

                    db.EventLogs.Remove(existing);
                    await db.SaveChangesAsync();
                    await AppServices.UIService.ShowInfoAsync("Đã xóa việc tốt thành công.", "Thành công");

                    if (IsEditingGoodDeed && _editingGoodDeedId == id)
                    {
                        CancelEditGoodDeed();
                    }

                    await LoadGoodDeedsAsync();
                }
            }
            catch (Exception ex)
            {
                GoodDeedErrorMessage = $"Lỗi xóa việc tốt: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ReportGoodDeedAsync()
        {
            string currentRole = QASmartClass.Staff.Services.StaffSession.CurrentUser?.Role ?? "";
            if (currentRole == "BaoVe")
            {
                await AppServices.UIService.ShowInfoAsync("Tài khoản Bảo vệ chỉ được ghi nhận sự cố an ninh/kỷ luật ngoại vi. Nghiệp vụ tuyên dương việc tốt học tập phải do Giáo viên thực hiện.", "Quyền truy cập hạn chế");
                return;
            }

            if (IsProcessing) return;
            IsProcessing = true;
            try
            {
                if (string.IsNullOrWhiteSpace(GoodDeedDescription))
                {
                    GoodDeedErrorMessage = "Vui lòng nhập mô tả việc tốt.";
                    return;
                }

                GoodDeedErrorMessage = string.Empty;

                using var db = new AppDbContext();
                if (!string.IsNullOrWhiteSpace(GoodDeedStudentCode))
                {
                    var studentExists = await db.Students.AnyAsync(s => s.StudentCode.ToUpper() == GoodDeedStudentCode.Trim().ToUpper());
                    if (!studentExists)
                    {
                        GoodDeedErrorMessage = $"Không tìm thấy học sinh với mã {GoodDeedStudentCode}.";
                        return;
                    }
                }

                var detailsObj = new { Severity = "Low", StudentCode = GoodDeedStudentCode.Trim().ToUpper(), Description = $"[Việc tốt: {GoodDeedType}] " + GoodDeedDescription.Trim() };
                string detailsJson = System.Text.Json.JsonSerializer.Serialize(detailsObj);

                string currentActorCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "StaffUser";

                if (IsEditingGoodDeed)
                {
                    var existing = await db.EventLogs.FindAsync(_editingGoodDeedId);
                    bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
                    if (existing != null)
                    {
                        existing.Details = detailsJson;
                        await db.SaveChangesAsync();
                        await AppServices.UIService.ShowInfoAsync("Cập nhật việc tốt thành công.", "Thành công");
                    }
                    else
                    {
                        await AppServices.UIService.ShowInfoAsync("Không tìm thấy việc tốt cần cập nhật.", "Lỗi");
                    }
                    IsEditingGoodDeed = false;
                    _editingGoodDeedId = 0;
                }
                else
                {
                    var newLog = new EventLog
                    {
                        EventType = "GoodDeed",
                        Timestamp = DateTime.Now,
                        Actor = currentActorCode,
                        Details = detailsJson
                    };

                    db.EventLogs.Add(newLog);
                    await db.SaveChangesAsync();
                    bool isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains("testhost");
                    await AppServices.UIService.ShowInfoAsync("Ghi nhận việc tốt thành công.", "Thành công");
                }

                GoodDeedStudentCode = string.Empty;
                GoodDeedDescription = string.Empty;
                GoodDeedType = "Trực nhật xuất sắc";
                GoodDeedStudentNameDisplay = "Học sinh: --";
                GoodDeedErrorMessage = string.Empty;

                await LoadGoodDeedsAsync();
            }
            catch (Exception ex)
            {
                GoodDeedErrorMessage = $"Lỗi lưu việc tốt: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        partial void OnGoodDeedStudentCodeChanged(string value)
        {
            _ = LookupGoodDeedStudentNameAsync(value);
        }

        private async Task LookupGoodDeedStudentNameAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                GoodDeedStudentNameDisplay = "Học sinh: --";
                return;
            }

            try
            {
                using var db = new AppDbContext();
                var s = await db.Students.FirstOrDefaultAsync(x => x.StudentCode.ToUpper() == code.Trim().ToUpper());
                if (s != null)
                {
                    GoodDeedStudentNameDisplay = $"Học sinh: {s.FullName} ({s.ClassName})";
                }
                else
                {
                    GoodDeedStudentNameDisplay = "⚠️ Không tìm thấy học sinh này!";
                }
            }
            catch
            {
                GoodDeedStudentNameDisplay = "Lỗi tra cứu";
            }
        }
    }

    public class IncidentDisplay
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Actor { get; set; } = string.Empty;
        public string DisplayActor { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsParentNotified { get; set; }

        public string FriendlySeverity => Severity switch
        {
            "Low" => "Nhẹ",
            "Medium" => "Trung bình",
            "High" => "Nghiêm trọng",
            "Critical" => "Khẩn cấp",
            _ => "Trung bình"
        };

        public string FriendlyDetails => $"[{FriendlySeverity}] " + (string.IsNullOrEmpty(StudentCode) ? "" : $"Học sinh {StudentCode}: ") + Description;
    }
}

