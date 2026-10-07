using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using QASmartTouch.Forms;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_TOUCH_ACTIVATION: Helper tĩnh toàn diện đảm bảo mọi Window và Control nhận cú chạm đầu tiên
    /// trên màn hình tương tác (IFP) mà không bị OS nuốt (MA_ACTIVATEANDEAT) và không bị trễ do gesture.
    /// </summary>
    public static class TouchActivationHelper
    {
        static TouchActivationHelper()
        {
            try
            {
                // Tự động kích hoạt bàn phím ảo cho bất kỳ ô nhập liệu nào
                // CHỈ KHI nó thuộc phạm vi Bảng viết tương tác (Form2_MainDashboard hoặc các Form2_*)
                EventManager.RegisterClassHandler(
                    typeof(TextBoxBase),
                    UIElement.PreviewTouchDownEvent,
                    new EventHandler<TouchEventArgs>((sender, e) =>
                    {
                        TryShowKeyboardForSmartTouch(sender);
                    }));

                EventManager.RegisterClassHandler(
                    typeof(TextBoxBase),
                    UIElement.PreviewStylusDownEvent,
                    new StylusDownEventHandler((sender, e) =>
                    {
                        TryShowKeyboardForSmartTouch(sender);
                    }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ RegisterClassHandler TextBox error: {ex.Message}");
            }
        }

        private static void TryShowKeyboardForSmartTouch(object? sender)
        {
            if (sender is DependencyObject d)
            {
                var win = Window.GetWindow(d);
                if (win != null && (win is Form2_MainDashboard || win.GetType().Name.StartsWith("Form2_")))
                {
                    TouchKeyboardHelper.ShowTouchKeyboard();
                }
            }
        }

        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_ACTIVATE = 1;

        // DependencyProperty để đánh dấu các control đã được gắn touch, tránh gán trùng lặp
        private static readonly DependencyProperty IsTouchWiredProperty =
            DependencyProperty.RegisterAttached(
                "IsTouchWired",
                typeof(bool),
                typeof(TouchActivationHelper),
                new PropertyMetadata(false));

        public static bool GetIsTouchWired(DependencyObject obj) =>
            (bool)obj.GetValue(IsTouchWiredProperty);

        public static void SetIsTouchWired(DependencyObject obj, bool value) =>
            obj.SetValue(IsTouchWiredProperty, value);

        /// <summary>
        /// Áp dụng hook WM_MOUSEACTIVATE cho một Window, đảm bảo cú chạm đầu tiên
        /// trên màn hình tương tác không bị OS nuốt (MA_ACTIVATEANDEAT).
        /// </summary>
        public static void Apply(Window window)
        {
            if (window == null) return;

            var source = PresentationSource.FromVisual(window) as HwndSource;
            if (source != null)
            {
                source.RemoveHook(WndProc);
                source.AddHook(WndProc);
            }
            else
            {
                window.SourceInitialized += (s, e) =>
                {
                    var src = PresentationSource.FromVisual(window) as HwndSource;
                    src?.RemoveHook(WndProc);
                    src?.AddHook(WndProc);
                };
            }
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                try
                {
                    var src = HwndSource.FromHwnd(hwnd);
                    if (src?.RootVisual is Window win && !win.IsActive)
                    {
                        win.Activate();
                    }
                }
                catch { }
                return new IntPtr(MA_ACTIVATE); // Kích hoạt cửa sổ VÀ KHÔNG NUỐT cú chạm/click
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Tự động áp dụng toàn diện cho bất kỳ Window nào (SubMenu, Dialog, Editor 3D/Biểu đồ):
        /// 1. Hook WM_MOUSEACTIVATE trả về MA_ACTIVATE (1) để chống OS nuốt cú chạm khi kích hoạt cửa sổ.
        /// 2. Duyệt cây Visual Tree khi Loaded để gắn pipeline cảm ứng trực tiếp cho Buttons, Sliders, ComboBoxes...
        /// </summary>
        public static void ApplyToWindow(Window window, FrameworkElement? exclude = null)
        {
            if (window == null) return;

            Apply(window);

            // Tự động thu gọn bàn phím ảo khi cửa sổ đóng lại
            window.Closed += (s, e) =>
            {
                TouchKeyboardHelper.HideTouchKeyboard();
            };

            if (window.IsLoaded)
            {
                WireAllInteractiveControls(window, exclude);
            }
            else
            {
                window.Loaded += (s, e) =>
                {
                    WireAllInteractiveControls(window, exclude);
                };
            }
        }

        /// <summary>
        /// Tương thích ngược: Áp dụng cho SubMenu (chuyển tiếp tới ApplyToWindow).
        /// </summary>
        public static void ApplyToSubMenu(Window window, FrameworkElement? exclude = null)
        {
            ApplyToWindow(window, exclude);
        }

        /// <summary>
        /// Áp dụng cho WPF Popup (như ColorPalettePopup, CandidatesPopup...):
        /// 1. Hook WM_MOUSEACTIVATE trên Win32 PopupRoot trả về MA_ACTIVATE để OS không nuốt cú chạm đầu tiên.
        /// 2. Tự động duyệt cây Visual Tree của Popup.Child khi mở để gắn pipeline cảm ứng cho các nút bấm bên trong.
        /// </summary>
        public static void ApplyToPopup(Popup popup)
        {
            if (popup == null) return;

            popup.Opened += (s, e) =>
            {
                if (popup.Child != null)
                {
                    var source = PresentationSource.FromVisual(popup.Child) as HwndSource;
                    if (source != null)
                    {
                        source.RemoveHook(WndProc);
                        source.AddHook(WndProc);
                    }

                    WireAllInteractiveControls(popup.Child);
                }
            };
        }

        /// <summary>
        /// Duyệt đệ quy cây Visual Tree, gắn pipeline cảm ứng cho ButtonBase, Slider, ComboBox, TabControl, TabItem, Canvas...
        /// </summary>
        public static void WireAllInteractiveControls(DependencyObject parent, FrameworkElement? exclude = null)
        {
            if (parent == null) return;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child == exclude)
                {
                    continue;
                }

                if (child is FrameworkElement fe && (fe.Tag?.ToString() == "DragHandle" || fe.Name == "btnMove" || fe.Cursor == Cursors.SizeAll))
                {
                    Stylus.SetIsPressAndHoldEnabled(fe, false);
                    continue;
                }

                if (child is ButtonBase buttonBase)
                {
                    WireButton(buttonBase);
                }
                else if (child is Slider slider)
                {
                    WireSlider(slider);
                }
                else if (child is ComboBox comboBox)
                {
                    WireComboBox(comboBox);
                }
                else if (child is TabControl tabControl)
                {
                    WireTabControl(tabControl);
                }
                else if (child is TabItem tabItem)
                {
                    WireTabItem(tabItem);
                }
                else if (child is Canvas canvas)
                {
                    Stylus.SetIsPressAndHoldEnabled(canvas, false);
                }
                else if (child is TextBoxBase textBox)
                {
                    WireTextBox(textBox);
                }

                // Đệ quy tiếp vào các con
                WireAllInteractiveControls(child, exclude);
            }
        }

        /// <summary>
        /// Tự động lắng nghe TabControl để wire các controls con khi người dùng chuyển Tab (deferred rendering).
        /// </summary>
        public static void WireTabControl(TabControl tabControl)
        {
            if (tabControl == null || GetIsTouchWired(tabControl)) return;
            SetIsTouchWired(tabControl, true);

            tabControl.SelectionChanged += (s, e) =>
            {
                // Chỉ xử lý nếu sự kiện bắn ra từ chính tabControl này (tránh bubble từ ComboBox con)
                if (e.OriginalSource == tabControl)
                {
                    tabControl.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        WireAllInteractiveControls(tabControl);
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            };
        }

        /// <summary>
        /// Gắn cảm ứng tức thì cho TabItem:
        /// Tắt PressAndHold và chuyển tab ngay khi ngón tay vừa chạm vào tiêu đề tab trên IFP.
        /// </summary>
        public static void WireTabItem(TabItem tabItem)
        {
            if (tabItem == null || GetIsTouchWired(tabItem)) return;
            SetIsTouchWired(tabItem, true);

            tabItem.Focusable = false;
            Stylus.SetIsPressAndHoldEnabled(tabItem, false);

            tabItem.PreviewTouchDown += (s, e) =>
            {
                tabItem.IsSelected = true;
            };

            tabItem.PreviewStylusDown += (s, e) =>
            {
                tabItem.IsSelected = true;
            };
        }

        /// <summary>
        /// Vô hiệu hóa press-and-hold delay và cấu hình tối ưu cảm ứng cho thanh trượt Slider.
        /// </summary>
        public static void WireSlider(Slider slider)
        {
            if (slider == null || GetIsTouchWired(slider)) return;
            SetIsTouchWired(slider, true);

            TouchSliderHelper.ConfigureSlider(slider);
        }

        /// <summary>
        /// Vô hiệu hóa press-and-hold delay cho ComboBox trên màn hình tương tác.
        /// </summary>
        public static void WireComboBox(ComboBox comboBox)
        {
            if (comboBox == null || GetIsTouchWired(comboBox)) return;
            SetIsTouchWired(comboBox, true);

            Stylus.SetIsPressAndHoldEnabled(comboBox, false);
        }

        /// <summary>
        /// Gắn cơ chế tự động mở Bàn phím ảo khi chạm vào TextBox / RichTextBox trên màn hình tương tác.
        /// </summary>
        public static void WireTextBox(TextBoxBase textBox)
        {
            if (textBox == null || GetIsTouchWired(textBox)) return;
            SetIsTouchWired(textBox, true);

            Stylus.SetIsPressAndHoldEnabled(textBox, false);

            // Bật bàn phím ảo khi chạm ngón tay hoặc bút Stylus
            textBox.PreviewTouchDown += (s, e) =>
            {
                TouchKeyboardHelper.ShowTouchKeyboard();
            };

            textBox.PreviewStylusDown += (s, e) =>
            {
                TouchKeyboardHelper.ShowTouchKeyboard();
            };

            // Dự phòng khi click chuột hoặc Tab focus vào
            textBox.GotKeyboardFocus += (s, e) =>
            {
                TouchKeyboardHelper.ShowTouchKeyboard();
            };
        }

        /// <summary>
        /// Gắn Touch/Stylus trực tiếp cho một ButtonBase đơn lẻ (hoặc control tạo động).
        /// </summary>
        public static void WireButton(ButtonBase button)
        {
            if (button == null || GetIsTouchWired(button)) return;
            if (button.Tag?.ToString() == "DragHandle" || button.Name == "btnMove" || button.Cursor == Cursors.SizeAll)
            {
                Stylus.SetIsPressAndHoldEnabled(button, false);
                return;
            }
            SetIsTouchWired(button, true);

            button.Focusable = false;
            Stylus.SetIsPressAndHoldEnabled(button, false);

            Point? touchStart = null;

            button.PreviewTouchDown += (s, e) =>
            {
                touchStart = e.GetTouchPoint(button).Position;
                e.TouchDevice.Capture(button);
                e.Handled = true;
            };

            button.PreviewTouchUp += (s, e) =>
            {
                if (e.TouchDevice.Captured == button)
                {
                    button.ReleaseTouchCapture(e.TouchDevice);
                    TriggerClickIfInside(button, touchStart, e.GetTouchPoint(button).Position);
                }
                touchStart = null;
                e.Handled = true;
            };

            Point? stylusStart = null;

            button.PreviewStylusDown += (s, e) =>
            {
                stylusStart = e.GetPosition(button);
                e.StylusDevice.Capture(button);
                e.Handled = true;
            };

            button.PreviewStylusUp += (s, e) =>
            {
                if (e.StylusDevice.Captured == button)
                {
                    button.ReleaseStylusCapture();
                    TriggerClickIfInside(button, stylusStart, e.GetPosition(button));
                }
                stylusStart = null;
                e.Handled = true;
            };
        }

        private static void TriggerClickIfInside(ButtonBase button, Point? startPoint, Point currentPoint)
        {
            try
            {
                double dist = startPoint.HasValue
                    ? Math.Sqrt(Math.Pow(currentPoint.X - startPoint.Value.X, 2) + Math.Pow(currentPoint.Y - startPoint.Value.Y, 2))
                    : 0;

                // ✅ Dung sai biên ±15px phù hợp diện tích tiếp xúc ngón tay trên màn hình IFP kích thước lớn
                bool isInside = (button.ActualWidth > 0 && button.ActualHeight > 0)
                    ? (currentPoint.X >= -15 && currentPoint.X <= button.ActualWidth + 15 &&
                       currentPoint.Y >= -15 && currentPoint.Y <= button.ActualHeight + 15)
                    : true;

                // Tap (< 35px) và nằm trong phạm vi nút -> Kích hoạt ngay lập tức
                if (dist < 35 && isInside)
                {
                    button.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ExecuteButtonClick(button);
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                }
            }
            catch
            {
                button.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ExecuteButtonClick(button);
                }), System.Windows.Threading.DispatcherPriority.Normal);
            }
        }

        private static void ExecuteButtonClick(ButtonBase button)
        {
            if (button is RadioButton radio)
            {
                radio.IsChecked = true;
                radio.RaiseEvent(new RoutedEventArgs(ToggleButton.CheckedEvent, radio));
            }
            else if (button is ToggleButton toggle)
            {
                if (toggle.IsThreeState)
                {
                    if (toggle.IsChecked == null) toggle.IsChecked = false;
                    else if (toggle.IsChecked == true) toggle.IsChecked = null;
                    else toggle.IsChecked = true;
                }
                else
                {
                    toggle.IsChecked = !(toggle.IsChecked ?? false);
                }

                toggle.RaiseEvent(new RoutedEventArgs(
                    toggle.IsChecked == true ? ToggleButton.CheckedEvent : ToggleButton.UncheckedEvent, 
                    toggle));
            }

            // Kích hoạt ICommand nếu nút bấm có Command binding
            if (button.Command != null && button.Command.CanExecute(button.CommandParameter))
            {
                button.Command.Execute(button.CommandParameter);
            }

            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));
        }
    }
}
