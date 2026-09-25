using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartTouch.Forms
{
    public partial class ColorPickerPopup : Window
    {
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;
        public event EventHandler<ThicknessChangedEventArgs>? ThicknessChanged;

        private Color _selectedColor = Colors.Red;
        private double _selectedThickness = 3;

        public ColorPickerPopup()
        {
            InitializeComponent();
            
            // Set initial values
            sliderThickness.Value = _selectedThickness;
            
            // Highlight default color (Red)
            btnColorRed.BorderBrush = new SolidColorBrush(Color.FromRgb(136, 192, 208));
            btnColorRed.BorderThickness = new Thickness(3);
            
            // Auto close when clicking outside
            this.Deactivated += (s, e) => 
            {
                System.Diagnostics.Debug.WriteLine("❌ ColorPickerPopup deactivated - auto closing");
                this.Close();
            };
            
            System.Diagnostics.Debug.WriteLine("✅ ColorPickerPopup initialized");
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
            this.Close();
        }

        public Color SelectedColor => _selectedColor;
        public double SelectedThickness => _selectedThickness;
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
