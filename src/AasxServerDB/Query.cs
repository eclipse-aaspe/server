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
    using Irony.Parsing;
    using System.Reflection.Metadata;
    using HotChocolate.Resolvers;
    using HotChocolate.Language;

    public partial class Query
    {
        public Query(QueryGrammar queryGrammar)
        {
            grammar = queryGrammar;
        }
        private readonly QueryGrammar grammar;
        public static string? ExternalBlazor { get; set; }

        // --------------- API ---------------
        public QResult SearchSMs(IResolverContext context, string semanticId = "", string identifier = "", string diff = "", string expression = "")
        {
            var qResult = new QResult();
            qResult.Messages = new List<string>();
            var text = "";

            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nSearchSMs");

            watch.Restart();
            var query = GetSMs(qResult.Messages, db, semanticId, identifier, diff, expression);
            if (query == null)
            {
                text = "No query is generated due to incorrect parameter combination.";
                Console.WriteLine(text);
                qResult.Messages.Add(text);
            }
            else
            {
                text = "Generate query in " + watch.ElapsedMilliseconds + " ms";
                Console.WriteLine(text);
                qResult.Messages.Add(text);

                watch.Restart();
                var result = GetSMResult(query);
                text = "Collect results in " + watch.ElapsedMilliseconds + " ms";
                Console.WriteLine(text);
                qResult.Messages.Add(text);
                text = "SMs found " + result.Count + "/" + db.SMSets.Count();
                Console.WriteLine(text);
                qResult.Messages.Add(text);

                qResult.SMResults = result;
            }

            return qResult;
        }

        public int CountSMs(string semanticId = "", string identifier = "", string diff = "", string expression = "")
        {
            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nCountSMs");

            watch.Restart();
            var query = GetSMs(new List<string>(), db, semanticId, identifier, diff, expression);
            if (query == null)
            {
                Console.WriteLine("No query is generated due to incorrect parameter combination.");
                return 0;
            }
            Console.WriteLine("Generate query in " + watch.ElapsedMilliseconds + " ms");

            watch.Restart();
            var result = query.Count();
            Console.WriteLine("Collect results in " + watch.ElapsedMilliseconds + " ms\nSMs found\t" + result + "/" + db.SMSets.Count());

            return result;
        }

        public QResult SearchSMEs(
            IResolverContext context,
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            // Get the requested fields
            string  requested = "";
            var requestedFields = context.Selection.SyntaxNode.SelectionSet.Selections;
            foreach (var selection in requestedFields)
            {
                if (selection is FieldNode field)
                {
                    requested += " " + field.ToString();
                }
            }

            var qResult = new QResult();
            qResult.Messages = new List<string>();
            var text = "";

            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nSearchSMEs");

            watch.Restart();
            var query = GetSMEs(qResult.Messages, requested, db, false, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            if (query == null)
            {
                text = "No query is generated due to incorrect parameter combination.";
                Console.WriteLine(text);
                qResult.Messages.Add(text);
            }
            else
            {
                text = "Generate query in " + watch.ElapsedMilliseconds + " ms";
                Console.WriteLine(text);
                qResult.Messages.Add(text);

                watch.Restart();
                var result = GetSMEResult(requested, (IQueryable<CombinedSMEResult>)query);
                text = "SMEs found " + result.Count + "/" + db.SMESets.Count();
                Console.WriteLine(text);
                qResult.Messages.Add(text);
                text = "Collect results in " + watch.ElapsedMilliseconds + " ms";
                Console.WriteLine(text);
                qResult.Messages.Add(text);

                qResult.SMEResults = result;
            }

            return qResult;
        }

        public int CountSMEs(
            string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
            string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
        {
            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nCountSMEs");

            watch.Restart();
            var query = GetSMEs(new List<string>(), "", db, true, smSemanticId, smIdentifier, semanticId, diff, contains, equal, lower, upper, expression);
            if (query == null)
            {
                Console.WriteLine("No query is generated due to incorrect parameter combination.");
                return 0;
            }
            Console.WriteLine("Generate query in " + watch.ElapsedMilliseconds + " ms");

            watch.Restart();
            var result = query.Count();
            Console.WriteLine("Collect results in " + watch.ElapsedMilliseconds + " ms\nSMEs found\t" + result + "/" + db.SMESets.Count());

            return result;
        }

        // --------------- SM Methods ---------------
        private IQueryable<CombinedSMResult>? GetSMs(List<string> messages, AasContext db, string semanticId = "", string identifier = "", string diffString = "", string expression = "")
        {
            // analyse parameters
            var withSemanticId = !semanticId.IsNullOrEmpty();
            var withIdentifier = !identifier.IsNullOrEmpty();
            var diff = TimeStamp.TimeStamp.StringToDateTime(diffString);
            var withDiff = !diff.Equals(DateTime.MinValue);
            var withParameters = withSemanticId || withIdentifier || withDiff;
            var withExpression = !expression.IsNullOrEmpty();
            var text = "";

            // wrong parameters
            if (withExpression == withParameters)
                return null;

            var log = false;
            int withQueryLanguage = 0;

            if (expression.StartsWith("$LOG"))
            {
                log = true;
                expression = expression.Replace("$LOG", "");
                Console.WriteLine("$LOG");
                messages.Add("$LOG");
            }

            if (expression.StartsWith("$QL"))
            {
                withQueryLanguage = 1;
                expression = expression.Replace("$QL", "");
                Console.WriteLine("$QL");
                messages.Add("$QL");
            }

            if (expression.StartsWith("$JSON"))
            {
                withQueryLanguage = 2;
                expression = expression.Replace("$JSON", "");
                Console.WriteLine("$JSON");
                messages.Add("$JSON");
            }

            // get data
            IQueryable<SMSet> smTable;
            if (withExpression) // with expression
            {
                // shorten expression
                expression = expression.Replace("$REVERSE", "");

                string combinedCondition = "";
                string conditionSM = "";

                if (withQueryLanguage == 0)
                {
                    expression = Regex.Replace(expression, @"\s+", replacement: string.Empty);

                    // init parser
                    var countTypePrefix = 0;
                    var parser = new ParserWithAST(new Lexer(expression));
                    var ast = parser.Parse();

                    // combined condition
                    combinedCondition = parser.GenerateSql(ast, "", ref countTypePrefix, "filter");
                    text = "combinedCondition: " + combinedCondition;
                    Console.WriteLine(text);
                    messages.Add(text);

                    // sm condition
                    conditionSM = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel");
                    if (conditionSM.IsNullOrEmpty())
                        conditionSM = parser.GenerateSql(ast, "sm.", ref countTypePrefix, "filter");
                    conditionSM = conditionSM.Replace("sm.", "");
                    text = "conditionSM: " + conditionSM;
                    Console.WriteLine(text);
                    messages.Add(text);

                    // check restrictions
                    var restrictSM = !conditionSM.IsNullOrEmpty() && !conditionSM.Equals("true");

                    // get data
                    if (!restrictSM)
                        return null;
                }
                else if (withQueryLanguage == 1)
                {
                    // with newest query language from QueryParser.cs
                    // var grammar = new QueryGrammar();
                    var parser = new Parser(grammar);
                    parser.Context.TracingEnabled = true;
                    var parseTree = parser.Parse(expression);

                    if (parseTree.HasErrors())
                    {
                        var pos = parser.Context.CurrentToken.Location.Position;
                        text = expression.Substring(0, pos) + "$$$" + expression.Substring(pos);
                        text = string.Join("\n", parseTree.ParserMessages) + "\nSee $$$: " + text;
                        Console.WriteLine(text);
                        messages.Add(text);
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
                        text = "combinedCondition: " + combinedCondition;
                        Console.WriteLine(text);
                        messages.Add(text);

                        countTypePrefix = 0;
                        conditionSM = grammar.ParseTreeToExpression(parseTree.Root, "sm.", ref countTypePrefix);
                        if (conditionSM == "$SKIP")
                        {
                            conditionSM = "";
                        }
                        text = "conditionSM: " + conditionSM;
                        Console.WriteLine(text);
                        messages.Add(text);
                    }
                }
                else
                {
                    // JSON Query Language from JsonParser.cs
                    var jParser = new JsonParser();
                    messages.Add("");

                    int countTypePrefix = 0;
                    combinedCondition = jParser.ParseTreeToExpression(expression, "", ref countTypePrefix);
                    messages.Add("combinedCondition: " + combinedCondition);

                    countTypePrefix = 0;
                    conditionSM = jParser.ParseTreeToExpression(expression, "sm.", ref countTypePrefix);
                    if (conditionSM == "$SKIP")
                    {
                        conditionSM = "";
                    }
                    messages.Add("conditionSM: " + conditionSM);
                    messages.Add("");
                }

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

        // --------------- SME Methods ---------------
        private IQueryable? GetSMEs(List<string> messages, string requested, AasContext db,
            bool withCount = false, string smSemanticId = "", string smIdentifier = "", string semanticId = "",
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
            string text = "";

            // wrong parameters
            if (withExpression == withParameters ||
                (withEqualString && (withContains || withLower || withUpper)) ||
                (withContains && (withLower || withUpper)) ||
                (withLower != withUpper) || requested.IsNullOrEmpty())
                return null;

            // direction
            var direction = 0; // 0 = top-down, 1 = middle-out, 2 = bottom-up

            // restrict all tables seperate
            IQueryable<SMSet> smTable;
            IQueryable<SMESet> smeTable;
            IQueryable<SValueSet>? sValueTable;
            IQueryable<IValueSet>? iValueTable;
            IQueryable<DValueSet>? dValueTable;

            var log = false;
            int withQueryLanguage = 0;

            if (withExpression && expression.StartsWith("$LOG"))
            {
                log = true;
                expression = expression.Replace("$LOG", "");
                Console.WriteLine("$LOG");
                messages.Add("$LOG");
            }

            if (withExpression && expression.StartsWith("$QL"))
            {
                withQueryLanguage = 1;
                expression = expression.Replace("$QL", "");
                Console.WriteLine("$QL");
                messages.Add("$QL");
            }

            if (expression.StartsWith("$JSON"))
            {
                withQueryLanguage = 2;
                expression = expression.Replace("$JSON", "");
                Console.WriteLine("$JSON");
                messages.Add("$JSON");
            }

            // check additional columns for expression
            var needSmSemanticId = false;
            var needSmIdShort = false;
            var needSmDisplayName = false;
            var needSmDescription = false;
            var needSmId = false;
            var needSmeSemanticId = false;
            var needSmeIdShort = false;
            var needSmeDisplayName = false;
            var needSmeDescription = false;
            var needSmeValue = false;
            var needSmeValueType = false; // <-- is unclear

            // get data
            var combiCondition = string.Empty; // this saves the combinedCondition but in a way that raw sql can work with it
            if (withExpression) // with expression
            {
                // direction
                direction = !expression.Contains("$REVERSE") ? 0 : 2;

                // shorten expression
                expression = expression.Replace("$REVERSE", "").Replace("$LOG", "");

                var combinedCondition = string.Empty;
                var conditionSM = string.Empty;
                var conditionSME = string.Empty;
                var conditionStr = string.Empty;
                var conditionNum = string.Empty;

                if (withQueryLanguage == 0)
                {
                    messages.Add("");

                    expression = Regex.Replace(expression, @"\s+", string.Empty);

                    // init parser
                    var countTypePrefix = 0;
                    var parser = new ParserWithAST(new Lexer(expression));
                    var ast = parser.Parse();

                    // combined condition
                    combinedCondition = parser.GenerateSql(ast, "", ref countTypePrefix, "filter");
                    text = "combinedCondition: " + combinedCondition;
                    Console.WriteLine(text);
                    messages.Add(text);

                    // sm condition
                    conditionSM = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel");
                    if (conditionSM.IsNullOrEmpty())
                        conditionSM = parser.GenerateSql(ast, "sm.", ref countTypePrefix, "filter");
                    conditionSM = conditionSM.Replace("sm.", "");
                    text = "conditionSM: " + conditionSM;
                    Console.WriteLine(text);
                    messages.Add(text);

                    // sme condition
                    conditionSME = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel_elements");
                    if (conditionSME.IsNullOrEmpty())
                        conditionSME = parser.GenerateSql(ast, "sme.", ref countTypePrefix, "filter");
                    conditionSME = conditionSME.Replace("sme.", "");
                    text = "conditionSME: " + conditionSME;
                    Console.WriteLine(text);
                    messages.Add(text);

                    // string condition
                    conditionStr = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_str");
                    if (conditionStr.IsNullOrEmpty())
                        conditionStr = parser.GenerateSql(ast, "sValue", ref countTypePrefix, "filter");
                    if (conditionStr.Equals("true"))
                        conditionStr = "sValue != null";
                    conditionStr = conditionStr.Replace("sValue", "Value");
                    text = "conditionStr: " + conditionStr;
                    Console.WriteLine(text);
                    messages.Add(text);

                    // num condition
                    conditionNum = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_num");
                    if (conditionNum.IsNullOrEmpty())
                        conditionNum = parser.GenerateSql(ast, "mValue", ref countTypePrefix, "filter");
                    if (conditionNum.Equals("true"))
                        conditionNum = "mValue != null";
                    conditionNum = conditionNum.Replace("mValue", "Value");
                    text = "conditionNum: " + conditionNum;
                    Console.WriteLine(text);
                    messages.Add(text);

                    messages.Add("");
                }
                else if (withQueryLanguage == 1)
                {
                    // with newest query language from QueryParser.cs
                    // var grammar = new QueryGrammar();
                    var parser = new Parser(grammar);
                    parser.Context.TracingEnabled = true;
                    var parseTree = parser.Parse(expression);

                    if (parseTree.HasErrors())
                    {
                        var pos = parser.Context.CurrentToken.Location.Position;
                        text = expression.Substring(0, pos) + "$$$" + expression.Substring(pos);
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
                        messages.Add("");

                        // Security
                        if (parseTree.Root.Term.Name == "AllRules")
                        {
                            grammar.ParseAccessRules(parseTree.Root);
                            throw new Exception("Access Rules parsed!");
                        }

                        int countTypePrefix = 0;
                        combinedCondition = grammar.ParseTreeToExpression(parseTree.Root, "", ref countTypePrefix);
                        text = "combinedCondition: " + combinedCondition;
                        Console.WriteLine(text);
                        messages.Add(text);

                        countTypePrefix = 0;
                        conditionSM = grammar.ParseTreeToExpression(parseTree.Root, "sm.", ref countTypePrefix);
                        if (conditionSM == "$SKIP")
                        {
                            conditionSM = "";
                        }
                        text = "conditionSM: " + conditionSM;
                        Console.WriteLine(text);
                        messages.Add(text);

                        countTypePrefix = 0;
                        conditionSME = grammar.ParseTreeToExpression(parseTree.Root, "sme.", ref countTypePrefix);
                        if (conditionSME == "$SKIP")
                        {
                            conditionSME = "";
                        }
                        text = "conditionSME: " + conditionSME;
                        Console.WriteLine(text);
                        messages.Add(text);

                        countTypePrefix = 0;
                        conditionStr = grammar.ParseTreeToExpression(parseTree.Root, "str()", ref countTypePrefix);
                        if (conditionStr == "$SKIP")
                        {
                            conditionStr = "";
                        }
                        text = "conditionStr: " + conditionStr;
                        Console.WriteLine(text);
                        messages.Add(text);

                        countTypePrefix = 0;
                        conditionNum = grammar.ParseTreeToExpression(parseTree.Root, "num()", ref countTypePrefix);
                        if (conditionNum == "$SKIP")
                        {
                            conditionNum = "";
                        }
                        text = "conditionNum: " + conditionNum;
                        Console.WriteLine(text);
                        messages.Add(text);

                        messages.Add("");
                    }
                }
                else
                {
                    // JSON Query Language from JsonParser.cs
                    var jParser = new JsonParser();
                    messages.Add("");

                    int countTypePrefix = 0;
                    combinedCondition = jParser.ParseTreeToExpression(expression, "", ref countTypePrefix);
                    messages.Add("combinedCondition: " + combinedCondition);

                    countTypePrefix = 0;
                    conditionSM = jParser.ParseTreeToExpression(expression, "sm.", ref countTypePrefix);
                    if (conditionSM == "$SKIP")
                    {
                        conditionSM = "";
                    }
                    messages.Add("conditionSM: " + conditionSM);

                    countTypePrefix = 0;
                    conditionSME = jParser.ParseTreeToExpression(expression, "sme.", ref countTypePrefix);
                    if (conditionSME == "$SKIP")
                    {
                        conditionSME = "";
                    }
                    messages.Add("conditionSME: " + conditionSME);

                    countTypePrefix = 0;
                    conditionStr = jParser.ParseTreeToExpression(expression, "str()", ref countTypePrefix);
                    if (conditionStr == "$SKIP")
                    {
                        conditionStr = "";
                    }
                    messages.Add("conditionStr: " + conditionStr);

                    countTypePrefix = 0;
                    conditionNum = jParser.ParseTreeToExpression(expression, "num()", ref countTypePrefix);
                    if (conditionNum == "$SKIP")
                    {
                        conditionNum = "";
                    }
                    messages.Add("conditionNum: " + conditionNum);
                    messages.Add("");
                }

                // check restrictions
                var restrictSM = !conditionSM.IsNullOrEmpty() && !conditionSM.Equals("true");
                var restrictSME = !conditionSME.IsNullOrEmpty() && !conditionSME.Equals("true");
                var restrictSVaue = !conditionStr.IsNullOrEmpty() && !conditionStr.Equals("true");
                var restrictNumVaue = !conditionNum.IsNullOrEmpty() && !conditionNum.Equals("true");
                var restrictValue = restrictSVaue || restrictNumVaue;

                // restrict all tables seperate 
                smTable = restrictSM ? db.SMSets.Where(conditionSM) : db.SMSets;
                smeTable = restrictSME ? db.SMESets.Where(conditionSME) : db.SMESets;
                sValueTable = restrictValue ? (restrictSVaue ? db.SValueSets.Where(conditionStr) : null) : db.SValueSets;
                iValueTable = restrictValue ? (restrictNumVaue ? db.IValueSets.Where(conditionNum) : null) : db.IValueSets;
                dValueTable = restrictValue ? (restrictNumVaue ? db.DValueSets.Where(conditionNum) : null) : db.DValueSets;

                // check additional columns for expression
                needSmSemanticId = combinedCondition.Contains("sm.semanticId");
                needSmIdShort = combinedCondition.Contains("sm.idShort");
                needSmDisplayName = combinedCondition.Contains("sm.displayName");
                needSmDescription = combinedCondition.Contains("sm.description");
                needSmId = combinedCondition.Contains("sm.id");
                needSmeSemanticId = combinedCondition.Contains("sme.semanticId");
                needSmeIdShort = combinedCondition.Contains("sme.idShort");
                needSmeDisplayName = combinedCondition.Contains("sme.displayName");
                needSmeDescription = combinedCondition.Contains("sme.description");
                needSmeValue = combinedCondition.Contains("sme.value");
                needSmeValueType = combinedCondition.Contains("sme.valueType");

                // convert to sql
                combiCondition = ConvertToSqlString(combinedCondition);
                combiCondition = "WHERE " + combiCondition;
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
                sValueTable = restrictValue ? (restrictSValue ? db.SValueSets.Where(v => restrictSValue && v.Value != null && (!withContains || v.Value.Contains(contains)) && (!withEqualString || v.Value.Equals(equal))) : null) : db.SValueSets;
                iValueTable = restrictValue ? (restrictNumValue ? db.IValueSets.Where(v => restrictNumValue && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))) : null) : db.IValueSets;
                dValueTable = restrictValue ? (restrictNumValue ? db.DValueSets.Where(v => restrictNumValue && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))) : null) : db.DValueSets;
            }

            // set select
            var smSelect = smTable.Select(sm => new {
                sm.Id,
                SemanticId = needSmSemanticId ? sm.SemanticId : null,
                IdShort = needSmIdShort ? sm.IdShort : null,
                DisplayName = needSmDisplayName ? sm.DisplayName : null,
                Description = needSmDescription ? sm.Description : null,
                sm.Identifier
            });
            var smeSelect = smeTable.Select(sme => new {
                sme.SMId,
                sme.Id,
                SemanticId = needSmeSemanticId ? sme.SemanticId : null,
                IdShort = needSmeIdShort ? sme.IdShort : null,
                DisplayName = needSmeDisplayName ? sme.DisplayName : null,
                Description = needSmeDescription ? sme.Description : null,
                sme.TimeStamp,
                sme.TValue
            });
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
                // add SM_ and SME_ to front 
                combiCondition = AddSMToFront(combiCondition);
                combiCondition = AddSMEToFront(combiCondition);

                // SM => SME
                rawSQL +=
                    $"FilteredSMAndSME AS ( \n" +
                        $"SELECT " +
                            $"{(needSmSemanticId ? "sm.SemanticId AS SM_SemanticId, " : string.Empty)}" +
                            $"{(needSmIdShort ? "sm.IdShort AS SM_IdShort, " : string.Empty)}" +
                            $"{(needSmDisplayName ? "sm.DisplayName AS SM_DisplayName, " : string.Empty)}" +
                            $"{(needSmDescription ? "sm.Description AS SM_Description, " : string.Empty)}" +
                            $"sm.Identifier, " +

                            $"{(needSmeSemanticId ? "sme.SemanticId AS SME_SemanticId, " : string.Empty)}" +
                            $"{(needSmeIdShort ? "sme.IdShort AS SME_IdShort, " : string.Empty)}" +
                            $"{(needSmeDisplayName ? "sme.DisplayName AS SME_DisplayName, " : string.Empty)}" +
                            $"{(needSmeDescription ? "sme.Description AS SME_Description, " : string.Empty)}" +
                            $"sme.Id, " +
                            $"sme.TimeStamp, " +
                            $"sme.TValue \n" +
                        $"FROM FilteredSM AS sm \n" +
                        $"INNER JOIN FilteredSME AS sme ON sm.Id = sme.SMId \n" +
                    $"), \n";

                // SM-SME => VALUE
                var selectStart =
                    $"SELECT " +
                        $"{(needSmSemanticId ? "sm_sme.SM_SemanticId, " : string.Empty)}" +
                        $"{(needSmIdShort ? "sm_sme.SM_IdShort, " : string.Empty)}" +
                        $"{(needSmDisplayName ? "sm_sme.SM_DisplayName, " : string.Empty)}" +
                        $"{(needSmDescription ? "sm_sme.SM_Description, " : string.Empty)}" +
                        $"sm_sme.Identifier, " +

                        $"{(needSmeSemanticId ? "sm_sme.SME_SemanticId, " : string.Empty)}" +
                        $"{(needSmeIdShort ? "sm_sme.SME_IdShort, " : string.Empty)}" +
                        $"{(needSmeDisplayName ? "sm_sme.SME_DisplayName, " : string.Empty)}" +
                        $"{(needSmeDescription ? "sm_sme.SME_Description, " : string.Empty)}" +
                        $"sm_sme.Id, " +
                        $"sm_sme.TimeStamp, " +
                        $"v.Value \n" +
                    $"FROM FilteredSMAndSME AS sm_sme \n" +
                    $"INNER JOIN ";
                var selectEnd = $" AS v ON sm_sme.Id = v.SMEId \n" +
                    $"{combiCondition.Replace("sme.", "sm_sme.").Replace("sm.", "sm_sme.")}\n ";
                rawSQL +=
                    $"FilteredSMAndSMEAndValue AS ( \n" +
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredSValue{selectEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredIValue{selectEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredDValue{selectEnd}" : string.Empty) +
                        ((withParameters && !withContains && !withEqualString && !withEqualNum && !withCompare) ?
                            $"UNION ALL \n" +
                            $"SELECT " +
                                $"{(needSmSemanticId ? "sm_sme.SM_SemanticId, " : string.Empty)}" +
                                $"{(needSmIdShort ? "sm_sme.SM_IdShort, " : string.Empty)}" +
                                $"{(needSmDisplayName ? "sm_sme.SM_DisplayName, " : string.Empty)}" +
                                $"{(needSmDescription ? "sm_sme.SM_Description, " : string.Empty)}" +
                                $"sm_sme.Identifier, " +

                                $"{(needSmeSemanticId ? "sm_sme.SME_SemanticId, " : string.Empty)}" +
                                $"{(needSmeIdShort ? "sm_sme.SME_IdShort, " : string.Empty)}" +
                                $"{(needSmeDisplayName ? "sm_sme.SME_DisplayName, " : string.Empty)}" +
                                $"{(needSmeDescription ? "sm_sme.SME_Description, " : string.Empty)}" +
                                $"sm_sme.Id, " +
                                $"sm_sme.TimeStamp, " +
                                $"NULL \n" +
                            $"FROM FilteredSMAndSME AS sm_sme \n" +
                            $"WHERE sm_sme.TValue IS NULL OR sm_sme.TValue = '' {(withExpression ? $"AND ({combiCondition})" : string.Empty)} \n" : string.Empty) +
                    $")";
            }
            else if (direction == 1) // middle-out
            {
                // add SM_ and SME_ to front 
                combiCondition = AddSMToFront(combiCondition);
                combiCondition = AddSMEToFront(combiCondition);

                // SME => SM
                rawSQL +=
                    $"FilteredSMAndSME AS ( \n" +
                        $"SELECT " +
                            $"{(needSmSemanticId ? "sm.SemanticId AS SM_SemanticId, " : string.Empty)}" +
                            $"{(needSmIdShort ? "sm.IdShort AS SM_IdShort, " : string.Empty)}" +
                            $"{(needSmDisplayName ? "sm.DisplayName AS SM_DisplayName, " : string.Empty)}" +
                            $"{(needSmDescription ? "sm.Description AS SM_Description, " : string.Empty)}" +
                            $"sm.Identifier, " +

                            $"{(needSmeSemanticId ? "sme.SemanticId AS SME_SemanticId, " : string.Empty)}" +
                            $"{(needSmeIdShort ? "sme.IdShort AS SME_IdShort, " : string.Empty)}" +
                            $"{(needSmeDisplayName ? "sme.DisplayName AS SME_DisplayName, " : string.Empty)}" +
                            $"{(needSmeDescription ? "sme.Description AS SME_Description, " : string.Empty)}" +
                            $"sme.Id, " +
                            $"sme.TimeStamp, " +
                            $"sme.TValue \n" +
                        $"FROM FilteredSME AS sme \n" +
                        $"INNER JOIN FilteredSM AS sm ON sm.Id = sme.SMId \n" +
                    $"), \n";

                // SME-SM => VALUE
                var selectStart =
                    $"SELECT " +
                        $"{(needSmSemanticId ? "sm_sme.SM_SemanticId, " : string.Empty)}" +
                        $"{(needSmIdShort ? "sm_sme.SM_IdShort, " : string.Empty)}" +
                        $"{(needSmDisplayName ? "sm_sme.SM_DisplayName, " : string.Empty)}" +
                        $"{(needSmDescription ? "sm_sme.SM_Description, " : string.Empty)}" +
                        $"sm_sme.Identifier, " +

                        $"{(needSmeSemanticId ? "sm_sme.SME_SemanticId, " : string.Empty)}" +
                        $"{(needSmeIdShort ? "sm_sme.SME_IdShort, " : string.Empty)}" +
                        $"{(needSmeDisplayName ? "sm_sme.SME_DisplayName, " : string.Empty)}" +
                        $"{(needSmeDescription ? "sm_sme.SME_Description, " : string.Empty)}" +
                        $"sm_sme.Id, " +
                        $"sm_sme.TimeStamp, " +
                        $"v.Value \n" +
                    $"FROM FilteredSMAndSME AS sm_sme \n" +
                    $"INNER JOIN ";
                var selectEnd = $" AS v ON sm_sme.Id = v.SMEId\n" +
                    $"{combiCondition.Replace("sme.", "sm_sme.").Replace("sm.", "sm_sme.")}\n ";
                rawSQL +=
                    $"FilteredSMAndSMEAndValue AS ( \n" +
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredSValue{selectEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredIValue{selectEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectStart}FilteredDValue{selectEnd}" : string.Empty) +
                        ((withParameters && !withContains && !withEqualString && !withEqualNum && !withCompare) ?
                            $"UNION ALL \n" +
                            $"SELECT " +
                                $"{(needSmSemanticId ? "sm_sme.SM_SemanticId, " : string.Empty)}" +
                                $"{(needSmIdShort ? "sm_sme.SM_IdShort, " : string.Empty)}" +
                                $"{(needSmDisplayName ? "sm_sme.SM_DisplayName, " : string.Empty)}" +
                                $"{(needSmDescription ? "sm_sme.SM_Description, " : string.Empty)}" +
                                $"sm_sme.Identifier, " +

                                $"{(needSmeSemanticId ? "sm_sme.SME_SemanticId, " : string.Empty)}" +
                                $"{(needSmeIdShort ? "sm_sme.SME_IdShort, " : string.Empty)}" +
                                $"{(needSmeDisplayName ? "sm_sme.SME_DisplayName, " : string.Empty)}" +
                                $"{(needSmeDescription ? "sm_sme.SME_Description, " : string.Empty)}" +
                                $"sm_sme.Id, " +
                                $"sm_sme.TimeStamp, " +
                                $"NULL \n" +
                            $"FROM FilteredSMAndSME AS sm_sme \n" +
                            $"WHERE sm_sme.TValue IS NULL OR sm_sme.TValue = ''\n" : string.Empty) +
                    $")";
            }
            else if (direction == 2) // bottom-up
            {
                // add SME_ to front 
                combiCondition = AddSMEToFront(combiCondition);

                // VALUE => SME
                var selectWithStart =
                    $"SELECT " +
                        $"sme.SMId, " +

                        $"{(needSmeSemanticId ? "sme.SemanticId AS SME_SemanticId, " : string.Empty)}" +
                        $"{(needSmeIdShort ? "sme.IdShort AS SME_IdShort, " : string.Empty)}" +
                        $"{(needSmeDisplayName ? "sme.DisplayName AS SME_DisplayName, " : string.Empty)}" +
                        $"{(needSmeDescription ? "sme.Description AS SME_Description, " : string.Empty)}" +
                        $"sme.Id, " +
                        $"sme.TimeStamp, " +
                        $"v.Value \n " +
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
                            $"SELECT " +
                                $"sme.SMId, " +

                                $"{(needSmeSemanticId ? "sme.SemanticId AS SME_SemanticId, " : string.Empty)}" +
                                $"{(needSmeIdShort ? "sme.IdShort AS SME_IdShort, " : string.Empty)}" +
                                $"{(needSmeDisplayName ? "sme.DisplayName AS SME_DisplayName, " : string.Empty)}" +
                                $"{(needSmeDescription ? "sme.Description AS SME_Description, " : string.Empty)}" +
                                $"sme.Id, " +
                                $"sme.TimeStamp, " +
                                $"NULL \n" +
                            $"FROM FilteredSME AS sme \n" +
                            $"WHERE sme.TValue IS NULL OR sme.TValue = ''\n" : string.Empty) +
                    $"), \n";

                // VALUE-SME => SM
                rawSQL +=
                    $"FilteredSMAndSMEAndValue AS ( \n" +
                        $"SELECT " +
                            $"{(needSmSemanticId ? "sm.SemanticId AS SM_SemanticId, " : string.Empty)}" +
                            $"{(needSmIdShort ? "sm.IdShort AS SM_IdShort, " : string.Empty)}" +
                            $"{(needSmDisplayName ? "sm.DisplayName AS SM_DisplayName, " : string.Empty)}" +
                            $"{(needSmDescription ? "sm.Description AS SM_Description, " : string.Empty)}" +
                            $"sm.Identifier, " +

                            $"{(needSmeSemanticId ? "sme_v.SME_SemanticId, " : string.Empty)}" +
                            $"{(needSmeIdShort ? "sme_v.SME_IdShort, " : string.Empty)}" +
                            $"{(needSmeDisplayName ? "sme_v.SME_DisplayName, " : string.Empty)}" +
                            $"{(needSmeDescription ? "sme_v.SME_Description, " : string.Empty)}" +
                            $"sme_v.Id, " +
                            $"sme_v.TimeStamp, " +
                            $"sme_v.Value \n" +
                        $"FROM FilteredSMEAndValue AS sme_v \n" +
                        $"INNER JOIN FilteredSM AS sm ON sme_v.SMId = sm.Id \n" +
                        $"{combiCondition.Replace("v.", "sme_v.").Replace("sme.", "sme_v.")}\n" +
                    $")";
            }

            if (withCount)
            {
                // select
                rawSQL += $"\nSELECT sme.Id \nFROM FilteredSMAndSMEAndValue AS sme";

                // create queryable
                var resultCount = db.Database.SqlQueryRaw<int>(rawSQL);
                return resultCount;
            }


            /* old

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

            // select
            rawSQL += $"SELECT sme.Identifier, r.IdShortPath, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', sme.TimeStamp) AS TimeStamp, sme.Value \n" +
                "FROM FilteredSMAndSMEAndValue AS sme \n" +
                "INNER JOIN RecursiveSME AS r ON sme.Id = r.Id";

            // create queryable
            var result = db.Database.SqlQueryRaw<CombinedSMEResult>(rawSQL);

            return result;
            */

            if (requested.Contains("idShortPath") || requested.Contains("url"))
            {
                // Append the "get path" section
                rawSQL += $"\n ," +
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
            }

            if (requested.Contains("idShortPath") || requested.Contains("url"))
            {
                // Join with RecursiveSME if path is included
                rawSQL += $"SELECT sme.Identifier, r.IdShortPath, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', sme.TimeStamp) AS TimeStamp, sme.Value \n" +
                    "FROM FilteredSMAndSMEAndValue AS sme \n" +
                    "INNER JOIN RecursiveSME AS r ON sme.Id = r.Id \n";
            }
            else
            {
                // Complete the select statement
                rawSQL += $"SELECT sme.Identifier, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', sme.TimeStamp) AS TimeStamp, sme.Value \n" +
                          "FROM FilteredSMAndSMEAndValue AS sme \n";
            }

            // Create queryable
            var result = db.Database.SqlQueryRaw<CombinedSMEResult>(rawSQL);

            return result;
        }

        public static string AddSMToFront(string prediction)
        {
            var sqlString = prediction
                .Replace("sm.semanticId", "sm.SM_SemanticId")
                .Replace("sm.idShort", "sm.SM_IdShort")
                .Replace("sm.displayName", "sm.SM_DisplayName")
                .Replace("sm.description", "sm.SM_Description");

            return sqlString;
        }

        public static string AddSMEToFront(string prediction)
        {
            var sqlString = prediction
                .Replace("sme.semanticId", "sme.SME_SemanticId")
                .Replace("sme.idShort", "sme.SME_IdShort")
                .Replace("sme.displayName", "sme.SME_DisplayName")
                .Replace("sme.description", "sme.SME_Description");

            return sqlString;
        }

        public static string ConvertToSqlString(string prediction)
        {
            prediction = Regex.Replace(prediction, @"\.Contains\(""([^""]+)""\)", " LIKE '%$1%'");
            var sqlString = prediction
                .Replace("&&", " AND ")
                .Replace("||", " OR ")
                .Replace("mvalue", "v.Value")
                .Replace("svalue", "v.Value");

            return sqlString;
        }

        private static List<SMEResult> GetSMEResult(string requested, IQueryable<CombinedSMEResult> query)
        {
            bool withSmId = requested.Contains("smId");
            bool withTimeStamp = requested.Contains("timeStamp");
            bool withValue = requested.Contains("value");
            bool withIdShortPath = requested.Contains("idShortPath");
            bool withUrl = requested.Contains("url");

            var q = query.Select(sm_sme_v => new SMEResult
            {
                smId = withSmId ? sm_sme_v.Identifier : null,
                timeStamp = withTimeStamp ? sm_sme_v.TimeStamp : null,
                value = withValue ? sm_sme_v.Value : null,
                idShortPath = withIdShortPath ? sm_sme_v.IdShortPath : null,
                url = withUrl && sm_sme_v.Identifier != null && sm_sme_v.IdShortPath != null
                    ? $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm_sme_v.Identifier)}/submodel-elements/{sm_sme_v.IdShortPath}"
                    : null
            });

            var smeResults = q.ToList();

            return smeResults;
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
