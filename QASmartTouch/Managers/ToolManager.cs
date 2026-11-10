using System;
using System.Collections.Generic;
using QASmartTouch.Services.Tools;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Tool types available in the application
    /// </summary>
    public enum ToolType
    {
        None,           // No tool selected
        Pen,            // Drawing pen
        Eraser,         // Eraser tool
        Shapes,         // Shape drawing
        Insert,         // Insert content (text, image, video)
        Zoom,           // Zoom tool
        Selection,      // Object selection
        Board,          // Board management
        WindowMode,     // Window mode toggle
        FullScreen,     // Full screen toggle
        More            // More options menu
    }
    
    /// <summary>
    /// Manages tool selection, tool state, and tool switching
    /// Handles tool activation, deactivation, and state persistence
    /// REFACTORED: Now uses internal tool services (PenToolService, EraserToolService, SelectionToolService)
    /// </summary>
    public class ToolManager
    {
        #region Fields
        
        private ToolType _currentTool;
        private ToolType _previousTool;
        private readonly Dictionary<ToolType, ToolSettings> _toolSettings;
        private readonly HashSet<ToolType> _activatedTools;
        
        // Internal tool services
        private readonly IPenToolService _penToolService;
        private readonly IEraserToolService _eraserToolService;
        private readonly ISelectionToolService _selectionToolService;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the currently active tool
        /// </summary>
        public ToolType CurrentTool => _currentTool;
        
        /// <summary>
        /// Gets the previously active tool
        /// </summary>
        public ToolType PreviousTool => _previousTool;
        
        /// <summary>
        /// Gets whether any tool is currently active
        /// </summary>
        public bool HasActiveTool => _currentTool != ToolType.None;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when the active tool changes
        /// </summary>
        public event EventHandler<ToolChangedEventArgs>? ToolChanged;
        
        /// <summary>
        /// Fired when a tool is activated
        /// </summary>
        public event EventHandler<ToolEventArgs>? ToolActivated;
        
        /// <summary>
        /// Fired when a tool is deactivated
        /// </summary>
        public event EventHandler<ToolEventArgs>? ToolDeactivated;
        
        /// <summary>
        /// Fired when tool settings are changed
        /// </summary>
        public event EventHandler<ToolSettingsChangedEventArgs>? ToolSettingsChanged;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of ToolManager
        /// </summary>
        /// <param name="penToolService">Optional pen tool service (auto-created if null)</param>
        /// <param name="eraserToolService">Optional eraser tool service (auto-created if null)</param>
        /// <param name="selectionToolService">Optional selection tool service (auto-created if null)</param>
        public ToolManager(
            IPenToolService? penToolService = null,
            IEraserToolService? eraserToolService = null,
            ISelectionToolService? selectionToolService = null)
        {
            _currentTool = ToolType.None;
            _previousTool = ToolType.None;
            _toolSettings = new Dictionary<ToolType, ToolSettings>();
            _activatedTools = new HashSet<ToolType>();
            
            // Initialize tool services (use provided or create new)
            _penToolService = penToolService ?? new PenToolService();
            _eraserToolService = eraserToolService ?? new EraserToolService();
            _selectionToolService = selectionToolService ?? new SelectionToolService();
            
            // Wire up service events
            WireUpServiceEvents();
            
            // Initialize default settings for each tool
            InitializeDefaultSettings();
            
            System.Diagnostics.Debug.WriteLine("✅ ToolManager initialized with internal tool services");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Selects and activates a tool
        /// </summary>
        /// <param name="tool">The tool to select</param>
        /// <param name="force">Force selection even if already selected</param>
        /// <returns>True if tool was selected, false otherwise</returns>
        public bool SelectTool(ToolType tool, bool force = false)
        {
            if (!force && _currentTool == tool)
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️ Tool already selected: {tool}");
                return true;
            }
            
            var oldTool = _currentTool;
            
            // Deactivate current tool
            if (_currentTool != ToolType.None)
            {
                DeactivateTool(_currentTool);
            }
            
            // Update tool state
            _previousTool = oldTool;
            _currentTool = tool;
            
            // Activate new tool
            if (tool != ToolType.None)
            {
                ActivateTool(tool);
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Tool selected: {oldTool} → {tool}");
            
            // Fire event
            ToolChanged?.Invoke(this, new ToolChangedEventArgs(oldTool, tool));
            
            return true;
        }
        
        /// <summary>
        /// Deselects the current tool
        /// </summary>
        public void DeselectCurrentTool()
        {
            if (_currentTool != ToolType.None)
            {
                SelectTool(ToolType.None);
            }
        }
        
        /// <summary>
        /// Switches back to the previous tool
        /// </summary>
        /// <returns>True if switched successfully, false otherwise</returns>
        public bool SwitchToPreviousTool()
        {
            if (_previousTool == ToolType.None)
            {
                System.Diagnostics.Debug.WriteLine($"❌ No previous tool to switch to");
                return false;
            }
            
            return SelectTool(_previousTool);
        }
        
        /// <summary>
        /// Checks if a specific tool is currently active
        /// </summary>
        /// <param name="tool">The tool to check</param>
        /// <returns>True if the tool is active, false otherwise</returns>
        public bool IsToolActive(ToolType tool)
        {
            return _currentTool == tool;
        }
        
        /// <summary>
        /// Gets the settings for a specific tool
        /// </summary>
        /// <param name="tool">The tool to get settings for</param>
        /// <returns>The tool settings, or null if not found</returns>
        public ToolSettings? GetToolSettings(ToolType tool)
        {
            return _toolSettings.TryGetValue(tool, out var settings) ? settings : null;
        }
        
        /// <summary>
        /// Updates the settings for a specific tool
        /// </summary>
        /// <param name="tool">The tool to update settings for</param>
        /// <param name="settings">The new settings</param>
        public void UpdateToolSettings(ToolType tool, ToolSettings settings)
        {
            if (settings == null)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot update tool settings: Settings is null");
                return;
            }
            
            _toolSettings[tool] = settings;
            
            System.Diagnostics.Debug.WriteLine($"✅ Tool settings updated: {tool}");
            
            // Fire event
            ToolSettingsChanged?.Invoke(this, new ToolSettingsChangedEventArgs(tool, settings));
        }
        
        /// <summary>
        /// Resets all tool settings to defaults
        /// </summary>
        public void ResetAllSettings()
        {
            InitializeDefaultSettings();
            
            System.Diagnostics.Debug.WriteLine($"✅ All tool settings reset to defaults");
        }
        
        /// <summary>
        /// Gets a list of all tools that have been activated at least once
        /// </summary>
        /// <returns>List of activated tools</returns>
        public IReadOnlyCollection<ToolType> GetActivatedTools()
        {
            return _activatedTools;
        }
        
        #endregion
        
        #region Public Methods - Service Access (for advanced scenarios)
        
        /// <summary>
        /// Gets the pen tool service for direct pen operations
        /// </summary>
        public IPenToolService PenToolService => _penToolService;
        
        /// <summary>
        /// Gets the eraser tool service for direct eraser operations
        /// </summary>
        public IEraserToolService EraserToolService => _eraserToolService;
        
        /// <summary>
        /// Gets the selection tool service for direct selection operations
        /// </summary>
        public ISelectionToolService SelectionToolService => _selectionToolService;
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Activates a tool
        /// </summary>
        /// <param name="tool">The tool to activate</param>
        private void ActivateTool(ToolType tool)
        {
            _activatedTools.Add(tool);
            
            // Activate corresponding service
            switch (tool)
            {
                case ToolType.Pen:
                    _penToolService.Activate();
                    break;
                case ToolType.Eraser:
                    _eraserToolService.Activate();
                    break;
                case ToolType.Selection:
                    _selectionToolService.Activate();
                    break;
            }
            
            System.Diagnostics.Debug.WriteLine($"🔧 Tool activated: {tool}");
            
            // Fire event
            ToolActivated?.Invoke(this, new ToolEventArgs(tool));
        }
        
        /// <summary>
        /// Deactivates a tool
        /// </summary>
        /// <param name="tool">The tool to deactivate</param>
        private void DeactivateTool(ToolType tool)
        {
            // Deactivate corresponding service
            switch (tool)
            {
                case ToolType.Pen:
                    _penToolService.Deactivate();
                    break;
                case ToolType.Eraser:
                    _eraserToolService.Deactivate();
                    break;
                case ToolType.Selection:
                    _selectionToolService.Deactivate();
                    break;
            }
            
            System.Diagnostics.Debug.WriteLine($"🔧 Tool deactivated: {tool}");
            
            // Fire event
            ToolDeactivated?.Invoke(this, new ToolEventArgs(tool));
        }
        
        /// <summary>
        /// Wires up service events to sync with ToolManager
        /// </summary>
        private void WireUpServiceEvents()
        {
            // Pen tool service events
            if (_penToolService is PenToolService penService)
            {
                penService.SettingsChanged += (s, e) =>
                {
                    SyncPenSettings(e.PenSize, e.PenColor, e.BrushType);
                };
            }
            
            // Eraser tool service events
            if (_eraserToolService is EraserToolService eraserService)
            {
                eraserService.SettingsChanged += (s, e) =>
                {
                    SyncEraserSettings(e.EraserSize, e.Mode);
                };
            }
            
            // Selection tool service events
            if (_selectionToolService is SelectionToolService selectionService)
            {
                selectionService.SettingsChanged += (s, e) =>
                {
                    SyncSelectionSettings(e.Mode, e.ShowHandles);
                };
            }
        }
        
        /// <summary>
        /// Syncs pen tool settings to ToolSettings dictionary
        /// </summary>
        private void SyncPenSettings(int penSize, System.Windows.Media.Color penColor, BrushType brushType)
        {
            if (_toolSettings.TryGetValue(ToolType.Pen, out var settings))
            {
                settings.SetProperty("PenSize", penSize);
                settings.SetProperty("PenColor", penColor);
                settings.SetProperty("BrushType", brushType.ToString());
                
                ToolSettingsChanged?.Invoke(this, new ToolSettingsChangedEventArgs(ToolType.Pen, settings));
            }
        }
        
        /// <summary>
        /// Syncs eraser tool settings to ToolSettings dictionary
        /// </summary>
        private void SyncEraserSettings(int eraserSize, EraserMode mode)
        {
            if (_toolSettings.TryGetValue(ToolType.Eraser, out var settings))
            {
                settings.SetProperty("EraserSize", eraserSize);
                settings.SetProperty("EraserMode", mode.ToString());
                
                ToolSettingsChanged?.Invoke(this, new ToolSettingsChangedEventArgs(ToolType.Eraser, settings));
            }
        }
        
        /// <summary>
        /// Syncs selection tool settings to ToolSettings dictionary
        /// </summary>
        private void SyncSelectionSettings(SelectionMode mode, bool showHandles)
        {
            if (_toolSettings.TryGetValue(ToolType.Selection, out var settings))
            {
                settings.SetProperty("SelectionMode", mode.ToString());
                settings.SetProperty("ShowHandles", showHandles);
                
                ToolSettingsChanged?.Invoke(this, new ToolSettingsChangedEventArgs(ToolType.Selection, settings));
            }
        }
        
        /// <summary>
        /// Initializes default settings for all tools
        /// </summary>
        private void InitializeDefaultSettings()
        {
            _toolSettings.Clear();
            
            // Pen tool default settings
            _toolSettings[ToolType.Pen] = new ToolSettings
            {
                IsEnabled = true,
                Properties = new Dictionary<string, object>
                {
                    { "BrushType", "Normal" },
                    { "PenSize", 2 },
                    { "PenColor", System.Windows.Media.Colors.Black }
                }
            };
            
            // Eraser tool default settings
            _toolSettings[ToolType.Eraser] = new ToolSettings
            {
                IsEnabled = true,
                Properties = new Dictionary<string, object>
                {
                    { "EraserMode", "Stroke" },
                    { "EraserSize", 20 }
                }
            };
            
            // Zoom tool default settings
            _toolSettings[ToolType.Zoom] = new ToolSettings
            {
                IsEnabled = true,
                Properties = new Dictionary<string, object>
                {
                    { "ZoomLevel", 1.0 },
                    { "MinZoom", 0.5 },
                    { "MaxZoom", 4.0 }
                }
            };
            
            // Selection tool default settings
            _toolSettings[ToolType.Selection] = new ToolSettings
            {
                IsEnabled = true,
                Properties = new Dictionary<string, object>
                {
                    { "SelectionMode", "Single" },
                    { "ShowHandles", true }
                }
            };
            
            // Initialize other tools with default enabled state
            foreach (ToolType tool in Enum.GetValues(typeof(ToolType)))
            {
                if (!_toolSettings.ContainsKey(tool))
                {
                    _toolSettings[tool] = new ToolSettings { IsEnabled = true };
                }
            }
        }
        
        #endregion
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for tool changed events
    /// </summary>
    public class ToolChangedEventArgs : EventArgs
    {
        public ToolType OldTool { get; }
        public ToolType NewTool { get; }
        
        public ToolChangedEventArgs(ToolType oldTool, ToolType newTool)
        {
            OldTool = oldTool;
            NewTool = newTool;
        }
    }
    
    /// <summary>
    /// Event arguments for tool events
    /// </summary>
    public class ToolEventArgs : EventArgs
    {
        public ToolType Tool { get; }
        
        public ToolEventArgs(ToolType tool)
        {
            Tool = tool;
        }
    }
    
    /// <summary>
    /// Event arguments for tool settings changed events
    /// </summary>
    public class ToolSettingsChangedEventArgs : EventArgs
    {
        public ToolType Tool { get; }
        public ToolSettings Settings { get; }
        
        public ToolSettingsChangedEventArgs(ToolType tool, ToolSettings settings)
        {
            Tool = tool;
            Settings = settings;
        }
    }
    
    #endregion
    
    #region Tool Settings Model
    
    /// <summary>
    /// Represents settings for a specific tool
    /// </summary>
    public class ToolSettings
    {
        /// <summary>
        /// Whether the tool is enabled
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// Tool-specific properties (key-value pairs)
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }
        
        /// <summary>
        /// Last time the settings were modified
        /// </summary>
        public DateTime LastModified { get; set; }
        
        public ToolSettings()
        {
            Properties = new Dictionary<string, object>();
            LastModified = DateTime.Now;
        }
        
        /// <summary>
        /// Gets a property value by key
        /// </summary>
        /// <typeparam name="T">The type of the property value</typeparam>
        /// <param name="key">The property key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>The property value, or default if not found</returns>
        public T GetProperty<T>(string key, T defaultValue = default!)
        {
            if (Properties.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Sets a property value
        /// </summary>
        /// <param name="key">The property key</param>
        /// <param name="value">The property value</param>
        public void SetProperty(string key, object value)
        {
            Properties[key] = value;
            LastModified = DateTime.Now;
        }
    }
    
    #endregion
}
