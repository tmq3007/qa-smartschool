using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using QASmartTouch.Controls;
using QASmartTouch.Services;

// ══════════════════════════════════════════════════════════════════
// Form2_MainDashboard.RadialToolbar.cs — Partial Class
// QC_4.2 SMART TOUCH Radial Dial + Corner Dock (Phase 1)
//
// Strangler Fig: toolbar mới KHÔNG thay thế logic cũ. Mỗi múi/mục chỉ "bấm hộ" nút legacy
// tương ứng trong panelTools → toàn bộ hành vi (chọn công cụ, mở submenu, Undo/Redo,
// DESKTOP, SMART CLASS, Thoát...) giữ nguyên 100%.
// Feature flag: AppSettings.UseRadialToolbar (false = dùng lại thanh ngang cũ).
// ══════════════════════════════════════════════════════════════════

namespace QASmartTouch.Forms
{
    public partial class Form2_MainDashboard
    {
        private bool _radialToolbarInitialized;
        private Dictionary<string, Button>? _radialLegacyMap;
        private readonly List<(Button Button, EventHandler Handler)> _radialBackgroundListeners = new();

        private static readonly HashSet<string> RadialDialKeys = new() { "pen", "eraser", "select", "shapes" };
        private static readonly HashSet<string> RadialDockKeys = new() { "insert", "zoom", "board", "desktop", "smartclass", "more" };
        private static readonly Color RadialActiveColor = Color.FromRgb(0x2E, 0x86, 0xDE);

        /// <summary>
        /// Khởi tạo toolbar Radial/Dock (chạy 1 lần sau khi cửa sổ Loaded).
        /// </summary>
        private void InitializeRadialToolbar()
        {
            if (_radialToolbarInitialized) return;
            _radialToolbarInitialized = true;

            try
            {
                if (radialDial == null || borderLegacyToolbar == null) return;

                _radialLegacyMap = new Dictionary<string, Button>
                {
                    ["pen"] = btn1_Pen,
                    ["eraser"] = btn2_Eraser,
                    ["undo"] = btn3_Undo,
                    ["redo"] = btn4_Redo,
                    ["shapes"] = btn5_Shapes,
                    ["insert"] = btn6_Inserts,
                    ["zoom"] = btn7_Zoom,
                    ["select"] = btn8_Select,
                    ["board"] = btn9_BoardManagement,
                    ["desktop"] = btn10_WindowMode,
                    ["smartclass"] = btn11_SmartClass,
                    ["more"] = btn12_MoreExtended,
                    ["exit"] = btn13_Exit,
                };

                radialDial.ToolInvoked += (s, key) => HandleDialToolInvoked(key);
                radialDial.OrbitalAction += (s, e) => HandleDialOrbitalAction(e.Action, e.Value);
                radialDial.CustomizerRequested += (s, e) => OpenRadialCustomizer();

                if (cornerDock != null)
                {
                    cornerDock.ItemInvoked += (s, key) => InvokeLegacyToolbarButton(key);
                    cornerDock.ToolInvoked += (s, e) => HandleDockToolInvoked(e.Group, e.Tool);
                }

                if (smartDockPill != null)
                {
                    smartDockPill.ToolInvoked += (s, toolKey) => HandleSmartDockToolInvoked(toolKey);
                    smartDockPill.PrevBoardClicked += (s, e) => NavigateBoard(-1);
                    smartDockPill.NextBoardClicked += (s, e) => NavigateBoard(1);
                    smartDockPill.AddBoardClicked += (s, e) => CreateNewBoardFromDock();
                    smartDockPill.BoardListClicked += (s, e) => InvokeLegacyToolbarButton("board");
                    smartDockPill.ZoomOutClicked += (s, e) => ApplyZoom("Decrement", 0.25);
                    smartDockPill.ZoomInClicked += (s, e) => ApplyZoom("Increment", 0.25);
                    smartDockPill.ZoomResetClicked += (s, e) => ApplyZoom("Fixed", 1.0);
                    smartDockPill.ZoomFitClicked += (s, e) => ApplyZoom("Fixed", 1.0);
                    smartDockPill.TouchModeToggleClicked += (s, e) =>
                    {
                        btnHudTouchMode_Click(btnHudTouchMode, new RoutedEventArgs());
                        SyncSmartDockTouchMode();
                    };
                }

                if (radialCustomizerModal != null)
                {
                    radialCustomizerModal.ConfigurationSaved += (s, config) =>
                    {
                        radialDial?.RebuildWedges();
                        ShowSmartStatusBadge("Đã lưu cấu hình 6 múi tròn!");
                    };
                    radialCustomizerModal.Closed += (s, e) =>
                    {
                        radialCustomizerModal.Visibility = Visibility.Collapsed;
                    };
                }

                if (exitConfirmModal != null)
                {
                    exitConfirmModal.Cancelled += (s, e) =>
                    {
                        exitConfirmModal.Visibility = Visibility.Collapsed;
                    };
                    exitConfirmModal.ExitWithoutSaveRequested += (s, e) => PerformExitWithoutSave();
                    exitConfirmModal.ExitWithSaveRequested += (s, e) => PerformExitWithSave();
                }

                // Lắng nghe sự kiện chuyển bảng từ BoardManager để đồng bộ số trang
                if (_boardManager != null)
                {
                    _boardManager.BoardSwitched += (s, e) => SyncSmartDockBoardDisplay();
                    _boardManager.BoardCreated += (s, e) => SyncSmartDockBoardDisplay();
                    _boardManager.BoardDeleted += (s, e) => SyncSmartDockBoardDisplay();
                }

                // Đồng bộ trạng thái active: lắng nghe thay đổi Background của nút legacy
                // (SelectTool / CloseActiveSubMenu đổi màu nền #2E86DE ↔ #F1F2F6).
                var descriptor = DependencyPropertyDescriptor.FromProperty(Control.BackgroundProperty, typeof(Button));
                if (descriptor != null)
                {
                    foreach (var kv in _radialLegacyMap)
                    {
                        if (kv.Key == "exit" || kv.Key == "undo" || kv.Key == "redo") continue;
                        var btn = kv.Value;
                        if (btn == null) continue;
                        EventHandler handler = (s, e) => SyncRadialActiveState();
                        descriptor.AddValueChanged(btn, handler);
                        _radialBackgroundListeners.Add((btn, handler));
                    }
                }

                this.Closed += (s, e) => DisposeRadialToolbar();

                ApplyRadialToolbarMode(AppSettings.UseRadialToolbar);
                SyncRadialActiveState();
            }
            catch (Exception ex)
            {
                // An toàn tuyệt đối: lỗi toolbar mới → quay về thanh ngang cũ
                System.Diagnostics.Debug.WriteLine($"⚠️ InitializeRadialToolbar failed: {ex.Message}");
                try { ApplyRadialToolbarMode(false); } catch { }
            }
        }

        /// <summary>
        /// Bật/tắt toolbar Radial + Smart Dock Pill. Thanh ngang cũ chỉ bị ẩn (Hidden) chứ không Collapsed
        /// để template/PART_Border và toạ độ nút vẫn hợp lệ cho logic legacy.
        /// </summary>
        private void ApplyRadialToolbarMode(bool useRadial)
        {
            if (borderLegacyToolbar != null)
                borderLegacyToolbar.Visibility = useRadial ? Visibility.Hidden : Visibility.Visible;
            if (radialDial != null)
                radialDial.Visibility = useRadial ? Visibility.Visible : Visibility.Collapsed;
            if (smartDockPill != null)
            {
                smartDockPill.Visibility = useRadial ? Visibility.Visible : Visibility.Collapsed;
                if (useRadial)
                {
                    SyncSmartDockBoardDisplay();
                    SyncSmartDockZoomDisplay();
                    SyncSmartDockTouchMode();
                }
            }
            if (cornerDock != null)
            {
                cornerDock.Visibility = Visibility.Collapsed; // Ưu tiên SmartDockPill theo Stitch mới
            }
            if (hudZoomIndicator != null)
            {
                // Thu gọn HUD góc phải khi đã tích hợp vào Dock Pill
                hudZoomIndicator.Visibility = useRadial ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// "Bấm hộ" nút legacy → chạy đúng handler Click hiện có (sender = nút legacy).
        /// </summary>
        private void InvokeLegacyToolbarButton(string key)
        {
            if (_radialLegacyMap == null || !_radialLegacyMap.TryGetValue(key, out var legacy) || legacy == null) return;
            if (!legacy.IsEnabled) return;

            try
            {
                legacy.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, legacy));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Radial toolbar invoke '{key}' failed: {ex.Message}");
            }
        }

        private void SyncRadialActiveState()
        {
            if (_radialLegacyMap == null || radialDial == null || cornerDock == null) return;

            string? dialActive = null;
            string? dockActive = null;

            foreach (var kv in _radialLegacyMap)
            {
                if (kv.Value?.Background is SolidColorBrush b && b.Color == RadialActiveColor)
                {
                    if (dialActive == null && RadialDialKeys.Contains(kv.Key)) dialActive = kv.Key;
                    else if (dockActive == null && RadialDockKeys.Contains(kv.Key)) dockActive = kv.Key;
                }
            }

            radialDial.SetActive(dialActive);
            cornerDock.SetActive(dockActive);

            // Đồng bộ giá trị hiển thị trên cung Orbital
            try
            {
                radialDial.SetPenSettings(_savedBrushType, _savedPenSize, _savedPenColor);
                radialDial.SetEraserSettings(_savedEraserSize, _savedEraserMode);
                radialDial.SetActiveShape(_shapeDrawingEnabled ? _currentShape : null);
            }
            catch { }
        }

        #region Radial Dial → logic hiện có

        private string? _radialLastShape;

        /// <summary>
        /// Chạm múi trên Dial. Bút/Tẩy/Chọn vùng chỉ KÍCH HOẠT công cụ (giống lần bấm đầu của nút cũ);
        /// phần cài đặt nằm trên cung Orbital nên KHÔNG mở cửa sổ SubMenu cũ.
        /// </summary>
        private void HandleDialToolInvoked(string key)
        {
            try
            {
                switch (key)
                {
                    case "pen": ActivateDialPen(); break;
                    case "eraser": ActivateDialEraser(); break;
                    case "select":
                        if (!_objectSelectionMode) InvokeLegacyToolbarButton("select");
                        break;
                    case "shapes":
                        if (!string.IsNullOrEmpty(_radialLastShape)) ActivateDialShape(_radialLastShape!);
                        break;
                    case "highlighter":
                        _savedBrushType = "Marker";
                        ActivateDialPen();
                        break;
                    case "laser":
                        _savedBrushType = "Laser";
                        ActivateDialPen();
                        break;
                    case "ruler":
                        OpenRulerTool();
                        break;
                    case "compass":
                        OpenCompassTool();
                        break;
                    case "setsquare":
                        OpenSetSquareTool();
                        break;
                    case "stickynote":
                        EnableTextTool();
                        break;
                    case "spotlight":
                        OpenSpotlightTool();
                        break;
                    case "curtain":
                        btn9_5_ScreenCurtain_Click(btn9_5_ScreenCurtain, new RoutedEventArgs());
                        break;
                    case "timer":
                        OpenCountdownTimerFromToolbar();
                        break;
                    case "clearall":
                        ExecuteEraserClearAll();
                        break;
                    default:
                        InvokeLegacyToolbarButton(key); // undo / redo
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Radial dial '{key}' failed: {ex.Message}");
            }
        }

        private async void HandleDialOrbitalAction(string action, object? value)
        {
            try
            {
                switch (action)
                {
                    case "penSize" when value is int size:
                        _savedPenSize = size;
                        ActivateDialPen();
                        break;
                    case "penType" when value is string type:
                        _savedBrushType = type;
                        ActivateDialPen();
                        break;
                    case "penColor" when value is Color color:
                        _savedPenColor = color;
                        ActivateDialPen();
                        break;

                    case "eraserSize" when value is int esize:
                        _savedEraserSize = esize;
                        ActivateDialEraser();
                        break;
                    case "eraserMode" when value is string mode:
                        _savedEraserMode = mode;
                        ActivateDialEraser();
                        break;
                    case "eraserClearAll":
                        ExecuteEraserClearAll();
                        break;

                    case "selectMode" when value is string sel:
                        if (sel == "Lasso") ActivateLassoSelectionMode();
                        else if (sel == "Magic") ActivateMagicWandSelectionMode();
                        else ActivateRectangleSelectionMode();
                        break;
                    case "smartAction" when value is string sa:
                        if (Enum.TryParse<SmartActionType>(sa, out var smartType))
                            await ExecuteSmartActionAsync(smartType);
                        break;

                    case "shape" when value is string shape:
                        ActivateDialShape(shape);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Radial orbital '{action}' failed: {ex.Message}");
            }
            finally
            {
                SyncRadialActiveState();
            }
        }

        /// <summary>Kích hoạt Bút với cài đặt đã lưu — trùng logic nhánh "bấm lần đầu" của btn1_Pen_Click.</summary>
        private void ActivateDialPen()
        {
            if (_selectedTool == btn1_Pen && _drawingEnabled && _activeSubMenu == null)
            {
                EnableDrawingMode(_savedBrushType, _savedPenSize, _savedPenColor);
                return;
            }
            DeactivateAllTools();
            CloseAllSubmenus();
            SelectTool(btn1_Pen);
            HideWelcomeState();
            EnableDrawingMode(_savedBrushType, _savedPenSize, _savedPenColor);
        }

        /// <summary>Kích hoạt Tẩy với cài đặt đã lưu — trùng logic nhánh "bấm lần đầu" của btn2_Eraser_Click.</summary>
        private void ActivateDialEraser()
        {
            if (_savedEraserMode == "ClearAll") _savedEraserMode = "Stroke";
            if (_selectedTool == btn2_Eraser && _eraserEnabled && _activeSubMenu == null)
            {
                EnableEraserMode(_savedEraserSize, _savedEraserMode);
                return;
            }
            DeactivateAllTools();
            CloseAllSubmenus();
            SelectTool(btn2_Eraser);
            HideWelcomeState();
            EnableEraserMode(_savedEraserSize, _savedEraserMode);
        }

        /// <summary>Chọn hình — trùng logic btn5_Shapes_Click + Form2_5_SubMenuDrawShapes (EnableShapeDrawingMode).</summary>
        private void ActivateDialShape(string shape)
        {
            bool is3D = shape is "Cube" or "Sphere" or "Cylinder" or "Cone" or "Pyramid" or "Prism";
            if (is3D)
            {
                // Hình 3D mở trình dựng 3D riêng (giống SubMenu cũ)
                CloseAllSubmenus();
                HideWelcomeState();
                EnableShapeDrawingMode(shape);
                return;
            }

            _radialLastShape = shape;
            DeactivateAllTools();
            CloseAllSubmenus();
            SelectTool(btn5_Shapes);
            HideWelcomeState();
            EnableShapeDrawingMode(shape);
        }

        #endregion

        #region Corner Dock (Tầng 2) → logic hiện có

        /// <summary>
        /// Chạm công cụ ở Tầng 2 của Dock. Chạy ĐÚNG handler Click của SubMenu cũ (Form2_4 / Form2_8)
        /// thông qua một instance ẩn ngoài màn hình → hành vi giống 100% khi chọn trong menu cũ.
        /// </summary>
        private void HandleDockToolInvoked(string group, string tool)
        {
            try
            {
                if (group == "insert") RunDockInsertTool(tool);
                else if (group == "expand") RunDockExpandTool(tool);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Dock tool '{group}/{tool}' failed: {ex.Message}");
            }
        }

        private void RunDockInsertTool(string tool)
        {
            // Giống btn6_Inserts_Click: tắt công cụ đang dùng + đóng submenu khác trước khi chèn
            DeactivateAllTools();
            CloseAllSubmenus();
            HideWelcomeState();

            var menu = new Form2_4_SubMenuInsertContent(this);
            menu.Closed += (s, e) =>
            {
                try
                {
                    this.Activate();
                    MainInteractiveBoard.Focus();
                }
                catch { }

                // Giống btn6_Inserts_Click: chạy sau khi menu đóng hẳn (tránh treo focus trên IFP)
                Dispatcher.BeginInvoke(new Action(() => ApplyInsertMenuSelection(menu.SelectedInsertType)),
                    System.Windows.Threading.DispatcherPriority.Input);
            };

            RunHeadlessSubMenu(menu, () => menu.InvokeTool(tool));
        }

        private void RunDockExpandTool(string tool)
        {
            CloseAllSubmenus();

            var menu = new Form2_8_SubMenuMoreExtended(this);
            RunHeadlessSubMenu(menu, () =>
            {
                if (!menu.InvokeTool(tool) && tool == "camera-ai")
                {
                    MessageBox.Show(this, "Tính năng Camera AI chưa được kích hoạt trong phiên bản hiện tại.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return true;
            });
        }

        /// <summary>
        /// Hiển thị SubMenu ngoài màn hình (không chiếm focus, không hiện taskbar), chạy hành động,
        /// rồi đảm bảo SubMenu được đóng (kể cả khi handler gọi lại Show() ở nhánh huỷ).
        /// </summary>
        private void RunHeadlessSubMenu(Window menu, Func<bool> action)
        {
            bool closed = false;
            menu.Closed += (s, e) => closed = true;

            menu.Owner = this;
            menu.ShowActivated = false;
            menu.ShowInTaskbar = false;
            menu.WindowStartupLocation = WindowStartupLocation.Manual;
            menu.Left = -20000;
            menu.Top = -20000;
            menu.Opacity = 0;

            try
            {
                menu.Show();
                action();
            }
            finally
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (closed) return;
                    try { menu.Close(); } catch { }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        #endregion

        #region Smart Dock Pill & Modals Handlers

        private void HandleSmartDockToolInvoked(string toolKey)
        {
            try
            {
                switch (toolKey)
                {
                    case "ruler": OpenRulerTool(); break;
                    case "compass": OpenCompassTool(); break;
                    case "setsquare": OpenSetSquareTool(); break;
                    case "timer": OpenCountdownTimerFromToolbar(); break;
                    case "spotlight": OpenSpotlightTool(); break;
                    case "curtain":
                        btn9_5_ScreenCurtain_Click(btn9_5_ScreenCurtain, new RoutedEventArgs());
                        break;
                    case "desktop":
                        InvokeLegacyToolbarButton("desktop");
                        break;
                    case "exit":
                        HandleExitPrompt();
                        break;
                    default:
                        RunDockInsertTool(toolKey);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ SmartDock tool '{toolKey}' failed: {ex.Message}");
            }
        }

        private void OpenRadialCustomizer()
        {
            if (radialCustomizerModal != null)
            {
                radialCustomizerModal.LoadCurrentConfiguration();
                radialCustomizerModal.Visibility = Visibility.Visible;
            }
        }

        private void OpenSpotlightTool()
        {
            try
            {
                var magnifierTool = new Form2_7_4_MagnifierTool(false);
                Shared.WindowHelper.ShowChildWindow(magnifierTool, this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ OpenSpotlightTool failed: {ex.Message}");
            }
        }

        private void OpenCountdownTimerFromToolbar()
        {
            try
            {
                var countdownTimer = new Form2_19_CountdownTimer();
                Shared.WindowHelper.ShowChildWindow(countdownTimer, this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ OpenCountdownTimer failed: {ex.Message}");
            }
        }

        private void HandleExitPrompt()
        {
            if (CheckHasUnsavedChanges())
            {
                if (exitConfirmModal != null)
                {
                    exitConfirmModal.Visibility = Visibility.Visible;
                    return;
                }
            }
            btn13_Exit_Click(btn13_Exit, new RoutedEventArgs());
        }

        private void PerformExitWithoutSave()
        {
            CloseAllToolWindows();
            var loginForm = new Form1_MainLogin();
            loginForm.Show();
            this.Close();
        }

        private void PerformExitWithSave()
        {
            bool saved = SaveCurrentLecture(showOpenFolderPrompt: false);
            if (saved)
            {
                CloseAllToolWindows();
                var loginForm = new Form1_MainLogin();
                loginForm.Show();
                this.Close();
            }
        }

        private void NavigateBoard(int direction)
        {
            if (_boardManager == null) return;
            int target = _boardManager.CurrentBoardIndex + direction;
            if (target >= 0 && target < _boardManager.BoardCount)
            {
                _boardManager.SwitchBoard(target);
                SyncSmartDockBoardDisplay();
            }
        }

        private void CreateNewBoardFromDock()
        {
            if (_boardManager == null) return;
            _boardManager.CreateBoard($"Trang {_boardManager.BoardCount + 1}");
            SyncSmartDockBoardDisplay();
            ShowSmartStatusBadge($"Đã thêm Trang {_boardManager.BoardCount}");
        }

        public void SyncSmartDockBoardDisplay()
        {
            if (smartDockPill == null || _boardManager == null) return;
            int current = _boardManager.CurrentBoardIndex + 1;
            int total = _boardManager.BoardCount;
            smartDockPill.UpdateBoardDisplay(current, total);
        }

        public void SyncSmartDockZoomDisplay()
        {
            smartDockPill?.UpdateZoomDisplay(_currentZoomLevel);
        }

        public void SyncSmartDockTouchMode()
        {
            if (smartDockPill == null || _touchHandler == null) return;
            bool isSingleFinger = (_touchHandler.CurrentMode == QASmartTouch.Managers.TouchInteractionMode.SingleFinger);
            smartDockPill.UpdateTouchModeDisplay(isSingleFinger);
        }

        #endregion

        private void DisposeRadialToolbar()
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(Control.BackgroundProperty, typeof(Button));
            if (descriptor != null)
            {
                foreach (var (btn, handler) in _radialBackgroundListeners)
                    descriptor.RemoveValueChanged(btn, handler);
            }
            _radialBackgroundListeners.Clear();
        }
    }
}
