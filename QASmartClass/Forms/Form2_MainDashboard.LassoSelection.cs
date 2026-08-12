using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartTouch.Models;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Form2_MainDashboard - Lasso Selection Tool Integration
    /// Partial class chứa logic cho chức năng chọn vùng tự do (Lasso Selection)
    /// </summary>
    public partial class Form2_MainDashboard : Window
    {
        #region Lasso Selection - Event Handlers

        /// <summary>
        /// Event handler khi lasso selection hoàn thành
        /// </summary>
        private void LassoTool_SelectionCompleted(object? sender, List<UIElement> selectedElements)
        {
            // 1. Làm tươi danh sách đối tượng trên Canvas trước khi chọn
            RefreshSelectableObjects();

            if (selectedElements.Count > 0)
            {
                // 2. Gọi SelectionManager để xử lý chọn đối tượng
                _selectionManager?.SelectFromLasso(selectedElements);

                System.Diagnostics.Debug.WriteLine($"✅ Lasso selection completed: {selectedElements.Count} object(s) selected");

                var selectedObjects = _selectionManager?.GetSelectedObjects();
                if (selectedObjects != null && selectedObjects.Count > 0)
                {
                    if (selectedObjects.Count == 1)
                    {
                        // ✅ FIX LỖI 1: Attach SelectionBox hiển thị khung chữ nhật Bounding Box xung quanh đối tượng duy nhất
                        _selectionBox?.AttachTo(selectedObjects[0]);
                        ShowContextToolbarForObject(selectedObjects[0]);
                    }
                    else
                    {
                        // ✅ FIX LỖI 1: Attach SelectionBox hiển thị khung chữ nhật Bounding Box nhóm xung quanh toàn bộ đối tượng
                        double gMinX = double.MaxValue, gMinY = double.MaxValue;
                        double gMaxX = double.MinValue, gMaxY = double.MinValue;
                        foreach (var obj in selectedObjects)
                        {
                            var b = obj.Bounds;
                            gMinX = Math.Min(gMinX, b.Left);  gMinY = Math.Min(gMinY, b.Top);
                            gMaxX = Math.Max(gMaxX, b.Right);  gMaxY = Math.Max(gMaxY, b.Bottom);
                        }
                        var groupRect = new Rect(gMinX, gMinY, gMaxX - gMinX, gMaxY - gMinY);
                        var toolbarPos = CalculateOptimalToolbarPosition(groupRect);

                        var groupObject = new SelectableObject
                        {
                            Element = null,
                            Type = ObjectType.Group,
                            IsGroup = true,
                            GroupMembers = selectedObjects.ToList(),
                            Position = new Point(gMinX, gMinY),
                            Size = new Size(groupRect.Width, groupRect.Height),
                            Bounds = groupRect
                        };
                        _selectionBox?.AttachTo(groupObject);
                        _contextToolbar?.ShowAt(toolbarPos, selectedObjects[0]);
                    }

                    // ✅ FIX DRAG & RESIZE: Sau khi Lasso chọn thành công đối tượng,
                    // chuyển sang Chế độ Chọn đối tượng (Object Selection Mode) để kích hoạt Drag-to-Move & Resize
                    _isLassoMode = false;
                    _lassoTool?.Deactivate();
                    _objectSelectionMode = true;
                    MainInteractiveBoard.Cursor = Cursors.Hand;
                }
                else
                {
                    // ✅ QA EDGE-CASE FIX: Lasso khoanh được elements nhưng tất cả bị reject (locked/background)
                    _selectionManager?.DeselectAll();
                    _selectionBox?.Detach();
                    _contextToolbar?.Hide();
                    System.Diagnostics.Debug.WriteLine("⚠️ Lasso: Elements found but none selectable — staying in Lasso mode");
                    MainInteractiveBoard.Cursor = Cursors.Cross;
                }
            }
            else
            {
                _selectionManager?.DeselectAll();
                _selectionBox?.Detach();
                _contextToolbar?.Hide();
                System.Diagnostics.Debug.WriteLine("⚠️ Lasso selection: No objects found in selection area");
                // Giữ Lasso mode nếu khoanh vào vùng trống
                MainInteractiveBoard.Cursor = Cursors.Cross;
            }
        }

        /// <summary>
        /// Hiển thị ContextToolbar cho object được chọn
        /// </summary>
        private void ShowContextToolbarForObject(SelectableObject obj)
        {
            if (_contextToolbar == null) return;

            try
            {
                // Tính toán vị trí hiển thị (dưới object, căn giữa)
                var bounds = obj.Bounds;
                var position = new Point(
                    bounds.Left + bounds.Width / 2,
                    bounds.Bottom + 10
                );

                _contextToolbar.ShowAt(position, obj);
                System.Diagnostics.Debug.WriteLine($"✅ ContextToolbar shown at ({position.X:F0}, {position.Y:F0})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error showing ContextToolbar: {ex.Message}");
            }
        }

        #endregion

        #region Lasso Selection - Toggle Mode

        /// <summary>
        /// Bật/tắt Lasso Selection Mode
        /// </summary>
        public void ToggleLassoMode()
        {
            _isLassoMode = !_isLassoMode;

            if (_isLassoMode)
            {
                // Kích hoạt lasso mode
                _lassoTool?.Activate();

                // TODO: Highlight button (nếu có nút UI)
                // btnLassoSelect.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));

                System.Diagnostics.Debug.WriteLine("✅ Lasso mode ACTIVATED - Draw a loop around objects to select them");
            }
            else
            {
                // Tắt lasso mode
                _lassoTool?.Deactivate();

                // TODO: Reset button
                // btnLassoSelect.Background = Brushes.Transparent;

                System.Diagnostics.Debug.WriteLine("❌ Lasso mode DEACTIVATED");
            }
        }

        #endregion

        #region Keyboard Shortcuts

        /// <summary>
        /// Xử lý phím tắt cho Lasso Selection
        /// Gọi method này từ Form2_MainDashboard_PreviewKeyDown
        /// </summary>
        public void HandleLassoKeyboardShortcuts(KeyEventArgs e)
        {
            // L: Toggle Lasso Mode (không có Ctrl)
            if (e.Key == Key.L && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                ToggleLassoMode();
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("⌨️ Keyboard shortcut: L (Toggle Lasso Mode)");
            }
            // Escape: Tắt Lasso Mode nếu đang bật
            else if (e.Key == Key.Escape && _isLassoMode)
            {
                ToggleLassoMode();
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("⌨️ Keyboard shortcut: Escape (Exit Lasso Mode)");
            }
        }

        #endregion
    }
}
