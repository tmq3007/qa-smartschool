using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Cấu trúc chứa kết quả thực thi nhận diện chữ OCR
    /// Phân định rành rọt giữa Nhận diện Thành công và Ngoại lệ Lỗi hệ thống.
    /// </summary>
    public class OcrOperationResult
    {
        public bool IsSuccess { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service thực hiện OCR nhận diện chữ viết bằng thư viện ngoại tuyến Windows.Media.Ocr của hệ điều hành Windows 10/11.
    /// Hoạt động 100% offline, bảo mật cao và không phát sinh chi phí hoặc phụ thuộc mạng Internet.
    /// </summary>
    public class WindowsOCRService
    {
        private OcrEngine? _ocrEngine;

        public WindowsOCRService()
        {
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            try
            {
                // Ưu tiên thử en-US (hỗ trợ tuyệt đối tốt cho chữ số 0-9 và ký tự chữ Latin)
                _ocrEngine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"))
                          ?? OcrEngine.TryCreateFromUserProfileLanguages();

                if (_ocrEngine == null && OcrEngine.AvailableRecognizerLanguages.Count > 0)
                {
                    var defaultLang = OcrEngine.AvailableRecognizerLanguages[0];
                    if (defaultLang != null)
                    {
                        _ocrEngine = OcrEngine.TryCreateFromLanguage(defaultLang);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to initialize Windows OCR Engine: {ex.Message}");
            }
        }

        /// <summary>
        /// Thực hiện nhận diện chữ viết tay/in từ đối tượng BitmapSource của WPF
        /// Đảm bảo không bị sập luồng bộ nhớ ObjectDisposedException
        /// </summary>
        public async Task<OcrOperationResult> RecognizeTextAsync(BitmapSource bitmapSource)
        {
            if (bitmapSource == null)
            {
                return new OcrOperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Hình ảnh đầu vào không hợp lệ."
                };
            }

            if (_ocrEngine == null)
            {
                InitializeEngine();
                if (_ocrEngine == null)
                {
                    return new OcrOperationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Không thể khởi tạo công cụ OCR ngoại tuyến của Windows."
                    };
                }
            }

            try
            {
                // 1. Chuyển BitmapSource thành mảng byte PNG
                byte[] imageBytes;
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    imageBytes = ms.ToArray();
                }

                // 2. Ghi mảng byte vào luồng WinRT InMemoryRandomAccessStream an toàn
                using (var randomAccessStream = new InMemoryRandomAccessStream())
                {
                    using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(imageBytes);
                        await writer.StoreAsync();
                        await writer.FlushAsync();
                        writer.DetachStream(); // Tách luồng để giải phóng writer mà KHÔNG đóng randomAccessStream
                    }

                    randomAccessStream.Seek(0);

                    // 3. Giải mã hình ảnh và nhận diện OCR
                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(randomAccessStream);
                    using (SoftwareBitmap rawSoftwareBitmap = await decoder.GetSoftwareBitmapAsync())
                    {
                        // Chuyển đổi SoftwareBitmap sang định dạng Bgra8 với AlphaMode.Ignore (bắt buộc đối với Windows.Media.Ocr Engine)
                        using (SoftwareBitmap softwareBitmap = SoftwareBitmap.Convert(rawSoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore))
                        {
                            OcrResult result = await _ocrEngine.RecognizeAsync(softwareBitmap);

                            if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                            {
                                string cleanedText = result.Text.Trim();
                                System.Diagnostics.Debug.WriteLine($"✅ Windows OCR completed. Recognized text: '{cleanedText}'");
                                return new OcrOperationResult
                                {
                                    IsSuccess = true,
                                    Text = cleanedText
                                };
                            }

                            // Thử lại bằng tất cả ngôn ngữ khả dụng khác trên hệ điều hành (ví dụ: vi-VN, en-US)
                            foreach (var lang in OcrEngine.AvailableRecognizerLanguages)
                            {
                                try
                                {
                                    var altEngine = OcrEngine.TryCreateFromLanguage(lang);
                                    if (altEngine != null)
                                    {
                                        var altResult = await altEngine.RecognizeAsync(softwareBitmap);
                                        if (altResult != null && !string.IsNullOrWhiteSpace(altResult.Text))
                                        {
                                            string altText = altResult.Text.Trim();
                                            System.Diagnostics.Debug.WriteLine($"✅ Windows OCR ({lang.LanguageTag}) completed: '{altText}'");
                                            return new OcrOperationResult
                                            {
                                                IsSuccess = true,
                                                Text = altText
                                            };
                                        }
                                    }
                                }
                                catch { }
                            }

                            return new OcrOperationResult
                            {
                                IsSuccess = false,
                                ErrorMessage = "Không tìm thấy chữ hoặc chữ số hợp lệ trong vùng khoanh chọn."
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Windows OCR Exception: {ex.Message}");
                return new OcrOperationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lỗi xử lý OCR: {ex.Message}"
                };
            }
        }
    }
}
