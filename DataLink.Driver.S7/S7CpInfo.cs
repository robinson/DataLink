namespace DataLink.Driver.S7
{
    public class S7CpInfo
    {

        public int MaxPduLength;
        public int MaxConnections;
        public int MaxMpiRate;
        public int MaxBusRate;

        public void Update(byte[] Src, int Pos)
        {
            MaxPduLength = S7.GetShortAt(Src, 2);
            MaxConnections = S7.GetShortAt(Src, 4);
            MaxMpiRate = S7.GetDIntAt(Src, 6);
            MaxBusRate = S7.GetDIntAt(Src, 10);
        }
    }
}
