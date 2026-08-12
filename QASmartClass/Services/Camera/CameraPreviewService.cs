using QASmartTouch.Models.Camera;
using OpenCvSharp;
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
            // Use Task.Run to run initialization in background
            var initTask = Task.Run(() =>
            {
                VideoCapture capture;
                if (profile.SelectedCameraId == "RTSP" || (!string.IsNullOrEmpty(rtspUrl) && rtspUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase)))
                {
                    string url = !string.IsNullOrEmpty(rtspUrl) ? rtspUrl : (profile.SelectedCameraId.StartsWith("rtsp://") ? profile.SelectedCameraId : "rtsp://127.0.0.1:8554/live");
                    capture = new VideoCapture(url);
                }
                else if (int.TryParse(profile.SelectedCameraId, out int cameraIndex))
                {
                    capture = new VideoCapture(cameraIndex);
                }
                else
                {
                    throw new ArgumentException("Invalid camera identifier");
                }
                return capture;
            });

            // Enforce a 3-second timeout
            var timeoutTask = Task.Delay(3000, cancellationToken);
            var completedTask = await Task.WhenAny(initTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout exceeded
                _isRunning = false;
                _cts.Cancel();
                throw new TimeoutException("Kết nối camera RTSP quá hạn (3 giây). Vui lòng kiểm tra địa chỉ IP và trạng thái mạng.");
            }

            // If initTask completed, get the result (or propagate exception)
            _capture = await initTask;

            if (_capture == null || !_capture.IsOpened())
            {
                _isRunning = false;
                ErrorOccurred?.Invoke("Failed to open camera");
                return;
            }

            // Set camera properties (might fail for RTSP, so catch errors silently)
            try
            {
                if (profile.SelectedCameraId != "RTSP")
                {
                    _capture.Set(VideoCaptureProperties.FrameWidth, profile.Width);
                    _capture.Set(VideoCaptureProperties.FrameHeight, profile.Height);
                    _capture.Set(VideoCaptureProperties.Fps, profile.FPS);
                }
            }
            catch { }

            // Start capture loop in background
            _captureTask = Task.Run(() => CaptureLoop(cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Error starting camera: {ex.Message}");
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

        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                if (_capture == null || !_capture.IsOpened())
                {
                    ErrorOccurred?.Invoke("Camera connection lost");
                    _isRunning = false;
                    break;
                }

                // Read frame from camera
                if (_capture.Read(frame) && !frame.Empty())
                {
                    // Convert Mat to BitmapSource for WPF
                    var bitmap = ConvertMatToBitmapSource(frame);
                    
                    // Freeze for cross-thread access
                    bitmap.Freeze();

                    // Raise event on UI thread
                    FrameArrived?.Invoke(bitmap);
                }
                else
                {
                    // Frame read failed, wait a bit and retry
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Frame capture error: {ex.Message}");
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
