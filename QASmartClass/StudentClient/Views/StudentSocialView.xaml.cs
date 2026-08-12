using System;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.StudentClient.Services;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentSocialView : Page
    {
        private AppDbContext? _db;
        private SocialService? _socialService;
        private string _currentUser = "Học sinh";

        public StudentSocialView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            _socialService = new SocialService(_db);

            try
            {
                var identityService = new StudentIdentityService(_db);
                var (_, _, name) = identityService.GetCurrentStudent();
                if (!string.IsNullOrEmpty(name))
                {
                    _currentUser = name;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("StudentSocialView: Cannot resolve student identity: {Err}", ex.Message);
            }

            LoadFeed();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Page_Loaded;
            Unloaded -= Page_Unloaded;
            _db?.Dispose();
            _db = null;
            _socialService = null;
        }

        private void LoadFeed()
        {
            if (_socialService == null) return;
            LvFeed.ItemsSource = _socialService.GetFeed();
        }

        private void BtnPost_Click(object sender, RoutedEventArgs e)
        {
            if (_socialService == null) return;
            if (string.IsNullOrWhiteSpace(TxtPostContent.Text)) return;

            var post = new SocialPost
            {
                AuthorName = _currentUser,
                Content = TxtPostContent.Text
            };

            var result = _socialService.CreatePost(post);
            
            if (result.Status != "Published")
            {
                MessageBox.Show("Bài viết của em chứa từ khóa nhạy cảm và đã bị chặn bởi hệ thống kiểm duyệt AI.", "Vi phạm tiêu chuẩn", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            TxtPostContent.Text = "";
            LoadFeed();
        }

        private void BtnLike_Click(object sender, RoutedEventArgs e)
        {
            if (_socialService == null) return;
            if (sender is Button btn && btn.Tag is int postId)
            {
                _socialService.LikePost(postId);
                LoadFeed(); // Refresh list to update like count
            }
        }
    }
}
