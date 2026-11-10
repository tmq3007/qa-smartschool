using System;

namespace QASmartClass.WhiteboardCore.STEM
{
    /// <summary>
    /// IStemTool — Interface cho các công cụ STEM (Ruler, Compass, Protractor, SetSquare).
    /// 
    /// Giai đoạn 3.4: Scaffold rỗng.
    /// Khi triển khai, mỗi công cụ STEM sẽ implement interface này:
    /// - RulerService: Quản lý logic thước kẻ (vị trí, góc xoay, vẽ đường)
    /// - CompassService: Quản lý compa (bán kính, vẽ cung/đường tròn)
    /// - ProtractorService: Quản lý thước đo góc
    /// - ChartService: Quản lý biểu đồ (Line, Pie, Area, Scatter, Radar)
    /// </summary>
    public interface IStemTool
    {
        /// <summary>Tên công cụ STEM.</summary>
        string Name { get; }

        /// <summary>Đang hoạt động hay không.</summary>
        bool IsActive { get; }

        /// <summary>Kích hoạt công cụ.</summary>
        void Activate();

        /// <summary>Đóng/hủy kích hoạt.</summary>
        void Deactivate();
    }

    /// <summary>
    /// StemManager — Facade cho tất cả công cụ STEM.
    /// Giai đoạn 3.4: Scaffold rỗng — sẵn sàng cho implementation.
    /// </summary>
    public class StemManager
    {
        public StemManager()
        {
            System.Diagnostics.Debug.WriteLine("✅ StemManager initialized (scaffold)");
        }
    }
}
