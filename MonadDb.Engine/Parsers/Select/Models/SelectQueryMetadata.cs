using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Parsers.Select.Models
{
    public class SelectQueryMetadata
    {
        public List<SelectTableMetadata> Tables { get; set; } = new();
        public List<SelectJoinMetadata> Joins { get; set; } = new();
        public List<SelectAggregationMetadata> Aggregations { get; set; } = new();
    }
}
