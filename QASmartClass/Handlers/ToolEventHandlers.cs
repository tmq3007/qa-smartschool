using System;
using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Managers;

namespace QASmartTouch.Handlers
{
    /// <summary>
    /// Handles all tool button click events
    /// Responsibilities:
    /// - Handle tool button clicks (Pen, Eraser, Undo, Redo, etc.)
    /// - Show/hide tool submenus
    /// - Coordinate tool activation with managers
    /// - Update UI state when tools change
    /// </summary>
    public class ToolEventHandlers
    {
        #region Fields

        private readonly ToolManager _toolManager;
        private readonly DrawingEngine _drawingEngine;
        private readonly EraserEngine _eraserEngine;
        private readonly UndoRedoManager _undoRedoManager;
        private readonly ZoomManager _zoomManager;
        private readonly UIStateManager _uiStateManager;
        private readonly CanvasEventHandlers _canvasEventHandlers;

        // Active submenu tracking
        private Window _activeSubMenu;
        private Button _activeSubMenuButton;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a tool is activated
        /// </summary>
        public event EventHandler<string> OnToolActivated;

        /// <summary>
        /// Fired when a submenu is opened
        /// </summary>
        public event EventHandler<string> OnSubmenuOpened;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize ToolEventHandlers
        /// </summary>
        public ToolEventHandlers(
            ToolManager toolManager,
            DrawingEngine drawingEngine,
            EraserEngine eraserEngine,
            UndoRedoManager undoRedoManager,
            ZoomManager zoomManager,
            UIStateManager uiStateManager,
            CanvasEventHandlers canvasEventHandlers)
        {
            _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            _drawingEngine = drawingEngine;
            _eraserEngine = eraserEngine;
            _undoRedoManager = undoRedoManager;
            _zoomManager = zoomManager;
            _uiStateManager = uiStateManager;
            _canvasEventHandlers = canvasEventHandlers;

            System.Diagnostics.Debug.WriteLine("✅ ToolEventHandlers initialized");
        }

        #endregion

        #region Public Methods - Tool Button Handlers

        /// <summary>
        /// Handle Pen button click
        /// </summary>
        public void OnPenClick(Button button)
        {
            // Select tool in UI
            _uiStateManager?.SelectTool(button);
            _uiStateManager?.HideWelcomeState();

            // Activate pen tool
            _toolManager?.SelectTool(ToolType.Pen);
            
            // Enable drawing mode
            _canvasEventHandlers?.SetMode("Drawing");

            OnToolActivated?.Invoke(this, "Pen");
            System.Diagnostics.Debug.WriteLine("🖊️ Pen tool activated");
        }

        /// <summary>
        /// Handle Eraser button click
        /// </summary>
        public void OnEraserClick(Button button)
        {
            // Select tool in UI
            _uiStateManager?.SelectTool(button);
            _uiStateManager?.HideWelcomeState();

            // Activate eraser tool
            _toolManager?.SelectTool(ToolType.Eraser);
            
            // Enable eraser mode
            if (_eraserEngine != null)
            {
                _eraserEngine.EnableEraser(EraserMode.Drag); // Default to drag mode
                _canvasEventHandlers?.SetMode("Eraser");
            }

            OnToolActivated?.Invoke(this, "Eraser");
            System.Diagnostics.Debug.WriteLine("🧹 Eraser tool activated");
        }

        /// <summary>
        /// Handle Undo button click
        /// </summary>
        public void OnUndoClick(Button button)
        {
            if (_undoRedoManager != null && _undoRedoManager.CanUndo)
            {
                _undoRedoManager.Undo();
                System.Diagnostics.Debug.WriteLine("↩️ Undo executed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Cannot undo - stack is empty");
            }
        }

        /// <summary>
        /// Handle Redo button click
        /// </summary>
        public void OnRedoClick(Button button)
        {
            if (_undoRedoManager != null && _undoRedoManager.CanRedo)
            {
                _undoRedoManager.Redo();
                System.Diagnostics.Debug.WriteLine("↪️ Redo executed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Cannot redo - stack is empty");
            }
        }

        /// <summary>
        /// Handle Shapes button click
        /// </summary>
        public void OnShapesClick(Button button)
        {
            // Select tool in UI
            _uiStateManager?.SelectTool(button);
            _uiStateManager?.HideWelcomeState();

            // Activate shapes tool
            _toolManager?.SelectTool(ToolType.Shapes);

            // Show shapes submenu (this would need to be implemented)
            // ShowShapesSubmenu(button);

            OnToolActivated?.Invoke(this, "Shapes");
            System.Diagnostics.Debug.WriteLine("📐 Shapes tool activated");
        }

        /// <summary>
        /// Handle Inserts button click
        /// </summary>
        public void OnInsertsClick(Button button)
        {
            // Select tool in UI
            _uiStateManager?.SelectTool(button);
            _uiStateManager?.HideWelcomeState();

            // Activate inserts tool
            _toolManager?.SelectTool(ToolType.Insert);

            // Show inserts submenu (this would need to be implemented)
            // ShowInsertsSubmenu(button);

            OnToolActivated?.Invoke(this, "Inserts");
            System.Diagnostics.Debug.WriteLine("📎 Inserts tool activated");
        }

        /// <summary>
        /// Handle Zoom button click
        /// </summary>
        public void OnZoomClick(Button button)
        {
            // Select tool in UI
            _uiStateManager?.SelectTool(button);
            _uiStateManager?.HideWelcomeState();

            // Activate zoom tool
            _toolManager?.SelectTool(ToolType.Zoom);

            // Show zoom submenu (this would need to be implemented)
            // ShowZoomSubmenu(button);

            OnToolActivated?.Invoke(this, "Zoom");
            System.Diagnostics.Debug.WriteLine("🔍 Zoom tool activated");
        }

        /// <summary>
        /// Handle Selection button click
        /// </summary>
        public void OnSelectionClick(Button button)
        {
            // Select tool in UI
            _uiStateManager?.SelectTool(button);
            _uiStateManager?.HideWelcomeState();

            // Activate selection tool
            _toolManager?.SelectTool(ToolType.Selection);
            _canvasEventHandlers?.SetMode("Selection");

            OnToolActivated?.Invoke(this, "Selection");
            System.Diagnostics.Debug.WriteLine("🎯 Selection tool activated");
        }

        /// <summary>
        /// Handle Board button click
        /// </summary>
        public void OnBoardClick(Button button)
        {
            // Select tool in UI
            _uiStateManager?.SelectTool(button);

            // Activate board tool
            _toolManager?.SelectTool(ToolType.Board);

            // Show board management submenu (this would need to be implemented)
            // ShowBoardSubmenu(button);

            OnToolActivated?.Invoke(this, "Board");
            System.Diagnostics.Debug.WriteLine("📋 Board tool activated");
        }

        /// <summary>
        /// Handle Window Mode button click
        /// </summary>
        public void OnWindowModeClick(Button button)
        {
            // Toggle window mode
            _uiStateManager?.ToggleWindowMode();

            System.Diagnostics.Debug.WriteLine("🪟 Window mode toggled");
        }

        /// <summary>
        /// Handle Full Screen button click
        /// </summary>
        public void OnFullScreenClick(Button button)
        {
            // Toggle fullscreen mode
            _uiStateManager?.ToggleFullScreen();

            System.Diagnostics.Debug.WriteLine("🖥️ Fullscreen toggled");
        }

        /// <summary>
        /// Handle More button click
        /// </summary>
        public void OnMoreClick(Button button)
        {
            // Select tool in UI
            _uiStateManager?.SelectTool(button);

            // Show more menu (this would need to be implemented)
            // ShowMoreMenu(button);

            OnToolActivated?.Invoke(this, "More");
            System.Diagnostics.Debug.WriteLine("⚙️ More menu activated");
        }

        /// <summary>
        /// Handle Exit button click
        /// </summary>
        public void OnExitClick(Button button)
        {
            // Close application (this would need confirmation dialog)
            System.Diagnostics.Debug.WriteLine("❌ Exit button clicked");
            
            // This would typically show a confirmation dialog
            // Application.Current.Shutdown();
        }

        #endregion

        #region Public Methods - Submenu Management

        /// <summary>
        /// Close active submenu if any
        /// </summary>
        public void CloseActiveSubMenu()
        {
            if (_activeSubMenu != null)
            {
                _activeSubMenu.Close();
                _activeSubMenu = null;
                _activeSubMenuButton = null;
                System.Diagnostics.Debug.WriteLine("❌ Closed active submenu");
            }
        }

        /// <summary>
        /// Show a submenu window
        /// </summary>
        /// <param name="submenu">Submenu window to show</param>
        /// <param name="button">Button that triggered the submenu</param>
        public void ShowSubmenu(Window submenu, Button button)
        {
            // Close any existing submenu
            CloseActiveSubMenu();

            // Show new submenu
            _activeSubMenu = submenu;
            _activeSubMenuButton = button;
            
            // Position submenu near button
            PositionSubmenuNearButton(submenu, button);
            
            submenu.Show();
            
            OnSubmenuOpened?.Invoke(this, submenu.GetType().Name);
            System.Diagnostics.Debug.WriteLine($"📋 Opened submenu: {submenu.GetType().Name}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Position submenu window near the button that triggered it
        /// </summary>
        private void PositionSubmenuNearButton(Window submenu, Button button)
        {
            try
            {
                // Get button position on screen
                Point buttonPosition = button.PointToScreen(new Point(0, 0));
                
                // Position submenu to the right of the button
                submenu.Left = buttonPosition.X + button.ActualWidth + 10;
                submenu.Top = buttonPosition.Y;

                // Basic screen bounds checking using SystemParameters
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                
                if (submenu.Left + submenu.Width > screenWidth)
                {
                    submenu.Left = buttonPosition.X - submenu.Width - 10;
                }

                if (submenu.Top + submenu.Height > screenHeight)
                {
                    submenu.Top = screenHeight - submenu.Height - 40; // 40px for taskbar
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error positioning submenu: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods - Zoom Specific

        /// <summary>
        /// Handle zoom in action
        /// </summary>
        public void OnZoomIn()
        {
            _zoomManager?.ZoomIn();
            System.Diagnostics.Debug.WriteLine("🔍➕ Zoom in");
        }

        /// <summary>
        /// Handle zoom out action
        /// </summary>
        public void OnZoomOut()
        {
            _zoomManager?.ZoomOut();
            System.Diagnostics.Debug.WriteLine("🔍➖ Zoom out");
        }

        /// <summary>
        /// Handle zoom reset action
        /// </summary>
        public void OnZoomReset()
        {
            _zoomManager?.ResetZoom();
            System.Diagnostics.Debug.WriteLine("🔍🔄 Zoom reset");
        }

        /// <summary>
        /// Handle zoom to area action
        /// </summary>
        public void OnZoomToArea()
        {
            _canvasEventHandlers?.SetMode("ZoomArea");
            System.Diagnostics.Debug.WriteLine("🔍🎯 Zoom to area mode activated");
        }

        #endregion
    }
}
