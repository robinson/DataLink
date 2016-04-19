using System;

namespace DataLink.Driver.S7
{
    public class S7Szl
    {
        public int LENTHDR;
        public int N_DR;
        public int DataSize;
        public byte[] Data;

        public S7Szl(int BufferSize)
        {
            Data = new byte[BufferSize];
        }
        internal void Copy(byte[] Src, int SrcPos, int DestPos, int Size)
        {
            Array.Copy(Src, SrcPos, Data, DestPos, Size);
        }
    }
}
