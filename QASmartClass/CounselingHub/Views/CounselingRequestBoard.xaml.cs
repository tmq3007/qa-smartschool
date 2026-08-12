using System;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.CounselingHub.Views
{
    public partial class CounselingRequestBoard : Page
    {
        private AppDbContext? _db;
        private CounselingService? _service;

        public CounselingRequestBoard()
        {
            InitializeComponent();
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
            await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            if (_service == null) return;
            try
            {
                var profiles = await _service.GetProfilesAsync();
                DgRequests.ItemsSource = profiles;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("LoadData error: {Err}", ex.Message);
            }
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new CounselingRequestWindow { Owner = Window.GetWindow(this) };
                if (win.ShowDialog() == true)
                {
                    _ = LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgRequests_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DgRequests.SelectedItem is CounselingProfile profile)
            {
                var detailPage = new CounselingSessionDetail(profile.Id);
                NavigationService?.Navigate(detailPage);
            }
        }
    }
}
