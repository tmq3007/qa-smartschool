using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartClass.LearningTools.Controls
{
    /// <summary>
    /// Bàn phím text mini dạng Popup — dành cho nhập tên/nhãn trên màn hình cảm ứng.
    /// Hiện bàn phím QWERTY thu gọn khi TextBox nhận focus.
    /// 
    /// Cách dùng:
    ///   TouchTextPad.Attach(txtName);                   // QWERTY cơ bản
    ///   TouchTextPad.Attach(txtChem, mode: "chem");     // Bàn phím hóa học
    /// </summary>
    public static class TouchTextPad
    {
        private static Popup? _popup;
        private static TextBox? _currentTarget;
        private static string _mode = "text"; // "text" or "chem"
        private static Popup? _chemPopup;
        private static Popup? _textPopup;

        private static Popup? GetTextPopup()
        {
            if (_textPopup != null)
            {
                if (!_textPopup.Dispatcher.CheckAccess())
                {
                    _textPopup = null;
                }
            }
            return _textPopup;
        }

        private static Popup? GetChemPopup()
        {
            if (_chemPopup != null)
            {
                if (!_chemPopup.Dispatcher.CheckAccess())
                {
                    _chemPopup = null;
                }
            }
            return _chemPopup;
        }

        static TouchTextPad()
        {
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.Activated += (s, e) =>
                    {
                        try
                        {
                            var win = Application.Current.MainWindow;
                            if (win != null)
                            {
                                win.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        win.DpiChanged += (sender, args) =>
                                        {
                                            try
                                            {
                                                UpdatePopupScale(args.NewDpi.DpiScaleX);
                                            }
                                            catch {}
                                        };
                                    }
                                    catch {}
                                }));
                            }
                        }
                        catch {}
                    };
                }
            }
            catch {}
        }

        private static void UpdatePopupScale(double dpiScale)
        {
            double finalScale = Math.Min(1.5, dpiScale);
            var scaleTransform = new ScaleTransform(finalScale, finalScale);
            var textPopup = GetTextPopup();
            if (textPopup?.IsOpen == true && textPopup.Child is Border containerText)
            {
                containerText.LayoutTransform = scaleTransform;
            }
            var chemPopup = GetChemPopup();
            if (chemPopup?.IsOpen == true && chemPopup.Child is Border containerChem)
            {
                containerChem.LayoutTransform = scaleTransform;
            }
        }

        public static void Attach(TextBox textBox, string mode = "text")
        {
            void HandleFocus()
            {
                _currentTarget = textBox;
                _mode = mode;
                ShowPopup(textBox);
            }

            textBox.GotFocus += (s, e) => HandleFocus();
            textBox.PreviewMouseLeftButtonUp += (s, e) => HandleFocus();
            textBox.PreviewTouchUp += (s, e) => HandleFocus();

            textBox.LostFocus += (s, e) =>
            {
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!IsPopupFocused(GetTextPopup()) && !IsPopupFocused(GetChemPopup()))
                    {
                        HideAll();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            };
        }

        private static bool IsPopupFocused(Popup? popup)
        {
            if (popup?.Child == null) return false;
            var focused = Keyboard.FocusedElement as DependencyObject;
            if (focused == null) return false;
            var parent = focused;
            while (parent != null)
            {
                if (parent == popup.Child) return true;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return false;
        }

        private static void ShowPopup(TextBox target)
        {
            HideAll();
            double dpiScale = 1.0;
            try
            {
                dpiScale = VisualTreeHelper.GetDpi(target).DpiScaleX;
            }
            catch {}
            double finalScale = Math.Min(1.5, dpiScale);
            var scaleTransform = new ScaleTransform(finalScale, finalScale);

            if (_mode == "chem")
            {
                var chemPopup = GetChemPopup();
                if (chemPopup == null)
                {
                    _chemPopup = CreateChemPopup();
                    chemPopup = _chemPopup;
                }
                if (chemPopup.Child is Border containerChem)
                {
                    containerChem.LayoutTransform = scaleTransform;
                }
                chemPopup.HorizontalOffset = 0;
                chemPopup.VerticalOffset = 0;
                chemPopup.PlacementTarget = target;
                chemPopup.IsOpen = true;
            }
            else
            {
                var textPopup = GetTextPopup();
                if (textPopup == null)
                {
                    _textPopup = CreateTextPopup();
                    textPopup = _textPopup;
                }
                if (textPopup.Child is Border containerText)
                {
                    containerText.LayoutTransform = scaleTransform;
                }
                textPopup.HorizontalOffset = 0;
                textPopup.VerticalOffset = 0;
                textPopup.PlacementTarget = target;
                textPopup.IsOpen = true;
            }
        }

        private static void HideAll()
        {
            var textPopup = GetTextPopup();
            if (textPopup != null)
            {
                try { textPopup.IsOpen = false; } catch {}
            }
            var chemPopup = GetChemPopup();
            if (chemPopup != null)
            {
                try { chemPopup.IsOpen = false; } catch {}
            }
        }

        // ═══════════════════════════════════════════════════
        //  TEXT KEYBOARD — QWERTY thu gọn cho tên/nhãn
        // ═══════════════════════════════════════════════════
        private static Popup CreateTextPopup()
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
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 4, 0, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12, ShadowDepth = 3, Opacity = 0.3, Color = Colors.Black
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
            btnClose.Click += (s, e) => HideAll();
            DockPanel.SetDock(btnClose, Dock.Right);
            titleBar.Children.Add(btnClose);

            var titleText = new TextBlock
            {
                Text = "Bàn phím chữ",
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
                if (popup != null)
                {
                    isDragging = true;
                    titleBar.CaptureMouse();
                    dragStartPoint = e.GetPosition(null);
                    startHorz = popup.HorizontalOffset;
                    startVert = popup.VerticalOffset;
                }
            };

            titleBar.MouseMove += (s, e) =>
            {
                if (isDragging && popup != null)
                {
                    var currentPoint = e.GetPosition(null);
                    popup.HorizontalOffset = startHorz + (currentPoint.X - dragStartPoint.X);
                    popup.VerticalOffset = startVert + (currentPoint.Y - dragStartPoint.Y);
                }
            };

            titleBar.MouseLeftButtonUp += (s, e) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    titleBar.ReleaseMouseCapture();
                }
            };

            // Row 1: Q W E R T Y U I O P
            mainStack.Children.Add(MakeRow("QWERTYUIOP"));
            // Row 2: A S D F G H J K L
            mainStack.Children.Add(MakeRow("ASDFGHJKL"));
            // Row 3: Z X C V B N M
            mainStack.Children.Add(MakeRow("ZXCVBNM"));

            // Row 4: numbers 0-9
            var numRow = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 2) };
            for (int i = 0; i <= 9; i++)
            {
                numRow.Children.Add(MakeKey(i.ToString(), 30, Color.FromRgb(70, 73, 82)));
            }
            numRow.Children.Add(MakeKey(".", 30, Color.FromRgb(70, 73, 82)));
            numRow.Children.Add(MakeKey(",", 30, Color.FromRgb(70, 73, 82)));
            mainStack.Children.Add(numRow);

            // Row 5: Space, ⌫, C, OK
            var actionRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) };
            var btnSpace = MakeActionKey("Dấu cách", 100, Color.FromRgb(70, 73, 82));
            btnSpace.Click += (s, e) => AppendText(" ");
            actionRow.Children.Add(btnSpace);

            var btnBack = MakeActionKey("⌫", 40, Color.FromRgb(198, 40, 40));
            btnBack.Click += (s, e) => Backspace();
            actionRow.Children.Add(btnBack);

            var btnClear = MakeActionKey("C", 35, Color.FromRgb(255, 143, 0));
            btnClear.Click += (s, e) => ClearText();
            actionRow.Children.Add(btnClear);

            var btnOk = MakeActionKey("OK ✓", 55, Color.FromRgb(46, 125, 50));
            btnOk.Click += (s, e) => HideAll();
            actionRow.Children.Add(btnOk);

            mainStack.Children.Add(actionRow);

            container.Child = mainStack;
            popup.Child = container;
            return popup;
        }

        // ═══════════════════════════════════════════════════
        //  CHEM KEYBOARD — cho nhập PTHH
        // ═══════════════════════════════════════════════════
        private static Popup CreateChemPopup()
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
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 4, 0, 0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 12, ShadowDepth = 3, Opacity = 0.3, Color = Colors.Black
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
            btnClose.Click += (s, e) => HideAll();
            DockPanel.SetDock(btnClose, Dock.Right);
            titleBar.Children.Add(btnClose);

            var titleText = new TextBlock
            {
                Text = "Bàn phím hóa học",
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
                if (popup != null)
                {
                    isDragging = true;
                    titleBar.CaptureMouse();
                    dragStartPoint = e.GetPosition(null);
                    startHorz = popup.HorizontalOffset;
                    startVert = popup.VerticalOffset;
                }
            };

            titleBar.MouseMove += (s, e) =>
            {
                if (isDragging && popup != null)
                {
                    var currentPoint = e.GetPosition(null);
                    popup.HorizontalOffset = startHorz + (currentPoint.X - dragStartPoint.X);
                    popup.VerticalOffset = startVert + (currentPoint.Y - dragStartPoint.Y);
                }
            };

            titleBar.MouseLeftButtonUp += (s, e) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    titleBar.ReleaseMouseCapture();
                }
            };

            // Label
            mainStack.Children.Add(new TextBlock
            {
                Text = "⚗️ Nguyên tố", FontSize = 10, Foreground = Brushes.Gray,
                Margin = new Thickness(4, 0, 0, 4)
            });

            // Row 1: Common elements
            var row1 = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
            foreach (var el in new[] { "H", "O", "C", "N", "S", "Fe", "Al", "Na", "K", "Ca" })
                row1.Children.Add(MakeKey(el, el.Length > 1 ? 36 : 30, Color.FromRgb(33, 150, 243)));
            mainStack.Children.Add(row1);

            var row2 = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
            foreach (var el in new[] { "Mg", "Zn", "Cu", "Ag", "Ba", "Pb", "Cl", "Mn", "P", "Si" })
                row2.Children.Add(MakeKey(el, el.Length > 1 ? 36 : 30, Color.FromRgb(0, 137, 123)));
            mainStack.Children.Add(row2);

            // Row 3: Numbers
            mainStack.Children.Add(new TextBlock
            {
                Text = "🔢 Số & Ký hiệu", FontSize = 10, Foreground = Brushes.Gray,
                Margin = new Thickness(4, 4, 0, 4)
            });

            var numRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
            for (int i = 0; i <= 9; i++)
                numRow.Children.Add(MakeKey(i.ToString(), 30, Color.FromRgb(70, 73, 82)));
            mainStack.Children.Add(numRow);

            // Row 4: Operators
            var opRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
            foreach (var op in new[] { "+", "->", "(", ")", "." })
                opRow.Children.Add(MakeKey(op, op.Length > 1 ? 38 : 30, Color.FromRgb(255, 143, 0)));
            mainStack.Children.Add(opRow);

            // Row 5: Actions
            var actionRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };

            var btnSpace = MakeActionKey("␣", 40, Color.FromRgb(70, 73, 82));
            btnSpace.Click += (s, e) => AppendText(" ");
            actionRow.Children.Add(btnSpace);

            var btnBack = MakeActionKey("⌫", 40, Color.FromRgb(198, 40, 40));
            btnBack.Click += (s, e) => Backspace();
            actionRow.Children.Add(btnBack);

            var btnClear = MakeActionKey("C", 35, Color.FromRgb(198, 40, 40));
            btnClear.Click += (s, e) => ClearText();
            actionRow.Children.Add(btnClear);

            var btnOk = MakeActionKey("OK ✓", 55, Color.FromRgb(46, 125, 50));
            btnOk.Click += (s, e) => HideAll();
            actionRow.Children.Add(btnOk);

            mainStack.Children.Add(actionRow);

            container.Child = mainStack;
            popup.Child = container;
            return popup;
        }

        // ─── Helpers ──────────────────────────────────────

        private static WrapPanel MakeRow(string chars)
        {
            var panel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            };
            foreach (char c in chars)
                panel.Children.Add(MakeKey(c.ToString(), 30, Color.FromRgb(60, 63, 72)));
            return panel;
        }

        private static Button MakeKey(string text, double width, Color bgColor)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = text.Length > 1 ? 12 : 15,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.White,
                Width = width, Height = 34,
                Margin = new Thickness(1),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Focusable = false,
            };
            btn.Style = CreateRoundStyle(bgColor);
            btn.Click += (s, e) => AppendText(text);
            return btn;
        }

        private static Button MakeActionKey(string text, double width, Color bgColor)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.White,
                Width = width, Height = 34,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Focusable = false,
            };
            btn.Style = CreateRoundStyle(bgColor);
            return btn;
        }

        private static Style CreateRoundStyle(Color normal)
        {
            var hover = Color.FromRgb(
                (byte)Math.Min(255, normal.R + 25),
                (byte)Math.Min(255, normal.G + 25),
                (byte)Math.Min(255, normal.B + 25));

            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(normal));
            border.Name = "bd";

            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hover), "bd"));
            template.Triggers.Add(trigger);

            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        // ─── Text Manipulation ────────────────────────────

        private static void AppendText(string text)
        {
            if (_currentTarget == null) return;
            var current = _currentTarget.Text ?? "";
            int caret = _currentTarget.CaretIndex;
            _currentTarget.Text = current.Insert(caret, text);
            _currentTarget.CaretIndex = caret + text.Length;
            _currentTarget.Focus();
        }

        private static void Backspace()
        {
            if (_currentTarget == null) return;
            var text = _currentTarget.Text ?? "";
            int caret = _currentTarget.CaretIndex;
            if (caret > 0 && text.Length > 0)
            {
                _currentTarget.Text = text.Remove(caret - 1, 1);
                _currentTarget.CaretIndex = caret - 1;
            }
            _currentTarget.Focus();
        }

        private static void ClearText()
        {
            if (_currentTarget == null) return;
            _currentTarget.Text = "";
            _currentTarget.Focus();
        }

        public static void Detach(TextBox textBox)
        {
            if (_currentTarget == textBox)
            {
                _currentTarget = null;
                HideAll();
            }
        }
    }
}
