using MonadDb.Engine.Interfaces;
using MonadDb.Engine.Models;
using System.Text.RegularExpressions;

namespace MonadDb.Engine.Parsers
{
    public class InsertCommandParser
    {
        public List<InsertCommandMetadata> Parse(string sql)
        {
            // split by ; but ignore empty lines
            var commands = sql
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var results = new List<InsertCommandMetadata>();

            foreach (var cmd in commands)
                results.Add(ParseSingle(cmd));

            return results;
        }

        public InsertCommandMetadata ParseSingle(string sql)
        {
            sql = sql.Trim().ToLower();

            var meta = new InsertCommandMetadata
            {
                IsInsert = true
            };

            // ✅ Nome da tabela
            var tableMatch = Regex.Match(sql, @"insert\s+into\s+(\w+)");
            if (!tableMatch.Success)
                throw new Exception("Invalid INSERT syntax: table name not found.");

            meta.Table = tableMatch.Groups[1].Value;

            // ✅ Colunas explícitas: INSERT INTO table (id,name) VALUES(...)
            var colMatch = Regex.Match(sql, @"\(\s*([^)]+?)\s*\)\s*values");
            if (colMatch.Success)
            {
                meta.InsertColumns = colMatch.Groups[1].Value
                    .Split(',', StringSplitOptions.TrimEntries)
                    .ToList();
            }

            // ✅ Valores: VALUES(...)
            var valuesMatch = Regex.Match(sql, @"values\s*\((.+?)\)", RegexOptions.Singleline);
            if (!valuesMatch.Success)
                throw new Exception("Invalid INSERT syntax: VALUES(...) not found.");

            var rawValues = SplitValues(valuesMatch.Groups[1].Value);

            foreach (var v in rawValues)
                meta.InsertValues.Add(ParseValue(v));

            return meta;
        }

        private List<string> SplitValues(string values)
        {
            var list = new List<string>();
            bool inString = false;
            char stringChar = '\0';
            int start = 0;

            for (int i = 0; i < values.Length; i++)
            {
                if ((values[i] == '"' || values[i] == '\''))
                {
                    if (!inString)
                    {
                        inString = true;
                        stringChar = values[i];
                    }
                    else if (values[i] == stringChar)
                    {
                        inString = false;
                    }
                }

                if (values[i] == ',' && !inString)
                {
                    list.Add(values.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            list.Add(values.Substring(start).Trim());
            return list;
        }

        private object ParseValue(string raw)
        {
            raw = raw.Trim();

            if (raw == "null") return null;
            if (raw == "true") return true;
            if (raw == "false") return false;

            if ((raw.StartsWith("\"") && raw.EndsWith("\""))
             || (raw.StartsWith("'") && raw.EndsWith("'")))
                return raw.Substring(1, raw.Length - 2);

            if (int.TryParse(raw, out int i)) return i;
            if (double.TryParse(raw, out double d)) return d;

            throw new Exception($"Invalid value: {raw}");
        }
    }
}


