using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLink.Ads.Client.Common;

namespace DataLink.Ads.Client.CommandResponse
{
    public class AdsReadStateCommandResponse : AdsCommandResponse
    {
        private AdsState adsState;
        public AdsState AdsState
        {
            get { return adsState; }
        }
        
        protected override void AdsResponseIsChanged()
        {
            adsState = new AdsState();
            adsState.State = BitConverter.ToUInt16(this.AdsResponse, 4);
            adsState.DeviceState = BitConverter.ToUInt16(this.AdsResponse, 6);
        }
    }
}
