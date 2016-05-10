using DataLink.Ads.Client.Common;
using DataLink.Ads.Client.Finder;
using DataLink.Ads.Client.Finder.Common;
using DataLink.Ads.Client.Socket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DataLink.Ads.Client.Test
{
    public class SoftPlcTest
    {
        const string _localhostIp = "192.168.48.200";
        const string _localAsmNetId = "192.168.48.200.1.1";
        const string _plcIpAddress = "192.168.48.50";
        const string _amsNetIdSource = "192.168.120.85.1.1";
        [Fact]
        public async Task TestSoftBhf()
        {
            IPAddress plcIpAddress = IPAddress.Parse(_plcIpAddress);
            IPAddress localhost = IPAddress.Parse(_localhostIp);

            RouteInfo info = new RouteInfo();
            info.Localhost = localhost.ToString();
            info.LocalAmsNetId = new AdsNetId(_localAsmNetId);
            info.IsTemporaryRoute = false;
            info.Login = "AZO";
            info.Password = "06291";

            bool isSuccessful = await RouteManager.AddRemoteRouteAsync(localhost, plcIpAddress, info, 1000);
            Assert.Equal(isSuccessful, true);
            //missing client finder

            using (Socket.AdsClient client = new Socket.AdsClient(
                                         amsNetIdSource: _localAsmNetId,
                              ipTarget: _plcIpAddress,
                               amsNetIdTarget: _amsNetIdSource))
            {  
                AdsDeviceInfo deviceInfo = await client.ReadDeviceInfoAsync();
                AdsState state = await client.ReadStateAsync();
                client.OnNotification += (sender, e) =>
                {
                    var value = e.Notification.ByteValue;
                    int i = BitConverter.ToInt32(value, 0);
                    Debug.Write(i);
                };
                uint notification1Handle = await client.AddNotificationAsync<int>(
                      "MAIN.ARRUDICOUNTER[1]", AdsTransmissionMode.OnChange, 1000);

                await Task.Delay(TimeSpan.FromSeconds(10));
                Assert.Equal(true, true);
            }
        } 
        
    }
}
