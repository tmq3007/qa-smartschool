using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentAttendancePage : Page
    {
        private AppDbContext? _db;
        private readonly int _studentId;
        private readonly bool _isLocalDbCreated = false;

        public ParentAttendancePage(int studentId = 1)
        {
            InitializeComponent();
            if (QASmartClass.Services.AppServices.Database != null)
            {
                _db = QASmartClass.Services.AppServices.Database;
                _isLocalDbCreated = false;
            }
            else
            {
                _db = new AppDbContext();
                _isLocalDbCreated = true;
            }
            _studentId = studentId;
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_isLocalDbCreated)
            {
                _db?.Dispose();
            }
            _db = null;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate months
            for (int m = 1; m <= 12; m++)
                CbMonth.Items.Add(new ComboBoxItem { Content = $"Tháng {m}", Tag = m.ToString() });
            CbMonth.SelectedIndex = DateTime.Now.Month - 1;
        }

        private void CbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            LoadAttendance();
        }

        private string GetSetting(string id, string defaultValue)
        {
            if (_db == null) return defaultValue;
            var setting = _db.SystemSettings.FirstOrDefault(s => s.Id == id);
            if (setting == null)
            {
                setting = new SystemSetting { Id = id, Value = defaultValue, Category = "ParentPortal", LastUpdated = DateTime.Now };
                _db.SystemSettings.Add(setting);
                try { _db.SaveChanges(); } catch {}
            }
            return setting.Value;
        }

        private void LoadAttendance()
        {
            if (_db == null) return;
            try
            {
                int month = int.TryParse((CbMonth.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out int m) ? m : DateTime.Now.Month;
                int year = DateTime.Now.Year;

                var caseSetting = GetSetting("ParentPortal_AttendanceCase", "CaseInsensitive");
                bool isStrict = caseSetting.Equals("Strict", StringComparison.OrdinalIgnoreCase);

                var allRecords = _db.AttendanceRecords
                    .Where(a => a.StudentId == _studentId && a.Date.Month == month && a.Date.Year == year)
                    .ToList();

                var absences = allRecords
                    .Where(a => isStrict
                        ? a.Status != "Present"
                        : !a.Status.Equals("present", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => a.Date)
                    .Select(a => new
                    {
                        DateDisplay = a.Date.ToString("dd/MM/yyyy (dddd)", new System.Globalization.CultureInfo("vi-VN")),
                        StatusText = isStrict
                            ? (a.Status == "Absent" ? "Vắng không phép" : (a.Status == "Excused" ? "Vắng có phép" : (a.Status == "Late" ? "Đi trễ" : a.Status)))
                            : (a.Status.Equals("absent", StringComparison.OrdinalIgnoreCase) ? "Vắng không phép"
                              : (a.Status.Equals("excused", StringComparison.OrdinalIgnoreCase) ? "Vắng có phép"
                              : (a.Status.Equals("late", StringComparison.OrdinalIgnoreCase) ? "Đi trễ" : a.Status)))
                    }).ToList();

                int totalDays = allRecords.Count;
                int lateCount = allRecords.Count(a => isStrict
                    ? a.Status == "Late"
                    : a.Status.Equals("late", StringComparison.OrdinalIgnoreCase));
                int absentCount = allRecords.Count(a => isStrict
                    ? (a.Status == "Absent" || a.Status == "Excused")
                    : (a.Status.Equals("absent", StringComparison.OrdinalIgnoreCase) || a.Status.Equals("excused", StringComparison.OrdinalIgnoreCase)));
                int presentCount = Math.Max(0, totalDays - absentCount - lateCount);

                LvAbsences.ItemsSource = absences;
                TxtSummary.Text = $"Có mặt: {presentCount} ngày | Đi trễ: {lateCount} ngày | Vắng: {absentCount} ngày (Tổng số ngày điểm danh: {totalDays} ngày)";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tải dữ liệu điểm danh cho phụ huynh");
            }
        }
    }
}

