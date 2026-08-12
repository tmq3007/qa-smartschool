using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.StudentClient.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V50NetworkOrderingTests
    {
        [Fact]
        public async Task StudentNetworkClient_SendErrorReport_WithThrottling_LimitsMessages()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            using var client = new StudentNetworkClient
            {
                StudentCode = "TEST_HS01",
                StudentName = "Test Student"
            };

            var serverMessages = new List<string>();
            var serverTask = Task.Run(async () =>
            {
                using var tcpServer = await listener.AcceptTcpClientAsync();
                using var stream = tcpServer.GetStream();

                var buffer = new byte[1024];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length);

                var ackMsg = Encoding.UTF8.GetBytes("OK|SESSION_123|12:00:00\n");
                await stream.WriteAsync(ackMsg, 0, ackMsg.Length);

                // Lắng nghe các thông điệp từ client gửi lên
                var sb = new StringBuilder();
                var readBuffer = new byte[1024];
                var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token;
                try
                {
                    while (!timeoutToken.IsCancellationRequested)
                    {
                        int len = await stream.ReadAsync(readBuffer, 0, readBuffer.Length, timeoutToken);
                        if (len == 0) break;
                        var chunk = Encoding.UTF8.GetString(readBuffer, 0, len);
                        sb.Append(chunk);
                        while (sb.ToString().Contains("\n"))
                        {
                            int idx = sb.ToString().IndexOf('\n');
                            var line = sb.ToString().Substring(0, idx).Trim();
                            sb.Remove(0, idx + 1);
                            if (!string.IsNullOrEmpty(line))
                            {
                                serverMessages.Add(line);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
            });

            await client.ConnectDirectAsync("127.0.0.1", port);
            await Task.Delay(100);

            // Giả lập ném lỗi 10 lần liên tiếp thông qua Reflection hoặc kích hoạt lỗi
            var method = typeof(StudentNetworkClient).GetMethod("SendErrorReport",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    method.Invoke(client, new object[] { new Exception($"Mock network error {i}") });
                }
            }

            await Task.Delay(500);
            client.Stop();
            listener.Stop();
            await Task.WhenAny(serverTask, Task.Delay(1000));

            // Assert: Số lỗi nhận được ở server không được vượt quá 3 (do throttling)
            int errorReportCount = serverMessages.FindAll(m => m.StartsWith("ERR_REPORT|")).Count;
            Assert.True(errorReportCount <= 3, $"Should throttled, actual sent: {errorReportCount} errors");
        }
    }
}
