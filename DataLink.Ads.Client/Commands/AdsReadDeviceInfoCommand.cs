using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLink.Ads.Client.CommandResponse;
using DataLink.Ads.Client.Common;

namespace DataLink.Ads.Client.Commands
{
    public class AdsReadDeviceInfoCommand : AdsCommand
    {

        public AdsReadDeviceInfoCommand()
            : base(AdsCommandId.ReadDeviceInfo)
        {
            
        }

        internal override IEnumerable<byte> GetBytes()
        {
            return new List<byte>();
        }

     
        public Task<AdsReadDeviceInfoCommandResponse> RunAsync(Ams ams)
        {
            return RunAsync<AdsReadDeviceInfoCommandResponse>(ams);
        }
     

      
        public AdsReadDeviceInfoCommandResponse Run(Ams ams)
        {
            return Run<AdsReadDeviceInfoCommandResponse>(ams);
        }
    
    }
}
