using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using AasxServerDB;
using AasxServerDB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

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
        public static IConfiguration? _con       { get; set; }
        public static string?         _dataPath  { get; set; }
        public static bool            IsPostgres { get; set; }

        public DbSet<AASXSet> AASXSets { get; set; }
        public DbSet<AASSet> AASSets { get; set; }
        public DbSet<SMSet> SMSets { get; set; }
        public DbSet<SMESet> SMESets { get; set; }
        public DbSet<SValueSet> SValueSets { get; set; }
        public DbSet<IValueSet> IValueSets { get; set; }
        public DbSet<DValueSet> DValueSets { get; set; }
        public DbSet<OValueSet> OValueSets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_con == null)
                throw new Exception("No Configuration!");

            var connectionString = _con["DatabaseConnection:ConnectionString"];
            if (connectionString.IsNullOrEmpty())
                throw new Exception("No connectionString in appsettings");

            if (connectionString != null && connectionString.Contains("$DATAPATH"))
                connectionString = connectionString.Replace("$DATAPATH", _dataPath);

            if (connectionString != null && connectionString.ToLower().Contains("host")) // PostgreSQL
            {
                IsPostgres = true;
                options.UseNpgsql(connectionString);
            }
            else // SQLite
            {
                IsPostgres = false;
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
                DValueSets.ExecuteDeleteAsync(),
                OValueSets.ExecuteDeleteAsync()
            };

            // Wait for all delete tasks to complete
            await Task.WhenAll(tasks);

            // Save changes to the database
            SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OValueSet>()
                .Property(e => e.Value)
                .HasConversion(
                    v => v.ToJsonString(null),
                    v => JsonNode.Parse(v, null, default));

            modelBuilder.Entity<SValueSet>()
                .HasIndex(d => d.Value);

            modelBuilder.Entity<IValueSet>()
                .HasIndex(d => d.Value);

            modelBuilder.Entity<DValueSet>()
                .HasIndex(d => d.Value);

            modelBuilder.Entity<SMESet>()
                .HasIndex(d => d.ParentSMEId);
        }
    }
}