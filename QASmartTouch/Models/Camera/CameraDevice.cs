namespace QASmartTouch.Models.Camera;

/// <summary>
/// Represents a camera device in the system
/// </summary>
public class CameraDevice
{
    /// <summary>
    /// Unique device identifier (e.g., device index or hardware ID)
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// User-friendly name of the camera
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer name (if available)
    /// </summary>
    public string Manufacturer { get; set; } = "Unknown";

    /// <summary>
    /// Indicates whether the camera is currently available for use
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    public override string ToString()
    {
        return FriendlyName;
    }
}
