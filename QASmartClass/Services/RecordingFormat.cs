namespace QASmartTouch.Services
{
    /// <summary>
    /// Recording format options
    /// </summary>
    public enum RecordingFormat
    {
        /// <summary>
        /// Animated GIF (256 colors, optimized, good compatibility)
        /// </summary>
        GIF,
        
        /// <summary>
        /// Animated WebP (millions of colors, zero dithering, smaller file size)
        /// </summary>
        WebP
    }

    /// <summary>
    /// Cấu hình chất lượng và tối ưu dung lượng khi ghi hình bài giảng MP4
    /// </summary>
    public enum RecordingQualityPreset
    {
        /// <summary>
        /// Chuẩn sắc nét Full HD 1080p (1920x1080, 20fps, Quality 50, ~2.5 - 3.5 MB/phút)
        /// </summary>
        Standard1080p = 0,

        /// <summary>
        /// Siêu nhẹ HD 720p tối ưu gửi Zalo / LMS nhanh (1280x720, 15fps, Quality 45, ~1.5 - 2.2 MB/phút)
        /// </summary>
        Compact720p = 1
    }
}
