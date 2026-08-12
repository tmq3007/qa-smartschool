using System.Windows.Media;
using QASmartTouch.Managers;

namespace QASmartTouch.Services.Tools
{
    /// <summary>
    /// Interface for pen tool operations
    /// Handles pen-specific settings, brush types, and drawing configurations
    /// </summary>
    public interface IPenToolService
    {
        /// <summary>
        /// Gets the current pen size
        /// </summary>
        int PenSize { get; }
        
        /// <summary>
        /// Gets the current pen color
        /// </summary>
        Color PenColor { get; }
        
        /// <summary>
        /// Gets the current brush type
        /// </summary>
        BrushType BrushType { get; }
        
        /// <summary>
        /// Sets the pen size (1-16)
        /// </summary>
        /// <param name="size">The pen size</param>
        void SetPenSize(int size);
        
        /// <summary>
        /// Sets the pen color
        /// </summary>
        /// <param name="color">The pen color</param>
        void SetPenColor(Color color);
        
        /// <summary>
        /// Sets the brush type
        /// </summary>
        /// <param name="brushType">The brush type</param>
        void SetBrushType(BrushType brushType);
        
        /// <summary>
        /// Activates the pen tool
        /// </summary>
        void Activate();
        
        /// <summary>
        /// Deactivates the pen tool
        /// </summary>
        void Deactivate();
        
        /// <summary>
        /// Resets pen settings to defaults
        /// </summary>
        void ResetToDefaults();
    }
}
