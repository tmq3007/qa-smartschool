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
        /// Định nghĩa tất cả bước trong tour
        /// </summary>
        private List<TourStep> BuildTourSteps()
        {
            return new List<TourStep>
            {
                new()
                {
                    Title = "Chào mừng đến QA SmartTouch! 👋",
                    Description = "Đây là bảng trắng tương tác thông minh, được thiết kế cho màn hình cảm ứng 65-86 inch.\n\nHãy cùng tìm hiểu các công cụ chính!",
                    Category = "Bắt đầu",
                    ElementName = null, // No target — center screen
                    Shortcut = "F1 để mở lại tour"
                },
                new()
                {
                    Title = "✏️ Bút viết",
                    Description = "Công cụ chính để viết và vẽ trên bảng.\n• Nhấn giữ để mở bảng chọn màu & kích thước\n• Hỗ trợ nhận dạng chữ viết tay (OCR)",
                    Category = "Công cụ vẽ",
                    ElementName = "btn1_Pen",
                    Shortcut = "P"
                },
                new()
                {
                    Title = "🧹 Tẩy",
                    Description = "Xóa nét vẽ trên bảng.\n• Nhấn giữ để chọn kích thước tẩy\n• Hoặc dùng Ctrl+Z để hoàn tác nhanh",
                    Category = "Công cụ vẽ",
                    ElementName = "btn2_Eraser",
                    Shortcut = "E"
                },
                new()
                {
                    Title = "↩ Hoàn tác / Làm lại",
                    Description = "Quay lại thao tác trước hoặc làm lại thao tác đã hoàn tác.\n• Hỗ trợ tối đa 50 bước",
                    Category = "Chỉnh sửa",
                    ElementName = "btn3_Undo",
                    Shortcut = "Ctrl+Z / Ctrl+Y"
                },
                new()
                {
                    Title = "🔷 Hình học",
                    Description = "Vẽ hình nhanh: Vuông, Tròn, Tam giác, Mũi tên...\n• Nhấn giữ để mở bảng hình\n• Bao gồm biểu đồ: Line Chart, Pie Chart",
                    Category = "Chèn",
                    ElementName = "btn5_Shapes",
                    Shortcut = "S"
                },
                new()
                {
                    Title = "➕ Chèn nội dung",
                    Description = "Chèn đa phương tiện vào bảng:\n• 📷 Ảnh & Video\n• 📝 Văn bản\n• 🔤 OCR AI — nhận dạng chữ viết tay\n• 🌐 Dịch 100+ ngôn ngữ\n• 🔊 Text-to-Speech",
                    Category = "Chèn",
                    ElementName = "btn6_Inserts",
                    Shortcut = "I"
                },
                new()
                {
                    Title = "🔍 Phóng to / Thu nhỏ",
                    Description = "Phóng to để xem chi tiết hoặc thu nhỏ để nhìn tổng quan.\n• Cuộn chuột để zoom nhanh\n• Pinch-to-Zoom trên màn hình cảm ứng",
                    Category = "Điều hướng",
                    ElementName = "btn7_Zoom",
                    Shortcut = "Ctrl+Mouse Wheel"
                },
                new()
                {
                    Title = "👆 Chọn & Di chuyển",
                    Description = "Chọn đối tượng để:\n• Di chuyển, xoay, thay đổi kích thước\n• Sao chép, nhóm, xuất ảnh\n• Xem thuộc tính chi tiết",
                    Category = "Chỉnh sửa",
                    ElementName = "btn8_Select",
                    Shortcut = "V"
                },
                new()
                {
                    Title = "📋 Công cụ bảng",
                    Description = "Quản lý nhiều trang bảng:\n• Thêm / Xóa / Đổi tên trang\n• Đổi màu nền, đặt lưới\n• Thước kẻ, com-pa, ê-ke 3D",
                    Category = "Quản lý",
                    ElementName = "btn9_BoardManagement",
                    Shortcut = "B"
                },
                new()
                {
                    Title = "⚙️ Mở rộng",
                    Description = "Các tính năng nâng cao:\n• Cài đặt ứng dụng\n• Camera AI\n• Đếm giờ, Máy tính\n• Khảo sát nhanh\n• Thông tin & Cập nhật",
                    Category = "Tiện ích",
                    ElementName = "btn12_MoreExtended",
                    Shortcut = "M"
                },
                new()
                {
                    Title = "🎉 Bạn đã sẵn sàng!",
                    Description = "Bắt đầu tạo bài giảng tuyệt vời ngay!\n\n💡 Mẹo: Nhấn F1 bất kỳ lúc nào để mở lại hướng dẫn này.\n\nChúc bạn dạy và học hiệu quả! 🚀",
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

                TooltipCard.Margin = new Thickness(
                    (SystemParameters.PrimaryScreenWidth - 380) / 2,
                    (SystemParameters.PrimaryScreenHeight - 300) / 2,
                    0, 0);
            }

            // Animate tooltip entrance
            AnimateTooltipEntrance();
        }

        /// <summary>
        /// Đặt spotlight và tooltip tại vị trí của element trên target window
        /// </summary>
        private void PositionOnElement(string elementName)
        {
            try
            {
                // Tìm element trong target window
                var element = FindElementByName(_targetWindow, elementName) as FrameworkElement;
                if (element == null)
                {
                    SpotlightBorder.Visibility = Visibility.Collapsed;
                    TooltipCard.Visibility = Visibility.Visible;
                    TooltipCard.Margin = new Thickness(
                        (SystemParameters.PrimaryScreenWidth - 380) / 2,
                        (SystemParameters.PrimaryScreenHeight - 300) / 2,
                        0, 0);
                    return;
                }

                // Lấy vị trí tuyệt đối trên màn hình
                var point = element.PointToScreen(new Point(0, 0));
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

                // Tooltip card — đặt phía trên element
                double tooltipWidth = 360;
                double tooltipX = point.X + elWidth / 2 - tooltipWidth / 2;
                double tooltipY = point.Y - 240; // Phía trên element

                // Nếu tooltip bị che, đặt sang phải
                if (tooltipY < 20)
                {
                    tooltipY = point.Y + elHeight + 20;
                }

                // Giữ trong màn hình
                tooltipX = Math.Max(20, Math.Min(tooltipX, SystemParameters.PrimaryScreenWidth - tooltipWidth - 20));
                tooltipY = Math.Max(20, Math.Min(tooltipY, SystemParameters.PrimaryScreenHeight - 350));

                TooltipCard.Width = tooltipWidth;
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
