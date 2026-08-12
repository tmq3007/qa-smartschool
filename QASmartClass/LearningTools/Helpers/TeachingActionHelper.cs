using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Serilog;
using System.Linq;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Helpers
{
    /// <summary>
    /// Helper class cung cấp các chức năng giảng dạy (Focus, Unfocus, Bảng trắng, Chiếu)
    /// cho tất cả Learning Tools — tái sử dụng pattern từ LessonEditorPage.
    /// </summary>
    public static class TeachingActionHelper
    {
        /// <summary>Cho phép ghi đè kết quả IsStudent() phục vụ unit test</summary>
        public static bool? IsStudentOverride { get; set; }

        /// <summary>Kiểm tra an toàn xem máy hiện tại có phải là Học sinh không</summary>
        public static bool IsStudent()
        {
            if (IsStudentOverride.HasValue)
                return IsStudentOverride.Value;

            var app = Application.Current as QASmartTouch.App;
            if (app != null && app.UserRoleService?.CurrentRole == QASmartClass.Shared.UserRole.Student)
                return true;

            var args = Environment.GetCommandLineArgs();
            if (args.Any(a => a.Equals("--student", StringComparison.OrdinalIgnoreCase)))
                return true;

            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    foreach (Window win in Application.Current.Windows)
                    {
                        if (win.GetType().Name.Contains("StudentShell"))
                            return true;
                    }
                }
                else
                {
                    bool found = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window win in Application.Current.Windows)
                        {
                            if (win.GetType().Name.Contains("StudentShell"))
                            {
                                found = true;
                                break;
                            }
                        }
                    });
                    if (found) return true;
                }
            }

            return false;
        }

        // ═══════════════════════════════════════════════════════════
        //  SMART SCREEN DISCOVERY
        // ═══════════════════════════════════════════════════════════

        /// <summary>Tìm SmartScreen window (MainDashboard / Form2)</summary>
        public static Window? FindSmartScreenWindow()
        {
            foreach (Window win in Application.Current.Windows)
            {
                var typeName = win.GetType().Name;
                if (typeName.Contains("MainDashboard") || typeName.Contains("Form2"))
                    return win;
            }
            return null;
        }

        /// <summary>Tìm canvas trong SmartScreen</summary>
        public static Canvas? FindSmartScreenCanvas(Window smartScreen)
        {
            return smartScreen.FindName("MainInteractiveBoard") as Canvas;
        }

        // ═══════════════════════════════════════════════════════════
        //  FOCUS / UNFOCUS — Gửi lệnh qua network cho HS
        // ═══════════════════════════════════════════════════════════

        private static System.Windows.Threading.DispatcherTimer? _focusHeartbeatTimer;

        private static void StartFocusHeartbeat()
        {
            if (_focusHeartbeatTimer == null)
            {
                _focusHeartbeatTimer = new System.Windows.Threading.DispatcherTimer();
                _focusHeartbeatTimer.Interval = TimeSpan.FromSeconds(10);
                _focusHeartbeatTimer.Tick += (s, e) =>
                {
                    try
                    {
                        var focusId = QASmartTouch.App.LessonState.ActiveToolFocusId;
                        if (string.IsNullOrEmpty(focusId))
                        {
                            StopFocusHeartbeat();
                            return;
                        }

                        var app = Application.Current as QASmartTouch.App;
                        var net = app?.NetworkService;
                        if (net?.IsBroadcasting == true)
                        {
                            var cmd = $"CMD|TOOL_FOCUS|{focusId}";
                            _ = net.SendCommandAsync(cmd);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Focus heartbeat error: {Err}", ex.Message);
                    }
                };
            }
            _focusHeartbeatTimer.Start();
        }

        private static void StopFocusHeartbeat()
        {
            _focusHeartbeatTimer?.Stop();
        }

        /// <summary>Focus HS — gửi lệnh cho tất cả HS tập trung vào tool</summary>
        public static async void FocusStudents(string toolId)
        {
            try
            {
                if (IsStudent())
                {
                    Log.Warning("Học sinh không thể thực hiện lệnh Focus học sinh.");
                    return;
                }
                var app = Application.Current as QASmartTouch.App;
                var net = app?.NetworkService;

                var cmd = $"CMD|TOOL_FOCUS|{toolId}";
                QASmartTouch.App.LessonState.LastTeacherCommand = cmd;
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
                QASmartTouch.App.LessonState.ActiveToolFocusId = toolId;

                if (net?.IsBroadcasting == true)
                    await net.SendCommandAsync(cmd);
                else
                    app?.RaiseLocalCommand(cmd);

                StartFocusHeartbeat();
                Log.Information("Teacher focused Learning Tool: {ToolId}", toolId);
            }
            catch (Exception ex) { Log.Warning("Focus tool error: {Err}", ex.Message); }
        }

        public static void UnfocusStudents()
        {
            try
            {
                if (IsStudent())
                {
                    Log.Warning("Học sinh không thể thực hiện lệnh Unfocus học sinh.");
                    return;
                }
                var app = Application.Current as QASmartTouch.App;
                var net = app?.NetworkService;

                var cmd = "CMD|TOOL_UNFOCUS";
                QASmartTouch.App.LessonState.LastTeacherCommand = cmd;
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
                QASmartTouch.App.LessonState.ActiveToolFocusId = null;

                if (net?.IsBroadcasting == true)
                    _ = net.SendCommandAsync(cmd);
                else
                    app?.RaiseLocalCommand(cmd);

                StopFocusHeartbeat();
                Log.Information("Teacher unfocused Learning Tool");
            }
            catch (Exception ex) { Log.Warning("Unfocus tool error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════════════
        //  BẢNG TRẮNG — Chụp control và render bitmap dán lên SmartScreen
        // ═══════════════════════════════════════════════════════════

        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int childCount = 0;
            try { childCount = VisualTreeHelper.GetChildrenCount(parent); } catch { }
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        public static async System.Threading.Tasks.Task<bool> SendToWhiteboardAsync(UserControl control, string toolName)
        {
            if (IsStudent())
            {
                Log.Warning("Học sinh không thể chụp gửi lên bảng trắng.");
                return false;
            }
            var targetWin = FindSmartScreenWindow();
            if (targetWin == null)
            {
                MessageBox.Show("Chưa mở SmartScreen.\nHãy chuyển sang SmartScreen trước khi sử dụng chức năng Bảng trắng.",
                    "Bảng Trắng", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var canvas = FindSmartScreenCanvas(targetWin);
            if (canvas == null)
            {
                MessageBox.Show("Không tìm thấy canvas SmartScreen.",
                    "Bảng Trắng", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                double actualW = control.ActualWidth;
                double actualH = control.ActualHeight;
                if (actualW < 1 || actualH < 1)
                {
                    MessageBox.Show("Nội dung chưa sẵn sàng. Hãy thử lại.",
                        "Bảng Trắng", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                BitmapSource? rtb = null;

                // 1. Nếu control implement IWhiteboardCaptureProvider
                if (control is IWhiteboardCaptureProvider provider)
                {
                    rtb = await provider.GetWhiteboardBitmapAsync();
                }

                // 2. Nếu không, quét xem có WebView2 trong visual tree không
                if (rtb == null)
                {
                    var webView = FindVisualChild<Microsoft.Web.WebView2.Wpf.WebView2>(control);
                    if (webView != null && webView.CoreWebView2 != null)
                    {
                        using (var ms = new System.IO.MemoryStream())
                        {
                            await webView.CoreWebView2.CapturePreviewAsync(
                                Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, ms);
                            ms.Position = 0;
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            rtb = bitmap;
                        }
                    }
                }

                // 3. Fallback chụp thường
                if (rtb == null)
                {
                    double scale = 2.0;
                    rtb = RenderVisualUnclipped(control, scale);
                }

                if (rtb == null) return false;

                // Tính kích thước phù hợp canvas
                double maxW = canvas.ActualWidth * 0.8;
                double maxH = canvas.ActualHeight * 0.85;

                // Sử dụng kích thước thực tế của bitmap nếu nó từ WebView2/Provider
                double imgW = rtb.Width;
                double imgH = rtb.Height;

                // Nếu rtb có DPI cao, ta cần đưa về kích thước WPF Device Independent Pixels (DIPs)
                if (rtb is RenderTargetBitmap)
                {
                    imgW = actualW;
                    imgH = actualH;
                }
                else
                {
                    // Cho WebView2 hay ảnh tĩnh bên ngoài
                    imgW = rtb.Width * (96.0 / rtb.DpiX);
                    imgH = rtb.Height * (96.0 / rtb.DpiY);
                }

                double ratio = System.Math.Min(maxW / imgW, maxH / imgH);
                if (ratio > 1) ratio = 1;
                double outW = imgW * ratio;
                double outH = imgH * ratio;

                // Tạo interactive container
                string displayName = toolName;
                if (!displayName.StartsWith("🛠️") && !displayName.StartsWith("📚"))
                {
                    displayName = "🛠️ " + displayName;
                }
                var container = BuildInteractiveContainer(rtb, outW, outH, canvas, displayName);

                double left = System.Math.Max(20, (canvas.ActualWidth - outW) / 2);
                double top = System.Math.Max(40, (canvas.ActualHeight - outH) / 2);
                Canvas.SetLeft(container, left);
                Canvas.SetTop(container, top);
                canvas.Children.Add(container);

                targetWin.Show();
                targetWin.Activate();
                targetWin.WindowState = WindowState.Maximized;

                Log.Information("Learning Tool '{Name}' sent to SmartScreen whiteboard", toolName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Whiteboard error: {Err}", ex.Message);
                MessageBox.Show($"Lỗi: {ex.Message}", "Bảng Trắng",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CHIẾU — Broadcast tool lên SmartScreen dưới dạng ảnh lớn
        // ═══════════════════════════════════════════════════════════

        public static bool BroadcastToScreen(UserControl control, string toolName)
        {
            if (IsStudent())
            {
                Log.Warning("Học sinh không thể thực hiện chiếu lên màn hình.");
                return false;
            }
            var targetWin = FindSmartScreenWindow();
            if (targetWin == null)
            {
                MessageBox.Show("Chưa mở SmartScreen.\nHãy chuyển sang SmartScreen trước.",
                    "Chiếu", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var canvas = FindSmartScreenCanvas(targetWin);
            if (canvas == null)
            {
                MessageBox.Show("Không tìm thấy canvas SmartScreen.",
                    "Chiếu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                double actualW = control.ActualWidth;
                double actualH = control.ActualHeight;
                if (actualW < 1 || actualH < 1) return false;

                // Render cao hơn cho chế độ chiếu (không bị cut-off/parent clipping)
                double scale = 2.0;
                var rtb = RenderVisualUnclipped(control, scale);
                if (rtb == null) return false;

                // Chiều rộng lớn hơn (90% canvas) cho chế độ chiếu
                double maxW = canvas.ActualWidth * 0.92;
                double maxH = canvas.ActualHeight * 0.88;
                double ratio = System.Math.Min(maxW / actualW, maxH / actualH);
                if (ratio > 1) ratio = 1;
                double outW = actualW * ratio;
                double outH = actualH * ratio;

                // Container chiếu có header đặc biệt
                string displayName = toolName;
                if (!displayName.StartsWith("📺") && !displayName.StartsWith("📚"))
                {
                    displayName = "📺 " + displayName;
                }
                var container = BuildBroadcastContainer(rtb, outW, outH, canvas, displayName);

                double left = System.Math.Max(10, (canvas.ActualWidth - outW) / 2);
                double top = System.Math.Max(10, (canvas.ActualHeight - outH - 40) / 2);
                Canvas.SetLeft(container, left);
                Canvas.SetTop(container, top);
                canvas.Children.Add(container);

                targetWin.Show();
                targetWin.Activate();
                targetWin.WindowState = WindowState.Maximized;

                Log.Information("Learning Tool '{Name}' broadcast to SmartScreen", toolName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Broadcast error: {Err}", ex.Message);
                MessageBox.Show($"Lỗi: {ex.Message}", "Chiếu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  CONTAINER BUILDERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo interactive container trên SmartScreen canvas: kéo thả, copy ảnh, xóa.
        /// Tái sử dụng pattern từ LessonEditorPage.BuildInteractiveImageContainer().
        /// </summary>
        private static Grid BuildInteractiveContainer(
            BitmapSource rtb, double imgWidth, double imgHeight,
            Canvas canvas, string toolName)
        {
            var imgElement = new Image
            {
                Source = rtb, Width = imgWidth, Height = imgHeight,
                Stretch = Stretch.Uniform
            };

            // Bọc ảnh vào một Border để hiển thị viền chọn xanh dương giống ảnh chụp màn hình
            var imageBorder = new Border
            {
                Child = imgElement,
                BorderBrush = null,
                BorderThickness = new Thickness(0)
            };

            // Toolbar (hiện khi hover)
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 8, 0), 
                Opacity = 0.35 // ✨ Mặc định mờ để người dùng nhận biết
            };

            var moveBtn = MakeToolbarButton(" Di chuyển", Color.FromArgb(220, 33, 150, 243));
            var copyBtn = MakeToolbarButton("📋 Copy ảnh", Color.FromArgb(220, 76, 175, 80));
            var delBtn  = MakeToolbarButton("🗑️ Xóa", Color.FromArgb(220, 229, 57, 53));

            toolbar.Children.Add(moveBtn);
            toolbar.Children.Add(copyBtn);
            toolbar.Children.Add(delBtn);

            // Label nhỏ góc trái trên
            var label = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 25, 118, 210)),
                CornerRadius = new CornerRadius(0, 0, 6, 0),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            label.Child = new TextBlock
            {
                Text = $"{toolName}", FontSize = 10, Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            };

            var container = new Grid { Width = imgWidth, Tag = "LearningToolWhiteboard" };
            container.Children.Add(imageBorder);
            container.Children.Add(label);
            container.Children.Add(toolbar);

            // Hover effects
            container.MouseEnter += (s, e) => { toolbar.Opacity = 1.0; container.Cursor = Cursors.SizeAll; };
            container.MouseLeave += (s, e) => 
            { 
                if (imageBorder.BorderThickness.Left == 0) // Chưa chọn
                {
                    toolbar.Opacity = 0.35; 
                }
                container.Cursor = Cursors.Arrow; 
            };

            // Click để chọn đối tượng
            container.MouseLeftButtonDown += (s, e) =>
            {
                if (QASmartTouch.Utilities.InputValidationHelper.IsEventFromInteractiveControl(e.OriginalSource, container)) return;
                
                imageBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // #2196F3
                imageBorder.BorderThickness = new Thickness(2);
                toolbar.Opacity = 1.0;
            };

            // === ĐĂNG KÝ CLICK OUTSIDE TRÁNH MEMORY LEAK ===
            MouseButtonEventHandler clickOutsideHandler = null!;
            clickOutsideHandler = (s, e) =>
            {
                var clickPos = e.GetPosition(canvas);
                double left = Canvas.GetLeft(container); if (double.IsNaN(left)) left = 0;
                double top = Canvas.GetTop(container); if (double.IsNaN(top)) top = 0;
                
                Rect rect = new Rect(left, top, 
                    container.ActualWidth > 0 ? container.ActualWidth : imgWidth, 
                    container.ActualHeight > 0 ? container.ActualHeight : imgHeight);
                
                if (!rect.Contains(clickPos))
                {
                    imageBorder.BorderBrush = null;
                    imageBorder.BorderThickness = new Thickness(0);
                    toolbar.Opacity = 0.35;
                }
            };
            canvas.MouseLeftButtonDown += clickOutsideHandler;

            // Drag
            EnableDrag(container, canvas);

            // Copy ảnh vào clipboard
            copyBtn.Click += (s, e) =>
            {
                try { Clipboard.SetImage(rtb); } catch { }
                e.Handled = true;
            };

            // Xóa
            delBtn.Click += (s, e) =>
            {
                if (container.IsMouseCaptured)
                {
                    container.ReleaseMouseCapture();
                }
                canvas.Children.Remove(container);
                canvas.MouseLeftButtonDown -= clickOutsideHandler; // Giải phóng sự kiện tránh rò rỉ bộ nhớ
                e.Handled = true;
            };

            return container;
        }

        /// <summary>Container chế độ chiếu — có header xanh và nút đóng</summary>
        private static Grid BuildBroadcastContainer(
            BitmapSource rtb, double imgWidth, double imgHeight,
            Canvas canvas, string toolName)
        {
            var container = new Grid { Tag = "LearningToolBroadcast" };
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header bar
            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                CornerRadius = new CornerRadius(10, 10, 0, 0),
                Padding = new Thickness(16, 8, 16, 8)
            };
            var headerDock = new DockPanel();
            headerDock.Children.Add(new TextBlock
            {
                Text = $"{toolName} Đang chiếu",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center
            });

            // Buttons phải
            var headerBtnPanel = new StackPanel { Orientation = Orientation.Horizontal };
            DockPanel.SetDock(headerBtnPanel, Dock.Right);

            var copyBtn = MakeToolbarButton("Copy", Color.FromRgb(76, 175, 80));
            copyBtn.Margin = new Thickness(0, 0, 6, 0);
            copyBtn.Click += (s, e) =>
            {
                try { Clipboard.SetImage(rtb); } catch { }
                e.Handled = true;
            };

            var closeBtn = MakeToolbarButton("Đóng", Color.FromRgb(229, 57, 53));
            closeBtn.Click += (s, e) => { canvas.Children.Remove(container); e.Handled = true; };

            headerBtnPanel.Children.Add(copyBtn);
            headerBtnPanel.Children.Add(closeBtn);
            headerDock.Children.Add(headerBtnPanel);
            header.Child = headerDock;
            Grid.SetRow(header, 0);
            container.Children.Add(header);

            // Image
            var imgBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(0, 0, 10, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                BorderThickness = new Thickness(2, 0, 2, 2)
            };
            imgBorder.Child = new Image
            {
                Source = rtb, Width = imgWidth, Height = imgHeight,
                Stretch = Stretch.Uniform
            };
            Grid.SetRow(imgBorder, 1);
            container.Children.Add(imgBorder);

            // Drag via header
            EnableDrag(container, canvas);

            return container;
        }

        // ═══════════════════════════════════════════════════════════
        //  UTILITY HELPERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>Enable drag trên Canvas cho một UIElement</summary>
        public static void EnableDrag(FrameworkElement element, Canvas canvas)
        {
            bool isDragging = false;
            Point dragStart = new();

            element.MouseLeftButtonDown += (s, e) =>
            {
                // Không drag nếu click vào Button hoặc con của Button
                if (QASmartTouch.Utilities.InputValidationHelper.IsEventFromInteractiveControl(e.OriginalSource, element)) return;
                isDragging = true;
                dragStart = e.GetPosition(canvas);
                element.CaptureMouse();
                e.Handled = true;
            };
            element.MouseMove += (s, e) =>
            {
                if (!isDragging) return;
                var pos = e.GetPosition(canvas);
                double curLeft = Canvas.GetLeft(element); if (double.IsNaN(curLeft)) curLeft = 0;
                double curTop = Canvas.GetTop(element); if (double.IsNaN(curTop)) curTop = 0;
                Canvas.SetLeft(element, curLeft + pos.X - dragStart.X);
                Canvas.SetTop(element, curTop + pos.Y - dragStart.Y);
                dragStart = pos;
                e.Handled = true;
            };
            element.MouseLeftButtonUp += (s, e) =>
            {
                isDragging = false;
                element.ReleaseMouseCapture();
                e.Handled = true;
            };
        }

        /// <summary>Tạo button cho toolbar trên SmartScreen</summary>
        private static Button MakeToolbarButton(string text, Color bgColor)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Cursor = Cursors.Hand,
                Margin = new Thickness(3, 0, 3, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(bgColor));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            
            // Drop shadow for button
            var shadow = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 4,
                ShadowDepth = 1,
                Opacity = 0.15,
                Color = Colors.Black
            };
            borderFactory.SetValue(Border.EffectProperty, shadow);

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;

            // Hover: Brighten background slightly
            var hoverColor = Color.FromArgb(
                bgColor.A,
                (byte)Math.Min(255, bgColor.R + 25),
                (byte)Math.Min(255, bgColor.G + 25),
                (byte)Math.Min(255, bgColor.B + 25)
            );
            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush(hoverColor)
            });
            template.Triggers.Add(hoverTrigger);

            // Pressed: Darken background slightly
            var pressedColor = Color.FromArgb(
                bgColor.A,
                (byte)Math.Max(0, bgColor.R - 25),
                (byte)Math.Max(0, bgColor.G - 25),
                (byte)Math.Max(0, bgColor.B - 25)
            );
            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush(pressedColor)
            });
            template.Triggers.Add(pressedTrigger);

            btn.Template = template;
            return btn;
        }

        /// <summary>Tạo action button thống nhất cho teaching toolbar</summary>
        public static Button MakeActionButton(string text, string bgHex, string fgHex, string tooltip)
        {
            var bgBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(bgHex)!;
            var fgBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(fgHex)!;
            var bgColor = bgBrush.Color;

            var btn = new Button
            {
                Content = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = fgBrush,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 6, 0),
                Padding = new Thickness(10, 5, 10, 5),
                ToolTip = tooltip
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.BackgroundProperty, bgBrush);
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var shadow = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 4,
                ShadowDepth = 1,
                Opacity = 0.15,
                Color = Colors.Black
            };
            borderFactory.SetValue(Border.EffectProperty, shadow);

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;

            // Hover trigger (increase brightness slightly)
            var hoverColor = Color.FromArgb(
                bgColor.A,
                (byte)Math.Min(255, bgColor.R + 25),
                (byte)Math.Min(255, bgColor.G + 25),
                (byte)Math.Min(255, bgColor.B + 25)
            );
            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush(hoverColor)
            });
            template.Triggers.Add(hoverTrigger);

            // Pressed trigger (decrease brightness slightly)
            var pressedColor = Color.FromArgb(
                bgColor.A,
                (byte)Math.Max(0, bgColor.R - 25),
                (byte)Math.Max(0, bgColor.G - 25),
                (byte)Math.Max(0, bgColor.B - 25)
            );
            var pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush(pressedColor)
            });
            template.Triggers.Add(pressedTrigger);

            btn.Template = template;
            return btn;
        }

        /// <summary>Kiểm tra tool có thuộc nhóm Game/Quiz không (ẩn nút Bảng trắng)</summary>
        public static bool IsGameTool(string toolId)
        {
            var tool = ToolRegistry.AllTools.FirstOrDefault(t => t.Id == toolId);
            return tool?.HideWhiteboard ?? false;
        }

        // ═══════════════════════════════════════════════════════════
        //  SECTION-LEVEL FOCUS (Phương án A)
        //  Highlight 1 section cụ thể trong tool + chụp section riêng
        // ═══════════════════════════════════════════════════════════

        private static FrameworkElement? _lastFocusedSection;
        private static Brush? _lastFocusedOriginalBorder;
        private static Thickness _lastFocusedOriginalThickness;

        private static void FindTextBlocksInElement(DependencyObject parent, System.Collections.Generic.List<TextBlock> results)
        {
            if (parent == null) return;
            if (parent is TextBlock tb)
            {
                results.Add(tb);
                return;
            }
            int visualChildren = 0;
            try { visualChildren = VisualTreeHelper.GetChildrenCount(parent); } catch { }
            if (visualChildren > 0)
            {
                for (int i = 0; i < visualChildren; i++)
                {
                    FindTextBlocksInElement(VisualTreeHelper.GetChild(parent, i), results);
                }
            }
            else
            {
                // Fallback logical/property search
                if (parent is Border border && border.Child != null)
                {
                    FindTextBlocksInElement(border.Child, results);
                }
                else if (parent is ContentControl cc && cc.Content is DependencyObject dobj)
                {
                    FindTextBlocksInElement(dobj, results);
                }
                else if (parent is Panel panel)
                {
                    foreach (UIElement child in panel.Children)
                    {
                        FindTextBlocksInElement(child, results);
                    }
                }
            }
        }

        private static void ExtractStepContent(FrameworkElement section, string toolId, out string title, out string detail)
        {
            title = "";
            detail = "";
            if (section == null) return;

            var textBlocks = new System.Collections.Generic.List<TextBlock>();
            FindTextBlocksInElement(section, textBlocks);

            if (toolId == "constants" && textBlocks.Count >= 4)
            {
                string cat = textBlocks[0].Text?.Trim() ?? "";
                string symbol = textBlocks[1].Text?.Trim() ?? "";
                string name = textBlocks[2].Text?.Trim() ?? "";
                string value = textBlocks[3].Text?.Trim() ?? "";

                // Trích xuất mô tả từ ToolTip
                string desc = "";
                string? tooltip = section.ToolTip?.ToString();
                if (!string.IsNullOrEmpty(tooltip))
                {
                    int idx = tooltip.IndexOf("\n\n");
                    if (idx > 0) desc = tooltip.Substring(0, idx).Trim();
                    else desc = tooltip.Trim();
                }

                title = $"Hằng số: {name} ({symbol})";
                detail = $"• Ký hiệu: {symbol}\n" +
                         $"• Giá trị: {value}\n" +
                         $"• Phân loại: {cat}\n\n" +
                         $"📖 Mô tả chi tiết:\n{desc}";
                return;
            }

            if (textBlocks.Count > 0)
            {
                title = textBlocks[0].Text?.Trim() ?? "";
                if (textBlocks.Count > 1)
                {
                    var details = new System.Collections.Generic.List<string>();
                    for (int i = 1; i < textBlocks.Count; i++)
                    {
                        string txt = textBlocks[i].Text?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(txt)) details.Add(txt);
                    }
                    detail = string.Join("\n", details);
                }
            }
        }

        public static void FocusSectionVisual(FrameworkElement section, string toolId, string sectionId)
        {
            if (IsStudent())
            {
                Log.Warning("Học sinh không thể gửi lệnh Focus Section.");
                return;
            }
            // Unfocus section cũ (nếu có)
            UnfocusSectionVisual();

            if (section is Border border)
            {
                _lastFocusedSection = border;
                _lastFocusedOriginalBorder = border.BorderBrush;
                _lastFocusedOriginalThickness = border.BorderThickness;

                // Highlight: golden glow border
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 160, 0));
                border.BorderThickness = new Thickness(3);
            }
            else
            {
                _lastFocusedSection = section;
            }

            // Gửi command cho HS (nếu có mạng)
            try
            {
                string title = "";
                string detail = "";
                ExtractStepContent(section, toolId, out title, out detail);
                string encodedTitle = Uri.EscapeDataString(title);
                string encodedDetail = Uri.EscapeDataString(detail);

                var app = Application.Current as QASmartTouch.App;
                var net = app?.NetworkService;
                var cmd = $"CMD|TOOL_SECTION_FOCUS|{toolId}|{sectionId}|{encodedTitle}|{encodedDetail}";
                QASmartTouch.App.LessonState.LastTeacherCommand = cmd;
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
                if (net?.IsBroadcasting == true)
                    _ = net.SendCommandAsync(cmd);
                else
                    app.RaiseLocalCommand(cmd);
                Log.Information("Section focus: {ToolId}/{SectionId}", toolId, sectionId);
            }
            catch (Exception ex) { Log.Warning("Section focus error: {Err}", ex.Message); }
        }

        public static void UnfocusSectionVisual()
        {
            if (IsStudent())
            {
                Log.Warning("Học sinh không thể gửi lệnh Unfocus Section.");
                return;
            }
            if (_lastFocusedSection is Border border)
            {
                border.BorderBrush = _lastFocusedOriginalBorder;
                border.BorderThickness = _lastFocusedOriginalThickness;
            }
            _lastFocusedSection = null;
            _lastFocusedOriginalBorder = null;

            // Gửi unfocus command
            try
            {
                var app = Application.Current as QASmartTouch.App;
                var net = app?.NetworkService;
                var cmd = "CMD|TOOL_SECTION_UNFOCUS";
                QASmartTouch.App.LessonState.LastTeacherCommand = cmd;
                QASmartTouch.App.LessonState.LastCommandTime = DateTime.Now;
                if (net?.IsBroadcasting == true)
                    _ = net.SendCommandAsync(cmd);
                else
                    app.RaiseLocalCommand(cmd);
            }
            catch { }
        }

        public static bool SendSectionToWhiteboard(FrameworkElement section, string sectionName)
        {
            if (IsStudent())
            {
                Log.Warning("Học sinh không thể gửi section lên bảng trắng.");
                return false;
            }
            var targetWin = FindSmartScreenWindow();
            if (targetWin == null)
            {
                MessageBox.Show("Chưa mở SmartScreen.\nHãy chuyển sang SmartScreen trước.",
                    "Bảng Trắng Section", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            var canvas = FindSmartScreenCanvas(targetWin);
            if (canvas == null) return false;

            try
            {
                double actualW = section.ActualWidth;
                double actualH = section.ActualHeight;
                if (actualW < 1 || actualH < 1) return false;

                double scale = 2.0;
                var rtb = RenderVisualUnclipped(section, scale);
                if (rtb == null) return false;

                double maxW = canvas.ActualWidth * 0.7;
                double maxH = canvas.ActualHeight * 0.7;
                double ratio = System.Math.Min(maxW / actualW, maxH / actualH);
                if (ratio > 1) ratio = 1;
                double outW = actualW * ratio;
                double outH = actualH * ratio;

                string displayName = sectionName;
                if (!displayName.StartsWith("📚"))
                {
                    displayName = "📚 " + displayName;
                }
                var container = BuildInteractiveContainer(rtb, outW, outH, canvas, displayName);
                double left = System.Math.Max(20, (canvas.ActualWidth - outW) / 2);
                double top = System.Math.Max(40, (canvas.ActualHeight - outH) / 2);
                Canvas.SetLeft(container, left);
                Canvas.SetTop(container, top);
                canvas.Children.Add(container);

                targetWin.Show();
                targetWin.Activate();
                targetWin.WindowState = WindowState.Maximized;
                Log.Information("Section '{Name}' sent to whiteboard", sectionName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("Section whiteboard error: {Err}", ex.Message);
                return false;
            }
        }

        private class ScrollBarState
        {
            public ScrollViewer ScrollViewer { get; }
            public ScrollBarVisibility HorizontalVisibility { get; }
            public ScrollBarVisibility VerticalVisibility { get; }

            public ScrollBarState(ScrollViewer sv, ScrollBarVisibility h, ScrollBarVisibility v)
            {
                ScrollViewer = sv;
                HorizontalVisibility = h;
                VerticalVisibility = v;
            }
        }

        private class ScrollBarVisibilityState
        {
            public UIElement Element { get; }
            public Visibility Visibility { get; }

            public ScrollBarVisibilityState(UIElement el, Visibility vis)
            {
                Element = el;
                Visibility = vis;
            }
        }

        private static void CollectAndHideScrollBars(DependencyObject parent, System.Collections.Generic.List<ScrollBarState> list)
        {
            if (parent == null) return;
            if (parent is ScrollViewer sv)
            {
                list.Add(new ScrollBarState(sv, sv.HorizontalScrollBarVisibility, sv.VerticalScrollBarVisibility));
                sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
            int count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(parent); } catch { }
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                CollectAndHideScrollBars(child, list);
            }
        }

        private static void CollectAndHideIndividualScrollBars(DependencyObject parent, System.Collections.Generic.List<ScrollBarVisibilityState> list)
        {
            if (parent == null) return;
            if (parent is System.Windows.Controls.Primitives.ScrollBar sb)
            {
                list.Add(new ScrollBarVisibilityState(sb, sb.Visibility));
                sb.Visibility = Visibility.Collapsed;
            }
            int count = 0;
            try { count = VisualTreeHelper.GetChildrenCount(parent); } catch { }
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                CollectAndHideIndividualScrollBars(child, list);
            }
        }

        /// <summary>
        /// Helper render một FrameworkElement thành RenderTargetBitmap sắc nét ở độ phân giải cao,
        /// tránh mờ do VisualBrush và không bị giới hạn bởi clipping/scrolling của phần tử cha.
        /// </summary>
        public static RenderTargetBitmap? RenderVisualUnclipped(FrameworkElement element, double scale = 2.0)
        {
            var svStates = new System.Collections.Generic.List<ScrollBarState>();
            var sbStates = new System.Collections.Generic.List<ScrollBarVisibilityState>();

            // Lưu giữ trạng thái và ngắt kết nối cha tạm thời để tránh parent clipping
            DependencyObject? parent = element.Parent ?? VisualTreeHelper.GetParent(element);
            Panel? parentPanel = parent as Panel;
            ContentControl? parentContent = parent as ContentControl;
            Decorator? parentDecorator = parent as Decorator;

            int childIndex = -1;
            object? oldContent = null;
            UIElement? oldChild = null;

            try
            {
                if (parentPanel != null)
                {
                    childIndex = parentPanel.Children.IndexOf(element);
                    if (childIndex >= 0) parentPanel.Children.RemoveAt(childIndex);
                }
                else if (parentContent != null)
                {
                    oldContent = parentContent.Content;
                    parentContent.Content = null;
                }
                else if (parentDecorator != null)
                {
                    oldChild = parentDecorator.Child;
                    parentDecorator.Child = null;
                }

                // 1. Xác định chiều rộng cố định để làm mốc tự động xuống dòng
                double width = element.ActualWidth;
                if (double.IsNaN(width) || width < 1) width = element.Width;
                if (double.IsNaN(width) || width < 10) width = 350; // Kích thước chiều rộng mặc định hợp lý cho thẻ

                // Tìm và ẩn ScrollBars trước khi measure/arrange
                CollectAndHideScrollBars(element, svStates);
                CollectAndHideIndividualScrollBars(element, sbStates);

                // 2. Đo đạc phần tử với chiều cao không giới hạn
                element.Measure(new Size(width, double.PositiveInfinity));
                double height = element.DesiredSize.Height;

                // Fallback phòng hờ trường hợp không đo được
                if (double.IsNaN(height) || height < 10) height = element.ActualHeight;
                if (double.IsNaN(height) || height < 10) height = element.Height;
                if (double.IsNaN(height) || height < 10) height = 200;

                // Cắt chiều cao tối đa ở mức width * 10.0 để tránh ảnh quá dài
                if (height > width * 10.0)
                {
                    height = width * 10.0;
                }

                // 3. Tạm thời định vị và giãn nở toàn bộ phần tử theo kích thước thực tế
                element.Arrange(new Rect(0, 0, width, height));
                element.UpdateLayout();

                // 4. Vẽ nội dung lên DrawingVisual bằng VisualBrush
                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    double radiusX = 0;
                    double radiusY = 0;
                    if (element is Border border)
                    {
                        radiusX = border.CornerRadius.TopLeft;
                        radiusY = border.CornerRadius.TopLeft;
                    }

                    var rect = new Rect(0, 0, width, height);
                    
                    // Vẽ nền trắng trước để tránh bị trong suốt
                    if (radiusX > 0)
                    {
                        dc.DrawRoundedRectangle(Brushes.White, null, rect, radiusX, radiusY);
                    }
                    else
                    {
                        dc.DrawRectangle(Brushes.White, null, rect);
                    }

                    var brush = new VisualBrush(element)
                    {
                        Stretch = Stretch.None,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Viewbox = new Rect(0, 0, width, height)
                    };
                    
                    dc.DrawRectangle(brush, null, rect);
                }

                // 5. Thực hiện render ra Bitmap sắc nét
                var finalRtb = new RenderTargetBitmap(
                    (int)(width * scale), (int)(height * scale),
                    96 * scale, 96 * scale, PixelFormats.Pbgra32);
                finalRtb.Render(visual);

                return finalRtb;
            }
            catch (Exception ex)
            {
                Log.Warning("RenderVisualUnclipped error: {Err}", ex.Message);
                return null;
            }
            finally
            {
                // Khôi phục lại liên kết trong Visual Tree
                if (parentPanel != null && childIndex >= 0)
                {
                    if (!parentPanel.Children.Contains(element))
                        parentPanel.Children.Insert(childIndex, element);
                }
                else if (parentContent != null)
                {
                    parentContent.Content = oldContent;
                }
                else if (parentDecorator != null)
                {
                    parentDecorator.Child = oldChild;
                }

                element.InvalidateMeasure();
                element.InvalidateArrange();
                if (parent is UIElement parentElement)
                {
                    parentElement.InvalidateMeasure();
                    parentElement.InvalidateArrange();
                }
                element.UpdateLayout();

                // Khôi phục lại trạng thái hiển thị ScrollBars ban đầu
                foreach (var state in svStates)
                {
                    state.ScrollViewer.HorizontalScrollBarVisibility = state.HorizontalVisibility;
                    state.ScrollViewer.VerticalScrollBarVisibility = state.VerticalVisibility;
                }
                foreach (var state in sbStates)
                {
                    state.Element.Visibility = state.Visibility;
                }
            }
        }
    }
}
