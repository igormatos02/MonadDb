using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Readers
{
    public class MemoryColumnReader
    {
        private readonly MemoryMappedFile _mmf;
        private readonly int _recordSize;

        public MemoryColumnReader(string filePath, int recordSize)
        {
            _mmf = MemoryMappedFile.CreateFromFile(filePath);
            _recordSize = recordSize;
        }

        public object ReadValue(long recordIndex, ColumnMetadata meta)
        {
            long offset = recordIndex * _recordSize;
            using var stream = _mmf.CreateViewStream(offset, _recordSize);

            byte[] raw = new byte[meta.Size];
            stream.Read(raw, 0, meta.Size);

            return ConvertBinary(raw, meta);
        }

        private object ConvertBinary(byte[] raw, ColumnMetadata meta)
        {
            if (meta.Type == "int")
                return BitConverter.ToInt32(raw, 0);

            if (meta.Type == "string")
                return Encoding.UTF8.GetString(raw).TrimEnd('\0');

            throw new NotImplementedException(meta.Type);
        }
    }

}
