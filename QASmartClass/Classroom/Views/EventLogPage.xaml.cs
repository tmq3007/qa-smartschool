using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class EventLogPage : Page
    {
        private List<EventLog> _allLogs = new();
        private string _filterType = "All";
        private string _searchText = "";

        public EventLogPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                // Set placeholder text
                txtSearch.Text = txtSearch.Tag?.ToString() ?? "";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                LoadLogs();
            };
        }

        private void LoadLogs()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                _allLogs = app.Database.EventLogs
                    .OrderByDescending(e => e.Timestamp)
                    .Take(500)
                    .ToList();

                if (!_allLogs.Any())
                {
                    // Demo data
                    var now2 = DateTime.Now;
                    _allLogs = new List<EventLog>
                    {
                        new() { EventType="INFO",    Actor="Hệ thống",    Details="QA Smart Class v4.0 khởi động thành công",                       Timestamp=now2.AddMinutes(-90) },
                        new() { EventType="SESSION", Actor="GV Nguyễn A", Details="Lớp học bắt đầu — Toán 10A (20 HS online)",                      Timestamp=now2.AddMinutes(-60) },
                        new() { EventType="LESSON",  Actor="GV Nguyễn A", Details="Bắt đầu dạy: Hàm số bậc nhất y=ax+b (LessonRunner giai đoạn 1)", Timestamp=now2.AddMinutes(-55) },
                        new() { EventType="QUIZ",    Actor="GV Nguyễn A", Details="Quiz Battle bắt đầu: 5 câu, 4 nhóm thi đua, 20 HS tham gia",    Timestamp=now2.AddMinutes(-40) },
                        new() { EventType="QUIZ",    Actor="Hệ thống",    Details="Quiz kết thúc — Nhóm 1 chiến thắng (95 điểm)",                   Timestamp=now2.AddMinutes(-35) },
                        new() { EventType="POLL",    Actor="GV Nguyễn A", Details="Quick Poll: 'Bạn hiểu bài chưa?' — 18/20 HS trả lời",            Timestamp=now2.AddMinutes(-28) },
                        new() { EventType="FILE",    Actor="GV Nguyễn A", Details="Phát file: BaiTap_HamSo_BacNhat.pdf → 20/20 HS nhận thành công", Timestamp=now2.AddMinutes(-20) },
                        new() { EventType="LOCK",    Actor="GV Nguyễn A", Details="Khoá màn hình 20 máy HS trong 5 phút",                           Timestamp=now2.AddMinutes(-15) },
                        new() { EventType="FILE",    Actor="GV Nguyễn A", Details="Thu bài: 18/20 HS nộp BaiLam_Nhom.docx",                         Timestamp=now2.AddMinutes(-8)  },
                        new() { EventType="SESSION", Actor="GV Nguyễn A", Details="Lớp học kết thúc — 45 phút — Giao bài tập về nhà",              Timestamp=now2.AddMinutes(-2)  },
                    };
                }

                ApplyFilter();
                UpdateStats();
            }
            catch (Exception ex)
            {
                Log.Warning("EventLogPage load error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  🔍 SEARCH & FILTER
        // ═══════════════════════════════════════════════════════════

        private void ApplyFilter()
        {
            var filtered = _allLogs.AsEnumerable();

            // Type filter
            if (_filterType != "All")
                filtered = filtered.Where(e => e.EventType.Contains(_filterType, StringComparison.OrdinalIgnoreCase));

            // Search text filter
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string q = _searchText.ToLowerInvariant();
                filtered = filtered.Where(e =>
                    (e.EventType?.ToLower().Contains(q) == true) ||
                    (e.Actor?.ToLower().Contains(q) == true) ||
                    (e.Details?.ToLower().Contains(q) == true) ||
                    (e.MacAddress?.ToLower().Contains(q) == true) ||
                    (e.ClientIP?.ToLower().Contains(q) == true));
            }

            // Date range filter
            if (dpFrom?.SelectedDate is DateTime fromDate)
                filtered = filtered.Where(e => e.Timestamp >= fromDate);

            if (dpTo?.SelectedDate is DateTime toDate)
                filtered = filtered.Where(e => e.Timestamp < toDate.AddDays(1));

            var result = filtered.ToList();
            logGrid.ItemsSource = result;

            // Update total count to show filtered vs total
            if (!string.IsNullOrWhiteSpace(_searchText) || _filterType != "All" ||
                dpFrom?.SelectedDate != null || dpTo?.SelectedDate != null)
            {
                txtTotalEvents.Text = $"{result.Count}/{_allLogs.Count}";
            }
            else
            {
                txtTotalEvents.Text = _allLogs.Count.ToString();
            }
        }

        private void UpdateStats()
        {
            txtTotalEvents.Text = _allLogs.Count.ToString();
            txtSessionEvents.Text = _allLogs.Count(e => e.EventType == "SESSION").ToString();
            txtQuizEvents.Text = _allLogs.Count(e => e.EventType == "QUIZ").ToString();
            txtFileEvents.Text = _allLogs.Count(e => e.EventType == "FILE").ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = txtSearch.Text;
            string placeholder = txtSearch.Tag?.ToString() ?? "";

            // Ignore placeholder text
            if (text == placeholder) return;

            _searchText = text;
            ApplyFilter();
        }

        private void DateFilter_Changed(object? sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            _searchText = "";
            _filterType = "All";
            txtSearch.Text = txtSearch.Tag?.ToString() ?? "";
            txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;
            ApplyFilter();
            UpdateStats();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == (txtSearch.Tag?.ToString() ?? ""))
            {
                txtSearch.Text = "";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = txtSearch.Tag?.ToString() ?? "";
                txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                _searchText = "";
            }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _filterType = tag;
                ApplyFilter();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadLogs();

        private void ExportLog_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Xuất nhật ký",
                Filter = "Text Files|*.txt|CSV Files|*.csv",
                FileName = $"EventLog_{DateTime.Now:yyyyMMdd_HHmm}"
            };
            if (dlg.ShowDialog() == true)
            {
                // Export current filtered view
                var items = logGrid.ItemsSource as List<EventLog> ?? _allLogs;
                var lines = new List<string>
                {
                    "Thời gian\tLoại\tNgười thực hiện\tChi tiết\tĐịa chỉ MAC\tĐịa chỉ IP"
                };
                lines.AddRange(items.Select(l =>
                    $"{l.Timestamp:yyyy-MM-dd HH:mm:ss}\t{l.EventType}\t{l.Actor}\t{l.Details}\t{l.MacAddress}\t{l.ClientIP}"));
                System.IO.File.WriteAllLines(dlg.FileName, lines);
                MessageBox.Show($"✅ Đã xuất {items.Count} sự kiện ra:\n{dlg.FileName}",
                    "Xuất thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("EventLog exported: {File}, {Count} events", dlg.FileName, items.Count);
            }
        }
    }
}
