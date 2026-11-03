using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartClass.Classroom.Services;

namespace QASmartClass.Classroom.Views
{
    /// <summary>
    /// Màn hình phủ trong suốt cho phép Giáo viên kéo chuột hoặc chạm tay/bút cảm ứng
    /// để chọn chính xác một phần màn hình (Region) cần trình chiếu cho học sinh.
    /// </summary>
    public sealed class ScreenRegionSelectorWindow : Window
    {
        private readonly Canvas _canvas = new() { Background = Brushes.Transparent };
        private readonly Path _dimMask = new()
        {
            Fill = new SolidColorBrush(Color.FromArgb(135, 15, 23, 42)),
            IsHitTestVisible = false
        };
        private readonly Border _selectionFrame = new()
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
            BorderThickness = new Thickness(3),
            Background = new SolidColorBrush(Color.FromArgb(18, 37, 99, 235)),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
        private readonly Border _sizeBadge = new()
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 15, 23, 42)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4, 10, 4),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
        private readonly TextBlock _txtSizeBadge = new()
        {
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.Bold
        };
        private readonly Border _actionDock = new()
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(10),
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6, 10, 6),
            Visibility = Visibility.Collapsed
        };

        private Point _dragStartDip;
        private Rect _currentDipRect = Rect.Empty;
        private bool _isDragging;
        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;

        public ScreenSourceInfo? SelectedRegion { get; private set; }

        public ScreenRegionSelectorWindow(Rect? initialDipRect = null)
        {
            Title = "Chọn vùng màn hình để chiếu";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;

            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            _sizeBadge.Child = _txtSizeBadge;

            // Thanh hướng dẫn & chọn nhanh trên đỉnh màn hình
            var topBar = BuildTopInstructionBar();
            // Thanh xác nhận nổi dưới vùng chọn
            BuildFloatingActionDock();

            _canvas.Children.Add(_dimMask);
            _canvas.Children.Add(_selectionFrame);
            _canvas.Children.Add(_sizeBadge);
            _canvas.Children.Add(topBar);
            _canvas.Children.Add(_actionDock);

            Content = _canvas;

            Loaded += (_, _) =>
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                _dpiScaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
                _dpiScaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

                // Đặt thanh hướng dẫn ở chính giữa phía trên
                topBar.Measure(new Size(Width, Height));
                Canvas.SetLeft(topBar, Math.Max(16, (Width - topBar.DesiredSize.Width) / 2));
                Canvas.SetTop(topBar, 20);

                if (initialDipRect.HasValue && initialDipRect.Value.Width >= 80 && initialDipRect.Value.Height >= 60)
                {
                    UpdateSelectionRect(initialDipRect.Value, showActions: true);
                }
                else
                {
                    // Mặc định gợi ý sẵn khung 16:9 ở giữa màn hình để GV có thể bấm "Chiếu vùng này" ngay hoặc kéo lại tùy ý
                    double defW = Math.Round(Width * 0.72);
                    double defH = Math.Round(defW * 9.0 / 16.0);
                    if (defH > Height * 0.72)
                    {
                        defH = Math.Round(Height * 0.72);
                        defW = Math.Round(defH * 16.0 / 9.0);
                    }
                    var defRect = new Rect(Math.Round((Width - defW) / 2), Math.Round((Height - defH) / 2), defW, defH);
                    UpdateSelectionRect(defRect, showActions: true);
                }
            };

            // Hỗ trợ cả Chuột (Desktop) và Cảm ứng / Bút Stylus (Interactive Touch Screen)
            _canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;

            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    SelectedRegion = null;
                    DialogResult = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter && _currentDipRect.Width >= 60 && _currentDipRect.Height >= 60)
                {
                    ConfirmSelection();
                    e.Handled = true;
                }
            };
        }

        private Border BuildTopInstructionBar()
        {
            var bar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(242, 15, 23, 42)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 10, 16, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            stack.Children.Add(new TextBlock
            {
                Text = "✂️ Kéo chuột hoặc chạm tay để vẽ vùng muốn chiếu  |",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            var btnSmartClassArea = MakeQuickChip("📐 Khung nội dung SMART CLASS", () =>
            {
                double sideDip = 250;
                double topDip = 64;
                var r = new Rect(sideDip, topDip, Math.Max(300, Width - sideDip), Math.Max(200, Height - topDip));
                UpdateSelectionRect(r, showActions: true);
            });
            var btnLeftHalf = MakeQuickChip("◧ Nửa trái", () =>
            {
                UpdateSelectionRect(new Rect(0, 0, Math.Round(Width / 2), Height), showActions: true);
            });
            var btnRightHalf = MakeQuickChip("◨ Nửa phải", () =>
            {
                UpdateSelectionRect(new Rect(Math.Round(Width / 2), 0, Math.Round(Width / 2), Height), showActions: true);
            });
            var btnCancelTop = MakeQuickChip("✕ Hủy (Esc)", () =>
            {
                SelectedRegion = null;
                DialogResult = false;
            }, isDanger: true);

            stack.Children.Add(btnSmartClassArea);
            stack.Children.Add(btnLeftHalf);
            stack.Children.Add(btnRightHalf);
            stack.Children.Add(btnCancelTop);

            bar.Child = stack;
            return bar;
        }

        private void BuildFloatingActionDock()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };

            var btnConfirm = new Button
            {
                Content = "✅ Chiếu vùng này (Enter)",
                MinHeight = 40,
                Padding = new Thickness(16, 6, 16, 6),
                Background = new SolidColorBrush(Color.FromRgb(22, 163, 74)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnConfirm.Click += (_, _) => ConfirmSelection();

            var btnRedraw = new Button
            {
                Content = "🔄 Kéo chọn lại",
                MinHeight = 40,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnRedraw.Click += (_, _) =>
            {
                _currentDipRect = Rect.Empty;
                _selectionFrame.Visibility = Visibility.Collapsed;
                _sizeBadge.Visibility = Visibility.Collapsed;
                _actionDock.Visibility = Visibility.Collapsed;
                UpdateMaskGeometry(Rect.Empty);
            };

            var btnCancel = new Button
            {
                Content = "Hủy",
                MinHeight = 40,
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (_, _) =>
            {
                SelectedRegion = null;
                DialogResult = false;
            };

            row.Children.Add(btnConfirm);
            row.Children.Add(btnRedraw);
            row.Children.Add(btnCancel);
            _actionDock.Child = row;
        }

        private static Button MakeQuickChip(string text, Action onClick, bool isDanger = false)
        {
            var btn = new Button
            {
                Content = text,
                MinHeight = 36,
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(4, 0, 0, 0),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Background = isDanger
                    ? new SolidColorBrush(Color.FromRgb(220, 38, 38))
                    : new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White
            };
            btn.Click += (_, e) =>
            {
                e.Handled = true;
                onClick();
            };
            return btn;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dep && IsInsideInteractiveButton(dep))
                return;

            _dragStartDip = e.GetPosition(_canvas);
            _isDragging = true;
            _actionDock.Visibility = Visibility.Collapsed;
            _canvas.CaptureMouse();
            e.Handled = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pos = e.GetPosition(_canvas);
            double x = Math.Min(_dragStartDip.X, pos.X);
            double y = Math.Min(_dragStartDip.Y, pos.Y);
            double w = Math.Abs(pos.X - _dragStartDip.X);
            double h = Math.Abs(pos.Y - _dragStartDip.Y);

            if (w > 8 && h > 8)
            {
                UpdateSelectionRect(new Rect(x, y, w, h), showActions: false);
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            _canvas.ReleaseMouseCapture();

            if (_currentDipRect.Width >= 60 && _currentDipRect.Height >= 60)
            {
                UpdateSelectionRect(_currentDipRect, showActions: true);
            }
            e.Handled = true;
        }

        private static bool IsInsideInteractiveButton(DependencyObject? d)
        {
            while (d != null)
            {
                if (d is Button) return true;
                d = VisualTreeHelper.GetParent(d);
            }
            return false;
        }

        private void UpdateSelectionRect(Rect dipRect, bool showActions)
        {
            _currentDipRect = dipRect;
            UpdateMaskGeometry(dipRect);

            _selectionFrame.Width = dipRect.Width;
            _selectionFrame.Height = dipRect.Height;
            Canvas.SetLeft(_selectionFrame, dipRect.Left);
            Canvas.SetTop(_selectionFrame, dipRect.Top);
            _selectionFrame.Visibility = Visibility.Visible;

            int pxW = Math.Max(64, (int)Math.Round(dipRect.Width * _dpiScaleX));
            int pxH = Math.Max(64, (int)Math.Round(dipRect.Height * _dpiScaleY));
            _txtSizeBadge.Text = $"Vùng chiếu: {pxW} × {pxH} px";
            _sizeBadge.Visibility = Visibility.Visible;
            Canvas.SetLeft(_sizeBadge, Math.Max(8, dipRect.Left));
            Canvas.SetTop(_sizeBadge, dipRect.Top > 40 ? dipRect.Top - 34 : dipRect.Top + 8);

            if (showActions)
            {
                _actionDock.Visibility = Visibility.Visible;
                _actionDock.Measure(new Size(Width, Height));
                double dockW = _actionDock.DesiredSize.Width > 0 ? _actionDock.DesiredSize.Width : 360;
                double dockH = _actionDock.DesiredSize.Height > 0 ? _actionDock.DesiredSize.Height : 52;
                double dockLeft = Math.Clamp(dipRect.Right - dockW, 12, Math.Max(12, Width - dockW - 12));
                double dockTop = (dipRect.Bottom + dockH + 14 < Height)
                    ? dipRect.Bottom + 10
                    : Math.Max(12, dipRect.Bottom - dockH - 12);

                Canvas.SetLeft(_actionDock, dockLeft);
                Canvas.SetTop(_actionDock, dockTop);
            }
            else
            {
                _actionDock.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateMaskGeometry(Rect holeRect)
        {
            var fullGeo = new RectangleGeometry(new Rect(0, 0, Width, Height));
            if (holeRect.IsEmpty || holeRect.Width < 4 || holeRect.Height < 4)
            {
                _dimMask.Data = fullGeo;
            }
            else
            {
                var holeGeo = new RectangleGeometry(holeRect);
                _dimMask.Data = new CombinedGeometry(GeometryCombineMode.Exclude, fullGeo, holeGeo);
            }
        }

        private void ConfirmSelection()
        {
            if (_currentDipRect.Width < 60 || _currentDipRect.Height < 60) return;

            int pxX = Math.Max(0, (int)Math.Round(_currentDipRect.Left * _dpiScaleX));
            int pxY = Math.Max(0, (int)Math.Round(_currentDipRect.Top * _dpiScaleY));
            int pxW = Math.Max(64, (int)Math.Round(_currentDipRect.Width * _dpiScaleX));
            int pxH = Math.Max(64, (int)Math.Round(_currentDipRect.Height * _dpiScaleY));

            // Ẩn cửa sổ phủ trước khi chụp thumbnail để ảnh xem trước không bị tối
            Visibility = Visibility.Hidden;
            var info = new ScreenSourceInfo
            {
                Kind = ScreenSourceKind.Region,
                IconGlyph = "✂️",
                Title = $"Vùng tùy chọn ({pxW}×{pxH})",
                Subtitle = $"Tọa độ ({pxX}, {pxY}) — Kích thước {pxW}×{pxH} px",
                X = pxX,
                Y = pxY,
                Width = pxW,
                Height = pxH,
                DipLeft = _currentDipRect.Left,
                DipTop = _currentDipRect.Top,
                DipWidth = _currentDipRect.Width,
                DipHeight = _currentDipRect.Height
            };
            info.Thumbnail = ScreenSourceEnumerator.CaptureThumbnail(info);
            SelectedRegion = info;
            DialogResult = true;
        }
    }
}

