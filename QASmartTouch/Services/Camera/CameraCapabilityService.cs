using QASmartTouch.Models.Camera;
using OpenCvSharp;

namespace QASmartTouch.Services.Camera;

/// <summary>
/// Service for querying camera capabilities (resolutions, frame rates)
/// </summary>
public class CameraCapabilityService
{
    /// <summary>
    /// Gets available resolution options for a camera device
    /// </summary>
    public List<ResolutionOption> GetAvailableResolutions(CameraDevice device)
    {
        var resolutions = new List<ResolutionOption>();

        if (!int.TryParse(device.DeviceId, out int index))
            return resolutions;

        // Common resolution presets to test
        var presetsToTest = new[]
        {
            new { Width = 640, Height = 480 },
            new { Width = 800, Height = 600 },
            new { Width = 1280, Height = 720 },
            new { Width = 1920, Height = 1080 },
            new { Width = 2560, Height = 1440 },
            new { Width = 3840, Height = 2160 }
        };

        var fpsToTest = new[] { 15, 30, 60 };

        try
        {
            using var capture = new VideoCapture(index);
            if (!capture.IsOpened())
                return resolutions;

            foreach (var preset in presetsToTest)
            {
                foreach (var fps in fpsToTest)
                {
                    // Try to set the resolution
                    capture.Set(VideoCaptureProperties.FrameWidth, preset.Width);
                    capture.Set(VideoCaptureProperties.FrameHeight, preset.Height);
                    capture.Set(VideoCaptureProperties.Fps, fps);

                    // Read back what was actually set
                    var actualWidth = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                    var actualHeight = (int)capture.Get(VideoCaptureProperties.FrameHeight);
                    var actualFps = (int)capture.Get(VideoCaptureProperties.Fps);

                    // If it matches what we requested, it's supported
                    if (actualWidth == preset.Width && actualHeight == preset.Height && actualFps > 0)
                    {
                        var option = new ResolutionOption
                        {
                            Width = actualWidth,
                            Height = actualHeight,
                            FPS = actualFps > 0 ? actualFps : fps
                        };

                        // Avoid duplicates
                        if (!resolutions.Any(r => r.Width == option.Width && 
                                                  r.Height == option.Height && 
                                                  r.FPS == option.FPS))
                        {
                            resolutions.Add(option);
                        }
                    }
                }
            }
        }
        catch
        {
            // If capability query fails, return common defaults
        }

        // If no resolutions found, add common defaults
        if (resolutions.Count == 0)
        {
            resolutions.Add(ResolutionOption.Presets.VGA_30);
            resolutions.Add(ResolutionOption.Presets.HD_30);
            resolutions.Add(ResolutionOption.Presets.FullHD_30);
        }

        return resolutions.OrderBy(r => r.Width).ThenBy(r => r.FPS).ToList();
    }

    /// <summary>
    /// Asynchronously gets available resolutions
    /// </summary>
    public async Task<List<ResolutionOption>> GetAvailableResolutionsAsync(CameraDevice device)
    {
        return await Task.Run(() => GetAvailableResolutions(device));
    }

    /// <summary>
    /// Tests if a specific resolution is supported
    /// </summary>
    public bool IsResolutionSupported(CameraDevice device, int width, int height, int fps)
    {
        if (!int.TryParse(device.DeviceId, out int index))
            return false;

        try
        {
            using var capture = new VideoCapture(index);
            if (!capture.IsOpened())
                return false;

            capture.Set(VideoCaptureProperties.FrameWidth, width);
            capture.Set(VideoCaptureProperties.FrameHeight, height);
            capture.Set(VideoCaptureProperties.Fps, fps);

            var actualWidth = (int)capture.Get(VideoCaptureProperties.FrameWidth);
            var actualHeight = (int)capture.Get(VideoCaptureProperties.FrameHeight);

            return actualWidth == width && actualHeight == height;
        }
        catch
        {
            return false;
        }
    }
}
