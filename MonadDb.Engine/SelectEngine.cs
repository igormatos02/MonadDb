using MonadDb.Engine.Models;
using MonadDb.Engine.Parsers.Select;
using MonadDb.Engine.Processors;
using MonadDb.Engine.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonadDb.Engine
{
    public class SelectEngine
    {
        private readonly MonadConfiguration _config;
        public SelectEngine(MonadConfiguration monadConfiguration) {
            _config = monadConfiguration;
        }
        public void Select(string sql)
        {
            SelectParser parser = new SelectParser();
            var selectMetadata = parser.Parse(sql);
            List<TableRecordMapping> selectMapping = new List<TableRecordMapping>();

            foreach(var table in selectMetadata.Tables)
            {
                var path = $"{_config.GetDbDirectory()}/{table.Name}/{table.Name}.json";
                var recordPositionProcessor = new RecordPositionProcessor();
                var metadata = TableMetadataReader.LoadFromJson(path);
                var mapping = recordPositionProcessor.Generate(metadata);
                selectMapping.Add(mapping);
            }
           

        }
    }
}
