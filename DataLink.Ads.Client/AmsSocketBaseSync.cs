using System;
using DataLink.Ads.Client.Helpers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DataLink.Ads.Client.Common;
using System.Diagnostics;

namespace DataLink.Ads.Client
{
    public abstract class AmsSocketBaseSync : IAmsSocketSync
    {
		protected AmsSocketBase amsSocket;
        public AmsSocketBaseSync(AmsSocketBase amsSocket)
        {
			this.amsSocket = amsSocket;
        }

        public void ConnectAndListen()
        {
            if (!amsSocket.IsConnected)
            {
                amsSocket.ConnectedAsync = false;
                Connect();
                amsSocket.Listen();
            }
        }

        protected abstract void Connect();
        //Debug.WriteLine("Sending bytes: " + ByteArrayHelper.ByteArrayToTestString(message));
        public abstract void Send(byte[] message);
        public abstract void Receive(byte[] message);
    }
}