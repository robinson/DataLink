using DataLink.Core;
using DataLink.Driver.S7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink
{
    public sealed class DataLinkApplication
    {
        private static IList<Tag> _Tags;
        public Machine Machine { get; set; }
        private static IList<DataLink.Core.Application> _activedApplications;
        DataLogging _dataLogging = new DataLogging();
        AzureDevice _azureDevice = new AzureDevice();
        public void LoadSetting()
        {
            var applications = Bootstrapper.LoadApplications("Data/DataLink.xml");
            _activedApplications = applications.Where(a => a.Enabled == true).ToList();
            if (_activedApplications == null || _activedApplications.Count < 1 || _activedApplications.Count < 1)
                //no application
                return;
            _Tags = Bootstrapper.LoadTags("Data/DataLinkTags.xml");
            if (_Tags == null || _Tags.Count == 0)
            {
                //log here
                return;
            }
            foreach (Core.Application appItem in _activedApplications)
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
            Machine = Bootstrapper.LoadMachine("Data/DataLinkConfiguration.xml");
            if (Machine == null)
            {
                //logging here have no machine
                return;
            }
            foreach (Core.Application appItem in _activedApplications)
            {
                if (appItem.Type == "Historian")
                {
                    _dataLogging.Initial(_Tags, Machine);
                }
                else if (appItem.Type == "AzureIoT")
                {
                    _azureDevice.Initial(_Tags, Machine);
                }
            }
        }
        public void Start()
        {
            foreach (Core.Application appItem in _activedApplications)
            {
                if (appItem.Type == "Historian")
                {
                    _dataLogging.Start();
                }
                else if (appItem.Type == "AzureIoT")
                {
                    _azureDevice.Start();
                }
            }
        }
    }
}
