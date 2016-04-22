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
        
       
        public static Historian _Historian = new Historian();
        
        private static DbService.DatabaseServiceClient _ServiceClient = new DbService.DatabaseServiceClient();

        public void Initial(ref Dictionary<Command, List<Tag>> jobs, List<Tag> tags)
        {
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
                    jobs.Add(pair, tList);
                }
            }
        }
        public void LoadSetting()//run 1
        {   
            _Historian = Bootstrapper.LoadHistorian("Data/DataLinkHistorian.xml");
        }
        public void Start(Dictionary<Command, List<Tag>> jobs)
        {
            foreach (KeyValuePair<Command, List<Tag>> kvp in jobs)
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
                var result = DataLinkApplication.S7Client.ReadArea(area, tagItem.Number, tagItem.Position, length, buffer);
                if (result == 0)
                {
                    tagItem.Value = Utility.GetS7Value(tagItem, buffer);
                }
            }
            var message = Utility.BuildData(tags);
            await _ServiceClient.SendDataAsync(message);
        }



    }
}
