namespace AasxServerDB
{
    using System.Diagnostics;
    using Microsoft.EntityFrameworkCore;
    using AasxServerDB.Entities;
    using AasxServerDB.Result;
    using Extensions;
    using Microsoft.IdentityModel.Tokens;
    using System.Text.RegularExpressions;
    using System.Linq.Dynamic.Core;
    using QueryParserTest;
    using System.Linq;
    using System.Security.AccessControl;

    public class Query
    {
        public static string? ExternalBlazor { get; set; }

        // --------------- Help Class ---------------
        private class CombinedSMResult
        {
            public string? Identifier { get; set; }
            public DateTime TimeStampTree { get; set; }
        }

        private class CombinedSMEResult
        {
            public int Id { get; set; }
            public string? Identifier { get; set; }
            public int? ParentSMEId { get; set; }
            public string? IdShort { get; set; }
            public DateTime TimeStamp { get; set; }
            public string? Value { get; set; }
        }

        // --------------- API ---------------
        public List<SMResult> SearchSMs(string semanticId = "", string identifier = "", string diff = "", string expression = "")
        {
            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nSearchSMs");

            watch.Restart();
            var query = GetSMs(db, semanticId, identifier, diff, expression);
            if (query == null)
            {
                Console.WriteLine("No query can be generated due to incorrect parameter combinations");
                return new List<SMResult>();
            }
            else
            {
                Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");
            }

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
            if (query == null)
            {
                Console.WriteLine("No query can be generated due to incorrect parameter combinations");
                return 0;
            }
            else
            {
                Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");
            }

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
            if (query == null)
            {
                Console.WriteLine("No query can be generated due to incorrect parameter combinations");
                return new List<SMEResult>();
            }
            else
            {
                Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");
            }

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
            if (query == null)
            {
                Console.WriteLine("No query can be generated due to incorrect parameter combinations");
                return 0;
            }
            else
            {
                Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");
            }

            watch.Restart();
            var result = query.Count();
            Console.WriteLine("Collect results\tin " + watch.ElapsedMilliseconds + " ms\nSMEs found\t" + result + "/" + db.SMESets.Count());

            return result;
        }

        public List<SMEResult> SearchSMEsResult(
            string smSemanticId = "",
            string searchSemanticId = "", string searchIdShort = "",
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
        private static IQueryable<CombinedSMResult>? GetSMs(AasContext db, string semanticId = "", string identifier = "", string diffString = "", string expression = "")
        {
            // analyse parameters
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();
            var diff = TimeStamp.TimeStamp.StringToDateTime(diffString);
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withParameters = withSemanticId || withIdentifier || withDiff;
            var withExpression = !expression.IsNullOrEmpty();

            // wrong parameters
            if (withExpression == withParameters)
                return null;

            // get data
            IQueryable<SMSet> smTable;
            if (withExpression) // with expression
            {
                // shorten expression
                expression = expression.Replace("$REVERSE", "").Replace("$LOG", "");
                expression = Regex.Replace(expression, @"\s+", replacement: string.Empty);

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
                if (!restrictSM)
                    return null;
                smTable = db.SMSets.Where(conditionSM);
            }
            else // with parameters
            {
                smTable = db.SMSets
                    .Where(s =>
                        (!withSemanticId || (s.SemanticId != null && s.SemanticId.Equals(semanticId))) &&
                        (!withIdentifier || (s.Identifier != null && s.Identifier.Equals(identifier))) &&
                        (!withDiff || s.TimeStampTree.CompareTo(diff) > 0));
            }

            // set select
            var smSelect = smTable.Select(sm => new { sm.Identifier, sm.TimeStampTree });

            // to query string
            var smQueryString = smSelect.ToQueryString();

            // set parameters
            smQueryString = SetParameter(smQueryString);

            // change INSTR to LIKE
            smQueryString = withExpression ? ChangeINSTRToLIKE(smQueryString) : smQueryString;

            // create queryable
            var result = db.Database.SqlQueryRaw<CombinedSMResult>(smQueryString);
            return result;
        }

        private static List<SMResult> GetSMResult(IQueryable<CombinedSMResult> query)
        {
            var result = new List<SMResult>();
            foreach (var sm in query)
            {
                result.Add(
                    new SMResult
                    {
                        smId = sm.Identifier ?? string.Empty,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm.Identifier ?? string.Empty)}",
                        timeStampTree = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree)
                    }
                );
            }
            return result;
        }

        // --------------- SME Methodes ---------------
        private static IQueryable<CombinedSMEResult>? GetSMEs(AasContext db, string smSemanticId = "", string smIdentifier = "", string semanticId = "",
            string diffString = "", string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            // analyse parameters
            var withSMSemanticId = !smSemanticId.IsNullOrEmpty();
            var withSMIdentifier = !smIdentifier.IsNullOrEmpty();
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var diff = TimeStamp.TimeStamp.StringToDateTime(diffString);
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqualString = !equal.IsNullOrEmpty();
            var withEqualNum = Int64.TryParse(equal, out var equalNum);
            var withLower = Int64.TryParse(lower, out var lowerNum);
            var withUpper = Int64.TryParse(upper, out var upperNum);
            var withCompare = withLower && withUpper;
            var withParameters = withSMSemanticId || withSMIdentifier || withSemanticId || withDiff || withContains || withEqualString || withLower || withUpper;
            var withExpression = !expression.IsNullOrEmpty();

            // wrong parameters
            if (withExpression == withParameters ||
                (withEqualString && (withContains || withLower || withUpper)) ||
                (withContains && (withLower || withUpper)) ||
                (withLower != withUpper))
                return null;

            // direction
            var direction = 0; // 0 = top-down, 1 = middle-out, 2 = bottom-up

            // implementierung OValueSet

            // restrict all tables seperate
            IQueryable<SMSet> smTable;
            IQueryable<SMESet> smeTable;
            IQueryable<SValueSet>? sValueTable;
            IQueryable<IValueSet>? iValueTable;
            IQueryable<DValueSet>? dValueTable;

            // get data
            if (withExpression) // with expression
            {
                // direction
                direction = !expression.Contains("$REVERSE") ? 0 : 2;

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
                var restrictValue = restrictSVaue || restrictNumVaue;

                // restrict all tables seperate 
                smTable = restrictSM ? db.SMSets.Where(conditionSM) : db.SMSets;
                smeTable = restrictSME ? db.SMESets.Where(conditionSME) : db.SMESets;
                sValueTable = restrictValue ? (restrictSVaue   ? db.SValueSets.Where(conditionStr) : null) : db.SValueSets;
                iValueTable = restrictValue ? (restrictNumVaue ? db.IValueSets.Where(conditionNum) : null) : db.IValueSets;
                dValueTable = restrictValue ? (restrictNumVaue ? db.DValueSets.Where(conditionNum) : null) : db.DValueSets;
            }
            else // with parameters
            {
                // direction
                direction = (withSMSemanticId || withSMIdentifier) ? 0 : ((withContains || withEqualString || withLower || withUpper) ? 2 : 1);

                // check restrictions
                var restrictSM = withSMSemanticId || withSMIdentifier;
                var restrictSME = withSemanticId || withDiff;
                var restrictSValue = withContains || withEqualString;
                var restrictNumValue = withEqualNum || withCompare;
                var restrictValue = restrictSValue || restrictNumValue;

                // restrict all tables seperate 
                smTable = restrictSM ? db.SMSets.Where(sm => (!withSMSemanticId || (sm.SemanticId != null && sm.SemanticId.Equals(smSemanticId))) && (!withSMIdentifier || (sm.Identifier != null && sm.Identifier.Equals(smIdentifier)))) : db.SMSets;
                smeTable = restrictSME ? db.SMESets.Where(sme => (!withSemanticId || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) && (!withDiff || sme.TimeStamp.CompareTo(diff) > 0)) : db.SMESets;
                sValueTable = restrictValue ? (restrictSValue   ? db.SValueSets.Where(v => restrictSValue   && v.Value != null && (!withContains || v.Value.Contains(contains)) && (!withEqualString || v.Value.Equals(equal))) : null) : db.SValueSets;
                iValueTable = restrictValue ? (restrictNumValue ? db.IValueSets.Where(v => restrictNumValue && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))) : null) : db.IValueSets;
                dValueTable = restrictValue ? (restrictNumValue ? db.DValueSets.Where(v => restrictNumValue && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))) : null) : db.DValueSets;
            }

            // set select
            var smSelect = smTable.Select(sm => new { sm.Id, sm.Identifier });
            var smeSelect = smeTable.Select(sme => new { sme.SMId, sme.Id, sme.ParentSMEId, sme.IdShort, sme.TimeStamp, sme.TValue });
            var sValueSelect = sValueTable?.Select(sV => new { sV.SMEId, sV.Value });
            var iValueSelect = iValueTable?.Select(iV => new { iV.SMEId, iV.Value });
            var dValueSelect = dValueTable?.Select(dV => new { dV.SMEId, dV.Value });

            // to query string
            var smQueryString = smSelect.ToQueryString();
            var smeQueryString = smeSelect.ToQueryString();
            var sValueQueryString = sValueSelect?.ToQueryString();
            var iValueQueryString = iValueSelect?.ToQueryString();
            var dValueQueryString = dValueSelect?.ToQueryString();

            // set parameters
            smQueryString = SetParameter(smQueryString);
            smeQueryString = SetParameter(smeQueryString);
            sValueQueryString = sValueQueryString != null ? SetParameter(sValueQueryString) : null;
            iValueQueryString = iValueQueryString != null ? SetParameter(iValueQueryString) : null;
            dValueQueryString = dValueQueryString != null ? SetParameter(dValueQueryString) : null;

            // change INSTR to LIKE
            smQueryString = ChangeINSTRToLIKE(smQueryString);
            smeQueryString = ChangeINSTRToLIKE(smeQueryString);
            sValueQueryString = sValueQueryString != null ? ChangeINSTRToLIKE(sValueQueryString) : null;
            iValueQueryString = iValueQueryString != null ? ChangeINSTRToLIKE(iValueQueryString) : null;
            dValueQueryString = dValueQueryString != null ? ChangeINSTRToLIKE(dValueQueryString) : null;

            // with querys for each table
            var queryWithTables = $"WITH\r\n" +
                $"FilteredSM AS (\r\n{smQueryString}\r\n),\r\n" +
                $"FilteredSME AS (\r\n{smeQueryString}\r\n),\r\n" +
                (sValueQueryString != null ? $"FilteredSValue AS (\r\n{sValueQueryString}\r\n),\r\n" : string.Empty) +
                (iValueQueryString != null ? $"FilteredIValue AS (\r\n{iValueQueryString}\r\n),\r\n" : string.Empty) +
                (dValueQueryString != null ? $"FilteredDValue AS (\r\n{dValueQueryString}\r\n),\r\n" : string.Empty);

            // first join in with
            var selectWithStart = $"SELECT sme.SMId, sme.Id, sme.ParentSMEId, sme.IdShort, sme.TimeStamp, v.Value \r\n" +
                $"FROM";
            var selectWithEnd = $"AS v \r\n" +
                $"INNER JOIN FilteredSME AS sme \r\n" +
                $"ON sme.Id = v.SMEId \r\n ";
            var queryWithJoin =
                (direction == 0 ? // top-down
                    $"FilteredSMAndSME AS ( \r\n" +
                        $"SELECT sme.SMId, sm.Identifier, sme.Id, sme.ParentSMEId, sme.IdShort, sme.TimeStamp, sme.TValue \r\n" +
                        $"FROM FilteredSM AS sm \r\n" +
                        $"INNER JOIN FilteredSME AS sme \r\n" +
                        $"ON sm.Id = sme.SMId \r\n" +
                    $")" : string.Empty) +
                (direction == 1 ? // middle-out
                    $"FilteredSMAndSME AS ( \r\n" +
                        $"SELECT sme.SMId, sm.Identifier, sme.Id, sme.ParentSMEId, sme.IdShort, sme.TimeStamp, sme.TValue \r\n" +
                        $"FROM FilteredSME AS sme \r\n" +
                        $"INNER JOIN FilteredSM AS sm \r\n" +
                        $"ON sm.Id = sme.SMId \r\n" +
                    $")" : string.Empty) +
                (direction == 2 ? // bottom-up
                    $"FilteredSMEAndValue AS (\r\n" +
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectWithStart} FilteredSValue {selectWithEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \r\n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectWithStart} FilteredIValue {selectWithEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \r\n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectWithStart} FilteredDValue {selectWithEnd}" : string.Empty) +
                        ((withParameters && !withContains && !withEqualString && !withEqualNum && !withCompare) ? // Das hier bei Expression richtig?
                            $"UNION ALL \r\n" +
                            $"SELECT sme.SMId, sme.Id, sme.ParentSMEId, sme.IdShort, sme.TimeStamp, NULL \r\n" +
                            $"FROM FilteredSME AS sme " +
                            $"WHERE sme.TValue IS NULL OR sme.TValue = ''" : string.Empty) +
                    $")" : string.Empty)
                    + "\r\n";

            // secound join in select
            var selectStart = $"SELECT sm_sme.Identifier, sm_sme.Id, sm_sme.ParentSMEId, sm_sme.IdShort, sm_sme.TimeStamp, v.Value \r\n" +
                $"FROM FilteredSMAndSME AS sm_sme \r\n" +
                $"INNER JOIN";
            var selectEnd = $"AS v \r\n" +
                $"ON sm_sme.Id = v.SMEId \r\n ";
            var querySelectJoin =
                (direction is 0 or 1) ? // top-down and middle-out
                    (!sValueQueryString.IsNullOrEmpty() ? $"{selectStart} FilteredSValue {selectEnd}" : string.Empty) +
                    ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \r\n" : string.Empty) +
                    (!iValueQueryString.IsNullOrEmpty() ? $"{selectStart} FilteredIValue {selectEnd}" : string.Empty) +
                    ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \r\n" : string.Empty) +
                    (!dValueQueryString.IsNullOrEmpty() ? $"{selectStart} FilteredDValue {selectEnd}" : string.Empty) +
                    ((withParameters && !withContains && !withEqualString && !withEqualNum && !withCompare) ? // Das hier bei Expression richtig?
                        $"UNION ALL \r\n" +
                        $"SELECT sm_sme.SMId, sm_sme.Id, sm_sme.ParentSMEId, sm_sme.IdShort, sm_sme.TimeStamp, NULL \r\n" +
                        $"FROM FilteredSMAndSME AS sm_sme " +
                        $"WHERE sm_sme.TValue IS NULL OR sm_sme.TValue = ''" : string.Empty)
                : // bottom-up
                    $"SELECT sm.Identifier, sme_v.Id, sme_v.ParentSMEId, sme_v.IdShort, sme_v.TimeStamp, sme_v.Value \r\n" +
                    $"FROM FilteredSMEAndValue AS sme_v \r\n" +
                    $"INNER JOIN FilteredSM AS sm \r\n" +
                    $"ON sme_v.SMId = sm.Id";

            // create queryable
            var rawSql = $"{queryWithTables}{queryWithJoin}{querySelectJoin}";
            var result = db.Database.SqlQueryRaw<CombinedSMEResult>(rawSql);
            return result;
        }

        private static string SetParameter(string rawSQL)
        {
            var splitParam = rawSQL.Split(new string[] { ".param set ", "\r\n\r\n" }, StringSplitOptions.None);
            var result = splitParam[splitParam.Length - 1];
            for (var i = 1; i < splitParam.Length - 1; i++)
            {
                var param = splitParam[i].Split(new[] { ' ' }, 2);
                result = result.Replace(param[0], param[1]);
            }
            return result;
        }

        private static string ChangeINSTRToLIKE(string rawSQL)
        {
            var splitParam = rawSQL.Split(new string[] { "instr(", ") > 0" }, StringSplitOptions.None);
            var result = splitParam[0];
            for (var i = 1; i < splitParam.Length - 1; i++)
            {
                var param = splitParam[i].Split(", ", 2);
                var paramS = param[1].Split("\'", 3);
                i++;
                result += $"{param[0]} LIKE \'%{paramS[1]}%\' {splitParam[i]}";
            }
            return result;
        }

        private static List<SMEResult> GetSMEResult(AasContext db, IQueryable<CombinedSMEResult> query)
        {
            var result = new List<SMEResult>();
            foreach (var sm_sme_v in query)
            {
                /*
                 * ------ParentID Löschen------
                var path = sm_sme_v.IdShort;
                var pId = sm_sme_v.ParentSMEId;
                while (pId != null)
                {
                    var smeDB = db.SMESets.Where(s => s.Id == pId).Select(sme => new { sme.IdShort, sme.ParentSMEId }).First();
                    path = $"{smeDB.IdShort}.{path}";
                    pId = smeDB.ParentSMEId;
                }*/

                var rawSql = $"WITH " +
                    $"RECURSIVE SME_CTE AS (\r\n" +
                        $"SELECT IdShort, ParentSMEId, CAST(IdShort AS TEXT) AS Path\r\n" +
                        $"FROM SMESets\r\n" +
                        $"WHERE Id = {sm_sme_v.Id}\r\n" +
                        $"UNION ALL\r\n" +
                        $"SELECT x.IdShort, x.ParentSMEId, x.IdShort || '.' || c.Path\r\n" +
                        $"FROM SMESets x\r\n" +
                        $"INNER JOIN SME_CTE c " +
                        $"ON x.Id = c.ParentSMEId\r\n" +
                    $")\r\n" +
                    $"SELECT Path FROM SME_CTE WHERE ParentSMEId IS NULL";
                var path = db.Database.SqlQueryRaw<string>(rawSql).ToList()[0];

                result.Add(
                    new SMEResult()
                    {
                        smId = sm_sme_v.Identifier ?? string.Empty,
                        value = sm_sme_v.Value,
                        idShortPath = path ?? string.Empty,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm_sme_v.Identifier ?? "")}/submodel-elements/{path}",
                        timeStamp = TimeStamp.TimeStamp.DateTimeToString(sm_sme_v.TimeStamp)
                    }
                );
            }
            return result;
        }
    }
}