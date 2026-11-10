using System;
using System.Collections.Generic;
using System.Windows;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Quản lý trạng thái selection hiện tại
    /// </summary>
    public class SelectionState
    {
        #region Properties

        /// <summary>
        /// Danh sách các objects đang được chọn
        /// </summary>
        public List<SelectableObject> SelectedObjects { get; private set; }

        /// <summary>
        /// Object đang được chọn chính (khi multi-select, object được chọn cuối cùng)
        /// </summary>
        public SelectableObject? PrimarySelectedObject { get; set; }

        /// <summary>
        /// Có đang trong chế độ selection không
        /// </summary>
        public bool IsSelectionMode { get; set; }

        /// <summary>
        /// Có đang drag đối tượng không
        /// </summary>
        public bool IsDragging { get; set; }

        /// <summary>
        /// Có đang resize không
        /// </summary>
        public bool IsResizing { get; set; }

        /// <summary>
        /// Có đang rotate không
        /// </summary>
        public bool IsRotating { get; set; }

        /// <summary>
        /// Vị trí bắt đầu drag
        /// </summary>
        public Point DragStartPoint { get; set; }

        /// <summary>
        /// Resize mode hiện tại
        /// </summary>
        public ResizeMode CurrentResizeMode { get; set; }

        /// <summary>
        /// Có phải multi-selection không
        /// </summary>
        public bool IsMultiSelection => SelectedObjects.Count > 1;

        /// <summary>
        /// Có object nào đang được chọn không
        /// </summary>
        public bool HasSelection => SelectedObjects.Count > 0;

        #endregion

        #region Constructor

        public SelectionState()
        {
            SelectedObjects = new List<SelectableObject>();
            IsSelectionMode = false;
            IsDragging = false;
            IsResizing = false;
            IsRotating = false;
            CurrentResizeMode = ResizeMode.None;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clear tất cả selection
        /// </summary>
        public void ClearSelection()
        {
            foreach (var obj in SelectedObjects)
            {
                obj.IsSelected = false;
            }
            SelectedObjects.Clear();
            PrimarySelectedObject = null;
        }

        /// <summary>
        /// Thêm object vào selection
        /// </summary>
        public void AddToSelection(SelectableObject obj)
        {
            if (!SelectedObjects.Contains(obj))
            {
                obj.IsSelected = true;
                SelectedObjects.Add(obj);
                PrimarySelectedObject = obj;
            }
        }

        /// <summary>
        /// Remove object khỏi selection
        /// </summary>
        public void RemoveFromSelection(SelectableObject obj)
        {
            if (SelectedObjects.Contains(obj))
            {
                obj.IsSelected = false;
                SelectedObjects.Remove(obj);
                
                if (PrimarySelectedObject == obj)
                {
                    PrimarySelectedObject = SelectedObjects.Count > 0 ? SelectedObjects[^1] : null;
                }
            }
        }

        /// <summary>
        /// Toggle selection của object
        /// </summary>
        public void ToggleSelection(SelectableObject obj)
        {
            if (obj.IsSelected)
            {
                RemoveFromSelection(obj);
            }
            else
            {
                AddToSelection(obj);
            }
        }

        /// <summary>
        /// Select chỉ một object (clear các object khác)
        /// </summary>
        public void SelectSingle(SelectableObject obj)
        {
            ClearSelection();
            AddToSelection(obj);
        }

        /// <summary>
        /// Reset drag state
        /// </summary>
        public void ResetDragState()
        {
            IsDragging = false;
            IsResizing = false;
            IsRotating = false;
            CurrentResizeMode = ResizeMode.None;
        }

        #endregion
    }

    /// <summary>
    /// Enum cho resize mode
    /// </summary>
    public enum ResizeMode
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Top,
        Bottom,
        Left,
        Right,
        Rotate
    }
}
