using QRCoder;
using System.IO;
using System.Windows.Media.Imaging;

namespace SmartLibrary.Desktop.Helpers
{
    public static class QrCodeHelper
    {
        public static BitmapImage GenerateQrCode(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);
            
            var image = new BitmapImage();
            using (var ms = new MemoryStream(qrCodeAsPngByteArr))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }
            image.Freeze(); // Đảm bảo an toàn khi truyền dữ liệu giữa các Thread trong WPF
            return image;
        }
    }
}
