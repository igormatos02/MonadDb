using MonadDb.Engine.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MonadDb.Engine.Util
{
    public static class SqlCommandIdentifier
    {
        public static MonadCommandType Identify(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return MonadCommandType.Unknown;

            // Remove comentários
            sql = RemoveComments(sql).Trim().ToLower();

            // Normaliza espaços
            sql = Regex.Replace(sql, @"\s+", " ");

            // Testes na ordem certa
            if (sql.StartsWith("create table"))
                return MonadCommandType.CreateTable;

            if (sql.StartsWith("alter table"))
                return MonadCommandType.AlterTable;

            if (sql.StartsWith("drop table"))
                return MonadCommandType.DropTable;

            if (sql.StartsWith("insert into"))
                return MonadCommandType.Insert;

            if (sql.StartsWith("update "))
                return MonadCommandType.Update;

            if (sql.StartsWith("delete from"))
                return MonadCommandType.Delete;

            if (sql.StartsWith("select "))
                return MonadCommandType.Select;

            return MonadCommandType.Unknown;
        }

        private static string RemoveComments(string sql)
        {
            // Remove comentários de linha "--" e "//"
            sql = Regex.Replace(sql, @"(--|//).*?$", "", RegexOptions.Multiline);

            // Remove comentários de bloco "/* ... */"
            sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);

            return sql;
        }
    }
}
