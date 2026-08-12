using Xunit;
using System;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.Tests
{
    public class V73SecuritySanitizationTests
    {
        [Fact]
        public void PathHelper_SanitizeInput_StripsHtmlTags()
        {
            var dirty = "<script>alert('xss')</script>Chào học sinh <span class=\"highlight\">Lớp 10</span>!";
            var clean = PathHelper.SanitizeInput(dirty, 200);
            
            // Should strip script and span tags completely
            Assert.Equal("alert('xss')Chào học sinh Lớp 10!", clean);
        }

        [Fact]
        public void PathHelper_SanitizeInput_StripsControlCharacters()
        {
            // ASCII 7 is bell, ASCII 27 is escape
            var dirty = "Hello\u0007World\u001b!";
            var clean = PathHelper.SanitizeInput(dirty, 200);
            
            Assert.Equal("HelloWorld!", clean);
        }

        [Fact]
        public void PathHelper_SanitizeInput_EnforcesMaxLength()
        {
            var longText = new string('A', 500);
            var clean = PathHelper.SanitizeInput(longText, 50);
            
            Assert.Equal(50, clean.Length);
            Assert.Equal(new string('A', 50), clean);
        }

        [Fact]
        public void PathHelper_SanitizeInput_HandlesEmptyOrNull()
        {
            Assert.Equal(string.Empty, PathHelper.SanitizeInput(null, 100));
            Assert.Equal(string.Empty, PathHelper.SanitizeInput(string.Empty, 100));
            Assert.Equal(string.Empty, PathHelper.SanitizeInput("   ", 100)); // Trimming will return empty string
        }

        [Fact]
        public void IsPathSafe_SubPath_ReturnsTrue()
        {
            // Arrange
            string basePath = @"C:\App\Storage";
            string targetPath = @"C:\App\Storage\Subdir\file.txt";

            // Act & Assert
            Assert.True(PathHelper.IsPathSafe(basePath, targetPath));
        }

        [Fact]
        public void IsPathSafe_TraversalPath_ReturnsFalse()
        {
            // Arrange
            string basePath = @"C:\App\Storage";
            string targetPath = @"C:\App\Storage\..\..\Windows\System32\cmd.exe";

            // Act & Assert
            Assert.False(PathHelper.IsPathSafe(basePath, targetPath));
        }

        [Fact]
        public void IsPathSafe_PartialNameMatch_ReturnsFalse()
        {
            // Arrange
            string basePath = @"C:\App";
            string targetPath = @"C:\AppSecrets\secret.txt";

            // Act & Assert
            Assert.False(PathHelper.IsPathSafe(basePath, targetPath));
        }

        [Fact]
        public void PathHelper_SanitizeFileNameInput_StripsSpecialCharacters()
        {
            var dirty = "HS001*?|..\\..\\test";
            var clean = PathHelper.SanitizeFileNameInput(dirty, 20);
            
            Assert.Equal("HS001test", clean);
        }

        [Fact]
        public void PathHelper_SanitizeFileNameInput_EnforcesMaxLength()
        {
            var longText = new string('A', 50);
            var clean = PathHelper.SanitizeFileNameInput(longText, 15);
            
            Assert.Equal(15, clean.Length);
            Assert.Equal(new string('A', 15), clean);
        }

        [Fact]
        public void PathHelper_SanitizeFileNameInput_HandlesEmptyOrNull()
        {
            Assert.Equal(string.Empty, PathHelper.SanitizeFileNameInput(null, 100));
            Assert.Equal(string.Empty, PathHelper.SanitizeFileNameInput(string.Empty, 100));
        }
    }
}
