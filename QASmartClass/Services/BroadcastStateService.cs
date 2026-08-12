using System;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// State Service quản lý trạng thái broadcast.
    /// GV phát bảng/file, HS nhận qua HTTP API.
    /// </summary>
    public class BroadcastStateService
    {
        public static BroadcastStateService Instance { get; } = new();
        private readonly object _lock = new();

        // Filter persistence
        private string _targetSelectionMode = "ALL";
        public string TargetSelectionMode
        {
            get { lock (_lock) return _targetSelectionMode; }
            set { lock (_lock) _targetSelectionMode = value; }
        }

        public System.Collections.Generic.HashSet<string> TargetSelectedGroups { get; } = new(StringComparer.OrdinalIgnoreCase);
        public System.Collections.Generic.HashSet<string> TargetSelectedStudents { get; } = new(StringComparer.OrdinalIgnoreCase);

        // Screen broadcast
        private string _screenCapturePath = string.Empty;
        public string ScreenCapturePath
        {
            get { lock (_lock) return _screenCapturePath; }
            set { lock (_lock) _screenCapturePath = value; }
        }
        private byte[] _screenCaptureBytes = Array.Empty<byte>();
        public byte[] ScreenCaptureBytes
        {
            get { lock (_lock) return _screenCaptureBytes; }
            set { lock (_lock) _screenCaptureBytes = value; }
        }
        private DateTime _screenCaptureTime = DateTime.MinValue;
        public DateTime ScreenCaptureTime
        {
            get { lock (_lock) return _screenCaptureTime; }
            set { lock (_lock) _screenCaptureTime = value; }
        }
        private bool _isScreenBroadcastActive;
        public bool IsScreenBroadcastActive
        {
            get { lock (_lock) return _isScreenBroadcastActive; }
            set { lock (_lock) _isScreenBroadcastActive = value; }
        }
        public int ScreenBroadcastIntervalSec { get; set; } = 1;
        public bool IsForceWatchActive { get; set; }

        private string _broadcastToken = string.Empty;
        public string BroadcastToken
        {
            get { lock (_lock) return _broadcastToken; }
            set { lock (_lock) _broadcastToken = value; }
        }

        // File broadcast
        private string _fileBroadcastPath = string.Empty;
        public string FileBroadcastPath
        {
            get { lock (_lock) return _fileBroadcastPath; }
            set { lock (_lock) _fileBroadcastPath = value; }
        }

        public System.Collections.Generic.Dictionary<string, string> ActiveBroadcastFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void AddBroadcastFile(string filePath)
        {
            lock (_lock)
            {
                _fileBroadcastPath = filePath;
                var fileName = System.IO.Path.GetFileName(filePath);
                ActiveBroadcastFiles[fileName] = filePath;
            }
        }

        // Broadcast casting
        private bool _isBroadcastCasting;
        public bool IsBroadcastCasting
        {
            get { lock (_lock) return _isBroadcastCasting; }
            set { lock (_lock) _isBroadcastCasting = value; }
        }

        public event EventHandler? StateChanged;

        public void Reset()
        {
            lock (_lock)
            {
                _screenCapturePath = string.Empty;
                _screenCaptureBytes = Array.Empty<byte>();
                _screenCaptureTime = DateTime.MinValue;
                _isScreenBroadcastActive = false;
                IsForceWatchActive = false;
                _broadcastToken = string.Empty;
                _fileBroadcastPath = string.Empty;
                ActiveBroadcastFiles.Clear();
                _isBroadcastCasting = false;
                _targetSelectionMode = "ALL";
                TargetSelectedGroups.Clear();
                TargetSelectedStudents.Clear();
            }
            Log.Information("[BroadcastState] Reset");
        }

        public void NotifyChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
