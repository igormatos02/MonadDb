using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{
    public class InsertCommandMetadata
    {
        public string Table { get; set; }
        public bool IsInsert { get; set; } = false;
        public List<string> InsertColumns { get; set; } = new();
        public List<object> InsertValues { get; set; } = new();
    }
}
