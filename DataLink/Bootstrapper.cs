using DataLink.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataLink
{
    public sealed class Bootstrapper
    {
        public static IList<Tag> LoadTags(string tagFile)
        {
            if (!File.Exists(tagFile))
                throw new FileNotFoundException();
            List<Tag> tags = new List<Tag>();
            //load tags
            try
            {
                XDocument xdocument = XDocument.Load(tagFile);
                foreach (var x in xdocument.Descendants("Tag"))
                {
                    var tagItem = new Tag();
                    tagItem.Area = (x.Attribute("Area") != null) ? x.Attribute("Area").Value : string.Empty;
                    tagItem.DataType = (x.Attribute("DataType") != null) ? x.Attribute("DataType").Value : string.Empty;
                    tagItem.Name = (x.Attribute("Name") != null) ? x.Attribute("Name").Value : string.Empty;
                    tagItem.Number = (x.Attribute("Number") != null) ? Convert.ToInt32(x.Attribute("Number").Value) : 0;
                    tagItem.Position = (x.Attribute("Position") != null) ? Convert.ToInt32(x.Attribute("Position").Value) : 0;
                    tags.Add(tagItem);
                }
            }
            catch (Exception ex)
            {
                //log
                throw ex;
            }
            return tags;
        }
        public static Historian LoadHistorian(string historianFile)
        {
            if (!File.Exists(historianFile))
                throw new FileNotFoundException();
            var historian = new Historian();
            try
            {
                XDocument xdocument = XDocument.Load(historianFile);
                historian.DatabaseType = (xdocument.Element("DataLinkHistorian").Attribute("DatabaseType") != null) ?
                    (DatabaseType)Enum.Parse(typeof(DatabaseType), xdocument.Element("DataLinkHistorian").Attribute("DatabaseType").Value) :
                    DatabaseType.MSSQL;
                historian.ConnectionString = (xdocument.Element("DataLinkHistorian").Attribute("ConnectionString") != null) ?
                 xdocument.Element("DataLinkHistorian").Attribute("ConnectionString").Value : string.Empty;
                historian.Jobs = new List<Job>();
                foreach (var x in xdocument.Descendants("Job"))
                {
                    var jobItem = new Job();
                    jobItem.Name = (x.Attribute("Name") != null) ? x.Attribute("Name").Value : string.Empty;
                    jobItem.CycleTime = (x.Attribute("CycleTime") != null) ? Convert.ToInt32(x.Attribute("CycleTime").Value) : 0;
                    jobItem.Enable = (x.Attribute("Enable") != null) ? Convert.ToBoolean(x.Attribute("Enable").Value) : false;
                    jobItem.Commands = new List<Command>();
                    foreach (var y in x.Descendants("Command"))
                    {
                        var commandItem = new Command();
                        commandItem.Name = (y.Attribute("Name") != null) ? y.Attribute("Name").Value : string.Empty;
                        commandItem.Type = (y.Attribute("Type") != null) ? y.Attribute("Type").Value : string.Empty;
                        commandItem.CommandText = (y.Attribute("CommandText") != null) ? y.Attribute("CommandText").Value : string.Empty;
                        commandItem.StoreProcedure = (y.Attribute("StoreProcedure") != null) ? y.Attribute("StoreProcedure").Value : string.Empty;
                        commandItem.TableName = (y.Attribute("TableName") != null) ? y.Attribute("TableName").Value : string.Empty;
                        commandItem.Tags = y.Descendants("Tag")
                                       .Select(node => (string)node.Attribute("Name"))
                                       .ToList();
                        commandItem.Job = jobItem;
                        jobItem.Commands.Add(commandItem);
                    }
                    historian.Jobs.Add(jobItem);
                }

            }
            catch (Exception ex)
            {
                //log
                throw ex;
            }
            return historian;
        }

        public static Machine LoadMachine(string configurationFile)
        {
            if (!File.Exists(configurationFile))
                throw new FileNotFoundException();
            Machine machine = new Machine();
            XDocument xdocument = XDocument.Load(configurationFile);
            var root = xdocument.Element("DataLinkConfiguration");
            machine.Name = (root.Element("Machine").Attribute("Name") != null) ?
                 root.Element("Machine").Attribute("Name").Value : string.Empty;
            machine.IPAddress = (root.Element("Machine").Attribute("IPAddress") != null) ?
                root.Element("Machine").Attribute("IPAddress").Value : string.Empty;
            machine.Rack = (root.Element("Machine").Attribute("Rack") != null) ?
               Convert.ToInt32(root.Element("Machine").Attribute("Rack").Value) : 0;
            machine.Slot = (root.Element("Machine").Attribute("Slot") != null) ?
               Convert.ToInt32(root.Element("Machine").Attribute("Slot").Value) : 0;
            return machine;
        }
    }
}
