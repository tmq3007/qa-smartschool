using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartTouch.Services.Canvas
{
    /// <summary>
    /// Implementation of canvas interaction operations
    /// Handles all direct canvas manipulations and element management
    /// </summary>
    public class CanvasService : ICanvasService
    {
        #region Fields
        
        private readonly System.Windows.Controls.Canvas _canvas;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of CanvasService
        /// </summary>
        /// <param name="canvas">The canvas to manage</param>
        public CanvasService(System.Windows.Controls.Canvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }
        
        #endregion
        
        #region ICanvasService Implementation
        
        /// <inheritdoc/>
        public void AddElement(UIElement element)
        {
            if (element == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot add null element to canvas");
                return;
            }
            
            _canvas.Children.Add(element);
            System.Diagnostics.Debug.WriteLine($"✅ Element added to canvas (Total: {_canvas.Children.Count})");
        }
        
        /// <inheritdoc/>
        public bool RemoveElement(UIElement element)
        {
            if (element == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cannot remove null element from canvas");
                return false;
            }
            
            if (!_canvas.Children.Contains(element))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Element not found on canvas");
                return false;
            }
            
            _canvas.Children.Remove(element);
            System.Diagnostics.Debug.WriteLine($"✅ Element removed from canvas (Remaining: {_canvas.Children.Count})");
            return true;
        }
        
        /// <inheritdoc/>
        public void Clear()
        {
            var count = _canvas.Children.Count;
            _canvas.Children.Clear();
            System.Diagnostics.Debug.WriteLine($"🗑️ Canvas cleared ({count} elements removed)");
        }
        
        /// <inheritdoc/>
        public int GetElementCount()
        {
            return _canvas.Children.Count;
        }
        
        /// <inheritdoc/>
        public UIElement? HitTest(Point point)
        {
            // Perform visual hit test
            var hitTestResult = VisualTreeHelper.HitTest(_canvas, point);
            
            if (hitTestResult?.VisualHit == null)
                return null;
            
            // Find the first canvas child that contains the hit point
            var hitElement = hitTestResult.VisualHit;
            
            while (hitElement != null && hitElement != _canvas)
            {
                if (hitElement is UIElement uiElement && _canvas.Children.Contains(uiElement))
                {
                    return uiElement;
                }
                hitElement = VisualTreeHelper.GetParent(hitElement);
            }
            
            return null;
        }
        
        /// <inheritdoc/>
        public List<UIElement> GetElementsInRegion(Rect rect)
        {
            var elementsInRegion = new List<UIElement>();
            
            foreach (UIElement element in _canvas.Children)
            {
                // Get element bounds
                var elementRect = GetElementBounds(element);
                
                // Check if element intersects with selection rect
                if (rect.IntersectsWith(elementRect))
                {
                    elementsInRegion.Add(element);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"🔍 Found {elementsInRegion.Count} elements in region");
            return elementsInRegion;
        }
        
        /// <inheritdoc/>
        public void BringToFront(UIElement element)
        {
            if (element == null || !_canvas.Children.Contains(element))
                return;
            
            _canvas.Children.Remove(element);
            _canvas.Children.Add(element);
            
            System.Diagnostics.Debug.WriteLine("⬆️ Element brought to front");
        }
        
        /// <inheritdoc/>
        public void SendToBack(UIElement element)
        {
            if (element == null || !_canvas.Children.Contains(element))
                return;
            
            _canvas.Children.Remove(element);
            _canvas.Children.Insert(0, element);
            
            System.Diagnostics.Debug.WriteLine("⬇️ Element sent to back");
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Gets the bounding rectangle for a UI element
        /// </summary>
        /// <param name="element">The element to get bounds for</param>
        /// <returns>The bounding rectangle</returns>
        private Rect GetElementBounds(UIElement element)
        {
            var left = System.Windows.Controls.Canvas.GetLeft(element);
            var top = System.Windows.Controls.Canvas.GetTop(element);
            
            // Handle NaN values
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;
            
            var width = element.RenderSize.Width;
            var height = element.RenderSize.Height;
            
            return new Rect(left, top, width, height);
        }
        
        #endregion
    }
}
