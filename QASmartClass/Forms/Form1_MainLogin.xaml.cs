using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Models;
using QASmartTouch.Services;

namespace QASmartTouch.Forms
{
    public partial class Form1_MainLogin : Window
    {
        private readonly QASmartClass.Shared.AppMode _targetMode;

        public Form1_MainLogin(QASmartClass.Shared.AppMode targetMode = QASmartClass.Shared.AppMode.SmartScreen)
        {
            _targetMode = targetMode;
            InitializeComponent();
            ApplyBrandingConfig();
            LoadSavedCredentials();
            LoadTestAccounts();

            // Nạp hình nền giáo viên nếu có
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
                var config = AppBrandingService.Instance.Config;
                string appName = config.AppName;
                string brandColorStr = config.BrandColor;
                string description = "Nền tảng giảng dạy tương tác thông minh";
                string iconPathData = "M12,3L1,9L12,15L21,10.09V17H23V9M5,13.18V17.18L12,21L19,17.18V13.18L12,17L5,13.18Z"; // GraduationCap

                // Override based on target mode
                if (_targetMode == QASmartClass.Shared.AppMode.SmartScreen)
                {
                    appName = "QA SmartTouch";
                    brandColorStr = "#2563EB"; // Blue
                    description = "Giải pháp bảng tương tác thông minh";
                    iconPathData = "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.06,6.18L3,17.25Z"; // Pencil/Touch
                }
                else if (_targetMode == QASmartClass.Shared.AppMode.SmartClass)
                {
                    appName = "QA SmartClass";
                    brandColorStr = "#059669"; // Green
                    description = "Nền tảng giảng dạy tương tác thông minh";
                    iconPathData = "M12,3L1,9L12,15L21,10.09V17H23V9M5,13.18V17.18L12,21L19,17.18V13.18L12,17L5,13.18Z"; // GraduationCap
                }

                this.Title = $"{appName} - Đăng nhập";

                // Apply app name
                if (LoginAppName != null)
                {
                    LoginAppName.Text = appName;
                    try
                    {
                        var brandColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(brandColorStr);
                        LoginAppName.Foreground = new System.Windows.Media.SolidColorBrush(brandColor);

                        // Apply same color and data to icon
                        if (LoginIcon != null)
                        {
                            LoginIcon.Fill = new System.Windows.Media.SolidColorBrush(brandColor);
                            LoginIcon.Data = System.Windows.Media.Geometry.Parse(iconPathData);
                        }
                    }
                    catch { /* Keep default color if conversion fails */ }
                }

                // Apply welcome text
                if (txtWelcome != null)
                    txtWelcome.Text = $"Chào mừng đến với {appName}";

                // Apply description
                if (txtDescription != null)
                    txtDescription.Text = description;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login Branding] Error: {ex.Message}");
            }
        }

        private void LoadSavedCredentials()
        {
            var savedUser = SettingsManager.Instance.SavedUser;
            if (savedUser != null && savedUser.RememberCredentials)
            {
                txtUserID.Text = savedUser.UserId;
                txtPassword.Password = savedUser.Password;
                txtLicenseKey.Text = savedUser.LicenseKey;
                chkRememberCredentials.IsChecked = true;

                // Update license status
                UpdateLicenseStatus(savedUser.IsTrialAccount ? LicenseStatus.Trial : LicenseStatus.Valid);
            }
        }



        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Clear error message when user types
            HideError();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string userId = txtUserID.Text.Trim();
            string password = txtPassword.Password;
            string licenseKey = txtLicenseKey.Text.Trim();

            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId) || 
                string.IsNullOrWhiteSpace(password) || 
                string.IsNullOrWhiteSpace(licenseKey))
            {
                ShowError("Vui lòng nhập đầy đủ thông tin đăng nhập!");
                return;
            }

            // Show loading state
            SetLoadingState(true);
            HideError();

            try
            {
                // Authenticate
                var (success, message) = await AuthenticationService.Instance
                    .AuthenticateAsync(userId, password, licenseKey);

                if (!success)
                {
                    ShowError(message);
                    SetLoadingState(false);
                    return;
                }

                // Check license
                bool isOnline = CheckInternetConnection();
                var licenseInfo = await AuthenticationService.Instance
                    .CheckLicenseAsync(licenseKey, isOnline);

                // Save credentials if remember is checked
                if (chkRememberCredentials.IsChecked == true)
                {
                    var userInfo = new UserInfo
                    {
                        UserId = userId,
                        Password = password,
                        LicenseKey = licenseKey,
                        RememberCredentials = true,
                        IsTrialAccount = licenseKey == "1AAAAAAAAAAAAAA1",
                        LastLogin = DateTime.Now
                    };
                    SettingsManager.Instance.UpdateUserInfo(userInfo);
                }

                // Save last selected test user if in test mode
                if (AppSettings.RunningMode == "Test" && cboTestAccounts.SelectedItem is QASmartClass.Data.TeacherProfile selectedTeacher)
                {
                    AppSettings.LastTestUserTeacher = selectedTeacher.TeacherCode;
                    AppSettings.Save();
                }

                // Success - Navigate to Main Dashboard
                // [QC_4.2_WHITE_FLASH_FIX] Giữ trạng thái loading trong khi khởi tạo Dashboard để giao diện liền mạch
                SetLoadingState(true);
                
                // Only show license warning if expiring within 30 days
                if (licenseInfo.ExpiresAt > DateTime.MinValue)
                {
                    var daysRemaining = (licenseInfo.ExpiresAt - DateTime.UtcNow).Days;
                    if (daysRemaining <= 30 && daysRemaining >= 0)
                    {
                        MessageBox.Show(
                            $"⚠️ Cảnh báo: License của bạn sắp hết hạn!\n\n" +
                            $"Số ngày còn lại: {daysRemaining} ngày\n" +
                            $"Hạn sử dụng: {licenseInfo.ExpiresAt:dd/MM/yyyy}\n\n" +
                            $"Vui lòng gia hạn để tiếp tục sử dụng dịch vụ.",
                            "Cảnh báo License",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else if (daysRemaining < 0)
                    {
                        MessageBox.Show(
                            $"❌ License của bạn đã hết hạn!\n\n" +
                            $"Hạn sử dụng: {licenseInfo.ExpiresAt:dd/MM/yyyy}\n\n" +
                            $"Vui lòng liên hệ để gia hạn License.",
                            "License Hết hạn",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        SetLoadingState(false);
                        return;
                    }
                }

                // Open appropriate view based on mode
                var app = (App)Application.Current;
                app.ModeService.SwitchTo(_targetMode, force: true);

                // [QC_4.2_WHITE_FLASH_FIX] Seamless Handover: Đợi Dashboard render xong frame đầu tiên rồi mới đóng cửa sổ đăng nhập
                Window? targetWindow = _targetMode == QASmartClass.Shared.AppMode.SmartClass
                    ? (Window?)app._classroomShell
                    : (Window?)app._whiteboardShell;

                if (targetWindow != null && !targetWindow.IsLoaded)
                {
                    bool isClosed = false;
                    void SafeClose()
                    {
                        if (isClosed) return;
                        isClosed = true;
                        Dispatcher.InvokeAsync(() =>
                        {
                            try { this.Close(); } catch { }
                        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    }

                    void OnTargetRendered(object? s, EventArgs e)
                    {
                        targetWindow.ContentRendered -= OnTargetRendered;
                        targetWindow.Loaded -= OnTargetRendered;
                        SafeClose();
                    }

                    targetWindow.ContentRendered += OnTargetRendered;
                    targetWindow.Loaded += OnTargetRendered;

                    // Fallback timeout sau 1.5s nếu targetWindow không bắn event
                    var fallbackTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(1500)
                    };
                    fallbackTimer.Tick += (s, e) =>
                    {
                        fallbackTimer.Stop();
                        targetWindow.ContentRendered -= OnTargetRendered;
                        targetWindow.Loaded -= OnTargetRendered;
                        SafeClose();
                    };
                    fallbackTimer.Start();
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi đăng nhập: {ex.Message}");
                SetLoadingState(false);
            }
        }

        private void btnForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Tính năng 'Quên mật khẩu' sẽ được triển khai trong phiên bản tiếp theo.\n\n" +
                "Để được hỗ trợ, vui lòng liên hệ:\n" +
                "Email: support@iprosmart.edu.vn\n" +
                "Hotline: 1900-xxxx",
                "Quên mật khẩu",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void btnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            ExitApplication();
        }

        private void ExitApplication()
        {
            // Kiểm tra biến master để quyết định có hiển thị xác nhận hay không
            if (AppSettings.ShouldShowExitConfirmation)
            {
                var result = MessageBox.Show(
                    "Bạn có chắc chắn muốn thoát ứng dụng?",
                    "Xác nhận thoát",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {
                // Thoát trực tiếp không cần xác nhận
                Application.Current.Shutdown();
            }
        }

        private void ShowError(string message)
        {
            lblErrorMessage.Text = message;
            lblErrorMessage.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            lblErrorMessage.Visibility = Visibility.Collapsed;
            lblErrorMessage.Text = string.Empty;
        }

        private void SetLoadingState(bool isLoading)
        {
            btnLogin.IsEnabled = !isLoading;
            btnForgotPassword.IsEnabled = !isLoading;
            txtUserID.IsEnabled = !isLoading;
            txtPassword.IsEnabled = !isLoading;
            txtLicenseKey.IsEnabled = !isLoading;
            chkRememberCredentials.IsEnabled = !isLoading;
            progressLogin.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateLicenseStatus(LicenseStatus status)
        {
            lblLicenseStatus.Text = status switch
            {
                LicenseStatus.Valid or LicenseStatus.Active => "Đang hoạt động",
                LicenseStatus.Expired or LicenseStatus.FullyExpired => "Hết hạn",
                LicenseStatus.GracePeriod => "Ân hạn",
                LicenseStatus.ExpiringSoon => "Sắp hết hạn",
                LicenseStatus.NotActivated => "Chưa kích hoạt",
                LicenseStatus.Trial or LicenseStatus.FreeTrial => "Thử nghiệm (Trial)",
                LicenseStatus.Offline => "Ngoại tuyến (Offline)",
                _ => "Không xác định"
            };

            // Update color based on status
            var style = status switch
            {
                LicenseStatus.Valid or LicenseStatus.Active => "StatusActive",
                LicenseStatus.Expired or LicenseStatus.FullyExpired => "StatusExpired",
                LicenseStatus.ExpiringSoon => "StatusWarning",
                LicenseStatus.Trial or LicenseStatus.FreeTrial => "StatusTrial",
                _ => "StatusActive"
            };

            try
            {
                lblLicenseStatus.Style = (Style)FindResource(style);
            }
            catch
            {
                // Fallback nếu thiếu resource Style trong XAML
                lblLicenseStatus.Foreground = status switch
                {
                    LicenseStatus.Valid or LicenseStatus.Active => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50)),
                    LicenseStatus.Expired or LicenseStatus.FullyExpired => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(198, 40, 40)),
                    LicenseStatus.ExpiringSoon => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 81, 0)),
                    LicenseStatus.Trial or LicenseStatus.FreeTrial => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 101, 192)),
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(117, 117, 117))
                };
            }
        }

        private bool CheckInternetConnection()
        {
            try
            {
                using var client = new System.Net.NetworkInformation.Ping();
                var reply = client.Send("8.8.8.8", 1000);
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private void LoadTestAccounts()
        {
            if (AppSettings.RunningMode != "Test")
            {
                pnlTestAccounts.Visibility = Visibility.Collapsed;
                if (txtUserIDHint != null) txtUserIDHint.Visibility = Visibility.Collapsed;
                if (txtPasswordHint != null) txtPasswordHint.Visibility = Visibility.Collapsed;
                if (txtLicenseHint != null) txtLicenseHint.Visibility = Visibility.Collapsed;
                return;
            }

            pnlTestAccounts.Visibility = Visibility.Visible;
            if (txtUserIDHint != null) txtUserIDHint.Visibility = Visibility.Visible;
            if (txtPasswordHint != null) txtPasswordHint.Visibility = Visibility.Visible;
            if (txtLicenseHint != null) txtLicenseHint.Visibility = Visibility.Visible;
            System.Collections.Generic.List<QASmartClass.Data.TeacherProfile> teachers = new();

            try
            {
                using (var db = new QASmartClass.Data.AppDbContext())
                {
                    teachers = db.TeacherProfiles
                        .Where(t => t.Role == "GV" || t.Role == "Admin" || t.Role == "HieuTruong" || t.Role == "HieuPho")
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading test teachers: {Err}", ex.Message);
            }

            // Fallback if DB is empty or fails
            if (teachers == null || teachers.Count == 0)
            {
                teachers = new System.Collections.Generic.List<QASmartClass.Data.TeacherProfile>
                {
                    new QASmartClass.Data.TeacherProfile { TeacherCode = "GiaoVien01", FullName = "Giáo Viên Demo", Role = "GV" },
                    new QASmartClass.Data.TeacherProfile { TeacherCode = "ADMIN", FullName = "Quản trị hệ thống", Role = "Admin" }
                };
            }

            cboTestAccounts.ItemsSource = teachers;
            cboTestAccounts.DisplayMemberPath = "FullName";

            // Select last test user
            string lastUser = AppSettings.LastTestUserTeacher;
            if (!string.IsNullOrEmpty(lastUser))
            {
                var match = teachers.Find(t => t.TeacherCode == lastUser);
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

        private void cboTestAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboTestAccounts.SelectedItem is QASmartClass.Data.TeacherProfile selected)
            {
                txtUserID.Text = selected.TeacherCode;
                txtLicenseKey.Text = "1AAAAAAAAAAAAAA1";
                
                string password = GetTestPassword(selected.TeacherCode, selected.Role);
                txtPassword.Password = password;

                if (txtPasswordHint != null)
                {
                    txtPasswordHint.Text = $"(Thử nghiệm: {password})";
                }
            }
        }

        private string GetTestPassword(string teacherCode, string role)
        {
            if (teacherCode == "GiaoVien01") return "PassGiaoVien01";
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
            return "gv2026"; // Default
        }
    }
}
