using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using QRCoder;
using QASmartTouch.Helpers;
using QASmartTouch.Shared;

namespace QASmartTouch.Forms
{
    public partial class Form2_21_ShareLectureDialog : Window
    {
        private readonly Form2_MainDashboard? _mainDashboard;
        private DispatcherTimer? _toastTimer;
        private string _shareContent = string.Empty;

        public Form2_21_ShareLectureDialog(Form2_MainDashboard? mainDashboard = null)
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            _mainDashboard = mainDashboard ?? Application.Current.MainWindow as Form2_MainDashboard;
            Owner = _mainDashboard;

            InitializeLectureInfoAndQr();
        }

        private void InitializeLectureInfoAndQr()
        {
            string lectureTitle = "BaiGiang_Moi";
            int boardCount = 1;

            if (_mainDashboard != null)
            {
                if (!string.IsNullOrWhiteSpace(_mainDashboard.CurrentLectureFilePath))
                {
                    lectureTitle = Path.GetFileNameWithoutExtension(_mainDashboard.CurrentLectureFilePath);
                    txtLectureName.Text = lectureTitle;
                    txtBadgeStatus.Text = "Đã lưu";
                    badgeStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 252, 231));
                    txtBadgeStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 128, 61));
                }
                else
                {
                    lectureTitle = $"BaiGiang_{DateTime.Now:yyyyMMdd_HHmm}";
                    txtLectureName.Text = $"{lectureTitle} (Bản nháp)";
                    txtBadgeStatus.Text = "Bản nháp";
                    badgeStatus.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 243, 199));
                    txtBadgeStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 83, 9));
                }

                if (_mainDashboard.BoardManager != null)
                {
                    boardCount = _mainDashboard.BoardManager.BoardCount;
                }
            }

            txtLectureStats.Text = $"Tổng cộng: {boardCount} trang bảng • Định dạng QA SmartClass (.qasc)";

            // Nội dung chuỗi chia sẻ cho mã QR và Copy Link
            // Định dạng deep link tương thích hệ sinh thái QA SmartClass
            _shareContent = $"qasmartclass://lecture/share?title={Uri.EscapeDataString(lectureTitle)}&pages={boardCount}&time={DateTime.Now:yyyyMMddHHmmss}";

            GenerateQrCode(_shareContent);
        }

        private void GenerateQrCode(string content)
        {
            try
            {
                using (var qrGenerator = new QRCodeGenerator())
                using (var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M))
                using (var qrCode = new QRCode(qrCodeData))
                using (Bitmap qrBitmap = qrCode.GetGraphic(20, Color.FromArgb(15, 23, 42), Color.White, true))
                {
                    using (var ms = new MemoryStream())
                    {
                        qrBitmap.Save(ms, ImageFormat.Png);
                        ms.Position = 0;

                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = ms;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();

                        imgQrCode.Source = bitmapImage;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ShareLectureDialog] Lỗi tạo mã QR: {ex.Message}");
            }
        }

        private void btnCopyLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_shareContent);
                ShowToast("Đã sao chép liên kết bài giảng vào bộ nhớ tạm!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể sao chép: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnSaveToUsb_Click(object sender, RoutedEventArgs e)
        {
            if (_mainDashboard == null)
            {
                MessageBox.Show("Không tìm thấy bảng vẽ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Title = "Xuất tệp bài giảng chia sẻ ra USB hoặc Thư mục",
                Filter = "Bài giảng QA SmartClass (*.qasc)|*.qasc",
                DefaultExt = ".qasc",
                FileName = $"{Path.GetFileNameWithoutExtension(txtLectureName.Text.Replace(" (Bản nháp)", ""))}.qasc"
            };

            if (saveDialog.ShowDialog(this) == true)
            {
                bool success = _mainDashboard.SaveCurrentLecture(saveDialog.FileName, showOpenFolderPrompt: true);
                if (success)
                {
                    ShowToast("✓ Đã xuất tệp bài giảng thành công!");
                }
            }
        }

        private void btnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            if (_mainDashboard != null)
            {
                var exportDialog = new Form2_20_ExportLectureDialog(_mainDashboard);
                WindowHelper.ShowChildDialog(exportDialog);
                _mainDashboard.Activate();
            }
        }

        private void btnSendEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string title = txtLectureName.Text.Replace(" (Bản nháp)", "");
                string subject = Uri.EscapeDataString($"[QA SmartClass] Chia sẻ bài giảng: {title}");
                string body = Uri.EscapeDataString($"Kính gửi,\n\nTôi xin chia sẻ nội dung bài giảng \"{title}\" được soạn trên hệ thống bảng tương tác QA SmartClass.\n\nTrân trọng!");
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"mailto:?subject={subject}&body={body}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở ứng dụng gửi thư: {ex.Message}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ShowToast(string message)
        {
            txtToastMessage.Text = message;
            panelToast.Visibility = Visibility.Visible;

            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _toastTimer.Tick += (s, e) =>
            {
                panelToast.Visibility = Visibility.Collapsed;
                _toastTimer.Stop();
            };
            _toastTimer.Start();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
