using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine.Models
{
    public class CommandResult
    {
        public string CommandText { get; set; }
        public string Message { get; set; }
        public int ExecutionTime { get; set; }
    }
}
