using System;
using System.Windows;
using System.Windows.Controls;

namespace QASmartTouch.Forms
{
    public partial class Form2_5_SubMenuZoom : Window
    {
        private double currentZoom = 100.0;

        public Form2_5_SubMenuZoom()
        {
            InitializeComponent();
            if (btnClose != null)
            {
                btnClose.PreviewTouchDown += (s, e) => { this.Close(); e.Handled = true; };
                btnClose.PreviewStylusDown += (s, e) => { this.Close(); e.Handled = true; };
            }
            UpdateZoomDisplay();
        }

        /// <summary>
        /// Update zoom percentage display
        /// </summary>
        private void UpdateZoomDisplay()
        {
            txtZoomValue.Text = $"{currentZoom:F0}%";
        }

        #region Event Handlers

        private void sliderZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentZoom = sliderZoom.Value;
            UpdateZoomDisplay();
        }

        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            currentZoom = Math.Max(50, currentZoom - 50);
            sliderZoom.Value = currentZoom;
        }

        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            currentZoom = Math.Min(400, currentZoom + 50);
            sliderZoom.Value = currentZoom;
        }

        private void btn100_Click(object sender, RoutedEventArgs e)
        {
            currentZoom = 100;
            sliderZoom.Value = currentZoom;
            this.DialogResult = true;
            this.Close();
        }

        private void btnFit_Click(object sender, RoutedEventArgs e)
        {
            // Fit to screen: Set to 100%
            currentZoom = 100;
            sliderZoom.Value = currentZoom;
            this.DialogResult = true;
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get current zoom percentage
        /// </summary>
        public double ZoomPercentage => currentZoom;

        #endregion
    }
}
