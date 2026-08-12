using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Serilog;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// FloatingTouchKeyboard - Bàn phím cảm ứng nổi tùy biến cao với 3 Tab phím Sư phạm,
    /// hỗ trợ kéo di chuyển quanh màn hình và nút Thoát 1-Click
    /// </summary>
    public partial class FloatingTouchKeyboard : UserControl
    {
        #region Fields

        private bool _isShiftActive = false;
        private Point _dragStartPoint;
        private bool _isDraggingHeader = false;

        // m1-FIX: Stack lưu lịch sử nhập ký tự — Backspace xóa nguyên cụm ký hiệu khoa học
        private readonly Stack<string> _inputHistory = new Stack<string>();

        // [LOI_VID_53] SafeFocusScroll: Lưu trạng thái dịch Canvas
        private double _currentCanvasShift = 0;
        private Canvas? _parentCanvas;
        private TranslateTransform? _canvasTranslate;

        // [LOI_VID_53] Auto-dismiss debounce timer
        private DispatcherTimer? _autoDismissTimer;
        private const int AUTO_DISMISS_DELAY_MS = 500;

        // [LOI_VID_53] TextBox đang được edit (reference để auto-dismiss)
        private FrameworkElement? _currentFocusedElement;

        #endregion

        #region Events

        public event EventHandler<string>? TextConfirmed;
        public event EventHandler? KeyboardClosed;

        #endregion

        #region Constructor

        public FloatingTouchKeyboard()
        {
            InitializeComponent();
            QASmartTouch.Helpers.InputValidationHelper.ApplyTouchIsolation(this);
            this.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// [LOI_VID_53] Hiển thị bàn phím với vị trí thông minh — không che lấp TextBox.
        /// Tự tính toán SafeFocusScroll nếu cần thiết.
        /// </summary>
        /// <param name="initialText">Nội dung ban đầu của TextBox</param>
        /// <param name="position">Vị trí mong muốn của bàn phím trên Canvas</param>
        /// <param name="focusedElement">[LOI_VID_53] Element đang được edit (để tính SafeFocusScroll)</param>
        public void ShowWithText(string initialText, Point position, FrameworkElement? focusedElement = null)
        {
            txtInput.Text = initialText ?? string.Empty;
            txtInput.SelectionStart = txtInput.Text.Length;

            // m1-FIX: Reset input history khi mở bàn phím mới
            _inputHistory.Clear();

            // m2-FIX: Luôn reset Tab về QWERTY khi mở lại bàn phím
            panelQwerty.Visibility = Visibility.Visible;
            panelMath.Visibility = Visibility.Collapsed;
            panelScience.Visibility = Visibility.Collapsed;
            SetTabColors(tabQwerty);

            // Reset Shift state
            _isShiftActive = false;

            // [LOI_VID_53] Lưu reference element đang edit
            _currentFocusedElement = focusedElement;

            // [LOI_VID_53] Tính vị trí thông minh
            Point safePosition = CalculateSafePosition(position, focusedElement);

            Canvas.SetLeft(this, safePosition.X);
            Canvas.SetTop(this, safePosition.Y);

            this.Visibility = Visibility.Visible;
            txtInput.Focus();

            // [LOI_VID_53] Setup auto-dismiss timer
            SetupAutoDismissTimer();

            // [LOI_VID_53] Khởi động SafeFocusScroll nếu cần
            ApplySafeFocusScroll(focusedElement);

            Log.Debug("[LOI_VID_53] Keyboard shown at ({X}, {Y})",
                safePosition.X, safePosition.Y);
        }

        /// <summary>
        /// [LOI_VID_53] Ẩn bàn phím và khôi phục Canvas về vị trí gốc.
        /// </summary>
        public void HideKeyboard()
        {
            // [LOI_VID_53] Khôi phục Canvas shift
            RestoreCanvasPosition();

            // [LOI_VID_53] Dừng auto-dismiss timer
            _autoDismissTimer?.Stop();
            _currentFocusedElement = null;

            this.Visibility = Visibility.Collapsed;
            KeyboardClosed?.Invoke(this, EventArgs.Empty);

            Log.Debug("[LOI_VID_53] Keyboard hidden, canvas restored");
        }

        /// <summary>
        /// [LOI_VID_53] Auto-dismiss: Gọi khi chạm vào Canvas (ngoài TextBox).
        /// Sử dụng debounce 500ms để tránh đóng khi đang chạm phím.
        /// </summary>
        public void RequestAutoDismiss()
        {
            if (Visibility != Visibility.Visible) return;

            if (_autoDismissTimer == null)
            {
                SetupAutoDismissTimer();
            }
            _autoDismissTimer!.Stop();
            _autoDismissTimer.Start();

            Log.Debug("[LOI_VID_53] Auto-dismiss requested (debounce {Ms}ms)", AUTO_DISMISS_DELAY_MS);
        }

        /// <summary>
        /// [LOI_VID_53] Cancel auto-dismiss (khi focus chuyển sang TextBox khác).
        /// </summary>
        public void CancelAutoDismiss()
        {
            _autoDismissTimer?.Stop();
        }

        /// <summary>
        /// [LOI_VID_53] Kiểm tra keyboard đang hiển thị hay không.
        /// </summary>
        public bool IsKeyboardVisible => Visibility == Visibility.Visible;

        #endregion

        #region Header Dragging (Move Keyboard)

        private void DragHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                _isDraggingHeader = true;
                _dragStartPoint = e.GetPosition(this.Parent as UIElement);
                DragHeader.CaptureMouse();
                DragHeader.MouseMove += DragHeader_MouseMove;
                DragHeader.MouseLeftButtonUp += DragHeader_MouseLeftButtonUp;
                e.Handled = true;
            }
        }

        private void DragHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingHeader && this.Parent is Canvas canvas)
            {
                Point current = e.GetPosition(canvas);
                double deltaX = current.X - _dragStartPoint.X;
                double deltaY = current.Y - _dragStartPoint.Y;

                double newLeft = Canvas.GetLeft(this) + deltaX;
                double newTop = Canvas.GetTop(this) + deltaY;

                Canvas.SetLeft(this, Math.Max(0, newLeft));
                Canvas.SetTop(this, Math.Max(0, newTop));

                _dragStartPoint = current;
            }
        }

        private void DragHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingHeader)
            {
                _isDraggingHeader = false;
                DragHeader.ReleaseMouseCapture();
                DragHeader.MouseMove -= DragHeader_MouseMove;
                DragHeader.MouseLeftButtonUp -= DragHeader_MouseLeftButtonUp;
                e.Handled = true;
            }
        }

        #endregion

        #region Button & Tab Click Handlers

        private void Key_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string keyText)
            {
                string textToInsert = _isShiftActive ? keyText.ToUpper() : keyText;

                // m1-FIX: Ghi lại cụm ký tự vừa nhập vào stack lịch sử
                _inputHistory.Push(textToInsert);

                txtInput.Text += textToInsert;
                txtInput.SelectionStart = txtInput.Text.Length;
            }
        }

        private void btnBackspace_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtInput.Text)) return;

            // m1-FIX: Xóa nguyên cụm ký tự vừa nhập (VD: "H₂O", "CO₂", "m/s")
            if (_inputHistory.Count > 0)
            {
                string lastInput = _inputHistory.Pop();
                if (txtInput.Text.EndsWith(lastInput))
                {
                    txtInput.Text = txtInput.Text.Substring(0, txtInput.Text.Length - lastInput.Length);
                }
                else
                {
                    // Fallback: xóa 1 ký tự cuối nếu text đã bị sửa thủ công
                    txtInput.Text = txtInput.Text.Substring(0, txtInput.Text.Length - 1);
                }
            }
            else
            {
                // Fallback: xóa 1 ký tự cuối
                txtInput.Text = txtInput.Text.Substring(0, txtInput.Text.Length - 1);
            }

            txtInput.SelectionStart = txtInput.Text.Length;
        }

        private void btnSpace_Click(object sender, RoutedEventArgs e)
        {
            _inputHistory.Push(" ");
            txtInput.Text += " ";
            txtInput.SelectionStart = txtInput.Text.Length;
        }

        private void btnShift_Click(object sender, RoutedEventArgs e)
        {
            _isShiftActive = !_isShiftActive;
            if (sender is Button btn)
            {
                btn.Background = _isShiftActive ? new SolidColorBrush(Color.FromRgb(52, 152, 219)) : new SolidColorBrush(Color.FromRgb(242, 243, 244));
                btn.Foreground = _isShiftActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(44, 62, 80));
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string confirmedText = txtInput.Text;
            this.Visibility = Visibility.Collapsed;
            TextConfirmed?.Invoke(this, confirmedText);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            HideKeyboard();
        }

        private void tabQwerty_Click(object sender, RoutedEventArgs e)
        {
            panelQwerty.Visibility = Visibility.Visible;
            panelMath.Visibility = Visibility.Collapsed;
            panelScience.Visibility = Visibility.Collapsed;
            SetTabColors(tabQwerty);
        }

        private void tabMath_Click(object sender, RoutedEventArgs e)
        {
            panelQwerty.Visibility = Visibility.Collapsed;
            panelMath.Visibility = Visibility.Visible;
            panelScience.Visibility = Visibility.Collapsed;
            SetTabColors(tabMath);
        }

        private void tabScience_Click(object sender, RoutedEventArgs e)
        {
            panelQwerty.Visibility = Visibility.Collapsed;
            panelMath.Visibility = Visibility.Collapsed;
            panelScience.Visibility = Visibility.Visible;
            SetTabColors(tabScience);
        }

        private void SetTabColors(Button activeTab)
        {
            tabQwerty.Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141));
            tabMath.Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141));
            tabScience.Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141));

            activeTab.Foreground = new SolidColorBrush(Color.FromRgb(52, 152, 219));
        }

        #endregion

        // ═══════════════════════════════════════════════════════
        //  [LOI_VID_53] SAFE FOCUS SCROLL
        //  Thuật toán đảm bảo TextBox không bị bàn phím ảo che lấp.
        //  Công thức: ΔY = max(0, Y_tb + H_tb - (H_screen - H_keyboard - P_margin))
        // ═══════════════════════════════════════════════════════

        #region SafeFocusScroll

        /// <summary>
        /// [LOI_VID_53] Tính vị trí an toàn cho bàn phím — không che lấp element đang edit.
        /// Nếu element ở nửa dưới màn hình, đặt keyboard PHÍA TRÊN element.
        /// Nếu element ở nửa trên, đặt keyboard PHÍA DƯỚI.
        /// </summary>
        private Point CalculateSafePosition(Point requestedPosition, FrameworkElement? focusedElement)
        {
            if (focusedElement == null || Parent is not Canvas canvas)
                return requestedPosition;

            try
            {
                double canvasHeight = canvas.ActualHeight;
                double keyboardHeight = this.ActualHeight > 0 ? this.ActualHeight : 350; // Estimate
                double keyboardWidth = this.ActualWidth > 0 ? this.ActualWidth : 800;

                double elementTop = Canvas.GetTop(focusedElement);
                double elementHeight = focusedElement.ActualHeight;
                if (double.IsNaN(elementTop)) elementTop = requestedPosition.Y;

                double elementBottom = elementTop + elementHeight;
                double screenMidY = canvasHeight / 2;

                double safeY;
                if (elementBottom > screenMidY)
                {
                    // Element ở nửa dưới → đặt keyboard PHÍA TRÊN element
                    safeY = Math.Max(20, elementTop - keyboardHeight - 15);
                }
                else
                {
                    // Element ở nửa trên → đặt keyboard PHÍA DƯỚI
                    safeY = elementBottom + 15;
                }

                // Đảm bảo keyboard không vượt ngoài canvas
                safeY = Math.Max(10, Math.Min(safeY, canvasHeight - keyboardHeight - 10));

                // X: giữ vị trí yêu cầu nhưng không tràn canvas
                double safeX = Math.Max(10, Math.Min(requestedPosition.X,
                    canvas.ActualWidth - keyboardWidth - 10));

                return new Point(safeX, safeY);
            }
            catch (Exception ex)
            {
                Log.Warning("[LOI_VID_53] CalculateSafePosition error: {Err}", ex.Message);
                return requestedPosition;
            }
        }

        /// <summary>
        /// [LOI_VID_53] Áp dụng SafeFocusScroll — dịch Canvas lên nếu keyboard vẫn che element.
        /// Giới hạn dịch tối đa 40% chiều cao Canvas (theo phản biện TS. Trần Thị Hương).
        /// </summary>
        private void ApplySafeFocusScroll(FrameworkElement? focusedElement)
        {
            if (focusedElement == null) return;

            try
            {
                _parentCanvas = this.Parent as Canvas;
                if (_parentCanvas == null) return;

                double canvasHeight = _parentCanvas.ActualHeight;
                double keyboardTop = Canvas.GetTop(this);
                double keyboardHeight = this.ActualHeight > 0 ? this.ActualHeight : 350;
                double keyboardBottom = keyboardTop + keyboardHeight;

                double elementTop = Canvas.GetTop(focusedElement);
                double elementHeight = focusedElement.ActualHeight;
                if (double.IsNaN(elementTop)) return;

                double elementBottom = elementTop + elementHeight;
                double safeMargin = 20;

                // Tính ΔY = overlap giữa keyboard và element
                double deltaY = 0;
                if (elementBottom > keyboardTop - safeMargin && elementTop < keyboardBottom)
                {
                    // Element bị che → cần dịch canvas
                    deltaY = elementBottom - (keyboardTop - safeMargin);
                }

                if (deltaY <= 0) return; // Không cần dịch

                // Giới hạn dịch tối đa 40% canvas height
                double maxShift = canvasHeight * 0.4;
                deltaY = Math.Min(deltaY, maxShift);

                // [CAI_TIEN_VID_09] Animation dịch mượt 200ms
                _canvasTranslate = _parentCanvas.RenderTransform as TranslateTransform
                                    ?? new TranslateTransform();
                _parentCanvas.RenderTransform = _canvasTranslate;

                var shiftAnim = new DoubleAnimation(
                    _canvasTranslate.Y, -deltaY, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                _canvasTranslate.BeginAnimation(TranslateTransform.YProperty, shiftAnim);

                _currentCanvasShift = deltaY;

                Log.Information("[LOI_VID_53] Canvas shifted up {Delta}px (max={Max}px)",
                    deltaY, maxShift);
            }
            catch (Exception ex)
            {
                Log.Warning("[LOI_VID_53] ApplySafeFocusScroll error: {Err}", ex.Message);
            }
        }

        /// <summary>
        /// [LOI_VID_53] Khôi phục Canvas về vị trí gốc với animation mượt.
        /// </summary>
        private void RestoreCanvasPosition()
        {
            if (_currentCanvasShift <= 0 || _canvasTranslate == null) return;

            try
            {
                // [CAI_TIEN_VID_09] Animation khôi phục mượt 200ms
                var restoreAnim = new DoubleAnimation(
                    _canvasTranslate.Y, 0, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                _canvasTranslate.BeginAnimation(TranslateTransform.YProperty, restoreAnim);

                Log.Information("[LOI_VID_53] Canvas restored from {Delta}px shift",
                    _currentCanvasShift);

                _currentCanvasShift = 0;
            }
            catch (Exception ex)
            {
                Log.Warning("[LOI_VID_53] RestoreCanvasPosition error: {Err}", ex.Message);
                // Fallback: reset trực tiếp không animation
                if (_canvasTranslate != null)
                {
                    _canvasTranslate.BeginAnimation(TranslateTransform.YProperty, null);
                    _canvasTranslate.Y = 0;
                }
                _currentCanvasShift = 0;
            }
        }

        #endregion

        #region Auto-Dismiss

        /// <summary>
        /// [LOI_VID_53] Setup debounce timer cho auto-dismiss.
        /// Cancel pending dismiss nếu focus chuyển sang TextBox khác.
        /// </summary>
        private void SetupAutoDismissTimer()
        {
            if (_autoDismissTimer != null) return;

            _autoDismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AUTO_DISMISS_DELAY_MS)
            };
            _autoDismissTimer.Tick += (s, e) =>
            {
                _autoDismissTimer.Stop();
                if (Visibility == Visibility.Visible)
                {
                    Log.Debug("[LOI_VID_53] Auto-dismiss triggered");
                    HideKeyboard();
                }
            };
        }

        #endregion
    }
}
