using AasxServerDB.Entities;
using AasxServerDB.Result;
using Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerDB
{
    public class Query
    {
        public static string ExternalBlazor { get; set; }

        // --------------- API ---------------
        public List<SMResult> SearchSMs(string semanticId = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMs");
            Console.WriteLine("Total number of SMs " + (new AasContext()).SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var smList = GetSMSet(semanticId);
            Console.WriteLine("Found " + smList.Count() + " SM in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMResult(smList);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public int CountSMs(string semanticId = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("CountSMs");
            Console.WriteLine("Total number of SMs " + new AasContext().SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var smList = GetSMSet(semanticId);
            var count = smList.Count();
            Console.WriteLine("Found " + count + " SM in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEs(
            string smSemanticId = "", string semanticId = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var smeWithValue = GetSMEWithValue(smSemanticId, semanticId, contains, equal, lower, upper);
            Console.WriteLine("Found " + smeWithValue.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMEResult(smeWithValue);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }
        
        public int CountSMEs(
            string smSemanticId = "", string semanticId = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("CountSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var smeWithValue = GetSMEWithValue(smSemanticId, semanticId, contains, equal, lower, upper);
            var count = smeWithValue.Count();
            Console.WriteLine("Found " + count + " SMEs in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEsResult(
            string smSemanticId = "", 
            string searchSemanticId = "",  string searchIdShort = "",
            string equal = "", string contains = "",
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
                        r.url = "";
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
                                if (l.SMEType == "F")
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
        private List<SMSet> GetSMSet(string semanticId = "")
        {
            if (semanticId.IsNullOrEmpty())
                return new List<SMSet>();
            return new AasContext().SMSets
                .Where(s => s.SemanticId != null && s.SemanticId.Equals(semanticId))
                .ToList();
        }

        private List<SMResult> GetSMResult(List<SMSet> smList)
        {
            return smList.ConvertAll(
                sm =>
                {
                    string identifier = (sm != null && !sm.Identifier.IsNullOrEmpty()) ? sm.Identifier : string.Empty;
                    return new SMResult()
                    {
                        smId = identifier,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(identifier)}"
                    };
                }
            );
        }

        // --------------- SME Methodes ---------------
        private class SMEWithValue
        {
            public SMSet sm;
            public SMESet? sme;
            public string? value;
        }

        private List<SMEWithValue> GetSMEWithValue( string smSemanticId = "", string semanticId = "", string contains = "", string equal = "", string lower = "", string upper = "")
        {
            bool withContains = !contains.IsNullOrEmpty();
            bool withEquals = !equal.IsNullOrEmpty();
            bool withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            bool withOneOperation =
                (withContains && !withEquals && !withCompare) ||
                (!withContains && withEquals && !withCompare) ||
                (!withContains && !withEquals && withCompare);
            var result = new List<SMEWithValue>();
            if (withOneOperation)
            {
                GetSValue(ref result, semanticId, contains, equal);
                GetIValue(ref result, semanticId, equal, lower, upper);
                GetDValue(ref result, semanticId, equal, lower, upper);
                SelectSM(ref result, smSemanticId);
            }
            return result;
        }

        private void GetSValue(ref List<SMEWithValue> smeValue, string semanticId = "", string contains = "", string equal = "")
        {
            bool withSME = !semanticId.IsNullOrEmpty();
            bool withContains = !contains.IsNullOrEmpty();
            bool withEqual = !equal.IsNullOrEmpty();
            if (!withContains && !withEqual)
                return;

            using AasContext db = new();
            smeValue.AddRange(db.SValueSets
                .Where(v => v.Value != null &&
                    (!withContains || v.Value.Contains(contains)) &&
                    (!withEqual || v.Value.Equals(equal)))
                .Join(
                    (db.SMESets.Where(sme => !withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId)))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value })
                .ToList());
        }

        private void GetIValue(ref List<SMEWithValue> smeValue, string semanticId = "", string equal = "", string lower = "", string upper = "")
        {
            bool withSME = !semanticId.IsNullOrEmpty();
            bool withEqual = !equal.IsNullOrEmpty();
            bool withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withEqual && !withCompare)
                return;

            long iEqual = 0;
            long iLower = 0;
            long iUpper = 0;
            try
            {
                if (withEqual)
                    iEqual = Convert.ToInt64(equal);
                else if (withCompare)
                {
                    iLower = Convert.ToInt64(lower);
                    iUpper = Convert.ToInt64(upper);
                }
                else
                    return;
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
                    (db.SMESets.Where(sme => !withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId)))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() })
                .ToList());
        }
        
        private void GetDValue(ref List<SMEWithValue> smeValue, string semanticId = "", string equal = "", string lower = "", string upper = "")
        {
            bool withSME = !semanticId.IsNullOrEmpty();
            bool withEqual = !equal.IsNullOrEmpty();
            bool withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withEqual && !withCompare)
                return;

            long dEqual = 0;
            long dLower = 0;
            long dUpper = 0;
            try
            {
                if (withEqual)
                    dEqual = Convert.ToInt64(equal);
                else if (withCompare)
                {
                    dLower = Convert.ToInt64(lower);
                    dUpper = Convert.ToInt64(upper);
                }
                else
                    return;
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
                    (db.SMESets.Where(sme => !withSME || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId)))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() })
                .ToList());
        }

        private void SelectSM(ref List<SMEWithValue> smeValue, string semanticId = "")
        {
            bool withSM = !semanticId.IsNullOrEmpty();
            using AasContext db = new();
            smeValue = smeValue
                .Join((db.SMSets.Where(sm => !withSM || (sm.SemanticId != null && sm.SemanticId.Equals(semanticId)))),
                    sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new SMEWithValue { sm = sm, sme = sme.sme, value = sme.value })
                .Where(sme => sme.sm != null)
                .ToList();
        }

        private List<SMEResult> GetSMEResult(List<SMEWithValue> smeList)
        {
            using AasContext db = new();
            return smeList.ConvertAll(
                sme =>
                {
                    string identifier = (sme != null && sme.sm.Identifier != null) ? sme.sm.Identifier : "";
                    string path = sme.sme.IdShort;
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
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(identifier)}/submodel-elements/{path}"
                    };
                }
            );
        }
    }
}
