using MonadDb.Engine.Models;

namespace MonadDb.UI
{
    public static class ConsolePrinter
    {
        public static void PrintTable( List<RecordResult> results, TableMetadata metadata)
        {
         
                // Imprime apenas as colunas que foram selecionadas
                System.Console.WriteLine(string.Join(" | ", metadata.Columns.Select(x=>x.Name)));
           

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

                System.Console.WriteLine(string.Join(" | ", formatted));
            }

            System.Console.WriteLine();
        }
    }
}
