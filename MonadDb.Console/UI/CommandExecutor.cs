using MonadDb.Engine;
using MonadDb.Engine.Commands;
using MonadDb.Engine.Interfaces;
using MonadDb.Engine.Util;
namespace MonadDb.Console.UI
{
    public class CommandExecutor
    {
        private readonly MonadConfiguration _config;
        public CommandExecutor(MonadConfiguration config) {
            _config = config;
        }
        public void Execute(string sql) {
            try
            {
                IMonadCommand command = null;
                var type = SqlCommandIdentifier.Identify(sql);
                switch (type)
                {
                    case Engine.Enums.MonadCommandType.CreateTable:
                        {
                            command = new CreateTableCommand(_config);
                            break;
                        }
                    case Engine.Enums.MonadCommandType.Insert:
                        {
                            command = new InsertCommand(_config);
                            break;
                        }
                    default: { 
                        
                        }
                    break;
                }


                if (command == null) {
                    throw new Exception("Invalid Command!");
                }
                command.ExecuteCommand(sql);

            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
        }
    }
}
