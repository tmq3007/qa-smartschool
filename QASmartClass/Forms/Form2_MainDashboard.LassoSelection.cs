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

            var lassoPoints = _lassoTool?.GetLassoPoints();
            if ((selectedElements != null && selectedElements.Count > 0) || (lassoPoints != null && lassoPoints.Count >= 3))
            {
                // 2. Gọi SelectionManager để xử lý chọn đối tượng (bao gồm cả UIElement và Native Stroke)
                _selectionManager?.SelectFromLasso(selectedElements ?? new List<UIElement>(), lassoPoints);

                System.Diagnostics.Debug.WriteLine($"✅ Lasso selection completed");

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
                        var visualBounds = GetSelectionVisualBounds(groupObject);
                        var toolbarPos = CalculateOptimalToolbarPosition(visualBounds);
                        _contextToolbar?.ShowAt(toolbarPos, groupObject);
                    }

                    // ✅ DUY TRÌ LIÊN TỤC CHẾ ĐỘ CHỌN TỰ DO (Lasso Mode)
                    // Giữ nguyên _isLassoMode = true để giáo viên có thể khoanh chọn nhiều lần liên tiếp.
                    // Thao tác kéo di chuyển / co giãn / xoay đối tượng đã chọn đã được nhường quyền qua CanStartSelectionPredicate
                    _isLassoMode = true;
                    _objectSelectionMode = true;
                    MainInteractiveBoard.Cursor = Cursors.Cross;
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

        #region Lasso Tool Hooks Configuration

        /// <summary>
        /// Cấu hình các hook kiểm tra cho Lasso Selection Tool
        /// Ngăn vẽ đè nét Lasso khi người dùng tương tác kéo di chuyển/co giãn/xoay SelectionBox, Toolbar hoặc đối tượng đã chọn
        /// </summary>
        public void ConfigureLassoToolHooks()
        {
            if (_lassoTool == null) return;

            _lassoTool.CanStartSelectionPredicate = (clickPoint, originalSource) =>
            {
                // 1. Nếu click vào button, thanh công cụ dưới đáy, menu context
                if (originalSource is DependencyObject depObj)
                {
                    if (QASmartTouch.Utilities.InputValidationHelper.IsEventFromButton(depObj, MainInteractiveBoard) ||
                        QASmartTouch.Utilities.InputValidationHelper.IsEventFromInteractiveControl(depObj, MainInteractiveBoard))
                    {
                        return false;
                    }
                }

                // 2. Nếu click vào SelectionBox hoặc Adorner của nó (để kéo di chuyển, co giãn, xoay)
                if (_selectionBox != null && _selectionBox.Visibility == Visibility.Visible)
                {
                    if (originalSource is DependencyObject hitDep && _selectionBox.IsAncestorOf(hitDep))
                    {
                        return false;
                    }

                    if (_selectionBox.AttachedObject != null && _selectionBox.AttachedObject.Bounds.Contains(clickPoint))
                    {
                        return false;
                    }
                }

                // 3. Nếu click vào Context Toolbar
                if (_contextToolbar != null && _contextToolbar.Visibility == Visibility.Visible)
                {
                    if (originalSource is DependencyObject hitDep && _contextToolbar.IsAncestorOf(hitDep))
                    {
                        return false;
                    }
                }

                // 4. Nếu click vào đối tượng ĐANG ĐƯỢC CHỌN (để kéo di chuyển)
                var hitObject = _selectionManager?.HitTest(clickPoint);
                var currentSelection = _selectionManager?.GetSelectedObjects();
                if (hitObject != null && currentSelection != null && currentSelection.Contains(hitObject))
                {
                    return false;
                }

                return true;
            };

            _lassoTool.BeforeSelectionStarted = () =>
            {
                // Khi người dùng bấm ra vùng trống để khoanh vùng mới:
                // Tự động deselect vùng chọn cũ và ẩn SelectionBox
                _selectionManager?.DeselectAll();
                _selectionBox?.Detach();
                _contextToolbar?.Hide();
            };
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
