using System;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.CounselingHub.Views
{
    public partial class CounselingSessionDetail : Page
    {
        private AppDbContext? _db;
        private CounselingService? _service;
        private int _profileId;

        public CounselingSessionDetail(int profileId)
        {
            InitializeComponent();
            _profileId = profileId;
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
            _service = null;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            _service = new CounselingService(_db);
            await LoadProfileData();
        }

        private async System.Threading.Tasks.Task LoadProfileData()
        {
            if (_service == null) return;
            try
            {
                var profile = await _service.GetProfileByIdAsync(_profileId);
                if (profile != null)
                {
                    TxtStudentName.Text = profile.StudentName;
                    TxtClassName.Text = profile.ClassName;
                    TxtProblemType.Text = profile.ProblemType;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("LoadProfileData error: {Err}", ex.Message);
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_service == null) return;
            string notes = TxtNotes.Text.Trim();
            string solution = TxtSolution.Text.Trim();

            if (string.IsNullOrWhiteSpace(notes))
            {
                MessageBox.Show("Vui lòng nhập nội dung ghi chép chuyên môn!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(solution))
            {
                MessageBox.Show("Vui lòng nhập kế hoạch hành động / giải pháp!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var session = new CounselingSession
                {
                    ProfileId = _profileId,
                    SessionDate = DateTime.Now,
                    Content = notes,
                    Solution = solution
                };
                
                await _service.CreateSessionAsync(session);
                await _service.UpdateProfileStatusAsync(_profileId, StatusConstants.CounselingStatus.InProgress);
                MessageBox.Show("Đã lưu hồ sơ phiên tư vấn thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                if (NavigationService != null && NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                TxtNotes.Clear();
                TxtSolution.Clear();
            }
        }
    }
}
