using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLink.Ads.Client.Common
{
    public interface IAdsSymhandle
    {
        string VarName { get; set; }
        UInt32 IndexGroup { get; set; }
        UInt32 IndexOffset { get; set; }
        string TypeName { get; set; }
        string Comment { get; set; }
        uint Symhandle { get; set; }
        string ConnectionName { get; set; }
        uint ByteLength { get; set; }
    }
}
