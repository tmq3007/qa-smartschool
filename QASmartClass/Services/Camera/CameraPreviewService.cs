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
    private Task? _captureTask;
    private bool _isRunning;

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
        if (_isRunning)
        {
            await StopAsync();
        }

        _isRunning = true;
        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;

        try
        {
            bool isRtsp = profile.SelectedCameraId == "RTSP" || (!string.IsNullOrEmpty(rtspUrl) && rtspUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase));
            int timeoutMs = isRtsp ? 4000 : 10000;

            var initTask = Task.Run(() =>
            {
                VideoCapture? capture = null;
                if (isRtsp)
                {
                    string url = !string.IsNullOrEmpty(rtspUrl) ? rtspUrl : (profile.SelectedCameraId.StartsWith("rtsp://") ? profile.SelectedCameraId : "rtsp://127.0.0.1:8554/live");
                    capture = new VideoCapture(url);
                }
                else
                {
                    int targetIndex = 0;
                    if (!string.IsNullOrEmpty(profile.SelectedCameraId) && int.TryParse(profile.SelectedCameraId, out int parsedIdx))
                    {
                        targetIndex = parsedIdx;
                    }

                    // 1. Thử mở camera theo targetIndex bằng DirectShow (DSHOW)
                    try
                    {
                        capture = new VideoCapture(targetIndex, VideoCaptureAPIs.DSHOW);
                    }
                    catch { }

                    // 2. Nếu DSHOW không mở được, thử qua default API (MSMF)
                    if (capture == null || !capture.IsOpened())
                    {
                        capture?.Dispose();
                        try
                        {
                            capture = new VideoCapture(targetIndex, VideoCaptureAPIs.ANY);
                        }
                        catch { }
                    }

                    // 3. Nếu targetIndex không hoạt động, quét các index khác (0-4)
                    if (capture == null || !capture.IsOpened())
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (i == targetIndex) continue;
                            capture?.Dispose();
                            try
                            {
                                capture = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
                                if (capture.IsOpened()) break;

                                capture.Dispose();
                                capture = new VideoCapture(i, VideoCaptureAPIs.ANY);
                                if (capture.IsOpened()) break;
                            }
                            catch { }
                        }
                    }
                }
                return capture;
            });

            var timeoutTask = Task.Delay(timeoutMs, cancellationToken);
            var completedTask = await Task.WhenAny(initTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _isRunning = false;
                _cts.Cancel();
                throw new TimeoutException(isRtsp ? "Kết nối camera RTSP quá hạn (4 giây)." : "Khởi động Camera quá hạn (10 giây).");
            }

            _capture = await initTask;

            if (_capture == null || !_capture.IsOpened())
            {
                _isRunning = false;
                ErrorOccurred?.Invoke("Không tìm thấy hoặc không mở được thiết bị Camera.");
                return;
            }

            // Set camera properties safely
            try
            {
                if (!isRtsp)
                {
                    try { _capture.Set(VideoCaptureProperties.FourCC, VideoWriter.FourCC('M', 'J', 'P', 'G')); } catch { }

                    if (profile.Width > 0 && profile.Height > 0)
                    {
                        _capture.Set(VideoCaptureProperties.FrameWidth, profile.Width);
                        _capture.Set(VideoCaptureProperties.FrameHeight, profile.Height);
                    }
                    if (profile.FPS > 0)
                    {
                        _capture.Set(VideoCaptureProperties.Fps, profile.FPS);
                    }
                }
            }
            catch { }

            // Start capture loop in background
            _captureTask = Task.Run(() => CaptureLoop(cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Lỗi camera: {ex.Message}");
            _isRunning = false;
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
            throw;
        }
    }

    /// <summary>
    /// Stops the camera preview
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _isRunning = false;

        // Cancel the capture task
        _cts?.Cancel();

        // Wait for capture task to complete
        if (_captureTask != null)
        {
            try
            {
                await _captureTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when canceling
            }
        }

        // Cleanup resources
        _capture?.Release();
        _capture?.Dispose();
        _capture = null;

        _cts?.Dispose();
        _cts = null;
        _captureTask = null;
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
                if (_capture == null || !_capture.IsOpened())
                {
                    ErrorOccurred?.Invoke("Kết nối Camera đã bị đóng hoặc gián đoạn.");
                    _isRunning = false;
                    break;
                }

                // Read frame from camera
                if (_capture.Read(frame) && !frame.Empty())
                {
                    consecutiveFailures = 0;
                    // Convert Mat to BitmapSource for WPF
                    var bitmap = ConvertMatToBitmapSource(frame);
                    
                    // Freeze for cross-thread access
                    bitmap.Freeze();

                    // Raise event on UI thread
                    FrameArrived?.Invoke(bitmap);
                }
                else
                {
                    consecutiveFailures++;
                    if (consecutiveFailures == 25)
                    {
                        ErrorOccurred?.Invoke("Không đọc được hình ảnh từ Camera. Có thể camera đang bị phần mềm khác chiếm dụng hoặc bị chặn quyền riêng tư.");
                    }
                    Thread.Sleep(20);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Lỗi đọc khung hình: {ex.Message}");
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
        StopAsync().Wait();
        GC.SuppressFinalize(this);
    }
}
