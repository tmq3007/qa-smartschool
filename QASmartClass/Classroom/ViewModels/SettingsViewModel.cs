using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using QASmartClass.Shared;
using QASmartClass.Data;
using QASmartTouch.Services;
using Microsoft.EntityFrameworkCore;

namespace QASmartClass.Classroom.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private static readonly string SettingsFilePath = QASmartClass.Services.AppPaths.ClassroomSettingsFile;

        // General settings
        [ObservableProperty] private string theme = "Light";
        [ObservableProperty] private string language = "vi";
        [ObservableProperty] private int fontSize = 14;
        [ObservableProperty] private string graphicsQuality = "Auto";

        // Network settings
        [ObservableProperty] private int tcpPort = 29877;
        [ObservableProperty] private bool autoReconnect = true;
        [ObservableProperty] private int maxStudents = 35;

        // Master Broadcast Settings
        [ObservableProperty] private int broadcastUdpHeartbeatTimeout = 10;
        [ObservableProperty] private bool broadcastEnableScreenExclusion = true;
        [ObservableProperty] private bool broadcastAllowTurboMode = true;
        [ObservableProperty] private bool broadcastTurboDisableExclusion = true;
        [ObservableProperty] private string broadcastTurboStudentBg = "FlatBlack";

        // Info Display
        [ObservableProperty] private string dbSizeText = "—";
        [ObservableProperty] private double dbBarRatio = 0;
        [ObservableProperty] private double dbBarWidth = 4;
        [ObservableProperty] private string cacheSizeText = "—";
        [ObservableProperty] private double cacheBarRatio = 0;
        [ObservableProperty] private double cacheBarWidth = 4;
        [ObservableProperty] private string countLessons = "—";
        [ObservableProperty] private string countStudents = "—";
        [ObservableProperty] private string countQuizzes = "—";
        [ObservableProperty] private string countEvents = "—";
        
        // Net Status
        [ObservableProperty] private string netStatus = "Trạng thái: Chưa hoạt động";
        [ObservableProperty] private string netIp = "IP: —";
        [ObservableProperty] private string netStatusColor = "#F44336"; // Default red
        [ObservableProperty] private string netStatusBgColor = "#FFEBEE"; // Light red
        [ObservableProperty] private string netStatusTextColor = "#C62828"; // Dark red
        [ObservableProperty] private string netIpColor = "#E57373"; // Light-dark red

        // Teacher Account
        [ObservableProperty] private string teacherName = "";
        [ObservableProperty] private string teacherSubject = "";
        [ObservableProperty] private string teacherSchool = "";
        [ObservableProperty] private string teacherTitle = "GV";
        
        [ObservableProperty] private string currentPassword = "";
        [ObservableProperty] private string newPassword = "";
        [ObservableProperty] private string confirmPassword = "";
        [ObservableProperty] private string passwordMsg = "";

        // API Key
        private string _actualApiKey = "";
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ApiKey))]
        [NotifyPropertyChangedFor(nameof(IsApiKeyReadOnly))]
        [NotifyPropertyChangedFor(nameof(ApiKeyVisibilityIcon))]
        private bool isApiKeyVisible = false;

        public bool IsApiKeyReadOnly => !IsApiKeyVisible;

        public string ApiKeyVisibilityIcon => IsApiKeyVisible ? "🔒" : "👁️";

        public string ApiKey
        {
            get
            {
                if (IsApiKeyVisible)
                {
                    return _actualApiKey;
                }
                else
                {
                    return string.IsNullOrEmpty(_actualApiKey) ? "" : new string('•', 24);
                }
            }
            set
            {
                if (IsApiKeyVisible)
                {
                    if (_actualApiKey != value)
                    {
                        _actualApiKey = value;
                        OnPropertyChanged(nameof(ApiKey));
                    }
                }
            }
        }

        [ObservableProperty] private string apiStatus = "Chưa kiểm tra";

        // School Config
        [ObservableProperty] private string schoolYear = "2025-2026";
        [ObservableProperty] private string defaultSemester = "HK2";
        [ObservableProperty] private string rosterSummary = "Đang tải...";
        [ObservableProperty] private string formulaPreview = "TB = ...";

        public ObservableCollection<GradeTypeMaster> GradeTypes { get; } = new();
        public ObservableCollection<ClassRoster> RosterRows { get; } = new();
        public ObservableCollection<RoleCardViewModel> Roles { get; } = new();

        [ObservableProperty] private string currentRoleIcon = "";
        [ObservableProperty] private string currentRoleName = "";
        [ObservableProperty] private string currentRoleDesc = "";

        // Master Upgrades
        [ObservableProperty] private bool enableAdminLock = false;
        [ObservableProperty] private string customDbPath = "";
        [ObservableProperty] private string selectedCamera = "";
        [ObservableProperty] private string selectedMicrophone = "";
        public ObservableCollection<string> Cameras { get; } = new();
        public ObservableCollection<string> Microphones { get; } = new();

        public SettingsViewModel()
        {
        }

        public void LoadSettings()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var saved = JsonSerializer.Deserialize<ClassroomSettings>(json) ?? new ClassroomSettings();
                    
                    Theme = saved.Theme;
                    Language = saved.Language;
                    FontSize = saved.FontSize;
                    GraphicsQuality = saved.GraphicsQuality ?? "Auto";
                    AppSettings.GraphicsQuality = GraphicsQuality;
                    TcpPort = saved.TcpPort;
                    MaxStudents = saved.MaxStudents;
                    EnableAdminLock = saved.EnableAdminLock;
                    CustomDbPath = QASmartClass.Services.AppPaths.DataDirOverride ?? "";
                    SelectedCamera = saved.DefaultCameraName;
                    SelectedMicrophone = saved.DefaultMicName;
                    QASmartClass.Shared.LanguageManager.SetLanguage(Language);
                    Application.Current.Resources["GlobalFontSize"] = (double)FontSize;
                }

                RefreshStorageInfo();
                LoadMediaDevices();
                LoadDbStats();
                LoadTeacherProfile();
                LoadApiKey();
                LoadGradeTypes();
                LoadRosterRows();
                LoadRoles();

                // Load master settings from database
                AppSettings.LoadFromDatabase(app.Database);
                BroadcastUdpHeartbeatTimeout = AppSettings.Broadcast_UdpHeartbeatTimeout;
                BroadcastEnableScreenExclusion = AppSettings.Broadcast_EnableScreenExclusion;
                BroadcastAllowTurboMode = AppSettings.Broadcast_AllowTurboMode;
                BroadcastTurboDisableExclusion = AppSettings.Broadcast_TurboDisableExclusion;
                BroadcastTurboStudentBg = AppSettings.Broadcast_TurboStudentBg;

                var net = app.NetworkService;
                if (net != null && !string.IsNullOrEmpty(net.ServerIP))
                {
                    if (net.IsBroadcasting)
                    {
                        NetStatus = "Trạng thái: Đang hoạt động";
                        NetStatusColor = "#4CAF50";
                        NetStatusBgColor = "#E8F5E9";
                        NetStatusTextColor = "#2E7D32";
                        NetIpColor = "#66BB6A";
                    }
                    else
                    {
                        NetStatus = "Trạng thái: Sẵn sàng";
                        NetStatusColor = "#FFC107";
                        NetStatusBgColor = "#FFF8E1";
                        NetStatusTextColor = "#E65100";
                        NetIpColor = "#FFB74D";
                    }
                    NetIp = $"IP: {net.ServerIP} | Cổng: {TcpPort}";
                }
                else
                {
                    NetStatus = "Trạng thái: Chưa hoạt động";
                    NetStatusColor = "#F44336";
                    NetStatusBgColor = "#FFEBEE";
                    NetStatusTextColor = "#C62828";
                    NetIpColor = "#E57373";
                    NetIp = "IP: —";
                }
            }
            catch (Exception ex)
            {
                Log.Warning("LoadSettings error: {Err}", ex.Message);
            }
        }

        [RelayCommand]
        public void SaveGeneralSettings()
        {
            try
            {
                var settings = new ClassroomSettings
                {
                    Theme = Theme,
                    Language = Language,
                    FontSize = FontSize,
                    GraphicsQuality = GraphicsQuality,
                    TcpPort = TcpPort,
                    AutoReconnect = AutoReconnect,
                    MaxStudents = MaxStudents,
                    EnableAdminLock = EnableAdminLock,
                    CustomDbPath = CustomDbPath,
                    DefaultCameraName = SelectedCamera,
                    DefaultMicName = SelectedMicrophone,
                    SavedAt = DateTime.Now
                };

                Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
                AppSettings.GraphicsQuality = GraphicsQuality;
                AppSettings.Save();
                QASmartClass.Shared.LanguageManager.SetLanguage(Language);
                Application.Current.Resources["GlobalFontSize"] = (double)FontSize;

                // Save Master Settings to AppSettings and SQLite Database
                AppSettings.Broadcast_UdpHeartbeatTimeout = BroadcastUdpHeartbeatTimeout;
                AppSettings.Broadcast_EnableScreenExclusion = BroadcastEnableScreenExclusion;
                AppSettings.Broadcast_AllowTurboMode = BroadcastAllowTurboMode;
                AppSettings.Broadcast_TurboDisableExclusion = BroadcastTurboDisableExclusion;
                AppSettings.Broadcast_TurboStudentBg = BroadcastTurboStudentBg;

                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;

                var timeoutSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_UdpHeartbeatTimeout");
                if (timeoutSetting == null)
                {
                    timeoutSetting = new SystemSetting { Id = "Broadcast_UdpHeartbeatTimeout", Category = "Broadcast" };
                    db.SystemSettings.Add(timeoutSetting);
                }
                timeoutSetting.Value = BroadcastUdpHeartbeatTimeout.ToString();
                timeoutSetting.LastUpdated = DateTime.Now;

                var exclusionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_EnableScreenExclusion");
                if (exclusionSetting == null)
                {
                    exclusionSetting = new SystemSetting { Id = "Broadcast_EnableScreenExclusion", Category = "Broadcast" };
                    db.SystemSettings.Add(exclusionSetting);
                }
                exclusionSetting.Value = BroadcastEnableScreenExclusion.ToString().ToLower();
                exclusionSetting.LastUpdated = DateTime.Now;

                var allowTurboSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_AllowTurboMode");
                if (allowTurboSetting == null)
                {
                    allowTurboSetting = new SystemSetting { Id = "Broadcast_AllowTurboMode", Category = "Broadcast" };
                    db.SystemSettings.Add(allowTurboSetting);
                }
                allowTurboSetting.Value = BroadcastAllowTurboMode.ToString().ToLower();
                allowTurboSetting.LastUpdated = DateTime.Now;

                var turboExSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_TurboDisableExclusion");
                if (turboExSetting == null)
                {
                    turboExSetting = new SystemSetting { Id = "Broadcast_TurboDisableExclusion", Category = "Broadcast" };
                    db.SystemSettings.Add(turboExSetting);
                }
                turboExSetting.Value = BroadcastTurboDisableExclusion.ToString().ToLower();
                turboExSetting.LastUpdated = DateTime.Now;

                var turboBgSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Broadcast_TurboStudentBg");
                if (turboBgSetting == null)
                {
                    turboBgSetting = new SystemSetting { Id = "Broadcast_TurboStudentBg", Category = "Broadcast" };
                    db.SystemSettings.Add(turboBgSetting);
                }
                turboBgSetting.Value = BroadcastTurboStudentBg;
                turboBgSetting.LastUpdated = DateTime.Now;

                db.SaveChanges();

                // Broadcast settings update to active student clients
                if (app.NetworkService?.IsBroadcasting == true)
                {
                    string configCmd = $"CMD|SESSION_CONFIG|Broadcast_UdpHeartbeatTimeout={BroadcastUdpHeartbeatTimeout}|Broadcast_EnableScreenExclusion={BroadcastEnableScreenExclusion}|Broadcast_TurboMode={AppSettings.Broadcast_TurboMode}|Broadcast_AllowTurboMode={BroadcastAllowTurboMode}|Broadcast_TurboDisableExclusion={BroadcastTurboDisableExclusion}|Broadcast_TurboStudentBg={BroadcastTurboStudentBg}";
                    _ = app.NetworkService.SendCommandAsync(configCmd);
                }

                Log.Information("Settings and Master Settings saved to DB & JSON. Broadcasted configCmd.");
            }
            catch (Exception ex)
            {
                Log.Warning("SaveSettings error: {Err}", ex.Message);
            }
        }

        private void LoadDbStats()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                if (db != null)
                {
                    CountLessons = db.Lessons.Count().ToString();
                    CountStudents = db.Students.Count().ToString();
                    CountQuizzes = db.Quizzes.Count().ToString();
                    CountEvents = db.EventLogs.Count().ToString();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("LoadDbStats error: {Err}", ex.Message);
            }
        }

        private void RefreshStorageInfo()
        {
            try
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                long dbSize = 0;
                if (File.Exists(dbPath)) dbSize = new FileInfo(dbPath).Length;
                DbSizeText = FormatBytes(dbSize);
                DbBarRatio = Math.Min((double)dbSize / (50 * 1024 * 1024), 1.0);
                DbBarWidth = Math.Max(DbBarRatio * 500, 4);

                long cacheSize = 0;
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (Directory.Exists(logDir))
                {
                    foreach (var f in Directory.GetFiles(logDir, "*", SearchOption.AllDirectories))
                    {
                        try { cacheSize += new FileInfo(f).Length; } catch { }
                    }
                }
                CacheSizeText = FormatBytes(cacheSize);
                CacheBarRatio = Math.Min((double)cacheSize / (50 * 1024 * 1024), 1.0);
                CacheBarWidth = Math.Max(CacheBarRatio * 500, 4);
            }
            catch (Exception ex) { Log.Warning("RefreshStorageInfo error: {Err}", ex.Message); }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        [RelayCommand]
        private void ClearCache()
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            int deleted = 0;
            long freed = 0;
            if (Directory.Exists(logDir))
            {
                foreach (var f in Directory.GetFiles(logDir, "*.log"))
                {
                    try
                    {
                        freed += new FileInfo(f).Length;
                        File.Delete(f);
                        deleted++;
                    }
                    catch { }
                }
            }
            MessageBox.Show($"Đã xóa {deleted} tệp cache ({FormatBytes(freed)})!\nThư mục logs đã được dọn sạch.",
                "Xóa Cache", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshStorageInfo();
            Log.Information("Cache cleared: {Count} files, {Size} freed", deleted, FormatBytes(freed));
        }

        [RelayCommand]
        private void OpenDbFolder()
        {
            var folder = QASmartClass.Services.AppPaths.RootDir;
            Directory.CreateDirectory(folder);
            System.Diagnostics.Process.Start("explorer.exe", folder);
        }

        [RelayCommand]
        private void ResetDb()
        {
            var result = MessageBox.Show(
                "Bạn chắc chắn muốn XÓA TOÀN BỘ dữ liệu?\n\nThao tác này không thể hoàn tác!",
                "Xác nhận Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    var db = app.Database;
                    
                    if (db != null)
                    {
                        try { db.Database.CloseConnection(); } catch { }
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (File.Exists(dbPath)) File.Delete(dbPath);
                    
                    MessageBox.Show("Đã reset Cơ sở dữ liệu thành công.\nỨng dụng sẽ tự động khởi động lại để tạo mới CSDL.",
                        "Reset thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    Log.Warning("Database reset by user");

                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        System.Diagnostics.Process.Start(exePath);
                        Application.Current.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadTeacherProfile()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                var teacher = db.TeacherProfiles.FirstOrDefault();
                if (teacher != null)
                {
                    TeacherName = teacher.FullName;
                    TeacherSubject = teacher.Subject;
                    TeacherSchool = teacher.School;
                    TeacherTitle = teacher.Title;
                }
            }
            catch (Exception ex) { Log.Warning("LoadTeacherProfile error: {Err}", ex.Message); }
        }

        [RelayCommand]
        private void SaveAccount()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                var teacher = db.TeacherProfiles.FirstOrDefault();

                if (teacher == null)
                {
                    teacher = new TeacherProfile();
                    db.TeacherProfiles.Add(teacher);
                }

                teacher.FullName = TeacherName?.Trim();
                teacher.Subject = TeacherSubject?.Trim();
                teacher.School = TeacherSchool?.Trim();
                teacher.Title = TeacherTitle?.Trim();
                teacher.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                MessageBox.Show("Đã lưu thông tin giáo viên!", "Lưu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ChangePassword()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                var teacher = db.TeacherProfiles.FirstOrDefault();

                bool isCorrect = false;
                if (teacher != null)
                {
                    if (!string.IsNullOrEmpty(teacher.PasswordHash))
                    {
                        isCorrect = QASmartTouch.Services.AuthenticationService.VerifyPassword(CurrentPassword, teacher.PasswordHash);
                    }
                    else
                    {
                        isCorrect = (CurrentPassword == teacher.TeacherPassword);
                    }
                }
                else
                {
                    isCorrect = (CurrentPassword == "admin");
                }

                if (!isCorrect)
                {
                    PasswordMsg = "Mật khẩu hiện tại không đúng!";
                    return;
                }

                if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 4)
                {
                    PasswordMsg = "Mật khẩu mới phải có ít nhất 4 ký tự!";
                    return;
                }

                if (NewPassword != ConfirmPassword)
                {
                    PasswordMsg = "Xác nhận mật khẩu không khớp!";
                    return;
                }

                if (teacher == null)
                {
                    teacher = new TeacherProfile();
                    db.TeacherProfiles.Add(teacher);
                }
                teacher.PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword(NewPassword);
                teacher.TeacherPassword = "";
                teacher.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                CurrentPassword = "";
                NewPassword = "";
                ConfirmPassword = "";
                PasswordMsg = "Đổi mật khẩu thành công!";
            }
            catch (Exception ex)
            {
                PasswordMsg = $"Lỗi: {ex.Message}";
            }
        }

        private void LoadApiKey()
        {
            try
            {
                string storedKey = QASmartClass.Properties.Settings.Default.GoogleCloudVisionApiKey ?? "";
                string decryptedKey = QASmartClass.Utilities.CryptoHelper.DecryptWithDpapi(storedKey);
                if (string.IsNullOrEmpty(decryptedKey) && !string.IsNullOrEmpty(storedKey))
                {
                    _actualApiKey = storedKey; // Fallback for legacy plaintext key
                }
                else
                {
                    _actualApiKey = decryptedKey;
                }
                ApiStatus = string.IsNullOrWhiteSpace(_actualApiKey) ? "⚠️ API key trống" : "✅ API key đã được cấu hình";
                OnPropertyChanged(nameof(ApiKey));
            }
            catch (Exception ex) { Log.Warning("LoadApiKey error: {Err}", ex.Message); }
        }

        [RelayCommand]
        private void SaveApiKey()
        {
            try
            {
                QASmartClass.Properties.Settings.Default.GoogleCloudVisionApiKey = QASmartClass.Utilities.CryptoHelper.EncryptWithDpapi(_actualApiKey?.Trim());
                QASmartClass.Properties.Settings.Default.Save();
                ApiStatus = string.IsNullOrWhiteSpace(_actualApiKey) ? "⚠️ API key trống" : "✅ API key đã lưu thành công";
                MessageBox.Show("Đã lưu API key thành công!\n\nKhởi động lại ứng dụng để áp dụng thay đổi.", "Lưu API Key", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        [RelayCommand]
        private void ToggleApiKeyVisibility()
        {
            IsApiKeyVisible = !IsApiKeyVisible;
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task TestApiConnection()
        {
            if (string.IsNullOrWhiteSpace(_actualApiKey))
            {
                ApiStatus = "⚠️ Vui lòng nhập API key trước";
                return;
            }
            ApiStatus = "🔄 Đang kiểm tra...";
            try
            {
                var testService = new HandwritingRecognitionService(_actualApiKey);
                if (!testService.IsInitialized)
                {
                    ApiStatus = "❌ Không thể khởi tạo service";
                    return;
                }
                bool connected = await testService.TestConnectionAsync();
                ApiStatus = connected ? "✅ Kết nối thành công! API hoạt động bình thường" : "❌ Kết nối thất bại.";
            }
            catch (Exception ex)
            {
                ApiStatus = $"❌ Lỗi: {ex.Message}";
            }
        }

        private void LoadGradeTypes()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var types = app.Database.GradeTypeMasters.OrderBy(g => g.SortOrder).ToList();
                GradeTypes.Clear();
                foreach (var t in types) GradeTypes.Add(t);
                UpdateFormulaPreview();
            }
            catch (Exception ex) { Log.Warning("LoadGradeTypes error: {Err}", ex.Message); }
        }

        [RelayCommand]
        private void AddGradeType()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                int nextOrder = db.GradeTypeMasters.Any() ? db.GradeTypeMasters.Max(g => g.SortOrder) + 1 : 1;
                var gt = new GradeTypeMaster
                {
                    Code = $"Custom{nextOrder}",
                    DisplayName = "Loại điểm mới",
                    ShortName = "?",
                    Weight = 1,
                    MaxAttempts = 1,
                    SortOrder = nextOrder,
                    IsActive = true
                };
                db.GradeTypeMasters.Add(gt);
                db.SaveChanges();
                LoadGradeTypes();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        [RelayCommand]
        private void SaveGradeType(GradeTypeMaster gt)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                app.Database.SaveChanges();
                UpdateFormulaPreview();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        [RelayCommand]
        private void DeleteGradeType(GradeTypeMaster gt)
        {
            if (gt == null) return;
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                bool hasGrades = db.StudentGrades.Any(sg => sg.GradeTypeId == gt.Id);
                if (hasGrades)
                {
                    if (MessageBox.Show($"Loại điểm \"{gt.DisplayName}\" đã có dữ liệu.\nVô hiệu hóa thay vì xóa?", "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        gt.IsActive = false;
                        db.SaveChanges();
                        UpdateFormulaPreview();
                    }
                    return;
                }
                if (MessageBox.Show($"Xóa loại điểm \"{gt.DisplayName}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    db.GradeTypeMasters.Remove(gt);
                    db.SaveChanges();
                    LoadGradeTypes();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void UpdateFormulaPreview()
        {
            var activeTypes = GradeTypes.Where(t => t.IsActive).ToList();
            if (activeTypes.Count == 0)
            {
                FormulaPreview = "(Không có loại điểm hoạt động)";
                return;
            }
            var numerator = string.Join(" + ", activeTypes.Select(t =>
            {
                if (t.MaxAttempts > 1) return string.Join(" + ", Enumerable.Range(1, t.MaxAttempts).Select(a => $"{t.ShortName}{a}×{t.Weight}"));
                return $"{t.ShortName}×{t.Weight}";
            }));
            int totalWeight = activeTypes.Sum(t => t.Weight * t.MaxAttempts);
            FormulaPreview = $"TB = ({numerator}) / {totalWeight}";
        }

        private void LoadRosterRows()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var rosters = app.Database.ClassRosters.Where(r => r.IsActive).OrderBy(r => r.GradeLevel).ThenBy(r => r.ClassName).ToList();
                RosterRows.Clear();
                foreach (var r in rosters) RosterRows.Add(r);
                RosterSummary = $"🏫 {rosters.Count} lớp • Khối: {string.Join(", ", rosters.Select(r => r.GradeLevel).Distinct().OrderBy(x => x))}";
            }
            catch (Exception ex) { Log.Warning("LoadRosterRows error: {Err}", ex.Message); }
        }

        [RelayCommand]
        private void AddRoster()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                db.ClassRosters.Add(new ClassRoster
                {
                    ClassName = "Lớp mới",
                    GradeLevel = "10",
                    SchoolYear = SchoolYear,
                    Semester = DefaultSemester,
                    Subject = "",
                    IsActive = true
                });
                db.SaveChanges();
                LoadRosterRows();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        [RelayCommand]
        private void SaveSchoolConfig()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                app.Database.SaveChanges();
                LoadRosterRows();
                MessageBox.Show("Đã lưu cấu hình trường học!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        [RelayCommand]
        private void DeleteRoster(ClassRoster roster)
        {
            if (roster == null) return;
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var db = app.Database;
                bool hasStudents = db.ClassRosterStudents.Any(rs => rs.RosterId == roster.Id);
                bool hasGrades = db.StudentGrades.Any(sg => sg.RosterId == roster.Id);

                if (hasStudents || hasGrades)
                {
                    if (MessageBox.Show($"Lớp \"{roster.ClassName}\" có dữ liệu.\nVô hiệu hóa thay vì xóa?", "Không thể xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        roster.IsActive = false;
                        db.SaveChanges();
                        LoadRosterRows();
                    }
                    return;
                }
                if (MessageBox.Show($"Xóa lớp \"{roster.ClassName}\"?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    db.ClassRosters.Remove(roster);
                    db.SaveChanges();
                    LoadRosterRows();
                }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void LoadRoles()
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var currentRole = app.UserRoleService.CurrentRole;
                var currentInfo = UserRoleService.GetRoleInfo(currentRole);

                CurrentRoleIcon = currentInfo.Icon;
                CurrentRoleName = currentInfo.Name;
                CurrentRoleDesc = currentInfo.Description;

                Roles.Clear();
                foreach (var info in UserRoleInfo.All)
                {
                    Roles.Add(new RoleCardViewModel
                    {
                        Info = info,
                        IsSelected = info.Role == currentRole,
                        ChangeRoleCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => ChangeRole(info.Role))
                    });
                }
            }
            catch (Exception ex) { Log.Warning("LoadRoles error: {Err}", ex.Message); }
        }

        private void ChangeRole(UserRole role)
        {
            var app = (QASmartTouch.App)Application.Current;
            if (role == app.UserRoleService.CurrentRole) return;

            var info = UserRoleService.GetRoleInfo(role);
            if (MessageBox.Show($"Chuyển vai trò sang: {info.Icon} {info.Name}?\n\nỨng dụng sẽ khởi động lại để áp dụng.", "Thay đổi vai trò sử dụng", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                app.UserRoleService.SaveRole(role);
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    System.Diagnostics.Process.Start(exePath);
                    Application.Current.Shutdown();
                }
            }
        }

        private void LoadMediaDevices()
        {
            Cameras.Clear();
            Cameras.Add("Không sử dụng Camera");
            try
            {
                var discovery = new QASmartTouch.Services.Camera.CameraDiscoveryService();
                var cameraList = discovery.GetAllCameras();
                foreach (var cam in cameraList)
                {
                    Cameras.Add(cam.FriendlyName);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load cameras in settings: {Err}", ex.Message);
            }

            LoadMicrophones();

            // Load saved profile
            try
            {
                var cameraConfig = new QASmartTouch.Services.Camera.CameraConfigService();
                var profile = cameraConfig.Load();
                if (!string.IsNullOrEmpty(profile.SelectedCameraId))
                {
                    var discovery = new QASmartTouch.Services.Camera.CameraDiscoveryService();
                    var cam = discovery.GetCameraById(profile.SelectedCameraId);
                    if (cam != null)
                    {
                        SelectedCamera = cam.FriendlyName;
                    }
                }
                else
                {
                    SelectedCamera = "Không sử dụng Camera";
                }
            }
            catch { SelectedCamera = "Không sử dụng Camera"; }

            // Load saved microphone
            try
            {
                string cameraConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CameraConfigDemo");
                string micFile = Path.Combine(cameraConfigDir, "microphone_config.txt");
                if (File.Exists(micFile))
                {
                    string savedMic = File.ReadAllText(micFile).Trim();
                    if (Microphones.Contains(savedMic))
                    {
                        SelectedMicrophone = savedMic;
                    }
                    else
                    {
                        SelectedMicrophone = "Thiết bị mặc định (Default Audio)";
                    }
                }
                else
                {
                    SelectedMicrophone = "Thiết bị mặc định (Default Audio)";
                }
            }
            catch { SelectedMicrophone = "Thiết bị mặc định (Default Audio)"; }
        }

        private void LoadMicrophones()
        {
            Microphones.Clear();
            Microphones.Add("Thiết bị mặc định (Default Audio)");
            try
            {
                int count = NAudio.Wave.WaveIn.DeviceCount;
                for (int i = 0; i < count; i++)
                {
                    var caps = NAudio.Wave.WaveIn.GetCapabilities(i);
                    if (!string.IsNullOrEmpty(caps.ProductName))
                    {
                        Microphones.Add(caps.ProductName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to query NAudio WaveIn devices: {Err}", ex.Message);
            }
        }

        partial void OnEnableAdminLockChanged(bool value)
        {
            if (value)
            {
                MessageBox.Show("Chức năng Admin đã bị vô hiệu hóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                enableAdminLock = false;
                OnPropertyChanged(nameof(EnableAdminLock));
            }
        }

        [RelayCommand]
        private void SetupAdminPin()
        {
            MessageBox.Show("Chức năng Admin đã bị vô hiệu hóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void BrowseDbFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Chọn thư mục lưu trữ CSDL tùy chỉnh"
            };

            if (dialog.ShowDialog() == true)
            {
                string newPath = dialog.FolderName;
                if (string.IsNullOrEmpty(newPath)) return;

                string testFile = Path.Combine(newPath, ".write_test");
                try
                {
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch (Exception)
                {
                    MessageBox.Show("Thư mục được chọn không có quyền ghi dữ liệu. Vui lòng chọn ổ đĩa khác!", "Lỗi quyền ghi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show($"Bạn có muốn di chuyển dữ liệu CSDL hiện tại sang thư mục mới:\n{newPath}?\n\nỨng dụng sẽ tự động khởi động lại sau khi hoàn tất.", "Xác nhận di chuyển", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    var db = app.Database;
                    string oldDbFile = QASmartClass.Services.AppPaths.DatabaseFile;
                    string newDbFile = Path.Combine(newPath, Path.GetFileName(oldDbFile));

                    if (db != null)
                    {
                        db.Database.CloseConnection();
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    if (File.Exists(oldDbFile))
                    {
                        File.Copy(oldDbFile, newDbFile, true);
                    }

                    string defaultSettingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QASmartClass", "Settings");
                    Directory.CreateDirectory(defaultSettingsDir);
                    string pathConfigFile = Path.Combine(defaultSettingsDir, "db_path_config.json");
                    
                    var configData = new { CustomDbPath = newPath };
                    string configJson = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(pathConfigFile, configJson);

                    CustomDbPath = newPath;

                    MessageBox.Show("Di chuyển CSDL thành công! Hệ thống sẽ khởi động lại bây giờ.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        System.Diagnostics.Process.Start(exePath);
                        Application.Current.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi di chuyển CSDL: {ex.Message}", "Lỗi di chuyển", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ExportConfig()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = "qasmartclass_config.json",
                Title = "Xuất cấu hình hệ thống"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var exportData = new ClassroomSettings
                    {
                        Theme = Theme,
                        Language = Language,
                        FontSize = FontSize,
                        GraphicsQuality = GraphicsQuality,
                        TcpPort = TcpPort,
                        AutoReconnect = AutoReconnect,
                        MaxStudents = MaxStudents,
                        EnableAdminLock = EnableAdminLock,
                        CustomDbPath = CustomDbPath,
                        DefaultCameraName = SelectedCamera,
                        DefaultMicName = SelectedMicrophone
                    };

                    string json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("Xuất cấu hình hệ thống thành công!", "Xuất cấu hình", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi xuất cấu hình", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ImportConfig()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Nhập cấu hình hệ thống"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    var imported = JsonSerializer.Deserialize<ClassroomSettings>(json);
                    if (imported != null)
                    {
                        Theme = imported.Theme;
                        Language = imported.Language;
                        FontSize = imported.FontSize;
                        GraphicsQuality = imported.GraphicsQuality ?? "Auto";
                        TcpPort = imported.TcpPort;
                        AutoReconnect = imported.AutoReconnect;
                        MaxStudents = imported.MaxStudents;
                        EnableAdminLock = imported.EnableAdminLock;
                        SelectedCamera = imported.DefaultCameraName;
                        SelectedMicrophone = imported.DefaultMicName;

                        MessageBox.Show("Nhập cấu hình thành công! Hãy nhấn 'Lưu thiết lập hệ thống' để áp dụng.", "Nhập cấu hình", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi import: {ex.Message}", "Lỗi nhập cấu hình", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void OpenStudentClient()
        {
            MessageBox.Show("Tính năng Học sinh đã bị loại bỏ.");
        }
    }

    public class RoleCardViewModel : ObservableObject
    {
        public UserRoleInfo Info { get; set; } = null!;
        public bool IsSelected { get; set; }
        public IRelayCommand ChangeRoleCommand { get; set; } = null!;
        public System.Windows.Media.Color RoleColor => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Info.Color);
        public System.Windows.Media.Color RoleBgColor => System.Windows.Media.Color.FromArgb(25, RoleColor.R, RoleColor.G, RoleColor.B);
    }

    public class ClassroomSettings
    {
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "vi";
        public int FontSize { get; set; } = 14;
        public string GraphicsQuality { get; set; } = "Auto";
        public int TcpPort { get; set; } = 29877;
        public bool AutoReconnect { get; set; } = true;
        public int MaxStudents { get; set; } = 35;
        public bool EnableAdminLock { get; set; } = false;
        public string CustomDbPath { get; set; } = "";
        public string DefaultCameraName { get; set; } = "";
        public string DefaultMicName { get; set; } = "";
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}
