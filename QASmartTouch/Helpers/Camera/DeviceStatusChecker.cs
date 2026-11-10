using OpenCvSharp;

namespace QASmartTouch.Helpers.Camera;

/// <summary>
/// Utility class for checking camera device status
/// </summary>
public class DeviceStatusChecker
{
    /// <summary>
    /// Checks if a camera device is currently in use by another application
    /// </summary>
    public static bool IsCameraBusy(string deviceId)
    {
        if (!int.TryParse(deviceId, out int index))
            return true;

        try
        {
            using var capture = new VideoCapture(index);
            if (!capture.IsOpened())
                return true;

            // Try to read a frame to ensure camera is actually accessible
            using var testFrame = new Mat();
            var canRead = capture.Read(testFrame);
            
            return !canRead || testFrame.Empty();
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Attempts to open a camera with retry logic
    /// </summary>
    public static async Task<VideoCapture?> OpenCameraWithRetryAsync(
        string deviceId, 
        int maxRetries = 3, 
        int delayMs = 1000)
    {
        if (!int.TryParse(deviceId, out int index))
            return null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var capture = new VideoCapture(index);
                if (capture.IsOpened())
                {
                    // Verify we can actually read frames
                    using var testFrame = new Mat();
                    if (capture.Read(testFrame) && !testFrame.Empty())
                    {
                        return capture;
                    }
                }

                capture?.Dispose();
            }
            catch
            {
                // Retry on exception
            }

            if (attempt < maxRetries - 1)
            {
                await Task.Delay(delayMs);
            }
        }

        return null;
    }

    /// <summary>
    /// Tests camera connectivity
    /// </summary>
    public static CameraStatus CheckCameraStatus(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return CameraStatus.InvalidId;

        if (!int.TryParse(deviceId, out int index))
            return CameraStatus.InvalidId;

        try
        {
            using var capture = new VideoCapture(index);
            
            if (!capture.IsOpened())
                return CameraStatus.NotFound;

            using var testFrame = new Mat();
            if (!capture.Read(testFrame))
                return CameraStatus.Busy;

            if (testFrame.Empty())
                return CameraStatus.NoSignal;

            return CameraStatus.Available;
        }
        catch (Exception)
        {
            return CameraStatus.Error;
        }
    }

    /// <summary>
    /// Asynchronously checks camera status
    /// </summary>
    public static async Task<CameraStatus> CheckCameraStatusAsync(string deviceId)
    {
        return await Task.Run(() => CheckCameraStatus(deviceId));
    }
}

/// <summary>
/// Represents the status of a camera device
/// </summary>
public enum CameraStatus
{
    /// <summary>Camera is available and ready to use</summary>
    Available,
    
    /// <summary>Camera not found at the specified index</summary>
    NotFound,
    
    /// <summary>Camera is busy (in use by another application)</summary>
    Busy,
    
    /// <summary>Camera opened but no signal/frames</summary>
    NoSignal,
    
    /// <summary>Invalid device ID</summary>
    InvalidId,
    
    /// <summary>Error occurred while checking</summary>
    Error
}
