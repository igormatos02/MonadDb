using MonadDb.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MonadDb.Engine.Readers
{
    public static class TableMetadataReader
    {
        public static TableMetadata LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Metadata file not found: {filePath}");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            string json = File.ReadAllText(filePath);

            var metadata = JsonSerializer.Deserialize<TableMetadata>(json, options);

            if (metadata == null)
                throw new Exception("Failed to deserialize TableMetadata from JSON.");

            return metadata;
        }
    }
}
