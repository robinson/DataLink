using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace DataLink.Ads.Client.Finder.Common
{
    public class Request
    {
        public const int DEFAULT_UDP_PORT = 48899;


        DatagramSocket socket;
        public DatagramSocket Socket { get { return socket; } }
        private byte[] responseByte;
        bool received = false;


        public Request()
        {

            socket = new DatagramSocket();
            socket.MessageReceived += Socket_MessageReceived;

        }

        private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                var r = args.GetDataReader();
                var l = r.UnconsumedBufferLength;
                responseByte = new byte[l];
                r.ReadBytes(responseByte);
                received = true;
            }
            catch (Exception exception)
            {
                SocketErrorStatus socketError = Windows.Networking.Sockets.SocketError.GetStatus(exception.HResult);

                if (Windows.Networking.Sockets.SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

            }
        }

        public async Task SendAsync(IPEndPoint endPoint)
        {
            var hostname = new HostName(endPoint.Address.ToString());
            var port = endPoint.Port.ToString();
            using (var stream = await socket.GetOutputStreamAsync(hostname, port.ToString()))
            {
                using (var writer = new DataWriter(stream))
                {
                    var data = GetRequestBytes;

                    writer.WriteBytes(data);
                    await writer.StoreAsync();
                }
            }           
        }

        List<byte[]> listOfBytes = new List<byte[]>();
        public byte[] GetRequestBytes
        {
            get { return listOfBytes.SelectMany(a => a).ToArray(); }
        }
        public byte[] GetResponseBytes
        {
            get
            {
                while (!received)
                {
                    Task.Delay(100);
                }
                return responseByte;

            }
        }

        public void Add(byte[] segment)
        {
            listOfBytes.Add(segment);
        }

        public void Clear()
        {
            listOfBytes.Clear();
        }
    }
}
