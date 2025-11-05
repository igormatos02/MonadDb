using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Enums
{
    public enum MonadCommandType
    {
        CreateTable,
        AlterTable,
        DropTable,
        Insert,
        Update,
        Delete,
        Select,
        Unknown
    }
}
