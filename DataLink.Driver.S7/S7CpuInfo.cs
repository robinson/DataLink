﻿using System;

namespace DataLink.Driver.S7
{
    /// <summary>
    /// Information of the S7 CPU
    /// </summary>
    public class S7CpuInfo
    {

        private static int BufSize = 256;
        protected byte[] Buffer = new byte[BufSize];

        internal void Update(byte[] Src, int Pos)
        {
            Array.Copy(Src, Pos, Buffer, 0, BufSize);
        }

        public String ModuleTypeName()
        {
            return S7.GetStringAt(Buffer, 172, 32);
        }
        public String SerialNumber()
        {
            return S7.GetStringAt(Buffer, 138, 24);
        }
        public String ASName()
        {
            return S7.GetStringAt(Buffer, 2, 24);
        }
        public String Copyright()
        {
            return S7.GetStringAt(Buffer, 104, 26);
        }
        public String ModuleName()
        {
            return S7.GetStringAt(Buffer, 36, 24);
        }
    }
}
