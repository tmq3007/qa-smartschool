using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using QASmartClass.Classroom.Helpers;
using QASmartClass.Classroom.Services;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class TimetablePage : Page
    {
        private DateTime _weekStart;
        private List<TimetableSlot> _slots = new();
        private TimetableSlot? _currentActiveSlot;
        private DispatcherTimer? _timer;
        private string _className = "";
        private string _schoolYear = "";
        private string _viewMode = "Class";

        // Khung giờ học linh hoạt (theo mùa hoặc theo cài đặt của trường)
        private (int StartH, int StartM, int EndH, int EndM)[] PeriodTimeRanges => Services.PeriodScheduleService.Instance.GetPeriodTimeRanges();
        private string[] PeriodTimes => Services.PeriodScheduleService.Instance.GetPeriodTimes();

        private static readonly string[] PeriodLabels = new[]
        {
            "Tiết 1", "Tiết 2", "Tiết 3", "Tiết 4", "Tiết 5",
            "Tiết 6", "Tiết 7", "Tiết 8", "Tiết 9", "Tiết 10"
        };

        private static readonly string[] DayNames = new[]
        {
            "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7"
        };

        // Màu cho môn học
        private static readonly Dictionary<string, string> SubjectColors = new()
        {
            ["Toán"] = "#1976D2", ["Văn"] = "#7B1FA2", ["Anh"] = "#00838F",
            ["Lý"] = "#E64A19", ["Hóa"] = "#2E7D32", ["Sinh"] = "#AD1457",
            ["Sử"] = "#5D4037", ["Địa"] = "#00695C", ["GDCD"] = "#F9A825",
            ["Tin"] = "#0277BD", ["TD"] = "#EF6C00", ["CN"] = "#558B2F",
            ["Nhạc"] = "#8E24AA", ["MT"] = "#D81B60", ["STEM"] = "#1565C0",
            ["Chào cờ"] = "#D32F2F", ["SHDC"] = "#D32F2F", ["SHL"] = "#303F9F"
        };

        private bool _isSimulating = false;
        private TimeSpan _simTime = new TimeSpan(8, 0, 0); // Default: 08:00

        public TimetablePage()
        {
            InitializeComponent();
            _weekStart = GetMonday(DateTime.Today);
            Loaded += (_, _) =>
            {
                Services.PeriodScheduleService.Instance.EnsureLoaded(ClassroomAppContext.Db);
                LoadClassInfo(); InitSimControls(); LoadSlots(); RenderTimetable(); StartTimer();

                // Auto-refresh khi GV chuyển lớp — N7 FIX: Dùng named handler để Unsubscribe tránh memory leak
                try
                {
                    // → ClassroomAppContext
                    ClassroomAppContext.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                    ClassroomAppContext.ClassRoster.ActiveRosterChanged += OnActiveRosterChanged;

                    SubjectCatalogService.Instance.SubjectsChanged -= OnSubjectsChanged;
                    SubjectCatalogService.Instance.SubjectsChanged += OnSubjectsChanged;

                    Services.PeriodScheduleService.Instance.ScheduleChanged -= OnScheduleChanged;
                    Services.PeriodScheduleService.Instance.ScheduleChanged += OnScheduleChanged;
                }
                catch { }
            };
            Unloaded += (_, _) =>
            {
                StopTimer();
                try
                {
                    // → ClassroomAppContext
                    ClassroomAppContext.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                    SubjectCatalogService.Instance.SubjectsChanged -= OnSubjectsChanged;
                    Services.PeriodScheduleService.Instance.ScheduleChanged -= OnScheduleChanged;
                }
                catch { }
            };
        }

        private void OnActiveRosterChanged(object? sender, Data.ClassRoster? e)
        {
            Dispatcher.Invoke(() => { LoadClassInfo(); LoadSlots(); RenderTimetable(); });
        }

        private void OnSubjectsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(RenderTimetable);
        }

        private void OnScheduleChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(RenderTimetable);
        }

        private void ConfigurePeriodTimes_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ConfigurePeriodTimesDialog
            {
                Owner = Window.GetWindow(this)
            };
            dlg.ShowDialog();
        }

        private void LoadClassInfo()
        {
            try
            {
                // → ClassroomAppContext
                var roster = ClassroomAppContext.ClassRoster.ActiveRoster;

                if (roster != null)
                {
                    _className = roster.ClassName;
                    _schoolYear = roster.SchoolYear;
                }
                else
                {
                    _className = "Lớp học";
                    int year = DateTime.Today.Year;
                    int month = DateTime.Today.Month;
                    _schoolYear = month >= 9 ? $"{year}-{year + 1}" : $"{year - 1}-{year}";
                }
                UpdateClassInfoText();
            }
            catch (Exception ex)
            {
                Log.Warning("LoadClassInfo error: {Err}", ex.Message);
                _className = "Lớp học";
                _schoolYear = $"{DateTime.Today.Year}";
                UpdateClassInfoText();
            }
        }

        private void UpdateClassInfoText()
        {
            if (txtClassInfo == null) return; // Prevent NRE during XAML initialization
            try
            {
                // → ClassroomAppContext
                var roster = ClassroomAppContext.ClassRoster.ActiveRoster;
                string semester = (roster?.Semester == "HK2" || (DateTime.Today.Month >= 1 && DateTime.Today.Month <= 5)) ? "Học kỳ 2" : "Học kỳ 1";
                string subject = roster != null && !string.IsNullOrWhiteSpace(roster.Subject) ? $"  •  {roster.Subject}" : "";
                
                if (_viewMode == "Class")
                {
                    string teacher = roster != null && !string.IsNullOrWhiteSpace(roster.TeacherName) ? $"  •  GV: {roster.TeacherName}" : "";
                    string displayClass = string.IsNullOrWhiteSpace(_className) 
                        ? "Chưa chọn lớp" 
                        : (_className.StartsWith("Lớp", StringComparison.OrdinalIgnoreCase) ? _className : $"Lớp {_className}");
                    txtClassInfo.Text = $"🏫 {displayClass}{subject}  •  Năm học {_schoolYear}  •  {semester}{teacher}";
                }
                else
                {
                    var profile = ClassroomAppContext.Db.TeacherProfiles.FirstOrDefault();
                    string teacherName = profile?.FullName ?? (roster?.TeacherName ?? "Giáo viên");
                    txtClassInfo.Text = $"👨‍🏫 Giáo viên: {teacherName}  •  Năm học {_schoolYear}  •  {semester}";
                }
            }
            catch { }
        }

        private void TimetableMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (txtClassInfo == null || timetableGrid == null) return; // Not fully initialized yet
            if (cboTimetableMode?.SelectedItem is ComboBoxItem item)
            {
                _viewMode = item.Tag?.ToString() ?? "Class";
                UpdateClassInfoText();
                LoadSlots();
                RenderTimetable();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  SIMULATION TIME
        // ═══════════════════════════════════════════════════════════

        private void InitSimControls()
        {
            // Populate hours (6-18)
            for (int h = 6; h <= 18; h++)
                cboSimHour.Items.Add(new ComboBoxItem { Content = h.ToString("D2") });
            // Populate minutes (00, 05, 10, ... 55)
            for (int m = 0; m < 60; m += 5)
                cboSimMinute.Items.Add(new ComboBoxItem { Content = m.ToString("D2") });

            // Default to 08:00
            cboSimHour.SelectedIndex = 2; // 08
            cboSimMinute.SelectedIndex = 0; // 00
        }

        /// <summary>Returns the effective time: simulated or real</summary>
        private TimeSpan GetEffectiveTime()
        {
            return _isSimulating ? _simTime : DateTime.Now.TimeOfDay;
        }

        private void SimTime_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!_isSimulating) return;
            if (cboSimHour?.SelectedItem is not ComboBoxItem hItem) return;
            if (cboSimMinute?.SelectedItem is not ComboBoxItem mItem) return;

            if (int.TryParse(hItem.Content.ToString(), out int h) &&
                int.TryParse(mItem.Content.ToString(), out int m))
            {
                _simTime = new TimeSpan(h, m, 0);
                RenderTimetable();
            }
        }

        private void SimToggle_Click(object sender, RoutedEventArgs e)
        {
            _isSimulating = !_isSimulating;

            if (_isSimulating)
            {
                // Read current combo values
                if (cboSimHour?.SelectedItem is ComboBoxItem hItem &&
                    cboSimMinute?.SelectedItem is ComboBoxItem mItem &&
                    int.TryParse(hItem.Content.ToString(), out int h) &&
                    int.TryParse(mItem.Content.ToString(), out int m))
                {
                    _simTime = new TimeSpan(h, m, 0);
                }

                btnSimToggle.Content = "⏹ Tắt";
                btnSimToggle.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            else
            {
                btnSimToggle.Content = "▶ Test";
                btnSimToggle.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
            }

            RenderTimetable();
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _timer.Tick += (_, _) => RenderTimetable();
            _timer.Start();
        }

        private void StopTimer() { _timer?.Stop(); _timer = null; }

        /// <summary>Returns current period index (0-9) or -1 if not in class time</summary>
        private int GetCurrentPeriod()
        {
            var time = GetEffectiveTime();
            for (int i = 0; i < PeriodTimeRanges.Length; i++)
            {
                var (sh, sm, eh, em) = PeriodTimeRanges[i];
                var start = new TimeSpan(sh, sm, 0);
                var end = new TimeSpan(eh, em, 0);
                if (time >= start && time <= end)
                    return i;
            }
            return -1;
        }

        /// <summary>Returns next period index (0-9) or -1 if no more periods today</summary>
        private int GetNextPeriod()
        {
            var time = GetEffectiveTime();
            for (int i = 0; i < PeriodTimeRanges.Length; i++)
            {
                var (sh, sm, _, _) = PeriodTimeRanges[i];
                var start = new TimeSpan(sh, sm, 0);
                if (time < start)
                    return i;
            }
            return -1;
        }

        /// <summary>Get today's column index (0=Mon...5=Sat) or -1 if Sunday</summary>
        private int GetTodayColumn()
        {
            var today = DateTime.Today;
            for (int d = 0; d < 6; d++)
            {
                if (_weekStart.AddDays(d).Date == today)
                    return d;
            }
            return -1;
        }

        // ═══════════════════════════════════════════════════════════
        //  LOAD / SAVE
        // ═══════════════════════════════════════════════════════════

        private string GetTeacherName()
        {
            try
            {
                // → ClassroomAppContext
                var profile = ClassroomAppContext.Db.TeacherProfiles.FirstOrDefault();
                return profile?.FullName ?? (ClassroomAppContext.ClassRoster.ActiveRoster?.TeacherName ?? "Giáo viên");
            }
            catch
            {
                return "Giáo viên";
            }
        }

        private void LoadSlots()
        {
            try
            {
                // → ClassroomAppContext

                string timetableKey;
                if (_viewMode == "Class")
                {
                    timetableKey = $"TIMETABLE_{_className}";
                }
                else
                {
                    string teacherName = GetTeacherName();
                    timetableKey = $"TIMETABLE_TEACHER_{teacherName}";
                }

                // 1) Try loading from EventLogs first (edited timetable)
                var logs = ClassroomAppContext.Db.EventLogs
                    .Where(e => e.EventType == timetableKey)
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefault();

                if (logs != null && !string.IsNullOrEmpty(logs.Details))
                {
                    // Tự động nhận biết và thay thế mẫu dữ liệu cũ bị trùng lặp do Random(42)
                    if (_viewMode == "Class" && logs.Details.Contains("\"Subject\":\"Địa\"") && logs.Details.Contains("\"Room\":\"P.10A\""))
                    {
                        _slots = Data.TimetableDataHelper.GetDistinctTimetable(_className);
                        SaveSlots();
                        Log.Information("Replaced old duplicate timetable with distinct timetable for {Class}", _className);
                        return;
                    }

                    _slots = System.Text.Json.JsonSerializer.Deserialize<List<TimetableSlot>>(logs.Details)
                             ?? new List<TimetableSlot>();
                    Log.Information("Loaded edited timetable from EventLog for key {Key}: {Count} slots", timetableKey, _slots.Count);
                    return;
                }

                // 2) Fallback to Lessons table
                var query = ClassroomAppContext.Db.Lessons.Where(l => l.Period > 0 && !string.IsNullOrEmpty(l.DayOfWeek));

                if (_viewMode == "Class")
                {
                    query = query.Where(l => l.ClassName == _className);
                }
                else
                {
                    string teacherName = GetTeacherName();
                    if (!string.IsNullOrEmpty(teacherName))
                    {
                        query = query.Where(l => l.TeacherName == teacherName);
                    }
                }

                var dbLessons = query.ToList();

                if (dbLessons.Any())
                {
                    _slots = dbLessons.Select(l =>
                    {
                        int dayIdx = l.DayOfWeek switch
                        {
                            "Thứ 2" => 0, "Thứ 3" => 1, "Thứ 4" => 2,
                            "Thứ 5" => 3, "Thứ 6" => 4, "Thứ 7" => 5, _ => -1
                        };
                        return new TimetableSlot
                        {
                            DayOfWeek = dayIdx,
                            Period = l.Period - 1, // Lesson.Period is 1-based, slot is 0-based
                            Subject = l.Subject,
                            Room = _viewMode == "Class" ? ("P." + l.ClassName) : ("Lớp " + l.ClassName),
                            Teacher = l.TeacherName,
                            Note = l.Title
                        };
                    }).Where(s => s.DayOfWeek >= 0).ToList();
                    Log.Information("Loaded default timetable from DB Lessons for key {Key}: {Count} slots", timetableKey, _slots.Count);
                    return;
                }

                // 3) Final fallback: Sample timetable
                _slots = GenerateSampleTimetable();
                SaveSlots();
                Log.Information("Generated sample timetable for key {Key}: {Count} slots", timetableKey, _slots.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("LoadSlots error: {Err}", ex.Message);
                _slots = GenerateSampleTimetable();
            }
        }

        private void SaveSlots()
        {
            try
            {
                // → ClassroomAppContext
                var json = System.Text.Json.JsonSerializer.Serialize(_slots);
                
                string timetableKey;
                if (_viewMode == "Class")
                {
                    timetableKey = $"TIMETABLE_{_className}";
                }
                else
                {
                    string teacherName = GetTeacherName();
                    timetableKey = $"TIMETABLE_TEACHER_{teacherName}";
                }

                // Remove old for this class/teacher key
                var old = ClassroomAppContext.Db.EventLogs.Where(e => e.EventType == timetableKey).ToList();
                ClassroomAppContext.Db.EventLogs.RemoveRange(old);

                // Add new record
                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog
                {
                    EventType = timetableKey,
                    Actor = "GV",
                    Details = json,
                    Timestamp = DateTime.Now
                });
                ClassroomAppContext.Db.SaveChanges();
                Log.Information("Saved timetable to EventLog for key {Key}: {Count} slots", timetableKey, _slots.Count);
            }
            catch (Exception ex) 
            { 
                Log.Warning("SaveSlots error: {Err}", ex.Message); 
            }
        }

        private List<TimetableSlot> GenerateSampleTimetable()
        {
            if (_viewMode == "Class" && !string.IsNullOrEmpty(_className))
            {
                return Data.TimetableDataHelper.GetDistinctTimetable(_className);
            }
            return Data.TimetableDataHelper.GetDistinctTimetable("10A1");
        }

        // ═══════════════════════════════════════════════════════════
        //  IMPORT / EXPORT (Giai đoạn 3)
        // ═══════════════════════════════════════════════════════════

        private void ExportTimetable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Xuất Thời khóa biểu",
                    Filter = "CSV (Excel)|*.csv",
                    FileName = $"TKB_{_className}_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (dlg.ShowDialog() == true)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("Thu,Tiet,MonHoc,Phong,GiaoVien,GhiChu");

                    foreach (var slot in _slots.OrderBy(s => s.DayOfWeek).ThenBy(s => s.Period))
                    {
                        string thu = DayNames[slot.DayOfWeek];
                        string tiet = PeriodLabels[slot.Period];
                        sb.AppendLine($"\"{thu}\",\"{tiet}\",\"{slot.Subject}\",\"{slot.Room}\",\"{slot.Teacher}\",\"{slot.Note}\"");
                    }

                    System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    ClassroomDialog.Info($"Đã xuất thời khóa biểu ra file:\n{dlg.FileName}", "Thành công");
                    Log.Information("Exported Timetable for {Class} to {Path}", _className, dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export timetable error");
                ClassroomDialog.Error($"Lỗi khi xuất file: {ex.Message}", "Lỗi");
            }
        }

        private void ImportTimetable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Nhập Thời khóa biểu",
                    Filter = "Excel & CSV Files (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv"
                };

                if (dlg.ShowDialog() == true)
                {
                    string filePath = dlg.FileName;
                    System.Data.DataTable dt;

                    if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        dt = QASmartClass.Services.ExcelDataService.ReadExcelToDataTable(filePath);
                    }
                    else if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        dt = ParseTimetableCsvToDataTable(filePath);
                    }
                    else
                    {
                        ClassroomDialog.Warn("Định dạng file không được hỗ trợ. Vui lòng chọn file .xlsx hoặc .csv.", "Lỗi định dạng");
                        return;
                    }

                    if (dt == null || dt.Rows.Count == 0)
                    {
                        ClassroomDialog.Warn("File dữ liệu không có dòng thông tin hợp lệ nào!", "Thông báo");
                        return;
                    }

                    var newSlots = new List<TimetableSlot>();
                    int rowNum = 1;
                    var warnings = new List<string>();

                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        rowNum++;
                        if (dt.Columns.Count < 3) continue;

                        string thu = row[0]?.ToString()?.Trim() ?? "";
                        string tiet = row[1]?.ToString()?.Trim() ?? "";
                        string mon = row[2]?.ToString()?.Trim() ?? "";
                        string phong = dt.Columns.Count > 3 ? row[3]?.ToString()?.Trim() ?? "" : "";
                        string gv = dt.Columns.Count > 4 ? row[4]?.ToString()?.Trim() ?? "" : "";
                        string gc = dt.Columns.Count > 5 ? row[5]?.ToString()?.Trim() ?? "" : "";

                        if (string.IsNullOrEmpty(thu) && string.IsNullOrEmpty(tiet) && string.IsNullOrEmpty(mon)) continue;

                        int dayIdx = Array.IndexOf(DayNames, thu);
                        int periodIdx = Array.IndexOf(PeriodLabels, tiet);

                        if (dayIdx >= 0 && periodIdx >= 0 && !string.IsNullOrEmpty(mon))
                        {
                            newSlots.Add(new TimetableSlot
                            {
                                DayOfWeek = dayIdx,
                                Period = periodIdx,
                                Subject = mon,
                                Room = phong,
                                Teacher = gv,
                                Note = gc
                            });
                        }
                        else
                        {
                            warnings.Add($"Dòng {rowNum}: Lịch học không hợp lệ (Thứ: '{thu}', Tiết: '{tiet}', Môn: '{mon}').");
                        }
                    }

                    if (newSlots.Any())
                    {
                        _slots = newSlots;
                        SaveSlots();
                        RenderTimetable();

                        string msg = $"Đã nhập thành công {newSlots.Count} tiết học!";
                        if (warnings.Any())
                        {
                            msg += $"\n\n⚠️ Dòng bị bỏ qua do không đúng quy chuẩn (Thứ 2->7, Tiết 1->10):\n" + string.Join("\n", warnings.Take(5));
                            if (warnings.Count > 5) msg += $"\n... và {warnings.Count - 5} cảnh báo khác.";
                        }
                        MessageBox.Show(msg, "Thành công", MessageBoxButton.OK, warnings.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
                        Log.Information("Imported Timetable for {Class} from {Path}", _className, filePath);
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy tiết học nào hợp lệ. Vui lòng kiểm tra lại định dạng file (Thứ, Tiết, Môn).", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                ClassroomDialog.Error($"Lỗi nhập TKB: {ex.Message}", "Lỗi");
                Log.Error(ex, "Import timetable error");
            }
        }

        private static System.Data.DataTable ParseTimetableCsvToDataTable(string filePath)
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("Thứ");
            table.Columns.Add("Tiết");
            table.Columns.Add("Môn");
            table.Columns.Add("Phòng");
            table.Columns.Add("Giáo viên");
            table.Columns.Add("Ghi chú");

            var lines = System.IO.File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
            if (lines.Length <= 1) return table;

            // Auto-detect delimiter by reading the header row
            char delimiter = ',';
            string headerLine = lines[0];
            int commaCount = headerLine.Split(',').Length - 1;
            int semicolonCount = headerLine.Split(';').Length - 1;
            if (semicolonCount > commaCount)
            {
                delimiter = ';';
            }

            foreach (var line in lines.Skip(1))
            {
                var cleanLine = line.Trim();
                if (string.IsNullOrEmpty(cleanLine)) continue;

                var cols = QASmartClass.Classroom.Services.ClassRosterService.ParseCsvLine(cleanLine, delimiter);
                var row = table.NewRow();
                for (int col = 0; col < Math.Min(cols.Length, 6); col++)
                {
                    row[col] = cols[col];
                }
                table.Rows.Add(row);
            }
            return table;
        }

        // ═══════════════════════════════════════════════════════════
        //  RENDER GRID
        // ═══════════════════════════════════════════════════════════

        private void RenderTimetable()
        {
            timetableGrid.Children.Clear();
            timetableGrid.ColumnDefinitions.Clear();
            timetableGrid.RowDefinitions.Clear();

            // Update week label
            var weekEnd = _weekStart.AddDays(5);
            int weekNum = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                _weekStart, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday);
            txtWeekRange.Text = $"Tuần {weekNum}: {_weekStart:dd/MM} → {weekEnd:dd/MM/yyyy}";

            // Detect current focus
            int currentPeriod = GetCurrentPeriod();
            int nextPeriod = GetNextPeriod();
            int todayCol = GetTodayColumn();

            // Update subtitle with current period info
            UpdateFocusInfo(currentPeriod, nextPeriod, todayCol);

            // Columns: Period label + 6 days
            timetableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) }); // Period col — wider
            for (int d = 0; d < 6; d++)
                timetableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Rows: Header + Break label + 10 periods
            timetableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // Day header — taller
            for (int p = 0; p < 10; p++)
            {
                if (p == 5) // Break between morning/afternoon
                    timetableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
                timetableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(90) }); // — taller rows
            }

            // ── Day headers ──
            for (int d = 0; d < 6; d++)
            {
                var dayDate = _weekStart.AddDays(d);
                bool isToday = dayDate.Date == DateTime.Today;

                var header = new Border
                {
                    Background = isToday
                        ? new LinearGradientBrush(Color.FromRgb(25, 118, 210), Color.FromRgb(66, 165, 245), 0)
                        : new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                sp.Children.Add(new TextBlock
                {
                    Text = DayNames[d],
                    FontSize = 15, FontWeight = FontWeights.Bold,
                    Foreground = isToday ? Brushes.White : new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                sp.Children.Add(new TextBlock
                {
                    Text = dayDate.ToString("dd/MM"),
                    FontSize = 12, Foreground = isToday ? new SolidColorBrush(Color.FromRgb(200, 230, 255)) : new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0)
                });
                header.Child = sp;
                Grid.SetColumn(header, d + 1);
                Grid.SetRow(header, 0);
                timetableGrid.Children.Add(header);
            }

            // ── Period labels + Cells ──
            int gridRow = 1;
            for (int p = 0; p < 10; p++)
            {
                // Break row
                if (p == 5)
                {
                    var breakBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    breakBorder.Child = new TextBlock
                    {
                        Text = "☕ NGHỈ TRƯA",
                        FontSize = 12, FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 0)),
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(breakBorder, 0);
                    Grid.SetColumnSpan(breakBorder, 7);
                    Grid.SetRow(breakBorder, gridRow);
                    timetableGrid.Children.Add(breakBorder);
                    gridRow++;
                }

                // Period label — highlight if current
                bool isCurrentPeriod = (p == currentPeriod && todayCol >= 0);
                bool isNextPeriod = (p == nextPeriod && todayCol >= 0 && currentPeriod == -1);

                var periodCell = new Border
                {
                    Background = isCurrentPeriod
                        ? new LinearGradientBrush(Color.FromRgb(76, 175, 80), Color.FromRgb(102, 187, 106), 90)
                        : isNextPeriod
                            ? new SolidColorBrush(Color.FromRgb(255, 248, 225))
                            : new SolidColorBrush(Color.FromRgb(250, 250, 252)),
                    BorderBrush = isCurrentPeriod
                        ? new SolidColorBrush(Color.FromRgb(56, 142, 60))
                        : new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                var periodSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

                // "ĐANG HỌC" badge for current period
                if (isCurrentPeriod)
                {
                    var badge = new Border
                    {
                        Background = Brushes.White, CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 0, 4),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    badge.Child = new TextBlock
                    {
                        Text = "▶ ĐANG HỌC", FontSize = 8, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
                    };
                    periodSp.Children.Add(badge);
                }
                else if (isNextPeriod)
                {
                    var badge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(255, 224, 130)),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 0, 0, 4),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    badge.Child = new TextBlock
                    {
                        Text = "⏳ SẮP TỚI", FontSize = 8, FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 0))
                    };
                    periodSp.Children.Add(badge);
                }

                periodSp.Children.Add(new TextBlock
                {
                    Text = PeriodLabels[p], FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = isCurrentPeriod ? Brushes.White : new SolidColorBrush(Color.FromRgb(33, 33, 33)),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                periodSp.Children.Add(new TextBlock
                {
                    Text = PeriodTimes[p], FontSize = 10,
                    Foreground = isCurrentPeriod ? new SolidColorBrush(Color.FromRgb(200, 255, 200)) : new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 0)
                });
                periodCell.Child = periodSp;
                Grid.SetColumn(periodCell, 0);
                Grid.SetRow(periodCell, gridRow);
                timetableGrid.Children.Add(periodCell);

                // Subject cells
                for (int d = 0; d < 6; d++)
                {
                    var slot = _slots.FirstOrDefault(s => s.DayOfWeek == d && s.Period == p);
                    bool isFocusCell = isCurrentPeriod && d == todayCol;
                    var cell = BuildCell(slot, d, p, isFocusCell);
                    Grid.SetColumn(cell, d + 1);
                    Grid.SetRow(cell, gridRow);
                    timetableGrid.Children.Add(cell);
                }

                gridRow++;
            }
        }

        private Border BuildCell(TimetableSlot? slot, int day, int period, bool isFocus = false)
        {
            var border = new Border
            {
                BorderBrush = isFocus
                    ? new SolidColorBrush(Color.FromRgb(46, 125, 50))
                    : new SolidColorBrush(Color.FromRgb(234, 234, 234)),
                BorderThickness = isFocus ? new Thickness(2.5) : new Thickness(0, 0, 1, 1),
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand,
                ToolTip = slot != null ? $"{slot.Subject} — {slot.Room}\nClick để chỉnh sửa" : "Click để thêm môn"
            };

            // Focus glow effect
            if (isFocus)
            {
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(76, 175, 80),
                    BlurRadius = 12,
                    ShadowDepth = 0,
                    Opacity = 0.5
                };
            }

            if (slot != null && !string.IsNullOrEmpty(slot.Subject))
            {
                string color = SubjectCatalogService.Instance.GetColorForSubject(slot.Subject);
                var accentColor = (Color)ColorConverter.ConvertFromString(color);

                border.Background = isFocus
                    ? new SolidColorBrush(Color.FromArgb(50, accentColor.R, accentColor.G, accentColor.B))
                    : new SolidColorBrush(Color.FromArgb(30, accentColor.R, accentColor.G, accentColor.B));

                var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 6, 8, 6) };

                // "NOW" indicator for focus cell
                if (isFocus)
                {
                    var nowBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(8, 2, 8, 2),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    nowBadge.Child = new TextBlock
                    {
                        Text = "🔴 ĐANG HỌC",
                        FontSize = 9, FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    sp.Children.Add(nowBadge);
                }

                // Subject name — LARGER
                sp.Children.Add(new TextBlock
                {
                    Text = slot.Subject,
                    FontSize = isFocus ? 18 : 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(accentColor),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                // Room — bigger
                if (!string.IsNullOrEmpty(slot.Room))
                {
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"📍 {slot.Room}",
                        FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                        HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0)
                    });
                }

                // Lesson badge if assigned
                if (!string.IsNullOrEmpty(slot.LessonTitle) || (slot.LessonId.HasValue && slot.LessonId.Value > 0))
                {
                    string lessonDisplayName = !string.IsNullOrEmpty(slot.LessonTitle)
                        ? slot.LessonTitle
                        : $"Bài #{slot.LessonId}";
                    var lessonBadge = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(90, accentColor.R, accentColor.G, accentColor.B)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 4, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        ToolTip = $"Bài giảng: {lessonDisplayName}"
                    };
                    lessonBadge.Child = new TextBlock
                    {
                        Text = $"📖 {lessonDisplayName}",
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(accentColor),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = 120
                    };
                    sp.Children.Add(lessonBadge);
                }

                // If isFocus (NOW), provide quick 🚀 Vào dạy button
                if (isFocus)
                {
                    var btnStartTeach = new Border
                    {
                        Background = new LinearGradientBrush(Color.FromRgb(21, 101, 192), Color.FromRgb(25, 118, 210), 0),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(10, 4, 10, 4),
                        Margin = new Thickness(0, 5, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Cursor = Cursors.Hand,
                        ToolTip = "Click để vào dạy bài học này ngay lập tức"
                    };
                    btnStartTeach.Child = new TextBlock
                    {
                        Text = "🚀 Vào dạy",
                        FontSize = 10.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    };
                    btnStartTeach.MouseLeftButtonDown += (s, e) =>
                    {
                        e.Handled = true;
                        StartTeachingSlot(slot);
                    };
                    sp.Children.Add(btnStartTeach);
                }

                // Left accent bar — thicker
                var grid = new Grid();
                grid.Children.Add(new Border
                {
                    Width = isFocus ? 5 : 4, HorizontalAlignment = HorizontalAlignment.Left,
                    Background = new SolidColorBrush(accentColor),
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(0, 4, 0, 4)
                });
                grid.Children.Add(sp);
                border.Child = grid;

                // Hover
                var bgNormal = isFocus
                    ? new SolidColorBrush(Color.FromArgb(50, accentColor.R, accentColor.G, accentColor.B))
                    : new SolidColorBrush(Color.FromArgb(30, accentColor.R, accentColor.G, accentColor.B));
                var bgHover = new SolidColorBrush(Color.FromArgb(65, accentColor.R, accentColor.G, accentColor.B));
                border.MouseEnter += (_, _) => border.Background = bgHover;
                border.MouseLeave += (_, _) => border.Background = bgNormal;
            }
            else
            {
                border.Background = Brushes.Transparent;
                border.MouseEnter += (_, _) => border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                border.MouseLeave += (_, _) => border.Background = Brushes.Transparent;
            }

            // Click to edit
            int d = day, p = period;
            border.MouseLeftButtonDown += (_, _) => EditSlot(d, p, slot);

            return border;
        }

        private void UpdateFocusInfo(int currentPeriod, int nextPeriod, int todayCol)
        {
            string simPrefix = _isSimulating ? $"🧪 MÔ PHỎNG ({_simTime:hh\\:mm})  —  " : "";

            if (todayCol < 0)
            {
                _currentActiveSlot = null;
                if (btnQuickTeachNow != null) btnQuickTeachNow.Visibility = Visibility.Collapsed;
                txtCurrentPeriod.Text = _isSimulating ? $"{simPrefix}Chọn giờ trong khoảng 07:00–17:40" : "";
                txtCurrentPeriod.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 0));
                txtCurrentPeriod.FontWeight = FontWeights.SemiBold;
                return;
            }

            var effectiveTime = GetEffectiveTime();

            if (currentPeriod >= 0)
            {
                var slot = _slots.FirstOrDefault(s => s.DayOfWeek == todayCol && s.Period == currentPeriod);
                _currentActiveSlot = slot;
                string subj = slot?.Subject ?? "—";
                string room = slot?.Room ?? "";
                var (_, _, eh, em) = PeriodTimeRanges[currentPeriod];
                var endTime = new TimeSpan(eh, em, 0);
                var remaining = endTime - effectiveTime;
                int minLeft = Math.Max(0, (int)remaining.TotalMinutes);

                txtCurrentPeriod.Text = $"{simPrefix}🔴 Đang học: {PeriodLabels[currentPeriod]} — {subj} {(room != "" ? $"({room})" : "")}  •  Còn {minLeft} phút";
                txtCurrentPeriod.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                txtCurrentPeriod.FontWeight = FontWeights.Bold;

                if (btnQuickTeachNow != null)
                {
                    btnQuickTeachNow.Visibility = Visibility.Visible;
                    string lessonTip = slot != null && !string.IsNullOrEmpty(slot.LessonTitle)
                        ? $" ({slot.LessonTitle})"
                        : "";
                    btnQuickTeachNow.ToolTip = $"Vào dạy ngay tiết này: {subj}{lessonTip}";
                }
            }
            else if (nextPeriod >= 0)
            {
                _currentActiveSlot = null;
                if (btnQuickTeachNow != null) btnQuickTeachNow.Visibility = Visibility.Collapsed;
                var slot = _slots.FirstOrDefault(s => s.DayOfWeek == todayCol && s.Period == nextPeriod);
                string subj = slot?.Subject ?? "—";
                var (sh, sm, _, _) = PeriodTimeRanges[nextPeriod];
                var startTime = new TimeSpan(sh, sm, 0);
                var untilStart = startTime - effectiveTime;
                int minUntil = Math.Max(0, (int)untilStart.TotalMinutes);

                txtCurrentPeriod.Text = $"{simPrefix}⏳ Sắp tới: {PeriodLabels[nextPeriod]} — {subj}  •  Bắt đầu sau {minUntil} phút";
                txtCurrentPeriod.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 0));
                txtCurrentPeriod.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                _currentActiveSlot = null;
                if (btnQuickTeachNow != null) btnQuickTeachNow.Visibility = Visibility.Collapsed;
                txtCurrentPeriod.Text = $"{simPrefix}✅ Đã hết tiết học hôm nay";
                txtCurrentPeriod.Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120));
                txtCurrentPeriod.FontWeight = FontWeights.Normal;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  EDIT SLOT
        // ═══════════════════════════════════════════════════════════

        private void EditSlot(int day, int period, TimetableSlot? existing)
        {
            string currentSubject = existing?.Subject ?? "";
            string currentRoom = existing?.Room ?? "";

            var dlg = new Window
            {
                Title = "Chỉnh sửa thời khóa biểu",
                Width = 480,
                SizeToContent = SizeToContent.Height,
                MaxHeight = 650,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true
            };

            var rootBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 20, ShadowDepth = 4, Opacity = 0.15,
                    Color = Colors.Black
                }
            };

            var mainSp = new StackPanel();

            // ── Dark header ──
            var header = new Border
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(37, 47, 63), Color.FromRgb(55, 71, 95), 0),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                Padding = new Thickness(20, 14, 20, 14)
            };
            var headerSp = new StackPanel();
            headerSp.Children.Add(new TextBlock
            {
                Text = $"📅 {DayNames[day]} — {PeriodLabels[period]}",
                FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.White
            });
            headerSp.Children.Add(new TextBlock
            {
                Text = _viewMode == "Class"
                    ? $"🕐 {PeriodTimes[period]}  •  🏫 Lớp {_className}"
                    : $"🕐 {PeriodTimes[period]}  •  👨‍🏫 GV {GetTeacherName()}",
                FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(180, 200, 220)),
                Margin = new Thickness(0, 4, 0, 0)
            });
            header.Child = headerSp;

            // Close button on header
            var closeBtn = new TextBlock
            {
                Text = "✕", FontSize = 16, Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, -30, 10, 0)
            };
            closeBtn.MouseLeftButtonDown += (_, _) => { dlg.DialogResult = false; };
            headerSp.Children.Add(closeBtn);

            mainSp.Children.Add(header);

            // ── Content area ──
            var contentSp = new StackPanel { Margin = new Thickness(24, 16, 24, 8) };

            // Subject label + Manage Subjects button in header line
            var subjLabelSp = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            var subjLabel = new TextBlock
            {
                Text = "📚 Tên môn học", FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(subjLabel, Dock.Left);
            subjLabelSp.Children.Add(subjLabel);

            var btnManage = new TextBlock
            {
                Text = "⚙️ Quản lý danh mục môn",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnManage.MouseEnter += (_, _) => btnManage.TextDecorations = TextDecorations.Underline;
            btnManage.MouseLeave += (_, _) => btnManage.TextDecorations = null;
            DockPanel.SetDock(btnManage, Dock.Right);
            subjLabelSp.Children.Add(btnManage);
            contentSp.Children.Add(subjLabelSp);

            var txtSubject = new TextBox
            {
                Text = currentSubject, FontSize = 14, Padding = new Thickness(12, 10, 12, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 210, 220)),
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(252, 253, 255))
            };
            txtSubject.GotFocus += (_, _) => txtSubject.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            txtSubject.LostFocus += (_, _) => txtSubject.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 210, 220));
            contentSp.Children.Add(txtSubject);

            // Room label + input (declared earlier for auto-fill binding)
            var roomLabel = new TextBlock
            {
                Text = "📍 Phòng học", FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                Margin = new Thickness(0, 8, 0, 6)
            };
            var txtRoom = new TextBox
            {
                Text = currentRoom, FontSize = 14, Padding = new Thickness(12, 10, 12, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 210, 220)),
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(252, 253, 255))
            };
            txtRoom.GotFocus += (_, _) => txtRoom.BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            txtRoom.LostFocus += (_, _) => txtRoom.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 210, 220));

            // Quick subject chips inside a scrollable container
            var chipPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 4) };
            var chipScroll = new ScrollViewer
            {
                Content = chipPanel,
                MaxHeight = 110,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 4, 0, 4)
            };

            void PopulateChips()
            {
                chipPanel.Children.Clear();
                var allSubjects = SubjectCatalogService.Instance.GetAllSubjects();
                foreach (var subj in allSubjects)
                {
                    string color = subj.ColorHex;
                    if (string.IsNullOrWhiteSpace(color)) color = "#546E7A";
                    Color c;
                    try { c = (Color)ColorConverter.ConvertFromString(color); }
                    catch { c = Color.FromRgb(84, 110, 122); }

                    var chip = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(22, c.R, c.G, c.B)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(60, c.R, c.G, c.B)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(12),
                        Padding = new Thickness(9, 4, 9, 4),
                        Margin = new Thickness(0, 0, 5, 5),
                        Cursor = Cursors.Hand,
                        ToolTip = $"{subj.Name} ({subj.ShortName})" +
                                  (!string.IsNullOrEmpty(subj.DefaultRoom) ? $"\nPhòng mặc định: {subj.DefaultRoom}" : "")
                    };
                    var chipSp = new StackPanel { Orientation = Orientation.Horizontal };
                    if (!string.IsNullOrEmpty(subj.Icon))
                    {
                        chipSp.Children.Add(new TextBlock
                        {
                            Text = subj.Icon + " ",
                            FontSize = 11,
                            VerticalAlignment = VerticalAlignment.Center
                        });
                    }
                    chipSp.Children.Add(new TextBlock
                    {
                        Text = subj.ShortName,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(c),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    chip.Child = chipSp;

                    var chosenSubj = subj;
                    chip.MouseLeftButtonDown += (_, _) =>
                    {
                        txtSubject.Text = chosenSubj.ShortName;
                        if (!string.IsNullOrWhiteSpace(chosenSubj.DefaultRoom) && string.IsNullOrWhiteSpace(txtRoom.Text))
                        {
                            txtRoom.Text = chosenSubj.DefaultRoom;
                        }
                    };
                    chip.MouseEnter += (_, _) => chip.Background = new SolidColorBrush(Color.FromArgb(50, c.R, c.G, c.B));
                    chip.MouseLeave += (_, _) => chip.Background = new SolidColorBrush(Color.FromArgb(22, c.R, c.G, c.B));
                    chipPanel.Children.Add(chip);
                }
            }

            PopulateChips();
            contentSp.Children.Add(chipScroll);

            btnManage.MouseLeftButtonDown += (_, _) =>
            {
                var manageDlg = new SubjectManagementDialog { Owner = dlg };
                manageDlg.ShowDialog();
                PopulateChips();
            };

            contentSp.Children.Add(roomLabel);
            contentSp.Children.Add(txtRoom);

            // ── Lesson assignment section ──
            var lessonLabelSp = new DockPanel { Margin = new Thickness(0, 10, 0, 6) };
            var lessonLabel = new TextBlock
            {
                Text = "📖 Gán bài giảng số cho tiết này",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(lessonLabel, Dock.Left);
            lessonLabelSp.Children.Add(lessonLabel);

            var cboLessons = new ComboBox
            {
                Height = 36,
                FontSize = 13,
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(Color.FromRgb(252, 253, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 210, 220)),
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(0, 0, 0, 8)
            };

            cboLessons.Items.Add(new ComboBoxItem { Content = "— Chưa gán bài giảng —", Tag = (int?)null });

            int? currentLessonId = existing?.LessonId;
            try
            {
                var allLessons = ClassroomAppContext.Db.Lessons
                    .OrderByDescending(l => l.UpdatedAt)
                    .Take(50)
                    .ToList();

                foreach (var l in allLessons)
                {
                    var item = new ComboBoxItem
                    {
                        Content = $"[{l.Subject}] {l.Title} (Lớp {l.Grade} - {l.DurationMinutes}p)",
                        Tag = (int?)l.Id
                    };
                    cboLessons.Items.Add(item);
                    if (currentLessonId.HasValue && l.Id == currentLessonId.Value)
                    {
                        cboLessons.SelectedItem = item;
                    }
                }
            }
            catch { }

            if (cboLessons.SelectedItem == null)
            {
                cboLessons.SelectedIndex = 0;
            }

            contentSp.Children.Add(lessonLabelSp);
            contentSp.Children.Add(cboLessons);

            mainSp.Children.Add(contentSp);

            // ── Styled buttons ──
            var btnPanel = new DockPanel { Margin = new Thickness(24, 12, 24, 20) };

            // Delete button (left)
            var btnDelete = CreateStyledButton("🗑️ Xóa tiết", "#F44336", "#FFFFFF", 12);
            DockPanel.SetDock(btnDelete, Dock.Left);

            // Cancel + Save (right)
            var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCancel = CreateStyledButton("✕ Hủy", "#E0E0E0", "#555555", 12);
            btnCancel.Margin = new Thickness(0, 0, 8, 0);
            var btnOk = CreateStyledButton("✅ Lưu thay đổi", "#1976D2", "#FFFFFF", 12);

            bool deleted = false;
            btnDelete.MouseLeftButtonDown += (_, _) => { deleted = true; dlg.DialogResult = true; };
            btnOk.MouseLeftButtonDown += (_, _) => { dlg.DialogResult = true; };
            btnCancel.MouseLeftButtonDown += (_, _) => { dlg.DialogResult = false; };

            rightPanel.Children.Add(btnCancel);
            rightPanel.Children.Add(btnOk);

            btnPanel.Children.Add(btnDelete);
            btnPanel.Children.Add(rightPanel);

            mainSp.Children.Add(btnPanel);
            rootBorder.Child = mainSp;
            dlg.Content = rootBorder;

            // Allow drag
            header.MouseLeftButtonDown += (_, _) => dlg.DragMove();

            if (dlg.ShowDialog() != true) return;

            // Remove existing
            _slots.RemoveAll(s => s.DayOfWeek == day && s.Period == period);

            int? selectedLessonId = null;
            string selectedLessonTitle = "";
            if (cboLessons.SelectedItem is ComboBoxItem selItem && selItem.Tag is int lid && lid > 0)
            {
                selectedLessonId = lid;
                try
                {
                    var lObj = ClassroomAppContext.Db.Lessons.Find(lid);
                    if (lObj != null) selectedLessonTitle = lObj.Title;
                }
                catch { }
            }

            if (!deleted && !string.IsNullOrWhiteSpace(txtSubject.Text))
            {
                _slots.Add(new TimetableSlot
                {
                    DayOfWeek = day,
                    Period = period,
                    Subject = txtSubject.Text.Trim(),
                    Room = txtRoom.Text.Trim(),
                    Teacher = _viewMode == "Class" ? "GV" : GetTeacherName(),
                    LessonId = selectedLessonId,
                    LessonTitle = selectedLessonTitle
                });
            }

            SaveSlots();
            RenderTimetable();
        }

        private Border CreateStyledButton(string text, string bgColor, string fgColor, double fontSize)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgColor);
            var fg = (Color)ColorConverter.ConvertFromString(fgColor);

            var border = new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                Cursor = Cursors.Hand
            };
            border.Child = new TextBlock
            {
                Text = text, FontSize = fontSize, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(fg),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var normalBg = new SolidColorBrush(bg);
            var hoverBg = new SolidColorBrush(Color.FromArgb(220, bg.R, bg.G, bg.B));
            border.MouseEnter += (_, _) => { border.Background = hoverBg; border.RenderTransform = new ScaleTransform(1.02, 1.02); };
            border.MouseLeave += (_, _) => { border.Background = normalBg; border.RenderTransform = null; };

            return border;
        }

        // ═══════════════════════════════════════════════════════════
        //  NAVIGATION
        // ═══════════════════════════════════════════════════════════

        private void PrevWeek_Click(object sender, RoutedEventArgs e)
        {
            _weekStart = _weekStart.AddDays(-7);
            RenderTimetable();
        }

        private void NextWeek_Click(object sender, RoutedEventArgs e)
        {
            _weekStart = _weekStart.AddDays(7);
            RenderTimetable();
        }

        private void ThisWeek_Click(object sender, RoutedEventArgs e)
        {
            _weekStart = GetMonday(DateTime.Today);
            RenderTimetable();
        }

        private void AddSlot_Click(object sender, RoutedEventArgs e)
        {
            EditSlot(0, 0, null); // Default: Monday period 1
        }

        private void ManageSubjects_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SubjectManagementDialog
            {
                Owner = Window.GetWindow(this)
            };
            dlg.ShowDialog();
            RenderTimetable();
        }

        private void OpenPeriodLogbook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var db = ClassroomAppContext.Db;
                var logbooks = db.PeriodLogbooks
                    .OrderByDescending(p => p.Date)
                    .ThenByDescending(p => p.Period)
                    .Take(100)
                    .ToList();

                var win = new Window
                {
                    Title = "📖 SỔ ĐẦU BÀI ĐIỆN TỬ — QA SMARTSCHOOL",
                    Width = 980,
                    Height = 650,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Owner = Window.GetWindow(this),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
                };

                var root = new Grid { Margin = new Thickness(24) };
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Header
                var headerSp = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
                headerSp.Children.Add(new TextBlock
                {
                    Text = "📖 SỔ ĐẦU BÀI ĐIỆN TỬ THEO TIẾT DẠY",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
                });
                headerSp.Children.Add(new TextBlock
                {
                    Text = "Theo dõi nhật ký giảng dạy, sĩ số lớp, nhận xét sư phạm và xếp loại giờ dạy chuẩn Bộ GD&ĐT",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(0, 4, 0, 0)
                });
                Grid.SetRow(headerSp, 0);
                root.Children.Add(headerSp);

                // Stats cards
                int totalLogs = logbooks.Count;
                int countA = logbooks.Count(l => l.Rating == "A");
                int countB = logbooks.Count(l => l.Rating == "B");
                int countC = logbooks.Count(l => l.Rating == "C");

                var statsGrid = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 16) };
                statsGrid.Children.Add(CreateStatCard("Tổng số tiết ghi nhận", totalLogs.ToString(), "#EEF2FF", "#4338CA"));
                statsGrid.Children.Add(CreateStatCard("Tiết dạy Xếp loại A (Tốt)", countA.ToString(), "#ECFDF5", "#047857"));
                statsGrid.Children.Add(CreateStatCard("Tiết dạy Xếp loại B (Khá)", countB.ToString(), "#EFF6FF", "#1D4ED8"));
                statsGrid.Children.Add(CreateStatCard("Tiết loại C / Cần chú ý", countC.ToString(), "#FFFBEB", "#B45309"));

                Grid.SetRow(statsGrid, 1);
                root.Children.Add(statsGrid);

                // DataGrid Table
                var listBorder = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(12),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8)
                };

                var dg = new DataGrid
                {
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    CanUserAddRows = false,
                    GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                    HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    BorderThickness = new Thickness(0),
                    Background = Brushes.White,
                    RowHeight = 40,
                    FontSize = 12.5,
                    ItemsSource = logbooks
                };

                dg.Columns.Add(new DataGridTextColumn { Header = "Ngày", Binding = new Binding("Date") { StringFormat = "dd/MM/yyyy" }, Width = new DataGridLength(90) });
                dg.Columns.Add(new DataGridTextColumn { Header = "Tiết", Binding = new Binding("Period") { StringFormat = "Tiết {0}" }, Width = new DataGridLength(65) });
                dg.Columns.Add(new DataGridTextColumn { Header = "Lớp", Binding = new Binding("ClassName"), Width = new DataGridLength(75) });
                dg.Columns.Add(new DataGridTextColumn { Header = "Môn học", Binding = new Binding("Subject"), Width = new DataGridLength(95) });
                dg.Columns.Add(new DataGridTextColumn { Header = "Tên bài dạy / Bài giảng số", Binding = new Binding("LessonTitle"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
                dg.Columns.Add(new DataGridTextColumn { Header = "Sĩ số (Có/Tổng)", Binding = new Binding("PresentCount") { StringFormat = "{0}" }, Width = new DataGridLength(90) });
                dg.Columns.Add(new DataGridTextColumn { Header = "Xếp loại", Binding = new Binding("Rating") { StringFormat = "Loại {0}" }, Width = new DataGridLength(75) });
                dg.Columns.Add(new DataGridTextColumn { Header = "Nhận xét của GV", Binding = new Binding("TeacherComment"), Width = new DataGridLength(3, DataGridLengthUnitType.Star) });
                dg.Columns.Add(new DataGridTextColumn { Header = "Bài tập về nhà", Binding = new Binding("HomeworkAssigned"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });

                listBorder.Child = dg;
                Grid.SetRow(listBorder, 2);
                root.Children.Add(listBorder);

                // Footer
                var footerSp = new DockPanel { Margin = new Thickness(0, 16, 0, 0) };
                var btnClose = new Button
                {
                    Content = "Đóng",
                    Padding = new Thickness(24, 10, 24, 10),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    Cursor = Cursors.Hand
                };
                btnClose.Click += (_, _) => win.Close();
                DockPanel.SetDock(btnClose, Dock.Right);
                footerSp.Children.Add(btnClose);

                var txtHint = new TextBlock
                {
                    Text = "💡 Dữ liệu được ghi nhận tự động sau mỗi khi giáo viên kết thúc tiết học từ Trình giảng dạy.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                footerSp.Children.Add(txtHint);

                Grid.SetRow(footerSp, 3);
                root.Children.Add(footerSp);

                win.Content = root;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                ClassroomDialog.Error($"Không thể mở Sổ đầu bài điện tử: {ex.Message}", "Lỗi");
                Log.Error(ex, "OpenPeriodLogbook error");
            }
        }

        private static Border CreateStatCard(string title, string value, string bgHex, string fgHex)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);

            var b = new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(4)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(fg)
            });
            sp.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg),
                Margin = new Thickness(0, 4, 0, 0)
            });
            b.Child = sp;
            return b;
        }

        private void QuickTeachNow_Click(object sender, RoutedEventArgs e)
        {
            if (_currentActiveSlot != null)
            {
                StartTeachingSlot(_currentActiveSlot);
            }
        }

        private void StartTeachingSlot(TimetableSlot slot)
        {
            try
            {
                // 1. Activate class if needed
                if (!string.IsNullOrEmpty(_className))
                {
                    var roster = ClassroomAppContext.Db.ClassRosters.FirstOrDefault(r => r.ClassName == _className);
                    if (roster != null)
                    {
                        ClassroomAppContext.ClassRoster.SetActiveRoster(roster);
                    }
                }

                int targetLessonId = slot.LessonId ?? 0;
                
                // If not assigned, try to find a lesson for this subject
                if (targetLessonId == 0 && !string.IsNullOrEmpty(slot.Subject))
                {
                    var matchingLesson = ClassroomAppContext.Db.Lessons
                        .Where(l => l.Subject == slot.Subject || slot.Subject.Contains(l.Subject))
                        .OrderByDescending(l => l.UpdatedAt)
                        .FirstOrDefault();
                    if (matchingLesson != null)
                    {
                        targetLessonId = matchingLesson.Id;
                    }
                }

                // 2. Open in ClassroomShell
                var shell = Window.GetWindow(this) as ClassroomShell;
                if (shell != null)
                {
                    shell.NavigateToRunner(targetLessonId);
                }
                else
                {
                    NavigationService?.Navigate(new LessonRunnerPage(targetLessonId));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "StartTeachingSlot failed");
            }
        }

        private static DateTime GetMonday(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  DATA MODEL
    // ═══════════════════════════════════════════════════════════

    public class TimetableSlot
    {
        public int DayOfWeek { get; set; } // 0=Mon, 5=Sat
        public int Period { get; set; }    // 0-9
        public string Subject { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string Teacher { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public int? LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
    }
}
