using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli
// Change database:
// Add-Migration XXXX
// Update-Database

namespace AasxServer
{
    public class AasContext : DbContext
    {
        public DbSet<AasSet> AasSets { get; set; }
        public DbSet<SubmodelSet> SubmodelSets { get; set; }
        public string DbPath { get; }

        public AasContext()
        {
            DbPath = "./database.db";
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }

    public class AasSet
    {
        public int Id { get; set; }
        public string AasId { get; set; }
        public string AssetId { get; set; }
        public string Aasx { get; set; }
        public string Idshort { get; set; }
        public List<SubmodelSet> Submodels { get; } = new();
    }
    public class SubmodelSet
    {
        public int Id { get; set; }
        public string SubmodelId { get; set; }
        public string SemanticId { get; set; }
        public string Aasx { get; set; }
        public string Idshort { get; set; }
        public string AasId { get; set; }
    }
}
