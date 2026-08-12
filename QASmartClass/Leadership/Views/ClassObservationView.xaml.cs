using QASmartClass.Data;
using QASmartClass.Staff.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Leadership.Views
{
    public partial class ClassObservationView : Page
    {
        private AppDbContext? _db;
        private DateTime _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public ClassObservationView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            _db = new AppDbContext();
            LoadData();
            LoadDepartments();
            LoadSchedules();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
        }

        // -------------------------------------------
        //  TAB 1  Phiếu dự giờ (WI-14 Rubric MOET)
        // -------------------------------------------

        private void LoadData()
        {
            if (_db == null) return;
            try
            {
                var list = _db.ClassObservations.OrderByDescending(o => o.ObservedAt).ToList();
                LvObs.ItemsSource = list.Select(o =>
                {
                    var total = 0.25 * o.LessonPlanScore + 0.30 * o.DeliveryScore
                              + 0.20 * o.StudentEngagementScore + 0.15 * o.TeacherSupportScore
                              + 0.10 * o.AssessmentScore;
                    var rating = total >= 3.5 ? "Tốt" : total >= 2.5 ? "Khá" : total >= 1.5 ? "Đạt" : "Chưa đạt";
                    return new
                    {
                        Title = $"GV: {o.TeacherName}  Lớp {o.ClassName}  Môn {o.Subject}",
                        Info = $"Người dự giờ: {o.ObserverName} | Ngày: {o.ObservedAt:dd/MM/yyyy} | Tổng: {total:F2}/4",
                        Notes = string.IsNullOrWhiteSpace(o.Notes) ? "" : $" Ghi chú: {o.Notes}",
                        RecommendText = string.IsNullOrWhiteSpace(o.Recommendation) ? "" : $" Đề xuất: {o.Recommendation}",
                        ScoreLP = $"KHBD:{o.LessonPlanScore}",
                        ScoreDL = $"TCH:{o.DeliveryScore}",
                        ScoreSE = $"HHS:{o.StudentEngagementScore}",
                        ScoreTS = $"HTGV:{o.TeacherSupportScore}",
                        ScoreAS = $"KTG:{o.AssessmentScore}",
                        RatingText = $"{rating} ({total:F2})",
                        RatingBg = rating == "Tốt" ? "#DCFCE7" : rating == "Khá" ? "#FEF9C3" : rating == "Đạt" ? "#FFF7ED" : "#FEE2E2",
                        RatingFg = rating == "Tốt" ? "#16A34A" : rating == "Khá" ? "#CA8A04" : rating == "Đạt" ? "#EA580C" : "#DC2626"
                    };
                }).ToList();
            }
            catch (Exception ex) { Log.Error("[ClassObs] LoadData err: {Err}", ex.Message); }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            string defaultTeacher = "";
            string defaultClass = "";
            string defaultSubject = "";
            string defaultObserver = StaffSession.CurrentUser?.FullName ?? "BGH";

            if (sender is Button btnSource && btnSource.Tag is int schedId && _db != null)
            {
                var sched = _db.ObservationSchedules.Find(schedId);
                if (sched != null)
                {
                    defaultTeacher = sched.TeacherName;
                    defaultClass = sched.ClassName;
                    defaultSubject = sched.Subject;
                    defaultObserver = sched.ObserverName;
                }
            }

            var dlg = new Window
            {
                Title = "Tạo phiếu dự giờ - Rubric CV 5555",
                Width = 480, Height = 720,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FontFamily = new FontFamily("Segoe UI"),
                ResizeMode = ResizeMode.NoResize
            };

            var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(Header("THÔNG TIN TIẾT DẠY"));
            sp.Children.Add(Lbl("GV được dự giờ:"));
            
            // Editable ComboBox for Teacher Auto-Suggest
            var txtTName = new ComboBox
            {
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(8, 6, 8, 6),
                IsEditable = true,
                DisplayMemberPath = "FullName",
                SelectedValuePath = "Id",
                Height = 35
            };
            try
            {
                txtTName.ItemsSource = _db?.StaffProfiles.OrderBy(s => s.FullName).Select(s => new { s.Id, s.FullName }).ToList();
                if (!string.IsNullOrEmpty(defaultTeacher))
                {
                    txtTName.Text = defaultTeacher;
                }
            }
            catch { }
            sp.Children.Add(txtTName);

            sp.Children.Add(Lbl("Lớp:"));
            var txtClass = Txt(defaultClass); sp.Children.Add(txtClass);

            sp.Children.Add(Lbl("Môn:"));
            var txtSubj = Txt(defaultSubject); sp.Children.Add(txtSubj);

            sp.Children.Add(Lbl("Người dự giờ:"));
            
            // Editable ComboBox for Observer Auto-Suggest
            var txtObserver = new ComboBox
            {
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(8, 6, 8, 6),
                IsEditable = true,
                DisplayMemberPath = "FullName",
                SelectedValuePath = "Id",
                Height = 35
            };
            try
            {
                txtObserver.ItemsSource = _db?.StaffProfiles.OrderBy(s => s.FullName).Select(s => new { s.Id, s.FullName }).ToList();
                txtObserver.Text = defaultObserver;
            }
            catch { }
            sp.Children.Add(txtObserver);

            sp.Children.Add(Header("RUBRIC ĐÁNH GIÁ (Thang 4 điểm)"));

            sp.Children.Add(Lbl("▶ Kế hoạch bài dạy (25%):"));
            var txtS1 = Txt("3"); sp.Children.Add(txtS1);

            sp.Children.Add(Lbl("▶ Tổ chức hoạt động dạy học (30%):"));
            var txtS2 = Txt("3"); sp.Children.Add(txtS2);

            sp.Children.Add(Lbl("▶ Hoạt động của học sinh (20%):"));
            var txtS3 = Txt("3"); sp.Children.Add(txtS3);

            sp.Children.Add(Lbl("▶ Hỗ trợ của giáo viên (15% - CV 5555):"));
            var txtS4 = Txt("3"); sp.Children.Add(txtS4);

            sp.Children.Add(Lbl("▶ Kiểm tra đánh giá (10% - CV 5555):"));
            var txtS5 = Txt("3"); sp.Children.Add(txtS5);

            sp.Children.Add(Header("NHẬN XÉT & ĐỀ XUẤT"));
            sp.Children.Add(Lbl("Nhận xét:"));
            var txtNote = new TextBox
            {
                FontSize = 13, Height = 60, TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true, Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(8, 6, 8, 6)
            };
            sp.Children.Add(txtNote);

            sp.Children.Add(Lbl("Đề xuất cải tiến:"));
            var txtRecommend = new TextBox
            {
                FontSize = 13, Height = 50, TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true, Margin = new Thickness(0, 0, 0, 14),
                Padding = new Thickness(8, 6, 8, 6)
            };
            sp.Children.Add(txtRecommend);

            // Auto-calc preview
            var txtPreview = new TextBlock
            {
                FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(txtPreview);

            void UpdatePreview(object? s2, EventArgs e2)
            {
                int.TryParse(txtS1.Text, out int v1);
                int.TryParse(txtS2.Text, out int v2);
                int.TryParse(txtS3.Text, out int v3);
                int.TryParse(txtS4.Text, out int v4);
                int.TryParse(txtS5.Text, out int v5);
                double t = 0.25 * v1 + 0.30 * v2 + 0.20 * v3 + 0.15 * v4 + 0.10 * v5;
                string r = t >= 3.5 ? "Tốt" : t >= 2.5 ? "Khá" : t >= 1.5 ? "Đạt" : "Chưa đạt";
                txtPreview.Text = $"📊 Tổng điểm: {t:F2}/4 | Xếp loại: {r}";
            }
            txtS1.TextChanged += (s2, e2) => UpdatePreview(s2, e2);
            txtS2.TextChanged += (s2, e2) => UpdatePreview(s2, e2);
            txtS3.TextChanged += (s2, e2) => UpdatePreview(s2, e2);
            txtS4.TextChanged += (s2, e2) => UpdatePreview(s2, e2);
            txtS5.TextChanged += (s2, e2) => UpdatePreview(s2, e2);
            UpdatePreview(null, EventArgs.Empty);

            var btn = new Button
            {
                Content = "💾 Lưu phiếu dự giờ",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                Foreground = Brushes.White, Padding = new Thickness(12, 8, 12, 8),
                FontSize = 14, FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0)
            };
            btn.Click += (_, __) =>
            {
                try
                {
                    if (!int.TryParse(txtS1.Text, out int lpScore)) { MessageBox.Show("Điểm KHBD phải là số nguyên."); return; }
                    if (lpScore < 1 || lpScore > 4) { MessageBox.Show("Điểm KHBD phải từ 1-4."); return; }

                    if (!int.TryParse(txtS2.Text, out int dlScore)) { MessageBox.Show("Điểm TCH phải là số nguyên."); return; }
                    if (dlScore < 1 || dlScore > 4) { MessageBox.Show("Điểm TCH phải từ 1-4."); return; }

                    if (!int.TryParse(txtS3.Text, out int seScore)) { MessageBox.Show("Điểm HHS phải là số nguyên."); return; }
                    if (seScore < 1 || seScore > 4) { MessageBox.Show("Điểm HHS phải từ 1-4."); return; }

                    if (!int.TryParse(txtS4.Text, out int tsScore)) { MessageBox.Show("Điểm HTGV phải là số nguyên."); return; }
                    if (tsScore < 1 || tsScore > 4) { MessageBox.Show("Điểm HTGV phải từ 1-4."); return; }

                    if (!int.TryParse(txtS5.Text, out int asScore)) { MessageBox.Show("Điểm KTG phải là số nguyên."); return; }
                    if (asScore < 1 || asScore > 4) { MessageBox.Show("Điểm KTG phải từ 1-4."); return; }

                    int s1 = lpScore;
                    int s2 = dlScore;
                    int s3 = seScore;
                    int s4 = tsScore;
                    int s5 = asScore;

                    double total = 0.25 * s1 + 0.30 * s2 + 0.20 * s3 + 0.15 * s4 + 0.10 * s5;
                    string rating = total >= 3.5 ? "Tốt" : total >= 2.5 ? "Khá" : total >= 1.5 ? "Đạt" : "Chưa đạt";

                    if (_db != null)
                    {
                        string teacherNameInput = txtTName.Text.Trim();
                        string observerNameInput = txtObserver.Text.Trim();
                        int teacherIdVal = 0;

                        // Resolve IDs from Auto-Suggest or fallback search
                        if (txtTName.SelectedValue is int tSelId && tSelId > 0)
                        {
                            teacherIdVal = tSelId;
                            var s = _db.StaffProfiles.Find(tSelId);
                            if (s != null) teacherNameInput = s.FullName;
                        }
                        else if (!string.IsNullOrEmpty(teacherNameInput))
                        {
                            var staff = _db.StaffProfiles.FirstOrDefault(s => s.FullName.ToLower() == teacherNameInput.ToLower());
                            if (staff != null)
                            {
                                teacherIdVal = staff.Id;
                                teacherNameInput = staff.FullName;
                            }
                        }

                        if (txtObserver.SelectedValue is int oSelId && oSelId > 0)
                        {
                            var s = _db.StaffProfiles.Find(oSelId);
                            if (s != null) observerNameInput = s.FullName;
                        }

                        _db.ClassObservations.Add(new ClassObservation
                        {
                            TeacherId = teacherIdVal,
                            TeacherName = teacherNameInput,
                            ClassName = txtClass.Text.Trim(),
                            Subject = txtSubj.Text.Trim(),
                            ObserverName = observerNameInput,
                            LessonPlanScore = s1,
                            DeliveryScore = s2,
                            StudentEngagementScore = s3,
                            TeacherSupportScore = s4,
                            AssessmentScore = s5,
                            TotalScore = total,
                            Rating = rating,
                            Notes = txtNote.Text.Trim(),
                            Recommendation = txtRecommend.Text.Trim(),
                            ObservedAt = DateTime.Now
                        });
                        _db.SaveChanges();
                    }
                    dlg.Close();
                    LoadData();
                }
                catch (Exception ex)
                {
                    Log.Error("[ClassObs] Save err: {Err}", ex.Message);
                    MessageBox.Show(ex.Message);
                }
            };
            sp.Children.Add(btn);
            sv.Content = sp;
            dlg.Content = sv;
            dlg.ShowDialog();
        }

        // -------------------------------------------
        //  TAB 2  Lịch dự giờ (WI-12)
        // -------------------------------------------

        private void LoadDepartments()
        {
            if (_db == null) return;
            try
            {
                var depts = _db.Departments.OrderBy(d => d.DepartmentName).ToList();
                var items = new System.Collections.Generic.List<object>();
                items.Add(new { Id = 0, DepartmentName = "-- Tất cả --" });
                foreach (var d in depts) items.Add(new { d.Id, d.DepartmentName });
                CbDeptFilter.DisplayMemberPath = "DepartmentName";
                CbDeptFilter.SelectedValuePath = "Id";
                CbDeptFilter.ItemsSource = items;
                CbDeptFilter.SelectedIndex = 0;
            }
            catch (Exception ex) { Log.Error("[ClassObs] LoadDepts err: {Err}", ex.Message); }
        }

        private void LoadSchedules()
        {
            if (_db == null) return;
            try
            {
                TxtCurrentMonth.Text = _currentMonth.ToString("MM/yyyy");

                int deptId = 0;
                if (CbDeptFilter.SelectedValue is int v) deptId = v;

                var query = _db.ObservationSchedules
                    .Where(s => s.ScheduledDate.Year == _currentMonth.Year && s.ScheduledDate.Month == _currentMonth.Month);
                if (deptId > 0) query = query.Where(s => s.DepartmentId == deptId);

                var list = query.OrderBy(s => s.ScheduledDate).ThenBy(s => s.Period).ToList();

                LvSchedule.ItemsSource = list.Select(s => new
                {
                    s.Id,
                    DayText = s.ScheduledDate.ToString("dd"),
                    MonthText = s.ScheduledDate.ToString("ddd"),
                    Title = $"GV: {s.TeacherName} Môn {s.Subject} Lớp {s.ClassName}",
                    Detail = $"Người dự: {s.ObserverName} | Tiết: {s.Period}",
                    StatusText = s.Status == "Completed" ? "✔ Đã hoàn thành" : s.Status == "Cancelled" ? "❌ Đã hủy" : "⏳ Chờ thực hiện",
                    StatusBg = s.Status == "Completed" ? "#DCFCE7" : s.Status == "Cancelled" ? "#FEE2E2" : "#FEF9C3",
                    StatusFg = s.Status == "Completed" ? "#16A34A" : s.Status == "Cancelled" ? "#DC2626" : "#CA8A04",
                    ActionText = s.Status == "Scheduled" ? "▶ Thực hiện dự giờ" : s.Status == "Completed" ? "👁 Xem phiếu" : "🔄 Khôi phục"
                }).ToList();
            }
            catch (Exception ex) { Log.Error("[ClassObs] LoadSchedules err: {Err}", ex.Message); }
        }

        private void CbDeptFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadSchedules();
        private void BtnPrevMonth_Click(object sender, RoutedEventArgs e) { _currentMonth = _currentMonth.AddMonths(-1); LoadSchedules(); }
        private void BtnNextMonth_Click(object sender, RoutedEventArgs e) { _currentMonth = _currentMonth.AddMonths(1); LoadSchedules(); }

        private void BtnAddSchedule_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Window
            {
                Title = "Tạo lịch dự giờ",
                Width = 440, Height = 540,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FontFamily = new FontFamily("Segoe UI"),
                ResizeMode = ResizeMode.NoResize
            };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(Header("THÔNG TIN LỊCH DỰ GIỜ"));

            sp.Children.Add(Lbl("Tổ Chuyên môn:"));
            var cbDept = new ComboBox { FontSize = 13, Margin = new Thickness(0, 0, 0, 10) };
            try
            {
                cbDept.DisplayMemberPath = "DepartmentName";
                cbDept.SelectedValuePath = "Id";
                cbDept.ItemsSource = _db?.Departments.OrderBy(d => d.DepartmentName).ToList();
                if (cbDept.Items.Count > 0) cbDept.SelectedIndex = 0;
            }
            catch { }
            sp.Children.Add(cbDept);

            sp.Children.Add(Lbl("GV được dự giờ:"));
            var txtTeacher = new ComboBox
            {
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(8, 6, 8, 6),
                IsEditable = true,
                DisplayMemberPath = "FullName",
                SelectedValuePath = "Id",
                Height = 35
            };
            try
            {
                txtTeacher.ItemsSource = _db?.StaffProfiles.OrderBy(s => s.FullName).Select(s => new { s.Id, s.FullName }).ToList();
            }
            catch { }
            sp.Children.Add(txtTeacher);

            sp.Children.Add(Lbl("Người dự giờ:"));
            var txtObserver = new ComboBox
            {
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(8, 6, 8, 6),
                IsEditable = true,
                DisplayMemberPath = "FullName",
                SelectedValuePath = "Id",
                Height = 35
            };
            try
            {
                var staffList = _db?.StaffProfiles.OrderBy(s => s.FullName).Select(s => new { s.Id, s.FullName }).ToList();
                txtObserver.ItemsSource = staffList;
                txtObserver.Text = StaffSession.CurrentUser?.FullName ?? "BGH";
            }
            catch { }
            sp.Children.Add(txtObserver);

            sp.Children.Add(Lbl("Môn:"));
            var txtSubj = Txt(""); sp.Children.Add(txtSubj);

            sp.Children.Add(Lbl("Lớp:"));
            var txtClass = Txt(""); sp.Children.Add(txtClass);

            sp.Children.Add(Lbl("Ngày dự giờ:"));
            var dp = new DatePicker { FontSize = 13, Margin = new Thickness(0, 0, 0, 10), SelectedDate = DateTime.Today };
            sp.Children.Add(dp);

            sp.Children.Add(Lbl("Tiết (1-10):"));
            var txtPeriod = Txt("1"); sp.Children.Add(txtPeriod);

            var btn = new Button
            {
                Content = "💾 Lưu lịch",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                Foreground = Brushes.White, Padding = new Thickness(12, 8, 12, 8),
                FontSize = 14, FontWeight = FontWeights.SemiBold, BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 10, 0, 0)
            };
            btn.Click += (_, __) =>
            {
                try
                {
                    int deptId = cbDept.SelectedValue is int did ? did : 0;
                    if (_db != null)
                    {
                        string teacherNameInput = txtTeacher.Text.Trim();
                        string observerNameInput = txtObserver.Text.Trim();
                        int tStaffId = 0;
                        int oStaffId = 0;

                        if (txtTeacher.SelectedValue is int tSelId && tSelId > 0)
                        {
                            tStaffId = tSelId;
                            var s = _db.StaffProfiles.Find(tSelId);
                            if (s != null) teacherNameInput = s.FullName;
                        }
                        else if (!string.IsNullOrEmpty(teacherNameInput))
                        {
                            var s = _db.StaffProfiles.FirstOrDefault(x => x.FullName.ToLower() == teacherNameInput.ToLower());
                            if (s != null)
                            {
                                tStaffId = s.Id;
                                teacherNameInput = s.FullName;
                            }
                        }

                        if (txtObserver.SelectedValue is int oSelId && oSelId > 0)
                        {
                            oStaffId = oSelId;
                            var s = _db.StaffProfiles.Find(oSelId);
                            if (s != null) observerNameInput = s.FullName;
                        }
                        else if (!string.IsNullOrEmpty(observerNameInput))
                        {
                            var s = _db.StaffProfiles.FirstOrDefault(x => x.FullName.ToLower() == observerNameInput.ToLower());
                            if (s != null)
                            {
                                oStaffId = s.Id;
                                observerNameInput = s.FullName;
                            }
                        }

                        _db.ObservationSchedules.Add(new ObservationSchedule
                        {
                            DepartmentId = deptId,
                            TeacherStaffId = tStaffId,
                            TeacherName = teacherNameInput,
                            ObserverStaffId = oStaffId,
                            ObserverName = observerNameInput,
                            Subject = txtSubj.Text.Trim(),
                            ClassName = txtClass.Text.Trim(),
                            ScheduledDate = dp.SelectedDate ?? DateTime.Today,
                            Period = int.TryParse(txtPeriod.Text, out int p) ? Math.Clamp(p, 1, 10) : 1,
                            Status = "Scheduled"
                        });
                        _db.SaveChanges();
                    }
                    dlg.Close();
                    LoadSchedules();
                }
                catch (Exception ex)
                {
                    Log.Error("[ClassObs] AddSchedule err: {Err}", ex.Message);
                    MessageBox.Show(ex.Message);
                }
            };
            sp.Children.Add(btn);
            dlg.Content = sp;
            dlg.ShowDialog();
        }

        private void BtnScheduleAction_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                if (sender is Button btnAction && btnAction.Tag is int schedId)
                {
                    var sched = _db.ObservationSchedules.Find(schedId);
                    if (sched == null) return;

                    if (sched.Status == "Scheduled")
                    {
                        // Mark completed & open observation form
                        sched.Status = "Completed";
                        _db.SaveChanges();
                        LoadSchedules();
                        BtnAdd_Click(sender, e); // open the Add observation dialog
                    }
                    else if (sched.Status == "Cancelled")
                    {
                        sched.Status = "Scheduled";
                        _db.SaveChanges();
                        LoadSchedules();
                    }
                }
            }
            catch (Exception ex) { Log.Error("[ClassObs] ScheduleAction err: {Err}", ex.Message); }
        }

        // Helpers
        private static TextBlock Lbl(string t) => new TextBlock
        {
            Text = t, Foreground = Brushes.Gray, FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4)
        };

        private static TextBox Txt(string def) => new TextBox
        {
            Text = def, FontSize = 13,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(8, 6, 8, 6)
        };

        private static TextBlock Header(string t) => new TextBlock
        {
            Text = t, FontSize = 14, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
            Margin = new Thickness(0, 6, 0, 10)
        };
    }
}
