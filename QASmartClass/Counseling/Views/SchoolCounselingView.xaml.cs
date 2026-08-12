using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Counseling.Views
{
    public partial class SchoolCounselingView : UserControl
    {
        private AppDbContext? _db;

        public SchoolCounselingView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private bool CheckPermission()
        {
            if (QASmartClass.Services.UserSessionService.Instance.IsLoggedIn)
            {
                return QASmartClass.Services.UserSessionService.Instance.IsCounselor;
            }
            if (QASmartClass.Staff.Services.StaffSession.IsLoggedIn)
            {
                var role = QASmartClass.Staff.Services.StaffSession.Role;
                return role == "Counselor" || role == "HieuTruong" || role == "Admin";
            }
            return false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();

            if (!CheckPermission())
            {
                MessageBox.Show("Bạn không có quyền truy cập module Tâm lý Học đường.\nVui lòng liên hệ Ban Giám Hiệu.",
                    "Phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                GridMainContent.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                GridMainContent.Visibility = Visibility.Visible;
            }

            DpFollowUp.DisplayDateStart = DateTime.Today;
            LoadStudents();
            LoadData();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private async void LoadStudents()
        {
            if (_db == null) return;
            try
            {
                var students = await _db.Students
                    .Where(s => s.Status == "Active")
                    .OrderBy(s => s.FullName)
                    .Select(s => new {
                        Id = s.Id,
                        DisplayInfo = s.StudentCode + " - " + s.FullName + " (" + s.ClassName + ")"
                    })
                    .ToListAsync();
                CboStudent.ItemsSource = students;
                CboStudent.DisplayMemberPath = "DisplayInfo";
                CboStudent.SelectedValuePath = "Id";
                if (students.Count > 0) CboStudent.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SchoolCounseling] Error loading students");
            }
        }

        private async void LoadData()
        {
            if (!CheckPermission()) return;
            if (_db == null) return;
            try
            {
                var query = from r in _db.StudentMentalHealthRecords
                            join s in _db.Students on r.StudentId equals s.Id
                            orderby r.RecordedAt descending
                            select new
                            {
                                r.Id,
                                StudentName = s.FullName,
                                r.RecordedAt,
                                r.MoodScore,
                                r.Notes,
                                r.RiskLevel,
                                RiskLevelDisplay = r.RiskLevel == "Critical" ? "Nguy kịch" : (r.RiskLevel == "High" ? "Cao" : (r.RiskLevel == "Medium" ? "Trung bình" : "Thấp")),
                                DetectedKeywords = string.IsNullOrEmpty(r.DetectedKeywords) ? "" : $"Từ khóa phát hiện: {r.DetectedKeywords}",
                                HasKeywords = string.IsNullOrEmpty(r.DetectedKeywords) ? Visibility.Collapsed : Visibility.Visible,
                                RiskBg = r.RiskLevel == "High" || r.RiskLevel == "Critical" ? "#FEE2E2" : (r.RiskLevel == "Medium" ? "#FEF9C3" : "#DCFCE7"),
                                RiskFg = r.RiskLevel == "High" || r.RiskLevel == "Critical" ? "#DC2626" : (r.RiskLevel == "Medium" ? "#CA8A04" : "#16A34A"),
                                MoodColor = r.MoodScore <= 3 ? "#DC2626" : (r.MoodScore <= 6 ? "#CA8A04" : "#16A34A"),
                                CanNotify = (r.RiskLevel == "High" || r.RiskLevel == "Critical") && !r.IsNotified ? Visibility.Visible : Visibility.Collapsed
                            };

                var list = await query.ToListAsync();
                DgRecords.ItemsSource = list;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SchoolCounseling] Load records error");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPermission()) return;
            LoadData();
        }

        private int _editingRecordId = 0;

        private void BtnAddRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPermission()) return;
            _editingRecordId = 0;
            CboStudent.IsEnabled = true;
            PnlAddRecord.Visibility = Visibility.Visible;
            SldMoodScore.Value = 5;
            TxtNotes.Text = "";
            CboInterventionLevel.SelectedIndex = 0;
            TxtInterventionNotes.Text = "";
            DpFollowUp.SelectedDate = null;
        }

        private async void BtnEditRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPermission()) return;
            if (_db == null) return;
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    var record = await _db.StudentMentalHealthRecords.FindAsync(id);
                    if (record == null) return;

                    _editingRecordId = id;
                    CboStudent.SelectedValue = record.StudentId;
                    CboStudent.IsEnabled = false; // Không cho phép đổi học sinh khi sửa
                    SldMoodScore.Value = record.MoodScore;
                    TxtNotes.Text = record.Notes;
                    
                    int level = record.InterventionLevel;
                    if (level < 0 || level > 3) level = 0;
                    CboInterventionLevel.SelectedIndex = level;

                    TxtInterventionNotes.Text = record.InterventionNotes;
                    DpFollowUp.SelectedDate = record.FollowUpDate;

                    PnlAddRecord.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error loading record for edit");
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            PnlAddRecord.Visibility = Visibility.Collapsed;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPermission()) return;
            if (_db == null) return;
            try
            {
                if (CboStudent.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn học sinh!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                int studentId = (int)CboStudent.SelectedValue;
                int moodScore = (int)SldMoodScore.Value;

                string notes = TxtNotes.Text.Trim();
                string notesLower = notes.ToLowerInvariant();
                string[] sensitiveKeywords = { "buồn bã", "đánh nhau", "tự kỷ", "bắt nạt", "trầm cảm" };
                string[] criticalKeywords = { "tự tử", "tự sát", "muốn chết", "hủy hoại bản thân", "cắt tay", "uống thuốc ngủ", "bạo hành" };
                
                var detected = sensitiveKeywords.Where(kw => notesLower.Contains(kw.ToLowerInvariant())).ToList();
                var detectedCritical = criticalKeywords.Where(kw => notesLower.Contains(kw.ToLowerInvariant())).ToList();

                string riskLevel = "Low";
                if (moodScore <= 3 || detected.Count > 0) riskLevel = "High";
                else if (moodScore <= 6) riskLevel = "Medium";

                if (detectedCritical.Count > 0)
                {
                    riskLevel = "Critical";
                    detected.AddRange(detectedCritical);
                }

                StudentMentalHealthRecord record;
                if (_editingRecordId != 0)
                {
                    record = await _db.StudentMentalHealthRecords.FindAsync(_editingRecordId);
                    if (record == null) return;
                    
                    record.MoodScore = moodScore;
                    record.Notes = notes;
                    record.RiskLevel = riskLevel;
                    record.DetectedKeywords = string.Join(", ", detected);
                    record.InterventionLevel = CboInterventionLevel.SelectedIndex;
                    record.InterventionNotes = TxtInterventionNotes.Text.Trim();
                    record.FollowUpDate = DpFollowUp.SelectedDate;
                }
                else
                {
                    record = new StudentMentalHealthRecord
                    {
                        StudentId = studentId,
                        MoodScore = moodScore,
                        Notes = notes,
                        RiskLevel = riskLevel,
                        DetectedKeywords = string.Join(", ", detected),
                        RecordedAt = DateTime.Now,
                        InterventionLevel = CboInterventionLevel.SelectedIndex,
                        InterventionNotes = TxtInterventionNotes.Text.Trim(),
                        FollowUpDate = DpFollowUp.SelectedDate
                    };
                    _db.StudentMentalHealthRecords.Add(record);
                }
                await _db.SaveChangesAsync();

                string loggedInUser = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName 
                     ?? (QASmartClass.Services.UserSessionService.Instance.IsLoggedIn ? QASmartClass.Services.UserSessionService.Instance.FullName : null) 
                     ?? "Counselor";
                QASmartClass.Services.AuditHelper.Log(_db, "SEL_Record_Added", loggedInUser, $"Added record for student {studentId}");
                
                PnlAddRecord.Visibility = Visibility.Collapsed;
                LoadData();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[SchoolCounseling] Save error");
                MessageBox.Show("Lỗi khi lưu dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SldMoodScore_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtMoodVal == null) return;
            int score = (int)e.NewValue;
            string emoji = score <= 3 ? "😞" : (score <= 6 ? "😐" : "🙂");
            TxtMoodVal.Text = $"{score} {emoji}";
            if (score <= 3) TxtMoodVal.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            else if (score <= 6) TxtMoodVal.Foreground = new SolidColorBrush(Color.FromRgb(202, 138, 4));
            else TxtMoodVal.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74));
        }

        private async void BtnNotify_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckPermission()) return;
            if (_db == null) return;
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    var record = await _db.StudentMentalHealthRecords.FindAsync(id);
                    if (record == null)
                    {
                        MessageBox.Show("Không tìm thấy bản ghi tâm lý tương ứng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var student = await _db.Students.FindAsync(record.StudentId);
                    if (student == null)
                    {
                        MessageBox.Show("Không tìm thấy học sinh liên kết với bản ghi này.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var notifService = new QASmartClass.Services.NotificationService(_db);
                    string subject = "Thông báo khẩn cấp mật từ Phòng tư vấn Tâm lý học đường";
                    string reason = "Nhà trường phát hiện con em đang gặp vấn đề áp lực lớn hoặc tâm trạng cần sự chia sẻ, đồng hành từ gia đình. Vui lòng liên hệ Phòng tư vấn học đường của nhà trường để cùng phối hợp hỗ trợ con em.";

                    bool success = await Task.Run(() => notifService.SendToParent(record.StudentId, subject, reason));
                    if (success)
                    {
                        record.IsNotified = true;
                        record.NotifiedAt = DateTime.Now;
                        await _db.SaveChangesAsync();

                        string reporter = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName 
                             ?? (QASmartClass.Services.UserSessionService.Instance.IsLoggedIn ? QASmartClass.Services.UserSessionService.Instance.FullName : null) 
                             ?? "Counselor";
                        QASmartClass.Services.AuditHelper.Log(_db, "SEL_Alert_Sent", reporter, $"Sent alert for record {id}");
                        MessageBox.Show($"Đã gửi thông báo mật thành công tới phụ huynh của học sinh {student.FullName}!", "Gửi thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Gửi thông báo thất bại. Vui lòng thử lại sau.", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi trong quá trình gửi thông báo: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
