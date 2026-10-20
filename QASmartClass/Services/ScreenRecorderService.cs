using System;
using System.IO;
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
        private DateTime _recordingStartTime;
        private RecordingFormat _recordingFormat = RecordingFormat.GIF; // Legacy

        private static ScreenRecorderService? _instance;
        public static ScreenRecorderService Instance => _instance ??= new ScreenRecorderService();

        public bool IsRecording => _isRecording;
        public bool IsPaused => _isPaused;
        public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
        public RecordingFormat CurrentFormat => _recordingFormat;
        
        public event Action<string?, string?>? SaveCompleted;

        private bool _isPaused;

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
        private bool _isMicrophoneMuted = false;
        public bool IsMicrophoneMuted => _isMicrophoneMuted;

        public void ToggleMicrophoneMute()
        {
            if (_micSource != null)
            {
                _isMicrophoneMuted = !_isMicrophoneMuted;
                _micSource.Volume = _isMicrophoneMuted ? 0.0f : 1.0f;
                System.Diagnostics.Debug.WriteLine($"🎤 Microphone Mute toggled: {_isMicrophoneMuted}");
            }
        }

        public void StartRecording(string? customPath = null)
        {
            StartRecording(true, true, null, customPath);
        }

        public void StartRecording(bool enableMicrophone, string? customPath = null)
        {
            StartRecording(enableMicrophone, true, null, customPath);
        }

        public void StartRecording(bool enableMicrophone, bool enableSystemAudio, string? specifiedMicId = null, string? customPath = null)
        {
            if (_isRecording) return;
            _isPaused = false;
            _isMicrophoneMuted = false;
            _micSource = null;

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

            var audioOptions = new AudioOptions
            {
                IsAudioEnabled = enableMicrophone || enableSystemAudio,
                Bitrate = AudioBitrate.bitrate_128kbps,
                Channels = AudioChannels.Stereo,
                AudioSources = new List<AudioSourceBase>()
            };

            // 1. Cấu hình Microphone (Thu âm giọng nói giáo viên giảng bài)
            if (enableMicrophone)
            {
                try
                {
                    string? selectedMicId = specifiedMicId;
                    if (string.IsNullOrEmpty(selectedMicId))
                    {
                        var captureDevices = Recorder.GetSystemAudioCaptureDevices();
                        if (captureDevices != null && captureDevices.Count > 0)
                        {
                            var targetMic = captureDevices.FirstOrDefault(d => d.IsDefaultDevice && !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                         ?? captureDevices.FirstOrDefault(d => !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                         ?? captureDevices.FirstOrDefault(d => d.IsDefaultDevice)
                                         ?? captureDevices[0];

                            selectedMicId = targetMic?.DeviceName;
                            System.Diagnostics.Debug.WriteLine($"🎤 Đã chọn Microphone: {targetMic?.FriendlyName} ({selectedMicId})");
                        }
                    }

                    _micSource = !string.IsNullOrEmpty(selectedMicId)
                        ? new CaptureAudioSource(selectedMicId)
                        : (CaptureAudioSource.Default ?? new CaptureAudioSource());

                    _micSource.Volume = 1.0f;
                    _micSource.IsAudioCaptureEnabled = true;
                    _micSource.ForceMono = true; // Chuẩn hóa mono cho giọng nói vào 2 kênh stereo
                    audioOptions.AudioSources.Add(_micSource);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Microphone config warning: {ex.Message}");
                }
            }

            // 2. Cấu hình Loa / Âm thanh hệ thống (Thu âm video bài giảng, youtube, clip minh họa trên máy/màn hình)
            if (enableSystemAudio)
            {
                try
                {
                    var loopbackDevices = Recorder.GetSystemAudioLoopbackDevices();
                    string? selectedSpeakerId = null;
                    if (loopbackDevices != null && loopbackDevices.Count > 0)
                    {
                        // Ưu tiên thiết bị phát âm thanh mặc định của Windows
                        var targetSpeaker = loopbackDevices.FirstOrDefault(d => d.IsDefaultDevice && !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                         ?? loopbackDevices.FirstOrDefault(d => d.IsDefaultDevice)
                                         ?? loopbackDevices.FirstOrDefault(d => !d.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                         ?? loopbackDevices[0];

                        selectedSpeakerId = targetSpeaker?.DeviceName;
                        System.Diagnostics.Debug.WriteLine($"🔊 Đã chọn Âm thanh hệ thống (Loa): {targetSpeaker?.FriendlyName} ({selectedSpeakerId})");
                    }

                    var speakerSource = !string.IsNullOrEmpty(selectedSpeakerId)
                        ? new LoopbackAudioSource(selectedSpeakerId)
                        : (LoopbackAudioSource.Default ?? new LoopbackAudioSource());

                    speakerSource.Volume = 1.0f;
                    speakerSource.IsAudioCaptureEnabled = true;
                    audioOptions.AudioSources.Add(speakerSource);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Speaker config warning: {ex.Message}");
                }
            }

            var options = new RecorderOptions
            {
                SourceOptions = new SourceOptions
                {
                    RecordingSources = new List<RecordingSourceBase>()
                },
                OutputOptions = new OutputOptions
                {
                    RecorderMode = RecorderMode.Video
                },
                VideoEncoderOptions = new VideoEncoderOptions
                {
                    Framerate = 30,
                    Bitrate = 4000 * 1000,
                    IsFixedFramerate = false,
                    // Dùng Software encoding (Media Foundation) để tương thích 100% mọi dòng máy,
                    // không bị lỗi "feature level is not supported" do GPU/driver rời hoặc hybrid laptop
                    IsHardwareEncodingEnabled = false
                },
                MouseOptions = new MouseOptions
                {
                    IsMousePointerEnabled = true,
                    IsMouseClicksDetected = false
                },
                AudioOptions = audioOptions
            };

            // Chọn màn hình và sử dụng WindowsGraphicsCapture API (tương thích đa màn hình, laptop card rời Optimus)
            var displays = Recorder.GetDisplays();
            DisplayRecordingSource displaySource;
            if (displays != null && displays.Count > 0)
            {
                var targetDisplay = displays.FirstOrDefault(d => !string.IsNullOrEmpty(d.DeviceName)) ?? displays[0];
                displaySource = new DisplayRecordingSource(targetDisplay.DeviceName);
            }
            else
            {
                displaySource = DisplayRecordingSource.MainMonitor;
            }

            // WindowsGraphicsCapture giải quyết triệt để lỗi DXGI_ERROR_UNSUPPORTED của DesktopDuplication
            displaySource.RecorderApi = RecorderApi.WindowsGraphicsCapture;
            displaySource.IsBorderRequired = false; // Tắt viền vàng của Windows khi quay
            displaySource.IsCursorCaptureEnabled = true;

            options.SourceOptions.RecordingSources.Add(displaySource);

            _recorder = Recorder.CreateRecorder(options);
            _recorder.OnRecordingComplete += Recorder_OnRecordingComplete;
            _recorder.OnRecordingFailed += Recorder_OnRecordingFailed;

            _recorder.Record(_currentRecordingPath);
            _recordingStartTime = DateTime.Now;
            _isRecording = true;
        }

        public async void StopRecording()
        {
            if (!_isRecording) return;
            _isRecording = false;

            // Đảm bảo có tối thiểu 1.2s để tránh lỗi "Không có frames nào được ghi" nếu bấm dừng quá nhanh
            var elapsed = DateTime.Now - _recordingStartTime;
            if (elapsed.TotalMilliseconds < 1200)
            {
                await Task.Delay(1200 - (int)elapsed.TotalMilliseconds);
            }

            try
            {
                _recorder?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stop recorder error: {ex.Message}");
            }
        }

        private void Recorder_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {
            var filePath = e.FilePath;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() => 
            {
                DisposeRecorder();
                SaveCompleted?.Invoke(filePath, null);
            }));
        }

        private void Recorder_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            var error = e.Error;
            Application.Current?.Dispatcher.BeginInvoke(new Action(() => 
            {
                DisposeRecorder();
                SaveCompleted?.Invoke(null, error);
            }));
            System.Diagnostics.Debug.WriteLine($"Recording failed: {error}");
        }

        private void DisposeRecorder()
        {
            if (_recorder != null)
            {
                _recorder.OnRecordingComplete -= Recorder_OnRecordingComplete;
                _recorder.OnRecordingFailed -= Recorder_OnRecordingFailed;
                _recorder.Dispose();
                _recorder = null;
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
