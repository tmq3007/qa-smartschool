namespace QASmartTouch.Models.Camera;

/// <summary>
/// Represents a camera resolution and frame rate option
/// </summary>
public class ResolutionOption
{
    /// <summary>
    /// Width in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Frames per second
    /// </summary>
    public int FPS { get; set; }

    /// <summary>
    /// Returns a formatted string representation (e.g., "1920 x 1080 @ 30 FPS")
    /// </summary>
    public override string ToString()
    {
        return $"{Width} x {Height} @ {FPS} FPS";
    }

    /// <summary>
    /// Common resolution presets
    /// </summary>
    public static class Presets
    {
        public static ResolutionOption VGA_30 => new() { Width = 640, Height = 480, FPS = 30 };
        public static ResolutionOption HD_30 => new() { Width = 1280, Height = 720, FPS = 30 };
        public static ResolutionOption FullHD_30 => new() { Width = 1920, Height = 1080, FPS = 30 };
        public static ResolutionOption HD_60 => new() { Width = 1280, Height = 720, FPS = 60 };
        public static ResolutionOption FullHD_60 => new() { Width = 1920, Height = 1080, FPS = 60 };
    }
}
