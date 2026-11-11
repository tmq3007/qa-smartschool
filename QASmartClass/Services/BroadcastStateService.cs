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
        private long _frameVersion;
        private bool _lastFrameIsKeyframe;
        private const int MaxRingFrames = 64; // ~1.0s sequential frame buffer at 60 FPS
        private readonly System.Collections.Generic.Queue<(long Version, byte[] Bytes, bool IsKeyframe)> _frameRingBuffer = new(MaxRingFrames);

        public long FrameVersion => System.Threading.Interlocked.Read(ref _frameVersion);
        public bool LastFrameIsKeyframe { get { lock (_lock) return _lastFrameIsKeyframe; } }

        public byte[] ScreenCaptureBytes
        {
            get { lock (_lock) return _screenCaptureBytes; }
            set => PushBroadcastFrame(value, isKeyframe: false);
        }

        /// <summary>
        /// Đẩy khung hình mới vào bộ đệm vòng tuần tự, gắn cờ IsKeyframe = true cho khung chốt nét Ultra-Sharp (Q=94).
        /// </summary>
        public void PushBroadcastFrame(byte[] value, bool isKeyframe)
        {
            long ver = System.Threading.Interlocked.Increment(ref _frameVersion);
            lock (_lock)
            {
                _screenCaptureBytes = value ?? Array.Empty<byte>();
                _lastFrameIsKeyframe = isKeyframe;
                if (value != null && value.Length > 0)
                {
                    while (_frameRingBuffer.Count >= MaxRingFrames)
                    {
                        _frameRingBuffer.Dequeue();
                    }
                    _frameRingBuffer.Enqueue((ver, value, isKeyframe));
                }
            }
        }

        /// <summary>
        /// Lấy tuần tự từng khung hình kế tiếp từ bộ đệm vòng (Ring Buffer) để phát mượt 30-60 FPS không bị nhảy cóc qua các nét vẽ.
        /// Nếu client mạng Wi-Fi yếu bị tụt lại quá xa (> maxAllowedLag khung), tự động tua thông minh tới khung mới nhất / Keyframe siêu nét gần nhất.
        /// </summary>
        public bool TryGetNextSequentialFrame(ref long lastSentVersion, out byte[] frameBytes)
        {
            return TryGetNextSequentialFrame(ref lastSentVersion, out frameBytes, out _, maxAllowedLag: 64);
        }

        public bool TryGetNextSequentialFrame(ref long lastSentVersion, out byte[] frameBytes, out bool isKeyframe, int maxAllowedLag = 30)
        {
            lock (_lock)
            {
                frameBytes = Array.Empty<byte>();
                isKeyframe = false;
                if (_frameRingBuffer.Count == 0)
                {
                    if (_screenCaptureBytes.Length > 0 && _frameVersion > lastSentVersion)
                    {
                        lastSentVersion = _frameVersion;
                        frameBytes = _screenCaptureBytes;
                        isKeyframe = _lastFrameIsKeyframe;
                        return true;
                    }
                    return false;
                }

                // Lần đầu kết nối: ưu tiên lấy khung gần cuối (cách 1-2 frame) để vào hình tức thì và có gối đệm mượt
                if (lastSentVersion <= 0)
                {
                    (long Version, byte[] Bytes, bool IsKeyframe) chosen = default;
                    int idx = 0;
                    int targetIdx = Math.Max(0, _frameRingBuffer.Count - 2);
                    foreach (var item in _frameRingBuffer)
                    {
                        if (idx == targetIdx)
                        {
                            chosen = item;
                            break;
                        }
                        idx++;
                    }
                    if (chosen.Bytes != null && chosen.Bytes.Length > 0)
                    {
                        lastSentVersion = chosen.Version;
                        frameBytes = chosen.Bytes;
                        isKeyframe = chosen.IsKeyframe;
                        return true;
                    }
                }

                // Adaptive Backpressure (kiểu Google Meet): Nếu 1 máy Wi-Fi yếu bị trễ quá xa (> maxAllowedLag khung ~ 1 giây),
                // tua êm tới Keyframe siêu nét mới nhất (hoặc cách đỉnh 2 frame) để cắt đứt độ trễ tích lũy.
                if (maxAllowedLag > 0 && (_frameVersion - lastSentVersion) > maxAllowedLag)
                {
                    (long Version, byte[] Bytes, bool IsKeyframe) bestCatchup = default;
                    int idx = 0;
                    int fallbackIdx = Math.Max(0, _frameRingBuffer.Count - 2);
                    foreach (var item in _frameRingBuffer)
                    {
                        if (item.Version > lastSentVersion && item.IsKeyframe)
                        {
                            bestCatchup = item; // Ưu tiên khung chốt nét Ultra-Sharp mới nhất
                        }
                        else if (idx == fallbackIdx && bestCatchup.Bytes == null)
                        {
                            bestCatchup = item;
                        }
                        idx++;
                    }
                    if (bestCatchup.Bytes != null && bestCatchup.Bytes.Length > 0)
                    {
                        lastSentVersion = bestCatchup.Version;
                        frameBytes = bestCatchup.Bytes;
                        isKeyframe = bestCatchup.IsKeyframe;
                        return true;
                    }
                }

                // Phát tuần tự 100% từng khung hình cho các máy mạng bình thường/mạnh
                foreach (var item in _frameRingBuffer)
                {
                    if (item.Version > lastSentVersion)
                    {
                        lastSentVersion = item.Version;
                        frameBytes = item.Bytes;
                        isKeyframe = item.IsKeyframe;
                        return true;
                    }
                }

                return false;
            }
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
            set
            {
                bool changed;
                lock (_lock)
                {
                    changed = _isScreenBroadcastActive != value;
                    _isScreenBroadcastActive = value;
                }
                if (changed) StateChanged?.Invoke(this, EventArgs.Empty);
            }
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
                _frameRingBuffer.Clear();
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
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
