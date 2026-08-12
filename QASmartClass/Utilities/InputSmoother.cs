using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace QASmartTouch.Utilities
{
    /// <summary>
    /// PHASE 3: Input Smoothing for Touch/Stylus Input
    /// Reduces jitter and improves handwriting quality on large displays
    /// </summary>
    public class InputSmoother
    {
        private Queue<Point> _pointBuffer;
        private int _bufferSize;
        private SmoothingMode _mode;
        
        public enum SmoothingMode
        {
            None,           // No smoothing (pass-through)
            Average,        // Simple moving average
            Weighted,       // Weighted average (recent points have more weight)
            Exponential     // Exponential moving average (best for real-time)
        }
        
        /// <summary>
        /// Initialize input smoother with buffer size and mode
        /// </summary>
        /// <param name="bufferSize">Number of points to average (3-5 recommended)</param>
        /// <param name="mode">Smoothing algorithm to use</param>
        public InputSmoother(int bufferSize = 3, SmoothingMode mode = SmoothingMode.Weighted)
        {
            _bufferSize = Math.Max(1, Math.Min(bufferSize, 10)); // Clamp between 1-10
            _mode = mode;
            _pointBuffer = new Queue<Point>(_bufferSize);
            
            System.Diagnostics.Debug.WriteLine($"✅ InputSmoother created: BufferSize={_bufferSize}, Mode={_mode}");
        }
        
        /// <summary>
        /// Smooth a new input point
        /// </summary>
        /// <param name="newPoint">Raw input point from touch/stylus</param>
        /// <returns>Smoothed point</returns>
        public Point SmoothPoint(Point newPoint)
        {
            // Add new point to buffer
            _pointBuffer.Enqueue(newPoint);
            
            // Remove oldest point if buffer is full
            if (_pointBuffer.Count > _bufferSize)
            {
                _pointBuffer.Dequeue();
            }
            
            // Apply smoothing based on mode
            return _mode switch
            {
                SmoothingMode.None => newPoint,
                SmoothingMode.Average => ApplyAverageSmoothing(),
                SmoothingMode.Weighted => ApplyWeightedSmoothing(),
                SmoothingMode.Exponential => ApplyExponentialSmoothing(newPoint),
                _ => newPoint
            };
        }
        
        /// <summary>
        /// Simple moving average - all points have equal weight
        /// </summary>
        private Point ApplyAverageSmoothing()
        {
            if (_pointBuffer.Count == 0)
                return new Point(0, 0);
            
            double avgX = _pointBuffer.Average(p => p.X);
            double avgY = _pointBuffer.Average(p => p.Y);
            
            return new Point(avgX, avgY);
        }
        
        /// <summary>
        /// Weighted average - recent points have more weight
        /// Weights: [1, 2, 3] for 3 points (most recent = 3)
        /// </summary>
        private Point ApplyWeightedSmoothing()
        {
            if (_pointBuffer.Count == 0)
                return new Point(0, 0);
            
            var points = _pointBuffer.ToArray();
            double totalWeight = 0;
            double weightedX = 0;
            double weightedY = 0;
            
            for (int i = 0; i < points.Length; i++)
            {
                // Weight increases linearly (older points = less weight)
                double weight = i + 1;
                totalWeight += weight;
                weightedX += points[i].X * weight;
                weightedY += points[i].Y * weight;
            }
            
            return new Point(weightedX / totalWeight, weightedY / totalWeight);
        }
        
        /// <summary>
        /// Exponential moving average - best for real-time smoothing
        /// Formula: smoothed = alpha * new + (1 - alpha) * previous
        /// </summary>
        private Point ApplyExponentialSmoothing(Point newPoint)
        {
            if (_pointBuffer.Count <= 1)
                return newPoint;
            
            // Alpha = smoothing factor (0.3 = 30% new, 70% previous)
            // Lower alpha = more smoothing but more lag
            // Higher alpha = less smoothing but more responsive
            double alpha = 0.4;
            
            var previousPoint = _pointBuffer.ElementAt(_pointBuffer.Count - 2);
            
            double smoothedX = alpha * newPoint.X + (1 - alpha) * previousPoint.X;
            double smoothedY = alpha * newPoint.Y + (1 - alpha) * previousPoint.Y;
            
            return new Point(smoothedX, smoothedY);
        }
        
        /// <summary>
        /// Clear the buffer (call when starting new stroke)
        /// </summary>
        public void Clear()
        {
            _pointBuffer.Clear();
        }
        
        /// <summary>
        /// Change smoothing mode on the fly
        /// </summary>
        public void SetMode(SmoothingMode mode)
        {
            _mode = mode;
            System.Diagnostics.Debug.WriteLine($"🔄 InputSmoother mode changed to: {_mode}");
        }
        
        /// <summary>
        /// Change buffer size on the fly
        /// </summary>
        public void SetBufferSize(int size)
        {
            _bufferSize = Math.Max(1, Math.Min(size, 10));
            
            // Trim buffer if new size is smaller
            while (_pointBuffer.Count > _bufferSize)
            {
                _pointBuffer.Dequeue();
            }
            
            System.Diagnostics.Debug.WriteLine($"🔄 InputSmoother buffer size changed to: {_bufferSize}");
        }
        
        /// <summary>
        /// Get current buffer size
        /// </summary>
        public int BufferSize => _bufferSize;
        
        /// <summary>
        /// Get current smoothing mode
        /// </summary>
        public SmoothingMode Mode => _mode;
        
        /// <summary>
        /// Get number of points currently in buffer
        /// </summary>
        public int CurrentBufferCount => _pointBuffer.Count;
    }
}
