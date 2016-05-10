using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLink.Ads.Client.CommandResponse;
using DataLink.Ads.Client.Common;

namespace DataLink.Ads.Client.Commands
{
    public class AdsReadStateCommand : AdsCommand
    {
        public AdsReadStateCommand()
            : base(AdsCommandId.ReadState)
        {
            
        }

        internal override IEnumerable<byte> GetBytes()
        {
            return new List<byte>();
        }

        
        public Task<AdsReadStateCommandResponse> RunAsync(Ams ams)
        {
            return RunAsync<AdsReadStateCommandResponse>(ams);
        }
        
        public AdsReadStateCommandResponse Run(Ams ams)
        {
            return Run<AdsReadStateCommandResponse>(ams);
        }
        
    }
}
