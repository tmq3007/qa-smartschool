using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Services.VersionManagement;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_1_SubMenuPen : Window
    {
        // Current pen settings
        private string currentBrushType = "Normal";
        private int currentPenSize = 4;
        private Color currentPenColor = Colors.Black;

        public bool IsApplied { get; private set; } = false;

        public Form2_1_SubMenuPen()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToSubMenu(this, btnClose);
            // QC_4.2_TOUCH_PIPELINE: Popup Window — WPF tự cô lập, KHÔNG cần ApplyTouchIsolation
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
            
            // Initialize after XAML is loaded
            this.Loaded += (s, e) =>
            {
                InitializeSizeIndicators();
                UpdatePreview();
                UpdateColorUI();
                HighlightBrushType(btnBrushNormal);
                ApplyFeatureVisibility();
            };
        }

        /// <summary>
        /// Initialize size indicator circles (1-16)
        /// </summary>
        private void InitializeSizeIndicators()
        {
            if (panelSizeIndicators == null) return;
            
            for (int i = 1; i <= 16; i++)
            {
                var ellipse = new Ellipse
                {
                    Width = Math.Min(i * 1.5, 18), // Scale size from 1.5px to 18px max
                    Height = Math.Min(i * 1.5, 18),
                    Fill = i <= currentPenSize ? new SolidColorBrush(Color.FromRgb(46, 134, 222)) : new SolidColorBrush(Color.FromRgb(220, 221, 225)),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Tag = i
                };

                panelSizeIndicators.Children.Add(ellipse);
            }
        }

        /// <summary>
        /// Update size indicators when slider changes
        /// </summary>
        private void UpdateSizeIndicators(int selectedSize)
        {
            if (panelSizeIndicators == null) return;
            
            foreach (Ellipse ellipse in panelSizeIndicators.Children)
            {
                int size = (int)ellipse.Tag;
                ellipse.Fill = size <= selectedSize 
                    ? new SolidColorBrush(Color.FromRgb(46, 134, 222)) 
                    : new SolidColorBrush(Color.FromRgb(220, 221, 225));
            }
        }

        /// <summary>
        /// Update stroke preview
        /// </summary>
        private void UpdatePreview()
        {
            if (rectStrokePreview == null) return; // ✅ Null check
            
            rectStrokePreview.Fill = new SolidColorBrush(currentPenColor);
            rectStrokePreview.Height = currentPenSize * 2; // Scale for visibility

            // Tự động điều chỉnh tương phản nền cho nét vẽ
            if (rectStrokePreview.Parent is Border parentBorder)
            {
                double luminance = (0.299 * currentPenColor.R + 0.587 * currentPenColor.G + 0.114 * currentPenColor.B) / 255.0;
                if (luminance > 0.8)
                {
                    // Nét vẽ màu sáng -> Dùng nền xanh lục bảng viết để dễ quan sát
                    parentBorder.Background = new SolidColorBrush(Color.FromRgb(61, 109, 100)); // #3D6D64
                }
                else
                {
                    // Nét vẽ màu tối -> Dùng nền xám sáng tiêu chuẩn
                    parentBorder.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)); // #F8F9FA
                }
            }
        }

        /// <summary>
        /// Highlight selected brush type
        /// </summary>
        private void HighlightBrushType(Button selectedButton)
        {
            if (btnBrushNormal == null) return; // ✅ Null check for buttons
            
            // Reset all brush type buttons to default background
            btnBrushNormal.Background = new SolidColorBrush(Color.FromRgb(255, 244, 230));
            btnBrushHoc.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
            btnBrushAI.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
            btnBrushSimple.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245));
            btnMarker.Background = new SolidColorBrush(Color.FromRgb(252, 228, 236));
            btnMaskPen.Background = new SolidColorBrush(Color.FromRgb(255, 249, 196));

            // Highlight selected button
            selectedButton.Background = new SolidColorBrush(Color.FromRgb(46, 134, 222));
        }

        #region Event Handlers

        private void btnBrushType_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                currentBrushType = button.Tag.ToString();
                HighlightBrushType(button);
                // No message box needed - just highlight the selected brush type
            }
        }

        private void btnBrushType_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Double-click on brush type = Select + Apply
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                currentBrushType = button.Tag.ToString();
                HighlightBrushType(button);
                
                // Mark event as handled to prevent bubbling
                e.Handled = true;
                
                // Auto-apply (same as clicking "Áp dụng" button)
                btnApply_Click(this, new RoutedEventArgs());
            }
        }

        private void sliderPenSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentPenSize = (int)sliderPenSize.Value;
            UpdateSizeIndicators(currentPenSize);
            UpdatePreview();
        }

        private void btnColor_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                string colorHex = button.Tag.ToString();
                currentPenColor = (Color)ColorConverter.ConvertFromString(colorHex);
                
                // Update custom color display
                txtCustomColorHex.Text = colorHex;
                ((Border)txtCustomColorHex.Parent).Background = new SolidColorBrush(currentPenColor);
                
                UpdatePreview();
            }
        }

        private void btnColorPicker_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open advanced color picker dialog
                var colorPicker = new Form5_1_ColorPicker(currentPenColor);
                colorPicker.Owner = this; // Set owner to center on this window
            
            if (colorPicker.ShowDialog() == true)
            {
                // User selected a color
                currentPenColor = colorPicker.SelectedColor;
                
                    // Update custom color display
                    txtCustomColorHex.Text = $"#{currentPenColor.R:X2}{currentPenColor.G:X2}{currentPenColor.B:X2}";
                    
                    if (txtCustomColorHex.Parent is Border border)
                    {
                        border.Background = new SolidColorBrush(currentPenColor);
                    }
                
                UpdatePreview();
            }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở bảng chọn màu:\n{ex.Message}", 
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            // Reset to default values
            currentBrushType = "Normal";
            currentPenSize = 4;
            currentPenColor = Colors.Black;

            sliderPenSize.Value = 4;
            HighlightBrushType(btnBrushNormal);
            
            txtCustomColorHex.Text = "#000000";
            ((Border)txtCustomColorHex.Parent).Background = new SolidColorBrush(Colors.Black);
            
            UpdatePreview();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            // Apply settings and close
            IsApplied = true;
            try
            {
                this.Owner?.Activate();
                this.Owner?.Focus();
            }
            catch { }
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Owner?.Activate();
                this.Owner?.Focus();
            }
            catch { }
            this.Close();
        }

        #endregion

        #region Public Properties for Parent Form

        /// <summary>
        /// Get or set current brush type
        /// </summary>
        public string BrushType 
        { 
            get => currentBrushType; 
            set 
            { 
                currentBrushType = value; 
                // Update UI if already loaded
                if (IsLoaded) UpdateBrushTypeUI();
            } 
        }
        
        /// <summary>
        /// Get or set current pen size
        /// </summary>
        public int PenSize 
        { 
            get => currentPenSize; 
            set 
            { 
                currentPenSize = value; 
                // Update UI if already loaded
                if (IsLoaded)
                {
                    sliderPenSize.Value = value;
                    UpdateSizeIndicators(value);
                    UpdatePreview();
                }
            } 
        }
        
        /// <summary>
        /// Get or set current pen color
        /// </summary>
        public Color PenColor 
        { 
            get => currentPenColor; 
            set 
            { 
                currentPenColor = value; 
                // Update UI if already loaded
                if (IsLoaded) UpdateColorUI();
            } 
        }

        // Helper methods for UI updates
        private void UpdateBrushTypeUI()
        {
            // Find and highlight the appropriate brush type button
            Button? targetButton = currentBrushType switch
            {
                "Normal" => btnBrushNormal,
                "Hoc" => btnBrushHoc,
                "AI" => btnBrushAI,
                "Simple" => btnBrushSimple,
                "Marker" => btnMarker,
                "Mask" => btnMaskPen,
                _ => btnBrushNormal
            };
            if (targetButton != null)
                HighlightBrushType(targetButton);
        }

        private void UpdateColorUI()
        {
            txtCustomColorHex.Text = $"#{currentPenColor.R:X2}{currentPenColor.G:X2}{currentPenColor.B:X2}";
            var border = (Border)txtCustomColorHex.Parent;
            border.Background = new SolidColorBrush(currentPenColor);
            
            // Thiết lập viền xám nhạt để phân biệt rõ khi nền custom color là màu trắng
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(225, 232, 237)); // #E1E8ED
            border.BorderThickness = new Thickness(1);

            // Tự động chọn màu chữ (đen/trắng) tương phản cao dựa trên độ sáng (Luminance) của màu nền
            double luminance = (0.299 * currentPenColor.R + 0.587 * currentPenColor.G + 0.114 * currentPenColor.B) / 255.0;
            txtCustomColorHex.Foreground = luminance > 0.7 
                ? new SolidColorBrush(Color.FromRgb(47, 53, 66))  // Chữ màu tối cho nền sáng
                : new SolidColorBrush(Colors.White);             // Chữ màu trắng cho nền tối

            UpdatePreview();
        }

        #endregion

        #region Feature Management

        /// <summary>
        /// Apply feature visibility based on VersionDetail.json configuration.
        /// Hides brush types that are not enabled in current version.
        /// </summary>
        private void ApplyFeatureVisibility()
        {
            try
            {
                // =====================================================
                // DRAWING TOOLS - BRUSH TYPES
                // =====================================================
                // NOTE: Premium brush types can be hidden in public release
                // To enable control, buttons need x:Name in XAML (already have)
                
                // Premium brushes (disabled in v1.0 public)
                // btnBrushAI?.SetVisibilityByFeature("ai_brush");
                
                // The following are available based on Tags in existing buttons:
                // - btnBrushNormal (Normal): always enabled
                // - btnBrushHoc (Hoc): enabled
                // - btnBrushAI (AI): could be premium
                // - btnBrushSimple (Simple): enabled
                // - btnMarker (Marker): enabled
                // - btnMaskPen (Mask): enabled
                
                // =====================================================
                // ENABLED FEATURES (always visible in v1.0)
                // =====================================================
                // - Normal Brush: enabled
                // - Highlighter: enabled  
                // - Marker: enabled
                // - Simple Brush: enabled
                // - Mask Pen: enabled
                // - Color picker: enabled
                // - Size slider: enabled
                
                System.Diagnostics.Debug.WriteLine("[Form2_1_SubMenuPen] Feature visibility applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_1_SubMenuPen] Error applying feature visibility: {ex.Message}");
            }
        }

        /// <summary>
        /// ✅ Đảm bảo khi đóng SubMenu Pen thì MainDashboard luôn được kích hoạt lại trên cùng
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                this.Owner?.Activate();
                this.Owner?.Focus();
            }
            catch { }
        }

        #endregion
    }
}
