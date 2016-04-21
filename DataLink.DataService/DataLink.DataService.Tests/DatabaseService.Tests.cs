using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DataLink.DataService.Tests
{
    public class DatabaseService
    {
        [Fact]
        public void SendDataTest()
        {

            DBService.DatabaseServiceClient client = new DBService.DatabaseServiceClient();
            //client.SendData()
        }
    }
}
