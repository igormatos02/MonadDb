/*using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Readers
{
    using MonadDb.Engine.Util;
    using System.Collections.Concurrent;
    using System.IO.MemoryMappedFiles;

    public class _MemoryTableReader
    {
        private static readonly ConcurrentDictionary<string, MemoryMappedFile> _files
            = new ConcurrentDictionary<string, MemoryMappedFile>();

        private readonly string _file;
        private readonly TableMetadata _metadata;
        private readonly List<PredicateFilter> _predicates;

        // lock apenas se um dia for preciso escrever
        private static readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        public _MemoryTableReader(string file, TableMetadata metadata, List<PredicateFilter> predicates)
        {
            _file = file;
            _metadata = metadata;
            _predicates = predicates;
        }

        public List<RecordResult> Read()
        {
            _rwLock.EnterReadLock();
            try
            {
                var mmf = _files.GetOrAdd(_file,
                    path => MemoryMappedFile.CreateFromFile(path));

                using var view = mmf.CreateViewStream();

                long fileLength = view.Length;
                int recordSize = _metadata.RecordSize;

                // ✅ novo: usa classe externa para calcular posições
                var positionCalculator = new RecordPositionCalculator();
                var validRecords = positionCalculator.Calculate(fileLength, recordSize);

                var neededColumns = _predicates.Select(p => p.Column).Distinct().ToList();
                byte[] buffer = new byte[recordSize];

                Parallel.ForEach(validRecords.Keys.Chunk(255), batch =>
                {
                    using var localView = mmf.CreateViewStream();

                    foreach (var recordIndex in batch)
                    {
                        var pos = validRecords[recordIndex].start;
                        localView.Position = pos;
                        localView.Read(buffer, 0, recordSize);

                        bool keep = true;

                        foreach (var pred in _predicates)
                        {
                            var col = _metadata.Columns.First(c => c.Name == pred.Column);
                            int offset = _metadata.GetColumnOffset(col.Name);
                            var raw = buffer.Skip(offset).Take(col.Size).ToArray();
                            object val = ConvertBinary(raw, col);

                            if (!EvaluatePredicate(val, pred))
                            {
                                keep = false;
                                break;
                            }
                        }

                        if (!keep)
                            validRecords.TryRemove(recordIndex, out _);
                    }
                });

                var result = new ConcurrentBag<RecordResult>();

                Parallel.ForEach(validRecords, record =>
                {
                    using var v = mmf.CreateViewStream();
                    byte[] rawRecord = new byte[recordSize];

                    v.Position = record.Value.start;
                    v.Read(rawRecord, 0, recordSize);

                    var values = new List<object>();
                    foreach (var col in _metadata.Columns)
                    {
                        int offset = _metadata.GetColumnOffset(col.Name);
                        var raw = rawRecord.Skip(offset).Take(col.Size).ToArray();
                        values.Add(ConvertBinary(raw, col));
                    }

                    result.Add(new RecordResult
                    {
                        LineNumber = record.Key,
                        Values = values.ToArray()
                    });
                });

                return result.ToList();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }


        private object ConvertBinary(byte[] raw, ColumnMetadata meta)
        {
            if (meta.Type == "int")
                return BitConverter.ToInt32(raw, 0);

            if (meta.Type == "string")
                return Encoding.UTF8.GetString(raw).TrimEnd('\0');

            throw new NotImplementedException(meta.Type);
        }

        private bool EvaluatePredicate(object value, PredicateFilter pred)
        {
            var comp = Convert.ChangeType(pred.Value, value.GetType());

            if (pred.Operator.ToLower() == "like")
            {
                if (value is not string s)
                    throw new Exception("LIKE só pode ser aplicado em colunas string.");

                var pattern = pred.Value;

                // %abc%  => contém
                if (pattern.StartsWith("%") && pattern.EndsWith("%"))
                {
                    var inner = pattern.Trim('%');
                    return s.Contains(inner, StringComparison.OrdinalIgnoreCase);
                }

                // abc% => começa com
                if (pattern.EndsWith("%"))
                {
                    var prefix = pattern.TrimEnd('%');
                    return s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                }

                // %abc => termina com
                if (pattern.StartsWith("%"))
                {
                    var suffix = pattern.TrimStart('%');
                    return s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
                }

                // sem % = igual
                return s.Equals(pattern, StringComparison.OrdinalIgnoreCase);
            }

            return pred.Operator switch
            {
                "=" => value.Equals(comp),
                ">=" => (dynamic)value >= (dynamic)comp,
                "<=" => (dynamic)value <= (dynamic)comp,
                ">" => (dynamic)value > (dynamic)comp,
                "<" => (dynamic)value < (dynamic)comp,
                "!=" => !value.Equals(comp),
                _ => throw new Exception($"Operador inválido: {pred.Operator}")
            };
        }
    }

}
*/