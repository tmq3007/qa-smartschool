using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using QASmartTouch.Services.VersionManagement;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_1_SubMenuPen : Window
    {
        private class BrushProfile
        {
            public int Size { get; set; }
            public Color Color { get; set; }
        }

        // Bộ nhớ cấu hình riêng biệt cho từng loại bút (duy trì suốt phiên làm việc)
        private static readonly Dictionary<string, BrushProfile> _brushProfiles = new Dictionary<string, BrushProfile>
        {
            ["Normal"] = new BrushProfile { Size = 5, Color = Colors.White },
            ["Shape"] = new BrushProfile { Size = 5, Color = Colors.White },
            ["Highlighter"] = new BrushProfile { Size = 10, Color = Color.FromRgb(255, 235, 59) },
            ["Laser"] = new BrushProfile { Size = 6, Color = Color.FromRgb(255, 59, 48) }
        };

        private static string NormalizeBrushKey(string? brushType) => (brushType ?? "Normal") switch
        {
            "Laser" => "Laser",
            "Highlighter" or "Marker" or "Mask" or "MaskPen" => "Highlighter",
            "Shape" or "Calligraphy" => "Shape",
            _ => "Normal"
        };

        // Current pen settings
        private string currentBrushType = "Normal";
        private int currentPenSize = 5;
        private Color currentPenColor = Colors.White;

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
                if (sliderPenSize != null)
                {
                    sliderPenSize.Value = currentPenSize;
                }
                InitializeSizeIndicators();
                UpdateColorUI();
                UpdateBrushTypeUI();
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
                    Tag = i,
                    Cursor = Cursors.Hand
                };

                // Cho phép chạm hoặc nhấp trực tiếp vào chấm tròn để chọn size ngay lập tức
                ellipse.MouseDown += (s, e) =>
                {
                    if (s is Ellipse el && el.Tag is int size)
                    {
                        sliderPenSize.Value = size;
                    }
                };
                ellipse.PreviewTouchDown += (s, e) =>
                {
                    if (s is Ellipse el && el.Tag is int size)
                    {
                        sliderPenSize.Value = size;
                        e.Handled = true;
                    }
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

            // Tùy biến preview theo loại bút
            switch (currentBrushType)
            {
                case "Shape" or "Calligraphy":
                    rectStrokePreview.Height = currentPenSize * 2.2;
                    rectStrokePreview.Opacity = 1.0;
                    break;
                case "Highlighter" or "Marker" or "Mask" or "MaskPen":
                    rectStrokePreview.Height = Math.Max(currentPenSize * 3.2, 12);
                    rectStrokePreview.Opacity = 0.45;
                    break;
                case "Laser":
                    rectStrokePreview.Height = currentPenSize * 2.2;
                    rectStrokePreview.Opacity = 0.95;
                    rectStrokePreview.Fill = new SolidColorBrush(currentPenColor);
                    break;
                default:
                    rectStrokePreview.Height = currentPenSize * 2;
                    rectStrokePreview.Opacity = 1.0;
                    break;
            }

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
            btnBrushNormal.Background = new SolidColorBrush(Color.FromRgb(255, 244, 230));     // #FFF4E6
            btnBrushShape.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245)); // #F3E5F5
            btnBrushHighlighter.Background = new SolidColorBrush(Color.FromRgb(255, 253, 231)); // #FFFDE7
            btnBrushLaser.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));       // #FFEBEE

            // Highlight selected button
            selectedButton.Background = new SolidColorBrush(Color.FromRgb(46, 134, 222));
        }

        #region Event Handlers

        /// <summary>
        /// Chuyển đổi loại bút với cơ chế lưu & khôi phục trạng thái riêng biệt cho từng loại bút
        /// </summary>
        private void SwitchToBrushType(string newBrushType)
        {
            string oldKey = NormalizeBrushKey(currentBrushType);
            string newKey = NormalizeBrushKey(newBrushType);

            // 1. Lưu cấu hình hiện tại của bút cũ
            if (_brushProfiles.ContainsKey(oldKey))
            {
                _brushProfiles[oldKey].Size = currentPenSize;
                _brushProfiles[oldKey].Color = currentPenColor;
            }

            // 2. Chuyển sang bút mới
            currentBrushType = newBrushType;

            // 3. Khôi phục cấu hình của bút mới
            if (_brushProfiles.TryGetValue(newKey, out var profile))
            {
                currentPenSize = profile.Size;
                currentPenColor = profile.Color;
            }

            // 4. Đồng bộ toàn bộ UI
            if (sliderPenSize != null) sliderPenSize.Value = currentPenSize;
            UpdateSizeIndicators(currentPenSize);
            UpdateColorUI();
            UpdatePreview();
        }

        private void btnBrushType_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                HighlightBrushType(button);
                SwitchToBrushType(button.Tag.ToString() ?? "Normal");
            }
        }

        private void btnBrushType_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Double-click on brush type = Select + Apply
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                HighlightBrushType(button);
                SwitchToBrushType(button.Tag.ToString() ?? "Normal");
                
                // Mark event as handled to prevent bubbling
                e.Handled = true;
                
                // Auto-apply (same as clicking "Áp dụng" button)
                btnApply_Click(this, new RoutedEventArgs());
            }
        }

        private void sliderPenSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentPenSize = (int)sliderPenSize.Value;
            string key = NormalizeBrushKey(currentBrushType);
            if (_brushProfiles.ContainsKey(key))
            {
                _brushProfiles[key].Size = currentPenSize;
            }
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
                
                string key = NormalizeBrushKey(currentBrushType);
                if (_brushProfiles.ContainsKey(key))
                {
                    _brushProfiles[key].Color = currentPenColor;
                }

                // Update custom color display
                txtCustomColorHex.Text = colorHex;
                ((Border)txtCustomColorHex.Parent).Background = new SolidColorBrush(currentPenColor);
                
                UpdateColorUI();
            }
        }

        private void btnColorPicker_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open advanced color picker dialog
                var colorPicker = new Form5_1_ColorPicker(currentPenColor);
                
                if (QASmartTouch.Shared.WindowHelper.ShowChildDialog(colorPicker, this) == true)
                {
                    // User selected a color
                    currentPenColor = colorPicker.SelectedColor;
                    
                    string key = NormalizeBrushKey(currentBrushType);
                    if (_brushProfiles.ContainsKey(key))
                    {
                        _brushProfiles[key].Color = currentPenColor;
                    }

                    // Update custom color display
                    txtCustomColorHex.Text = $"#{currentPenColor.R:X2}{currentPenColor.G:X2}{currentPenColor.B:X2}";
                    
                    if (txtCustomColorHex.Parent is Border border)
                    {
                        border.Background = new SolidColorBrush(currentPenColor);
                    }
                    
                    UpdateColorUI();
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
            _brushProfiles["Normal"] = new BrushProfile { Size = 5, Color = Colors.White };
            _brushProfiles["Shape"] = new BrushProfile { Size = 5, Color = Colors.White };
            _brushProfiles["Highlighter"] = new BrushProfile { Size = 10, Color = Color.FromRgb(255, 235, 59) };
            _brushProfiles["Laser"] = new BrushProfile { Size = 6, Color = Color.FromRgb(255, 59, 48) };

            currentBrushType = "Normal";
            currentPenSize = 5;
            currentPenColor = Colors.White;

            if (sliderPenSize != null) sliderPenSize.Value = 5;
            HighlightBrushType(btnBrushNormal);
            UpdateSizeIndicators(currentPenSize);
            UpdateColorUI();
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
                string key = NormalizeBrushKey(value);
                if (_brushProfiles.TryGetValue(key, out var profile))
                {
                    currentPenSize = profile.Size;
                    currentPenColor = profile.Color;
                }
                // Update UI if already loaded
                if (IsLoaded)
                {
                    if (sliderPenSize != null) sliderPenSize.Value = currentPenSize;
                    UpdateSizeIndicators(currentPenSize);
                    UpdateColorUI();
                    UpdateBrushTypeUI();
                }
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
                string key = NormalizeBrushKey(currentBrushType);
                if (_brushProfiles.ContainsKey(key))
                {
                    _brushProfiles[key].Size = value;
                }
                // Update UI if already loaded
                if (IsLoaded)
                {
                    if (sliderPenSize != null) sliderPenSize.Value = value;
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
                string key = NormalizeBrushKey(currentBrushType);
                if (_brushProfiles.ContainsKey(key))
                {
                    _brushProfiles[key].Color = value;
                }
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
                "Normal" or "Hoc" or "Simple" or "AI" => btnBrushNormal,
                "Shape" or "Calligraphy" => btnBrushShape,
                "Highlighter" or "Marker" or "Mask" or "MaskPen" => btnBrushHighlighter,
                "Laser" => btnBrushLaser,
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
