using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartLibrary.Desktop.Services
{
    public class QrScannerService
    {
        public bool IsCameraActive { get; private set; } = false;

        /// <summary>
        /// Bật camera — gọi trước khi quét
        /// </summary>
        public void StartCamera()
        {
            IsCameraActive = true;
            System.Diagnostics.Debug.WriteLine("[QrScanner] Camera STARTED");
        }

        /// <summary>
        /// Tắt camera — gọi khi screensaver hoặc cleanup
        /// </summary>
        public void StopCamera()
        {
            IsCameraActive = false;
            System.Diagnostics.Debug.WriteLine("[QrScanner] Camera STOPPED");
        }

        /// <summary>
        /// Quét QR Code — hỗ trợ CancellationToken để timeout
        /// </summary>
        /// <param name="ct">CancellationToken — sẽ throw OperationCanceledException nếu hết giờ</param>
        public async Task<string> ScanQrCodeAsync(CancellationToken ct = default)
        {
            // Giả lập độ trễ bật Camera và tìm QR Code (500ms - 2000ms)
            int delayMs = new Random().Next(500, 2000);
            await Task.Delay(delayMs, ct); // ct sẽ throw OperationCanceledException nếu hết giờ

            // Xác suất đọc thành công giả lập (90%)
            var random = new Random();
            if (random.Next(10) < 9)
            {
                // Trả về một mã SSO ngẫu nhiên (Student)
                return $"HS{random.Next(1000, 9999)}";
            }
            else
            {
                throw new Exception("Không tìm thấy mã QR hoặc ảnh quá mờ.");
            }
        }
    }
}
