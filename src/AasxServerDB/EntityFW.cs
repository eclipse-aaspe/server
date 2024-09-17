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
using AasCore.Aas3_0;
using System.Xml.Linq;
using AasxServerDB.Result;

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
            // SMESet
            modelBuilder.Entity<SMESet>()
                .Property(e => e.DisplayName)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ILangStringNameType>(text));
            modelBuilder.Entity<SMESet>()
                .Property(e => e.Description)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ILangStringTextType>(text));
            modelBuilder.Entity<SMESet>()
                .Property(e => e.Extensions)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IExtension>(text));
            modelBuilder.Entity<SMESet>()
                .Property(e => e.SupplementalSemanticIds)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IReference>(text));
            modelBuilder.Entity<SMESet>()
                .Property(e => e.Qualifiers)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IQualifier>(text));
            modelBuilder.Entity<SMESet>()
                .Property(e => e.DataSpecifications)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IEmbeddedDataSpecification>(text));

            // OValueSet
            modelBuilder.Entity<OValueSet>()
                .Property(e => e.Value)
                .HasConversion(
                    v => v.ToJsonString(null),
                    v => JsonNode.Parse(v, null, default));
        }

        public static string SerializeLangText<T>(List<T>? list)
        {
            // convert to formatted text
            var result = string.Empty;
            if (list == null)
            {
                return result;
            }
            foreach (var element in list)
            {
                if (element == null)
                {
                    continue;
                }

                if (element is ILangStringNameType langName)
                {
                    result += $"[ {langName.Language} ] {langName.Text}\n";
                }
                else if (element is ILangStringTextType langText)
                {
                    result += $"[ {langText.Language} ] {langText.Text}\n";
                }
            }
            return result;
        }

        public static string? SerializeList<T>(List<T>? list)
        {
            // check List
            if (list == null)
            {
                return null;
            }

            // convert to JsonArray
            var jsonArray = new JsonArray();
            foreach (var element in list)
            {
                if (element != null)
                {
                    jsonArray.Add(Jsonization.Serialize.ToJsonObject((IClass)element));
                }
            }

            // convert to String
            var text = jsonArray.ToJsonString(null);
            return text;
        }

        public static List<T>? DeserializeList<T>(string? text)
        {
            // check String
            if (text.IsNullOrEmpty() || text is not string textS)
            {
                return null;
            }

            // convert to JsonArray
            var jsonArray = JsonNode.Parse(textS);
            if (jsonArray == null)
            {
                throw new InvalidOperationException("Failed to parse JSON.");
            }
            if (jsonArray is not JsonArray array)
            {
                throw new InvalidOperationException("JSON is not an array.");
            }

            // convert to List
            var list = new List<T>();
            foreach (var element in array)
            {
                if (element != null)
                {
                    if (typeof(T).IsAssignableFrom(typeof(IReference)))
                    {
                        list.Add((T)(object)Jsonization.Deserialize.ReferenceFrom(element));
                    }
                    else if (typeof(T).IsAssignableFrom(typeof(IQualifier)))
                    {
                        list.Add((T)(object)Jsonization.Deserialize.QualifierFrom(element));
                    }
                    else if (typeof(T).IsAssignableFrom(typeof(ILangStringNameType)))
                    {
                        list.Add((T)(object)Jsonization.Deserialize.LangStringNameTypeFrom(element));
                    }
                    else if (typeof(T).IsAssignableFrom(typeof(ILangStringTextType)))
                    {
                        list.Add((T)(object)Jsonization.Deserialize.LangStringTextTypeFrom(element));
                    }
                    else if (typeof(T).IsAssignableFrom(typeof(IEmbeddedDataSpecification)))
                    {
                        list.Add((T)(object)Jsonization.Deserialize.EmbeddedDataSpecificationFrom(element));
                    }
                    else if (typeof(T).IsAssignableFrom(typeof(IExtension)))
                    {
                        list.Add((T)(object)Jsonization.Deserialize.ExtensionFrom(element));
                    }
                    else
                    {
                        throw new InvalidOperationException("Type not known.");
                    }
                }
            }
            return list;
        }
    }
}