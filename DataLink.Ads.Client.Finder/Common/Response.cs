using System;
using System.Collections.Generic;
using Windows.Networking.Sockets;
using System.Threading.Tasks;
using System.Net.Sockets;
using Windows.Storage.Streams;
using System.IO;

namespace DataLink.Ads.Client.Finder.Common
{
    public class Response
    {
        DatagramSocket socket;
        public DatagramSocket Socket { get { return socket; } }


        public Response(DatagramSocket socket)
        {
            this.socket = socket;
            socket.MessageReceived += Socket_MessageReceived;
        }
        //private async void SocketOnMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        //{
        //    var result = args.GetDataStream();
        //    var resultStream = result.AsStreamForRead(1024);

        //    using (var reader = new StreamReader(resultStream))
        //    {
        //        var text = await reader.ReadToEndAsync();

        //    }
        //}

        private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                var r = args.GetDataReader();
                var l = r.UnconsumedBufferLength;
                buffer = new byte[l];
                r.ReadBytes(buffer);

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
        static byte[] buffer;
        public byte[] ReceiveBuffer()
        {
            return buffer;

        }


    }
}
