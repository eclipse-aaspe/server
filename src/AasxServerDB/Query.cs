using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using AasxServerDB.Entities;
using AasxServerDB.Result;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;
using System.Linq.Dynamic.Core;
using QueryParserTest;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Newtonsoft.Json.Linq;

namespace AasxServerDB
{
    public class Query
    {
        public static string? ExternalBlazor { get; set; }

        // --------------- API ---------------
        public List<SMResult> SearchSMs(string semanticId = "", string identifier = "", string diff = "", string expression = "")
        {
            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nSearchSMs");

            watch.Restart();
            var query = GetSMs(db, semanticId, identifier, diff, expression);
            Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");

            watch.Restart();
            var result = GetSMResult(query);
            Console.WriteLine("Collect results\tin " + watch.ElapsedMilliseconds + " ms\nSMs found\t" + result.Count + "/" + db.SMSets.Count());

            return result;
        }

        public int CountSMs(string semanticId = "", string identifier = "", string diff = "", string expression = "")
        {
            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nCountSMs");

            watch.Restart();
            var query = GetSMs(db, semanticId, identifier, diff, expression);
            Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");

            watch.Restart();
            var result = query.Count();
            Console.WriteLine("Collect results\tin " + watch.ElapsedMilliseconds + " ms\nSMs found\t" + result + "/" + db.SMSets.Count());

            return result;
        }

        public List<SMEResult> SearchSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nSearchSMEs");

            watch.Restart();
            var query = GetSMEs(db, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");

            watch.Restart();
            var result = GetSMEResult(db, query);
            Console.WriteLine("Collect results\tin " + watch.ElapsedMilliseconds + " ms\nSMEs found\t" + result.Count + "/" + db.SMESets.Count());

            return result;
        }

        public int CountSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nCountSMEs");

            watch.Restart();
            var query = GetSMEs(db, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");

            watch.Restart();
            var result = query.Count();
            Console.WriteLine("Collect results\tin " + watch.ElapsedMilliseconds + " ms\nSMEs found\t" + result + "/" + db.SMESets.Count());

            return result;
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
        private static IQueryable<SMSet> GetSMs(AasContext db, string semanticId = "", string identifier = "", string diffString = "", string expression = "")
        {
            // analyse parameters
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();
            var diff = TimeStamp.TimeStamp.StringToDateTime(diffString);
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withParameters = withSemanticId || withIdentifier || withDiff;
            var withExpression = !expression.IsNullOrEmpty();

            // get data
            if (withExpression && !withParameters)
            {
                // shorten expression
                expression = expression.Replace("$REVERSE", "").Replace("$LOG", "");
                expression = Regex.Replace(expression, @"\s+", string.Empty);

                // init parser
                var countTypePrefix = 0;
                var parser = new ParserWithAST(new Lexer(expression));
                var ast = parser.Parse();

                // combined condition
                var combinedCondition = parser.GenerateSql(ast, "", ref countTypePrefix, "filter");

                // sm condition
                var conditionSM = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel");
                if (conditionSM.IsNullOrEmpty())
                    conditionSM = parser.GenerateSql(ast, "sm.", ref countTypePrefix, "filter");
                conditionSM = conditionSM.Replace("sm.", "");

                // check restrictions
                var restrictSM = !conditionSM.IsNullOrEmpty() && !conditionSM.Equals("true");

                // get data
                if (restrictSM)
                    return db.SMSets.Where(conditionSM);
                else
                    return Enumerable.Empty<SMSet>().AsQueryable();
            }
            else if (!withExpression && withParameters)
            {
                return db.SMSets
                .Where(s =>
                    (!withSemanticId || (s.SemanticId != null && s.SemanticId.Equals(semanticId))) &&
                    (!withIdentifier || (s.Identifier != null && s.Identifier.Equals(identifier))) &&
                    (!withDiff || s.TimeStampTree.CompareTo(diff) > 0));
            }
            return Enumerable.Empty<SMSet>().AsQueryable();
        }

        private static List<SMResult> GetSMResult(IQueryable<SMSet> query)
        {
            var shortEnum = query.Select(sm => new { identifier = sm.Identifier ?? "", sm.TimeStampTree });

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
        private class CombinedResult
        {
            public SMSet? sm;
            public SMESet? sme;
            public string? value;
        }

        private IQueryable<CombinedResult> GetSMEs(AasContext db, string smSemanticId = "", string smIdentifier = "", string semanticId = "",
            string diff = "", string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
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
            var withParameters = withSMSemanticId || withSMIdentifier || withSemanticId || withDiff || withContains || withEqualString || withLower || withUpper;
            var withExpression = !expression.IsNullOrEmpty();

            var test = db.SMSets.Where(sm => sm.Id == 50).Select(sm => new { sm.Id, sm.AASId });

            // direction
            var topDown = true;
            var restrictValue = false;

            // restrict all tables seperate
            IQueryable<SMSet> sm;
            IQueryable<SMESet> sme;
            //IQueryable<OValueSet> oValue;
            IQueryable<SValueSet> sValue;
            IQueryable<IValueSet> iValue;
            IQueryable<DValueSet> dValue;

            // get data
            if (withExpression && !withParameters)
            {
                // direction
                topDown = !expression.Contains("$REVERSE");

                // shorten expression
                expression = expression.Replace("$REVERSE", "").Replace("$LOG", "");
                expression = Regex.Replace(expression, @"\s+", string.Empty);

                // init parser
                var countTypePrefix = 0;
                var parser = new ParserWithAST(new Lexer(expression));
                var ast = parser.Parse();

                // combined condition
                var combinedCondition = parser.GenerateSql(ast, "", ref countTypePrefix, "filter");

                // sm condition
                var conditionSM = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel");
                if (conditionSM.IsNullOrEmpty())
                    conditionSM = parser.GenerateSql(ast, "sm.", ref countTypePrefix, "filter");
                conditionSM = conditionSM.Replace("sm.", "");

                // sme condition
                var conditionSME = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel_elements");
                if (conditionSME.IsNullOrEmpty())
                    conditionSME = parser.GenerateSql(ast, "sme.", ref countTypePrefix, "filter");
                conditionSME = conditionSME.Replace("sme.", "");

                // attribute condition (OValue)?

                // value condition
                var conditionSMEValue = parser.GenerateSql(ast, "sme.value", ref countTypePrefix, "filter");

                // string condition
                var conditionStr = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_str");
                if (conditionStr.IsNullOrEmpty())
                    conditionStr = parser.GenerateSql(ast, "sValue", ref countTypePrefix, "filter");
                if (conditionStr.Equals("true"))
                    conditionStr = "sValue != null";
                conditionStr = conditionStr.Replace("sValue", "Value");

                // num condition
                var conditionNum = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_num");
                if (conditionNum.IsNullOrEmpty())
                    conditionNum = parser.GenerateSql(ast, "mValue", ref countTypePrefix, "filter");
                if (conditionNum.Equals("true"))
                    conditionNum = "mValue != null";
                conditionNum = conditionNum.Replace("mValue", "Value");

                // check restrictions
                var restrictSM = !conditionSM.IsNullOrEmpty() && !conditionSM.Equals("true");
                var restrictSME = !conditionSME.IsNullOrEmpty() && !conditionSME.Equals("true");
                var restrictSVaue = !conditionStr.IsNullOrEmpty() && !conditionStr.Equals("true");
                var restrictNumVaue = !conditionNum.IsNullOrEmpty() && !conditionNum.Equals("true");
                restrictValue = restrictSVaue || restrictNumVaue;

                // restrict all tables seperate 
                sm =     !restrictSM      ? db.SMSets : db.SMSets.Where(conditionSM);
                sme =    !restrictSME     ? db.SMESets : db.SMESets.Where(conditionSME);
                //oValue = Enumerable.Empty<OValueSet>().AsQueryable(); // OValue implementation missing
                sValue = !restrictSVaue   ? db.SValueSets : db.SValueSets.Where(conditionStr);
                iValue = !restrictNumVaue ? db.IValueSets : db.IValueSets.Where(conditionNum);
                dValue = !restrictNumVaue ? db.DValueSets : db.DValueSets.Where(conditionNum);
            }
            else if (!withExpression && withParameters)
            {
                // direction
                topDown = withSMSemanticId || withSMIdentifier || withSemanticId || withDiff;

                // analyse value parameters
                var withText = withContains || withEqualString;
                var withCompare = withLower && withUpper;
                var withNum = withEqualNum || withCompare;
                if (withText && withNum && !withEqualString)
                    return Enumerable.Empty<CombinedResult>().AsQueryable();

                // check restrictions
                var restrictSM = withSMSemanticId || withSMIdentifier;
                var restrictSME = withSemanticId || withDiff;
                restrictValue = withText || withNum;

                // restrict all tables seperate 
                sm =     !restrictSM    ? db.SMSets     : db.SMSets.Where(sm => (!withSMSemanticId || (sm.SemanticId != null && sm.SemanticId.Equals(smSemanticId))) && (!withSMIdentifier || (sm.Identifier != null && sm.Identifier.Equals(smIdentifier))));
                sme =    !restrictSME   ? db.SMESets    : db.SMESets.Where(sme => (!withSemanticId || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) && (!withDiff || sme.TimeStamp.CompareTo(diffDateTime) > 0));
                //oValue = !restrictValue ? db.OValueSets : db.OValueSets.Where(v => withText && v.Value != null && (!withContains || ((string)v.Value).Contains(contains)) && (!withEqualString || ((string)v.Value).Equals(equal)));
                sValue = !restrictValue ? db.SValueSets : db.SValueSets.Where(v => withText && v.Value != null && (!withContains || v.Value.Contains(contains)) && (!withEqualString || v.Value.Equals(equal)));
                iValue = !restrictValue ? db.IValueSets : db.IValueSets.Where(v => withNum && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum)));
                dValue = !restrictValue ? db.DValueSets : db.DValueSets.Where(v => withNum && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum)));
            }
            else
                return Enumerable.Empty<CombinedResult>().AsQueryable();

            // join the restricted tables
            if (topDown) // top-down
            {
                var smSME = sm.Join(sme, sm => sm.Id, sme => sme.SMId, (sm, sme) => new { sm, sme }).Where(sme => sme.sm != null);
                var smSMESValue = smSME.Join(sValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new CombinedResult { sm = sme.sm, sme = sme.sme, value = v.Value });
                var smSMEIValue = smSME.Join(iValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new CombinedResult { sm = sme.sm, sme = sme.sme, value = v.Value.ToString() });
                var smSMEDValue = smSME.Join(dValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new CombinedResult { sm = sme.sm, sme = sme.sme, value = v.Value.ToString() });
                //var smSMEOValue = smSME.Join(oValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new CombinedResult { sm = sme.sm, sme = sme.sme, value = (string)v.Value });
                //.Union(smSMEOValue)
                return smSMESValue.Union(smSMEIValue).Union(smSMEDValue);
            }
            else // down-top
            {
                var sValueSME = sValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value });
                var iValueSME = iValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() });
                var dValueSME = dValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() });
                //var oValueSME = oValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = (string)v.Value });
                //.Union(oValueSME)
                var xValueSME = sme.Where(sme => !restrictValue && (sme.TValue == null || sme.TValue.Equals(""))).Select(sme => new { sme, value = "" });
                var valueSME = sValueSME.Union(iValueSME).Union(dValueSME).Union(xValueSME);
                return valueSME.Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new CombinedResult { sm = sm, sme = sme.sme, value = sme.value }).Where(sme => sme.sm != null);
            }
        }

        private static List<SMEResult> GetSMEResult(AasContext db, IQueryable<CombinedResult> query)
        {
            var shortEnum = query
                .Select(sme => new {
                    smId = sme.sm.Identifier ?? string.Empty,
                    idShort = sme.sme.IdShort,
                    parentSMEId = sme.sme.ParentSMEId,
                    timeStamp = sme.sme.TimeStamp,
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

                result.Add(new SMEResult()
                    {
                        smId = sme.smId,
                        value = sme.value,
                        idShortPath = path,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sme.smId)}/submodel-elements/{path}",
                        timeStamp = TimeStamp.TimeStamp.DateTimeToString(sme.timeStamp)
                    }
                );
            }
            return result;
        }
    }
}