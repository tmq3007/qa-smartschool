using Microsoft.Win32;
using QASmartTouch.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Navigation;

namespace QASmartTouch.Forms
{
    public partial class Form2_OCRSettingsDialog : Window
    {
        private string _apiKeyPath = string.Empty;
        private const string SETTINGS_FILE = "ocr_settings.txt";

        public string ApiKeyPath => _apiKeyPath;

        public Form2_OCRSettingsDialog()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SETTINGS_FILE))
                {
                    _apiKeyPath = File.ReadAllText(SETTINGS_FILE).Trim();
                    if (File.Exists(_apiKeyPath))
                    {
                        txtApiKeyPath.Text = _apiKeyPath;
                        txtStatus.Text = "✅ API key đã được cấu hình";
                        txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                        btnTest.IsEnabled = true;
                        btnSave.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to load settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                File.WriteAllText(SETTINGS_FILE, _apiKeyPath);
                Debug.WriteLine($"✅ Settings saved: {_apiKeyPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to save settings: {ex.Message}");
                MessageBox.Show($"Lỗi lưu cài đặt:\n{ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Chọn file Google Cloud Vision credentials"
            };

            if (dialog.ShowDialog() == true)
            {
                _apiKeyPath = dialog.FileName;
                txtApiKeyPath.Text = _apiKeyPath;
                txtStatus.Text = "⚠️ Chưa lưu. Nhấn 'Lưu' để áp dụng.";
                txtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                btnTest.IsEnabled = true;
                btnSave.IsEnabled = true;
            }
        }

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_apiKeyPath) || !File.Exists(_apiKeyPath))
            {
                MessageBox.Show("Vui lòng chọn file credentials trước!", "Thông báo",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnTest.IsEnabled = false;
            txtStatus.Text = "🔄 Đang kiểm tra kết nối...";
            txtStatus.Foreground = System.Windows.Media.Brushes.Blue;

            try
            {
                var ocrService = new GoogleVisionOCRService(_apiKeyPath);
                bool success = await ocrService.InitializeAsync();

                if (success)
                {
                    txtStatus.Text = "✅ Kết nối thành công!";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                    MessageBox.Show("✅ Kết nối Google Cloud Vision API thành công!\n\nBạn có thể sử dụng tính năng OCR.", 
                                  "Thành công",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    txtStatus.Text = "❌ Kết nối thất bại";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                    MessageBox.Show("❌ Không thể kết nối Google Cloud Vision API.\n\nVui lòng kiểm tra:\n• File credentials có đúng không\n• API đã được enable chưa\n• Service account có quyền chưa", 
                                  "Lỗi",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = "❌ Lỗi kết nối";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"Lỗi khi test kết nối:\n{ex.Message}", "Lỗi",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnTest.IsEnabled = true;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_apiKeyPath) || !File.Exists(_apiKeyPath))
            {
                MessageBox.Show("Vui lòng chọn file credentials hợp lệ!", "Thông báo",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveSettings();
            txtStatus.Text = "✅ Đã lưu cài đặt";
            txtStatus.Foreground = System.Windows.Media.Brushes.Green;
            
            MessageBox.Show("✅ Đã lưu cài đặt thành công!", "Thành công",
                          MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
