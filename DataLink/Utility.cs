using DataLink.Core;
using DataLink.Driver.S7;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLink
{
    public sealed class Utility
    {
        internal static int GetByteLengthFromType(string type)
        {
            int length = 65536;
            switch (type)
            {
                case "Bit":
                    length = 1;
                    break;
                case "Word":
                case "Short":
                    length = 2;
                    break;
                case "DWord":
                case "DInt":
                case "Float":
                    length = 4;
                    break;
                default:
                    break;
            }
            return length;
        }
        internal static int GetS7Area(string area)
        {
            int s7Area = 0;
            switch (area)
            {
                case "MK":
                    s7Area = S7.S7AreaMK;
                    break;
                case "PA":
                    s7Area = S7.S7AreaPA;
                    break;
                case "PE":
                    s7Area = S7.S7AreaPE;
                    break;
                case "TM":
                    s7Area = S7.S7AreaTM;
                    break;
                case "DB":
                default:
                    s7Area = S7.S7AreaDB;
                    break;
            }

            return s7Area;
        }

        internal static object GetS7Value(Tag tagItem, byte[] buffer)
        {
            object returnValue;
            if (tagItem.DataType == "Bit")
                returnValue = S7.GetBitAt(buffer, tagItem.Position, 0);
            else if (tagItem.DataType == "Word")
                returnValue = S7.GetWordAt(buffer, 0);
            else if (tagItem.DataType == "Short")
                returnValue = S7.GetShortAt(buffer, 0);
            else if (tagItem.DataType == "DWord")
                returnValue = S7.GetDWordAt(buffer, 0);
            else if (tagItem.DataType == "DInt")
                returnValue = S7.GetDIntAt(buffer, 0);
            else if (tagItem.DataType == "Float")
                returnValue = S7.GetFloatAt(buffer, 0);
            else if (tagItem.DataType == "String")
                returnValue = S7.GetStringAt(buffer,0, buffer.Length);
            else if (tagItem.DataType == "PrintableString")
                returnValue = S7.GetPrintableStringAt(buffer, 0, buffer.Length);
            else if (tagItem.DataType == "Date")
                returnValue = S7.GetDateAt(buffer, 0);
            else
                returnValue = S7.GetStringAt(buffer, 0, buffer.Length);
            return returnValue;
        }
        internal static string BuildData(Tag tag, Machine machine, Job job, Command cmd)
        {
            var messageString = "";
            messageString += (tag != null) ? JsonConvert.SerializeObject(tag): string.Empty;
            messageString += (machine != null) ? JsonConvert.SerializeObject(machine) : string.Empty;
            messageString += (job != null) ? JsonConvert.SerializeObject(job) : string.Empty;
            messageString += (cmd != null) ? JsonConvert.SerializeObject(cmd) : string.Empty;

            return messageString;
        }
        internal static string BuildData(List<Tag> tags)
        {
            var messageString = "";
            foreach (var tagItem in tags)
            {
                messageString += (tagItem != null) ? JsonConvert.SerializeObject(tagItem) : string.Empty;
            }
            return messageString;
        }

    }
}
