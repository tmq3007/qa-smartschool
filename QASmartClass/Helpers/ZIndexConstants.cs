namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_ZINDEX: Bảng phân tầng Z-Index tập trung cho toàn bộ Canvas.
    /// KHÔNG ĐƯỢC dùng magic number trực tiếp. Luôn tham chiếu class này.
    /// </summary>
    public static class ZIndexConstants
    {
        // Layer 0: Background & Grid
        public const int Background = 0;
        public const int GridOverlay = 1;
        
        // Layer 100-999: User Content (Strokes, Shapes, Images, Text)
        public const int UserContentBase = 100;
        // ✅ QC_4.2_TABLE_ZINDEX (T9): Bảng nằm TRÊN nét vẽ thường nhưng DƯỚI Widget
        public const int TableContainer = 500;
        public const int UserContentMax = 999;
        
        // Layer 1000-1999: Embedded Widgets (Browser, Spreadsheet, 3D Model)
        public const int EmbeddedWidgetBase = 1000;
        public const int EmbeddedBrowser = 1100;
        public const int EmbeddedSpreadsheet = 1200;
        public const int Embedded3DModel = 1300;
        
        // Layer 2000-2999: STEM Tools (Ruler, Protractor, Compass)
        public const int StemToolBase = 2000;
        public const int StemRuler = 2100;
        public const int StemProtractor = 2200;
        public const int StemSetSquare = 2300;
        public const int StemCompass = 2400;
        
        // Layer 3000-3999: Active Drawing Layer (current pen stroke)
        public const int ActiveStrokeLayer = 3000;
        
        // Layer 5000-5999: Eraser Preview, Selection Preview
        public const int EraserPreview = 5000;
        public const int SelectionPreview = 5100;
        public const int LassoPreview = 5200;
        
        // Layer 8000-8999: Floating Notes, Thumbnails
        public const int FloatingNote = 8000;
        public const int ThumbnailPreview = 8100;
        
        // Layer 10000+: System UI (KHÔNG BAO GIỜ bị ClearAll xóa)
        public const int SystemUIBase = 10000;
        public const int SelectionBox = 10000;
        public const int ContextToolbar = 10001;
        public const int ThicknessPicker = 10002;
        public const int ColorPicker = 10003;
        public const int MoreMenu = 10004;
        public const int FloatingKeyboard = 10005;
        public const int StatusBadge = 10050;
        public const int WelcomePanel = 10060;
        
        // Layer 20000+: Notification/Toast (trên cùng)
        public const int NotificationToast = 20000;
    }
}
