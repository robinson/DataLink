using System.Net;
using System.Threading.Tasks;
using DataLink.Ads.Client.Common;
using System;

namespace DataLink.Ads.Client
{
    public interface IAmsSocketSync
    {
        void ConnectAndListen();
        void Send(byte[] message);
        void Receive(byte[] message);
    }
}
