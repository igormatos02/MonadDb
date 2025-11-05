using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Parsers.Select.Models
{
    public class SelectAggregationMetadata
    {
        public string Function { get; set; } // SUM, COUNT, AVG, etc
        public string Column { get; set; }
        public string Alias { get; set; }
    }
}
