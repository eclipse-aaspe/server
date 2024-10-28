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

using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using AasxServerDB.Entities;
using AasxServerDB.Result;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using TimeStamp;
using System.Linq.Dynamic.Core;
using QueryParserTest;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Newtonsoft.Json.Linq;
using System.Linq;
using Irony.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Contracts;

namespace AasxServerDB
{
    public class Query
    {
        public static string? ExternalBlazor { get; set; }

        // --------------- API ---------------
        public List<SMResult> SearchSMs(string semanticId = "", string identifier = "", string diff = "", string expression = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMs");
            Console.WriteLine("Total number of SMs " + new AasContext().SMSets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var smList = GetSMSet(semanticId, identifier, diff, expression);
            Console.WriteLine("Found " + smList.Count + " SM in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMResult(smList);
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
            var smList = GetSMSet(semanticId, identifier, diff);
            var count = smList.Count;
            Console.WriteLine("Found " + count + " SM in " + watch.ElapsedMilliseconds + "ms");

            return count;
        }

        public List<SMEResult> SearchSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("SearchSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var smeWithValue = GetSMEWithValue(smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            Console.WriteLine("Found " + smeWithValue.Count + " SMEs in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();
            var result = GetSMEResult(smeWithValue);
            Console.WriteLine("Collected result in " + watch.ElapsedMilliseconds + "ms");

            return result;


        }

        public int CountSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = diff != "";
            var diffDT = TimeStamp.TimeStamp.StringToDateTime(diff);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine();
            Console.WriteLine("CountSMEs");
            Console.WriteLine("Total number of SMEs " + new AasContext().SMESets.Count() + " in " + watch.ElapsedMilliseconds + "ms");

            watch.Restart();

            var count = 0;
            if ((withSemanticID || withDiff)
                && contains.IsNullOrEmpty() && equal.IsNullOrEmpty() && lower.IsNullOrEmpty() && upper.IsNullOrEmpty())
            {
                using AasContext db = new();
                count = db.SMESets
                    .Where(sme =>
                        (!withSemanticID || (sme.SemanticId != null && sme.SemanticId == semanticId)) &&
                        (!withDiff || (sme.TimeStamp > diffDT))
                    )
                    .Count();
            }
            else
            {
                var smeWithValue = GetSMEWithValue(smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper);
                count = smeWithValue.Count;
            }

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
                    // var grammar = serviceProvider.GetService<QueryGrammar>();
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
                        // Security
                        if (parseTree.Root.Term.Name == "AllRules")
                        {
                            grammar.ParseAccessRules(parseTree.Root);
                            throw new Exception("Access Rules parsed!");
                        }

                        int countTypePrefix = 0;
                        combinedCondition = grammar.ParseTreeToExpression(parseTree.Root, "", ref countTypePrefix);

                        countTypePrefix = 0;
                        conditionSM = grammar.ParseTreeToExpression(parseTree.Root, "sm.", ref countTypePrefix);
                        if (conditionSM == "$SKIP")
                        {
                            conditionSM = "";
                        }

                        countTypePrefix = 0;
                        conditionSME = grammar.ParseTreeToExpression(parseTree.Root, "sme.", ref countTypePrefix);
                        if (conditionSME == "$SKIP")
                        {
                            conditionSME = "";
                        }

                        countTypePrefix = 0;
                        conditionStr = grammar.ParseTreeToExpression(parseTree.Root, "str()", ref countTypePrefix);
                        if (conditionStr == "$SKIP")
                        {
                            conditionStr = "";
                        }

                        countTypePrefix = 0;
                        conditionNum = grammar.ParseTreeToExpression(parseTree.Root, "num()", ref countTypePrefix);
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
        private static List<SMSet> GetSMSet(string semanticId = "", string identifier = "", string diffString = "", string expression = "")
        {
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();
            var diff = TimeStamp.TimeStamp.StringToDateTime(diffString);
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withExpression = !expression.IsNullOrEmpty();

            var listSMset = new List<SMSet>();
            List<SMEWithValue> notUsed = null;

            if (!withSemanticId && !withIdentifier && !withDiff && !withExpression)
                return listSMset;

            using (var db = new AasContext())
            {
                if (!withExpression)
                {
                    var x = db.SMSets
                        .Where(s =>
                            (!withSemanticId || (s.SemanticId != null && s.SemanticId.Equals(semanticId))) &&
                            (!withIdentifier || (s.Identifier != null && s.Identifier.Equals(identifier))) &&
                            (!withDiff || s.TimeStampTree.CompareTo(diff) > 0));
                    return x.ToList();
                }

                QuerySMorSME(ref listSMset, ref notUsed, expression);
            }

            return listSMset;
        }

        private static List<SMResult> GetSMResult(List<SMSet> smList)
        {
            return smList.ConvertAll(
                sm =>
                {
                    string identifier = (sm != null && !sm.Identifier.IsNullOrEmpty()) ? sm.Identifier : string.Empty;
                    return new SMResult()
                    {
                        smId = identifier,
                        url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(identifier)}",
                        timeStampTree = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree)
                    };
                }
            );
        }

        // --------------- SME Methodes ---------------
        private class SMEWithValue
        {
            public SMSet? sm;
            public SMESet? sme;
            public string? value;
        }

        private List<SMEWithValue> GetSMEWithValue(string smSemanticId = "", string smIdentifier = "", string semanticId = "",
            string diff = "", string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            var result = new List<SMEWithValue>();

            if (expression == "")
            {
                var dateTime = TimeStamp.TimeStamp.StringToDateTime(diff);
                var withDiff = diff != "";
                var withSemanticID = !semanticId.IsNullOrEmpty();

                var parameter = 0;
                if (!contains.IsNullOrEmpty())
                    parameter++;
                if (!equal.IsNullOrEmpty())
                    parameter++;
                if (!(lower.IsNullOrEmpty() && upper.IsNullOrEmpty()))
                    parameter++;
                if (parameter > 1 || (semanticId.IsNullOrEmpty() && !withDiff && parameter != 1))
                    return result;

                if ((withSemanticID || withDiff)
                    && contains.IsNullOrEmpty() && equal.IsNullOrEmpty() && lower.IsNullOrEmpty() && upper.IsNullOrEmpty())
                {
                    using AasContext db = new();
                    result.AddRange(db.SMESets
                        .Where(sme =>
                            (!withSemanticID || (sme.SemanticId != null && sme.SemanticId == semanticId)) &&
                            (!withDiff || (sme.TimeStamp > dateTime))
                        )
                        .Select(sme => new SMEWithValue { sme = sme })
                        .ToList());
                }
                else
                {
                    GetXValue(ref result, semanticId, dateTime, contains, equal, lower, upper);
                    GetSValue(ref result, semanticId, dateTime, contains, equal);
                    GetIValue(ref result, semanticId, dateTime, equal, lower, upper);
                    GetDValue(ref result, semanticId, dateTime, equal, lower, upper);
                    GetOValue(ref result, semanticId, dateTime, contains, equal);
                }

                SelectSM(ref result, smSemanticId, smIdentifier);
                return result;
            }

            List<SMSet> notUsed = null;
            QuerySMorSME(ref notUsed, ref result, expression);

            return result;
        }

        private static void GetXValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "", string lower = "", string upper = "")
        {
            var withValue = !contains.IsNullOrEmpty() || !equal.IsNullOrEmpty() || !lower.IsNullOrEmpty() || !upper.IsNullOrEmpty();
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            if ((!withSemanticID && !withDiff) || withValue)
                return;

            using AasContext db = new();
            smeValue.AddRange(db.SMESets
                        .Where(sme =>
                            (sme.TValue == string.Empty || sme.TValue == null) &&
                            (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStamp.CompareTo(diff) > 0))
                        .Select(sme => new SMEWithValue { sme = sme })
                .ToList());
        }

        private static void GetSValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "")
        {
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqual = !equal.IsNullOrEmpty();
            if (!withSemanticID && !withDiff && !withContains && !withEqual)
                return;

            using AasContext db = new();
            smeValue.AddRange(db.SValueSets
                .Where(v => v.Value != null &&
                    (!withContains || v.Value.Contains(contains)) &&
                    (!withEqual || v.Value.Equals(equal)))
                .Join(
                    db.SMESets
                        .Where(sme => 
                            (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStamp.CompareTo(diff) > 0)),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value })
                .ToList());
        }

        private static void GetIValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string equal = "", string lower = "", string upper = "")
        {
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withEqual = !equal.IsNullOrEmpty();
            var withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withSemanticID && !withDiff && !withEqual && !withCompare)
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
                            (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStamp.CompareTo(diff) > 0))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() })
                .ToList());
        }

        private static void GetDValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string equal = "", string lower = "", string upper = "")
        {
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withEqual = !equal.IsNullOrEmpty();
            var withCompare = !(lower.IsNullOrEmpty() && upper.IsNullOrEmpty());
            if (!withSemanticID && !withDiff && !withEqual && !withCompare)
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
                            (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStamp.CompareTo(diff) > 0))),
                    v => v.SMEId, sme => sme.Id, (v, sme) => new SMEWithValue { sme = sme, value = v.Value.ToString() })
                .ToList());
        }

        private static void GetOValue(ref List<SMEWithValue> smeValue, string semanticId = "", DateTime diff = new(), string contains = "", string equal = "")
        {
            var withSemanticID = !semanticId.IsNullOrEmpty();
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withContains = !contains.IsNullOrEmpty();
            var withEqual = !equal.IsNullOrEmpty();
            if (!withSemanticID && !withDiff && !withContains && !withEqual)
                return;

            using AasContext db = new();
            smeValue.AddRange(db.OValueSets
                .Where(v => v.Value != null &&
                    (!withContains || ((string) v.Value).Contains(contains)) &&
                    (!withEqual || ((string) v.Value).Equals(equal)))
                .Join(
                    db.SMESets
                        .Where(sme =>
                            (!withSemanticID || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) &&
                            (!withDiff || sme.TimeStamp.CompareTo(diff) > 0)),
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
                        timeStamp = TimeStamp.TimeStamp.DateTimeToString(sme.sme.TimeStamp)
                    };
                }
            );
        }
    }
}
