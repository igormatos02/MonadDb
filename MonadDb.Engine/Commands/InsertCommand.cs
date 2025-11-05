using MonadDb.Engine.Interfaces;
using MonadDb.Engine.Models;
using MonadDb.Engine.Parsers;
using MonadDb.Engine.Readers;
using MonadDb.Engine.Util;
using System.Text.Json;

namespace MonadDb.Engine.Commands
{
    public class InsertCommand : IMonadCommand
    {
        private readonly InsertCommandParser _parser;
        private readonly MonadConfiguration _config;
        private readonly BinaryValueSerializer _binarySerializer;

        public InsertCommand(MonadConfiguration config)
        {
            _parser = new InsertCommandParser();
            _binarySerializer = new BinaryValueSerializer();
            _config = config;
        }

        public CommandResult ExecuteCommand(string sql)
        {
            var inserts = _parser.Parse(sql);
            int totalInserted = 0;
            string? error = null;

            foreach (var insert in inserts)
            {
               
                
                    InsertSingle(insert);
                    totalInserted++;
               
            }

            return new CommandResult
            {
                CommandText = sql,
                Message = error == null
                    ? $"{totalInserted} row(s) inserted successfully."
                    : $"{totalInserted} row(s) inserted successfully. Insert aborted: {error}",
                //Errors = error != null ? new List<string> { error } : new(),
                ExecutionTime = totalInserted
            };
        }

        private void InsertSingle(InsertCommandMetadata insert)
        {
            string tableFolder = Path.Combine(_config.GetDbDirectory(), insert.Table);

            if (!Directory.Exists(tableFolder))
                throw new Exception($"Table '{insert.Table}' does not exist.");

            string metadataPath = Path.Combine(tableFolder, $"{insert.Table}.json");
            var tableMeta = TableMetadataReader.LoadFromJson(metadataPath);

            if (tableMeta.Columns.Count == 0)
                throw new Exception($"Table '{insert.Table}' has no columns.");

            // ✅ Order column values
            List<object> orderedValues;
            if (insert.InsertColumns.Any())
            {
                orderedValues = new();

                foreach (var col in tableMeta.Columns)
                {
                    int idx = insert.InsertColumns.FindIndex(x => x == col.Name);
                    if (idx < 0)
                        throw new Exception($"Missing value for column '{col.Name}'");

                    orderedValues.Add(insert.InsertValues[idx]);
                }
            }
            else
            {
                if (insert.InsertValues.Count != tableMeta.Columns.Count)
                    throw new Exception($"Expected {tableMeta.Columns.Count} values.");

                orderedValues = insert.InsertValues;
            }

            // ✅ Composite PK check
            if (tableMeta.PK.Any())
                EnsureCompositePrimaryKeyUnique(tableFolder, tableMeta, orderedValues, tableMeta.RecordCount);

            int recordIndex = tableMeta.RecordCount;

            // ✅ Write column values to disk
            for (int i = 0; i < tableMeta.Columns.Count; i++)
            {
                var col = tableMeta.Columns[i];
                var val = orderedValues[i];

                string colFile = Path.Combine(tableFolder, $"{col.Name}.bin");

                using var fs = new FileStream(colFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                long offset = recordIndex * col.Size;
                fs.Seek(offset, SeekOrigin.Begin);

                byte[] bytes = _binarySerializer.Serialize(val, col);

                if (bytes.Length > col.Size)
                    throw new Exception($"Value too large for column '{col.Name}' ({bytes.Length}/{col.Size})");

                if (bytes.Length < col.Size)
                {
                    var padded = new byte[col.Size];
                    Array.Copy(bytes, padded, bytes.Length);
                    bytes = padded;
                }

                fs.Write(bytes, 0, bytes.Length);
            }

            // ✅ Update metadata
            tableMeta.RecordCount++;

            var json = JsonSerializer.Serialize(tableMeta, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(metadataPath, json);
        }

        /// <summary>
        /// Validate that a composite primary key is unique across all existing records.
        /// Much faster version: each column file stays open while scanning.
        /// </summary>
        private void EnsureCompositePrimaryKeyUnique(
            string tableFolder,
            TableMetadata tableMeta,
            List<object> newOrderedValues,
            int totalRecords)
        {
            if (totalRecords == 0)
                return;

            var pkCols = tableMeta.PK
                .Select(pkName => tableMeta.Columns.First(c => c.Name == pkName))
                .ToList();

            var pkIndexes = pkCols
                .Select(col => tableMeta.Columns.FindIndex(c => c.Name == col.Name))
                .ToList();

            // ✅ Build new key tuple
            var newKey = pkIndexes.Select(index => newOrderedValues[index]).ToList();

            // ✅ Open all PK binary files ONCE
            var pkFiles = pkCols
                .Select(col => File.OpenRead(Path.Combine(tableFolder, $"{col.Name}.bin")))
                .ToList();

            byte[][] buffers = pkCols
                .Select(col => new byte[col.Size])
                .ToArray();

            for (int r = 0; r < totalRecords; r++)
            {
                var existingKey = new List<object>();

                for (int c = 0; c < pkCols.Count; c++)
                {
                    var col = pkCols[c];
                    var fs = pkFiles[c];
                    fs.Seek(r * col.Size, SeekOrigin.Begin);
                    fs.Read(buffers[c], 0, buffers[c].Length);

                    object storedValue = _binarySerializer.Deserialize(buffers[c], col);
                    existingKey.Add(storedValue);
                }

                // ✅ Check full tuple equality
                bool match = true;
                for (int i = 0; i < newKey.Count; i++)
                {
                    if (!Equals(newKey[i], existingKey[i]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    foreach (var f in pkFiles) f.Close();
                    throw new Exception($"Primary key duplicated: ({string.Join(", ", newKey)})");
                }
            }

            foreach (var f in pkFiles) f.Close();
        }
    }
}
