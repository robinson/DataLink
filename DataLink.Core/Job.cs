using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink.Core
{
    public sealed class Job
    {
        public string Name { get; set; }
        public int CycleTime { get; set; }
        public bool Enable { get; set; }
        public IList<Command> Commands { get; set; }
    }
}
