using DataLink.Driver.S7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UwpTestRunner
{
    /// <summary>
    /// run with softPlC
    /// </summary>
    public class S7DriverTests
    {
        private static long Elapsed;
        private static byte[] Buffer = new byte[65536]; // 64K buffer (maximum for S7400 systems)
        private static S7ClientAsync Client = new S7ClientAsync();
        private static int ok = 0;
        private static int ko = 0;
        private static String IpAddress = "192.168.48.100";
        private static int Rack = 0; // Default 0 for S7300
        private static int Slot = 2; // Default 2 for S7300 
        private static int DBSample = 1; // Sample DB that must be present in the CPU
        private static int DataToMove; // Data size to read/write
        [Fact]
        public void ReadDB100Int12Test()
        {
            int result = 0;
            if (!Client.Connected)
                result = Client.ConnectTo(IpAddress, Rack, Slot);
            if (result == 0)
            {
                byte[] buffer = new byte[2];
                result = Client.ReadArea(S7.S7AreaDB, 100, 12, 2, buffer);
                var int12 = S7.GetShortAt(buffer, 0);
                Assert.Equal(int12, 99);
            }
        }
        [Fact]
        public void ConnectTest()
        {
            Client.SetConnectionType(S7.OP);
            int Result = Client.ConnectTo(IpAddress, Rack, Slot);
            Assert.Equal(Result, 0);
            Assert.Equal(Client.Connected, true);
        }
        [Fact]
        public void WriteDb100Int12Test()
        {
            int value = 99;
            int result = 0;
            if (!Client.Connected)
                result = Client.ConnectTo(IpAddress, Rack, Slot);
            if (result == 0)
            {
                byte[] Buffer = new byte[26];
                S7.SetShortAt(Buffer, 12, value);
                result = Client.WriteArea(S7.S7AreaDB, 100,0, 26, Buffer);
                Assert.Equal(result, 0);
                result = Client.ReadArea(S7.S7AreaDB, 100, 0, 26, Buffer);
                var int12 = S7.GetShortAt(Buffer, 12);
                Assert.Equal(int12, value);
            }
        }
    }
}
