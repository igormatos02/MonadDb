using MonadDb.Engine.Models;
using System.IO;

namespace MonadDb.Engine.Processors
{
    public class RecordPositionProcessor
    {

        public TableRecordMapping Generate(TableMetadata metadata)
        {
            TableRecordMapping tableRecordMapping = new TableRecordMapping();

            //var _tableFolder = $"{MonadConfiguration.baseUrl}/{metadata.Table}/";
            var _tableFolder = $"";
            tableRecordMapping.TableUrl = _tableFolder;
            var firstCol = metadata.Columns.First();
            string firstPath = Path.Combine(_tableFolder, "col_"+firstCol.Name + ".bin");

            long fileLength = new FileInfo(firstPath).Length;
            long totalRecords = fileLength / firstCol.Size;

            for (long recordIndex = 0; recordIndex < totalRecords; recordIndex++)
            {
                var record = new RecordPosition
                {
                    RecordIndex = recordIndex,
                    IsValid = true,
                    ColumnPositions = new List<ColumnPosition>()
                };

                foreach (var col in metadata.Columns)
                {
                    long start = recordIndex * col.Size;
                    long end = start + col.Size;

                    record.ColumnPositions.Add(new ColumnPosition
                    {
                        Start = start,
                        End = end
                    });
                }

                tableRecordMapping.Positions.Add(record);
            }

            return tableRecordMapping;
        }
    }
    
}
