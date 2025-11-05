using MonadDb.Engine.Models;

namespace MonadDb.Engine.Interfaces
{
    public interface IMonadCommand
    {
        public CommandResult ExecuteCommand(string sql);
    }
}
