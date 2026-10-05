using System;
using System.Collections.Generic;
using QASmartClass.WhiteboardCore.Enums;
using QASmartClass.WhiteboardCore.Tools;

namespace QASmartClass.WhiteboardCore.Managers
{
    /// <summary>
    /// ToolManager — Quản lý vòng đời và chuyển đổi giữa các công cụ bảng vẽ.
    /// 
    /// Vai trò:
    /// - Đảm bảo chỉ có DUY NHẤT một công cụ hoạt động tại một thời điểm.
    /// - Tự động Deactivate công cụ cũ trước khi Activate công cụ mới.
    /// - Phát sự kiện ToolChanged để Form2 đồng bộ UI (cursor, touch handler...).
    /// 
    /// Cách dùng từ Form2:
    ///   _toolManager.SwitchTo(ToolMode.Pen);
    ///   _toolManager.PenTool.Configure("Normal", 3, Colors.Blue);
    /// </summary>
    public class ToolManager
    {
        #region Tools Registry

        /// <summary>Công cụ bút vẽ.</summary>
        public PenTool PenTool { get; }

        /// <summary>Công cụ tẩy.</summary>
        public EraserTool EraserTool { get; }

        /// <summary>Công cụ chọn đối tượng.</summary>
        public SelectionTool SelectionTool { get; }

        /// <summary>Công cụ hiện đang hoạt động (null nếu None).</summary>
        public IWhiteboardTool? ActiveTool { get; private set; }

        /// <summary>ToolMode hiện tại.</summary>
        public ToolMode CurrentMode { get; private set; } = ToolMode.None;

        /// <summary>ToolMode trước đó (hỗ trợ toggle back).</summary>
        public ToolMode PreviousMode { get; private set; } = ToolMode.None;

        // Mapping ToolMode → IWhiteboardTool instance
        private readonly Dictionary<ToolMode, IWhiteboardTool> _toolRegistry;

        #endregion

        #region Events

        /// <summary>
        /// Phát ra khi công cụ được chuyển đổi.
        /// Form2 lắng nghe để đồng bộ UI (cursor, touch handler, selection state...).
        /// </summary>
        public event EventHandler<ToolChangedEventArgs>? ToolChanged;

        #endregion

        #region Constructor

        public ToolManager()
        {
            PenTool = new PenTool();
            EraserTool = new EraserTool();
            SelectionTool = new SelectionTool();

            _toolRegistry = new Dictionary<ToolMode, IWhiteboardTool>
            {
                { ToolMode.Pen, PenTool },
                { ToolMode.Eraser, EraserTool },
                { ToolMode.Select, SelectionTool }
            };

            System.Diagnostics.Debug.WriteLine("✅ ToolManager initialized with Pen + Eraser + Selection tools");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Chuyển sang công cụ mới. Tự động tắt công cụ cũ.
        /// </summary>
        /// <param name="newMode">ToolMode đích.</param>
        public void SwitchTo(ToolMode newMode)
        {
            if (CurrentMode == newMode && newMode != ToolMode.None)
            {
                System.Diagnostics.Debug.WriteLine($"🔄 ToolManager: Already in {newMode} mode — no change.");
                return;
            }

            var previousMode = CurrentMode;
            var previousTool = ActiveTool;

            // 1. Deactivate current tool
            if (ActiveTool != null)
            {
                ActiveTool.Deactivate();
                System.Diagnostics.Debug.WriteLine($"⬛ ToolManager: Deactivated {ActiveTool.Name}");
            }

            // 2. Activate new tool
            PreviousMode = previousMode;
            CurrentMode = newMode;

            if (newMode != ToolMode.None && _toolRegistry.TryGetValue(newMode, out var newTool))
            {
                ActiveTool = newTool;
                newTool.Activate();
                System.Diagnostics.Debug.WriteLine($"🟢 ToolManager: Activated {newTool.Name}");
            }
            else
            {
                ActiveTool = null;
                System.Diagnostics.Debug.WriteLine($"⬜ ToolManager: Set to {newMode} (no registered tool)");
            }

            // 3. Phát sự kiện
            ToolChanged?.Invoke(this, new ToolChangedEventArgs(previousMode, newMode, previousTool, ActiveTool));
        }

        /// <summary>
        /// Tắt công cụ hiện tại, trở về trạng thái None.
        /// </summary>
        public void DeactivateAll()
        {
            SwitchTo(ToolMode.None);
        }

        /// <summary>
        /// Kiểm tra tool nào đang active.
        /// </summary>
        public bool IsToolActive(ToolMode mode) => CurrentMode == mode;

        /// <summary>
        /// Đăng ký thêm tool mới vào registry (cho extension).
        /// </summary>
        public void RegisterTool(ToolMode mode, IWhiteboardTool tool)
        {
            _toolRegistry[mode] = tool;
            System.Diagnostics.Debug.WriteLine($"📌 ToolManager: Registered tool {tool.Name} for mode {mode}");
        }

        #endregion
    }

    /// <summary>
    /// Event args cho sự kiện ToolChanged.
    /// </summary>
    public class ToolChangedEventArgs : EventArgs
    {
        public ToolMode PreviousMode { get; }
        public ToolMode NewMode { get; }
        public IWhiteboardTool? PreviousTool { get; }
        public IWhiteboardTool? NewTool { get; }

        public ToolChangedEventArgs(ToolMode previousMode, ToolMode newMode, 
            IWhiteboardTool? previousTool, IWhiteboardTool? newTool)
        {
            PreviousMode = previousMode;
            NewMode = newMode;
            PreviousTool = previousTool;
            NewTool = newTool;
        }
    }
}
