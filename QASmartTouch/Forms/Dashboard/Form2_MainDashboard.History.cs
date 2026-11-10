using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Helpers;
using QASmartTouch.Managers;
using QASmartTouch.Models;
using QASmartTouch.Services;
using QASmartTouch.WhiteboardCore.History;

// ══════════════════════════════════════════════════════════════════
// Form2_MainDashboard.History.cs — Partial Class
// Quản lý toàn bộ hệ thống Hoàn tác / Làm lại (Undo / Redo)
// cho cả phần tử UIElement (Canvas) và nét vẽ Native Inking (Stroke).
// ══════════════════════════════════════════════════════════════════

namespace QASmartTouch.Forms
{
    public partial class Form2_MainDashboard
    {
        #region Undo/Redo State & Configuration

        private Stack<UndoRedoAction> _undoStack = new();
        private Stack<UndoRedoAction> _redoStack = new();
        private int MAX_UNDO_LEVELS => GetMaxUndoSteps();

        private int GetMaxUndoSteps()
        {
            try
            {
                string settingsFile = System.IO.Path.Combine(AppPaths.SettingsDir, "board_settings.json");
                if (System.IO.File.Exists(settingsFile))
                {
                    string json = System.IO.File.ReadAllText(settingsFile);
                    var match = System.Text.RegularExpressions.Regex.Match(json, @"\""MaxUndoSteps\""\s*:\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int value))
                    {
                        return Math.Clamp(value, 10, 500);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to read MaxUndoSteps setting, using default 100: " + ex.Message);
            }
            return 100; // Default limit 100
        }

        #endregion

        #region Undo/Redo Toolbar Handlers & Core Engine

        private void btn3_Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                var action = _undoStack.Pop();
                ExecuteUndoAction(action);
                _redoStack.Push(action);
                MarkAsDirty();
                UpdateButtonStates();
                _selectionManager?.DeselectAll();
                RefreshSelectableObjects();
            }
        }

        private void btn4_Redo_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count > 0)
            {
                var action = _redoStack.Pop();
                ExecuteRedoAction(action);
                _undoStack.Push(action);
                MarkAsDirty();
                UpdateButtonStates();
                _selectionManager?.DeselectAll();
                RefreshSelectableObjects();
            }
        }

        /// <summary>
        /// Core method to record an action into undo stack
        /// </summary>
        private void RecordAction(UndoRedoAction action)
        {
            _undoStack.Push(action);
            _redoStack.Clear(); // Clear redo stack when new action is performed
            MarkAsDirty();

            // Limit stack size to prevent memory issues
            if (_undoStack.Count > MAX_UNDO_LEVELS)
            {
                var temp = _undoStack.ToList();
                temp.RemoveAt(temp.Count - 1); // Remove oldest action (at the bottom/end of the list)
                _undoStack = new Stack<UndoRedoAction>(
                    ((IEnumerable<UndoRedoAction>)temp).Reverse()
                );
            }

            UpdateButtonStates();
        }

        /// <summary>
        /// Convenience method to record Add action (drawing, shape creation)
        /// </summary>
        private void RecordAddAction(UIElement element, string? description = null)
        {
            if (element == null) return;

            // Accidental touch filtering for drawn strokes (Polyline / Path)
            // LƯU Ý: Tuyệt đối không lọc dấu chấm chủ ý (Dot) của giáo viên/học sinh!
            if (element is Polyline polyline)
            {
                if (polyline.Points != null && polyline.Points.Count > 0)
                {
                    bool isDot = polyline.Points.Count == 2 && Math.Abs(polyline.Points[1].X - polyline.Points[0].X - 0.01) < 0.005 && polyline.Points[1].Y == polyline.Points[0].Y;
                    if (!isDot && description != null && description.Contains("Accidental", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine("Ignoring accidental touch Polyline in Undo/Redo registration");
                        return;
                    }
                }
            }
            else if (element is Path path)
            {
                bool isDot = path.Tag is PointCollection pts && pts.Count == 2 && Math.Abs(pts[1].X - pts[0].X - 0.01) < 0.005;
                if (!isDot && path.Data != null)
                {
                    var bounds = path.Data.Bounds;
                    if (bounds.Width <= 5.0 && bounds.Height <= 5.0 &&
                        description != null && description.Contains("Accidental", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine("Ignoring accidental touch Path in Undo/Redo registration");
                        return;
                    }
                }
            }

            // ✅ QC_4.2_TABLE_OWNERSHIP: Tự động đính kèm nét vẽ nếu nằm trọn trong ô Bảng dữ liệu
            CheckAndAttachStrokeToTable(element);

            var action = new UndoRedoAction
            {
                Type = ActionType.Add,
                Element = element,
                Parent = MainInteractiveBoard,
                Description = description ?? $"Add {element.GetType().Name}"
            };
            RecordAction(action);
        }

        private void RecordAddStrokeAction(Stroke stroke, string? description = null, UIElement? targetParent = null)
        {
            if (stroke == null) return;
            var action = new UndoRedoAction
            {
                Type = ActionType.Add,
                Stroke = stroke,
                Parent = targetParent ?? MainInkPresenter ?? (UIElement)MainInteractiveBoard,
                Description = description ?? "Draw ink stroke"
            };
            RecordAction(action);
        }

        private void RecordRemoveStrokeAction(Stroke stroke, string? description = null)
        {
            if (stroke == null) return;
            var action = new UndoRedoAction
            {
                Type = ActionType.Remove,
                Stroke = stroke,
                Parent = MainInteractiveBoard,
                Description = description ?? "Erase ink stroke"
            };
            RecordAction(action);
        }

        private void RecordRemoveStrokesAction(IEnumerable<Stroke> strokes, string? description = null)
        {
            if (strokes == null) return;
            var list = strokes.ToList();
            if (list.Count == 0) return;
            var action = new UndoRedoAction
            {
                Type = ActionType.Remove,
                Strokes = list,
                Parent = MainInteractiveBoard,
                Description = description ?? $"Erase {list.Count} ink strokes"
            };
            RecordAction(action);
        }

        /// <summary>
        /// Ghi nhận kết quả của một phiên tẩy Native Inking (cắt xén nét hoặc xóa nét) vào Undo stack.
        /// </summary>
        private void FinalizeNativeEraseSession(QASmartTouch.WhiteboardCore.Tools.NativeEraseSession? session, string? description = null)
        {
            if (session == null || !session.HasChanges || MainInkPresenter == null) return;

            var removedStrokes = session.GetFinalRemovedStrokes();
            var addedStrokes = session.GetFinalAddedStrokes();

            if (removedStrokes.Count == 0 && addedStrokes.Count == 0) return;

            var batchAction = new UndoRedoAction
            {
                Type = ActionType.Batch,
                Description = description ?? "Erase native ink strokes",
                Parent = MainInteractiveBoard
            };

            if (removedStrokes.Count > 0)
            {
                batchAction.BatchActions.Add(new UndoRedoAction
                {
                    Type = ActionType.Remove,
                    Strokes = removedStrokes,
                    Parent = MainInteractiveBoard,
                    Description = $"Erase {removedStrokes.Count} native stroke(s)"
                });
            }

            if (addedStrokes.Count > 0)
            {
                batchAction.BatchActions.Add(new UndoRedoAction
                {
                    Type = ActionType.Add,
                    Strokes = addedStrokes,
                    Parent = MainInteractiveBoard,
                    Description = $"Add {addedStrokes.Count} sliced sub-stroke(s)"
                });
            }

            RecordAction(batchAction);
            System.Diagnostics.Debug.WriteLine($"🧹 Finalized Native Erase Session: {removedStrokes.Count} removed, {addedStrokes.Count} added");
        }

        /// <summary>
        /// Public method for external tool windows (Ruler, Protractor, SetSquare, Compass)
        /// to register drawn elements with the undo/redo system
        /// </summary>
        public void RecordToolDrawAction(UIElement element, string description)
        {
            RecordAddAction(element, description);

            // ✅ FIX: Cập nhật SelectionManager ngay khi tool window thêm nét vẽ mới,
            // đảm bảo nét mới có thể chọn được ngay mà không cần chuyển mode.
            RefreshSelectableObjects();
        }

        /// <summary>
        /// Public method for chart editors to register added charts with the undo/redo system
        /// </summary>
        public void RecordChartAdd(UIElement chartContainer, string chartType)
        {
            if (chartContainer == null) return;

            // ✅ Đảm bảo biểu đồ luôn có Z-Index cao hơn nét chữ cũ (thuộc tầng UserContent)
            if (Panel.GetZIndex(chartContainer) <= 0)
            {
                int z = AllocateImageZIndex();
                Panel.SetZIndex(chartContainer, z);
            }

            RecordAddAction(chartContainer, $"Insert {chartType}");
            RefreshSelectableObjects();
        }

        /// <summary>
        /// Public method for chart editors to register removed charts with the undo/redo system
        /// </summary>
        public void RecordChartRemove(UIElement chartContainer, string chartType)
        {
            if (chartContainer == null) return;
            RecordRemoveAction(chartContainer, $"Remove {chartType}");
            RefreshSelectableObjects();
        }

        /// <summary>
        /// Convenience method to record Remove action (erasing, delete)
        /// </summary>
        private void RecordRemoveAction(UIElement element, string? description = null)
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Remove,
                Element = element,
                Parent = MainInteractiveBoard,
                Description = description ?? $"Remove {element.GetType().Name}"
            };
            RecordAction(action);
        }

        /// <summary>
        /// Convenience method to record Remove action for multiple elements in a single Undo/Redo step (Batch)
        /// </summary>
        private void RecordRemoveBatchAction(IEnumerable<UIElement> elements, string? description = null)
        {
            var elementList = elements?.Where(e => e != null).ToList();
            if (elementList == null || elementList.Count == 0) return;

            if (elementList.Count == 1)
            {
                RecordRemoveAction(elementList[0], description);
                return;
            }

            var batchAction = new UndoRedoAction
            {
                Type = ActionType.Batch,
                Description = description ?? $"Delete {elementList.Count} elements"
            };

            foreach (var elem in elementList)
            {
                batchAction.BatchActions.Add(new UndoRedoAction
                {
                    Type = ActionType.Remove,
                    Element = elem,
                    Parent = MainInteractiveBoard,
                    Description = description ?? $"Remove {elem.GetType().Name}"
                });
            }

            RecordAction(batchAction);
        }

        /// <summary>
        /// Convenience method to record Modify action (move, rotate, resize)
        /// </summary>
        private void RecordModifyAction(UIElement element, object oldValue, object newValue, string? description = null)
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Modify,
                Element = element,
                Parent = MainInteractiveBoard,
                OldValue = oldValue,
                NewValue = newValue,
                Description = description ?? $"Modify {element.GetType().Name}"
            };
            RecordAction(action);
        }

        /// <summary>
        /// Overload for property-specific modifications
        /// </summary>
        private void RecordModifyAction(UIElement element, string propertyName, object oldValue, object newValue, string? description = null)
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Modify,
                Element = element,
                Parent = MainInteractiveBoard,
                OldValue = new { Property = propertyName, Value = oldValue },
                NewValue = new { Property = propertyName, Value = newValue },
                Description = description ?? $"Modify {propertyName}"
            };
            RecordAction(action);
        }

        /// <summary>
        /// Ghi nhận hành động chỉnh sửa thuộc tính cho Native Inking Stroke (Màu sắc hoặc Độ dày)
        /// </summary>
        private void RecordModifyStrokeAction(Stroke stroke, object oldValue, object newValue, string? description = null)
        {
            var action = new UndoRedoAction
            {
                Type = ActionType.Modify,
                Stroke = stroke,
                Parent = MainInkPresenter,
                OldValue = oldValue,
                NewValue = newValue,
                Description = description ?? "Modify Stroke"
            };
            RecordAction(action);
        }

        /// <summary>
        /// Update button states based on stack counts
        /// </summary>
        private void UpdateButtonStates()
        {
            // Update enabled state
            bool canUndo = _undoStack.Count > 0;
            bool canRedo = _redoStack.Count > 0;

            btn3_Undo.IsEnabled = canUndo;
            btn4_Redo.IsEnabled = canRedo;

            // Update visual appearance with opacity for better feedback
            btn3_Undo.Opacity = canUndo ? 1.0 : 0.5;
            btn4_Redo.Opacity = canRedo ? 1.0 : 0.5;

            // Update tooltips with action counts and descriptions
            if (canUndo)
            {
                var lastAction = _undoStack.Peek();
                btn3_Undo.ToolTip = $"Undo: {lastAction.Description} ({_undoStack.Count} actions)";
            }
            else
            {
                btn3_Undo.ToolTip = "Undo (no actions available)";
            }

            if (canRedo)
            {
                var nextAction = _redoStack.Peek();
                btn4_Redo.ToolTip = $"Redo: {nextAction.Description} ({_redoStack.Count} actions)";
            }
            else
            {
                btn4_Redo.ToolTip = "Redo (no actions available)";
            }
        }

        /// <summary>
        /// Ghi nhận hành động biến đổi (Move, Resize, Rotate) của một hoặc nhiều đối tượng vào Undo stack
        /// </summary>
        public void RecordTransformAction(Dictionary<SelectableObject, ElementTransformState> initialStates,
                                          Dictionary<SelectableObject, ElementTransformState> finalStates,
                                          string description = "Transform object")
        {
            if (initialStates == null || finalStates == null || initialStates.Count == 0) return;

            var changedPairs = new List<(SelectableObject Obj, ElementTransformState OldState, ElementTransformState NewState)>();
            foreach (var kvp in initialStates)
            {
                var obj = kvp.Key;
                var oldState = kvp.Value;
                if (finalStates.TryGetValue(obj, out var newState))
                {
                    if (oldState.IsDifferentFrom(newState))
                    {
                        changedPairs.Add((obj, oldState, newState));
                    }
                }
            }

            if (changedPairs.Count == 0) return;

            if (changedPairs.Count == 1)
            {
                var (obj, oldState, newState) = changedPairs[0];
                var action = new UndoRedoAction
                {
                    Type = ActionType.Modify,
                    Element = obj.Element,
                    Stroke = obj.Stroke,
                    Parent = obj.Stroke != null ? (UIElement)MainInkPresenter : MainInteractiveBoard,
                    OldValue = oldState,
                    NewValue = newState,
                    Description = description
                };
                RecordAction(action);
            }
            else
            {
                var batchAction = new UndoRedoAction
                {
                    Type = ActionType.Batch,
                    Description = description
                };
                foreach (var (obj, oldState, newState) in changedPairs)
                {
                    batchAction.BatchActions.Add(new UndoRedoAction
                    {
                        Type = ActionType.Modify,
                        Element = obj.Element,
                        Stroke = obj.Stroke,
                        Parent = obj.Stroke != null ? (UIElement)MainInkPresenter : MainInteractiveBoard,
                        OldValue = oldState,
                        NewValue = newState,
                        Description = description
                    });
                }
                RecordAction(batchAction);
            }
        }

        /// <summary>
        /// Execute an undo action
        /// </summary>
        private void ExecuteUndoAction(UndoRedoAction action)
        {
            switch (action.Type)
            {
                case ActionType.Add:
                    // Undo Add = Remove from canvas and selection manager
                    if (action.Element != null)
                    {
                        _selectionManager?.RemoveObjectByElement(action.Element);
                        if (MainInteractiveBoard.Children.Contains(action.Element))
                        {
                            MainInteractiveBoard.Children.Remove(action.Element);
                        }
                    }
                    var undoTargetIP = action.Parent as InkPresenter ?? MainInkPresenter;
                    if (action.Stroke != null && undoTargetIP?.Strokes != null)
                    {
                        undoTargetIP.Strokes.Remove(action.Stroke);
                        _selectionManager?.RemoveObjectByStroke(action.Stroke);
                    }
                    if (action.Strokes != null && undoTargetIP?.Strokes != null)
                    {
                        foreach (var s in action.Strokes)
                        {
                            undoTargetIP.Strokes.Remove(s);
                            _selectionManager?.RemoveObjectByStroke(s);
                        }
                    }
                    break;

                case ActionType.Remove:
                    // Undo Remove = Add back to canvas and register selection
                    if (action.Element != null)
                    {
                        if (!MainInteractiveBoard.Children.Contains(action.Element))
                        {
                            MainInteractiveBoard.Children.Add(action.Element);
                        }
                        RegisterNewObjectWithSelectionManager(action.Element);
                    }
                    var restoreTargetIP = action.Parent as InkPresenter ?? MainInkPresenter;
                    if (action.Stroke != null && restoreTargetIP?.Strokes != null)
                    {
                        if (!restoreTargetIP.Strokes.Contains(action.Stroke))
                            restoreTargetIP.Strokes.Add(action.Stroke);
                        RegisterNewStrokeWithSelectionManager(action.Stroke);
                    }
                    if (action.Strokes != null && restoreTargetIP?.Strokes != null)
                    {
                        foreach (var s in action.Strokes)
                        {
                            if (!restoreTargetIP.Strokes.Contains(s))
                                restoreTargetIP.Strokes.Add(s);
                            RegisterNewStrokeWithSelectionManager(s);
                        }
                    }
                    break;

                case ActionType.Modify:
                    // Undo Modify = Restore old value
                    ApplyModifyValue(action.Element, action.OldValue, true, action.Stroke);
                    break;

                case ActionType.Batch:
                    // Undo batch operations in reverse order
                    for (int i = action.BatchActions.Count - 1; i >= 0; i--)
                    {
                        ExecuteUndoAction(action.BatchActions[i]);
                    }
                    break;

                case ActionType.ClearAll:
                    // Undo ClearAll = Add all elements back and register selection
                    foreach (var batchAction in action.BatchActions)
                    {
                        if (batchAction.Element != null)
                        {
                            if (!MainInteractiveBoard.Children.Contains(batchAction.Element))
                            {
                                MainInteractiveBoard.Children.Add(batchAction.Element);
                            }
                            RegisterNewObjectWithSelectionManager(batchAction.Element);
                        }
                        var batchTargetIP = batchAction.Parent as InkPresenter ?? MainInkPresenter;
                        if (batchAction.Stroke != null && batchTargetIP?.Strokes != null)
                        {
                            if (!batchTargetIP.Strokes.Contains(batchAction.Stroke))
                                batchTargetIP.Strokes.Add(batchAction.Stroke);
                            RegisterNewStrokeWithSelectionManager(batchAction.Stroke);
                        }
                        if (batchAction.Strokes != null && batchTargetIP?.Strokes != null)
                        {
                            foreach (var s in batchAction.Strokes)
                            {
                                if (!batchTargetIP.Strokes.Contains(s))
                                    batchTargetIP.Strokes.Add(s);
                                RegisterNewStrokeWithSelectionManager(s);
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Execute a redo action
        /// </summary>
        private void ExecuteRedoAction(UndoRedoAction action)
        {
            switch (action.Type)
            {
                case ActionType.Add:
                    // Redo Add = Add back to canvas and register selection
                    if (action.Element != null)
                    {
                        if (!MainInteractiveBoard.Children.Contains(action.Element))
                        {
                            MainInteractiveBoard.Children.Add(action.Element);
                        }
                        RegisterNewObjectWithSelectionManager(action.Element);
                    }
                    var redoAddTargetIP = action.Parent as InkPresenter ?? MainInkPresenter;
                    if (action.Stroke != null && redoAddTargetIP?.Strokes != null)
                    {
                        if (!redoAddTargetIP.Strokes.Contains(action.Stroke))
                            redoAddTargetIP.Strokes.Add(action.Stroke);
                        RegisterNewStrokeWithSelectionManager(action.Stroke);
                    }
                    if (action.Strokes != null && redoAddTargetIP?.Strokes != null)
                    {
                        foreach (var s in action.Strokes)
                        {
                            if (!redoAddTargetIP.Strokes.Contains(s))
                                redoAddTargetIP.Strokes.Add(s);
                            RegisterNewStrokeWithSelectionManager(s);
                        }
                    }
                    break;

                case ActionType.Remove:
                    // Redo Remove = Remove from canvas and selection manager
                    if (action.Element != null)
                    {
                        _selectionManager?.RemoveObjectByElement(action.Element);
                        if (MainInteractiveBoard.Children.Contains(action.Element))
                        {
                            MainInteractiveBoard.Children.Remove(action.Element);
                        }
                    }
                    var redoRemoveTargetIP = action.Parent as InkPresenter ?? MainInkPresenter;
                    if (action.Stroke != null && redoRemoveTargetIP?.Strokes != null)
                    {
                        redoRemoveTargetIP.Strokes.Remove(action.Stroke);
                        _selectionManager?.RemoveObjectByStroke(action.Stroke);
                    }
                    if (action.Strokes != null && redoRemoveTargetIP?.Strokes != null)
                    {
                        foreach (var s in action.Strokes)
                        {
                            redoRemoveTargetIP.Strokes.Remove(s);
                            _selectionManager?.RemoveObjectByStroke(s);
                        }
                    }
                    break;

                case ActionType.Modify:
                    // Redo Modify = Apply new value
                    ApplyModifyValue(action.Element, action.NewValue, false, action.Stroke);
                    break;

                case ActionType.Batch:
                    // Redo batch operations in normal order
                    foreach (var batchAction in action.BatchActions)
                    {
                        ExecuteRedoAction(batchAction);
                    }
                    break;

                case ActionType.ClearAll:
                    // Redo ClearAll = Remove all elements and selection
                    foreach (var batchAction in action.BatchActions)
                    {
                        if (batchAction.Element != null)
                        {
                            _selectionManager?.RemoveObjectByElement(batchAction.Element);
                            CleanupWebView2Media(batchAction.Element);
                            if (MainInteractiveBoard.Children.Contains(batchAction.Element))
                            {
                                MainInteractiveBoard.Children.Remove(batchAction.Element);
                            }
                        }
                        if (batchAction.Stroke != null && MainInkPresenter?.Strokes != null)
                        {
                            MainInkPresenter.Strokes.Remove(batchAction.Stroke);
                        }
                        if (batchAction.Strokes != null && MainInkPresenter?.Strokes != null)
                        {
                            foreach (var s in batchAction.Strokes)
                            {
                                MainInkPresenter.Strokes.Remove(s);
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Áp dụng giá trị sửa đổi (snapshot hình học hoặc thuộc tính) cho UIElement, Stroke và SelectableObject
        /// </summary>
        private void ApplyModifyValue(UIElement? element, object? value, bool isUndo, Stroke? stroke = null)
        {
            if (value == null) return;

            // 0. Sửa đổi thuộc tính cho Native Inking Stroke (Màu sắc, Độ dày hoặc Biến đổi hình học)
            if (stroke != null)
            {
                var strokeObj = _selectionManager?.GetSelectableObjectForStroke(stroke);
                if (value is Color c)
                {
                    stroke.DrawingAttributes.Color = c;
                    if (strokeObj != null) strokeObj.StrokeColor = c;
                    _contextToolbar?.UpdateColorPreview(c);
                    System.Diagnostics.Debug.WriteLine($"   ✓ Restored Stroke color: {c}");
                }
                else if (value is double th)
                {
                    stroke.DrawingAttributes.Width = th;
                    stroke.DrawingAttributes.Height = th;
                    if (strokeObj != null)
                    {
                        strokeObj.StrokeThickness = th;
                        strokeObj.UpdateBounds();
                    }
                    _selectionBox?.UpdatePosition();
                    System.Diagnostics.Debug.WriteLine($"   ✓ Restored Stroke thickness: {th:F0}px");
                }
                else if (value is ElementTransformState strokeTransformState)
                {
                    strokeTransformState.ApplyTo(null, strokeObj);
                    _selectionBox?.UpdatePosition();
                    _selectionManager?.RebuildQuadTree();
                    System.Diagnostics.Debug.WriteLine($"   ✓ Restored Stroke transform via Undo/Redo");
                }
                return;
            }

            var obj = element != null ? _selectionManager?.GetSelectableObjectForElement(element) : null;

            // 1. Biến đổi hình học (ElementTransformState)
            if (value is ElementTransformState transformState)
            {
                transformState.ApplyTo(element, obj);
                return;
            }

            // 2. Thuộc tính cụ thể từ anonymous object { Property = "...", Value = ... }
            var valType = value.GetType();
            var propInfo = valType.GetProperty("Property");
            var valueInfo = valType.GetProperty("Value");
            if (propInfo != null && valueInfo != null)
            {
                string? propName = propInfo.GetValue(value)?.ToString();
                object? propVal = valueInfo.GetValue(value);

                if (!string.IsNullOrEmpty(propName) && propVal != null)
                {
                    switch (propName)
                    {
                        case "IsLocked":
                            bool isLocked = Convert.ToBoolean(propVal);
                            if (obj != null) obj.IsLocked = isLocked;
                            _contextToolbar?.UpdateLockIcon(isLocked);
                            break;

                        case "ZIndex":
                            int zIndex = Convert.ToInt32(propVal);
                            if (element != null) Panel.SetZIndex(element, zIndex);
                            if (obj != null) obj.ZIndex = zIndex;
                            break;

                        case "RotationAngle":
                            double angle = Convert.ToDouble(propVal);
                            if (obj != null)
                            {
                                obj.RotationAngle = angle;
                                obj.ApplyTransform();
                                obj.UpdateBounds();
                            }
                            break;

                        case "StrokeStyle":
                            if (element is Shape shape)
                            {
                                shape.StrokeDashArray = propVal as DoubleCollection;
                            }
                            break;
                    }
                    return;
                }
            }

            // 3. Màu sắc (Color)
            if (value is Color color)
            {
                if (obj != null) obj.StrokeColor = color;
                if (element is Polyline polyline)
                {
                    polyline.Stroke = new SolidColorBrush(color);
                }
                else if (element is Shape shape)
                {
                    shape.Stroke = new SolidColorBrush(color);
                }
                else if (element is Border border)
                {
                    border.BorderBrush = new SolidColorBrush(color);
                }
                else if (element is Canvas shapeCanvas)
                {
                    var brush = new SolidColorBrush(color);
                    foreach (var childShape in shapeCanvas.Children.OfType<Shape>())
                    {
                        if (childShape.Stroke != null)
                            childShape.Stroke = brush;
                        if (childShape.Fill != null && childShape.Fill != Brushes.Transparent)
                            childShape.Fill = brush;
                    }
                    foreach (var subCanvas in shapeCanvas.Children.OfType<Canvas>())
                    {
                        foreach (var subShape in subCanvas.Children.OfType<Shape>())
                        {
                            if (subShape.Stroke != null) subShape.Stroke = brush;
                            if (subShape.Fill != null && subShape.Fill != Brushes.Transparent) subShape.Fill = brush;
                        }
                    }
                }
                _contextToolbar?.UpdateColorPreview(color);
                return;
            }

            // 4. Độ dày nét (double)
            if (value is double dVal)
            {
                if (obj != null) obj.StrokeThickness = dVal;
                if (element is Shape shape)
                {
                    shape.StrokeThickness = dVal;
                }
                else if (element is Border border)
                {
                    border.BorderThickness = new Thickness(dVal);
                }
                else if (element is Canvas shapeCanvas)
                {
                    foreach (var childShape in shapeCanvas.Children.OfType<Shape>())
                    {
                        if (childShape.Stroke != null) childShape.StrokeThickness = dVal;
                    }
                }
                return;
            }

            // 5. Nội dung văn bản (string)
            if (value is string textVal)
            {
                if (obj != null) obj.TextContent = textVal;
                if (element is TextBlock tb)
                {
                    tb.Text = textVal;
                }
                else if (element is TextBox tbx)
                {
                    tbx.Text = textVal;
                }
                else if (element is Border b)
                {
                    var ctb = FindVisualChild<TextBlock>(b);
                    if (ctb != null) ctb.Text = textVal;
                }
                return;
            }
        }

        #endregion
    }
}
