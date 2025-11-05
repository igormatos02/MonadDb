
using MonadDb.Console.UI;
using MonadDb.Engine;



try
{
   // string sql = "CREATE TABLE user(id INT PRIMARY KEY, name STRING(20), age INT);";
    string sql = "INSERT INTO user (id, name, age) VALUES (7, \"igor\", 21);\r\n INSERT INTO user VALUES (8, \"joyce\", 40);\r\n INSERT INTO user VALUES (9, \"Rafael\", 100);";
    MonadConfiguration config = new MonadConfiguration();
    CommandExecutor commandExecutor = new CommandExecutor(config);

    commandExecutor.Execute(sql);
}
catch (Exception ex) { 
    Console.WriteLine(ex.ToString());
}
//SelectEngine selectEngine = new SelectEngine();

//string sql = "SELECT id FROM user";
//selectEngine.Select(sql);
/*var query = SelectParser.Parse(sql);

var reader = new MemoryTableReader(usersMetadata, query.Predicates);
var filtered = reader.Read();

// Aqui aplicamos a agregação
var finalResults = QueryProcessor.ApplyAggregation(query, filtered, usersMetadata);

// Aqui apenas imprimimos
ConsolePrinter.PrintTable(query, finalResults, usersMetadata);
//var engine = new StorageEngine("c://Repos/db");
//engine.Start();
*/
// create default admin if not exists
//if (!engine.UserManager.UserExists("admin"))
//  engine.UserManager.CreateUser("admin", "admin123", new[] { "DBA" });
//Console.WriteLine("Mini SQL Engine (types nativos + índice primário).");
//Console.WriteLine("Comandos suportados: CREATE TABLE, INSERT INTO, SELECT * FROM ... WHERE id = <int>, EXIT");
/*var currentUser = "admin";
var metadata = new TableMetadata
{
    Table = "user",
    Columns = new()
    {
        new ColumnMetadata {Name="id", Type="int", Size=4},
        new ColumnMetadata {Name="name", Type="string", Size=25}
    }
};

var predicates = new List<PredicateFilter>
{
    //new PredicateFilter {Column="id", Operator=">=", Value="10"}
    new PredicateFilter {Column="id", Operator=">=", Value="10"}
};

var reader = new MemoryTableReader("c:/Repos/db/users.bin", metadata, predicates);

var result = reader.Read();

foreach (var rec in result)
{
    Console.WriteLine($"Linha: {rec.LineNumber} | Valores: {string.Join(",", rec.Values)}");
}

   */
/*
while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line == null) break;
    line = line.Trim();
    if (string.Equals(line, "EXIT", StringComparison.OrdinalIgnoreCase)) break;
    try
    {
        var rows = engine.Execute(line, currentUser);
        if (rows != null)
        {
            foreach (var r in rows)
            {
                Console.WriteLine(string.Join(" | ", r.Select(FormatField)));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("ERR: " + ex.Message);
    }
}
*/
//engine.Stop();

static string FormatField(object o)
{
    if (o == null) return "NULL";
    if (o is DateTime dt) return dt.ToString("o");
    return o.ToString();
}
