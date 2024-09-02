using System;
using System.Diagnostics;
using AasxServerDB.Entities;
using AasxServerDB.Result;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerDB
{
    public class Query
    {
        public static string? ExternalBlazor { get; set; }

        // --------------- API ---------------
        public List<SMResult> SearchSMs(string semanticId = "", string identifier = "", string diff = "")
        {
            using AasContext db = new();

            var watch = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMs");
            Console.WriteLine("Total number of SMs " + new AasContext().SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMSet(db, semanticId, identifier, diff);
            Console.WriteLine("SMs found in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMResult(enumerable);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public int CountSMs(string semanticId = "", string identifier = "", string diff = "")
        {
            using AasContext db = new();

            var watch = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("CountSMs");
            Console.WriteLine("Total number of SMs " + new AasContext().SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMSet(db, semanticId, identifier, diff);
            var count = enumerable.Count();
            Console.WriteLine("Found " + count + " SM in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            using AasContext db = new();

            var watch = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMEWithValue(db, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
            Console.WriteLine("SMEs found in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMEResult(db, enumerable);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public int CountSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            using AasContext db = new();

            var watch = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("CountSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var smeWithValue = GetSMEWithValue(db, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
            var count = smeWithValue.Count();
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
        private static IEnumerable<SMSet> GetSMSet(AasContext db, string semanticId = "", string identifier = "", string diffString = "")
        {
            // analyse parameters
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();
            var diff = TimeStamp.TimeStamp.StringToDateTime(diffString);
            var withDiff = !diff.Equals(DateTime.MinValue);

            if (!withSemanticId && !withIdentifier && !withDiff)
                return new List<SMSet>();

            return db.SMSets
                .Where(s =>
                    (!withSemanticId || (s.SemanticId != null && s.SemanticId.Equals(semanticId))) &&
                    (!withIdentifier || (s.Identifier != null && s.Identifier.Equals(identifier))) &&
                    (!withDiff || s.TimeStampTree.CompareTo(diff) > 0));
        }

        private static List<SMResult> GetSMResult(IEnumerable<SMSet> enumerable)
        {
            var shortEnum = enumerable.Select(sm => new { identifier = sm.Identifier ?? "", sm.TimeStampTree });

            var result = new List<SMResult>();
            foreach (var sm in shortEnum)
            {
                result.Add(
                    new SMResult{
                        smId = sm.identifier,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm.identifier)}",
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

        private IEnumerable<SMEWithValue> GetSMEWithValue(AasContext db, string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "", string contains = "", string equal = "", string lower = "", string upper = "")
        {
            // analyse parameters
            var withSMSemanticId = !smSemanticId.IsNullOrEmpty();
            var withSMIdentifier = !smIdentifier.IsNullOrEmpty();
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var diffDateTime = TimeStamp.TimeStamp.StringToDateTime(diff);
            var withDiff = !diffDateTime.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqualString = !equal.IsNullOrEmpty();
            var withEqualNum = Int64.TryParse(equal, out long equalNum);
            var withLower = Int64.TryParse(lower, out long lowerNum);
            var withUpper = Int64.TryParse(upper, out long upperNum);

            // analyse value parameters
            var withText = withContains || withEqualString;
            var withCompare = withLower && withUpper;
            var withNum = withEqualNum || withCompare;
            if (withText && withNum && !withEqualString)
                return new List<SMEWithValue>();

            // check restrictions
            var restrictSM = withSMSemanticId || withSMIdentifier;
            var restrictSME = withSemanticId || withDiff;
            var restrictValue = withText || withNum;

            // restrict all tables seperate 
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

            /* choose top-down or down-top
             * 
             *              | top-down  | down-top
             * -------------------------------------
             * smSemanticId |     x     |           
             * smIdentifier |     x     |     
             * semanticId   |     x     |     
             * diff         |     x     |     
             * contains     |           |     X
             * equal        |           |     X
             * lower        |           |     X
             * upper        |           |     X
            */
            IEnumerable<SMEWithValue> valueSMESM;
            // top-down
            if (withSMSemanticId || withSMIdentifier || withSemanticId || withDiff)
            {
                var smSME = sm.Join(sme, sm => sm.Id, sme => sme.SMId, (sm, sme) => new { sm, sme }).Where(sme => sme.sm != null);
                var smSMESValue = smSME.Join(sValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new SMEWithValue { sm = sme.sm, sme = sme.sme, value = v.Value });
                var smSMEIValue = smSME.Join(iValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new SMEWithValue { sm = sme.sm, sme = sme.sme, value = v.Value.ToString() });
                var smSMEDValue = smSME.Join(dValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new SMEWithValue { sm = sme.sm, sme = sme.sme, value = v.Value.ToString() });
                var smSMEOValue = smSME.Join(oValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new SMEWithValue { sm = sme.sm, sme = sme.sme, value = (string) v.Value });
                valueSMESM = smSMESValue.Union(smSMEIValue).Union(smSMEDValue).Union(smSMEOValue);
            }
            else // down-top
            {
                var sValueSME = sValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value });
                var iValueSME = iValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() });
                var dValueSME = dValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() });
                var oValueSME = oValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = (string) v.Value });
                var xValueSME = sme.Where(sme => !restrictValue && (sme.TValue == null || sme.TValue.Equals(""))).Select(sme => new { sme, value = "" });
                var valueSME = sValueSME.Union(iValueSME).Union(dValueSME).Union(xValueSME).Union(oValueSME);
                valueSMESM = valueSME.Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value }).Where(sme => sme.sm != null);
            }
            return valueSMESM;
        }

        private static List<SMEResult> GetSMEResult(AasContext db, IEnumerable<SMEWithValue> enumerable)
        {
            var shortEnum = enumerable
                .Select(sme => new {
                    smId = sme.sm.Identifier ?? string.Empty,
                    idShort = sme.sme.IdShort,
                    parentSMEId = sme.sme.ParentSMEId,
                    timeStampTree = sme.sme.TimeStampTree,
                    sme.value
                });

            var result = new List<SMEResult>();
            foreach (var sme in shortEnum)
            {
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
                        smId = sme.smId,
                        value = sme.value,
                        idShortPath = path,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sme.smId)}/submodel-elements/{path}",
                        timeStampTree = TimeStamp.TimeStamp.DateTimeToString(sme.timeStampTree)
                    }
                );
            }
            return result;
        }
    }
}