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
            try { this.Owner?.Activate(); } catch { }
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
