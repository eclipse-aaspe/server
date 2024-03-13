using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

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
                    string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
                    string dbUser = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
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
            var task = Task.Run(async () => count = await AASXSets.ExecuteDeleteAsync());
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
    public class AASXSet
    {
        public int Id { get; set; }
        public string AASX { get; set; }

        public virtual ICollection<AASSet> AASSets { get; set; }
        public virtual ICollection<SMSet> SMSets { get; set; }
    }

    public class AASSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")]
        public int AASXId { get; set; }
        public virtual AASXSet AASXSet { get; set; }

        public string IdIdentifier { get; set; }
        public string IdShort { get; set; }
        public string AssetKind { get; set; }
        public string GlobalAssetId { get; set; }

        public virtual ICollection<SMSet> SMSets { get; set; }
    }

    public class SMSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")]
        public int AASXId { get; set; }
        public virtual AASXSet AASXSet { get; set; }


        [ForeignKey("AASSet")]
        public int AASId { get; set; }
        public virtual AASSet AASSet { get; set; }

        public string SemanticId { get; set; }
        public string IdIdentifier { get; set; }
        public string IdShort { get; set; }

        public virtual ICollection<SMESet> SMESets { get; set; }
    }

    public class SMESet
    {
        public int Id { get; set; }

        [ForeignKey("SMSet")]
        public int SMId { get; set; }
        public virtual SMSet SMSet { get; set; }

        [ForeignKey("ParentSMEId")]
        public int? ParentSMEId { get; set; }
        public virtual SMESet? ParentSMESet { get; set; }

        public string SMEType { get; set; }
        public string ValueType { get; set; }
        public string SemanticId { get; set; }
        public string IdShort { get; set; }

        public virtual ICollection<IValueSet> IValueSets { get; set; }
        public virtual ICollection<DValueSet> DValueSets { get; set; }
        public virtual ICollection<SValueSet> SValueSets { get; set; }

        public string getValue()
        {
            using (AasContext db = new AasContext())
            {
                switch (ValueType)
                {
                    case "S":
                        var ls = db.SValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
                        if (ls.Count != 0)
                            return ls.First() + "";
                        break;
                    case "I":
                        var li = db.IValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
                        if (li.Count != 0)
                            return li.First() + "";
                        break;
                    case "F":
                        var ld = db.DValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
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
                        .Where(s => s.SMEId == Id)
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
            var smeIds = smesets.OrderBy(s => s.Id).Select(s => s.Id).ToList();
            long first = smeIds.First();
            long last = smeIds.Last();
            List<SValueSet> valueList = null;
            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                valueList = db.SValueSets.FromSqlRaw("SELECT * FROM SValueSets WHERE ParentSMEId >= " + first + " AND ParentSMEId <=" + last + " UNION SELECT * FROM IValueSets WHERE ParentSMEId >= " + first + " AND ParentSMEId <=" + last + " UNION SELECT * FROM DValueSets WHERE ParentSMEId >= " + first + " AND ParentSMEId <=" + last)
                    .Where(v => smeIds.Contains(v.SMEId))
                    .OrderBy(v => v.SMEId)
                    .ToList();
                watch.Stop();
                Console.WriteLine("Getting the value list took this time: " + watch.ElapsedMilliseconds);
            }
            return valueList;
        }
    }

    public class IValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public int SMEId { get; set; }
        public virtual SMESet SMESet { get; set; }

        public long Value { get; set; }
        public string Annotation { get; set; }
        public SValueSet asStringValue()
        {
            return new SValueSet
            {
                Id = Id,
                SMEId = SMEId,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }

    public class SValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public int SMEId { get; set; }
        public virtual SMESet SMESet { get; set; }

        public string Value { get; set; }
        public string Annotation { get; set; }
    }

    public class DValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public int SMEId { get; set; }
        public virtual SMESet SMESet { get; set; }

        public double Value { get; set; }
        public string Annotation { get; set; }

        public SValueSet asStringValue()
        {
            return new SValueSet
            {
                Id = Id,
                SMEId = SMEId,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }
}