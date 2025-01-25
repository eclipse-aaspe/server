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
    using HotChocolate.Resolvers;
    using HotChocolate.Language;

    public partial class Query
    {
        /*
        public Query(QueryGrammar queryGrammar)
        {
            grammar = queryGrammar;
        }
        private readonly QueryGrammar grammar;
        */

        public Query(QueryGrammarJSON queryGrammar)
        {
            grammar = queryGrammar;
        }
        private readonly QueryGrammarJSON grammar;
        public static string? ExternalBlazor { get; set; }

        // --------------- API ---------------
        public QResult SearchSMs(IResolverContext context, string semanticId = "", string identifier = "", string diff = "", string expression = "")
        {
            var qResult = new QResult()
            {
                Messages = new List<string>(),
                SMResults = new List<SMResult>(),
                SMEResults = new List<SMEResult>()
            };

            var text = string.Empty;

            var watch = Stopwatch.StartNew();
            using AasContext db = new();
            Console.WriteLine("\nSearchSMs");

            watch.Restart();
            var query = GetSMs(qResult.Messages, db, false, semanticId, identifier, diff, expression);
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
                var result = GetSMResult((IQueryable<CombinedSMResult>)query);
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
            var query = GetSMs(new List<string>(), db, true, semanticId, identifier, diff, expression);
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
            var requested = string.Empty;
            var requestedFields = context.Selection.SyntaxNode.SelectionSet.Selections;
            foreach (var selection in requestedFields)
            {
                if (selection is FieldNode field)
                {
                    requested += " " + field.ToString();
                }
            }

            var qResult = new QResult()
            {
                Messages = new List<string>(),
                SMResults = new List<SMResult>(),
                SMEResults = new List<SMEResult>()
            };

            var text = string.Empty;

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
        private IQueryable? GetSMs(List<string> messages, AasContext db, bool withCount = false, string semanticId = "", string identifier = "", string diffString = "", string expression = "")
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

            // direction 0 = top-down, 1 = middle-out, 2 = bottom-up
            var direction = 0;
            if (expression.Contains("$BOTTOMUP") || expression.Contains("$REVERSE"))
            {
                messages.Add("$BOTTOMUP");
                direction = 2;
            }
            if (expression.Contains("$MIDDLEOUT"))
            {
                messages.Add("$MIDDLEOUT");
                direction = 1;
            }

            // shorten expression
            expression = expression.Replace("$REVERSE", string.Empty);
            expression = expression.Replace("$BOTTOMUP", string.Empty);
            expression = expression.Replace("$MIDDLEOUT", string.Empty);

            // get condition out of expression
            var conditionsExpression = ConditionFromExpression(messages, expression);

            var idShortPathList = new List<string>();
            var conditionList = new List<string>();
            var conditionAll = conditionsExpression["all"];
            if (conditionAll.Contains("$$path$$"))
            {
                var split1 = conditionAll.Split("$$");
                for (int i = 0; i < split1.Length; i++)
                {
                    if (split1[i] == "path")
                    {
                        var idShortPath = split1[i + 1];
                        if (!idShortPathList.Contains(idShortPath))
                        {
                            idShortPathList.Add(idShortPath);
                        }
                        // conditionAllNew += "$$ANY$$((sme.idShortPath==\"" + idShortPath + "\")&&" + split1[i + 2] + ")";
                        var c = "((sme.idShortPath==\"" + idShortPath + "\")&&" + split1[i + 2] + ")";
                        conditionList.Add(c);
                        i += 2;
                    }
                }
            }

            var smeFilteredByIdShort = getFilteredSMESets(db.SMESets, idShortPathList);
            // var x = smeFilteredByIdShort.Where("@0.Any(sm => sm.IdShortPath == @1)", smeFilteredByIdShort, idShortPathList[0]);
            // var x = smeFilteredByIdShort.Where("Any(IdShortPath == @0)", idShortPathList[0]);
            // var x = smeFilteredByIdShort.Where($"Exists(SMESet.Where(IdShortPath == @0))", idShortPathList[0]);
            // var x = smeFilteredByIdShort.Where("Any(@0.Where(IdShortPath == @1))", db.SMESets, idShortPathList[0]);

            var withPath = idShortPathList.Count != 0;
            if (withPath)
            {
                messages.Add("PATH SEARCH");
                conditionsExpression["all"] = "";
                conditionsExpression["sme"] = "true";
                conditionsExpression["svalue"] = "true";
                conditionsExpression["nvalue"] = "true";
            }
            else
            {
                messages.Add("RECURSIVE SEARCH");
            }

            // restrictions in which tables 
            var restrictSM = false;
            var restrictSME = false;
            var restrictSValue = false;
            var restrictNValue = false;
            var restrictValue = false;

            // restrict all tables seperate
            IQueryable<CombinedSMSMEV> comTable;
            IQueryable<SMSet> smTable;
            IQueryable<SMESet> smeTable;
            IQueryable<SValueSet>? sValueTable;
            IQueryable<IValueSet>? iValueTable;
            IQueryable<DValueSet>? dValueTable;

            // get data
            if (withExpression) // with expression
            {
                // check restrictions
                restrictSM = !conditionsExpression["sm"].IsNullOrEmpty() && !conditionsExpression["sm"].Equals("true");
                restrictSME = !conditionsExpression["sme"].IsNullOrEmpty() && !conditionsExpression["sme"].Equals("true");
                restrictSValue = !conditionsExpression["svalue"].IsNullOrEmpty() && !conditionsExpression["svalue"].Equals("true");
                restrictNValue = !conditionsExpression["nvalue"].IsNullOrEmpty() && !conditionsExpression["nvalue"].Equals("true");
                restrictValue = restrictSValue || restrictNValue;

                // restrict all tables seperate
                smTable = restrictSM ? db.SMSets.Where(conditionsExpression["sm"]) : db.SMSets;
                smeTable = restrictSME ? smeFilteredByIdShort.Where(conditionsExpression["sme"]) : smeFilteredByIdShort;
                sValueTable = restrictValue ? (restrictSValue ? db.SValueSets.Where(conditionsExpression["svalue"]) : null) : db.SValueSets;
                iValueTable = restrictValue ? (restrictNValue ? db.IValueSets.Where(conditionsExpression["nvalue"]) : null) : db.IValueSets;
                dValueTable = restrictValue ? (restrictNValue ? db.DValueSets.Where(conditionsExpression["nvalue"]) : null) : db.DValueSets;

                // combine tables to a raw sql 
                var rawSQLEx = CombineTablesToRawSQL(direction, smTable, smeTable, sValueTable, iValueTable, dValueTable, false);
                comTable = db.Database.SqlQueryRaw<CombinedSMSMEV>(rawSQLEx).AsQueryable();

                var combi = "";
                if (!withPath) // recursive search
                {
                    combi = conditionsExpression["all"].Replace("svalue", "V_Value").Replace("mvalue", "V_D_Value").Replace("sm.idShort", "SM_IdShort").Replace("sme.idShort", "SME_IdShort").Replace("sme.idShortPath", "SME_IdShortPath");
                    comTable = comTable.Where(combi);
                }
                else // path search
                {
                    List<IQueryable<CombinedSMSMEV>> comtableCondition = new List<IQueryable<CombinedSMSMEV>>();
                    IQueryable<String?> commonIdentifiers = null;
                    foreach (var condition in conditionList)
                    {
                        var c = condition.Replace("svalue", "V_Value").Replace("mvalue", "V_D_Value").Replace("sm.idShort", "SM_IdShort").Replace("sme.idShort", "SME_IdShort").Replace("sme.idShortPath", "SME_IdShortPath");
                        comtableCondition.Add(comTable.Where(c));
                        if (commonIdentifiers == null)
                        {
                            commonIdentifiers = comTable.Where(c).Select(sm => sm.SM_Identifier).Distinct();
                        }
                        else
                        {
                            commonIdentifiers.Intersect(comTable.Where(c).Select(sm => sm.SM_Identifier)).Distinct();
                        }
                    }
                    comTable = comTable.Where(sm => commonIdentifiers.Contains(sm.SM_Identifier))
                        .GroupBy(sm => sm.SM_Identifier)
                        .Select(sm => sm.FirstOrDefault());
                }
            }
            else // with parameters
            {
                // set conditions
                smTable = db.SMSets
                    .Where(s =>
                        (!withSemanticId || (s.SemanticId != null && s.SemanticId.Equals(semanticId))) &&
                        (!withIdentifier || (s.Identifier != null && s.Identifier.Equals(identifier))) &&
                        (!withDiff || s.TimeStampTree.CompareTo(diff) > 0));

                // Convert to CombinedSMSMEV
                comTable = smTable.Select(sm => new CombinedSMSMEV { SM_Identifier = sm.Identifier, SM_TimeStampTree = sm.TimeStampTree }).Distinct();
            }

            // modifiy raw sql
            var comTableQueryString = comTable.ToQueryString();
            comTableQueryString = ModifiyRawSQL(comTableQueryString);
            comTable = db.Database.SqlQueryRaw<CombinedSMSMEV>(comTableQueryString);

            // select for count
            if (withCount)
            {
                var qCount = comTable.Select(sm => sm.SM_Identifier);
                return qCount;
            }

            // change the first select
            var smRawSQL = comTable.ToQueryString();
            var index = smRawSQL.IndexOf("FROM");
            if (index == -1)
                return null;
            smRawSQL = smRawSQL.Substring(index);
            if (withExpression)
            {
                var prefix = "";
                if (!withPath)
                {
                    prefix = "a.";
                }
                else
                {
                    prefix = "t0.";
                }
                smRawSQL = $"SELECT DISTINCT {prefix}SM_Identifier AS Identifier, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', {prefix}SM_TimeStampTree) AS TimeStampTree \n {smRawSQL}";
            }
            else
            {
                smRawSQL = $"SELECT DISTINCT s.Identifier, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', s.TimeStampTree) AS TimeStampTree \n {smRawSQL}";
            }

            // create queryable
            var result = db.Database.SqlQueryRaw<CombinedSMResult>(smRawSQL);
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
        private IQueryable<SMESet> getFilteredSMESets(DbSet<SMESet> smeSets, List<string> idShortPathList)
        {
            if (idShortPathList.Count == 0)
            {
                return smeSets;
            }

            // Provided list of idShortPath and their depths
            List<int> depthList = idShortPathList.Select(path => path.Split('.').Length).ToList();

            // Prepare the list of valid entries for SQL
            var prefixEntriesSql = $@"(Depth < {depthList[0]} AND '{idShortPathList[0]}' LIKE BuiltIdShortPath || '%')";
            for (int i = 1; i < idShortPathList.Count; i++)
            {
                prefixEntriesSql += $@" OR (Depth < {depthList[i]} AND '{idShortPathList[i]}' LIKE BuiltIdShortPath || '%')";
            }

            var finalEntriesSql = $@"(Depth = {depthList[0]} AND '{idShortPathList[0]}' = BuiltIdShortPath)";
            for (int i = 1; i < idShortPathList.Count; i++)
            {
                finalEntriesSql += $@" OR (Depth = {depthList[i]} AND '{idShortPathList[i]}' = BuiltIdShortPath)";
            }

            // Recursive CTE to build the idShortPath and count the depth
            var sql = $@"
                WITH RECURSIVE RecursiveCTE AS (
                    -- Base case: Select top-level entries
                    SELECT s.*, s.IdShort AS BuiltIdShortPath, 1 AS Depth
                    FROM SMESets s
                    WHERE s.ParentSMEId IS NULL
                    AND (({prefixEntriesSql}) OR ({finalEntriesSql}))

                    UNION ALL

                    -- Recursive case: Select child entries and build the idShortPath
                    SELECT child.*, parent.BuiltIdShortPath || '.' || child.IdShort AS BuiltIdShortPath, parent.Depth + 1 AS Depth
                    FROM SMESets child
                    INNER JOIN RecursiveCTE parent ON child.ParentSMEId = parent.Id
                    AND (({prefixEntriesSql}) OR ({finalEntriesSql}))
                )
                -- Final selection
                SELECT r.Id, r.IdShort, r.BuiltIdShortPath AS IdShortPath, r.SMId, r.ParentSMEId, r.SMEType, r.DisplayName, r.Category, r.Description, r.Extensions, r.SemanticId, r.SupplementalSemanticIds, r.Qualifiers, r.EmbeddedDataSpecifications, r.TValue, r.TimeStampCreate, r.TimeStamp, r.TimeStampTree, r.TimeStampDelete
                FROM RecursiveCTE r
                JOIN SMESets s ON r.Id = s.Id
                WHERE ({finalEntriesSql})";

            // Execute the query
            return smeSets.FromSqlRaw(sql);
        }

        private IQueryable? GetSMEs(List<string> messages, string requested, AasContext db, bool withCount = false,
            string smSemanticId = "", string smIdentifier = "", string semanticId = "",
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
            var withParaWithoutValue = withParameters && !withContains && !withEqualString && !withEqualNum && !withCompare;
            var withExpression = !expression.IsNullOrEmpty();
            var text = string.Empty;

            // wrong parameters
            if (withExpression == withParameters ||
                (withEqualString && (withContains || withLower || withUpper)) ||
                (withContains && (withLower || withUpper)) ||
                (withLower != withUpper) ||
                (!withCount && requested.IsNullOrEmpty()))
                return null;

            // direction 0 = top-down, 1 = middle-out, 2 = bottom-up
            var direction = 0;
            if (withSMSemanticId || withSMIdentifier)
            {
                direction = (withContains || withEqualString || withLower || withUpper) ? 2 : 1;
            }

            if (expression.Contains("$BOTTOMUP") || expression.Contains("$REVERSE"))
            {
                messages.Add("$BOTTOMUP");
                direction = 2;
            }
            if (expression.Contains("$MIDDLEOUT"))
            {
                messages.Add("$MIDDLEOUT");
                direction = 1;
            }

            // shorten expression
            expression = expression.Replace("$REVERSE", string.Empty);
            expression = expression.Replace("$BOTTOMUP", string.Empty);
            expression = expression.Replace("$MIDDLEOUT", string.Empty);

            // get condition out of expression
            var conditionsExpression = ConditionFromExpression(messages, expression);

            var conditionAll = conditionsExpression["all"];
            if (conditionAll.Contains("$$path$$"))
            {
                messages.Add("ERROR: PATH SEARCH not allowed for SME!");
                return null;
            }

            // restrictions in which tables 
            var restrictSM = false;
            var restrictSME = false;
            var restrictSValue = false;
            var restrictNValue = false;
            var restrictValue = false;

            // restrict all tables seperate 
            IQueryable<SMSet> smTable;
            IQueryable<SMESet> smeTable;
            IQueryable<SValueSet>? sValueTable;
            IQueryable<IValueSet>? iValueTable;
            IQueryable<DValueSet>? dValueTable;

            // get data
            if (withExpression) // with expression
            {
                // check restrictions
                restrictSM = !conditionsExpression["sm"].IsNullOrEmpty() && !conditionsExpression["sm"].Equals("true");
                restrictSME =!conditionsExpression["sme"].IsNullOrEmpty() && !conditionsExpression["sme"].Equals("true");
                restrictSValue = !conditionsExpression["svalue"].IsNullOrEmpty() && !conditionsExpression["svalue"].Equals("true");
                restrictNValue = !conditionsExpression["nvalue"].IsNullOrEmpty() && !conditionsExpression["nvalue"].Equals("true");
                restrictValue = restrictSValue || restrictNValue;

                // restrict all tables seperate
                smTable = restrictSM ? db.SMSets.Where(conditionsExpression["sm"]) : db.SMSets;
                smeTable = restrictSME ? db.SMESets.Where(conditionsExpression["sme"]) : db.SMESets;
                sValueTable = restrictValue ? (restrictSValue ? db.SValueSets.Where(conditionsExpression["svalue"]) : null) : db.SValueSets;
                iValueTable = restrictValue ? (restrictNValue ? db.IValueSets.Where(conditionsExpression["nvalue"]) : null) : db.IValueSets;
                dValueTable = restrictValue ? (restrictNValue ? db.DValueSets.Where(conditionsExpression["nvalue"]) : null) : db.DValueSets;
            }
            else // with parameters
            {
                // check restrictions
                restrictSM = withSMSemanticId || withSMIdentifier;
                restrictSME = withSemanticId || withDiff;
                restrictSValue = withContains || withEqualString;
                restrictNValue = withEqualNum || withCompare;
                restrictValue = restrictSValue || restrictNValue;

                // restrict all tables seperate 
                smTable = restrictSM ? db.SMSets.Where(sm => (!withSMSemanticId || (sm.SemanticId != null && sm.SemanticId.Equals(smSemanticId))) && (!withSMIdentifier || (sm.Identifier != null && sm.Identifier.Equals(smIdentifier)))) : db.SMSets;
                smeTable = restrictSME ? db.SMESets.Where(sme => (!withSemanticId || (sme.SemanticId != null && sme.SemanticId.Equals(semanticId))) && (!withDiff || sme.TimeStamp.CompareTo(diff) > 0)) : db.SMESets;
                sValueTable = restrictValue ? (restrictSValue ? db.SValueSets.Where(v => restrictSValue && v.Value != null && (!withContains || v.Value.Contains(contains)) && (!withEqualString || v.Value.Equals(equal))) : null) : db.SValueSets;
                iValueTable = restrictValue ? (restrictNValue ? db.IValueSets.Where(v => restrictNValue && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))) : null) : db.IValueSets;
                dValueTable = restrictValue ? (restrictNValue ? db.DValueSets.Where(v => restrictNValue && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))) : null) : db.DValueSets;
            }

            // combine tables to a raw sql 
            var rawSQLEx = CombineTablesToRawSQL(direction, smTable, smeTable, sValueTable, iValueTable, dValueTable, withParaWithoutValue);

            // create queryable
            var combineTableQuery = db.Database.SqlQueryRaw<CombinedSMSMEV>(rawSQLEx);
            if (withExpression && conditionsExpression.ContainsKey("all"))
            {
                var combi = conditionsExpression["all"].Replace("svalue", "V_Value").Replace("mvalue", "V_D_Value").Replace("sm.idShort", "SM_IdShort").Replace("sme.idShort", "SME_IdShort");
                combineTableQuery = combineTableQuery.Where(combi);

                // modifiy raw sql
                var combineTableQueryString = combineTableQuery.ToQueryString();
                combineTableQueryString = ModifiyRawSQL(combineTableQueryString);
                combineTableQuery = db.Database.SqlQueryRaw<CombinedSMSMEV>(combineTableQueryString);
            }

            // select for count
            if (withCount)
            {
                var qCount = combineTableQuery.Select(sme => sme.SME_Id);
                return qCount;
            }

            // select for not count
            var combineTableQueryS = combineTableQuery.Select(sme => new { sme.SME_Id, sme.SM_Identifier, sme.V_Value, sme.SME_TimeStamp });
            var combineTableRawSQL = combineTableQueryS.ToQueryString();

            if (requested.Contains("idShortPath") || requested.Contains("url"))
            {
                // Append the "get path" section
                combineTableRawSQL =
                    $"WITH FilteredSMAndSMEAndValue AS (\n" +
                        $"{combineTableRawSQL}" +
                    $"\n), \n" +
                    "RecursiveSME AS( \n" +
                        $"WITH RECURSIVE SME_CTE AS ( \n" +
                            $"SELECT Id, IdShort, ParentSMEId, IdShort AS IdShortPath, Id AS StartId \n" +
                            $"FROM SMESets \n" +
                            $"WHERE Id IN (SELECT SME_Id FROM FilteredSMAndSMEAndValue) \n" +
                            $"UNION ALL \n" +
                            $"SELECT x.Id, x.IdShort, x.ParentSMEId, x.IdShort || '.' || c.IdShortPath, c.StartId \n" +
                            $"FROM SMESets x \n" +
                            $"INNER JOIN SME_CTE c ON x.Id = c.ParentSMEId \n" +
                        $") \n" +
                        $"SELECT StartId AS Id, IdShortPath \n" +
                        $"FROM SME_CTE \n" +
                        $"WHERE ParentSMEId IS NULL \n" +
                    $")\n" +
                    $"\n" +
                    $"SELECT sme.SM_Identifier, r.IdShortPath, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', sme.SME_TimeStamp) AS SME_TimeStamp, sme.V_Value \n" +
                        "FROM FilteredSMAndSMEAndValue AS sme \n" +
                        "INNER JOIN RecursiveSME AS r ON sme.SME_Id = r.Id";
            }
            else
            {
                // Complete the select statement
                var index = combineTableRawSQL.IndexOf("FROM");
                if (index == -1)
                    return null;
                combineTableRawSQL = combineTableRawSQL.Substring(index);
                combineTableRawSQL = $"SELECT sme.Identifier, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', sme.TimeStamp) AS TimeStamp, sme.Value \n {combineTableRawSQL}";
            }

            // Create queryable
            var qSearch = db.Database.SqlQueryRaw<CombinedSMEResult>(combineTableRawSQL);
            return qSearch;
        }

        private static List<SMEResult> GetSMEResult(string requested, IQueryable<CombinedSMEResult> query)
        {
            var withSmId = requested.Contains("smId");
            var withTimeStamp = requested.Contains("timeStamp");
            var withValue = requested.Contains("value");
            var withIdShortPath = requested.Contains("idShortPath");
            var withUrl = requested.Contains("url");

            var q = query.Select(sm_sme_v => new SMEResult
            {
                smId = withSmId ? sm_sme_v.SM_Identifier : null,
                timeStamp = withTimeStamp ? sm_sme_v.SME_TimeStamp : null,
                value = withValue ? sm_sme_v.V_Value : null,
                idShortPath = withIdShortPath ? sm_sme_v.IdShortPath : null,
                url = withUrl && sm_sme_v.SM_Identifier != null && sm_sme_v.IdShortPath != null
                    ? $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm_sme_v.SM_Identifier)}/submodel-elements/{sm_sme_v.IdShortPath}"
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

        private static string CombineTablesToRawSQL(int direction, IQueryable<SMSet> smTable, IQueryable<SMESet> smeTable, IQueryable<SValueSet>? sValueTable, IQueryable<IValueSet>? iValueTable, IQueryable<DValueSet>? dValueTable, bool withParaWithoutValue)
        {
            // set select
            var smSelect = smTable.Select(sm => new {
                SM_Id = sm.Id,
                SM_SemanticId = sm.SemanticId,
                SM_IdShort = sm.IdShort,
                SM_DisplayName = sm.DisplayName,
                SM_Description = sm.Description,
                SM_Identifier = sm.Identifier,
                SM_TimeStampTree = sm.TimeStampTree
            });
            var smeSelect = smeTable.Select(sme => new {
                SME_SMId = sme.SMId,
                SME_Id = sme.Id,
                SME_SemanticId = sme.SemanticId,
                SME_IdShort = sme.IdShort,
                SME_IdShortPath = sme.IdShortPath,
                SME_DisplayName = sme.DisplayName,
                SME_Description = sme.Description,
                SME_TimeStamp = sme.TimeStamp,
                SME_TValue = sme.TValue
            });
            var sValueSelect = sValueTable?.Select(sV => new {
                V_SMEId = sV.SMEId,
                V_Value = sV.Value,
                V_D_Value = sV.Value
            });
            var iValueSelect = iValueTable?.Select(iV => new {
                V_SMEId = iV.SMEId,
                V_Value = iV.Value,
                V_D_Value = iV.Value
            });
            var dValueSelect = dValueTable?.Select(dV => new {
                V_SMEId = dV.SMEId,
                V_Value = dV.Value,
                V_D_Value = dV.Value
            });

            // to query string
            var smQueryString = smSelect.ToQueryString();
            var smeQueryString = smeSelect.ToQueryString();
            var sValueQueryString = sValueSelect?.ToQueryString();
            var iValueQueryString = iValueSelect?.ToQueryString();
            var dValueQueryString = dValueSelect?.ToQueryString();

            // modify the raw sql (example set parameters, INSTR)
            smQueryString = ModifiyRawSQL(smQueryString);
            smeQueryString = ModifiyRawSQL(smeQueryString);
            sValueQueryString = ModifiyRawSQL(sValueQueryString);
            iValueQueryString = ModifiyRawSQL(iValueQueryString);
            dValueQueryString = ModifiyRawSQL(dValueQueryString);

            // with querys for each table
            var rawSQL = $"WITH\n" +
                $"FilteredSM AS (\n{smQueryString}\n),\n" +
                $"FilteredSME AS (\n{smeQueryString}\n),\n" +
                (sValueQueryString != null ? $"FilteredSValue AS (\n{sValueQueryString}\n),\n" : string.Empty) +
                (iValueQueryString != null ? $"FilteredIValue AS (\n{iValueQueryString}\n),\n" : string.Empty) +
                (dValueQueryString != null ? $"FilteredDValue AS (\n{dValueQueryString}\n),\n" : string.Empty);

            if (direction == 0) // top-down
            {
                // SM => SME
                rawSQL +=
                    $"FilteredSMAndSME AS ( \n" +
                        $"SELECT " +
                            $"sm.SM_SemanticId, " +
                            $"sm.SM_IdShort, " +
                            $"sm.SM_DisplayName, " +
                            $"sm.SM_Description, " +
                            $"sm.SM_Identifier, " +
                            $"sm.SM_TimeStampTree, " +
                            $"sme.SME_SemanticId, " +
                            $"sme.SME_IdShort, " +
                            $"sme.SME_IdShortPath, " +
                            $"sme.SME_DisplayName, " +
                            $"sme.SME_Description, " +
                            $"sme.SME_Id, " +
                            $"sme.SME_TimeStamp, " +
                            $"sme.SME_TValue \n" +
                        $"FROM FilteredSM AS sm \n" +
                        $"INNER JOIN FilteredSME AS sme ON sm.SM_Id = sme.SME_SMId \n" +
                    $") \n";

                // SM-SME => VALUE
                var selectStart =
                    $"SELECT " +
                        $"sm_sme.SM_SemanticId, " +
                        $"sm_sme.SM_IdShort, " +
                        $"sm_sme.SM_DisplayName, " +
                        $"sm_sme.SM_Description, " +
                        $"sm_sme.SM_Identifier, " +
                        $"sm_sme.SM_TimeStampTree, " +
                        $"sm_sme.SME_SemanticId, " +
                        $"sm_sme.SME_IdShort, " +
                        $"sm_sme.SME_IdShortPath, " +
                        $"sm_sme.SME_DisplayName, " +
                        $"sm_sme.SME_Description, " +
                        $"sm_sme.SME_Id, " +
                        $"sm_sme.SME_TimeStamp, " +
                        $"v.V_Value, ";
                var selectDValueFROM = $" AS V_D_Value \n FROM FilteredSMAndSME AS sm_sme \n" +
                    $"INNER JOIN ";
                var selectEnd = $" AS v ON sm_sme.SME_Id = v.V_SMEId \n";
                rawSQL +=
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectStart}NULL{selectDValueFROM}FilteredSValue{selectEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectStart}v.V_Value{selectDValueFROM}FilteredIValue{selectEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectStart}v.V_Value{selectDValueFROM}FilteredDValue{selectEnd}" : string.Empty) +
                        (withParaWithoutValue ?
                            $"UNION ALL \n" +
                            $"SELECT " +
                                $"sm_sme.SM_SemanticId, " +
                                $"sm_sme.SM_IdShort, " +
                                $"sm_sme.SM_DisplayName, " +
                                $"sm_sme.SM_Description, " +
                                $"sm_sme.SM_Identifier, " +
                                $"sm_sme.SM_TimeStampTree, " +
                                $"sm_sme.SME_SemanticId, " +
                                $"sm_sme.SME_IdShort, " +
                                $"sm_sme.SME_IdShortPath, " +
                                $"sm_sme.SME_DisplayName, " +
                                $"sm_sme.SME_Description, " +
                                $"sm_sme.SME_Id, " +
                                $"sm_sme.SME_TimeStamp, " +
                                $"NULL, NULL \n" +
                            $"FROM FilteredSMAndSME AS sm_sme \n" +
                            $"WHERE sm_sme.SME_TValue IS NULL OR sm_sme.SME_TValue = ''" : string.Empty);
            }
            else if (direction == 1) // middle-out
            {
                // SME => SM
                rawSQL +=
                    $"FilteredSMAndSME AS ( \n" +
                        $"SELECT " +
                            $"sm.SM_SemanticId, " +
                            $"sm.SM_IdShort, " +
                            $"sm.SM_DisplayName, " +
                            $"sm.SM_Description, " +
                            $"sm.SM_Identifier, " +
                            $"sm.SM_TimeStampTree, " +
                            $"sme.SME_SemanticId, " +
                            $"sme.SME_IdShort, " +
                            $"sme.SME_IdShortPath, " +
                            $"sme.SME_DisplayName, " +
                            $"sme.SME_Description, " +
                            $"sme.SME_Id, " +
                            $"sme.SME_TimeStamp, " +
                            $"sme.SME_TValue \n" +
                        $"FROM FilteredSME AS sme \n" +
                        $"INNER JOIN FilteredSM AS sm ON sm.SM_Id = sme.SME_SMId \n" +
                    $") \n";

                // SME-SM => VALUE
                var selectStart =
                    $"SELECT " +
                        $"sm_sme.SM_SemanticId, " +
                        $"sm_sme.SM_IdShort, " +
                        $"sm_sme.SM_DisplayName, " +
                        $"sm_sme.SM_Description, " +
                        $"sm_sme.SM_Identifier, " +
                        $"sm_sme.SM_TimeStampTree, " +
                        $"sm_sme.SME_SemanticId, " +
                        $"sm_sme.SME_IdShort, " +
                        $"sm_sme.SME_IdShortPath, " +
                        $"sm_sme.SME_DisplayName, " +
                        $"sm_sme.SME_Description, " +
                        $"sm_sme.SME_Id, " +
                        $"sm_sme.SME_TimeStamp, " +
                        $"v.V_Value, ";
                var selectDValueFROM = $" AS V_D_Value \n FROM FilteredSMAndSME AS sm_sme \n" +
                    $"INNER JOIN ";
                var selectEnd = $" AS v ON sm_sme.SME_Id = v.V_SMEId\n ";
                rawSQL +=
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectStart}NULL{selectDValueFROM}FilteredSValue{selectEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectStart}v.V_Value{selectDValueFROM}FilteredIValue{selectEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectStart}v.V_Value{selectDValueFROM}FilteredDValue{selectEnd}" : string.Empty) +
                        (withParaWithoutValue ?
                            $"UNION ALL \n" +
                            $"SELECT " +
                                $"sm_sme.SM_SemanticId, " +
                                $"sm_sme.SM_IdShort, " +
                                $"sm_sme.SM_DisplayName, " +
                                $"sm_sme.SM_Description, " +
                                $"sm_sme.SM_Identifier, " +
                                $"sm_sme.SM_TimeStampTree, " +
                                $"sm_sme.SME_SemanticId, " +
                                $"sm_sme.SME_IdShort, " +
                                $"sm_sme.SME_IdShortPath, " +
                                $"sm_sme.SME_DisplayName, " +
                                $"sm_sme.SME_Description, " +
                                $"sm_sme.SME_Id, " +
                                $"sm_sme.SME_TimeStamp, " +
                                $"NULL, NULL \n" +
                            $"FROM FilteredSMAndSME AS sm_sme \n" +
                            $"WHERE sm_sme.SME_TValue IS NULL OR sm_sme.SME_TValue = ''\n" : string.Empty);
            }
            else if (direction == 2) // bottom-up
            {
                // VALUE => SME
                var selectWithStart =
                    $"SELECT " +
                        $"sme.SME_SMId, " +
                        $"sme.SME_SemanticId, " +
                        $"sme.SME_IdShort, " +
                        $"sme.SME_IdShortPath, " +
                        $"sme.SME_DisplayName, " +
                        $"sme.SME_Description, " +
                        $"sme.SME_Id, " +
                        $"sme.SME_TimeStamp, " +
                        $"v.V_Value, ";
                var selectDValueFROM = $" AS V_D_Value \n FROM ";
                var selectWithEnd = $" AS v \n" +
                    $"INNER JOIN FilteredSME AS sme ON sme.SME_Id = v.V_SMEId \n ";
                rawSQL +=
                    $"FilteredSMEAndValue AS (\n" +
                        (!sValueQueryString.IsNullOrEmpty() ? $"{selectWithStart}NULL{selectDValueFROM}FilteredSValue{selectWithEnd}" : string.Empty) +
                        ((!sValueQueryString.IsNullOrEmpty() && !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!iValueQueryString.IsNullOrEmpty() ? $"{selectWithStart}v.V_Value{selectDValueFROM}FilteredIValue{selectWithEnd}" : string.Empty) +
                        ((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                        (!dValueQueryString.IsNullOrEmpty() ? $"{selectWithStart}v.V_Value{selectDValueFROM}FilteredDValue{selectWithEnd}" : string.Empty) +
                        (withParaWithoutValue ?
                            $"UNION ALL \n" +
                            $"SELECT " +
                                $"sme.SME_SMId, " +
                                $"sme.SME_SemanticId, " +
                                $"sme.SME_IdShort, " +
                                $"sme.SME_IdShortPath, " +
                                $"sme.SME_DisplayName, " +
                                $"sme.SME_Description, " +
                                $"sme.SME_Id, " +
                                $"sme.SME_TimeStamp, " +
                                $"NULL, NULL \n" +
                            $"FROM FilteredSME AS sme \n" +
                            $"WHERE sme.SME_TValue IS NULL OR sme.SME_TValue = ''\n" : string.Empty) +
                    $")";

                // VALUE-SME => SM
                rawSQL +=
                    $"SELECT " +
                        $"sm.SM_SemanticId, " +
                        $"sm.SM_IdShort, " +
                        $"sm.SM_DisplayName, " +
                        $"sm.SM_Description, " +
                        $"sm.SM_Identifier, " +
                        $"sm.SM_TimeStampTree, " +
                        $"sme_v.SME_SemanticId, " +
                        $"sme_v.SME_IdShort, " +
                        $"sme_v.SME_IdShortPath, " +
                        $"sme_v.SME_DisplayName, " +
                        $"sme_v.SME_Description, " +
                        $"sme_v.SME_Id, " +
                        $"sme_v.SME_TimeStamp, " +
                        $"sme_v.V_Value, " +
                        $"sme_v.V_D_Value \n" +
                    $"FROM FilteredSMEAndValue AS sme_v \n" +
                    $"INNER JOIN FilteredSM AS sm ON sme_v.SME_SMId = sm.SM_Id \n";
            }

            return rawSQL;
        }

        private Dictionary<string, string> ConditionFromExpression(List<string> messages, string expression)
        {
            var text = string.Empty;
            var condition = new Dictionary<string, string>();

            // no expression
            if (expression.IsNullOrEmpty())
                return condition;

            // log
            var log = false;
            if (expression.StartsWith("$LOG"))
            {
                log = true;
                expression = expression.Replace("$LOG", string.Empty);
                Console.WriteLine("$LOG");
                messages.Add("$LOG");
            }

            // query
            var withQueryLanguage = 0;
            if (expression.StartsWith("$QL"))
            {
                withQueryLanguage = 1;
                expression = expression.Replace("$QL", string.Empty);
                Console.WriteLine("$QL");
                messages.Add("$QL");
            }
            if (expression.StartsWith("$JSONGRAMMAR"))
            {
                withQueryLanguage = 3;
                expression = expression.Replace("$JSONGRAMMAR", string.Empty);
                Console.WriteLine("$JSONGRAMMAR");
                messages.Add("$JSONGRAMMAR");
            }
            if (expression.StartsWith("$JSON"))
            {
                withQueryLanguage = 2;
                expression = expression.Replace("$JSON", string.Empty);
                Console.WriteLine("$JSON");
                messages.Add("$JSON");
            }

            // query language
            if (withQueryLanguage == 0)
            {
                messages.Add("");

                expression = Regex.Replace(expression, @"\s+", string.Empty);

                // init parser
                var countTypePrefix = 0;
                var parser = new ParserWithAST(new Lexer(expression));
                var ast = parser.Parse();

                // combined condition
                condition["all"] = parser.GenerateSql(ast, "", ref countTypePrefix, "filter");
                text = "combinedCondition: " + condition["all"];
                Console.WriteLine(text);
                messages.Add(text);

                // sm condition
                condition["sm"] = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel");
                if (condition["sm"].IsNullOrEmpty())
                    condition["sm"] = parser.GenerateSql(ast, "sm.", ref countTypePrefix, "filter");
                condition["sm"] = condition["sm"].Replace("sm.", "");
                text = "conditionSM: " + condition["sm"];
                Console.WriteLine(text);
                messages.Add(text);

                // sme condition
                condition["sme"] = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_submodel_elements");
                if (condition["sme"].IsNullOrEmpty())
                    condition["sme"] = parser.GenerateSql(ast, "sme.", ref countTypePrefix, "filter");
                condition["sme"] = condition["sme"].Replace("sme.", "");
                text = "conditionSME: " + condition["sme"];
                Console.WriteLine(text);
                messages.Add(text);

                // string condition
                condition["svalue"] = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_str");
                if (condition["svalue"].IsNullOrEmpty())
                    condition["svalue"] = parser.GenerateSql(ast, "sValue", ref countTypePrefix, "filter");
                if (condition["svalue"].Equals("true"))
                    condition["svalue"] = "sValue != null";
                condition["svalue"] = condition["svalue"].Replace("sValue", "Value");
                text = "conditionSValue: " + condition["svalue"];
                Console.WriteLine(text);
                messages.Add(text);

                // num condition
                condition["nvalue"] = parser.GenerateSql(ast, "", ref countTypePrefix, "filter_num");
                if (condition["nvalue"].IsNullOrEmpty())
                    condition["nvalue"] = parser.GenerateSql(ast, "mValue", ref countTypePrefix, "filter");
                if (condition["nvalue"].Equals("true"))
                    condition["nvalue"] = "mValue != null";
                condition["nvalue"] = condition["nvalue"].Replace("mValue", "Value");
                text = "conditionNValue: " + condition["nvalue"];
                Console.WriteLine(text);
                messages.Add(text);

                messages.Add("");
            }
            else if (withQueryLanguage == 1)
            {
                var parser = new Parser(grammar);
                parser.Context.TracingEnabled = true;
                var parseTree = parser.Parse(expression);

                if (parseTree.HasErrors())
                {
                    var pos = parser.Context.CurrentToken.Location.Position;
                    var text2 = expression.Substring(0, pos) + "$$$" + expression.Substring(pos);
                    text2 = string.Join("\n", parseTree.ParserMessages) + "\nSee $$$: " + text2;
                    Console.WriteLine(text2);
                    while (text2 != text2.Replace("\n  ", "\n "))
                    {
                        text2 = text2.Replace("\n  ", "\n ");
                    };
                    text2 = text2.Replace("\n", "\n");
                    text2 = text2.Replace("\n", " ");
                    throw new Exception(text2);
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
                    condition["all"] = grammar.ParseTreeToExpression(parseTree.Root, "", ref countTypePrefix);
                    text = "combinedCondition: " + condition["all"];
                    Console.WriteLine(text);
                    messages.Add(text);

                    countTypePrefix = 0;
                    condition["sm"] = grammar.ParseTreeToExpression(parseTree.Root, "sm.", ref countTypePrefix);
                    if (condition["sm"] == "$SKIP")
                    {
                        condition["sm"] = "";
                    }
                    text = "conditionSM: " + condition["sm"];
                    Console.WriteLine(text);
                    messages.Add(text);

                    countTypePrefix = 0;
                    condition["sme"] = grammar.ParseTreeToExpression(parseTree.Root, "sme.", ref countTypePrefix);
                    if (condition["sme"] == "$SKIP")
                    {
                        condition["sme"] = "";
                    }
                    text = "conditionSME: " + condition["sme"];
                    Console.WriteLine(text);
                    messages.Add(text);

                    countTypePrefix = 0;
                    condition["svalue"] = grammar.ParseTreeToExpression(parseTree.Root, "str()", ref countTypePrefix);
                    if (condition["svalue"] == "$SKIP")
                    {
                        condition["svalue"] = "";
                    }
                    text = "conditionSValue: " + condition["svalue"];
                    Console.WriteLine(text);
                    messages.Add(text);

                    countTypePrefix = 0;
                    condition["nvalue"] = grammar.ParseTreeToExpression(parseTree.Root, "num()", ref countTypePrefix);
                    if (condition["nvalue"] == "$SKIP")
                    {
                        condition["nvalue"] = "";
                    }
                    text = "conditionNValue: " + condition["nvalue"];
                    Console.WriteLine(text);
                    messages.Add(text);
                }

                messages.Add("");
            }
            else if (withQueryLanguage == 2)
            {
                // JSON Query Language from JsonParser.cs
                var jParser = new JsonParser();
                messages.Add("");

                var countTypePrefix = 0;
                condition["all"] = jParser.ParseTreeToExpression(expression, "", ref countTypePrefix);
                messages.Add("combinedCondition: " + condition["all"]);

                countTypePrefix = 0;
                condition["sm"] = jParser.ParseTreeToExpression(expression, "sm.", ref countTypePrefix);
                if (condition["sm"] == "$SKIP")
                {
                    condition["sm"] = "";
                }
                messages.Add("conditionSM: " + condition["sm"]);

                countTypePrefix = 0;
                condition["sme"] = jParser.ParseTreeToExpression(expression, "sme.", ref countTypePrefix);
                if (condition["sme"] == "$SKIP")
                {
                    condition["sme"] = "";
                }
                messages.Add("conditionSME: " + condition["sme"]);

                countTypePrefix = 0;
                condition["svalue"] = jParser.ParseTreeToExpression(expression, "str()", ref countTypePrefix);
                if (condition["svalue"] == "$SKIP")
                {
                    condition["svalue"] = "";
                }
                messages.Add("conditionStr: " + condition["svalue"]);

                countTypePrefix = 0;
                condition["nvalue"] = jParser.ParseTreeToExpression(expression, "num()", ref countTypePrefix);
                if (condition["nvalue"] == "$SKIP")
                {
                    condition["nvalue"] = "";
                }
                messages.Add("conditionNum: " + condition["nvalue"]);

                messages.Add("");
            }
            else if (withQueryLanguage == 3)
            {
                // with newest query language from QueryParserJSON.cs
                var parser = new Parser(grammar);
                parser.Context.TracingEnabled = true;
                var parseTree = parser.Parse(expression);

                if (parseTree.HasErrors())
                {
                    var pos = parser.Context.CurrentToken.Location.Position;
                    var text2 = expression.Substring(0, pos) + "$$$" + expression.Substring(pos);
                    text2 = string.Join("\n", parseTree.ParserMessages) + "\nSee $$$: " + text2;
                    Console.WriteLine(text2);
                    while (text2 != text2.Replace("\n  ", "\n "))
                    {
                        text2 = text2.Replace("\n  ", "\n ");
                    };
                    text2 = text2.Replace("\n", "\n");
                    text2 = text2.Replace("\n", " ");
                    throw new Exception(text2);
                }
                else
                {
                    messages.Add("");

                    // Security
                    if (parseTree.Root.Term.Name == "all_access_permission_rules")
                    {
                        grammar.ParseAccessRules(parseTree.Root);
                        throw new Exception("Access Rules parsed!");
                    }

                    int countTypePrefix = 0;
                    condition["all"] = grammar.ParseTreeToExpression(parseTree.Root, "", ref countTypePrefix);
                    text = "combinedCondition: " + condition["all"];
                    Console.WriteLine(text);
                    messages.Add(text);

                    countTypePrefix = 0;
                    condition["sm"] = grammar.ParseTreeToExpression(parseTree.Root, "sm.", ref countTypePrefix);
                    if (condition["sm"] == "$SKIP")
                    {
                        condition["sm"] = "";
                    }
                    text = "conditionSM: " + condition["sm"];
                    Console.WriteLine(text);
                    messages.Add(text);

                    countTypePrefix = 0;
                    condition["sme"] = grammar.ParseTreeToExpression(parseTree.Root, "sme.", ref countTypePrefix);
                    if (condition["sme"] == "$SKIP")
                    {
                        condition["sme"] = "";
                    }
                    text = "conditionSME: " + condition["sme"];
                    Console.WriteLine(text);
                    messages.Add(text);
                    text = "$sme#path: " + grammar.idShortPath;
                    condition["idShortPath"] = grammar.idShortPath;
                    Console.WriteLine(text);
                    messages.Add(text);

                    countTypePrefix = 0;
                    condition["svalue"] = grammar.ParseTreeToExpression(parseTree.Root, "str()", ref countTypePrefix);
                    if (condition["svalue"] == "$SKIP")
                    {
                        condition["svalue"] = "";
                    }
                    text = "conditionSValue: " + condition["svalue"];
                    Console.WriteLine(text);
                    messages.Add(text);

                    countTypePrefix = 0;
                    condition["nvalue"] = grammar.ParseTreeToExpression(parseTree.Root, "num()", ref countTypePrefix);
                    if (condition["nvalue"] == "$SKIP")
                    {
                        condition["nvalue"] = "";
                    }
                    text = "conditionNValue: " + condition["nvalue"];
                    Console.WriteLine(text);
                    messages.Add(text);
                }

                messages.Add("");
            }
            return condition;
        }

        private static string ModifiyRawSQL(string? rawSQL)
        {
            if (rawSQL.IsNullOrEmpty())
                return rawSQL;
            rawSQL = SetParameter(rawSQL);
            rawSQL = ChangeINSTRToLIKE(rawSQL);
            return rawSQL;
        }
    }
}