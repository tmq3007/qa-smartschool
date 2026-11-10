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
    /// QC_4.2_TOUCH_SCROLL: Tiện ích tĩnh cung cấp khả năng cuộn cảm ứng và kéo thả (Touch & Drag-to-Scroll)
    /// chuẩn công nghiệp (tương thích EasiNote 5 / iPad) cho ScrollViewer trên cả máy tính lẫn màn hình tương tác IFP.
    /// 
    /// Các nguyên lý kỹ thuật cốt lõi:
    /// 1. Bộ đón bắt Cảm ứng Phần cứng Native (PreviewTouchDown/Move/Up) song song cùng luồng Chuột (PreviewMouse*).
    /// 2. Trọng tài Cử chỉ (Gesture Arbiter with Touch Slop Threshold = 8px):
    ///    - Chạm nhẹ / Nhấc tay tức thì (< 8px): Bảo lưu quyền Click cho nút bấm con bên dưới.
    ///    - Vuốt dứt khoát (>= 8px): ScrollViewer lập tức chiếm quyền (CaptureTouch), tước quyền Click của nút con để chống bấm nhầm.
    /// 3. Quán tính Chuyển động Vật lý (Kinetic Inertia Engine):
    ///    - Tính toán vector vận tốc khi buông tay (Release Velocity).
    ///    - Lướt mượt mà với gia tốc giảm dần hàm mũ (Exponential Friction Deceleration = 0.92).
    ///    - Chạm tay vào bất kỳ lúc nào để hãm phanh tức thì.
    /// 4. Bảo vệ Vùng vẽ Bảng chính (MainScrollViewer), Hộp nhập liệu (TextBox) và Thanh trượt (Slider).
    /// </summary>
    public static class TouchScrollHelper
    {
        private static readonly DependencyProperty IsDragScrollEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsDragScrollEnabled",
                typeof(bool),
                typeof(TouchScrollHelper),
                new PropertyMetadata(false));

        public static bool GetIsDragScrollEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(IsDragScrollEnabledProperty);

        public static void SetIsDragScrollEnabled(DependencyObject obj, bool value) =>
            obj.SetValue(IsDragScrollEnabledProperty, value);

        private struct VelocitySample
        {
            public DateTime Time;
            public Point Point;
        }

        /// <summary>
        /// Bộ máy chuyển động quán tính vật lý (Kinetic Inertia Runner) cho ScrollViewer.
        /// </summary>
        private class InertiaRunner
        {
            private readonly ScrollViewer _scrollViewer;
            private double _velocityX;
            private double _velocityY;
            private DateTime _lastTick;
            private bool _isRunning;

            public InertiaRunner(ScrollViewer scrollViewer)
            {
                _scrollViewer = scrollViewer;
            }

            public void Start(double initialVx, double initialVy)
            {
                Stop();

                // Giới hạn vận tốc cực đại (pixels/sec) để tránh trôi quá nhanh mất kiểm soát
                const double maxSpeed = 3000.0;
                _velocityX = Math.Clamp(initialVx, -maxSpeed, maxSpeed);
                _velocityY = Math.Clamp(initialVy, -maxSpeed, maxSpeed);

                // Ngưỡng vận tốc tối thiểu để kích hoạt quán tính
                if (Math.Abs(_velocityX) < 40 && Math.Abs(_velocityY) < 40)
                {
                    return;
                }

                _lastTick = DateTime.UtcNow;
                _isRunning = true;
                CompositionTarget.Rendering += OnRendering;
            }

            public void Stop()
            {
                if (_isRunning)
                {
                    _isRunning = false;
                    CompositionTarget.Rendering -= OnRendering;
                }
            }

            private void OnRendering(object? sender, EventArgs e)
            {
                if (!_isRunning) return;

                var now = DateTime.UtcNow;
                double dt = (now - _lastTick).TotalSeconds;
                _lastTick = now;

                if (dt <= 0 || dt > 0.1)
                {
                    dt = 0.016; // Phòng ngừa lag spike / treo frame
                }

                // Hệ số ma sát giảm tốc theo hàm mũ (Friction = 0.92 per 60Hz frame)
                double frictionFactor = Math.Pow(0.92, dt * 60.0);
                _velocityX *= frictionFactor;
                _velocityY *= frictionFactor;

                // 1. Cuộn theo chiều dọc
                if (_scrollViewer.ScrollableHeight > 0 && Math.Abs(_velocityY) > 15)
                {
                    double currentV = _scrollViewer.VerticalOffset;
                    double nextV = currentV - (_velocityY * dt);

                    if (nextV <= 0)
                    {
                        _scrollViewer.ScrollToVerticalOffset(0);
                        _velocityY = 0;
                    }
                    else if (nextV >= _scrollViewer.ScrollableHeight)
                    {
                        _scrollViewer.ScrollToVerticalOffset(_scrollViewer.ScrollableHeight);
                        _velocityY = 0;
                    }
                    else
                    {
                        _scrollViewer.ScrollToVerticalOffset(nextV);
                    }
                }
                else
                {
                    _velocityY = 0;
                }

                // 2. Cuộn theo chiều ngang
                if (_scrollViewer.ScrollableWidth > 0 && Math.Abs(_velocityX) > 15)
                {
                    double currentH = _scrollViewer.HorizontalOffset;
                    double nextH = currentH - (_velocityX * dt);

                    if (nextH <= 0)
                    {
                        _scrollViewer.ScrollToHorizontalOffset(0);
                        _velocityX = 0;
                    }
                    else if (nextH >= _scrollViewer.ScrollableWidth)
                    {
                        _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.ScrollableWidth);
                        _velocityX = 0;
                    }
                    else
                    {
                        _scrollViewer.ScrollToHorizontalOffset(nextH);
                    }
                }
                else
                {
                    _velocityX = 0;
                }

                // Kết thúc quán tính khi vận tốc giảm về gần 0
                if (Math.Abs(_velocityX) < 15 && Math.Abs(_velocityY) < 15)
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// Kích hoạt chế độ cuộn chạm và kéo cho một ScrollViewer cụ thể.
        /// </summary>
        public static void EnableTouchAndDragScroll(ScrollViewer? scrollViewer)
        {
            if (scrollViewer == null || GetIsDragScrollEnabled(scrollViewer)) return;
            SetIsDragScrollEnabled(scrollViewer, true);

            // Đảm bảo diện tích nhận diện cảm ứng (Hit-Test) phủ 100% diện tích ScrollViewer
            if (scrollViewer.Background == null)
            {
                scrollViewer.Background = Brushes.Transparent;
            }

            // 1. Cấu hình Native Touch Panning của WPF
            if (scrollViewer.PanningMode == PanningMode.None)
            {
                scrollViewer.PanningMode = PanningMode.Both;
            }
            scrollViewer.PanningDeceleration = 0.002;
            scrollViewer.PanningRatio = 1.0;
            Stylus.SetIsPressAndHoldEnabled(scrollViewer, false);

            var inertiaRunner = new InertiaRunner(scrollViewer);

            // =========================================================================
            // PIPELINE 1: CẢM ỨNG NATIVE TRÊN MÀN HÌNH TƯƠNG TÁC IFP (TouchDevice)
            // =========================================================================
            int activeTouchId = -1;
            bool isTouchPotential = false;
            bool isTouchDragging = false;
            Point startTouchPoint = new Point();
            double startVOffsetTouch = 0;
            double startHOffsetTouch = 0;
            var touchSamples = new List<VelocitySample>();

            scrollViewer.PreviewTouchDown += (sender, e) =>
            {
                // Dừng quán tính ngay khi có ngón tay chạm lại vào màn hình
                inertiaRunner.Stop();

                if (IsInsideExcludedControl(e.OriginalSource as DependencyObject))
                {
                    return;
                }

                activeTouchId = e.TouchDevice.Id;
                startTouchPoint = e.GetTouchPoint(scrollViewer).Position;
                startVOffsetTouch = scrollViewer.VerticalOffset;
                startHOffsetTouch = scrollViewer.HorizontalOffset;
                isTouchPotential = true;
                isTouchDragging = false;

                touchSamples.Clear();
                touchSamples.Add(new VelocitySample { Time = DateTime.UtcNow, Point = startTouchPoint });
            };

            scrollViewer.PreviewTouchMove += (sender, e) =>
            {
                if (!isTouchPotential || e.TouchDevice.Id != activeTouchId)
                {
                    return;
                }

                Point currentPoint = e.GetTouchPoint(scrollViewer).Position;
                double deltaX = currentPoint.X - startTouchPoint.X;
                double deltaY = currentPoint.Y - startTouchPoint.Y;

                // Vùng chết phân định Chạm vs Vuốt: 8 pixel
                if (!isTouchDragging && (Math.Abs(deltaY) > 8 || Math.Abs(deltaX) > 8))
                {
                    isTouchDragging = true;
                    // Chiếm độc quyền TouchDevice: Tước quyền bấm của Button con bên dưới
                    scrollViewer.CaptureTouch(e.TouchDevice);
                }

                if (isTouchDragging)
                {
                    if (scrollViewer.ScrollableHeight > 0)
                    {
                        scrollViewer.ScrollToVerticalOffset(startVOffsetTouch - deltaY);
                    }
                    if (scrollViewer.ScrollableWidth > 0)
                    {
                        scrollViewer.ScrollToHorizontalOffset(startHOffsetTouch - deltaX);
                    }

                    // Lưu vết mẫu vị trí để tính toán vận tốc quán tính (giữ trong 120ms gần nhất)
                    var now = DateTime.UtcNow;
                    touchSamples.Add(new VelocitySample { Time = now, Point = currentPoint });
                    touchSamples.RemoveAll(s => (now - s.Time).TotalMilliseconds > 120);

                    e.Handled = true;
                }
            };

            scrollViewer.PreviewTouchUp += (sender, e) =>
            {
                if (e.TouchDevice.Id != activeTouchId) return;

                isTouchPotential = false;

                if (isTouchDragging)
                {
                    isTouchDragging = false;
                    if (e.TouchDevice.Captured == scrollViewer)
                    {
                        scrollViewer.ReleaseTouchCapture(e.TouchDevice);
                    }
                    e.Handled = true; // Ngăn chặn sự kiện Click vào control bên dưới khi vừa vuốt xong

                    // Tính toán vận tốc buông tay và khởi động Quán tính vật lý
                    if (touchSamples.Count >= 2)
                    {
                        var first = touchSamples[0];
                        var last = touchSamples[touchSamples.Count - 1];
                        double dt = (last.Time - first.Time).TotalSeconds;

                        if (dt > 0.015 && dt < 0.25)
                        {
                            double vx = (last.Point.X - first.Point.X) / dt;
                            double vy = (last.Point.Y - first.Point.Y) / dt;
                            inertiaRunner.Start(vx, vy);
                        }
                    }
                }
            };

            scrollViewer.LostTouchCapture += (sender, e) =>
            {
                if (isTouchDragging)
                {
                    isTouchDragging = false;
                    isTouchPotential = false;
                }
            };

            // =========================================================================
            // PIPELINE 2: CHUỘT / GIẢ LẬP CHUỘT TRÊN MÁY TÍNH BÀN (MouseDevice)
            // =========================================================================
            bool isMousePotential = false;
            bool isMouseDragging = false;
            Point startMousePoint = new Point();
            double startVOffsetMouse = 0;
            double startHOffsetMouse = 0;
            var mouseSamples = new List<VelocitySample>();

            scrollViewer.PreviewMouseDown += (sender, e) =>
            {
                if (e.LeftButton != MouseButtonState.Pressed) return;

                // Dừng quán tính khi click chuột
                inertiaRunner.Stop();

                if (IsInsideExcludedControl(e.OriginalSource as DependencyObject))
                {
                    return;
                }

                startMousePoint = e.GetPosition(scrollViewer);
                startVOffsetMouse = scrollViewer.VerticalOffset;
                startHOffsetMouse = scrollViewer.HorizontalOffset;
                isMousePotential = true;
                isMouseDragging = false;

                mouseSamples.Clear();
                mouseSamples.Add(new VelocitySample { Time = DateTime.UtcNow, Point = startMousePoint });
            };

            scrollViewer.PreviewMouseMove += (sender, e) =>
            {
                if (!isMousePotential || e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }

                Point currentPoint = e.GetPosition(scrollViewer);
                double deltaX = currentPoint.X - startMousePoint.X;
                double deltaY = currentPoint.Y - startMousePoint.Y;

                // Ngưỡng kéo chuột: 8 pixel
                if (!isMouseDragging && (Math.Abs(deltaY) > 8 || Math.Abs(deltaX) > 8))
                {
                    isMouseDragging = true;
                    scrollViewer.CaptureMouse();
                }

                if (isMouseDragging)
                {
                    if (scrollViewer.ScrollableHeight > 0)
                    {
                        scrollViewer.ScrollToVerticalOffset(startVOffsetMouse - deltaY);
                    }
                    if (scrollViewer.ScrollableWidth > 0)
                    {
                        scrollViewer.ScrollToHorizontalOffset(startHOffsetMouse - deltaX);
                    }

                    var now = DateTime.UtcNow;
                    mouseSamples.Add(new VelocitySample { Time = now, Point = currentPoint });
                    mouseSamples.RemoveAll(s => (now - s.Time).TotalMilliseconds > 120);

                    e.Handled = true;
                }
            };

            scrollViewer.PreviewMouseUp += (sender, e) =>
            {
                isMousePotential = false;

                if (isMouseDragging)
                {
                    isMouseDragging = false;
                    scrollViewer.ReleaseMouseCapture();
                    e.Handled = true; // Ngăn kích hoạt Click nhầm

                    if (mouseSamples.Count >= 2)
                    {
                        var first = mouseSamples[0];
                        var last = mouseSamples[mouseSamples.Count - 1];
                        double dt = (last.Time - first.Time).TotalSeconds;

                        if (dt > 0.015 && dt < 0.25)
                        {
                            double vx = (last.Point.X - first.Point.X) / dt;
                            double vy = (last.Point.Y - first.Point.Y) / dt;
                            inertiaRunner.Start(vx, vy);
                        }
                    }
                }
            };

            scrollViewer.MouseLeave += (sender, e) =>
            {
                if (isMouseDragging && !scrollViewer.IsMouseCaptured)
                {
                    isMouseDragging = false;
                    isMousePotential = false;
                }
            };
        }

        /// <summary>
        /// Kích hoạt Scroll Bubbling cho ScrollViewer con để khi cuộn hết nội dung con,
        /// thao tác cuộn con lăn / vuốt sẽ được chuyển tiếp lên ScrollViewer cha.
        /// </summary>
        public static void EnableMouseWheelBubbling(ScrollViewer? childScrollViewer)
        {
            if (childScrollViewer == null) return;

            childScrollViewer.PreviewMouseWheel += (sender, e) =>
            {
                if (e.Handled) return;

                // Kiểm tra nếu ScrollViewer con đã ở đỉnh hoặc đáy
                bool atTop = e.Delta > 0 && childScrollViewer.VerticalOffset <= 0.001;
                bool atBottom = e.Delta < 0 && childScrollViewer.VerticalOffset >= (childScrollViewer.ScrollableHeight - 0.001);

                if (atTop || atBottom)
                {
                    e.Handled = true;
                    var parent = FindVisualParent<ScrollViewer>(childScrollViewer);
                    if (parent != null)
                    {
                        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                        {
                            RoutedEvent = UIElement.MouseWheelEvent,
                            Source = sender
                        };
                        parent.RaiseEvent(eventArg);
                    }
                }
            };
        }

        /// <summary>
        /// Duyệt đệ quy cây giao diện và tự động gắn cuộn cảm ứng cho toàn bộ ScrollViewer trong cửa sổ.
        /// </summary>
        public static void AttachToAllScrollViewers(DependencyObject? root)
        {
            if (root == null) return;

            if (root is Window window)
            {
                if (window.IsLoaded)
                {
                    WireScrollViewersRecursively(window);
                }
                else
                {
                    window.Loaded += (s, e) => WireScrollViewersRecursively(window);
                }
                return;
            }

            WireScrollViewersRecursively(root);
        }

        private static void WireScrollViewersRecursively(DependencyObject parent)
        {
            if (parent == null) return;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ScrollViewer scrollViewer)
                {
                    // QC_4.2_TOUCH_SCROLL: Bảo vệ vùng vẽ Bảng chính hoặc các ScrollViewer có tag NoTouchScroll
                    if ((scrollViewer.Name == "MainScrollViewer" && scrollViewer.PanningMode == PanningMode.None) ||
                        Equals(scrollViewer.Tag, "NoTouchScroll"))
                    {
                        WireScrollViewersRecursively(child);
                        continue;
                    }

                    EnableTouchAndDragScroll(scrollViewer);

                    // Kiểm tra nếu là ScrollViewer con nằm trong 1 ScrollViewer khác
                    var parentScroll = FindVisualParent<ScrollViewer>(scrollViewer);
                    if (parentScroll != null)
                    {
                        EnableMouseWheelBubbling(scrollViewer);
                    }
                }

                WireScrollViewersRecursively(child);
            }
        }

        private static bool IsInsideExcludedControl(DependencyObject? element)
        {
            if (element == null) return false;

            // Nếu là phần tử tài liệu văn bản FlowDocument (Paragraph, Run, Inline, Block...) -> thuộc RichTextBox
            if (element is System.Windows.Documents.TextElement || element is FrameworkContentElement)
            {
                return true;
            }

            // Nếu người dùng nhấn vào ScrollBar (thanh trượt cuộn) hoặc Slider -> cho phép tương tác bình thường
            if (FindVisualParent<ScrollBar>(element) != null) return true;
            if (FindVisualParent<Slider>(element) != null) return true;

            // Không can thiệp nếu nhấn vào TextBox / PasswordBox để đảm bảo gõ chữ, bôi đen văn bản và bật bàn phím ảo
            if (FindVisualParent<TextBoxBase>(element) != null) return true;
            if (FindVisualParent<PasswordBox>(element) != null) return true;

            // Không can thiệp nếu nhấn vào vùng vẽ nét InkCanvas
            if (FindVisualParent<InkCanvas>(element) != null) return true;

            return false;
        }

        public static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                {
                    return parent;
                }

                if (child is Visual || child is System.Windows.Media.Media3D.Visual3D)
                {
                    child = VisualTreeHelper.GetParent(child);
                }
                else if (child is FrameworkContentElement fce)
                {
                    child = fce.Parent ?? LogicalTreeHelper.GetParent(child);
                }
                else
                {
                    child = LogicalTreeHelper.GetParent(child);
                }
            }
            return null;
        }
    }
}
