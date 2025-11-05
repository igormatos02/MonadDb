namespace MonadDb.Engine.Models
{
    public class SqlQuery
    {
        public List<string> Columns { get; set; } = new();
        public List<PredicateFilter> Predicates { get; set; } = new();
        public List<Aggregation> Aggregations { get; set; } = new();
        public string Table { get; set; }
    }
}
