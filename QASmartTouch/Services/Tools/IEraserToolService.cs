using QASmartTouch.Managers;

namespace QASmartTouch.Services.Tools
{
    /// <summary>
    /// Interface for eraser tool operations
    /// Handles eraser-specific settings and modes
    /// Note: Uses EraserMode enum from QASmartTouch.Managers namespace
    /// </summary>
    public interface IEraserToolService
    {
        /// <summary>
        /// Gets the current eraser size
        /// </summary>
        int EraserSize { get; }
        
        /// <summary>
        /// Gets the current eraser mode
        /// </summary>
        EraserMode Mode { get; }
        
        /// <summary>
        /// Sets the eraser size (5-50)
        /// </summary>
        /// <param name="size">The eraser size</param>
        void SetEraserSize(int size);
        
        /// <summary>
        /// Sets the eraser mode
        /// </summary>
        /// <param name="mode">The eraser mode</param>
        void SetMode(EraserMode mode);
        
        /// <summary>
        /// Activates the eraser tool
        /// </summary>
        void Activate();
        
        /// <summary>
        /// Deactivates the eraser tool
        /// </summary>
        void Deactivate();
        
        /// <summary>
        /// Resets eraser settings to defaults
        /// </summary>
        void ResetToDefaults();
    }
}
