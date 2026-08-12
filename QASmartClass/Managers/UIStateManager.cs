using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Manages UI state for the main dashboard
    /// Responsibilities:
    /// - Welcome screen visibility
    /// - Tool button states (selected/unselected)
    /// - Window mode management (normal/fullscreen)
    /// - Toolbar visibility control
    /// - UI element state synchronization
    /// </summary>
    public class UIStateManager
    {
        #region Fields

        private readonly Window _window;
        private readonly Panel _welcomePanel;
        private Button _selectedTool;
        
        // Window state tracking
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private bool _isFullScreenMode = false;
        private bool _isWindowMode = false;

        // Colors for tool buttons
        private readonly Color _normalButtonColor = (Color)ColorConverter.ConvertFromString("#F1F2F6");
        private readonly Color _selectedButtonColor = (Color)ColorConverter.ConvertFromString("#2E86DE");

        #endregion

        #region Properties

        /// <summary>
        /// Is welcome screen visible
        /// </summary>
        public bool IsWelcomeVisible => _welcomePanel?.Visibility == Visibility.Visible;

        /// <summary>
        /// Current window mode
        /// </summary>
        public WindowMode CurrentWindowMode
        {
            get
            {
                if (_isFullScreenMode) return WindowMode.FullScreen;
                if (_isWindowMode) return WindowMode.Windowed;
                return WindowMode.Normal;
            }
        }

        /// <summary>
        /// Currently selected tool button
        /// </summary>
        public Button SelectedTool => _selectedTool;

        #endregion

        #region Events

        /// <summary>
        /// Fired when window mode changes
        /// </summary>
        public event EventHandler<WindowModeChangedEventArgs> OnWindowModeChanged;

        /// <summary>
        /// Fired when UI state changes
        /// </summary>
        public event EventHandler<UIStateChangedEventArgs> OnUIStateChanged;

        /// <summary>
        /// Fired when tool selection changes
        /// </summary>
        public event EventHandler<ToolSelectionChangedEventArgs> OnToolSelectionChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize UIStateManager
        /// </summary>
        /// <param name="window">Main window reference</param>
        /// <param name="welcomePanel">Welcome panel reference</param>
        public UIStateManager(Window window, Panel welcomePanel)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _welcomePanel = welcomePanel;

            // Save initial window state
            _previousWindowState = _window.WindowState;
            _previousWindowStyle = _window.WindowStyle;

            System.Diagnostics.Debug.WriteLine("✅ UIStateManager initialized");
        }

        #endregion

        #region Public Methods - Welcome State

        /// <summary>
        /// Show welcome screen
        /// </summary>
        public void ShowWelcomeState()
        {
            if (_welcomePanel != null)
            {
                _welcomePanel.Visibility = Visibility.Visible;
                OnUIStateChanged?.Invoke(this, new UIStateChangedEventArgs("WelcomeShown"));
                System.Diagnostics.Debug.WriteLine("👋 Welcome state shown");
            }
        }

        /// <summary>
        /// Hide welcome screen
        /// </summary>
        public void HideWelcomeState()
        {
            if (_welcomePanel != null)
            {
                _welcomePanel.Visibility = Visibility.Collapsed;
                OnUIStateChanged?.Invoke(this, new UIStateChangedEventArgs("WelcomeHidden"));
                System.Diagnostics.Debug.WriteLine("👋 Welcome state hidden");
            }
        }

        #endregion

        #region Public Methods - Tool Selection

        /// <summary>
        /// Select a tool button (highlights it and deselects previous)
        /// </summary>
        /// <param name="toolButton">Tool button to select</param>
        public void SelectTool(Button toolButton)
        {
            if (toolButton == null)
                return;

            // Deselect previous tool
            if (_selectedTool != null)
            {
                _selectedTool.Background = new SolidColorBrush(_normalButtonColor);
                UpdateIconColor(_selectedTool, Colors.Black);
            }

            // Select new tool
            _selectedTool = toolButton;
            _selectedTool.Background = new SolidColorBrush(_selectedButtonColor);
            UpdateIconColor(_selectedTool, Colors.White);

            OnToolSelectionChanged?.Invoke(this, new ToolSelectionChangedEventArgs(toolButton.Name));
            System.Diagnostics.Debug.WriteLine($"🔧 Tool selected: {toolButton.Name}");
        }

        /// <summary>
        /// Deselect current tool
        /// </summary>
        public void DeselectCurrentTool()
        {
            if (_selectedTool != null)
            {
                _selectedTool.Background = new SolidColorBrush(_normalButtonColor);
                UpdateIconColor(_selectedTool, Colors.Black);
                
                string previousToolName = _selectedTool.Name;
                _selectedTool = null;

                OnToolSelectionChanged?.Invoke(this, new ToolSelectionChangedEventArgs(null));
                System.Diagnostics.Debug.WriteLine($"🔧 Tool deselected: {previousToolName}");
            }
        }

        /// <summary>
        /// Update tool button states (enable/disable)
        /// </summary>
        /// <param name="toolName">Tool name</param>
        /// <param name="isEnabled">Enable state</param>
        public void UpdateToolButtonState(string toolName, bool isEnabled)
        {
            // This can be extended to manage button enable/disable states
            System.Diagnostics.Debug.WriteLine($"🔧 Tool {toolName} state: {(isEnabled ? "enabled" : "disabled")}");
        }

        #endregion

        #region Public Methods - Window Mode

        /// <summary>
        /// Toggle fullscreen mode
        /// </summary>
        public void ToggleFullScreen()
        {
            _isFullScreenMode = !_isFullScreenMode;

            if (_isFullScreenMode)
            {
                // Save current state
                _previousWindowState = _window.WindowState;
                _previousWindowStyle = _window.WindowStyle;

                // Disable window mode if active
                if (_isWindowMode)
                {
                    _isWindowMode = false;
                }

                // Enable fullscreen
                _window.WindowState = WindowState.Maximized;
                _window.WindowStyle = WindowStyle.None;
                _window.ResizeMode = ResizeMode.NoResize;
                _window.Topmost = true;

                System.Diagnostics.Debug.WriteLine("🖥️ Fullscreen mode: ON");
            }
            else
            {
                // Exit fullscreen
                _window.WindowState = _previousWindowState;
                _window.WindowStyle = _previousWindowStyle;
                _window.ResizeMode = ResizeMode.CanResize;
                _window.Topmost = false;

                System.Diagnostics.Debug.WriteLine("🖥️ Fullscreen mode: OFF");
            }

            OnWindowModeChanged?.Invoke(this, new WindowModeChangedEventArgs(CurrentWindowMode));
        }

        /// <summary>
        /// Toggle window mode
        /// </summary>
        public void ToggleWindowMode()
        {
            _isWindowMode = !_isWindowMode;

            if (_isWindowMode)
            {
                // Save current state
                _previousWindowState = _window.WindowState;
                _previousWindowStyle = _previousWindowStyle;

                // Disable fullscreen if active
                if (_isFullScreenMode)
                {
                    _isFullScreenMode = false;
                }

                // Set to normal windowed mode
                _window.WindowState = WindowState.Normal;
                _window.WindowStyle = WindowStyle.SingleBorderWindow;
                _window.ResizeMode = ResizeMode.CanResize;

                System.Diagnostics.Debug.WriteLine("🪟 Window mode: ON");
            }
            else
            {
                // Return to previous state
                _window.WindowState = _previousWindowState;
                _window.WindowStyle = _previousWindowStyle;

                System.Diagnostics.Debug.WriteLine("🪟 Window mode: OFF");
            }

            OnWindowModeChanged?.Invoke(this, new WindowModeChangedEventArgs(CurrentWindowMode));
        }

        /// <summary>
        /// Set specific window mode
        /// </summary>
        /// <param name="mode">Window mode to set</param>
        public void SetWindowMode(WindowMode mode)
        {
            switch (mode)
            {
                case WindowMode.Normal:
                    if (_isFullScreenMode || _isWindowMode)
                    {
                        _isFullScreenMode = false;
                        _isWindowMode = false;
                        _window.WindowState = _previousWindowState;
                        _window.WindowStyle = _previousWindowStyle;
                        _window.ResizeMode = ResizeMode.CanResize;
                        _window.Topmost = false;
                    }
                    break;

                case WindowMode.FullScreen:
                    if (!_isFullScreenMode)
                        ToggleFullScreen();
                    break;

                case WindowMode.Windowed:
                    if (!_isWindowMode)
                        ToggleWindowMode();
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"🖥️ Window mode set to: {mode}");
        }

        #endregion

        #region Public Methods - Toolbar

        /// <summary>
        /// Show toolbar
        /// </summary>
        public void ShowToolbar()
        {
            // This can be extended to manage toolbar visibility
            OnUIStateChanged?.Invoke(this, new UIStateChangedEventArgs("ToolbarShown"));
            System.Diagnostics.Debug.WriteLine("🔧 Toolbar shown");
        }

        /// <summary>
        /// Hide toolbar
        /// </summary>
        public void HideToolbar()
        {
            // This can be extended to manage toolbar visibility
            OnUIStateChanged?.Invoke(this, new UIStateChangedEventArgs("ToolbarHidden"));
            System.Diagnostics.Debug.WriteLine("🔧 Toolbar hidden");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Update icon color for tool button
        /// </summary>
        private void UpdateIconColor(Button button, Color color)
        {
            // Find Path element in button template and update its fill color
            if (button.Content is System.Windows.Shapes.Path path)
            {
                path.Fill = new SolidColorBrush(color);
            }
            else if (button.Content is Panel panel)
            {
                // Search for Path in panel children
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Shapes.Path childPath)
                    {
                        childPath.Fill = new SolidColorBrush(color);
                    }
                }
            }
        }

        #endregion

        #region Public Methods - Utility

        /// <summary>
        /// Check if fullscreen mode is active
        /// </summary>
        public bool IsFullScreen()
        {
            return _isFullScreenMode;
        }

        /// <summary>
        /// Check if window mode is active
        /// </summary>
        public bool IsWindowedMode()
        {
            return _isWindowMode;
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// Window mode enum
    /// </summary>
    public enum WindowMode
    {
        Normal,
        FullScreen,
        Windowed
    }

    #endregion

    #region Event Args

    /// <summary>
    /// Event args for window mode changed event
    /// </summary>
    public class WindowModeChangedEventArgs : EventArgs
    {
        public WindowMode Mode { get; }

        public WindowModeChangedEventArgs(WindowMode mode)
        {
            Mode = mode;
        }
    }

    /// <summary>
    /// Event args for UI state changed event
    /// </summary>
    public class UIStateChangedEventArgs : EventArgs
    {
        public string StateName { get; }

        public UIStateChangedEventArgs(string stateName)
        {
            StateName = stateName;
        }
    }

    /// <summary>
    /// Event args for tool selection changed event
    /// </summary>
    public class ToolSelectionChangedEventArgs : EventArgs
    {
        public string ToolName { get; }

        public ToolSelectionChangedEventArgs(string toolName)
        {
            ToolName = toolName;
        }
    }

    #endregion
}
