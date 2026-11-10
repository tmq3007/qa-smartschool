using System;
using System.Windows;
using System.Windows.Controls;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_3_CanvasResize : Window
    {
        public int NewWidth { get; private set; }
        public int NewHeight { get; private set; }

        public Form2_7_3_CanvasResize(int currentWidth, int currentHeight)
        {
            InitializeComponent();
            
            // Set current size
            txtCurrentSize.Text = $"{currentWidth} × {currentHeight} px";
            txtWidth.Text = currentWidth.ToString();
            txtHeight.Text = currentHeight.ToString();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnPreset_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string tag)
            {
                var parts = tag.Split(',');
                if (parts.Length == 2)
                {
                    txtWidth.Text = parts[0];
                    txtHeight.Text = parts[1];
                }
            }
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtWidth.Text, out int width) && 
                int.TryParse(txtHeight.Text, out int height))
            {
                if (width < 100 || height < 100)
                {
                    MessageBox.Show("Kích thước tối thiểu là 100 × 100 px", 
                                  "Cảnh báo", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    return;
                }

                if (width > 10000 || height > 10000)
                {
                    MessageBox.Show("Kích thước tối đa là 10000 × 10000 px", 
                                  "Cảnh báo", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Warning);
                    return;
                }

                NewWidth = width;
                NewHeight = height;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Vui lòng nhập số hợp lệ!", 
                              "Lỗi", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }
    }
}
