using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ScreenRecorderLib;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;

namespace QASmartTouch.Services
{
    public class ScreenRecorderService
    {
        private Recorder? _recorder;
        private string _defaultSavePath;
        private string? _currentRecordingPath;
        private bool _isRecording;
        private bool _isFinalizing;
        private bool _discardCurrentRecording;
        private TaskCompletionSource<(string? FilePath, string? Error)>? _sessionCompletionTcs;
        private Task? _pendingDisposeTask;
        private DateTime _recordingStartTime;
        private RecordingFormat _recordingFormat = RecordingFormat.GIF; // Legacy
        private RecordingQualityPreset _qualityPreset = RecordingQualityPreset.Standard1080p;

        private static ScreenRecorderService? _instance;
        public static ScreenRecorderService Instance => _instance ??= new ScreenRecorderService();

        public bool IsRecording => _isRecording;
        public bool IsFinalizing => _isFinalizing;
        public bool IsSessionActive => _isRecording || _isFinalizing;
        public bool IsPaused => _isPaused;
        public string DefaultSavePath => _defaultSavePath;
        public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
        public RecordingFormat CurrentFormat => _recordingFormat;
        public RecordingQualityPreset CurrentQualityPreset
        {
            get => _qualityPreset;
            set => _qualityPreset = value;
        }
        
        public event Action<string?, string?>? SaveCompleted;

        private bool _isPaused;

        #region Win32 Display Resolution Helper (DPI-independent physical resolution)
        private const int ENUM_CURRENT_SETTINGS = -1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODEW
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettingsW(string? lpszDeviceName, int iModeNum, ref DEVMODEW lpDevMode);

        /// <summary>
        /// Tính toán kích thước khung hình đầu ra tối ưu theo chuẩn H.264 (luôn chẵn pixel, không phóng to,
        /// tự động thu nhỏ từ màn hình 2K/4K về đúng trần 1080p hoặc 720p để chống phình dung lượng).
        /// </summary>
        private static ScreenSize CalculateOptimalOutputSize(string? deviceName, int maxWidth, int maxHeight)
        {
            int physWidth = 1920;
            int physHeight = 1080;

            try
            {
                var dm = new DEVMODEW();
                dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODEW));
                if ((!string.IsNullOrEmpty(deviceName) && EnumDisplaySettingsW(deviceName, ENUM_CURRENT_SETTINGS, ref dm)) ||
                    EnumDisplaySettingsW(null, ENUM_CURRENT_SETTINGS, ref dm))
                {
                    if (dm.dmPelsWidth > 0 && dm.dmPelsHeight > 0)
                    {
                        physWidth = dm.dmPelsWidth;
                        physHeight = dm.dmPelsHeight;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnumDisplaySettings fallback warning: {ex.Message}");
            }

            int outWidth = physWidth;
            int outHeight = physHeight;

            if (physWidth > maxWidth || physHeight > maxHeight)
            {
                double scale = Math.Min((double)maxWidth / physWidth, (double)maxHeight / physHeight);
                outWidth = (int)Math.Round(physWidth * scale);
                outHeight = (int)Math.Round(physHeight * scale);
            }

            // Chuẩn H.264 yêu cầu chiều rộng và chiều cao bắt buộc là số chẵn (chia hết cho 2)
            outWidth = Math.Max(640, outWidth & ~1);
            outHeight = Math.Max(360, outHeight & ~1);

            System.Diagnostics.Debug.WriteLine($"📐 Screen Physical: {physWidth}x{physHeight} -> Recording Output: {outWidth}x{outHeight}");
            return new ScreenSize(outWidth, outHeight);
        }
        #endregion

        public ScreenRecorderService()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _defaultSavePath = Path.Combine(documentsPath, "QASmartTouch", "Recordings");
            Directory.CreateDirectory(_defaultSavePath);
        }

        public void PauseRecording()
        {
            if (_isRecording && !_isPaused)
            {
                try
                {
                    _recorder?.Pause();
                    _isPaused = true;
                    System.Diagnostics.Debug.WriteLine("⏸️ Recording Paused");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Pause recorder error: {ex.Message}");
                }
            }
        }

        public void ResumeRecording()
        {
            if (_isRecording && _isPaused)
            {
                try
                {
                    _recorder?.Resume();
                    _isPaused = false;
                    System.Diagnostics.Debug.WriteLine("▶️ Recording Resumed");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Resume recorder error: {ex.Message}");
                }
            }
        }

        private CaptureAudioSource? _micSource;
        private LoopbackAudioSource? _speakerSource;
        private bool _isMicrophoneMuted = false;
        public bool IsMicrophoneMuted => _isMicrophoneMuted;
        public bool HasAudioSources => _micSource != null || _speakerSource != null;

        /// <summary>
        /// Bật / Tắt toàn bộ âm thanh thu vào video (cả Microphone lẫn Âm thanh hệ thống/YouTube)
        /// trong thời gian thực thông qua DynamicOptionsBuilder của ScreenRecorderLib.
        /// </summary>
        public void ToggleMicrophoneMute()
        {
            if (!_isRecording || _recorder == null || !HasAudioSources) return;

            _isMicrophoneMuted = !_isMicrophoneMuted;
            float targetVolume = _isMicrophoneMuted ? 0.0f : 1.0f;
            bool captureEnabled = !_isMicrophoneMuted;

            try
            {
                var builder = _recorder.GetDynamicOptionsBuilder();

                // 1. Hạ MasterVolume của luồng trộn âm thanh tổng về 0 khi Mute
                builder.SetDynamicAudioOptions(new DynamicAudioOptions
                {
                    MasterVolume = targetVolume
                });

                // 2. Cập nhật trạng thái nguồn Microphone xuống lõi C++
                if (_micSource != null)
                {
                    _micSource.Volume = targetVolume;
                    _micSource.IsAudioCaptureEnabled = captureEnabled;
                    builder.SetUpdatedAudioSource(_micSource);
                }

                // 3. Cập nhật trạng thái nguồn Âm thanh hệ thống (Loa / YouTube) xuống lõi C++
                if (_speakerSource != null)
                {
                    _speakerSource.Volume = targetVolume;
                    _speakerSource.IsAudioCaptureEnabled = captureEnabled;
                    builder.SetUpdatedAudioSource(_speakerSource);
                }

                // 4. Áp dụng thay đổi ngay lập tức vào phiên ghi hình đang chạy
                bool applied = builder.Apply();
                System.Diagnostics.Debug.WriteLine($"🎤 Audio Mute toggled: Muted={_isMicrophoneMuted}, Applied={applied}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleMicrophoneMute error: {ex.Message}");
            }
        }

        public void StartRecording(string? customPath = null)
        {
            StartRecording(true, true, null, customPath, _qualityPreset);
        }

        public void StartRecording(bool enableMicrophone, string? customPath = null)
        {
            StartRecording(enableMicrophone, enableMicrophone, null, customPath, _qualityPreset);
        }

        public void StartRecording(
            bool enableMicrophone,
            bool enableSystemAudio,
            string? specifiedMicId = null,
            string? customPath = null,
            RecordingQualityPreset? qualityPreset = null)
        {
            if (_isRecording || _isFinalizing) return;
            _isPaused = false;
            _discardCurrentRecording = false;

            bool initiallyMuted = !enableMicrophone && !enableSystemAudio;
            _isMicrophoneMuted = initiallyMuted;
            float initialVolume = initiallyMuted ? 0.0f : 1.0f;

            _micSource = null;
            _speakerSource = null;

            if (qualityPreset.HasValue)
            {
                _qualityPreset = qualityPreset.Value;
            }

            if (string.IsNullOrWhiteSpace(customPath))
            {
                var fileName = $"Record_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                _currentRecordingPath = Path.Combine(_defaultSavePath, fileName);
            }
            else
            {
                _currentRecordingPath = customPath;
            }

            var dir = Path.GetDirectoryName(_currentRecordingPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Luôn khởi tạo sẵn đường tiếng (Audio Track) để giáo viên có thể Bật/Tắt tiếng linh hoạt ngay trong lúc đang quay
            var audioOptions = new AudioOptions
            {
                IsAudioEnabled = true,
                MasterVolume = initialVolume,
                Bitrate = AudioBitrate.bitrate_96kbps,
                Channels = AudioChannels.Stereo,
                AudioSources = new List<AudioSourceBase>()
            };

            // 1. Cấu hình Microphone (Thu âm giọng nói giáo viên giảng bài — chỉ thêm khi thực sự có thiết bị thu âm)
            try
            {
                var captureDevices = Recorder.GetSystemAudioCaptureDevices();
                if (captureDevices != null && captureDevices.Count > 0)
                {
                    string? selectedMicId = null;
                    if (!string.IsNullOrEmpty(specifiedMicId))
                    {
                        var matched = captureDevices.FirstOrDefault(d =>
                            string.Equals(d.DeviceName, specifiedMicId, StringComparison.OrdinalIgnoreCase));
                        selectedMicId = matched?.DeviceName;
                    }

                    if (string.IsNullOrEmpty(selectedMicId))
                    {
                        var targetMic = captureDevices.FirstOrDefault(d => d.IsDefaultDevice && !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                     ?? captureDevices.FirstOrDefault(d => !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                     ?? captureDevices.FirstOrDefault(d => d.IsDefaultDevice)
                                     ?? captureDevices[0];

                        selectedMicId = targetMic?.DeviceName;
                        System.Diagnostics.Debug.WriteLine($"🎤 Đã chọn Microphone: {targetMic?.FriendlyName} ({selectedMicId})");
                    }

                    if (!string.IsNullOrEmpty(selectedMicId))
                    {
                        // Lưu ý sống còn: KHÔNG bật ForceMono = true trong ScreenRecorderLib 7.0.1!
                        // Bộ resampler WASAPICapture đã tự động trộn Mic 1 kênh (Mono) ra đều 2 kênh Stereo.
                        // Bật ForceMono = true gây tràn bộ đệm C++ (AudioManager::MixAudioSamples) với Mic 1ch/44.1kHz (như tranScreen Audio) làm văng app (0xc0000005).
                        _micSource = new CaptureAudioSource(selectedMicId)
                        {
                            Volume = enableMicrophone ? 1.0f : 0.0f,
                            IsAudioCaptureEnabled = enableMicrophone,
                            ForceMono = false
                        };
                        audioOptions.AudioSources.Add(_micSource);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Microphone config warning: {ex.Message}");
            }

            // 2. Cấu hình Loa / Âm thanh hệ thống (chỉ thêm khi thực sự có đầu ra âm thanh loopback)
            try
            {
                var loopbackDevices = Recorder.GetSystemAudioLoopbackDevices();
                if (loopbackDevices != null && loopbackDevices.Count > 0)
                {
                    var targetSpeaker = loopbackDevices.FirstOrDefault(d => d.IsDefaultDevice && !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                     ?? loopbackDevices.FirstOrDefault(d => d.IsDefaultDevice)
                                     ?? loopbackDevices.FirstOrDefault(d => !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                     ?? loopbackDevices[0];

                    string? selectedSpeakerId = targetSpeaker?.DeviceName;
                    System.Diagnostics.Debug.WriteLine($"🔊 Đã chọn Âm thanh hệ thống (Loa): {targetSpeaker?.FriendlyName} ({selectedSpeakerId})");

                    if (!string.IsNullOrEmpty(selectedSpeakerId))
                    {
                        _speakerSource = new LoopbackAudioSource(selectedSpeakerId)
                        {
                            Volume = enableSystemAudio ? 1.0f : 0.0f,
                            IsAudioCaptureEnabled = enableSystemAudio
                        };
                        audioOptions.AudioSources.Add(_speakerSource);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speaker config warning: {ex.Message}");
            }

            // Nếu máy không có bất kỳ đầu vào/đầu ra âm thanh nào, tắt đường tiếng để ghi hình màn hình không bị lỗi WASAPI
            if (audioOptions.AudioSources.Count == 0)
            {
                audioOptions.IsAudioEnabled = false;
                _isMicrophoneMuted = true;
            }
            else
            {
                bool hasActiveSource = (_micSource?.IsAudioCaptureEnabled == true) || (_speakerSource?.IsAudioCaptureEnabled == true);
                _isMicrophoneMuted = !hasActiveSource;
                audioOptions.MasterVolume = hasActiveSource ? 1.0f : 0.0f;
            }

            // 3. Chọn màn hình và sử dụng WindowsGraphicsCapture API
            var displays = Recorder.GetDisplays();
            DisplayRecordingSource displaySource;
            string? targetDisplayDeviceName = null;
            if (displays != null && displays.Count > 0)
            {
                var targetDisplay = displays.FirstOrDefault(d => !string.IsNullOrEmpty(d.DeviceName)) ?? displays[0];
                targetDisplayDeviceName = targetDisplay.DeviceName;
                displaySource = new DisplayRecordingSource(targetDisplay.DeviceName);
            }
            else
            {
                displaySource = DisplayRecordingSource.MainMonitor;
                targetDisplayDeviceName = displaySource?.DeviceName;
            }

            displaySource.RecorderApi = RecorderApi.WindowsGraphicsCapture;
            displaySource.IsBorderRequired = false;
            displaySource.IsCursorCaptureEnabled = true;

            // 4. Thiết lập thông số nén tối ưu chuyên biệt cho bài giảng bảng tương tác
            bool isCompact720p = _qualityPreset == RecordingQualityPreset.Compact720p;
            int maxWidth = isCompact720p ? 1280 : 1920;
            int maxHeight = isCompact720p ? 720 : 1080;
            int targetFramerate = isCompact720p ? 15 : 20;
            // Quality 65 (1080p) / 56 (720p): Bảo toàn sắc nét các đường kẻ ô ly mảnh 1px và nét chữ mảnh mà vẫn giữ dung lượng nhẹ
            int targetQuality = isCompact720p ? 56 : 65;
            int targetBitrate = isCompact720p ? 1000 * 1000 : 2000 * 1000;

            var optimalOutputSize = CalculateOptimalOutputSize(targetDisplayDeviceName, maxWidth, maxHeight);

            var options = new RecorderOptions
            {
                SourceOptions = new SourceOptions
                {
                    RecordingSources = new List<RecordingSourceBase> { displaySource }
                },
                OutputOptions = new OutputOptions
                {
                    RecorderMode = RecorderMode.Video,
                    OutputFrameSize = optimalOutputSize,
                    Stretch = StretchMode.Uniform
                },
                VideoEncoderOptions = new VideoEncoderOptions
                {
                    Framerate = targetFramerate,
                    Quality = targetQuality,
                    Bitrate = targetBitrate,
                    IsFixedFramerate = false,
                    IsMp4FastStartEnabled = true,
                    IsHardwareEncodingEnabled = false,
                    Encoder = new H264VideoEncoder
                    {
                        BitrateMode = H264BitrateControlMode.Quality,
                        EncoderProfile = H264Profile.High
                    }
                },
                MouseOptions = new MouseOptions
                {
                    IsMousePointerEnabled = true,
                    IsMouseClicksDetected = false
                },
                AudioOptions = audioOptions
            };

            _sessionCompletionTcs = new TaskCompletionSource<(string? FilePath, string? Error)>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _recorder = Recorder.CreateRecorder(options);
            _recorder.OnRecordingComplete += Recorder_OnRecordingComplete;
            _recorder.OnRecordingFailed += Recorder_OnRecordingFailed;

            try
            {
                _recorder.Record(_currentRecordingPath);
                _recordingStartTime = DateTime.Now;
                _isRecording = true;
            }
            catch
            {
                _isRecording = false;
                _isFinalizing = false;
                _sessionCompletionTcs = null;
                DisposeRecorder();
                throw;
            }
        }

        public void StopRecording()
        {
            _ = StopAndFinalizeAsync(discardFile: false);
        }

        /// <summary>
        /// Dừng phiên ghi hình và chờ bộ mã hóa MediaFoundation đóng gói trọn vẹn chỉ mục MP4 (moov atom)
        /// cũng như giải phóng hoàn toàn thiết bị thu âm/ghi hình trước khi trả về.
        /// Nếu discardFile = true, tệp video nháp đang quay dở sẽ được xóa sạch khỏi ổ đĩa.
        /// </summary>
        public async Task<(string? FilePath, string? Error)> StopAndFinalizeAsync(bool discardFile = false, int timeoutMs = 10000)
        {
            if (discardFile)
            {
                _discardCurrentRecording = true;
            }

            var tcs = _sessionCompletionTcs;

            if (_isRecording)
            {
                _isRecording = false;
                _isFinalizing = true;

                // Khi cần giữ lại video, đảm bảo tối thiểu 1.2s để tránh lỗi thiếu frame của bộ mã hóa
                if (!_discardCurrentRecording)
                {
                    var elapsed = DateTime.Now - _recordingStartTime;
                    if (elapsed.TotalMilliseconds < 1200)
                    {
                        await Task.Delay(1200 - (int)elapsed.TotalMilliseconds);
                    }
                }

                try
                {
                    if (_recorder != null)
                    {
                        _recorder.Stop();
                    }
                    else
                    {
                        _isFinalizing = false;
                        tcs?.TrySetResult((null, null));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Stop recorder error: {ex.Message}");
                    _isFinalizing = false;
                    DisposeRecorder();
                    if (_pendingDisposeTask != null)
                    {
                        await _pendingDisposeTask;
                    }
                    tcs?.TrySetResult((null, ex.Message));
                }
            }
            else if (!_isFinalizing || tcs == null)
            {
                return (null, null);
            }

            (string? FilePath, string? Error) result = (null, null);
            if (tcs != null)
            {
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Math.Max(1500, timeoutMs)));
                if (completedTask == tcs.Task)
                {
                    result = await tcs.Task;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ StopAndFinalizeAsync timed out waiting for OnRecordingComplete.");
                    _isFinalizing = false;
                    DisposeRecorder();
                    result = (_currentRecordingPath, "Quá thời gian chờ đóng gói tệp video.");
                }
            }

            var pendingDispose = _pendingDisposeTask;
            if (pendingDispose != null)
            {
                try
                {
                    await pendingDispose;
                }
                catch { }
            }

            if (discardFile || _discardCurrentRecording)
            {
                string? targetToDelete = !string.IsNullOrWhiteSpace(result.FilePath) ? result.FilePath : _currentRecordingPath;
                await DeleteFileWithRetryAsync(targetToDelete);
                return (null, null);
            }

            return result;
        }

        private void Recorder_OnRecordingComplete(object? sender, RecordingCompleteEventArgs e)
        {
            bool discard = _discardCurrentRecording;
            var tcs = _sessionCompletionTcs;
            _sessionCompletionTcs = null;

            _isRecording = false;
            _isFinalizing = false;
            _isPaused = false;

            var filePath = e.FilePath;
            DisposeRecorder();
            tcs?.TrySetResult((filePath, null));

            if (!discard)
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SaveCompleted?.Invoke(filePath, null);
                }));
            }
        }

        private void Recorder_OnRecordingFailed(object? sender, RecordingFailedEventArgs e)
        {
            bool discard = _discardCurrentRecording;
            var tcs = _sessionCompletionTcs;
            _sessionCompletionTcs = null;

            _isRecording = false;
            _isFinalizing = false;
            _isPaused = false;

            var error = e.Error ?? "Lỗi ghi hình không xác định.";
            var failedPath = !string.IsNullOrEmpty(e.FilePath) ? e.FilePath : _currentRecordingPath;
            System.Diagnostics.Debug.WriteLine($"Recording failed: {error}");

            DisposeRecorder();
            CleanupCorruptStubFile(failedPath);
            tcs?.TrySetResult((null, discard ? null : error));

            if (!discard)
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SaveCompleted?.Invoke(null, error);
                }));
            }
        }

        private static async Task DeleteFileWithRetryAsync(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (!File.Exists(path)) return;
                    File.Delete(path);
                    return;
                }
                catch
                {
                    await Task.Delay(120).ConfigureAwait(false);
                }
            }
        }

        private static void CleanupCorruptStubFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250).ConfigureAwait(false);
                    if (File.Exists(path))
                    {
                        var info = new FileInfo(path);
                        if (info.Length <= 128)
                        {
                            File.Delete(path);
                        }
                    }
                }
                catch { }
            });
        }

        private void DisposeRecorder()
        {
            var rec = _recorder;
            _recorder = null;
            _micSource = null;
            _speakerSource = null;

            if (rec != null)
            {
                rec.OnRecordingComplete -= Recorder_OnRecordingComplete;
                rec.OnRecordingFailed -= Recorder_OnRecordingFailed;
                _pendingDisposeTask = Task.Run(async () =>
                {
                    try
                    {
                        // Chờ luồng native C++ RecordTask thoát hẳn khỏi callback trước khi hủy C++/CLI wrapper
                        await Task.Delay(150).ConfigureAwait(false);
                        rec.Dispose();
                    }
                    catch { }
                });
            }
        }
        
        public string GetFormattedRecordingTime()
        {
            if (!_isRecording) return "00:00";
            return $"{RecordingDuration:mm\\:ss}";
        }

        public void OpenRecordingsFolder()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _defaultSavePath);
            }
            catch { }
        }

        public void SetRecordingFormat(RecordingFormat format)
        {
            _recordingFormat = format;
        }

        public RecordingFormat GetRecordingFormat()
        {
            return _recordingFormat;
        }
    }
}
