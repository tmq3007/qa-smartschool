using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Cấu hình branding của ứng dụng (Icon + Tên)
    /// </summary>
    public class AppBrandingConfig
    {
        /// <summary>
        /// Cho phép hiển thị icon ở header
        /// </summary>
        [JsonPropertyName("showIcon")]
        public bool ShowIcon { get; set; } = true;

        /// <summary>
        /// Cho phép hiển thị tên ứng dụng ở header
        /// </summary>
        [JsonPropertyName("showAppName")]
        public bool ShowAppName { get; set; } = true;

        /// <summary>
        /// Tên ứng dụng hiển thị
        /// </summary>
        [JsonPropertyName("appName")]
        public string AppName { get; set; } = "QASmartTouch";

        /// <summary>
        /// Màu chủ đạo của branding
        /// </summary>
        [JsonPropertyName("brandColor")]
        public string BrandColor { get; set; } = "#2E86DE";

        /// <summary>
        /// Loại icon: "GraduationCap", "Custom", "None"
        /// </summary>
        [JsonPropertyName("iconType")]
        public string IconType { get; set; } = "GraduationCap";

        /// <summary>
        /// Đường dẫn đến icon tùy chỉnh (nếu IconType = "Custom")
        /// </summary>
        [JsonPropertyName("customIconPath")]
        public string? CustomIconPath { get; set; }

        /// <summary>
        /// Cho phép hiển thị header (thanh trên cùng)
        /// </summary>
        [JsonPropertyName("showHeader")]
        public bool ShowHeader { get; set; } = false;
    }

    /// <summary>
    /// Service quản lý branding của ứng dụng
    /// </summary>
    public class AppBrandingService
    {
        private static AppBrandingService? _instance;
        private static readonly object _lock = new();
        private const string CONFIG_FILE = "AppBranding.json";
        private const string CONFIG_FOLDER = "config";

        private AppBrandingConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Singleton instance
        /// </summary>
        public static AppBrandingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new AppBrandingService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Cấu hình branding hiện tại
        /// </summary>
        public AppBrandingConfig Config => _config;

        /// <summary>
        /// Event được raise khi branding config thay đổi
        /// </summary>
        public event EventHandler? BrandingChanged;

        private AppBrandingService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            _config = LoadConfig();
        }

        /// <summary>
        /// Load cấu hình từ file
        /// </summary>
        private AppBrandingConfig LoadConfig()
        {
            try
            {
                string configPath = GetConfigFilePath();

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<AppBrandingConfig>(json, _jsonOptions);
                    if (config != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AppBranding] Loaded config: ShowIcon={config.ShowIcon}, ShowAppName={config.ShowAppName}, AppName={config.AppName}");
                        return config;
                    }
                }

                System.Diagnostics.Debug.WriteLine("[AppBranding] Config file not found, using defaults");
                return new AppBrandingConfig();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppBranding] Error loading config: {ex.Message}");
                return new AppBrandingConfig();
            }
        }

        /// <summary>
        /// Lưu cấu hình vào file
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                EnsureConfigDirectoryExists();
                string configPath = GetConfigFilePath();
                string json = JsonSerializer.Serialize(_config, _jsonOptions);
                File.WriteAllText(configPath, json);
                System.Diagnostics.Debug.WriteLine($"[AppBranding] Config saved successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppBranding] Error saving config: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật cấu hình
        /// </summary>
        public void UpdateConfig(Action<AppBrandingConfig> updateAction)
        {
            updateAction(_config);
            SaveConfig();
            OnBrandingChanged();
        }

        /// <summary>
        /// Raise event BrandingChanged
        /// </summary>
        private void OnBrandingChanged()
        {
            BrandingChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Lấy đường dẫn file config
        /// </summary>
        private string GetConfigFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, CONFIG_FOLDER, CONFIG_FILE);
        }

        /// <summary>
        /// Đảm bảo thư mục config tồn tại
        /// </summary>
        private void EnsureConfigDirectoryExists()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configDir = Path.Combine(baseDir, CONFIG_FOLDER);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
        }

        /// <summary>
        /// Tạo file config mặc định
        /// </summary>
        public static void CreateDefaultConfig()
        {
            var service = Instance;
            service.SaveConfig();
        }
    }
}
