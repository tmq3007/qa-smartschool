using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace QASmartClass.StudentClient.Services
{
    public class NetworkSocketWrapper : INetworkSocket
    {
        private readonly TcpClient _tcpClient;

        public NetworkSocketWrapper(TcpClient tcpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        }

        public bool Connected => _tcpClient.Connected;

        public Stream GetStream() => _tcpClient.GetStream();

        public void Close() => _tcpClient.Close();

        public Task ConnectAsync(string host, int port) => _tcpClient.ConnectAsync(host, port);

        public void Dispose()
        {
            _tcpClient.Dispose();
        }
    }
}
