using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Shared;
using QASmartTouch.Services;

namespace QASmartTouch.Forms
{
    public partial class Form0_LoginSelection : Window
    {
        private bool _isClosing = false;
        
        // Cờ cho biết màn hình đang ở trạng thái tải ngầm lần đầu
        public bool IsInitialLoading { get; set; } = false;

        public Form0_LoginSelection() : this(false) { }

        public Form0_LoginSelection(bool isInitialLoad)
        {
            IsInitialLoading = isInitialLoad;
            InitializeComponent();
            
            // Cho phép di chuyển cửa sổ bằng cách kéo thả vùng trống
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };

            this.Closing += (s, e) => _isClosing = true;

            // Đăng ký sự kiện phím tắt và Loaded
            this.PreviewKeyDown += Form0_LoginSelection_PreviewKeyDown;
            this.Loaded += Form0_LoginSelection_Loaded;

            // Nạp cấu hình chế độ chạy lúc khởi động
            UpdateModeUI();

            // Nạp hình nền mặc định nếu có
            var brush = LoadFileSafeImageBrush("login_default_bg.png");
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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnReleaseMode_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.RunningMode = "Release";
            AppSettings.Save();
            UpdateModeUI();
        }

        private void BtnTestMode_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.RunningMode = "Test";
            AppSettings.Save();
            UpdateModeUI();
        }

        private void Form0_LoginSelection_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Phím tắt bí mật Ctrl + Shift + Alt + T để bật/tắt mode switcher
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt) && e.Key == Key.T)
            {
                modeSwitcherBorder.Visibility = modeSwitcherBorder.Visibility == Visibility.Visible 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
                e.Handled = true;
            }
        }

        private void Form0_LoginSelection_Loaded(object sender, RoutedEventArgs e)
        {
            CheckAndBypassIfSingleProduct();
        }

        private void UpdateModeUI()
        {
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            if (isTest)
            {
                btnReleaseMode.Background = System.Windows.Media.Brushes.Transparent;
                btnReleaseMode.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));

                btnTestMode.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                btnTestMode.Foreground = System.Windows.Media.Brushes.White;
            }
            else
            {
                btnReleaseMode.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                btnReleaseMode.Foreground = System.Windows.Media.Brushes.White;

                btnTestMode.Background = System.Windows.Media.Brushes.Transparent;
                btnTestMode.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            }

            ApplyLicenseFeaturesUI();
        }

        private void ApplyLicenseFeaturesUI()
        {
            if (IsInitialLoading)
            {
                // Trạng thái Skeleton Loading lúc vừa bật app
                cardSmartTouch.Visibility = Visibility.Visible;
                cardSmartTouch.Opacity = 0.4;
                cardSmartTouch.IsEnabled = false;
                txtTouchLabel.Text = "Đang kiểm tra...";

                cardSmartClass.Visibility = Visibility.Visible;
                cardSmartClass.Opacity = 0.4;
                cardSmartClass.IsEnabled = false;
                btnClassTeacher.IsEnabled = false;
                btnClassStudent.IsEnabled = false;

                cardParent.Visibility = Visibility.Visible;
                cardParent.Opacity = 0.4;
                cardParent.IsEnabled = false;
                cardParent.IsHitTestVisible = false;
                txtParentLabel.Text = "Đang kiểm tra...";

                cardStaff.Visibility = Visibility.Visible;
                cardStaff.Opacity = 0.4;
                cardStaff.IsEnabled = false;
                cardStaff.IsHitTestVisible = false;
                txtStaffLabel.Text = "Đang kiểm tra...";

                cardsGrid.Columns = 4;
                return;
            }

            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            
            // Ở chế độ Test Mode, luôn hiển thị đầy đủ để phục vụ kiểm thử
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);

            // Đọc trạng thái bản quyền của từng phân hệ
            bool hasTouch = isTest || licenseService.HasFeature("smartscreen") || licenseService.HasFeature("learning_tools") || licenseService.IsInFreeTrial;
            bool hasClass = isTest || licenseService.HasFeature("smartclass") || licenseService.IsInFreeTrial;
            
            bool hasParent = isTest || licenseService.HasFeature("parent_portal") || licenseService.CurrentLicense?.LicenseType == "premium" || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;
            bool hasStaff = isTest || licenseService.HasFeature("staff_portal") || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;

            string inactiveMode = AppSettings.ShowInactivePackagesMode;

            // Thiết lập trạng thái Card SmartTouch
            ConfigureCardState(cardSmartTouch, btnTouchLabel, txtTouchLabel, "Đăng nhập Giáo viên", hasTouch, inactiveMode);
            
            // Thiết lập trạng thái Card SmartClass
            if (hasClass)
            {
                cardSmartClass.Visibility = Visibility.Visible;
                cardSmartClass.Opacity = 1.0;
                cardSmartClass.IsEnabled = true;
                btnClassTeacher.IsEnabled = true;
                btnClassStudent.IsEnabled = true;
            }
            else
            {
                if (string.Equals(inactiveMode, "Hidden", StringComparison.OrdinalIgnoreCase))
                {
                    cardSmartClass.Visibility = Visibility.Collapsed;
                    cardSmartClass.IsEnabled = false;
                }
                else // LockedUpsell
                {
                    cardSmartClass.Visibility = Visibility.Visible;
                    cardSmartClass.Opacity = 0.4;
                    cardSmartClass.IsEnabled = false;
                    btnClassTeacher.IsEnabled = false;
                    btnClassStudent.IsEnabled = false;
                }
            }

            // Thiết lập trạng thái Card Parent (Chưa xuất bản -> Làm mờ và khóa)
            cardParent.Visibility = Visibility.Visible;
            cardParent.Opacity = 0.4;
            cardParent.IsEnabled = false;
            cardParent.IsHitTestVisible = false;
            txtParentLabel.Text = "Đăng nhập Phụ huynh";

            // Thiết lập trạng thái Card Staff (Chưa xuất bản -> Làm mờ và khóa)
            cardStaff.Visibility = Visibility.Visible;
            cardStaff.Opacity = 0.4;
            cardStaff.IsEnabled = false;
            cardStaff.IsHitTestVisible = false;
            txtStaffLabel.Text = "Đăng nhập Nhân viên";

            // Tính toán động số lượng cột của UniformGrid để hiển thị trên một hàng ngang (tránh tràn dọc)
            int visibleCardsCount = 0;
            if (cardSmartTouch.Visibility == Visibility.Visible) visibleCardsCount++;
            if (cardSmartClass.Visibility == Visibility.Visible) visibleCardsCount++;
            if (cardParent.Visibility == Visibility.Visible) visibleCardsCount++;
            if (cardStaff.Visibility == Visibility.Visible) visibleCardsCount++;

            cardsGrid.Columns = visibleCardsCount;
        }

        private void ConfigureCardState(Border card, Border labelBorder, TextBlock labelText, string activeText, bool isActive, string mode)
        {
            if (isActive)
            {
                card.Visibility = Visibility.Visible;
                card.Opacity = 1.0;
                card.IsEnabled = true;
                labelText.Text = activeText;
            }
            else
            {
                if (string.Equals(mode, "Hidden", StringComparison.OrdinalIgnoreCase))
                {
                    card.Visibility = Visibility.Collapsed;
                    card.IsEnabled = false;
                }
                else // LockedUpsell
                {
                    card.Visibility = Visibility.Visible;
                    card.Opacity = 0.4;
                    card.IsEnabled = true; // Cho phép click để mở hộp thoại kích hoạt
                    labelText.Text = "Nhấn để kích hoạt / nâng cấp";
                }
            }
        }

        private void CheckAndBypassIfSingleProduct()
        {
            string mode = AppSettings.ShowInactivePackagesMode;
            if (!string.Equals(mode, "Hidden", StringComparison.OrdinalIgnoreCase)) return;

            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasTouch = isTest || licenseService.HasFeature("smartscreen") || licenseService.HasFeature("learning_tools") || licenseService.IsInFreeTrial;
            bool hasClass = isTest || licenseService.HasFeature("smartclass") || licenseService.IsInFreeTrial;
            
            bool hasParent = isTest || licenseService.HasFeature("parent_portal") || licenseService.CurrentLicense?.LicenseType == "premium" || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;
            bool hasStaff = isTest || licenseService.HasFeature("staff_portal") || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;

            string pinnedRole = AppSettings.ActiveUserRole;
            if (string.Equals(pinnedRole, "All", StringComparison.OrdinalIgnoreCase))
            {
                int activeCount = 0;
                if (hasTouch) activeCount++;
                if (hasClass) activeCount++;
                if (hasParent) activeCount++;
                if (hasStaff) activeCount++;

                if (activeCount == 1)
                {
                    if (hasTouch)
                    {
                        CardSmartTouch_Click(this, null!);
                    }
                    else if (hasClass)
                    {
                        BtnClassTeacher_Click(this, null!);
                    }
                    else if (hasParent)
                    {
                        CardParent_Click(this, null!);
                    }
                    else if (hasStaff)
                    {
                        CardStaff_Click(this, null!);
                    }
                }
            }
        }

        private void OpenActivationDialog()
        {
            var activationDialog = new ActivationDialog();
            activationDialog.Owner = this;
            if (activationDialog.ShowDialog() == true)
            {
                // Reload lại giao diện sau khi kích hoạt thành công
                ApplyLicenseFeaturesUI();
            }
        }

        private void CardSmartTouch_Click(object sender, MouseButtonEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasTouch = isTest || licenseService.HasFeature("smartscreen") || licenseService.HasFeature("learning_tools") || licenseService.IsInFreeTrial;
            if (!hasTouch)
            {
                OpenActivationDialog();
                return;
            }

            var app = (App)Application.Current;
            app.UserRoleService.SaveRole(UserRole.SmartTouchOnly);
            AppSettings.ShowLoginScreen = 1;

            var loginWindow = new Form1_MainLogin(QASmartClass.Shared.AppMode.SmartScreen);
            this.Hide();

            // Đăng ký hàm đóng cụ thể để chống rò rỉ bộ nhớ
            loginWindow.Closed += LoginWindow_Closed;
            loginWindow.Show();
        }

        private void BtnClassTeacher_Click(object sender, RoutedEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasClass = isTest || licenseService.HasFeature("smartclass") || licenseService.IsInFreeTrial;
            if (!hasClass)
            {
                OpenActivationDialog();
                return;
            }

            var app = (App)Application.Current;
            app.UserRoleService.SaveRole(UserRole.Teacher);
            AppSettings.ShowLoginScreen = 1;

            var loginWindow = new Form1_MainLogin(QASmartClass.Shared.AppMode.SmartClass);
            this.Hide();

            // Đăng ký hàm đóng cụ thể để chống rò rỉ bộ nhớ
            loginWindow.Closed += LoginWindow_Closed;
            loginWindow.Show();
        }

        private void BtnClassStudent_Click(object sender, RoutedEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasClass = isTest || licenseService.HasFeature("smartclass") || licenseService.IsInFreeTrial;
            if (!hasClass)
            {
                OpenActivationDialog();
                return;
            }

            var app = (App)Application.Current;
            var studentLogin = new QASmartClass.StudentClient.Views.StudentLoginWindow();
            this.Hide();

            if (studentLogin.ShowDialog() == true)
            {
                app.UserRoleService.SaveRole(UserRole.Student);
                if (app._studentShell == null)
                {
                    app._studentShell = new QASmartClass.StudentClient.Views.StudentShell();
                    app._studentShell.Closed += (s, ev) => app._studentShell = null;
                }
                app._studentShell.Show();
                app._studentShell.Activate();
                try { this.Close(); } catch { }
            }
            else
            {
                if (!_isClosing)
                {
                    try { this.Show(); } catch (InvalidOperationException) { }
                }
            }
        }

        private void CardParent_Click(object sender, MouseButtonEventArgs e)
        {
            if (DateTime.Today.Year > 2000) return; // Chức năng chưa xuất bản
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasParent = isTest || licenseService.HasFeature("parent_portal") || licenseService.CurrentLicense?.LicenseType == "premium" || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;
            if (!hasParent)
            {
                OpenActivationDialog();
                return;
            }

            var parentHost = new ParentLoginHostWindow();
            parentHost.Owner = this;
            this.Hide();

            parentHost.Closed += (s, ev) =>
            {
                if (!_isClosing)
                {
                    try { this.Show(); } catch (InvalidOperationException) { }
                }
            };

            parentHost.ShowDialog();
        }

        private void CardStaff_Click(object sender, MouseButtonEventArgs e)
        {
            if (DateTime.Today.Year > 2000) return; // Chức năng chưa xuất bản
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasStaff = isTest || licenseService.HasFeature("staff_portal") || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;
            if (!hasStaff)
            {
                OpenActivationDialog();
                return;
            }

            var staffLogin = new QASmartClass.Staff.Views.StaffLoginWindow();
            staffLogin.Owner = this;
            this.Hide();

            if (staffLogin.ShowDialog() == true)
            {
                var app = (App)Application.Current;
                if (app._staffShell == null)
                {
                    app._staffShell = new QASmartClass.Staff.Views.StaffDashboardWindow();
                    app._staffShell.Closed += StaffShell_Closed;
                }
                app._staffShell.Show();
            }
            else
            {
                if (!_isClosing)
                {
                    try { this.Show(); } catch (InvalidOperationException) { }
                }
            }
        }

        // --- Hàm xử lý Closed của Window để giải phóng bộ nhớ ---
        private void LoginWindow_Closed(object? sender, EventArgs e)
        {
            if (sender is Window win)
            {
                win.Closed -= LoginWindow_Closed; // Hủy đăng ký tường minh
            }
            if (_isClosing) return;

            bool isDashboardOpen = Application.Current.Windows.OfType<Form2_MainDashboard>().Any();
            bool isClassroomOpen = Application.Current.Windows.OfType<QASmartClass.Classroom.Views.ClassroomShell>().Any();
            
            if (isDashboardOpen || isClassroomOpen)
            {
                try { this.Close(); } catch { }
            }
            else
            {
                try { this.Show(); } catch (InvalidOperationException) { }
            }
        }

        private void StaffShell_Closed(object? sender, EventArgs e)
        {
            if (sender is Window win)
            {
                win.Closed -= StaffShell_Closed; // Hủy đăng ký tường minh
            }
            if (!_isClosing)
            {
                try { this.Show(); } catch (InvalidOperationException) { }
            }
        }

        // --- Settings Overlay Panel ---
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var pinDialog = new QASmartClass.Admin.Views.PinDialog();
            if (pinDialog.ShowDialog() == true)
            {
                // Nạp trạng thái ActiveUserRole
                string currentRole = AppSettings.ActiveUserRole;
                foreach (System.Windows.Controls.ComboBoxItem item in cboActiveRole.Items)
                {
                    if (item.Tag?.ToString() == currentRole)
                    {
                        cboActiveRole.SelectedItem = item;
                        break;
                    }
                }

                // Nạp trạng thái ShowInactivePackagesMode
                string currentInactiveMode = AppSettings.ShowInactivePackagesMode;
                foreach (System.Windows.Controls.ComboBoxItem item in cboShowInactiveMode.Items)
                {
                    if (item.Tag?.ToString() == currentInactiveMode)
                    {
                        cboShowInactiveMode.SelectedItem = item;
                        break;
                    }
                }

                // Hiển thị bộ chuyển đổi môi trường trong panel admin
                modeSwitcherBorder.Visibility = Visibility.Visible;

                adminOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnAdminCancel_Click(object sender, RoutedEventArgs e)
        {
            adminOverlay.Visibility = Visibility.Collapsed;
            modeSwitcherBorder.Visibility = Visibility.Collapsed;
        }

        private void BtnAdminSave_Click(object sender, RoutedEventArgs e)
        {
            bool saved = false;
            if (cboActiveRole.SelectedItem is System.Windows.Controls.ComboBoxItem selectedRole)
            {
                AppSettings.ActiveUserRole = selectedRole.Tag?.ToString() ?? "All";
                saved = true;
            }
            if (cboShowInactiveMode.SelectedItem is System.Windows.Controls.ComboBoxItem selectedInactive)
            {
                AppSettings.ShowInactivePackagesMode = selectedInactive.Tag?.ToString() ?? "Hidden";
                saved = true;
            }

            if (saved)
            {
                AppSettings.Save();
                adminOverlay.Visibility = Visibility.Collapsed;
                modeSwitcherBorder.Visibility = Visibility.Collapsed;
                
                // Cập nhật giao diện động tức thời
                ApplyLicenseFeaturesUI();
                
                MessageBox.Show("Cấu hình hệ thống đã được lưu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // --- Hover & GotFocus / LostFocus Effects ---
        private void CardSmartTouch_MouseEnter(object sender, MouseEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasTouch = isTest || licenseService.HasFeature("smartscreen") || licenseService.HasFeature("learning_tools") || licenseService.IsInFreeTrial;
            if (hasTouch)
            {
                cardSmartTouch.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                btnTouchLabel.Background = new SolidColorBrush(Color.FromRgb(29, 78, 216));
            }
        }

        private void CardSmartTouch_MouseLeave(object sender, MouseEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasTouch = isTest || licenseService.HasFeature("smartscreen") || licenseService.HasFeature("learning_tools") || licenseService.IsInFreeTrial;
            if (hasTouch)
            {
                cardSmartTouch.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                btnTouchLabel.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            }
        }

        private void CardSmartClass_MouseEnter(object sender, MouseEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasClass = isTest || licenseService.HasFeature("smartclass") || licenseService.IsInFreeTrial;
            if (hasClass)
            {
                cardSmartClass.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
        }

        private void CardSmartClass_MouseLeave(object sender, MouseEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasClass = isTest || licenseService.HasFeature("smartclass") || licenseService.IsInFreeTrial;
            if (hasClass)
            {
                cardSmartClass.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
            }
        }

        private void CardParent_MouseEnter(object sender, MouseEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasParent = isTest || licenseService.HasFeature("parent_portal") || licenseService.CurrentLicense?.LicenseType == "premium" || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;
            if (hasParent)
            {
                cardParent.BorderBrush = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                btnParentLabel.Background = new SolidColorBrush(Color.FromRgb(8, 145, 178));
            }
        }

        private void CardParent_MouseLeave(object sender, MouseEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasParent = isTest || licenseService.HasFeature("parent_portal") || licenseService.CurrentLicense?.LicenseType == "premium" || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;
            if (hasParent)
            {
                cardParent.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                btnParentLabel.Background = new SolidColorBrush(Color.FromRgb(6, 182, 212));
            }
        }

        private void CardStaff_MouseEnter(object sender, MouseEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasStaff = isTest || licenseService.HasFeature("staff_portal") || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;
            if (hasStaff)
            {
                cardStaff.BorderBrush = new SolidColorBrush(Color.FromRgb(249, 115, 22));
                btnStaffLabel.Background = new SolidColorBrush(Color.FromRgb(194, 65, 12));
            }
        }

        private void CardStaff_MouseLeave(object sender, MouseEventArgs e)
        {
            var licenseService = QASmartTouch.Services.License.LicenseService.Instance;
            bool isTest = string.Equals(AppSettings.RunningMode, "Test", StringComparison.OrdinalIgnoreCase);
            bool hasStaff = isTest || licenseService.HasFeature("staff_portal") || licenseService.CurrentLicense?.LicenseType == "enterprise" || licenseService.IsInFreeTrial;
            if (hasStaff)
            {
                cardStaff.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                btnStaffLabel.Background = new SolidColorBrush(Color.FromRgb(249, 115, 22));
            }
        }

        // --- GotFocus / LostFocus Handlers for Keyboard Tab Navigation ---
        private void CardSmartTouch_GotFocus(object sender, RoutedEventArgs e)
        {
            CardSmartTouch_MouseEnter(sender, null!);
        }

        private void CardSmartTouch_LostFocus(object sender, RoutedEventArgs e)
        {
            CardSmartTouch_MouseLeave(sender, null!);
        }

        private void CardSmartClass_GotFocus(object sender, RoutedEventArgs e)
        {
            CardSmartClass_MouseEnter(sender, null!);
        }

        private void CardSmartClass_LostFocus(object sender, RoutedEventArgs e)
        {
            CardSmartClass_MouseLeave(sender, null!);
        }

        private void CardParent_GotFocus(object sender, RoutedEventArgs e)
        {
            CardParent_MouseEnter(sender, null!);
        }

        private void CardParent_LostFocus(object sender, RoutedEventArgs e)
        {
            CardParent_MouseLeave(sender, null!);
        }

        private void CardStaff_GotFocus(object sender, RoutedEventArgs e)
        {
            CardStaff_MouseEnter(sender, null!);
        }

        private void CardStaff_LostFocus(object sender, RoutedEventArgs e)
        {
            CardStaff_MouseLeave(sender, null!);
        }

        // --- KeyDown Handlers for Keyboard Activation (Enter / Space) ---
        private void CardSmartTouch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                CardSmartTouch_Click(sender, null!);
                e.Handled = true;
            }
        }

        private void CardSmartClass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                BtnClassTeacher_Click(sender, null!);
                e.Handled = true;
            }
        }

        private void CardParent_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                CardParent_Click(sender, null!);
                e.Handled = true;
            }
        }

        private void CardStaff_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                CardStaff_Click(sender, null!);
                e.Handled = true;
            }
        }

        public void ShowLoadingOverlay()
        {
            loadingOverlay.Visibility = Visibility.Visible;
        }

        public void HideLoadingOverlay()
        {
            IsInitialLoading = false;
            loadingOverlay.Visibility = Visibility.Collapsed;
            UpdateModeUI();
            CheckAndBypassIfSingleProduct();
        }

        public void UpdateLoadingStatus(string status, int progress)
        {
            lblLoadingStatus.Text = status;
        }
    }
}
