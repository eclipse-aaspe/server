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
    using System.Text.Json.Nodes;
    using AasxServerDB.Entities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using AasCore.Aas3_0;

    public class AasContext : DbContext
    {
        public static IConfiguration? _con       { get; set; }
        public static string?         _dataPath  { get; set; }
        public static bool            IsPostgres { get; set; }

        public DbSet<EnvSet>    EnvSets    { get; set; }
        public DbSet<CDSet>     CDSets     { get; set; }
        public DbSet<AASSet>    AASSets    { get; set; }
        public DbSet<SMSet>     SMSets     { get; set; }
        public DbSet<SMESet>    SMESets    { get; set; }
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
                EnvSets.ExecuteDeleteAsync(),
                CDSets.ExecuteDeleteAsync(),
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
            // CD
            modelBuilder.Entity<CDSet>()
                .Property(e => e.DisplayName)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ILangStringNameType>(text));
            modelBuilder.Entity<CDSet>()
                .Property(e => e.Description)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ILangStringTextType>(text));
            modelBuilder.Entity<CDSet>()
                .Property(e => e.Extensions)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IExtension>(text));
            modelBuilder.Entity<CDSet>()
                .Property(e => e.Administration)
                .HasConversion(
                    obj => SerializeElement(obj),
                    text => DeserializeElementFromString<IAdministrativeInformation>(text));
            modelBuilder.Entity<CDSet>()
                .Property(e => e.IsCaseOf)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IReference>(text));
            modelBuilder.Entity<CDSet>()
                .Property(e => e.EmbeddedDataSpecifications)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IEmbeddedDataSpecification>(text));

            // AASSet
            modelBuilder.Entity<AASSet>()
                .Property(e => e.DisplayName)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ILangStringNameType>(text));
            modelBuilder.Entity<AASSet>()
                .Property(e => e.Description)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ILangStringTextType>(text));
            modelBuilder.Entity<AASSet>()
                .Property(e => e.Extensions)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IExtension>(text));
            modelBuilder.Entity<AASSet>()
                .Property(e => e.Administration)
                .HasConversion(
                    obj => SerializeElement(obj),
                    text => DeserializeElementFromString<IAdministrativeInformation>(text));
            modelBuilder.Entity<AASSet>()
                .Property(e => e.EmbeddedDataSpecifications)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IEmbeddedDataSpecification>(text));
            modelBuilder.Entity<AASSet>()
                .Property(e => e.DerivedFrom)
                .HasConversion(
                    obj => SerializeElement(obj),
                    text => DeserializeElementFromString<IReference?>(text));
            modelBuilder.Entity<AASSet>()
                .Property(e => e.AssetKind)
                .HasConversion(
                    obj => SerializeElement(obj),
                    text => DeserializeElementFromString<AssetKind>(text));
            modelBuilder.Entity<AASSet>()
                .Property(e => e.SpecificAssetIds)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ISpecificAssetId>(text));
            modelBuilder.Entity<AASSet>()
                .Property(e => e.DefaultThumbnail)
                .HasConversion(
                    obj => SerializeElement(obj),
                    text => DeserializeElementFromString<IResource>(text));

            // SMSet
            modelBuilder.Entity<SMSet>()
                .Property(e => e.DisplayName)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ILangStringNameType>(text));
            modelBuilder.Entity<SMSet>()
                .Property(e => e.Description)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<ILangStringTextType>(text));
            modelBuilder.Entity<SMSet>()
                .Property(e => e.Extensions)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IExtension>(text));
            modelBuilder.Entity<SMSet>()
                .Property(e => e.Administration)
                .HasConversion(
                    obj => SerializeElement(obj),
                    text => DeserializeElementFromString<IAdministrativeInformation>(text));
            modelBuilder.Entity<SMSet>()
                .Property(e => e.Kind)
                .HasConversion(
                    obj => SerializeElement(obj),
                    text => DeserializeElementFromString<ModellingKind>(text));
            modelBuilder.Entity<SMSet>()
                .Property(e => e.SupplementalSemanticIds)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IReference>(text));
            modelBuilder.Entity<SMSet>()
                .Property(e => e.Qualifiers)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IQualifier>(text));
            modelBuilder.Entity<SMSet>()
                .Property(e => e.EmbeddedDataSpecifications)
                .HasConversion(
                    obj => SerializeList(obj),
                    text => DeserializeList<IEmbeddedDataSpecification>(text));

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
                .Property(e => e.EmbeddedDataSpecifications)
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

        public static string? SerializeElement<T>(T? element)
        {
            // check element
            if (element == null)
            {
                return null;
            }

            // convert to string
            if (typeof(T).IsAssignableFrom(typeof(ModellingKind)) ||
                typeof(T).IsAssignableFrom(typeof(AssetKind)))
            {
                return element.ToString();
            }
            else if (typeof(T).IsAssignableFrom(typeof(IAdministrativeInformation)) ||
                typeof(T).IsAssignableFrom(typeof(IResource)) ||
                typeof(T).IsAssignableFrom(typeof(IReference)))
            {
                var jsonNode = Jsonization.Serialize.ToJsonObject((IClass)element);
                return jsonNode?.ToJsonString(null);
            }
            else
            {
                throw new InvalidOperationException("Unsupported type: " + typeof(T).FullName);
            }
        }

        public static T? DeserializeElementFromString<T>(string? text)
        {
            // check string
            if (text.IsNullOrEmpty() || text is not string textS)
            {
                return default;
            }

            // add " to create JsonValue
            if (typeof(T).IsAssignableFrom(typeof(AssetKind)) ||
                typeof(T).IsAssignableFrom(typeof(ModellingKind)))
            {
                textS = $"\"{textS}\"";
            }

            // convert to JsonNode
            var jsonNode = JsonNode.Parse(textS);
            if (jsonNode == null)
            {
                throw new InvalidOperationException("Failed to parse JSON.");
            }
            return DeserializeElementFromJsonNode<T>(jsonNode);
        }

        public static T? DeserializeElementFromJsonNode<T>(JsonNode? jsonNode)
        {
            // default
            if (jsonNode == null)
            {
                return default;
            }

            // convert to datatype
            if (typeof(T).IsAssignableFrom(typeof(IAdministrativeInformation)))
            {
                return (T)(object)Jsonization.Deserialize.AdministrativeInformationFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ModellingKind)))
            {
                return (T)(object)Jsonization.Deserialize.ModellingKindFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(AssetKind)))
            {
                return (T)(object)Jsonization.Deserialize.AssetKindFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(IResource)))
            {
                return (T)(object)Jsonization.Deserialize.ResourceFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(IReference)))
            {
                return (T)(object)Jsonization.Deserialize.ReferenceFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(IReference)))
            {
                return (T)(object)Jsonization.Deserialize.ReferenceFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(IQualifier)))
            {
                return (T)(object)Jsonization.Deserialize.QualifierFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringNameType)))
            {
                return (T)(object)Jsonization.Deserialize.LangStringNameTypeFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ILangStringTextType)))
            {
                return (T)(object)Jsonization.Deserialize.LangStringTextTypeFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(IEmbeddedDataSpecification)))
            {
                return (T)(object)Jsonization.Deserialize.EmbeddedDataSpecificationFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(IExtension)))
            {
                return (T)(object)Jsonization.Deserialize.ExtensionFrom(jsonNode);
            }
            else if (typeof(T).IsAssignableFrom(typeof(ISpecificAssetId)))
            {
                return (T)(object)Jsonization.Deserialize.SpecificAssetIdFrom(jsonNode);
            }
            else
            {
                throw new InvalidOperationException("Unsupported type: " + typeof(T).FullName);
            }
        }

        public static string SerializeList<T>(List<T>? list)
        {
            // check list
            if (list == null)
            {
                return string.Empty;
            }

            // convert to JsonArray
            var jsonArray = new JsonArray();
            foreach (var element in list)
            {
                if (element == null)
                    continue;

                var ele = Jsonization.Serialize.ToJsonObject((IClass)element);
                jsonArray.Add(ele);
            }

            // convert to string
            var text = jsonArray.ToJsonString();
            return text;
        }

        public static List<T>? DeserializeList<T>(string? text)
        {
            // check string
            if (text.IsNullOrEmpty() || text is not string textS)
                return null;

            // convert to JsonArray
            var jsonArray = JsonNode.Parse(textS);
            if (jsonArray == null)
                throw new InvalidOperationException("Failed to parse JSON.");
            if (jsonArray is not JsonArray array)
                throw new InvalidOperationException("JSON is not an array.");

            // convert to list
            var list = new List<T>();
            foreach (var element in array)
            {
                if (element == null)
                    continue;

                var ele = DeserializeElementFromJsonNode<T>(element);

                if (ele == null)
                    continue;

                list.Add(ele);
            }
            return list;
        }
    }
}