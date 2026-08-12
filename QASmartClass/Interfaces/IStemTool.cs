using System;

namespace QASmartTouch.Interfaces
{
    /// <summary>
    /// [CAI_TIEN_VID_10] Interface chuẩn hóa cho tất cả STEM tools.
    /// Mỗi STEM tool (Ruler, Protractor, SetSquare, Compass) phải implement interface này
    /// để hỗ trợ Object Pool Pattern, lifecycle management, và memory benchmarking.
    /// </summary>
    public interface IStemTool
    {
        /// <summary>
        /// Được gọi khi tool được kích hoạt (lấy ra từ pool hoặc tạo mới).
        /// Khởi tạo event listeners, load resources, enable touch input.
        /// </summary>
        void OnActivated();

        /// <summary>
        /// Được gọi khi tool bị ẩn (trả về pool).
        /// Unbind events, clear temp data, disable touch input, giải phóng BitmapCache.
        /// </summary>
        void OnDeactivated();

        /// <summary>
        /// Ước tính bộ nhớ đang sử dụng (MB).
        /// Dùng để benchmark và monitoring.
        /// Target: Tổng 4 tools ≤ 150MB.
        /// </summary>
        double GetMemoryUsageMB();

        /// <summary>
        /// Tên hiển thị của tool (để logging).
        /// </summary>
        string ToolDisplayName { get; }

        /// <summary>
        /// Thời điểm kích hoạt gần nhất.
        /// Dùng để StemToolPool tính idle time.
        /// </summary>
        DateTime LastActivatedTime { get; }
    }
}
