using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine
{
    public class MonadConfiguration
    {
        private string dbDirectory="c:/Repos/db";

        public string GetDbDirectory()
        {
            return dbDirectory;
        }
    }
}
