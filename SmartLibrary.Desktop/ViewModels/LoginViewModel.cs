using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly QrScannerService _qrScannerService;

        [ObservableProperty]
        private string _ssoUserId = "TT001";

        [ObservableProperty]
        private string _fullName = "Võ Minh Em";

        [ObservableProperty]
        private string _selectedRole = "Librarian";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isScanningQr;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private bool _isPasswordVisible = false;

        [ObservableProperty]
        private string _passwordText = "";

        [ObservableProperty]
        private string _inspirationalQuote = "";

        [ObservableProperty]
        private string _schoolName = "TRƯỜNG THPT SMART SCHOOL";

        private static readonly string[] Quotes = new[]
        {
            "\"Sách là ngọn đèn sáng bất diệt của trí tuệ loài người.\" — Không rõ tác giả",
            "\"Một cuốn sách tốt là một người bạn tốt.\" — Victor Hugo",
            "\"Đọc sách không phải để tin tất cả, mà để suy ngẫm.\" — Voltaire",
            "\"Hãy đọc, không phải vì bạn phải, mà vì bạn muốn.\" — Mark Twain",
            "\"Một căn phòng không có sách giống như một cơ thể không có linh hồn.\" — Cicero",
            "\"Sách mở rộng thế giới trước mắt bạn mà không cần rời khỏi ghế.\" — Jhumpa Lahiri",
            "\"Giáo dục là vũ khí mạnh nhất để thay đổi thế giới.\" — Nelson Mandela",
            "\"Tri thức là sức mạnh.\" — Francis Bacon",
            "\"Đọc sách cho ta sức mạnh để vượt qua mọi rào cản.\" — Malala Yousafzai",
            "\"Người không đọc sách không có lợi thế nào hơn người không biết đọc.\" — Mark Twain",
            "\"Trong sách có con đường vàng.\" — Tục ngữ Việt Nam",
            "\"Học, học nữa, học mãi.\" — V.I. Lê-nin",
            "\"Dân ta phải biết sử ta, cho tường gốc tích nước nhà Việt Nam.\" — Hồ Chí Minh",
            "\"Muốn sang thì bắc cầu Kiều, muốn con hay chữ thì yêu lấy thầy.\" — Ca dao Việt Nam",
            "\"Một người đọc sách mỗi ngày là một người giàu có mỗi ngày.\" — Khuyết danh"
        };

        public ICommand TogglePasswordVisibilityCommand { get; }

        public string[] AvailableRoles { get; } = new[] { "Student", "Teacher", "Librarian", "Admin" };

        public ICommand LoginCommand { get; }
        public ICommand LoginWithQrCommand { get; }
        public ICommand CancelQrCommand { get; }

        public Action? OnLoginSuccess { get; set; }

        public LoginViewModel(AuthService authService, QrScannerService qrScannerService)
        {
            _authService = authService;
            _qrScannerService = qrScannerService;
            LoginCommand = new AsyncRelayCommand<object>(LoginAsync);
            LoginWithQrCommand = new AsyncRelayCommand(ExecuteLoginWithQrAsync);
            CancelQrCommand = new RelayCommand(() => IsScanningQr = false);
            TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);
            InspirationalQuote = Quotes[new Random().Next(Quotes.Length)];
        }

        private async Task LoginAsync(object parameter)
        {
            try
            {
                ErrorMessage = "";
                
                string password;
                if (IsPasswordVisible)
                {
                    password = PasswordText; // TextBox visible mode
                }
                else
                {
                    var pb = parameter as System.Windows.Controls.PasswordBox;
                    password = pb?.Password ?? "";
                }


                if (string.IsNullOrWhiteSpace(SsoUserId))
                {
                    ErrorMessage = SelectedRole switch
                    {
                        "Student" => "Vui lòng nhập Mã học sinh (Mặc định: HS001)",
                        "Teacher" => "Vui lòng nhập Mã giáo viên (Mặc định: GV001)",
                        "Admin" => "Vui lòng nhập ID Quản trị (Mặc định: AD001)",
                        _ => "Vui lòng nhập ID Thủ thư (Mặc định: TT001)"
                    };
                    return;
                }

                IsBusy = true;
                
                bool success = await _authService.LoginAsync(SsoUserId, FullName, SelectedRole);
                
                if (success)
                {
                    await AuditLogService.WriteLogAsync("Đăng nhập", $"Đăng nhập vai trò {SelectedRole} thành công", true);
                    OnLoginSuccess?.Invoke();
                }
                else
                {
                    await AuditLogService.WriteLogAsync("Đăng nhập", $"Đăng nhập SsoUserId: {SsoUserId} thất bại", false);
                    ErrorMessage = "Đăng nhập thất bại. Kiểm tra lại thông tin.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                PasswordText = string.Empty;
            }
        }

        private async Task ExecuteLoginWithQrAsync()
        {
            IsScanningQr = true;
            ErrorMessage = string.Empty;

            try
            {
                // Gọi Scanner giả lập 3s
                string scannedSsoId = await _qrScannerService.ScanQrCodeAsync();
                
                // Cập nhật giao diện tự động điền
                SsoUserId = scannedSsoId;
                FullName = "Học sinh tự động đăng nhập (QR)";
                SelectedRole = "Student"; // Mặc định Mobile App của HS
                
                if (SsoUserId == "AD001" || SsoUserId == "hieutruong_01")
                {
                    SsoUserId = "AD001";
                    SelectedRole = "Admin";
                    FullName = "Hoàng Quản Trị";
                }

                // Gọi login ngay
                var success = await _authService.LoginAsync(SsoUserId, FullName, SelectedRole);
                if (success)
                {
                    await AuditLogService.WriteLogAsync("Đăng nhập QR", $"Đăng nhập bằng QR với ID {SsoUserId} thành công", true);
                    OnLoginSuccess?.Invoke();
                }
                else
                {
                    await AuditLogService.WriteLogAsync("Đăng nhập QR", $"Đăng nhập bằng QR với ID {SsoUserId} thất bại", false);
                    ErrorMessage = "QR quét thành công nhưng đăng nhập Server thất bại.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsScanningQr = false;
            }
        }

        partial void OnSelectedRoleChanged(string value)
        {
            if (value == "Librarian")
            {
                SsoUserId = "TT001";
                FullName = "Võ Minh Em";
            }
            else if (value == "Student")
            {
                SsoUserId = "HS001";
                FullName = "Nguyễn Văn An";
            }
            else if (value == "Teacher")
            {
                SsoUserId = "GV001";
                FullName = "Phạm Thị Dung";
            }
            else if (value == "Admin")
            {
                SsoUserId = "AD001";
                FullName = "Hoàng Quản Trị";
            }
        }
    }
}
