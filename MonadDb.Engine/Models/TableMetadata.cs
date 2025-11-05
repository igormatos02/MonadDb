using MonadDb.Engine.Models;
public class TableMetadata
{
    public string Table { get; set; }      
    public List<ColumnMetadata> Columns { get; set; } 
    public int RecordCount { get; set; }    
}