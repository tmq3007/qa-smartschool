using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QASmartClass.Classroom.Services;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class ClassroomPage : Page
    {
        private readonly ClassroomSessionService _sessionService;
        private DispatcherTimer? _sessionTimer;
        private int _sessionSeconds = 0;
        public string RoomStatus { get; set; } = "Học tập";

        public ClassroomPage()
        {
            InitializeComponent();
            
            // Lấy Instance Singleton của ClassroomSessionService từ App
            _sessionService = ((QASmartTouch.App)Application.Current).ClassroomSession;

            Loaded += (_, _) =>
            {
                // Liên kết ItemsSource với danh sách thực tế của Session
                studentGrid.ItemsSource = _sessionService.ConnectedStudents;

                // Nếu chưa có phiên chạy và chưa có học sinh nào, tải danh sách lớp offline ban đầu làm nền
                if (!_sessionService.IsSessionActive && !_sessionService.ConnectedStudents.Any())
                {
                    LoadOfflineRoster();
                }
                else
                {
                    UpdateClassroomUI();
                }

                // Kiểm tra kết nối mạng cục bộ để cảnh báo giáo viên
                CheckNetworkConnectivity();

                // Đăng ký sự kiện thay đổi Roster (Chọn lớp) - Hủy đăng ký trước để tránh memory leak
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    app.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                    app.ClassRoster.ActiveRosterChanged += OnActiveRosterChanged;
                }
                catch (Exception ex)
                {
                    Log.Warning("Không thể đăng ký sự kiện ActiveRosterChanged: {Err}", ex.Message);
                }

                // Đăng ký sự kiện thay đổi địa chỉ IP mạng thực tế
                try
                {
                    System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                    System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
                }
                catch (Exception ex)
                {
                    Log.Warning("Không thể đăng ký NetworkAddressChanged: {Err}", ex.Message);
                }
            };

            Unloaded += (_, _) =>
            {
                StopLocalTimer();
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    app.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                }
                catch { }
                try
                {
                    System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                }
                catch { }
            };
        }

        private void OnNetworkAddressChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                CheckNetworkConnectivity();
            });
        }

        private void OnActiveRosterChanged(object? sender, Data.ClassRoster? roster)
        {
            Dispatcher.Invoke(() =>
            {
                // Chỉ nạp lại danh sách offline nếu phiên học chưa bắt đầu
                if (!_sessionService.IsSessionActive)
                {
                    LoadOfflineRoster();
                }
                else
                {
                    // Nếu đang chạy học mạng, chỉ cập nhật lại tên hiển thị trên header
                    var rosterName = RosterHelper.GetActiveRosterName();
                    if (txtSessionName != null)
                        txtSessionName.Text = $" {rosterName}";
                }
            });
        }

        /// <summary>
        /// Nạp danh sách lớp ban đầu làm nền hiển thị (tất cả học sinh ngoại tuyến)
        /// </summary>
        private void LoadOfflineRoster()
        {
            try
            {
                _sessionService.ConnectedStudents.Clear();
                var rosterStudents = RosterHelper.GetStudents();
                var rosterName = RosterHelper.GetActiveRosterName();

                foreach (var s in rosterStudents)
                {
                    _sessionService.ConnectedStudents.Add(new QASmartClass.Classroom.Services.ConnectedStudent
                    {
                        Name = s.FullName,
                        StudentCode = string.IsNullOrWhiteSpace(s.StudentCode) ? $"ST-{s.Id:D3}" : s.StudentCode,
                        PCName = string.IsNullOrWhiteSpace(s.PCName) ? $"PC-{s.Id:D2}" : s.PCName,
                        IPAddress = string.IsNullOrWhiteSpace(s.IPAddress) ? "" : s.IPAddress,
                        IsOnline = false,
                        IsLocked = false
                    });
                }

                if (txtSessionName != null) txtSessionName.Text = $" {rosterName}";
                if (txtSessionIP != null) txtSessionIP.Text = $"IP: {((QASmartTouch.App)Application.Current).NetworkService.ServerIP}";
                if (txtSessionCode != null) txtSessionCode.Text = "Mã lớp: --";
                
                UpdateStudentCount();
                Log.Information("ClassroomPage loaded {Count} offline roster students", _sessionService.ConnectedStudents.Count);
            }
            catch (Exception ex)
            {
                Log.Warning("ClassroomPage load offline roster error: {Error}", ex.Message);
            }
        }

        private void UpdateStudentCount()
        {
            if (txtOnlineCount == null) return;
            int online = _sessionService.ConnectedStudents.Count(s => s.IsOnline);
            txtOnlineCount.Text = $"{online}/{_sessionService.ConnectedStudents.Count} Học sinh trực tuyến";
        }

        private void UpdateClassroomUI()
        {
            var rosterName = RosterHelper.GetActiveRosterName();
            if (txtSessionName != null) txtSessionName.Text = $" {rosterName}";

            var app = (QASmartTouch.App)Application.Current;
            if (txtSessionIP != null) txtSessionIP.Text = $"IP: {app.NetworkService.ServerIP}";
            if (txtSessionCode != null) txtSessionCode.Text = string.IsNullOrEmpty(_sessionService.ClassCode) ? "Mã lớp: --" : $"Mã lớp: {_sessionService.ClassCode}";

            UpdateStudentCount();

            // Cập nhật trạng thái và màu sắc của nút bắt đầu lớp
            if (_sessionService.IsSessionActive)
            {
                btnStartSession.Content = "Kết thúc (00:00)";
                btnStartSession.Tag = FindResource("GeomStop");
                btnStartSession.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336")); // Màu đỏ
                StartLocalTimer();
            }
            else
            {
                btnStartSession.Content = "Bắt đầu lớp";
                btnStartSession.Tag = FindResource("GeomPlay");
                btnStartSession.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50")); // Màu xanh lá
                StopLocalTimer();
            }

            // Cập nhật trạng thái và màu sắc của nút Im lặng theo chuẩn v4.1
            if (btnMute != null)
            {
                if (_sessionService.IsSilenceActive)
                {
                    btnMute.Content = "Mở im lặng";
                    btnMute.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFEBEE")); // Màu hồng đỏ nhạt sư phạm
                    btnMute.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#C62828")); // Màu đỏ đậm
                }
                else
                {
                    btnMute.Content = "Im lặng";
                    btnMute.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ECEFF1")); // Màu xám nhạt mặc định
                    btnMute.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#546E7A")); // Màu xám xanh
                }
            }
        }

        private void StartLocalTimer()
        {
            _sessionTimer?.Stop();
            _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sessionTimer.Tick += (s, ev) =>
            {
                if (_sessionService.SessionStartTime.HasValue)
                {
                    var elapsed = DateTime.Now - _sessionService.SessionStartTime.Value;
                    _sessionSeconds = (int)elapsed.TotalSeconds;
                }
                else
                {
                    _sessionSeconds++;
                }
                
                btnStartSession.Content = $"Kết thúc ({_sessionSeconds / 60:D2}:{_sessionSeconds % 60:D2})";
                UpdateStudentCount();
            };
            _sessionTimer.Start();
        }

        private void CheckNetworkConnectivity()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                string localIP = app.NetworkService?.ServerIP ?? "";

                if (localIP == "127.0.0.1" || string.IsNullOrEmpty(localIP))
                {
                    if (bannerNetworkWarning != null) bannerNetworkWarning.Visibility = Visibility.Visible;
                    if (txtWarningMessage != null) txtWarningMessage.Text = "Cảnh báo: Không phát hiện kết nối mạng LAN thật (IP đang là Loopback 127.0.0.1). Học sinh trên máy trạm/tablet sẽ không thể kết nối được lớp học. Vui lòng cắm cáp mạng hoặc kết nối mạng Wi-Fi của phòng học!";
                }
                else
                {
                    if (bannerNetworkWarning != null) bannerNetworkWarning.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Lỗi khi kiểm tra kết nối mạng: {Err}", ex.Message);
            }
        }

        private void StopLocalTimer()
        {
            _sessionTimer?.Stop();
            _sessionTimer = null;
            _sessionSeconds = 0;
        }

        private async void StartSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnStartSession.IsEnabled = false;

                if (!_sessionService.IsSessionActive)
                {
                    var rosterName = RosterHelper.GetActiveRosterName();
                    string teacherName = ((QASmartTouch.App)Application.Current).UserRoleService.DisplayName;
                    if (string.IsNullOrEmpty(teacherName)) teacherName = "Teacher";

                    await _sessionService.StartSessionAsync(rosterName, teacherName);

                    // Đồng bộ thông tin sang ClassroomShell
                    if (Window.GetWindow(this) is ClassroomShell shell)
                        shell.UpdateSessionInfo(rosterName, _sessionService.ConnectedStudents.Count(s => s.IsOnline));

                    Log.Information("Classroom session started successfully.");
                    MessageBox.Show($"✅ Lớp học đã bắt đầu!\n\n👥 {_sessionService.ConnectedStudents.Count} HS trong danh sách lớp.\n📡 Đang khám phá các thiết bị kết nối dưới mạng LAN...",
                        "Bắt đầu lớp học", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var result = MessageBox.Show("Bạn có chắc chắn muốn kết thúc lớp học này không?", "Kết thúc lớp học", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        await _sessionService.EndSessionAsync();

                        if (Window.GetWindow(this) is ClassroomShell shell)
                            shell.UpdateSessionInfo("Chưa chọn lớp", 0);

                        MessageBox.Show("⏹️ Lớp học đã kết thúc. Đã đóng kết nối với các thiết bị học sinh.", "Kết thúc lớp học", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                UpdateClassroomUI();
            }
            catch (Exception ex)
            {
                Log.Error("Lỗi chuyển đổi trạng thái phiên học: {Err}", ex.Message);
                MessageBox.Show($"Không thể thực hiện yêu cầu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnStartSession.IsEnabled = true;
            }
        }

        private void Broadcast_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateTo("F16");
        }

        private async void LockAll_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsSessionActive)
            {
                MessageBox.Show("Vui lòng bắt đầu lớp học trước khi thực hiện khóa màn hình!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _sessionService.LockAllAsync();
                MessageBox.Show($"🔒 Đã phát lệnh khóa màn hình đến tất cả học sinh trực tuyến!", "Khóa màn hình", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể khóa màn hình: {ex.Message}", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UnlockAll_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsSessionActive) return;

            try
            {
                await _sessionService.UnlockAllAsync();
                MessageBox.Show("🔓 Đã phát lệnh mở khóa màn hình đến tất cả học sinh!", "Mở khóa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở khóa màn hình: {ex.Message}", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SendFile_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateTo("F11");
        }

        private void Chat_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateTo("F13");
        }

        private void StartQuiz_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is ClassroomShell shell)
                shell.NavigateTo("F6");
        }

        private async void Mute_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsSessionActive)
            {
                MessageBox.Show("Vui lòng bắt đầu lớp học trước khi thực hiện lệnh!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_sessionService.IsSilenceActive)
                {
                    await _sessionService.ClearSilenceAllAsync();
                    MessageBox.Show("🔊 Đã gỡ bỏ trạng thái Im lặng thành công!", "Mở im lặng", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _sessionService.SilenceAllAsync();
                    MessageBox.Show("🔇 Đã kích hoạt chế độ Im lặng trên toàn bộ các máy trạm học sinh thành công!", "Im lặng", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                UpdateClassroomUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi truyền tin: {ex.Message}", "Lỗi kết nối mạng", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Lỗi khi truyền lệnh im lặng đến máy học sinh");
            }
        }

        private void StudentCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is QASmartClass.Classroom.Services.ConnectedStudent student)
            {
                // Điều hướng giáo viên sang màn hình Giám sát (MonitorPage - F5) khi nhấp vào học sinh
                if (Window.GetWindow(this) is ClassroomShell shell)
                {
                    shell.SelectedStudentCode = student.StudentCode;
                    shell.NavigateTo("F5");
                }
            }
        }

        private void LowerStudentHand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
            {
                if (contextMenu.PlacementTarget is Border border && border.DataContext is QASmartClass.Classroom.Services.ConnectedStudent student)
                {
                    student.IsHandRaised = false;

                    var app = (QASmartTouch.App)Application.Current;
                    if (app.NetworkService != null && !string.IsNullOrEmpty(student.StudentCode))
                    {
                        _ = app.NetworkService.SendToStudentAsync(student.StudentCode, "CMD|HAND_LOWER");
                        Log.Information("Teacher manually lowered hand for student: {Code}", student.StudentCode);
                    }
                }
            }
        }

        private void LowerAllHands_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionService != null)
            {
                foreach (var student in _sessionService.ConnectedStudents)
                {
                    student.IsHandRaised = false;
                }
            }

            var app = (QASmartTouch.App)Application.Current;
            if (app.NetworkService != null)
            {
                _ = app.NetworkService.SendCommandAsync("CMD|HAND_LOWER");
                Log.Information("Teacher manually lowered hands for all students");
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                bool isQuizActive = false;
                var shell = Window.GetWindow(this) as ClassroomShell;
                if (shell != null && shell.contentFrame.Content is QuizPage quizPage && quizPage.IsQuizActive)
                {
                    isQuizActive = true;
                }

                bool isExam = RoomStatus == "Đang thi" || isQuizActive;

                foreach (var item in menu.Items)
                {
                    if (item is MenuItem menuItem && menuItem.Header != null && menuItem.Header.ToString()!.Contains("Khôi phục"))
                    {
                        menuItem.IsEnabled = !isExam;
                        menuItem.Visibility = isExam ? Visibility.Collapsed : Visibility.Visible;
                    }
                }
            }
        }

        private void DiagnoseStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsSessionActive)
            {
                MessageBox.Show("Vui lòng bắt đầu lớp học trước khi thực hiện chẩn đoán!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
            {
                if (contextMenu.PlacementTarget is Border border && border.DataContext is QASmartClass.Classroom.Services.ConnectedStudent student)
                {
                    if (!student.IsOnline)
                    {
                        MessageBox.Show("Học sinh này đang offline. Không thể chẩn đoán từ xa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var app = (QASmartTouch.App)Application.Current;
                    if (app.NetworkService != null && !string.IsNullOrEmpty(student.StudentCode))
                    {
                        _ = app.NetworkService.SendCheckIntegrityAsync(student.StudentCode);
                        Log.Information("Teacher requested integrity check for student: {Code}", student.StudentCode);
                        MessageBox.Show($"📡 Đang gửi yêu cầu chẩn đoán tới trạm {student.Name} ({student.PCName})...", "Chẩn đoán từ xa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

                private string ComputeHmacSha256(string message, string secret)
        {
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return System.Convert.ToHexString(hashmessage);
            }
        }

        private async void RestoreStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsSessionActive)
            {
                MessageBox.Show("Vui lòng bắt đầu lớp học trước khi thực hiện khôi phục!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
            {
                if (contextMenu.PlacementTarget is Border border && border.DataContext is QASmartClass.Classroom.Services.ConnectedStudent student)
                {
                    if (!student.IsOnline)
                    {
                        MessageBox.Show("Học sinh này đang offline. Không thể khôi phục từ xa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var dialog = new PasswordInputDialog();
                    dialog.Owner = Window.GetWindow(this);
                    if (dialog.ShowDialog() == true)
                    {
                        var savedUser = QASmartTouch.Services.SettingsManager.Instance.SavedUser;
                        string expectedPassword = savedUser?.Password ?? "PassGiaoVien01";

                        if (dialog.Password == expectedPassword)
                        {
                            var app = (QASmartTouch.App)Application.Current;
                            if (app.NetworkService != null && !string.IsNullOrEmpty(student.StudentCode))
                            {
                                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                                string salt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
                                string payload = timestamp + salt + student.StudentCode;
                                string signature = ComputeHmacSha256(payload, dialog.Password);
                                string secureCommand = $"CMD|RESTORE_DEFAULTS|{timestamp}|{signature}";

                                await app.NetworkService.SendToStudentAsync(student.StudentCode, secureCommand);
                                Log.Information("Teacher triggered secure restore default config for student: {Code}", student.StudentCode);
                                MessageBox.Show($"Đã phát lệnh khôi phục cài đặt gốc an toàn tới trạm {student.Name} ({student.PCName}). Trạm học sinh số tự động xác thᱱc chữ ký và khởi động lại.", "Khôi phục cài đặt gốc", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Mật khẩu Giáo viên không chính xác. Yêu c���u khôi phục bị từ chối!", "Xác thᱱc thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }
    }

    public class PasswordInputDialog : Window
    {
        private PasswordBox _passwordBox;
        public string Password { get; private set; } = string.Empty;

        public PasswordInputDialog()
        {
            Title = "Xác nhận mật khẩu Giáo viên";
            Width = 350;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            
            var grid = new Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var label = new TextBlock 
            { 
                Text = "Nhập mật khẩu Giáo viên để tiếp tục:", 
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            _passwordBox = new PasswordBox { Height = 25 };
            Grid.SetRow(_passwordBox, 1);
            grid.Children.Add(_passwordBox);

            var stack = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            
            var btnOk = new Button { Content = "Xác nhận", Width = 80, Height = 25, IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
            btnOk.Click += (s, e) => { Password = _passwordBox.Password; DialogResult = true; Close(); };
            
            var btnCancel = new Button { Content = "Hủy", Width = 80, Height = 25, IsCancel = true };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            stack.Children.Add(btnOk);
            stack.Children.Add(btnCancel);
            
            Grid.SetRow(stack, 2);
            grid.Children.Add(stack);

            Content = grid;
        }
    }
}
