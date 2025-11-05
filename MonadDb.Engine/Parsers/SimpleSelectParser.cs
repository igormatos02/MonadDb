using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Parsers
{
    public static class SimpleSelectParser
    {
        public static SqlQuery Parse(string sql)
        {
            sql = sql.Trim().TrimEnd(';');

            var query = new SqlQuery();

            // localizar SELECT e FROM ignorando case
            int fromIndex = sql.IndexOf(" from ", StringComparison.OrdinalIgnoreCase);
            if (fromIndex < 0)
                throw new Exception("SQL inválido: falta FROM.");

            string selectPart = sql.Substring(6, fromIndex - 6).Trim(); // remove SELECT
            string afterFrom = sql.Substring(fromIndex + 6).Trim();     // depois de FROM

            // extrai tabela
            string table;
            int whereIndex = afterFrom.IndexOf(" where ", StringComparison.OrdinalIgnoreCase);
            if (whereIndex >= 0)
                table = afterFrom.Substring(0, whereIndex).Trim();
            else
                table = afterFrom.Trim();

            query.Table = table;

            // colunas / funções agregadas
            var rawColumns = selectPart.Split(',');
            foreach (var col in rawColumns)
            {
                var c = col.Trim();

                if (c.Contains("("))
                {
                    var fname = c.Substring(0, c.IndexOf("(")).Trim().ToUpper();
                    var inner = c.Substring(c.IndexOf("(") + 1).Replace(")", "").Trim();
                    query.Aggregations.Add(new Aggregation { Function = fname, Column = inner });
                }
                else
                {
                    query.Columns.Add(c);
                }
            }

            // WHERE?
            if (whereIndex >= 0)
            {
                var wherePart = afterFrom.Substring(whereIndex + 7).Trim();

                var conditions = wherePart.Split(" and ", StringSplitOptions.None);

                foreach (var cond in conditions)
                {
                    string condition = cond.Trim();

                    // LIKE
                    var likeIndex = condition.IndexOf(" like ", StringComparison.OrdinalIgnoreCase);
                    if (likeIndex > 0)
                    {
                        var col = condition.Substring(0, likeIndex).Trim();
                        var val = condition.Substring(likeIndex + 6).Trim().Trim('\'');
                        query.Predicates.Add(new PredicateFilter
                        {
                            Column = col,
                            Operator = "like",
                            Value = val
                        });
                        continue;
                    }

                    // operadores normais
                    string[] ops = { ">=", "<=", "!=", ">", "<", "=" };
                    foreach (var op in ops)
                    {
                        int idx = condition.IndexOf(op, StringComparison.OrdinalIgnoreCase);
                        if (idx > 0)
                        {
                            var col = condition.Substring(0, idx).Trim();
                            var val = condition.Substring(idx + op.Length).Trim().Trim('\'');
                            query.Predicates.Add(new PredicateFilter
                            {
                                Column = col,
                                Operator = op,
                                Value = val
                            });
                            break;
                        }
                    }
                }
            }

            return query;
        }
    }

}
