using System.Net.Sockets;

namespace DataLink.Driver.S7
{
    internal class NetworkStream
    {
        private Socket tcpSocket;

        public NetworkStream(Socket tcpSocket)
        {
            this.tcpSocket = tcpSocket;
        }
    }
}