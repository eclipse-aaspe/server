using AasCore.Aas3_0;
using AasxRestServerLibrary;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static AasCore.Aas3_0.Visitation;
using static Org.BouncyCastle.Math.EC.ECCurve;

// https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli
// Change database:
// Add-Migration XXXX
// Update-Database

namespace AasxServer
{
    public class AasContext : DbContext
    {
        public DbSet<DbConfigSet> DbConfigSets { get; set; }
        public DbSet<AASXSet> AASXSets { get; set; }
        public DbSet<AasSet> AasSets { get; set; }
        public DbSet<SubmodelSet> SubmodelSets { get; set; }
        public DbSet<SMESet> SMESets { get; set; }
        public string DbPath { get; }

        public AasContext()
        {
            DbPath = AasxHttpContextHelper.DataPath + "/database.db";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (System.IO.File.Exists(AasxHttpContextHelper.DataPath + "/POSTGRES.DAT"))
            {
                Console.WriteLine("Use POSTGRES");
                Program.isPostgres= true;
                // options.UseNpgsql("Host=localhost; Database=AAS; Username=postgres; Password=postres; Include Error Detail=true; Port=5432");
                options.UseNpgsql("Host=aasx-server-postgres; Database=AAS; Username=postgres; Password=postres; Include Error Detail=true; Port=5432");
            }
            else
            {
                Console.WriteLine("Use SQLITE");
                Program.isPostgres = false;
                options.UseSqlite($"Data Source={DbPath}");
            }
        }

        /*
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql("Host=localhost; Database=AAS; Username=postgres; Password=postres; Include Error Detail=true; Port=5432");
            // => options.UseNpgsql("Host=aasx-server-postgres; Database=AAS; Username=postgres; Password=postres; Include Error Detail=true; Port=5432");
        */
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
        public string SValue { get; set; }
        public long IValue { get; set; }
        public double FValue { get; set; }

        public string getValue()
        {
            switch (ValueType)
            {
                case "S":
                    return SValue;
                case "I":
                    return IValue + "";
                case "F":
                    return FValue + "";
            }

            return "";
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
        public AASXSet GetAASXRaw(int AASXnum)
        {
            using (AasContext db = new AasContext())
            {
                var aasx = db.AASXSets.Where(aasx => aasx.AASXNum == AASXnum).First();
                return aasx;
            }
        }
        public AasSet GetAasRaw(int aasnum)
        {
            using (AasContext db = new AasContext())
            {
                var aas = db.AasSets.Where(aas => aas.AasNum == aasnum).First();
                return aas;
            }
        }
        public SubmodelSet GetSubmodelRaw(int submodelnum)
        {
            using (AasContext db = new AasContext())
            {
                var sub = db.SubmodelSets.Where(s => s.SubmodelNum == submodelnum).First();
                return sub;
            }
        }
        public SMESet GetSMERaw(int SMEnum)
        {
            using (AasContext db = new AasContext())
            {
                var sme = db.SMESets.Where(s => s.SMENum == SMEnum).First();
                return sme;
            }
        }
        public List<SMESet> GetSMEListRaw(int submodelnum)
        {
            using (AasContext db = new AasContext())
            {
                var list = db.SMESets.Where(s => s.SubmodelNum == submodelnum).ToList();
                return list;
            }
        }
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

        List<SMESet> getPage(AasContext c, int j, int pageSize, long lower, long upper)
        {
            List<SMESet> list = new List<SMESet>();
            double fLower = 0;
            double fUpper = 0;
            try
            {
                fLower = lower;
                fUpper = upper;
            }
            catch { }
            list = c.SMESets
                .Where(s => s.SMEType == "P" && 
                        ((s.ValueType == "I" && s.IValue >= lower && s.IValue <= upper) ||
                        (s.ValueType == "F" && s.FValue >= fLower && s.FValue <= fUpper)))
                .Skip(j * pageSize)
                .Take(pageSize)
                .ToList();
            return list;
        }
        public List<SmeResult> SearchSMEs(string submodelSemanticId = "", string semanticId = "", string equal = "", string lower = "", string upper = "")
        {
            List<SmeResult> result = new List<SmeResult>();
            List<SMESet> list = new List<SMESet>();

            double fEqual = 0;
            double fLower = 0;
            double fUpper = 0;
            long iEqual = 0;
            long iLower = 0;
            long iUpper = 0;
            try
            {
                if (equal != "")
                {
                    fEqual = Convert.ToDouble(equal);
                    iEqual = Convert.ToInt64(equal);
                }
                if (lower != "" && upper != "")
                {
                    fLower = Convert.ToDouble(lower);
                    fUpper = Convert.ToDouble(upper);
                    iLower = Convert.ToInt64(lower);
                    iUpper = Convert.ToInt64(upper);
                }
            }
            catch { }

            if (semanticId == "" && equal == "" && lower == "" && upper == "")
                return result;

            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine("SearchSMEs");
                Console.WriteLine("Total number of SMEs " + db.SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                /*
                var c = db.SMESets
                    .Where(s => s.SMEType == "P" &&
                        (semanticId == "" || s.SemanticId == semanticId)
                        &&
                        (
                            (equal != "" &&
                                (
                                (s.ValueType == "I" && s.IValue == iEqual) ||
                                (s.ValueType == "F" && s.FValue == fEqual)
                                )
                            )
                            ||
                            (lower != "" && upper != "" &&
                                (
                                (s.ValueType == "I" && s.IValue >= iLower && s.IValue <= iUpper) ||
                                (s.ValueType == "F" && s.FValue >= fLower && s.FValue <= fUpper)
                                )
                            )
                        )
                    )
                    .Count();
                Console.WriteLine("Count number of found SMEs first: " + c + " in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();
                */

                list = db.SMESets
                    .Where(s => s.SMEType == "P" && 
                        (semanticId == "" || s.SemanticId == semanticId)
                        &&
                        (  
                            (equal != "" &&
                                (
                                (s.ValueType == "I" && s.IValue == iEqual) ||
                                (s.ValueType == "F" && s.FValue == fEqual)
                                )
                            )
                            ||
                            (lower != "" && upper != "" &&
                                (
                                (s.ValueType == "I" && s.IValue >= iLower && s.IValue <= iUpper) ||
                                (s.ValueType == "F" && s.FValue >= fLower && s.FValue <= fUpper)
                                )
                            )
                        )
                    )
                    .ToList();

                if (semanticId != "")
                {
                    /*
                    if (equal != "")
                    {
                        list = db.SMESets
                            .Where(s => s.SemanticId == semanticId)
                            .Where(s => s.SMEType == "P" &&
                                    ((s.ValueType == "I" && s.IValue == iEqual) ||
                                    (s.ValueType == "F" && s.FValue == fEqual)))
                            .ToList();
                    }
                    else
                    {
                        if (lower != "" && upper != "")
                        {
                            list = db.SMESets
                                .Where(s => s.SemanticId == semanticId)
                                .Where(s => s.SMEType == "P" &&
                                        ((s.ValueType == "I" && s.IValue >= iLower && s.IValue <= iUpper) ||
                                        (s.ValueType == "F" && s.FValue >= fLower && s.FValue <= fUpper)))
                                .ToList();
                        }
                    }
                    */
                }
                else
                {
                    /*
                    if (equal != "")
                    {
                        var listEqual = db.SMESets.Where(s => s.Value == equal).ToList();
                        list = listEqual;
                    }
                    else
                    {
                        if (lower != "" && upper != "")
                        {
                            int pageSize = 200000;
                            int maxPages = 8;

                            var count = db.SMESets
                                .Where(s => s.SMEType == "P" && s.ValueType != "" && s.Value != "").Count();
                            if (count < pageSize)
                            {
                                list = db.SMESets
                                    .Where(s => s.SMEType == "P" && s.ValueType != "" && s.Value != "")
                                    .ToList()
                                    .Where(s => isLowerUpper(s.ValueType, s.Value, lower, upper)).ToList();
                            }
                            else
                            {
                                int pageCount = count / pageSize + 1;
                                var tasks = new Task[maxPages];
                                var listPage = new List<SMESet>[maxPages];
                                var context = new AasContext[maxPages];

                                Console.WriteLine(pageCount + " pages with size " + pageSize);
                                int p = 0;
                                while (p < pageCount)
                                {
                                    if (pageCount - p < maxPages)
                                        maxPages = pageCount - p;

                                    for (int i = 0; i < maxPages; i++)
                                    {
                                        int pp = p; // workaround task-for paradox
                                        int j = i; // workaround task-for paradox
                                        context[j] = new AasContext();
                                        tasks[j] = Task.Run(() => listPage[j] = getPage(context[j], pp, pageSize, lower, upper));
                                        p++;
                                    }

                                    for (int i = 0; i < maxPages; i++)
                                    {
                                        tasks[i].Wait();
                                        list.AddRange(listPage[i]);

                                        context[i].Dispose();
                                        context[i] = null;
                                        tasks[i].Dispose();
                                        tasks[i] = null;
                                        listPage[i] = null;
                                    }
                                    Console.WriteLine("Processing " + p * pageSize + "/" + count);
                                }
                            }
                        }
                    }
                    */
                }
                Console.WriteLine("Found  " + list.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                foreach (var l in list)
                {
                    SmeResult r = new SmeResult();

                    var submodelDB = db.SubmodelSets.Where(s => s.SubmodelNum == l.SubmodelNum).First();
                    if (submodelDB != null && (submodelSemanticId == "" || submodelDB.SemanticId == submodelSemanticId))
                    {
                        r.submodelId = submodelDB.SubmodelId;
                        r.value = l.getValue();
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

        public int CountSMEs(string submodelSemanticId = "", string semanticId = "", string equal = "", string lower = "", string upper = "")
        {
            double fEqual = 0;
            double fLower = 0;
            double fUpper = 0;
            long iEqual = 0;
            long iLower = 0;
            long iUpper = 0;
            try
            {
                if (equal != "")
                {
                    fEqual = Convert.ToDouble(equal);
                    iEqual = Convert.ToInt64(equal);
                }
                if (lower != "" && upper != "")
                {
                    fLower = Convert.ToDouble(lower);
                    fUpper = Convert.ToDouble(upper);
                    iLower = Convert.ToInt64(lower);
                    iUpper = Convert.ToInt64(upper);
                }
            }
            catch { }

            if (semanticId == "" && equal == "" && lower == "" && upper == "")
                return 0;

            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine("CountSMEs");
                Console.WriteLine("Total number of SMEs " + db.SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                var c = db.SMESets
                    .Where(s => s.SMEType == "P" &&
                        (semanticId == "" || s.SemanticId == semanticId)
                        &&
                        (
                            (equal != "" &&
                                (
                                (s.ValueType == "I" && s.IValue == iEqual) ||
                                (s.ValueType == "F" && s.FValue == fEqual)
                                )
                            )
                            ||
                            (lower != "" && upper != "" &&
                                (
                                (s.ValueType == "I" && s.IValue >= iLower && s.IValue <= iUpper) ||
                                (s.ValueType == "F" && s.FValue >= fLower && s.FValue <= fUpper)
                                )
                            )
                        )
                    )
                    .Count();

                Console.WriteLine("Count number of " + c + " SMEs in " + watch.ElapsedMilliseconds + "ms");
                return c;
            }

        }

        public List<SmeResult> SearchSMEsInSubmodel(string submodelSemanticId = "", string semanticId = "",
            string equal = "", string lower = "", string upper = "")
        {
            List<SmeResult> result = new List<SmeResult>();
            List<SMESet> list = new List<SMESet>();

            if ((submodelSemanticId == "" || semanticId == "") && (equal == "" || (lower == "" && upper == "")))
                return result;

            /*
            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine("SearchSMEsInSubmodel");
                Console.WriteLine("Submodels: " + db.SubmodelSets.Count() + " SMEs " + db.SMESets.Count());

                if (false) // keep for future use
                {
                    var listJoin = db.SubmodelSets
                        .Where(s => s.SemanticId == submodelSemanticId)
                        .Join(db.SMESets,
                            sub => sub.SubmodelNum,
                            sme => sme.SubmodelNum,
                            (sub, sme) => new
                            {
                                SemanticId = sme.SemanticId,
                                Value = sme.SValue,
                                ValueType = sme.ValueType,
                                Idshort = sme.Idshort,
                                ParentSMENum = sme.ParentSMENum,
                                SubmodelNum = sub.SubmodelNum
                            }
                        )
                        .Where(j => j.SemanticId == semanticId && j.ValueType != "" && j.Value != "")
                        .ToList();
                    Console.WriteLine("Create join of " + listJoin.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");
                    watch.Restart();

                    var list2 = listJoin
                        .Where(j => (equal != "" && j.Value == equal)
                            || (equal == "" && isLowerUpper(j.ValueType, j.Value, lower, upper)))
                        .ToList();
                    Console.WriteLine("Filtered values to " + list2.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");
                    watch.Restart();
                }

                if (semanticId != "")
                {
                    list = db.SMESets.Where(s => s.SemanticId == semanticId).ToList();
                    if (equal != "")
                    {
                        var listEqual = list.Where(s => s.Value == equal).ToList();
                        list = listEqual;
                    }
                    else
                    {
                        if (lower != "" && upper != "")
                        {
                            var listLowerUpper = list.Where(s => isLowerUpper(s.ValueType, s.Value, lower, upper)).ToList();
                            list = listLowerUpper;
                        }
                    }
                }
                else
                {
                    if (equal != "")
                    {
                        var listEqual = db.SMESets.Where(s => s.Value == equal).ToList();
                        list = listEqual;
                    }
                    else
                    {
                        if (lower != "" && upper != "")
                        {
                            var listLowerUpper = db.SMESets.Where(s => isLowerUpper(s.ValueType, s.Value, lower, upper)).ToList();
                            list = listLowerUpper;
                        }
                    }
                }

                Console.WriteLine("Filtered values to " + list.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                foreach (var l in list)
                {
                    SmeResult r = new SmeResult();
                    var submodelDB = db.SubmodelSets.Where(s => s.SubmodelNum == l.SubmodelNum).First();
                    if (submodelDB != null && submodelDB.SemanticId == submodelSemanticId)
                    {
                        r.submodelId = submodelDB.SubmodelId;
                        r.value = l.Value;
                        string path = l.Idshort;
                        long pnum = l.ParentSMENum;
                        while (pnum != 0)
                        {
                            var smeDB = db.SMESets.Where(s => s.SMENum == pnum).First();
                            if (smeDB != null)
                                path = smeDB.Idshort + "." + path;
                            pnum = smeDB.ParentSMENum;
                        }
                        r.idShortPath = path;
                        string sub64 = Base64UrlEncoder.Encode(r.submodelId);
                        r.url = Program.externalBlazor + "/submodels/" + sub64 + "/submodelelements/" + path;
                        result.Add(r);
                    }
                }
                Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");
            }
            */

            return result;
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
        private void getValue(ISubmodelElement sme, out string vt, out string sValue, out long iValue, out double fValue)
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
                        v += ls[i].Language + "$$";
                        v += ls[i].Text;
                        if (i < ls.Count - 1)
                            v += "$$";
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
            getValue(sme, out vt, out sValue, out iValue, out fValue);

            var smeDB = new SMESet
            {
                SMENum = smeNum,
                SMEType = st,
                SemanticId = semanticId,
                Idshort = sme.IdShort,
                ValueType= vt,
                SValue= sValue,
                IValue= iValue,
                FValue= fValue,
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
                        if (smel.SValue != "")
                        {
                            var v = smel.SValue.Split("$$");
                            int i = 0;
                            while (i < v.Length)
                            {
                                ls.Add(new LangStringTextType(v[i], v[i + 1]));
                                i += 2;
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
