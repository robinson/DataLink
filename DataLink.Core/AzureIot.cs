using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink.Core
{
    public sealed class AzureIot
    {
        public string IotHubUri { get; set; }
        public string DeviceKey { get; set; }
        public string DeviceId { get; set; }
        public IList<Job> Jobs { get; set; }
    }
}
