﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink.Ads.Client.Common
{
    public class AdsDeviceInfo
    {
        public byte MajorVersion { get; set; }
        public byte MinorVersion { get; set; }
        public ushort VersionBuild { get; set; }
        public string DeviceName { get; set; }

        public override string ToString()
        {
            return String.Format("Version: {0}.{1}.{2} Devicename: {3}", MajorVersion, MinorVersion, VersionBuild, DeviceName);
        }

    }
}
