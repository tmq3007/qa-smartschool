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

        public bool IsRecording => _isRecording;
        public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
        public RecordingFormat CurrentFormat => _recordingFormat;
        
        public event Action<string?, string?>? SaveCompleted;

        public ScreenRecorderService()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _defaultSavePath = Path.Combine(documentsPath, "QASmartTouch", "Recordings");
            Directory.CreateDirectory(_defaultSavePath);
        }

        public void StartRecording(string? customPath = null)
        {
            StartRecording(true, true, customPath);
        }

        public void StartRecording(bool enableMicrophone, string? customPath = null)
        {
            StartRecording(enableMicrophone, true, customPath);
        }

        public void StartRecording(bool enableMicrophone, bool enableSystemAudio, string? customPath = null)
        {
            if (_isRecording) return;

            var fileName = $"Record_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
            _currentRecordingPath = customPath ?? Path.Combine(_defaultSavePath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(_currentRecordingPath)!);

            var audioOptions = new AudioOptions
            {
                IsAudioEnabled = enableMicrophone || enableSystemAudio,
                Bitrate = AudioBitrate.bitrate_128kbps,
                Channels = AudioChannels.Stereo
            };

            // Helper set dynamic / base property an toàn qua Reflection
            void SetDynamicProp(object target, string name, object? val)
            {
                var t = target.GetType();
                var p = t.GetProperty(name);
                while (p == null && t.BaseType != null)
                {
                    t = t.BaseType;
                    p = t.GetProperty(name);
                }
                p?.SetValue(target, val);
            }

            // Tìm và cấu hình chính xác thiết bị phát (Loa/Tai nghe) và Microphone qua Reflection
            // (tránh lỗi CS0103/CS0117 trong trình biên dịch tạm _wpftmp của WPF)
            try
            {
                var recType = typeof(Recorder);
                var getAudioDevicesMethod = recType.GetMethod("GetSystemAudioDevices");

                if (enableMicrophone && getAudioDevicesMethod != null)
                {
                    // 1 = AudioDeviceSource.InputDevices
                    var inputDevices = getAudioDevicesMethod.Invoke(null, new object[] { 1 }) as System.Collections.IEnumerable;
                    if (inputDevices != null)
                    {
                        string? bestInputId = null;
                        foreach (var item in inputDevices)
                        {
                            var fName = item.GetType().GetProperty("FriendlyName")?.GetValue(item)?.ToString() ?? "";
                            var dName = item.GetType().GetProperty("DeviceName")?.GetValue(item)?.ToString() ?? "";

                            if (!string.IsNullOrEmpty(dName) && !fName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                            {
                                if (fName.Contains("Realtek", StringComparison.OrdinalIgnoreCase) ||
                                    fName.Contains("Microphone", StringComparison.OrdinalIgnoreCase) ||
                                    fName.Contains("Mic", StringComparison.OrdinalIgnoreCase))
                                {
                                    bestInputId = dName;
                                    break;
                                }
                                if (bestInputId == null)
                                {
                                    bestInputId = dName;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(bestInputId))
                        {
                            SetDynamicProp(audioOptions, "AudioInputDevice", bestInputId);
                            System.Diagnostics.Debug.WriteLine($"🎤 Selected Audio Input ID: {bestInputId}");
                        }
                    }
                }

                if (enableSystemAudio && getAudioDevicesMethod != null)
                {
                    // 0 = AudioDeviceSource.OutputDevices
                    var outputDevices = getAudioDevicesMethod.Invoke(null, new object[] { 0 }) as System.Collections.IEnumerable;
                    if (outputDevices != null)
                    {
                        string? bestOutputId = null;
                        foreach (var item in outputDevices)
                        {
                            var fName = item.GetType().GetProperty("FriendlyName")?.GetValue(item)?.ToString() ?? "";
                            var dName = item.GetType().GetProperty("DeviceName")?.GetValue(item)?.ToString() ?? "";

                            if (!string.IsNullOrEmpty(dName) && 
                                !fName.Contains("Display", StringComparison.OrdinalIgnoreCase) && 
                                !fName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                            {
                                if (fName.Contains("Realtek", StringComparison.OrdinalIgnoreCase) ||
                                    fName.Contains("Speaker", StringComparison.OrdinalIgnoreCase) ||
                                    fName.Contains("Loa", StringComparison.OrdinalIgnoreCase) ||
                                    fName.Contains("Headphone", StringComparison.OrdinalIgnoreCase))
                                {
                                    bestOutputId = dName;
                                    break;
                                }
                                if (bestOutputId == null)
                                {
                                    bestOutputId = dName;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(bestOutputId))
                        {
                            SetDynamicProp(audioOptions, "AudioOutputDevice", bestOutputId);
                            System.Diagnostics.Debug.WriteLine($"🔊 Selected Audio Output ID: {bestOutputId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio device selection error: {ex.Message}");
            }

            try
            {
                SetDynamicProp(audioOptions, "IsInputDeviceEnabled", (bool?)enableMicrophone);
                SetDynamicProp(audioOptions, "IsOutputDeviceEnabled", (bool?)enableSystemAudio);
                SetDynamicProp(audioOptions, "ForceInputDeviceMono", (bool?)true);
                SetDynamicProp(audioOptions, "InputVolume", (float?)1.0f);
                SetDynamicProp(audioOptions, "OutputVolume", (float?)1.0f);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio options set warning: {ex.Message}");
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
