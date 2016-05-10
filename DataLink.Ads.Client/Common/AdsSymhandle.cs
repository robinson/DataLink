using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink.Ads.Client.Common
{
    public class AdsSymhandle : IAdsSymhandle
    {
        public string VarName { get; set; }
        public UInt32 IndexGroup { get; set; }
        public UInt32 IndexOffset { get; set; }
        public string TypeName { get; set; }
        public string Comment { get; set; }
        public uint Symhandle { get; set; }
        public string ConnectionName { get; set; }
        public uint ByteLength { get; set; }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", VarName, TypeName, Comment);
        }
    }
}
