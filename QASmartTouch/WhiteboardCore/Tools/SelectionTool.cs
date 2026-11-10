using System;
using QASmartClass.WhiteboardCore.Enums;

namespace QASmartClass.WhiteboardCore.Tools
{
    /// <summary>
    /// SelectionTool — Quản lý trạng thái và cấu hình công cụ chọn đối tượng.
    /// 
    /// Thiết kế: Class thuần logic, KHÔNG tham chiếu UI controls.
    /// - Lưu trữ chế độ chọn (Rectangle / Lasso / MagicWand).
    /// - Phát sự kiện khi chế độ thay đổi → Form2 lắng nghe để đồng bộ UI.
    /// 
    /// Logic chọn thực tế (HitTest, SelectionBox, ContextToolbar) vẫn ở Form2 partial class
    /// vì phụ thuộc chặt vào SelectionManager + Canvas — sẽ dần migrate ở giai đoạn sau.
    /// </summary>
    public class SelectionTool : IWhiteboardTool
    {
        #region IWhiteboardTool Implementation

        public string Name => "Selection";
        public ToolMode Mode => ToolMode.Select;
        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive = true;
            Activated?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"🔍 SelectionTool Activated — Mode={CurrentSelectionMode}");
        }

        public void Deactivate()
        {
            IsActive = false;
            Deactivated?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine("🔍 SelectionTool Deactivated");
        }

        #endregion

        #region Selection Configuration

        /// <summary>Chế độ chọn hiện tại.</summary>
        public SelectionMode CurrentSelectionMode { get; private set; } = SelectionMode.Rectangle;

        #endregion

        #region Events

        /// <summary>Phát ra khi tool được kích hoạt.</summary>
        public event EventHandler? Activated;

        /// <summary>Phát ra khi tool bị tắt.</summary>
        public event EventHandler? Deactivated;

        /// <summary>Phát ra khi chế độ chọn thay đổi.</summary>
        public event EventHandler? ModeChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Chuyển chế độ chọn.
        /// </summary>
        public void SetMode(SelectionMode mode)
        {
            if (CurrentSelectionMode == mode) return;
            CurrentSelectionMode = mode;
            ModeChanged?.Invoke(this, EventArgs.Empty);
            System.Diagnostics.Debug.WriteLine($"🔍 SelectionTool Mode → {mode}");
        }

        #endregion
    }
}
