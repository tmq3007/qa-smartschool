using System.Windows.Controls;
using System.Windows.Ink;

namespace QASmartClass.WhiteboardCore.Managers
{
    /// <summary>
    /// Context Interface — cho phép WhiteboardCore truy cập Canvas, Inking Layer và History
    /// mà không phụ thuộc ngược vào Form2_MainDashboard.
    /// Form2_MainDashboard sẽ implement interface này.
    /// </summary>
    public interface IWhiteboardContext
    {
        Canvas MainCanvas { get; }
        InkPresenter? MainInkPresenter { get; }
        StrokeCollection? Strokes { get; }
        History.IUndoRedoManager? History { get; }
        void InvalidateVisual();
    }
}
