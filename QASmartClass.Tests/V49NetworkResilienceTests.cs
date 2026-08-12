using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QASmartClass.StudentClient.Services;
using QASmartClass.Network;
using Serilog;
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

        [Fact]
        public async Task CircuitBreaker_SuccessfulOperation_ReturnsResult()
        {
            var breaker = new CircuitBreaker(failureThreshold: 2, openDuration: TimeSpan.FromMilliseconds(50), baseDelay: TimeSpan.FromMilliseconds(5));
            int callCount = 0;

            var result = await breaker.ExecuteAsync(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                return "success";
            });

            Assert.Equal("success", result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task CircuitBreaker_ConsecutiveFailures_OpensAndRejects()
        {
            var breaker = new CircuitBreaker(failureThreshold: 2, openDuration: TimeSpan.FromSeconds(5), baseDelay: TimeSpan.FromMilliseconds(5));
            int callCount = 0;

            // First call fails
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await breaker.ExecuteAsync<string>(async () =>
                {
                    callCount++;
                    await Task.CompletedTask;
                    throw new Exception("Temporary connection failure 1");
                });
            });

            // Second call fails and should open the circuit breaker
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await breaker.ExecuteAsync<string>(async () =>
                {
                    callCount++;
                    await Task.CompletedTask;
                    throw new Exception("Temporary connection failure 2");
                });
            });

            // Third call should be rejected immediately with InvalidOperationException (circuit breaker open) without calling the action
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await breaker.ExecuteAsync<string>(async () =>
                {
                    callCount++;
                    await Task.CompletedTask;
                    return "should not run";
                });
            });

            Assert.Contains("circuit breaker", ex.Message.ToLowerInvariant());
            Assert.Equal(2, callCount); // should not have incremented on the third call
        }

        [Fact]
        public async Task CircuitBreaker_OpenCooldownElapsed_AllowsRetry()
        {
            var breaker = new CircuitBreaker(failureThreshold: 1, openDuration: TimeSpan.FromMilliseconds(100), baseDelay: TimeSpan.FromMilliseconds(5));
            int callCount = 0;

            // Fail once to open circuit (since threshold is 1)
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await breaker.ExecuteAsync<string>(async () =>
                {
                    callCount++;
                    await Task.CompletedTask;
                    throw new Exception("Failure");
                });
            });

            // Immediate call should fail because circuit is open
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await breaker.ExecuteAsync<string>(async () =>
                {
                    callCount++;
                    await Task.CompletedTask;
                    return "should not run";
                });
            });

            // Wait for cooldown to expire
            await Task.Delay(150);

            // Now it should retry/allow call again
            var result = await breaker.ExecuteAsync(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                return "recovered";
            });

            Assert.Equal("recovered", result);
            Assert.Equal(2, callCount); // 1 for first failed attempt, 1 for recovered attempt
        }

        [Fact]
        public void SerilogLogger_WritesStructuredJsonLog()
        {
            var logFile = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid():N}.json");
            try
            {
                var logger = new Serilog.LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), logFile)
                    .CreateLogger();

                logger.Warning("Test warning message with parameter {Param}", "value123");
                logger.Dispose();

                Assert.True(File.Exists(logFile), "Log file should be created");
                var logContent = File.ReadAllText(logFile);
                Assert.NotEmpty(logContent);
                Assert.Contains("value123", logContent);
                Assert.Contains("Warning", logContent);
                Assert.Contains("Test warning message with parameter {Param}", logContent);
            }
            finally
            {
                if (File.Exists(logFile))
                {
                    File.Delete(logFile);
                }
            }
        }
    }
}
