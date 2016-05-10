using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataLink.Ads.Client.CommandResponse;
using DataLink.Ads.Client.Common;

namespace DataLink.Ads.Client.Commands
{
    public abstract class AdsCommand
    {
        public AdsCommand(ushort commandId)
        {
            this.commandId = commandId;
        }

        
        private ushort commandId;
        public ushort CommandId
        {
            get { return commandId; }
        }

        protected virtual void RunBefore(Ams ams)
        {
        }

        protected virtual void RunAfter(Ams ams)
        {
        }

        
        protected async Task<T> RunAsync<T>(Ams ams) where T : AdsCommandResponse
        {
            RunBefore(ams);
            var result = await ams.RunCommandAsync<T>(this);
            if (result.AdsErrorCode > 0)
                throw new AdsException(result.AdsErrorCode);
            RunAfter(ams);
            return result;
        }
      


     
        protected T Run<T>(Ams ams) where T : AdsCommandResponse
        {
            RunBefore(ams);
            var result = ams.RunCommand<T>(this);
            if (result.AdsErrorCode > 0)
                throw new AdsException(result.AdsErrorCode);
            RunAfter(ams);
            return result;
        }
       
        internal abstract IEnumerable<byte> GetBytes();
    }
}
