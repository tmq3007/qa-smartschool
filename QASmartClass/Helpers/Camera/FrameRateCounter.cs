namespace QASmartTouch.Helpers.Camera;

/// <summary>
/// Utility class for calculating real-time frame rate
/// </summary>
public class FrameRateCounter
{
    private readonly Queue<DateTime> _frameTimes = new();
    private readonly TimeSpan _windowDuration = TimeSpan.FromSeconds(1);
    private int _frameCount;

    /// <summary>
    /// Current calculated FPS
    /// </summary>
    public double CurrentFPS { get; private set; }

    /// <summary>
    /// Total number of frames processed
    /// </summary>
    public int TotalFrames => _frameCount;

    /// <summary>
    /// Registers that a new frame has arrived
    /// </summary>
    public void FrameArrived()
    {
        _frameCount++;
        var now = DateTime.Now;
        _frameTimes.Enqueue(now);

        // Remove frames older than the window duration
        while (_frameTimes.Count > 0 && (now - _frameTimes.Peek()) > _windowDuration)
        {
            _frameTimes.Dequeue();
        }

        // Calculate FPS based on frames in the current window
        if (_frameTimes.Count > 1)
        {
            var timeSpan = now - _frameTimes.Peek();
            if (timeSpan.TotalSeconds > 0)
            {
                CurrentFPS = _frameTimes.Count / timeSpan.TotalSeconds;
            }
        }
    }

    /// <summary>
    /// Resets the frame counter
    /// </summary>
    public void Reset()
    {
        _frameTimes.Clear();
        _frameCount = 0;
        CurrentFPS = 0;
    }

    /// <summary>
    /// Gets a formatted FPS string
    /// </summary>
    public string GetFormattedFPS()
    {
        return $"{CurrentFPS:F1} FPS";
    }
}
