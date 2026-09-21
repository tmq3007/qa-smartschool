using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartTouch.Forms
{
    public partial class Form5_1_ColorPicker : Window
    {
        private bool _isUpdating = false;
        public Color SelectedColor { get; private set; }

        public Form5_1_ColorPicker()
        {
            InitializeComponent();
            SelectedColor = Colors.Red;
            UpdatePreview();
        }

        public Form5_1_ColorPicker(Color initialColor) : this()
        {
            SelectedColor = initialColor;
            SetColorFromRgb(initialColor.R, initialColor.G, initialColor.B);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating) return;
            
            // Check if all controls are initialized
            if (sliderRed == null || sliderGreen == null || sliderBlue == null ||
                txtRed == null || txtGreen == null || txtBlue == null)
                return;
            
            _isUpdating = true;
            
            byte r = (byte)sliderRed.Value;
            byte g = (byte)sliderGreen.Value;
            byte b = (byte)sliderBlue.Value;

            txtRed.Text = r.ToString();
            txtGreen.Text = g.ToString();
            txtBlue.Text = b.ToString();

            SetColorFromRgb(r, g, b);
            
            _isUpdating = false;
        }

        private void txtRed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (sliderRed == null || sliderGreen == null || sliderBlue == null) return;
            if (byte.TryParse(txtRed.Text, out byte r))
            {
                _isUpdating = true;
                sliderRed.Value = r;
                SetColorFromRgb(r, (byte)sliderGreen.Value, (byte)sliderBlue.Value);
                _isUpdating = false;
            }
        }

        private void txtGreen_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (sliderRed == null || sliderGreen == null || sliderBlue == null) return;
            if (byte.TryParse(txtGreen.Text, out byte g))
            {
                _isUpdating = true;
                sliderGreen.Value = g;
                SetColorFromRgb((byte)sliderRed.Value, g, (byte)sliderBlue.Value);
                _isUpdating = false;
            }
        }

        private void txtBlue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            if (sliderRed == null || sliderGreen == null || sliderBlue == null) return;
            if (byte.TryParse(txtBlue.Text, out byte b))
            {
                _isUpdating = true;
                sliderBlue.Value = b;
                SetColorFromRgb((byte)sliderRed.Value, (byte)sliderGreen.Value, b);
                _isUpdating = false;
            }
        }

        private void txtHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;
            
            string hex = txtHex.Text.Trim();
            if (hex.Length == 6)
            {
                try
                {
                    Color color = (Color)ColorConverter.ConvertFromString("#" + hex);
                    _isUpdating = true;
                    SetColorFromRgb(color.R, color.G, color.B);
                    _isUpdating = false;
                }
                catch
                {
                    // Invalid hex color
                }
            }
        }

        private void btnQuickColor_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string colorHex)
            {
                try
                {
                    Color color = (Color)ColorConverter.ConvertFromString(colorHex);
                    SetColorFromRgb(color.R, color.G, color.B);
                }
                catch
                {
                    // Invalid color
                }
            }
        }

        private void SetColorFromRgb(byte r, byte g, byte b)
        {
            SelectedColor = Color.FromRgb(r, g, b);
            
            if (!_isUpdating)
            {
                _isUpdating = true;
                
                // ✅ Check if controls are initialized
                if (sliderRed != null && sliderGreen != null && sliderBlue != null)
                {
                    sliderRed.Value = r;
                    sliderGreen.Value = g;
                    sliderBlue.Value = b;
                }

                if (txtRed != null && txtGreen != null && txtBlue != null)
                {
                    txtRed.Text = r.ToString();
                    txtGreen.Text = g.ToString();
                    txtBlue.Text = b.ToString();
                }

                if (txtHex != null)
                {
                    txtHex.Text = $"{r:X2}{g:X2}{b:X2}";
                }
                
                _isUpdating = false;
            }
            else
            {
                if (txtHex != null)
                {
                    txtHex.Text = $"{r:X2}{g:X2}{b:X2}";
                }
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            // ✅ Check if colorPreview is initialized
            if (colorPreview == null) return;
            
            colorPreview.Background = new SolidColorBrush(SelectedColor);
            
            // Calculate luminance to determine text color (white or black)
            double luminance = (0.299 * SelectedColor.R + 0.587 * SelectedColor.G + 0.114 * SelectedColor.B) / 255;
            var textBlock = colorPreview.Child as TextBlock;
            if (textBlock != null)
            {
                textBlock.Foreground = new SolidColorBrush(luminance > 0.5 ? Colors.Black : Colors.White);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            try { this.Owner?.Activate(); this.Owner?.Focus(); } catch { }
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try { this.Owner?.Activate(); this.Owner?.Focus(); } catch { }
        }
    }
}
