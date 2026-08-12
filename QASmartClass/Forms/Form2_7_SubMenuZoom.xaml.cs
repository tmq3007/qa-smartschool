using System;
using System.Globalization;
using System.Windows;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_SubMenuZoom : Window
    {
        public double ZoomLevel { get; private set; } = 1.0;
        public string ZoomMode { get; private set; } = "Fixed";
        public bool UserSelected { get; private set; } = false;  // ✅ Flag to check if user clicked
        
        public Form2_7_SubMenuZoom()
        {
            InitializeComponent();
        }

        private void btnZoomLevel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag != null)
            {
                ZoomMode = "Fixed";
                ZoomLevel = double.Parse(button.Tag.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                UserSelected = true;  // ✅ User clicked
                
                // ✅ Safe DialogResult setter
                try
                {
                    if (this.IsLoaded)
                    {
                        DialogResult = true;
                    }
                }
                catch { }
                
                Close();
            }
        }

        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomMode = "Increment";
            ZoomLevel = 0.25;
            UserSelected = true;  // ✅ User clicked
            
            // ✅ Safe DialogResult setter
            try
            {
                if (this.IsLoaded)
                {
                    DialogResult = true;
                }
            }
            catch { }
            
            Close();
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomMode = "Decrement";
            ZoomLevel = 0.25;
            UserSelected = true;  // ✅ User clicked
            
            // ✅ Safe DialogResult setter
            try
            {
                if (this.IsLoaded)
                {
                    DialogResult = true;
                }
            }
            catch { }
            
            Close();
        }

        private void btnZoomArea_Click(object sender, RoutedEventArgs e)
        {
            ZoomMode = "Area";
            ZoomLevel = 0;
            UserSelected = true;  // ✅ User clicked
            
            // ✅ Safe DialogResult setter
            try
            {
                if (this.IsLoaded)
                {
                    DialogResult = true;
                }
            }
            catch { }
            
            Close();
        }
    }
}
