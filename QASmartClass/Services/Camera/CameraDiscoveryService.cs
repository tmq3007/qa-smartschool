using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        var wmiNames = GetDeviceNamesFromWmi();

        // Scan for cameras (typically 0-4 are common indices in classroom setups)
        for (int i = 0; i < 5; i++)
        {
            try
            {
                VideoCapture? capture = null;
                try
                {
                    capture = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
                }
                catch { }

                if (capture == null || !capture.IsOpened())
                {
                    capture?.Dispose();
                    try
                    {
                        capture = new VideoCapture(i, VideoCaptureAPIs.ANY);
                    }
                    catch { }
                }

                if (capture != null && capture.IsOpened())
                {
                    string friendlyName = (wmiNames.Count > i && !string.IsNullOrWhiteSpace(wmiNames[i]))
                        ? wmiNames[i]
                        : $"Camera {i}";

                    var backendName = capture.GetBackendName();
                    if (!string.IsNullOrEmpty(backendName) && !friendlyName.Contains(backendName))
                    {
                        friendlyName = $"{friendlyName} ({backendName})";
                    }

                    _cachedCameras.Add(new CameraDevice
                    {
                        DeviceId = i.ToString(),
                        FriendlyName = friendlyName,
                        Manufacturer = "Windows UVC",
                        IsAvailable = true
                    });

                    capture.Release();
                    capture.Dispose();
                }
            }
            catch
            {
                continue;
            }
        }

        return _cachedCameras;
    }

    private List<string> GetDeviceNamesFromWmi()
    {
        var names = new List<string>();
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT Name FROM Win32_PnPEntity WHERE PNPClass = 'Camera' OR PNPClass = 'Image'");
            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                {
                    names.Add(name);
                }
            }
        }
        catch { }
        return names;
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
