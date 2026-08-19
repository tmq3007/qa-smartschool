using System;
using System.Windows.Media;
using QASmartClass.WhiteboardCore.Enums;

namespace QASmartClass.WhiteboardCore.Tools
{
    /// <summary>
    /// PenTool — Quản lý cấu hình và trạng thái bút vẽ tự do.
    /// 
    /// Thiết kế: Class thuần logic, KHÔNG tham chiếu UI controls.
    /// - Lưu trữ cấu hình bút (BrushType, Size, Color).
    /// - Phát sự kiện khi cấu hình thay đổi → Form2 lắng nghe để đồng bộ UI.
    /// - Hỗ trợ lưu/khôi phục cấu hình qua SaveSettings() / RestoreSettings().
    /// </summary>
    public class PenTool : IWhiteboardTool
    {
        #region IWhiteboardTool Implementation

        public string Name => "Pen";
        public ToolMode Mode => ToolMode.Pen;
        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            // Khôi phục cấu hình đã lưu khi kích hoạt lại
            CurrentBrushType = _savedBrushType;
            CurrentPenSize = _savedPenSize;
            CurrentPenColor = _savedPenColor;
            
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"✏️ PenTool Activated — Brush={CurrentBrushType}, Size={CurrentPenSize}, Color={CurrentPenColor}");
        }

        public void Deactivate()
        {
            // Lưu cấu hình trước khi tắt
            SaveSettings();
            IsActive = false;
            System.Diagnostics.Debug.WriteLine("✏️ PenTool Deactivated");
        }

        #endregion

        #region Pen Configuration

        /// <summary>Loại bút hiện tại (Normal, Highlighter, Calligraphy...).</summary>
        public string CurrentBrushType { get; private set; } = "Normal";

        /// <summary>Kích thước nét bút (px).</summary>
        public int CurrentPenSize { get; private set; } = 2;

        /// <summary>Màu bút hiện tại.</summary>
        public Color CurrentPenColor { get; private set; } = Colors.White;

        // Cấu hình đã lưu (khôi phục khi re-activate)
        private string _savedBrushType = "Normal";
        private int _savedPenSize = 2;
        private Color _savedPenColor = Colors.White;

        #endregion

        #region Events

        /// <summary>
        /// Phát ra khi cấu hình bút thay đổi.
        /// Form2 lắng nghe để đồng bộ _touchHandler.SetDrawingProperties(...).
        /// </summary>
        public event EventHandler? SettingsChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Cập nhật cấu hình bút và tự động lưu.
        /// </summary>
        public void Configure(string brushType, int penSize, Color penColor)
        {
            CurrentBrushType = brushType;
            CurrentPenSize = penSize;
            CurrentPenColor = penColor;

            // Auto-save
            _savedBrushType = brushType;
            _savedPenSize = penSize;
            _savedPenColor = penColor;

            SettingsChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"✏️ PenTool Configured — Brush={brushType}, Size={penSize}, Color={penColor}");
        }

        /// <summary>Lưu cấu hình hiện tại (gọi trước khi Deactivate).</summary>
        public void SaveSettings()
        {
            _savedBrushType = CurrentBrushType;
            _savedPenSize = CurrentPenSize;
            _savedPenColor = CurrentPenColor;
        }

        /// <summary>Khôi phục cấu hình đã lưu.</summary>
        public void RestoreSettings()
        {
            CurrentBrushType = _savedBrushType;
            CurrentPenSize = _savedPenSize;
            CurrentPenColor = _savedPenColor;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Trả về cấu hình hiện tại dưới dạng tuple.
        /// Tiện cho Form2 lấy nhanh để truyền vào TouchHandler.
        /// </summary>
        public (string BrushType, int Size, Color Color) GetCurrentSettings()
        {
            return (CurrentBrushType, CurrentPenSize, CurrentPenColor);
        }

        #endregion
    }
}
