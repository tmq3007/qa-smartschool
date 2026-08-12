using QASmartClass.Data;
using QASmartClass.Staff.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace QASmartClass.Staff.Views
{
    public partial class StaffLoginWindow : Window
    {
        public StaffLoginWindow()
        {
            InitializeComponent();
            txtUsername.TextChanged += (s, e) => txtError.Visibility = Visibility.Collapsed;
            txtPassword.PasswordChanged += (s, e) => txtError.Visibility = Visibility.Collapsed;
            Loaded += (s, e) => { 
                txtUsername.Focus(); 
                LoadTestAccounts();
                ApplyBrandingConfig();
            };

            // Nạp hình nền nhân viên nếu có
            var brush = LoadFileSafeImageBrush("login_teacher_bg.png");
            if (brush != null)
            {
                MainGrid.Background = brush;
            }
        }

        private System.Windows.Media.ImageBrush? LoadFileSafeImageBrush(string fileName)
        {
            try
            {
                string dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                string filePath = System.IO.Path.Combine(dir, fileName);
                
                if (System.IO.File.Exists(filePath) && new System.IO.FileInfo(filePath).Length > 0)
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    return new System.Windows.Media.ImageBrush(bitmap)
                    {
                        Stretch = System.Windows.Media.Stretch.UniformToFill,
                        AlignmentX = System.Windows.Media.AlignmentX.Center,
                        AlignmentY = System.Windows.Media.AlignmentY.Center
                    };
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Không thể tải hình nền {FileName}: {Error}", fileName, ex.Message);
            }
            return null;
        }

        private void ApplyBrandingConfig()
        {
            try
            {
                var config = QASmartTouch.Services.AppBrandingService.Instance.Config;
                if (LoginAppName != null)
                {
                    LoginAppName.Text = config.AppName;
                    try
                    {
                        var brandColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(config.BrandColor);
                        LoginAppName.Foreground = new System.Windows.Media.SolidColorBrush(brandColor);
                        
                        if (LoginIcon != null)
                            LoginIcon.Fill = new System.Windows.Media.SolidColorBrush(brandColor);
                    }
                    catch { /* Keep default color if conversion fails */ }
                }

                if (txtWelcome != null)
                    txtWelcome.Text = $"Chào mừng đến với {config.AppName}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login Branding] Error: {ex.Message}");
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string code = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(pass))
            {
                ShowError("Vui lòng nhập Mã CB/GV và Mật khẩu.");
                return;
            }

            using var db = new AppDbContext();
            var codeLower = code.ToLower();
            var user = db.TeacherProfiles.FirstOrDefault(t => t.TeacherCode.ToUpper() == code.Trim().ToUpper());
            bool isValid = false;
            if (user != null)
            {
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    isValid = QASmartTouch.Services.AuthenticationService.VerifyPassword(pass, user.PasswordHash);
                }
            }

            if (isValid && user != null)
            {
                // Save last selected test user if in test mode
                if (QASmartTouch.Services.AppSettings.RunningMode == "Test" && cboTestAccounts.SelectedItem is TeacherProfile selectedStaff)
                {
                    QASmartTouch.Services.AppSettings.LastTestUserStaff = selectedStaff.TeacherCode;
                    QASmartTouch.Services.AppSettings.Save();
                }

                StaffSession.Login(user);
                DialogResult = true;
                Close();
            }
            else
            {
                ShowError("Sai tài khoản hoặc mật khẩu.");
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void LoadTestAccounts()
        {
            if (QASmartTouch.Services.AppSettings.RunningMode != "Test")
            {
                pnlTestAccounts.Visibility = Visibility.Collapsed;
                return;
            }

            pnlTestAccounts.Visibility = Visibility.Visible;
            System.Collections.Generic.List<TeacherProfile> staffList = new System.Collections.Generic.List<TeacherProfile>();

            try
            {
                using (var db = new AppDbContext())
                {
                    staffList = db.TeacherProfiles
                        .Where(t => t.Role != "GV" && t.Role != "Admin")
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading test staff: {Err}", ex.Message);
            }

            // Fallback
            if (staffList == null || staffList.Count == 0)
            {
                staffList = new System.Collections.Generic.List<TeacherProfile>
                {
                    new TeacherProfile { TeacherCode = "HT001", FullName = "Nguyễn Văn Hùng", Role = "HieuTruong" },
                    new TeacherProfile { TeacherCode = "HP001", FullName = "Trần Thị Mai", Role = "HieuPho" },
                    new TeacherProfile { TeacherCode = "BV001", FullName = "Trần Văn Bảo", Role = "BaoVe" }
                };
            }

            cboTestAccounts.ItemsSource = staffList;
            cboTestAccounts.DisplayMemberPath = "FullName";

            // Select last test user
            string lastUser = QASmartTouch.Services.AppSettings.LastTestUserStaff;
            if (!string.IsNullOrEmpty(lastUser))
            {
                var match = staffList.Find(t => t.TeacherCode == lastUser);
                if (match != null)
                {
                    cboTestAccounts.SelectedItem = match;
                }
                else
                {
                    cboTestAccounts.SelectedIndex = 0;
                }
            }
            else
            {
                cboTestAccounts.SelectedIndex = 0;
            }
        }

        private void cboTestAccounts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboTestAccounts.SelectedItem is TeacherProfile selected)
            {
                txtUsername.Text = selected.TeacherCode;
                txtPassword.Password = GetTestPassword(selected.TeacherCode, selected.Role);
            }
        }

        private string GetTestPassword(string teacherCode, string role)
        {
            if (teacherCode == "ADMIN") return "admin123";
            if (teacherCode == "HT001") return "ht2026";
            if (teacherCode == "HP001") return "hp2026";
            if (teacherCode == "GV001" || role == "GV") return "gv2026";
            if (teacherCode == "BV001" || role == "BaoVe") return "bv2026";
            if (teacherCode == "YT001" || role == "YTe") return "yt2026";
            if (teacherCode == "LC001" || role == "LaoCong") return "lc2026";
            if (teacherCode == "BP001" || role == "Bep") return "bp2026";
            if (teacherCode == "KT001" || role == "Ketoan") return "kt2026";
            if (teacherCode == "TV001" || role == "Counselor") return "tv2026";
            if (teacherCode == "TT001" || role == "Librarian") return "tt2026";
            return "gv2026"; // Default
        }

        private void btnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
