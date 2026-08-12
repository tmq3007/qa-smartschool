using System;
using System.IO;
using System.Threading.Tasks;

namespace QASmartClass.StudentClient.Services
{
    public interface INetworkSocket : IDisposable
    {
        bool Connected { get; }
        Stream GetStream();
        void Close();
        Task ConnectAsync(string host, int port);
    }
}
