using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using QASmartTouch.Managers;
using QASmartTouch.Services.VersionManagement;
using QASmartClass.Properties;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_SubMenuBoardManagement : Window
    {
        private Form2_MainDashboard? _mainDashboard;

        public Form2_7_SubMenuBoardManagement(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToSubMenu(this, btnClose);
            if (btnClose != null)
            {
                System.Windows.Input.Stylus.SetIsPressAndHoldEnabled(btnClose, false);

                btnClose.PreviewTouchDown += (s, e) =>
                {
                    e.TouchDevice.Capture(btnClose);
                    e.Handled = true;
                };

                btnClose.PreviewTouchUp += (s, e) =>
                {
                    if (e.TouchDevice.Captured == btnClose)
                    {
                        btnClose.ReleaseTouchCapture(e.TouchDevice);
                    }
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try 
                        {
                            this.Owner?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };

                btnClose.PreviewStylusDown += (s, e) =>
                {
                    e.StylusDevice.Capture(btnClose);
                    e.Handled = true;
                };

                btnClose.PreviewStylusUp += (s, e) =>
                {
                    if (e.StylusDevice.Captured == btnClose)
                    {
                        btnClose.ReleaseStylusCapture();
                    }
                    e.Handled = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try 
                        {
                            this.Owner?.Activate();
                            this.Close();
                        } 
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Normal);
                };
            }
            _mainDashboard = mainDashboard;
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyFeatureVisibility();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try { this.Owner?.Activate(); } catch { }
            this.Close();
        }

        private void btnBackgrounds_Click(object sender, RoutedEventArgs e)
        {
            var currentBoard = _mainDashboard?.BoardManager?.CurrentBoard;
            // Show background selection dialog with current board settings
            var bgSelector = new Form2_7_1_BackgroundSelector(
                currentBoard?.BackgroundColorHex,
                currentBoard?.BackgroundPattern,
                currentBoard?.BackgroundImagePath,
                currentBoard?.LineSpacing ?? 40,
                currentBoard?.LineOpacity ?? 25
            );
            
            // Position dialog centered
            bgSelector.Owner = this;
            bgSelector.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var result = bgSelector.ShowDialog();
            
            if (result == true)
            {
                if (_mainDashboard != null)
                {
                    if (bgSelector.SelectedBackgroundImage != null)
                    {
                        // Apply custom background image with file path
                        _mainDashboard.ApplyCanvasBackgroundImage(
                            bgSelector.SelectedBackgroundImage,
                            bgSelector.SelectedPattern,
                            bgSelector.SelectedBackgroundImagePath
                        );
                    }
                    else
                    {
                        // Apply color and pattern directly
                        _mainDashboard.ApplyCanvasBackground(
                            bgSelector.SelectedColor,
                            bgSelector.SelectedPattern,
                            bgSelector.LineSpacing,
                            bgSelector.LineOpacity
                        );
                    }
                }
                
                // Đóng sub-menu và đưa bảng chính về tiêu điểm hoạt động (tránh nhảy ra desktop)
                this.Close();
                _mainDashboard?.Activate();
                _mainDashboard?.Focus();
            }
        }

        private void btnMultiUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_mainDashboard == null)
                {
                    MessageBox.Show("Không tìm thấy bảng chính!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Open Multi-User Mode dialog
                var multiUserDialog = new Form2_7_4_MultiUserMode();
                multiUserDialog.Owner = Window.GetWindow(_mainDashboard);
                multiUserDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                multiUserDialog.Topmost = true;

                var result = multiUserDialog.ShowDialog();

                if (result == true)
                {
                    if (multiUserDialog.IsMultiUserEnabled)
                    {
                        // Khởi chạy chế độ Đa người dùng Umind Sandbox chuyên biệt
                        _mainDashboard.StartMultiUserSession(
                            multiUserDialog.Student1,
                            multiUserDialog.Student2
                        );
                    }
                    else
                    {
                        // Tắt chế độ đa người dùng nếu bỏ chọn
                        _mainDashboard.DisableMultiUserMode();
                    }

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"❌ Multi-user error: {ex.Message}");
            }
        }


        private void btnSpotlight_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard?.RegisterRecentTool(
                "board:Spotlight",
                "Đèn chiếu",
                QASmartTouch.Services.Tools.RecentToolsManager.SpotlightIconPath,
                Color.FromRgb(0xF5, 0x9E, 0x0B),
                () =>
                {
                    var tool = new Form2_7_4_MagnifierTool(false);
                    tool.ShowDialog();
                },
                "Dùng lại: Đèn chiếu Spotlight");

            // Close board menu first
            this.Close();
            
            // Open magnifier tool starting in spotlight mode
            var magnifierTool = new Form2_7_4_MagnifierTool(false);
            magnifierTool.ShowDialog();
        }


        private void btnCurtain_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard?.RegisterRecentTool(
                "board:Curtain",
                "Màn che",
                QASmartTouch.Services.Tools.RecentToolsManager.SpotlightIconPath,
                Color.FromRgb(0x6C, 0x5C, 0xE7),
                () =>
                {
                    var curtain = new Form2_18_ScreenCurtain();
                    curtain.Show();
                },
                "Dùng lại: Màn che bảng");

            // ✅ Activate Screen Curtain
            var screenCurtain = new Form2_18_ScreenCurtain();
            screenCurtain.Show();
            
            // Close submenu
            this.Close();
        }

        private void btnMagnifier_Click(object sender, RoutedEventArgs e)
        {
            _mainDashboard?.RegisterRecentTool(
                "board:Magnifier",
                "Kính lúp",
                QASmartTouch.Services.Tools.RecentToolsManager.SpotlightIconPath,
                Color.FromRgb(0x09, 0x84, 0xE3),
                () =>
                {
                    var tool = new Form2_7_4_MagnifierTool(true);
                    tool.ShowDialog();
                },
                "Dùng lại: Kính lúp phóng đại");

            // Close board menu first
            this.Close();
            
            // Open magnifier tool starting in magnifier mode
            var magnifierTool = new Form2_7_4_MagnifierTool(true);
            magnifierTool.ShowDialog();
        }

        private void btnScreenshot_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = _mainDashboard;
            // Close board menu first
            this.Close();
            
            // Open screenshot region selector
            var screenshotTool = new Form2_7_2_ScreenshotRegion();
            screenshotTool.ShowDialog();
            
            // ✨ FIX: Don't check DialogResult, just check PlaceOnBoard and CapturedImage
            if (screenshotTool.PlaceOnBoard && screenshotTool.CapturedImage != null)
            {
                dashboard?.RegisterRecentTool(
                    "board:Screenshot",
                    "Chụp vùng",
                    QASmartTouch.Services.Tools.RecentToolsManager.SelectRectIconPath,
                    Color.FromRgb(0x00, 0xB8, 0x94),
                    () =>
                    {
                        var shot = new Form2_7_2_ScreenshotRegion();
                        shot.ShowDialog();
                        if (shot.PlaceOnBoard && shot.CapturedImage != null)
                        {
                            dashboard?.AddImageToCanvas(shot.CapturedImage);
                        }
                    },
                    "Dùng lại: Chụp vùng màn hình đưa vào bảng");

                // Add image to canvas
                dashboard?.AddImageToCanvas(screenshotTool.CapturedImage);
            }
        }

        #region Feature Management

        /// <summary>
        /// Apply feature visibility based on VersionDetail.json configuration.
        /// Hides features that are not enabled in current version.
        /// </summary>
        private void ApplyFeatureVisibility()
        {
            try
            {
                // =====================================================
                // BOARD MANAGEMENT FEATURES
                // =====================================================
                
                // NOTE: These buttons don't have x:Name in XAML yet.
                // To enable feature control, add x:Name to buttons in XAML first.
                // Example: <Button x:Name="btnSpotlight" ... />
                
                // Spotlight - disabled in v1.0 (TODO: add x:Name in XAML)
                // btnSpotlight?.SetVisibilityByFeature("spotlight");
                
                // Multi-user mode - advanced feature (TODO: add x:Name in XAML)
                // btnMultiUser?.SetVisibilityByFeature("multi_user");
                
                // Screen Recording - disabled in v1.0
                // btnScreenRecording?.SetVisibilityByFeature("screen_recording");
                
                // =====================================================
                // ENABLED FEATURES (always visible in v1.0)
                // =====================================================
                // - Screen Curtain: enabled
                // - Countdown Timer: enabled
                // - Screenshot: enabled
                // - Magnifier: enabled
                // - Pointer: enabled
                // - Pan Canvas: enabled
                // - Backgrounds: enabled
                
                System.Diagnostics.Debug.WriteLine("[Form2_7_SubMenuBoardManagement] Feature visibility applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_7_SubMenuBoardManagement] Error applying feature visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ Đảm bảo khi đóng SubMenu thì MainDashboard luôn được kích hoạt lại trên cùng
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _mainDashboard?.Activate();
            _mainDashboard?.Focus();
        }

        #endregion
    }
}

