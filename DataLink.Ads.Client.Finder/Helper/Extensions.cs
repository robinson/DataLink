using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink.Ads.Client.Finder.Helper
{
    public static class Extensions
    {
        public static byte[] GetAdsBytes(this string s)
        {
            byte[] bytes = new byte[s.Length + 1];
            Encoding.ASCII.GetBytes(s).CopyTo(bytes, 0);
            bytes[bytes.Length - 1] = 0;

            return bytes;
        }
    }
}
