using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartClass.Properties; // ✨ Add for Settings
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_1_BackgroundSelector : Window
    {
        // ✨ Thay đổi từ single string → 2 properties riêng
        public string? SelectedColor { get; private set; }
        public string? SelectedPattern { get; private set; }
        public System.Windows.Media.Imaging.BitmapImage? SelectedBackgroundImage { get; private set; }
        public string? SelectedBackgroundImagePath { get; private set; }
        public int LineSpacing { get; private set; } = 40; // Default 40px (≈1.1cm)
        public int LineOpacity { get; private set; } = 10; // Default 10% (0-100)

        private Button? _lastSelectedColorButton;
        private Button? _lastSelectedPatternButton;
        private Button? _lastSelectedImageButton;

        public Form2_7_1_BackgroundSelector() : this(null, null, null, 40, 10)
        {
        }

        public Form2_7_1_BackgroundSelector(string? currentColor, string? currentPattern, string? currentImagePath = null, int lineSpacing = 40, int lineOpacity = 10)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToSubMenu(this, btnClose);
            
            LineSpacing = lineSpacing > 0 ? lineSpacing : 40;
            LineOpacity = lineOpacity > 0 ? lineOpacity : 10;

            // ✨ Sync sliders with default values
            LineOpacitySlider.Value = LineOpacity;
            LineSpacingSlider.Value = LineSpacing;
            
            // ✨ Nếu trang hiện tại đang dùng ảnh nền thì phục hồi ảnh nền và highlight nút Tải ảnh lên
            if (!string.IsNullOrEmpty(currentImagePath) && System.IO.File.Exists(currentImagePath))
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(currentImagePath, UriKind.Absolute);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    SelectedBackgroundImage = bitmap;
                    SelectedBackgroundImagePath = currentImagePath;
                    SelectedColor = null;
                    SelectedPattern = currentPattern;

                    UpdatePreviewWithImage();
                    if (btnLoadImage != null) HighlightButton(btnLoadImage, "image");
                    UpdateSpacingControlVisibility();
                    return;
                }
                catch
                {
                    // Fallback to color loading
                }
            }

            if (!string.IsNullOrEmpty(currentColor))
            {
                SelectedColor = currentColor;
                SelectedPattern = currentPattern;
            }
            else
            {
                // ✨ Load saved defaults
                LoadDefaults();
            }
            
            UpdatePreview();
            
            // Sync button highlights and spacing control visibility on load
            SyncPresetButtonsHighlight(SelectedColor, SelectedPattern);
            UpdateSpacingControlVisibility();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnBackground_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            string? tag = button.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            // ✨ Phân loại màu vs mẫu
            if (IsPattern(tag))
            {
                // ✨ Handle "None" pattern
                if (tag == "None")
                {
                    SelectedPattern = null; // No pattern
                }
                else
                {
                    SelectedPattern = tag;
                }
                HighlightButton(button, "pattern");
                
                // ✨ Show/hide spacing control based on pattern
                UpdateSpacingControlVisibility();
            }
            else
            {
                // ✨ Khi user chọn lại màu sắc, RESET ảnh nền đã tải lên để tránh bị kẹt ảnh cũ
                SelectedBackgroundImage = null;
                SelectedBackgroundImagePath = null;
                SelectedColor = tag;
                HighlightButton(button, "color");
            }

            // ✨ Cập nhật preview NGAY LẬP TỨC
            UpdatePreview();

            // ❌ KHÔNG đóng dialog - để user chọn cả 2
            // this.Close(); 
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            // ✨ Chỉ đóng khi nhấn "Áp dụng"
            this.DialogResult = true;
            this.Close();
        }

        // ✨ NEW: Quick Preset Handler
        private void btnPreset_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            
            string preset = button.Tag?.ToString() ?? "";
            ApplyPreset(preset);
        }

        private void ApplyPreset(string preset)
        {
            string color = "White";
            string? pattern = null;
            
            switch (preset)
            {
                case "Math":
                    color = "White";
                    pattern = "Grid";
                    break;
                    
                case "Literature":
                    color = "White";
                    pattern = "Lines";
                    break;
                    
                case "Music":
                    color = "White";
                    pattern = "MusicStaff";
                    break;
                    
                case "Presentation":
                    color = "#2F3542"; // Dark
                    pattern = null; // No pattern
                    break;
            }
            
            // Apply selection and reset custom image
            SelectedBackgroundImage = null;
            SelectedBackgroundImagePath = null;
            SelectedColor = color;
            SelectedPattern = pattern;
            
            // Update preview
            UpdatePreview();
            
            // Sync preset highlights and spacing control visibility
            SyncPresetButtonsHighlight(SelectedColor, SelectedPattern);
            UpdateSpacingControlVisibility();
        }

        private void SyncPresetButtonsHighlight(string? colorTag, string? patternTag)
        {
            FindAndHighlightButton(this, colorTag, "color");
            FindAndHighlightButton(this, patternTag, "pattern");
        }

        private void FindAndHighlightButton(DependencyObject parent, string? tagValue, string type)
        {
            if (tagValue == null && type == "pattern")
            {
                tagValue = "None";
            }
            
            if (string.IsNullOrEmpty(tagValue)) return;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Button button && button.Tag?.ToString() == tagValue)
                {
                    HighlightButton(button, type);
                    return;
                }
                else
                {
                    FindAndHighlightButton(child, tagValue, type);
                }
            }
        }

        // ✨ NEW: Load Defaults from Settings
        private void LoadDefaults()
        {
            try
            {
                // Load from settings
                string savedColor = Settings.Default.DefaultBackgroundColor;
                string? savedPattern = Settings.Default.DefaultBackgroundPattern;
                
                // Apply if valid
                if (!string.IsNullOrEmpty(savedColor))
                {
                    SelectedColor = savedColor;
                }
                else
                {
                    SelectedColor = "White"; // Fallback
                }
                
                if (!string.IsNullOrEmpty(savedPattern) && savedPattern != "None")
                {
                    SelectedPattern = savedPattern;
                }
                else
                {
                    SelectedPattern = null; // No pattern
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading defaults: {ex.Message}");
                // Use fallback defaults
                SelectedColor = "White";
                SelectedPattern = null;
            }
        }

        // ✨ NEW: Set Default Button Handler
        private void btnSetDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save to settings
                Settings.Default.DefaultBackgroundColor = SelectedColor ?? "White";
                Settings.Default.DefaultBackgroundPattern = SelectedPattern ?? string.Empty;
                Settings.Default.Save();
                
                // Show confirmation
                string message = "✅ Đã lưu làm mặc định!\n\n";
                message += $"• Màu nền: {GetColorDisplayName(SelectedColor)}\n";
                message += $"• Mẫu nền: {GetPatternDisplayName(SelectedPattern)}";
                
                MessageBox.Show(this, message, 
                               "Thành công", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Lỗi khi lưu cài đặt:\n{ex.Message}", 
                               "Lỗi", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Error);
            }
        }

        // ✨ Helper: Get display name for color
        private string GetColorDisplayName(string? color)
        {
            return color switch
            {
                "White" => "Trắng",
                "#F8F9FA" => "Xám nhạt",
                "#E3F2FD" => "Xanh nhạt",
                "#E8F5E9" => "Xanh lá nhạt",
                "#FFFDE7" => "Vàng nhạt",
                "#FCE4EC" => "Hồng nhạt",
                "#2F3542" => "Đen (trình chiếu)",
                "#3D6D64" => "Xanh đậm (bảng phấn)",
                _ => color ?? "Không xác định"
            };
        }

        // ✨ Helper: Get display name for pattern
        private string GetPatternDisplayName(string? pattern)
        {
            return pattern switch
            {
                "Grid" => "Lưới ô vuông",
                "Dots" => "Chấm tròn",
                "Lines" => "Kẻ ngang",
                "MusicStaff" => "Nhạc lý",
                null => "Không mẫu",
                _ => pattern
            };
        }

        private bool IsPattern(string? tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            return tag == "Grid" || tag == "Dots" || 
                   tag == "Lines" || tag == "MusicStaff" ||
                   tag == "None"; // ✨ Add None pattern
        }

        private void UpdatePreview()
        {
            System.Diagnostics.Debug.WriteLine($"🖼️ UpdatePreview: Color={SelectedColor}, Pattern={SelectedPattern}");
            
            // Hide hint text when user has made selections
            if (!string.IsNullOrEmpty(SelectedColor) || !string.IsNullOrEmpty(SelectedPattern))
            {
                PreviewHintText.Visibility = Visibility.Collapsed;
            }

            // Áp dụng màu nền
            if (!string.IsNullOrEmpty(SelectedColor))
            {
                try
                {
                    PreviewBackground.Fill = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(SelectedColor)
                    );
                }
                catch
                {
                    PreviewBackground.Fill = Brushes.White;
                }
            }

            // Áp dụng mẫu nền (overlay)
            if (!string.IsNullOrEmpty(SelectedPattern))
            {
                PreviewPattern.Fill = CreatePatternBrush(SelectedPattern);
            }
            else
            {
                PreviewPattern.Fill = null; // Xóa pattern nếu chưa chọn
            }
        }

        private void HighlightButton(Button button, string type)
        {
            if (type == "image")
            {
                // Khi chọn ảnh: dọn highlight nút màu
                if (_lastSelectedColorButton != null)
                {
                    _lastSelectedColorButton.BorderThickness = new Thickness(1);
                    _lastSelectedColorButton.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#DFE4EA")
                    );
                    _lastSelectedColorButton = null;
                }
                if (_lastSelectedImageButton != null)
                {
                    _lastSelectedImageButton.BorderThickness = new Thickness(1);
                    _lastSelectedImageButton.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#DFE4EA")
                    );
                }

                button.BorderThickness = new Thickness(3);
                button.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#667EEA")
                );
                _lastSelectedImageButton = button;
                return;
            }

            // Remove previous highlight
            if (type == "color")
            {
                // Dọn highlight nút ảnh nếu có
                if (_lastSelectedImageButton != null)
                {
                    _lastSelectedImageButton.BorderThickness = new Thickness(1);
                    _lastSelectedImageButton.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#DFE4EA")
                    );
                    _lastSelectedImageButton = null;
                }

                if (_lastSelectedColorButton != null)
                {
                    _lastSelectedColorButton.BorderThickness = new Thickness(1);
                    _lastSelectedColorButton.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#DFE4EA")
                    );
                }

                button.BorderThickness = new Thickness(3);
                button.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#667EEA")
                );
                _lastSelectedColorButton = button;
            }
            else if (type == "pattern")
            {
                if (_lastSelectedPatternButton != null)
                {
                    _lastSelectedPatternButton.BorderThickness = new Thickness(1);
                    _lastSelectedPatternButton.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#DFE4EA")
                    );
                }

                button.BorderThickness = new Thickness(3);
                button.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#667EEA")
                );
                _lastSelectedPatternButton = button;
            }
        }

        private Brush? CreatePatternBrush(string pattern)
        {
            System.Diagnostics.Debug.WriteLine($"🎨 CreatePatternBrush called with pattern: '{pattern}'");
            
            switch (pattern)
            {
                case "Grid":
                    System.Diagnostics.Debug.WriteLine("  → Matched 'Grid'");
                    return CreateGridPattern();
                case "Dots":
                    System.Diagnostics.Debug.WriteLine("  → Matched 'Dots'");
                    return CreateDotsPattern();
                case "Lines":
                    System.Diagnostics.Debug.WriteLine("  → Matched 'Lines'");
                    return CreateLinesPattern();
                case "MusicStaff":
                    System.Diagnostics.Debug.WriteLine("  → Matched 'MusicStaff'");
                    return CreateMusicStaffPattern();
                default:
                    System.Diagnostics.Debug.WriteLine($"  → NO MATCH! Returning null");
                    return null;
            }
        }

        private DrawingBrush CreateGridPattern()
        {
            var drawingBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, LineSpacing, LineSpacing), // Dynamic spacing
                ViewportUnits = BrushMappingMode.Absolute
            };

            var drawingGroup = new DrawingGroup();
            
            // Calculate alpha from opacity percentage (0-100 → 0-255)
            byte alpha = (byte)(LineOpacity * 255 / 100);
            
            // Vertical line
            drawingGroup.Children.Add(new GeometryDrawing
            {
                Pen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0)), 1),
                Geometry = new LineGeometry(new Point(0, 0), new Point(0, LineSpacing))
            });
            
            // Horizontal line
            drawingGroup.Children.Add(new GeometryDrawing
            {
                Pen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0)), 1),
                Geometry = new LineGeometry(new Point(0, 0), new Point(LineSpacing, 0))
            });

            drawingBrush.Drawing = drawingGroup;
            return drawingBrush;
        }

        private DrawingBrush CreateDotsPattern()
        {
            System.Diagnostics.Debug.WriteLine("🔵 CreateDotsPattern: radius=0.5, spacing=30x30, alpha=40");
            
            var drawingBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 30, 30),
                ViewportUnits = BrushMappingMode.Absolute
            };

            var drawingGroup = new DrawingGroup();
            
            // Dot
            drawingGroup.Children.Add(new GeometryDrawing
            {
                Brush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                Geometry = new EllipseGeometry(new Point(15, 15), 0.5, 0.5)
            });

            drawingBrush.Drawing = drawingGroup;
            return drawingBrush;
        }

        /// <summary>
        /// Create horizontal lines pattern (with dynamic spacing)
        /// </summary>
        private DrawingBrush CreateLinesPattern()
        {
            System.Diagnostics.Debug.WriteLine($"📏 CreateLinesPattern: spacing={LineSpacing}px");
            
            // Create drawing brush with dynamic spacing
            var brush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 1, LineSpacing), // Dynamic spacing
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None
            };

            // Create drawing group
            var group = new DrawingGroup();
            
            // Calculate alpha from opacity percentage (0-100 → 0-255)
            byte alpha = (byte)(LineOpacity * 255 / 100);
            
            // Draw a single horizontal line at the top of the tile
            var linePen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0)), 1);
            linePen.Freeze(); // Freeze for performance
            
            group.Children.Add(new GeometryDrawing
            {
                Pen = linePen,
                Geometry = new LineGeometry(new Point(0, 0), new Point(1, 0))
            });

            brush.Drawing = group;
            brush.Freeze(); // Freeze for performance
            
            return brush;
        }

        private DrawingBrush CreateMusicStaffPattern()
        {
            // Calculate total height for 5 lines with dynamic spacing
            int totalHeight = LineSpacing * 4; // 4 gaps between 5 lines
            
            var drawingBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 100, totalHeight),
                ViewportUnits = BrushMappingMode.Absolute
            };

            var drawingGroup = new DrawingGroup();
            
            // Calculate alpha from opacity percentage (0-100 → 0-255)
            byte alpha = (byte)(LineOpacity * 255 / 100);
            
            // 5 lines for music staff with dynamic spacing
            for (int i = 0; i < 5; i++)
            {
                drawingGroup.Children.Add(new GeometryDrawing
                {
                    Pen = new Pen(new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0)), 1),
                    Geometry = new LineGeometry(new Point(0, i * LineSpacing), new Point(100, i * LineSpacing))
                });
            }

            drawingBrush.Drawing = drawingGroup;
            return drawingBrush;
        }

        /// <summary>
        /// ✨ NEW: Event handler for loading custom background image
        /// </summary>
        private void btnLoadBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn ảnh nền",
                    Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Validate file size (max 10MB)
                    var fileInfo = new System.IO.FileInfo(openFileDialog.FileName);
                    if (fileInfo.Length > 10 * 1024 * 1024)
                    {
                        MessageBox.Show("Kích thước file quá lớn! Vui lòng chọn ảnh nhỏ hơn 10MB.",
                                      "Cảnh báo",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                        return;
                    }

                    // Load image
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // For better performance

                    SelectedBackgroundImage = bitmap;
                    SelectedBackgroundImagePath = openFileDialog.FileName;
                    
                    // Clear color and pattern selection when image is selected
                    SelectedColor = null;
                    SelectedPattern = null;
                    
                    // Update preview
                    UpdatePreviewWithImage();
                    
                    // Highlight nút Tải ảnh lên và dọn nút màu cũ
                    HighlightButton(btnLoadImage ?? (Button)sender, "image");
                    UpdateSpacingControlVisibility();
                    
                    System.Diagnostics.Debug.WriteLine($"📁 Loaded background image: {openFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải ảnh: {ex.Message}",
                              "Lỗi",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✨ Update preview with custom background image
        /// </summary>
        private void UpdatePreviewWithImage()
        {
            if (SelectedBackgroundImage != null)
            {
                PreviewHintText.Visibility = Visibility.Collapsed;
                
                // Set image as background
                var imageBrush = new ImageBrush
                {
                    ImageSource = SelectedBackgroundImage,
                    Stretch = Stretch.UniformToFill
                };
                
                PreviewBackground.Fill = imageBrush;
                PreviewPattern.Fill = null; // Clear pattern overlay
            }
        }

        /// <summary>
        /// ✨ NEW: Update spacing control visibility based on selected pattern
        /// </summary>
        private void UpdateSpacingControlVisibility()
        {
            // Show spacing control for Grid, Lines, and MusicStaff patterns
            if (SelectedPattern == "Grid" || SelectedPattern == "Lines" || SelectedPattern == "MusicStaff")
            {
                SpacingControl.Visibility = Visibility.Visible;
            }
            else
            {
                SpacingControl.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// ✨ NEW: Handle line spacing slider value changed
        /// </summary>
        private void LineSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LineSpacingSlider == null || SpacingValueText == null) return;
            
            // Update spacing value
            LineSpacing = (int)LineSpacingSlider.Value;
            
            // Update display text
            SpacingValueText.Text = $"{LineSpacing}px";
            
            // Update preview in real-time
            UpdatePreview();
            
            System.Diagnostics.Debug.WriteLine($"⚙️ Line spacing changed to: {LineSpacing}px");
        }

        /// <summary>
        /// ✨ NEW: Handle line opacity slider value changed
        /// </summary>
        private void LineOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LineOpacitySlider == null || OpacityValueText == null) return;
            
            // Update opacity value
            LineOpacity = (int)LineOpacitySlider.Value;
            
            // Update display text
            OpacityValueText.Text = $"{LineOpacity}%";
            
            // Update preview in real-time
            UpdatePreview();
            
            System.Diagnostics.Debug.WriteLine($"🎨 Line opacity changed to: {LineOpacity}%");
        }
    }
}
