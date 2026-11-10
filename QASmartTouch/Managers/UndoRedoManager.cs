using System;
using System.Collections.Generic;
using System.Windows;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Undo/Redo action types
    /// </summary>
    public enum ActionType
    {
        Add,        // Add element (drawing, shape creation)
        Remove,     // Remove element (erasing, delete)
        Modify,     // Modify element (move, rotate, resize)
        ClearAll,   // Clear all elements
        Batch       // Batch of multiple actions
    }
    
    /// <summary>
    /// Represents an action that can be undone/redone
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
    /// [OBSOLETE] Class quản lý Undo/Redo dạng standalone — KHÔNG ĐƯỢC SỬ DỤNG.
    /// 
    /// Dashboard (Form2_MainDashboard) dùng inline stacks + UndoRedoAction.
    /// Student Whiteboard dùng IUndoableCommand pattern.
    /// 
    /// Nếu cần Undo/Redo cho component mới, hãy tham khảo 2 pattern trên thay vì dùng class này.
    /// </summary>
    [Obsolete("Không sử dụng. Dùng inline stacks trong Form2_MainDashboard hoặc IUndoableCommand pattern trong StudentLocalWhiteboardPage.")]
    public class UndoRedoManager
    {
        #region Constants
        
        /// <summary>
        /// Maximum number of undo levels to prevent memory issues
        /// </summary>
        public const int MAX_UNDO_LEVELS = 50;
        
        #endregion
        
        #region Fields
        
        private readonly Stack<UndoRedoAction> _undoStack;
        private readonly Stack<UndoRedoAction> _redoStack;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets whether undo is available
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;
        
        /// <summary>
        /// Gets whether redo is available
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;
        
        /// <summary>
        /// Gets the number of actions in undo stack
        /// </summary>
        public int UndoCount => _undoStack.Count;
        
        /// <summary>
        /// Gets the number of actions in redo stack
        /// </summary>
        public int RedoCount => _redoStack.Count;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when undo stack changes
        /// </summary>
        public event EventHandler? UndoStackChanged;
        
        /// <summary>
        /// Fired when redo stack changes
        /// </summary>
        public event EventHandler? RedoStackChanged;
        
        /// <summary>
        /// Fired when an action is recorded
        /// </summary>
        public event EventHandler<ActionRecordedEventArgs>? ActionRecorded;
        
        /// <summary>
        /// Fired when an undo is executed
        /// </summary>
        public event EventHandler<UndoRedoExecutedEventArgs>? UndoExecuted;
        
        /// <summary>
        /// Fired when a redo is executed
        /// </summary>
        public event EventHandler<UndoRedoExecutedEventArgs>? RedoExecuted;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of UndoRedoManager
        /// </summary>
        public UndoRedoManager()
        {
            _undoStack = new Stack<UndoRedoAction>();
            _redoStack = new Stack<UndoRedoAction>();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Records an action to the undo stack
        /// </summary>
        /// <param name="action">The action to record</param>
        public void RecordAction(UndoRedoAction action)
        {
            if (action == null)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot record null action");
                return;
            }
            
            // Add to undo stack
            _undoStack.Push(action);
            
            // ✅ Phase D Fix: Clear redo stack khi record action mới
            // (Trước đây thiếu dòng này — nếu ai dùng class sẽ gây lỗi Redo stack không bao giờ bị xóa)
            _redoStack.Clear();
            
            // Limit stack size
            if (_undoStack.Count > MAX_UNDO_LEVELS)
            {
                // Remove oldest action
                var tempStack = new Stack<UndoRedoAction>();
                for (int i = 0; i < MAX_UNDO_LEVELS; i++)
                {
                    tempStack.Push(_undoStack.Pop());
                }
                _undoStack.Clear();
                while (tempStack.Count > 0)
                {
                    _undoStack.Push(tempStack.Pop());
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"📝 Action recorded: {action.Type} - {action.Description} (Undo: {_undoStack.Count})");
            
            // Fire events
            ActionRecorded?.Invoke(this, new ActionRecordedEventArgs(action));
            UndoStackChanged?.Invoke(this, EventArgs.Empty);
            RedoStackChanged?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Records an add action (drawing, shape creation)
        /// </summary>
        /// <param name="element">The element that was added</param>
        /// <param name="description">Optional description</param>
        public void RecordAddAction(UIElement element, string description = "Add element")
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Add,
                Element = element,
                Description = description
            };
            
            RecordAction(action);
        }
        
        /// <summary>
        /// Records a remove action (erasing, delete)
        /// </summary>
        /// <param name="element">The element that was removed</param>
        /// <param name="parent">The parent container</param>
        /// <param name="description">Optional description</param>
        public void RecordRemoveAction(UIElement element, UIElement parent, string description = "Remove element")
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Remove,
                Element = element,
                Parent = parent,
                Description = description
            };
            
            RecordAction(action);
        }
        
        /// <summary>
        /// Records a modify action (move, rotate, resize)
        /// </summary>
        /// <param name="element">The element that was modified</param>
        /// <param name="oldValue">The old value</param>
        /// <param name="newValue">The new value</param>
        /// <param name="description">Optional description</param>
        public void RecordModifyAction(UIElement element, object oldValue, object newValue, string description = "Modify element")
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Modify,
                Element = element,
                OldValue = oldValue,
                NewValue = newValue,
                Description = description
            };
            
            RecordAction(action);
        }
        
        /// <summary>
        /// Records a batch of actions
        /// </summary>
        /// <param name="actions">The actions to batch</param>
        /// <param name="description">Optional description</param>
        public void RecordBatchAction(List<UndoRedoAction> actions, string description = "Batch action")
        {
            var batchAction = new UndoRedoAction
            {
                Type = ActionType.Batch,
                BatchActions = actions,
                Description = description
            };
            
            RecordAction(batchAction);
        }
        
        /// <summary>
        /// Executes an undo operation
        /// </summary>
        /// <returns>The action that was undone, or null if nothing to undo</returns>
        public UndoRedoAction? Undo()
        {
            if (!CanUndo)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot undo: Stack is empty");
                return null;
            }
            
            var action = _undoStack.Pop();
            _redoStack.Push(action);
            
            System.Diagnostics.Debug.WriteLine($"↶ Undo: {action.Type} - {action.Description} (Undo: {_undoStack.Count}, Redo: {_redoStack.Count})");
            
            // Fire events
            UndoExecuted?.Invoke(this, new UndoRedoExecutedEventArgs(action));
            UndoStackChanged?.Invoke(this, EventArgs.Empty);
            RedoStackChanged?.Invoke(this, EventArgs.Empty);
            
            return action;
        }
        
        /// <summary>
        /// Executes a redo operation
        /// </summary>
        /// <returns>The action that was redone, or null if nothing to redo</returns>
        public UndoRedoAction? Redo()
        {
            if (!CanRedo)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot redo: Stack is empty");
                return null;
            }
            
            var action = _redoStack.Pop();
            _undoStack.Push(action);
            
            System.Diagnostics.Debug.WriteLine($"↷ Redo: {action.Type} - {action.Description} (Undo: {_undoStack.Count}, Redo: {_redoStack.Count})");
            
            // Fire events
            RedoExecuted?.Invoke(this, new UndoRedoExecutedEventArgs(action));
            UndoStackChanged?.Invoke(this, EventArgs.Empty);
            RedoStackChanged?.Invoke(this, EventArgs.Empty);
            
            return action;
        }
        
        /// <summary>
        /// Clears all undo/redo history
        /// </summary>
        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            
            System.Diagnostics.Debug.WriteLine($"🗑️ Undo/Redo history cleared");
            
            // Fire events
            UndoStackChanged?.Invoke(this, EventArgs.Empty);
            RedoStackChanged?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Gets the most recent action without removing it from the stack
        /// </summary>
        /// <returns>The most recent action, or null if stack is empty</returns>
        public UndoRedoAction? PeekUndo()
        {
            return CanUndo ? _undoStack.Peek() : null;
        }
        
        /// <summary>
        /// Gets the most recent redo action without removing it from the stack
        /// </summary>
        /// <returns>The most recent redo action, or null if stack is empty</returns>
        public UndoRedoAction? PeekRedo()
        {
            return CanRedo ? _redoStack.Peek() : null;
        }
        
        #endregion
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for action recorded events
    /// </summary>
    public class ActionRecordedEventArgs : EventArgs
    {
        public UndoRedoAction Action { get; }
        
        public ActionRecordedEventArgs(UndoRedoAction action)
        {
            Action = action;
        }
    }
    
    /// <summary>
    /// Event arguments for undo/redo executed events
    /// </summary>
    public class UndoRedoExecutedEventArgs : EventArgs
    {
        public UndoRedoAction Action { get; }
        
        public UndoRedoExecutedEventArgs(UndoRedoAction action)
        {
            Action = action;
        }
    }
    
    #endregion
}
