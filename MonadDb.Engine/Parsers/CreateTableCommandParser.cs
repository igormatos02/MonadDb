using MonadDb.Engine.Interfaces;
using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonadDb.Engine.Parsers
{
    public class CreateTableCommandParser 
    {
        public CommandMetadata Parse(string sql)
        {
            sql = sql.Trim().ToLower();

            // ✅ Nome da tabela
            var tableMatch = Regex.Match(sql, @"create\s+table\s+(\w+)");
            if (!tableMatch.Success)
                throw new Exception("Invalid CREATE TABLE syntax. Could not find table name.");

            string tableName = tableMatch.Groups[1].Value;

            // ✅ Extrai conteúdo entre os parênteses externos
            string inside = ExtractDefinitionBlock(sql);

            // ✅ Divide em linhas, respeitando parênteses internos
            var lines = SplitDefinitionLines(inside);

            var metadata = new CommandMetadata
            {
                Table = tableName,
                Columns = new List<ColumnMetadata>(),
                PK = new List<string>(),
                FKs = new List<ForeignKeyMetadata>()
            };

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

               
                var colMatch = Regex.Match(line, @"^(\w+)\s+(int|string)(\((\d+)\))?");
                if (colMatch.Success)
                {
                    var colName = colMatch.Groups[1].Value;
                    var type = colMatch.Groups[2].Value;
                    int size = type == "int" ? 4 : int.Parse(colMatch.Groups[4].Value);

                    metadata.Columns.Add(new ColumnMetadata
                    {
                        Name = colName,
                        Type = type,
                        Size = size
                    });

                   
                    if (line.Contains("primary key"))
                        metadata.PK.Add(colName);

                    continue;
                }

             
                var pkMatch = Regex.Match(line, @"primary\s+key\s*\((.+?)\)");
                if (pkMatch.Success)
                {
                    var pks = pkMatch.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries);
                    metadata.PK.AddRange(pks);
                    continue;
                }

                // ✅ FOREIGN KEY
                var fkMatch = Regex.Match(line,
                    @"foreign key\s*\((\w+)\)\s+references\s+(\w+)\((\w+)\)");

                if (fkMatch.Success)
                {
                    string local = fkMatch.Groups[1].Value;
                    string targetTable = fkMatch.Groups[2].Value;
                    string targetCol = fkMatch.Groups[3].Value;

                    metadata.FKs.Add(new ForeignKeyMetadata
                    {
                        Local = local,
                        Target = $"{targetTable}.{targetCol}"
                    });

                    continue;
                }
            }

            return metadata;
        }


        // ✅ Captura bloco entre o par de parênteses externos
        private string ExtractDefinitionBlock(string sql)
        {
            int start = sql.IndexOf('(');
            if (start == -1)
                throw new Exception("Invalid syntax: missing '('");

            int depth = 0;
            for (int i = start; i < sql.Length; i++)
            {
                if (sql[i] == '(') depth++;
                else if (sql[i] == ')') depth--;

                if (depth == 0)
                {
                    // conteúdo sem os parênteses
                    return sql.Substring(start + 1, i - start - 1);
                }
            }

            throw new Exception("Invalid syntax: parentheses not balanced");
        }

  

        // ✅ Divide itens por vírgula somente no nível superior
        private List<string> SplitDefinitionLines(string block)
        {
            var result = new List<string>();
            int depth = 0, start = 0;

            for (int i = 0; i < block.Length; i++)
            {
                if (block[i] == '(') depth++;
                else if (block[i] == ')') depth--;

                if (block[i] == ',' && depth == 0)
                {
                    result.Add(block.Substring(start, i - start));
                    start = i + 1;
                }
            }

            result.Add(block.Substring(start));
            return result;
        }
    }
}
