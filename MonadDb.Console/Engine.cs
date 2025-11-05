// Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
namespace MonadDb
{ 


#region Engine

enum DataType : byte { INT = 1, TEXT = 2, BOOL = 3, FLOAT = 4, DATE = 5 }

class Column
{
    public string Name { get; set; }
    public DataType Type { get; set; }
    public override string ToString() => $"{Name} {Type}";
}

class TableSchema
{
    public string TableName { get; set; }
    public Column[] Columns { get; set; }
    public string DataFile => TableName + ".dat";
    public string IndexFile => TableName + ".idx";
    public string SchemaFile => TableName + ".schema.json";
}

class StorageEngine
{
    readonly string path;
    readonly Dictionary<string, Table> tables = new();
    public readonly Wal Wal;
    public readonly UserManager UserManager;

    public StorageEngine(string path)
    {
        this.path = path;
        Directory.CreateDirectory(path);
        Wal = new Wal(System.IO.Path.Combine(path, "wal.log"));
        UserManager = new UserManager(System.IO.Path.Combine(path, "users.dat"));
    }

    public void Start()
    {
        UserManager.Load();
        // load schemas and tables
        foreach (var f in Directory.GetFiles(path, "*.schema.json"))
        {
            var json = File.ReadAllText(f);
            var schema = JsonSerializer.Deserialize<TableSchema>(json);
            if (schema != null)
            {
                var t = new Table(Path.Combine(path, schema.DataFile), Path.Combine(path, schema.IndexFile), schema);
                t.LoadIndex();
                tables[schema.TableName] = t;
            }
        }
        // replay WAL
        Wal.Replay(this);
    }

    public void Stop()
    {
        Wal.Flush();
        foreach (var t in tables.Values) t.Flush();
    }

    public List<object[]> Execute(string sql, string user)
    {
        sql = sql.Trim();
        if (sql.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase))
        {
            if (!UserManager.HasPermission(user, "CREATE")) throw new Exception("Sem permissão CREATE");
            var m = System.Text.RegularExpressions.Regex.Match(sql, @"CREATE\s+TABLE\s+(\w+)\s*\((.+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) throw new Exception("CREATE TABLE syntax inválida");
            var name = m.Groups[1].Value;
            var colsDef = m.Groups[2].Value.Split(',').Select(s => s.Trim()).ToArray();
            var cols = colsDef.Select(d =>
            {
                var parts = d.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) throw new Exception("col definition inválida");
                return new Column { Name = parts[0], Type = ParseType(parts[1]) };
            }).ToArray();

            // require first column INT as primary key
            if (cols.Length == 0 || cols[0].Type != DataType.INT) throw new Exception("Primeira coluna deve ser INT e será a PRIMARY KEY (nome qualquer).");

            var schema = new TableSchema { TableName = name, Columns = cols };
            var schemaJson = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(System.IO.Path.Combine(path, schema.SchemaFile), schemaJson);
            var t = new Table(System.IO.Path.Combine(path, schema.DataFile), System.IO.Path.Combine(path, schema.IndexFile), schema);
            tables[name] = t;
            Console.WriteLine($"Tabela {name} criada com colunas: {string.Join(", ", cols.Select(c => c.ToString()))}");
            return null;
        }
        else if (sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
        {
            if (!UserManager.HasPermission(user, "INSERT")) throw new Exception("Sem permissão INSERT");
            // simple parser: INSERT INTO name (c1,c2) VALUES (v1,v2)
            var m = System.Text.RegularExpressions.Regex.Match(sql, @"INSERT\s+INTO\s+(\w+)\s*(?:\((.+?)\))?\s*VALUES\s*\((.+)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) throw new Exception("INSERT syntax inválida");
            var name = m.Groups[1].Value;
            if (!tables.TryGetValue(name, out var t)) throw new Exception("Tabela não existe");
            var colsPart = m.Groups[2].Success ? m.Groups[2].Value : null;
            var valsPart = m.Groups[3].Value;
            var vals = SplitCsv(valsPart).Select(s => Unquote(s.Trim())).ToArray();

            // map values to columns
            string[] colNames;
            if (colsPart == null) colNames = t.Schema.Columns.Select(c => c.Name).ToArray();
            else colNames = colsPart.Split(',').Select(s => s.Trim()).ToArray();

            if (colNames.Length != vals.Length) throw new Exception("Col count mismatch");

            var row = new object[t.Schema.Columns.Length];
            for (int i = 0; i < colNames.Length; i++)
            {
                var colIdx = Array.FindIndex(t.Schema.Columns, c => c.Name.Equals(colNames[i], StringComparison.OrdinalIgnoreCase));
                if (colIdx < 0) throw new Exception("Coluna não existe: " + colNames[i]);
                row[colIdx] = ParseLiteral(vals[i], t.Schema.Columns[colIdx].Type);
            }
            // fill others as null
            t.Insert(row, Wal);
            Console.WriteLine("1 row inserted.");
            return null;
        }
        else if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            if (!UserManager.HasPermission(user, "SELECT")) throw new Exception("Sem permissão SELECT");
            // support: SELECT * FROM table WHERE id = <int>
            var m = System.Text.RegularExpressions.Regex.Match(sql, @"SELECT\s+\*\s+FROM\s+(\w+)(?:\s+WHERE\s+(\w+)\s*=\s*(.+))?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) throw new Exception("SELECT syntax inválida");
            var name = m.Groups[1].Value;
            if (!tables.TryGetValue(name, out var t)) throw new Exception("Tabela não existe");
            if (m.Groups[2].Success)
            {
                var whereCol = m.Groups[2].Value;
                var whereValRaw = Unquote(m.Groups[3].Value.Trim());
                // only efficient if whereCol is primary key (first col)
                if (whereCol.Equals(t.Schema.Columns[0].Name, StringComparison.OrdinalIgnoreCase))
                {
                    var key = int.Parse(whereValRaw);
                    var row = t.SelectByPrimaryKey(key);
                    return row == null ? new List<object[]>() : new List<object[]> { row };
                }
                else
                {
                    // fallback to full scan
                    return t.Select(whereCol, whereValRaw).Select(r => r.Select(box => box).ToArray()).ToList();
                }
            }
            else
            {
                return t.SelectAll().Select(r => r.Select(box => box).ToArray()).ToList();
            }
        }
        else
            throw new Exception("Comando não suportado.");
    }

    static DataType ParseType(string t)
    {
        t = t.ToUpperInvariant();
        return t switch
        {
            "INT" => DataType.INT,
            "TEXT" => DataType.TEXT,
            "BOOL" => DataType.BOOL,
            "FLOAT" => DataType.FLOAT,
            "DATE" => DataType.DATE,
            _ => throw new Exception("Tipo não suportado: " + t)
        };
    }

    static string Unquote(string s) => s.Trim().Trim('\'', '"');

    static IEnumerable<string> SplitCsv(string input)
    {
        var list = new List<string>();
        var cur = new StringBuilder();
        bool inQ = false;
        char qchar = '\'';
        foreach (var ch in input)
        {
            if ((ch == '\'' || ch == '"'))
            {
                if (!inQ) { inQ = true; qchar = ch; continue; }
                if (qchar == ch) { inQ = false; continue; }
            }
            if (ch == ',' && !inQ) { list.Add(cur.ToString().Trim()); cur.Clear(); continue; }
            cur.Append(ch);
        }
        if (cur.Length > 0) list.Add(cur.ToString().Trim());
        return list;
    }

    static object ParseLiteral(string literal, DataType type)
    {
        if (literal.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return null;
        return type switch
        {
            DataType.INT => int.Parse(literal),
            DataType.TEXT => literal,
            DataType.BOOL => bool.Parse(literal),
            DataType.FLOAT => double.Parse(literal),
            DataType.DATE => DateTime.Parse(literal),
            _ => throw new Exception("ParseLiteral unsupported")
        };
    }

    // called by WAL replay
    public void ApplyInsert(string tableName, object[] row)
    {
        if (!tables.TryGetValue(tableName, out var t)) throw new Exception("Tabela não existe (replay): " + tableName);
        t.Insert(row, null); // do not append WAL again during replay
    }
}

#endregion

#region Table

class Table
{
    public TableSchema Schema { get; }
    readonly string dataPath;
    readonly string idxPath;
    FileStream dataStream;
    readonly ReaderWriterLockSlim rw = new();
    // primary index: int -> file offset
    SortedDictionary<int, long> primaryIndex = new();

    public Table(string dataPath, string idxPath, TableSchema schema)
    {
        Schema = schema;
        this.dataPath = dataPath;
        this.idxPath = idxPath;
        dataStream = new FileStream(dataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
    }

    public string Name => Schema.TableName;

    public void Insert(object[] row, Wal wal)
    {
        // compute primary key (int) from first column
        if (row.Length != Schema.Columns.Length) throw new Exception("Row column count mismatch");
        if (row[0] == null) throw new Exception("Primary key (first column) não pode ser NULL");
        int pk = Convert.ToInt32(row[0]);

        rw.EnterWriteLock();
        try
        {
            if (primaryIndex.ContainsKey(pk)) throw new Exception("Duplicate primary key: " + pk);

            // WAL
            if (wal != null)
                wal.Append(new WalRecord { Op = "INSERT", Table = Name, Row = SerializeRowForWal(row) });

            // append to data file
            dataStream.Seek(0, SeekOrigin.End);
            var offset = dataStream.Position;
            using (var bw = new BinaryWriter(dataStream, Encoding.UTF8, true))
            {
                // record layout: [recordLength:int32][columnCount:int16][for each column: nullFlag:byte + payload]
                var startPos = dataStream.Position;
                bw.Write((Int32)0); // placeholder for length
                bw.Write((Int16)Schema.Columns.Length);
                for (int i = 0; i < Schema.Columns.Length; i++)
                {
                    var val = row[i];
                    if (val == null) { bw.Write((byte)1); continue; } else bw.Write((byte)0);
                    var dt = Schema.Columns[i].Type;
                    switch (dt)
                    {
                        case DataType.INT: bw.Write((Int32)Convert.ToInt32(val)); break;
                        case DataType.FLOAT: bw.Write((Double)Convert.ToDouble(val)); break;
                        case DataType.BOOL: bw.Write((byte)((bool)val ? 1 : 0)); break;
                        case DataType.DATE: bw.Write(((DateTime)val).ToBinary()); break;
                        case DataType.TEXT:
                            var bs = Encoding.UTF8.GetBytes(Convert.ToString(val));
                            bw.Write((Int32)bs.Length);
                            bw.Write(bs);
                            break;
                    }
                }
                var endPos = dataStream.Position;
                var length = (Int32)(endPos - startPos - 4);
                dataStream.Position = startPos;
                bw.Write(length);
                dataStream.Position = endPos;
                bw.Flush();
            }
            // update index in memory and persist
            primaryIndex[pk] = offset;
            PersistIndex();
        }
        finally { rw.ExitWriteLock(); }
    }

    static string[] SerializeRowForWal(object[] row)
    {
        // convert to string array to store in WAL (simple)
        return row.Select(r =>
        {
            if (r == null) return "__NULL__";
            if (r is DateTime dt) return "__DATE__" + dt.ToBinary().ToString();
            return r.ToString();
        }).ToArray();
    }

    public object[] SelectByPrimaryKey(int pk)
    {
        rw.EnterReadLock();
        try
        {
            if (!primaryIndex.TryGetValue(pk, out var offset)) return null;
            return ReadRowAt(offset);
        }
        finally { rw.ExitReadLock(); }
    }

    public IEnumerable<object[]> SelectAll()
    {
        rw.EnterReadLock();
        try
        {
            var results = new List<object[]>();
            dataStream.Seek(0, SeekOrigin.Begin);
            using var br = new BinaryReader(dataStream, Encoding.UTF8, true);
            while (dataStream.Position < dataStream.Length)
            {
                try
                {
                    var length = br.ReadInt32();
                    var colCount = br.ReadInt16();
                    var row = new object[colCount];
                    for (int i = 0; i < colCount; i++)
                    {
                        var isNull = br.ReadByte();
                        if (isNull == 1) { row[i] = null; continue; }
                        var dt = Schema.Columns[i].Type;
                        switch (dt)
                        {
                            case DataType.INT: row[i] = br.ReadInt32(); break;
                            case DataType.FLOAT: row[i] = br.ReadDouble(); break;
                            case DataType.BOOL: row[i] = br.ReadByte() == 1; break;
                            case DataType.DATE: row[i] = DateTime.FromBinary(br.ReadInt64()); break;
                            case DataType.TEXT:
                                var len = br.ReadInt32();
                                var bs = br.ReadBytes(len);
                                row[i] = Encoding.UTF8.GetString(bs);
                                break;
                        }
                    }
                    results.Add(row);
                }
                catch { break; }
            }
            return results;
        }
        finally { rw.ExitReadLock(); }
    }

    public List<object[]> Select(string whereCol, string whereValRaw)
    {
        // naive scan (used when WHERE not on PK)
        var res = new List<object[]>();
        var colIdx = Array.FindIndex(Schema.Columns, c => c.Name.Equals(whereCol, StringComparison.OrdinalIgnoreCase));
        if (colIdx < 0) throw new Exception("Coluna não existe: " + whereCol);
        var parsed = ParseLiteralForColumn(whereValRaw, Schema.Columns[colIdx].Type);

        foreach (var r in SelectAll())
        {
            var v = r[colIdx];
            if (v != null && v.Equals(parsed)) res.Add(r);
        }
        return res;
    }

    object ParseLiteralForColumn(string raw, DataType dt)
    {
        if (raw.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return null;
        return dt switch
        {
            DataType.INT => int.Parse(raw),
            DataType.FLOAT => double.Parse(raw),
            DataType.BOOL => bool.Parse(raw),
            DataType.DATE => DateTime.Parse(raw),
            DataType.TEXT => raw,
            _ => raw
        };
    }

    object[] ReadRowAt(long offset)
    {
        dataStream.Seek(offset, SeekOrigin.Begin);
        using var br = new BinaryReader(dataStream, Encoding.UTF8, true);
        var length = br.ReadInt32();
        var colCount = br.ReadInt16();
        var row = new object[colCount];
        for (int i = 0; i < colCount; i++)
        {
            var isNull = br.ReadByte();
            if (isNull == 1) { row[i] = null; continue; }
            var dt = Schema.Columns[i].Type;
            switch (dt)
            {
                case DataType.INT: row[i] = br.ReadInt32(); break;
                case DataType.FLOAT: row[i] = br.ReadDouble(); break;
                case DataType.BOOL: row[i] = br.ReadByte() == 1; break;
                case DataType.DATE: row[i] = DateTime.FromBinary(br.ReadInt64()); break;
                case DataType.TEXT:
                    var len = br.ReadInt32();
                    var bs = br.ReadBytes(len);
                    row[i] = Encoding.UTF8.GetString(bs);
                    break;
            }
        }
        return row;
    }

    public void LoadIndex()
    {
        rw.EnterWriteLock();
        try
        {
            primaryIndex.Clear();
            if (!File.Exists(idxPath)) return;
            using var fs = new FileStream(idxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);
            while (fs.Position < fs.Length)
            {
                var key = br.ReadInt32();
                var off = br.ReadInt64();
                primaryIndex[key] = off;
            }
        }
        finally { rw.ExitWriteLock(); }
    }

    void PersistIndex()
    {
        using var fs = new FileStream(idxPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);
        foreach (var kv in primaryIndex)
        {
            bw.Write(kv.Key);
            bw.Write(kv.Value);
        }
        bw.Flush();
        fs.Flush(true);
    }

    public void Flush()
    {
        rw.EnterWriteLock();
        try
        {
            dataStream.Flush(true);
            PersistIndex();
        }
        finally { rw.ExitWriteLock(); }
    }
}

#endregion

#region WAL

class WalRecord
{
    public string Op { get; set; }
    public string Table { get; set; }
    public string[] Row { get; set; }
}

class Wal
{
    readonly string path;
    readonly object fileLock = new();
    StreamWriter writer;

    public Wal(string path)
    {
        this.path = path;
        writer = new StreamWriter(new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        writer.BaseStream.Seek(0, SeekOrigin.End);
    }

    public void Append(WalRecord r)
    {
        lock (fileLock)
        {
            var json = JsonSerializer.Serialize(r);
            writer.WriteLine(json);
            writer.Flush();
        }
    }

    public void Replay(StorageEngine engine)
    {
        if (!File.Exists(path)) return;
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var r = JsonSerializer.Deserialize<WalRecord>(line);
                if (r.Op == "INSERT")
                {
                    // reconstruct object[] from r.Row (string markers)
                    var schema = engine; // not used
                    // lookup table
                    var tbl = engine; // placeholder
                    // we need to reconstruct row types: fetch schema file to map types
                    var schemaPath = System.IO.Path.Combine(engine.WalPathDirectory(), r.Table + ".schema.json");
                    var ts = JsonSerializer.Deserialize<TableSchema>(File.ReadAllText(schemaPath));
                    var objRow = new object[ts.Columns.Length];
                    for (int i = 0; i < r.Row.Length; i++)
                    {
                        var s = r.Row[i];
                        if (s == "__NULL__") { objRow[i] = null; continue; }
                        if (s.StartsWith("__DATE__")) { objRow[i] = DateTime.FromBinary(long.Parse(s.Substring(8))); continue; }
                        var dt = ts.Columns[i].Type;
                        objRow[i] = dt switch
                        {
                            DataType.INT => int.Parse(s),
                            DataType.FLOAT => double.Parse(s),
                            DataType.BOOL => bool.Parse(s),
                            DataType.DATE => DateTime.Parse(s),
                            DataType.TEXT => s,
                            _ => s
                        };
                    }
                    engine.ApplyInsert(r.Table, objRow);
                }
            }
            catch
            {
                // ignore malformed
            }
        }
        // truncate wal after replay
        File.WriteAllText(path, "");
    }

    public void Flush()
    {
        lock (fileLock)
        {
            writer.Flush();
            (writer.BaseStream as FileStream)?.Flush(true);
        }
    }
}

static class WalHelpers
{
    // helper to find wal dir (hacky but fine for prototype)
    public static string WalPathDirectory(this StorageEngine e)
    {
        // find existential schema files in same folder as wal path
        var walPath = ((Wal)e.GetType().GetField("Wal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(e)).GetType()
            .GetField("path", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(((Wal)e.GetType().GetField("Wal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(e))).ToString();
        return Path.GetDirectoryName(walPath);
    }
}

    #endregion

    #region UserManager (same as before, simplified)

    class UserManager
    {
        readonly string filePath;
        readonly Dictionary<string, UserRecord> users = new();
        readonly object lockObj = new();

        public UserManager(string path) { filePath = path; }

        public void Load()
        {
            if (!File.Exists(filePath)) return;

            lock (lockObj)
            {
                using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite // ✅ allow other processes to write
                );

                using var reader = new StreamReader(stream, Encoding.UTF8);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(':');
                    if (parts.Length < 4) continue;

                    users[parts[0]] = new UserRecord
                    {
                        Username = parts[0],
                        Salt = Convert.FromBase64String(parts[1]),
                        Hash = Convert.FromBase64String(parts[2]),
                        Roles = parts[3].Split(',')
                    };
                }
            }
        }

        public void Save()
        {
            lock (lockObj)
            {
                var tmp = filePath + ".tmp";

                using (var stream = new FileStream(
                    tmp,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read // ✅ allow reads during save
                ))
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    foreach (var u in users.Values)
                    {
                        writer.WriteLine(
                            $"{u.Username}:{Convert.ToBase64String(u.Salt)}:{Convert.ToBase64String(u.Hash)}:{string.Join(",", u.Roles)}"
                        );
                    }
                }

                // ✅ atomic replace (safe even if crash happens)
                File.Replace(tmp, filePath, null);
            }
        }

        public bool UserExists(string username) => users.ContainsKey(username);

        public void CreateUser(string username, string password, IEnumerable<string> roles)
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var salt = new byte[16];
            rng.GetBytes(salt);
            var hash = HashPassword(password, salt);

            lock (lockObj)
            {
                users[username] = new UserRecord
                {
                    Username = username,
                    Salt = salt,
                    Hash = hash,
                    Roles = roles.ToArray()
                };
                Save();
            }
        }

        public bool Validate(string username, string password)
        {
            lock (lockObj)
            {
                if (!users.TryGetValue(username, out var u))
                    return false;

                var h = HashPassword(password, u.Salt);
                return h.SequenceEqual(u.Hash);
            }
        }

        static byte[] HashPassword(string password, byte[] salt)
        {
            using var derive = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password,
                salt,
                100_000,
                System.Security.Cryptography.HashAlgorithmName.SHA256
            );
            return derive.GetBytes(32);
        }

        public bool HasPermission(string username, string permission)
        {
            lock (lockObj)
            {
                if (!users.ContainsKey(username)) return false;
                var roles = users[username].Roles;

                if (roles.Contains("DBA")) return true;
                if (permission == "SELECT" && roles.Contains("READER")) return true;
                if (permission == "INSERT" && roles.Contains("WRITER")) return true;
                if (permission == "CREATE" && roles.Contains("CREATOR")) return true;
                return false;
            }
        }

        class UserRecord
        {
            public string Username;
            public byte[] Salt;
            public byte[] Hash;
            public string[] Roles;
        }
    }

    #endregion
}