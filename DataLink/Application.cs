using DataLink.Core;
using DataLink.Driver.S7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink
{
    public class DataLinkApplication
    {
        private static IList<Tag> _Tags;
        private static Machine _Machine = new Machine();
        public static S7ClientAsync S7Client = new S7ClientAsync();
        private static IList<DataLink.Core.Application> _applications;
        DataLogging _dataLogging = new DataLogging();
        AzureDevice _azureDevice = new AzureDevice();
        public void LoadSetting()
        {
            _Tags = Bootstrapper.LoadTags("Data/DataLinkTags.xml");
            if (_Tags == null || _Tags.Count == 0)
            {
                //log here
                return;
            }
            _applications = Bootstrapper.LoadApplications("Data/DataLink.xml");
            foreach (Core.Application appItem in _applications.Where(a => a.Enabled == true))
            {
                if (appItem.Type == "Historian")
                {
                    _dataLogging.LoadSetting();
                }
                else if (appItem.Type == "AzureIoT")
                {
                    _azureDevice.LoadSetting();
                }
            }
        }
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
            S7Client.SetConnectionType(S7.OP);
            var result = S7Client.ConnectTo(_Machine.IPAddress, _Machine.Rack, _Machine.Slot);
            if (result > 0)
            {
                //S7ClientAsync.ErrorText(result);
                //log here
                //try to connect
                return;
            }
           
        }
        public void Start()
        {

        }
    }
}
