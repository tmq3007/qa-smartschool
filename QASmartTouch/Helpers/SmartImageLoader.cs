using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QASmartTouch.Helpers
{
    /// <summary>
    /// QC_4.2_SMART_IMAGE_LOADER: Bộ nạp ảnh thông minh đa nguồn dùng cho Bảng tương tác thông minh.
    /// Hỗ trợ nạp và giải mã an toàn mọi loại dữ liệu ảnh:
    /// 1. Base64 Data URI ("data:image/...;base64,...") từ Google Images thumbnails.
    /// 2. URL trực tuyến ("http://", "https://") với Browser Headers (User-Agent, Referer) vượt tường lửa chống hotlink (CGV, Pinterest, Facebook).
    /// 3. Định dạng ảnh hiện đại: WebP, AVIF, PNG, JPEG, BMP, GIF (fallback qua OpenCvSharp).
    /// 4. Kéo thả trực tiếp từ trình duyệt web hoặc Windows File Explorer (Drag & Drop).
    /// </summary>
    public static class SmartImageLoader
    {
        private static readonly HttpClient _httpClient;

        static SmartImageLoader()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add(
                "Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
        }

        /// <summary>
        /// Nạp hình ảnh từ bất kỳ định dạng URL, Data URI hoặc đường dẫn file cục bộ
        /// </summary>
        public static async Task<BitmapSource> LoadImageAsync(string source, string? referrerUrl = null)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Đường dẫn hình ảnh không hợp lệ.", nameof(source));

            source = source.Trim();

            // 1. Dạng Base64 Data URI (data:image/...;base64,...)
            if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                int commaIndex = source.IndexOf(',');
                if (commaIndex >= 0)
                {
                    string base64Str = source.Substring(commaIndex + 1).Trim();
                    byte[] bytes = Convert.FromBase64String(base64Str);
                    return DecodeBytes(bytes);
                }
            }

            // 2. Dạng File cục bộ (file:/// hoặc C:\...)
            if (source.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var fileUri = new Uri(source);
                    source = fileUri.LocalPath;
                }
                catch { }
            }

            if (File.Exists(source))
            {
                byte[] bytes = await File.ReadAllBytesAsync(source);
                return DecodeBytes(bytes);
            }

            // 3. Dạng URL trực tuyến (http:// hoặc https://)
            if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, source);

                // Gắn Referer thông minh để vượt tường lửa chống hotlinking
                if (!string.IsNullOrEmpty(referrerUrl) && Uri.TryCreate(referrerUrl, UriKind.Absolute, out var refUri))
                {
                    request.Headers.Referrer = refUri;
                }
                else if (Uri.TryCreate(source, UriKind.Absolute, out var targetUri))
                {
                    request.Headers.Referrer = new Uri(targetUri.GetLeftPart(UriPartial.Authority));
                }

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                response.EnsureSuccessStatusCode();

                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                return DecodeBytes(bytes);
            }

            throw new NotSupportedException($"Không hỗ trợ giao thức đường dẫn ảnh này: {source}");
        }

        /// <summary>
        /// Giải mã mảng byte thành BitmapSource với cơ chế fallback 3 lớp (WPF -> OpenCV WebP -> System.Drawing)
        /// </summary>
        public static BitmapSource DecodeBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("Dữ liệu byte rỗng.", nameof(bytes));

            // Lớp 1: WPF Native BitmapImage (PNG, JPEG, BMP, GIF, TIFF)
            try
            {
                using var ms = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception exWpf)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Native BitmapImage decode failed: {exWpf.Message}. Trying OpenCV fallback...");
            }

            // Lớp 2: OpenCvSharp decode (hỗ trợ WebP, AVIF, HDR, JPEG2000,...)
            try
            {
                using var mat = OpenCvSharp.Cv2.ImDecode(bytes, OpenCvSharp.ImreadModes.Unchanged);
                if (mat != null && !mat.Empty())
                {
                    OpenCvSharp.Cv2.ImEncode(".png", mat, out byte[] pngBytes);
                    using var msPng = new MemoryStream(pngBytes);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = msPng;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch (Exception exCv)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ OpenCV decode fallback failed: {exCv.Message}. Trying GDI fallback...");
            }

            // Lớp 3: System.Drawing GDI+ fallback
            try
            {
                using var ms = new MemoryStream(bytes);
                using var sysImg = System.Drawing.Image.FromStream(ms);
                using var bmp = new System.Drawing.Bitmap(sysImg);
                using var msPng = new MemoryStream();
                bmp.Save(msPng, System.Drawing.Imaging.ImageFormat.Png);
                msPng.Position = 0;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = msPng;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception exSys)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ GDI decode fallback failed: {exSys.Message}");
            }

            throw new InvalidOperationException("Không thể giải mã dữ liệu hình ảnh (định dạng không được hỗ trợ).");
        }

        /// <summary>
        /// Bóc tách và nạp BitmapSource từ dữ liệu Kéo-Thả (IDataObject từ Drag & Drop OLE/Windows)
        /// </summary>
        public static async Task<BitmapSource?> ExtractImageFromDataObjectAsync(IDataObject dataObject, string? referrerUrl = null)
        {
            if (dataObject == null) return null;

            // 1. Kéo thả file ảnh từ Windows File Explorer (DataFormats.FileDrop)
            if (dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                if (dataObject.GetData(DataFormats.FileDrop) is string[] files)
                {
                    foreach (var file in files)
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".webp")
                        {
                            return await LoadImageAsync(file, referrerUrl);
                        }
                    }
                }
            }

            // 2. Kéo thả Bitmap trực tiếp trong bộ nhớ
            if (dataObject.GetDataPresent(DataFormats.Bitmap))
            {
                var bmpObj = dataObject.GetData(DataFormats.Bitmap);
                if (bmpObj is BitmapSource bs)
                {
                    return bs;
                }
                if (bmpObj is System.Drawing.Bitmap gdiBmp)
                {
                    using var ms = new MemoryStream();
                    gdiBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }

            // 3. Kéo thả đoạn HTML từ trình duyệt web (chứa <img src="...">)
            if (dataObject.GetDataPresent(DataFormats.Html))
            {
                if (dataObject.GetData(DataFormats.Html) is string html)
                {
                    var match = Regex.Match(html, @"<img\s+[^>]*?src=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string imgSrc = WebUtility.HtmlDecode(match.Groups[1].Value);
                        return await LoadImageAsync(imgSrc, referrerUrl);
                    }
                }
            }

            // 4. Kéo thả đường dẫn ảnh dạng URL / Text
            string text = (dataObject.GetData(DataFormats.UnicodeText) as string)
                       ?? (dataObject.GetData(DataFormats.Text) as string) ?? "";
            text = text.Trim();

            if (!string.IsNullOrEmpty(text))
            {
                if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
                    File.Exists(text))
                {
                    return await LoadImageAsync(text, referrerUrl);
                }
            }

            // 5. Kéo thả UniformResourceLocator (IE/Edge format)
            if (dataObject.GetDataPresent("UniformResourceLocator"))
            {
                var urlObj = dataObject.GetData("UniformResourceLocator");
                if (urlObj is MemoryStream stream)
                {
                    using var reader = new StreamReader(stream);
                    string url = reader.ReadToEnd().Trim('\0', ' ', '\r', '\n');
                    if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        return await LoadImageAsync(url, referrerUrl);
                    }
                }
            }

            return null;
        }
    }
}
