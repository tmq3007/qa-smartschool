using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartClass.LearningTools.Controls
{
    /// <summary>
    /// Bàn phím số mini dạng Popup — tự động hiển thị khi TextBox nhận focus.
    /// Dùng cho màn hình tương tác không có bàn phím vật lý.
    /// 
    /// Cách dùng:
    ///   TouchNumPad.Attach(txtCoeffA);          // số nguyên: ▲▼ step 1
    ///   TouchNumPad.Attach(txtCoeffA, 0.1);     // số thực: ▲▼ step 0.1
    ///   TouchNumPad.Attach(txtAngle, 1, 0, 360);// giới hạn 0-360
    /// </summary>
    public static class TouchNumPad
    {
        private static Popup? _popup;
        private static TextBox? _currentTarget;
        private static double _step = 1;
        private static double _min = double.MinValue;
        private static double _max = double.MaxValue;
        private static bool _allowDecimal = false;
        private static bool _allowNegative = true;
        private static PlacementMode _placement = PlacementMode.Bottom;
        private static double _savedHorizontalOffset = 0;
        private static double _savedVerticalOffset = 0;
        private static bool _hasSavedOffset = false;
        private static bool _enableMathKeys = false;
        private static FrameworkElement? _mathPanel;

        public static void Attach(TextBox textBox, double step = 1,
            double min = double.MinValue, double max = double.MaxValue,
            bool allowDecimal = true, bool allowNegative = true,
            PlacementMode placement = PlacementMode.Bottom,
            bool enableMathKeys = false)
        {
            void HandleFocus()
            {
                _step = step;
                _min = min;
                _max = max;
                _allowDecimal = allowDecimal;
                _allowNegative = allowNegative;
                _placement = placement;
                _enableMathKeys = enableMathKeys;
                ShowPopup(textBox);
            }

            textBox.GotFocus += (s, e) => HandleFocus();
            textBox.PreviewMouseLeftButtonUp += (s, e) => HandleFocus();
            textBox.PreviewTouchUp += (s, e) => HandleFocus();

            // Không ẩn khi LostFocus nếu focus chuyển vào popup
            textBox.LostFocus += (s, e) =>
            {
                // Delay nhỏ để kiểm tra focus mới có phải popup không
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_popup != null && !IsPopupFocused())
                        HidePopup();
                }), System.Windows.Threading.DispatcherPriority.Background);
            };
        }

        private static bool IsPopupFocused()
        {
            if (_popup?.Child == null) return false;
            var focused = Keyboard.FocusedElement as DependencyObject;
            if (focused == null) return false;

            // Check if focused element is inside popup
            var parent = focused;
            while (parent != null)
            {
                if (parent == _popup.Child) return true;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return false;
        }

        private static void ShowPopup(TextBox target)
        {
            // Ép buộc WPF thực thi layout pass ngay lập tức để lấy tọa độ thực của TextBox
            target.UpdateLayout();

            if (_popup == null)
                _popup = CreatePopup();

            if (_mathPanel != null)
            {
                _mathPanel.Visibility = _enableMathKeys ? Visibility.Visible : Visibility.Collapsed;
            }

            // Reset vị trí lưu trữ nếu Placement thay đổi hoặc chuyển sang TextBox khác
            if (_popup.Placement != _placement)
            {
                _hasSavedOffset = false;
                _savedHorizontalOffset = 0;
                _savedVerticalOffset = 0;
            }
            else if (_currentTarget != null && _currentTarget != target)
            {
                // Preserve user dragged offset across TextBox targets / new questions
                // Do not reset _hasSavedOffset here
            }

            _popup.Placement = _placement;
            _popup.PlacementTarget = target;
            if (_hasSavedOffset)
            {
                _popup.HorizontalOffset = _savedHorizontalOffset;
                _popup.VerticalOffset = _savedVerticalOffset;
            }
            else
            {
                _popup.HorizontalOffset = 0;
                _popup.VerticalOffset = 0;
            }
            _popup.IsOpen = true;

            // Ghi nhận target hiện tại ở cuối để phục vụ so sánh ở lần gọi tiếp theo
            _currentTarget = target;
        }

        public static void HidePopup()
        {
            if (_popup != null)
                _popup.IsOpen = false;
        }

        private static Popup CreatePopup()
        {
            var popup = new Popup
            {
                Placement = PlacementMode.Bottom,
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
            };

            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 48, 56)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4, 0, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Opacity = 0.3,
                    Color = Colors.Black
                }
            };

            var mainStack = new StackPanel();

            // Title Bar (Draggable & Close)
            var titleBar = new DockPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 33, 40)),
                Height = 32,
                LastChildFill = true,
                Cursor = Cursors.SizeAll
            };
            var btnClose = new Button
            {
                Content = "✕",
                Width = 32, Height = 32,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Focusable = false
            };
            btnClose.Click += (s, e) => HidePopup();
            DockPanel.SetDock(btnClose, Dock.Right);
            titleBar.Children.Add(btnClose);

            var titleText = new TextBlock
            {
                Text = "Bàn phím số",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            titleBar.Children.Add(titleText);
            mainStack.Children.Add(titleBar);

            // Drag-Drop logic
            bool isDragging = false;
            Point dragStartPoint = new Point();
            double startHorz = 0, startVert = 0;

            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (_popup != null)
                {
                    isDragging = true;
                    titleBar.CaptureMouse();
                    dragStartPoint = e.GetPosition(null);
                    startHorz = _popup.HorizontalOffset;
                    startVert = _popup.VerticalOffset;
                }
            };

            titleBar.MouseMove += (s, e) =>
            {
                if (isDragging && _popup != null)
                {
                    var currentPoint = e.GetPosition(null);
                    _popup.HorizontalOffset = startHorz + (currentPoint.X - dragStartPoint.X);
                    _popup.VerticalOffset = startVert + (currentPoint.Y - dragStartPoint.Y);
                }
            };

            titleBar.MouseLeftButtonUp += (s, e) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    titleBar.ReleaseMouseCapture();
                    if (_popup != null)
                    {
                        _savedHorizontalOffset = _popup.HorizontalOffset;
                        _savedVerticalOffset = _popup.VerticalOffset;
                        _hasSavedOffset = true;
                    }
                }
            };

            // Body Grid: Col 0 for Numeric panel, Col 1 for Math panel
            var bodyGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var numericPanel = new StackPanel();
            Grid.SetColumn(numericPanel, 0);
            bodyGrid.Children.Add(numericPanel);

            // Row 1: ▲ increment + display
            var topRow = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            var btnUp = MakeArrowButton("▲", Color.FromRgb(76, 175, 80));
            btnUp.Click += (s, e) => IncrementValue(_step);
            DockPanel.SetDock(btnUp, Dock.Right);
            topRow.Children.Add(btnUp);

            var btnDown = MakeArrowButton("▼", Color.FromRgb(239, 83, 80));
            btnDown.Click += (s, e) => IncrementValue(-_step);
            DockPanel.SetDock(btnDown, Dock.Right);
            topRow.Children.Add(btnDown);
            numericPanel.Children.Add(topRow);

            // Row 2-4: Number grid (1-9)
            var numGrid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            for (int r = 0; r < 4; r++)
                numGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
            for (int c = 0; c < 3; c++)
                numGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });

            // 1-9
            for (int n = 1; n <= 9; n++)
            {
                int row = (n - 1) / 3;
                int col = (n - 1) % 3;
                var btn = MakeNumButton(n.ToString());
                Grid.SetRow(btn, row);
                Grid.SetColumn(btn, col);
                numGrid.Children.Add(btn);
            }

            // Bottom row: ± | 0 | ⌫
            var btnNeg = MakeSpecialButton("±", Color.FromRgb(255, 152, 0));
            btnNeg.Click += (s, e) => ToggleSign();
            Grid.SetRow(btnNeg, 3);
            Grid.SetColumn(btnNeg, 0);
            numGrid.Children.Add(btnNeg);

            var btn0 = MakeNumButton("0");
            Grid.SetRow(btn0, 3);
            Grid.SetColumn(btn0, 1);
            numGrid.Children.Add(btn0);

            var btnBack = MakeSpecialButton("⌫", Color.FromRgb(198, 40, 40));
            btnBack.Click += (s, e) => Backspace();
            Grid.SetRow(btnBack, 3);
            Grid.SetColumn(btnBack, 2);
            numGrid.Children.Add(btnBack);

            numericPanel.Children.Add(numGrid);

            // Bottom: Decimal point + Clear + OK
            var bottomRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };

            var btnDot = MakeActionButton(".", 44, Color.FromRgb(100, 100, 100));
            btnDot.Click += (s, e) => AppendText(".");
            bottomRow.Children.Add(btnDot);

            var btnClear = MakeActionButton("C", 44, Color.FromRgb(198, 40, 40));
            btnClear.Click += (s, e) => ClearText();
            bottomRow.Children.Add(btnClear);

            var btnOk = MakeActionButton("OK ✓", 60, Color.FromRgb(46, 125, 50));
            btnOk.Click += (s, e) =>
            {
                HidePopup();
                // Trigger TextChanged on target to recalculate
                _currentTarget?.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            };
            bottomRow.Children.Add(btnOk);

            numericPanel.Children.Add(bottomRow);

            // Math panel
            var mathPanel = new StackPanel { Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };
            var mathKeys = new[] { "²", "³", "√", "π" };
            foreach (var key in mathKeys)
            {
                var btn = new Button
                {
                    Content = key,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = Brushes.White,
                    Width = 48, Height = 42,
                    Margin = new Thickness(2),
                    Cursor = Cursors.Hand,
                    BorderThickness = new Thickness(0),
                    Focusable = false,
                };
                var accentColor = Color.FromRgb(21, 101, 192);
                btn.Style = CreateRoundButtonStyle(accentColor,
                    Color.FromRgb(
                        (byte)Math.Min(255, accentColor.R + 20),
                        (byte)Math.Min(255, accentColor.G + 20),
                        (byte)Math.Min(255, accentColor.B + 20)));
                btn.Click += (s, e) => InsertCharacterAtCursor(_currentTarget, key);
                mathPanel.Children.Add(btn);
            }
            _mathPanel = mathPanel;
            Grid.SetColumn(mathPanel, 1);
            bodyGrid.Children.Add(mathPanel);

            mainStack.Children.Add(bodyGrid);
            container.Child = mainStack;
            popup.Child = container;
            return popup;
        }

        private static void InsertCharacterAtCursor(TextBox? tb, string text)
        {
            if (tb == null) return;
            int index = tb.SelectionStart;
            tb.Text = (tb.Text ?? "").Insert(index, text);
            tb.SelectionStart = index + text.Length;
            tb.Focus();
        }

        // ─── Button Factories ─────────────────────────────────

        private static Button MakeNumButton(string digit)
        {
            var btn = new Button
            {
                Content = digit,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(60, 63, 72)),
                Width = 48, Height = 42,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Focusable = false,
            };
            // Override template for rounded corners
            btn.Style = CreateRoundButtonStyle(Color.FromRgb(60, 63, 72), Color.FromRgb(80, 83, 92));
            btn.Click += (s, e) => AppendText(digit);
            return btn;
        }

        private static Button MakeSpecialButton(string text, Color bgColor)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.White,
                Width = 48, Height = 42,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Focusable = false,
            };
            btn.Style = CreateRoundButtonStyle(bgColor,
                Color.FromRgb(
                    (byte)Math.Min(255, bgColor.R + 30),
                    (byte)Math.Min(255, bgColor.G + 30),
                    (byte)Math.Min(255, bgColor.B + 30)));
            return btn;
        }

        private static Button MakeArrowButton(string arrow, Color color)
        {
            var btn = new Button
            {
                Content = arrow,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Width = 68, Height = 36,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Focusable = false,
            };
            btn.Style = CreateRoundButtonStyle(color,
                Color.FromRgb(
                    (byte)Math.Min(255, color.R + 20),
                    (byte)Math.Min(255, color.G + 20),
                    (byte)Math.Min(255, color.B + 20)));
            return btn;
        }

        private static Button MakeActionButton(string text, double width, Color color)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.White,
                Width = width, Height = 36,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Focusable = false,
            };
            btn.Style = CreateRoundButtonStyle(color,
                Color.FromRgb(
                    (byte)Math.Min(255, color.R + 20),
                    (byte)Math.Min(255, color.G + 20),
                    (byte)Math.Min(255, color.B + 20)));
            return btn;
        }

        private static Style CreateRoundButtonStyle(Color normal, Color hover)
        {
            var style = new Style(typeof(Button));

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(normal));
            borderFactory.Name = "bd";

            var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(cpFactory);

            template.VisualTree = borderFactory;

            // Hover trigger
            var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush(hover), "bd"));
            template.Triggers.Add(hoverTrigger);

            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        // ─── Text Manipulation ────────────────────────────────

        private static void AppendText(string text)
        {
            if (_currentTarget == null) return;

            if (text == ".")
            {
                if (!_allowDecimal) return;
                if (_currentTarget.Text.Contains(".") || _currentTarget.Text.Contains(",")) return;
                text = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            }

            var current = _currentTarget.Text ?? "";
            _currentTarget.Text = current + text;
            _currentTarget.CaretIndex = _currentTarget.Text.Length;
            _currentTarget.Focus();
        }

        private static void Backspace()
        {
            if (_currentTarget == null) return;
            var text = _currentTarget.Text ?? "";
            if (text.Length > 0)
                _currentTarget.Text = text.Substring(0, text.Length - 1);
            _currentTarget.CaretIndex = _currentTarget.Text.Length;
            _currentTarget.Focus();
        }

        private static void ClearText()
        {
            if (_currentTarget == null) return;
            _currentTarget.Text = "";
            _currentTarget.Focus();
        }

        private static void ToggleSign()
        {
            if (_currentTarget == null || !_allowNegative) return;
            var text = _currentTarget.Text ?? "";
            if (text.StartsWith("-"))
                _currentTarget.Text = text.Substring(1);
            else
                _currentTarget.Text = "-" + text;
            _currentTarget.CaretIndex = _currentTarget.Text.Length;
            _currentTarget.Focus();
        }

        private static void IncrementValue(double delta)
        {
            if (_currentTarget == null) return;

            double current = 0;
            if (!string.IsNullOrWhiteSpace(_currentTarget.Text))
            {
                if (!double.TryParse(_currentTarget.Text.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out current))
                    return;
            }

            current += delta;
            current = Math.Max(_min, Math.Min(_max, current));

            // Format: integers show as int, decimals show appropriately
            if (Math.Abs(_step - Math.Floor(_step)) < 0.0001)
                _currentTarget.Text = ((int)Math.Round(current)).ToString();
            else
                _currentTarget.Text = current.ToString("G10",
                    System.Globalization.CultureInfo.CurrentCulture);

            _currentTarget.CaretIndex = _currentTarget.Text.Length;
            _currentTarget.Focus();
        }
    }
}
