using System.IO;
using System.Text.Json;
using QASmartTouch.Models;

namespace QASmartTouch.Services
{
    public class SettingsManager
    {
        private static SettingsManager? _instance;
        private static readonly object _lock = new();
        private const string SettingsFileName = "settings.ini";
        
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SettingsManager();
                    }
                }
                return _instance;
            }
        }

        public UserInfo? SavedUser { get; private set; }

        private SettingsManager()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                string settingsPath = Path.Combine(appPath, SettingsFileName);

                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    SavedUser = JsonSerializer.Deserialize<UserInfo>(json);
                }
                else
                {
                    // Load giá trị thử nghiệm mặc định (Chỉ dùng cho mục đích demo/trial)
                    SavedUser = new UserInfo
                    {
                        UserId = "GiaoVien01",
                        Password = "PassGiaoVien01", // Mật khẩu demo mặc định
                        LicenseKey = "1AAAAAAAAAAAAAA1",
                        RememberCredentials = true,
                        IsTrialAccount = true
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                SavedUser = null;
            }
        }

        public void SaveSettings()
        {
            try
            {
                if (SavedUser != null && SavedUser.RememberCredentials)
                {
                    string appPath = AppDomain.CurrentDomain.BaseDirectory;
                    string settingsPath = Path.Combine(appPath, SettingsFileName);
                    
                    string json = JsonSerializer.Serialize(SavedUser, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    File.WriteAllText(settingsPath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public void UpdateUserInfo(UserInfo userInfo)
        {
            SavedUser = userInfo;
            SaveSettings();
        }

        public void ClearSettings()
        {
            try
            {
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                string settingsPath = Path.Combine(appPath, SettingsFileName);
                
                if (File.Exists(settingsPath))
                {
                    File.Delete(settingsPath);
                }
                
                SavedUser = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing settings: {ex.Message}");
            }
        }
    }
}
