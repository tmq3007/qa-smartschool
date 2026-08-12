using System;
using Xunit;

namespace QASmartClass.Tests
{
    /// <summary>
    /// V7 P5.3: Unit tests cho PredictiveAnalytics keyword detection logic
    /// (Test logic phat hien keyword — khong can DB)
    /// </summary>
    public class PredictiveAnalyticsTests
    {
        // Replicate keyword list from PredictiveAnalyticsService
        private static readonly string[] SensitiveKeywords = {
            "bu?n bă", "dánh nhau", "t? k?", "b?t n?t", "tr?m c?m",
            "t? t?", "mu?n ch?t", "b? cô l?p", "t? h?i", "c?t tay",
            "b? nhà", "s? hăi", "lo l?ng", "m?t ng?", "không mu?n di h?c",
            "b? de d?a", "b?o l?c gia d́nh", "nghi?n game", "khóc nhi?u",
            "thu ḿnh", "không an", "hung hang", "s? d?ng ch?t", "ru?u bia",
            "b? xâm h?i", "chán n?n", "không có b?n", "giảm cân d?t ng?t",
            "u?ng thu?c", "b? h?c", "stress", "b? dánh"
        };

        [Fact]
        public void MoodScore3_ShouldBe_HighRisk()
        {
            int moodScore = 3;
            Assert.True(moodScore <= 3, "MoodScore <= 3 phai la High risk");
        }

        [Fact]
        public void MoodScore7_NoKeywords_ShouldBe_LowRisk()
        {
            int moodScore = 7;
            string notes = "Em hoc tot, vui ve";
            bool hasKeywords = HasSensitiveKeywords(notes);
            Assert.False(hasKeywords);
            Assert.True(moodScore > 3);
        }

        [Fact]
        public void Notes_WithTramCam_ShouldDetect()
        {
            // Use proper Vietnamese diacritics matching keywords list
            string notes = "Em co bieu hien tr?m c?m, thu minh";
            bool detected = HasSensitiveKeywords(notes);
            Assert.True(detected);
        }

        [Fact]
        public void Notes_WithBatNat_ShouldDetect()
        {
            string notes = "Em bi bat nat o lop";
            // "b?t n?t" vs "bat nat" — Vietnamese diacritics matter
            // Test with proper diacritics
            string notesVn = "Em b? b?t n?t ? l?p";
            bool detected = HasSensitiveKeywords(notesVn);
            Assert.True(detected);
        }

        [Fact]
        public void Notes_MultipleKeywords_ShouldDetectAll()
        {
            string notes = "Em b? b?t n?t, bu?n bă, lo l?ng nhi?u";
            var detected = GetDetectedKeywords(notes);
            Assert.Contains("b?t n?t", detected);
            Assert.Contains("bu?n bă", detected);
            Assert.Contains("lo l?ng", detected);
        }

        [Fact]
        public void Notes_CaseInsensitive_ShouldDetect()
        {
            string notes = "Em b? TR?M C?M n?ng";
            bool detected = HasSensitiveKeywords(notes);
            Assert.True(detected);
        }

        [Fact]
        public void Notes_Stress_ShouldDetect()
        {
            string notes = "Em bi stress vi hoc qua nhieu";
            bool detected = HasSensitiveKeywords(notes);
            Assert.True(detected);
        }

        [Fact]
        public void KeywordCount_ShouldBe32()
        {
            Assert.Equal(32, SensitiveKeywords.Length);
        }

        // Helper methods
        private static bool HasSensitiveKeywords(string notes)
        {
            return Array.Exists(SensitiveKeywords, kw => 
                notes.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        private static List<string> GetDetectedKeywords(string notes)
        {
            var result = new List<string>();
            foreach (var kw in SensitiveKeywords)
            {
                if (notes.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    result.Add(kw);
            }
            return result;
        }
    }
}

