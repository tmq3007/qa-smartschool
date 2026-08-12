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
    public partial class SkknView : Page
    {
        private AppDbContext _db;
        private SkknService _skknService;
        private string _teacherName = "Gio vin Demo"; // Default for demo

        public SkknView()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                _db = new AppDbContext();
                _skknService = new SkknService(_db);
                try 
                { 
                    _teacherName = StaffSession.CurrentUser?.FullName ?? _db.TeacherProfiles.FirstOrDefault()?.FullName ?? "Gio vin Demo"; 
                } 
                catch { }
                LoadData();
            };

            Unloaded += (s, e) =>
            {
                _db?.Dispose();
                _db = null;
                _skknService = null;
            };
        }

        private void LoadData()
        {
            try
            {
                string teacherCode = StaffSession.CurrentUser?.TeacherCode ?? "GV001";
                var list = _skknService.GetSkknsByAuthorCode(teacherCode);
                LvSkkns.ItemsSource = list.Select(s => new
                {
                    s.Title,
                    s.Subject,
                    s.Level,
                    s.SubmittedAt,
                    s.ReviewNote,
                    HasNote = string.IsNullOrEmpty(s.ReviewNote) ? Visibility.Collapsed : Visibility.Visible,
                    Status = s.Status,
                    StatusColor = s.Status switch
                    {
                        "Draft" => new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                        "Submitted" => new SolidColorBrush(Color.FromRgb(59, 130, 246)), // Blue 500
                        "UnderReview" => new SolidColorBrush(Color.FromRgb(245, 158, 11)), // Amber 500
                        "Approved" => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Emerald 500
                        "Rejected" => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // Red 500
                        _ => Brushes.Gray
                    }
                }).ToList();
            }
            catch (Exception ex) { Log.Warning("Skkn Load error: {Err}", ex.Message); }
        }

        private void BtnNewSkkn_Click(object sender, RoutedEventArgs e)
        {
            if (StaffSession.CurrentUser == null)
            {
                MessageBox.Show("Phiên làm việc đã hết hạn hoặc bạn chưa đăng nhập. Vui lòng đăng nhập lại để nộp Sáng kiến kinh nghiệm.", "Yêu cầu đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new Window { Title = "Nộp SKKN", Width = 450, Height = 400, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var sp = new StackPanel { Margin = new Thickness(20) };
            
            sp.Children.Add(new TextBlock { Text = "Tên đề tài SKKN:" });
            var txtTitle = new TextBox { Margin = new Thickness(0,4,0,10) }; sp.Children.Add(txtTitle);
            
            sp.Children.Add(new TextBlock { Text = "Môn học/Lĩnh vực:" });
            var txtSubject = new ComboBox { Margin = new Thickness(0,4,0,10) };
            txtSubject.Items.Add("Toán học"); txtSubject.Items.Add("Ngữ văn"); txtSubject.Items.Add("Ngoại ngữ"); txtSubject.Items.Add("Công tác CN"); txtSubject.Items.Add("Khác");
            txtSubject.SelectedIndex = 0;
            sp.Children.Add(txtSubject);

            sp.Children.Add(new TextBlock { Text = "Cấp độ đánh giá:" });
            var txtLevel = new ComboBox { Margin = new Thickness(0,4,0,15) };
            txtLevel.Items.Add("Cấp Trường"); txtLevel.Items.Add("Cấp Quận/Huyện"); txtLevel.Items.Add("Cấp Tỉnh/Thành phố");
            txtLevel.SelectedIndex = 0;
            sp.Children.Add(txtLevel);

            sp.Children.Add(new TextBlock { Text = "File đính kèm (PDF/Word):" });
            var dock = new DockPanel { Margin = new Thickness(0,4,0,20) };
            var txtFile = new TextBox { IsReadOnly = true }; 
            var btnBrowse = new Button { Content = "Chọn file", Width = 80, Margin = new Thickness(5,0,0,0) };
            btnBrowse.Click += (_, __) =>
            {
                var fd = new Microsoft.Win32.OpenFileDialog { Filter = "Tài liệu (*.pdf;*.docx)|*.pdf;*.docx" };
                if (fd.ShowDialog() == true) txtFile.Text = fd.FileName;
            };
            dock.Children.Add(btnBrowse);
            DockPanel.SetDock(btnBrowse, Dock.Right);
            dock.Children.Add(txtFile);
            sp.Children.Add(dock);

            var btnSubmit = new Button { Content = "Gửi SKKN", Background = Brushes.MediumSeaGreen, Foreground = Brushes.White, Padding = new Thickness(10), FontWeight = FontWeights.Bold };
            btnSubmit.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtFile.Text))
                {
                    MessageBox.Show("Vui lòng điền đầy đủ thông tin và chọn file.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                string teacherCode = StaffSession.CurrentUser?.TeacherCode ?? "GV001";
                _skknService.SubmitSkkn(StaffSession.CurrentUser.FullName, txtTitle.Text.Trim(), txtSubject.SelectedItem.ToString() ?? "Khác", txtLevel.SelectedItem.ToString() ?? "Cấp Trường", txtFile.Text, teacherCode);
                MessageBox.Show("Nộp SKKN thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                dlg.Close();
                LoadData();
            };
            sp.Children.Add(btnSubmit);
            dlg.Content = sp;
            dlg.ShowDialog();
        }
    }
}

