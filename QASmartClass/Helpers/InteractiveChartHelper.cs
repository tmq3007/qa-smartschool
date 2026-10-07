using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using QASmartTouch.Forms;

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
            public Form2_MainDashboard MainDashboard { get; set; } = null!;
            public string ChartTitle { get; set; } = "Biểu đồ";
            public string Icon { get; set; } = "📊";
            public RenderTargetBitmap Bitmap { get; set; } = null!;
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
        /// Tạo và gắn container biểu đồ tương tác lên Canvas MainInteractiveBoard
        /// </summary>
        public static Grid CreateAndAddChartToCanvas(ChartContainerOptions options)
        {
            if (options.MainDashboard == null)
                throw new ArgumentNullException(nameof(options.MainDashboard));

            var mainCanvas = options.MainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas == null)
                throw new InvalidOperationException("Không tìm thấy MainInteractiveBoard trên dashboard.");

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
                Source = options.Bitmap,
                Width = options.InitialWidth,
                Height = options.InitialHeight,
                Stretch = Stretch.Uniform
            };

            // 3. Header Drag Bar
            var headerDragBar = CreateHeaderDragBar(rootContainer, chartImage, options, mainCanvas);
            Grid.SetRow(headerDragBar, 0);

            // 4. Content Border chứa ảnh
            var contentBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 214, 229)),
                BorderThickness = new Thickness(1.5, 0, 1.5, 1.5),
                CornerRadius = new CornerRadius(0, 0, 8, 8),
                Child = chartImage,
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

            // 5. Tính toán tọa độ đặt trên Canvas
            double left = options.TargetLeft ?? Math.Max(20, (mainCanvas.ActualWidth - options.InitialWidth) / 2);
            double top = options.TargetTop ?? Math.Max(20, (mainCanvas.ActualHeight - (options.InitialHeight + HeaderBarHeight)) / 2);

            if (!options.TargetLeft.HasValue)
            {
                int existingCharts = mainCanvas.Children.OfType<Grid>().Count();
                if (existingCharts > 0)
                {
                    left += (existingCharts % 5) * 25;
                    top += (existingCharts % 5) * 25;
                }
            }

            Canvas.SetLeft(rootContainer, Math.Max(0, left));
            Canvas.SetTop(rootContainer, Math.Max(0, top));

            // 6. Cấp phát Z-Index chuẩn (đè lên nét chữ cũ, nhưng nét chữ mới sẽ nổi lên trên biểu đồ)
            int zIndex = options.MainDashboard.AllocateImageZIndex();
            Panel.SetZIndex(rootContainer, zIndex);

            // 7. Thêm vào Canvas và đăng ký Undo/Redo
            mainCanvas.Children.Add(rootContainer);
            options.MainDashboard.RecordChartAdd(rootContainer, options.ChartTitle);
            options.MainDashboard.RegisterObjectWithSelection(rootContainer);

            // 8. Đăng ký với SelectionManager (nếu có)
            TouchActivationHelper.WireAllInteractiveControls(headerDragBar);

            return rootContainer;
        }

        /// <summary>
        /// Tạo thanh Header Drag Bar với tiêu đề và cụm nút chức năng
        /// </summary>
        private static Border CreateHeaderDragBar(
            Grid rootContainer, 
            Image chartImage, 
            ChartContainerOptions options, 
            Canvas mainCanvas)
        {
            var headerBar = new Border
            {
                Height = HeaderBarHeight,
                Background = new SolidColorBrush(Color.FromRgb(30, 39, 46)), // Slate Dark hiện đại
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Cursor = Cursors.SizeAll,
                ToolTip = "Nhấn giữ thanh này để di chuyển biểu đồ",
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
            btnZoomIn.Click += (s, e) => ZoomIn(rootContainer, chartImage, mainCanvas);

            // Nút Thu nhỏ ➖
            var btnZoomOut = CreateHeaderButton("➖", "Thu nhỏ biểu đồ (-15%)", Color.FromRgb(230, 126, 34));
            btnZoomOut.Click += (s, e) => ZoomOut(rootContainer, chartImage);

            // Nút Sao chép 📋
            var btnCopy = CreateHeaderButton("📋", "Sao chép biểu đồ", Color.FromRgb(0, 184, 148));
            btnCopy.Click += (s, e) =>
            {
                if (options.OnCopy != null)
                {
                    options.OnCopy(rootContainer, options.Bitmap);
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
                options.OnEdit?.Invoke(rootContainer);
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
            EnableHeaderDrag(headerBar, rootContainer, mainCanvas);

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
        private static void EnableHeaderDrag(Border headerBar, Grid rootContainer, Canvas mainCanvas)
        {
            bool isDragging = false;
            Point dragStartPoint = new Point();
            double originalLeft = 0;
            double originalTop = 0;

            // Kiểm tra xem cú nhấn có phải trúng nút bấm không (nếu trúng nút bấm thì không drag)
            bool IsHitOnButton(object originalSource)
            {
                if (originalSource is DependencyObject dep)
                {
                    return FindVisualParent<Button>(dep) != null;
                }
                return false;
            }

            // === 1. XỬ LÝ CHUỘT (DESKTOP) ===
            headerBar.MouseLeftButtonDown += (s, e) =>
            {
                if (IsHitOnButton(e.OriginalSource)) return;

                isDragging = true;
                dragStartPoint = e.GetPosition(mainCanvas);
                originalLeft = Canvas.GetLeft(rootContainer);
                originalTop = Canvas.GetTop(rootContainer);
                if (double.IsNaN(originalLeft)) originalLeft = 0;
                if (double.IsNaN(originalTop)) originalTop = 0;

                headerBar.CaptureMouse();
                e.Handled = true;
            };

            headerBar.MouseMove += (s, e) =>
            {
                if (isDragging && headerBar.IsMouseCaptured)
                {
                    Point currentPoint = e.GetPosition(mainCanvas);
                    double deltaX = currentPoint.X - dragStartPoint.X;
                    double deltaY = currentPoint.Y - dragStartPoint.Y;

                    double newLeft = originalLeft + deltaX;
                    double newTop = originalTop + deltaY;

                    // Giới hạn trong Canvas
                    newLeft = Math.Max(0, Math.Min(newLeft, mainCanvas.ActualWidth - rootContainer.ActualWidth));
                    newTop = Math.Max(0, Math.Min(newTop, mainCanvas.ActualHeight - (rootContainer.ActualHeight > 0 ? rootContainer.ActualHeight : 300)));

                    Canvas.SetLeft(rootContainer, newLeft);
                    Canvas.SetTop(rootContainer, newTop);
                    e.Handled = true;
                }
            };

            headerBar.MouseLeftButtonUp += (s, e) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    headerBar.ReleaseMouseCapture();
                    e.Handled = true;
                }
            };

            headerBar.LostMouseCapture += (s, e) =>
            {
                isDragging = false;
            };

            // === 2. XỬ LÝ CẢM ỨNG (TOUCH / IFP) ===
            headerBar.PreviewTouchDown += (s, e) =>
            {
                if (IsHitOnButton(e.OriginalSource)) return;

                isDragging = true;
                dragStartPoint = e.GetTouchPoint(mainCanvas).Position;
                originalLeft = Canvas.GetLeft(rootContainer);
                originalTop = Canvas.GetTop(rootContainer);
                if (double.IsNaN(originalLeft)) originalLeft = 0;
                if (double.IsNaN(originalTop)) originalTop = 0;

                headerBar.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            };

            headerBar.TouchMove += (s, e) =>
            {
                if (isDragging)
                {
                    Point currentPoint = e.GetTouchPoint(mainCanvas).Position;
                    double deltaX = currentPoint.X - dragStartPoint.X;
                    double deltaY = currentPoint.Y - dragStartPoint.Y;

                    double newLeft = originalLeft + deltaX;
                    double newTop = originalTop + deltaY;

                    newLeft = Math.Max(0, Math.Min(newLeft, mainCanvas.ActualWidth - rootContainer.ActualWidth));
                    newTop = Math.Max(0, Math.Min(newTop, mainCanvas.ActualHeight - (rootContainer.ActualHeight > 0 ? rootContainer.ActualHeight : 300)));

                    Canvas.SetLeft(rootContainer, newLeft);
                    Canvas.SetTop(rootContainer, newTop);
                    e.Handled = true;
                }
            };

            headerBar.TouchUp += (s, e) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    headerBar.ReleaseTouchCapture(e.TouchDevice);
                    e.Handled = true;
                }
            };

            // === 3. XỬ LÝ BÚT CẢM ỨNG (STYLUS) ===
            headerBar.PreviewStylusDown += (s, e) =>
            {
                if (IsHitOnButton(e.OriginalSource)) return;

                isDragging = true;
                dragStartPoint = e.GetPosition(mainCanvas);
                originalLeft = Canvas.GetLeft(rootContainer);
                originalTop = Canvas.GetTop(rootContainer);
                if (double.IsNaN(originalLeft)) originalLeft = 0;
                if (double.IsNaN(originalTop)) originalTop = 0;

                headerBar.CaptureStylus();
                e.Handled = true;
            };

            headerBar.StylusMove += (s, e) =>
            {
                if (isDragging)
                {
                    Point currentPoint = e.GetPosition(mainCanvas);
                    double deltaX = currentPoint.X - dragStartPoint.X;
                    double deltaY = currentPoint.Y - dragStartPoint.Y;

                    double newLeft = originalLeft + deltaX;
                    double newTop = originalTop + deltaY;

                    newLeft = Math.Max(0, Math.Min(newLeft, mainCanvas.ActualWidth - rootContainer.ActualWidth));
                    newTop = Math.Max(0, Math.Min(newTop, mainCanvas.ActualHeight - (rootContainer.ActualHeight > 0 ? rootContainer.ActualHeight : 300)));

                    Canvas.SetLeft(rootContainer, newLeft);
                    Canvas.SetTop(rootContainer, newTop);
                    e.Handled = true;
                }
            };

            headerBar.StylusUp += (s, e) =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    headerBar.ReleaseStylusCapture();
                    e.Handled = true;
                }
            };
        }

        /// <summary>
        /// Phóng to biểu đồ 15%
        /// </summary>
        private static void ZoomIn(Grid rootContainer, Image chartImage, Canvas mainCanvas)
        {
            double scale = 1.15;
            double currentW = rootContainer.Width > 0 ? rootContainer.Width : chartImage.Width;
            double currentH = chartImage.Height > 0 ? chartImage.Height : 300;

            double newW = currentW * scale;
            double newH = currentH * scale;

            if (newW <= mainCanvas.ActualWidth - 40 && newH <= mainCanvas.ActualHeight - 100)
            {
                rootContainer.Width = newW;
                chartImage.Width = newW;
                chartImage.Height = newH;
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
                rootContainer.Width = newW;
                chartImage.Width = newW;
                chartImage.Height = newH;
            }
        }

        /// <summary>
        /// Xóa biểu đồ mặc định
        /// </summary>
        private static void DefaultDeleteChart(Grid rootContainer, ChartContainerOptions options)
        {
            var mainCanvas = options.MainDashboard.FindName("MainInteractiveBoard") as Canvas;
            if (mainCanvas != null)
            {
                mainCanvas.Children.Remove(rootContainer);
                options.MainDashboard.RecordChartRemove(rootContainer, options.ChartTitle);
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

            var copyOptions = new ChartContainerOptions
            {
                MainDashboard = options.MainDashboard,
                ChartTitle = $"{options.ChartTitle} (Bản sao)",
                Icon = options.Icon,
                Bitmap = options.Bitmap,
                InitialWidth = rootContainer.Width,
                InitialHeight = (rootContainer.Children.OfType<Border>().FirstOrDefault(b => Grid.GetRow(b) == 1)?.Child as Image)?.Height ?? options.InitialHeight,
                TargetLeft = currentLeft + 30,
                TargetTop = currentTop + 30,
                Configuration = options.Configuration,
                OnEdit = options.OnEdit,
                OnCopy = options.OnCopy,
                OnDelete = options.OnDelete
            };

            CreateAndAddChartToCanvas(copyOptions);
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }
    }
}
