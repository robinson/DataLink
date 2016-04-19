using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink.Core
{
    public sealed class Historian
    {
        public DatabaseType DatabaseType { get; set; }
        public string ConnectionString { get; set; }
        public IList<Job> Jobs { get; set; }
    }
}
