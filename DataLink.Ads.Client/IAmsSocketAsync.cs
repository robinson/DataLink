using System.Net;
using System.Threading.Tasks;
using DataLink.Ads.Client.Common;
using System;

namespace DataLink.Ads.Client
{
    public interface IAmsSocketAsync : IDisposable
    {
        Task ConnectAndListenAsync();
        Task SendAsync(byte[] message);
        Task ReceiveAsync(byte[] message);
    }
}
