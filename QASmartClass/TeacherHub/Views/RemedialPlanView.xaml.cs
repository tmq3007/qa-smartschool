using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.TeacherHub.Views
{
    public partial class RemedialPlanView : Page
    {
        private AppDbContext? _db;
        private RemedialService? _remedialService;
        private int _teacherId = 1;

        public RemedialPlanView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            _remedialService = new RemedialService(_db);

            if (StaffSession.CurrentUser != null)
            {
                _teacherId = StaffSession.CurrentUser.Id;
            }
            else
            {
                try { _teacherId = _db.TeacherProfiles.FirstOrDefault()?.Id ?? 1; } catch { }
            }

            LoadRosters();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
            _remedialService = null;
        }

        private void LoadRosters()
        {
            if (_db == null) return;
            try
            {
                var rosters = _db.ClassRosters.Where(r => r.IsActive).ToList();
                CbRoster.ItemsSource = rosters;
                CbRoster.DisplayMemberPath = "ClassName";
                CbRoster.SelectedValuePath = "Id";
                if (rosters.Any()) CbRoster.SelectedIndex = 0;
            }
            catch (Exception ex) { Log.Warning("RemedialPlan LoadRosters error: {Err}", ex.Message); }
        }

        private void CbRoster_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            if (_db == null || _remedialService == null) return;
            try
            {
                int rosterId = 1;
                if (CbRoster.SelectedValue is int rId)
                {
                    rosterId = rId;
                }

                // Load warnings based on selected class
                var warnings = _remedialService.GetEarlyWarningStudents(rosterId);
                LvWarnings.ItemsSource = warnings;

                // Load plans
                var plans = _remedialService.GetPlansByTeacher(_teacherId);
                var planVms = plans.Select(p => new
                {
                    p.Id,
                    StudentName = _db.Students.FirstOrDefault(s => s.Id == p.StudentId)?.FullName ?? $"Học sinh #{p.StudentId}",
                    p.Subject,
                    p.Goal,
                    DateRange = $"{p.StartDate:dd/MM} - {p.EndDate:dd/MM/yyyy}",
                    p.Progress,
                    ProgressText = p.Status == "Pending" ? "Chờ phê duyệt" : $"{p.Progress}% Cải thiện"
                }).ToList();

                LvPlans.ItemsSource = planVms;
            }
            catch (Exception ex) { Log.Warning("RemedialPlan Load error: {Err}", ex.Message); }
        }

        private void BtnCreatePlan_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null || _remedialService == null) return;

            var dlg = new Window { Title = "Tạo Kế hoạch Phụ đạo", Width = 400, Height = 480, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var sp = new StackPanel { Margin = new Thickness(20) };

            sp.Children.Add(new TextBlock { Text = "Học sinh:" });
            var cbStudents = new ComboBox { Margin = new Thickness(0, 4, 0, 10), DisplayMemberPath = "FullName", SelectedValuePath = "Id" };
            cbStudents.ItemsSource = _db.Students.ToList();
            if (cbStudents.Items.Count > 0) cbStudents.SelectedIndex = 0;
            sp.Children.Add(cbStudents);

            sp.Children.Add(new TextBlock { Text = "Môn học:" });
            var cboSubject = new ComboBox { Margin = new Thickness(0, 4, 0, 10) };
            
            // Default subjects
            var subjectList = new System.Collections.Generic.List<string> 
            { 
                "Toán học", "Ngữ văn", "Tiếng Anh", "Vật lý", "Hóa học", "Sinh học", "Lịch sử", "Địa lý", "Tin học", "Khác..." 
            };
            cboSubject.ItemsSource = subjectList;
            cboSubject.SelectedIndex = 0;
            sp.Children.Add(cboSubject);

            var txtSubject = new TextBox { Margin = new Thickness(0, 0, 0, 10), Visibility = Visibility.Collapsed };
            sp.Children.Add(txtSubject);

            cboSubject.SelectionChanged += (s, ev) =>
            {
                if (cboSubject.SelectedItem?.ToString() == "Khác...")
                {
                    txtSubject.Visibility = Visibility.Visible;
                }
                else
                {
                    txtSubject.Visibility = Visibility.Collapsed;
                }
            };

            sp.Children.Add(new TextBlock { Text = "Mục tiêu cần đạt:" });
            var txtGoal = new TextBox { Height = 60, AcceptsReturn = true, Margin = new Thickness(0, 4, 0, 10) }; sp.Children.Add(txtGoal);

            sp.Children.Add(new TextBlock { Text = "Thời gian thực hiện:" });
            var dpStart = new DatePicker { SelectedDate = DateTime.Today, Margin = new Thickness(0, 4, 0, 10) }; sp.Children.Add(dpStart);
            var dpEnd = new DatePicker { SelectedDate = DateTime.Today.AddDays(30), Margin = new Thickness(0, 4, 0, 20) }; sp.Children.Add(dpEnd);

            var btnSubmit = new Button { Content = "Lưu Kế hoạch", Background = Brushes.MediumSeaGreen, Foreground = Brushes.White, Padding = new Thickness(10), FontWeight = FontWeights.Bold };
            btnSubmit.Click += (_, __) =>
            {
                if (cbStudents.SelectedValue == null) return;
                int sId = (int)cbStudents.SelectedValue;
                string sName = ((Student)cbStudents.SelectedItem).FullName;

                string chosenSubject = cboSubject.SelectedItem?.ToString() ?? "Toán học";
                if (chosenSubject == "Khác...")
                {
                    chosenSubject = txtSubject.Text.Trim();
                }

                if (string.IsNullOrWhiteSpace(chosenSubject))
                {
                    MessageBox.Show("Vui lòng điền tên môn học.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                if (_remedialService != null)
                {
                    // Check configuration for requires approval
                    bool requiresApproval = false;
                    try
                    {
                        var setting = _db.SystemSettings.FirstOrDefault(s => s.Id == "RemedialPlanRequiresApproval");
                        if (setting != null && setting.Value.ToLower() == "true")
                        {
                            requiresApproval = true;
                        }
                    }
                    catch { }

                    _remedialService.CreatePlan(_teacherId, sId, sName, chosenSubject, txtGoal.Text.Trim(), dpStart.SelectedDate ?? DateTime.Today, dpEnd.SelectedDate ?? DateTime.Today);

                    // Update status if needed
                    var newPlan = _db.RemedialPlans
                        .Where(p => p.TeacherId == _teacherId && p.StudentId == sId && p.Subject == chosenSubject)
                        .OrderByDescending(p => p.Id)
                        .FirstOrDefault();

                    if (newPlan != null)
                    {
                        newPlan.Status = requiresApproval ? "Pending" : "Approved";
                        _db.SaveChanges();

                        if (requiresApproval)
                        {
                            MessageBox.Show("Kế hoạch phụ đạo đã được lưu dưới dạng 'Chờ duyệt' và gửi lên Ban Giám hiệu.", "Chờ duyệt kế hoạch", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                
                dlg.Close();
                LoadData();
            };
            sp.Children.Add(btnSubmit);
            dlg.Content = sp;
            dlg.ShowDialog();
        }

        private void LvPlans_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_remedialService == null || _db == null) return;

            if (LvPlans.SelectedItem != null)
            {
                dynamic selected = LvPlans.SelectedItem;
                int planId = selected.Id;

                // Check plan status
                var plan = _db.RemedialPlans.Find(planId);
                if (plan != null && plan.Status == "Pending")
                {
                    MessageBox.Show("Kế hoạch này đang chờ Ban Giám hiệu phê duyệt. Bạn chưa thể ghi chép buổi học hay cập nhật tiến độ.", "Kế hoạch chờ duyệt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    LvPlans.SelectedItem = null;
                    return;
                }

                var dlg = new Window { Title = "Ghi chép buổi Phụ đạo", Width = 400, Height = 350, WindowStartupLocation = WindowStartupLocation.CenterScreen };
                var sp = new StackPanel { Margin = new Thickness(20) };

                sp.Children.Add(new TextBlock { Text = "Nội dung đã dạy:" });
                var txtContent = new TextBox { Height = 60, AcceptsReturn = true, Margin = new Thickness(0, 4, 0, 10) }; sp.Children.Add(txtContent);

                sp.Children.Add(new TextBlock { Text = "Đánh giá tiến bộ:" });
                var txtImp = new TextBox { Height = 60, AcceptsReturn = true, Margin = new Thickness(0, 4, 0, 10) }; sp.Children.Add(txtImp);

                sp.Children.Add(new TextBlock { Text = "Mức độ hoàn thành mục tiêu (%):" });
                var txtProg = new TextBox { Text = selected.Progress.ToString(), Margin = new Thickness(0, 4, 0, 20) }; sp.Children.Add(txtProg);

                var btnSubmit = new Button { Content = "Lưu buổi học", Background = Brushes.MediumSeaGreen, Foreground = Brushes.White, Padding = new Thickness(10) };
                btnSubmit.Click += (_, __) =>
                {
                    int.TryParse(txtProg.Text, out int prog);
                    if (_remedialService != null)
                    {
                        _remedialService.AddSession(planId, txtContent.Text.Trim(), txtImp.Text.Trim(), prog);
                    }
                    dlg.Close();
                    LoadData();
                };
                sp.Children.Add(btnSubmit);
                dlg.Content = sp;
                dlg.ShowDialog();

                LvPlans.SelectedItem = null;
            }
        }
    }
}
