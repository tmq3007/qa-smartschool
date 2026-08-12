using System;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Model cho kết quả nhận dạng chữ viết tay từ Google Cloud Vision API
    /// </summary>
    public class HandwritingRecognitionResult
    {
        /// <summary>
        /// Văn bản được nhận dạng
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Độ tin cậy (0.0 - 1.0)
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Độ tin cậy dạng phần trăm (0-100)
        /// </summary>
        public int ConfidencePercent => (int)(Confidence * 100);

        /// <summary>
        /// Ngôn ngữ được phát hiện (vi, en, etc.)
        /// </summary>
        public string? DetectedLanguage { get; set; }

        /// <summary>
        /// Thứ tự ưu tiên (0 = cao nhất)
        /// </summary>
        public int Rank { get; set; }

        public HandwritingRecognitionResult()
        {
        }

        public HandwritingRecognitionResult(string text, float confidence, int rank = 0)
        {
            Text = text;
            Confidence = confidence;
            Rank = rank;
        }

        public override string ToString()
        {
            return $"{Text} ({ConfidencePercent}%)";
        }
    }
}
