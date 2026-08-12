using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.TeacherHub.Views
{
    public partial class HomeroomDiaryPage : Page
    {
        private AppDbContext? _db;

        public HomeroomDiaryPage()
        {
            InitializeComponent();
            DpDate.SelectedDate = DateTime.Now.Date;
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            LoadRosters();
            LoadDiary();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        private void LoadRosters()
        {
            if (_db == null) return;
            try
            {
                var rosters = _db.ClassRosters.OrderBy(r => r.ClassName).ToList();
                CbRoster.Items.Clear();
                foreach (var r in rosters)
                {
                    CbRoster.Items.Add(new ComboBoxItem { Content = r.ClassName, Tag = r.Id.ToString() });
                }
                if (CbRoster.Items.Count > 0)
                    CbRoster.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i táº£i danh sÃ¡ch ClassRosters");
            }
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadDiary();
        }

        private void CbRoster_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadDiary();
        }

        private int GetCurrentRosterId()
        {
            return int.TryParse((CbRoster.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out int id) ? id : 0;
        }

        private void LoadDiary()
        {
            if (_db == null) return;
            try
            {
                int rosterId = GetCurrentRosterId();
                if (rosterId == 0) return;
                DateTime date = DpDate.SelectedDate ?? DateTime.Now.Date;

                // Auto-count absent t? AttendanceRecords
                var absentCount = _db.AttendanceRecords
                    .Count(a => a.RosterId == rosterId && a.Date == date && a.Status == "Absent");

                var diary = _db.HomeroomDiaries.FirstOrDefault(d => d.RosterId == rosterId && d.Date == date);

                if (diary != null)
                {
                    TxtAbsentCount.Text = diary.AbsentCount > 0 ? diary.AbsentCount.ToString() : absentCount.ToString();
                    TxtDiscipline.Text = diary.DisciplineNotes;
                    TxtEvents.Text = diary.Events;
                    TxtReminders.Text = diary.Reminders;
                }
                else
                {
                    TxtAbsentCount.Text = absentCount.ToString();
                    TxtDiscipline.Text = "";
                    TxtEvents.Text = "";
                    TxtReminders.Text = "";
                }

                LoadDisciplines();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i táº£i dá»¯ liá»‡u HomeroomDiary");
            }
        }

        // --- P2-08: Khen thuong / Ky luat ---
        private void LoadDisciplines()
        {
            if (_db == null) return;
            try
            {
                string className = (CbRoster.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                if (string.IsNullOrEmpty(className)) return;

                string filter = (CboDisciplineFilter?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";

                var records = _db.DisciplineRecords
                    .Where(d => d.ClassName == className)
                    .OrderByDescending(d => d.Date)
                    .ToList();

                if (filter != "All")
                    records = records.Where(d => d.Type == filter).ToList();

                var green = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                var red = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                var orange = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));

                var bgGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));
                var bgRed = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                var bgOrange = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));

                LvDisciplines.ItemsSource = records.Select(d => new
                {
                    d.StudentName,
                    d.Reason,
                    DateStr = d.Date.ToString("dd/MM/yyyy"),
                    TypeText = d.Type == "Commendation" ? "Khen thÆ°á»Ÿng" : d.Type == "Warning" ? "Nháº¯c nhá»Ÿ" : "Ká»· luáº­t",
                    TypeFg = d.Type == "Commendation" ? green : d.Type == "Warning" ? orange : red,
                    TypeBg = d.Type == "Commendation" ? bgGreen : d.Type == "Warning" ? bgOrange : bgRed
                }).ToList();
            }
            catch (Exception ex) { Log.Warning("[Discipline] Load err: {Err}", ex.Message); }
        }

        private void CboDisciplineFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadDisciplines();
        }

        private void BtnAddDiscipline_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            string className = (CbRoster.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            if (string.IsNullOrEmpty(className)) return;

            int rosterId = GetCurrentRosterId();
            if (rosterId == 0)
            {
                MessageBox.Show("Vui lÃ²ng chá» n lá»›p há» c trÆ°á»›c khi thÃªm khen thÆ°á»Ÿng/ká»· luáº­t.", "Cáº£nh bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new Window
            {
                Title = "ThÃªm Khen thÆ°á»Ÿng / Ká»· luáº­t", Width = 400, Height = 580,
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = new FontFamily("Segoe UI"), ResizeMode = ResizeMode.NoResize
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            // ComboBox chá» n há» c sinh thay cho TextBox nháº­p tay
            var cboStudents = new ComboBox { FontSize = 13, Margin = new Thickness(0,0,0,10), SelectedValuePath = "Id", DisplayMemberPath = "DisplayInfo", IsEditable = true, IsReadOnly = false };
            var studentList = _db.ClassRosterStudents
                .Where(rs => rs.RosterId == rosterId)
                .Join(_db.Students,
                    rs => rs.StudentId,
                    s => s.Id,
                    (rs, s) => new { s.Id, DisplayInfo = $"{s.FullName} ({s.StudentCode})" })
                .ToList();
            cboStudents.ItemsSource = studentList;
            if (studentList.Any()) cboStudents.SelectedIndex = 0;

            sp.Children.Add(new TextBlock { Text = "Há» c sinh:", Margin = new Thickness(0,0,0,4) });
            sp.Children.Add(cboStudents);

            var cboType = new ComboBox { FontSize = 13, Margin = new Thickness(0,0,0,10) };
            cboType.Items.Add(new ComboBoxItem { Content = "Khen thÆ°á»Ÿng", Tag = "Commendation" });
            cboType.Items.Add(new ComboBoxItem { Content = "Nháº¯c nhá»Ÿ", Tag = "Warning" });
            cboType.Items.Add(new ComboBoxItem { Content = "Ká»· luáº­t", Tag = "Discipline" });
            cboType.SelectedIndex = 0;
            sp.Children.Add(new TextBlock { Text = "Loáº¡i:", Margin = new Thickness(0,0,0,4) });
            sp.Children.Add(cboType);

            // Bá»• sung ComboBox cho má»©c Ä‘á»™ vi pháº¡m vÃ  hÃ¬nh thá»©c ká»· luáº­t
            var cboViolationType = new ComboBox { FontSize = 13, Margin = new Thickness(0,0,0,10) };
            cboViolationType.Items.Add(new ComboBoxItem { Content = "Nháº¹", Tag = "Nháº¹" });
            cboViolationType.Items.Add(new ComboBoxItem { Content = "Trung bÃ¬nh", Tag = "Trung bÃ¬nh" });
            cboViolationType.Items.Add(new ComboBoxItem { Content = "Náº·ng", Tag = "Náº·ng" });
            cboViolationType.SelectedIndex = 0;

            var cboDisciplineLevel = new ComboBox { FontSize = 13, Margin = new Thickness(0,0,0,15) };
            cboDisciplineLevel.Items.Add(new ComboBoxItem { Content = "Nháº¯c nhá»Ÿ riÃªng", Tag = "Nháº¯c nhá»Ÿ riÃªng" });
            cboDisciplineLevel.Items.Add(new ComboBoxItem { Content = "PhÃª bÃ¬nh trÆ°á»›c lá»›p", Tag = "PhÃª bÃ¬nh trÆ°á»›c lá»›p" });
            cboDisciplineLevel.Items.Add(new ComboBoxItem { Content = "Ká»· luáº­t trÆ°á»›c há»™i Ä‘á»“ng", Tag = "Ká»· luáº­t trÆ°á»›c há»™i Ä‘á»“ng" });
            cboDisciplineLevel.SelectedIndex = 0;

            var lblViolation = new TextBlock { Text = "Má»©c Ä‘á»™ vi pháº¡m:", Margin = new Thickness(0,0,0,4) };
            var lblLevel = new TextBlock { Text = "HÃ¬nh thá»©c xá» lÃ½:", Margin = new Thickness(0,0,0,4) };

            sp.Children.Add(lblViolation);
            sp.Children.Add(cboViolationType);
            sp.Children.Add(lblLevel);
            sp.Children.Add(cboDisciplineLevel);

            void UpdateVisibility()
            {
                var selectedTag = (cboType.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                if (selectedTag == "Discipline" || selectedTag == "Warning")
                {
                    lblViolation.Visibility = Visibility.Visible;
                    cboViolationType.Visibility = Visibility.Visible;
                    lblLevel.Visibility = Visibility.Visible;
                    cboDisciplineLevel.Visibility = Visibility.Visible;
                    
                    if (selectedTag == "Warning")
                    {
                        lblLevel.Text = "HÃ¬nh thá»©c nháº¯c nhá»Ÿ:";
                    }
                    else
                    {
                        lblLevel.Text = "HÃ¬nh thá»©c ká»· luáº­t:";
                    }
                }
                else
                {
                    lblViolation.Visibility = Visibility.Collapsed;
                    cboViolationType.Visibility = Visibility.Collapsed;
                    lblLevel.Visibility = Visibility.Collapsed;
                    cboDisciplineLevel.Visibility = Visibility.Collapsed;
                }
            }

            cboType.SelectionChanged += (s, ev) => UpdateVisibility();
            UpdateVisibility(); // Initial trigger

            var txtReason = new TextBox { FontSize = 13, Height = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,15) };
            sp.Children.Add(new TextBlock { Text = "LÃ½ do:", Margin = new Thickness(0,0,0,4) });
            sp.Children.Add(txtReason);

            var btn = new Button { Content = "LÆ°u láº¡i", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")), Foreground = Brushes.White, Padding = new Thickness(10) };
            btn.Click += (_, __) =>
            {
                try
                {
                    if (cboStudents.SelectedValue == null)
                    {
                        MessageBox.Show("Vui lÃ²ng chá» n há» c sinh.", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    int studentId = (int)cboStudents.SelectedValue;
                    string type = (cboType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Warning";
                    string reason = txtReason.Text.Trim();
                    string violationType = "";
                    string disciplineLevel = "";

                    if (type == "Discipline" || type == "Warning")
                    {
                        violationType = (cboViolationType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Nháº¹";
                        disciplineLevel = (cboDisciplineLevel.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Nháº¯c nhá»Ÿ riÃªng";
                    }

                    if ((type == "Discipline" || type == "Warning") && string.IsNullOrWhiteSpace(reason))
                    {
                        MessageBox.Show("Vui lÃ²ng nháº­p lÃ½ do/nháº­n xÃ©t chi tiáº¿t khi nháº¯c nhá»Ÿ hoáº·c ká»· luáº­t há» c sinh.", "Thiáº¿u thÃ´ng tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var student = _db.Students.Find(studentId);
                    if (student == null)
                    {
                        MessageBox.Show("KhÃ´ng tÃ¬m tháº¥y dá»¯ liá»‡u há» c sinh trong há»‡ thá»‘ng.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var disciplineService = new QASmartClass.Services.DisciplineService(_db);
                    string reporter = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "GV_Mock";
                    bool success = disciplineService.CreateHomeroomRecord(student.Id, student.FullName, className, type, reason, reporter, violationType, disciplineLevel);
                    
                    if (success)
                    {
                        dlg.Close();
                        LoadDisciplines();
                    }
                    else
                    {
                        MessageBox.Show("LÆ°u báº£n ghi tháº¥t báº¡i qua dá»‹ch vá»¥.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Lá»—i khi lÆ°u: " + ex.Message, "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error); }
            };
            sp.Children.Add(btn);
            dlg.Content = sp;
            dlg.ShowDialog();
        }

        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                int rosterId = GetCurrentRosterId();
                var roster = _db.ClassRosters.Find(rosterId);
                if (roster == null)
                {
                    MessageBox.Show("Vui lÃ²ng chá»n lá»›p chá»§ nhiá»‡m.", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string className = roster.ClassName;
                string semester = $"Há»c ká»³ {roster.Semester}, {roster.SchoolYear}";

                // 1. Láº¥y danh sÃ¡ch há»c sinh thá»±c táº¿ cá»§a lá»›p
                var rosterStudents = _db.ClassRosterStudents
                    .Where(rs => rs.RosterId == rosterId)
                    .ToList();

                if (!rosterStudents.Any())
                {
                    MessageBox.Show("Lá»›p há»c nÃ y chÆ°a cÃ³ há»c sinh nÃ o.", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. TÃ­nh GPA thá»±c táº¿ cá»§a tá»«ng há»c sinh sá»­ dá»¥ng ContactBookService
                var contactService = new QASmartClass.Services.ContactBookService(_db);
                var grades = new List<QASmartClass.Services.StudentGradeDto>();

                foreach (var rs in rosterStudents)
                {
                    var student = _db.Students.Find(rs.StudentId);
                    if (student != null)
                    {
                        var entry = contactService.GetOrCalculateEntry(student.Id, roster.SchoolYear, roster.Semester);
                        grades.Add(new QASmartClass.Services.StudentGradeDto
                        {
                            StudentCode = student.StudentCode,
                            FullName = student.FullName,
                            Score = entry.AverageGrade
                        });
                    }
                }

                // 3. TÃ­nh tá»•ng sá»‘ ngÃ y váº¯ng máº·t thá»±c táº¿ cá»§a cáº£ lá»›p
                int totalAbsences = _db.AttendanceRecords
                    .Count(a => a.RosterId == rosterId && (a.Status == "Absent" || a.Status == "Váº¯ng"));

                // 4. TÃ­nh tá»•ng sá»‘ ká»· luáº­t/nháº¯c nhá»Ÿ thá»±c táº¿ cá»§a cáº£ lá»›p
                int totalDiscipline = _db.DisciplineRecords
                    .Count(d => d.ClassName == className && (d.Type == "Discipline" || d.Type == "Warning"));

                string emulationRank = "Háº¡ng 3 / 15 lá»›p"; // Chá»‰ sá»‘ mÃ´ phá»ng thi Ä‘ua ngoÃ i há»‡ thá»‘ng

                var exportService = new QASmartClass.Services.PdfExportService(_db);
                string path = exportService.GenerateHomeroomReport(className, semester, grades, totalAbsences, totalDiscipline, emulationRank);

                if (!string.IsNullOrEmpty(path))
                {
                    MessageBox.Show($"ÄÃ£ xuáº¥t bÃ¡o cÃ¡o tá»•ng káº¿t thÃ nh cÃ´ng!\nLÆ°u táº¡i: {path}", "Xuáº¥t BÃ¡o cÃ¡o", MessageBoxButton.OK, MessageBoxImage.Information);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Lá»—i khi táº¡o bÃ¡o cÃ¡o.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i BtnReport_Click");
                MessageBox.Show("CÃ³ lá»—i xáº£y ra khi táº¡o bÃ¡o cÃ¡o: " + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                int rosterId = GetCurrentRosterId();
                DateTime date = DpDate.SelectedDate ?? DateTime.Now.Date;

                var diary = _db.HomeroomDiaries.FirstOrDefault(d => d.RosterId == rosterId && d.Date == date);

                int absent = int.TryParse(TxtAbsentCount.Text, out int a) ? a : 0;

                if (diary != null)
                {
                    // Update
                    diary.AbsentCount = absent;
                    diary.DisciplineNotes = TxtDiscipline.Text.Trim();
                    diary.Events = TxtEvents.Text.Trim();
                    diary.Reminders = TxtReminders.Text.Trim();
                    diary.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Add new
                    diary = new HomeroomDiary
                    {
                        RosterId = rosterId,
                        Date = date,
                        AbsentCount = absent,
                        DisciplineNotes = TxtDiscipline.Text.Trim(),
                        Events = TxtEvents.Text.Trim(),
                        Reminders = TxtReminders.Text.Trim()
                    };
                    _db.HomeroomDiaries.Add(diary);
                }

                _db.SaveChanges();
                MessageBox.Show("ÄÃ£ lÆ°u nháº­t kÃ½ chá»§ nhiá»‡m thÃ nh cÃ´ng!", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i lÆ°u HomeroomDiary");
                MessageBox.Show("CÃ³ lá»—i xáº£y ra khi lÆ°u nháº­t kÃ½ chá»§ nhiá»‡m.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

