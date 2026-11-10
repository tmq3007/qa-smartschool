using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_TOUCH_SLIDER: Bộ điều khiển tối ưu hóa toàn diện cho thanh trượt Slider trên màn hình tương tác (SMART TOUCH)
    /// và máy tính thông thường (DESKTOP).
    ///
    /// NGUYÊN NHÂN GỐC RỄ ĐÃ ĐƯỢC ĐIỀU TRA TRIỆT ĐỂ:
    /// 1. Mặc định WPF Slider không có bộ xử lý sự kiện Touch độc lập (TouchDown, TouchMove, TouchUp) mà phụ thuộc vào
    ///    cơ chế mô phỏng chuột (Mouse Promotion) trễ của Windows.
    /// 2. Cử chỉ mặc định Stylus Press-and-Hold và Flicks gây trễ ~500ms, nuốt lần chạm đầu tiên làm cử chỉ chọn/focus,
    ///    khiến người dùng phải chạm lần 1 để chọn núm trượt (Thumb), chạm lần 2 mới kéo được.
    /// 3. IsMoveToPointEnabled mặc định là false, và Thumb có diện tích tiếp xúc ngón tay quá nhỏ.
    ///
    /// GIẢI PHÁP TỐI ƯU TOÀN DIỆN (QC_4.2_BUGFIX_PROTOCOL):
    /// 1. Tự động áp dụng cho TOÀN BỘ 70+ Slider trong toàn bộ ứng dụng qua EventManager.RegisterClassHandler(typeof(Slider)).
    /// 2. Đánh chặn ngay tại giai đoạn Tunneling (PreviewTouchDown) -> Khóa ngón tay (CaptureTouch) -> Cập nhật giá trị tức thì
    ///    ngay tại mili-giây chạm đầu tiên (1 chạm nhận ngay).
    /// 3. Trong suốt quá trình kéo (PreviewTouchMove): bám sát tọa độ ngón tay mượt mà liên tục, ngay cả khi ngón tay trượt lệch
    ///    lên/xuống ra ngoài thanh trượt (nhờ Touch Capture).
    /// 4. Đánh dấu e.Handled = true để ngăn chặn rò rỉ sự kiện chạm xuống ScrollViewer cha (gây giật màn hình) hoặc Canvas vẽ nét bút.
    /// 5. Tính toán giá trị chuẩn xác bằng Track.ValueFromPoint của WPF kết hợp thuật toán dự phòng hình học (Geometric Fallback),
    ///    hỗ trợ đầy đủ: Orientation (Ngang/Dọc), IsDirectionReversed, IsSnapToTickEnabled (TickFrequency & Ticks collection).
    /// 6. Tương thích chuẩn kép DESKTOP & SMART TOUCH: Không can thiệp Mouse events, bật IsMoveToPointEnabled giúp chuột DESKTOP
    ///    cũng nhấp 1 phát nhảy ngay đến điểm chọn.
    /// </summary>
    public static class TouchSliderHelper
    {
        #region Attached Properties

        /// <summary>
        /// Attached property cho phép bật/tắt hành vi TouchSlider trên từng Slider cụ thể nếu cần ngoại lệ (mặc định: true).
        /// </summary>
        public static readonly DependencyProperty EnableTouchSliderProperty =
            DependencyProperty.RegisterAttached(
                "EnableTouchSlider",
                typeof(bool),
                typeof(TouchSliderHelper),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetEnableTouchSlider(DependencyObject obj) =>
            (bool)obj.GetValue(EnableTouchSliderProperty);

        public static void SetEnableTouchSlider(DependencyObject obj, bool value) =>
            obj.SetValue(EnableTouchSliderProperty, value);

        #endregion

        #region Private State

        private static bool _isInitialized = false;
        private static readonly object _syncLock = new();

        // Quản lý đa chạm (Multi-touch Isolation): mỗi Slider chỉ nhận 1 Device Touch/Stylus tại 1 thời điểm
        private static readonly Dictionary<Slider, int> _activeTouchDevices = new();
        private static readonly Dictionary<Slider, int> _activeStylusDevices = new();

        #endregion

        #region Initialization & Class Handler Registration

        /// <summary>
        /// Khởi tạo và đăng ký Global Class Handler cho toàn bộ Slider trong hệ thống.
        /// Được gọi tại App.OnStartup để đảm bảo mọi Slider (XAML hoặc Code-behind) đều tự động kích hoạt.
        /// </summary>
        public static void Initialize()
        {
            lock (_syncLock)
            {
                if (_isInitialized) return;
                _isInitialized = true;
            }

            // 1. Vòng đời nạp / hủy nạp Slider
            EventManager.RegisterClassHandler(
                typeof(Slider),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnSliderLoaded));

            EventManager.RegisterClassHandler(
                typeof(Slider),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnSliderUnloaded));

            // 2. Chuỗi sự kiện Cảm ứng chạm (Touch - Tunneling Preview)
            EventManager.RegisterClassHandler(
                typeof(Slider),
                UIElement.PreviewTouchDownEvent,
                new EventHandler<TouchEventArgs>(OnSliderPreviewTouchDown));

            EventManager.RegisterClassHandler(
                typeof(Slider),
                UIElement.PreviewTouchMoveEvent,
                new EventHandler<TouchEventArgs>(OnSliderPreviewTouchMove));

            EventManager.RegisterClassHandler(
                typeof(Slider),
                UIElement.PreviewTouchUpEvent,
                new EventHandler<TouchEventArgs>(OnSliderPreviewTouchUp));

            EventManager.RegisterClassHandler(
                typeof(Slider),
                UIElement.LostTouchCaptureEvent,
                new EventHandler<TouchEventArgs>(OnSliderLostTouchCapture));

            // 3. Chuỗi sự kiện Bút cảm ứng / Stylus chủ động (Active Stylus Digitizer)
            EventManager.RegisterClassHandler(
                typeof(Slider),
                UIElement.PreviewStylusDownEvent,
                new StylusDownEventHandler(OnSliderPreviewStylusDown));

            EventManager.RegisterClassHandler(
                typeof(Slider),
                UIElement.PreviewStylusMoveEvent,
                new StylusEventHandler(OnSliderPreviewStylusMove));

            EventManager.RegisterClassHandler(
                typeof(Slider),
                UIElement.PreviewStylusUpEvent,
                new StylusEventHandler(OnSliderPreviewStylusUp));

            EventManager.RegisterClassHandler(
                typeof(Slider),
                UIElement.LostStylusCaptureEvent,
                new StylusEventHandler(OnSliderLostStylusCapture));
        }

        /// <summary>
        /// Cấu hình trực tiếp các thuộc tính tối ưu cảm ứng cho một Slider cụ thể.
        /// </summary>
        public static void ConfigureSlider(Slider slider)
        {
            if (slider == null) return;

            // Bật di chuyển tức thì khi nhấp chuột / chạm
            slider.IsMoveToPointEnabled = true;

            // Triệt tiêu hoàn toàn độ trễ nhận dạng cử chỉ của Windows
            Stylus.SetIsPressAndHoldEnabled(slider, false);
            Stylus.SetIsFlicksEnabled(slider, false);

            // Nạp template nếu chưa được nạp
            slider.ApplyTemplate();
        }

        #endregion

        #region Lifecycle Event Handlers

        private static void OnSliderLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Slider slider && GetEnableTouchSlider(slider))
            {
                ConfigureSlider(slider);
            }
        }

        private static void OnSliderUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Slider slider)
            {
                lock (_syncLock)
                {
                    _activeTouchDevices.Remove(slider);
                    _activeStylusDevices.Remove(slider);
                }
            }
        }

        #endregion

        #region Native Touch Handlers (Multi-Touch IFP Screens)

        private static void OnSliderPreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (sender is not Slider slider || !GetEnableTouchSlider(slider)) return;
            if (!slider.IsEnabled || !slider.IsVisible || !slider.IsHitTestVisible) return;
            if (slider.Maximum <= slider.Minimum) return;

            lock (_syncLock)
            {
                _activeTouchDevices[slider] = e.TouchDevice.Id;
            }

            // Chiếm giữ luồng Touch để nhận tọa độ ngay cả khi ngón tay trượt lệch ra khỏi thanh trượt
            slider.CaptureTouch(e.TouchDevice);

            // Cập nhật giá trị tức thì ngay cú chạm đầu tiên (1 chạm là nhận)
            UpdateSliderValueFromTouch(slider, e);

            // Ngăn chặn sự kiện lan xuống ScrollViewer hoặc Canvas vẽ nét bút
            e.Handled = true;
        }

        private static void OnSliderPreviewTouchMove(object sender, TouchEventArgs e)
        {
            if (sender is not Slider slider || !GetEnableTouchSlider(slider)) return;

            bool isOurTouch = false;
            lock (_syncLock)
            {
                if (_activeTouchDevices.TryGetValue(slider, out int activeId) && activeId == e.TouchDevice.Id)
                {
                    isOurTouch = true;
                }
            }

            if (isOurTouch)
            {
                UpdateSliderValueFromTouch(slider, e);
                e.Handled = true;
            }
        }

        private static void OnSliderPreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (sender is not Slider slider) return;

            bool isOurTouch = false;
            lock (_syncLock)
            {
                if (_activeTouchDevices.TryGetValue(slider, out int activeId) && activeId == e.TouchDevice.Id)
                {
                    isOurTouch = true;
                    _activeTouchDevices.Remove(slider);
                }
            }

            if (isOurTouch)
            {
                slider.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
            }
        }

        private static void OnSliderLostTouchCapture(object sender, TouchEventArgs e)
        {
            if (sender is Slider slider)
            {
                lock (_syncLock)
                {
                    _activeTouchDevices.Remove(slider);
                }
            }
        }

        #endregion

        #region Active Stylus Handlers (Digitizer Pen on IFP)

        private static void OnSliderPreviewStylusDown(object sender, StylusDownEventArgs e)
        {
            if (sender is not Slider slider || !GetEnableTouchSlider(slider)) return;

            // Nếu thiết bị là ngón tay (Touch), nhường quyền xử lý cho PreviewTouchDown để hỗ trợ Multi-Touch Device ID
            if (e.StylusDevice?.TabletDevice?.Type == TabletDeviceType.Touch)
                return;

            if (!slider.IsEnabled || !slider.IsVisible || !slider.IsHitTestVisible) return;
            if (slider.Maximum <= slider.Minimum) return;

            int stylusId = e.StylusDevice?.Id ?? 0;
            lock (_syncLock)
            {
                _activeStylusDevices[slider] = stylusId;
            }

            slider.CaptureStylus();
            UpdateSliderValueFromStylus(slider, e);
            e.Handled = true;
        }

        private static void OnSliderPreviewStylusMove(object sender, StylusEventArgs e)
        {
            if (sender is not Slider slider || !GetEnableTouchSlider(slider)) return;
            if (e.StylusDevice?.TabletDevice?.Type == TabletDeviceType.Touch) return;

            bool isOurStylus = false;
            int stylusId = e.StylusDevice?.Id ?? 0;
            lock (_syncLock)
            {
                if (_activeStylusDevices.TryGetValue(slider, out int activeId) && activeId == stylusId)
                {
                    isOurStylus = true;
                }
            }

            if (isOurStylus)
            {
                UpdateSliderValueFromStylus(slider, e);
                e.Handled = true;
            }
        }

        private static void OnSliderPreviewStylusUp(object sender, StylusEventArgs e)
        {
            if (sender is not Slider slider) return;
            if (e.StylusDevice?.TabletDevice?.Type == TabletDeviceType.Touch) return;

            bool isOurStylus = false;
            int stylusId = e.StylusDevice?.Id ?? 0;
            lock (_syncLock)
            {
                if (_activeStylusDevices.TryGetValue(slider, out int activeId) && activeId == stylusId)
                {
                    isOurStylus = true;
                    _activeStylusDevices.Remove(slider);
                }
            }

            if (isOurStylus)
            {
                slider.ReleaseStylusCapture();
                e.Handled = true;
            }
        }

        private static void OnSliderLostStylusCapture(object sender, StylusEventArgs e)
        {
            if (sender is Slider slider)
            {
                lock (_syncLock)
                {
                    _activeStylusDevices.Remove(slider);
                }
            }
        }

        #endregion

        #region Value Calculation & Snapping Engine

        private static void UpdateSliderValueFromTouch(Slider slider, TouchEventArgs e)
        {
            Track? track = FindTrack(slider);
            double calculatedValue;

            if (track != null && track.ActualWidth > 0 && track.ActualHeight > 0)
            {
                try
                {
                    TouchPoint tp = e.GetTouchPoint(track);
                    calculatedValue = track.ValueFromPoint(tp.Position);
                }
                catch
                {
                    TouchPoint tp = e.GetTouchPoint(slider);
                    calculatedValue = CalculateFallbackValue(slider, tp.Position);
                }
            }
            else
            {
                TouchPoint tp = e.GetTouchPoint(slider);
                calculatedValue = CalculateFallbackValue(slider, tp.Position);
            }

            ApplySnappedValue(slider, calculatedValue);
        }

        private static void UpdateSliderValueFromStylus(Slider slider, StylusEventArgs e)
        {
            Track? track = FindTrack(slider);
            double calculatedValue;

            if (track != null && track.ActualWidth > 0 && track.ActualHeight > 0)
            {
                try
                {
                    Point pt = e.GetPosition(track);
                    calculatedValue = track.ValueFromPoint(pt);
                }
                catch
                {
                    Point pt = e.GetPosition(slider);
                    calculatedValue = CalculateFallbackValue(slider, pt);
                }
            }
            else
            {
                Point pt = e.GetPosition(slider);
                calculatedValue = CalculateFallbackValue(slider, pt);
            }

            ApplySnappedValue(slider, calculatedValue);
        }

        /// <summary>
        /// Áp dụng thuật toán bước nhảy SnapToTick và gán giá trị mới vào Slider.
        /// </summary>
        private static void ApplySnappedValue(Slider slider, double rawValue)
        {
            double snappedValue = SnapToTick(slider, rawValue);
            snappedValue = Math.Clamp(snappedValue, slider.Minimum, slider.Maximum);

            if (Math.Abs(slider.Value - snappedValue) > 0.0001)
            {
                slider.Value = snappedValue;
            }
        }

        /// <summary>
        /// Xử lý SnapToTick chuẩn xác: ưu tiên bộ sưu tập Ticks, kế tiếp là TickFrequency.
        /// </summary>
        private static double SnapToTick(Slider slider, double val)
        {
            if (!slider.IsSnapToTickEnabled)
                return val;

            // 1. Ưu tiên bộ sưu tập Ticks tùy chỉnh nếu có
            if (slider.Ticks != null && slider.Ticks.Count > 0)
            {
                double closest = val;
                double minDiff = double.PositiveInfinity;
                foreach (double tick in slider.Ticks)
                {
                    double diff = Math.Abs(tick - val);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        closest = tick;
                    }
                }
                return closest;
            }

            // 2. Snap theo TickFrequency (ví dụ: kích thước nét bút 1, 2, 3...)
            double freq = slider.TickFrequency;
            if (freq > 0)
            {
                double min = slider.Minimum;
                double max = slider.Maximum;
                double offset = val - min;
                double snappedOffset = Math.Round(offset / freq) * freq;
                double snappedVal = min + snappedOffset;
                return Math.Clamp(snappedVal, min, max);
            }

            return val;
        }

        /// <summary>
        /// Thuật toán dự phòng tính toán giá trị khi Visual Tree của Slider chưa dựng xong Track.
        /// </summary>
        private static double CalculateFallbackValue(Slider slider, Point ptInSlider)
        {
            double range = slider.Maximum - slider.Minimum;
            if (range <= 0) return slider.Minimum;

            double ratio;
            if (slider.Orientation == Orientation.Horizontal)
            {
                if (slider.ActualWidth <= 0) return slider.Value;
                ratio = ptInSlider.X / slider.ActualWidth;
            }
            else
            {
                if (slider.ActualHeight <= 0) return slider.Value;
                // Trong WPF Slider dọc, đáy (Bottom) là Minimum (ratio 0), đỉnh (Top) là Maximum (ratio 1)
                ratio = (slider.ActualHeight - ptInSlider.Y) / slider.ActualHeight;
            }

            if (slider.IsDirectionReversed)
            {
                ratio = 1.0 - ratio;
            }

            ratio = Math.Clamp(ratio, 0.0, 1.0);
            return slider.Minimum + (ratio * range);
        }

        /// <summary>
        /// Tìm thành phần PART_Track chuẩn trong ControlTemplate của Slider hoặc duyệt Visual Tree.
        /// </summary>
        private static Track? FindTrack(Slider slider)
        {
            if (slider.Template?.FindName("PART_Track", slider) is Track track)
            {
                return track;
            }
            return FindVisualChild<Track>(slider);
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                    return match;
                var sub = FindVisualChild<T>(child);
                if (sub != null)
                    return sub;
            }
            return null;
        }

        #endregion
    }
}
