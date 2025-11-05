using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{
    public class RecordPosition
    {
        public RecordPosition() {
            ColumnPositions = new List<ColumnPosition>();
        }
        public long RecordIndex;
        public required List<ColumnPosition> ColumnPositions;
        public bool IsValid;
    }
}
