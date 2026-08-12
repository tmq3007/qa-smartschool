using System;
using QASmartTouch.Managers;

namespace QASmartTouch.Services.Tools
{
    /// <summary>
    /// Implementation of eraser tool operations
    /// Manages eraser settings, modes, and tool activation state
    /// </summary>
    public class EraserToolService : IEraserToolService
    {
        #region Constants
        
        private const int MIN_ERASER_SIZE = 5;
        private const int MAX_ERASER_SIZE = 50;
        private const int DEFAULT_ERASER_SIZE = 20;
        
        #endregion
        
        #region Fields
        
        private int _eraserSize;
        private EraserMode _mode;
        private bool _isActive;
        
        #endregion
        
        #region Properties
        
        /// <inheritdoc/>
        public int EraserSize => _eraserSize;
        
        /// <inheritdoc/>
        public EraserMode Mode => _mode;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when eraser settings change
        /// </summary>
        public event EventHandler<EraserSettingsChangedEventArgs>? SettingsChanged;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of EraserToolService
        /// </summary>
        public EraserToolService()
        {
            ResetToDefaults();
        }
        
        #endregion
        
        #region IEraserToolService Implementation
        
        /// <inheritdoc/>
        public void SetEraserSize(int size)
        {
            // Clamp to valid range
            size = Math.Clamp(size, MIN_ERASER_SIZE, MAX_ERASER_SIZE);
            
            if (_eraserSize == size)
                return;
            
            var oldSize = _eraserSize;
            _eraserSize = size;
            
            System.Diagnostics.Debug.WriteLine($"🧹 Eraser size changed: {oldSize} → {size}");
            
            OnSettingsChanged();
        }
        
        /// <inheritdoc/>
        public void SetMode(EraserMode mode)
        {
            if (_mode == mode)
                return;
            
            var oldMode = _mode;
            _mode = mode;
            
            System.Diagnostics.Debug.WriteLine($"🧹 Eraser mode changed: {oldMode} → {mode}");
            
            OnSettingsChanged();
        }
        
        /// <inheritdoc/>
        public void Activate()
        {
            if (_isActive)
                return;
            
            _isActive = true;
            System.Diagnostics.Debug.WriteLine("🧹 Eraser tool activated");
        }
        
        /// <inheritdoc/>
        public void Deactivate()
        {
            if (!_isActive)
                return;
            
            _isActive = false;
            System.Diagnostics.Debug.WriteLine("🧹 Eraser tool deactivated");
        }
        
        /// <inheritdoc/>
        public void ResetToDefaults()
        {
            _eraserSize = DEFAULT_ERASER_SIZE;
            _mode = EraserMode.Stroke;
            _isActive = false;
            
            System.Diagnostics.Debug.WriteLine("🔄 Eraser tool reset to defaults");
            
            OnSettingsChanged();
        }
        
        #endregion
        
        #region Private Methods
        
        private void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, new EraserSettingsChangedEventArgs(_eraserSize, _mode));
        }
        
        #endregion
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for eraser settings changed event
    /// </summary>
    public class EraserSettingsChangedEventArgs : EventArgs
    {
        public int EraserSize { get; }
        public EraserMode Mode { get; }
        
        public EraserSettingsChangedEventArgs(int eraserSize, EraserMode mode)
        {
            EraserSize = eraserSize;
            Mode = mode;
        }
    }
    
    #endregion
}
