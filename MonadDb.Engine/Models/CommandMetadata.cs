using MonadDb.Engine.Parsers.Select.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{
    public class CommandMetadata
    {
        // === CREATE TABLE ===
        public string Table { get; set; }
        public int RecordCount { get; set; } = 0;
        public List<string> PK { get; set; } = new();
        public List<ForeignKeyMetadata> FKs { get; set; } = new();
        public List<ColumnMetadata> Columns { get; set; } = new();
        public int RecordSize => Columns.Sum(c => c.Size);
        public List<SelectPredicateMetadata> Predicates { get; set; } = new();

        // === INSERT ===
       public List<InsertCommandMetadata> InsertCommands { get; set; } = new();
    }
}
