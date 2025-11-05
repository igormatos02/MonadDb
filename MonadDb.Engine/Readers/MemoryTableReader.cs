using MonadDb.Engine.Models;

namespace MonadDb.Engine.Readers
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class MemoryTableReader
    {
        private readonly string _tablePath;
        private readonly TableMetadata _metadata;
        private readonly List<PredicateFilter> _predicates;
        private readonly Dictionary<string, MemoryColumnReader> _columns;
        private readonly MonadConfiguration _config;

    

        public MemoryTableReader(MonadConfiguration config)
        {
            _config = config;
            string tableFolder = _config.GetDbDirectory()+"/user";

            if (!Directory.Exists(tableFolder))
                throw new Exception($"Table '{"user"}' does not exist.");

            string metadataPath = Path.Combine(tableFolder, $"{"user"}.json");
            var metadata = TableMetadataReader.LoadFromJson(metadataPath);

            _tablePath = _config.GetDbDirectory()+"/"+metadata.Table;
            _metadata = metadata;
            _predicates = new List<PredicateFilter>();
            _config = config;
            _columns = metadata.Columns.ToDictionary(
                c => c.Name,
                c => new MemoryColumnReader(_tablePath+$"/{c.Name}.bin", c.Size)
            );
        }

        public List<RecordResult> Read()
        {
            long totalRecords = _metadata.RecordCount;
            var result = new ConcurrentBag<RecordResult>();

            Parallel.For(0, totalRecords, recordIndex =>
            {
                foreach (var pred in _predicates)
                {
                    var col = _metadata.Columns.First(c => c.Name == pred.Column);
                    var value = _columns[col.Name].ReadValue(recordIndex, col);

                    if (!EvaluatePredicate(value, pred))
                        return;
                }

                // passou nos filtros → materializa toda a linha
                var values = new List<object>();
                foreach (var col in _metadata.Columns)
                    values.Add(_columns[col.Name].ReadValue(recordIndex, col));

                result.Add(new RecordResult
                {
                    LineNumber = recordIndex,
                    Values = values.ToArray()
                });
            });

            return result.ToList();
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
