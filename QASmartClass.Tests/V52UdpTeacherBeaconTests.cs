using System;
using Xunit;
using QASmartClass.LearningTools.Helpers;
using System.Reflection;

namespace QASmartClass.Tests
{
    public class V52UdpTeacherBeaconTests
    {
        [Fact]
        public void ComputeSHA256_ValidInput_ReturnsExpectedHexHash()
        {
            // Arrange
            var method = typeof(TeachingActionHelper.UdpTeacherBeacon).GetMethod(
                "ComputeSHA256", 
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            string input = "10A12026-06-07QASecretKey";
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                var sb = new System.Text.StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                string expectedHash = sb.ToString();

                // Act
                string actualHash = (string)method.Invoke(null, new object[] { input })!;

                // Assert
                Assert.Equal(64, actualHash.Length);
                Assert.Equal(expectedHash, actualHash);
            }
        }

        [Fact]
        public void UdpTeacherBeacon_StartAndStop_ThreadSafety()
        {
            // Test that starting and stopping UdpTeacherBeacon concurrently from multiple threads does not crash
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                TeachingActionHelper.UdpTeacherBeacon.Start("test_tool_" + i);
                System.Threading.Thread.Sleep(10);
                TeachingActionHelper.UdpTeacherBeacon.Stop();
            });
        }

        [Fact]
        public void GetLocalIPAddress_ReturnsNonLoopbackOrValidFallback()
        {
            var method = typeof(TeachingActionHelper.UdpTeacherBeacon).GetMethod(
                "GetLocalIPAddress", 
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);

            // Act
            string ip = (string)method.Invoke(null, null)!;

            // Assert
            Assert.False(string.IsNullOrEmpty(ip));
            Assert.True(System.Net.IPAddress.TryParse(ip, out _));
        }
    }
}
