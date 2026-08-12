using System;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// State Service quản lý điều khiển lớp học.
    /// Screen lock, silence mode, late-join state.
    /// </summary>
    public class ClassControlService
    {
        public static ClassControlService Instance { get; } = new();
        private readonly object _lock = new();

        public volatile bool IsScreenLocked;

        private string _screenLockType = string.Empty;
        public string ScreenLockType
        {
            get { lock (_lock) return _screenLockType; }
            set { lock (_lock) _screenLockType = value; }
        }

        public volatile bool IsSilenceActive;

        private string _activeToolFocusId = string.Empty;
        public string ActiveToolFocusId
        {
            get { lock (_lock) return _activeToolFocusId; }
            set { lock (_lock) _activeToolFocusId = value; }
        }

        private string _activeToolSectionId = string.Empty;
        public string ActiveToolSectionId
        {
            get { lock (_lock) return _activeToolSectionId; }
            set { lock (_lock) _activeToolSectionId = value; }
        }

        public volatile bool IsWebBlocked;
        public volatile bool IsWebWhitelistActive;

        private string _webWhitelistUrls = string.Empty;
        public string WebWhitelistUrls
        {
            get { lock (_lock) return _webWhitelistUrls; }
            set { lock (_lock) _webWhitelistUrls = value; }
        }

        public volatile bool IsExitPinRequired;
        
        private string _exitPinHash = string.Empty;
        public string ExitPinHash
        {
            get { lock (_lock) return _exitPinHash; }
            set { lock (_lock) _exitPinHash = value; }
        }

        private string _exitPinCode = string.Empty;
        public string ExitPinCode
        {
            get { lock (_lock) return _exitPinCode; }
            set { lock (_lock) _exitPinCode = value; }
        }

        private string _appWhitelist = "[]";
        public string AppWhitelist
        {
            get { lock (_lock) return _appWhitelist; }
            set { lock (_lock) _appWhitelist = value; }
        }

        private string _sessionSalt = string.Empty;
        public string SessionSalt
        {
            get { lock (_lock) return _sessionSalt; }
            set { lock (_lock) _sessionSalt = value; }
        }

        public void InitializeNewSession()
        {
            lock (_lock)
            {
                _sessionSalt = Guid.NewGuid().ToString("N").Substring(0, 16);
            }
        }

        public volatile bool IsInternetBlocked;
        public volatile bool IsEduOnlyAllowed;
        public volatile bool IsSocialBlocked;
        public volatile bool IsGamesBlocked;
        public volatile bool IsDesktopLocked;
        public volatile bool IsTeacherScreenShown;
        public volatile bool IsTaskbarDisabled;
        public volatile bool IsUsbBlocked;
        public volatile bool IsAppInstallBlocked;
        public volatile bool IsAppWhitelistOnly;
        public volatile bool IsPrintBlocked;

        public static string ComputeSha256Hash(string rawData, string salt)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData + salt));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        public event EventHandler? StateChanged;

        public void Reset()
        {
            IsScreenLocked = false;
            ScreenLockType = string.Empty;
            IsSilenceActive = false;
            ActiveToolFocusId = string.Empty;
            ActiveToolSectionId = string.Empty;
            IsWebBlocked = false;
            IsWebWhitelistActive = false;
            WebWhitelistUrls = string.Empty;
            IsExitPinRequired = false;
            ExitPinCode = string.Empty;
            ExitPinHash = string.Empty;
            
            IsInternetBlocked = false;
            IsEduOnlyAllowed = false;
            IsSocialBlocked = false;
            IsGamesBlocked = false;
            IsDesktopLocked = false;
            IsTeacherScreenShown = false;
            IsTaskbarDisabled = false;
            IsUsbBlocked = false;
            IsAppInstallBlocked = false;
            IsAppWhitelistOnly = false;
            IsPrintBlocked = false;

            AppWhitelist = "[]";
            SessionSalt = string.Empty;

            Log.Information("[ClassControl] Reset");
        }

        public void NotifyChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
