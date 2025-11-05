using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonadDb.UI
{
    public static class ConsolePrinter
    {
        public static void PrintTable(SqlQuery query, List<RecordResult> results, TableMetadata metadata)
        {
            // Se for agregação, o cabeçalho é o nome da função + coluna
            if (query.Aggregations?.Any() == true)
            {
                var headers = query.Aggregations
                    .Select(a => $"{a.Function.ToUpper()}({a.Column})");

                Console.WriteLine(string.Join(" | ", headers));
            }
            else
            {
                // Imprime apenas as colunas que foram selecionadas
                Console.WriteLine(string.Join(" | ", query.Columns));
            }

            // Agora imprime cada linha
            foreach (var record in results)
            {
                // record.Values já está alinhado com query.Columns
                var formatted = record.Values.Select(v =>
                {
                    if (v == null) return "null";
                    if (v is string s) return s;
                    if (v is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
                    return v.ToString();
                });

                Console.WriteLine(string.Join(" | ", formatted));
            }

            Console.WriteLine();
        }
    }
}
