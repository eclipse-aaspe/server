using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using AasxServerDB.Entities;
using AasxServerDB.Result;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TimeStamp;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AasxServerDB
{
    public class Query
    {
        public static string? ExternalBlazor { get; set; }

        public int Test(string semanticId = "", string identifier = "", string diff = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            watch.Restart();
            var testOld = TestList();
            Console.WriteLine("SME Old in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var testEnum = TestEnum();
            Console.WriteLine("SME Enum in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var testQuer = TestQuer();
            Console.WriteLine("SME Quer in " + watch.ElapsedMilliseconds + "ms");

            return 1;
        }

        private static List<SMESet> TestList()
        {
            return new AasContext().SMESets.ToList();
        }

        private static IEnumerable<SMESet> TestEnum()
        {
            return new AasContext().SMESets;
        }

        private static IQueryable<SMESet> TestQuer()
        {
            return new AasContext().SMESets.AsQueryable();
        }








        // --------------- API ---------------
        public List<SMResult> SearchSMs(string semanticId = "", string identifier = "", string diff = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMs");
            Console.WriteLine("Total number of SMs " + new AasContext().SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMSet(semanticId, identifier, diff);
            Console.WriteLine("SM found in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMResult(enumerable);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public int CountSMs(string semanticId = "", string identifier = "", string diff = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("CountSMs");
            Console.WriteLine("Total number of SMs " + new AasContext().SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMSet(semanticId, identifier, diff);
            var count = enumerable.Count();
            Console.WriteLine("Found " + count + " SM in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMEWithValue(smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
            Console.WriteLine("SME found in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMEResult(enumerable);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public List<SMEResult> SearchSMEsNew(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMEsNew");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMEWithValueNew(smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
            Console.WriteLine("SME found in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMEResultNew(enumerable);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public int CountSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("CountSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var smeWithValue = GetSMEWithValue(smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
            var count = smeWithValue.Count;
            Console.WriteLine("Found " + count + " SMEs in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEsResult(
            string smSemanticId = "",
            string searchSemanticId = "",  string searchIdShort = "",
            string? equal = "", string? contains = "",
            string resultSemanticId = "", string resultIdShort = "")
        {
            List<SMEResult> result = new List<SMEResult>();
            
            if (searchSemanticId.IsNullOrEmpty() && searchIdShort.IsNullOrEmpty())
                return result;
            if (equal.IsNullOrEmpty() && contains.IsNullOrEmpty())
                return result;
            if (resultSemanticId.IsNullOrEmpty() && resultIdShort.IsNullOrEmpty())
                return result;

            bool withI = false;
            long iEqual = 0;
            bool withF = false;
            double fEqual = 0;
            try
            {
                if (!equal.IsNullOrEmpty())
                {
                    iEqual = Convert.ToInt64(equal);
                    withI = true;
                    fEqual = Convert.ToDouble(equal);
                    withF = true;
                }
            }
            catch { }

            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine("SearchSMEs");
                Console.WriteLine("Total number of SMEs " + db.SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                bool withContains = (!contains.IsNullOrEmpty());
                bool withEqual = !withContains && (!equal.IsNullOrEmpty());

                var list = db.SValueSets.Where(v =>
                    (withContains && v.Value.Contains(contains)) ||
                    (withEqual && v.Value == equal)
                    )
                    .Join(db.SMESets,
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SMId = sme.SMId,
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            ParentSme = sme.ParentSMEId,
                            Value = v.Value
                        }
                    )
                    .Where(s =>
                        (!searchSemanticId.IsNullOrEmpty() && s.SemanticId == searchSemanticId) ||
                        (!searchIdShort.IsNullOrEmpty() && s.IdShort == searchIdShort)
                    )
                    .Join(db.SMSets,
                        v => v.SMId,
                        s => s.Id,
                        (v, s) => new
                        {
                            Id = s.Id,
                            SemanticId = s.SemanticId,
                            ParentSme = v.ParentSme,
                            Value = v.Value
                        }
                    )
                    .Where(s =>
                        smSemanticId == "" || s.SemanticId == smSemanticId
                    )
                    .ToList();

                list.AddRange(db.IValueSets.Where(v =>
                    (withEqual && withI && v.Value == iEqual)
                    )
                    .Join(db.SMESets,
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SMId = sme.SMId,
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            ParentSme = sme.ParentSMEId,
                            Value = v.Value.ToString()
                        }
                    )
                    .Where(s =>
                        (!searchSemanticId.IsNullOrEmpty() && s.SemanticId == searchSemanticId) ||
                        (!searchIdShort.IsNullOrEmpty() && s.IdShort == searchIdShort)
                    )
                    .Join(db.SMSets,
                        v => v.SMId,
                        s => s.Id,
                        (v, s) => new
                        {
                            Id = s.Id,
                            SemanticId = s.SemanticId,
                            ParentSme = v.ParentSme,
                            Value = v.Value
                        }
                    )
                    .Where(s =>
                        smSemanticId == "" || s.SemanticId == smSemanticId
                    )
                    .ToList());

                list.AddRange(db.DValueSets.Where(v =>
                    (withEqual && withF && v.Value == fEqual)
                    )
                    .Join(db.SMESets,
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SMId = sme.SMId,
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            ParentSme = sme.ParentSMEId,
                            Value = v.Value.ToString()
                        }
                    )
                    .Where(s =>
                        (!searchSemanticId.IsNullOrEmpty() && s.SemanticId == searchSemanticId) ||
                        (!searchIdShort.IsNullOrEmpty() && s.IdShort == searchIdShort)
                    )
                    .Join(db.SMSets,
                        v => v.SMId,
                        s => s.Id,
                        (v, s) => new
                        {
                            Id = s.Id,
                            SemanticId = s.SemanticId,
                            ParentSme = v.ParentSme,
                            Value = v.Value
                        }
                    )
                    .Where(s =>
                        smSemanticId == "" || s.SemanticId == smSemanticId
                    )
                    .ToList());

                Console.WriteLine("Found " + list.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");

                var hSubmodel = new HashSet<long>();
                var lParentParentNum = new List<int?>();
                foreach (var l in list)
                {
                    hSubmodel.Add(l.Id);
                    var smeDB = db.SMESets.Where(s => s.Id == l.ParentSme).First();
                    lParentParentNum.Add(smeDB.ParentSMEId);
                }

                Console.WriteLine("Found " + hSubmodel.Count() + " Submodels");

                watch.Restart();

                var SMEResult = db.SMESets.Where(s =>
                    hSubmodel.Contains(s.SMId) &&
                    ((!resultSemanticId.IsNullOrEmpty() && s.SemanticId == resultSemanticId) ||
                    (!resultIdShort.IsNullOrEmpty() && s.IdShort == resultIdShort))
                    )
                    .ToList();

                if (equal.IsNullOrEmpty())
                    equal = contains;

                foreach (var l in SMEResult)
                {
                    SMEResult r = new SMEResult();
                    bool found = false;

                    var submodelDB = db.SMSets.Where(s => s.Id == l.SMId).First();
                    if (submodelDB != null && (smSemanticId.IsNullOrEmpty() || submodelDB.SemanticId == smSemanticId))
                    {
                        r.value = equal;
                        r.url = string.Empty;
                        r.smId = submodelDB.Identifier;
                        string path = l.IdShort;
                        int? pId = l.ParentSMEId;
                        while (pId != null)
                        {
                            var smeDB = db.SMESets.Where(s => s.Id == pId).First();
                            path = smeDB.IdShort + "." + path;
                            pId = smeDB.ParentSMEId;
                            if (lParentParentNum.Contains(pId))
                            {
                                found = true;
                                if (l.SMEType == "D")
                                {
                                    var v = db.SValueSets.Where(v => v.SMEId == l.Id).FirstOrDefault();
                                    if (v.Value.ToLower().StartsWith("http"))
                                        r.url = v.Value;
                                }
                            }
                        }
                        r.idShortPath = path;
                        string sub64 = Base64UrlEncoder.Encode(r.smId);
                        if (r.url.IsNullOrEmpty())
                            r.url = ExternalBlazor + "/submodels/" + sub64 + "/submodel-elements/" + path + "/attachment";
                        if (found)
                            result.Add(r);
                    }
                }
                Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");
            }

            return result;
        }

        // --------------- SM Methodes ---------------
        private static IEnumerable<SMSet> GetSMSet(string semanticId = "", string identifier = "", string diffString = "")
        {
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();
            var diff = TimeStamp.TimeStamp.StringToDateTime(diffString);
            var withDiff = !diff.Equals(DateTime.MinValue);

            if (!withSemanticId && !withIdentifier && !withDiff)
                return new List<SMSet>();

            return new AasContext().SMSets
                .Where(s =>
                    (!withSemanticId || (s.SemanticId != null && s.SemanticId.Equals(semanticId))) &&
                    (!withIdentifier || (s.Identifier != null && s.Identifier.Equals(identifier))) &&
                    (!withDiff || s.TimeStampTree.CompareTo(diff) > 0));
        }

        private static List<SMResult> GetSMResult(IEnumerable<SMSet> enumerable)
        {
            var shortEnum = enumerable
                .Select(sm => new { sm.Identifier, sm.TimeStampTree })
                .Distinct();

            var result = new List<SMResult>();
            foreach (var sm in shortEnum)
            {
                var identifier = (sm != null && !sm.Identifier.IsNullOrEmpty()) ? sm.Identifier : string.Empty;
                result.Add( new SMResult {
                        smId = identifier,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(identifier)}",
                        timeStampTree = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree)
                    }
                );
            }
            return result;
        }

        // --------------- SME Methodes ---------------
        private class SMEWithValue
        {
            public SMSet? sm;
            public SMESet? sme;
            public string? value;
        }

        private List<SMEWithValue> GetSMEWithValue(string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "", string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var result = new List<SMEWithValue>();

            var dateTime = TimeStamp.TimeStamp.StringToDateTime(diff);
            var withDiff = !diff.Equals(DateTime.MinValue);

            var parameter = 0;
            if (!contains.IsNullOrEmpty())
                parameter++;
            if (!equal.IsNullOrEmpty())
                parameter++;
            if (!(lower.IsNullOrEmpty() && upper.IsNullOrEmpty()))
                parameter++;
            if (parameter > 1 || (semanticId.IsNullOrEmpty() && !withDiff && parameter != 1))
                return result;

            GetXValue(ref result, semanticId, dateTime, contains, equal, lower, upper);
            GetSValue(ref result, semanticId, dateTime, contains, equal);
            GetIValue(ref result, semanticId, dateTime, equal, lower, upper);
            GetDValue(ref result, semanticId, dateTime, equal, lower, upper);
            GetOValue(ref result, semanticId, dateTime, contains, equal);
            SelectSM(ref result, smSemanticId, smIdentifier);
            return result;
        }

        private static void GetXValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var withValue = !contains.IsNullOrEmpty() || !equal.IsNullOrEmpty() || !lower.IsNullOrEmpty() || !upper.IsNullOrEmpty();
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            if (!withDiff || withValue)
                return;

            using AasContext db = new();
            smeValue.AddRange(db.SMESets
                        .Where(sme =>
                            (sme.TValue == string.Empty || sme.TValue == null) &&
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0))
                        .Select(sme => new SMEWithValue { sme = sme })
                .ToList());
        }

        private static void GetSValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "")
        {
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqual = !equal.IsNullOrEmpty();
            if (!withDiff && !withContains && !withEqual)
                return;

            using AasContext db = new();
            smeValue.AddRange(db.SValueSets
                .Where(v => v.Value != null &&
                    (!withContains || v.Value.Contains(contains)) &&
                    (!withEqual || v.Value.Equals(equal)))
                .Join(
                    db.SMESets
                        .Where(sme => 
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0)),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value })
                .ToList());
        }

        private static void GetIValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string equal = "", string lower = "", string upper = "")
        {
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withEqual = !equal.IsNullOrEmpty();
            var withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withDiff && !withEqual && !withCompare)
                return;

            var iEqual = (long) 0;
            var iLower = (long) 0;
            var iUpper = (long) 0;
            try
            {
                if (withEqual)
                    iEqual = Convert.ToInt64(equal);
                else if (withCompare)
                {
                    iLower = Convert.ToInt64(lower);
                    iUpper = Convert.ToInt64(upper);
                }
            }
            catch 
            {
                return;
            }

            using AasContext db = new();
            smeValue.AddRange(db.IValueSets
                .Where(v => v.Value != null &&
                    (!withEqual || v.Value == iEqual) &&
                    (!withCompare || (v.Value >= iLower && v.Value <= iUpper)))
                .Join(
                    (db.SMESets
                        .Where(sme =>
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() })
                .ToList());
        }

        private static void GetDValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string equal = "", string lower = "", string upper = "")
        {
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withEqual = !equal.IsNullOrEmpty();
            var withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withDiff && !withEqual && !withCompare)
                return;

            var dEqual = (long) 0;
            var dLower = (long) 0;
            var dUpper = (long) 0;
            try
            {
                if (withEqual)
                    dEqual = Convert.ToInt64(equal);
                else if (withCompare)
                {
                    dLower = Convert.ToInt64(lower);
                    dUpper = Convert.ToInt64(upper);
                }
            }
            catch 
            {
                return;
            }

            using AasContext db = new();
            smeValue.AddRange(db.DValueSets
                .Where(v => v.Value != null &&
                    (!withEqual || v.Value == dEqual) &&
                    (!withCompare || (v.Value >= dLower && v.Value <= dUpper)))
                .Join(
                    (db.SMESets
                        .Where(sme =>
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() })
                .ToList());
        }

        private static void GetOValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "")
        {
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqual = !equal.IsNullOrEmpty();
            if (!withDiff && !withContains && !withEqual)
                return;

            using AasContext db = new();
            smeValue.AddRange(db.OValueSets
                .Where(v => v.Value != null &&
                    (!withContains || ((string) v.Value).Contains(contains)) &&
                    (!withEqual || ((string) v.Value).Equals(equal)))
                .Join(
                    db.SMESets
                        .Where(sme =>
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0)),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = (string) v.Value })
                .ToList());
        }

        private static void SelectSM(ref List<SMEWithValue> smeValue, string semanticId = "", string identifier = "")
        {
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();
            using AasContext db = new();
            smeValue = smeValue
                .Join((db.SMSets.Where(sm =>
                    (!withSemanticId || (sm.SemanticId != null && sm.SemanticId.Equals(semanticId))) &&
                    (!withIdentifier || (sm.Identifier != null && sm.Identifier.Equals(identifier))))),
                    sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value })
                .Where(sme => sme.sm != null)
                .ToList();
        }

        private static List<SMEResult> GetSMEResult(List<SMEWithValue> smeList)
        {
            using AasContext db = new();
            return smeList.ConvertAll(
                sme =>
                {
                    var identifier = (sme != null && sme.sm.Identifier != null) ? sme.sm.Identifier : "";
                    var path = sme.sme.IdShort;
                    int? pId = sme.sme.ParentSMEId;
                    while (pId != null)
                    {
                        var smeDB = db.SMESets.Where(s => s.Id == pId).First();
                        path = $"{smeDB.IdShort}.{path}";
                        pId = smeDB.ParentSMEId;
                    }

                    return new SMEResult()
                    {
                        smId = identifier,
                        value = sme.value,
                        idShortPath = path,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(identifier)}/submodel-elements/{path}",
                        timeStampTree = TimeStamp.TimeStamp.DateTimeToString(sme.sme.TimeStampTree)
                    };
                }
            );
        }


        private IEnumerable<SMEWithValue> GetSMEWithValueNew(string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "", string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var withSMSemanticId = !smSemanticId.IsNullOrEmpty();
            var withSMIdentifier = !smIdentifier.IsNullOrEmpty();
            var restrictSM = withSMSemanticId || withSMIdentifier;

            var withSemanticId = !semanticId.IsNullOrEmpty();
            var diffDateTime = TimeStamp.TimeStamp.StringToDateTime(diff);
            var withDiff = !diffDateTime.Equals(DateTime.MinValue);
            var restrictSME = withSemanticId || withDiff;

            var withContains = !contains.IsNullOrEmpty();
            var withEqualString = !equal.IsNullOrEmpty();
            var withEqualNum = Int64.TryParse(equal, out long equalNum);
            var withLower = Int64.TryParse(lower, out long lowerNum);
            var withUpper = Int64.TryParse(upper, out long upperNum);

            var withText = withContains || withEqualString;
            var withCompare = withLower && withUpper;
            var withNum = withEqualNum || withCompare;
            if (withText && withNum)
                return new List<SMEWithValue>();
            var restrictValue = (withText && !withNum) || (!withText && withNum);

            /* choose bottom-up or up-bottom
             * 
             *              | bottom-up | up-bottom
             * -------------------------------------
             * smSemanticId |           |     X      
             * smIdentifier |           |     X
             * semanticId   |           |
             * diff         |           |
             * contains     |     X     |
             * equal        |     X     |
             * lower        |     X     |
             * upper        |     X     |
            */
            using AasContext db = new();

            // Alle, keine, teilweise
            // query for each table
            var sValue = db.SValueSets
                .Where(v =>
                    !restrictValue ||
                        (restrictValue && withText && v.Value != null &&
                        (!withContains || v.Value.Contains(contains)) &&
                        (!withEqualString || v.Value.Equals(equal))));

            var iValue = db.IValueSets
                .Where(v =>
                    !restrictValue ||
                        (restrictValue && withNum && v.Value != null &&
                        (!withEqualNum || v.Value == equalNum) &&
                        (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))));

            var dValue = db.DValueSets
                .Where(v =>
                    !restrictValue ||
                        (restrictValue && withNum && v.Value != null &&
                        (!withEqualNum || v.Value == equalNum) &&
                        (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))));

            var oValue = db.OValueSets
                .Where(v =>
                    !restrictValue ||
                        (restrictValue && withText && v.Value != null &&
                        (!withContains || ((string) v.Value).Contains(contains)) &&
                        (!withEqualString || ((string) v.Value).Equals(equal))));

            var sme = db.SMESets
                .Where(sme =>
                    !restrictSME ||
                    (restrictSME &&
                    (!withSemanticId || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                    (!withDiff || sme.TimeStampTree.CompareTo(diffDateTime) > 0)));

            var sm = db.SMSets
                .Where(sm =>
                    !restrictSM ||
                    (restrictSM &&
                    (!withSMSemanticId || (sm.SemanticId != null && sm.SemanticId.Equals(smSemanticId))) &&
                    (!withSMIdentifier || (sm.Identifier != null && sm.Identifier.Equals(smIdentifier)))));

            // bottom-up
            var sValueSMESM = sValue
                .Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value })
                .Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value });
            var iValueSMESM = iValue
                .Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() })
                .Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value });
            var dValueSMESM = dValue
                .Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() })
                .Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value });
            var oValueSMESM = oValue
                .Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() })
                .Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value });
            
            var xValueSMESM = sme.Where(sme => !restrictValue && (sme.TValue == null || sme.TValue.Equals(""))).Select(sme => new { sme, value = "" })
                .Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value });

            // Das problem ist das Union bzw. Concat, erst valuesSMe zusammen und dann sm, oder erst 
            var valueSME = sValueSMESM.Union(iValueSMESM).Union(dValueSMESM).Union(oValueSMESM).Union(xValueSMESM);
            /*var valueSMESM = valueSME.Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value })
                .Where(sme => sme.sm != null);*/

            //Check ich glaube der mach inner Join hier

            return new List<SMEWithValue>();
        }

        private static void GetXValueNew(ref IEnumerable<SMEWithValue> enumerable, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var withValue = !contains.IsNullOrEmpty() || !equal.IsNullOrEmpty() || !lower.IsNullOrEmpty() || !upper.IsNullOrEmpty();
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            if (!withDiff || withValue)
                return;

            using AasContext db = new();
            enumerable = enumerable.Concat(db.SMESets
                        .Where(sme =>
                            (sme.TValue == string.Empty || sme.TValue == null) &&
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0))
                        .Select(sme => new SMEWithValue { sme = sme }));
        }

        private static void GetSValueNew(ref IEnumerable<SMEWithValue> enumerable, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "")
        {
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqual = !equal.IsNullOrEmpty();
            if (!withDiff && !withContains && !withEqual)
                return;

            using AasContext db = new();
            enumerable = enumerable.Concat(db.SValueSets
                .Where(v => v.Value != null &&
                    (!withContains || v.Value.Contains(contains)) &&
                    (!withEqual || v.Value.Equals(equal)))
                .Join(
                    db.SMESets
                        .Where(sme =>
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0)),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value }));
        }

        private static void GetIValueNew(ref IEnumerable<SMEWithValue> enumerable, string semanticId = "", DateTime diff = new(), string equal = "", string lower = "", string upper = "")
        {
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withEqual = !equal.IsNullOrEmpty();
            var withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withDiff && !withEqual && !withCompare)
                return;

            var iEqual = (long)0;
            var iLower = (long)0;
            var iUpper = (long)0;
            try
            {
                if (withEqual)
                    iEqual = Convert.ToInt64(equal);
                else if (withCompare)
                {
                    iLower = Convert.ToInt64(lower);
                    iUpper = Convert.ToInt64(upper);
                }
            }
            catch
            {
                return;
            }

            using AasContext db = new();
            enumerable = enumerable.Concat(db.IValueSets
                .Where(v => v.Value != null &&
                    (!withEqual || v.Value == iEqual) &&
                    (!withCompare || (v.Value >= iLower && v.Value <= iUpper)))
                .Join(
                    (db.SMESets
                        .Where(sme =>
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() }));
            
        }

        private static void GetDValueNew(ref IEnumerable<SMEWithValue> enumerable, string semanticId = "", DateTime diff = new(), string equal = "", string lower = "", string upper = "")
        {
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withEqual = !equal.IsNullOrEmpty();
            var withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withDiff && !withEqual && !withCompare)
                return;

            var dEqual = (long)0;
            var dLower = (long)0;
            var dUpper = (long)0;
            try
            {
                if (withEqual)
                    dEqual = Convert.ToInt64(equal);
                else if (withCompare)
                {
                    dLower = Convert.ToInt64(lower);
                    dUpper = Convert.ToInt64(upper);
                }
            }
            catch
            {
                return;
            }

            using AasContext db = new();
            enumerable = enumerable.Concat(db.DValueSets
                .Where(v => v.Value != null &&
                    (!withEqual || v.Value == dEqual) &&
                    (!withCompare || (v.Value >= dLower && v.Value <= dUpper)))
                .Join(
                    (db.SMESets
                        .Where(sme =>
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() }));
        }

        private static void GetOValueNew(ref IEnumerable<SMEWithValue> enumerable, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "")
        {
            var withSME = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqual = !equal.IsNullOrEmpty();
            if (!withDiff && !withContains && !withEqual)
                return;

            using AasContext db = new();
            enumerable = enumerable.Concat(db.OValueSets
                .Where(v => v.Value != null &&
                    (!withContains || ((string)v.Value).Contains(contains)) &&
                    (!withEqual || ((string)v.Value).Equals(equal)))
                .Join(
                    db.SMESets
                        .Where(sme =>
                            (!withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStampTree.CompareTo(diff) > 0)),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = (string)v.Value }));
        }

        private static void SelectSMNew(ref IEnumerable<SMEWithValue> enumerable, string semanticId = "", string identifier = "")
        {
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();

            using AasContext db = new();
            enumerable = enumerable
                .Join((db.SMSets.Where(sm =>
                    (!withSemanticId || (sm.SemanticId != null && sm.SemanticId.Equals(semanticId))) &&
                    (!withIdentifier || (sm.Identifier != null && sm.Identifier.Equals(identifier))))),
                    sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value })
                .Where(sme => sme.sm != null);
        }

        private static List<SMEResult> GetSMEResultNew(IEnumerable<SMEWithValue> enumerable)
        {
            using AasContext db = new();

            var shortEnum = enumerable
                .Select(sme => new {
                    smId = sme.sm.Identifier,
                    idShort = sme.sme.IdShort,
                    parentSMEId = sme.sme.ParentSMEId,
                    timeStampTree = sme.sme.TimeStampTree,
                    value = sme.value
                })
                .Distinct();

            var result = new List<SMEResult>();
            foreach (var sme in shortEnum)
            {
                var smId = (sme != null && sme.smId != null) ? sme.smId : "";
                var path = sme.idShort;
                int? pId = sme.parentSMEId;
                while (pId != null)
                {
                    var smeDB = db.SMESets.Where(s => s.Id == pId).First();
                    path = $"{smeDB.IdShort}.{path}";
                    pId = smeDB.ParentSMEId;
                }
                result.Add( new SMEResult()
                    {
                        smId = smId,
                        value = sme.value,
                        idShortPath = path,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(smId)}/submodel-elements/{path}",
                        timeStampTree = TimeStamp.TimeStamp.DateTimeToString(sme.timeStampTree)
                    }
                );
            }
            return result;
        }
    }
}
