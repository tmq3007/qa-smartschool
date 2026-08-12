using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.HomeroomHub.ViewModels
{
    public partial class HomeroomHubViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableObject _currentViewModel;

        private HomeroomDashboardViewModel _dashboardVM = new();
        private HomeroomInboxViewModel _inboxVM = new();
        private ContactBookViewModel? _contactBookVM;

        [ObservableProperty]
        private int _unreadMessagesCount;

        public HomeroomHubViewModel()
        {
            CurrentViewModel = _dashboardVM;
            _ = LoadUnreadCountAsync();
        }

        private async Task LoadUnreadCountAsync()
        {
            try
            {
                using var db = new AppDbContext();
                string teacherCode = QASmartClass.Services.UserSessionService.Instance.TeacherCode;
                if (string.IsNullOrEmpty(teacherCode))
                {
                    teacherCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV001";
                }
                UnreadMessagesCount = await db.InboxMessages.CountAsync(m => m.ReceiverId == teacherCode && !m.IsRead);
            }
            catch { }
        }

        [RelayCommand]
        private void NavigateToDashboard() => CurrentViewModel = _dashboardVM;

        [RelayCommand]
        private void NavigateToInbox()
        {
            CurrentViewModel = _inboxVM;
            _ = _inboxVM.LoadMessagesAsync();
            _ = LoadUnreadCountAsync(); // Refresh count
        }

        [RelayCommand]
        private void NavigateToContactBook()
        {
            if (_contactBookVM == null)
            {
                _contactBookVM = new ContactBookViewModel();
            }
            else
            {
                _ = _contactBookVM.LoadDataAsync();
            }
            CurrentViewModel = _contactBookVM;
        }
    }

    public partial class HomeroomDashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private ClassRoster? _homeroomClass;

        [ObservableProperty]
        private ObservableCollection<StudentDisplayModel> _students = new();

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _attendanceSummary = string.Empty;

        // Conduct Popup
        [ObservableProperty]
        private bool _isConductPopupOpen;

        [ObservableProperty]
        private StudentDisplayModel? _selectedConductStudent;

        [ObservableProperty]
        private int _conductDelta;

        [ObservableProperty]
        private string _conductReason = string.Empty;

        public HomeroomDashboardViewModel()
        {
            _ = LoadHomeroomDataAsync();
        }

        private async Task LoadHomeroomDataAsync()
        {
            StatusMessage = "Đang tải dữ liệu lớp chủ nhiệm...";
            try
            {
                using var db = new AppDbContext();
                // Giả định: Tìm lớp mà GV này làm chủ nhiệm (lấy tạm lớp đầu tiên)
                var roster = await db.ClassRosters.FirstOrDefaultAsync(r => r.IsActive);
                
                if (roster == null)
                {
                    StatusMessage = "Không tìm thấy lớp chủ nhiệm nào.";
                    return;
                }
                
                HomeroomClass = roster;

                var rosterStudents = await db.ClassRosterStudents
                    .Where(rs => rs.RosterId == roster.Id)
                    .OrderBy(rs => rs.SeatNumber)
                    .ToListAsync();
                    
                var studentIds = rosterStudents.Select(rs => rs.StudentId).ToList();
                var students = await db.Students.Where(s => studentIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);

                var today = DateTime.Today;
                var attendance = await db.AttendanceRecords
                    .Where(a => a.RosterId == roster.Id && a.Date.Date == today)
                    .ToListAsync();

                var list = new ObservableCollection<StudentDisplayModel>();
                foreach (var rs in rosterStudents)
                {
                    if (students.TryGetValue(rs.StudentId, out var student))
                    {
                        var att = attendance.FirstOrDefault(a => a.StudentId == student.Id);
                        list.Add(new StudentDisplayModel
                        {
                            Student = student,
                            SeatNumber = rs.SeatNumber,
                            AvatarPath = string.IsNullOrWhiteSpace(student.AvatarPath) ? "pack://application:,,,/Resources/default_avatar.png" : student.AvatarPath,
                            AttendanceStatus = att != null ? att.Status : "Chưa điểm danh",
                            ConductScore = student.ConductScore
                        });
                    }
                }

                Students = list;
                UpdateAttendanceSummary();
                StatusMessage = $"Đã tải danh sách lớp {roster.ClassName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        private void UpdateAttendanceSummary()
        {
            int total = Students.Count;
            int present = Students.Count(s => s.AttendanceStatus == "Có mặt");
            int absent = Students.Count(s => s.AttendanceStatus == "Vắng");
            int late = Students.Count(s => s.AttendanceStatus == "Trễ");
            int unknown = Students.Count(s => s.AttendanceStatus == "Chưa điểm danh");

            AttendanceSummary = $"Tổng: {total} | Có mặt: {present} | Vắng: {absent} | Trễ: {late} | Chưa ĐD: {unknown}";
        }

        [RelayCommand]
        private async Task StartAttendanceAsync()
        {
            if (HomeroomClass == null) return;
            StatusMessage = "Đang đánh dấu tất cả có mặt...";
            try
            {
                using var db = new AppDbContext();
                var today = DateTime.Today;
                var existingRecords = await db.AttendanceRecords
                    .Where(a => a.RosterId == HomeroomClass.Id && a.Date.Date == today)
                    .ToListAsync();

                foreach (var st in Students)
                {
                    if (st.AttendanceStatus == "Chưa điểm danh")
                    {
                        st.AttendanceStatus = "Có mặt";
                        var att = existingRecords.FirstOrDefault(a => a.StudentId == st.Student.Id);
                        if (att != null)
                        {
                            att.Status = "Có mặt";
                        }
                        else
                        {
                            db.AttendanceRecords.Add(new AttendanceRecord
                            {
                                StudentId = st.Student.Id,
                                RosterId = HomeroomClass.Id,
                                Date = today,
                                Status = "Có mặt"
                            });
                        }
                    }
                }
                
                await db.SaveChangesAsync();
                UpdateAttendanceSummary();
                StatusMessage = "✅ Đã điểm danh mặc định thành công!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ToggleAttendanceAsync(StudentDisplayModel model)
        {
            if (model == null || HomeroomClass == null) return;

            if (model.AttendanceStatus == "Có mặt") model.AttendanceStatus = "Vắng";
            else if (model.AttendanceStatus == "Vắng") model.AttendanceStatus = "Trễ";
            else model.AttendanceStatus = "Có mặt";

            try
            {
                using var db = new AppDbContext();
                var today = DateTime.Today;
                var att = await db.AttendanceRecords.FirstOrDefaultAsync(a => 
                    a.StudentId == model.Student.Id && 
                    a.RosterId == HomeroomClass.Id && 
                    a.Date.Date == today);
                
                if (att != null)
                {
                    att.Status = model.AttendanceStatus;
                }
                else
                {
                    db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        StudentId = model.Student.Id,
                        RosterId = HomeroomClass.Id,
                        Date = today,
                        Status = model.AttendanceStatus
                    });
                }
                
                await db.SaveChangesAsync();
                UpdateAttendanceSummary();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi cập nhật điểm danh: {ex.Message}";
            }
        }

        [RelayCommand]
        private void AddConduct(StudentDisplayModel model)
        {
            SelectedConductStudent = model;
            ConductDelta = 5;
            ConductReason = "Tích cực tham gia bài học";
            IsConductPopupOpen = true;
        }

        [RelayCommand]
        private void SubtractConduct(StudentDisplayModel model)
        {
            SelectedConductStudent = model;
            ConductDelta = -5;
            ConductReason = "Vi phạm nội quy";
            IsConductPopupOpen = true;
        }

        [RelayCommand]
        private void CancelConduct()
        {
            IsConductPopupOpen = false;
        }

        [RelayCommand]
        private async Task SaveConductAsync()
        {
            if (SelectedConductStudent == null || HomeroomClass == null) return;
            
            try
            {
                using var db = new AppDbContext();
                
                string teacherName = QASmartClass.Services.UserSessionService.Instance.FullName;
                if (string.IsNullOrEmpty(teacherName))
                {
                    teacherName = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "GVCN";
                }

                db.ConductRecords.Add(new ConductRecord
                {
                    StudentId = SelectedConductStudent.Student.Id,
                    RosterId = HomeroomClass.Id,
                    PointsDelta = ConductDelta,
                    Reason = ConductReason,
                    TeacherName = teacherName
                });
                
                var student = await db.Students.FindAsync(SelectedConductStudent.Student.Id);
                if (student != null)
                {
                    student.ConductScore += ConductDelta;
                    if (student.ConductScore > 100) student.ConductScore = 100;
                    if (student.ConductScore < 0) student.ConductScore = 0;
                    
                    SelectedConductStudent.ConductScore = student.ConductScore;
                }
                
                await db.SaveChangesAsync();
                IsConductPopupOpen = false;
                StatusMessage = "✅ Đã lưu thay đổi hạnh kiểm!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi cập nhật hạnh kiểm: {ex.Message}";
            }
        }
    }

    public class StudentDisplayModel : ObservableObject
    {
        public Student Student { get; set; } = null!;
        public int SeatNumber { get; set; }
        public string AvatarPath { get; set; } = string.Empty;
        
        private string _attendanceStatus = string.Empty;
        public string AttendanceStatus
        {
            get => _attendanceStatus;
            set => SetProperty(ref _attendanceStatus, value);
        }
        
        private int _conductScore;
        public int ConductScore
        {
            get => _conductScore;
            set
            {
                if (SetProperty(ref _conductScore, value))
                {
                    OnPropertyChanged(nameof(ConductClassification));
                }
            }
        }

        public string ConductClassification
        {
            get
            {
                if (ConductScore >= 80) return "Tốt";
                if (ConductScore >= 65) return "Khá";
                if (ConductScore >= 50) return "Trung bình";
                return "Yếu";
            }
        }
    }

    public partial class HomeroomInboxViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<InboxMessage> _messages = new();

        [ObservableProperty]
        private InboxMessage? _selectedMessage;

        [ObservableProperty]
        private string _replyContent = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public async Task LoadMessagesAsync()
        {
            try
            {
                using var db = new AppDbContext();
                string teacherCode = QASmartClass.Services.UserSessionService.Instance.TeacherCode;
                if (string.IsNullOrEmpty(teacherCode))
                {
                    teacherCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV001";
                }
                var msgs = await db.InboxMessages
                    .Where(m => m.ReceiverId == teacherCode || m.SenderId == teacherCode)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();
                    
                Messages = new ObservableCollection<InboxMessage>(msgs);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi tải tin nhắn: {ex.Message}";
            }
        }

        partial void OnSelectedMessageChanged(InboxMessage? value)
        {
            string teacherCode = QASmartClass.Services.UserSessionService.Instance.TeacherCode;
            if (string.IsNullOrEmpty(teacherCode))
            {
                teacherCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV001";
            }
            if (value != null && !value.IsRead && value.ReceiverId == teacherCode)
            {
                _ = MarkAsReadAsync(value);
            }
            ReplyContent = string.Empty;
        }

        private async Task MarkAsReadAsync(InboxMessage msg)
        {
            try
            {
                using var db = new AppDbContext();
                var dbMsg = await db.InboxMessages.FindAsync(msg.Id);
                if (dbMsg != null)
                {
                    dbMsg.IsRead = true;
                    msg.IsRead = true;
                    await db.SaveChangesAsync();
                }
            }
            catch { }
        }

        [RelayCommand]
        private async Task SendReplyAsync()
        {
            if (SelectedMessage == null || string.IsNullOrWhiteSpace(ReplyContent)) return;

            try
            {
                using var db = new AppDbContext();
                string teacherCode = QASmartClass.Services.UserSessionService.Instance.TeacherCode;
                if (string.IsNullOrEmpty(teacherCode))
                {
                    teacherCode = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV001";
                }
                string teacherName = QASmartClass.Services.UserSessionService.Instance.FullName;
                if (string.IsNullOrEmpty(teacherName))
                {
                    teacherName = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "GV Chủ nhiệm";
                }

                var newMsg = new InboxMessage
                {
                    SenderId = teacherCode,
                    SenderName = teacherName,
                    ReceiverId = SelectedMessage.SenderId, // Gửi lại cho người gửi
                    Content = ReplyContent,
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    ThreadId = string.IsNullOrEmpty(SelectedMessage.ThreadId) ? SelectedMessage.Id.ToString() : SelectedMessage.ThreadId
                };

                db.InboxMessages.Add(newMsg);
                await db.SaveChangesAsync();

                Messages.Insert(0, newMsg); // Thêm lên đầu danh sách
                ReplyContent = string.Empty;
                StatusMessage = "✅ Đã gửi phản hồi thành công!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi gửi phản hồi: {ex.Message}";
            }
        }
        
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadMessagesAsync();
        }
    }
}

