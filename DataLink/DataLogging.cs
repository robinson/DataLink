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
        private static S7ClientAsync _Client = new S7ClientAsync();
        private static IList<Tag> _Tags;
        private static Historian _Historian = new Historian();
        private static Dictionary<Command, List<Tag>> _JobDict = new Dictionary<Command, List<Tag>>();
        private static Machine _Machine = new Machine();
        private static DbService.DatabaseServiceClient _ServiceClient = new DbService.DatabaseServiceClient();

        public void Initial()
        {
            //get machine
            _Machine = Bootstrapper.LoadMachine("Data/DataLinkConfiguration.xml");
            if (_Machine == null)
            {
                //logging here have no machine
                return;
            }
            //connect
            _Client.SetConnectionType(S7.OP);
            var result = _Client.ConnectTo(_Machine.IPAddress, _Machine.Rack, _Machine.Slot);
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
                    foreach (var tagItem in _Tags)
                    {
                        
                        if (pair.Tags.Contains(tagItem.Name))
                        {
                            tList.Add(tagItem);
                        }
                    }
                    _JobDict.Add(pair, tList);
                }
            }
        }
        public void LoadSetting()//run 1
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
            foreach (KeyValuePair<Command, List<Tag>> kvp in _JobDict)
            {
                var cycle = ((Command)kvp.Key).Job.CycleTime;
                ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer(_ => Execute(kvp.Value, kvp.Key), TimeSpan.FromMilliseconds(cycle));
            }
        }
        internal void Execute(List<Tag> tags, Command cmd)
        {
            //1. get data
            //DB area
            //var dbTags = tags.Where(x => x.Area == "DB").ToList();
            for (int i = 0; i < tags.Count; i++)
            {
                var tagItem = tags[i];
                var length = Utility.GetByteLengthFromType(tagItem.DataType);
                int area = Utility.GetS7Area(tagItem.Area);
                byte[] buffer = new byte[length];
                var result = _Client.ReadArea(area, tagItem.Number, tagItem.Position, length, buffer);
                if (result == 0)
                {
                    tagItem.Value = Utility.GetS7Value(tagItem, buffer);
                    if (tagItem.Value != null)
                    {
                       var message = Utility.BuildData(tagItem, null, null,null);
                        _ServiceClient.SendDataAsync(message);
                    }
                }
            }
        }



    }
}
