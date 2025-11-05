using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{
    public class PredicateFilter
    {
        public string Column { get; set; }
        public string Operator { get; set; } // =, >=, <=, !=, etc
        public string Value { get; set; }
    }
}
