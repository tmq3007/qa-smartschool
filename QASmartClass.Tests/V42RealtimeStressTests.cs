using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using QASmartClass.StudentClient.Services;
using Xunit;
using Serilog;

namespace QASmartClass.Tests
{
    public class V42RealtimeStressTests
    {
        [Fact]
        public async Task TestRealtimeHub_StressLoad_50Clients()
        {
            const int clientCount = 50;
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var serverConnections = new List<TcpClient>();
            var clientConnections = new List<StudentNetworkClient>();
            var serverTasks = new List<Task>();

            int clientConnectedCount = 0;
            var lockObj = new object();

            // 1. Khởi chạy tác vụ Server chấp nhận 50 kết nối
            for (int i = 0; i < clientCount; i++)
            {
                serverTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var tcpServer = await listener.AcceptTcpClientAsync();
                        lock (lockObj)
                        {
                            serverConnections.Add(tcpServer);
                        }
                        
                        var stream = tcpServer.GetStream();
                        var buffer = new byte[2048];
                        int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                        
                        // Write OK ACK with a mock session key
                        var ackMsg = Encoding.UTF8.GetBytes("OK|TEST_CLASS|TEST_TEACHER|TEST_KEY\n");
                        await stream.WriteAsync(ackMsg, 0, ackMsg.Length);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Mock server accept error: {Err}", ex.Message);
                    }
                }));
            }

            try
            {
                // 2. Khởi tạo và kết nối 50 clients
                var clientConnectTasks = new List<Task>();
                for (int i = 0; i < clientCount; i++)
                {
                    var client = new StudentNetworkClient
                    {
                        StudentCode = $"STU_STRESS_{i}",
                        StudentName = $"Stress Student {i}"
                    };
                    
                    client.Connected += (s, e) =>
                    {
                        lock (lockObj)
                        {
                            clientConnectedCount++;
                        }
                    };

                    clientConnections.Add(client);
                    clientConnectTasks.Add(client.ConnectDirectAsync("127.0.0.1", port));
                }

                // 3. Đợi tất cả kết nối được tạo và phản hồi ACK từ Server
                await Task.WhenAll(clientConnectTasks);
                await Task.WhenAll(serverTasks);

                // Chờ một chút để các sự kiện Connected kịp kích hoạt
                await Task.Delay(500);

                // 4. Xác nhận kết quả
                Log.Information("StressTest: Expected connected={Expected}, Actual connected={Actual}", clientCount, clientConnectedCount);
                Assert.Equal(clientCount, clientConnectedCount);

                // 5. Kiểm tra tất cả client ảo đều ở trạng thái IsConnected
                foreach (var client in clientConnections)
                {
                    Assert.True(client.IsConnected, $"Client {client.StudentCode} should be connected.");
                }
            }
            finally
            {
                // Giải phóng tài nguyên
                foreach (var client in clientConnections)
                {
                    client.Dispose();
                }
                foreach (var serverConn in serverConnections)
                {
                    serverConn.Close();
                }
                listener.Stop();
            }
        }
    }
}
