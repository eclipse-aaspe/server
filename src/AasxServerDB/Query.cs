using Extensions;
using System.Globalization;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerDB
{
    // --------------- Result Schema ---------------
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

    // --------------- Query ---------------
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
                    sr.url = GlobalDB.ExternalBlazor + "/submodels/" + sub64;
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
                        r.url = GlobalDB.ExternalBlazor + "/submodels/" + sub64 + "/submodel-elements/" + path;
                        result.Add(r);
                    }
                }
                Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");
            }
            return result;
        }

        public List<SmeResult> SearchSMEsResult(
            string submodelSemanticId = "",
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
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            ParentSme = sme.ParentSMENum,
                            Value = v.Value
                        }
                    )
                    .Where(s =>
                        (searchSemanticId != "" && s.SemanticId == searchSemanticId) ||
                        (searchIdShort != "" && s.Idshort == searchIdShort)
                    )
                    .Join(db.SubmodelSets,
                        v => v.SubmodelNum,
                        s => s.SubmodelNum,
                        (v, s) => new
                        {
                            SubmodelNum = s.SubmodelNum,
                            SemanticId = s.SemanticId,
                            ParentSme = v.ParentSme,
                            Value = v.Value
                        }
                    )
                    .Where(s =>
                        submodelSemanticId == "" || s.SemanticId == submodelSemanticId
                    )
                    .ToList();

                list.AddRange(db.IValueSets.Where(v =>
                    (withEqual && withI && v.Value == iEqual)
                    )
                    .Join(db.SMESets,
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            ParentSme = sme.ParentSMENum,
                            Value = v.Value.ToString()
                        }
                    )
                    .Where(s =>
                        (searchSemanticId != "" && s.SemanticId == searchSemanticId) ||
                        (searchIdShort != "" && s.Idshort == searchIdShort)
                    )
                    .Join(db.SubmodelSets,
                        v => v.SubmodelNum,
                        s => s.SubmodelNum,
                        (v, s) => new
                        {
                            SubmodelNum = s.SubmodelNum,
                            SemanticId = s.SemanticId,
                            ParentSme = v.ParentSme,
                            Value = v.Value
                        }
                    )
                    .Where(s =>
                        submodelSemanticId == "" || s.SemanticId == submodelSemanticId
                    )
                    .ToList());

                list.AddRange(db.DValueSets.Where(v =>
                    (withEqual && withF && v.Value == fEqual)
                    )
                    .Join(db.SMESets,
                        v => v.ParentSMENum,
                        sme => sme.SMENum,
                        (v, sme) => new
                        {
                            SemanticId = sme.SemanticId,
                            Idshort = sme.Idshort,
                            SubmodelNum = sme.SubmodelNum,
                            ParentSme = sme.ParentSMENum,
                            Value = v.Value.ToString()
                        }
                    )
                    .Where(s =>
                        (searchSemanticId != "" && s.SemanticId == searchSemanticId) ||
                        (searchIdShort != "" && s.Idshort == searchIdShort)
                    )
                    .Join(db.SubmodelSets,
                        v => v.SubmodelNum,
                        s => s.SubmodelNum,
                        (v, s) => new
                        {
                            SubmodelNum = s.SubmodelNum,
                            SemanticId = s.SemanticId,
                            ParentSme = v.ParentSme,
                            Value = v.Value
                        }
                    )
                    .Where(s =>
                        submodelSemanticId == "" || s.SemanticId == submodelSemanticId
                    )
                    .ToList());

                Console.WriteLine("Found " + list.Count() + " SMEs in " + watch.ElapsedMilliseconds + "ms");

                var hSubmodel = new HashSet<long>();
                var lParentParentNum = new List<long>();
                foreach (var l in list)
                {
                    hSubmodel.Add(l.SubmodelNum);
                    var smeDB = db.SMESets.Where(s => s.SMENum == l.ParentSme).First();
                    lParentParentNum.Add(smeDB.ParentSMENum);
                }

                Console.WriteLine("Found " + hSubmodel.Count() + " Submodels");

                watch.Restart();

                var smeResult = db.SMESets.Where(s =>
                    hSubmodel.Contains(s.SubmodelNum) &&
                    ((resultSemanticId != "" && s.SemanticId == resultSemanticId) ||
                    (resultIdShort != "" && s.Idshort == resultIdShort))
                    )
                    .ToList();

                if (equal == "")
                    equal = contains;

                foreach (var l in smeResult)
                {
                    SmeResult r = new SmeResult();
                    bool found = false;

                    var submodelDB = db.SubmodelSets.Where(s => s.SubmodelNum == l.SubmodelNum).First();
                    if (submodelDB != null && (submodelSemanticId == "" || submodelDB.SemanticId == submodelSemanticId))
                    {
                        r.value = equal;
                        r.url = "";
                        r.submodelId = submodelDB.SubmodelId;
                        string path = l.Idshort;
                        long pnum = l.ParentSMENum;
                        while (pnum != 0)
                        {
                            var smeDB = db.SMESets.Where(s => s.SMENum == pnum).First();
                            path = smeDB.Idshort + "." + path;
                            pnum = smeDB.ParentSMENum;
                            if (lParentParentNum.Contains(pnum))
                            {
                                found = true;
                                if (l.SMEType == "F")
                                {
                                    var v = db.SValueSets.Where(v => v.ParentSMENum == l.SMENum).FirstOrDefault();
                                    if (v.Value.ToLower().StartsWith("http"))
                                        r.url = v.Value;
                                }
                            }
                        }
                        r.idShortPath = path;
                        string sub64 = Base64UrlEncoder.Encode(r.submodelId);
                        if (r.url == "")
                            r.url = GlobalDB.ExternalBlazor + "/submodels/" + sub64 + "/submodel-elements/" + path + "/attachment";
                        if (found)
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
}
