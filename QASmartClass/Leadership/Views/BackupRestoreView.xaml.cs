using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.Leadership.Views
{
    public partial class BackupRestoreView : Page
    {
        private AppDbContext? _db;
        private AutoBackupService? _backupService;

        public BackupRestoreView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            _db = new AppDbContext();
            _backupService = new AutoBackupService(_db);
            LoadLogs();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _db?.Dispose();
            _db = null;
            _backupService = null;
        }

        private void LoadLogs()
        {
            if (_db == null) return;
            var logs = _db.BackupLogs.OrderByDescending(b => b.CreatedAt).Take(50).ToList();
            dgBackupLogs.ItemsSource = logs;
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            if (_backupService == null) return;
            _backupService.RunBackup("Admin");
            MessageBox.Show("Đã tạo bản sao lưu dữ liệu (Backup) thành công!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadLogs();
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (dgBackupLogs.SelectedItem is BackupLog log)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"CẢNH BÁO: Phục hồi dữ liệu sẽ ghi đè toàn bộ CSDL hiện tại bằng bản sao lưu '{log.FileName}'.\n" +
                    "Mọi dữ liệu phát sinh sau thời điểm sao lưu này sẽ bị mất.\n\n" +
                    "Bạn có chắc chắn muốn tiếp tục khôi phục?", 
                    "Xác nhận Phục hồi Hệ thống", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Tích hợp hộp thoại xác thực mật khẩu
                    var dlg = new Window
                    {
                        Title = "Xác nhận Mật khẩu Quản trị",
                        Width = 360, Height = 180,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                        ResizeMode = ResizeMode.NoResize,
                        ShowInTaskbar = false
                    };

                    var sp = new StackPanel { Margin = new Thickness(20) };
                    sp.Children.Add(new TextBlock 
                    { 
                        Text = "Vui lòng nhập mật khẩu tài khoản để xác nhận khôi phục:", 
                        Margin = new Thickness(0, 0, 0, 10),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12
                    });

                    var pb = new PasswordBox { FontSize = 14, Margin = new Thickness(0, 0, 0, 15) };
                    sp.Children.Add(pb);

                    var btnConfirm = new Button
                    {
                        Content = "Xác nhận",
                        Height = 35,
                        Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F44336")),
                        Foreground = System.Windows.Media.Brushes.White,
                        FontWeight = FontWeights.Bold,
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    
                    bool authenticated = false;
                    btnConfirm.Click += (_, __) =>
                    {
                        string enteredPassword = pb.Password;
                        var currentUser = QASmartClass.Staff.Services.StaffSession.CurrentUser;
                        if (currentUser != null && !string.IsNullOrEmpty(currentUser.PasswordHash))
                        {
                            bool match = QASmartTouch.Services.AuthenticationService.VerifyPassword(enteredPassword, currentUser.PasswordHash);
                            if (match)
                            {
                                authenticated = true;
                                dlg.Close();
                            }
                            else
                            {
                                MessageBox.Show("Mật khẩu xác nhận không chính xác!", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            // Chế độ chạy thử / Debug
                            if (enteredPassword == "Admin123" || enteredPassword == "PassGiaoVien01")
                            {
                                authenticated = true;
                                dlg.Close();
                            }
                            else
                            {
                                MessageBox.Show("Mật khẩu xác nhận không chính xác!", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    };
                    
                    sp.Children.Add(btnConfirm);
                    dlg.Content = sp;
                    dlg.ShowDialog();

                    if (!authenticated)
                    {
                        return; // Hủy nếu không xác thực được
                    }

                    try
                    {
                        var backupService = new BackupService();
                        string backupFilePath = Path.Combine(AppPaths.BackupsDir, log.FileName);
                        if (!File.Exists(backupFilePath))
                        {
                            MessageBox.Show($"Không tìm thấy tệp tin sao lưu vật lý tại: {backupFilePath}", "Lỗi vật lý", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var restoreResult = backupService.Restore(backupFilePath);
                        if (restoreResult == RestoreResult.Success)
                        {
                            MessageBox.Show(
                                "✅ Hệ thống đã phục hồi dữ liệu thành công!\n" +
                                "Ứng dụng sẽ tự động đóng lại. Vui lòng khởi động lại ứng dụng để áp dụng cấu hình dữ liệu mới.", 
                                "Khôi phục Thành công", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information);
                            
                            Application.Current.Shutdown();
                        }
                        else
                        {
                            string detail = restoreResult switch
                            {
                                RestoreResult.FileNotFound => "Không tìm thấy tệp tin sao lưu trên ổ đĩa.",
                                RestoreResult.IntegrityCheckFailed => "Tệp sao lưu bị lỗi cấu trúc SQLite vật lý (Integrity check failed).",
                                RestoreResult.ChecksumMismatch => "Chữ ký bảo mật SHA-256 không khớp, tệp sao lưu có thể đã bị thay đổi trái phép.",
                                _ => "Lỗi hệ thống không xác định."
                            };
                            MessageBox.Show($"Không thể phục hồi dữ liệu!\nChi tiết: {detail}", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi trong quá trình khôi phục: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bản ghi sao lưu trong danh sách dưới đây để phục hồi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

