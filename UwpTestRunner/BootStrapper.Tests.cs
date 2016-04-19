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
        [Fact]
        public void LoadTagsTest()
        {
            var tagList = Bootstrapper.LoadTags(_tagFile);
            Assert.Equal(tagList.Count, 3);

        }
        [Fact]
        public void LoadHistorian()
        {
            var historian = Bootstrapper.LoadHistorian(_historianFile);
            Assert.Equal(historian.Jobs.Count, 1);
        }
    }
}
