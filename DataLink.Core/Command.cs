using System.Collections.Generic;

namespace DataLink.Core
{
    public sealed class Command
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string CommandText { get; set; }
        public string StoreProcedure { get; set; }
        public string TableName { get; set; }
        public IList<string> Tags {get;set;}
}
}