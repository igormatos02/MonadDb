using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Processors
{
    public static class QueryProcessor
    {
        public static List<RecordResult> ApplyAggregation(SqlQuery query, List<RecordResult> results, TableMetadata metadata)
        {
            // não tem agregação → retorna resultados normais
            if (!query.Aggregations.Any())
                return results;

            var aggregated = new List<RecordResult>();
            var valuesList = new List<object>();

            foreach (var agg in query.Aggregations)
            {
                double resultValue = 0;

                if (agg.Function.ToUpper() == "COUNT" && agg.Column == "*")
                {
                    resultValue = results.Count;
                }
                else
                {
                    var colMeta = metadata.Columns.First(c => c.Name == agg.Column);
                    int colIndex = metadata.Columns.IndexOf(colMeta);

                    var columnValues = results
                        .Select(r => Convert.ToDouble(r.Values[colIndex]))
                        .ToList();

                    resultValue = agg.Function.ToUpper() switch
                    {
                        "SUM" => columnValues.Sum(),
                        "AVG" => columnValues.Average(),
                        "MAX" => columnValues.Max(),
                        "MIN" => columnValues.Min(),
                        _ => throw new Exception($"Função agregada não suportada: {agg.Function}")
                    };
                }

                valuesList.Add(resultValue);
            }

            aggregated.Add(new RecordResult
            {
                LineNumber = -1,
                Values = valuesList.ToArray()
            });

            return aggregated;
        }
    }

}
