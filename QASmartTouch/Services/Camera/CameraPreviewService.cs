using QASmartTouch.Models.Camera;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QASmartTouch.Services.Camera;

/// <summary>
/// Service for previewing camera stream
/// </summary>
public class CameraPreviewService : IDisposable
{
    private VideoCapture? _capture;
    private CancellationTokenSource? _cts;
    private Task<VideoCapture?>? _initTask;
    private Task? _captureTask;
    private volatile bool _isRunning;

    /// <summary>
    /// Event raised when a new frame is available
    /// </summary>
    public event Action<BitmapSource>? FrameArrived;

    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    public event Action<string>? ErrorOccurred;

    /// <summary>
    /// Indicates if the preview is currently running
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Starts the camera preview with the specified profile and optional RTSP URL
    /// </summary>
    public async Task StartAsync(CameraProfile profile, string? rtspUrl = null)
    {
        if (_isRunning || _initTask != null || _captureTask != null || _capture != null)
        {
            await StopAsync();
        }

        var localCts = new CancellationTokenSource();
        _cts = localCts;
        var cancellationToken = localCts.Token;
        _isRunning = true;

        try
        {
            bool isRtsp = profile.SelectedCameraId == "RTSP" || (!string.IsNullOrEmpty(rtspUrl) && rtspUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase));
            int timeoutMs = isRtsp ? 4000 : 8000;

            var initTask = Task.Run(async () =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                bool lockAcquired = false;
                VideoCapture? capture = null;
                try
                {
                    await CameraDiscoveryService.HardwareLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    lockAcquired = true;

                    if (cancellationToken.IsCancellationRequested)
                        return null;

                    if (isRtsp)
                    {
                        string url = !string.IsNullOrEmpty(rtspUrl)
                            ? rtspUrl
                            : (profile.SelectedCameraId.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase)
                                ? profile.SelectedCameraId
                                : "rtsp://127.0.0.1:8554/live");
                        capture = new VideoCapture(url);
                    }
                    else
                    {
                        int targetIndex = 0;
                        if (!string.IsNullOrEmpty(profile.SelectedCameraId) && int.TryParse(profile.SelectedCameraId, out int parsedIdx))
                        {
                            targetIndex = parsedIdx;
                        }

                        // 1. Ưu tiên mở camera theo targetIndex bằng DirectShow (DSHOW)
                        try
                        {
                            capture = new VideoCapture(targetIndex, VideoCaptureAPIs.DSHOW);
                        }
                        catch { }

                        bool isOpened = capture != null && capture.IsOpened();

                        // 2. Chỉ thử fallback sang MSMF/ANY hoặc quét cổng lân cận khi WMI xác nhận có phần cứng Camera thật
                        if (!isOpened && !cancellationToken.IsCancellationRequested)
                        {
                            ReleaseAndDisposeCapture(ref capture);

                            var wmiNames = CameraDiscoveryService.GetDeviceNamesFromWmi();
                            if (wmiNames.Count > 0)
                            {
                                try
                                {
                                    capture = new VideoCapture(targetIndex, VideoCaptureAPIs.ANY);
                                    isOpened = capture != null && capture.IsOpened();
                                }
                                catch { }

                                if (!isOpened && !cancellationToken.IsCancellationRequested)
                                {
                                    ReleaseAndDisposeCapture(ref capture);
                                    int maxFallback = Math.Clamp(wmiNames.Count + 1, 1, 5);
                                    for (int i = 0; i < maxFallback && !cancellationToken.IsCancellationRequested; i++)
                                    {
                                        if (i == targetIndex) continue;
                                        try
                                        {
                                            capture = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
                                            if (capture.IsOpened())
                                            {
                                                isOpened = true;
                                                break;
                                            }
                                            ReleaseAndDisposeCapture(ref capture);

                                            if (i < wmiNames.Count && !cancellationToken.IsCancellationRequested)
                                            {
                                                capture = new VideoCapture(i, VideoCaptureAPIs.ANY);
                                                if (capture.IsOpened())
                                                {
                                                    isOpened = true;
                                                    break;
                                                }
                                                ReleaseAndDisposeCapture(ref capture);
                                            }
                                        }
                                        catch
                                        {
                                            ReleaseAndDisposeCapture(ref capture);
                                        }
                                    }
                                }
                            }
                            else if (targetIndex != 0)
                            {
                                // Thử cổng DSHOW 0 cho trường hợp Virtual Camera (OBS)
                                try
                                {
                                    capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
                                    isOpened = capture != null && capture.IsOpened();
                                }
                                catch { }
                            }
                        }

                        if (!isOpened || cancellationToken.IsCancellationRequested)
                        {
                            ReleaseAndDisposeCapture(ref capture);
                            return null;
                        }
                    }

                    if (capture == null || !capture.IsOpened() || cancellationToken.IsCancellationRequested)
                    {
                        ReleaseAndDisposeCapture(ref capture);
                        return null;
                    }

                    // Thiết lập thông số khung hình an toàn ngay trong vùng khóa phần cứng
                    if (!isRtsp)
                    {
                        try { capture.Set(VideoCaptureProperties.FourCC, VideoWriter.FourCC('M', 'J', 'P', 'G')); } catch { }
                        try
                        {
                            if (profile.Width > 0 && profile.Height > 0)
                            {
                                capture.Set(VideoCaptureProperties.FrameWidth, profile.Width);
                                capture.Set(VideoCaptureProperties.FrameHeight, profile.Height);
                            }
                            if (profile.FPS > 0)
                            {
                                capture.Set(VideoCaptureProperties.Fps, profile.FPS);
                            }
                        }
                        catch { }
                    }

                    return capture;
                }
                catch (OperationCanceledException)
                {
                    if (lockAcquired)
                    {
                        ReleaseAndDisposeCapture(ref capture);
                    }
                    return null;
                }
                catch
                {
                    if (lockAcquired)
                    {
                        ReleaseAndDisposeCapture(ref capture);
                    }
                    return null;
                }
                finally
                {
                    if (lockAcquired)
                    {
                        CameraDiscoveryService.HardwareLock.Release();
                    }
                }
            });

            _initTask = initTask;

            Task timeoutTask;
            try
            {
                timeoutTask = Task.Delay(timeoutMs, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            var completedTask = await Task.WhenAny(initTask, timeoutTask);

            // Nếu đã bị hủy chủ động bởi StopAsync / đóng cửa sổ thì thoát êm, không ném lỗi giả
            if (cancellationToken.IsCancellationRequested || !_isRunning)
            {
                return;
            }

            if (completedTask == timeoutTask)
            {
                _isRunning = false;
                try { localCts.Cancel(); } catch { }
                _ = initTask.ContinueWith(async t =>
                {
                    var lateCap = t.Status == TaskStatus.RanToCompletion ? t.Result : null;
                    if (lateCap != null)
                    {
                        await CameraDiscoveryService.HardwareLock.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            ReleaseAndDisposeCapture(ref lateCap);
                        }
                        finally
                        {
                            CameraDiscoveryService.HardwareLock.Release();
                        }
                    }
                }, TaskScheduler.Default);

                throw new TimeoutException(isRtsp ? "Kết nối camera RTSP quá hạn (4 giây)." : "Khởi động Camera quá hạn (8 giây).");
            }

            var openedCapture = await initTask;
            if (ReferenceEquals(_initTask, initTask))
            {
                _initTask = null;
            }

            if (cancellationToken.IsCancellationRequested || !_isRunning)
            {
                if (openedCapture != null)
                {
                    await CameraDiscoveryService.HardwareLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        ReleaseAndDisposeCapture(ref openedCapture);
                    }
                    finally
                    {
                        CameraDiscoveryService.HardwareLock.Release();
                    }
                }
                return;
            }

            if (openedCapture == null)
            {
                _isRunning = false;
                ErrorOccurred?.Invoke("Không tìm thấy hoặc không mở được thiết bị Camera.");
                return;
            }

            _capture = openedCapture;

            // Start capture loop in background
            _captureTask = Task.Run(() => CaptureLoop(cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _isRunning = false;
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                ErrorOccurred?.Invoke($"Lỗi camera: {ex.Message}");
            }
            _isRunning = false;
            var cap = _capture;
            _capture = null;
            if (cap != null)
            {
                await CameraDiscoveryService.HardwareLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    ReleaseAndDisposeCapture(ref cap);
                }
                finally
                {
                    CameraDiscoveryService.HardwareLock.Release();
                }
            }
            if (!cancellationToken.IsCancellationRequested)
            {
                throw;
            }
        }
    }

    private static void ReleaseAndDisposeCapture(ref VideoCapture? capture)
    {
        if (capture == null) return;
        try { capture.Release(); } catch { }
        try { capture.Dispose(); } catch { }
        capture = null;
    }

    /// <summary>
    /// Stops the camera preview
    /// </summary>
    public async Task StopAsync()
    {
        var localCts = _cts;
        var initTask = _initTask;
        var captureTask = _captureTask;

        if (!_isRunning && localCts == null && initTask == null && captureTask == null && _capture == null)
            return;

        _isRunning = false;
        _cts = null;
        _initTask = null;
        _captureTask = null;

        try
        {
            localCts?.Cancel();
        }
        catch { }

        if (initTask != null)
        {
            try
            {
                var pendingCapture = await initTask.ConfigureAwait(false);
                if (pendingCapture != null && !ReferenceEquals(pendingCapture, _capture))
                {
                    await CameraDiscoveryService.HardwareLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        ReleaseAndDisposeCapture(ref pendingCapture);
                    }
                    finally
                    {
                        CameraDiscoveryService.HardwareLock.Release();
                    }
                }
            }
            catch { }
        }

        // Wait for capture task to complete
        if (captureTask != null)
        {
            try
            {
                await captureTask.ConfigureAwait(false);
            }
            catch
            {
                // Expected when canceling
            }
        }

        // Cleanup resources safely under hardware lock
        var cap = _capture;
        _capture = null;
        if (cap != null)
        {
            await CameraDiscoveryService.HardwareLock.WaitAsync().ConfigureAwait(false);
            try
            {
                ReleaseAndDisposeCapture(ref cap);
            }
            finally
            {
                CameraDiscoveryService.HardwareLock.Release();
            }
        }

        try
        {
            localCts?.Dispose();
        }
        catch { }
    }

    /// <summary>
    /// Main capture loop running in background
    /// </summary>
    private void CaptureLoop(CancellationToken cancellationToken)
    {
        using var frame = new Mat();
        int consecutiveFailures = 0;

        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                var currentCapture = _capture;
                if (currentCapture == null || !currentCapture.IsOpened())
                {
                    if (_isRunning && !cancellationToken.IsCancellationRequested)
                    {
                        ErrorOccurred?.Invoke("Kết nối Camera đã bị đóng hoặc gián đoạn.");
                    }
                    _isRunning = false;
                    break;
                }

                // Read frame from camera
                if (currentCapture.Read(frame) && !frame.Empty())
                {
                    consecutiveFailures = 0;
                    if (!_isRunning || cancellationToken.IsCancellationRequested)
                        break;

                    // Convert Mat to BitmapSource for WPF
                    var bitmap = ConvertMatToBitmapSource(frame);
                    
                    // Freeze for cross-thread access
                    bitmap.Freeze();

                    // Raise event on UI thread
                    if (_isRunning && !cancellationToken.IsCancellationRequested)
                    {
                        FrameArrived?.Invoke(bitmap);
                    }
                }
                else
                {
                    consecutiveFailures++;
                    if (consecutiveFailures == 25 && _isRunning && !cancellationToken.IsCancellationRequested)
                    {
                        ErrorOccurred?.Invoke("Không đọc được hình ảnh từ Camera. Có thể camera đang bị phần mềm khác chiếm dụng hoặc bị chặn quyền riêng tư.");
                        _isRunning = false;
                        break;
                    }
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                if (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    ErrorOccurred?.Invoke($"Lỗi đọc khung hình: {ex.Message}");
                }
                Thread.Sleep(100); // Wait before retry
            }
        }
    }

    /// <summary>
    /// Converts OpenCV Mat to WPF BitmapSource
    /// </summary>
    private static BitmapSource ConvertMatToBitmapSource(Mat mat)
    {
        // Convert BGR to RGB
        using var rgbMat = new Mat();
        Cv2.CvtColor(mat, rgbMat, ColorConversionCodes.BGR2RGB);
        
        // Create BitmapSource from Mat data
        var bitmapSource = BitmapSource.Create(
            rgbMat.Width,
            rgbMat.Height,
            96,
            96,
            System.Windows.Media.PixelFormats.Rgb24,
            null,
            rgbMat.Data,
            (int)(rgbMat.Step() * rgbMat.Height),
            (int)rgbMat.Step());

        return bitmapSource;
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        if (_isRunning || _cts != null || _initTask != null || _captureTask != null || _capture != null)
        {
            try
            {
                Task.Run(() => StopAsync()).GetAwaiter().GetResult();
            }
            catch { }
        }
        GC.SuppressFinalize(this);
    }
}
