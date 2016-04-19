using DataLink.Core;
using DataLink.Driver.S7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink
{
    public sealed class DataLogging
    {
        private static S7ClientAsync _Client = new S7ClientAsync();
        private static IList<Tag> _Tags;
        private static Historian _Historian = new Historian();

        public void Initial()
        {
            //get machine
            Machine machine = Bootstrapper.LoadMachine("Data/DataLinkConfiguration.xml");
            if (machine == null)
            {
                //logging here have no machine
                return;
            }
            //connect
            _Client.SetConnectionType(S7.OP);
            var result = _Client.ConnectTo(machine.IPAddress, machine.Rack, machine.Slot);
            if (result > 0)
            {
                //S7ClientAsync.ErrorText(result);
                //log here
                //try to connect
                return;
            }
        }
        public void LoadSetting()
        {
            _Tags = Bootstrapper.LoadTags("Data/DataLinkTags.xml");
            if (_Tags == null || _Tags.Count == 0)
            {
                //log here
                return;
            }
            _Historian = Bootstrapper.LoadHistorian("Data/DataLinkHistorian.xml");
        }
        public void Start()
        {
            var enabledJob = _Historian.Jobs.Select(x => x.Enable == true).ToList();
            foreach (var jobItem in enabledJob)
            {
                //System.Threading.Timer timer = new System.Threading.Timer()
            }
        }
    }
}
