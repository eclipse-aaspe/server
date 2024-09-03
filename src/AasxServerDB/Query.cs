using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using AasxServerDB.Entities;
using AasxServerDB.Result;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using TimeStamp;
using System.Text.RegularExpressions;
using System.Linq.Dynamic.Core;
using QueryParserTest;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Newtonsoft.Json.Linq;
using System.Linq;

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
            var enumerable = GetSMs(db, semanticId, identifier, diff);
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
            var enumerable = GetSMs(db, semanticId, identifier, diff);
            var count = enumerable.Count();
            Console.WriteLine("Found " + count + " SM in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            using AasContext db = new();

            var watch = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMEs(db, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            Console.WriteLine("SMEs found in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMEResult(db, enumerable);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;
        }

        public int CountSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            using AasContext db = new();

            var watch = Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("CountSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var enumerable = GetSMEs(db, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            var count = enumerable.Count();
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
        private static IEnumerable<SMSet> GetSMs(AasContext db, string semanticId = "", string identifier = "", string diffString = "")
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
        private class CombinedResult
        {
            public SMSet? sm;
            public SMESet? sme;
            public string? value;
        }

        private IEnumerable<CombinedResult> GetSMEs(AasContext db, string smSemanticId = "", string smIdentifier = "", string semanticId = "",
            string diff = "", string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            // analyse parameters
            var diffDateTime = TimeStamp.TimeStamp.StringToDateTime(diff);
            var withParameters = !smSemanticId.IsNullOrEmpty() || !smIdentifier.IsNullOrEmpty() ||
                !semanticId.IsNullOrEmpty() || !diffDateTime.Equals(DateTime.MinValue) ||
                !contains.IsNullOrEmpty() || !equal.IsNullOrEmpty() ||
                Int64.TryParse(lower, out long lowerNum) || Int64.TryParse(upper, out long upperNum);
            var withExpression = !expression.IsNullOrEmpty();

            // get data
            IEnumerable<CombinedResult> result;
            if (withExpression && !withParameters)
                result = GetSMEsWithExpression(db, expression);
            else if (!withExpression && withParameters)
                result = GetSMEsWithParameters(db, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
            else
                result = new List<CombinedResult>();
            return result;
        }

        private IEnumerable<CombinedResult> GetSMEsWithExpression(AasContext db, string expression = "")
        {
            var reverse = expression.Contains("$REVERSE");
            var withLog = expression.Contains("$LOG");

            // shorten expression
            expression = expression.Replace("$REVERSE", "").Replace("$LOG", "");
            expression = Regex.Replace(expression, @"\s+", string.Empty);

            // init parser
            var countTypePrefix = 0;
            var parser = new ParserWithAST(new Lexer(expression));
            var ast = parser.Parse();

            // combined condition
            var combinedCondition = parser.GenerateSql(ast, "", ref countTypePrefix, "filter");
            Console.WriteLine("combinedCondition: " + combinedCondition);

            // SM condition
            var conditionSM = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel");
            if (conditionSM.IsNullOrEmpty())
                conditionSM = parser.GenerateSql(ast, "sm.", ref countTypePrefix, "filter");
            conditionSM = conditionSM.Replace("sm.", "");
            Console.WriteLine("condition sm. #" + countTypePrefix + ": " + conditionSM);

            // SME condition
            var conditionSME = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel_elements");
            if (conditionSME.IsNullOrEmpty())
                conditionSME = parser.GenerateSql(ast, "sme.", ref countTypePrefix, "filter");
            conditionSME = conditionSME.Replace("sme.", "");
            Console.WriteLine("condition sme. #" + countTypePrefix + ": " + conditionSME);

            // Value condition
            var conditionSMEValue = parser.GenerateSql(ast, "sme.value", ref countTypePrefix, "filter");
            Console.WriteLine("condition sme.value #" + countTypePrefix + ": " + conditionSMEValue);

            // String condition
            var conditionStr = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_str");
            if (conditionStr.IsNullOrEmpty())
                conditionStr = parser.GenerateSql(ast, "sValue", ref countTypePrefix, "filter");
            if (conditionStr.Equals("true"))
                conditionStr = "sValue != null";
            conditionStr = conditionStr.Replace("sValue", "Value");
            Console.WriteLine("condition sValue #" + countTypePrefix + ": " + conditionStr);

            // Num condition
            var conditionNum = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_num");
            if (conditionNum.IsNullOrEmpty())
                conditionNum = parser.GenerateSql(ast, "mValue", ref countTypePrefix, "filter");
            if (conditionNum.Equals("true"))
                conditionNum = "mValue != null";
            conditionNum = conditionNum.Replace("mValue", "Value");
            Console.WriteLine("condition mValue #" + countTypePrefix + ": " + conditionNum);

            // dynamic condition
            var querySM = db.SMSets.AsQueryable();
            var querySME = db.SMESets.AsQueryable();
            var querySValue = db.SValueSets.AsQueryable();
            var queryIValue = db.IValueSets.AsQueryable();
            var queryDValue = db.DValueSets.AsQueryable();

            // get data
            IEnumerable<CombinedResult> result;
            if (!reverse) // top-down
            {

                /*var querySMWhere = querySM;
                if (conditionSM != "" && conditionSM != "true")
                {
                    querySMWhere = querySMWhere.Where(conditionSM);
                }

                var querySMWhere = querySM;
                if (!conditionSM.IsNullOrEmpty() && !conditionSM.Equals("true"))
                    querySMWhere = querySMWhere.Where(conditionSM);
                
                if (conditionSME == "" && conditionStr == "" && conditionNum == "" && smeSet == null)
                {
                    smSet = querySMWhere.Distinct().ToList();
                    return;
                }

                var querySMEWhere = querySME;
                if (conditionSME != "" && conditionSME != "true")
                {
                    querySMEWhere = querySMEWhere.Where(conditionSME);
                }
                var querySMandSME = querySMWhere
                    .Join(
                        querySMEWhere,
                        sm => sm.Id,
                        sme => sme.SMId,
                        (sm, sme) => new { sm, sme }
                    );
                var sLog = "";
                if (withLog)
                    sLog = "Found " + querySMandSME.Count();

                if (smeSet == null)
                {
                    if (smSet != null)
                    {
                        smSet = querySMandSME.Select(result => result.sm).Distinct().ToList();
                    }

                    return;
                }

                var querySValueWhere = querySValue;
                if (conditionStr != "" && conditionStr != "true")
                {
                    querySValueWhere = querySValue.Where(conditionStr);
                }
                var querySMandSMEandSValue = querySMandSME
                    .Join(
                        querySValueWhere,
                        combined => combined.sme.Id,
                        sValue => sValue.SMEId,
                        (combined, sValue) => new CombinedResult { sm = combined.sm, sme = combined.sme, sValue = sValue.Value, mValue = null }
                    );
                sLog = "";
                if (withLog)
                    sLog = "Found " + querySMandSMEandSValue.Count();

                var queryIValueWhere = queryIValue;
                if (conditionNum != "" && conditionNum != "true")
                {
                    queryIValueWhere = queryIValue.Where(conditionNum);
                }
                var querySMandSMEandIValue = querySMandSME
                    .Join(
                        queryIValueWhere,
                        combined => combined.sme.Id,
                        iValue => iValue.SMEId,
                        (combined, iValue) => new CombinedResult { sm = combined.sm, sme = combined.sme, sValue = null, mValue = iValue.Value }
                    );
                sLog = "";
                if (withLog)
                    sLog = "Found " + querySMandSMEandIValue.Count();

                var queryDValueWhere = queryDValue;
                if (conditionNum != "" && conditionNum != "true")
                {
                    queryDValueWhere = queryDValue.Where(conditionNum);
                }
                var querySMandSMEandDValue = querySMandSME
                    .Join(
                        queryDValueWhere,
                        combined => combined.sme.Id,
                        dValue => dValue.SMEId,
                        (combined, dValue) => new CombinedResult { sm = combined.sm, sme = combined.sme, sValue = null, mValue = dValue.Value }
                    );
                sLog = "";
                if (withLog)
                    sLog = "Found " + querySMandSMEandDValue.Count();

                var queryUnion = querySMandSMEandSValue.Union(querySMandSMEandIValue).Union(querySMandSMEandDValue).Distinct();

                var query = queryUnion
                    .Where(combinedCondition)
                    .Distinct();
                sLog = "";
                if (withLog)
                    sLog = "Found " + query.Count();

                if (smSet != null)
                {
                    smSet = query.Select(result => result.sm).Distinct().ToList();
                }

                if (smeSet != null)
                {
                    result = query
                        .Select(result => new CombinedResult
                        {
                            sm = result.sm,
                            sme = result.sme,
                            value = result.sValue ?? result.mValue.ToString()
                        })
                        .Distinct();
                }*/
            }
            else // down-top
            {
            }
            result = new List<CombinedResult>();
            return result;
        }

        private IEnumerable<CombinedResult> GetSMEsWithParameters(AasContext db, string smSemanticId = "", string smIdentifier = "", string semanticId = "",
            string diff = "", string contains = "", string equal = "", string lower = "", string upper = "")
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
                return new List<CombinedResult>();

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
                        (!withContains || ((string)v.Value).Contains(contains)) &&
                        (!withEqualString || ((string)v.Value).Equals(equal))));
            var sme = db.SMESets
                .Where(sme =>
                    !restrictSME ||
                    (restrictSME &&
                    (!withSemanticId || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                    (!withDiff || sme.TimeStamp.CompareTo(diffDateTime) > 0)));
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

            // top-down
            IEnumerable<CombinedResult> result;
            if (withSMSemanticId || withSMIdentifier || withSemanticId || withDiff)
            {
                var smSME = sm.Join(sme, sm => sm.Id, sme => sme.SMId, (sm, sme) => new { sm, sme }).Where(sme => sme.sm != null);
                var smSMESValue = smSME.Join(sValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new CombinedResult { sm = sme.sm, sme = sme.sme, value = v.Value });
                var smSMEIValue = smSME.Join(iValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new CombinedResult { sm = sme.sm, sme = sme.sme, value = v.Value.ToString() });
                var smSMEDValue = smSME.Join(dValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new CombinedResult { sm = sme.sm, sme = sme.sme, value = v.Value.ToString() });
                var smSMEOValue = smSME.Join(oValue, sme => sme.sme.Id, v => v.SMEId, (sme, v) => new CombinedResult { sm = sme.sm, sme = sme.sme, value = (string)v.Value });
                result = smSMESValue.Union(smSMEIValue).Union(smSMEDValue).Union(smSMEOValue);
            }
            else // down-top
            {
                var sValueSME = sValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value });
                var iValueSME = iValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() });
                var dValueSME = dValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = v.Value.ToString() });
                var oValueSME = oValue.Join(sme, v => v.SMEId, sme => sme.Id, (v, sme) => new { sme, value = (string)v.Value });
                var xValueSME = sme.Where(sme => !restrictValue && (sme.TValue == null || sme.TValue.Equals(""))).Select(sme => new { sme, value = "" });
                var valueSME = sValueSME.Union(iValueSME).Union(dValueSME).Union(xValueSME).Union(oValueSME);
                result = valueSME.Join(sm, sme => sme.sme.SMId, sm => sm.Id, (sme, sm) => new CombinedResult { sm = sm, sme = sme.sme, value = sme.value }).Where(sme => sme.sm != null);
            }
            return result;
        }

        private static List<SMEResult> GetSMEResult(AasContext db, IEnumerable<CombinedResult> enumerable)
        {
            var shortEnum = enumerable
                .Select(sme => new {
                    smId = sme.sm.Identifier ?? string.Empty,
                    idShort = sme.sme.IdShort,
                    parentSMEId = sme.sme.ParentSMEId,
                    timeStamp= sme.sme.TimeStamp,
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
                        timeStamp = TimeStamp.TimeStamp.DateTimeToString(sme.timeStamp)
                    }
                );
            }
            return result;
        }
    }
}