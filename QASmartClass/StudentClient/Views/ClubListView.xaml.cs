using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.StudentClient.Views
{
    public partial class ClubListView : Page
    {
        private readonly AppDbContext _db;
        private int _studentId = 1;

        public ClubListView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            Unloaded += (s, e) => { _db?.Dispose(); };
            Loaded += (_, __) => 
            {
                try
                {
                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
                    var (sId, sCode, sName) = identityService.GetCurrentStudent();
                    _studentId = sId;
                }
                catch { }
                LoadClubs();
            };
        }

        private void LoadClubs()
        {
            try
            {
                // Seed if empty
                if (!_db.Clubs.Any())
                {
                    _db.Clubs.AddRange(
                        new Club { Name = "CLB Tin học - Lập trình", Category = "Học thuật", TeacherId = "GV001", MaxMembers = 30, Description = "Nơi dành cho các bạn yêu thích lập trình và công nghệ." },
                        new Club { Name = "CLB Bóng Rổ", Category = "Thể thao", TeacherId = "GV002", MaxMembers = 50, Description = "Rèn luyện thể lực và kỹ năng chơi bóng rổ." },
                        new Club { Name = "CLB Âm nhạc", Category = "Nghệ thuật", TeacherId = "GV003", MaxMembers = 40, Description = "Học hát, chơi nhạc cụ và biểu diễn." }
                    );
                    _db.SaveChanges();
                }

                var clubs = _db.Clubs.ToList();
                DgClubs.ItemsSource = clubs;
            }
            catch (Exception ex) { Log.Warning("LoadClubs error: {Err}", ex.Message); }
        }

        private void DgClubs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgClubs.SelectedItem is Club club)
            {
                TxtClubName.Text = club.Name;
                TxtClubCategory.Text = club.Category;
                TxtClubDesc.Text = club.Description;
                TxtTeacher.Text = club.TeacherId;

                bool isMember = _db.ClubMembers.Any(m => m.ClubId == club.Id && m.StudentId == _studentId);
                if (isMember)
                {
                    BtnJoin.Visibility = Visibility.Collapsed;
                    TxtStatus.Visibility = Visibility.Visible;
                }
                else
                {
                    BtnJoin.Visibility = Visibility.Visible;
                    TxtStatus.Visibility = Visibility.Collapsed;
                }

                PanelDetail.Visibility = Visibility.Visible;
            }
            else
            {
                PanelDetail.Visibility = Visibility.Hidden;
            }
        }

        private void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            if (DgClubs.SelectedItem is Club club)
            {
                try
                {
                    var count = _db.ClubMembers.Count(m => m.ClubId == club.Id);
                    if (count >= club.MaxMembers)
                    {
                        MessageBox.Show("Câu lạc bộ đã đầy số lượng thành viên!", "Đăng ký thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(_db);
                    var (sId, sCode, sName) = identityService.GetCurrentStudent();
                    var student = _db.Students.Find(sId);

                    _db.ClubMembers.Add(new ClubMember
                    {
                        ClubId = club.Id,
                        StudentId = sId,
                        StudentName = student?.FullName ?? sName,
                        ClassName = student?.ClassName ?? "10A"
                    });
                    _db.SaveChanges();

                    BtnJoin.Visibility = Visibility.Collapsed;
                    TxtStatus.Visibility = Visibility.Visible;
                    MessageBox.Show($"Bạn đã đăng ký tham gia {club.Name} thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { Log.Warning("Join club error: {Err}", ex.Message); }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadClubs();
    }
}
