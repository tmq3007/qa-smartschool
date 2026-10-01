using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QASmartTouch.Controls
{
    public partial class ColorPicker : UserControl
    {
        #region Events

        public event EventHandler<Color>? ColorSelected;

        #endregion

        #region Constructor

        public ColorPicker()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Properties

        public Color SelectedColor { get; private set; } = Colors.Black;

        #endregion

        #region Event Handlers

        private void ColorSwatch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string hexColor)
            {
                try
                {
                    Color color = (Color)ColorConverter.ConvertFromString(hexColor);
                    SelectedColor = color;
                    ColorSelected?.Invoke(this, color);
                    
                    // Close picker after selection
                    this.Hide();
                }
                catch
                {
                    // Handle invalid color
                }
            }
        }

        private void btnCustomColor_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement custom color dialog
            // For now, default to black
            MessageBox.Show("Tính năng chọn màu tùy chỉnh sẽ được bổ sung sau.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Public Methods

        public void ShowAt(Point position)
        {
            Canvas.SetLeft(this, position.X);
            Canvas.SetTop(this, position.Y);
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
