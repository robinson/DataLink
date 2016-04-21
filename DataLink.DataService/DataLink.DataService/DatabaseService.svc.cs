using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace DataLink.DataService
{
    public class DatabaseService : IDatabaseService
    {
        static DataProcessor processor;
        public DatabaseService()
        {
            if(processor == null)
                processor = new DataProcessor();
        }

        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }
        public string SendData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return "Empty Data";
            }
            processor.AddToQueue(data);
            return "Success receive";
        }
    }
}
