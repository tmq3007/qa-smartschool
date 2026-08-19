using System.Windows.Controls;

namespace QASmartClass.WhiteboardCore.Managers
{
    /// <summary>
    /// Context Interface — cho phép WhiteboardCore truy cập Canvas và History
    /// mà không phụ thuộc ngược vào Form2_MainDashboard.
    /// Form2 sẽ implement interface này.
    /// </summary>
    public interface IWhiteboardContext
    {
        Canvas MainCanvas { get; }
        History.IUndoRedoManager History { get; }
        void InvalidateVisual();
    }
}
