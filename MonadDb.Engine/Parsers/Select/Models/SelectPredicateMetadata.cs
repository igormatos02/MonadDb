using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Parsers.Select.Models
{
    public class SelectPredicateMetadata
    {
        public string Column { get; set; }
        public string Operator { get; set; } // =, <, >, LIKE, etc
        public object Value { get; set; }
        public string Logic { get; set; } = "AND"; // AND / OR
        public List<SelectPredicateMetadata> NestedPredicates { get; set; } = new(); // for parentheses
    }
}
