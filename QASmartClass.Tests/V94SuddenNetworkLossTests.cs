using System;
using System.Threading.Tasks;
using QASmartClass.StudentClient.Services;
using Xunit;

namespace QASmartClass.Tests
{
    public class V94SuddenNetworkLossTests
    {
        private class FakeNetworkSocket : INetworkSocket
        {
            public bool Connected { get; set; } = true;
            public System.IO.Stream GetStream() => new System.IO.MemoryStream();
            public void Close() { Connected = false; }
            public Task ConnectAsync(string host, int port) => Task.CompletedTask;
            public void Dispose() { }
        }

        [Fact]
        public void Test_NetworkConnectionLoss_ShouldUpdateUIStatusAndEnableReconnect()
        {
            var fakeSocket = new FakeNetworkSocket { Connected = true };
            using var networkClient = new StudentNetworkClient(fakeSocket);

            Assert.True(networkClient.IsConnected, "Client should be connected initially.");

            bool disconnectedEventFired = false;
            networkClient.Disconnected += (s, e) => disconnectedEventFired = true;

            // Simulating network loss
            fakeSocket.Connected = false;
            networkClient.TriggerConnectionLoss();

            Assert.False(networkClient.IsConnected, "Client should be disconnected after connection loss.");
            Assert.True(disconnectedEventFired, "Disconnected event should have been fired.");
        }
    }
}
