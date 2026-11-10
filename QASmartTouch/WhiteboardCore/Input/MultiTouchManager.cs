using System;
using System.Windows;
using QASmartClass.WhiteboardCore.Enums;

namespace QASmartClass.WhiteboardCore.Input
{
    /// <summary>
    /// IInputBridge — Interface cầu nối giữa WhiteboardCore và lớp Input Handler (TouchHandler, StylusHandler...).
    /// 
    /// Thiết kế:
    /// - WhiteboardCore chỉ biết đến IInputBridge, KHÔNG tham chiếu TouchHandler trực tiếp.
    /// - Form2 sẽ tạo adapter class implement IInputBridge, delegate sang TouchHandler thật.
    /// - Khi cần thay thế TouchHandler (VD: mock cho unit test), chỉ cần thay adapter.
    /// 
    /// Giai đoạn 3.2: Tạo interface + adapter cơ bản.
    /// Các phương thức sẽ được mở rộng khi cần.
    /// </summary>
    public interface IInputBridge
    {
        /// <summary>Đồng bộ ToolMode từ WhiteboardCore sang Touch subsystem.</summary>
        void SetToolMode(ToolMode mode);

        /// <summary>Đặt thuộc tính bút vẽ cho touch drawing.</summary>
        void SetDrawingProperties(System.Windows.Media.Color color, int size, string brushType);

        /// <summary>Đặt thuộc tính tẩy cho touch erasing.</summary>
        void SetEraserProperties(int size, string mode);

        /// <summary>Bật/tắt xử lý touch.</summary>
        void SetEnabled(bool enabled);

        /// <summary>Có touch nào đang active hay không (dùng để chặn mouse promoted events).</summary>
        bool HasActiveTouches { get; }
    }

    /// <summary>
    /// MultiTouchManager — Quản lý tương tác cảm ứng thông qua IInputBridge.
    /// 
    /// Vai trò trong WhiteboardCore:
    /// - Lưu trữ trạng thái input hiện tại (enabled, mode).
    /// - Phát event khi trạng thái thay đổi.
    /// - Delegate thực thi sang IInputBridge (TouchHandler thật).
    /// 
    /// Class này KHÔNG xử lý touch events trực tiếp.
    /// </summary>
    public class MultiTouchManager
    {
        private IInputBridge? _bridge;
        private bool _isEnabled = true;

        /// <summary>
        /// Phát ra khi trạng thái input thay đổi.
        /// </summary>
        public event EventHandler? StateChanged;

        public MultiTouchManager() 
        {
            System.Diagnostics.Debug.WriteLine("✅ MultiTouchManager initialized");
        }

        /// <summary>
        /// Gắn bridge adapter (gọi từ Form2 sau khi tạo TouchHandler).
        /// </summary>
        public void SetBridge(IInputBridge bridge)
        {
            _bridge = bridge;
            System.Diagnostics.Debug.WriteLine("✅ MultiTouchManager: Bridge attached");
        }

        /// <summary>
        /// Đồng bộ ToolMode hiện tại xuống lớp touch.
        /// </summary>
        public void SyncToolMode(ToolMode mode)
        {
            _bridge?.SetToolMode(mode);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Đặt thuộc tính bút vẽ.
        /// </summary>
        public void SyncDrawingProperties(System.Windows.Media.Color color, int size, string brushType)
        {
            _bridge?.SetDrawingProperties(color, size, brushType);
        }

        /// <summary>
        /// Đặt thuộc tính tẩy.
        /// </summary>
        public void SyncEraserProperties(int size, string mode)
        {
            _bridge?.SetEraserProperties(size, mode);
        }

        /// <summary>
        /// Bật/tắt touch.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _bridge?.SetEnabled(enabled);
        }

        /// <summary>
        /// Kiểm tra có touch đang active không.
        /// </summary>
        public bool HasActiveTouches => _bridge?.HasActiveTouches ?? false;

        /// <summary>
        /// Trạng thái bật/tắt.
        /// </summary>
        public bool IsEnabled => _isEnabled;
    }
}
