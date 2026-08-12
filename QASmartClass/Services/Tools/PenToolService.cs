using System;
using System.Windows.Media;
using QASmartTouch.Managers;

namespace QASmartTouch.Services.Tools
{
    /// <summary>
    /// Implementation of pen tool operations
    /// Manages pen settings, brush types, and tool activation state
    /// </summary>
    public class PenToolService : IPenToolService
    {
        #region Constants
        
        private const int MIN_PEN_SIZE = 1;
        private const int MAX_PEN_SIZE = 16;
        private const int DEFAULT_PEN_SIZE = 2;
        
        #endregion
        
        #region Fields
        
        private int _penSize;
        private Color _penColor;
        private BrushType _brushType;
        private bool _isActive;
        
        #endregion
        
        #region Properties
        
        /// <inheritdoc/>
        public int PenSize => _penSize;
        
        /// <inheritdoc/>
        public Color PenColor => _penColor;
        
        /// <inheritdoc/>
        public BrushType BrushType => _brushType;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when pen settings change
        /// </summary>
        public event EventHandler<PenSettingsChangedEventArgs>? SettingsChanged;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of PenToolService
        /// </summary>
        public PenToolService()
        {
            ResetToDefaults();
        }
        
        #endregion
        
        #region IPenToolService Implementation
        
        /// <inheritdoc/>
        public void SetPenSize(int size)
        {
            // Clamp to valid range
            size = Math.Clamp(size, MIN_PEN_SIZE, MAX_PEN_SIZE);
            
            if (_penSize == size)
                return;
            
            var oldSize = _penSize;
            _penSize = size;
            
            System.Diagnostics.Debug.WriteLine($"🖊️ Pen size changed: {oldSize} → {size}");
            
            OnSettingsChanged();
        }
        
        /// <inheritdoc/>
        public void SetPenColor(Color color)
        {
            if (_penColor == color)
                return;
            
            var oldColor = _penColor;
            _penColor = color;
            
            System.Diagnostics.Debug.WriteLine($"🎨 Pen color changed: {oldColor} → {color}");
            
            OnSettingsChanged();
        }
        
        /// <inheritdoc/>
        public void SetBrushType(BrushType brushType)
        {
            if (_brushType == brushType)
                return;
            
            var oldBrushType = _brushType;
            _brushType = brushType;
            
            System.Diagnostics.Debug.WriteLine($"🖌️ Brush type changed: {oldBrushType} → {brushType}");
            
            OnSettingsChanged();
        }
        
        /// <inheritdoc/>
        public void Activate()
        {
            if (_isActive)
                return;
            
            _isActive = true;
            System.Diagnostics.Debug.WriteLine("🖊️ Pen tool activated");
        }
        
        /// <inheritdoc/>
        public void Deactivate()
        {
            if (!_isActive)
                return;
            
            _isActive = false;
            System.Diagnostics.Debug.WriteLine("🖊️ Pen tool deactivated");
        }
        
        /// <inheritdoc/>
        public void ResetToDefaults()
        {
            _penSize = DEFAULT_PEN_SIZE;
            _penColor = Colors.Black;
            _brushType = BrushType.Normal;
            _isActive = false;
            
            System.Diagnostics.Debug.WriteLine("🔄 Pen tool reset to defaults");
            
            OnSettingsChanged();
        }
        
        #endregion
        
        #region Private Methods
        
        private void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, new PenSettingsChangedEventArgs(_penSize, _penColor, _brushType));
        }
        
        #endregion
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for pen settings changed event
    /// </summary>
    public class PenSettingsChangedEventArgs : EventArgs
    {
        public int PenSize { get; }
        public Color PenColor { get; }
        public BrushType BrushType { get; }
        
        public PenSettingsChangedEventArgs(int penSize, Color penColor, BrushType brushType)
        {
            PenSize = penSize;
            PenColor = penColor;
            BrushType = brushType;
        }
    }
    
    #endregion
}
