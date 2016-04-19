using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink.Core
{
    public sealed class Tag
    {
        public string Name { get; set; }
        public string Area { get; set; }
        public int Number { get; set; }
        public int Position { get; set; }
        public string DataType { get; set; }
    }
}
