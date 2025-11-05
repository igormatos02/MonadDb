using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{
    public class ForeignKeyMetadata
    {
        public string Target { get; set; } // ex: "address.id"
        public string Local { get; set; }  // ex: "id"
    }
}
