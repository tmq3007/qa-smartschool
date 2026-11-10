using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace QASmartTouch.Services.Canvas
{
    /// <summary>
    /// Interface for canvas interaction operations
    /// Handles adding/removing elements, hit testing, and canvas state management
    /// </summary>
    public interface ICanvasService
    {
        /// <summary>
        /// Adds a UI element to the canvas
        /// </summary>
        /// <param name="element">The element to add</param>
        void AddElement(UIElement element);
        
        /// <summary>
        /// Removes a UI element from the canvas
        /// </summary>
        /// <param name="element">The element to remove</param>
        /// <returns>True if removed successfully, false otherwise</returns>
        bool RemoveElement(UIElement element);
        
        /// <summary>
        /// Clears all elements from the canvas
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Gets the number of elements on the canvas
        /// </summary>
        int GetElementCount();
        
        /// <summary>
        /// Performs hit testing to find element at a point
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>The element at the point, or null if none found</returns>
        UIElement? HitTest(Point point);
        
        /// <summary>
        /// Gets all elements within a rectangular region
        /// </summary>
        /// <param name="rect">The rectangular region</param>
        /// <returns>List of elements within the region</returns>
        System.Collections.Generic.List<UIElement> GetElementsInRegion(Rect rect);
        
        /// <summary>
        /// Brings an element to front (top of z-order)
        /// </summary>
        /// <param name="element">The element to bring to front</param>
        void BringToFront(UIElement element);
        
        /// <summary>
        /// Sends an element to back (bottom of z-order)
        /// </summary>
        /// <param name="element">The element to send to back</param>
        void SendToBack(UIElement element);
    }
}
