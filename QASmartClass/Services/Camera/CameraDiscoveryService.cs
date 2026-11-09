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
    /// <summary>
    /// Khóa đồng bộ hóa toàn cục cho mọi thao tác mở/quét/đóng OpenCvSharp VideoCapture,
    /// ngăn ngừa xung đột đa luồng Native C++ (DirectShow / MediaFoundation) gây văng ứng dụng.
    /// </summary>
    internal static readonly System.Threading.SemaphoreSlim HardwareLock = new(1, 1);

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
        HardwareLock.Wait();
        try
        {
            _cachedCameras.Clear();
            var wmiNames = GetDeviceNamesFromWmi();

            // Khi WMI không ghi nhận thiết bị Camera vật lý nào, chỉ kiểm tra nhanh DSHOW cổng 0-1
            // (hỗ trợ Virtual Camera như OBS) và dừng ngay khi không mở được, tuyệt đối không gọi
            // VideoCaptureAPIs.ANY (MSMF) để tránh đụng độ MediaFoundation và tránh treo quét 5-8 giây.
            int maxProbe = wmiNames.Count > 0 ? Math.Clamp(wmiNames.Count + 1, 1, 5) : 2;

            for (int i = 0; i < maxProbe; i++)
            {
                VideoCapture? capture = null;
                try
                {
                    try
                    {
                        capture = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
                    }
                    catch { }

                    bool isOpened = capture != null && capture.IsOpened();

                    // Chỉ fallback sang MSMF/ANY khi WMI xác nhận thực sự có thiết bị Camera vật lý ở index tương ứng
                    if (!isOpened && wmiNames.Count > 0 && i < wmiNames.Count)
                    {
                        try { capture?.Release(); } catch { }
                        try { capture?.Dispose(); } catch { }
                        capture = null;

                        try
                        {
                            capture = new VideoCapture(i, VideoCaptureAPIs.ANY);
                            isOpened = capture != null && capture.IsOpened();
                        }
                        catch { }
                    }

                    if (isOpened && capture != null)
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
                    }
                    else if (wmiNames.Count == 0 || i >= wmiNames.Count)
                    {
                        // Dừng sớm ngay khi hết dải thiết bị thực tế
                        break;
                    }
                }
                catch
                {
                    if (wmiNames.Count == 0)
                        break;
                }
                finally
                {
                    if (capture != null)
                    {
                        try { capture.Release(); } catch { }
                        try { capture.Dispose(); } catch { }
                    }
                }
            }

            return new List<CameraDevice>(_cachedCameras);
        }
        finally
        {
            HardwareLock.Release();
        }
    }

    internal static List<string> GetDeviceNamesFromWmi()
    {
        var names = new List<string>();
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT Name, ConfigManagerErrorCode FROM Win32_PnPEntity WHERE PNPClass = 'Camera' OR PNPClass = 'Image'");
            foreach (var obj in searcher.Get())
            {
                if (obj["ConfigManagerErrorCode"] != null &&
                    int.TryParse(obj["ConfigManagerErrorCode"]?.ToString(), out int errCode) &&
                    errCode != 0)
                {
                    continue;
                }

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

        if (!HardwareLock.Wait(2000))
            return false;

        VideoCapture? capture = null;
        try
        {
            capture = new VideoCapture(index, VideoCaptureAPIs.DSHOW);
            return capture.IsOpened();
        }
        catch
        {
            return false;
        }
        finally
        {
            if (capture != null)
            {
                try { capture.Release(); } catch { }
                try { capture.Dispose(); } catch { }
            }
            HardwareLock.Release();
        }
    }
}
