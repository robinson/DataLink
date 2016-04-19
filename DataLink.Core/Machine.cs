using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink.Core
{
    public sealed class Machine
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
    }
}
