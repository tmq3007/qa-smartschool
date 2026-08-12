using QASmartClass.Data;
using QASmartClass.Services;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QASmartClass.ParentPortal.Views
{
    public partial class ParentLoginPage : Page
    {
        private readonly AppDbContext _db;
        
        /// <summary>Event khi login thành công, tr? v? Student dă xác th?c</summary>
        public event Action<Student>? LoginSuccess;

        public ParentLoginPage() : this(QASmartClass.Services.AppServices.Database ?? new AppDbContext())
        {
        }

        public ParentLoginPage(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            Loaded += (_, _) =>
            {
                LoadTestAccounts();
                ApplySecuritySettings();
                ApplyBrandingConfig();
            };

            // Nạp hình nền phụ huynh nếu có
            var brush = LoadFileSafeImageBrush("login_student_bg.png");
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
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    return new ImageBrush(bitmap)
                    {
                        Stretch = Stretch.UniformToFill,
                        AlignmentX = AlignmentX.Center,
                        AlignmentY = AlignmentY.Center
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Không thể tải hình nền {FileName}: {Error}", fileName, ex.Message);
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
                        var brandColor = (Color)ColorConverter.ConvertFromString(config.BrandColor);
                        LoginAppName.Foreground = new SolidColorBrush(brandColor);
                        if (LoginIcon != null)
                            LoginIcon.Fill = new SolidColorBrush(brandColor);
                    }
                    catch { /* Keep default color if conversion fails */ }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Login Branding] Parent error: {ex.Message}");
            }
        }

        private string GetSetting(string id, string defaultValue)
        {
            var setting = _db.SystemSettings.FirstOrDefault(s => s.Id == id);
            if (setting == null)
            {
                setting = new SystemSetting { Id = id, Value = defaultValue, Category = "ParentPortal", LastUpdated = DateTime.Now };
                _db.SystemSettings.Add(setting);
                try { _db.SaveChanges(); } catch {}
            }
            return setting.Value;
        }

        private void ApplySecuritySettings()
        {
            try
            {
                var secureLevel = GetSetting("ParentPortal_AuthSecureLevel", "Simple");
                if (secureLevel.Equals("High", StringComparison.OrdinalIgnoreCase))
                {
                    PnlParentPin.Visibility = Visibility.Visible;
                    TxtGuidanceStep3.Text = "3. Nhập Mã PIN/Mật khẩu phụ huynh.";
                }
                else
                {
                    PnlParentPin.Visibility = Visibility.Collapsed;
                    TxtGuidanceStep3.Text = "3. Nhấn 'Đăng Nhập' để truy cập.";
                }
            }
            catch (Exception ex)
            {
                Log.Warning("ParentLogin ApplySecuritySettings error: {Err}", ex.Message);
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Text = "";

            string code = TxtStudentCode.Text.Trim();
            string phone = TxtParentPhone.Text.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                TxtError.Text = "Vui lòng nhập mã học sinh.";
                return;
            }
            if (string.IsNullOrWhiteSpace(phone))
            {
                TxtError.Text = "Vui lòng nhập số điện thoại phụ huynh.";
                return;
            }

            var secureLevel = GetSetting("ParentPortal_AuthSecureLevel", "Simple");
            bool isHighSecure = secureLevel.Equals("High", StringComparison.OrdinalIgnoreCase);
            string pin = PswParentPin.Password;

            if (isHighSecure && string.IsNullOrWhiteSpace(pin))
            {
                TxtError.Text = "Vui lòng nhập mã PIN/Mật khẩu phụ huynh.";
                return;
            }

            try
            {
                var student = _db.Students.FirstOrDefault(s => s.StudentCode == code);
                if (student == null)
                {
                    TxtError.Text = "Không tìm thấy học sinh với mã này.";
                    return;
                }

                // Validate phone (simple check)
                if (!string.IsNullOrEmpty(student.ParentPhone) && student.ParentPhone != phone)
                {
                    TxtError.Text = "Số điện thoại không khớp với hồ sơ.";
                    Log.Warning("ParentLogin: Phone mismatch for {Code}", code);
                    return;
                }

                // Validate PIN/Password if high security is active
                if (isHighSecure)
                {
                    bool isPasswordValid = false;
                    if (!string.IsNullOrEmpty(student.PasswordHash))
                    {
                        isPasswordValid = QASmartTouch.Services.AuthenticationService.VerifyPassword(pin, student.PasswordHash);
                    }

                    if (!isPasswordValid)
                    {
                        TxtError.Text = "Mã PIN/Mật khẩu phụ huynh không đúng.";
                        Log.Warning("ParentLogin: PIN mismatch for {Code}", code);
                        return;
                    }
                }

                Log.Information("ParentLogin: Success for {Code} ({Name})", code, student.FullName);

                // Save last selected test user if in test mode
                if (QASmartTouch.Services.AppSettings.RunningMode == "Test")
                {
                    QASmartTouch.Services.AppSettings.LastTestUserParent = code;
                    QASmartTouch.Services.AppSettings.Save();
                }

                LoginSuccess?.Invoke(student);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ParentLogin error");
                TxtError.Text = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại.";
            }
        }

        // --- Master Login & Test Mode Helpers ---
        private class ParentTestAccount
        {
            public string StudentCode { get; set; } = "";
            public string ParentPhone { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }

        private void LoadTestAccounts()
        {
            if (QASmartTouch.Services.AppSettings.RunningMode != "Test")
            {
                PnlTestAccounts.Visibility = Visibility.Collapsed;
                return;
            }

            PnlTestAccounts.Visibility = Visibility.Visible;
            System.Collections.Generic.List<Student> parentAccounts = new System.Collections.Generic.List<Student>();

            try
            {
                parentAccounts = _db.Students
                    .Where(s => !string.IsNullOrEmpty(s.ParentPhone))
                    .Take(20)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Warning("Error loading test parents: {Err}", ex.Message);
            }

            if (parentAccounts == null || parentAccounts.Count == 0)
            {
                parentAccounts = new System.Collections.Generic.List<Student>
                {
                    new Student { StudentCode = "HS001", FullName = "Nguyễn Văn An", ParentPhone = "0901234567", ParentName = "PH An" }
                };
            }

            var displayList = parentAccounts.Select(s => new ParentTestAccount
            {
                StudentCode = s.StudentCode,
                ParentPhone = s.ParentPhone,
                DisplayName = $"PH {s.FullName} ({s.StudentCode} - {s.ParentPhone})"
            }).ToList();

            CboTestAccounts.ItemsSource = displayList;
            CboTestAccounts.DisplayMemberPath = "DisplayName";

            string lastUser = QASmartTouch.Services.AppSettings.LastTestUserParent;
            if (!string.IsNullOrEmpty(lastUser))
            {
                var match = displayList.Find(item => item.StudentCode == lastUser);
                if (match != null)
                {
                    CboTestAccounts.SelectedItem = match;
                }
                else
                {
                    CboTestAccounts.SelectedIndex = 0;
                }
            }
            else
            {
                CboTestAccounts.SelectedIndex = 0;
            }
        }

        private void CboTestAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboTestAccounts.SelectedItem is ParentTestAccount selected)
            {
                TxtStudentCode.Text = selected.StudentCode;
                TxtParentPhone.Text = selected.ParentPhone;
            }
        }
    }
}
