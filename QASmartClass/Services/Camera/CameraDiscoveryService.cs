using QASmartTouch.Models.Camera;
using OpenCvSharp;

namespace QASmartTouch.Services.Camera;

/// <summary>
/// Service for discovering available camera devices in the system
/// </summary>
public class CameraDiscoveryService
{
    private List<CameraDevice> _cachedCameras = new();

    /// <summary>
    /// Gets all available cameras in the system
    /// </summary>
    public List<CameraDevice> GetAllCameras()
    {
        if (_cachedCameras.Count > 0)
            return _cachedCameras;

        return RefreshCameras();
    }

    /// <summary>
    /// Refreshes the camera list by scanning the system
    /// </summary>
    public List<CameraDevice> RefreshCameras()
    {
        _cachedCameras.Clear();

        // Scan for cameras (typically 0-9 are common indices)
        for (int i = 0; i < 10; i++)
        {
            try
            {
                using var capture = new VideoCapture(i);
                if (capture.IsOpened())
                {
                    var device = new CameraDevice
                    {
                        DeviceId = i.ToString(),
                        FriendlyName = $"Camera {i}",
                        Manufacturer = "Unknown",
                        IsAvailable = true
                    };

                    // Try to get more detailed name from backend
                    var backendName = capture.GetBackendName();
                    if (!string.IsNullOrEmpty(backendName))
                    {
                        device.FriendlyName = $"Camera {i} ({backendName})";
                    }

                    _cachedCameras.Add(device);
                }
            }
            catch
            {
                // Camera not available or error opening
                continue;
            }
        }

        return _cachedCameras;
    }

    /// <summary>
    /// Asynchronously refreshes the camera list
    /// </summary>
    public async Task<List<CameraDevice>> RefreshCamerasAsync()
    {
        return await Task.Run(() => RefreshCameras());
    }

    /// <summary>
    /// Gets a specific camera by ID
    /// </summary>
    public CameraDevice? GetCameraById(string deviceId)
    {
        return _cachedCameras.FirstOrDefault(c => c.DeviceId == deviceId);
    }

    /// <summary>
    /// Checks if a camera is currently available
    /// </summary>
    public bool IsCameraAvailable(string deviceId)
    {
        if (!int.TryParse(deviceId, out int index))
            return false;

        try
        {
            using var capture = new VideoCapture(index);
            return capture.IsOpened();
        }
        catch
        {
            return false;
        }
    }
}
