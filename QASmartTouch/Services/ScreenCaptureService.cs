using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace QASmartTouch.Services
{
    // NOTE: Namespace cũ QASmartTouch.Services được giữ lại vì 160+ chỗ tham chiếu.
    // Nên sử dụng QASmartClass.Services cho code mới.
    // Alias: using ScreenCapture = QASmartTouch.Services.ScreenCaptureService;
    /// <summary>
    /// Service for capturing screenshots
    /// </summary>
    public class ScreenCaptureService
    {
        private string _defaultSavePath;

        public ScreenCaptureService()
        {
            // Default save path: Documents/QASmartTouch/Screenshots
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _defaultSavePath = Path.Combine(documentsPath, "QASmartTouch", "Screenshots");
            
            // Create directory if not exists
            Directory.CreateDirectory(_defaultSavePath);
            
            System.Diagnostics.Debug.WriteLine($"✅ ScreenCaptureService initialized. Save path: {_defaultSavePath}");
        }

        /// <summary>
        /// Capture full screen
        /// </summary>
        public Bitmap CaptureScreen()
        {
            try
            {
                // Use SystemParameters for screen dimensions
                var width = (int)SystemParameters.PrimaryScreenWidth;
                var height = (int)SystemParameters.PrimaryScreenHeight;
                var bitmap = new Bitmap(width, height);
                
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
                }
                
                System.Diagnostics.Debug.WriteLine($"📷 Captured screen: {width}x{height}");
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error capturing screen: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Capture specific area
        /// </summary>
        public Bitmap CaptureArea(Rectangle area)
        {
            try
            {
                var bitmap = new Bitmap(area.Width, area.Height);
                
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(area.X, area.Y, 0, 0, new System.Drawing.Size(area.Width, area.Height), CopyPixelOperation.SourceCopy);
                }
                
                System.Diagnostics.Debug.WriteLine($"📷 Captured area: {area.Width}x{area.Height} at ({area.X}, {area.Y})");
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error capturing area: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Save screenshot to file
        /// </summary>
        public string SaveScreenshot(Bitmap bitmap, string? customPath = null)
        {
            try
            {
                var fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var savePath = customPath ?? _defaultSavePath;
                var fullPath = Path.Combine(savePath, fileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(savePath);
                
                // Save as PNG
                bitmap.Save(fullPath, ImageFormat.Png);
                
                System.Diagnostics.Debug.WriteLine($"💾 Screenshot saved: {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error saving screenshot: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Capture and save screenshot in one call
        /// </summary>
        public string CaptureAndSave(string? customPath = null)
        {
            using (var bitmap = CaptureScreen())
            {
                return SaveScreenshot(bitmap, customPath);
            }
        }

        /// <summary>
        /// Set default save path
        /// </summary>
        public void SetDefaultSavePath(string path)
        {
            _defaultSavePath = path;
            Directory.CreateDirectory(path);
            System.Diagnostics.Debug.WriteLine($"📁 Save path updated: {path}");
        }

        /// <summary>
        /// Get default save path
        /// </summary>
        public string GetDefaultSavePath()
        {
            return _defaultSavePath;
        }

        /// <summary>
        /// Open screenshot folder
        /// </summary>
        public void OpenScreenshotFolder()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _defaultSavePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error opening folder: {ex.Message}");
            }
        }
    }
}
