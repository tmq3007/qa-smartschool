using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartTouch.Models;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// Context Toolbar xuất hiện dưới object được chọn
    /// </summary>
    public partial class ContextToolbar : UserControl
    {
        #region Fields

        private SelectableObject? _attachedObject;
        private bool _moreBtnProcessing = false;

        #endregion

        #region Events

        public event EventHandler? CopyClicked;
        public event EventHandler? RecognizeHandwritingClicked;
        public event EventHandler? LockClicked;
        public event EventHandler? BringToFrontClicked;
        public event EventHandler? SendToBackClicked;
        public event EventHandler? Rotate90Clicked;
        public event EventHandler? FlipHorizontalClicked;
        public event EventHandler? FlipVerticalClicked;
        public event EventHandler? ThicknessClicked;
        public event EventHandler? ColorClicked;
        public event EventHandler<Color>? ColorSelected;
        public event EventHandler<string>? CandidateSelected;
        public event EventHandler? DeleteClicked;
        public event EventHandler? SpotlightClicked;
        public event EventHandler? MoreClicked;

        #endregion

        #region Constructor

        public ContextToolbar()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show toolbar at position
        /// ✅ GĐ1-FIX: Xử lý trường hợp ActualWidth = 0 khi mới chuyển từ Collapsed → Visible
        /// </summary>
        public void ShowAt(Point position, SelectableObject obj)
        {
            _attachedObject = obj;
            
            // Update lock icon
            txtLockIcon.Text = obj.IsLocked ? "🔒" : "🔓";
            
            // Update color preview
            ColorPreview.Color = obj.StrokeColor;
            
            // ✅ GĐ1-FIX: Make visible FIRST so WPF can calculate ActualWidth
            this.Visibility = Visibility.Visible;
            
            // ✅ QC_4.2_BOUNDARY_GUARD: Tự động kẹp biên màn hình (Screen Boundary Clamping)
            // Tránh ContextToolbar bị trôi ra ngoài lề trái hoặc lề dưới màn hình làm che khuất nút công cụ
            Action applyBoundsClamping = () =>
            {
                double width = this.ActualWidth > 0 ? this.ActualWidth : 420;
                double height = this.ActualHeight > 0 ? this.ActualHeight : 44;

                var parent = this.Parent as Canvas;
                double canvasWidth = (parent != null && parent.ActualWidth > 0) ? parent.ActualWidth : SystemParameters.PrimaryScreenWidth;
                double canvasHeight = (parent != null && parent.ActualHeight > 0) ? parent.ActualHeight : SystemParameters.PrimaryScreenHeight;

                double targetLeft = position.X - (width / 2.0);
                double clampedLeft = Math.Max(10, Math.Min(targetLeft, canvasWidth - width - 10));

                double targetTop = position.Y;
                if (targetTop + height > canvasHeight - 10 && _attachedObject != null)
                {
                    targetTop = Math.Max(10, _attachedObject.Position.Y - height - 10);
                }
                double clampedTop = Math.Max(10, Math.Min(targetTop, canvasHeight - height - 10));

                Canvas.SetLeft(this, clampedLeft);
                Canvas.SetTop(this, clampedTop);
            };

            applyBoundsClamping();

            // Sau khi layout pass hoàn thành, tái định vị chính xác với ActualWidth/ActualHeight thực tế
            this.Dispatcher.BeginInvoke(applyBoundsClamping, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Hide toolbar
        /// </summary>
        public void Hide()
        {
            _attachedObject = null;

            // ✅ REVIEW-FIX #1: Reset trạng thái Tier 2 & Popup khi ẩn toolbar
            // Tránh trường hợp giáo viên mở Tier 2 rồi bỏ chọn → lần chọn tiếp vẫn đang mở
            if (ColorPalettePopup != null)
                ColorPalettePopup.IsOpen = false;
            if (HandwritingCandidatesPopup != null)
                HandwritingCandidatesPopup.IsOpen = false;
            if (AdvancedToolsPanel != null)
                AdvancedToolsPanel.Visibility = Visibility.Collapsed;

            this.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Button Click Events

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            CopyClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnRecognizeHandwriting_Click(object sender, RoutedEventArgs e)
        {
            RecognizeHandwritingClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnLock_Click(object sender, RoutedEventArgs e)
        {
            if (_attachedObject != null)
            {
                // Toggle lock icon
                txtLockIcon.Text = _attachedObject.IsLocked ? "🔓" : "🔒";
            }
            LockClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnBringToFront_Click(object sender, RoutedEventArgs e)
        {
            BringToFrontClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnSendToBack_Click(object sender, RoutedEventArgs e)
        {
            SendToBackClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnRotate90_Click(object sender, RoutedEventArgs e)
        {
            Rotate90Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnFlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            FlipHorizontalClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnFlipVertical_Click(object sender, RoutedEventArgs e)
        {
            FlipVerticalClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnThickness_Click(object sender, RoutedEventArgs e)
        {
            ThicknessClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnColor_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            e.Handled = true;
            btnColor_Click(sender, e);
        }

        private void btnColor_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (HandwritingCandidatesPopup != null) HandwritingCandidatesPopup.IsOpen = false;
            if (ColorPalettePopup != null)
            {
                ColorPalettePopup.IsOpen = !ColorPalettePopup.IsOpen;
            }
            ColorClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnDelete_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            e.Handled = true;
            btnDelete_Click(sender, e);
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            DeleteClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Hiển thị Popover các gợi ý dạng hiển thị nhận diện chữ viết tay 1-Click
        /// </summary>
        public void ShowHandwritingCandidates(System.Collections.Generic.List<string> candidates)
        {
            if (candidates == null || candidates.Count == 0 || CandidatesContainer == null)
                return;

            if (ColorPalettePopup != null) ColorPalettePopup.IsOpen = false;

            CandidatesContainer.Children.Clear();

            foreach (var cand in candidates)
            {
                var btn = new Button
                {
                    Content = cand,
                    Margin = new Thickness(3, 0, 3, 0),
                    Padding = new Thickness(10, 4, 10, 4),
                    Background = new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                    Foreground = Brushes.White,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand,
                    Tag = cand
                };

                var template = new ControlTemplate(typeof(Button));
                var border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
                border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
                var cp = new FrameworkElementFactory(typeof(ContentPresenter));
                cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                border.AppendChild(cp);
                template.VisualTree = border;
                btn.Template = template;

                btn.Click += CandidateOption_Click;
                CandidatesContainer.Children.Add(btn);
            }

            HandwritingCandidatesPopup.IsOpen = true;
        }

        private void CandidateOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string selectedText)
            {
                HandwritingCandidatesPopup.IsOpen = false;
                CandidateSelected?.Invoke(this, selectedText);
            }
        }

        private void ColorOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorHex)
            {
                try
                {
                    Color color = (Color)ColorConverter.ConvertFromString(colorHex);
                    UpdateColorPreview(color);
                    ColorPalettePopup.IsOpen = false;
                    ColorSelected?.Invoke(this, color);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ ColorOption_Click error: {ex.Message}");
                }
            }
        }

        private void btnSpotlight_Click(object sender, RoutedEventArgs e)
        {
            SpotlightClicked?.Invoke(this, EventArgs.Empty);
        }

        private void btnMore_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            e.Handled = true;
            ExecuteMoreToggle();
        }

        private void btnMore_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ExecuteMoreToggle();
        }

        /// <summary>
        /// ✅ BUG #10 FIX: Guard flag ngăn double execution khi WPF promote Touch→Mouse→Click
        /// ✅ BUG #13 FIX: MoreClicked fire BÊN TRONG Dispatcher SAU KHI recenter hoàn tất
        /// </summary>
        private void ExecuteMoreToggle()
        {
            // Guard: Ngăn thực thi 2 lần trên thiết bị cảm ứng IFP
            if (_moreBtnProcessing) return;
            _moreBtnProcessing = true;

            try
            {
                // Toggle Tier 2 Advanced Tools Panel (Progressive Disclosure)
                if (AdvancedToolsPanel != null)
                {
                    AdvancedToolsPanel.Visibility = AdvancedToolsPanel.Visibility == Visibility.Visible 
                        ? Visibility.Collapsed 
                        : Visibility.Visible;
                }

                // ✅ Auto-recenter ContextToolbar + Fire MoreClicked SAU KHI layout hoàn tất
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Recenter toolbar
                    var parent = this.Parent as Canvas;
                    if (parent != null && _attachedObject != null)
                    {
                        double objCenterX = _attachedObject.Position.X + (_attachedObject.Size.Width / 2.0);
                        double newLeft = Math.Max(10, Math.Min(
                            objCenterX - (this.ActualWidth / 2.0), 
                            parent.ActualWidth - this.ActualWidth - 10));
                        Canvas.SetLeft(this, newLeft);
                    }

                    // ✅ BUG #13 FIX: Fire AFTER recenter — đảm bảo Canvas.GetLeft chính xác
                    MoreClicked?.Invoke(this, EventArgs.Empty);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            finally
            {
                // Reset guard flag sau khi cả Touch và Click event đã qua
                this.Dispatcher.BeginInvoke(
                    new Action(() => _moreBtnProcessing = false),
                    System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        #endregion

        #region Public Methods - Update UI

        /// <summary>
        /// Update color preview
        /// </summary>
        public void UpdateColorPreview(Color color)
        {
            ColorPreview.Color = color;
        }

        /// <summary>
        /// Update lock icon
        /// </summary>
        public void UpdateLockIcon(bool isLocked)
        {
            txtLockIcon.Text = isLocked ? "🔒" : "🔓";
        }

        #endregion
    }
}
