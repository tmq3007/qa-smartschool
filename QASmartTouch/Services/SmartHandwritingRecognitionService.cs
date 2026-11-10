using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace QASmartTouch.Services
{
    /// <summary>
    /// SmartHandwritingRecognitionService - Dịch vụ tạo biến thể gợi ý nhận diện chữ viết tay
    /// và căn chỉnh kích thước phông chữ / màu sắc sư phạm
    /// </summary>
    public class SmartHandwritingRecognitionService
    {
        #region Candidate Generation

        /// <summary>
        /// Sinh ra danh sách biến thể khoảng cách ngắt số/chữ từ chuỗi thô nhận diện
        /// Ví dụ: "123" -> ["123", "1 23", "12 3", "1 2 3"]
        /// </summary>
        public List<string> GenerateSpacingCandidates(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return new List<string>();

            string clean = rawText.Trim();
            string noSpace = clean.Replace(" ", "");

            if (noSpace.Length <= 1)
                return new List<string> { clean };

            var candidates = new List<string>();

            // 1. Biến thể liền mạch (e.g. "123")
            candidates.Add(noSpace);

            // 2. Biến thể cách đều tất cả các ký tự (e.g. "1 2 3")
            candidates.Add(string.Join(" ", noSpace.ToCharArray()));

            // 3. Biến thể cách từng phần (nếu độ dài >= 3)
            if (noSpace.Length >= 3)
            {
                candidates.Add(noSpace.Substring(0, 1) + " " + noSpace.Substring(1)); // "1 23"
                candidates.Add(noSpace.Substring(0, noSpace.Length - 1) + " " + noSpace.Substring(noSpace.Length - 1)); // "12 3"
            }

            return candidates.Distinct().ToList();
        }

        #endregion

        #region Formatting Helpers

        /// <summary>
        /// Tính toán cỡ chữ (FontSize) tự động căn theo chiều cao Bounding Box
        /// </summary>
        public double CalculateAutoFontSize(double containerHeight)
        {
            if (containerHeight <= 0) return 24.0;
            // Tỷ lệ 0.75 so với chiều cao khung, giới hạn từ 14px đến 120px
            return Math.Clamp(containerHeight * 0.75, 14.0, 120.0);
        }

        /// <summary>
        /// Trích xuất màu nét vẽ gốc của phần tử viết tay (Polyline/Shape)
        /// </summary>
        public Color ExtractElementColor(UIElement? element)
        {
            if (element is Polyline polyline && polyline.Stroke is SolidColorBrush polyBrush)
            {
                return polyBrush.Color;
            }
            else if (element is Shape shape && shape.Stroke is SolidColorBrush shapeBrush)
            {
                return shapeBrush.Color;
            }
            return Colors.Black; // Mặc định đen nếu không tìm thấy
        }

        #endregion
    }
}
