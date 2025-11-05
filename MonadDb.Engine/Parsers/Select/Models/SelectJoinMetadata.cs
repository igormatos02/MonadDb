using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Parsers.Select.Models
{
    public class SelectJoinMetadata
    {
        public string LeftTable { get; set; }
        public string RightTable { get; set; }
        public List<SelectPredicateMetadata> Conditions { get; set; } = new();
    }
}
