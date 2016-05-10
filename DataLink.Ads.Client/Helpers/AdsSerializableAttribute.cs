using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLink.Ads.Client
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class AdsSerializableAttribute : Attribute
    {
        public AdsSerializableAttribute()
        {

        }
    }
}
