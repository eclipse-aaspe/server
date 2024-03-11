using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Extensions;

/*
 * https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli
 * 
 * Initial Migration
 * Add-Migration InitialCreate -Context SqliteAasContext -OutputDir Migrations\Sqlite
 * Add-Migration InitialCreate -Context PostgreAasContext -OutputDir Migrations\Postgres
 * 
 * Change database
 * Add-Migration XXX -Context SqliteAasContext
 * Add-Migration XXX -Context PostgreAasContext
 * Update-Database -Context SqliteAasContext
 * Update-Database -Context PostgreAasContext
 */

namespace AasxServerDB
{
    public class AasContext : DbContext
    {
        public static IConfiguration _con { get; set; }
        public static string _dataPath { get; set; }
        public static bool _isPostgres { get; set; }

        // --------------- Database Schema ---------------
        public DbSet<DBConfigSet> DBConfigSets { get; set; }
        public DbSet<AASXSet> AASXSets { get; set; }
        public DbSet<AASSet> AASSets { get; set; }
        public DbSet<SMSet> SMSets { get; set; }
        public DbSet<SMESet> SMESets { get; set; }
        public DbSet<SValueSet> SValueSets { get; set; }
        public DbSet<IValueSet> IValueSets { get; set; }
        public DbSet<DValueSet> DValueSets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_con == null)
            {
                throw new Exception("No Configuration!");
            }
            string connectionString = _con["DatabaseConnection:ConnectionString"];
            if (connectionString != null)
            {
                if (connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", _dataPath);
                if (connectionString.ToLower().Contains("host")) // Postgres
                {
                    string[] Params = connectionString.Split(";");
                    string dbPassword = System.Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
                    string dbUser = System.Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
                    for (int i = 0; i < Params.Length; i++)
                    {
                        if (Params[i].Contains("Username") && dbUser != null)
                        {
                            Params[i] = "Username=" + dbUser;
                        }
                        if (Params[i].Contains("Password") && dbPassword != null)
                        {
                            Params[i] = "Password=" + dbPassword;
                        }
                    }
                    _isPostgres = true;
                    options.UseNpgsql(connectionString);
                }
                else // SQLite
                {
                    _isPostgres = false;
                    options.UseSqlite(connectionString);
                }
            }
            else
            {
                throw new Exception("No connectionString in appsettings");
            }
        }

        public void ClearDB()
        {
            int count = 0;
            var task = Task.Run(async () => count = await DBConfigSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await AASXSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await AASSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await SMSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await SMESets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await IValueSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await SValueSets.ExecuteDeleteAsync());
            task.Wait();
            task = Task.Run(async () => count = await DValueSets.ExecuteDeleteAsync());
            task.Wait();

            DBConfigSet dbConfig = new DBConfigSet
            {
                Id = 1,
                AASCount = 0,
                SMCount = 0,
                AASXCount = 0,
                SMECount = 0
            };
            Add(dbConfig);
            SaveChanges();
        }
    }

    public class SqliteAasContext : AasContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_con == null)
            {
                options.UseSqlite("");
            }
            else
            {
                var connectionString = _con["DatabaseConnection:ConnectionString"];
                if (connectionString != null && connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", _dataPath);
                options.UseSqlite(connectionString);
            }
        }
    }

    public class PostgreAasContext : AasContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_con == null)
            {
                options.UseNpgsql("");
            }
            else
            {
                var connectionString = _con["DatabaseConnection:ConnectionString"];
                if (connectionString != null && connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", _dataPath);
                options.UseNpgsql(connectionString);
            }
        }
    }



    // --------------- Database Schema ---------------
    public class DBConfigSet
    {
        public int Id { get; set; }
        public long AASXCount { get; set; }
        public long AASCount { get; set; }
        public long SMCount { get; set; }
        public long SMECount { get; set; }
    }

    [Index(nameof(AASXNum))]
    public class AASXSet
    {
        public int Id { get; set; }
        public long AASXNum { get; set; }
        public string AASX { get; set; }
    }

    [Index(nameof(AASNum))]
    public class AASSet
    {
        public int Id { get; set; }
        public long AASNum { get; set; }
        public long AASXNum { get; set; }
        public string AASId { get; set; }
        public string IdShort { get; set; }
        public string AssetKind { get; set; }
        public string GlobalAssetId { get; set; }
    }

    [Index(nameof(SMNum))]
    public class SMSet
    {
        public int Id { get; set; }
        public long SMNum { get; set; }
        public long AASXNum { get; set; }
        public long AASNum { get; set; }
        public string SemanticId { get; set; }
        public string SMId { get; set; }
        public string IdShort { get; set; }
    }

    [Index(nameof(SMNum))]
    [Index(nameof(SMENum))]
    public class SMESet
    {
        public int Id { get; set; }
        public long SMENum { get; set; }
        public long SMNum { get; set; }
        public long ParentSMENum { get; set; }
        public string SMEType { get; set; }
        public string ValueType { get; set; }
        public string SemanticId { get; set; }
        public string IdShort { get; set; }

        public string getValue()
        {
            using (AasContext db = new AasContext())
            {
                switch (ValueType)
                {
                    case "S":
                        var ls = db.SValueSets.Where(s => s.ParentSMENum == SMENum).Select(s => s.Value).ToList();
                        if (ls.Count != 0)
                            return ls.First() + "";
                        break;
                    case "I":
                        var li = db.IValueSets.Where(s => s.ParentSMENum == SMENum).Select(s => s.Value).ToList();
                        if (li.Count != 0)
                            return li.First() + "";
                        break;
                    case "F":
                        var ld = db.DValueSets.Where(s => s.ParentSMENum == SMENum).Select(s => s.Value).ToList();
                        if (ld.Count != 0)
                            return ld.First() + "";
                        break;
                }
            }
            return "";
        }

        public List<string> getMLPValue()
        {
            var list = new List<String>();
            if (SMEType == "MLP")
            {
                using (AasContext db = new AasContext())
                {
                    var MLPValueSetList = db.SValueSets
                        .Where(s => s.ParentSMENum == SMENum)
                        .ToList();
                    foreach (var MLPValue in MLPValueSetList)
                    {
                        list.Add(MLPValue.Annotation);
                        list.Add(MLPValue.Value);
                    }
                    return list;
                }
            }
            return new List<string>();
        }

        public static List<SValueSet> getValueList(List<SMESet> smesets)
        {
            var smeNums = smesets.OrderBy(s => s.SMENum).Select(s => s.SMENum).ToList();
            long first = smeNums.First();
            long last = smeNums.Last();
            List<SValueSet> valueList = null;
            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                valueList = db.SValueSets.FromSqlRaw("SELECT * FROM SValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last + " UNION SELECT * FROM IValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last + " UNION SELECT * FROM DValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last)
                    .Where(v => smeNums.Contains(v.ParentSMENum))
                    .OrderBy(v => v.ParentSMENum)
                    .ToList();
                watch.Stop();
                Console.WriteLine("Getting the value list took this time: " + watch.ElapsedMilliseconds);
            }
            return valueList;
        }
    }

    [Index(nameof(ParentSMENum))]
    public class IValueSet
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public long Value { get; set; }
        public string Annotation { get; set; }
        public SValueSet asStringValue()
        {
            return new SValueSet
            {
                Id = Id,
                ParentSMENum = ParentSMENum,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }

    [Index(nameof(ParentSMENum))]
    public class SValueSet
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public string Value { get; set; }
        public string Annotation { get; set; }
    }

    [Index(nameof(ParentSMENum))]
    public class DValueSet
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public double Value { get; set; }
        public string Annotation { get; set; }
        public SValueSet asStringValue()
        {
            return new SValueSet
            {
                Id = Id,
                ParentSMENum = ParentSMENum,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }
}