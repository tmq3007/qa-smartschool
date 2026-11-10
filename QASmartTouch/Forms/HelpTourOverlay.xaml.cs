using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace QASmartTouch.Forms
{
    /// <summary>
    /// Help Tour Overlay — Hướng dẫn tương tác cho người dùng mới
    /// Hiển thị tooltip + spotlight từng bước trên toolbar chính
    /// Kích hoạt: F1 hoặc menu Trợ giúp → Hướng dẫn nhanh
    /// </summary>
    public partial class HelpTourOverlay : Window
    {
        private readonly List<TourStep> _steps;
        private readonly Window _targetWindow;
        private int _currentStep;

        public HelpTourOverlay(Window targetWindow)
        {
            InitializeComponent();
            _targetWindow = targetWindow;
            _currentStep = 0;

            _steps = BuildTourSteps();
            BuildProgressDots();
        }

        /// <summary>
        /// Định nghĩa tất cả bước trong tour chuẩn hóa theo v4.2
        /// </summary>
        private List<TourStep> BuildTourSteps()
        {
            return new List<TourStep>
            {
                new()
                {
                    Title = "Chào mừng đến QA SmartTouch! 👋",
                    Description = "Bảng trắng tương tác thông minh thế hệ mới, tối ưu cho màn hình cảm ứng phòng học.\n\nHãy cùng khám phá các công cụ giảng dạy cốt lõi!",
                    Category = "Bắt đầu",
                    ElementName = null, // Giữa màn hình
                    Shortcut = "F1 hoặc Mở rộng ⚙️ → Trợ giúp"
                },
                new()
                {
                    Title = "✏️ Bút viết",
                    Description = "Công cụ chính để viết, vẽ và chú thích trên bảng.\n• Chạm 1 lần để kích hoạt và viết ngay\n• Chạm lần 2 để mở bảng chọn 4 loại bút (Thường, Dạ quang, Bút lông, Laser), màu sắc & nét vẽ",
                    Category = "Công cụ vẽ",
                    ElementName = "btn1_Pen",
                    Shortcut = null
                },
                new()
                {
                    Title = "🧹 Tẩy xóa",
                    Description = "Xóa nét vẽ và đối tượng nhanh chóng.\n• Chạm 1 lần để kích hoạt tẩy\n• Chạm lần 2 để chọn 3 chế độ: Xóa từng nét, Xóa theo vệt điểm, hoặc Xóa sạch toàn bộ bảng",
                    Category = "Công cụ vẽ",
                    ElementName = "btn2_Eraser",
                    Shortcut = null
                },
                new()
                {
                    Title = "↩ Hoàn tác & Làm lại",
                    Description = "Quay lại thao tác trước (Undo) hoặc khôi phục thao tác vừa hoàn tác (Redo).\n• Nút ↩: Hoàn tác | Nút ↪: Làm lại bên cạnh\n• Hỗ trợ lưu trữ lịch sử đến 100 bước",
                    Category = "Chỉnh sửa",
                    ElementName = "btn3_Undo",
                    Shortcut = "Ctrl+Z / Ctrl+Y"
                },
                new()
                {
                    Title = "🔷 Hình học 2D & 3D",
                    Description = "Chèn nhanh các hình vẽ trực quan cho bài giảng.\n• 2D: Đường thẳng, nét đứt, mũi tên, hình tròn, tam giác, hình vuông, chữ nhật...\n• 3D: Khối lập phương, khối cầu, hình chóp, khối trụ, hình nón, lăng kính...",
                    Category = "Hình học",
                    ElementName = "btn5_Shapes",
                    Shortcut = null
                },
                new()
                {
                    Title = "➕ Chèn nội dung & Học liệu",
                    Description = "Kho học liệu đa phương tiện phong phú gồm 5 danh mục:\n• Thước kẻ, com-pa, ê-ke, thước đo độ, bàn phím ảo\n• Hộp văn bản, bảng biểu, hình ảnh, video MP4\n• Biểu đồ, đồ thị hàm số 2D & 3D, Desmos 3D\n• Bảng tuần hoàn 118 nguyên tố, Sách giáo khoa\n• Mô hình 3D, thí nghiệm ảo PhET, Google Maps",
                    Category = "Học liệu",
                    ElementName = "btn6_Inserts",
                    Shortcut = "Ctrl+T (Chèn văn bản)"
                },
                new()
                {
                    Title = "👆 Chọn vùng (Lasso, Đũa phép)",
                    Description = "Khoanh chọn đối tượng để chỉnh sửa:\n• 3 chế độ: Khung chữ nhật, Tự do (Lasso) hoặc Đũa phép (chọn toàn bộ nét cùng màu)\n• Di chuyển, xoay, thay đổi kích thước\n• Thanh nổi nhanh: Đổi màu nét, độ dày, sao chép, xóa",
                    Category = "Chỉnh sửa",
                    ElementName = "btn8_Select",
                    Shortcut = "Ctrl+C / Ctrl+V / Delete"
                },
                new()
                {
                    Title = "📋 Công cụ bảng sư phạm",
                    Description = "Bộ 6 trợ thủ giảng dạy đắc lực cho giáo viên:\n• Đổi màu nền bảng & mẫu lưới kẻ ô ly\n• Màn che bảng (hé mở đáp án từng phần)\n• Kính lúp phóng to chi tiết bài học\n• Đèn rọi Spotlight tập trung sự chú ý\n• Cắt chụp nhanh một vùng bảng vẽ\n• Đa người dùng (chia đôi bảng cho 2 HS cùng viết)",
                    Category = "Sư phạm",
                    ElementName = "btn9_BoardManagement",
                    Shortcut = null
                },
                new()
                {
                    Title = "🖥️ Chế độ DESKTOP",
                    Description = "Chuyển đổi linh hoạt giữa Bảng vẽ và Màn hình Windows:\n• Thu nhỏ toolbar thành thanh công cụ nổi tinh gọn\n• Viết vẽ, ghi chú trực tiếp lên slide PowerPoint, tài liệu Word/PDF hoặc trang web đang trình chiếu",
                    Category = "Chế độ",
                    ElementName = "btn10_WindowMode",
                    Shortcut = null
                },
                new()
                {
                    Title = "⚙️ Mở rộng & Quản lý bài giảng",
                    Description = "Quản lý tệp tin và tiện ích giảng dạy nâng cao:\n• Mở & lưu bài giảng (.qasc), xuất PDF / Ảnh, in bảng\n• Ghi hình toàn bộ bài giảng thành video (MP4)\n• Đồng hồ đếm ngược giờ, máy tính\n• Cài đặt ứng dụng & Quản trị phiên bản",
                    Category = "Tiện ích",
                    ElementName = "btn12_MoreExtended",
                    Shortcut = null
                },
                new()
                {
                    Title = "📑 Quản lý & Chuyển trang",
                    Description = "Thanh điều hướng trang nổi ở góc dưới bên trái:\n• Nút ◀ / ▶: Lùi hoặc tiến giữa các trang bảng\n• Chạm vào số trang: Mở bảng quản lý danh sách trang\n• Nút ➕: Tạo nhanh trang bảng mới",
                    Category = "Điều hướng",
                    ElementName = "hudPageNavigation",
                    Shortcut = "Ctrl+N (Trang mới) / PageUp / PageDown"
                },
                new()
                {
                    Title = "🔍 Thu phóng bảng (Zoom)",
                    Description = "Điều chỉnh không gian hiển thị bảng vẽ:\n• Nút − / + : Thu nhỏ hoặc phóng to bảng\n• Chạm vào % : Đưa góc nhìn về chuẩn 100%\n• Cử chỉ: Cuộn chuột (Ctrl + Wheel) hoặc chụm 2 ngón tay (Pinch-to-Zoom)",
                    Category = "Thu phóng",
                    ElementName = "hudZoomIndicator",
                    Shortcut = "Ctrl+0 (Về 100%) / Pinch"
                },
                new()
                {
                    Title = "🎉 Bạn đã sẵn sàng!",
                    Description = "Chúc bạn có những tiết dạy sinh động và hiệu quả cùng QA SmartTouch! 🚀\n\n💡 Mẹo mở lại hướng dẫn bất kỳ lúc nào:\n• Nhấn phím F1 trên bàn phím, hoặc\n• Vào menu Mở rộng ⚙️ → chọn Trợ giúp.",
                    Category = "Hoàn thành",
                    ElementName = null,
                    Shortcut = null
                }
            };
        }

        /// <summary>
        /// Hiển thị bước hiện tại
        /// </summary>
        private void ShowStep(int index)
        {
            if (index < 0 || index >= _steps.Count) return;
            _currentStep = index;
            var step = _steps[index];

            // Update content
            tbStepNumber.Text = $"{index + 1}/{_steps.Count}";
            tbCategory.Text = step.Category;
            tbTitle.Text = step.Title;
            tbDescription.Text = step.Description;

            // Keyboard shortcut
            if (!string.IsNullOrEmpty(step.Shortcut))
            {
                ShortcutPanel.Visibility = Visibility.Visible;
                tbShortcut.Text = step.Shortcut;
            }
            else
            {
                ShortcutPanel.Visibility = Visibility.Collapsed;
            }

            // Navigation buttons
            btnBack.Visibility = index > 0 ? Visibility.Visible : Visibility.Collapsed;
            btnNext.Content = index == _steps.Count - 1 ? "Hoàn thành ✓" : "Tiếp →";

            // Update progress dots
            UpdateProgressDots(index);

            // Position spotlight and tooltip
            if (!string.IsNullOrEmpty(step.ElementName))
            {
                PositionOnElement(step.ElementName);
            }
            else
            {
                // Center on screen (welcome/finish step)
                SpotlightBorder.Visibility = Visibility.Collapsed;
                TooltipCard.Visibility = Visibility.Visible;

                double screenWidth = this.ActualWidth > 100 ? this.ActualWidth : SystemParameters.PrimaryScreenWidth;
                double screenHeight = this.ActualHeight > 100 ? this.ActualHeight : SystemParameters.PrimaryScreenHeight;
                double tooltipWidth = 380;
                TooltipCard.Width = tooltipWidth;
                TooltipCard.Measure(new Size(tooltipWidth, double.PositiveInfinity));
                double cardHeight = TooltipCard.DesiredSize.Height > 100 ? TooltipCard.DesiredSize.Height : 280;

                TooltipCard.Margin = new Thickness(
                    (screenWidth - tooltipWidth) / 2,
                    (screenHeight - cardHeight) / 2,
                    0, 0);
            }

            // Animate tooltip entrance
            AnimateTooltipEntrance();
        }

        /// <summary>
        /// Đặt spotlight và tooltip tại vị trí của element trên target window (chuẩn hóa đa độ phân giải DPI)
        /// </summary>
        private void PositionOnElement(string elementName)
        {
            try
            {
                double screenWidth = this.ActualWidth > 100 ? this.ActualWidth : SystemParameters.PrimaryScreenWidth;
                double screenHeight = this.ActualHeight > 100 ? this.ActualHeight : SystemParameters.PrimaryScreenHeight;
                double tooltipWidth = 380;

                // Tìm element trong target window
                var element = FindElementByName(_targetWindow, elementName) as FrameworkElement;
                if (element == null)
                {
                    SpotlightBorder.Visibility = Visibility.Collapsed;
                    TooltipCard.Visibility = Visibility.Visible;
                    TooltipCard.Width = tooltipWidth;
                    TooltipCard.Measure(new Size(tooltipWidth, double.PositiveInfinity));
                    double cardH = TooltipCard.DesiredSize.Height > 100 ? TooltipCard.DesiredSize.Height : 280;
                    TooltipCard.Margin = new Thickness(
                        (screenWidth - tooltipWidth) / 2,
                        (screenHeight - cardH) / 2,
                        0, 0);
                    return;
                }

                // Lấy vị trí phần tử và chuyển đổi sang tọa độ local của HelpTourOverlay (chuẩn hóa đa DPI màn hình tương tác)
                var screenPoint = element.PointToScreen(new Point(0, 0));
                var point = this.PointFromScreen(screenPoint);
                double elWidth = element.ActualWidth;
                double elHeight = element.ActualHeight;

                // Spotlight border
                double padding = 8;
                SpotlightBorder.Width = elWidth + padding * 2;
                SpotlightBorder.Height = elHeight + padding * 2;
                SpotlightBorder.Margin = new Thickness(
                    point.X - padding,
                    point.Y - padding,
                    0, 0);
                SpotlightBorder.Visibility = Visibility.Visible;

                // Tooltip card — đo kích thước thực tế và đặt phía trên element
                TooltipCard.Width = tooltipWidth;
                TooltipCard.Measure(new Size(tooltipWidth, double.PositiveInfinity));
                double cardHeight = TooltipCard.DesiredSize.Height > 100 ? TooltipCard.DesiredSize.Height : 260;

                double tooltipX = point.X + elWidth / 2 - tooltipWidth / 2;
                double tooltipY = point.Y - cardHeight - 16; // 16px phía trên spotlight

                // Nếu tooltip bị chạm mép trên màn hình, đặt phía dưới element
                if (tooltipY < 20)
                {
                    tooltipY = point.Y + elHeight + 16;
                }

                // Giữ trong giới hạn màn hình
                tooltipX = Math.Max(20, Math.Min(tooltipX, screenWidth - tooltipWidth - 20));
                tooltipY = Math.Max(20, Math.Min(tooltipY, screenHeight - cardHeight - 20));

                TooltipCard.Margin = new Thickness(tooltipX, tooltipY, 0, 0);
                TooltipCard.Visibility = Visibility.Visible;

                // Animate spotlight pulse
                AnimateSpotlightPulse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HelpTour] Position error: {ex.Message}");
                SpotlightBorder.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Tìm element theo tên trong visual tree
        /// </summary>
        private static DependencyObject? FindElementByName(DependencyObject parent, string name)
        {
            if (parent is FrameworkElement fe && fe.Name == name)
                return parent;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindElementByName(child, name);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// Animation entrance cho tooltip
        /// </summary>
        private void AnimateTooltipEntrance()
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            TooltipCard.BeginAnimation(OpacityProperty, fadeIn);

            var slideIn = new ThicknessAnimation(
                new Thickness(TooltipCard.Margin.Left, TooltipCard.Margin.Top + 15, 0, 0),
                TooltipCard.Margin,
                new Duration(TimeSpan.FromMilliseconds(300)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            TooltipCard.BeginAnimation(MarginProperty, slideIn);
        }

        /// <summary>
        /// Spotlight pulse animation
        /// </summary>
        private void AnimateSpotlightPulse()
        {
            var pulse = new DoubleAnimation(0.6, 1.0, TimeSpan.FromMilliseconds(800))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase()
            };
            SpotlightBorder.Effect.BeginAnimation(
                System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, pulse);
        }

        /// <summary>
        /// Tạo progress dots
        /// </summary>
        private void BuildProgressDots()
        {
            ProgressDots.Children.Clear();
            for (int i = 0; i < _steps.Count; i++)
            {
                var dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    Margin = new Thickness(3, 0, 3, 0)
                };
                ProgressDots.Children.Add(dot);
            }
        }

        /// <summary>
        /// Cập nhật progress dots
        /// </summary>
        private void UpdateProgressDots(int activeIndex)
        {
            for (int i = 0; i < ProgressDots.Children.Count; i++)
            {
                if (ProgressDots.Children[i] is Ellipse dot)
                {
                    if (i == activeIndex)
                    {
                        dot.Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0x86, 0xDE));
                        dot.Width = 12;
                        dot.Height = 8;
                    }
                    else if (i < activeIndex)
                    {
                        dot.Fill = new SolidColorBrush(Color.FromArgb(180, 0x2E, 0x86, 0xDE));
                        dot.Width = 8;
                        dot.Height = 8;
                    }
                    else
                    {
                        dot.Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                        dot.Width = 8;
                        dot.Height = 8;
                    }
                }
            }
        }

        #region Event Handlers

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            ShowStep(0);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep >= _steps.Count - 1)
            {
                Close(); // Finish tour
                return;
            }
            ShowStep(_currentStep + 1);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 0)
                ShowStep(_currentStep - 1);
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DarkOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Click on overlay → next step (or close on last)
            if (_currentStep >= _steps.Count - 1)
                Close();
            else
                ShowStep(_currentStep + 1);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Right:
                case Key.Enter:
                case Key.Space:
                    if (_currentStep >= _steps.Count - 1) Close();
                    else ShowStep(_currentStep + 1);
                    break;
                case Key.Left:
                    if (_currentStep > 0) ShowStep(_currentStep - 1);
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// Một bước trong Help Tour
    /// </summary>
    public class TourStep
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ElementName { get; set; }
        public string? Shortcut { get; set; }
    }
}
