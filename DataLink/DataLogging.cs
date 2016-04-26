using DataLink.Core;
using DataLink.Driver.S7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace DataLink
{
    public sealed class DataLogging
    {
        private static S7ClientAsync S7Client = new S7ClientAsync();
        private static Historian _Historian = new Historian();
        
        private static DbService.DatabaseServiceClient _ServiceClient = new DbService.DatabaseServiceClient();
        private Dictionary<Command, List<Tag>> DictJob = new Dictionary<Command, List<Tag>>();

        public void Initial( IList<Tag> tags, Machine machine)
        {
            S7Client = new S7ClientAsync();
            //connect
            S7Client.SetConnectionType(S7.OP);
            var result = S7Client.ConnectTo(machine.IPAddress, machine.Rack, machine.Slot);
            if (result > 0)
            {
                //S7ClientAsync.ErrorText(result);
                //log here
                //try to connect
                return;
            }
            var enabledJobs = _Historian.Jobs.Where(x => x.Enable == true && x.Commands != null).ToList();
            foreach (Job jobItem in enabledJobs)
            {
                var activedCommand = jobItem.Commands.Where(x => x.Tags != null && x.Tags.Count > 0);
                foreach (var pair in activedCommand)
                {
                    var tList = new List<Tag>();
                    foreach (var tagItem in tags)
                    {

                        if (pair.Tags.Contains(tagItem.Name))
                        {
                            tList.Add(tagItem);
                        }
                    }
                    DictJob.Add(pair, tList);
                }
            }
        }
        public void LoadSetting()//run 1
        {   
            _Historian = Bootstrapper.LoadHistorian("Data/DataLinkHistorian.xml");
        }
        public void Start()
        {
            foreach (KeyValuePair<Command, List<Tag>> kvp in DictJob)
            {
                var cycle = ((Command)kvp.Key).Job.CycleTime;
                ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer(_ => Execute(kvp.Value, kvp.Key), TimeSpan.FromMilliseconds(cycle));
            }
        }
        internal async void  Execute(List<Tag> tags, Command cmd)
        {
            //1. get data
            for (int i = 0; i < tags.Count; i++)
            {
                var tagItem = tags[i];
                var length = Utility.GetByteLengthFromType(tagItem.DataType);
                int area = Utility.GetS7Area(tagItem.Area);
                byte[] buffer = new byte[length];
                var result = S7Client.ReadArea(area, tagItem.Number, tagItem.Position, length, buffer);
                if (result == 0)
                {
                    tagItem.Value = Utility.GetS7Value(tagItem, buffer);
                }
            }
            var message = Utility.BuildData(tags);
            await _ServiceClient.SendDataToDatabaseAsync(message);
        }
    }
}
