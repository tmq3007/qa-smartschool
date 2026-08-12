using System.Windows;
using QASmartTouch.Forms;

namespace QASmartTouch
{
    /// <summary>
    /// Test launcher for Compass Tool 3D
    /// Usage: Call OpenCompassTool3D() from MainDashboard menu or create test button
    /// </summary>
    public class CompassTool3D_Launcher
    {
        /// <summary>
        /// Launch the 3D Compass Tool
        /// </summary>
        public static void Launch()
        {
            try
            {
                var compassTool3D = new Form2_18_CompassTool_3D();
                compassTool3D.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching 3D Compass Tool:\n{ex.Message}", 
                    "Launch Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Test method - Can be called from App.xaml.cs for standalone testing
        /// </summary>
        public static void TestStandalone()
        {
            var app = new Application();
            var window = new Form2_18_CompassTool_3D();
            app.Run(window);
        }
    }
}
