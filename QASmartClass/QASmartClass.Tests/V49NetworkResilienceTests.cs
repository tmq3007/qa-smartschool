using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.StudentClient.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V49NetworkResilienceTests
    {
        [Fact]
        public async Task StudentNetworkClient_OnConnectionLoss_ResetsConnectionState()
        {
            // Start local mock TCP server on an ephemeral port
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            // Initialize client
            using var client = new StudentNetworkClient
            {
                StudentCode = "TEST_HS01",
                StudentName = "Test Student"
            };

            bool connectedInvoked = false;
            bool disconnectedInvoked = false;

            client.Connected += (s, e) => connectedInvoked = true;
            client.Disconnected += (s, e) => disconnectedInvoked = true;

            // Accept connection in background and send ACK, then disconnect
            var serverTask = Task.Run(async () =>
            {
                using var tcpServer = await listener.AcceptTcpClientAsync();
                using var stream = tcpServer.GetStream();
                
                // Read JOIN message
                var buffer = new byte[1024];
                int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                
                // Write OK ACK
                var ackMsg = Encoding.UTF8.GetBytes("OK|TEST_SESSION_ID|12:00:00\n");
                await stream.WriteAsync(ackMsg, 0, ackMsg.Length);

                // Wait a bit for communication to settle
                await Task.Delay(200);

                // Disconnect by closing stream and socket
                stream.Close();
                tcpServer.Close();
            });

            // Connect client
            await client.ConnectDirectAsync("127.0.0.1", port);

            // Wait for server task to finish and client to process disconnection
            await Task.WhenAny(serverTask, Task.Delay(2000));

            // Small delay to allow client thread to update state
            await Task.Delay(300);

            // Assert
            Assert.True(connectedInvoked, "Connected event should have been invoked");
            Assert.True(disconnectedInvoked, "Disconnected event should have been invoked");
            Assert.False(client.IsConnected, "Client should be marked as disconnected");

            // Cleanup
            listener.Stop();
        }
    }
}
