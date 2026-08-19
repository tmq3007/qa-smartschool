using System;
using QASmartClass.WhiteboardCore.Enums;
using QASmartClass.WhiteboardCore.History;
using QASmartClass.WhiteboardCore.Input;
using QASmartClass.WhiteboardCore.Tools;

namespace QASmartClass.WhiteboardCore.Managers
{
    /// <summary>
    /// WhiteboardManager — Façade class: Cầu nối DUY NHẤT giữa Form2 và WhiteboardCore.
    /// 
    /// Form2 chỉ cần giữ 1 tham chiếu _whiteboardManager và gọi qua nó:
    ///   _whiteboardManager.ToolManager.SwitchTo(ToolMode.Pen);
    ///   _whiteboardManager.ToolManager.PenTool.Configure("Normal", 3, Colors.Blue);
    ///   _whiteboardManager.History.RecordAdd(element);
    /// 
    /// Thiết kế:
    /// - Khởi tạo tất cả sub-managers (ToolManager, History, Input, Canvas...).
    /// - KHÔNG chứa logic nghiệp vụ — chỉ delegate.
    /// - Có thể inject IWhiteboardContext (Form2) để sub-managers truy cập Canvas nếu cần.
    /// </summary>
    public class WhiteboardManager
    {
        #region Sub-Managers

        /// <summary>Quản lý công cụ (Pen, Eraser, Select...).</summary>
        public ToolManager ToolManager { get; }

        /// <summary>Quản lý tương tác cảm ứng (Touch/Stylus/Mouse).</summary>
        public MultiTouchManager InputManager { get; }

        /// <summary>Quản lý Undo/Redo.</summary>
        public IUndoRedoManager? History { get; private set; }

        /// <summary>Context interface — cho phép truy cập Canvas từ Form2.</summary>
        public IWhiteboardContext? Context { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Khởi tạo WhiteboardManager.
        /// Giai đoạn 3: ToolManager + InputManager.
        /// </summary>
        public WhiteboardManager()
        {
            ToolManager = new ToolManager();
            InputManager = new MultiTouchManager();
            System.Diagnostics.Debug.WriteLine("✅ WhiteboardManager initialized (Facade) — ToolManager + InputManager");
        }

        /// <summary>
        /// Khởi tạo WhiteboardManager với Context (Form2 implements IWhiteboardContext).
        /// </summary>
        public WhiteboardManager(IWhiteboardContext context) : this()
        {
            Context = context;
            System.Diagnostics.Debug.WriteLine("✅ WhiteboardManager: Context bound to Form2");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gắn UndoRedoManager (đã được khởi tạo bên ngoài).
        /// Cho phép Form2 truyền instance UndoRedoManager đang dùng vào đây.
        /// </summary>
        public void SetHistoryManager(IUndoRedoManager historyManager)
        {
            History = historyManager;
            System.Diagnostics.Debug.WriteLine("✅ WhiteboardManager: History manager attached");
        }

        /// <summary>
        /// Tắt tất cả công cụ và trở về trạng thái mặc định.
        /// </summary>
        public void Reset()
        {
            ToolManager.DeactivateAll();
            System.Diagnostics.Debug.WriteLine("🔄 WhiteboardManager: Reset to default state");
        }

        /// <summary>
        /// Trả về ToolMode hiện tại (shortcut).
        /// </summary>
        public ToolMode CurrentToolMode => ToolManager.CurrentMode;

        #endregion
    }
}
