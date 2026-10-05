using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class AttendancePage : Page
    {
        private List<AttendanceRow> _rows = new();
        private DateTime _selectedDate;

        public AttendancePage()
        {
            InitializeComponent();
            _selectedDate = DateTime.Today;
            Loaded += (_, _) =>
            {
                LoadAttendance();

                // Subscribe to roster change events for live sync
                try
                {
                    // → ClassroomAppContext
                    ClassroomAppContext.ClassRoster.ActiveRosterChanged += OnActiveRosterChanged;
                }
                catch { }
            };

            Unloaded += (_, _) =>
            {
                try
                {
                    // → ClassroomAppContext
                    ClassroomAppContext.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                }
                catch { }
            };
        }

        private void OnActiveRosterChanged(object? sender, Data.ClassRoster? e)
        {
            Dispatcher.Invoke(() => LoadAttendance());
        }

        // ═══════════════════════════════════════════════════════════
        //  LOAD DATA
        // ═══════════════════════════════════════════════════════════

        private async void LoadAttendance()
        {
            if (attendanceList == null || txtTotal == null) return;
            try
            {
                // → ClassroomAppContext
                var activeRoster = ClassroomAppContext.ClassRoster.ActiveRoster;
                
                List<Data.Student> students = null!;
                string rosterName = activeRoster != null ? activeRoster.DisplayName : "Chưa chọn lớp";
                
                await Task.Run(() =>
                {
                    if (activeRoster != null)
                    {
                        students = ClassroomAppContext.ClassRoster.GetActiveStudents();
                    }
                    else
                    {
                        students = Services.VietnameseNameHelper.SortByVietnameseName(
                            ClassroomAppContext.Db?.Students?.ToList() ?? new List<Data.Student>(), s => s.FullName);
                    }
                });

                if (txtSubtitle != null)
                    txtSubtitle.Text = $"{rosterName} — {_selectedDate:dd/MM/yyyy}";

                _rows.Clear();
                attendanceList.Children.Clear();

                if (students == null || students.Count == 0)
                {
                    attendanceList.Children.Add(new TextBlock
                    {
                        Text = activeRoster != null
                            ? $"Lớp {rosterName} chưa có học sinh. Vào mục 2.2 Học sinh để thêm."
                            : "Chưa chọn lớp. Vui lòng chọn lớp ở thanh bên trái trước khi điểm danh.",
                        FontSize = 13, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic,
                        Margin = new Thickness(20, 40, 0, 0)
                    });
                    return;
                }

                int rosterId = activeRoster?.Id ?? 0;
                List<Data.AttendanceRecord> savedRecords = null!;
                var targetDate = _selectedDate.Date;

                await Task.Run(() =>
                {
                    savedRecords = ClassroomAppContext.Db.AttendanceRecords
                        .Where(r => r.RosterId == rosterId && r.Date.Date == targetDate)
                        .ToList();
                });

                int stt = 0;
                foreach (var s in students)
                {
                    stt++;
                    var rec = savedRecords.FirstOrDefault(r => r.StudentId == s.Id);
                    string defaultStatus = s.IsOnline ? "present" : "unknown";
                    
                    var row = new AttendanceRow
                    {
                        STT = stt,
                        StudentId = s.Id,
                        FullName = s.FullName,
                        StudentCode = s.StudentCode,
                        IsOnline = s.IsOnline,
                        Status = rec != null ? rec.Status : defaultStatus,
                        Note = rec != null ? rec.Note : ""
                    };
                    _rows.Add(row);
                    attendanceList.Children.Add(BuildRow(row));
                }

                UpdateStats();
                Log.Information("Attendance loaded asynchronously: {Count} students", students.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("LoadAttendance error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  BUILD ROW
        // ═══════════════════════════════════════════════════════════

        private Border BuildRow(AttendanceRow row)
        {
            var border = new Border
            {
                Padding = new Thickness(16, 8, 16, 8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = row.STT % 2 == 0 ? new SolidColorBrush(Color.FromRgb(250, 250, 250)) : Brushes.White
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // STT
            var txtSTT = new TextBlock { Text = row.STT.ToString(), FontSize = 12, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtSTT, 0);
            grid.Children.Add(txtSTT);

            // Name with avatar
            var namePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var avatar = new Border
            {
                Width = 28, Height = 28, CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromRgb(
                    (byte)(Math.Abs(row.FullName.GetHashCode()) % 100 + 100),
                    (byte)(Math.Abs(row.FullName.GetHashCode() >> 8) % 100 + 120),
                    (byte)(Math.Abs(row.FullName.GetHashCode() >> 16) % 100 + 140))),
                Margin = new Thickness(0, 0, 8, 0)
            };
            avatar.Child = new TextBlock
            {
                Text = row.FullName.Length > 0 ? row.FullName.Split(' ').Last()[..1].ToUpper() : "?",
                FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            namePanel.Children.Add(avatar);
            namePanel.Children.Add(new TextBlock { Text = row.FullName, FontSize = 13, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            Grid.SetColumn(namePanel, 1);
            grid.Children.Add(namePanel);

            // Code
            var txtCode = new TextBlock { Text = row.StudentCode, FontSize = 12, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(txtCode, 2);
            grid.Children.Add(txtCode);

            // Online status
            var onlineBadge = new Border
            {
                Background = row.IsOnline ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) : new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                CornerRadius = new CornerRadius(10), Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            onlineBadge.Child = new TextBlock
            {
                Text = row.IsOnline ? "🟢 On" : "⚫ Off",
                FontSize = 11, Foreground = row.IsOnline ? new SolidColorBrush(Color.FromRgb(46, 125, 50)) : Brushes.Gray
            };
            Grid.SetColumn(onlineBadge, 3);
            grid.Children.Add(onlineBadge);

            // Radio buttons — Present / Late / Absent
            string groupName = $"att_{row.StudentId}";

            var rbPresent = new RadioButton
            {
                Content = "Có mặt", GroupName = groupName, FontSize = 12,
                IsChecked = row.Status == "present",
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            rbPresent.Checked += (_, _) => { row.Status = "present"; UpdateStats(); };
            Grid.SetColumn(rbPresent, 4);
            grid.Children.Add(rbPresent);
            row.RbPresent = rbPresent;

            var rbLate = new RadioButton
            {
                Content = "Đi trễ", GroupName = groupName, FontSize = 12,
                IsChecked = row.Status == "late",
                Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            rbLate.Checked += (_, _) => { row.Status = "late"; UpdateStats(); };
            Grid.SetColumn(rbLate, 5);
            grid.Children.Add(rbLate);
            row.RbLate = rbLate;

            var rbAbsent = new RadioButton
            {
                Content = "Vắng", GroupName = groupName, FontSize = 12,
                IsChecked = row.Status == "absent",
                Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
            rbAbsent.Checked += (_, _) => { row.Status = "absent"; UpdateStats(); };
            Grid.SetColumn(rbAbsent, 6);
            grid.Children.Add(rbAbsent);
            row.RbAbsent = rbAbsent;

            // Note
            var txtNote = new TextBox
            {
                Text = row.Note, FontSize = 12, BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                Background = Brushes.Transparent, Padding = new Thickness(4, 4, 4, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            txtNote.TextChanged += (_, _) => row.Note = txtNote.Text;
            txtNote.GotFocus += (_, _) => txtNote.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            txtNote.LostFocus += (_, _) => txtNote.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            Grid.SetColumn(txtNote, 7);
            grid.Children.Add(txtNote);

            border.Child = grid;

            // Hover effect
            border.MouseEnter += (_, _) => border.Background = new SolidColorBrush(Color.FromRgb(232, 245, 255));
            border.MouseLeave += (_, _) => border.Background = row.STT % 2 == 0 ? new SolidColorBrush(Color.FromRgb(250, 250, 250)) : Brushes.White;

            return border;
        }

        // ═══════════════════════════════════════════════════════════
        //  STATS
        // ═══════════════════════════════════════════════════════════

        private void UpdateStats()
        {
            int total = _rows.Count;
            int present = _rows.Count(r => r.Status == "present");
            int late = _rows.Count(r => r.Status == "late");
            int absent = _rows.Count(r => r.Status == "absent");
            double rate = total > 0 ? (present + late) * 100.0 / total : 0;

            if (txtTotal != null) txtTotal.Text = total.ToString();
            if (txtPresent != null) txtPresent.Text = present.ToString();
            if (txtLate != null) txtLate.Text = late.ToString();
            if (txtAbsent != null) txtAbsent.Text = absent.ToString();
            if (txtRate != null) txtRate.Text = $"{rate:F0}%";
        }

        // ═══════════════════════════════════════════════════════════
        //  ACTIONS
        // ═══════════════════════════════════════════════════════════

        private void AutoAttend_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in _rows)
            {
                if (row.IsOnline)
                    row.Status = "present";
                else
                    row.Status = "absent";
            }
            // Rebuild UI
            if (attendanceList == null) return;
            attendanceList.Children.Clear();
            foreach (var row in _rows)
                attendanceList.Children.Add(BuildRow(row));
            UpdateStats();

            MessageBox.Show(
                $"✅ Đã tự động điểm danh!\n\n" +
                $"• Có mặt: {_rows.Count(r => r.Status == "present")} HS (online)\n" +
                $"• Vắng: {_rows.Count(r => r.Status == "absent")} HS (offline)",
                "Điểm danh tự động", MessageBoxButton.OK, MessageBoxImage.Information);

            Log.Information("Auto attendance completed");
        }

        private void DateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpDate?.SelectedDate != null)
            {
                _selectedDate = dpDate.SelectedDate.Value;
                LoadAttendance();
            }
        }

        private async void SaveAttendance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // → ClassroomAppContext
                var activeRoster = ClassroomAppContext.ClassRoster.ActiveRoster;
                int rosterId = activeRoster?.Id ?? 0;
                
                if (rosterId == 0)
                {
                    ClassroomDialog.Warn("Vui lòng chọn lớp học ở danh sách bên trái trước khi lưu!", "Lỗi");
                    return;
                }

                // 1. Chụp bản sao dữ liệu (snapshot) trên UI Thread để bảo vệ Thread-Safety
                var rowsCopy = _rows.Select(r => new AttendanceRow
                {
                    StudentId = r.StudentId,
                    Status = r.Status,
                    Note = r.Note
                }).ToList();

                var selectedDateCopy = _selectedDate.Date;
                int added = 0;
                int updated = 0;

                // 2. Chạy tác vụ ghi cơ sở dữ liệu trên background thread
                await Task.Run(() =>
                {
                    var existingRecords = ClassroomAppContext.Db.AttendanceRecords
                        .Where(r => r.RosterId == rosterId && r.Date.Date == selectedDateCopy)
                        .ToList();

                    foreach (var row in rowsCopy)
                    {
                        var rec = existingRecords.FirstOrDefault(r => r.StudentId == row.StudentId);
                        if (rec != null)
                        {
                            rec.Status = row.Status;
                            rec.Note = row.Note;
                            rec.UpdatedAt = DateTime.Now;
                            updated++;
                        }
                        else
                        {
                            ClassroomAppContext.Db.AttendanceRecords.Add(new Data.AttendanceRecord
                            {
                                StudentId = row.StudentId,
                                RosterId = rosterId,
                                Date = selectedDateCopy,
                                Status = row.Status,
                                Note = row.Note,
                                UpdatedAt = DateTime.Now
                            });
                            added++;
                        }
                    }

                    ClassroomAppContext.Db.SaveChanges();
                });

                // 3. Cập nhật giao diện trên UI Thread
                txtLastSaved.Text = $"💾 Đã lưu lúc {DateTime.Now:HH:mm:ss}";
                MessageBox.Show($"✅ Đã lưu điểm danh ngày {_selectedDate:dd/MM/yyyy}!\n(Thêm mới: {added}, Cập nhật: {updated})",
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                Log.Information("Attendance saved asynchronously for {Date}: {Add} added, {Up} updated", _selectedDate.ToString("yyyy-MM-dd"), added, updated);
            }
            catch (Exception ex)
            {
                ClassroomDialog.Warn($"Lỗi lưu điểm danh: {ex.Message}", "Lỗi");
            }
        }

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Xuất điểm danh",
                    FileName = $"DiemDanh_{_selectedDate:yyyyMMdd}.csv",
                    Filter = "CSV (*.csv)|*.csv|Tất cả|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (dlg.ShowDialog() == true)
                {
                    var filePath = dlg.FileName;
                    var selectedDateCopy = _selectedDate;
                    
                    // 1. Chụp snapshot dữ liệu từ UI Thread để tránh lỗi xung đột đa luồng khi ghi file
                    var rowsCopy = _rows.Select(r => new AttendanceRow
                    {
                        STT = r.STT,
                        FullName = r.FullName,
                        StudentCode = r.StudentCode,
                        Status = r.Status,
                        Note = r.Note
                    }).ToList();

                    // 2. Chạy ngầm xử lý chuỗi và ghi tệp tin trên background thread
                    await Task.Run(() =>
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("STT,Họ tên,Mã HS,Trạng thái,Ghi chú");
                        
                        foreach (var r in rowsCopy)
                        {
                            string statusText = r.Status switch
                            {
                                "present" => "Có mặt",
                                "late" => "Đi trễ",
                                "absent" => "Vắng",
                                _ => "Chưa điểm danh"
                            };

                            // Khử trùng và lọc sạch ký tự độc hại Excel Injection
                            var cleanName = Classroom.Services.ClassRosterService.SanitizeCsvField(r.FullName);
                            var cleanCode = Classroom.Services.ClassRosterService.SanitizeCsvField(r.StudentCode);
                            var cleanStatus = Classroom.Services.ClassRosterService.SanitizeCsvField(statusText);
                            var cleanNote = Classroom.Services.ClassRosterService.SanitizeCsvField(r.Note);

                            sb.AppendLine($"{r.STT},\"{cleanName}\",\"{cleanCode}\",\"{cleanStatus}\",\"{cleanNote}\"");
                        }

                        // Append phần thống kê báo cáo (Summary block)
                        sb.AppendLine();
                        sb.AppendLine($"Ngày,{selectedDateCopy:dd/MM/yyyy}");
                        sb.AppendLine($"Tổng HS,{rowsCopy.Count}");
                        sb.AppendLine($"Có mặt,{rowsCopy.Count(r => r.Status == "present")}");
                        sb.AppendLine($"Đi trễ,{rowsCopy.Count(r => r.Status == "late")}");
                        sb.AppendLine($"Vắng,{rowsCopy.Count(r => r.Status == "absent")}");

                        // Ghi tệp tin kèm mã nhận dạng UTF-8 BOM (Byte Order Mark) để Microsoft Excel hiển thị đúng dấu tiếng Việt
                        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
                    });

                    ClassroomDialog.Info($"✅ Đã xuất file báo cáo điểm danh thành công:\n{filePath}", "Xuất thành công");
                    
                    Log.Information("Attendance exported asynchronously to {Path}", filePath);
                }
            }
            catch (Exception ex)
            {
                ClassroomDialog.Warn($"Lỗi xuất file: {ex.Message}", "Lỗi");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  DATA MODEL
    // ═══════════════════════════════════════════════════════════

    public class AttendanceRow
    {
        public int STT { get; set; }
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        /// <summary>present, late, absent, unknown</summary>
        public string Status { get; set; } = "unknown";
        public string Note { get; set; } = string.Empty;

        // UI elements for testing
        public RadioButton RbPresent { get; set; } = null!;
        public RadioButton RbLate { get; set; } = null!;
        public RadioButton RbAbsent { get; set; } = null!;
    }
}
