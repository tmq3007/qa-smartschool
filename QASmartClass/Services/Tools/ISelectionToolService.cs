using System.Collections.Generic;
using System.Windows;

namespace QASmartTouch.Services.Tools
{
    /// <summary>
    /// Selection mode types
    /// </summary>
    public enum SelectionMode
    {
        Single,      // Select single object
        Multiple,    // Select multiple objects
        Rectangular  // Rectangular selection
    }
    
    /// <summary>
    /// Interface for selection tool operations
    /// Handles selection modes and multi-selection state
    /// </summary>
    public interface ISelectionToolService
    {
        /// <summary>
        /// Gets the current selection mode
        /// </summary>
        SelectionMode Mode { get; }
        
        /// <summary>
        /// Gets whether handles should be shown on selected objects
        /// </summary>
        bool ShowHandles { get; }
        
        /// <summary>
        /// Gets the selected objects count
        /// </summary>
        int SelectedCount { get; }
        
        /// <summary>
        /// Sets the selection mode
        /// </summary>
        /// <param name="mode">The selection mode</param>
        void SetMode(SelectionMode mode);
        
        /// <summary>
        /// Sets whether to show handles on selected objects
        /// </summary>
        /// <param name="show">True to show handles, false to hide</param>
        void SetShowHandles(bool show);
        
        /// <summary>
        /// Adds an object to selection
        /// </summary>
        /// <param name="objectId">The object identifier</param>
        void AddToSelection(string objectId);
        
        /// <summary>
        /// Removes an object from selection
        /// </summary>
        /// <param name="objectId">The object identifier</param>
        void RemoveFromSelection(string objectId);
        
        /// <summary>
        /// Clears all selection
        /// </summary>
        void ClearSelection();
        
        /// <summary>
        /// Checks if an object is selected
        /// </summary>
        /// <param name="objectId">The object identifier</param>
        /// <returns>True if selected, false otherwise</returns>
        bool IsSelected(string objectId);
        
        /// <summary>
        /// Gets all selected object identifiers
        /// </summary>
        /// <returns>List of selected object IDs</returns>
        List<string> GetSelectedObjects();
        
        /// <summary>
        /// Activates the selection tool
        /// </summary>
        void Activate();
        
        /// <summary>
        /// Deactivates the selection tool
        /// </summary>
        void Deactivate();
        
        /// <summary>
        /// Resets selection tool settings to defaults
        /// </summary>
        void ResetToDefaults();
    }
}
