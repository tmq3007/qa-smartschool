using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// QC_4.2 SMART TOUCH: Floating Bottom Center Dock Pill (Thanh điều khiển đáy dạng viên thuốc)
    /// </summary>
    public partial class SmartDockPill : UserControl
    {
        private static readonly Brush BrandBrush = RadialToolDial.Hex("#2E86DE");
        private static readonly Brush TextBrush = RadialToolDial.Hex("#2F3542");
        private static readonly Brush SubTextBrush = RadialToolDial.Hex("#747D8C");
        private static readonly Brush LightBg = RadialToolDial.Hex("#F1F2F6");
        private static readonly Brush ActiveTabBg = RadialToolDial.Hex("#4F46E5");

        public event EventHandler<string>? ToolInvoked;
        public event EventHandler? PrevBoardClicked;
        public event EventHandler? NextBoardClicked;
        public event EventHandler? AddBoardClicked;
        public event EventHandler? BoardListClicked;
        public event EventHandler? ZoomOutClicked;
        public event EventHandler? ZoomInClicked;
        public event EventHandler? ZoomResetClicked;
        public event EventHandler? ZoomFitClicked;
        public event EventHandler? TouchModeToggleClicked;

        private int _selectedSubTab = 0;

        private sealed class SubTabDef
        {
            public string Title { get; set; } = "";
            public string Icon { get; set; } = "";
            public List<(string Key, string Label, string Icon, string Color)> Tools { get; set; } = new();
        }

        private static readonly List<SubTabDef> SubTabs = new()
        {
            new SubTabDef
            {
                Title = "4.1 Công cụ vẽ & Toán",
                Icon = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6Z",
                Tools = new()
                {
                    ("ruler", "Thước kẻ 20cm", "M2,7H22V17H2V7M4,9V12H5V9H7V11H8V9H10V12H11V9H13V11H14V9H16V12H17V9H19V11H20V9H20V15H4V9Z", "#2E86DE"),
                    ("compass", "Compa chuẩn", "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,6A6,6 0 0,1 18,12A6,6 0 0,1 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6Z", "#2E86DE"),
                    ("triangle", "Eke góc 45°/90°", "M2,2V22H22L2,2M5,7.8L16.2,19H5V7.8Z", "#2E86DE"),
                    ("protractor", "Thước đo độ 180°", "M12,4A9,9 0 0,0 3,13H21A9,9 0 0,0 12,4M12,6A7,7 0 0,1 18.9,11H5.1A7,7 0 0,1 12,6Z", "#2E86DE"),
                    ("keyboard", "Bàn phím ảo Smart", "M19,7H5A2,2 0 0,0 3,9V17A2,2 0 0,0 5,19H19A2,2 0 0,0 21,17V9A2,2 0 0,0 19,7M7,11H9V13H7V11M11,11H13V13H11V11M15,11H17V13H15V11M7,15H17V17H7V15Z", "#4F46E5"),
                }
            },
            new SubTabDef
            {
                Title = "4.2 Nội dung cơ bản",
                Icon = "M18.5,4L19.66,8.35L18.7,8.61C18.25,7.74 17.79,6.87 17.26,6.43C16.73,6 16.11,6 15.4,6H13V16.5C13,17 13.09,17.36 13.27,17.59C13.45,17.82 13.84,18 14.44,18.06V19H9.56V18.06C10.16,18 10.55,17.82 10.73,17.59C10.91,17.36 11,17 11,16.5V6H8.6C7.89,6 7.27,6 6.74,6.43C6.21,6.87 5.75,7.74 5.3,8.61L4.34,8.35L5.5,4H18.5Z",
                Tools = new()
                {
                    ("textbox", "Hộp văn bản mẫu", "M14,17H7V15H14M17,13H7V11H17M17,9H7V7H17M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3Z", "#2E86DE"),
                    ("table", "Bảng số liệu", "M19,3H5C3.9,3 3,3.9 3,5V19C3,20.1 3.9,21 5,21H19C20.1,21 21,20.1 21,19V5C21,3.9 20.1,3 19,3M19,5V8H5V5H19M5,10H10V14H5V10M12,10H19V14H12V10M5,16H10V19H5V16M12,16H19V19H12V16Z", "#2E86DE"),
                    ("image", "Tải ảnh lên", "M8.5,13.5L11,16.5L14.5,12L19,18H5M21,19V5C21,3.89 20.1,3 19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19Z", "#2E86DE"),
                    ("camera", "Camera thu hình", "M4,4H7L9,2H15L17,4H20A2,2 0 0,1 22,6V18A2,2 0 0,1 20,20H4A2,2 0 0,1 2,18V6A2,2 0 0,1 4,4M12,7A5,5 0 0,0 7,12A5,5 0 0,0 12,17A5,5 0 0,0 17,12A5,5 0 0,0 12,7M12,9A3,3 0 0,1 15,12A3,3 0 0,1 12,15A3,3 0 0,1 9,12A3,3 0 0,1 12,9Z", "#2E86DE"),
                    ("video", "Video minh họa", "M17,10.5V7A1,1 0 0,0 16,6H4A1,1 0 0,0 3,7V17A1,1 0 0,0 4,18H16A1,1 0 0,0 17,17V13.5L21,17.5V6.5L17,10.5Z", "#2E86DE"),
                    ("stickynote", "Sticky Note", "M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H15L21,15V5A2,2 0 0,0 19,3M14,19.5V14H19.5L14,19.5Z", "#F59E0B"),
                }
            },
            new SubTabDef
            {
                Title = "4.3 Biểu đồ & Đồ thị",
                Icon = "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z",
                Tools = new()
                {
                    ("chart-bar", "Biểu đồ Cột", "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z", "#2E86DE"),
                    ("chart-line", "Đường xu hướng", "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H2V17H4.54L9.77,8L16,11.78Z", "#2E86DE"),
                    ("graph-2d", "Đồ thị y=f(x)", "M7,2H9V6H11V8H9V12A2,2 0 0,0 11,14H13V16H11A4,4 0 0,1 7,12V8H5V6H7V2M15,18A2,2 0 0,0 17,20A2,2 0 0,0 19,18A2,2 0 0,0 17,16A2,2 0 0,0 15,18Z", "#4F46E5"),
                    ("graph-3d", "Desmos 3D", "M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5Z", "#7C3AED"),
                }
            },
            new SubTabDef
            {
                Title = "4.4 Môn học",
                Icon = "M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3M19,19H5V5H19V19Z",
                Tools = new()
                {
                    ("periodic", "Bảng tuần hoàn", "M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3M19,19H5V5H19V19Z", "#D97706"),
                    ("book", "SGK điện tử", "M18,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V4A2,2 0 0,0 18,2M6,4H11V12L8.5,10.5L6,12V4Z", "#2E86DE"),
                    ("geometry", "Hình học trực quan", "M11,13.5V21.5H3V13.5H11M12,2L17.5,11H6.5L12,2M17.5,13C20,13 22,15 22,17.5C22,20 20,22 17.5,22C15,22 13,20 13,17.5C13,15 15,13 17.5,13Z", "#059669"),
                    ("circuit", "Mạch điện ảo", "M2,11H6V13H2V11M22,11V13H18V11H22M9,7H11V17H9V7M13,9H15V15H13V9Z", "#DC2626"),
                }
            },
            new SubTabDef
            {
                Title = "4.5 Mô hình 3D",
                Icon = "M12,2L1,7L12,12L23,7L12,2M12,14.5L3.5,10.7V17.2L12,22L20.5,17.2V10.7L12,14.5Z",
                Tools = new()
                {
                    ("model-3d", "Mô hình 3D (OBJ/GLB)", "M12,2L1,7L12,12L23,7L12,2M12,14.5L3.5,10.7V17.2L12,22L20.5,17.2V10.7L12,14.5Z", "#7C3AED"),
                    ("phet", "Mô phỏng PhET", "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M10,16.5V7.5L16,12L10,16.5Z", "#059669"),
                }
            },
            new SubTabDef
            {
                Title = "4.6 Trợ lý AI",
                Icon = "M19,10.5V13.5H17.5V10.5H19M7.5,10.5V13.5H6V10.5H7.5M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4Z",
                Tools = new()
                {
                    ("ai-quiz", "Tạo câu hỏi AI", "M9,22A1,1 0 0,1 8,21V18H4A2,2 0 0,1 2,16V4C2,2.89 2.9,2 4,2H20A2,2 0 0,1 22,4V16A2,2 0 0,1 20,18H13.9L10.2,21.71C10,21.9 9.75,22 9.5,22V22H9M11,6V9H13V6H11M11,11V13H13V11H11Z", "#4F46E5"),
                    ("ai-chat", "Trợ lý AI SmartClass", "M12,2A2,2 0 0,1 14,4C14,4.74 13.6,5.39 13,5.73V7H14A7,7 0 0,1 21,14H22A1,1 0 0,1 23,15V18A1,1 0 0,1 22,19H21V20A2,2 0 0,1 19,22H5A2,2 0 0,1 3,20V19H2A1,1 0 0,1 1,18V15A1,1 0 0,1 2,14H3A7,7 0 0,1 10,7H11V5.73C10.4,5.39 10,4.74 10,4A2,2 0 0,1 12,2M7.5,13A2.5,2.5 0 0,0 5,15.5A2.5,2.5 0 0,0 7.5,18A2.5,2.5 0 0,0 10,15.5A2.5,2.5 0 0,0 7.5,13M16.5,13A2.5,2.5 0 0,0 14,15.5A2.5,2.5 0 0,0 16.5,18A2.5,2.5 0 0,0 19,15.5A2.5,2.5 0 0,0 16.5,13Z", "#4F46E5"),
                }
            },
        };

        public SmartDockPill()
        {
            InitializeComponent();
            BuildFlyoutSubTabs();
            AttachEvents();
        }

        private void AttachEvents()
        {
            // Launcher toggles flyout
            btnInsertLauncher.Click += (s, e) => ToggleFlyout();
            ToolbarPointerHelper.AttachTap(btnInsertLauncher, ToggleFlyout, handleMouse: false);

            btnCloseFlyout.Click += (s, e) => flyoutInsertContent.Visibility = Visibility.Collapsed;
            ToolbarPointerHelper.AttachTap(btnCloseFlyout, () => flyoutInsertContent.Visibility = Visibility.Collapsed, handleMouse: false);

            // Fast tools
            AttachToolTap(btnToolRuler, "ruler");
            AttachToolTap(btnToolCompass, "compass");
            AttachToolTap(btnToolSetSquare, "setsquare");

            // Board navigation
            btnBoardPrev.Click += (s, e) => PrevBoardClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnBoardPrev, () => PrevBoardClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnBoardNext.Click += (s, e) => NextBoardClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnBoardNext, () => NextBoardClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnBoardAdd.Click += (s, e) => AddBoardClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnBoardAdd, () => AddBoardClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnBoardBadge.Click += (s, e) => BoardListClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnBoardBadge, () => BoardListClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            // Zoom launcher toggles mini popover
            btnZoomLauncher.Click += (s, e) => ToggleZoomPopover();
            ToolbarPointerHelper.AttachTap(btnZoomLauncher, ToggleZoomPopover, handleMouse: false);

            // Zoom mini controls
            btnZoomOut.Click += (s, e) => ZoomOutClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnZoomOut, () => ZoomOutClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnZoomIn.Click += (s, e) => ZoomInClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnZoomIn, () => ZoomInClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnZoomReset.Click += (s, e) => ZoomResetClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnZoomReset, () => ZoomResetClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnZoomFit.Click += (s, e) => ZoomFitClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnZoomFit, () => ZoomFitClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            btnTouchModeToggle.Click += (s, e) => TouchModeToggleClicked?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnTouchModeToggle, () => TouchModeToggleClicked?.Invoke(this, EventArgs.Empty), handleMouse: false);

            // Utilities
            AttachToolTap(btnUtilTimer, "timer");
            AttachToolTap(btnUtilSpotlight, "spotlight");
            AttachToolTap(btnUtilCurtain, "curtain");

            // Actions
            AttachToolTap(btnDesktopMode, "desktop");
            AttachToolTap(btnExit, "exit");
        }

        private void AttachToolTap(Button btn, string key)
        {
            btn.Click += (s, e) => ToolInvoked?.Invoke(this, key);
            ToolbarPointerHelper.AttachTap(btn, () => ToolInvoked?.Invoke(this, key), handleMouse: false);
        }

        private void ToggleFlyout()
        {
            popoverZoomTouch.Visibility = Visibility.Collapsed;
            flyoutInsertContent.Visibility = (flyoutInsertContent.Visibility == Visibility.Visible)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void ToggleZoomPopover()
        {
            flyoutInsertContent.Visibility = Visibility.Collapsed;
            popoverZoomTouch.Visibility = (popoverZoomTouch.Visibility == Visibility.Visible)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public void CloseFlyouts()
        {
            flyoutInsertContent.Visibility = Visibility.Collapsed;
            popoverZoomTouch.Visibility = Visibility.Collapsed;
        }

        #region Flyout Tabs & Tools Builder

        private void BuildFlyoutSubTabs()
        {
            flyoutTabsHost.Children.Clear();
            for (int i = 0; i < SubTabs.Count; i++)
            {
                int index = i;
                var tabDef = SubTabs[i];

                var icon = new Path { Data = Geometry.Parse(tabDef.Icon), Fill = (i == _selectedSubTab) ? Brushes.White : SubTextBrush };
                var text = new TextBlock
                {
                    Text = tabDef.Title,
                    FontSize = 11.5,
                    FontWeight = (i == _selectedSubTab) ? FontWeights.Bold : FontWeights.SemiBold,
                    Foreground = (i == _selectedSubTab) ? Brushes.White : TextBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0),
                };
                text.SetResourceReference(TextBlock.FontFamilyProperty, "InterFont");

                var btn = new Button
                {
                    Height = 32,
                    Padding = new Thickness(10, 0, 10, 0),
                    Margin = new Thickness(0, 0, 4, 0),
                    Background = (i == _selectedSubTab) ? ActiveTabBg : Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Focusable = false,
                    Template = RadialToolDial.CreatePillTemplate(16),
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new Viewbox { Width = 14, Height = 14, VerticalAlignment = VerticalAlignment.Center,
                                          Child = new Canvas { Width = 24, Height = 24, Children = { icon } } },
                            text
                        }
                    }
                };

                btn.Click += (s, e) => SwitchSubTab(index);
                ToolbarPointerHelper.AttachTap(btn, () => SwitchSubTab(index), handleMouse: false);

                flyoutTabsHost.Children.Add(btn);
            }

            RenderSelectedTabTools();
        }

        private void SwitchSubTab(int index)
        {
            _selectedSubTab = index;
            BuildFlyoutSubTabs();
        }

        private void RenderSelectedTabTools()
        {
            flyoutToolsHost.Children.Clear();
            if (_selectedSubTab < 0 || _selectedSubTab >= SubTabs.Count) return;

            var currentTab = SubTabs[_selectedSubTab];
            foreach (var (key, label, iconData, colorHex) in currentTab.Tools)
            {
                var iconBrush = RadialToolDial.Hex(colorHex);
                var icon = new Path { Data = Geometry.Parse(iconData), Fill = iconBrush };
                var text = new TextBlock
                {
                    Text = label,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TextBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0),
                };
                text.SetResourceReference(TextBlock.FontFamilyProperty, "InterFont");

                var btn = new Button
                {
                    Height = 38,
                    Padding = new Thickness(12, 0, 14, 0),
                    Margin = new Thickness(3),
                    Background = RadialToolDial.Hex("#F8FAFC"),
                    BorderBrush = RadialToolDial.Hex("#E1E8ED"),
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Focusable = false,
                    Template = RadialToolDial.CreatePillTemplate(14),
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new Viewbox { Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center,
                                          Child = new Canvas { Width = 24, Height = 24, Children = { icon } } },
                            text
                        }
                    }
                };

                string k = key;
                btn.Click += (s, e) =>
                {
                    flyoutInsertContent.Visibility = Visibility.Collapsed;
                    ToolInvoked?.Invoke(this, k);
                };
                ToolbarPointerHelper.AttachTap(btn, () =>
                {
                    flyoutInsertContent.Visibility = Visibility.Collapsed;
                    ToolInvoked?.Invoke(this, k);
                }, handleMouse: false);

                flyoutToolsHost.Children.Add(btn);
            }
        }

        #endregion

        #region Public State Synchronization

        public void UpdateBoardDisplay(int current, int total)
        {
            txtBoardIndicator.Text = $"Bảng {current}/{Math.Max(1, total)}";
        }

        public void UpdateZoomDisplay(double zoomLevel)
        {
            string pct = $"{zoomLevel * 100:F0}%";
            txtZoomPercent.Text = pct;
            txtMiniZoomReset.Text = pct;
        }

        public void UpdateTouchModeDisplay(bool isSingleFinger)
        {
            txtTouchMode.Text = isSingleFinger ? "Cử chỉ 2 ngón" : "Đa chạm";
            btnTouchModeToggle.Background = isSingleFinger ? RadialToolDial.Hex("#2E86DE") : LightBg;
            txtTouchMode.Foreground = isSingleFinger ? Brushes.White : TextBrush;
            iconTouchMode.Fill = isSingleFinger ? Brushes.White : TextBrush;
        }

        #endregion
    }
}
