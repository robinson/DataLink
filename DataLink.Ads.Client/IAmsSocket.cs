using System.Net;
using System.Threading.Tasks;
using DataLink.Ads.Client.Common;
using System;

namespace DataLink.Ads.Client
{
    public delegate void AmsSocketResponseDelegate(object sender, AmsSocketResponseArgs e); 

    public interface IAmsSocket : IDisposable
    {
        bool IsConnected { get;}
        bool ConnectedAsync { get; }
		string IpTarget { get; set; }
		int PortTarget { get; set; }
        int Subscribers { get; set; }

        event AmsSocketResponseDelegate OnReadCallBack;

        IAmsSocketSync Sync { get; set; }
        IAmsSocketAsync Async { get; set; }
    }
}
