namespace QASmartTouch.Models.Camera;

/// <summary>
/// Represents a saved camera configuration profile
/// </summary>
public class CameraProfile
{
    /// <summary>
    /// ID of the selected camera device
    /// </summary>
    public string SelectedCameraId { get; set; } = string.Empty;

    /// <summary>
    /// Configured width in pixels
    /// </summary>
    public int Width { get; set; } = 1280;

    /// <summary>
    /// Configured height in pixels
    /// </summary>
    public int Height { get; set; } = 720;

    /// <summary>
    /// Configured frames per second
    /// </summary>
    public int FPS { get; set; } = 30;

    /// <summary>
    /// Whether to automatically start camera on application launch
    /// </summary>
    public bool AutoStartOnLaunch { get; set; } = true;

    /// <summary>
    /// Validates the profile configuration
    /// </summary>
    public bool IsValid()
    {
        return Width >= 320 && Width <= 7680 &&
               Height >= 240 && Height <= 4320 &&
               FPS >= 1 && FPS <= 120;
    }

    /// <summary>
    /// Creates a default profile with common settings
    /// </summary>
    public static CameraProfile CreateDefault()
    {
        return new CameraProfile
        {
            SelectedCameraId = string.Empty,
            Width = 1280,
            Height = 720,
            FPS = 30,
            AutoStartOnLaunch = true
        };
    }
}
