using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{   
    public class ColumnMetadata
    {
        public string Name { get; set; }
        public string Type { get; set; } 
        public int Size { get; set; }    
    }

}
