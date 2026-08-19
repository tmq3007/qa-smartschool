using System;
using QASmartClass.WhiteboardCore.Enums;

namespace QASmartClass.WhiteboardCore.Tools
{
    /// <summary>
    /// EraserTool — Quản lý cấu hình và trạng thái công cụ tẩy.
    /// 
    /// Thiết kế: Class thuần logic, KHÔNG tham chiếu UI controls.
    /// - Lưu trữ cấu hình tẩy (Size, Mode).
    /// - Phát sự kiện khi cấu hình thay đổi → Form2 lắng nghe để đồng bộ UI.
    /// - Hỗ trợ lưu/khôi phục cấu hình qua SaveSettings() / RestoreSettings().
    /// 
    /// Logic tẩy thực tế (EraseStrokeAt, EraseElementsInRect) vẫn ở Form2 partial class
    /// vì phụ thuộc trực tiếp vào Canvas.Children — sẽ chuyển sang Core ở giai đoạn sau.
    /// </summary>
    public class EraserTool : IWhiteboardTool
    {
        #region IWhiteboardTool Implementation

        public string Name => "Eraser";
        public ToolMode Mode => ToolMode.Eraser;
        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            // Khôi phục cấu hình đã lưu khi kích hoạt lại
            CurrentEraserSize = _savedEraserSize;
            CurrentEraserMode = _savedEraserMode;

            SettingsChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"🧹 EraserTool Activated — Size={CurrentEraserSize}, Mode={CurrentEraserMode}");
        }

        public void Deactivate()
        {
            // Lưu cấu hình trước khi tắt
            SaveSettings();
            IsActive = false;
            Deactivated?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("🧹 EraserTool Deactivated");
        }

        #endregion

        #region Eraser Configuration

        /// <summary>Kích thước vùng tẩy (bán kính, px).</summary>
        public int CurrentEraserSize { get; private set; } = 20;

        /// <summary>Chế độ tẩy hiện tại.</summary>
        public EraserMode CurrentEraserMode { get; private set; } = Enums.EraserMode.Stroke;

        // Cấu hình đã lưu (khôi phục khi re-activate)
        private int _savedEraserSize = 20;
        private EraserMode _savedEraserMode = Enums.EraserMode.Stroke;

        #endregion

        #region Events

        /// <summary>
        /// Phát ra khi cấu hình tẩy thay đổi.
        /// Form2 lắng nghe để đồng bộ _touchHandler.SetEraserProperties(...).
        /// </summary>
        public event EventHandler? SettingsChanged;

        /// <summary>
        /// Phát ra khi tool bị tắt.
        /// Form2 lắng nghe để dọn dẹp UI (eraser preview, cursor...).
        /// </summary>
        public event EventHandler? Deactivated;

        /// <summary>
        /// Phát ra khi chế độ ClearAll được chọn.
        /// Form2 xử lý logic xóa tất cả (cần truy cập Canvas.Children).
        /// </summary>
        public event EventHandler? ClearAllRequested;

        #endregion

        #region Public Methods

        /// <summary>
        /// Cập nhật cấu hình tẩy (từ string mode, tương thích legacy code).
        /// </summary>
        public void Configure(int eraserSize, string eraserMode)
        {
            CurrentEraserSize = eraserSize;
            CurrentEraserMode = ParseEraserMode(eraserMode);

            // Auto-save
            _savedEraserSize = eraserSize;
            _savedEraserMode = CurrentEraserMode;

            if (CurrentEraserMode == Enums.EraserMode.ClearAll)
            {
                ClearAllRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }

            System.Diagnostics.Debug.WriteLine($"🧹 EraserTool Configured — Size={eraserSize}, Mode={eraserMode}");
        }

        /// <summary>
        /// Cập nhật cấu hình tẩy (từ enum mode).
        /// </summary>
        public void Configure(int eraserSize, EraserMode eraserMode)
        {
            CurrentEraserSize = eraserSize;
            CurrentEraserMode = eraserMode;

            _savedEraserSize = eraserSize;
            _savedEraserMode = eraserMode;

            if (eraserMode == Enums.EraserMode.ClearAll)
            {
                ClearAllRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Lưu cấu hình hiện tại.</summary>
        public void SaveSettings()
        {
            _savedEraserSize = CurrentEraserSize;
            _savedEraserMode = CurrentEraserMode;
        }

        /// <summary>Khôi phục cấu hình đã lưu.</summary>
        public void RestoreSettings()
        {
            CurrentEraserSize = _savedEraserSize;
            CurrentEraserMode = _savedEraserMode;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Trả về cấu hình hiện tại dưới dạng tuple.
        /// </summary>
        public (int Size, EraserMode Mode) GetCurrentSettings()
        {
            return (CurrentEraserSize, CurrentEraserMode);
        }

        /// <summary>
        /// Trả về chế độ tẩy dưới dạng string (tương thích legacy code).
        /// </summary>
        public string GetCurrentModeString()
        {
            return CurrentEraserMode switch
            {
                Enums.EraserMode.Stroke => "Stroke",
                Enums.EraserMode.Drag => "Drag",
                Enums.EraserMode.ClearAll => "ClearAll",
                _ => "Stroke"
            };
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Chuyển đổi string → EraserMode enum (tương thích legacy code).
        /// </summary>
        private static EraserMode ParseEraserMode(string mode)
        {
            return mode?.ToLowerInvariant() switch
            {
                "stroke" => Enums.EraserMode.Stroke,
                "drag" => Enums.EraserMode.Drag,
                "clearall" => Enums.EraserMode.ClearAll,
                _ => Enums.EraserMode.Stroke
            };
        }

        #endregion
    }
}
