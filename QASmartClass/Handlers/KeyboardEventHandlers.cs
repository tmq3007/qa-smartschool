using System;
using System.Windows.Input;
using QASmartTouch.Managers;
using QASmartTouch.Services;

namespace QASmartTouch.Handlers
{
    /// <summary>
    /// Handles all keyboard shortcuts
    /// Responsibilities:
    /// - Handle keyboard shortcuts (Ctrl+C, Ctrl+V, Delete, ESC, etc.)
    /// - Coordinate keyboard actions with managers
    /// - Provide consistent keyboard experience across the application
    /// </summary>
    public class KeyboardEventHandlers
    {
        #region Fields

        private readonly UndoRedoManager _undoRedoManager;
        private readonly SelectionManager _selectionManager;
        private readonly UIStateManager _uiStateManager;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a keyboard shortcut is executed
        /// </summary>
        public event EventHandler<string> OnShortcutExecuted;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize KeyboardEventHandlers
        /// </summary>
        public KeyboardEventHandlers(
            UndoRedoManager undoRedoManager,
            SelectionManager selectionManager,
            UIStateManager uiStateManager)
        {
            _undoRedoManager = undoRedoManager;
            _selectionManager = selectionManager;
            _uiStateManager = uiStateManager;

            System.Diagnostics.Debug.WriteLine("✅ KeyboardEventHandlers initialized");
        }

        #endregion

        #region Public Methods - Main Handler

        /// <summary>
        /// Handle keyboard key down event
        /// </summary>
        /// <param name="e">KeyEventArgs from PreviewKeyDown</param>
        /// <returns>True if event was handled, false otherwise</returns>
        public bool HandleKeyDown(KeyEventArgs e)
        {
            bool handled = false;

            // Check for modifier keys
            bool isCtrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool isShiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            bool isAltPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

            // Handle shortcuts based on key combination
            if (isCtrlPressed)
            {
                handled = HandleCtrlShortcuts(e.Key, isShiftPressed);
            }
            else
            {
                handled = HandleStandaloneKeys(e.Key);
            }

            if (handled)
            {
                e.Handled = true;
            }

            return handled;
        }

        #endregion

        #region Private Methods - Ctrl Shortcuts

        /// <summary>
        /// Handle Ctrl+ keyboard shortcuts
        /// </summary>
        private bool HandleCtrlShortcuts(Key key, bool isShiftPressed)
        {
            switch (key)
            {
                case Key.C:
                    return OnCtrlC();

                case Key.V:
                    return OnCtrlV();

                case Key.X:
                    return OnCtrlX();

                case Key.A:
                    return OnCtrlA();

                case Key.Z:
                    if (isShiftPressed)
                        return OnCtrlShiftZ(); // Redo
                    else
                        return OnCtrlZ(); // Undo

                case Key.Y:
                    return OnCtrlY(); // Redo (alternative)

                case Key.D:
                    return OnCtrlD(); // Duplicate

                default:
                    return false;
            }
        }

        /// <summary>
        /// Handle standalone keyboard shortcuts (no modifiers)
        /// </summary>
        private bool HandleStandaloneKeys(Key key)
        {
            switch (key)
            {
                case Key.Delete:
                    return OnDelete();

                case Key.Escape:
                    return OnEscape();

                case Key.F11:
                    return OnF11(); // Toggle fullscreen

                default:
                    return false;
            }
        }

        #endregion

        #region Private Methods - Individual Shortcuts

        /// <summary>
        /// Handle Ctrl+C (Copy)
        /// </summary>
        private bool OnCtrlC()
        {
            if (_selectionManager?.HasSelectedObjects() == true)
            {
                _selectionManager.CopySelectedObject();
                OnShortcutExecuted?.Invoke(this, "Copy");
                System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+C: Copy");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle Ctrl+V (Paste)
        /// </summary>
        private bool OnCtrlV()
        {
            if (_selectionManager?.HasClipboardContent() == true)
            {
                // Paste will be handled by canvas click event
                // This just confirms clipboard has content
                OnShortcutExecuted?.Invoke(this, "Paste");
                System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+V: Paste mode activated");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle Ctrl+X (Cut)
        /// </summary>
        private bool OnCtrlX()
        {
            if (_selectionManager?.HasSelectedObjects() == true)
            {
                _selectionManager.CopySelectedObject();
                _selectionManager.DeleteSelectedObjects();
                OnShortcutExecuted?.Invoke(this, "Cut");
                System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+X: Cut");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle Ctrl+A (Select All)
        /// </summary>
        private bool OnCtrlA()
        {
            // Select all functionality would need to be implemented in SelectionManager
            OnShortcutExecuted?.Invoke(this, "SelectAll");
            System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+A: Select All");
            return true;
        }

        /// <summary>
        /// Handle Ctrl+Z (Undo)
        /// </summary>
        private bool OnCtrlZ()
        {
            if (_undoRedoManager?.CanUndo == true)
            {
                _undoRedoManager.Undo();
                OnShortcutExecuted?.Invoke(this, "Undo");
                System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+Z: Undo");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle Ctrl+Y (Redo)
        /// </summary>
        private bool OnCtrlY()
        {
            if (_undoRedoManager?.CanRedo == true)
            {
                _undoRedoManager.Redo();
                OnShortcutExecuted?.Invoke(this, "Redo");
                System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+Y: Redo");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle Ctrl+Shift+Z (Redo alternative)
        /// </summary>
        private bool OnCtrlShiftZ()
        {
            if (_undoRedoManager?.CanRedo == true)
            {
                _undoRedoManager.Redo();
                OnShortcutExecuted?.Invoke(this, "Redo");
                System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+Shift+Z: Redo");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle Ctrl+D (Duplicate)
        /// </summary>
        private bool OnCtrlD()
        {
            if (_selectionManager?.HasSelectedObjects() == true)
            {
                _selectionManager.CopySelectedObject();
                // Auto-paste would happen on next canvas click
                OnShortcutExecuted?.Invoke(this, "Duplicate");
                System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+D: Duplicate");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle Delete key
        /// </summary>
        private bool OnDelete()
        {
            if (_selectionManager?.HasSelectedObjects() == true)
            {
                _selectionManager.DeleteSelectedObjects();
                OnShortcutExecuted?.Invoke(this, "Delete");
                System.Diagnostics.Debug.WriteLine("⌨️ Delete: Delete selected");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handle Escape key
        /// </summary>
        private bool OnEscape()
        {
            // Cancel paste mode if active
            if (_selectionManager?.HasClipboardContent() == true)
            {
                _selectionManager.ClearClipboard();
                OnShortcutExecuted?.Invoke(this, "CancelPaste");
                System.Diagnostics.Debug.WriteLine("⌨️ ESC: Cancelled paste mode");
                return true;
            }

            // Deselect all if anything is selected
            if (_selectionManager?.HasSelectedObjects() == true)
            {
                _selectionManager.DeselectAll();
                OnShortcutExecuted?.Invoke(this, "DeselectAll");
                System.Diagnostics.Debug.WriteLine("⌨️ ESC: Deselected all");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle F11 key (Toggle fullscreen)
        /// </summary>
        private bool OnF11()
        {
            _uiStateManager?.ToggleFullScreen();
            OnShortcutExecuted?.Invoke(this, "ToggleFullscreen");
            System.Diagnostics.Debug.WriteLine("⌨️ F11: Toggle fullscreen");
            return true;
        }

        #endregion

        #region Public Methods - Utility

        /// <summary>
        /// Get description of a keyboard shortcut
        /// </summary>
        public string GetShortcutDescription(string action)
        {
            return action switch
            {
                "Copy" => "Ctrl+C: Copy selected object",
                "Paste" => "Ctrl+V: Paste from clipboard",
                "Cut" => "Ctrl+X: Cut selected object",
                "SelectAll" => "Ctrl+A: Select all objects",
                "Undo" => "Ctrl+Z: Undo last action",
                "Redo" => "Ctrl+Y or Ctrl+Shift+Z: Redo last undone action",
                "Duplicate" => "Ctrl+D: Duplicate selected object",
                "Delete" => "Delete: Delete selected object",
                "CancelPaste" => "ESC: Cancel paste mode",
                "DeselectAll" => "ESC: Deselect all objects",
                "ToggleFullscreen" => "F11: Toggle fullscreen mode",
                _ => "Unknown shortcut"
            };
        }

        /// <summary>
        /// Get all available keyboard shortcuts
        /// </summary>
        public string[] GetAllShortcuts()
        {
            return new[]
            {
                "Copy", "Paste", "Cut", "SelectAll",
                "Undo", "Redo", "Duplicate", "Delete",
                "CancelPaste", "DeselectAll", "ToggleFullscreen"
            };
        }

        #endregion
    }
}
