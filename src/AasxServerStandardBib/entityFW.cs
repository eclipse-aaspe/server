using AasxRestServerLibrary;
using AdminShellNS;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using SpookilySharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
// using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using static AasCore.Aas3_0.Visitation;

// https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli

// Initial Migration
// Add-Migration InitialCreate -Context SqliteAasContext -OutputDir Migrations\Sqlite
// Add-Migration InitialCreate -Context PostgreAasContext -OutputDir Migrations\Postgres

// Change database:
// Add-Migration XXX -Context SqliteAasContext
// Add-Migration XXX -Context PostgreAasContext
// Update-Database -Context SqliteAasContext
// Update-Database -Context PostgreAasContext

namespace AasxServer
{
    public class AasContext : DbContext
    {
        public DbSet<DbConfigSet> DbConfigSets { get; set; }
        public DbSet<AASXSet> AASXSets { get; set; }
        public DbSet<AasSet> AasSets { get; set; }
        public DbSet<SubmodelSet> SubmodelSets { get; set; }
        public DbSet<SMESet> SMESets { get; set; }
        public DbSet<StringValue> SValueSets { get; set; }
        public DbSet<IntValue> IValueSets { get; set; }
        public DbSet<DoubleValue> DValueSets { get; set; }
        public string DbPath { get; }
        public static IConfiguration _con { get; set; }

        public AasContext()
        {
            DbPath = AasxHttpContextHelper.DataPath + "/database.db";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string connectionString = "";
            if (_con == null)
            {
                throw new Exception("No Configuration!");
            }
            connectionString = _con["DatabaseConnection:ConnectionString"];
            if (connectionString != null)
            {
                if (connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", AasxHttpContextHelper.DataPath);
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
                    // Console.WriteLine("Use POSTGRES: " + connectionString);
                    Program.isPostgres = true;
                    options.UseNpgsql(connectionString);
                }
                else // SQLite
                {
                    // Console.WriteLine("Use SQLITE: " + connectionString);
                    Program.isPostgres = false;
                    options.UseSqlite(connectionString);
                }
            }
            else
            {
                throw new Exception("No connectionString in appsettings");
            }

            /*
            string f = AasxHttpContextHelper.DataPath + "/CONNECTION.DAT";
            string connection = "";
            if (System.IO.File.Exists(f))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(f))
                    {
                        connection = sr.ReadLine();
                        Console.WriteLine(AasxHttpContextHelper.DataPath + "/CONNECTION.DAT" + " : " + connection);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(f + " not found!");
                }
            }

            if (connection == "")
            {
                if (System.IO.File.Exists(AasxHttpContextHelper.DataPath + "/POSTGRES.DAT"))
                {
                    connection = "Host=aasx-server-postgres; Database=AAS; Username=postgres; Password=postres; Include Error Detail=true; Port=5432";
                }
                else
                {
                    connection = "Data Source=" + DbPath;
                }
            }

            if (connection != "")
            {
                if (connection.ToLower().Contains("host")) // postgres
                {
                    Console.WriteLine("Use POSTGRES: " + connection);
                    Program.isPostgres = true;
                    options.UseNpgsql(connection);
                }
                else // SQLite
                {
                    Console.WriteLine("Use SQLITE: " + connection);
                    Program.isPostgres = false;
                    options.UseSqlite(connection);
                }
            }
            */
        }

        /*
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql("Host=localhost; Database=AAS; Username=postgres; Password=postres; Include Error Detail=true; Port=5432");
            // => options.UseNpgsql("Host=aasx-server-postgres; Database=AAS; Username=postgres; Password=postres; Include Error Detail=true; Port=5432");
        */
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
                if (connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", AasxHttpContextHelper.DataPath);
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
                if (connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", AasxHttpContextHelper.DataPath);
                options.UseNpgsql(connectionString);
            }
        }
    }
    public class DbConfigSet
    {
        public int Id { get; set; }
        public long AasCount { get; set; }
        public long SubmodelCount { get; set; }
        public long AASXCount { get; set; }
        public long SMECount { get; set; }
    }

    [Index(nameof(AASXNum))]
    public class AASXSet
    {
        public int Id { get; set; }
        public long AASXNum { get; set; }
        public string AASX { get; set; }
    }

    [Index(nameof(AasNum))]
    public class AasSet
    {
        public int Id { get; set; }
        public long AASXNum { get; set; }
        public long AasNum { get; set; }
        public string AasId { get; set; }
        public string Idshort { get; set; }
        public string AssetId { get; set; }
        public string AssetKind { get; set; }
    }

    [Index(nameof(SubmodelNum))]
    public class SubmodelSet
    {
        public int Id { get; set; }
        public long AASXNum { get; set; }
        public long AasNum { get; set; }
        public long SubmodelNum { get; set; }
        public string SubmodelId { get; set; }
        public string Idshort { get; set; }
        public string SemanticId { get; set; }
    }

    [Index(nameof(SubmodelNum))]
    [Index(nameof(SMENum))]
    public class SMESet
    {
        public int Id { get; set; }
        public long SubmodelNum { get; set; }
        public long ParentSMENum { get; set; }
        public long SMENum { get; set; }
        public string SMEType { get; set; }
        public string Idshort { get; set; }
        public string SemanticId { get; set; }
        public string ValueType { get; set; }

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

        public static List<StringValue> getValueList(List<SMESet> smesets)
        {
            var smeNums = smesets.OrderBy(s => s.SMENum).Select(s => s.SMENum).ToList();
            long first = smeNums.First();
            long last = smeNums.Last();
            List<StringValue> valueList = null;
            using (AasContext db = new AasContext())
            {
                /* Syntax korrekt, kann aber nicht übersetzt werden
                valueList = db.SValueSets
                    .Union(db.IValueSets.Select(v => v.asStringValue()))
                    .Union(db.DValueSets.Select(v => v.asStringValue()))
                    .Where(v => smeNums.Contains(v.ParentSMENum))
                    .OrderBy(v => v.ParentSMENum)
                    .ToList();
                */

                // SValue, IValue, DValue Tabellen zusammenführen
                var watch2 = System.Diagnostics.Stopwatch.StartNew();
                valueList = db.SValueSets.FromSqlRaw("SELECT * FROM SValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last + " UNION SELECT * FROM IValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last + " UNION SELECT * FROM DValueSets WHERE ParentSMENum >= " + first + " AND ParentSMENum <=" + last)
                    .Where(v => smeNums.Contains(v.ParentSMENum))
                    .OrderBy(v => v.ParentSMENum)
                    .ToList();
                watch2.Stop();
                Console.WriteLine("It took this time: " + watch2.ElapsedMilliseconds);
            }
            return valueList;
        }
    }

    [Index(nameof(ParentSMENum))]
    public class IntValue
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public long Value { get; set; }
        public string Annotation { get; set; }
        public StringValue asStringValue()
        {
            return new StringValue
            {
                Id = Id,
                ParentSMENum = ParentSMENum,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }

    [Index(nameof(ParentSMENum))]
    public class StringValue
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public string Value { get; set; }
        public string Annotation { get; set; }
    }

    [Index(nameof(ParentSMENum))]
    public class DoubleValue
    {
        public int Id { get; set; }
        public long ParentSMENum { get; set; }
        public double Value { get; set; }
        public string Annotation { get; set; }
        public StringValue asStringValue()
        {
            return new StringValue
            {
                Id = Id,
                ParentSMENum = ParentSMENum,
                Annotation = Annotation,
                Value = Value.ToString()
            };
        }
    }

    public class SubmodelResult
    {
        public string submodelId { get; set; }
        public string url { get; set; }
    }
    public class SmeResult
    {
        public string submodelId { get; set; }
        public string idShortPath { get; set; }
        public string value { get; set; }
        public string url { get; set; }
    }
    public class Query
    {
        public List<SubmodelResult> SearchSubmodels(string semanticId)
        {
            List<SubmodelResult> list = new List<SubmodelResult>();

            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine("SearchSubmodels");
                Console.WriteLine("Submodels " + db.SubmodelSets.Count());

                var subList = db.SubmodelSets.Where(s => s.SemanticId == semanticId).ToList();
                Console.WriteLine("Found " + subList.Count() + " Submodels in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                foreach (var submodel in subList)
                {
                    var sr = new SubmodelResult();
                    sr.submodelId = submodel.SubmodelId;
                    string sub64 = Base64UrlEncoder.Encode(sr.submodelId);
                    sr.url = Program.externalBlazor + "/submodels/" + sub64;
                    list.Add(sr);
                }
                Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");
            }

            return list;
        }

        bool isLowerUpper(string valueType, string value, string lower, string upper)
        {
            if (valueType == "F") // double
            {
                try
                {
                    string legal = "012345679.";

                    foreach (var c in lower + upper)
                    {
                        if (Char.IsDigit(c))
                            continue;
                        if (c == '.')
                            continue;
                        if (!legal.Contains(c))
                            return false;
                    }
                    var decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    // Console.WriteLine("seperator = " + decSep);
                    lower = lower.Replace(".", decSep);
                    lower = lower.Replace(",", decSep);
                    upper = upper.Replace(".", decSep);
                    upper = upper.Replace(",", decSep);
                    value = value.Replace(".", decSep);
                    value = value.Replace(",", decSep);
                    double l = Convert.ToDouble(lower);
                    double u = Convert.ToDouble(upper);
                    double v = Convert.ToDouble(value);
                    return (l < v && v < u);
                }
                catch
                {
                    return false;
                }
            }

            if (valueType == "I")
            {
                if (value.Length > 10)
                    return (false);
                if (!lower.All(char.IsDigit))
                    return false;
                if (!upper.All(char.IsDigit))
                    return false;
                try
                {
                    int l = Convert.ToInt32(lower);
                    int u = Convert.ToInt32(upper);
                    int v = Convert.ToInt32(value);
                    return (l < v && v < u);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public List<SmeResult> SearchSMEs(
            string submodelSemanticId = "", string semanticId = "",
            string equal = "", string lower = "", string upper = "", string contains = "")
        {
            List<SmeResult> result = new List<SmeResult>();

            bool withI = false;
            long iEqual = 0;
            long iLower = 0;
            long iUpper = 0;
            bool withF = false;
            double fEqual = 0;
            double fLower = 0;
            double fUpper = 0;
            try
            {
                if (equal != "")
                {
                    iEqual = Convert.ToInt64(equal);
                    withI = true;
                    fEqual = Convert.ToDouble(equal);
                    withF= true;
                }
                else if (lower != "" && upper != "")
                {
                    iLower = Convert.ToInt64(lower);
                    iUpper = Convert.ToInt64(upper);
                    withI = true;
                    fLower = Convert.ToDouble(lower);
                    fUpper = Convert.ToDouble(upper);
                    withF = true;
                }
            }
            catch { }

            if (semanticId == "" && equal == "" && lower == "" && upper == "" && contains == "")
                return result;

            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine("SearchSMEs");
                Console.WriteLine("Total number of SMEs " + db.SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                bool withContains = (contains != "");
                bool withEqual = !withContains && (equal != "");
                bool withCompare = !withContains && !withEqual && (lower != "" && upper != "");

                var list = db.SValueSets.Where(v =>
                    (withContains && v.Value.Contains(contains)) ||
                    (withEqual && v.Value == equal)
                    )
                    .Join(db.SMESets,
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            Value = v.Value.ToString(),
                            ParentSMENum = sme.ParentSMENum
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .ToList();

                list.AddRange(db.IValueSets.Where(v =>
                    (withEqual && withI && v.Value == iEqual) ||
                    (withCompare && withI && v.Value >= iLower && v.Value <= iUpper)
                    )
                    .Join(db.SMESets,
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            Value = v.Value.ToString(),
                            ParentSMENum = sme.ParentSMENum
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .ToList());

                list.AddRange(db.DValueSets.Where(v =>
                    (withEqual && withF && v.Value == fEqual) ||
                    (withCompare && withF && v.Value >= fLower && v.Value <= fUpper)
                    )
                    .Join(db.SMESets,
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            Value = v.Value.ToString(),
                            ParentSMENum = sme.ParentSMENum
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .ToList());

                Console.WriteLine("Found " + list.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                foreach (var l in list)
                {
                    SmeResult r = new SmeResult();

                    var submodelDB = db.SubmodelSets.Where(s => s.SubmodelNum == l.SubmodelNum).First();
                    if (submodelDB != null && (submodelSemanticId == "" || submodelDB.SemanticId == submodelSemanticId))
                    {
                        r.submodelId = submodelDB.SubmodelId;
                        r.value = l.Value;
                        string path = l.Idshort;
                        long pnum = l.ParentSMENum;
                        while (pnum != 0)
                        {
                            var smeDB = db.SMESets.Where(s => s.SMENum == pnum).First();
                            path = smeDB.Idshort + "." + path;
                            pnum = smeDB.ParentSMENum;
                        }
                        r.idShortPath = path;
                        string sub64 = Base64UrlEncoder.Encode(r.submodelId);
                        r.url = Program.externalBlazor + "/submodels/" + sub64 + "/submodel-elements/" + path;
                        result.Add(r);
                    }
                }
                Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");
            }

            return result;
        }

        public int CountSMEs(
            string submodelSemanticId = "", string semanticId = "",
            string equal = "", string lower = "", string upper = "", string contains = "")

        {
            bool withI = false;
            long iEqual = 0;
            long iLower = 0;
            long iUpper = 0;
            bool withF = false;
            double fEqual = 0;
            double fLower = 0;
            double fUpper = 0;
            try
            {
                if (equal != "")
                {
                    iEqual = Convert.ToInt64(equal);
                    withI = true;
                    fEqual = Convert.ToDouble(equal);
                    withF = true;
                }
                else if (lower != "" && upper != "")
                {
                    iLower = Convert.ToInt64(lower);
                    iUpper = Convert.ToInt64(upper);
                    withI = true;
                    fLower = Convert.ToDouble(lower);
                    fUpper = Convert.ToDouble(upper);
                    withF = true;
                }
            }
            catch { }

            if (semanticId == "" && equal == "" && lower == "" && upper == "" && contains == "")
                return 0;

            int c = 0;

            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine("CountSMEs");
                Console.WriteLine("Total number of SMEs " + db.SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                bool withContains = (contains != "");
                bool withEqual = !withContains && (equal != "");
                bool withCompare = !withContains && !withEqual && (lower != "" && upper != "");

                c = db.SValueSets.Where(v =>
                    (withContains && v.Value.Contains(contains)) ||
                    (withEqual && v.Value == equal) 
                    )
                    .Join(db.SMESets,
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            Value = v.Value.ToString(),
                            ParentSMENum = sme.ParentSMENum
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .Count();

                c += db.IValueSets.Where(v =>
                    (withEqual && withI && v.Value == iEqual) ||
                    (withCompare && withI && v.Value >= iLower && v.Value <= iUpper)
                    )
                    .Join(db.SMESets,
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            Value = v.Value.ToString(),
                            ParentSMENum = sme.ParentSMENum
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .Count();

                 c += db.DValueSets.Where(v =>
                    (withEqual && withF && v.Value == fEqual) ||
                    (withCompare && withF && v.Value >= fLower && v.Value <= fUpper)
                    )
                    .Join(db.SMESets,
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            Value = v.Value.ToString(),
                            ParentSMENum = sme.ParentSMENum
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .Count();

                Console.WriteLine("Count " + c + " SMEs in " + watch.ElapsedMilliseconds + "ms");
            }

            return c;
        }
    }

    public class VisitorAASX : VisitorThrough
    {
        AasContext _db = null;
        DbConfigSet _dbConfig = null;
        long _smNum = 0;
        List<long> _parentNum = null;
        public VisitorAASX(AasContext db, DbConfigSet dbConfigSet, long smNum)
        {
            _db = db;
            _dbConfig = dbConfigSet;
            _smNum = smNum;
            _parentNum = new List<long>();
        }

        public static void LoadAASInDB(AasContext db, IAssetAdministrationShell aas, long aasxNum, AdminShellPackageEnv asp)
        {
            var dbConfig = db.DbConfigSets.FirstOrDefault();
            LoadAASInDB(db, aas, aasxNum, asp, dbConfig);
        }

        public static void LoadAASInDB(AasContext db, IAssetAdministrationShell aas, long aasxNum, AdminShellPackageEnv asp, DbConfigSet dbConfig)
        {

            long aasNum = ++dbConfig.AasCount;
            var aasDB = new AasSet
            {
                AasNum = aasNum,
                AasId = aas.Id,
                AssetId = aas.AssetInformation.GlobalAssetId,
                AASXNum = aasxNum,
                Idshort = aas.IdShort,
                AssetKind = aas.AssetInformation.AssetKind.ToString()
            };
            db.Add(aasDB);

            // Iterate submodels
            if (aas.Submodels != null && aas.Submodels.Count > 0)
            {
                foreach (var smr in aas.Submodels)
                {
                    var sm = asp.AasEnv.FindSubmodel(smr);
                    if (sm != null)
                    {
                        var semanticId = sm.SemanticId.GetAsIdentifier();
                        if (semanticId == null)
                            semanticId = "";

                        long submodelNum = ++dbConfig.SubmodelCount;

                        var submodelDB = new SubmodelSet
                        {
                            SubmodelNum = submodelNum,
                            SubmodelId = sm.Id,
                            SemanticId = semanticId,
                            AASXNum = aasxNum,
                            AasNum = aasNum,
                            Idshort = sm.IdShort
                        };
                        db.Add(submodelDB);

                        VisitorAASX v = new VisitorAASX(db, dbConfig, submodelNum);
                        v.Visit(sm);
                    }
                }
            }
        }

        private string shortType(ISubmodelElement sme)
        {
            if (sme is Property)
                return ("P");
            if (sme is RelationshipElement)
                return "RE";
            if (sme is SubmodelElementList)
                return "SEL";
            if (sme is SubmodelElementCollection)
                return "SEC";
            if (sme is MultiLanguageProperty)
                return "MLP";
            if (sme is ReferenceElement)
                return ("RE");
            if (sme is AasCore.Aas3_0.Range)
                return "R";
            if (sme is Blob)
                return "B";
            if (sme is AasCore.Aas3_0.File)
                return "F";
            if (sme is AnnotatedRelationshipElement)
                return "ARE";
            if (sme is Entity)
                return "E";
            if (sme is Operation)
                return "O";
            if (sme is Capability)
                return "C";

            return null;
        }

        private string getValueAndType(string v, out string sValue, out long iValue, out double fValue)
        {
            sValue = "";
            iValue = 0;
            fValue = 0;

            if (v.All(char.IsDigit) && v.Length <= 10)
            {
                try
                {
                    iValue = Convert.ToInt64(v);
                    return ("I");
                }
                catch
                {
                    sValue = v;
                    return "S";
                }
            }

            if (v.Contains("."))
            {
                string legal = "012345679.E";

                foreach (var c in v)
                {
                    if (Char.IsDigit(c))
                        continue;
                    if (c == '.')
                        continue;
                    if (!legal.Contains(c))
                    {
                        sValue = v;
                        return "S";
                    }
                }

                try
                {
                    var decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    v = v.Replace(".", decSep);
                    v = v.Replace(",", decSep);
                    fValue = Convert.ToDouble(v);
                    return "F";
                }
                catch { }
            }

            sValue = v;
            return "S";
        }
        private void getValue(ISubmodelElement sme, long smeNum, out string vt, out string sValue, out long iValue, out double fValue)
        {
            sValue = "";
            iValue = 0;
            fValue = 0;
            vt = "";
            string v = "";

            if (sme is Property p)
            {
                v = sme.ValueAsText();
                if (v != "")
                {
                    vt = getValueAndType(v, out sValue, out iValue, out fValue);
                }
            }

            if (sme is AasCore.Aas3_0.File f)
            {
                v = f.Value;
                vt = "S";
                sValue = v;
            }

            if (sme is MultiLanguageProperty mlp)
            {
                var ls = mlp.Value;
                if (ls != null)
                {
                    for (int i = 0; i < ls.Count; i++)
                    {
                        var mlpval = new StringValue()
                        {
                            Annotation = ls[i].Language,
                            Value = ls[i].Text,
                            ParentSMENum = smeNum
                        };
                        _db.Add(mlpval);
                    }
                    vt = "S";
                    sValue = v;
                }
            }
            if (sme is AasCore.Aas3_0.Range r)
            {
                v = r.Min;
                // if (v != "")
                //    vt = getValueType(v);

                var v2 = r.Max;
                // var vt2 = "";
                // if (v2 != "")
                //    vt2 = getValueType(v2);

                v += "$$" + v2;
                vt = "S";
                sValue= v;

                /*
                if (vt == "S" || vt2 == "S")
                    vt = "S";
                else if (vt == "I" && vt2 == "I")
                    vt = "I";
                else
                    vt = "F";
                */
            }
        }
        private long collectSMEData(ISubmodelElement sme)
        {
            string st = shortType(sme);
            // Console.WriteLine(st + " idshort " + sme.IdShort);

            long smeNum = ++_dbConfig.SMECount;
            long pn = 0;
            if (_parentNum.Count > 0)
                pn = _parentNum[_parentNum.Count - 1];
            var semanticId = sme.SemanticId.GetAsIdentifier();
            if (semanticId == null)
                semanticId = "";

            string vt = "";
            string sValue = "";
            long iValue = 0;
            double fValue = 0;
            getValue(sme, smeNum, out vt, out sValue, out iValue, out fValue);

            if (vt == "S" && st != "MLP")
            {
                var ValueDB = new StringValue
                {
                    ParentSMENum = smeNum,
                    Value = sValue,
                    Annotation = ""
            };
                _db.Add(ValueDB);
            }
            if (vt == "I")
            {
                var ValueDB = new IntValue
                {
                    ParentSMENum = smeNum,
                    Value = iValue,
                    Annotation = ""
                };
                _db.Add(ValueDB);
            }
            if (vt == "F")
            {
                var ValueDB = new DoubleValue
                {
                    ParentSMENum = smeNum,
                    Value = fValue,
                    Annotation = ""
                };
                _db.Add(ValueDB);
            }

            var smeDB = new SMESet
            {
                SMENum = smeNum,
                SMEType = st,
                SemanticId = semanticId,
                Idshort = sme.IdShort,
                ValueType= vt,
                SubmodelNum = _smNum,
                ParentSMENum = pn
            };
            _db.Add(smeDB);
            return smeNum;
        }
        public override void VisitExtension(IExtension that)
        {
        }
        public override void VisitAdministrativeInformation(IAdministrativeInformation that)
        {
        }
        public override void VisitQualifier(IQualifier that)
        {
        }
        public override void VisitAssetAdministrationShell(IAssetAdministrationShell that)
        {
        }
        public override void VisitAssetInformation(IAssetInformation that)
        {
        }
        public override void VisitResource(IResource that)
        {
        }
        public override void VisitSpecificAssetId(ISpecificAssetId that)
        {
        }
        public override void VisitSubmodel(ISubmodel that)
        {
            base.VisitSubmodel(that);
        }
        public override void VisitRelationshipElement(IRelationshipElement that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitRelationshipElement(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitSubmodelElementList(ISubmodelElementList that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitSubmodelElementList(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitSubmodelElementCollection(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitProperty(IProperty that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitProperty(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitMultiLanguageProperty(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitRange(AasCore.Aas3_0.IRange that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitRange(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitReferenceElement(IReferenceElement that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitReferenceElement(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitBlob(IBlob that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitBlob(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitFile(AasCore.Aas3_0.IFile that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitFile(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitAnnotatedRelationshipElement(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitEntity(IEntity that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitEntity(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitEventPayload(IEventPayload that)
        {
        }
        public override void VisitBasicEventElement(IBasicEventElement that)
        {
        }
        public override void VisitOperation(IOperation that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitOperation(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitOperationVariable(IOperationVariable that)
        {
        }
        public override void VisitCapability(ICapability that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.VisitCapability(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void VisitConceptDescription(IConceptDescription that)
        {
        }
        public override void VisitReference(AasCore.Aas3_0.IReference that)
        {
        }
        public override void VisitKey(IKey that)
        {
        }

        public override void VisitEnvironment(AasCore.Aas3_0.IEnvironment that)
        {
        }

        public override void VisitLangStringNameType(
            ILangStringNameType that
        )
        { }
        public override void VisitLangStringTextType(
            ILangStringTextType that
        )
        { }
        public override void VisitEmbeddedDataSpecification(
            IEmbeddedDataSpecification that
        )
        { }
        public override void VisitLevelType(
            ILevelType that
        )
        { }
        public override void VisitValueReferencePair(
            IValueReferencePair that
        )
        { }
        public override void VisitValueList(
            IValueList that
        )
        { }
        public override void VisitLangStringPreferredNameTypeIec61360(
            ILangStringPreferredNameTypeIec61360 that
        )
        { }
        public override void VisitLangStringShortNameTypeIec61360(
            ILangStringShortNameTypeIec61360 that
        )
        { }
        public override void VisitLangStringDefinitionTypeIec61360(
            ILangStringDefinitionTypeIec61360 that
        )
        { }
        public override void VisitDataSpecificationIec61360(
            IDataSpecificationIec61360 that
        )
        { }

    }

    public class DBRead
    {
        public DBRead() { }
        static public Submodel getSubmodel(string submodelId)
        {
            using (AasContext db = new AasContext())
            {
                var subDB = db.SubmodelSets
                    .OrderBy(s => s.SubmodelNum)
                    .Where(s => s.SubmodelId == submodelId)
                    .ToList()
                    .First();

                if (subDB != null)
                {
                    var SMEList = db.SMESets
                            .OrderBy(sme => sme.SMENum)
                            .Where(sme => sme.SubmodelNum == subDB.SubmodelNum)
                            .ToList();

                    Submodel submodel = new Submodel(submodelId);
                    submodel.IdShort = subDB.Idshort;
                    submodel.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                        new List<IKey>() { new Key(KeyTypes.GlobalReference, subDB.SemanticId) });

                    loadSME(submodel, null, null, SMEList, 0);

                    DateTime timeStamp = DateTime.Now;
                    submodel.TimeStampCreate = timeStamp;
                    submodel.SetTimeStamp(timeStamp);
                    submodel.SetAllParents(timeStamp);

                    return submodel;
                }
            }

            return null;
        }

        static public string getSubmodelJson(string submodelId)
        {
            var submodel = getSubmodel(submodelId);

            if (submodel != null)
            {
                var j = Jsonization.Serialize.ToJsonObject(submodel);
                string json = j.ToJsonString();
                return json;
            }

            return "";
        }

        static private void loadSME(Submodel submodel, ISubmodelElement sme, string SMEType, List<SMESet> SMEList, long smeNum)
        {
            var smeLevel = SMEList.Where(s => s.ParentSMENum== smeNum).OrderBy(s => s.Idshort).ToList();

            foreach (var smel in smeLevel)
            {
                ISubmodelElement nextSME= null;
                switch (smel.SMEType)
                {
                    case "P":
                        nextSME = new Property(DataTypeDefXsd.String, idShort: smel.Idshort, value: smel.getValue());
                        break;
                    case "SEC":
                        nextSME = new SubmodelElementCollection(idShort: smel.Idshort, value: new List<ISubmodelElement>());
                        break;
                    case "MLP":
                        var mlp = new MultiLanguageProperty(idShort: smel.Idshort);
                        var ls = new List<ILangStringTextType>();

                        using (AasContext db = new AasContext())
                        {
                            var SValueSetList = db.SValueSets
                                .Where(s => s.ParentSMENum == smel.SMENum)
                                .ToList();
                            foreach (var MLPValue in SValueSetList)
                            {
                                ls.Add(new LangStringTextType(MLPValue.Annotation, MLPValue.Value));
                            }
                        }

                        mlp.Value = ls;
                        nextSME = mlp;
                        break;
                    case "F":
                        nextSME = new AasCore.Aas3_0.File("text", idShort: smel.Idshort, value: smel.getValue());
                        break;
                }
                if (nextSME == null)
                    continue;

                if (smel.SemanticId != "")
                {
                    nextSME.SemanticId = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference,
                        new List<IKey>() { new Key(KeyTypes.GlobalReference, smel.SemanticId) });
                }

                if (sme == null)
                {
                    submodel.Add(nextSME);
                }
                else
                {
                    switch(SMEType)
                    {
                        case "SEC":
                            (sme as SubmodelElementCollection).Value.Add(nextSME);
                            break;
                    }
                }

                if (smel.SMEType == "SEC")
                {
                    loadSME(submodel, nextSME, smel.SMEType, SMEList, smel.SMENum);
                }

                /*
                if (sme is RelationshipElement)
                    return "RE";
                if (sme is SubmodelElementList)
                    return "SEL";
                if (sme is SubmodelElementCollection)
                    return "SEC";
                if (sme is MultiLanguageProperty)
                    return "MLP";
                if (sme is ReferenceElement)
                    return ("RE");
                if (sme is AasCore.Aas3_0_RC02.Range)
                    return "R";
                if (sme is Blob)
                    return "B";
                if (sme is File)
                    return "F";
                if (sme is AnnotatedRelationshipElement)
                    return "ARE";
                if (sme is Entity)
                    return "E";
                if (sme is Operation)
                    return "O";
                if (sme is Capability)
                    return "C";
                */
            }
        }
    }
}
