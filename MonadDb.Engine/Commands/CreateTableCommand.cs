using MonadDb.Engine.Interfaces;
using MonadDb.Engine.Models;
using MonadDb.Engine.Parsers;
using System.Text.Json;


namespace MonadDb.Engine.Commands
{
    public class CreateTableCommand : IMonadCommand
    {
        private readonly CreateTableCommandParser _parser;
        private readonly MonadConfiguration _config;

        public CreateTableCommand(MonadConfiguration config)
        {
            _parser = new CreateTableCommandParser();
            _config = config;
        }

        public CommandResult ExecuteCommand(string sql)
        {
            var metadata = _parser.Parse(sql);

            var directory = $"{_config.GetDbDirectory()}/{metadata.Table}";

            if (Directory.Exists(directory))
                throw new Exception($"Table {metadata.Table} already exists!");

            Directory.CreateDirectory(directory);

            string fileName = $"{directory}/{metadata.Table}.json";
            SaveToFile(metadata, fileName);
            CreateColumnFiles(metadata, directory);
            return new CommandResult()
            {
                CommandText = sql,
                Message="Table created successfully!",
                ExecutionTime=1
            };
        }

        private void CreateColumnFiles(CommandMetadata metadata, string tablePath)
        {
            foreach (var col in metadata.Columns)
            {
                string colFile = Path.Combine(tablePath, $"{col.Name}.bin");

                // ✅ apenas cria o ficheiro vazio no disco
                using var fs = new FileStream(colFile, FileMode.CreateNew);
            }
        }

        private void SaveToFile(CommandMetadata metadata, string path)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(metadata, options);
            File.WriteAllText(path, json);
        }
    }
}
