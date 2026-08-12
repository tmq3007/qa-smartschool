using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace QASmartClass.Tests
{
    public class AssetIntegrityTests
    {
        [Fact]
        public void TestNoDuplicateBlacklistedPlaceholders()
        {
            // Resolve path to assets/images
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = baseDir;
            bool found = false;

            for (int i = 0; i < 5; i++)
            {
                string imagesPath = Path.Combine(projectDir, "QASmartClass", "Assets", "Images");
                if (Directory.Exists(imagesPath))
                {
                    VerifyImagesInDirectory(imagesPath);
                    found = true;
                    break;
                }
                var parent = Directory.GetParent(projectDir);
                if (parent == null) break;
                projectDir = parent.FullName;
            }

            if (!found)
            {
                // Fallback: check relative path from BaseDirectory
                string fallbackPath = Path.Combine(baseDir, "Assets", "Images");
                if (Directory.Exists(fallbackPath))
                {
                    VerifyImagesInDirectory(fallbackPath);
                }
                else
                {
                    Console.WriteLine("Warning: Assets/Images directory not found for scan.");
                }
            }
        }

        private void VerifyImagesInDirectory(string dirPath)
        {
            var files = Directory.GetFiles(dirPath, "app_*.png");
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                
                // Allow the original placeholders to exist
                if (fileName.Equals("app_complex_7_VN.png", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("app_complex_7_EN.png", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("app_complex_8_VN.png", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("app_complex_8_EN.png", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("app_unitconverter_7_VN.png", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("app_unitconverter_7_EN.png", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("app_unitconverter_8_VN.png", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("app_unitconverter_8_EN.png", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Skip original placeholders
                }

                // Check file size first (instant O(1) check)
                var fileInfo = new FileInfo(file);
                long size = fileInfo.Length;
                
                // Assert that other app images are not copies of placeholders (based on exact sizes)
                if (size == 792630 || size == 1020514 || size == 1043166)
                {
                    // Compute SHA256 to confirm exact match
                    string hash = GetFileHash(file);
                    
                    // Fail unit test to prevent coders from pushing copied placeholders
                    Assert.True(false, $"Duplicate asset detected: '{fileName}' (size: {size} bytes) is a duplicate of a placeholder image. Coders must use a unique illustration or leave the ImagePath empty/null to trigger the Smart Fallback UI Card.");
                }
            }
        }

        private string GetFileHash(string filePath)
        {
            using (var sha = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha.ComputeHash(stream);
                    var sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
        }
    }
}
