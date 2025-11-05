using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Parsers.Select.Models
{
    public class SelectTableMetadata
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; } = new();
        public List<SelectPredicateMetadata> Predicates { get; set; } = new();
    }
}
