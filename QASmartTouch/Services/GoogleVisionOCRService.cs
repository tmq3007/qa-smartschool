using Google.Cloud.Vision.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service for OCR using Google Cloud Vision API
    /// </summary>
    public class GoogleVisionOCRService
    {
        private ImageAnnotatorClient? _client;
        private readonly string _apiKeyPath;

        public GoogleVisionOCRService(string apiKeyPath = "")
        {
            _apiKeyPath = apiKeyPath;
        }

        /// <summary>
        /// Initialize the Google Vision client with API credentials
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKeyPath) || !File.Exists(_apiKeyPath))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Google Vision API key file not found. Please set the path.");
                    return false;
                }

                // Set environment variable for Google credentials
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _apiKeyPath);

                // Create client
                _client = await ImageAnnotatorClient.CreateAsync();
                
                System.Diagnostics.Debug.WriteLine("✅ Google Vision OCR Service initialized");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to initialize Google Vision: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Perform OCR on a bitmap image and return multiple text suggestions
        /// </summary>
        public async Task<List<OCRResult>> RecognizeTextAsync(BitmapSource bitmap)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Google Vision client not initialized. Call InitializeAsync() first.");
            }

            try
            {
                // Convert BitmapSource to byte array
                byte[] imageBytes = BitmapToByteArray(bitmap);

                // Create Google Vision Image
                var image = Google.Cloud.Vision.V1.Image.FromBytes(imageBytes);

                // Perform text detection (supports handwriting)
                var response = await _client.DetectDocumentTextAsync(image);

                // Extract text annotations
                var results = new List<OCRResult>();

                if (response != null && response.Pages.Count > 0)
                {
                    foreach (var page in response.Pages)
                    {
                        foreach (var block in page.Blocks)
                        {
                            foreach (var paragraph in block.Paragraphs)
                            {
                                string text = "";
                                float confidence = 0;

                                foreach (var word in paragraph.Words)
                                {
                                    string wordText = string.Join("", word.Symbols.Select(s => s.Text));
                                    text += wordText + " ";
                                    confidence += word.Confidence;
                                }

                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    results.Add(new OCRResult
                                    {
                                        Text = text.Trim(),
                                        Confidence = confidence / paragraph.Words.Count
                                    });
                                }
                            }
                        }
                    }
                }

                // If no document text found, try regular text detection
                if (results.Count == 0)
                {
                    var textResponse = await _client.DetectTextAsync(image);
                    if (textResponse.Count > 0)
                    {
                        foreach (var annotation in textResponse)
                        {
                            if (!string.IsNullOrWhiteSpace(annotation.Description))
                            {
                                results.Add(new OCRResult
                                {
                                    Text = annotation.Description,
                                    Confidence = 0.8f // Default confidence for text detection
                                });
                            }
                        }
                    }
                }

                // Sort by confidence descending
                results = results.OrderByDescending(r => r.Confidence).ToList();

                System.Diagnostics.Debug.WriteLine($"✅ OCR completed. Found {results.Count} results");
                return results;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ OCR failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Convert BitmapSource to byte array
        /// </summary>
        private byte[] BitmapToByteArray(BitmapSource bitmap)
        {
            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }
    }

    /// <summary>
    /// OCR result with text and confidence score
    /// </summary>
    public class OCRResult
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }

        public string DisplayText => $"{Text} ({Confidence:P0})";
    }
}
