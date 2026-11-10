using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;
using QASmartTouch.Models;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service nhận dạng chữ viết tay sử dụng Google Cloud Vision API
    /// </summary>
    public class HandwritingRecognitionService
    {
        private readonly ImageAnnotatorClient? _visionClient;
        private readonly string _apiKey;
        private bool _isInitialized;

        #region Constructor

        public HandwritingRecognitionService(string apiKey)
        {
            _apiKey = apiKey;
            _isInitialized = false;

            try
            {
                // Initialize Google Cloud Vision client
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    // Set API key as environment variable
                    Environment.SetEnvironmentVariable("GOOGLE_API_KEY", apiKey);
                    
                    // Create client builder
                    var clientBuilder = new ImageAnnotatorClientBuilder
                    {
                        // Use API key authentication
                        ApiKey = apiKey
                    };

                    _visionClient = clientBuilder.Build();
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize Vision API: {ex.Message}");
                _isInitialized = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Kiểm tra xem service đã được khởi tạo thành công chưa
        /// </summary>
        public bool IsInitialized => _isInitialized && _visionClient != null;

        /// <summary>
        /// Nhận dạng chữ viết tay từ byte array (PNG/JPEG)
        /// </summary>
        /// <param name="imageBytes">Byte array của ảnh</param>
        /// <param name="maxResults">Số lượng kết quả tối đa (default: 5)</param>
        /// <returns>Danh sách kết quả nhận dạng, sắp xếp theo độ tin cậy</returns>
        public async Task<List<HandwritingRecognitionResult>> RecognizeHandwritingAsync(
            byte[] imageBytes, 
            int maxResults = 5)
        {
            if (!IsInitialized || _visionClient == null)
            {
                throw new InvalidOperationException("HandwritingRecognitionService chưa được khởi tạo. Vui lòng kiểm tra API key.");
            }

            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new ArgumentException("Image bytes không được rỗng.", nameof(imageBytes));
            }

            try
            {
                // Create Google Cloud Vision Image
                var image = Image.FromBytes(imageBytes);

                // Perform Document Text Detection (best for handwriting)
                var response = await _visionClient.DetectDocumentTextAsync(image);

                // Parse results
                var results = ParseRecognitionResults(response, maxResults);

                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Recognition error: {ex.Message}");
                throw new Exception($"Lỗi khi nhận dạng chữ viết: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Nhận dạng chữ viết tay từ file ảnh
        /// </summary>
        public async Task<List<HandwritingRecognitionResult>> RecognizeHandwritingFromFileAsync(
            string imagePath, 
            int maxResults = 5)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file: {imagePath}");
            }

            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            return await RecognizeHandwritingAsync(imageBytes, maxResults);
        }

        /// <summary>
        /// Test connection với Google Cloud Vision API
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            if (!IsInitialized || _visionClient == null)
            {
                return false;
            }

            try
            {
                // Create a simple test image (1x1 white pixel)
                var testImage = CreateTestImage();
                var image = Image.FromBytes(testImage);

                // Try to detect text (should return empty, but connection works)
                var response = await _visionClient.DetectTextAsync(image);
                
                return true; // If no exception, connection is OK
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Connection test failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parse kết quả từ Google Cloud Vision API
        /// </summary>
        private List<HandwritingRecognitionResult> ParseRecognitionResults(
            TextAnnotation response, 
            int maxResults)
        {
            var results = new List<HandwritingRecognitionResult>();

            if (response == null || string.IsNullOrWhiteSpace(response.Text))
            {
                return results; // No text found
            }

            // Main text result (highest confidence)
            var mainResult = new HandwritingRecognitionResult
            {
                Text = response.Text.Trim(),
                Confidence = 0.95f, // Document text detection doesn't provide confidence per se
                Rank = 0,
                DetectedLanguage = DetectLanguage(response.Text)
            };
            results.Add(mainResult);

            // Try to extract alternative interpretations from pages
            if (response.Pages != null && response.Pages.Count > 0)
            {
                var alternatives = ExtractAlternativeTexts(response.Pages, maxResults - 1);
                results.AddRange(alternatives);
            }

            // If we still don't have enough results, generate variations
            if (results.Count < maxResults && results.Count > 0)
            {
                var variations = GenerateTextVariations(mainResult.Text, maxResults - results.Count);
                results.AddRange(variations);
            }

            return results.Take(maxResults).ToList();
        }

        /// <summary>
        /// Trích xuất các phương án thay thế từ pages
        /// </summary>
        private List<HandwritingRecognitionResult> ExtractAlternativeTexts(
            IList<Google.Cloud.Vision.V1.Page> pages, 
            int maxAlternatives)
        {
            var alternatives = new List<HandwritingRecognitionResult>();
            int rank = 1;

            foreach (var page in pages)
            {
                if (page.Blocks == null) continue;

                foreach (var block in page.Blocks)
                {
                    if (block.Paragraphs == null) continue;

                    foreach (var paragraph in block.Paragraphs)
                    {
                        if (paragraph.Words == null) continue;

                        // Combine words to form alternative text
                        var words = paragraph.Words
                            .Select(w => string.Join("", w.Symbols?.Select(s => s.Text) ?? new List<string>()))
                            .Where(w => !string.IsNullOrWhiteSpace(w));

                        var alternativeText = string.Join(" ", words).Trim();

                        if (!string.IsNullOrWhiteSpace(alternativeText) && 
                            !alternatives.Any(a => a.Text == alternativeText))
                        {
                            alternatives.Add(new HandwritingRecognitionResult
                            {
                                Text = alternativeText,
                                Confidence = Math.Max(0.5f, 0.95f - (rank * 0.1f)),
                                Rank = rank++
                            });

                            if (alternatives.Count >= maxAlternatives)
                            {
                                return alternatives;
                            }
                        }
                    }
                }
            }

            return alternatives;
        }

        /// <summary>
        /// Tạo các biến thể văn bản (fallback nếu API không trả về đủ alternatives)
        /// </summary>
        private List<HandwritingRecognitionResult> GenerateTextVariations(string originalText, int count)
        {
            var variations = new List<HandwritingRecognitionResult>();
            
            // Common handwriting confusions
            var confusions = new Dictionary<char, char[]>
            {
                { 'a', new[] { 'o', 'e' } },
                { 'e', new[] { 'c', 'o' } },
                { 'i', new[] { 'l', '1' } },
                { 'o', new[] { 'a', '0' } },
                { 'u', new[] { 'v', 'n' } },
                { '0', new[] { 'O', 'o' } },
                { '1', new[] { 'l', 'I' } },
                { '5', new[] { 'S', 's' } },
                { '8', new[] { 'B', 'b' } },
            };

            int rank = variations.Count + 1;
            var generated = new HashSet<string> { originalText };

            // Generate variations by replacing confusable characters
            for (int i = 0; i < originalText.Length && variations.Count < count; i++)
            {
                char c = originalText[i];
                if (confusions.ContainsKey(c))
                {
                    foreach (var replacement in confusions[c])
                    {
                        var variation = originalText.Remove(i, 1).Insert(i, replacement.ToString());
                        if (!generated.Contains(variation))
                        {
                            variations.Add(new HandwritingRecognitionResult
                            {
                                Text = variation,
                                Confidence = Math.Max(0.3f, 0.85f - (rank * 0.1f)),
                                Rank = rank++
                            });
                            generated.Add(variation);

                            if (variations.Count >= count)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return variations;
        }

        /// <summary>
        /// Phát hiện ngôn ngữ đơn giản (Vietnamese, English, Numbers)
        /// </summary>
        private string DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "unknown";
            }

            // Check for Vietnamese characters
            var vietnameseChars = "àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ";
            if (text.Any(c => vietnameseChars.Contains(char.ToLower(c))))
            {
                return "vi";
            }

            // Check if mostly numbers
            if (text.Count(char.IsDigit) > text.Length * 0.5)
            {
                return "numbers";
            }

            // Default to English
            return "en";
        }

        /// <summary>
        /// Tạo ảnh test đơn giản (1x1 white pixel PNG)
        /// </summary>
        private byte[] CreateTestImage()
        {
            // Simple 1x1 white PNG
            return new byte[]
            {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
                0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1
                0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
                0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
                0x54, 0x08, 0xD7, 0x63, 0xF8, 0xFF, 0xFF, 0x3F,
                0x00, 0x05, 0xFE, 0x02, 0xFE, 0xDC, 0xCC, 0x59,
                0xE7, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND chunk
                0x44, 0xAE, 0x42, 0x60, 0x82
            };
        }

        #endregion
    }
}
