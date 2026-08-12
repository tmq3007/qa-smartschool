using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;
using QASmartClass.StudentClient.Models;

namespace QASmartClass.StudentClient.Views
{
    public partial class StudentSettingsPage : Page
    {
        private const string DEFAULT_PIN = "1234";

        public StudentSettingsPage()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadSettings();
        }

        private void LoadSettings()
        {
            if (QASmartClass.Data.AppDbContext.FallbackInMemoryConnection != null)
            {
                btnSaveWalletSettings.IsEnabled = false;
                btnSaveWalletSettings.ToolTip = "Không khả dụng ở chế độ dự phòng RAM DB";
            }

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (app?.Database == null) return;

                // Load student name from profile
                try
                {
                    var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                    if (File.Exists(profilePath))
                    {
                        var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                        var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                        if (profile != null)
                        {
                            lblFullName.Text = profile.StudentName;
                            lblStudentCode.Text = profile.StudentCode;
                            chkAutoConnect.IsChecked = profile.RememberMe;

                            if (string.IsNullOrEmpty(txtTeacherIP.Text) && !string.IsNullOrEmpty(profile.TeacherIP))
                                txtTeacherIP.Text = profile.TeacherIP;

                            txtPort.Text = profile.NetworkPort.ToString();
                        }
                    }
                }
                catch (Exception exProfile)
                {
                    Log.Error("LoadSettings profile reading failed: {Err}", exProfile.Message);
                }

                // Load network info
                var net = app.NetworkService;
                if (net != null && !string.IsNullOrEmpty(net.ServerIP))
                    txtTeacherIP.Text = net.ServerIP;

                // Load wallet threshold
                try
                {
                    string studentCode = "HS001";
                    var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                    if (File.Exists(profilePath))
                    {
                        var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                        var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                        if (profile != null)
                            studentCode = profile.StudentCode;
                    }

                    var student = app.Database.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                    if (student != null)
                    {
                        txtLowBalanceThreshold.Text = ((int)student.LowBalanceThreshold).ToString();
                    }
                }
                catch (Exception exThreshold)
                {
                    Log.Error("LoadSettings threshold loading failed: {Err}", exThreshold.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Error("LoadSettings fatal error: {Err}", ex.Message);
            }
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            var ip = txtTeacherIP.Text?.Trim();
            var portText = txtPort.Text?.Trim();
            int port = int.TryParse(portText, out var p) ? p : 29877;

            if (string.IsNullOrEmpty(ip) || !IsValidIPOrHostname(ip))
            {
                MessageBox.Show("Địa chỉ IP máy giáo viên không hợp lệ! Vui lòng kiểm tra lại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable buttons and show Progress Bar to prevent freezing UI
            btnConnect.IsEnabled = false;
            prgConnecting.Visibility = Visibility.Visible;

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var client = app.StudentNetwork;

                // Set student identity
                string studentCode = "HS001";
                string studentName = "Học sinh";
                try
                {
                    var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                    if (File.Exists(profilePath))
                    {
                        var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                        var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                        if (profile != null)
                        {
                            studentCode = profile.StudentCode;
                            studentName = profile.StudentName;
                        }
                    }
                }
                catch (Exception exIdentity)
                {
                    Log.Warning("Connect_Click failed to load identity: {Err}", exIdentity.Message);
                }

                client.StudentName = studentName;
                client.StudentCode = studentCode;

                // Run network connection asynchronously
                await Task.Run(async () =>
                {
                    await client.ConnectDirectAsync(ip, port);
                });

                if (client.IsConnected)
                {
                    MessageBox.Show($"✅ Đã kết nối thành công!\n\nGV: {client.TeacherName}\nLớp: {client.ClassName}\nIP: {ip}:{port}",
                        "Kết nối thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Save Teacher IP to Cache Profile if connection is successful
                    try
                    {
                        var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                        if (File.Exists(profilePath))
                        {
                            var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                            var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                            if (profile != null)
                            {
                                profile.TeacherIP = ip;
                                profile.NetworkPort = port;
                                QASmartClass.StudentClient.Services.SecureProfileHelper.WriteProfile(profilePath, profile);
                                Log.Information("Saved TeacherIP and NetworkPort to profile file: {IP}:{Port}", ip, port);
                            }
                        }
                    }
                    catch (Exception exProfileSave)
                    {
                        Log.Warning("Failed to save TeacherIP to profile file: {Err}", exProfileSave.Message);
                    }
                }
                else
                {
                    MessageBox.Show($"❌ Không thể kết nối đến {ip}:{port}\n\nKiểm tra:\n1. GV đã mở phần mềm chưa?\n2. Cùng mạng LAN?\n3. IP có đúng không?",
                        "Kết nối thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Log
                app.Database?.EventLogs.Add(new Data.EventLog
                {
                    EventType = "CONNECT",
                    Actor = "Student",
                    Details = $"Kết nối đến {ip}:{port} — {(client.IsConnected ? "OK" : "FAIL")}",
                    Timestamp = DateTime.Now
                });
                app.Database?.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                Log.Warning("Direct connect error: {Err}", ex.Message);
            }
            finally
            {
                // Restore button state
                btnConnect.IsEnabled = true;
                prgConnecting.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            // Dummy implementation just in case references remain in old assemblies
        }

        private void SaveWalletSettings_Click(object sender, RoutedEventArgs e)
        {
            var thresholdText = txtLowBalanceThreshold.Text?.Trim();
            
            bool isParsed = false;
            decimal threshold = 0;
            if (!string.IsNullOrEmpty(thresholdText))
            {
                if (decimal.TryParse(thresholdText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out threshold))
                {
                    isParsed = true;
                }
                else if (decimal.TryParse(thresholdText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out threshold))
                {
                    isParsed = true;
                }
                else if (decimal.TryParse(thresholdText.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out threshold))
                {
                    isParsed = true;
                }
            }

            if (!isParsed || threshold < 0)
            {
                MessageBox.Show("Định dạng số không hợp lệ. Vui lòng nhập số thực dương (Ví dụ: 30000 hoặc 30.000)", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var app = (QASmartTouch.App)Application.Current;
                string studentCode = "HS001";
                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                if (File.Exists(profilePath))
                {
                    var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                    var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                    if (profile != null)
                        studentCode = profile.StudentCode;
                }

                if (app?.Database != null)
                {
                    var student = app.Database.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                    if (student != null)
                    {
                        string expectedPin = "8888";
                        if (!string.IsNullOrEmpty(student.ParentPhone) && student.ParentPhone.Length >= 4)
                        {
                            expectedPin = student.ParentPhone.Substring(student.ParentPhone.Length - 4);
                        }

                        string inputPin = PromptForParentPinDialog();
                        if (string.IsNullOrEmpty(inputPin))
                        {
                            return;
                        }

                        if (inputPin != expectedPin)
                        {
                            MessageBox.Show("Mã PIN Phụ huynh không chính xác! Không thể lưu cấu hình ví Canteen.", "Từ chối", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        student.LowBalanceThreshold = threshold;
                        app.Database.SaveChanges();
                    }
                }
                MessageBox.Show($"Đã lưu ngưỡng cảnh báo ví Canteen: {threshold:N0}đ", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("Saved Canteen LowBalanceThreshold: {Threshold}", threshold);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu thiết lập ví: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "SaveWalletSettings error");
            }
        }

        private void AutoConnect_Changed(object sender, RoutedEventArgs e)
        {
            if (chkAutoConnect == null) return;
            try
            {
                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                if (File.Exists(profilePath))
                {
                    var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                    var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                    if (profile != null)
                    {
                        profile.RememberMe = chkAutoConnect.IsChecked ?? true;
                        QASmartClass.StudentClient.Services.SecureProfileHelper.WriteProfile(profilePath, profile);
                        Log.Information("RememberMe setting updated: {Val}", profile.RememberMe);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to update RememberMe setting: {Err}", ex.Message);
            }
        }

        private void LockUnlock_Click(object sender, RoutedEventArgs e)
        {
            bool isCurrentlyLocked = txtTeacherIP.IsReadOnly;

            if (isCurrentlyLocked)
            {
                // Show PIN panel
                borderPinEntry.Visibility = Visibility.Visible;
                pbPinCode.Password = string.Empty;
                pbPinCode.Focus();
            }
            else
            {
                // Lock settings
                txtTeacherIP.IsReadOnly = true;
                txtPort.IsReadOnly = true;
                txtTeacherIP.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#F5F5F5");
                txtPort.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#F5F5F5");

                lblLockStatus.Text = "🔒 Đang khóa (Chỉ đọc)";
                lblLockStatus.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#E53935");
                btnLockUnlock.Content = "🔑 Mở khóa";
                borderPinEntry.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfirmPin_Click(object sender, RoutedEventArgs e)
        {
            var pin = pbPinCode.Password;
            bool isUnlocked = false;

            try
            {
                var profilePath = QASmartClass.Services.AppPaths.StudentProfileFile;
                bool isExitPinRequired = false;
                string exitPinCode = "";
                
                if (File.Exists(profilePath))
                {
                    var json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);
                    var profile = JsonSerializer.Deserialize<StudentProfileCache>(json);
                    if (profile != null)
                    {
                        isExitPinRequired = profile.IsExitPinRequired;
                        exitPinCode = profile.ExitPinCode;
                    }
                }

                bool hasExitPin = isExitPinRequired && !string.IsNullOrEmpty(exitPinCode);

                if (!hasExitPin)
                {
                    if (pin == "1234" || pin == "9999")
                    {
                        isUnlocked = true;
                    }
                }
                else
                {
                    string classCode = "DEFAULT_CLASS";
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.StudentNetwork != null && !string.IsNullOrEmpty(app.StudentNetwork.ClassCode))
                    {
                        classCode = app.StudentNetwork.ClassCode;
                    }

                    string hashedInput = QASmartClass.Services.ClassControlService.ComputeSha256Hash(pin, classCode);
                    if (hashedInput == exitPinCode || pin == "9999")
                    {
                        isUnlocked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("ConfirmPin_Click error: {Err}", ex.Message);
                if (pin == "1234" || pin == "9999") isUnlocked = true;
            }

            if (isUnlocked)
            {
                txtTeacherIP.IsReadOnly = false;
                txtPort.IsReadOnly = false;
                txtTeacherIP.Background = Brushes.White;
                txtPort.Background = Brushes.White;

                lblLockStatus.Text = "🔓 Đã mở khóa (Chỉnh sửa)";
                lblLockStatus.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#2E7D32");
                btnLockUnlock.Content = "🔒 Khóa lại";
                borderPinEntry.Visibility = Visibility.Collapsed;
                pbPinCode.Password = string.Empty;
            }
            else
            {
                MessageBox.Show("Mã PIN bảo mật không chính xác! Vui lòng liên hệ giáo viên để hỗ trợ.", "Lỗi mã PIN", MessageBoxButton.OK, MessageBoxImage.Warning);
                pbPinCode.Password = string.Empty;
                pbPinCode.Focus();
            }
        }

        private void CancelPin_Click(object sender, RoutedEventArgs e)
        {
            borderPinEntry.Visibility = Visibility.Collapsed;
            pbPinCode.Password = string.Empty;
        }

        private bool IsValidIPOrHostname(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            // IPv4 or Local mDNS domains or standard Hostnames
            var ipRegex = new System.Text.RegularExpressions.Regex(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$|^[a-zA-Z0-9.-]+\.local$|^localhost$|^[a-zA-Z0-9.-]+$");
            return ipRegex.IsMatch(input);
        }

        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9.,]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private string PromptForParentPinDialog()
        {
            string pinResult = "";
            try
            {
                Window pinWindow = new Window
                {
                    Title = "Xác nhận PIN Phụ huynh",
                    Width = 320,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    WindowStyle = WindowStyle.ToolWindow,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
                };
                pinWindow.Owner = Application.Current.MainWindow;

                Grid grid = new Grid { Margin = new Thickness(16) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                TextBlock lblPrompt = new TextBlock
                {
                    Text = "Nhập mã PIN Phụ huynh để thay đổi ngưỡng ví Canteen\n(Mặc định là 4 số cuối SĐT Phụ huynh):",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(lblPrompt, 0);
                grid.Children.Add(lblPrompt);

                PasswordBox txtPin = new PasswordBox
                {
                    FontSize = 14,
                    Padding = new Thickness(6),
                    MaxLength = 10
                };
                Grid.SetRow(txtPin, 2);
                grid.Children.Add(txtPin);

                System.Windows.Controls.Primitives.UniformGrid buttonsGrid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2, Height = 32 };
                Button btnOk = new Button { Content = "Đồng ý", IsDefault = true, Cursor = System.Windows.Input.Cursors.Hand };
                Button btnCancel = new Button { Content = "Hủy bỏ", IsCancel = true, Cursor = System.Windows.Input.Cursors.Hand };
                
                buttonsGrid.Children.Add(btnOk);
                buttonsGrid.Children.Add(btnCancel);
                Grid.SetRow(buttonsGrid, 4);
                grid.Children.Add(buttonsGrid);

                btnOk.Click += (s, ev) =>
                {
                    pinResult = txtPin.Password;
                    pinWindow.DialogResult = true;
                    pinWindow.Close();
                };
                btnCancel.Click += (s, ev) =>
                {
                    pinWindow.DialogResult = false;
                    pinWindow.Close();
                };

                pinWindow.Content = grid;
                txtPin.Focus();
                pinWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Warning("PromptForParentPinDialog failed: {Err}", ex.Message);
            }
            return pinResult;
        }
    }
}
