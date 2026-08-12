using QASmartClass.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace QASmartClass.TeacherHub.Views
{
    public partial class AddMeetingWindow : Window
    {
        public AddMeetingWindow()
        {
            InitializeComponent();
            
            Loaded += AddMeetingWindow_Loaded;
        }

        private async void AddMeetingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DpDate.SelectedDate = DateTime.Today;
            DpDueDate.SelectedDate = DateTime.Today.AddDays(7);

            try
            {
                using var db = new AppDbContext();
                // Nạp danh sách các tổ có sẵn trong database làm gợi ý
                var depts = await db.DeptMeetings.Select(m => m.DeptName).Distinct().ToListAsync();
                
                // Dự phòng một số tổ chuyên môn chuẩn nếu chưa có dữ liệu
                if (!depts.Any())
                {
                    depts.AddRange(new[] { "Tổ Toán - Tin", "Tổ Ngữ Văn", "Tổ Ngoại Ngữ", "Tổ KHTN", "Tổ KHXH", "Tổ Nghệ thuật - Thể chất" });
                }

                foreach (var dept in depts.Where(d => !string.IsNullOrEmpty(d)))
                {
                    CboDept.Items.Add(dept);
                }
                
                if (CboDept.Items.Count > 0)
                {
                    CboDept.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[AddMeetingWindow] Load departments error");
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string deptName = CboDept.Text.Trim();
            string agenda = TxtAgenda.Text.Trim();
            string minutes = TxtMinutes.Text.Trim();
            string attendees = TxtAttendees.Text.Trim();

            // Ràng buộc dữ liệu đầu vào (Validation)
            if (string.IsNullOrEmpty(deptName))
            {
                MessageBox.Show("Vui lòng nhập hoặc chọn Tên tổ chuyên môn.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                CboDept.Focus();
                return;
            }

            if (string.IsNullOrEmpty(agenda))
            {
                MessageBox.Show("Vui lòng nhập Nội dung nghị sự cuộc họp.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtAgenda.Focus();
                return;
            }

            if (string.IsNullOrEmpty(minutes))
            {
                MessageBox.Show("Vui lòng nhập Biên bản cuộc họp chi tiết.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMinutes.Focus();
                return;
            }

            // Thực thi lưu dữ liệu đồng bộ bằng DB Transaction (TC-06)
            using var db = new AppDbContext();
            using (var transaction = await db.Database.BeginTransactionAsync())
            {
                try
                {
                    string currentTeacherCode = QASmartClass.Services.UserSessionService.Instance.TeacherCode;
                    
                    var meeting = new DeptMeeting
                    {
                        DeptName = deptName,
                        MeetingDate = DpDate.SelectedDate ?? DateTime.Today,
                        Agenda = agenda,
                        Minutes = minutes,
                        Attendees = attendees,
                        CreatedBy = string.IsNullOrEmpty(currentTeacherCode) ? "GV" : currentTeacherCode,
                        CreatedAt = DateTime.Now
                    };
                    
                    db.DeptMeetings.Add(meeting);
                    await db.SaveChangesAsync(); // Lưu để EF Core sinh ra ID tự động của Meeting phục vụ khóa ngoại

                    // Lưu các công việc giao đi kèm
                    if (!string.IsNullOrWhiteSpace(TxtAction.Text))
                    {
                        var lines = TxtAction.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();
                            if (string.IsNullOrEmpty(trimmedLine)) continue;

                            string assignedTo = "";
                            string content = trimmedLine;

                            // Tự động gán việc theo cú pháp [Tên Người Nhận]
                            var match = Regex.Match(trimmedLine, @"^\[([^\]]+)\]\s*:?\s*(.*)$");
                            if (match.Success)
                            {
                                assignedTo = match.Groups[1].Value.Trim();
                                content = match.Groups[2].Value.Trim();
                            }
                            else
                            {
                                // Dự phòng gán cho người đầu tiên trong danh sách tham dự
                                assignedTo = attendees.Split(',').FirstOrDefault()?.Trim() ?? "";
                            }

                            db.DeptMeetingActions.Add(new DeptMeetingAction
                            {
                                MeetingId = meeting.Id,
                                Content = content,
                                AssignedTo = assignedTo,
                                DueDate = DpDueDate.SelectedDate ?? DateTime.Today.AddDays(7),
                                IsCompleted = false
                            });
                        }
                        
                        await db.SaveChangesAsync();
                    }

                    await transaction.CommitAsync(); // Hoàn tất giao dịch cơ sở dữ liệu
                    
                    MessageBox.Show("Lưu biên bản sinh hoạt tổ chuyên môn thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(); // Hoàn tác toàn bộ nếu có bất kỳ lỗi nào xảy ra
                    Serilog.Log.Error(ex, "[AddMeetingWindow] Save error");
                    MessageBox.Show($"Lưu biên bản thất bại. Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtAgenda.Text) || 
                !string.IsNullOrWhiteSpace(TxtMinutes.Text) || 
                !string.IsNullOrWhiteSpace(TxtAction.Text) ||
                CboDept.Text.Trim() != "")
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn hủy bỏ? Mọi thay đổi chưa lưu sẽ bị mất.", "Xác nhận hủy bỏ", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            DialogResult = false;
            Close();
        }
    }
}
