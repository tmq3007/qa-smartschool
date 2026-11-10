using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartTouch.Services.Tools
{
    /// <summary>
    /// Implementation of selection tool operations
    /// Manages selection state, modes, and multi-selection tracking
    /// </summary>
    public class SelectionToolService : ISelectionToolService
    {
        #region Fields
        
        private SelectionMode _mode;
        private bool _showHandles;
        private readonly HashSet<string> _selectedObjects;
        private bool _isActive;
        
        #endregion
        
        #region Properties
        
        /// <inheritdoc/>
        public SelectionMode Mode => _mode;
        
        /// <inheritdoc/>
        public bool ShowHandles => _showHandles;
        
        /// <inheritdoc/>
        public int SelectedCount => _selectedObjects.Count;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when selection changes
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
        
        /// <summary>
        /// Fired when selection settings change
        /// </summary>
        public event EventHandler<SelectionSettingsChangedEventArgs>? SettingsChanged;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of SelectionToolService
        /// </summary>
        public SelectionToolService()
        {
            _selectedObjects = new HashSet<string>();
            ResetToDefaults();
        }
        
        #endregion
        
        #region ISelectionToolService Implementation
        
        /// <inheritdoc/>
        public void SetMode(SelectionMode mode)
        {
            if (_mode == mode)
                return;
            
            var oldMode = _mode;
            _mode = mode;
            
            System.Diagnostics.Debug.WriteLine($"👆 Selection mode changed: {oldMode} → {mode}");
            
            OnSettingsChanged();
        }
        
        /// <inheritdoc/>
        public void SetShowHandles(bool show)
        {
            if (_showHandles == show)
                return;
            
            _showHandles = show;
            
            System.Diagnostics.Debug.WriteLine($"👆 Show handles: {show}");
            
            OnSettingsChanged();
        }
        
        /// <inheritdoc/>
        public void AddToSelection(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
                return;
            
            if (_selectedObjects.Add(objectId))
            {
                System.Diagnostics.Debug.WriteLine($"✅ Added to selection: {objectId} (Total: {_selectedObjects.Count})");
                OnSelectionChanged();
            }
        }
        
        /// <inheritdoc/>
        public void RemoveFromSelection(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
                return;
            
            if (_selectedObjects.Remove(objectId))
            {
                System.Diagnostics.Debug.WriteLine($"❌ Removed from selection: {objectId} (Remaining: {_selectedObjects.Count})");
                OnSelectionChanged();
            }
        }
        
        /// <inheritdoc/>
        public void ClearSelection()
        {
            if (_selectedObjects.Count == 0)
                return;
            
            var count = _selectedObjects.Count;
            _selectedObjects.Clear();
            
            System.Diagnostics.Debug.WriteLine($"🗑️ Selection cleared ({count} objects deselected)");
            
            OnSelectionChanged();
        }
        
        /// <inheritdoc/>
        public bool IsSelected(string objectId)
        {
            return !string.IsNullOrEmpty(objectId) && _selectedObjects.Contains(objectId);
        }
        
        /// <inheritdoc/>
        public List<string> GetSelectedObjects()
        {
            return _selectedObjects.ToList();
        }
        
        /// <inheritdoc/>
        public void Activate()
        {
            if (_isActive)
                return;
            
            _isActive = true;
            System.Diagnostics.Debug.WriteLine("👆 Selection tool activated");
        }
        
        /// <inheritdoc/>
        public void Deactivate()
        {
            if (!_isActive)
                return;
            
            _isActive = false;
            ClearSelection(); // Clear selection when deactivating
            
            System.Diagnostics.Debug.WriteLine("👆 Selection tool deactivated");
        }
        
        /// <inheritdoc/>
        public void ResetToDefaults()
        {
            _mode = SelectionMode.Single;
            _showHandles = true;
            _isActive = false;
            _selectedObjects.Clear();
            
            System.Diagnostics.Debug.WriteLine("🔄 Selection tool reset to defaults");
            
            OnSettingsChanged();
        }
        
        #endregion
        
        #region Private Methods
        
        private void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(_selectedObjects.ToList()));
        }
        
        private void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, new SelectionSettingsChangedEventArgs(_mode, _showHandles));
        }
        
        #endregion
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for selection changed event
    /// </summary>
    public class SelectionChangedEventArgs : EventArgs
    {
        public List<string> SelectedObjects { get; }
        
        public SelectionChangedEventArgs(List<string> selectedObjects)
        {
            SelectedObjects = selectedObjects;
        }
    }
    
    /// <summary>
    /// Event arguments for selection settings changed event
    /// </summary>
    public class SelectionSettingsChangedEventArgs : EventArgs
    {
        public SelectionMode Mode { get; }
        public bool ShowHandles { get; }
        
        public SelectionSettingsChangedEventArgs(SelectionMode mode, bool showHandles)
        {
            Mode = mode;
            ShowHandles = showHandles;
        }
    }
    
    #endregion
}
