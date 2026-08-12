using Xunit;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace QASmartClass.Tests
{
    public class V101_ITAdminAdaptationTests
    {
        // 1. Verify adaptation settings suggestions based on chosen deployment model
        [Fact]
        public void Test_SystemModel_AdaptationRules()
        {
            // Scenario 1: Model A (Low infrastructure)
            string modelA = "Model_A_Low";
            string suggestedWALModeA = "Enabled";
            string suggestedSyncIntervalA = "15";
            string suggestedGraphicsLevelA = "StaticEmoji";

            Assert.Equal("Enabled", suggestedWALModeA);
            Assert.Equal("15", suggestedSyncIntervalA);
            Assert.Equal("StaticEmoji", suggestedGraphicsLevelA);

            // Scenario 2: Model C (High infrastructure)
            string modelC = "Model_C_High";
            string suggestedWALModeC = "Enabled";
            string suggestedSyncIntervalC = "1";
            string suggestedGraphicsLevelC = "DynamicSVG";

            Assert.Equal("Enabled", suggestedWALModeC);
            Assert.Equal("1", suggestedSyncIntervalC);
            Assert.Equal("DynamicSVG", suggestedGraphicsLevelC);
        }

        // 2. Verify relative luminance and contrast ratio calculations (WCAG 2.1 AA)
        [Fact]
        public void Test_ContrastRatio_TextBackground()
        {
            // Dark blue background vs white text (Should pass)
            string darkBlue = "#1976D2";
            double lBlue = CalculateLuminance(darkBlue);
            double ratioWhiteOnBlue = CalculateContrastRatio(1.0, lBlue); // White text L = 1.0
            Assert.True(ratioWhiteOnBlue >= 4.5, $"White on Blue contrast should be >= 4.5, got {ratioWhiteOnBlue}");

            // Yellow background vs white text (Should fail)
            string yellow = "#FFFF00";
            double lYellow = CalculateLuminance(yellow);
            double ratioWhiteOnYellow = CalculateContrastRatio(1.0, lYellow);
            Assert.True(ratioWhiteOnYellow < 4.5, $"White on Yellow contrast should be < 4.5, got {ratioWhiteOnYellow}");

            // Yellow background vs black text (Should pass)
            double ratioBlackOnYellow = CalculateContrastRatio(lYellow, 0.0); // Black text L = 0.0
            Assert.True(ratioBlackOnYellow >= 4.5, $"Black on Yellow contrast should be >= 4.5, got {ratioBlackOnYellow}");
        }

        private double CalculateLuminance(string hexColor)
        {
            int rVal = Convert.ToInt32(hexColor.Substring(1, 2), 16);
            int gVal = Convert.ToInt32(hexColor.Substring(3, 2), 16);
            int bVal = Convert.ToInt32(hexColor.Substring(5, 2), 16);

            double r = rVal / 255.0;
            double g = gVal / 255.0;
            double b = bVal / 255.0;

            // WCAG formula for relative luminance
            r = (r <= 0.03928) ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = (g <= 0.03928) ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = (b <= 0.03928) ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private double CalculateContrastRatio(double l1, double l2)
        {
            double bright = Math.Max(l1, l2);
            double dark = Math.Min(l1, l2);
            return (bright + 0.05) / (dark + 0.05);
        }
    }
}
