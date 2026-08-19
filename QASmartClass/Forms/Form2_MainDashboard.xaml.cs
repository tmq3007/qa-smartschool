using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Handlers;
using QASmartTouch.Managers;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using System.Windows.Input;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using QASmartTouch.Services;
using QASmartTouch.Helpers;
using QASmartTouch.Models;
using QASmartTouch.Controls;
using QASmartTouch.Controllers;
using QASmartTouch.Shared;
using QASmartClass.Properties;
using System.Windows.Documents; // For TextRange
using QASmartClass.WhiteboardCore.Managers;
using QASmartClass.WhiteboardCore.Enums;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Undo/Redo Action Types
    /// </summary>
    public enum ActionType
    {
        Add,        // Thêm element (drawing, shape creation)
        Remove,     // Xóa element (erasing, delete)
        Modify,     // Sửa element (move, rotate, resize)
        ClearAll,   // Xóa tất cả elements
        Batch       // Nhóm nhiều actions (compound operations)
    }

    /// <summary>
    /// Represents an action that can be undone/redone
    /// </summary>
    public class UndoRedoAction
    {
        public ActionType Type { get; set; }
        public UIElement? Element { get; set; }
        public UIElement? Parent { get; set; }
        public object? OldValue { get; set; }    // For modify operations
        public object? NewValue { get; set; }    // For modify operations
        public List<UndoRedoAction> BatchActions { get; set; } // For batch operations
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } // For debugging

        public UndoRedoAction()
        {
            Timestamp = DateTime.Now;
            BatchActions = new List<UndoRedoAction>();
            Description = string.Empty;
        }
    }

    public partial class Form2_MainDashboard : Window
    {
        private Button? _selectedTool;
        
        // Active submenu tracking
        private Window? _activeSubMenu;
        private Button? _activeSubMenuButton; // Track which button opened the submenu
        
        // Drawing state
        private bool _isDrawing = false;
        private Point _lastPoint;
        private Polyline? _currentStroke;
        
        // Pen settings
        private string _currentBrushType = "Normal";
        private int _currentPenSize = 2;
        private Color _currentPenColor = Colors.White;
        private bool _drawingEnabled = false;

        public bool IsDrawingEnabled => _drawingEnabled;
        public bool IsEraserEnabled => _eraserEnabled;
        public bool IsShapeDrawingEnabled => _shapeDrawingEnabled;
        
        // Saved pen settings (to restore when reopening pen tool)
        private string _savedBrushType = "Normal";
        private int _savedPenSize = 2;
        private Color _savedPenColor = Colors.White;
        
        // Double-click detection for pen button
        private DateTime _lastPenClickTime = DateTime.MinValue;
        private static readonly TimeSpan DoubleClickInterval = TimeSpan.FromMilliseconds(300);
        
        // Eraser settings
        private bool _eraserEnabled = false;
        private int _eraserSize = 20;
        private string _eraserMode = "Stroke";
        private Ellipse? _eraserPreview; // Visual indicator for eraser
        
        // Ruler Tool
        private Form2_15_RulerTool? _activeRulerTool;
        
        // Protractor Tool
        private Form2_16_ProtractorTool? _activeProtractorTool;
        
        // Set Square Tool
        private Form2_17_SetSquareTool? _activeSetSquareTool;
        
        // Compass Tool
        private Form2_18_CompassTool? _activeCompassTool;
        
        // Compass Tool 3D
        
        // âœ¨ Line spacing for patterns
        private int _currentLineSpacing = 40; // Default 40px (â‰ˆ1.1cm)
        private int _currentLineOpacity = 10; // Default 10% (0-100)
        private Form2_18_CompassTool_3D? _activeCompassTool3D;
        
        // Saved eraser settings (to restore when reopening eraser tool)
        private string _savedEraserMode = "Stroke";
        private int _savedEraserSize = 20;
        
        // Double-click detection for eraser button
        private DateTime _lastEraserClickTime = DateTime.MinValue;
        
        // Double-click detection for select button (to activate Lasso mode)
        private DateTime _lastSelectClickTime = DateTime.MinValue;
        
        // Shape drawing settings
        private bool _shapeDrawingEnabled = false;
        private string _currentShape = "";
        private Point _shapeStartPoint;
        private Shape? _previewShape;
        
        // Text tool settings
        private bool _textToolEnabled = false;
        private string _currentFontFamily = "Arial";
        private int _currentFontSize = 16;
        private Color _currentTextColor = Colors.Black;
        
        // Selection tool settings
        private bool _selectionToolEnabled = false;
        private UIElement? _selectedElement;
        private Point _selectionStartPoint;
        
        // Object action buttons (Copy & Delete)
        private Canvas? _actionButtonsContainer;
        private UIElement? _currentSelectedObject;
        private bool _isDraggingElement = false;
        
        // Zoom area selection settings
        private bool _zoomAreaSelectionEnabled = false;
        private Rectangle? _zoomAreaPreview;
        private Point _zoomAreaStartPoint;
        private double _currentZoomLevel = 1.0; // Track current zoom level

        // Drag-erase selection area
        private Rectangle? _dragErasePreview;
        private Point _dragEraseStartPoint;
        
        // Selection area rectangle (for selecting objects)
        private bool _isSelectingArea = false;
        private Rectangle? _selectionAreaPreview;
        private Point _selectionAreaStartPoint;
        private Canvas? _activeSelectionMenu; // Track active context menu
        
        // Object Selection System (NEW)
        private SelectionManager? _selectionManager;
        private WindowsOCRService? _windowsOcrService;
        private System.Windows.Shapes.Path? _currentSpotlightOverlay;
        private TransformService? _transformService;
        private SelectionBox? _selectionBox;
        private ContextToolbar? _contextToolbar;
        private ThicknessPicker? _thicknessPicker;
        private ColorPicker? _colorPicker;
        private MoreMenu? _moreMenu;
        private FloatingTouchKeyboard? _floatingTouchKeyboard;
        private readonly List<UIElement> _systemUIControls = new();
        private TextBlock? _editingTextBlock;
        private bool _objectSelectionMode = false; // Toggle for object selection mode
        
        // Handwriting Recognition Services
        private HandwritingRecognitionService? _handwritingService;
        private SelectionCaptureService? _captureService;
        private SmartHandwritingRecognitionService _smartHandwritingRecognitionService = new SmartHandwritingRecognitionService();
        
        // Chart Manager (Sprint 2 Day 6 - Hybrid Refactor)
        private QASmartTouch.Managers.ChartManager? _chartManager;
        
        // Board Manager (Multiple Boards Support)
        private QASmartTouch.Managers.BoardManager? _boardManager;
        public QASmartTouch.Managers.BoardManager? BoardManager => _boardManager;
        
        // Object Selection Drag & Drop
        private SelectableObject? _draggedSelectionObject;
        private Point _selectionDragStartPoint;
        private bool _isDraggingSelection = false;
        
        // Rectangle Drag Selection (NG-1 Fix)
        private bool _isRectangleSelecting = false;
        private Point _rectangleSelectionStartPoint;
        private Rectangle? _rectangleSelectionPreview;
        private Border? _smartStatusBadge; // ✅ G4.1: Thanh chỉ dẫn trạng thái sư phạm
        private SelectableObject? _pendingHitObject;
        private bool _pendingIsCtrl = false;

        // Lasso Selection Tool (NEW)
        private QASmartTouch.Tools.LassoSelectionTool? _lassoTool;
        private bool _isLassoMode = false;
        private bool _isMagicWandMode = false; // 🪄 Magic Wand (Chọn theo cùng màu)
        private SmartActionType? _pendingSmartAction = null; // Smart recognition callback after rectangle selection
        private Rect _lastDraggedSelectionRect = Rect.Empty; // Vùng hình chữ nhật vừa kéo chọn trên bảng
        private System.Speech.Synthesis.SpeechSynthesizer? _speechSynthesizer; // 🔊 Text-To-Speech Synthesizer
        
        // Pan (move canvas) when zoomed
        private bool _isPanning = false;
        private Point _panStartPoint;
        private TranslateTransform? _panTransform;
        
        // Zoom area overlay (snapshot with +, -, X controls)
        private UIElement? _zoomAreaOverlay;
        private double _zoomAreaOverlayScale = 1.0;
        private BitmapSource? _zoomAreaSnapshot;
        private Rect _zoomAreaBounds; // Original bounds of zoomed area
        
        // Zoom area overlay drag functionality
        private bool _isDraggingOverlay = false;
        private Point _overlayDragStartPoint;
        
        // Undo/Redo stacks with advanced action tracking
        private System.Collections.Generic.Stack<UndoRedoAction> _undoStack = new();
        private System.Collections.Generic.Stack<UndoRedoAction> _redoStack = new();
        private int MAX_UNDO_LEVELS => GetMaxUndoSteps();
        
        private int GetMaxUndoSteps()
        {
            try
            {
                string settingsDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QASmartClass", "Settings");
                string settingsFile = System.IO.Path.Combine(settingsDir, "board_settings.json");
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
        
        // Window/Fullscreen mode settings
        private bool _isWindowMode = false;
        private bool _isFullScreenMode = false;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;

        private TouchHandler? _touchHandler;
        private WindowModeController? _windowModeController;
        private bool _canvasClickHandlerAdded = false; // Track if canvas click handler is added

        // ── WhiteboardCore Façade (Giai đoạn 3 — Rút logic sang Core) ──
        private WhiteboardManager _whiteboardManager = null!;

        // âœ¨ STROKE OPTIMIZER - Reduces jitter on interactive boards
        private QASmartTouch.Helpers.StrokeOptimizer? _strokeOptimizer;
        private List<Point> _tempStrokePoints = new List<Point>(); // Temporary collection for optimization

        public Form2_MainDashboard()
        {
            InitializeComponent();

            // ── WhiteboardCore: Khởi tạo Façade ──
            _whiteboardManager = new WhiteboardManager();
            _whiteboardManager.ToolManager.ToolChanged += OnWhiteboardToolChanged;

            // =====================================================
            // PHASE 2: DPI AWARENESS - Handle DPI changes
            // Fix touch accuracy when moving between displays
            // =====================================================
            this.DpiChanged += OnDpiChanged;
            System.Diagnostics.Debug.WriteLine("✅ DPI change handler registered");

            // Initialize Touch Handler
            _touchHandler = new TouchHandler(MainInteractiveBoard);
            _touchHandler.SetRecordAddAction(RecordAddAction);
            _touchHandler.SetRecordRemoveAction(RecordRemoveAction); // QC_4.2_TOUCH_ERASER_FIX: Wire undo cho touch erase
            _touchHandler.SetUpdateEraserPreviewAction(UpdateEraserCursorPreview); // ✅ Wire eraser preview callback for touch
            _touchHandler.SetOnCanvasTouchDownAction(() => { if (_activeSubMenu != null) CloseAllSubmenus(); }); // ✅ Close SubMenus on touch canvas
            _touchHandler.SetDrawingProperties(_currentPenColor, _currentPenSize, _currentBrushType);
            _touchHandler.GetColorForPosition = (pos) => _isMultiUserModeActive ? GetStudentByPosition(pos)?.Color : null;
            System.Diagnostics.Debug.WriteLine("✅ Touch interaction initialized");

            // Initialize Window Mode Controller
            _windowModeController = new WindowModeController(this);
            _windowModeController.WindowModeExited += WindowModeController_WindowModeExited;
            System.Diagnostics.Debug.WriteLine("✅ Window Mode Controller initialized");

            // Save initial window state
            _previousWindowState = this.WindowState;
            _previousWindowStyle = this.WindowStyle;
            
            // Initialize undo/redo button states
            UpdateButtonStates();
            
            // Initialize Object Selection System
            InitializeSelectionSystem();
            
            // Initialize Chart Manager (Sprint 2 Day 6)
            InitializeChartManager();
            
            // Initialize Board Manager (Multiple Boards Support)
            InitializeBoardManager();
            
            // âœ¨ Initialize Stroke Optimizer for Interactive Board
            // 🎯 BALANCED MODE: Reduce jitter while preserving natural strokes
            // Optimized for large touch displays (86") with HDMI connection
            _strokeOptimizer = new QASmartTouch.Helpers.StrokeOptimizer();
            
            // 🎯 BALANCED SETTINGS for Large Touch Displays
            // Strategy: Light filtering + Light smoothing to reduce hardware jitter
            // while still preserving the natural shape of strokes
            _strokeOptimizer.MinPointDistance = 2.5;        // 🎯 Balanced (not too low, not too high)
            _strokeOptimizer.EnableDecimation = false;      // 🎯 DISABLED - Keep all points for shape accuracy
            _strokeOptimizer.EnableSmoothing = true;        // 🎯 ENABLED - Light smoothing to reduce hardware jitter
            _strokeOptimizer.DecimationEpsilon = 0.0;       // Not used when disabled
            _strokeOptimizer.SmoothingFactor = 0.2;         // 🎯 20% smoothing (very light, just enough to reduce jitter)
            
            System.Diagnostics.Debug.WriteLine("✅ Stroke Optimizer: 🎯 BALANCED MODE (Light Smoothing, No Decimation)");
            
            // 🔥 Increase UI thread priority for smoother drawing
            try
            {
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
                System.Diagnostics.Debug.WriteLine("✅ UI Thread Priority: HIGHEST");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"âš ï¸ Cannot set thread priority: {ex.Message}");
            }
            
            // âœ¨ Apply default background after window is loaded
            this.Loaded += (s, e) => ApplyDefaultBackground();
            
            // NG-3: Add keyboard shortcuts for Copy/Paste/Delete
            this.PreviewKeyDown += Form2_MainDashboard_PreviewKeyDown;
            this.KeyDown += Form2_MainDashboard_KeyDown;
            
            // Apply App Branding Configuration
            ApplyBrandingConfig();
            AppBrandingService.Instance.BrandingChanged += OnBrandingChanged;
            
            // Initial scrollbar visibility check after window loaded
            this.Loaded += (s, e) => {
                UpdateScrollBarVisibility();
                CheckLicenseWarnings();
            };
            
            // Show welcome state on startup (no tool selected)
            ShowWelcomeState();
            
            // Setup canvas click handler to hide action buttons when clicking empty space
            var mainCanvas = this.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas != null)
            {
                mainCanvas.MouseLeftButtonDown += MainCanvas_MouseLeftButtonDown;
            }

            // =====================================================
            // AUTO HELP TOUR — Mở tour hướng dẫn lần đầu chạy
            // =====================================================
            this.ContentRendered += (s, e) =>
            {
                if (!Services.AppSettings.HasCompletedFirstRun)
                {
                    // Delay 1.5s cho UI ổn định
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(1500)
                    };
                    timer.Tick += (ts, te) =>
                    {
                        timer.Stop();
                        var tour = new HelpTourOverlay(this);
                        tour.Closed += (tc, tce) =>
                        {
                            // Đánh dấu đã hoàn thành first-run
                            Services.AppSettings.HasCompletedFirstRun = true;
                        };
                        tour.Show();
                    };
                    timer.Start();
                }
            };
        }
        
        /// <summary>
        /// PHASE 2: Handle DPI changes when moving between displays
        /// Ensures touch accuracy and rendering quality on different monitors
        /// </summary>
        private void OnDpiChanged(object sender, DpiChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"🖥️ DPI changed: {e.OldDpi.PixelsPerInchX}x{e.OldDpi.PixelsPerInchY} → " +
                $"{e.NewDpi.PixelsPerInchX}x{e.NewDpi.PixelsPerInchY}");
            
            // Refresh canvas rendering to adapt to new DPI
            MainInteractiveBoard.InvalidateVisual();
            
            // Force layout update
            MainInteractiveBoard.UpdateLayout();
            
            System.Diagnostics.Debug.WriteLine("✅ Canvas refreshed for new DPI");
        }
        
        /// <summary>
        /// Handles click on empty canvas area to hide action buttons and deselect objects
        /// âœ¨ Replaces ESC keyboard shortcut for touch-based interface
        /// </summary>
        private void CheckLicenseWarnings()
        {
            var lic = QASmartTouch.Services.License.LicenseService.Instance.CurrentLicense;
            if (lic == null) return;

            if (lic.IsInGracePeriod)
            {
                int remaining = (int)(lic.GraceDeadline - DateTime.UtcNow).TotalDays;
                MessageBox.Show($"⚠️ BẢN QUYỀN ĐÃ HẾT HẠN\n\nPhần mềm đang trong thời gian ân hạn. Bạn còn {remaining} ngày để tiếp tục sử dụng.\nVui lòng liên hệ QA Team để gia hạn ngay lập tức.", 
                    "Cảnh Báo Bản Quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (lic.DaysRemaining <= 15)
            {
                MessageBox.Show($"⚠️ BẢN QUYỀN SẮP HẾT HẠN\n\nGói bản quyền của bạn sẽ hết hạn sau {lic.DaysRemaining} ngày ({lic.ExpiresAt:dd/MM/yyyy}).\nVui lòng chuẩn bị gia hạn để không làm gián đoạn việc giảng dạy.", 
                    "Thông Báo Bản Quyền", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // âœ¨ PHASE 7: Detect double-click for Eke teleport
            if (e.ClickCount == 2)
            {
                MainCanvas_MouseDoubleClick(sender, e);
                return;
            }
            
            // Only process if clicked directly on canvas (not on child elements)
            if (e.Source == sender)
            {
                // Hide legacy action buttons
                HideActionButtons();
                
                // âœ¨ NEW: Deselect all objects (replaces ESC key)
                if (_selectionManager?.HasSelectedObjects() == true)
                {
                    _selectionManager?.DeselectAll();
                    System.Diagnostics.Debug.WriteLine("❌ Deselected all objects (tap on empty area)");
                }
                
                // Clear clipboard if in paste mode
                if (_selectionManager?.HasClipboardContent() == true)
                {
                    _selectionManager?.ClearClipboard();
                    System.Diagnostics.Debug.WriteLine("❌ Paste mode cancelled (tap on empty area)");
                }
            }
        }
        
        /// <summary>
        /// âœ¨ PHASE 7: Handle double-click to teleport Eke
        /// </summary>
        private void MainCanvas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Check 1: Eke is open and visible
                if (_activeSetSquareTool == null || !_activeSetSquareTool.IsVisible)
                    return;
                
                // Check 2: Not currently drawing
                if (_isDrawing)
                    return;
                
                // Check 3: Click directly on MainInteractiveBoard (not on shapes/UI)
                if (e.Source != MainInteractiveBoard)
                    return;
                
                // Get click position in screen coordinates
                var clickPos = e.GetPosition(MainInteractiveBoard);
                var screenPos = MainInteractiveBoard.PointToScreen(clickPos);
                
                // Teleport Eke to clicked position
                _activeSetSquareTool.TeleportToPosition(screenPos);
            }
            catch (Exception ex)
            {
                // Silent fail - don't interrupt user workflow
                System.Diagnostics.Debug.WriteLine($"Teleport error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ⚠️ DISABLED: Keyboard shortcuts removed for touch-optimized interactive board
        /// All functions now accessible via Context Toolbar buttons only
        /// - Copy/Paste: Use toolbar buttons
        /// - Delete: Use toolbar button
        /// - Deselect: Tap empty area on canvas
        /// - Lasso: Double-click "Select" button
        /// </summary>
        private void Form2_MainDashboard_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Do not process global shortcuts when focused in an editable text control
            var focusedControl = Keyboard.FocusedElement as DependencyObject;
            if (focusedControl != null && (focusedControl is TextBox || focusedControl is RichTextBox || focusedControl is PasswordBox || focusedControl is ComboBox))
            {
                return;
            }

            // ✅ GĐ4-FIX: Cho phép ESC để hủy selection/drag
            if (e.Key == Key.Escape && _objectSelectionMode && _selectionManager != null)
            {
                // Hủy kéo vùng nếu đang kéo
                _isRectangleSelecting = false;
                if (_rectangleSelectionPreview != null)
                {
                    MainInteractiveBoard.Children.Remove(_rectangleSelectionPreview);
                    _rectangleSelectionPreview = null;
                }
                
                // Hủy drag nếu đang drag
                _draggedSelectionObject = null;
                _isDraggingSelection = false;
                
                // Bỏ chọn tất cả
                _selectionManager.DeselectAll();
                
                // Ẩn context toolbar
                _contextToolbar?.Hide();
                
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("⌨️ ESC: Cancelled selection & drag state");
                return;
            }
            
            // ✅ Shortcut Ctrl + T: Kích hoạt nhanh công cụ Văn bản
            if (e.Key == Key.T && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                EnableTextTool();
                e.Handled = true;
                System.Diagnostics.Debug.WriteLine("⌨️ Ctrl+T: Activated Text Tool");
                return;
            }

            // ❌ All OTHER keyboard shortcuts vẫn DISABLED cho touch-only interface
            System.Diagnostics.Debug.WriteLine($"⚠️ Keyboard shortcut disabled: {e.Key}");
            System.Diagnostics.Debug.WriteLine($"   Use Context Toolbar buttons for touch interaction");
            
            // Do not handle any keys - let them pass through
            // This ensures no keyboard shortcuts interfere with touch-based workflow
        }
        
        /// <summary>
        /// Initialize Object Selection System
        /// </summary>
        /// <summary>
        /// Initialize Chart Manager (Sprint 2 Day 6 - Hybrid Refactor)
        /// </summary>
        private void InitializeChartManager()
        {
            _chartManager = new QASmartTouch.Managers.ChartManager(MainInteractiveBoard);
            
            // Wire up events
            _chartManager.OnChartCreated += (sender, chart) =>
            {
                Enable3DShapeDragging(chart);
                RecordAddAction(chart, "Chart");
                System.Diagnostics.Debug.WriteLine($"✅ ChartManager created: {chart.Width}x{chart.Height}");
            };
        }
        
        /// <summary>
        /// Initialize Board Manager (Multiple Boards Support)
        /// </summary>
        private void InitializeBoardManager()
        {
            _boardManager = new QASmartTouch.Managers.BoardManager(MainInteractiveBoard);
            _boardManager.IsSystemElementPredicate = IsSystemElement;
            
            System.Diagnostics.Debug.WriteLine($"✅ BoardManager initialized with {_boardManager.BoardCount} board(s)");
        }
        
        private void InitializeSelectionSystem()
        {
            // Initialize services
            _transformService = new TransformService();
            _selectionManager = new SelectionManager(MainInteractiveBoard);
            
            // Initialize UI controls
            _selectionBox = new SelectionBox();
            _contextToolbar = new ContextToolbar();
            _thicknessPicker = new ThicknessPicker();
            _colorPicker = new ColorPicker();
            _moreMenu = new MoreMenu();
            _floatingTouchKeyboard = new FloatingTouchKeyboard();

            // Add controls to canvas
            MainInteractiveBoard.Children.Add(_selectionBox);
            MainInteractiveBoard.Children.Add(_contextToolbar);
            MainInteractiveBoard.Children.Add(_thicknessPicker);
            MainInteractiveBoard.Children.Add(_colorPicker);
            MainInteractiveBoard.Children.Add(_moreMenu);
            MainInteractiveBoard.Children.Add(_floatingTouchKeyboard);

            // Set ZIndex for controls to appear above other elements (QC_4.2_ZINDEX)
            Canvas.SetZIndex(_selectionBox, ZIndexConstants.SelectionBox);
            Canvas.SetZIndex(_contextToolbar, ZIndexConstants.ContextToolbar);
            Canvas.SetZIndex(_thicknessPicker, ZIndexConstants.ThicknessPicker);
            Canvas.SetZIndex(_colorPicker, ZIndexConstants.ColorPicker);
            Canvas.SetZIndex(_moreMenu, ZIndexConstants.MoreMenu);
            Canvas.SetZIndex(_floatingTouchKeyboard, ZIndexConstants.FloatingKeyboard);

            // Register system controls in System UI Registry (QC_4.2_STATE_GUARD)
            _systemUIControls.Clear();
            _systemUIControls.AddRange(new UIElement[] {
                _selectionBox, _contextToolbar, _thicknessPicker,
                _colorPicker, _moreMenu, _floatingTouchKeyboard
            });

            // Wire up Passthrough Touch Focus events
            MainInteractiveBoard.PreviewMouseDown += MainInteractiveBoard_PreviewMouseDown;
            MainInteractiveBoard.PreviewTouchDown += MainInteractiveBoard_PreviewTouchDown;
            MainInteractiveBoard.PreviewStylusDown += MainInteractiveBoard_PreviewStylusDown;

            // Wire up SelectionManager events
            _selectionManager.SelectionChanged += OnSelectionChanged;
            _selectionManager.MultiSelectionChanged += OnMultiSelectionChanged;
            
            // Wire up SelectionBox events
            _selectionBox.ObjectTransformed += OnObjectTransformed;
            _selectionBox.TextEditRequested += OnTextEditRequested;
            
            // Wire up ContextToolbar events
            _contextToolbar.CopyClicked += OnToolbarCopyClicked;
            _contextToolbar.RecognizeHandwritingClicked += OnToolbarRecognizeHandwritingClicked;
            _contextToolbar.LockClicked += OnToolbarLockClicked;
            _contextToolbar.BringToFrontClicked += OnToolbarBringToFrontClicked;
            _contextToolbar.SendToBackClicked += OnToolbarSendToBackClicked;
            _contextToolbar.Rotate90Clicked += OnToolbarRotate90Clicked;
            _contextToolbar.FlipHorizontalClicked += OnToolbarFlipHorizontalClicked;
            _contextToolbar.FlipVerticalClicked += OnToolbarFlipVerticalClicked;
            _contextToolbar.ThicknessClicked += OnToolbarThicknessClicked;
            _contextToolbar.ColorClicked += OnToolbarColorClicked;
            _contextToolbar.ColorSelected += OnColorSelected;
            _contextToolbar.CandidateSelected += OnCandidateSelected;
            _contextToolbar.DeleteClicked += OnToolbarDeleteClicked;
            _contextToolbar.SpotlightClicked += OnToolbarSpotlightClicked;
            _contextToolbar.MoreClicked += OnToolbarMoreClicked;
            
            // Wire up ThicknessPicker events
            _thicknessPicker.ThicknessChanged += OnThicknessChanged;
            
            // Wire up ColorPicker events
            _colorPicker.ColorSelected += OnColorSelected;
            
            // Wire up MoreMenu events (will be implemented in Sprint 2-5)
            _moreMenu.FlipHorizontalClicked += OnMoreMenuFlipHorizontal;
            _moreMenu.FlipVerticalClicked += OnMoreMenuFlipVertical;
            _moreMenu.Rotate180Clicked += OnMoreMenuRotate180;
            _moreMenu.RotateLeft90Clicked += OnMoreMenuRotateLeft90;
            _moreMenu.RotateCustomClicked += OnMoreMenuRotateCustom;
            
            // Wire up Text Operation events (Sprint 2 Module 2.3)
            _moreMenu.UpperCaseClicked += OnMoreMenuUpperCase;
            _moreMenu.LowerCaseClicked += OnMoreMenuLowerCase;
            _moreMenu.TitleCaseClicked += OnMoreMenuTitleCase;
            _moreMenu.EditTextClicked += OnMoreMenuEditText;
            
            // Wire up OCR event (Sprint 2 Module 2.5)
            _moreMenu.OCRClicked += OnMoreMenuOCR;
            
            // Wire up Sprint 3 Smart Features events
            _moreMenu.WebSearchClicked += OnMoreMenuWebSearch;
            _moreMenu.TranslateClicked += OnMoreMenuTranslate;
            _moreMenu.TextToSpeechClicked += OnMoreMenuTextToSpeech;
            _moreMenu.VoiceInputClicked += OnMoreMenuVoiceInput;
            
            // Wire up Sprint 4 AI Features events
            _moreMenu.HandwritingClicked += OnMoreMenuHandwriting;
            _moreMenu.FormulaRecognitionClicked += OnMoreMenuFormulaRecognition;
            
            // Wire up Sprint 5 Advanced Features events
            _moreMenu.GroupClicked += OnMoreMenuGroup;
            _moreMenu.UngroupClicked += OnMoreMenuUngroup;
            _moreMenu.DuplicateClicked += OnMoreMenuDuplicate;
            _moreMenu.CreateLinkClicked += OnMoreMenuCreateLink;
            _moreMenu.ExportImageClicked += OnMoreMenuExportImage;
            
            // Wire up Properties events
            _moreMenu.ObjectInfoClicked += OnMoreMenuObjectInfo;
            _moreMenu.SizePositionClicked += OnMoreMenuSizePosition;
            _moreMenu.StrokeStyleClicked += OnMoreMenuStrokeStyle;

            // Subscribe to SelectionManager events
            if (_selectionManager != null)
            {
                _selectionManager.MultiSelectionChanged += OnMultiSelectionChanged;
            }

            // Initialize Lasso Selection Tool (NEW)
            _lassoTool = new QASmartTouch.Tools.LassoSelectionTool(MainInteractiveBoard);
            _lassoTool.SelectionCompleted += LassoTool_SelectionCompleted;
            
            // Initialize Handwriting Recognition Services
            InitializeHandwritingRecognitionServices();
            
            System.Diagnostics.Debug.WriteLine("✅ Selection System initialized successfully");
        }

        /// <summary>
        /// Initialize Handwriting Recognition Services
        /// </summary>
        private void InitializeHandwritingRecognitionServices()
        {
            try
            {
                // Get API key from settings
                string storedKey = QASmartClass.Properties.Settings.Default.GoogleCloudVisionApiKey ?? string.Empty;
                string decryptedKey = QASmartClass.Utilities.CryptoHelper.DecryptWithDpapi(storedKey);
                string apiKey = string.Empty;

                if (string.IsNullOrEmpty(decryptedKey) && !string.IsNullOrEmpty(storedKey))
                {
                    apiKey = storedKey; // Fallback for legacy plaintext key
                }
                else
                {
                    apiKey = decryptedKey;
                }
                
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    System.Diagnostics.Debug.WriteLine("âš ï¸ Google Cloud Vision API key not configured");
                    System.Diagnostics.Debug.WriteLine("   Handwriting recognition will be disabled");
                    return;
                }

                // Initialize services
                _handwritingService = new HandwritingRecognitionService(apiKey);
                _captureService = new SelectionCaptureService();

                if (_handwritingService.IsInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Handwriting Recognition Service initialized");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Handwriting Recognition Service failed to initialize");
                    _handwritingService = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize Handwriting Recognition: {ex.Message}");
                _handwritingService = null;
                _captureService = null;
            }
        }

        /// <summary>
        /// Scan canvas and register all existing objects with SelectionManager
        /// </summary>
        private void RefreshSelectableObjects()
        {
            if (_selectionManager == null)
                return;

            // ✅ FIX: Clear stale registrations trước khi re-scan
            // (Không dùng ClearAllObjects vì nó xóa cả Canvas.Children)
            _selectionManager.ClearRegistrations();

            int registeredCount = 0;

            // Scan all canvas children
            System.Diagnostics.Debug.WriteLine($"🔍 RefreshSelectableObjects: Scanning {MainInteractiveBoard.Children.Count} canvas children...");
            int childIndex = 0;
            foreach (UIElement child in MainInteractiveBoard.Children)
            {
                // ✅ DEBUG: Log every child element for diagnosis
                var fe = child as FrameworkElement;
                string childType = child.GetType().Name;
                string childName = fe?.Name ?? "(no name)";
                double childW = fe?.ActualWidth ?? 0;
                double childH = fe?.ActualHeight ?? 0;
                System.Diagnostics.Debug.WriteLine($"   [{childIndex}] {childType} Name='{childName}' Size={childW:F0}x{childH:F0} ZIndex={Panel.GetZIndex(child)}");
                childIndex++;
                // Skip selection system UI controls
                if (child == _selectionBox || child == _contextToolbar || 
                    child == _thicknessPicker || child == _colorPicker || child == _moreMenu)
                    continue;

                // ✅ FIX: Skip ALL preview/system elements
                if (child == _eraserPreview || 
                    child == _rectangleSelectionPreview || 
                    child == _zoomAreaPreview || 
                    child == _dragErasePreview ||
                    child == _currentSpotlightOverlay ||
                    child == _smartStatusBadge)
                    continue;

                // ✅ FIX: Skip lasso visual (Polyline with ZIndex >= SystemUIBase)
                if (Panel.GetZIndex(child) >= ZIndexConstants.SystemUIBase)
                    continue;

                // ✅ GĐ1-FIX: Skip welcome state panel
                if (child == panelWelcomeState)
                    continue;

                // ✅ GĐ1-FIX: Skip elements with very low opacity (background overlays)
                if (child is FrameworkElement feCheck)
                {
                    if (feCheck.Opacity < 0.1)
                        continue;
                    
                    // Skip elements tagged/named as background or covering board
                    if (SelectionManager.IsBackgroundElement(feCheck, Rect.Empty, MainInteractiveBoard.ActualWidth, MainInteractiveBoard.ActualHeight))
                    {
                        System.Diagnostics.Debug.WriteLine($"⏭️ Skipped background element: {feCheck.Name ?? feCheck.GetType().Name}");
                        continue;
                    }
                }

                // Register Polyline (drawn strokes)
                if (child is Polyline polyline && polyline.Points.Count > 0)
                {
                    // ✅ QC_4.2_SELECTION_BOUNDS_FIX: Dùng BoundsHelper.GetAbsoluteBounds để lấy chính xác tuyệt đối tọa độ Canvas
                    var bounds = QASmartTouch.Helpers.BoundsHelper.GetAbsoluteBounds(polyline, MainInteractiveBoard);
                    if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
                        continue;

                    var selectableObj = new SelectableObject
                    {
                        Element = polyline,
                        Type = ObjectType.Stroke,
                        Bounds = bounds,
                        Position = new Point(bounds.Left, bounds.Top),
                        Size = new Size(bounds.Width, bounds.Height),
                        ZIndex = Panel.GetZIndex(polyline),
                        StrokeColor = (polyline.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                        StrokeThickness = polyline.StrokeThickness
                    };

                    _selectionManager.AddObject(selectableObj);
                    registeredCount++;
                    
                    System.Diagnostics.Debug.WriteLine($"📝 Registered Polyline: Bounds=({bounds.Left:F0},{bounds.Top:F0},{bounds.Width:F0},{bounds.Height:F0})");
                }
                // Register Path (smooth Bezier strokes)
                else if (child is System.Windows.Shapes.Path path && path.Data != null)
                {
                    // ✅ QC_4.2_SELECTION_BOUNDS_FIX: Dùng BoundsHelper.GetAbsoluteBounds để lấy chính xác tuyệt đối tọa độ Canvas
                    var dataBounds = QASmartTouch.Helpers.BoundsHelper.GetAbsoluteBounds(path, MainInteractiveBoard);
                    if (dataBounds.IsEmpty || dataBounds.Width <= 0 || dataBounds.Height <= 0)
                        continue;

                    var selectableObj = new SelectableObject
                    {
                        Element = path,
                        Type = ObjectType.Drawing,
                        Bounds = dataBounds,
                        Position = new Point(dataBounds.Left, dataBounds.Top),
                        Size = new Size(dataBounds.Width, dataBounds.Height),
                        ZIndex = Panel.GetZIndex(path),
                        StrokeColor = (path.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                        StrokeThickness = path.StrokeThickness
                    };

                    _selectionManager.AddObject(selectableObj);
                    registeredCount++;
                    System.Diagnostics.Debug.WriteLine($"🖊️ Registered Path: Bounds=({dataBounds.Left:F0},{dataBounds.Top:F0},{dataBounds.Width:F0},{dataBounds.Height:F0})");
                }
                // Register Line (ruler/protractor/tool lines)
                // ✅ GĐ1-FIX: Bỏ qua grid/guide lines kéo dài toàn canvas
                else if (child is System.Windows.Shapes.Line line)
                {
                    double lMinX = Math.Min(line.X1, line.X2);
                    double lMinY = Math.Min(line.Y1, line.Y2);
                    double lW = Math.Abs(line.X2 - line.X1);
                    double lH = Math.Abs(line.Y2 - line.Y1);
                    if (lW < 5) lW = 5;
                    if (lH < 5) lH = 5;

                    // ✅ GĐ1-FIX: Skip grid/guide lines (kéo dài > 800px một chiều)
                    // Grid lines thường kéo dài toàn bộ chiều ngang hoặc chiều dọc canvas
                    if (lW > 800 || lH > 800)
                    {
                        System.Diagnostics.Debug.WriteLine($"⏭️ Skipped grid/guide Line: ({line.X1:F0},{line.Y1:F0}) → ({line.X2:F0},{line.Y2:F0}) — too large (W={lW:F0}, H={lH:F0})");
                        continue;
                    }

                    var selectableObj = new SelectableObject
                    {
                        Element = line,
                        Type = ObjectType.Shape,
                        Bounds = new Rect(lMinX, lMinY, lW, lH),
                        Position = new Point(lMinX, lMinY),
                        Size = new Size(lW, lH),
                        ZIndex = Panel.GetZIndex(line),
                        StrokeColor = (line.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                        StrokeThickness = line.StrokeThickness
                    };

                    _selectionManager.AddObject(selectableObj);
                    registeredCount++;
                    System.Diagnostics.Debug.WriteLine($"📏 Registered Line: ({line.X1:F0},{line.Y1:F0}) → ({line.X2:F0},{line.Y2:F0})");
                }
                // Register Image
                else if (child is System.Windows.Controls.Image image)
                {
                    double imgL = Canvas.GetLeft(image);
                    double imgT = Canvas.GetTop(image);
                    if (double.IsNaN(imgL)) imgL = 0;
                    if (double.IsNaN(imgT)) imgT = 0;
                    double imgW = image.ActualWidth > 0 ? image.ActualWidth : image.Width;
                    double imgH = image.ActualHeight > 0 ? image.ActualHeight : image.Height;
                    if (imgW <= 0 || imgH <= 0) continue;

                    var selectableObj = new SelectableObject
                    {
                        Element = image,
                        Type = ObjectType.Image,
                        Bounds = new Rect(imgL, imgT, imgW, imgH),
                        Position = new Point(imgL, imgT),
                        Size = new Size(imgW, imgH),
                        ZIndex = Panel.GetZIndex(image)
                    };

                    _selectionManager.AddObject(selectableObj);
                    registeredCount++;
                    System.Diagnostics.Debug.WriteLine($"🖼️ Registered Image: Bounds=({imgL:F0},{imgT:F0},{imgW:F0},{imgH:F0})");
                }
                // Register Shape (Rectangle, Ellipse - NOT Path/Line/Polyline)
                else if (child is System.Windows.Shapes.Shape shape && !(shape is Polyline) && !(shape is System.Windows.Shapes.Path) && !(shape is System.Windows.Shapes.Line))
                {
                    double left = Canvas.GetLeft(shape);
                    double top = Canvas.GetTop(shape);
                    double width = shape.ActualWidth;
                    double height = shape.ActualHeight;

                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    if (double.IsNaN(width) || width <= 0) width = shape.Width;
                    if (double.IsNaN(height) || height <= 0) height = shape.Height;
                    
                    if (width <= 0 || height <= 0)
                        continue;

                    // ✅ GĐ1-FIX: Skip background/overlay shapes quá lớn (>= canvas size)
                    if (width >= MainInteractiveBoard.ActualWidth || height >= MainInteractiveBoard.ActualHeight)
                    {
                        System.Diagnostics.Debug.WriteLine($"⏭️ Skipped full-canvas Shape: {shape.GetType().Name} ({width:F0}x{height:F0}) — background/overlay");
                        continue;
                    }

                    var bounds = new Rect(left, top, width, height);

                    var selectableObj = new SelectableObject
                    {
                        Element = shape,
                        Type = ObjectType.Shape,
                        Bounds = bounds,
                        Position = new Point(left, top),
                        Size = new Size(width, height),
                        ZIndex = Panel.GetZIndex(shape),
                        StrokeColor = (shape.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                        StrokeThickness = shape.StrokeThickness,
                        FillColor = (shape.Fill as SolidColorBrush)?.Color ?? Colors.Transparent
                    };

                    _selectionManager.AddObject(selectableObj);
                    registeredCount++;
                    
                    System.Diagnostics.Debug.WriteLine($"🔷 Registered Shape: {shape.GetType().Name} Bounds=({left:F0},{top:F0},{width:F0},{height:F0})");
                }
                // ✅ N26 FIX: Register StackPanel Container (Table / Graph Containers)
                else if (child is StackPanel stackPanel)
                {
                    double left = Canvas.GetLeft(stackPanel);
                    double top = Canvas.GetTop(stackPanel);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    double width = stackPanel.ActualWidth > 0 ? stackPanel.ActualWidth : stackPanel.Width;
                    double height = stackPanel.ActualHeight > 0 ? stackPanel.ActualHeight : stackPanel.RenderSize.Height;

                    if (double.IsNaN(width) || width <= 0) width = 300;
                    if (double.IsNaN(height) || height <= 0) height = 200;

                    if (width > 0 && height > 0 && width <= 1800 && height <= 1800)
                    {
                        var bounds = new Rect(left, top, width, height);
                        var selectableObj = new SelectableObject
                        {
                            Element = stackPanel,
                            Type = ObjectType.Other,
                            Bounds = bounds,
                            Position = new Point(left, top),
                            Size = new Size(width, height),
                            ZIndex = Panel.GetZIndex(stackPanel)
                        };
                        _selectionManager.AddObject(selectableObj);
                        registeredCount++;
                        System.Diagnostics.Debug.WriteLine($"📊 Registered StackPanel Container: Bounds=({left:F0},{top:F0},{width:F0},{height:F0})");
                    }
                }
                // Register Container (Border, Grid, etc. containing 3D shapes)
                else if (child is Border border)
                {
                    double left = Canvas.GetLeft(border);
                    double top = Canvas.GetTop(border);
                    double width = border.ActualWidth;
                    double height = border.ActualHeight;

                    // Handle NaN values
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    
                    // Skip if no valid size
                    if (width <= 0 || height <= 0)
                        continue;

                    // ✅ GĐ1-FIX: Skip large background borders
                    if (width > 800 || height > 800)
                    {
                        System.Diagnostics.Debug.WriteLine($"⏭️ Skipped large Border: ({width:F0}x{height:F0}) — background container");
                        continue;
                    }

                    var bounds = new Rect(left, top, width, height);

                    var selectableObj = new SelectableObject
                    {
                        Element = border,
                        Type = ObjectType.Other,
                        Bounds = bounds,
                        Position = new Point(left, top),
                        Size = new Size(width, height),
                        ZIndex = Panel.GetZIndex(border),
                        StrokeColor = (border.BorderBrush as SolidColorBrush)?.Color ?? Colors.Black,
                        StrokeThickness = border.BorderThickness.Left, // Use Left thickness as representative
                        FillColor = (border.Background as SolidColorBrush)?.Color ?? Colors.Transparent
                    };

                    _selectionManager.AddObject(selectableObj);
                    registeredCount++;
                    
                    System.Diagnostics.Debug.WriteLine($"📦 Registered Border: Bounds=({left:F0},{top:F0},{width:F0},{height:F0})");
                }
            }

            _selectionManager.RebuildQuadTree();
            System.Diagnostics.Debug.WriteLine($"✅ Registered {registeredCount} selectable objects & rebuilt QuadTree index.");
        }
        
        /// <summary>
        /// BUGFIX: Register a newly created object with SelectionManager
        /// </summary>
        private void RegisterNewObjectWithSelectionManager(UIElement element)
        {
            if (_selectionManager == null || element == null)
                return;

            // Do NOT register background layers, grid patterns, or full-canvas overlays
            if (SelectionManager.IsBackgroundElement(element, Rect.Empty, MainInteractiveBoard.ActualWidth, MainInteractiveBoard.ActualHeight))
            {
                System.Diagnostics.Debug.WriteLine($"⏭️ RegisterNewObject: Skipped background element: {element.GetType().Name}");
                return;
            }

            // Ensure the element is added to the canvas children first so it can load and populate points/bounds
            if (!MainInteractiveBoard.Children.Contains(element))
            {
                MainInteractiveBoard.Children.Add(element);
            }

            // Register Polyline (drawn strokes)
            if (element is Polyline polyline)
            {
                // ✅ N25 FIX: Register Polyline immediately if Points.Count > 0
                // Do NOT rely on IsLoaded since strokes are already attached to Visual Tree during freehand drawing
                if (polyline.Points != null && polyline.Points.Count > 0)
                {
                    RegisterPolylineWithBounds(polyline);
                }
            }
            // BUG-1603: Register Text objects (Border containing TextBlock/TextBox)
            // Phải đặt TRƯỚC nhánh Shape vì Border không phải Shape.
            else if (element is Border border && (border.Child is TextBlock || border.Child is TextBox))
            {
                RegisterTextElement(border);
            }
            else if (element is TextBlock textBlock)
            {
                RegisterTextElement(textBlock);
            }
            else if (element is TextBox textBox)
            {
                RegisterTextElement(textBox);
            }
            // Register Shape (Rectangle, Ellipse, etc.)
            else if (element is System.Windows.Shapes.Shape shape && !(shape is Polyline))
            {
                // BUG-1603 FIX: Nếu size = 0 (chưa qua Layout Pass), hook Loaded event
                // thay vì return bỏ qua đăng ký.
                double width = shape.ActualWidth;
                double height = shape.ActualHeight;
                
                if (width <= 0 || height <= 0)
                {
                    // Fallback 1: Thử lấy từ Width/Height property
                    if (double.IsNaN(width) || width <= 0) width = shape.Width;
                    if (double.IsNaN(height) || height <= 0) height = shape.Height;
                }
                
                if (width > 0 && height > 0 && !double.IsNaN(width) && !double.IsNaN(height))
                {
                    // Kích thước hợp lệ → đăng ký ngay
                    RegisterShapeWithBounds(shape, width, height);
                }
                else
                {
                    // BUG-1603 FIX: Chưa có kích thước → chờ Loaded event
                    System.Diagnostics.Debug.WriteLine(
                        $"⏳ BUG-1603: Shape {shape.GetType().Name} size=0, waiting for Loaded event");
                    
                    shape.Loaded += (s, ev) =>
                    {
                        double w = shape.ActualWidth > 0 ? shape.ActualWidth : shape.Width;
                        double h = shape.ActualHeight > 0 ? shape.ActualHeight : shape.Height;
                        
                        if (w > 0 && h > 0 && !double.IsNaN(w) && !double.IsNaN(h))
                        {
                            RegisterShapeWithBounds(shape, w, h);
                            System.Diagnostics.Debug.WriteLine(
                                $"✅ BUG-1603: Shape {shape.GetType().Name} registered after Loaded ({w:F0}x{h:F0})");
                        }
                        else
                        {
                            // Fallback 2: Chờ SizeChanged
                            shape.SizeChanged += (s2, ev2) =>
                            {
                                if (ev2.NewSize.Width > 0 && ev2.NewSize.Height > 0)
                                {
                                    RegisterShapeWithBounds(shape, ev2.NewSize.Width, ev2.NewSize.Height);
                                    System.Diagnostics.Debug.WriteLine(
                                        $"✅ BUG-1603: Shape {shape.GetType().Name} registered after SizeChanged ({ev2.NewSize.Width:F0}x{ev2.NewSize.Height:F0})");
                                }
                            };
                        }
                    };
                }
            }
            // Register Path (smooth Bezier strokes after ConvertToSmoothPath)
            else if (element is System.Windows.Shapes.Path path && path.Data != null)
            {
                var dataBounds = path.Data.Bounds;
                if (dataBounds.Width > 0 && dataBounds.Height > 0)
                {
                    var selectableObj = new SelectableObject
                    {
                        Element = path,
                        Type = ObjectType.Drawing,
                        Bounds = dataBounds,
                        Position = new Point(dataBounds.Left, dataBounds.Top),
                        Size = new Size(dataBounds.Width, dataBounds.Height),
                        ZIndex = Panel.GetZIndex(path),
                        StrokeColor = (path.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                        StrokeThickness = path.StrokeThickness
                    };

                    _selectionManager.AddObject(selectableObj);
                    System.Diagnostics.Debug.WriteLine($"✅ Auto-registered Path (Bezier): Bounds=({dataBounds.Left:F0},{dataBounds.Top:F0},{dataBounds.Width:F0},{dataBounds.Height:F0})");
                }
                else
                {
                    // Path not yet rendered, wait for Loaded
                    path.Loaded += (s, ev) =>
                    {
                        var b = path.Data?.Bounds ?? Rect.Empty;
                        if (b.Width > 0 && b.Height > 0)
                        {
                            var obj = new SelectableObject
                            {
                                Element = path,
                                Type = ObjectType.Drawing,
                                Bounds = b,
                                Position = new Point(b.Left, b.Top),
                                Size = new Size(b.Width, b.Height),
                                ZIndex = Panel.GetZIndex(path),
                                StrokeColor = (path.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                                StrokeThickness = path.StrokeThickness
                            };
                            _selectionManager?.AddObject(obj);
                        }
                    };
                }
            }
            // Register Line (ruler/tool drawn lines)
            else if (element is System.Windows.Shapes.Line line)
            {
                double minX = Math.Min(line.X1, line.X2);
                double minY = Math.Min(line.Y1, line.Y2);
                double width = Math.Abs(line.X2 - line.X1);
                double height = Math.Abs(line.Y2 - line.Y1);
                if (width < 5) width = 5;
                if (height < 5) height = 5;

                var selectableObj = new SelectableObject
                {
                    Element = line,
                    Type = ObjectType.Shape,
                    Bounds = new Rect(minX, minY, width, height),
                    Position = new Point(minX, minY),
                    Size = new Size(width, height),
                    ZIndex = Panel.GetZIndex(line),
                    StrokeColor = (line.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                    StrokeThickness = line.StrokeThickness
                };

                _selectionManager.AddObject(selectableObj);
                System.Diagnostics.Debug.WriteLine($"✅ Auto-registered Line: ({line.X1:F0},{line.Y1:F0}) → ({line.X2:F0},{line.Y2:F0})");
            }
        }

        /// <summary>
        /// BUG-1603: Đăng ký Shape với SelectionManager sử dụng kích thước đã xác nhận.
        /// Extracted helper method để tránh lặp code giữa đăng ký trực tiếp và Loaded event.
        /// </summary>
        private void RegisterShapeWithBounds(System.Windows.Shapes.Shape shape, double width, double height)
        {
            if (_selectionManager == null) return;
            
            double left = Canvas.GetLeft(shape);
            double top = Canvas.GetTop(shape);
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            var bounds = new Rect(left, top, width, height);
            var selectableObj = new SelectableObject
            {
                Element = shape,
                Type = ObjectType.Shape,
                Bounds = bounds,
                Position = new Point(left, top),
                Size = new Size(width, height),
                ZIndex = Panel.GetZIndex(shape),
                StrokeColor = (shape.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                StrokeThickness = shape.StrokeThickness,
                FillColor = (shape.Fill as SolidColorBrush)?.Color ?? Colors.Transparent
            };

            _selectionManager.AddObject(selectableObj);
            System.Diagnostics.Debug.WriteLine(
                $"✅ Auto-registered Shape: {shape.GetType().Name} Bounds=({left:F0},{top:F0},{width:F0},{height:F0})");
        }

        /// <summary>
        /// BUG-1603: Đăng ký đối tượng Text (Border/TextBlock/TextBox) với SelectionManager.
        /// Nếu kích thước chưa có (chưa qua Layout Pass), chờ Loaded event.
        /// </summary>
        private void RegisterTextElement(FrameworkElement textElement)
        {
            if (_selectionManager == null) return;

            void DoRegister()
            {
                double left = Canvas.GetLeft(textElement);
                double top = Canvas.GetTop(textElement);
                double width = textElement.ActualWidth > 0 ? textElement.ActualWidth : textElement.Width;
                double height = textElement.ActualHeight > 0 ? textElement.ActualHeight : textElement.Height;

                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;
                if (double.IsNaN(width) || width <= 0 || double.IsNaN(height) || height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"⚠️ BUG-1603: Text element {textElement.GetType().Name} still has no size after Loaded");
                    return;
                }

                var bounds = new Rect(left, top, width, height);
                var selectableObj = new SelectableObject
                {
                    Element = textElement,
                    Type = ObjectType.Text,
                    Bounds = bounds,
                    Position = new Point(left, top),
                    Size = new Size(width, height),
                    ZIndex = Panel.GetZIndex(textElement)
                };

                _selectionManager?.AddObject(selectableObj);
                System.Diagnostics.Debug.WriteLine(
                    $"✅ BUG-1603: Registered Text ({textElement.GetType().Name}) Bounds=({left:F0},{top:F0},{width:F0},{height:F0})");
            }

            // Thử đăng ký ngay nếu đã có kích thước
            if (textElement.IsLoaded && textElement.ActualWidth > 0 && textElement.ActualHeight > 0)
            {
                DoRegister();
            }
            else
            {
                // Chờ Loaded event để có kích thước chính xác
                textElement.Loaded += (s, ev) => DoRegister();
                System.Diagnostics.Debug.WriteLine(
                    $"⏳ BUG-1603: Text {textElement.GetType().Name} waiting for Loaded event");
            }
        }

        private void RegisterPolylineWithBounds(Polyline polyline)
        {
            if (_selectionManager == null || polyline == null)
                return;

            // CRITICAL: Check if Points collection exists and has points
            if (polyline.Points == null || polyline.Points.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"âš ï¸ Cannot register Polyline: Points.Count = {polyline.Points?.Count ?? 0}");
                return;
            }

            // Calculate bounds from Points
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var point in polyline.Points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            double width = maxX - minX;
            double height = maxY - minY;
            
            // CRITICAL: Validate calculated bounds before padding
            if (double.IsInfinity(minX) || double.IsInfinity(minY) || 
                double.IsInfinity(maxX) || double.IsInfinity(maxY) ||
                double.IsNaN(width) || double.IsNaN(height) ||
                width < 0 || height < 0)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Invalid bounds calculation: minX={minX}, minY={minY}, width={width}, height={height}");
                return;
            }
            
            // Add padding for stroke thickness
            double padding = polyline.StrokeThickness;
            minX -= padding;
            minY -= padding;
            width += padding * 2;
            height += padding * 2;
            
            // Ensure minimum size for selection
            if (width < 10) width = 10;
            if (height < 10) height = 10;

            var bounds = new Rect(minX, minY, width, height);

            var selectableObj = new SelectableObject
            {
                Element = polyline,
                Type = ObjectType.Stroke,
                Bounds = bounds,
                Position = new Point(minX, minY),
                Size = new Size(width, height),
                ZIndex = Panel.GetZIndex(polyline),
                StrokeColor = (polyline.Stroke as SolidColorBrush)?.Color ?? Colors.Black,
                StrokeThickness = polyline.StrokeThickness
            };

            _selectionManager.AddObject(selectableObj);
            System.Diagnostics.Debug.WriteLine($"✅ Registered Polyline: Points={polyline.Points.Count}, Bounds=({minX:F0},{minY:F0},{width:F0},{height:F0})");
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Lấy kích thước màn hình hiện tại
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // Lấy kích thước cửa sổ
            var windowWidth = this.ActualWidth;
            var windowHeight = this.ActualHeight;

            // Căn giữa theo chiều ngang và sát cạnh dưới
            this.Left = (screenWidth - windowWidth) / 2;
            this.Top = screenHeight - windowHeight - 10; // chừa 10px cách mép dưới
        }

        #region Tool State Management - Helper Methods

        /// <summary>
        /// ✅ PHASE 1: Deactivate all drawing tools (Pen, Eraser, Select)
        /// Prevents tool conflicts and ensures only one tool is active at a time
        /// </summary>
        private void DeactivateAllTools()
        {
            // Clear spotlight overlay
            if (_currentSpotlightOverlay != null)
            {
                MainInteractiveBoard.Children.Remove(_currentSpotlightOverlay);
                _currentSpotlightOverlay = null;
            }
            
            // Deactivate Lasso selection tool
            if (_lassoTool != null)
            {
                _lassoTool.Deactivate();
            }

            System.Diagnostics.Debug.WriteLine("🔄 DeactivateAllTools called");
            
            // Reset drawing state
            _isDrawing = false;
            
            // Disable all tool modes
            _drawingEnabled = false;
            _eraserEnabled = false;
            _shapeDrawingEnabled = false;
            _textToolEnabled = false;
            _selectionToolEnabled = false;
            _zoomAreaSelectionEnabled = false;
            _objectSelectionMode = false; // BUGFIX: Disable object selection mode
            _isLassoMode = false;
            _isMagicWandMode = false;
            
            System.Diagnostics.Debug.WriteLine($"   All tool modes disabled");
            
            // Detach selection box & hide context toolbar & touch keyboard (QC_4.2_KEYBOARD)
            _selectionBox?.Detach();
            _contextToolbar?.Hide();
            _floatingTouchKeyboard?.HideKeyboard();

            // Clean up rectangle selection preview if dangling
            if (_rectangleSelectionPreview != null)
            {
                MainInteractiveBoard?.Children.Remove(_rectangleSelectionPreview);
                _rectangleSelectionPreview = null;
            }
            _isRectangleSelecting = false;
            _isDraggingSelection = false;
            _draggedSelectionObject = null;
            _pendingHitObject = null;

            // Release mouse, touch, and stylus captures
            MainInteractiveBoard?.ReleaseMouseCapture();
            MainInteractiveBoard?.ReleaseStylusCapture();
            MainInteractiveBoard?.ReleaseAllTouchCaptures();

            // Deselect any selected objects & rebuild QuadTree index
            _selectionManager?.DeselectAll();
            _selectionManager?.RebuildQuadTree();
            
            // Reset button backgrounds & icon colors to default
            var defaultBrush = new SolidColorBrush(Color.FromRgb(241, 242, 246)); // #F1F2F6
            var defaultIconColor = Color.FromRgb(0x2F, 0x35, 0x42); // #2F3542
            
            if (_selectedTool != null)
            {
                _selectedTool.Background = defaultBrush;
                UpdateIconColor(_selectedTool, defaultIconColor);
                _selectedTool = null;
            }
            
            if (btn1_Pen != null)
            {
                btn1_Pen.Background = defaultBrush;
                UpdateIconColor(btn1_Pen, defaultIconColor);
            }
            
            if (btn2_Eraser != null)
            {
                btn2_Eraser.Background = defaultBrush;
                UpdateIconColor(btn2_Eraser, defaultIconColor);
            }
            
            if (btn8_Select != null)
            {
                btn8_Select.Background = defaultBrush;
                UpdateIconColor(btn8_Select, defaultIconColor);
            }
            
            // Reset cursor to default
            this.Cursor = System.Windows.Input.Cursors.Arrow;
            MainInteractiveBoard.Cursor = System.Windows.Input.Cursors.Arrow;
            
            // Clear any active drawing state
            if (_currentStroke != null)
            {
                _currentStroke = null;
            }
            
            // Remove eraser preview if exists
            RemoveEraserCursorPreview();

            // QC_4.2_MODE_GUARD: Tắt Pan Mode hoàn toàn nếu đang active
            if (_isPanModeActive)
                DisablePanMode();

            // Giải phóng ScrollViewer capture
            if (MainScrollViewer.IsMouseCaptured)
                MainScrollViewer.ReleaseMouseCapture();

            // Đồng bộ con trỏ ScrollViewer cha về Arrow
            MainScrollViewer.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        /// <summary>
        /// [LOI_VID_52] Close all active submenus with fade-out animation.
        /// Fade-out 150ms → Close() → reset button.
        /// Prevents submenu overlap and ensures clean UI.
        /// </summary>
        private void CloseAllSubmenus()
        {
            // Close active submenu window
            if (_activeSubMenu != null)
            {
                var menuToClose = _activeSubMenu;
                _activeSubMenu = null; // Clear reference ngay để tránh re-entrant

                try
                {
                    // [LOI_VID_52] Fade-out animation 150ms
                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(
                        1.0, 0.0, TimeSpan.FromMilliseconds(150))
                    {
                        EasingFunction = new System.Windows.Media.Animation.QuadraticEase
                        {
                            EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn
                        }
                    };
                    fadeOut.Completed += (s, e) =>
                    {
                        try { menuToClose.Close(); }
                        catch { /* Ignore if already closed */ }
                    };
                    menuToClose.BeginAnimation(Window.OpacityProperty, fadeOut);
                }
                catch
                {
                    // Fallback: đóng trực tiếp nếu animation lỗi
                    try { menuToClose.Close(); }
                    catch { /* Ignore if already closed */ }
                }
            }
            
            // Reset active submenu button background
            if (_activeSubMenuButton != null)
            {
                var defaultBrush = new SolidColorBrush(Color.FromRgb(241, 242, 246)); // #F1F2F6
                _activeSubMenuButton.Background = defaultBrush;
                UpdateIconColor(_activeSubMenuButton, Color.FromRgb(0x2F, 0x35, 0x42));
                _activeSubMenuButton = null;
            }
            
            // Reset selected tool
            _selectedTool = null;
        }

        /// <summary>
        /// ✅ PHASE 1: Activate a specific tool
        /// Centralizes tool activation logic
        /// </summary>
        private void ActivateTool(string toolName, Button toolButton)
        {
            // First, deactivate all other tools
            DeactivateAllTools();
            
            // Close any open submenus
            CloseAllSubmenus();
            
            // Activate the specified tool
            _selectedTool = toolButton;
            
            // Highlight the active tool button
            var activeBrush = new SolidColorBrush(Color.FromRgb(46, 134, 222)); // #2E86DE
            if (toolButton != null)
            {
                toolButton.Background = activeBrush;
            }
        }

        #endregion

        // ── MOVED TO Form2_MainDashboard.Tools.cs (original lines 1596-3909, 2314 lines) ──

        #region Helper Methods

        /// <summary>
        /// QC_4.2_STATE_GUARDRAIL: Đảm bảo dọn dẹp sạch sẽ toàn bộ trạng thái dở dang của công cụ cũ trước khi chuyển đổi công cụ mới.
        /// Ngăn ngừa triệt để lỗi rò rỉ sự kiện (Event Leakage), xung đột Adorner và SelectionBox khi sử dụng đan xen các tính năng.
        /// </summary>
        public void ResetAllToolStates()
        {
            try
            {
                // 1. Deselect và ẩn toàn bộ Adorner/SelectionBox/Toolbar
                _selectionManager?.DeselectAll();
                _selectionBox?.Detach();
                _contextToolbar?.Hide();

                // 2. Dọn dẹp trạng thái chọn vùng hình chữ nhật / Lasso
                if (_isRectangleSelecting && _rectangleSelectionPreview != null)
                {
                    MainInteractiveBoard?.Children.Remove(_rectangleSelectionPreview);
                    _rectangleSelectionPreview = null;
                }
                _isRectangleSelecting = false;
                _isLassoMode = false;
                _isDraggingSelection = false;
                _draggedSelectionObject = null;
                _pendingHitObject = null;

                // 3. Rebuild lại chỉ mục QuadTree để đồng bộ không gian
                _selectionManager?.RebuildQuadTree();

                // 4. Giải phóng chuột/ngón tay capture nếu có
                MainInteractiveBoard?.ReleaseMouseCapture();
                MainInteractiveBoard?.ReleaseStylusCapture();
                MainInteractiveBoard?.ReleaseAllTouchCaptures();

                System.Diagnostics.Debug.WriteLine("🔄 ResetAllToolStates: Cleaned up all previous tool states successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error in ResetAllToolStates: {ex.Message}");
            }
        }

        private void SelectTool(Button tool)
        {
            // Reset toàn bộ trạng thái dở dang của công cụ cũ trước khi đổi công cụ
            ResetAllToolStates();

            // Close any active submenu before switching tools
            CloseActiveSubMenu();
            
            // Reset previous selected tool
            if (_selectedTool != null)
            {
                _selectedTool.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F2F6"));
                // KHẮC PHỤC: Reset màu icon của công cụ cũ về màu tối mặc định (#2F3542)
                UpdateIconColor(_selectedTool, Color.FromRgb(0x2F, 0x35, 0x42));
            }

            // Set new selected tool
            _selectedTool = tool;
            _selectedTool.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E86DE"));

            // Update icon color to white for selected tool
            UpdateIconColor(_selectedTool, Colors.White);
        }

        public void CloseActiveSubMenu()
        {
            if (_activeSubMenu != null && _activeSubMenu.IsVisible)
            {
                if (_activeSubMenu is Form2_2_SubMenuEraser eraserMenu)
                {
                    _savedEraserMode = eraserMenu.EraserMode;
                    _savedEraserSize = eraserMenu.EraserSize;
                    if (_savedEraserMode != "ClearAll")
                    {
                        EnableEraserMode(_savedEraserSize, _savedEraserMode);
                    }
                }
                var menuToClose = _activeSubMenu;
                _activeSubMenu = null;
                _activeSubMenuButton = null;
                menuToClose.Close();
            }
        }

        private void UpdateIconColor(Button button, Color color)
        {
            // Find the Path element inside the button and update its fill color
            if (button.Template != null)
            {
                var border = button.Template.FindName("PART_Border", button) as Border;
                if (border != null)
                {
                    var viewbox = FindVisualChild<Viewbox>(border);
                    if (viewbox != null)
                    {
                        var canvas = FindVisualChild<Canvas>(viewbox);
                        if (canvas != null)
                        {
                            var path = FindVisualChild<Path>(canvas);
                            if (path != null)
                            {
                                path.Fill = new SolidColorBrush(color);
                            }
                        }
                    }
                }
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void HideWelcomeState()
        {
            panelWelcomeState.Visibility = Visibility.Collapsed;
        }

        private void ShowWelcomeState()
        {
            panelWelcomeState.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Apply app branding configuration from AppBrandingService
        /// </summary>
        private void ApplyBrandingConfig()
        {
            try
            {
                var config = AppBrandingService.Instance.Config;
                
                // Find header border and adjust grid row
                var headerBorder = this.FindName("HeaderBorder") as Border;
                var mainGrid = this.FindName("MainGrid") as Grid;
                
                // Apply header visibility
                if (headerBorder != null)
                {
                    headerBorder.Visibility = config.ShowHeader ? Visibility.Visible : Visibility.Collapsed;
                }
                
                // Adjust Grid row height
                if (mainGrid != null && mainGrid.RowDefinitions.Count > 0)
                {
                    mainGrid.RowDefinitions[0].Height = config.ShowHeader ? new GridLength(50) : new GridLength(0);
                }
                
                // Find header icon and text elements
                var headerIcon = this.FindName("HeaderIcon") as FrameworkElement;
                var headerAppName = this.FindName("HeaderAppName") as TextBlock;
                
                // Apply icon visibility and color
                if (headerIcon != null)
                {
                    headerIcon.Visibility = config.ShowIcon ? Visibility.Visible : Visibility.Collapsed;
                    
                    // Apply icon color if it's a Path inside Viewbox
                    if (headerIcon is Viewbox viewbox && viewbox.Child is Canvas canvas)
                    {
                        foreach (var child in canvas.Children)
                        {
                            if (child is System.Windows.Shapes.Path path)
                            {
                                try
                                {
                                    path.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(config.BrandColor));
                                }
                                catch
                                {
                                    // Keep default color if conversion fails
                                }
                            }
                        }
                    }
                }
                
                // Apply app name visibility and color
                if (headerAppName != null)
                {
                    headerAppName.Visibility = config.ShowAppName ? Visibility.Visible : Visibility.Collapsed;
                    headerAppName.Text = config.AppName;
                    
                    try
                    {
                        headerAppName.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(config.BrandColor));
                    }
                    catch
                    {
                        // Keep default color if conversion fails
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[Branding] Applied: ShowHeader={config.ShowHeader}, ShowIcon={config.ShowIcon}, ShowAppName={config.ShowAppName}, AppName={config.AppName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Branding] Error applying config: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle branding config changes
        /// </summary>
        private void OnBrandingChanged(object? sender, EventArgs e)
        {
            // Apply new branding configuration
            ApplyBrandingConfig();
            System.Diagnostics.Debug.WriteLine("[Branding] Configuration changed, reapplied branding");
        }

        /// <summary>
        /// Handle keyboard shortcuts for header toggle
        /// </summary>
        private void Form2_MainDashboard_KeyDown(object sender, KeyEventArgs e)
        {
            // F1: Open Help Tour
            if (e.Key == Key.F1)
            {
                var tour = new HelpTourOverlay(this);
                tour.Show();
                e.Handled = true;
            }
            // F11: Toggle header visibility
            else if (e.Key == Key.F11)
            {
                ToggleHeaderVisibility();
                e.Handled = true;
            }
            // F12: Open Diagnostics / Support Ticket
            else if (e.Key == Key.F12)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    var ticketWindow = new QASmartClass.Forms.InAppSupportTicketWindow();
                    ticketWindow.Owner = this;
                    ticketWindow.ShowDialog();
                }
                else
                {
                    var diagWindow = new QASmartClass.Forms.SelfDiagnosticsWindow();
                    diagWindow.Owner = this;
                    diagWindow.ShowDialog();
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Toggle header visibility and save to config
        /// </summary>
        private void ToggleHeaderVisibility()
        {
            try
            {
                var service = AppBrandingService.Instance;
                service.UpdateConfig(config =>
                {
                    config.ShowHeader = !config.ShowHeader;
                });
                
                System.Diagnostics.Debug.WriteLine($"[Header] Toggled to: {service.Config.ShowHeader}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Header] Toggle error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle ScrollViewer size changed to update scrollbar visibility
        /// </summary>
        private void MainScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollBarVisibility();
        }

        /// <summary>
        /// Handle Canvas size changed to update scrollbar visibility
        /// </summary>
        private void MainInteractiveBoard_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollBarVisibility();
        }

        /// <summary>
        /// Tự động hiện/ẩn scrollbar dựa trên kích thước canvas so với viewport
        /// </summary>
        private void UpdateScrollBarVisibility()
        {
            try
            {
                if (MainScrollViewer == null || MainInteractiveBoard == null)
                    return;

                // Get viewport size (visible area)
                double viewportWidth = MainScrollViewer.ViewportWidth;
                double viewportHeight = MainScrollViewer.ViewportHeight;

                // Get canvas size
                double canvasWidth = MainInteractiveBoard.Width;
                double canvasHeight = MainInteractiveBoard.Height;

                // Add tolerance: only show scrollbar if canvas significantly larger than viewport
                // This prevents scrollbar from appearing due to minor size differences
                const double TOLERANCE = 200; // pixels (increased to prevent showing on startup)

                // Show horizontal scrollbar only if canvas is significantly wider than viewport
                bool needHorizontalScroll = (canvasWidth - viewportWidth) > TOLERANCE;
                MainScrollViewer.HorizontalScrollBarVisibility = needHorizontalScroll 
                    ? ScrollBarVisibility.Auto 
                    : ScrollBarVisibility.Hidden;

                // Show vertical scrollbar only if canvas is significantly taller than viewport
                bool needVerticalScroll = (canvasHeight - viewportHeight) > TOLERANCE;
                MainScrollViewer.VerticalScrollBarVisibility = needVerticalScroll 
                    ? ScrollBarVisibility.Auto 
                    : ScrollBarVisibility.Hidden;

                System.Diagnostics.Debug.WriteLine($"[ScrollBar] Canvas: {canvasWidth}x{canvasHeight}, Viewport: {viewportWidth}x{viewportHeight}, H-Scroll: {needHorizontalScroll}, V-Scroll: {needVerticalScroll}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScrollBar] Update error: {ex.Message}");
            }
        }

        /// <summary>
        /// Thay đổi kích thước canvas (public method)
        /// </summary>
        /// <param name="width">Chiều rộng mới</param>
        /// <param name="height">Chiều cao mới</param>
        public void SetCanvasSize(double width, double height)
        {
            if (MainInteractiveBoard != null)
            {
                MainInteractiveBoard.Width = width;
                MainInteractiveBoard.Height = height;
                System.Diagnostics.Debug.WriteLine($"[Canvas] Size changed to: {width}x{height}");
                
                // Scrollbar sẽ tự động update qua SizeChanged event
            }
        }

        /// <summary>
        /// Lấy kích thước canvas hiện tại
        /// </summary>
        public (double Width, double Height) GetCanvasSize()
        {
            if (MainInteractiveBoard != null)
            {
                return (MainInteractiveBoard.Width, MainInteractiveBoard.Height);
            }
            return (1920, 1080); // Default size
        }

        #endregion

        // ── MOVED TO Form2_MainDashboard.Input.cs (original lines 4287-12614, 8328 lines) ──

        // ── MOVED TO Form2_MainDashboard.STEM.cs (original lines 12616-14697, 2082 lines) ──

        // ── MOVED TO Form2_MainDashboard.ShapeDrawing.cs (original lines 14699-17119, 2421 lines) ──

        // ── MOVED TO Form2_MainDashboard.Selection.cs (original lines 17121-18925, 1805 lines) ──

        // ── MOVED TO Form2_MainDashboard.AI.cs (original lines 18927-20685, 1759 lines) ──

        // ── MOVED TO Form2_MainDashboard.AdvancedFeatures.cs (original lines 20687-21745, 1059 lines) ──

        // ── MOVED TO Form2_MainDashboard.BoardManagement.cs (original lines 21747-22679, 933 lines) ──

        /// <summary>
        /// Enable or disable touch drawing
        /// </summary>
        public void SetTouchDrawingEnabled(bool enabled)
        {
            if (_touchHandler != null)
            {
                _touchHandler.SetEnabled(enabled);
                System.Diagnostics.Debug.WriteLine($"🔄 Touch drawing {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Get touch interaction statistics
        /// </summary>
        public string GetTouchStatistics()
        {
            return _touchHandler?.GetStatistics() ?? "Touch handler not initialized";
        }

        /// <summary>
        /// Set touch interaction mode
        /// </summary>
        public void SetTouchMode(TouchInteractionMode mode)
        {
            if (_touchHandler != null)
            {
                _touchHandler.CurrentMode = mode;
                System.Diagnostics.Debug.WriteLine($"🔄 Touch mode: {mode}");
            }
        }

        // ── MOVED TO Form2_MainDashboard.MediaControls.cs (original lines 22713-25118, 2406 lines) ──

        #region WhiteboardCore Event Handlers

        /// <summary>
        /// Xử lý sự kiện khi ToolManager chuyển đổi công cụ.
        /// Đồng bộ UI (cursor, touch handler, selection state...) theo tool mới.
        /// </summary>
        private void OnWhiteboardToolChanged(object? sender, QASmartClass.WhiteboardCore.Managers.ToolChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"🔄 WhiteboardCore ToolChanged: {e.PreviousMode} → {e.NewMode}");

            // Giai đoạn 3.1: Log event — chưa thay thế logic cũ.
            // Khi migration hoàn tất, block này sẽ:
            // - Đồng bộ cursor (Pen, None, Cross...)
            // - Gọi _touchHandler.SetToolMode(...)
            // - Gọi _selectionManager.DeselectAll() nếu cần
        }

        #endregion

    }

    /// <summary>
    /// Các tính năng nhận diện thông minh trên bảng
    /// </summary>
    public enum SmartActionType
    {
        OCR,
        Translate,
        Read,
        Search,
        AIChat
    }
}
