using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Writers
{
    public class MemoryColumnWriter
    {
        private readonly TableMetadata _metadata;

        public void Insert(object[] values)
        {
            for (int i = 0; i < _metadata.Columns.Count; i++)
            {
                var col = _metadata.Columns[i];
                var value = values[i];
              //  WriteValueToColumnFile(col, value); // já existe
            }

            _metadata.RecordCount++; // ✅ incrementa
            //SaveMetadata(_metadata); // ✅ grava no disco
        }
    }
}
