using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;

namespace QASmartClass.HealthRoom.Views
{
    public partial class EmergencyReportView : UserControl
    {
        private AppDbContext? _db;
        private EmergencyService? _emergencyService;

        public EmergencyReportView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            _emergencyService = new EmergencyService(_db, new NotificationService(_db));
            
            try
            {
                var activeStudents = _db.Students.Where(s => s.Status == "Active").ToList();
                CboStudent.ItemsSource = activeStudents;
                CboStudent.DisplayMemberPath = "FullName";
                CboStudent.SelectedValuePath = "Id";
                if (activeStudents.Any()) CboStudent.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Health] Error loading active students");
            }

            LoadEmergencies();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
            _emergencyService = null;
        }

        private void LoadEmergencies()
        {
            if (_emergencyService == null) return;
            LvEmergencies.ItemsSource = _emergencyService.GetRecentEmergencies();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadEmergencies();
        }

        private void BtnResolve_Click(object sender, RoutedEventArgs e)
        {
            if (_emergencyService == null) return;

            if (LvEmergencies.SelectedItem is EmergencyLog log)
            {
                if (log.Status != "Resolved")
                {
                    if (_emergencyService.UpdateEmergencyStatus(log.Id, "Resolved"))
                    {
                        // Ghi log kiểm toán y tế
                        string resolver = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Y tế học đường";
                        QASmartClass.Services.AuditHelper.Log(_db, "Medical_Emergency_Resolved", resolver, $"Resolved emergency for student {log.StudentName}");

                        MessageBox.Show("Đã cập nhật trạng thái Giải Quyết thành công.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadEmergencies();
                    }
                }
                else
                {
                    MessageBox.Show("Sự cố này đã được giải quyết.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một sự cố để cập nhật.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null || _emergencyService == null) return;

            if (CboStudent.SelectedValue == null || string.IsNullOrWhiteSpace(TxtDescription.Text))
            {
                MessageBox.Show("Vui lòng chọn học sinh và nhập mô tả tình trạng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int studentId = (int)CboStudent.SelectedValue;
                var student = _db.Students.Find(studentId);
                if (student == null) return;

                var typeItem = CmbIncidentType.SelectedItem as ComboBoxItem;
                var log = new EmergencyLog
                {
                    StudentName = student.FullName,
                    IncidentType = typeItem?.Tag?.ToString() ?? "Injury",
                    Description = TxtDescription.Text.Trim(),
                    FirstAidApplied = TxtFirstAid.Text.Trim(),
                    Status = "RequiresAttention"
                };

                // 1. Lưu CSDL trước
                _emergencyService.AddEmergency(log);

                // Ghi Audit log với tên cán bộ y tế thực tế
                string reporter = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Y tế học đường";
                QASmartClass.Services.AuditHelper.Log(_db, "Medical_Emergency_Reported", reporter, $"Reported emergency for student {student.FullName}");

                // 2. Gửi Push Notification (bọc trong try-catch riêng để tránh lỗi mạng làm mất dữ liệu DB)
                bool notificationSent = true;
                try
                {
                    string vietnameseIncidentType = log.IncidentType switch
                    {
                        "Injury" => "Chấn thương / Tai nạn",
                        "Illness" => "Ốm đột xuất / Mệt mỏi",
                        "Poisoning" => "Nghi ngờ ngộ độc thực phẩm",
                        _ => "Sự cố sức khỏe khác"
                    };

                    var mobileApi = new MobileApiService(_db);
                    mobileApi.SendPushNotification(studentId, "Parent", $"CẢNH BÁO Y TẾ: {student.FullName}", $"Sự cố: {vietnameseIncidentType}\nMô tả: {log.Description}\nSơ cứu: {log.FirstAidApplied}", "Emergency");
                }
                catch (Exception ex)
                {
                    notificationSent = false;
                    Log.Error(ex, "[Health] Failed to send emergency push notification to parent");
                }

                if (notificationSent)
                {
                    MessageBox.Show("Đã ghi nhận và tự động gửi thông báo khẩn cấp tới App Phụ huynh!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Đã ghi nhận bệnh án thành công, nhưng kết nối mạng bị lỗi nên không thể gửi tin nhắn đẩy. Vui lòng liên hệ trực tiếp phụ huynh!", "Thông báo (Lỗi mạng)", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                TxtDescription.Text = "";
                TxtFirstAid.Text = "";
                
                LoadEmergencies();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Health] Error saving emergency report");
                MessageBox.Show($"Lỗi hệ thống khi lưu kết quả sơ cứu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

