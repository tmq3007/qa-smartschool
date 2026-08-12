using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public partial class FloatingToolbarWindow : Window
    {
        private Point _dragStartPoint;
        private bool _isDragging = false;
        private DateTime _mouseDownTime;
        private const int DRAG_THRESHOLD = 5; // pixels
        private const int CLICK_THRESHOLD_MS = 200; // milliseconds
        private DispatcherTimer? _recDotTimer;

        // Events
        public event EventHandler? BackToMainApp;
        public event EventHandler? PenSelected;
        public event EventHandler? MouseModeRequested;
        public event EventHandler? ShapesSelected;
        public event EventHandler? FillSelected;
        public event EventHandler? ClearAllRequested;
        public event EventHandler? UndoRequested;
        public event EventHandler? RedoRequested;
        public event EventHandler? ScreenshotRequested;
        public event EventHandler? WindowModeToggled;
        public event EventHandler? SelectDisplayRequested;
        public event EventHandler? SelectAreaRequested;
        public event EventHandler? RecordToggled;
        public event EventHandler? DeleteLastStrokeRequested;

        public FloatingToolbarWindow()
        {
            InitializeComponent();
            
            // Position at right edge of screen
            PositionAtScreenEdge();
            
            // Animate record dot
            StartRecDotAnimation();
            
            System.Diagnostics.Debug.WriteLine("✅ FloatingToolbarWindow initialized");
        }
        
        private void StartRecDotAnimation()
        {
            _recDotTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            
            _recDotTimer.Tick += (s, e) =>
            {
                if (recDot != null)
                {
                    recDot.Opacity = recDot.Opacity == 1.0 ? 0.3 : 1.0;
                }
            };
            
            _recDotTimer.Start();
        }

        private void PositionAtScreenEdge()
        {
            // Position at right edge, vertically centered
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 10;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;
        }

        #region Drag & Back Button

        private void btnDragBack_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _dragStartPoint = e.GetPosition(this);
                _mouseDownTime = DateTime.Now;
                _isDragging = false;
                
                // Capture mouse for dragging
                ((UIElement)sender).CaptureMouse();
            }
        }

        private void btnDragBack_Click(object sender, RoutedEventArgs e)
        {
            // Only trigger if it was a click (not a drag)
            var elapsed = (DateTime.Now - _mouseDownTime).TotalMilliseconds;
            
            if (!_isDragging && elapsed < CLICK_THRESHOLD_MS)
            {
                System.Diagnostics.Debug.WriteLine("🔙 Back to main app");
                BackToMainApp?.Invoke(this, EventArgs.Empty);
            }
            
            ((UIElement)sender).ReleaseMouseCapture();
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (e.LeftButton == MouseButtonState.Pressed && this.IsMouseCaptured)
            {
                var currentPoint = e.GetPosition(this);
                var delta = currentPoint - _dragStartPoint;
                
                // Check if moved beyond threshold
                if (Math.Abs(delta.X) > DRAG_THRESHOLD || Math.Abs(delta.Y) > DRAG_THRESHOLD)
                {
                    _isDragging = true;
                    
                    // Move window
                    this.Left += delta.X;
                    this.Top += delta.Y;
                    
                    System.Diagnostics.Debug.WriteLine($"📍 Toolbar moved to ({this.Left:F0}, {this.Top:F0})");
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (_isDragging)
            {
                // Snap to nearest edge
                SnapToNearestEdge();
                _isDragging = false;
            }
        }

        private void SnapToNearestEdge()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // Calculate distance to each edge
            var distToLeft = this.Left;
            var distToRight = screenWidth - (this.Left + this.Width);
            
            // Snap to nearest edge
            if (distToLeft < distToRight)
            {
                // Snap to left
                this.Left = 10;
            }
            else
            {
                // Snap to right
                this.Left = screenWidth - this.Width - 10;
            }
            
            // Keep within vertical bounds
            if (this.Top < 0)
                this.Top = 0;
            if (this.Top + this.Height > screenHeight)
                this.Top = screenHeight - this.Height;
            
            System.Diagnostics.Debug.WriteLine($"📌 Snapped to edge at ({this.Left:F0}, {this.Top:F0})");
        }
        
        /// <summary>
        /// Move toolbar to specified side (left or right)
        /// </summary>
        public void MoveToolbarToSide(bool isLeftSide)
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            
            if (isLeftSide)
            {
                // Move to left edge
                this.Left = 10;
                System.Diagnostics.Debug.WriteLine("📍 Toolbar moved to LEFT edge");
            }
            else
            {
                // Move to right edge (default)
                this.Left = screenWidth - this.Width - 10;
                System.Diagnostics.Debug.WriteLine("📍 Toolbar moved to RIGHT edge");
            }
        }

        #endregion

        #region Button Click Handlers

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Exit Window Mode clicked");
            BackToMainApp?.Invoke(this, EventArgs.Empty);
        }

        private void btnPen_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("✏️ Pen selected");
            PenSelected?.Invoke(this, EventArgs.Empty);
        }

        private void btnMouse_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🖥️ Mouse mode requested");
            MouseModeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnShapes_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📐 Shapes selected");
            ShapesSelected?.Invoke(this, EventArgs.Empty);
        }

        private void btnFill_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎨 Fill selected");
            FillSelected?.Invoke(this, EventArgs.Empty);
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🗑️ Clear all requested");
            ClearAllRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnDeleteStroke_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("❌ Delete last stroke requested");
            DeleteLastStrokeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("↶ Undo requested");
            UndoRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnRedo_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("↷ Redo requested");
            RedoRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📷 Screenshot requested");
            ScreenshotRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnWindowMode_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🪟 Window Mode toggled");
            WindowModeToggled?.Invoke(this, EventArgs.Empty);
        }

        private void btnSelectDisplay_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📺 Select display requested");
            SelectDisplayRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnSelectArea_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📋 Select area requested");
            SelectAreaRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("⏺️ Record toggled");
            RecordToggled?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods
        
        /// <summary>
        /// Set recording state
        /// </summary>
        public void SetRecordingState(bool isRecording)
        {
            if (isRecording)
            {
                btnRecord.Background = new SolidColorBrush(Color.FromRgb(191, 97, 106)); // Red
            }
            else
            {
                btnRecord.Background = new SolidColorBrush(Color.FromRgb(46, 52, 64)); // Dark
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Toggle visual state of Delete Stroke button (highlight when erase mode is active)
        /// </summary>
        public void SetDeleteStrokeButtonActive(bool isActive)
        {
            if (isActive)
            {
                btnDeleteStroke.Background = new SolidColorBrush(Color.FromRgb(191, 97, 106)); // Red
                // Tắt highlight Pen khi bật Erase mode
                btnPen.Background = new SolidColorBrush(Color.FromRgb(46, 52, 64));
            }
            else
            {
                btnDeleteStroke.Background = new SolidColorBrush(Color.FromRgb(46, 52, 64));
            }
        }

        /// <summary>
        /// Update enabled state of Undo/Redo buttons
        /// </summary>
        public void UpdateUndoRedoButtonsState(bool canUndo, bool canRedo)
        {
            if (btnUndo != null)
            {
                btnUndo.IsEnabled = canUndo;
            }
            if (btnRedo != null)
            {
                btnRedo.IsEnabled = canRedo;
            }
        }

        /// <summary>
        /// Set active tool button (highlight selected tool, reset all others)
        /// </summary>
        public void SetActiveToolButton(string toolName)
        {
            var activeColor  = new SolidColorBrush(Color.FromRgb(92, 107, 192));  // #5C6BC0 Indigo
            var mouseColor   = new SolidColorBrush(Color.FromRgb(92, 107, 192));  // #5C6BC0 Indigo
            var defaultColor = Brushes.Transparent;
            var defaultIconColor = new SolidColorBrush(Color.FromRgb(55, 71, 79)); // #37474F Dark Gray

            // Reset tất cả các nút tool
            btnPen.Background         = defaultColor;
            btnMouse.Background       = defaultColor;
            btnDeleteStroke.Background = defaultColor;

            // Reset icon colors
            SetPathFill(btnPen, defaultIconColor);
            SetPathFill(btnMouse, defaultIconColor);
            SetPathFill(btnDeleteStroke, defaultIconColor);

            switch (toolName.ToLower())
            {
                case "pen":
                    btnPen.Background = activeColor;
                    SetPathFill(btnPen, Brushes.White);
                    break;
                case "mouse":
                    btnMouse.Background = mouseColor;
                    SetPathFill(btnMouse, Brushes.White);
                    break;
                case "erase":
                    btnDeleteStroke.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Red
                    SetPathFill(btnDeleteStroke, Brushes.White);
                    break;
                case "none":
                default:
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"🎨 Active tool: {toolName}");
        }

        private void SetPathFill(DependencyObject parent, Brush brush)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Shapes.Path path)
                {
                    path.Fill = brush;
                    return;
                }
                SetPathFill(child, brush);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _recDotTimer?.Stop();
            _recDotTimer = null;
        }

        #endregion
    }
}
