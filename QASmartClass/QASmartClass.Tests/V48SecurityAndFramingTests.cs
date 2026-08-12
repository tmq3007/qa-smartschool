using QASmartClass.Data;
using QASmartTouch.Services;
using System;
using System.Text;
using Xunit;

namespace QASmartClass.Tests
{
    public class V48SecurityAndFramingTests
    {
        [Fact]
        public void PasswordHashing_VerifyCorrectPassword_ReturnsTrue()
        {
            // Arrange
            string originalPassword = "MySecurePassword123";

            // Act
            string hash = AuthenticationService.HashPassword(originalPassword);

            // Assert
            Assert.StartsWith("pbkdf2:100000:", hash);
            Assert.True(AuthenticationService.VerifyPassword(originalPassword, hash));
        }

        [Fact]
        public void PasswordHashing_VerifyWrongPassword_ReturnsFalse()
        {
            // Arrange
            string originalPassword = "MySecurePassword123";
            string wrongPassword = "WrongPassword123";

            // Act
            string hash = AuthenticationService.HashPassword(originalPassword);

            // Assert
            Assert.False(AuthenticationService.VerifyPassword(wrongPassword, hash));
        }

        [Fact]
        public void TCPMessageFraming_AccumulateAndSplit_ExtractsCorrectMessages()
        {
            // Arrange
            var msgBuffer = new StringBuilder();
            string streamPart1 = "CMD|LOCK|ALL\nCMD|QUIZ_START|5";
            string streamPart2 = "\nCMD|WHITEBOARD_CLEAR|ALL\n";

            var receivedCommands = new System.Collections.Generic.List<string>();

            // Act - Part 1
            msgBuffer.Append(streamPart1);
            ProcessBuffer(msgBuffer, receivedCommands);

            // Assert Part 1
            Assert.Single(receivedCommands);
            Assert.Equal("CMD|LOCK|ALL", receivedCommands[0]);

            // Act - Part 2
            msgBuffer.Append(streamPart2);
            ProcessBuffer(msgBuffer, receivedCommands);

            // Assert Part 2
            Assert.Equal(3, receivedCommands.Count);
            Assert.Equal("CMD|QUIZ_START|5", receivedCommands[1]);
            Assert.Equal("CMD|WHITEBOARD_CLEAR|ALL", receivedCommands[2]);
        }

        private void ProcessBuffer(StringBuilder msgBuffer, System.Collections.Generic.List<string> commandList)
        {
            string accumulated = msgBuffer.ToString();
            int nlIdx;
            while ((nlIdx = accumulated.IndexOf('\n')) >= 0)
            {
                var line = accumulated.Substring(0, nlIdx).Trim();
                accumulated = accumulated.Substring(nlIdx + 1);

                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("CMD|"))
                {
                    commandList.Add(line);
                }
            }
            msgBuffer.Clear();
            if (!string.IsNullOrEmpty(accumulated))
                msgBuffer.Append(accumulated);
        }
    }
}
