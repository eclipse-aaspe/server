using AasCore.Aas3_0_RC02;
using AasxRestServerLibrary;
using Extenstions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static AasCore.Aas3_0_RC02.Visitation;
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
            => options.UseSqlite($"Data Source={DbPath}");
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
        public string Value { get; set; }
        public string ValueType { get; set; }
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
            AasContext db = new AasContext();

            var aasx = db.AASXSets.Where(aasx => aasx.AASXNum == AASXnum).Single();
            return aasx;
        }
        public AasSet GetAasRaw(int aasnum)
        {
            AasContext db = new AasContext();

            var aas = db.AasSets.Where(aas => aas.AasNum == aasnum).Single();
            return aas;
        }
        public SubmodelSet GetSubmodelRaw(int submodelnum)
        {
            AasContext db = new AasContext();

            var sub = db.SubmodelSets.Where(s => s.SubmodelNum == submodelnum).Single();
            return sub;
        }
        public SMESet GetSMERaw(int SMEnum)
        {
            AasContext db = new AasContext();

            var sme = db.SMESets.Where(s => s.SMENum == SMEnum).Single();
            return sme;
        }
        public List<SMESet> GetSMEListRaw(int submodelnum)
        {
            AasContext db = new AasContext();

            var list = db.SMESets.Where(s => s.SubmodelNum == submodelnum).ToList();
            return list;
        }
        public List<SubmodelResult> SearchSubmodels(string semanticId)
        {
            AasContext db = new AasContext();

            var subList = db.SubmodelSets.Where(s => s.SemanticId == semanticId).ToList();
            List<SubmodelResult> list = new List<SubmodelResult>();
            foreach (var submodel in subList)
            {
                var sr = new SubmodelResult();
                sr.submodelId= submodel.SubmodelId;
                string sub64 = Base64UrlEncoder.Encode(sr.submodelId);
                sr.url = Program.externalBlazor + "/submodels/" + sub64;
                list.Add(sr);
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
        public List<SmeResult> SearchSMEs(string semanticId = "", string equal = "", string lower = "", string upper = "")
        {
            AasContext db = new AasContext();
            List<SmeResult> result = new List<SmeResult>();
            List<SMESet> list = new List<SMESet>();

            if (semanticId == "" && equal == "" && lower == "" && upper == "")
                return result;

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

            foreach (var l in list)
            {
                SmeResult r = new SmeResult();
                var submodelSet = db.SubmodelSets.Where(s => s.SubmodelNum == l.SubmodelNum).Single();
                r.submodelId = submodelSet.SubmodelId;
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
                r.url = Program.externalBlazor + "/submodels/" + sub64 + "/submodelelements/" + path;
                result.Add(r);
            }

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
            if (sme is AasCore.Aas3_0_RC02.Range)
                return "R";
            if (sme is Blob)
                return "B";
            if (sme is AasCore.Aas3_0_RC02.File)
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

        private string getValueType(string v)
        {
            if (v.All(char.IsDigit))
            {
                return "I";
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
                        return "S";
                }
                return "F";
            }

            return "S";
        }
        private void getValue(ISubmodelElement sme, out string v, out string vt)
        {
            v = "";
            vt = "";

            if (sme is Property p)
            {
                v = sme.ValueAsText();
                if (v != "")
                    vt = getValueType(v);
            }

            if (sme is AasCore.Aas3_0_RC02.File f)
            {
                v = f.Value;
                vt = "S";
            }

            if (sme is MultiLanguageProperty mlp)
            {
                var ls = mlp.Value;
                if (ls != null)
                {
                    for (int i = 0; i < ls.LangStrings.Count; i++)
                    {
                        v += ls.LangStrings[i].Language + "$$";
                        v += ls.LangStrings[i].Text;
                        if (i < ls.LangStrings.Count - 1)
                            v += "$$";
                    }
                    vt = "S";
                }
            }
            if (sme is AasCore.Aas3_0_RC02.Range r)
            {
                v = r.Min;
                if (v != "")
                    vt = getValueType(v);

                var v2 = r.Max;
                var vt2 = "";
                if (v2 != "")
                    vt2 = getValueType(v2);

                v += "$$" + v2;

                if (vt == "S" || vt2 == "S")
                    vt = "S";
                else if (vt == "I" && vt2 == "I")
                    vt = "I";
                else
                    vt = "F";
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

            string v = "";
            string vt = "";
            getValue(sme, out v, out vt);

            var smeDB = new SMESet
            {
                SMENum = smeNum,
                SMEType = st,
                SemanticId = semanticId,
                Idshort = sme.IdShort,
                ValueType= vt,
                Value = v,
                SubmodelNum = _smNum,
                ParentSMENum = pn
            };
            _db.Add(smeDB);
            return smeNum;
        }
        public override void Visit(Extension that)
        {
        }
        public override void Visit(AdministrativeInformation that)
        {
        }
        public override void Visit(Qualifier that)
        {
        }
        public override void Visit(AssetAdministrationShell that)
        {
        }
        public override void Visit(AssetInformation that)
        {
        }
        public override void Visit(Resource that)
        {
        }
        public override void Visit(SpecificAssetId that)
        {
        }
        public override void Visit(Submodel that)
        {
            base.Visit(that);
        }
        public override void Visit(RelationshipElement that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(SubmodelElementList that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(SubmodelElementCollection that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(Property that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(MultiLanguageProperty that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(AasCore.Aas3_0_RC02.Range that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(ReferenceElement that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(Blob that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(AasCore.Aas3_0_RC02.File that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(AnnotatedRelationshipElement that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(Entity that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(EventPayload that)
        {
        }
        public override void Visit(BasicEventElement that)
        {
        }
        public override void Visit(Operation that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(OperationVariable that)
        {
        }
        public override void Visit(Capability that)
        {
            long smeNum = collectSMEData(that);
            _parentNum.Add(smeNum);
            base.Visit(that);
            _parentNum.RemoveAt(_parentNum.Count - 1);
        }
        public override void Visit(ConceptDescription that)
        {
        }
        public override void Visit(Reference that)
        {
        }
        public override void Visit(Key that)
        {
        }
        public override void Visit(LangString that)
        {
        }
        public override void Visit(LangStringSet that)
        {
        }
        public override void Visit(DataSpecificationContent that)
        {
        }
        public override void Visit(DataSpecification that)
        {
        }
        public override void Visit(AasCore.Aas3_0_RC02.Environment that)
        {
        }
    }

    public class DBRead
    {
        public DBRead() { }

        static public string getSubmodel(string submodelId)
        {
            AasContext db = new AasContext();
            var subDB = db.SubmodelSets
                .OrderBy(s => s.SubmodelNum)
                .Where(s => s.SubmodelId == submodelId)
                .Single();

            if (subDB != null)
            {
                var SMEList = db.SMESets
                        .OrderBy(sme => sme.SMENum)
                        .Where(sme => sme.SubmodelNum == subDB.SubmodelNum)
                        .ToList();

                Submodel submodel = new Submodel (submodelId);
                submodel.SemanticId = new Reference(AasCore.Aas3_0_RC02.ReferenceTypes.GlobalReference,
                    new List<Key>() { new Key(KeyTypes.GlobalReference, subDB.SemanticId) });

                loadSME(submodel, null, null, SMEList, 0);

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
                        nextSME = new Property(DataTypeDefXsd.String, idShort: smel.Idshort, value: smel.Value);
                        break;
                    case "SEC":
                        nextSME = new SubmodelElementCollection(idShort: smel.Idshort, value: new List<ISubmodelElement>());
                        break;
                    case "MLP":
                        var mlp = new MultiLanguageProperty(idShort: smel.Idshort);
                        var ls = new List<LangString>();
                        if (smel.Value != "")
                        {
                            var v = smel.Value.Split("$$");
                            int i = 0;
                            while (i < v.Length)
                            {
                                ls.Add(new LangString(v[i / 2], v[i / 2 + 1]));
                                i += 2;
                            }
                        }
                        mlp.Value = new LangStringSet(ls);
                        nextSME = mlp;
                        break;
                    case "F":
                        nextSME = new AasCore.Aas3_0_RC02.File("text", idShort: smel.Idshort, value: smel.Value);
                        break;
                }
                if (nextSME != null && smel.SemanticId != "")
                {
                    nextSME.SemanticId = new Reference(AasCore.Aas3_0_RC02.ReferenceTypes.GlobalReference,
                        new List<Key>() { new Key(KeyTypes.GlobalReference, smel.SemanticId) });
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
