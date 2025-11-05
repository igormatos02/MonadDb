using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{
    public class TableRecordMapping
    {
        public TableRecordMapping(){
            Positions = new List<RecordPosition>();
            Columns = new List<string>();
        }
        public List<RecordPosition> Positions { get; set; }
        public List<String> Columns { get; set; }
        public string TableUrl { get; set; }
    }
}
