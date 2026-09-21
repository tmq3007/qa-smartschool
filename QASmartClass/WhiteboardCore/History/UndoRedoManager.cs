using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.WhiteboardCore.History
{
    /// <summary>
    /// Enum liệt kê các loại hành động Undo/Redo.
    /// </summary>
    public enum ActionType
    {
        Add,
        Remove,
        Modify,
        ClearAll,
        Batch
    }

    /// <summary>
    /// Đại diện cho 1 hành động có thể Undo/Redo.
    /// </summary>
    public class UndoRedoAction
    {
        public ActionType Type { get; set; }
        public UIElement? Element { get; set; }
        public UIElement? Parent { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public List<UndoRedoAction> BatchActions { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }

        public UndoRedoAction()
        {
            Timestamp = DateTime.Now;
            BatchActions = new List<UndoRedoAction>();
            Description = string.Empty;
        }
    }

    /// <summary>
    /// Interface chuẩn cho UndoRedoManager — cho phép Tools gọi mà không cần biết implementation.
    /// </summary>
    public interface IUndoRedoManager
    {
        int UndoCount { get; }
        int RedoCount { get; }
        void RecordAction(UndoRedoAction action);
        void RecordAdd(UIElement element, string? description = null);
        void RecordRemove(UIElement element, string? description = null);
        void RecordModify(UIElement element, object oldValue, object newValue, string? description = null);
        void Undo();
        void Redo();
    }

    /// <summary>
    /// UndoRedoManager — quản lý lịch sử hoàn tác/làm lại theo Command Pattern.
    /// Đây là class thuần logic, không phụ thuộc UI (ngoại trừ UIElement references).
    /// </summary>
    public class UndoRedoManager : IUndoRedoManager
    {
        private readonly Canvas _canvas;
        private Stack<UndoRedoAction> _undoStack = new();
        private Stack<UndoRedoAction> _redoStack = new();
        private int _maxUndoLevels = 100;

        /// <summary>
        /// Callback tùy chọn — Form2 có thể đăng ký để cập nhật trạng thái nút Undo/Redo.
        /// </summary>
        public event Action? StateChanged;

        /// <summary>
        /// Callback tùy chọn — gọi khi cần refresh selectable objects sau Undo/Redo.
        /// </summary>
        public event Action? RequestRefreshObjects;

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public UndoRedoManager(Canvas canvas, int maxUndoLevels = 100)
        {
            _canvas = canvas;
            _maxUndoLevels = maxUndoLevels;
        }

        public void RecordAction(UndoRedoAction action)
        {
            _undoStack.Push(action);
            _redoStack.Clear();

            if (_undoStack.Count > _maxUndoLevels)
            {
                var temp = _undoStack.ToList();
                temp.RemoveAt(temp.Count - 1);
                _undoStack = new Stack<UndoRedoAction>(
                    ((IEnumerable<UndoRedoAction>)temp).Reverse()
                );
            }

            StateChanged?.Invoke();
        }

        public void RecordAdd(UIElement element, string? description = null)
        {
            if (element == null) return;

            // Lọc nét chạm nhỏ (accidental touch)
            // LƯU Ý: Tuyệt đối không lọc dấu chấm chủ ý (Dot) của giáo viên/học sinh!
            if (element is System.Windows.Shapes.Polyline polyline)
            {
                if (polyline.Points != null && polyline.Points.Count > 0)
                {
                    bool isDot = polyline.Points.Count == 2 && Math.Abs(polyline.Points[1].X - polyline.Points[0].X - 0.01) < 0.005 && polyline.Points[1].Y == polyline.Points[0].Y;
                    if (!isDot && description != null && description.Contains("Accidental", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine("Ignoring accidental touch Polyline in Undo/Redo registration");
                        return;
                    }
                }
            }
            else if (element is System.Windows.Shapes.Path path)
            {
                bool isDot = path.Tag is System.Windows.Media.PointCollection pts && pts.Count == 2 && Math.Abs(pts[1].X - pts[0].X - 0.01) < 0.005;
                if (!isDot && path.Data != null)
                {
                    var bounds = path.Data.Bounds;
                    if (bounds.Width <= 5.0 && bounds.Height <= 5.0 &&
                        description != null && description.Contains("Accidental", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine("Ignoring accidental touch Path in Undo/Redo registration");
                        return;
                    }
                }
            }

            var action = new UndoRedoAction
            {
                Type = ActionType.Add,
                Element = element,
                Parent = _canvas,
                Description = description ?? $"Add {element.GetType().Name}"
            };
            RecordAction(action);
        }

        public void RecordRemove(UIElement element, string? description = null)
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Remove,
                Element = element,
                Parent = _canvas,
                Description = description ?? $"Remove {element.GetType().Name}"
            };
            RecordAction(action);
        }

        public void RecordModify(UIElement element, object oldValue, object newValue, string? description = null)
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Modify,
                Element = element,
                Parent = _canvas,
                OldValue = oldValue,
                NewValue = newValue,
                Description = description ?? $"Modify {element.GetType().Name}"
            };
            RecordAction(action);
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            var action = _undoStack.Pop();
            ExecuteUndo(action);
            _redoStack.Push(action);
            StateChanged?.Invoke();
            RequestRefreshObjects?.Invoke();
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var action = _redoStack.Pop();
            ExecuteRedo(action);
            _undoStack.Push(action);
            StateChanged?.Invoke();
            RequestRefreshObjects?.Invoke();
        }

        private void ExecuteUndo(UndoRedoAction action)
        {
            switch (action.Type)
            {
                case ActionType.Add:
                    if (action.Element != null && _canvas.Children.Contains(action.Element))
                        _canvas.Children.Remove(action.Element);
                    break;
                case ActionType.Remove:
                    if (action.Element != null && !_canvas.Children.Contains(action.Element))
                        _canvas.Children.Add(action.Element);
                    break;
                case ActionType.Batch:
                    for (int i = action.BatchActions.Count - 1; i >= 0; i--)
                        ExecuteUndo(action.BatchActions[i]);
                    break;
                case ActionType.ClearAll:
                    foreach (var ba in action.BatchActions)
                        if (ba.Element != null && !_canvas.Children.Contains(ba.Element))
                            _canvas.Children.Add(ba.Element);
                    break;
            }
        }

        private void ExecuteRedo(UndoRedoAction action)
        {
            switch (action.Type)
            {
                case ActionType.Add:
                    if (action.Element != null && !_canvas.Children.Contains(action.Element))
                        _canvas.Children.Add(action.Element);
                    break;
                case ActionType.Remove:
                    if (action.Element != null && _canvas.Children.Contains(action.Element))
                        _canvas.Children.Remove(action.Element);
                    break;
                case ActionType.Batch:
                    foreach (var ba in action.BatchActions)
                        ExecuteRedo(ba);
                    break;
                case ActionType.ClearAll:
                    foreach (var ba in action.BatchActions)
                        if (ba.Element != null && _canvas.Children.Contains(ba.Element))
                            _canvas.Children.Remove(ba.Element);
                    break;
            }
        }
    }
}
