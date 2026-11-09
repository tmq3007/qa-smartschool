using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using QASmartTouch.Forms;
using QASmartTouch.Shared;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_INTERACTIVE_CHART_CONTAINER: Bộ quản lý Unified Container chuẩn cho các biểu đồ (Charts)
    /// Đảm bảo tương thích chuẩn kép: Màn hình tương tác phòng học (Touch, Multi-touch, Stylus) & Máy tính (Mouse).
    /// 
    /// Các nguyên tắc kiến trúc:
    /// 1. Tách biệt Vùng Điều Khiển (Header Drag Bar) và Vùng Thân Biểu Đồ (Chart Body).
    /// 2. Header Drag Bar luôn hiển thị (không ẩn Opacity=0), tích hợp đầy đủ nút chức năng chạm 1 lần nhận ngay (Zero 2nd tap).
    /// 3. Thao tác kéo di chuyển (Drag) CHỈ áp dụng trên Header Drag Bar.
    /// 4. Thân biểu đồ KHÔNG bắt chặn Touch/Stylus, cho phép giáo viên viết, vẽ, ghi chú và tẩy xóa đè lên biểu đồ 100% tự nhiên.
    /// 5. Đăng ký tự động với hệ thống Undo/Redo và SelectionManager.
    /// </summary>
    public static class InteractiveChartHelper
    {
        public class ChartContainerOptions
        {
            public Form2_MainDashboard? MainDashboard { get; set; }
            public string ChartTitle { get; set; } = "Biểu đồ";
            public string Icon { get; set; } = "📊";
            public RenderTargetBitmap? Bitmap { get; set; }
            public ImageSource? ImageSource { get; set; }
            public double InitialWidth { get; set; } = 640;
            public double InitialHeight { get; set; } = 420;
            public double? TargetLeft { get; set; }
            public double? TargetTop { get; set; }
            public object? Configuration { get; set; }
            public Action<Grid>? OnEdit { get; set; }
            public Action<Grid, RenderTargetBitmap>? OnCopy { get; set; }
            public Action<Grid>? OnDelete { get; set; }
        }

        public const double HeaderBarHeight = 38.0;

        /// <summary>
        /// Xây dựng đối tượng Grid container biểu đồ tương tác độc lập (dùng cho cả thêm mới và phục hồi bài giảng)
        /// </summary>
        public static Grid BuildChartContainer(ChartContainerOptions options, Canvas? mainCanvas = null)
        {
            if (double.IsNaN(options.InitialWidth) || options.InitialWidth <= 0) options.InitialWidth = 640;
            if (double.IsNaN(options.InitialHeight) || options.InitialHeight <= 0) options.InitialHeight = 420;

            // 1. Root Container (Grid 2 dòng: Dòng 0 Header Drag Bar, Dòng 1 Chart Body)
            var rootContainer = new Grid
            {
                Width = options.InitialWidth,
                Tag = options.Configuration,
                Background = Brushes.Transparent
            };

            rootContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(HeaderBarHeight) });
            rootContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 2. Image hiển thị biểu đồ
            var chartImage = new Image
            {
                Source = options.ImageSource ?? options.Bitmap,
                Width = options.InitialWidth,
                Height = options.InitialHeight,
                Stretch = Stretch.Uniform
            };

            // 3. Header Drag Bar
            var headerDragBar = CreateHeaderDragBar(rootContainer, chartImage, options, mainCanvas);
            Grid.SetRow(headerDragBar, 0);

            // 4. Content Border chứa ảnh và InkPresenter phủ lên trên (QC_4.2_CONTAINER_OWNED_INKING)
            var chartBodyGrid = new Grid
            {
                ClipToBounds = true
            };
            chartBodyGrid.Children.Add(chartImage);

            var chartInkPresenter = new InkPresenter
            {
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            chartBodyGrid.Children.Add(chartInkPresenter);

            var contentBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 214, 229)),
                BorderThickness = new Thickness(1.5, 0, 1.5, 1.5),
                CornerRadius = new CornerRadius(0, 0, 8, 8),
                Child = chartBodyGrid,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.2,
                    ShadowDepth = 3,
                    BlurRadius = 8
                }
            };
            Grid.SetRow(contentBorder, 1);

            rootContainer.Children.Add(headerDragBar);
            rootContainer.Children.Add(contentBorder);

            // Đăng ký bộ lọc chạm 1 lần nhận ngay (Zero 2nd tap)
            TouchActivationHelper.WireAllInteractiveControls(headerDragBar);

            return rootContainer;
        }

        /// <summary>
        /// Tạo và gắn container biểu đồ tương tác lên Canvas MainInteractiveBoard
        /// </summary>
        public static Grid CreateAndAddChartToCanvas(ChartContainerOptions options)
        {
            if (options.MainDashboard == null)
                throw new ArgumentNullException(nameof(options.MainDashboard));

            var mainCanvas = options.MainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas == null)
                throw new InvalidOperationException("Không tìm thấy MainInteractiveBoard trên dashboard.");

            var rootContainer = BuildChartContainer(options, mainCanvas);

            // 5. Tính toán tọa độ đặt trên Canvas (ưu tiên căn giữa vùng nhìn thấy hiện tại khi bảng đang zoom/pan)
            Rect visibleBounds = BoundsHelper.GetVisibleCanvasBounds(mainCanvas);
            double left = options.TargetLeft ?? (visibleBounds.Left + Math.Max(20, (visibleBounds.Width - options.InitialWidth) / 2));
            double top = options.TargetTop ?? (visibleBounds.Top + Math.Max(20, (visibleBounds.Height - (options.InitialHeight + HeaderBarHeight)) / 2));

            if (!options.TargetLeft.HasValue)
            {
                int existingCharts = mainCanvas.Children.OfType<Grid>().Count();
                if (existingCharts > 0)
                {
                    left += (existingCharts % 5) * 25;
                    top += (existingCharts % 5) * 25;
                }
            }

            var (clampedLeft, clampedTop) = BoundsHelper.ClampToVirtualBoard(left, top, options.InitialWidth, options.InitialHeight + HeaderBarHeight);
            Canvas.SetLeft(rootContainer, clampedLeft);
            Canvas.SetTop(rootContainer, clampedTop);

            // 6. Cấp phát Z-Index chuẩn (đè lên nét chữ cũ, nhưng nét chữ mới sẽ nổi lên trên biểu đồ)
            int zIndex = options.MainDashboard.AllocateImageZIndex();
            Panel.SetZIndex(rootContainer, zIndex);

            // 7. Thêm vào Canvas và đăng ký Undo/Redo
            mainCanvas.Children.Add(rootContainer);
            options.MainDashboard.RecordChartAdd(rootContainer, options.ChartTitle);
            options.MainDashboard.RegisterObjectWithSelection(rootContainer);

            return rootContainer;
        }

        /// <summary>
        /// Tạo thanh Header Drag Bar với tiêu đề và cụm nút chức năng
        /// </summary>
        private static Border CreateHeaderDragBar(
            Grid rootContainer, 
            Image chartImage, 
            ChartContainerOptions options, 
            Canvas? mainCanvas)
        {
            var headerBar = new Border
            {
                Height = HeaderBarHeight,
                Background = new SolidColorBrush(Color.FromRgb(30, 39, 46)), // Slate Dark hiện đại
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Cursor = Cursors.SizeAll,
                ToolTip = "Nhấn giữ thanh này để di chuyển biểu đồ", Tag = "DragHandle",
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.15,
                    ShadowDepth = 2,
                    BlurRadius = 4
                }
            };

            var barGrid = new Grid();
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Action Buttons

            // Tiêu đề biểu đồ
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 5, 0)
            };

            var txtIcon = new TextBlock
            {
                Text = options.Icon,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            var txtTitle = new TextBlock
            {
                Text = options.ChartTitle,
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            titlePanel.Children.Add(txtIcon);
            titlePanel.Children.Add(txtTitle);
            Grid.SetColumn(titlePanel, 0);
            barGrid.Children.Add(titlePanel);

            // Panel các nút chức năng
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 4, 0)
            };

            // Nút Phóng to ➕
            var btnZoomIn = CreateHeaderButton("➕", "Phóng to biểu đồ (+15%)", Color.FromRgb(108, 92, 231));
            btnZoomIn.Click += (s, e) =>
            {
                ZoomIn(rootContainer, chartImage, mainCanvas);
                options.MainDashboard?.SyncElementSelectionBounds(rootContainer);
            };

            // Nút Thu nhỏ ➖
            var btnZoomOut = CreateHeaderButton("➖", "Thu nhỏ biểu đồ (-15%)", Color.FromRgb(230, 126, 34));
            btnZoomOut.Click += (s, e) =>
            {
                ZoomOut(rootContainer, chartImage);
                options.MainDashboard?.SyncElementSelectionBounds(rootContainer);
            };

            // Nút Sao chép 📋
            var btnCopy = CreateHeaderButton("📋", "Sao chép biểu đồ", Color.FromRgb(0, 184, 148));
            btnCopy.Click += (s, e) =>
            {
                if (options.OnCopy != null && options.Bitmap is RenderTargetBitmap rtb)
                {
                    options.OnCopy(rootContainer, rtb);
                    var srcInk = GetChartInkPresenter(rootContainer);
                    var newChart = (mainCanvas ?? rootContainer.Parent as Canvas)?.Children.OfType<Grid>().LastOrDefault(g => !ReferenceEquals(g, rootContainer) && IsChartContainer(g));
                    var dstInk = GetChartInkPresenter(newChart);
                    if (srcInk != null && dstInk != null && srcInk.Strokes.Count > 0 && dstInk.Strokes.Count == 0)
                    {
                        foreach (var stroke in srcInk.Strokes) dstInk.Strokes.Add(stroke.Clone());
                    }
                }
                else
                {
                    DefaultCopyChart(rootContainer, options);
                }
            };

            // Nút Chỉnh sửa ✏️
            var btnEdit = CreateHeaderButton("✏️", "Chỉnh sửa số liệu & cấu hình", Color.FromRgb(9, 132, 227));
            btnEdit.Click += (s, e) =>
            {
                if (options.OnEdit != null)
                {
                    options.OnEdit(rootContainer);
                }
                else
                {
                    DefaultEditChart(rootContainer, options);
                }
            };

            // Nút Xóa ❌
            var btnDelete = CreateHeaderButton("❌", "Xóa biểu đồ khỏi bảng", Color.FromRgb(214, 48, 49));
            btnDelete.Click += (s, e) =>
            {
                if (options.OnDelete != null)
                {
                    options.OnDelete(rootContainer);
                }
                else
                {
                    DefaultDeleteChart(rootContainer, options);
                }
            };

            buttonPanel.Children.Add(btnZoomIn);
            buttonPanel.Children.Add(btnZoomOut);
            buttonPanel.Children.Add(btnCopy);
            buttonPanel.Children.Add(btnEdit);
            buttonPanel.Children.Add(btnDelete);

            Grid.SetColumn(buttonPanel, 1);
            barGrid.Children.Add(buttonPanel);
            headerBar.Child = barGrid;

            // Gắn sự kiện kéo thả (Drag) CHỈ trên headerBar
            EnableHeaderDrag(headerBar, rootContainer, mainCanvas, options.MainDashboard);

            return headerBar;
        }

        /// <summary>
        /// Tạo nút bấm trên Header Bar với kích thước và style chuẩn cảm ứng
        /// </summary>
        private static Button CreateHeaderButton(string content, string toolTip, Color bgColor)
        {
            var btn = new Button
            {
                Content = content,
                Width = 28,
                Height = 28,
                Margin = new Thickness(2, 0, 2, 0),
                Background = new SolidColorBrush(bgColor),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                ToolTip = toolTip,
                Focusable = false
            };

            // Template bo góc đẹp mắt
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.PaddingProperty, new Thickness(2));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);

            template.VisualTree = border;
            btn.Template = template;

            // Zero 2nd tap cho màn hình tương tác
            TouchActivationHelper.WireButton(btn);

            return btn;
        }

        /// <summary>
        /// Kích hoạt cơ chế kéo di chuyển (Drag) CHỈ trên Header Bar (Chuẩn kép Mouse + Touch + Stylus)
        /// </summary>
        private static void EnableHeaderDrag(Border headerBar, Grid rootContainer, Canvas? mainCanvas, Forms.Form2_MainDashboard? mainDashboard = null)
        {
            headerBar.Tag ??= "DragHandle";
            Stylus.SetIsPressAndHoldEnabled(headerBar, false);
            Stylus.SetIsFlicksEnabled(headerBar, false);
            Stylus.SetIsTapFeedbackEnabled(headerBar, false);
            Stylus.SetIsTouchFeedbackEnabled(headerBar, false);

            bool isMouseDragging = false;
            int? activeTouchId = null;
            bool isStylusDragging = false;
            Point dragStartPoint = new Point();
            double originalLeft = 0;
            double originalTop = 0;

            Canvas? GetCanvas() => mainCanvas ?? rootContainer.Parent as Canvas ?? FindVisualParent<Canvas>(rootContainer);

            // Kiểm tra xem cú nhấn có phải trúng nút bấm không (nếu trúng nút bấm thì không drag)
            bool IsHitOnButton(object originalSource)
            {
                if (originalSource is DependencyObject dep)
                {
                    return QASmartTouch.Utilities.InputValidationHelper.IsEventFromButton(dep, headerBar) ||
                           FindVisualParent<Button>(dep) != null;
                }
                return false;
            }

            void ApplyDragToPosition(Canvas canvas, Point currentPoint)
            {
                double deltaX = currentPoint.X - dragStartPoint.X;
                double deltaY = currentPoint.Y - dragStartPoint.Y;

                double newLeft = originalLeft + deltaX;
                double newTop = originalTop + deltaY;

                double elemW = rootContainer.ActualWidth > 0 ? rootContainer.ActualWidth : (rootContainer.Width > 0 ? rootContainer.Width : 400);
                double elemH = rootContainer.ActualHeight > 0 ? rootContainer.ActualHeight : (rootContainer.Height > 0 ? rootContainer.Height : 300);
                var (clampedLeft, clampedTop) = BoundsHelper.ClampToVirtualBoard(newLeft, newTop, elemW, elemH);

                Canvas.SetLeft(rootContainer, clampedLeft);
                Canvas.SetTop(rootContainer, clampedTop);
            }

            // === 1. XỬ LÝ CẢM ỨNG (TOUCH / IFP) ===
            headerBar.PreviewTouchDown += (s, e) =>
            {
                if (IsHitOnButton(e.OriginalSource)) return;
                var canvas = GetCanvas();
                if (canvas == null) return;

                if (!activeTouchId.HasValue)
                {
                    activeTouchId = e.TouchDevice.Id;
                    isMouseDragging = false;
                    isStylusDragging = false;
                    dragStartPoint = e.GetTouchPoint(canvas).Position;
                    originalLeft = Canvas.GetLeft(rootContainer);
                    originalTop = Canvas.GetTop(rootContainer);
                    if (double.IsNaN(originalLeft)) originalLeft = 0;
                    if (double.IsNaN(originalTop)) originalTop = 0;

                    headerBar.CaptureTouch(e.TouchDevice);
                    e.Handled = true;
                }
            };

            headerBar.PreviewTouchMove += (s, e) =>
            {
                if (activeTouchId.HasValue && e.TouchDevice.Id == activeTouchId.Value)
                {
                    var canvas = GetCanvas();
                    if (canvas == null) return;

                    ApplyDragToPosition(canvas, e.GetTouchPoint(canvas).Position);
                    e.Handled = true;
                }
            };

            EventHandler<TouchEventArgs> endTouch = (s, e) =>
            {
                if (activeTouchId.HasValue && e.TouchDevice.Id == activeTouchId.Value)
                {
                    activeTouchId = null;
                    try { headerBar.ReleaseTouchCapture(e.TouchDevice); } catch { }
                    mainDashboard?.SyncElementSelectionBounds(rootContainer);
                    e.Handled = true;
                }
            };

            headerBar.PreviewTouchUp += endTouch;
            headerBar.TouchUp += endTouch;
            headerBar.LostTouchCapture += (s, e) =>
            {
                if (activeTouchId.HasValue && e.TouchDevice.Id == activeTouchId.Value)
                {
                    activeTouchId = null;
                    mainDashboard?.SyncElementSelectionBounds(rootContainer);
                }
            };

            // === 2. XỬ LÝ CHUỘT (DESKTOP - Chặn tuyệt đối chuột ảo từ cảm ứng) ===
            headerBar.MouseLeftButtonDown += (s, e) =>
            {
                if (activeTouchId.HasValue || isStylusDragging || e.StylusDevice != null) return;
                if (IsHitOnButton(e.OriginalSource)) return;
                var canvas = GetCanvas();
                if (canvas == null) return;

                isMouseDragging = true;
                dragStartPoint = e.GetPosition(canvas);
                originalLeft = Canvas.GetLeft(rootContainer);
                originalTop = Canvas.GetTop(rootContainer);
                if (double.IsNaN(originalLeft)) originalLeft = 0;
                if (double.IsNaN(originalTop)) originalTop = 0;

                headerBar.CaptureMouse();
                e.Handled = true;
            };

            headerBar.MouseMove += (s, e) =>
            {
                if (activeTouchId.HasValue || isStylusDragging || e.StylusDevice != null) return;
                if (isMouseDragging && headerBar.IsMouseCaptured)
                {
                    var canvas = GetCanvas();
                    if (canvas == null) return;

                    ApplyDragToPosition(canvas, e.GetPosition(canvas));
                    e.Handled = true;
                }
            };

            headerBar.MouseLeftButtonUp += (s, e) =>
            {
                if (activeTouchId.HasValue || isStylusDragging || e.StylusDevice != null) return;
                if (isMouseDragging)
                {
                    isMouseDragging = false;
                    headerBar.ReleaseMouseCapture();
                    mainDashboard?.SyncElementSelectionBounds(rootContainer);
                    e.Handled = true;
                }
            };

            headerBar.LostMouseCapture += (s, e) =>
            {
                isMouseDragging = false;
            };

            // === 3. XỬ LÝ BÚT CẢM ỨNG THẬT (STYLUS - Không bắt nhầm ngón tay Touch) ===
            headerBar.PreviewStylusDown += (s, e) =>
            {
                if (e.StylusDevice?.TabletDevice?.Type != TabletDeviceType.Stylus || activeTouchId.HasValue) return;
                if (IsHitOnButton(e.OriginalSource)) return;
                var canvas = GetCanvas();
                if (canvas == null) return;

                isStylusDragging = true;
                dragStartPoint = e.GetPosition(canvas);
                originalLeft = Canvas.GetLeft(rootContainer);
                originalTop = Canvas.GetTop(rootContainer);
                if (double.IsNaN(originalLeft)) originalLeft = 0;
                if (double.IsNaN(originalTop)) originalTop = 0;

                headerBar.CaptureStylus();
                e.Handled = true;
            };

            headerBar.StylusMove += (s, e) =>
            {
                if (e.StylusDevice?.TabletDevice?.Type != TabletDeviceType.Stylus || activeTouchId.HasValue) return;
                if (isStylusDragging)
                {
                    var canvas = GetCanvas();
                    if (canvas == null) return;

                    ApplyDragToPosition(canvas, e.GetPosition(canvas));
                    e.Handled = true;
                }
            };

            headerBar.StylusUp += (s, e) =>
            {
                if (isStylusDragging)
                {
                    isStylusDragging = false;
                    headerBar.ReleaseStylusCapture();
                    mainDashboard?.SyncElementSelectionBounds(rootContainer);
                    e.Handled = true;
                }
            };

            headerBar.LostStylusCapture += (s, e) =>
            {
                isStylusDragging = false;
            };
        }

        /// <summary>
        /// Phóng to biểu đồ 15%
        /// </summary>
        private static void ZoomIn(Grid rootContainer, Image chartImage, Canvas? mainCanvas)
        {
            var canvas = mainCanvas ?? rootContainer.Parent as Canvas ?? FindVisualParent<Canvas>(rootContainer);
            Rect visibleBounds = BoundsHelper.GetVisibleCanvasBounds(canvas);
            double maxW = Math.Max(2400, visibleBounds.Width - 40);
            double maxH = Math.Max(1600, visibleBounds.Height - 100);

            double scale = 1.15;
            double currentW = rootContainer.Width > 0 ? rootContainer.Width : chartImage.Width;
            double currentH = chartImage.Height > 0 ? chartImage.Height : 300;

            double newW = currentW * scale;
            double newH = currentH * scale;

            if (newW <= maxW && newH <= maxH)
            {
                double sx = newW / currentW;
                double sy = newH / currentH;
                rootContainer.Width = newW;
                chartImage.Width = newW;
                chartImage.Height = newH;
                var inkPresenter = GetChartInkPresenter(rootContainer);
                if (inkPresenter != null && inkPresenter.Strokes.Count > 0)
                {
                    inkPresenter.Strokes.Transform(new Matrix(sx, 0, 0, sy, 0, 0), false);
                }
            }
        }

        /// <summary>
        /// Thu nhỏ biểu đồ 15%
        /// </summary>
        private static void ZoomOut(Grid rootContainer, Image chartImage)
        {
            double scale = 0.85;
            double currentW = rootContainer.Width > 0 ? rootContainer.Width : chartImage.Width;
            double currentH = chartImage.Height > 0 ? chartImage.Height : 300;

            double newW = currentW * scale;
            double newH = currentH * scale;

            if (newW >= 240 && newH >= 150)
            {
                double sx = newW / currentW;
                double sy = newH / currentH;
                rootContainer.Width = newW;
                chartImage.Width = newW;
                chartImage.Height = newH;
                var inkPresenter = GetChartInkPresenter(rootContainer);
                if (inkPresenter != null && inkPresenter.Strokes.Count > 0)
                {
                    inkPresenter.Strokes.Transform(new Matrix(sx, 0, 0, sy, 0, 0), false);
                }
            }
        }

        /// <summary>
        /// Xóa biểu đồ mặc định
        /// </summary>
        private static void DefaultDeleteChart(Grid rootContainer, ChartContainerOptions options)
        {
            if (options.MainDashboard != null)
            {
                var mainCanvas = options.MainDashboard.FindName("MainInteractiveBoard") as Canvas;
                if (mainCanvas != null)
                {
                    mainCanvas.Children.Remove(rootContainer);
                    options.MainDashboard.RecordChartRemove(rootContainer, options.ChartTitle);
                    return;
                }
            }
            if (rootContainer.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(rootContainer);
            }
        }

        /// <summary>
        /// Chỉnh sửa biểu đồ mặc định (khi mở lại bài giảng từ file đã lưu)
        /// </summary>
        private static void DefaultEditChart(Grid rootContainer, ChartContainerOptions options)
        {
            var config = options.Configuration ?? rootContainer.Tag;
            if (config == null) return;

            var dashboard = options.MainDashboard 
                ?? Application.Current.Windows.OfType<Form2_MainDashboard>().FirstOrDefault();

            double originalLeft = Canvas.GetLeft(rootContainer);
            double originalTop = Canvas.GetTop(rootContainer);
            if (double.IsNaN(originalLeft)) originalLeft = 0;
            if (double.IsNaN(originalTop)) originalTop = 0;

            bool? result = false;

            if (config is BarChartConfiguration barConfig)
            {
                var editor = new Form2_8_BarChartEditor(dashboard)
                {
                    TargetLeft = originalLeft,
                    TargetTop = originalTop
                };
                editor.LoadConfiguration(barConfig);
                result = WindowHelper.ShowChildDialog(editor, dashboard);
            }
            else if (config is LineChartConfiguration lineConfig)
            {
                var editor = new Form2_9_LineChartEditor(dashboard)
                {
                    TargetLeft = originalLeft,
                    TargetTop = originalTop
                };
                editor.LoadConfiguration(lineConfig);
                result = WindowHelper.ShowChildDialog(editor, dashboard);
            }
            else if (config is PieChartConfiguration pieConfig)
            {
                var editor = new Form2_10_PieChartEditor(dashboard)
                {
                    TargetLeft = originalLeft,
                    TargetTop = originalTop
                };
                editor.LoadConfiguration(pieConfig);
                result = WindowHelper.ShowChildDialog(editor, dashboard);
            }
            else if (config is AreaChartConfiguration areaConfig)
            {
                var editor = new Form2_12_AreaChartEditor(dashboard)
                {
                    TargetLeft = originalLeft,
                    TargetTop = originalTop
                };
                editor.LoadConfiguration(areaConfig);
                result = WindowHelper.ShowChildDialog(editor, dashboard);
            }
            else if (config is ScatterChartConfiguration scatterConfig)
            {
                var editor = new Form2_13_ScatterChartEditor(dashboard)
                {
                    TargetLeft = originalLeft,
                    TargetTop = originalTop
                };
                editor.LoadConfiguration(scatterConfig);
                result = WindowHelper.ShowChildDialog(editor, dashboard);
            }
            else if (config is RadarChartConfiguration radarConfig)
            {
                var editor = new Form2_14_RadarChartEditor(dashboard)
                {
                    TargetLeft = originalLeft,
                    TargetTop = originalTop
                };
                editor.LoadConfiguration(radarConfig);
                result = WindowHelper.ShowChildDialog(editor, dashboard);
            }

            if (result == true)
            {
                DefaultDeleteChart(rootContainer, options);
            }
        }

        /// <summary>
        /// Nhân bản biểu đồ mặc định
        /// </summary>
        private static void DefaultCopyChart(Grid rootContainer, ChartContainerOptions options)
        {
            double currentLeft = Canvas.GetLeft(rootContainer);
            double currentTop = Canvas.GetTop(rootContainer);
            if (double.IsNaN(currentLeft)) currentLeft = 0;
            if (double.IsNaN(currentTop)) currentTop = 0;

            var chartImg = GetChartImage(rootContainer);

            var copyOptions = new ChartContainerOptions
            {
                MainDashboard = options.MainDashboard,
                ChartTitle = $"{options.ChartTitle} (Bản sao)",
                Icon = options.Icon,
                Bitmap = options.Bitmap,
                ImageSource = options.ImageSource ?? chartImg?.Source,
                InitialWidth = rootContainer.Width > 0 ? rootContainer.Width : 640,
                InitialHeight = chartImg?.Height > 0 ? chartImg.Height : options.InitialHeight,
                TargetLeft = currentLeft + 30,
                TargetTop = currentTop + 30,
                Configuration = options.Configuration ?? rootContainer.Tag,
                OnEdit = options.OnEdit,
                OnCopy = options.OnCopy,
                OnDelete = options.OnDelete
            };

            Grid? newChart = null;
            if (options.MainDashboard != null)
            {
                newChart = CreateAndAddChartToCanvas(copyOptions);
            }
            else if (rootContainer.Parent is Canvas parentCanvas)
            {
                newChart = BuildChartContainer(copyOptions, parentCanvas);
                var (clampedCopyL, clampedCopyT) = BoundsHelper.ClampToVirtualBoard(
                    copyOptions.TargetLeft ?? 0,
                    copyOptions.TargetTop ?? 0,
                    copyOptions.InitialWidth,
                    copyOptions.InitialHeight + HeaderBarHeight);
                Canvas.SetLeft(newChart, clampedCopyL);
                Canvas.SetTop(newChart, clampedCopyT);
                parentCanvas.Children.Add(newChart);
            }

            if (newChart != null)
            {
                var srcInk = GetChartInkPresenter(rootContainer);
                var dstInk = GetChartInkPresenter(newChart);
                if (srcInk != null && dstInk != null && srcInk.Strokes.Count > 0)
                {
                    foreach (var s in srcInk.Strokes)
                    {
                        dstInk.Strokes.Add(s.Clone());
                    }
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem một UIElement có phải là Container của biểu đồ tương tác hay không
        /// </summary>
        public static bool IsChartContainer(FrameworkElement? element)
        {
            if (element is not Grid grid) return false;

            // Kiểm tra cấu hình Tag
            if (grid.Tag != null)
            {
                var typeName = grid.Tag.GetType().Name;
                if (typeName.EndsWith("ChartConfiguration"))
                    return true;
            }

            if (grid.RowDefinitions.Count != 2) return false;
            if (grid.Children.Count < 2) return false;

            // Kiểm tra cấu trúc: child 0 là Border header (chiều cao xấp xỉ HeaderBarHeight), child 1 là Border chứa Image hoặc Grid
            if (grid.Children[0] is Border headerBorder && Math.Abs(headerBorder.Height - HeaderBarHeight) < 2.0)
            {
                if (grid.Children[1] is Border contentBorder)
                {
                    if (contentBorder.Child is Image)
                        return true;
                    if (contentBorder.Child is Grid bodyGrid && bodyGrid.Children.OfType<Image>().Any())
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        public static double GetChartBodyHeight(Grid? container, double fallback = 420)
        {
            if (container == null) return fallback;
            var chartImg = GetChartImage(container);
            if (chartImg != null)
            {
                if (!double.IsNaN(chartImg.Height) && chartImg.Height > 0)
                    return chartImg.Height;
                if (chartImg.ActualHeight > 0)
                    return chartImg.ActualHeight;
            }
            if (!double.IsNaN(container.Height) && container.Height > HeaderBarHeight)
                return container.Height - HeaderBarHeight;
            if (container.ActualHeight > HeaderBarHeight)
                return container.ActualHeight - HeaderBarHeight;
            return fallback;
        }

        /// <summary>
        /// Lấy đối tượng Image chứa hình ảnh biểu đồ từ bên trong Grid container
        /// </summary>
        public static Image? GetChartImage(FrameworkElement? container)
        {
            if (container is not Grid grid) return null;
            if (grid.Children.Count >= 2 && grid.Children[1] is Border contentBorder)
            {
                if (contentBorder.Child is Image img) return img;
                if (contentBorder.Child is Grid bodyGrid) return bodyGrid.Children.OfType<Image>().FirstOrDefault();
            }
            return FindVisualChild<Image>(grid);
        }

        /// <summary>
        /// Lấy đối tượng InkPresenter chứa nét vẽ chú thích trên thân biểu đồ (QC_4.2_CONTAINER_OWNED_INKING)
        /// </summary>
        public static InkPresenter? GetChartInkPresenter(FrameworkElement? container)
        {
            if (container is not Grid grid) return null;
            if (grid.Children.Count >= 2 && grid.Children[1] is Border contentBorder && contentBorder.Child is Grid bodyGrid)
            {
                return bodyGrid.Children.OfType<InkPresenter>().FirstOrDefault();
            }
            return FindVisualChild<InkPresenter>(grid);
        }

        /// <summary>
        /// Lấy Icon và Title của biểu đồ từ thanh Header Drag Bar
        /// </summary>
        public static (string icon, string title) GetChartHeaderInfo(FrameworkElement? container)
        {
            string icon = "📊";
            string title = "Biểu đồ";
            if (container is not Grid grid || grid.Children.Count == 0) return (icon, title);

            if (grid.Children[0] is Border headerBorder && headerBorder.Child is Grid barGrid && barGrid.Children.Count > 0)
            {
                if (barGrid.Children[0] is StackPanel titlePanel && titlePanel.Children.Count >= 2)
                {
                    if (titlePanel.Children[0] is TextBlock tbIcon) icon = tbIcon.Text;
                    if (titlePanel.Children[1] is TextBlock tbTitle) title = tbTitle.Text;
                }
            }
            return (icon, title);
        }

        public static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child is T typedSelf) return typedSelf;
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;
                var nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
