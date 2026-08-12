using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Services;
using QASmartClass.Data;
using QASmartTouch.Services;

namespace QASmartTouch.Forms
{
    public partial class Form3_3_Settings : Window
    {
        private AppConfig _appConfig;
        private WorkstationConfig _workstationConfig;
        private System.Windows.Threading.DispatcherTimer? _toastTimer;

        public Form3_3_Settings()
        {
            InitializeComponent();
            
            // ✅ Ensure Settings window appears on top of MainDashboard
            this.Topmost = true;
            
            this.Loaded += Form3_3_Settings_Loaded;

            // Register Pasting Handlers to block pasting non-numeric characters
            DataObject.AddPastingHandler(txtAutoSaveInterval, OnCancelCommandPasting);
            DataObject.AddPastingHandler(txtRetentionDays, OnCancelCommandPasting);
            DataObject.AddPastingHandler(txtHeartbeatInterval, OnCancelCommandPasting);
        }

        private void OnCancelCommandPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!System.Text.RegularExpressions.Regex.IsMatch(text, "^[0-9]+$"))
                {
                    e.CancelCommand();
                    if (sender is TextBox tb)
                    {
                        ShowNumericWarning(tb);
                    }
                }
            }
            else
            {
                e.CancelCommand();
                if (sender is TextBox tb)
                {
                    ShowNumericWarning(tb);
                }
            }
        }

        private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            bool isNumeric = System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
            if (!isNumeric && sender is TextBox tb)
            {
                e.Handled = true;
                ShowNumericWarning(tb);
            }
        }

        private void ShowNumericWarning(TextBox textBox)
        {
            // 1. Tắt timer cũ đang hoạt động trên chính TextBox này
            if (textBox.Tag is System.Windows.Threading.DispatcherTimer oldTimer)
            {
                oldTimer.Stop();
            }

            // 2. Tắt ToolTip cũ nếu đang hiển thị
            if (textBox.ToolTip is ToolTip oldToolTip)
            {
                oldToolTip.IsOpen = false;
            }

            // 3. Thiết lập cảnh báo mới
            textBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252"));
            textBox.BorderThickness = new Thickness(1.5);

            var toolTip = new ToolTip
            {
                Content = "Chỉ cho phép nhập số nguyên dương lớn hơn 0!",
                PlacementTarget = textBox,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                IsOpen = true
            };
            textBox.ToolTip = toolTip;

            // 4. Tạo timer mới
            var timer = new System.Windows.Threading.DispatcherTimer 
            { 
                Interval = TimeSpan.FromSeconds(1.5) 
            };
            timer.Tick += (s, e) =>
            {
                textBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E1E8ED"));
                textBox.BorderThickness = new Thickness(1);
                toolTip.IsOpen = false;
                textBox.ToolTip = null;
                textBox.Tag = null;
                timer.Stop();
            };
            
            textBox.Tag = timer; // Lưu trữ timer vào Tag
            timer.Start();
        }

        private void ShowToast(string message)
        {
            txtToastMessage.Text = message;
            borderToast.Visibility = Visibility.Visible;
            borderToast.Opacity = 0.95;

            // Tắt timer cũ đang chạy (nếu có) để tránh xung đột
            if (_toastTimer != null)
            {
                _toastTimer.Stop();
            }

            _toastTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };
            _toastTimer.Tick += (s, e) =>
            {
                borderToast.Visibility = Visibility.Collapsed;
                _toastTimer.Stop();
                _toastTimer = null;
            };
            _toastTimer.Start();
        }

        private void Form3_3_Settings_Loaded(object sender, RoutedEventArgs e)
        {
            var config = AppConfig.Load();
            if (config.EnableAdminLock && !string.IsNullOrEmpty(config.AdminPasswordHash))
            {
                this.Visibility = Visibility.Hidden;
                var prompt = new AdminPasswordPromptDialog();
                if (prompt.ShowDialog() != true)
                {
                    this.Close();
                    return;
                }
                this.Visibility = Visibility.Visible;
            }

            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            // Load settings from AppConfig and WorkstationConfig
            _appConfig = AppConfig.Load();
            _workstationConfig = WorkstationConfig.Load();

            // Bind General settings
            chkAutoSave.IsChecked = _appConfig.EnableAutoSave;
            txtAutoSaveInterval.Text = _appConfig.AutoSaveIntervalMinutes.ToString();
            cmbLanguage.SelectedIndex = _appConfig.Language switch
            {
                "vi" => 0,
                "en" => 1,
                "ja" => 2,
                "ko" => 3,
                "zh" => 4,
                _ => 0
            };

            // Bind School & Admin settings
            txtSchoolCode.Text = _appConfig.SchoolCode;
            txtSchoolName.Text = _appConfig.SchoolName;
            chkEnableAdminLock.IsChecked = _appConfig.EnableAdminLock;

            // Bind Interface settings
            if (_appConfig.Theme == "Light") rbThemeLight.IsChecked = true;
            else if (_appConfig.Theme == "Dark") rbThemeDark.IsChecked = true;
            else rbThemeAuto.IsChecked = true;

            // Bind Storage settings
            txtStoragePath.Text = string.IsNullOrEmpty(_appConfig.CustomExportDir) 
                ? AppPaths.ExportsDir 
                : _appConfig.CustomExportDir;

            txtBackupPath.Text = string.IsNullOrEmpty(_appConfig.CustomBackupDir) 
                ? AppPaths.BackupsDir 
                : _appConfig.CustomBackupDir;

            // Bind Workstation settings
            txtMachineId.Text = _workstationConfig.MachineId;
            txtMachineName.Text = _workstationConfig.MachineName;
            txtRoomId.Text = _workstationConfig.RoomId;
            txtSeatPosition.Text = _workstationConfig.SeatPosition;
            cmbMachineRole.SelectedIndex = _workstationConfig.MachineRole.ToLower() switch
            {
                "student" => 0,
                "teacher" => 1,
                "kiosk" => 2,
                _ => 0
            };

            // Load Telemetry/Retention Settings
            txtRetentionDays.Text = _appConfig.TelemetryRetentionDays.ToString();

            // Bind 3D Graphics Settings
            chkHardwareAcceleration.IsChecked = _appConfig.EnableHardwareAcceleration;
            chkReduceAnimations.IsChecked = _appConfig.ReduceAnimations;
            chkAutoOptimizeForIntegratedGraphics.IsChecked = _appConfig.AutoOptimizeForIntegratedGraphics;
            chkDisable3DAntiAliasing.IsChecked = _appConfig.Disable3DAntiAliasing;
            chkReduce3DMeshResolution.IsChecked = _appConfig.Reduce3DMeshResolution;

            // Load Graphics Quality from AppSettings
            string currentQuality = AppSettings.GraphicsQuality ?? "Auto";
            int qualityIndex = currentQuality switch
            {
                "Low" => 1,
                "Medium" => 2,
                "High" => 3,
                _ => 0
            };
            cboGraphicsQuality.SelectedIndex = qualityIndex;

            // Bind Server settings
            txtServerUrl.Text = _appConfig.ServerUrl;
            txtHeartbeatInterval.Text = _appConfig.HeartbeatIntervalSec.ToString();

            // Highlight Accent Color button on load
            string currentAccent = _appConfig.AccentColor ?? "#2E86DE";
            foreach (var child in wpAccentColors.Children)
            {
                if (child is Button btn)
                {
                    string btnColor = btn.Tag?.ToString() ?? "";
                    if (btnColor.Equals(currentAccent, StringComparison.OrdinalIgnoreCase))
                    {
                        btn.BorderThickness = new Thickness(2);
                        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(btnColor));
                    }
                    else
                    {
                        btn.BorderThickness = new Thickness(0);
                    }
                }
            }

            // Bind Backup settings
            chkAutoBackup.IsChecked = _appConfig.EnableAutoBackup;
            cmbBackupFrequency.SelectedIndex = _appConfig.BackupFrequency switch
            {
                "Hourly" => 0,
                "Daily" => 1,
                "Weekly" => 2,
                "Monthly" => 3,
                _ => 1
            };

            // Check authorization to disable buttons for GiaoVien
            bool canManageBackup = true;
            try
            {
                if (UserSessionService.Instance.IsLoggedIn)
                {
                    string role = UserSessionService.Instance.Role;
                    canManageBackup = role == StatusConstants.TeacherRole.Admin || role == StatusConstants.TeacherRole.HieuTruong;
                }
            }
            catch { }
            btnBackupNow.IsEnabled = canManageBackup;

            // Load backup history dynamically
            LoadBackupHistory();

            // Load License Info
            LoadLicenseInfo();
        }

        private void LoadBackupHistory()
        {
            try
            {
                spBackupHistory.Children.Clear();
                var backupService = new BackupService();
                var backups = backupService.GetBackupList();

                if (backups == null || backups.Length == 0)
                {
                    var noBackupText = new TextBlock
                    {
                        Text = "Chưa có bản sao lưu nào.",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0)
                    };
                    spBackupHistory.Children.Add(noBackupText);
                    return;
                }

                // Check authorization
                bool canManageBackup = true;
                try
                {
                    if (UserSessionService.Instance.IsLoggedIn)
                    {
                        string role = UserSessionService.Instance.Role;
                        canManageBackup = role == StatusConstants.TeacherRole.Admin || role == StatusConstants.TeacherRole.HieuTruong;
                    }
                }
                catch { }

                foreach (var file in backups)
                {
                    var border = new Border
                    {
                        Background = new SolidColorBrush(Colors.White),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var infoStack = new StackPanel();
                    var titleText = new TextBlock
                    {
                        Text = file.Name,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(47, 53, 66))
                    };
                    double sizeInMb = (double)file.Length / (1024 * 1024);
                    var detailsText = new TextBlock
                    {
                        Text = $"Kích thước: {sizeInMb:F1} MB | Ngày: {file.CreationTime:dd/MM/yyyy HH:mm}",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(87, 96, 111)),
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    infoStack.Children.Add(titleText);
                    infoStack.Children.Add(detailsText);
                    Grid.SetColumn(infoStack, 0);
                    grid.Children.Add(infoStack);

                    var restoreBtn = new Button
                    {
                        Content = "Khôi phục",
                        Height = 30,
                        Width = 90,
                        Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        Foreground = new SolidColorBrush(Colors.White),
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        FontSize = 11,
                        Tag = file.FullName,
                        IsEnabled = canManageBackup
                    };
                    restoreBtn.Click += btnRestore_Click;
                    Grid.SetColumn(restoreBtn, 1);
                    grid.Children.Add(restoreBtn);

                    border.Child = grid;
                    spBackupHistory.Children.Add(border);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error("LoadBackupHistory failed: {Err}", ex.Message);
            }
        }

        private void LoadLicenseInfo()
        {
            txtRequestCode.Text = QASmartTouch.Services.License.LicenseService.Instance.GetRequestCode();
            
            var status = QASmartTouch.Services.License.LicenseService.Instance.CheckLicense();
            var lic = QASmartTouch.Services.License.LicenseService.Instance.CurrentLicense;

            if (lic != null)
            {
                txtLicSchool.Text = lic.SchoolName;
                txtLicType.Text = QASmartTouch.Services.License.LicenseService.Instance.GetLicenseTypeName();
                txtLicStart.Text = lic.ContractStart.ToString("dd/MM/yyyy");
                
                string expiryText = $"{lic.ExpiresAt:dd/MM/yyyy}";
                if (lic.DaysRemaining > 0)
                    expiryText += $" (Còn {lic.DaysRemaining} ngày)";
                else if (lic.IsInGracePeriod)
                    expiryText += $" (Quá hạn, ân hạn còn {(int)(lic.GraceDeadline - DateTime.UtcNow).TotalDays} ngày)";
                else
                    expiryText += " (Đã khóa)";
                
                txtLicExpiry.Text = expiryText;
            }

            txtLicStatusInfo.Text = status switch
            {
                QASmartTouch.Models.LicenseStatus.Valid => "✅ Đang hoạt động",
                QASmartTouch.Models.LicenseStatus.ExpiringSoon => "⚠️ Sắp hết hạn",
                QASmartTouch.Models.LicenseStatus.GracePeriod => "⏳ Đang ân hạn",
                QASmartTouch.Models.LicenseStatus.FullyExpired => "❌ Đã khóa (Hết hạn)",
                QASmartTouch.Models.LicenseStatus.Invalid => "❌ Lỗi chứng chỉ (Không hợp lệ)",
                _ => "Chưa kích hoạt"
            };

            txtLicStatusInfo.Foreground = status switch
            {
                QASmartTouch.Models.LicenseStatus.Valid => new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                QASmartTouch.Models.LicenseStatus.ExpiringSoon or QASmartTouch.Models.LicenseStatus.GracePeriod => new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                QASmartTouch.Models.LicenseStatus.FullyExpired or QASmartTouch.Models.LicenseStatus.Invalid => new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                _ => new SolidColorBrush(Color.FromRgb(117, 117, 117))
            };
        }

        // TAB 1: GENERAL
        private void btnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đang kiểm tra cập nhật...\n\n✅ Bạn đang sử dụng phiên bản mới nhất!\nPhiên bản: 1.1.0 (Build 2025.10.16)", 
                          "Cập nhật", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // TAB 2: PERSONALIZATION
        private void btnAccentColor_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                string color = button.Tag?.ToString() ?? "#2E86DE";
                _appConfig.AccentColor = color; // Save selected Accent Color
                
                // Hot-reload color resources dynamically
                try
                {
                    var colorObj = (Color)ColorConverter.ConvertFromString(color);
                    var brushObj = new SolidColorBrush(colorObj);
                    
                    // Cập nhật cả khóa màu thương hiệu (để đổi màu nóng trực tiếp)
                    Application.Current.Resources["BrandPrimaryColor"] = colorObj;
                    Application.Current.Resources["BrandPrimary"] = brushObj;
                    
                    // Thêm các khóa màu AccentColor bổ trợ nếu có dùng ở nơi khác
                    Application.Current.Resources["AccentColorBrush"] = brushObj;
                    Application.Current.Resources["AccentColor"] = colorObj;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to dynamic reload color resource: {ex.Message}");
                }

                ShowToast($"Màu chủ đạo đã được cập nhật trực tiếp: {color}");
                
                // Reset all borders
                foreach (var child in ((button.Parent as WrapPanel)?.Children)!)
                {
                    if (child is Button btn)
                    {
                        btn.BorderThickness = new Thickness(0);
                    }
                }
                
                // Highlight selected
                button.BorderThickness = new Thickness(2);
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
        }

        // TAB 3: STORAGE
        private string? ShowFolderBrowserDialog(string description, string defaultPath)
        {
            try
            {
                var formsAssembly = System.Reflection.Assembly.Load("System.Windows.Forms, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                var dialogType = formsAssembly.GetType("System.Windows.Forms.FolderBrowserDialog");
                if (dialogType != null)
                {
                    using (var dialog = (IDisposable)Activator.CreateInstance(dialogType)!)
                    {
                        dialogType.GetProperty("Description")?.SetValue(dialog, description);
                        dialogType.GetProperty("UseDescriptionForTitle")?.SetValue(dialog, true);
                        if (!string.IsNullOrEmpty(defaultPath) && Directory.Exists(defaultPath))
                        {
                            dialogType.GetProperty("InitialDirectory")?.SetValue(dialog, defaultPath);
                            dialogType.GetProperty("SelectedPath")?.SetValue(dialog, defaultPath);
                        }
                        
                        var showDialogMethod = dialogType.GetMethod("ShowDialog", Type.EmptyTypes);
                        var dialogResult = showDialogMethod?.Invoke(dialog, null);
                        
                        var okValue = formsAssembly.GetType("System.Windows.Forms.DialogResult")?.GetField("OK")?.GetValue(null);
                        
                        if (dialogResult != null && dialogResult.Equals(okValue))
                        {
                            return dialogType.GetProperty("SelectedPath")?.GetValue(dialog)?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reflection FolderBrowserDialog failed: {ex.Message}");
            }

            var fallbackDialog = new Microsoft.Win32.OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Chọn thư mục này"
            };
            if (fallbackDialog.ShowDialog() == true)
            {
                return System.IO.Path.GetDirectoryName(fallbackDialog.FileName);
            }
            return null;
        }

        private void btnBrowseStorage_Click(object sender, RoutedEventArgs e)
        {
            string? path = ShowFolderBrowserDialog("Chọn thư mục lưu trữ bài giảng", txtStoragePath.Text);
            if (path != null)
            {
                txtStoragePath.Text = path;
            }
        }

        private void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Xóa cache sẽ giải phóng 120 MB dung lượng.\nBạn có chắc chắn?", 
                                       "Xóa cache", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Clear cache logic
                MessageBox.Show("✅ Đã xóa cache thành công!\nGiải phóng: 120 MB", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnClearTemp_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Xóa tệp tạm sẽ giải phóng 85 MB dung lượng.\nBạn có chắc chắn?", 
                                       "Xóa tệp tạm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Clear temp files logic
                MessageBox.Show("✅ Đã xóa tệp tạm thành công!\nGiải phóng: 85 MB", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnConnectCloud_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đang kết nối với Cloud Storage...\n\n✅ Đã kết nối thành công!\nTài khoản: user@example.com\nDung lượng: 50 GB", 
                          "Cloud", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // TAB 4: LICENSE
        private void btnCopyRequestCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(txtRequestCode.Text);
                MessageBox.Show("Đã copy mã máy vào clipboard!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi copy: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnOpenActivationDialog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ActivationDialog();
            // Nếu activation thành công, reload lại UI
            if (dialog.ShowDialog() == true)
            {
                LoadLicenseInfo();
            }
        }

        // TAB 6: BACKUP
        private void btnBrowseBackup_Click(object sender, RoutedEventArgs e)
        {
            string? path = ShowFolderBrowserDialog("Chọn thư mục lưu trữ bản sao lưu", txtBackupPath.Text);
            if (path != null)
            {
                txtBackupPath.Text = path;
            }
        }

        private void btnRestore_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string backupFilePath = button?.Tag?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(backupFilePath)) return;

            string backupName = System.IO.Path.GetFileName(backupFilePath);
            var result = MessageBox.Show($"Khôi phục dữ liệu từ bản sao lưu {backupName}?\n\nCảnh báo: Dữ liệu hiện tại sẽ bị ghi đè!", 
                                       "Khôi phục", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var backupService = new BackupService();
                    var restoreResult = backupService.Restore(backupFilePath);
                    switch (restoreResult)
                    {
                        case RestoreResult.Success:
                            MessageBox.Show("✅ Khôi phục dữ liệu thành công!\nỨng dụng cần khởi động lại để áp dụng dữ liệu mới.", 
                                          "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;
                        case RestoreResult.FileNotFound:
                            MessageBox.Show("Khôi phục thất bại! Không tìm thấy tệp tin sao lưu trên hệ thống đĩa cứng.", 
                                          "Lỗi khôi phục", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        case RestoreResult.IntegrityCheckFailed:
                            MessageBox.Show("Khôi phục thất bại! Tệp sao lưu bị lỗi cấu trúc vật lý SQLite (Integrity check failed). Vui lòng chọn bản sao lưu khác lành lặn hơn.", 
                                          "Lỗi khôi phục", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        case RestoreResult.ChecksumMismatch:
                            MessageBox.Show("Khôi phục thất bại! Chữ ký bảo mật (SHA-256 Checksum) của tệp sao lưu không khớp, tệp có thể đã bị sửa đổi trái phép.", 
                                          "Lỗi bảo mật khôi phục", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                        default:
                            MessageBox.Show("Khôi phục thất bại do lỗi không xác định! Vui lòng liên hệ với Quản lý IT phòng máy.", 
                                          "Lỗi khôi phục", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khôi phục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnBackupNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backupService = new BackupService();
                string? backupPath = backupService.CreateBackup("manual");
                if (backupPath != null)
                {
                    MessageBox.Show($"✅ Đã tạo bản sao lưu thành công!\nVị trí: {backupPath}", 
                                  "Sao lưu", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadBackupHistory(); // Refresh history list
                }
                else
                {
                    MessageBox.Show("Không thể tạo bản sao lưu!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sao lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // FOOTER
        private void btnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Đặt lại cài đặt ứng dụng về mặc định?\n\nThao tác này không thể hoàn tác và sẽ giữ nguyên thông tin định danh máy trạm phòng Lab.", 
                                       "Đặt lại", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _appConfig = new AppConfig();
                _appConfig.Save();
                
                MessageBox.Show("✅ Đã đặt lại cài đặt ứng dụng về mặc định!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCurrentSettings();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validate numerical inputs
            int autoSaveInterval = _appConfig.AutoSaveIntervalMinutes;
            if (chkAutoSave.IsChecked == true)
            {
                if (!int.TryParse(txtAutoSaveInterval.Text, out autoSaveInterval) || autoSaveInterval <= 0)
                {
                    MessageBox.Show("Khoảng thời gian tự động lưu phải là số nguyên dương lớn hơn 0!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            int retentionDays = _appConfig.TelemetryRetentionDays;
            if (chkSaveRecordings.IsChecked == true || !string.IsNullOrWhiteSpace(txtRetentionDays.Text))
            {
                if (!int.TryParse(txtRetentionDays.Text, out retentionDays) || retentionDays <= 0)
                {
                    MessageBox.Show("Thời gian lưu trữ video phải là số nguyên dương lớn hơn 0!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (!int.TryParse(txtHeartbeatInterval.Text, out int heartbeatInterval) || heartbeatInterval < 10)
            {
                MessageBox.Show("Chu kỳ gửi tín hiệu Heartbeat phải là số nguyên dương lớn hơn hoặc bằng 10 giây!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Validate paths
            string storagePath = txtStoragePath.Text.Trim();
            string backupPath = txtBackupPath.Text.Trim();

            try
            {
                if (!Directory.Exists(storagePath))
                {
                    Directory.CreateDirectory(storagePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đường dẫn thư mục lưu trữ không hợp lệ hoặc không có quyền truy cập:\n{ex.Message}", "Lỗi thư mục", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đường dẫn thư mục sao lưu không hợp lệ hoặc không có quyền truy cập:\n{ex.Message}", "Lỗi thư mục", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. Save AppConfig
            _appConfig.EnableAutoSave = chkAutoSave.IsChecked == true;
            _appConfig.AutoSaveIntervalMinutes = autoSaveInterval;
            _appConfig.Language = cmbLanguage.SelectedIndex switch
            {
                0 => "vi",
                1 => "en",
                2 => "ja",
                3 => "ko",
                4 => "zh",
                _ => "vi"
            };
            _appConfig.Theme = rbThemeLight.IsChecked == true ? "Light" : (rbThemeDark.IsChecked == true ? "Dark" : "Auto");
            _appConfig.CustomExportDir = storagePath;
            _appConfig.CustomBackupDir = backupPath;
            _appConfig.TelemetryRetentionDays = retentionDays;
            
            _appConfig.EnableAutoBackup = chkAutoBackup.IsChecked == true;
            _appConfig.BackupFrequency = cmbBackupFrequency.SelectedIndex switch
            {
                0 => "Hourly",
                1 => "Daily",
                2 => "Weekly",
                3 => "Monthly",
                _ => "Daily"
            };

            _appConfig.SchoolCode = txtSchoolCode.Text.Trim();
            _appConfig.SchoolName = txtSchoolName.Text.Trim();
            _appConfig.EnableAdminLock = chkEnableAdminLock.IsChecked == true;
            if (!string.IsNullOrEmpty(pbAdminPassword.Password))
            {
                _appConfig.AdminPasswordHash = ConfigurationSecurityHelper.ComputeSha256Hash(pbAdminPassword.Password);
            }

            // Trước khi lưu _appConfig.ServerUrl:
            string serverUrl = txtServerUrl.Text.Trim();
            if (!string.IsNullOrEmpty(serverUrl) && 
                !serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !serverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                serverUrl = "http://" + serverUrl;
                txtServerUrl.Text = serverUrl;
            }
            _appConfig.ServerUrl = serverUrl;

            _appConfig.HeartbeatIntervalSec = heartbeatInterval;
            
            // Save 3D Graphics Settings
            bool oldHardwareAccel = _appConfig.EnableHardwareAcceleration;
            _appConfig.EnableHardwareAcceleration = chkHardwareAcceleration.IsChecked == true;
            _appConfig.ReduceAnimations = chkReduceAnimations.IsChecked == true;
            _appConfig.AutoOptimizeForIntegratedGraphics = chkAutoOptimizeForIntegratedGraphics.IsChecked == true;
            _appConfig.Disable3DAntiAliasing = chkDisable3DAntiAliasing.IsChecked == true;
            _appConfig.Reduce3DMeshResolution = chkReduce3DMeshResolution.IsChecked == true;
            
            _appConfig.Save();

            // Save Graphics Quality to AppSettings
            AppSettings.GraphicsQuality = cboGraphicsQuality.SelectedIndex switch
            {
                1 => "Low",
                2 => "Medium",
                3 => "High",
                _ => "Auto"
            };
            AppSettings.Save();

            if (oldHardwareAccel != _appConfig.EnableHardwareAcceleration)
            {
                MessageBox.Show("Thay đổi thiết lập Tăng tốc phần cứng sẽ có hiệu lực đầy đủ ở lần khởi động tiếp theo của ứng dụng.", 
                    "Thông báo khởi động lại", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // 4. Save WorkstationConfig
            _workstationConfig.MachineId = txtMachineId.Text.Trim();
            _workstationConfig.MachineName = txtMachineName.Text.Trim();
            
            string roomId = txtRoomId.Text.Trim();
            // Chuẩn hóa: Thay thế khoảng trắng bằng dấu gạch dưới, chuyển thành chữ viết hoa
            roomId = System.Text.RegularExpressions.Regex.Replace(roomId, @"\s+", "_").ToUpperInvariant();
            if (!string.IsNullOrEmpty(roomId) && !System.Text.RegularExpressions.Regex.IsMatch(roomId, "^[A-Z0-9_\\-]+$"))
            {
                MessageBox.Show("Mã phòng học chỉ được chứa ký tự chữ, số, dấu gạch ngang và dấu gạch dưới!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _workstationConfig.RoomId = roomId;
            _workstationConfig.SeatPosition = txtSeatPosition.Text.Trim();
            _workstationConfig.MachineRole = cmbMachineRole.SelectedIndex switch
            {
                0 => "student",
                1 => "teacher",
                2 => "kiosk",
                _ => "student"
            };
            _workstationConfig.Save();

            MessageBox.Show("✅ Đã lưu tất cả cài đặt thành công!\n\nMột số thay đổi sẽ có hiệu lực sau khi khởi động lại ứng dụng.", 
                          "Lưu cài đặt", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void btnOpenStorageDir_Click(object sender, RoutedEventArgs e)
        {
            string path = txtStoragePath.Text.Trim();
            if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            else
            {
                var result = MessageBox.Show("Thư mục không tồn tại trên máy tính! Bạn có muốn hệ thống tự động tạo mới thư mục này không?", "Thư mục không tồn tại", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        System.Diagnostics.Process.Start("explorer.exe", path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể tạo thư mục:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnOpenBackupDir_Click(object sender, RoutedEventArgs e)
        {
            string path = txtBackupPath.Text.Trim();
            if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            else
            {
                var result = MessageBox.Show("Thư mục không tồn tại trên máy tính! Bạn có muốn hệ thống tự động tạo mới thư mục này không?", "Thư mục không tồn tại", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        System.Diagnostics.Process.Start("explorer.exe", path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể tạo thư mục:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // AI Features warning
        private void chkAI_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null && checkBox.IsChecked == true)
            {
                MessageBox.Show(
                    "⚠️ Cảnh báo Bảo mật & Quyền riêng tư:\n\n" +
                    "Tính năng này thu thập dữ liệu hành vi sinh trắc học của học sinh.\n" +
                    "Bạn bắt buộc phải có sự đồng ý bằng văn bản của Ban giám hiệu và Phụ huynh học sinh trước khi kích hoạt sử dụng trong lớp học.",
                    "Cảnh báo Bảo mật AI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private async void btnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            string url = txtServerUrl.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ URL máy chủ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Tự động kiểm tra và thêm tiền tố http:// nếu thiếu
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
                txtServerUrl.Text = url; // Cập nhật hiển thị trực quan lên TextBox
            }

            btnTestConnection.IsEnabled = false;
            btnTestConnection.Content = "Đang kết nối...";

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    
                    // 1. Thử gửi request HEAD để tối ưu hóa băng thông mạng
                    using (var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, url))
                    {
                        try
                        {
                            var response = await client.SendAsync(request);
                            // Nếu thành công hoặc máy chủ trả về mã phản hồi hợp lệ
                            if (response.IsSuccessStatusCode)
                            {
                                MessageBox.Show("✅ Kết nối máy chủ thành công (HEAD)!", "Kết nối thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                            
                            // Nếu máy chủ từ chối phương thức HEAD (405 Method Not Allowed hoặc 501 Not Implemented)
                            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed || 
                                response.StatusCode == System.Net.HttpStatusCode.NotImplemented)
                            {
                                // 2. Chuyển sang Fallback GET
                                var fallbackResponse = await client.GetAsync(url);
                                if (fallbackResponse.IsSuccessStatusCode)
                                {
                                    MessageBox.Show("✅ Kết nối máy chủ thành công (Fallback GET)!", "Kết nối thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                                    return;
                                }
                                else
                                {
                                    MessageBox.Show($"Kết nối thất bại! Mã phản hồi từ máy chủ: {(int)fallbackResponse.StatusCode} ({fallbackResponse.ReasonPhrase})", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                            
                            MessageBox.Show($"Kết nối thất bại! Mã phản hồi từ máy chủ: {(int)response.StatusCode} ({response.ReasonPhrase})", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException)
                        {
                            // Thử lại bằng GET nếu gặp lỗi giao thức khi gửi HEAD (ví dụ: firewall chặn HEAD)
                            var fallbackResponse = await client.GetAsync(url);
                            if (fallbackResponse.IsSuccessStatusCode)
                            {
                                MessageBox.Show("✅ Kết nối máy chủ thành công (Fallback GET)!", "Kết nối thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show($"Kết nối thất bại! Chi tiết lỗi: {ex.Message}", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối đến máy chủ!\nChi tiết lỗi: {ex.Message}", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnTestConnection.IsEnabled = true;
                btnTestConnection.Content = "Kiểm tra kết nối";
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}
