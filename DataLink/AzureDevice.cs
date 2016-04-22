using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Diagnostics;
using DataLink.Core;

namespace DataLink
{
    class AzureDevice
    {
        static DeviceClient _deviceClient;
        string _iotHubUri = string.Empty;
        string _deviceKey = string.Empty;
        string _deviceId = string.Empty;
        private static AzureIot _azureIot = new AzureIot();
        public AzureDevice()
        {
            _deviceClient = DeviceClient.Create(_iotHubUri, AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(_deviceId, _deviceKey), TransportType.Http1);
        }
        public async void SendDeviceToCloudMessagesAsync(string messageInput)
        {
            var message = new Message(Encoding.ASCII.GetBytes(messageInput));
            await _deviceClient.SendEventAsync(message);
            //log when debug
            Debug.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageInput);
        }
        public void Initial()
        {

        }
        public void LoadSetting()
        {
            _azureIot = Bootstrapper.LoadAzureIot("Data/DataLinkAzureIoT.xml");
        }

    }
}
