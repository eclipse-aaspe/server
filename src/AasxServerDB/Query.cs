/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/
using System.Linq.Dynamic.Core;
using QueryParserTest;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Newtonsoft.Json.Linq;
using System.Linq;
using Irony.Parsing;

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

    public partial class Query
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
            if (query == null)
            {
                Console.WriteLine("No query is generated due to incorrect parameter combination.");
                return new List<SMResult>();
            }
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
            if (query == null)
            {
                Console.WriteLine("No query is generated due to incorrect parameter combination.");
                return 0;
            }
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
            var query = GetSMEs(db, false, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            if (query == null)
            {
                Console.WriteLine("No query is generated due to incorrect parameter combination.");
                return new List<SMEResult>();
            }
            Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");

            watch.Restart();
            var result = GetSMEResult((IQueryable<CombinedSMEResult>)query);
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
            var query = GetSMEs(db, true, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            if (query == null)
            {
                Console.WriteLine("No query is generated due to incorrect parameter combination.");
                return 0;
            }
            Console.WriteLine("Generate query\tin " + watch.ElapsedMilliseconds + " ms");

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

        private class CombinedResult
        {
            public SMSet sm { get; set; }
            public SMESet sme { get; set; }
            public string? sValue { get; set; }
            public double? mValue { get; set; }
        }

        private class CombinedValueResult
        {
            public int SMEId { get; set; }
            public string? sValue { get; set; }
            public double? mValue { get; set; }
        }

        private static void QuerySMorSME(ref List<SMSet>? smSet, ref List<SMEWithValue>? smeSet, string expression = "")
        {
            if (expression.StartsWith("$REVERSE"))
            {
                expression = expression.Replace("$REVERSE", "");
                Console.WriteLine("$REVERSE");
                QuerySMorSME_reverse(ref smSet, ref smeSet, expression);
                return;
            }
            QuerySMorSME_normal(ref smSet, ref smeSet, expression);
        }
        private static void QuerySMorSME_reverse(ref List<SMSet>? smSet, ref List<SMEWithValue>? smeSet, string expression = "")
        {
            if (expression == "")
            {
                return;
            }

            bool log = false;
            if (expression.StartsWith("$LOG"))
            {
                log = true;
                expression = expression.Replace("$LOG", "");
                Console.WriteLine("$LOG");
            }

            using (var db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                expression = expression.Replace("\n", "").Replace(" ", "");

                // Parser
                int countTypePrefix = 0;
                var parser = new ParserWithAST(new Lexer(expression));
                var ast = parser.Parse();

                var combinedCondition = parser.GenerateSql(ast, "", ref countTypePrefix, "filter");
                Console.WriteLine("combinedCondition: " + combinedCondition);

                var conditionSM = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel");
                if (conditionSM == "")
                {
                    conditionSM = parser.GenerateSql(ast, "sm.", ref countTypePrefix, "filter");
                }
                conditionSM = conditionSM.Replace("sm.", "");
                Console.WriteLine("condition sm. #" + countTypePrefix + ": " + conditionSM);
                var conditionSME = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel_elements");
                if (conditionSME == "")
                {
                    conditionSME = parser.GenerateSql(ast, "sme.", ref countTypePrefix, "filter");
                }
                conditionSME = conditionSME.Replace("sme.", "");
                Console.WriteLine("condition sme. #" + countTypePrefix + ": " + conditionSME);
                var conditionSMEValue = parser.GenerateSql(ast, "sme.value", ref countTypePrefix, "filter");
                Console.WriteLine("condition sme.value #" + countTypePrefix + ": " + conditionSMEValue);
                var conditionStr = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_str");
                if (conditionStr == "")
                {
                    conditionStr = parser.GenerateSql(ast, "sValue", ref countTypePrefix, "filter");
                }
                conditionStr = conditionStr.Replace("sValue", "Value");
                Console.WriteLine("condition sValue #" + countTypePrefix + ": " + conditionStr);
                var conditionNum = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_num");
                if (conditionNum == "")
                {
                    conditionNum = parser.GenerateSql(ast, "mValue", ref countTypePrefix, "filter");
                }
                conditionNum = conditionNum.Replace("mValue", "Value");
                Console.WriteLine("condition mValue #" + countTypePrefix + ": " + conditionNum);

                // Dynamic condition
                var querySM = db.SMSets.AsQueryable();
                var querySME = db.SMESets.AsQueryable();
                var querySValue = db.SValueSets.AsQueryable();
                var queryIValue = db.IValueSets.AsQueryable();
                var queryDValue = db.DValueSets.AsQueryable();

                var querySValueWhere = querySValue;
                if (conditionStr != "")
                {
                    querySValueWhere = querySValue.Where(conditionStr);
                }
                var querySValueFiltered =
                        querySValueWhere
                        .Select(sValue => new CombinedValueResult { SMEId = sValue.SMEId, sValue = sValue.Value, mValue = null } );
                var sLog = "";
                if (log)
                {
                    sLog = "Found " + querySValueFiltered.Count();
                }
                Console.WriteLine(sLog + " querySValueFiltered " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                var queryIValueWhere = queryIValue;
                if (conditionNum != "")
                {
                    queryIValueWhere = queryIValue.Where(conditionNum);
                }
                var queryIValueFiltered =
                        queryIValueWhere
                        .Select(iValue => new CombinedValueResult { SMEId = iValue.SMEId, sValue = null, mValue = iValue.Value });
                sLog = "";
                if (log)
                {
                    sLog = "Found " + queryIValueFiltered.Count();
                }
                Console.WriteLine(sLog + " queryIValueFiltered " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                var queryDValueWhere = queryDValue;
                if (conditionNum != "")
                {
                    queryDValueWhere = queryDValue.Where(conditionNum);
                }
                var queryDValueFiltered =
                        queryDValueWhere
                        .Select(dValue => new CombinedValueResult { SMEId = dValue.SMEId, sValue = null, mValue = dValue.Value });
                sLog = "";
                if (log)
                {
                    sLog = "Found " + queryDValueFiltered.Count();
                }
                Console.WriteLine(sLog + " queryDValueFiltered " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                var queryUnionValueFiltered = querySValueFiltered.Union(queryIValueFiltered).Union(queryDValueFiltered).Distinct();
                sLog = "";
                if (log)
                {
                    sLog = "Found " + queryUnionValueFiltered.Count();
                }
                Console.WriteLine(sLog + " queryUnionValueFiltered " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                var querySMEWhere = querySME;
                if (conditionSME != "")
                {
                    querySMEWhere = querySME.Where(conditionSME);
                }
                var queryValueAndSME = queryUnionValueFiltered
                    .Join(
                        querySMEWhere,
                        value => value.SMEId,
                        sme => sme.Id,
                            (value, sme) => new { sme, value.sValue, value.mValue }
                        );
                sLog = "";
                if (log)
                {
                    sLog = "Found " + queryValueAndSME.Count();
                }
                Console.WriteLine(sLog + " queryValueAndSME " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                var querySMWhere = querySM;
                if (conditionSM != "")
                {
                    querySMWhere = querySM.Where(conditionSM);
                }
                var queryValueAndSMEandSM = queryValueAndSME
                    .Join(
                        querySMWhere,
                        valuesme => valuesme.sme.SMId,
                        sm => sm.Id,
                            (valuesme, sm) => new CombinedResult { sm = sm, sme = valuesme.sme, sValue = valuesme.sValue, mValue = valuesme.mValue }
                        );
                sLog = "";
                if (log)
                {
                    sLog = "Found " + queryValueAndSMEandSM.Count();
                }
                Console.WriteLine(sLog + " queryValueAndSMEandSM " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                var query = queryValueAndSMEandSM
                    .Where(combinedCondition)
                    .Distinct();
                sLog = "";
                if (log)
                {
                    sLog = "Found " + query.Count();
                }
                Console.WriteLine(sLog + " query in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                if (smSet != null)
                {
                    smSet = query.Select(result => result.sm).Distinct().ToList();
                    Console.WriteLine("smSet in " + watch.ElapsedMilliseconds + "ms");
                    watch.Restart();
                }

                if (smeSet != null)
                {
                    smeSet = query
                        .Select(result => new SMEWithValue
                        {
                            sm = result.sm,
                            sme = result.sme,
                            value = result.sValue ?? result.mValue.ToString()
                        })
                        .Distinct()
                        .ToList();
                    Console.WriteLine("smeSet in " + watch.ElapsedMilliseconds + "ms");
                    watch.Restart();
                }

                return;
            }
        }

        private static void QuerySMorSME_normal(ref List<SMSet>? smSet, ref List<SMEWithValue>? smeSet, string expression = "")
        {
            bool log = false;
            bool withQueryLanguage = false;

            if (expression.StartsWith("$LOG"))
            {
                log = true;
                expression = expression.Replace("$LOG", "");
                Console.WriteLine("$LOG");
            }

            if (expression.StartsWith("$QL"))
            {
                withQueryLanguage = true;
                expression = expression.Replace("$QL", "");
                Console.WriteLine("$QL");
            }

            if (expression == "" || (smSet == null && smeSet == null))
            {
                return;
            }

            using (var db = new AasContext())
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                string combinedCondition = "";
                string conditionSM = "";
                string conditionSME = "";
                string conditionSMEValue = "";
                string conditionStr = "";
                string conditionNum = "";

                if (!withQueryLanguage)
                {
                    expression = expression.Replace("\n", "").Replace(" ", "");

                    // Parser
                    int countTypePrefix = 0;
                    var parser = new ParserWithAST(new Lexer(expression));
                    var ast = parser.Parse();

                    combinedCondition = parser.GenerateSql(ast, "", ref countTypePrefix, "filter");
                    Console.WriteLine("combinedCondition: " + combinedCondition);

                    conditionSM = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel");
                    if (conditionSM == "")
                    {
                        conditionSM = parser.GenerateSql(ast, "sm.", ref countTypePrefix, "filter");
                    }
                    conditionSM = conditionSM.Replace("sm.", "");
                    Console.WriteLine("condition sm. #" + countTypePrefix + ": " + conditionSM);
                    conditionSME = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel_elements");
                    if (conditionSME == "")
                    {
                        conditionSME = parser.GenerateSql(ast, "sme.", ref countTypePrefix, "filter");
                    }
                    conditionSME = conditionSME.Replace("sme.", "");
                    Console.WriteLine("condition sme. #" + countTypePrefix + ": " + conditionSME);
                    conditionSMEValue = parser.GenerateSql(ast, "sme.value", ref countTypePrefix, "filter");
                    Console.WriteLine("condition sme.value #" + countTypePrefix + ": " + conditionSMEValue);
                    conditionStr = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_str");
                    if (conditionStr == "")
                    {
                        conditionStr = parser.GenerateSql(ast, "sValue", ref countTypePrefix, "filter");
                    }
                    if (conditionStr == "true")
                    {
                        conditionStr = "sValue != null";
                    }
                    conditionStr = conditionStr.Replace("sValue", "Value");
                    Console.WriteLine("condition sValue #" + countTypePrefix + ": " + conditionStr);
                    conditionNum = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_num");
                    if (conditionNum == "")
                    {
                        conditionNum = parser.GenerateSql(ast, "mValue", ref countTypePrefix, "filter");
                    }
                    if (conditionNum == "true")
                    {
                        conditionNum = "mValue != null";
                    }
                    conditionNum = conditionNum.Replace("mValue", "Value");
                    Console.WriteLine("condition mValue #" + countTypePrefix + ": " + conditionNum);
                }
                else
                {
                    // with newest query language from QueryParser.cs
                    var grammar = new QueryGrammar();
                    var parser = new Parser(grammar);
                    parser.Context.TracingEnabled = true;
                    var parseTree = parser.Parse(expression);

                    if (parseTree.HasErrors())
                    {
                        var pos = parser.Context.CurrentToken.Location.Position;
                        var text = expression.Substring(0, pos) + "$$$" + expression.Substring(pos);
                        text = string.Join("\n", parseTree.ParserMessages) + "\nSee $$$: " + text;
                        Console.WriteLine(text);
                        while (text != text.Replace("\n  ", "\n "))
                        {
                            text = text.Replace("\n  ", "\n ");
                        };
                        text = text.Replace("\n", "\n");
                        text = text.Replace("\n", " ");
                        throw new Exception(text);
                    }
                    else
                    {
                        int countTypePrefix = 0;
                        combinedCondition = QueryGrammar.ParseTreeToExpression(parseTree.Root, "", ref countTypePrefix);

                        countTypePrefix = 0;
                        conditionSM = QueryGrammar.ParseTreeToExpression(parseTree.Root, "sm.", ref countTypePrefix);
                        if (conditionSM == "$SKIP")
                        {
                            conditionSM = "";
                        }

                        countTypePrefix = 0;
                        conditionSME = QueryGrammar.ParseTreeToExpression(parseTree.Root, "sme.", ref countTypePrefix);
                        if (conditionSME == "$SKIP")
                        {
                            conditionSME = "";
                        }

                        countTypePrefix = 0;
                        conditionStr = QueryGrammar.ParseTreeToExpression(parseTree.Root, "str()", ref countTypePrefix);
                        if (conditionStr == "$SKIP")
                        {
                            conditionStr = "";
                        }

                        countTypePrefix = 0;
                        conditionNum = QueryGrammar.ParseTreeToExpression(parseTree.Root, "num()", ref countTypePrefix);
                        if (conditionNum == "$SKIP")
                        {
                            conditionNum = "";
                        }
                    }
                }

                // Dynamic condition
                var querySM = db.SMSets.AsQueryable();
                var querySME = db.SMESets.AsQueryable();
                var querySValue = db.SValueSets.AsQueryable();
                var queryIValue = db.IValueSets.AsQueryable();
                var queryDValue = db.DValueSets.AsQueryable();

                var querySMWhere = querySM;
                if (conditionSM != "" && conditionSM != "true")
                {
                    querySMWhere = querySMWhere.Where(conditionSM);
                }

                if (conditionSME == "" && conditionStr == "" && conditionNum == "" && smeSet == null)
                {
                    smSet = querySMWhere.Distinct().ToList();
                    Console.WriteLine("smSet in " + watch.ElapsedMilliseconds + "ms");
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
                if (log)
                {
                    sLog = "Found " + querySMandSME.Count();
                }
                Console.WriteLine(sLog + " filterd SM+SME in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                if (conditionStr == "" && conditionNum == "" && smeSet == null)
                {
                    if (smSet != null)
                    {
                        smSet = querySMandSME.Select(result => result.sm).Distinct().ToList();
                        Console.WriteLine("smSet in " + watch.ElapsedMilliseconds + "ms");
                        watch.Restart();
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
                if (log)
                {
                    sLog = "Found " + querySMandSMEandSValue.Count();
                }
                Console.WriteLine(sLog + " filtered SM+SME+SVALUE in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

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
                if (log)
                {
                    sLog = "Found " + querySMandSMEandIValue.Count();
                }
                Console.WriteLine(sLog + " filtered SM+SME+IVALUE in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

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
                if (log)
                {
                    sLog = "Found " + querySMandSMEandDValue.Count();
                }
                Console.WriteLine(sLog + " filtered SM+SME+DVALUE in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                var queryUnion = querySMandSMEandSValue.Union(querySMandSMEandIValue).Union(querySMandSMEandDValue).Distinct();

                var query = queryUnion
                    .Where(combinedCondition)
                    .Distinct();
                sLog = "";
                if (log)
                {
                    sLog = "Found " + query.Count();
                }
                Console.WriteLine(sLog + " Union in " + watch.ElapsedMilliseconds + "ms");
                watch.Restart();

                if (smSet != null)
                {
                    smSet = query.Select(result => result.sm).Distinct().ToList();
                    Console.WriteLine("smSet in " + watch.ElapsedMilliseconds + "ms");
                    watch.Restart();
                }

                if (smeSet != null)
                {
                    smeSet = query
                        .Select(result => new SMEWithValue
                        {
                            sm = result.sm,
                            sme = result.sme,
                            value = result.sValue ?? result.mValue.ToString()
                        })
                        .Distinct()
                        .ToList();
                    Console.WriteLine("smeSet in " + watch.ElapsedMilliseconds + "ms");
                    watch.Restart();
                }

                return;
            }
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

            // format date time
            smQueryString = FormatDateTimeInSQL(smQueryString);

            // create queryable
            var result = db.Database.SqlQueryRaw<CombinedSMResult>(smQueryString);
            return result;
        }

        private static List<SMResult> GetSMResult(IQueryable<CombinedSMResult> query)
        {
            var result = query
                .AsEnumerable()
                .Select(sm => new SMResult()
                {
                    smId = sm.Identifier,
                    timeStampTree = sm.TimeStampTree,
                    url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm.Identifier ?? string.Empty)}"
                })
                .ToList();
            return result;
        }

        // --------------- SME Methodes ---------------
        private static IQueryable? GetSMEs(AasContext db, bool withCount = false, string smSemanticId = "", string smIdentifier = "", string semanticId = "",
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
            var smeSelect = smeTable.Select(sme => new { sme.SMId, sme.Id, sme.TimeStamp, sme.TValue });
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
            var rawSQL = $"WITH\n" +
                $"FilteredSM AS (\n{smQueryString}\n),\n" +
                $"FilteredSME AS (\n{smeQueryString}\n),\n" +
                (sValueQueryString != null ? $"FilteredSValue AS (\n{sValueQueryString}\n),\n" : string.Empty) +
                (iValueQueryString != null ? $"FilteredIValue AS (\n{iValueQueryString}\n),\n" : string.Empty) +
                (dValueQueryString != null ? $"FilteredDValue AS (\n{dValueQueryString}\n),\n" : string.Empty);

            // join direction
            if (direction == 0) // top-down
            {
                // SM => SME
                rawSQL +=
                    $"FilteredSMAndSME AS ( \n" +
                        $"SELECT sm.Identifier, sme.Id, sme.TimeStamp, sme.TValue \n" +
                        $"FROM FilteredSM AS sm \n" +
                        $"INNER JOIN FilteredSME AS sme ON sm.Id = sme.SMId \n" +
                    $"), \n";

                // SM-SME => VALUE
                var selectStart = $"SELECT sm_sme.Identifier, sm_sme.Id, sm_sme.TimeStamp, v.Value \n" +
                    $"FROM FilteredSMAndSME AS sm_sme \n" +
                    $"INNER JOIN ";
                var selectEnd = $" AS v ON sm_sme.Id = v.SMEId \n ";
                rawSQL +=
                    $"FilteredSMAndSMEAndValue AS ( \n" +
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredSValue{selectEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredIValue{selectEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredDValue{selectEnd}" : string.Empty) +
                        ((withParameters && !withContains && !withEqualString && !withEqualNum && !withCompare) ?
                            $"UNION ALL \n" +
                            $"SELECT sm_sme.Identifier, sm_sme.Id, sm_sme.TimeStamp, NULL \n" +
                            $"FROM FilteredSMAndSME AS sm_sme \n" +
                            $"WHERE sm_sme.TValue IS NULL OR sm_sme.TValue = ''\n" : string.Empty) +
                    $")";
            }
            else if (direction == 1) // middle-out
            {
                // SME => SM
                rawSQL +=
                    $"FilteredSMAndSME AS ( \n" +
                        $"SELECT sm.Identifier, sme.Id, sme.TimeStamp, sme.TValue \n" +
                        $"FROM FilteredSME AS sme \n" +
                        $"INNER JOIN FilteredSM AS sm ON sm.Id = sme.SMId \n" +
                    $"), \n";

                // SME-SM => VALUE
                var selectStart = $"SELECT sm_sme.Identifier, sm_sme.Id, sm_sme.TimeStamp, v.Value \n" +
                    $"FROM FilteredSMAndSME AS sm_sme \n" +
                    $"INNER JOIN ";
                var selectEnd = $" AS v ON sm_sme.Id = v.SMEId \n ";
                rawSQL +=
                    $"FilteredSMAndSMEAndValue AS ( \n" +
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredSValue{selectEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredIValue{selectEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredDValue{selectEnd}" : string.Empty) +
                        ((withParameters && !withContains && !withEqualString && !withEqualNum && !withCompare) ?
                            $"UNION ALL \n" +
                            $"SELECT sm_sme.Identifier, sm_sme.Id, sm_sme.TimeStamp, NULL \n" +
                            $"FROM FilteredSMAndSME AS sm_sme \n" +
                            $"WHERE sm_sme.TValue IS NULL OR sm_sme.TValue = ''\n" : string.Empty) +
                    $")";
            }
            else if (direction == 2) // bottom-up
            {
                // VALUE => SME
                var selectWithStart = $"SELECT sme.SMId, sme.Id, sme.TimeStamp, v.Value \n " +
                    $"FROM ";
                var selectWithEnd = $" AS v \n" +
                    $"INNER JOIN FilteredSME AS sme ON sme.Id = v.SMEId \n ";
                rawSQL +=
                    $"FilteredSMEAndValue AS (\n" +
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectWithStart}FilteredSValue{selectWithEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectWithStart}FilteredIValue{selectWithEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectWithStart}FilteredDValue{selectWithEnd}" : string.Empty) +
                        ((withParameters && !withContains && !withEqualString && !withEqualNum && !withCompare) ?
                            $"UNION ALL \n" +
                            $"SELECT sme.SMId, sme.Id, sme.TimeStamp, NULL \n" +
                            $"FROM FilteredSME AS sme \n" +
                            $"WHERE sme.TValue IS NULL OR sme.TValue = ''\n" : string.Empty) +
                    $"), \n";

                // VALUE-SME => SM
                rawSQL +=
                    $"FilteredSMAndSMEAndValue AS ( \n" +
                        $"SELECT sm.Identifier, sme_v.Id, sme_v.TimeStamp, sme_v.Value \n" +
                        $"FROM FilteredSMEAndValue AS sme_v \n" +
                        $"INNER JOIN FilteredSM AS sm ON sme_v.SMId = sm.Id \n" +
                    $")";
            }

            if (withCount)
            {
                //select
                rawSQL += $"\nSELECT sme.Id \nFROM FilteredSMAndSMEAndValue AS sme";

                // create queryable
                var resultCount = db.Database.SqlQueryRaw<int>(rawSQL);
                return resultCount;
            }

            // get path
            rawSQL += $", \n" +
                $"RecursiveSME AS( \n" +
                    $"WITH RECURSIVE SME_CTE AS ( \n" +
                        $"SELECT Id, IdShort, ParentSMEId, IdShort AS IdShortPath, Id AS StartId \n" +
                        $"FROM SMESets \n" +
                        $"WHERE Id IN (SELECT Id FROM FilteredSMAndSMEAndValue) \n" +
                        $"UNION ALL \n" +
                        $"SELECT x.Id, x.IdShort, x.ParentSMEId, x.IdShort || '.' || c.IdShortPath, c.StartId \n" +
                        $"FROM SMESets x \n" +
                        $"INNER JOIN SME_CTE c ON x.Id = c.ParentSMEId \n" +
                    $") \n" +
                    $"SELECT StartId AS Id, IdShortPath \n" +
                    $"FROM SME_CTE \n" +
                    $"WHERE ParentSMEId IS NULL \n" +
                $") \n";

            //select
            rawSQL += $"SELECT sme.Identifier, r.IdShortPath, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', sme.TimeStamp) AS TimeStamp, sme.Value \n" +
                "FROM FilteredSMAndSMEAndValue AS sme \n" +
                "INNER JOIN RecursiveSME AS r ON sme.Id = r.Id";

            // create queryable
            var result = db.Database.SqlQueryRaw<CombinedSMEResult>(rawSQL);
            return result;
        }

        private static List<SMEResult> GetSMEResult(IQueryable<CombinedSMEResult> query)
        {
            var result = query
                .AsEnumerable()
                .Select(sm_sme_v => new SMEResult()
                {
                    smId = sm_sme_v.Identifier,
                    idShortPath = sm_sme_v.IdShortPath,
                    timeStamp = sm_sme_v.TimeStamp,
                    value = sm_sme_v.Value,
                    url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm_sme_v.Identifier ?? string.Empty)}/submodel-elements/{sm_sme_v.IdShortPath}"
                })
                .ToList();
            return result;
        }

        // --------------- Help Methodes ---------------
        private static string SetParameter(string rawSQL)
        {
            var parameters = new Dictionary<string, string>();
            var lines = rawSQL.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sqlLines = new List<string>();
            foreach (var line in lines)
            {
                if (line.StartsWith(".param set"))
                {
                    // Extract parameter
                    var parts = line.Split(new[] { ' ' }, 4);
                    parameters[parts[2]] = parts[3];
                }
                else
                {
                    sqlLines.Add(line);
                }
            }

            // Join the remaining lines to form the SQL query
            var sqlQuery = string.Join("\n", sqlLines);

            // Replace parameter placeholders in the SQL query with their values
            foreach (var param in parameters)
            {
                sqlQuery = sqlQuery.Replace(param.Key, $"{param.Value}");
            }

            return sqlQuery;
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

        private static string FormatDateTimeInSQL(string rawSQL)
        {
            var replaced = false;
            var result = Regex.Replace(rawSQL, @", ""([a-zA-Z])?""\.\""TimeStampTree\""", match =>
            {
                if (!replaced)
                {
                    replaced = true;
                    return $", strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', \"{match.Groups[1].Value}\".\"TimeStampTree\") AS \"TimeStampTree\"";
                }
                return match.Value;
            }, RegexOptions.None, TimeSpan.FromMilliseconds(500));
            //var result = Regex.Replace(rawSQL, @", ""([a-zA-Z])?""\.\""TimeStampTree\""", @", strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', ""$1"".""TimeStampTree"") AS ""TimeStampTree""", RegexOptions.None, TimeSpan.FromMilliseconds(500));
            return result;
        }
    }
}