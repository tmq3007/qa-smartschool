using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartClass.Counseling.Views
{
    public partial class CounselingProfileView : Page
    {
        private readonly AppDbContext _db;
        private readonly QASmartClass.Services.AnonymousService _anonymousService;
        private int _currentProfileId = 0;
        private System.Collections.Generic.List<CounselingProfile> _allProfilesList = new();

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

        public CounselingProfileView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            Unloaded += (s, e) => { _db?.Dispose(); };
            _anonymousService = new QASmartClass.Services.AnonymousService(_db);
            Loaded += (_, __) => { 
                if (!CheckPermission())
                {
                    MessageBox.Show("Bạn không có quyền truy cập hồ sơ tư vấn chuyên sâu.", "Từ chối truy cập", MessageBoxButton.OK, MessageBoxImage.Stop);
                    this.Content = new Grid { Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 249, 255)) }; 
                    return;
                }
                LoadProfiles(); 
                LoadAnonymous(); 
            };
        }

        private void LoadProfiles()
        {
            try
            {
                _allProfilesList = _db.CounselingProfiles.OrderByDescending(p => p.CreatedAt).ToList();
                PopulateClassFilter();
                ApplyFilter();
            }
            catch (Exception ex) { Log.Warning("[Counseling] Load profiles err: {Err}", ex.Message); }
        }

        private void PopulateClassFilter()
        {
            if (CboClassFilter == null) return;
            var classes = _allProfilesList
                .Select(p => p.ClassName)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            CboClassFilter.SelectionChanged -= CboClassFilter_SelectionChanged;
            CboClassFilter.Items.Clear();
            CboClassFilter.Items.Add("Tất cả lớp");
            foreach (var cls in classes)
            {
                CboClassFilter.Items.Add(cls);
            }
            CboClassFilter.SelectedIndex = 0;
            CboClassFilter.SelectionChanged += CboClassFilter_SelectionChanged;
        }

        private void ApplyFilter()
        {
            if (TxtSearch == null || CboClassFilter == null || LvProfiles == null) return;
            string search = TxtSearch.Text.Trim().ToLowerInvariant();
            string selectedClass = CboClassFilter.SelectedItem?.ToString() ?? "Tất cả lớp";

            var filtered = _allProfilesList.Where(p =>
                (string.IsNullOrEmpty(search) || p.StudentName.ToLowerInvariant().Contains(search) || p.ClassName.ToLowerInvariant().Contains(search)) &&
                (selectedClass == "Tất cả lớp" || p.ClassName == selectedClass)
            ).ToList();

            LvProfiles.ItemsSource = filtered;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CboClassFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void LvProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LvProfiles.SelectedItem is CounselingProfile profile)
            {
                _currentProfileId = profile.Id;
                TxtStudentName.Text = $"{profile.StudentName} ({profile.ClassName})";
                TxtProblemType.Text = profile.ProblemType;
                TxtNotes.Text = string.IsNullOrEmpty(profile.Notes) ? "Chưa có ghi chép ban đầu." : profile.Notes;
                GridDetails.Visibility = Visibility.Visible;
                LoadSessions();
            }
            else
            {
                GridDetails.Visibility = Visibility.Hidden;
            }
        }

        private void LoadSessions()
        {
            try
            {
                var sessions = _db.CounselingSessions
                    .Where(s => s.ProfileId == _currentProfileId)
                    .OrderByDescending(s => s.SessionDate)
                    .ToList();

                LvSessions.ItemsSource = sessions.Select(s => new
                {
                    SessionDateStr = s.SessionDate.ToString("dd/MM/yyyy HH:mm"),
                    s.Content,
                    SolutionStr = s.Solution,
                    FollowUpStr = s.FollowUpDate.HasValue ? $"Hẹn gặp lại: {s.FollowUpDate.Value:dd/MM/yyyy}" : ""
                }).ToList();
            }
            catch (Exception ex) { Log.Warning("[Counseling] Load sessions err: {Err}", ex.Message); }
        }

        private async void BtnAddProfile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CounselingProfileDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    _db.CounselingProfiles.Add(dlg.ProfileResult);
                    await _db.SaveChangesAsync();
                    LoadProfiles();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi khi lưu hồ sơ tư vấn mới.");
                    MessageBox.Show("Không thể lưu hồ sơ tư vấn mới. Chi tiết lỗi: " + ex.Message, "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnAddSession_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProfileId == 0) return;

            var dlg = new CounselingSessionDialog { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var session = dlg.SessionResult;
                    session.ProfileId = _currentProfileId;
                    _db.CounselingSessions.Add(session);
                    await _db.SaveChangesAsync();
                    LoadSessions();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Lỗi khi lưu phiên tư vấn mới.");
                    MessageBox.Show("Không thể lưu phiên tư vấn. Chi tiết lỗi: " + ex.Message, "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadAnonymous()
        {
            try
            {
                var questions = _anonymousService.GetAllPendingQuestions();
                LvAnonymous.ItemsSource = questions;
            }
            catch (Exception ex) { Log.Warning("LoadAnonymous err: {Err}", ex.Message); }
        }

        private void BtnReplyAnonymous_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int qId)
            {
                var dlg = new Window { Title = "Phản hồi Góc Ẩn Danh", Width = 400, Height = 250, WindowStartupLocation = WindowStartupLocation.CenterScreen };
                var sp = new StackPanel { Margin = new Thickness(20) };
                
                sp.Children.Add(new TextBlock { Text = "Nội dung phản hồi:" });
                var txtAnswer = new TextBox { Height = 100, AcceptsReturn = true, Margin = new Thickness(0,4,0,15) }; sp.Children.Add(txtAnswer);

                var btnSend = new Button { Content = "Gửi Phản Hồi", Background = Brushes.MediumSeaGreen, Foreground = Brushes.White, Padding = new Thickness(10) };
                btnSend.Click += (_, __) =>
                {
                    if (!string.IsNullOrWhiteSpace(txtAnswer.Text))
                    {
                        _anonymousService.AnswerQuestion(qId, txtAnswer.Text.Trim());
                        dlg.Close();
                        LoadAnonymous();
                    }
                };
                sp.Children.Add(btnSend);
                dlg.Content = sp;
                dlg.ShowDialog();
            }
        }
    }
}

