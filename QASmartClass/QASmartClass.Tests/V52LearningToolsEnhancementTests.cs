using System;
using Xunit;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.Tests
{
    /// <summary>
    /// Verification tests for LearningTools enhancements in version 52 (G6.2).
    /// </summary>
    public class V52LearningToolsEnhancementTests
    {
        [Fact]
        public void TryParseDouble_WithDotSeparator_ParsesCorrectly()
        {
            // Arrange
            string input = "123.45";

            // Act
            bool success = ParsingHelper.TryParseDouble(input, out double val);

            // Assert
            Assert.True(success);
            Assert.Equal(123.45, val);
        }

        [Fact]
        public void TryParseDouble_WithCommaSeparator_ParsesCorrectly()
        {
            // Arrange
            string input = "123,45";

            // Act
            bool success = ParsingHelper.TryParseDouble(input, out double val);

            // Assert
            Assert.True(success);
            Assert.Equal(123.45, val);
        }

        [Fact]
        public void TryParseDouble_WithSpaces_ParsesCorrectly()
        {
            // Arrange
            string input = "  123.45  ";

            // Act
            bool success = ParsingHelper.TryParseDouble(input, out double val);

            // Assert
            Assert.True(success);
            Assert.Equal(123.45, val);
        }

        [Fact]
        public void TryParseDouble_InvalidInput_ReturnsFalse()
        {
            // Arrange
            string input = "abc";

            // Act
            bool success = ParsingHelper.TryParseDouble(input, out double val);

            // Assert
            Assert.False(success);
            Assert.Equal(0.0, val);
        }

        [Fact]
        public void TryParseDouble_NullOrEmptyInput_ReturnsFalse()
        {
            Assert.False(ParsingHelper.TryParseDouble(null!, out _));
            Assert.False(ParsingHelper.TryParseDouble(string.Empty, out _));
            Assert.False(ParsingHelper.TryParseDouble("   ", out _));
        }
    }
}
