using System;

namespace QASmartClass.WhiteboardCore.IO
{
    /// <summary>
    /// IOManager — Quản lý Save/Load bài giảng và Export.
    /// 
    /// Giai đoạn 3.7: Scaffold rỗng.
    /// Khi triển khai sẽ chứa:
    /// - SaveLoadService: Lưu/mở file bài giảng (.qasc)
    /// - ExportService: Xuất PDF/Image/Video
    /// - ImportService: Nhập Image, Video, Camera
    /// - MediaManager: Quản lý PhET, YouTube, Google Maps integration
    /// </summary>
    public class IOManager
    {
        public IOManager()
        {
            System.Diagnostics.Debug.WriteLine("✅ IOManager initialized (scaffold)");
        }
    }
}
