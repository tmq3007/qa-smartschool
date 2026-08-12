using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace QASmartTouch.Forms.ChemistryTools
{
    /// <summary>
    /// Wrapper to launch Periodic Table application
    /// </summary>
    public class PeriodicTableLauncher
    {
        private static readonly string PeriodicTablePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            @"..\..\..\New_Function\Bảng tuần hoàn_Đơn giản_2\PeriodicTableApp\bin\Debug\PeriodicTableApp.exe"
        );

        /// <summary>
        /// Launch Periodic Table application
        /// </summary>
        public static void Launch()
        {
            try
            {
                string fullPath = Path.GetFullPath(PeriodicTablePath);
                
                if (!File.Exists(fullPath))
                {
                    MessageBox.Show(
                        "Không tìm thấy ứng dụng Bảng tuần hoàn.\n" +
                        $"Đường dẫn: {fullPath}\n\n" +
                        "Vui lòng build PeriodicTableApp trước.",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Launch the application
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(fullPath)
                };

                Process.Start(startInfo);
                
                Debug.WriteLine($"✅ Launched Periodic Table: {fullPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi mở Bảng tuần hoàn:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Debug.WriteLine($"❌ Error launching Periodic Table: {ex.Message}");
            }
        }
    }
}
