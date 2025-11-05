using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{
    public class Aggregation
    {
        public string Function { get; set; }   // COUNT, SUM, AVG, MAX, MIN
        public string Column { get; set; }     // *, id, etc
    }
}
