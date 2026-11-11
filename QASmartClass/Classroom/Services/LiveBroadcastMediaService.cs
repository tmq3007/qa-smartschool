using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using QASmartTouch.Models.Camera;
using QASmartTouch.Services.Camera;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    public enum MeetCameraCorner
    {
        BottomRight = 0,
        BottomLeft = 1,
        TopRight = 2
    }

    public enum MeetCameraShape
    {
        GoogleMeetRect = 0,
        SmartTouchCircle = 1
    }

    /// <summary>
    /// Dịch vụ trung tâm điều phối Âm thanh (Hệ thống Loopback + Micro Giáo viên) & Camera PiP
    /// cho tính năng Chia sẻ màn hình chuẩn Google Meet trong QA SmartClass.
    /// Tái sử dụng trực tiếp CameraDiscoveryService, CameraPreviewService và MediaDeviceCatalogService từ QA SmartTouch.
    /// </summary>
    public sealed class LiveBroadcastMediaService : IDisposable
    {
        public static LiveBroadcastMediaService Instance { get; } = new LiveBroadcastMediaService();

        // Chuẩn luồng âm thanh LAN độ trễ thấp: 24kHz, 16-bit Signed PCM, Mono (960 bytes / 20ms = 48 KB/s)
        public const int SAMPLE_RATE = 24000;
        public const int CHANNELS = 1;
        public const int SAMPLES_PER_PACKET = 480; // 20ms @ 24kHz
        public const int BYTES_PER_PACKET = SAMPLES_PER_PACKET * 2; // 960 bytes

        private readonly object _stateLock = new();
        private readonly object _audioBufferLock = new();
        private readonly object _cameraFrameLock = new();

        // ─── Danh sách phần cứng thực tế (Zero Fake Hardware) ───
        public List<AudioEndpointOption> AvailableMicrophones { get; private set; } = new();
        public List<AudioEndpointOption> AvailableSystemOutputs { get; private set; } = new();
        public List<CameraEndpointOption> AvailableCameras { get; private set; } = new();

        public bool HasMicrophoneHardware => AvailableMicrophones.Count > 0;
        public bool HasSystemAudioHardware => AvailableSystemOutputs.Count > 0;
        public bool HasCameraHardware => AvailableCameras.Count > 0;
        public bool IsScanningCameras { get; private set; }

        // ─── Cấu hình phiên hiện tại (Chuẩn Google Meet) ───
        public bool IsSessionRunning { get; private set; }
        public bool IsSystemAudioEnabled { get; private set; } = true; // Mặc định BẬT để chiếu video/bài nghe có tiếng ngay
        public bool IsMicrophoneEnabled { get; private set; } = false; // Mặc định TẮT tránh hú loa trong cùng phòng học
        public bool IsCameraEnabled { get; private set; } = false;

        public string SelectedMicrophoneId { get; private set; } = "";
        public string SelectedSystemOutputId { get; private set; } = "";
        public string SelectedCameraId { get; private set; } = "0";

        public MeetCameraCorner CameraCorner { get; set; } = MeetCameraCorner.BottomRight;
        public MeetCameraShape CameraShape { get; set; } = MeetCameraShape.GoogleMeetRect;
        public bool IsCameraMirrored { get; set; } = true;

        public bool IsAnyAudioActive => IsSessionRunning && ((IsSystemAudioEnabled && HasSystemAudioHardware) || (IsMicrophoneEnabled && HasMicrophoneHardware));

        // ─── Đo mức sóng âm thời gian thực (0..100%) cho thanh Google Meet ───
        public double CurrentMicLevelPercent { get; private set; }
        public double CurrentSystemLevelPercent { get; private set; }
        public double CurrentCombinedAudioPercent => Math.Max(CurrentMicLevelPercent, CurrentSystemLevelPercent);

        // ─── Bộ đệm hàng đợi mẫu 24kHz Float từ WASAPI ───
        private readonly Queue<float> _sysSampleQueue = new(9600);
        private readonly Queue<float> _micSampleQueue = new(9600);
        private WasapiLoopbackCapture? _sysCapture;
        private WasapiCapture? _micCapture;
        private MMDevice? _activeSysDevice;
        private MMDevice? _activeMicDevice;
        private CancellationTokenSource? _mixerCts;
        private Task? _mixerTask;

        // ─── Ring Buffer 128 gói PCM16 20ms (~2.5 giây) cho /api/audio_stream ───
        private const int AUDIO_RING_CAPACITY = 128;
        private readonly byte[][] _audioRingPackets = new byte[AUDIO_RING_CAPACITY][];
        private readonly long[] _audioRingSeq = new long[AUDIO_RING_CAPACITY];
        private long _latestAudioSeq = 0;

        // ─── Camera Preview (Tái sử dụng CameraPreviewService của SmartTouch) ───
        private CameraPreviewService? _cameraService;
        private byte[]? _latestCamBgr32;
        private int _latestCamWidth;
        private int _latestCamHeight;
        private BitmapSource? _latestCamPreviewBitmap;
        private long _cameraFrameSeq;

        public long LatestCameraFrameSeq => Interlocked.Read(ref _cameraFrameSeq);
        public BitmapSource? LatestCameraPreviewBitmap
        {
            get { lock (_cameraFrameLock) return _latestCamPreviewBitmap; }
        }

        public event EventHandler? StateChanged;
        public event EventHandler<string>? HardwareWarning;

        private LiveBroadcastMediaService()
        {
            RefreshAudioHardwareCatalog();
            _ = RefreshCameraHardwareCatalogAsync();
        }

        public void RefreshAudioHardwareCatalog()
        {
            AvailableMicrophones = MediaDeviceCatalogService.GetCaptureMicrophones();
            AvailableSystemOutputs = MediaDeviceCatalogService.GetSystemRenderLoopbacks();

            if (AvailableMicrophones.Count > 0)
            {
                if (string.IsNullOrEmpty(SelectedMicrophoneId) ||
                    !AvailableMicrophones.Any(m => string.Equals(m.DeviceId, SelectedMicrophoneId, StringComparison.OrdinalIgnoreCase)))
                {
                    var preferred = AvailableMicrophones.FirstOrDefault(m => !m.IsVirtual && m.IsDefault)
                                 ?? AvailableMicrophones.FirstOrDefault(m => !m.IsVirtual)
                                 ?? AvailableMicrophones[0];
                    SelectedMicrophoneId = preferred.DeviceId;
                }
            }
            else
            {
                SelectedMicrophoneId = "";
                IsMicrophoneEnabled = false;
            }

            if (AvailableSystemOutputs.Count > 0)
            {
                if (string.IsNullOrEmpty(SelectedSystemOutputId) ||
                    !AvailableSystemOutputs.Any(o => string.Equals(o.DeviceId, SelectedSystemOutputId, StringComparison.OrdinalIgnoreCase)))
                {
                    var preferredOut = AvailableSystemOutputs.FirstOrDefault(o => o.IsDefault) ?? AvailableSystemOutputs[0];
                    SelectedSystemOutputId = preferredOut.DeviceId;
                }
            }
            else
            {
                SelectedSystemOutputId = "";
                IsSystemAudioEnabled = false;
            }

            RaiseStateChanged();
        }

        public async Task RefreshCameraHardwareCatalogAsync()
        {
            IsScanningCameras = true;
            RaiseStateChanged();
            try
            {
                var cams = await MediaDeviceCatalogService.GetCamerasAsync().ConfigureAwait(false);
                AvailableCameras = cams;
                if (cams.Count > 0)
                {
                    if (string.IsNullOrEmpty(SelectedCameraId) ||
                        !cams.Any(c => string.Equals(c.DeviceId, SelectedCameraId, StringComparison.OrdinalIgnoreCase)))
                    {
                        var preferredCam = cams.FirstOrDefault(c => !c.IsVirtual) ?? cams[0];
                        SelectedCameraId = preferredCam.DeviceId;
                    }
                }
                else
                {
                    SelectedCameraId = "";
                    IsCameraEnabled = false;
                }
            }
            finally
            {
                IsScanningCameras = false;
                RaiseStateChanged();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SESSION LIFECYCLE (BẮT ĐẦU / DỪNG CÙNG PHIÊN TRÌNH CHIẾU)
        // ═══════════════════════════════════════════════════════════════════

        public async Task StartSessionAsync()
        {
            RefreshAudioHardwareCatalog();
            if (AvailableCameras.Count == 0 && !IsScanningCameras)
            {
                _ = RefreshCameraHardwareCatalogAsync();
            }

            lock (_stateLock)
            {
                IsSessionRunning = true;
            }

            RebuildAudioCaptures();
            EnsureAudioMixerLoopRunning();

            if (IsCameraEnabled && HasCameraHardware)
            {
                await StartCameraInternalAsync().ConfigureAwait(false);
            }

            RaiseStateChanged();
        }

        public async Task StopSessionAsync()
        {
            lock (_stateLock)
            {
                IsSessionRunning = false;
            }

            StopAudioCaptures();
            StopAudioMixerLoop();
            await StopCameraInternalAsync().ConfigureAwait(false);

            CurrentMicLevelPercent = 0;
            CurrentSystemLevelPercent = 0;
            RaiseStateChanged();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  ĐIỀU KHIỂN TRỰC TIẾP TỪ THANH GOOGLE MEET BAR / POPOVER
        // ═══════════════════════════════════════════════════════════════════

        public void SetMicrophoneEnabled(bool enabled)
        {
            RefreshAudioHardwareCatalog();
            if (enabled && !HasMicrophoneHardware)
            {
                IsMicrophoneEnabled = false;
                HardwareWarning?.Invoke(this, "Không tìm thấy thiết bị Micro trên máy Giáo viên.");
                RaiseStateChanged();
                return;
            }

            IsMicrophoneEnabled = enabled;
            if (!enabled) CurrentMicLevelPercent = 0;
            if (IsSessionRunning)
            {
                RebuildAudioCaptures();
                EnsureAudioMixerLoopRunning();
            }
            RaiseStateChanged();
        }

        public void SelectMicrophone(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return;
            SelectedMicrophoneId = deviceId;
            if (IsSessionRunning && IsMicrophoneEnabled)
            {
                RebuildAudioCaptures();
            }
            RaiseStateChanged();
        }

        public void SetSystemAudioEnabled(bool enabled)
        {
            RefreshAudioHardwareCatalog();
            if (enabled && !HasSystemAudioHardware)
            {
                IsSystemAudioEnabled = false;
                HardwareWarning?.Invoke(this, "Không tìm thấy đầu ra âm thanh hệ thống để chia sẻ.");
                RaiseStateChanged();
                return;
            }

            IsSystemAudioEnabled = enabled;
            if (!enabled) CurrentSystemLevelPercent = 0;
            if (IsSessionRunning)
            {
                RebuildAudioCaptures();
                EnsureAudioMixerLoopRunning();
            }
            RaiseStateChanged();
        }

        public void SelectSystemOutput(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return;
            SelectedSystemOutputId = deviceId;
            if (IsSessionRunning && IsSystemAudioEnabled)
            {
                RebuildAudioCaptures();
            }
            RaiseStateChanged();
        }

        public async Task SetCameraEnabledAsync(bool enabled)
        {
            if (enabled && !HasCameraHardware)
            {
                await RefreshCameraHardwareCatalogAsync().ConfigureAwait(false);
                if (!HasCameraHardware)
                {
                    IsCameraEnabled = false;
                    HardwareWarning?.Invoke(this, "Không tìm thấy thiết bị Camera trên máy Giáo viên.");
                    RaiseStateChanged();
                    return;
                }
            }

            IsCameraEnabled = enabled;
            if (IsSessionRunning)
            {
                if (enabled)
                    await StartCameraInternalAsync().ConfigureAwait(false);
                else
                    await StopCameraInternalAsync().ConfigureAwait(false);
            }
            else if (!enabled)
            {
                await StopCameraInternalAsync().ConfigureAwait(false);
            }
            Interlocked.Increment(ref _cameraFrameSeq);
            RaiseStateChanged();
        }

        public async Task SelectCameraAsync(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId)) return;
            SelectedCameraId = deviceId;
            if (IsSessionRunning && IsCameraEnabled)
            {
                await StartCameraInternalAsync().ConfigureAwait(false);
            }
            RaiseStateChanged();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  WASAPI CAPTURE & 24kHz LOW-LATENCY MIXER
        // ═══════════════════════════════════════════════════════════════════

        private void RebuildAudioCaptures()
        {
            StopAudioCaptures();

            if (!IsSessionRunning) return;

            // 1. System Audio Loopback (WasapiLoopbackCapture)
            if (IsSystemAudioEnabled && HasSystemAudioHardware)
            {
                try
                {
                    using var enumerator = new MMDeviceEnumerator();
                    var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
                    _activeSysDevice = endpoints.FirstOrDefault(e => string.Equals(e.ID, SelectedSystemOutputId, StringComparison.OrdinalIgnoreCase))
                                    ?? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                    if (_activeSysDevice != null)
                    {
                        _sysCapture = new WasapiLoopbackCapture(_activeSysDevice);
                        _sysCapture.DataAvailable += OnSystemAudioDataAvailable;
                        _sysCapture.RecordingStopped += OnSystemRecordingStopped;
                        _sysCapture.StartRecording();
                        Log.Information("[LiveBroadcastMedia] System Audio Loopback started on: {Name}", _activeSysDevice.FriendlyName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[LiveBroadcastMedia] Failed to start System Audio Loopback: {Err}", ex.Message);
                    IsSystemAudioEnabled = false;
                }
            }

            // 2. Teacher Microphone (WasapiCapture)
            if (IsMicrophoneEnabled && HasMicrophoneHardware)
            {
                try
                {
                    using var enumerator = new MMDeviceEnumerator();
                    var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                    _activeMicDevice = endpoints.FirstOrDefault(e => string.Equals(e.ID, SelectedMicrophoneId, StringComparison.OrdinalIgnoreCase))
                                    ?? endpoints.FirstOrDefault(e => !e.FriendlyName.Contains("tranScreen", StringComparison.OrdinalIgnoreCase))
                                    ?? enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

                    if (_activeMicDevice != null)
                    {
                        _micCapture = new WasapiCapture(_activeMicDevice, true, 20);
                        _micCapture.DataAvailable += OnMicAudioDataAvailable;
                        _micCapture.RecordingStopped += OnMicRecordingStopped;
                        _micCapture.StartRecording();
                        Log.Information("[LiveBroadcastMedia] Microphone capture started on: {Name}", _activeMicDevice.FriendlyName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[LiveBroadcastMedia] Failed to start Microphone capture: {Err}", ex.Message);
                    IsMicrophoneEnabled = false;
                    HardwareWarning?.Invoke(this, "Không thể mở Micro (có thể đang bị ứng dụng khác khóa độc quyền).");
                }
            }
        }

        private void StopAudioCaptures()
        {
            var sys = _sysCapture;
            _sysCapture = null;
            if (sys != null)
            {
                try { sys.DataAvailable -= OnSystemAudioDataAvailable; } catch { }
                try { sys.RecordingStopped -= OnSystemRecordingStopped; } catch { }
                try { sys.StopRecording(); } catch { }
                try { sys.Dispose(); } catch { }
            }

            var sysDev = _activeSysDevice;
            _activeSysDevice = null;
            if (sysDev != null)
            {
                try { sysDev.Dispose(); } catch { }
            }

            var mic = _micCapture;
            _micCapture = null;
            if (mic != null)
            {
                try { mic.DataAvailable -= OnMicAudioDataAvailable; } catch { }
                try { mic.RecordingStopped -= OnMicRecordingStopped; } catch { }
                try { mic.StopRecording(); } catch { }
                try { mic.Dispose(); } catch { }
            }

            var micDev = _activeMicDevice;
            _activeMicDevice = null;
            if (micDev != null)
            {
                try { micDev.Dispose(); } catch { }
            }

            lock (_audioBufferLock)
            {
                _sysSampleQueue.Clear();
                _micSampleQueue.Clear();
            }
        }

        private void OnSystemRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null && IsSessionRunning && IsSystemAudioEnabled)
            {
                Log.Warning("[LiveBroadcastMedia] System audio device disconnected: {Err}", e.Exception.Message);
                IsSystemAudioEnabled = false;
                CurrentSystemLevelPercent = 0;
                RaiseStateChanged();
            }
        }

        private void OnMicRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null && IsSessionRunning && IsMicrophoneEnabled)
            {
                Log.Warning("[LiveBroadcastMedia] Microphone disconnected mid-session: {Err}", e.Exception.Message);
                IsMicrophoneEnabled = false;
                CurrentMicLevelPercent = 0;
                HardwareWarning?.Invoke(this, "Thiết bị Micro vừa ngắt kết nối — Đã tự động tắt tiếng Micro.");
                RaiseStateChanged();
            }
        }

        private void OnSystemAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            var cap = _sysCapture;
            if (cap == null || e.BytesRecorded <= 0) return;
            float peak = ConvertAndEnqueueMono24k(e.Buffer, e.BytesRecorded, cap.WaveFormat, _sysSampleQueue);
            double target = MediaDeviceCatalogService.ComputeNormalizedMeterPercent(peak);
            CurrentSystemLevelPercent = MediaDeviceCatalogService.SmoothMeterLevel(CurrentSystemLevelPercent, target);
        }

        private void OnMicAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            var cap = _micCapture;
            if (cap == null || e.BytesRecorded <= 0) return;
            float peak = ConvertAndEnqueueMono24k(e.Buffer, e.BytesRecorded, cap.WaveFormat, _micSampleQueue);
            double target = MediaDeviceCatalogService.ComputeNormalizedMeterPercent(peak);
            CurrentMicLevelPercent = MediaDeviceCatalogService.SmoothMeterLevel(CurrentMicLevelPercent, target);
        }

        /// <summary>
        /// Chuyển đổi mọi định dạng WASAPI (Float32 / PCM16 / PCM24 / PCM32, Stereo/Multi-channel, 48kHz/44.1kHz/16kHz)
        /// sang Mono 24,000 Hz nhanh, không cấp phát bộ nhớ thừa.
        /// </summary>
        private float ConvertAndEnqueueMono24k(byte[] buffer, int bytesRecorded, WaveFormat fmt, Queue<float> targetQueue)
        {
            int channels = Math.Max(1, fmt.Channels);
            int bytesPerSample = Math.Max(2, fmt.BitsPerSample / 8);
            int frameSize = bytesPerSample * channels;
            int totalInputFrames = bytesRecorded / frameSize;
            if (totalInputFrames <= 0) return 0f;

            bool isFloat = fmt.Encoding == WaveFormatEncoding.IeeeFloat ||
                           (fmt is WaveFormatExtensible ext && ext.SubFormat == NAudio.Dmo.AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT);

            float maxPeak = 0f;
            double step = (double)fmt.SampleRate / SAMPLE_RATE;
            int outSamples = Math.Max(1, (int)(totalInputFrames / step));

            lock (_audioBufferLock)
            {
                // Giới hạn hàng đợi tối đa 160ms (3840 mẫu) để triệt tiêu độ trễ tích lũy
                while (targetQueue.Count + outSamples > 3840 && targetQueue.Count > 0)
                {
                    targetQueue.Dequeue();
                }

                for (int o = 0; o < outSamples; o++)
                {
                    int srcFrame = Math.Min(totalInputFrames - 1, (int)(o * step));
                    int baseOffset = srcFrame * frameSize;
                    float sum = 0f;
                    int mixCh = Math.Min(channels, 2);
                    for (int ch = 0; ch < mixCh; ch++)
                    {
                        int off = baseOffset + ch * bytesPerSample;
                        float s = 0f;
                        if (isFloat && bytesPerSample == 4 && off + 4 <= bytesRecorded)
                        {
                            s = BitConverter.ToSingle(buffer, off);
                        }
                        else if (bytesPerSample == 2 && off + 2 <= bytesRecorded)
                        {
                            s = BitConverter.ToInt16(buffer, off) / 32768f;
                        }
                        else if (bytesPerSample == 4 && off + 4 <= bytesRecorded)
                        {
                            s = BitConverter.ToInt32(buffer, off) / 2147483648f;
                        }
                        else if (bytesPerSample == 3 && off + 3 <= bytesRecorded)
                        {
                            int s24 = (buffer[off + 2] << 24) | (buffer[off + 1] << 16) | (buffer[off] << 8);
                            s = s24 / 2147483648f;
                        }
                        sum += s;
                    }

                    float mono = sum / mixCh;
                    if (float.IsNaN(mono) || float.IsInfinity(mono)) mono = 0f;
                    float abs = Math.Abs(mono);
                    if (abs > maxPeak) maxPeak = abs;
                    targetQueue.Enqueue(mono);
                }
            }

            return maxPeak;
        }

        private void EnsureAudioMixerLoopRunning()
        {
            if (_mixerTask != null && !_mixerTask.IsCompleted) return;
            _mixerCts?.Cancel();
            _mixerCts = new CancellationTokenSource();
            var ct = _mixerCts.Token;
            _mixerTask = Task.Run(() => RunAudioMixerLoopAsync(ct), ct);
        }

        private void StopAudioMixerLoop()
        {
            _mixerCts?.Cancel();
            _mixerTask = null;
        }

        private async Task RunAudioMixerLoopAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
            int silentTicks = 100;
            try
            {
                while (!ct.IsCancellationRequested && IsSessionRunning)
                {
                    if (!await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                        break;

                    if (!IsSystemAudioEnabled) CurrentSystemLevelPercent = MediaDeviceCatalogService.SmoothMeterLevel(CurrentSystemLevelPercent, 0);
                    if (!IsMicrophoneEnabled) CurrentMicLevelPercent = MediaDeviceCatalogService.SmoothMeterLevel(CurrentMicLevelPercent, 0);

                    if (!IsAnyAudioActive)
                    {
                        silentTicks++;
                        continue;
                    }

                    var pcmBytes = new byte[BYTES_PER_PACKET];
                    bool hasAudibleSignal = false;

                    lock (_audioBufferLock)
                    {
                        bool hasSysData = _sysSampleQueue.Count >= SAMPLES_PER_PACKET;
                        bool hasMicData = _micSampleQueue.Count >= SAMPLES_PER_PACKET;

                        if (!hasSysData && _sysCapture != null)
                        {
                            // Khi máy tính im lặng, WasapiLoopbackCapture ngừng đẩy sự kiện -> tự giảm mượt vạch sóng âm về 0
                            CurrentSystemLevelPercent = MediaDeviceCatalogService.SmoothMeterLevel(CurrentSystemLevelPercent, 0);
                        }

                        for (int i = 0; i < SAMPLES_PER_PACKET; i++)
                        {
                            float sysVal = (IsSystemAudioEnabled && _sysSampleQueue.Count > 0) ? _sysSampleQueue.Dequeue() : 0f;
                            float micVal = (IsMicrophoneEnabled && _micSampleQueue.Count > 0) ? _micSampleQueue.Dequeue() * 1.15f : 0f;
                            float mixed = Math.Clamp(sysVal + micVal, -1.0f, 1.0f);
                            if (Math.Abs(mixed) > 0.0015f) hasAudibleSignal = true;

                            short s16 = (short)Math.Clamp((int)(mixed * 32767f), short.MinValue, short.MaxValue);
                            pcmBytes[i * 2] = (byte)(s16 & 0xFF);
                            pcmBytes[i * 2 + 1] = (byte)((s16 >> 8) & 0xFF);
                        }
                    }

                    if (hasAudibleSignal)
                    {
                        silentTicks = 0;
                        PushAudioPacket(pcmBytes);
                    }
                    else
                    {
                        silentTicks++;
                        // Gửi thêm đuôi im lặng 160ms (8 gói) để âm thanh ngắt êm, sau đó gửi nhịp Keep-Alive mỗi 1.5s
                        if (silentTicks <= 8 || silentTicks % 75 == 0)
                        {
                            PushAudioPacket(pcmBytes);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Warning("[LiveBroadcastMedia] Audio mixer loop ended: {Err}", ex.Message);
            }
        }

        private void PushAudioPacket(byte[] pcmPacket)
        {
            lock (_audioBufferLock)
            {
                long nextSeq = ++_latestAudioSeq;
                int slot = (int)(nextSeq % AUDIO_RING_CAPACITY);
                _audioRingPackets[slot] = pcmPacket;
                _audioRingSeq[slot] = nextSeq;
            }
        }

        public bool TryGetNextAudioPacket(ref long clientCursorSeq, out byte[] packet)
        {
            lock (_audioBufferLock)
            {
                if (_latestAudioSeq <= 0)
                {
                    packet = Array.Empty<byte>();
                    return false;
                }

                // Lần đầu kết nối hoặc tụt quá 10 gói (200ms) -> nhảy tới gói gần nhất để âm thanh luôn khớp miệng với hình ảnh!
                if (clientCursorSeq < 0 || (_latestAudioSeq - clientCursorSeq) > 10)
                {
                    clientCursorSeq = Math.Max(0, _latestAudioSeq - 2);
                }

                long target = clientCursorSeq + 1;
                if (target > _latestAudioSeq)
                {
                    packet = Array.Empty<byte>();
                    return false;
                }

                int slot = (int)(target % AUDIO_RING_CAPACITY);
                if (_audioRingSeq[slot] == target && _audioRingPackets[slot] != null)
                {
                    clientCursorSeq = target;
                    packet = _audioRingPackets[slot];
                    return true;
                }

                // Fallback về gói mới nhất nếu slot đã xoay vòng
                int latestSlot = (int)(_latestAudioSeq % AUDIO_RING_CAPACITY);
                if (_audioRingPackets[latestSlot] != null)
                {
                    clientCursorSeq = _latestAudioSeq;
                    packet = _audioRingPackets[latestSlot];
                    return true;
                }

                packet = Array.Empty<byte>();
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  CAMERA PREVIEW & GOOGLE MEET PiP COMPOSITOR (THREAD-SAFE BGR32)
        // ═══════════════════════════════════════════════════════════════════

        private async Task StartCameraInternalAsync()
        {
            await StopCameraInternalAsync().ConfigureAwait(false);
            if (!HasCameraHardware || string.IsNullOrEmpty(SelectedCameraId)) return;

            try
            {
                var svc = new CameraPreviewService();
                svc.FrameArrived += OnCameraFrameArrived;
                svc.ErrorOccurred += OnCameraHardwareError;
                _cameraService = svc;

                var profile = new CameraProfile
                {
                    SelectedCameraId = SelectedCameraId,
                    Width = 640,
                    Height = 360,
                    FPS = 30
                };

                await svc.StartAsync(profile).ConfigureAwait(false);
                Log.Information("[LiveBroadcastMedia] Camera started on DeviceId={Id}", SelectedCameraId);
            }
            catch (Exception ex)
            {
                Log.Warning("[LiveBroadcastMedia] Error starting camera: {Err}", ex.Message);
                IsCameraEnabled = false;
                HardwareWarning?.Invoke(this, $"Không thể khởi động Camera: {ex.Message}");
                RaiseStateChanged();
            }
        }

        private async Task StopCameraInternalAsync()
        {
            var svc = _cameraService;
            _cameraService = null;
            if (svc != null)
            {
                svc.FrameArrived -= OnCameraFrameArrived;
                svc.ErrorOccurred -= OnCameraHardwareError;
                try
                {
                    await svc.StopAsync().ConfigureAwait(false);
                    svc.Dispose();
                }
                catch { }
            }

            lock (_cameraFrameLock)
            {
                _latestCamBgr32 = null;
                _latestCamPreviewBitmap = null;
                _latestCamWidth = 0;
                _latestCamHeight = 0;
            }
        }

        private void OnCameraHardwareError(string err)
        {
            Log.Warning("[LiveBroadcastMedia] Camera hardware error/unplugged: {Err}", err);
            IsCameraEnabled = false;
            _ = StopCameraInternalAsync();
            HardwareWarning?.Invoke(this, "Camera vừa ngắt kết nối hoặc đang bận — Đã tự động tắt Camera.");
            RaiseStateChanged();
        }

        private void OnCameraFrameArrived(BitmapSource frame)
        {
            if (!IsCameraEnabled || frame == null) return;
            try
            {
                BitmapSource src = frame.Format == PixelFormats.Bgr32
                    ? frame
                    : new FormatConvertedBitmap(frame, PixelFormats.Bgr32, null, 0);
                if (!src.IsFrozen && src.CanFreeze) src.Freeze();

                int w = src.PixelWidth;
                int h = src.PixelHeight;
                if (w <= 0 || h <= 0) return;

                int stride = w * 4;
                var pixels = new byte[stride * h];
                src.CopyPixels(pixels, stride, 0);

                lock (_cameraFrameLock)
                {
                    _latestCamBgr32 = pixels;
                    _latestCamWidth = w;
                    _latestCamHeight = h;
                    _latestCamPreviewBitmap = src;
                }
                Interlocked.Increment(ref _cameraFrameSeq);
            }
            catch { }
        }

        /// <summary>
        /// Ghép trực tiếp ô Camera Giáo viên (bo góc 16:9 chuẩn Google Meet hoặc hình tròn kiểu SmartTouch)
        /// lên khung hình phát sóng Bgr32 trên ThreadPool mà không dùng GDI/Dispatcher (0ms UI block, < 1ms CPU).
        /// </summary>
        public BitmapSource CompositeCameraTileIfActive(BitmapSource screenFrame)
        {
            if (!IsCameraEnabled || screenFrame == null) return screenFrame;

            byte[]? camPixels;
            int camW, camH;
            lock (_cameraFrameLock)
            {
                camPixels = _latestCamBgr32;
                camW = _latestCamWidth;
                camH = _latestCamHeight;
            }

            if (camPixels == null || camW <= 16 || camH <= 16) return screenFrame;

            int dstW = screenFrame.PixelWidth;
            int dstH = screenFrame.PixelHeight;
            if (dstW < 240 || dstH < 180) return screenFrame;

            try
            {
                BitmapSource bgrScreen = screenFrame.Format == PixelFormats.Bgr32
                    ? screenFrame
                    : new FormatConvertedBitmap(screenFrame, PixelFormats.Bgr32, null, 0);

                int dstStride = dstW * 4;
                var dstPixels = new byte[dstStride * dstH];
                bgrScreen.CopyPixels(dstPixels, dstStride, 0);

                bool isCircle = CameraShape == MeetCameraShape.SmartTouchCircle;
                int tileW = isCircle
                    ? Math.Clamp((int)(dstH * 0.23), 120, 260)
                    : Math.Clamp((int)(dstW * 0.21), 180, 380);
                int tileH = isCircle ? tileW : (tileW * 9 / 16);

                int margin = Math.Max(16, (int)(dstW * 0.015));
                int startX = CameraCorner == MeetCameraCorner.BottomLeft ? margin : (dstW - tileW - margin);
                int startY = CameraCorner == MeetCameraCorner.TopRight ? margin : (dstH - tileH - margin);

                bool mirror = IsCameraMirrored;
                int camStride = camW * 4;
                int cornerRadius = isCircle ? (tileW / 2) : Math.Max(12, tileH / 10);
                int borderPx = Math.Max(2, tileW / 95);

                // Tỷ lệ cắt trung tâm (Center-Crop) để không méo mặt Giáo viên
                double targetAspect = (double)tileW / tileH;
                double srcAspect = (double)camW / camH;
                int cropX = 0, cropY = 0, cropW = camW, cropH = camH;
                if (srcAspect > targetAspect)
                {
                    cropW = (int)Math.Round(camH * targetAspect);
                    cropX = (camW - cropW) / 2;
                }
                else if (srcAspect < targetAspect)
                {
                    cropH = (int)Math.Round(camW / targetAspect);
                    cropY = (camH - cropH) / 2;
                }

                float cx = tileW * 0.5f;
                float cy = tileH * 0.5f;
                float rCircle = tileW * 0.5f;
                float rCircleInner = rCircle - borderPx;

                for (int ty = 0; ty < tileH; ty++)
                {
                    int dy = startY + ty;
                    if (dy < 0 || dy >= dstH) continue;

                    for (int tx = 0; tx < tileW; tx++)
                    {
                        int dx = startX + tx;
                        if (dx < 0 || dx >= dstW) continue;

                        bool inside;
                        bool isBorder;
                        if (isCircle)
                        {
                            float ddx = tx + 0.5f - cx;
                            float ddy = ty + 0.5f - cy;
                            float distSq = ddx * ddx + ddy * ddy;
                            inside = distSq <= rCircle * rCircle;
                            isBorder = inside && distSq >= rCircleInner * rCircleInner;
                        }
                        else
                        {
                            int rx = tx < cornerRadius ? (cornerRadius - tx) : (tx >= tileW - cornerRadius ? (tx - (tileW - cornerRadius - 1)) : 0);
                            int ry = ty < cornerRadius ? (cornerRadius - ty) : (ty >= tileH - cornerRadius ? (ty - (tileH - cornerRadius - 1)) : 0);
                            int distSq = rx * rx + ry * ry;
                            inside = (rx == 0 || ry == 0) || (distSq <= cornerRadius * cornerRadius);
                            if (!inside) continue;

                            bool nearEdge = tx < borderPx || tx >= tileW - borderPx || ty < borderPx || ty >= tileH - borderPx;
                            int innerR = Math.Max(1, cornerRadius - borderPx);
                            bool nearCorner = rx > 0 && ry > 0 && distSq >= innerR * innerR;
                            isBorder = nearEdge || nearCorner;
                        }

                        if (!inside) continue;

                        int dstOff = dy * dstStride + dx * 4;
                        if (isBorder)
                        {
                            // Viền xanh sáng kiểu Google Meet Active Speaker (#8AB4F8 -> BGR: F8, B4, 8A)
                            dstPixels[dstOff] = 0xF8;
                            dstPixels[dstOff + 1] = 0xB4;
                            dstPixels[dstOff + 2] = 0x8A;
                            dstPixels[dstOff + 3] = 0xFF;
                        }
                        else
                        {
                            double u = (double)tx / tileW;
                            if (mirror) u = 1.0 - u;
                            double v = (double)ty / tileH;

                            int sx = Math.Clamp(cropX + (int)(u * cropW), 0, camW - 1);
                            int sy = Math.Clamp(cropY + (int)(v * cropH), 0, camH - 1);
                            int srcOff = sy * camStride + sx * 4;

                            dstPixels[dstOff] = camPixels[srcOff];
                            dstPixels[dstOff + 1] = camPixels[srcOff + 1];
                            dstPixels[dstOff + 2] = camPixels[srcOff + 2];
                            dstPixels[dstOff + 3] = 0xFF;
                        }
                    }
                }

                var composited = BitmapSource.Create(dstW, dstH, 96, 96, PixelFormats.Bgr32, null, dstPixels, dstStride);
                composited.Freeze();
                return composited;
            }
            catch
            {
                return screenFrame;
            }
        }

        private void RaiseStateChanged()
        {
            try { StateChanged?.Invoke(this, EventArgs.Empty); } catch { }
        }

        public void Dispose()
        {
            _ = StopSessionAsync();
        }
    }
}

