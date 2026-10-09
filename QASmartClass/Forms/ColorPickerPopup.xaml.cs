using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QASmartTouch.Forms
{
    public partial class ColorPickerPopup : Window
    {
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;
        public event EventHandler<ThicknessChangedEventArgs>? ThicknessChanged;
        public event EventHandler<string>? BrushTypeChanged;

        private Color _selectedColor = Colors.Red;
        private double _selectedThickness = 3;
        private string _selectedBrushType = "Normal";
        private bool _isClosing = false;

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
            base.OnClosing(e);
        }

        private void SafeClose()
        {
            if (_isClosing) return;
            _isClosing = true;
            try
            {
                this.Close();
            }
            catch (InvalidOperationException)
            {
                // Triệt tiêu ngoại lệ nếu cửa sổ đang ở giữa chu trình đóng
            }
        }

        public ColorPickerPopup() : this(Colors.Red, 3, "Normal")
        {
        }

        public ColorPickerPopup(Color initialColor, double initialThickness, string initialBrushType = "Normal")
        {
            InitializeComponent();
            // QC_4.2_TOUCH_ACTIVATION: Đảm bảo ColorPickerPopup nhận diện cảm ứng 1 chạm ngay lập tức
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(this);
            
            _selectedColor = initialColor;
            _selectedThickness = initialThickness;
            _selectedBrushType = string.IsNullOrEmpty(initialBrushType) ? "Normal" : initialBrushType;
            
            // Set initial values
            sliderThickness.Value = _selectedThickness;
            
            // Highlight initial color button
            HighlightMatchingColorButton(initialColor);
            
            // Update Pen Mode button states
            UpdateBrushTypeUI();
            
            // Auto close when clicking outside (gọi qua SafeClose để chống double-close)
            this.Deactivated += (s, e) => 
            {
                System.Diagnostics.Debug.WriteLine("❌ ColorPickerPopup deactivated - auto closing");
                SafeClose();
            };
            
            System.Diagnostics.Debug.WriteLine($"✅ ColorPickerPopup initialized (color={initialColor}, thickness={initialThickness:F0}px, brush={_selectedBrushType})");
        }

        private void btnModeNormal_Click(object sender, RoutedEventArgs e)
        {
            _selectedBrushType = "Normal";
            UpdateBrushTypeUI();
            BrushTypeChanged?.Invoke(this, _selectedBrushType);
            System.Diagnostics.Debug.WriteLine("✏️ Brush mode changed to: Normal");
        }

        private void btnModeShape_Click(object sender, RoutedEventArgs e)
        {
            _selectedBrushType = "Shape";
            UpdateBrushTypeUI();
            BrushTypeChanged?.Invoke(this, _selectedBrushType);
            System.Diagnostics.Debug.WriteLine("📐 Brush mode changed to: Shape");
        }

        private void UpdateBrushTypeUI()
        {
            if (btnModeNormal == null || btnModeShape == null) return;
            var activeBg = new SolidColorBrush(Color.FromRgb(92, 107, 192)); // #5C6BC0
            var inactiveBg = Brushes.Transparent;
            var activeFg = Brushes.White;
            var inactiveFg = new SolidColorBrush(Color.FromRgb(216, 222, 233)); // #D8DEE9

            if (_selectedBrushType == "Shape")
            {
                btnModeNormal.Background = inactiveBg;
                btnModeNormal.Foreground = inactiveFg;
                btnModeShape.Background = activeBg;
                btnModeShape.Foreground = activeFg;
            }
            else
            {
                btnModeNormal.Background = activeBg;
                btnModeNormal.Foreground = activeFg;
                btnModeShape.Background = inactiveBg;
                btnModeShape.Foreground = inactiveFg;
            }
        }

        /// <summary>
        /// Highlight the color button matching the given color
        /// </summary>
        private void HighlightMatchingColorButton(Color color)
        {
            ResetColorButtonBorders();
            
            string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            Button? matchBtn = null;
            
            if (gridColors != null)
            {
                foreach (var child in gridColors.Children)
                {
                    if (child is Button btn && btn.Tag is string tag && 
                        string.Equals(tag, hex, StringComparison.OrdinalIgnoreCase))
                    {
                        matchBtn = btn;
                        break;
                    }
                }
            }
            
            if (matchBtn != null)
            {
                matchBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(136, 192, 208));
                matchBtn.BorderThickness = new Thickness(3);
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string colorHex)
            {
                try
                {
                    _selectedColor = (Color)ColorConverter.ConvertFromString(colorHex);
                    
                    // Reset all button borders
                    ResetColorButtonBorders();
                    
                    // Highlight selected button
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(136, 192, 208)); // Blue
                    button.BorderThickness = new Thickness(3);
                    
                    // Raise event
                    ColorChanged?.Invoke(this, new ColorChangedEventArgs(_selectedColor));
                    
                    System.Diagnostics.Debug.WriteLine($"🎨 Color selected: {colorHex}");

                    SafeClose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error selecting color: {ex.Message}");
                }
            }
        }

        private void ResetColorButtonBorders()
        {
            // Reset all color buttons
            btnColorRed.BorderThickness = new Thickness(2);
            btnColorOrange.BorderThickness = new Thickness(2);
            btnColorYellow.BorderThickness = new Thickness(2);
            btnColorGreen.BorderThickness = new Thickness(2);
            btnColorBlue.BorderThickness = new Thickness(2);
            btnColorPurple.BorderThickness = new Thickness(2);
            btnColorWhite.BorderThickness = new Thickness(2);
            btnColorGray.BorderThickness = new Thickness(2);
            btnColorBlack.BorderThickness = new Thickness(2);
            btnColorPink.BorderThickness = new Thickness(2);
            btnColorCyan.BorderThickness = new Thickness(2);
            btnColorBrown.BorderThickness = new Thickness(2);
            
            // Reset border color to transparent
            var transparentBrush = new SolidColorBrush(Colors.Transparent);
            btnColorRed.BorderBrush = transparentBrush;
            btnColorOrange.BorderBrush = transparentBrush;
            btnColorYellow.BorderBrush = transparentBrush;
            btnColorGreen.BorderBrush = transparentBrush;
            btnColorBlue.BorderBrush = transparentBrush;
            btnColorPurple.BorderBrush = transparentBrush;
            btnColorWhite.BorderBrush = transparentBrush;
            btnColorGray.BorderBrush = transparentBrush;
            btnColorBlack.BorderBrush = transparentBrush;
            btnColorPink.BorderBrush = transparentBrush;
            btnColorCyan.BorderBrush = transparentBrush;
            btnColorBrown.BorderBrush = transparentBrush;
        }

        private void sliderThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _selectedThickness = e.NewValue;
            
            if (txtThickness != null)
            {
                txtThickness.Text = $"{_selectedThickness:F0}px";
            }
            
            // Raise event
            ThicknessChanged?.Invoke(this, new ThicknessChangedEventArgs(_selectedThickness));
            
            System.Diagnostics.Debug.WriteLine($"📏 Thickness changed: {_selectedThickness:F0}px");
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("✅ Color picker confirmed - closing");
            SafeClose();
        }

        public Color SelectedColor => _selectedColor;
        public double SelectedThickness => _selectedThickness;
        public string SelectedBrushType => _selectedBrushType;
    }

    public class ColorChangedEventArgs : EventArgs
    {
        public Color Color { get; }
        
        public ColorChangedEventArgs(Color color)
        {
            Color = color;
        }
    }

    public class ThicknessChangedEventArgs : EventArgs
    {
        public double Thickness { get; }
        
        public ThicknessChangedEventArgs(double thickness)
        {
            Thickness = thickness;
        }
    }
}
