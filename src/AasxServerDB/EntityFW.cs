using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.IdentityModel.Tokens;

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
        public static bool isPostgres { get; set; }

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
                throw new Exception("No Configuration!");

            var connectionString = _con["DatabaseConnection:ConnectionString"];
            if (connectionString.IsNullOrEmpty())
                throw new Exception("No connectionString in appsettings");

            if (connectionString.Contains("$DATAPATH"))
                connectionString = connectionString.Replace("$DATAPATH", _dataPath);

            if (connectionString.ToLower().Contains("host")) // PostgreSQL
            {
                isPostgres = true;
                options.UseNpgsql(connectionString);
            }
            else // SQLite
            {
                isPostgres = false;
                options.UseSqlite(connectionString);
            }
        }

        public async Task ClearDB()
        {
            // Queue up all delete operations asynchronously
            var tasks = new List<Task<int>>
            {
                AASXSets.ExecuteDeleteAsync(),
                AASSets.ExecuteDeleteAsync(),
                SMSets.ExecuteDeleteAsync(),
                SMESets.ExecuteDeleteAsync(),
                IValueSets.ExecuteDeleteAsync(),
                SValueSets.ExecuteDeleteAsync(),
                DValueSets.ExecuteDeleteAsync()
            };

            // Wait for all delete tasks to complete
            await Task.WhenAll(tasks);

            // Save changes to the database
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
                options.UseNpgsql(connectionString);
            }
        }
    }

    public class AASXSet
    {
        public int Id { get; set; }
        public string AASX { get; set; }

        public virtual ICollection<AASSet> AASSets { get; } = new List<AASSet>();
        public virtual ICollection<SMSet> SMSets { get; } = new List<SMSet>();
    }

    public class AASSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")]
        public int AASXId { get; set; }
        public virtual AASXSet AASXSet { get; set; }

        public string? Identifier { get; set; }
        public string? IdShort { get; set; }
        public string? AssetKind { get; set; }
        public string? GlobalAssetId { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }

        public virtual ICollection<SMSet> SMSets { get; } = new List<SMSet>();
    }

    public class SMSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")]
        public int AASXId { get; set; }
        public virtual AASXSet AASXSet { get; set; }

        [ForeignKey("AASSet")]
        public int? AASId { get; set; }
        public virtual AASSet? AASSet { get; set; }

        public string? SemanticId { get; set; }
        public string? Identifier { get; set; }
        public string? IdShort { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }

        public virtual ICollection<SMESet> SMESets { get; } = new List<SMESet>();
    }

    public class SMESet
    {
        public int Id { get; set; }

        [ForeignKey("SMSet")]
        public int SMId { get; set; }
        public virtual SMSet SMSet { get; set; }

        public int? ParentSMEId { get; set; }
        public virtual SMESet? ParentSME { get; set; }

        public string? SMEType { get; set; }
        public string? ValueType { get; set; }
        public string? SemanticId { get; set; }
        public string? IdShort { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }

        public virtual ICollection<IValueSet> IValueSets { get; } = new List<IValueSet>();
        public virtual ICollection<DValueSet> DValueSets { get; } = new List<DValueSet>();
        public virtual ICollection<SValueSet> SValueSets { get; } = new List<SValueSet>();

        public string getValue()
        {
            using (AasContext db = new AasContext())
            {
                switch (ValueType)
                {
                    case "S":
                        var ls = db.SValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
                        if (ls.Count != 0)
                            return ls.First().ToString();
                        break;
                    case "I":
                        var li = db.IValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
                        if (li.Count != 0)
                            return li.First().ToString();
                        break;
                    case "F":
                        var ld = db.DValueSets.Where(s => s.SMEId == Id).Select(s => s.Value).ToList();
                        if (ld.Count != 0)
                            return ld.First().ToString();
                        break;
                }
            }
            return string.Empty;
        }

        public List<string> getMLPValue()
        {
            var list = new List<String>();
            if (SMEType == "MLP")
            {
                using (AasContext db = new AasContext())
                {
                    var mlpValueSetList = db.SValueSets.Where(s => s.SMEId == Id).ToList();
                    foreach (var mlpValue in mlpValueSetList)
                    {
                        list.Add(mlpValue.Annotation);
                        list.Add(mlpValue.Value);
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

        public long? Value { get; set; }
        public string? Annotation { get; set; }

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

        public string? Value { get; set; }
        public string? Annotation { get; set; }
    }

    public class DValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public int SMEId { get; set; }
        public virtual SMESet SMESet { get; set; }

        public double? Value { get; set; }
        public string? Annotation { get; set; }

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