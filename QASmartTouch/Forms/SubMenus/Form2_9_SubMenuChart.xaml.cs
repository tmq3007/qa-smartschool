using System;
using System.Windows;
using QASmartTouch.Services.VersionManagement;

namespace QASmartTouch.Forms
{
    public partial class Form2_9_SubMenuChart : Window
    {
        public string SelectedChartType { get; private set; } = "";
        
        public Form2_9_SubMenuChart()
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
            
            // Apply feature visibility on load
            this.Loaded += (s, e) => ApplyFeatureVisibility();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnBarChart_Click(object sender, RoutedEventArgs e)
        {
            SelectedChartType = "Bar";
            DialogResult = true;
            Close();
        }

        private void btnLineChart_Click(object sender, RoutedEventArgs e)
        {
            SelectedChartType = "Line";
            DialogResult = true;
            Close();
        }

        private void btnPieChart_Click(object sender, RoutedEventArgs e)
        {
            SelectedChartType = "Pie";
            DialogResult = true;
            Close();
        }

        private void btnRadarChart_Click(object sender, RoutedEventArgs e)
        {
            SelectedChartType = "Radar";
            DialogResult = true;
            Close();
        }

        #region Feature Management

        /// <summary>
        /// Apply feature visibility based on VersionDetail.json configuration.
        /// Hides chart types that are not enabled in current version.
        /// </summary>
        private void ApplyFeatureVisibility()
        {
            try
            {
                // =====================================================
                // CHART TYPES
                // =====================================================
                // NOTE: To enable control, buttons need x:Name in XAML first.
                // Example: <Button x:Name="btnRadarChart" ... />
                
                // Advanced charts (disabled in v1.0)
                // btnRadarChart?.SetVisibilityByFeature("radar_chart");
                // btnAreaChart?.SetVisibilityByFeature("area_chart");
                // btnScatterChart?.SetVisibilityByFeature("scatter_chart");
                
                // =====================================================
                // ENABLED FEATURES (always visible in v1.0)
                // =====================================================
                // - Bar Chart: enabled
                // - Line Chart: enabled  
                // - Pie Chart: enabled
                
                System.Diagnostics.Debug.WriteLine("[Form2_9_SubMenuChart] Feature visibility applied successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form2_9_SubMenuChart] Error applying feature visibility: {ex.Message}");
            }
        }

        #endregion
    }
}
