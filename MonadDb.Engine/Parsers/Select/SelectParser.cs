using MonadDb.Engine.Parsers.Models;
using MonadDb.Engine.Parsers.Select.Models;
using System.Text.RegularExpressions;

namespace MonadDb.Engine.Parsers.Select
{
    public class SelectParser
    {
        public SelectQueryMetadata Parse(string sql)
        {
            var query = new SelectQueryMetadata();

            // Normalize whitespace
            sql = Regex.Replace(sql, @"\s+", " ").Trim();

            // 1️⃣ Parse SELECT clause for columns and aggregations
            var selectMatch = Regex.Match(sql, @"SELECT (.+?) FROM", RegexOptions.IgnoreCase);
            if (selectMatch.Success)
            {
                var selectPart = selectMatch.Groups[1].Value;
                foreach (var item in selectPart.Split(','))
                {
                    var trimmed = item.Trim();
                    var aggMatch = Regex.Match(trimmed, @"(\w+)\((\w+)\)(?: AS (\w+))?", RegexOptions.IgnoreCase);
                    if (aggMatch.Success)
                    {
                        query.Aggregations.Add(new SelectAggregationMetadata
                        {
                            Function = aggMatch.Groups[1].Value.ToUpper(),
                            Column = aggMatch.Groups[2].Value,
                            Alias = aggMatch.Groups[3].Success ? aggMatch.Groups[3].Value : null
                        });
                    }
                }
            }

            // 2️⃣ Parse FROM clause to identify tables
            var fromMatch = Regex.Match(sql, @"FROM (.+?)( WHERE| JOIN|$)", RegexOptions.IgnoreCase);
            if (fromMatch.Success)
            {
                var tableNames = fromMatch.Groups[1].Value.Split(',');
                foreach (var t in tableNames)
                {
                    query.Tables.Add(new SelectTableMetadata { Name = t.Trim() });
                }
            }

            // 3️⃣ Parse JOINs
            var joinRegex = new Regex(@"(INNER|LEFT|RIGHT)?\s*JOIN\s+(\w+)\s+ON\s+(.+?)( WHERE| JOIN|$)", RegexOptions.IgnoreCase);
            foreach (Match m in joinRegex.Matches(sql))
            {
                var join = new SelectJoinMetadata
                {
                    RightTable = m.Groups[2].Value.Trim(),
                    Conditions = ParsePredicates(m.Groups[3].Value)
                };
                query.Joins.Add(join);
            }

            // 4️⃣ Parse WHERE clause
            var whereMatch = Regex.Match(sql, @"WHERE (.+)$", RegexOptions.IgnoreCase);
            if (whereMatch.Success)
            {
                var wherePredicates = ParsePredicates(whereMatch.Groups[1].Value);
                // assign to first table for simplicity
                if (query.Tables.Count > 0)
                    query.Tables[0].Predicates.AddRange(wherePredicates);
            }

            return query;
        }

        // Recursively parse AND / OR predicates
        private List<SelectPredicateMetadata> ParsePredicates(string expr)
        {
            var predicates = new List<SelectPredicateMetadata>();

            // Split on AND/OR but keep them
            var regex = new Regex(@"\s+(AND|OR)\s+", RegexOptions.IgnoreCase);
            var parts = regex.Split(expr);

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                if (part.Equals("AND", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("OR", StringComparison.OrdinalIgnoreCase))
                    continue;

                var opMatch = Regex.Match(part, @"(\w+)\s*(=|<>|>=|<=|>|<|LIKE)\s*(.+)", RegexOptions.IgnoreCase);
                if (opMatch.Success)
                {
                    var logic = i > 0 ? parts[i - 1].Trim().ToUpper() : "AND";
                    predicates.Add(new SelectPredicateMetadata
                    {
                        Column = opMatch.Groups[1].Value,
                        Operator = opMatch.Groups[2].Value,
                        Value = opMatch.Groups[3].Value.Trim('\''),
                        Logic = logic
                    });
                }
            }

            return predicates;
        }
    }
}
