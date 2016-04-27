using DataLink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace UwpTestRunner
{
    public class BootStrapperTests
    {
        private static string _tagFile = "Data/DatalinkTags.xml";
        private static string _historianFile = "Data/DataLinkHistorian.xml";
        private static string _configFile = "Data/DataConfiguration.xml";
        [Fact]
        public void LoadTagsTest()
        {
            var tagList = Bootstrapper.LoadTags(_tagFile);
            Assert.Equal(tagList.Count, 3);

        }
        [Fact]
        public void LoadHistorianTest()
        {
            var historian = Bootstrapper.LoadHistorian(_historianFile);
            Assert.Equal(historian.Jobs.Count, 1);
        }
        [Fact]
        public void LoadMachineTest()
        {
            var machine = Bootstrapper.LoadMachine(_configFile);
            Assert.Equal(machine.Name, "SoftPLC");
        }
    }
}
