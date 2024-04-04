using Extensions;
using System.Globalization;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerDB
{
    // --------------- Result Schema ---------------
    public class SmResult
    {
        public string smId { get; set; }
        public string url { get; set; }
    }
    public class SmeResult
    {
        public string smId { get; set; }
        public string idShortPath { get; set; }
        public string value { get; set; }
        public string url { get; set; }
    }

    // --------------- Query ---------------
    public class Query
    {
        public static string ExternalBlazor { get; set; }

        public List<SmResult> SearchSMs(string semanticId)
        {
            List<SmResult> list = new List<SmResult>();
            using (AasContext db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Console.WriteLine();
                Console.WriteLine("SearchSubmodels");
                Console.WriteLine("Submodels " + db.SMSets.Count());

                var subList = db.SMSets.Where(s => s.SemanticId == semanticId).ToList();
                Console.WriteLine("Found " + subList.Count() + " Submodels in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                foreach (var submodel in subList)
                {
                    var sr = new SmResult();
                    sr.smId = submodel.IdIdentifier;
                    string sub64 = Base64UrlEncoder.Encode(sr.smId);
                    sr.url = ExternalBlazor + "/submodels/" + sub64;
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
            string smSemanticId = "", string semanticId = "",
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
                    (withEqual && v.Value == equal) ||
                    semanticId != ""
                    )
                    .Join(db.SMESets,
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            Id = sme.Id,
                            Value = v.Value.ToString(),
                            ParentSMEId = sme.ParentSMEId,
                            SMId = sme.SMId
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .ToList();

                list.AddRange(db.IValueSets.Where(v =>
                    (withEqual && withI && v.Value == iEqual) ||
                    (withCompare && withI && v.Value >= iLower && v.Value <= iUpper) ||
                    semanticId != ""
                    )
                    .Join(db.SMESets,
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            Id = sme.Id,
                            Value = v.Value.ToString(),
                            ParentSMEId = sme.ParentSMEId,
                            SMId = sme.SMId
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .ToList());

                list.AddRange(db.DValueSets.Where(v =>
                    (withEqual && withF && v.Value == fEqual) ||
                    (withCompare && withF && v.Value >= fLower && v.Value <= fUpper) ||
                    semanticId != ""
                    )
                    .Join(db.SMESets,
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            Id = sme.Id,
                            Value = v.Value.ToString(),
                            ParentSMEId = sme.ParentSMEId,
                            SMId = sme.SMId
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .ToList());

                Console.WriteLine("Found " + list.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                foreach (var l in list)
                {
                    SmeResult r = new SmeResult();

                    var submodelDBList = db.SMSets.Where(s => s.Id == l.SMId);
                    if (submodelDBList.Count() != 0)
                    {
                        var submodelDB = submodelDBList.First();
                        if (submodelDB == null || (smSemanticId != "" && submodelDB.SemanticId != smSemanticId))
                            continue;
                        r.smId = submodelDB.IdIdentifier;
                        r.value = l.Value;
                        string path = l.IdShort;
                        int? pId = l.ParentSMEId;
                        while (pId != null)
                        {
                            var smeDB = db.SMESets.Where(s => s.Id == pId).First();
                            path = smeDB.IdShort + "." + path;
                            pId = smeDB.ParentSMEId;
                        }
                        r.idShortPath = path;
                        string sub64 = Base64UrlEncoder.Encode(r.smId);
                        r.url = ExternalBlazor + "/submodels/" + sub64 + "/submodel-elements/" + path;
                        result.Add(r);
                    }
                }
                Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");
            }
            return result;
        }

        public List<SmeResult> SearchSMEsResult(
            string smSemanticId = "",
            string searchSemanticId = "",
            string searchIdShort = "",
            string equal = "",
            string contains = "",
            string resultSemanticId = "",
            string resultIdShort = ""
            )
        {
            List<SmeResult> result = new List<SmeResult>();
            
            if (searchSemanticId == "" && searchIdShort == "")
                return result;
            if (equal == "" && contains == "")
                return result;
            if (resultSemanticId == "" && resultIdShort == "")
                return result;

            bool withI = false;
            long iEqual = 0;
            bool withF = false;
            double fEqual = 0;
            try
            {
                if (equal != "")
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

                bool withContains = (contains != "");
                bool withEqual = !withContains && (equal != "");

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
                        (searchSemanticId != "" && s.SemanticId == searchSemanticId) ||
                        (searchIdShort != "" && s.IdShort == searchIdShort)
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
                        (searchSemanticId != "" && s.SemanticId == searchSemanticId) ||
                        (searchIdShort != "" && s.IdShort == searchIdShort)
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
                        (searchSemanticId != "" && s.SemanticId == searchSemanticId) ||
                        (searchIdShort != "" && s.IdShort == searchIdShort)
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

                var smeResult = db.SMESets.Where(s =>
                    hSubmodel.Contains(s.SMId) &&
                    ((resultSemanticId != "" && s.SemanticId == resultSemanticId) ||
                    (resultIdShort != "" && s.IdShort == resultIdShort))
                    )
                    .ToList();

                if (equal == "")
                    equal = contains;

                foreach (var l in smeResult)
                {
                    SmeResult r = new SmeResult();
                    bool found = false;

                    var submodelDB = db.SMSets.Where(s => s.Id == l.SMId).First();
                    if (submodelDB != null && (smSemanticId == "" || submodelDB.SemanticId == smSemanticId))
                    {
                        r.value = equal;
                        r.url = "";
                        r.smId = submodelDB.IdIdentifier;
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
                        if (r.url == "")
                            r.url = ExternalBlazor + "/submodels/" + sub64 + "/submodel-elements/" + path + "/attachment";
                        if (found)
                            result.Add(r);
                    }
                }
                Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");
            }

            return result;
        }

        public int CountSMEs(
            string smSemanticId = "", string semanticId = "",
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
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            Id = sme.Id,
                            Value = v.Value.ToString(),
                            ParentSMEId = sme.ParentSMEId
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .Count();

                c += db.IValueSets.Where(v =>
                    (withEqual && withI && v.Value == iEqual) ||
                    (withCompare && withI && v.Value >= iLower && v.Value <= iUpper)
                    )
                    .Join(db.SMESets,
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            Id = sme.Id,
                            Value = v.Value.ToString(),
                            ParentSMEId = sme.ParentSMEId
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .Count();

                 c += db.DValueSets.Where(v =>
                    (withEqual && withF && v.Value == fEqual) ||
                    (withCompare && withF && v.Value >= fLower && v.Value <= fUpper)
                    )
                    .Join(db.SMESets,
                        v => v.SMEId,
                        sme => sme.Id,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            IdShort = sme.IdShort,
                            Id = sme.Id,
                            Value = v.Value.ToString(),
                            ParentSMEId = sme.ParentSMEId
                        }
                    )
                    .Where(s => semanticId == "" || s.SemanticId == semanticId)
                    .Count();

                Console.WriteLine("Count " + c + " SMEs in " + watch.ElapsedMilliseconds + "ms");
            }

            return c;
        }
    }
}
