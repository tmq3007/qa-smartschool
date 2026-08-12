using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using QASmartTouch.Models;

namespace QASmartTouch.Services
{
    /// <summary>
    /// AutoGroupingService - Tự động gom nhóm các nét viết tay (Polyline/Stroke)
    /// dựa trên khoảng cách không gian (<= 45px) và khoảng thời gian viết liên tục (<= 2.2s)
    /// </summary>
    public class AutoGroupingService
    {
        #region Fields

        private readonly List<StrokeMetadata> _strokeHistory = new List<StrokeMetadata>();
        private const double SpatialThresholdPx = 45.0;
        private const double TemporalThresholdSeconds = 2.2;
        private const int MaxStrokeHistorySize = 500;
        private const double EvictionTimeSeconds = 60.0;

        #endregion

        #region Nested Types

        public class StrokeMetadata
        {
            public UIElement Element { get; set; } = null!;
            public Rect Bounds { get; set; }
            public DateTime Timestamp { get; set; }
            public Guid GroupId { get; set; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ghi nhận 1 nét vẽ mới và tự động gắn GroupId nếu nằm trong phạm vi không gian/thời gian
        /// </summary>
        public Guid RegisterStroke(UIElement strokeElement, Rect bounds)
        {
            DateTime now = DateTime.Now;
            Guid assignedGroupId = Guid.NewGuid();

            // ✅ REVIEW-FIX #3: Tự động evict các nét quá cũ (> 60s) khi danh sách > 500
            // Tránh tiêu thụ bộ nhớ vô hạn khi giáo viên dạy buổi dài liên tục
            if (_strokeHistory.Count > MaxStrokeHistorySize)
            {
                _strokeHistory.RemoveAll(s => (now - s.Timestamp).TotalSeconds > EvictionTimeSeconds);
            }

            // Tìm nét vẽ gần nhất thỏa mãn cả khoảng cách không gian và khoảng thời gian
            var candidate = _strokeHistory
                .Where(s => (now - s.Timestamp).TotalSeconds <= TemporalThresholdSeconds)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefault(s => CalculateMinDistance(s.Bounds, bounds) <= SpatialThresholdPx);

            if (candidate != null)
            {
                assignedGroupId = candidate.GroupId;
            }

            _strokeHistory.Add(new StrokeMetadata
            {
                Element = strokeElement,
                Bounds = bounds,
                Timestamp = now,
                GroupId = assignedGroupId
            });

            return assignedGroupId;
        }

        /// <summary>
        /// Lấy tất cả các nét vẽ thuộc cùng nhóm với 1 phần tử được chọn
        /// </summary>
        public List<UIElement> GetGroupElements(UIElement targetElement)
        {
            var meta = _strokeHistory.FirstOrDefault(s => s.Element == targetElement);
            if (meta == null) return new List<UIElement> { targetElement };

            return _strokeHistory
                .Where(s => s.GroupId == meta.GroupId)
                .Select(s => s.Element)
                .ToList();
        }

        /// <summary>
        /// Xóa lịch sử khi chuyển trang hoặc làm sạch bảng
        /// </summary>
        public void Clear()
        {
            _strokeHistory.Clear();
        }

        #endregion

        #region Helpers

        private double CalculateMinDistance(Rect r1, Rect r2)
        {
            double dx = Math.Max(0, Math.Max(r1.Left - r2.Right, r2.Left - r1.Right));
            double dy = Math.Max(0, Math.Max(r1.Top - r2.Bottom, r2.Top - r1.Bottom));
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion
    }
}
