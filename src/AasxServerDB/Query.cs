/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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

namespace AasxServerDB;

using System.Diagnostics;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Dynamic.Core;
using System.Linq;
using Irony.Parsing;
using System;
using System.Collections.Generic;
using Contracts.QueryResult;
using AasxServerDB.Entities;
using Microsoft.EntityFrameworkCore;
using AasCore.Aas3_0;
using Contracts.Security;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
// using Newtonsoft.Json.Schema;
using HotChocolate.Types.Relay;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Text.Json;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Runtime.Intrinsics.X86;

public class CombinedValue
{
    public int SMEId { get; set; }
    public string? SValue { get; set; }
    public Double? MValue { get; set; }
}

public class SMEResultRaw
{
    public string? SM_Identifier { get; set; }
    public string? IdShortPath { get; set; }
    public DateTime? SME_TimeStamp { get; set; }
    public string? SValue { get; set; }
    public double? MValue { get; set; }
}

public class SubmodelsQueryResult
{
    public List<ISubmodel> Submodels { get; set; }
    public List<ISubmodelElement> SubmodelElements { get; set; }
    public List<string> Ids { get; set; }
}


public partial class Query
{
    public Query(QueryGrammarJSON queryGrammar)
    {
        grammar = queryGrammar;
    }
    private readonly QueryGrammarJSON grammar;
    public static string? ExternalBlazor { get; set; }

    /*
    public Query(QueryGrammar queryGrammar)
    {
        grammar = queryGrammar;
    }
    private readonly QueryGrammar grammar;
    */

    public QResult SearchSMs(Dictionary<string, string>? securityCondition, AasContext db, bool withTotalCount, bool withLastId, string semanticId,
        string identifier, string diff, int pageFrom, int pageSize, string expression)
    {
        bool noSecurity = securityCondition == null;

        var qResult = new QResult()
        {
            Count = 0,
            TotalCount = 0,
            PageFrom = 0,
            PageSize = QResult.DefaultPageSize,
            LastID = 0,
            Messages = new List<string>(),
            SMResults = new List<SMResult>(),
            SMEResults = new List<SMEResult>(),
            SQL = new List<string>()
        };

        var text = string.Empty;

        var watch = Stopwatch.StartNew();
        Console.WriteLine("\nSearchSMs");

        watch.Restart();
        Dictionary<string, string>? condition;
        var query = GetSMs(noSecurity, securityCondition, out condition, qResult, watch, db, false, withTotalCount, semanticId, identifier, diff, pageFrom, pageSize, expression);
        if (query == null)
        {
            text = "No query is generated.";
            Console.WriteLine(text);
            qResult.Messages.Add(text);
        }
        else
        {
            text = "Generate query in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            qResult.Messages.Add(text);

            watch.Restart();
            int lastId = 0;
            var result = GetSMResult(qResult, (IQueryable<CombinedSMResult>)query, withLastId, out lastId);
            qResult.LastID = lastId;
            qResult.Count = result.Count;
            text = "Collect results in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            qResult.Messages.Add(text);
            text = "SMs found ";
            if (withTotalCount)
            {
                text += qResult.TotalCount;
            }
            else
            {
                text += "totalCount";
            }
            text += "/" + db.SMSets.Count() + ": " + qResult.Count + " queried";
            Console.WriteLine(text);
            qResult.Messages.Add(text);

            qResult.SMResults = result;
        }

        return qResult;
    }

    public int CountSMs(ISecurityConfig securityConfig, Dictionary<string, string>? securityCondition, AasContext db, string semanticId, string identifier, string diff, int pageFrom, int pageSize, string expression)
    {
        var watch = Stopwatch.StartNew();
        Console.WriteLine("\nCountSMs");

        watch.Restart();
        Dictionary<string, string>? condition;
        var query = GetSMs(securityConfig.NoSecurity, securityCondition, out condition,
            new QResult(), watch, db, true, false, semanticId, identifier, diff, pageFrom, pageSize, expression);
        if (query == null)
        {
            Console.WriteLine("No query is generated.");
            return 0;
        }
        Console.WriteLine("Generate query in " + watch.ElapsedMilliseconds + " ms");

        watch.Restart();
        var result = query.Count();
        Console.WriteLine("Collect results in " + watch.ElapsedMilliseconds + " ms\nSMs found\t" + result + "/" + db.SMSets.Count());

        return result;
    }

    public QResult SearchSMEs(ISecurityConfig securityConfig, Dictionary<string, string>? securityCondition,
        AasContext db, string requested, bool withTotalCount, bool withLastId,
        string smSemanticId, string smIdentifier, string semanticId, string diff, string contains,
        string equal, string lower, string upper, int pageFrom, int pageSize, string expression)
    {

        var qResult = new QResult()
        {
            Count = 0,
            TotalCount = 0,
            PageFrom = 0,
            PageSize = QResult.DefaultPageSize,
            LastID = 0,
            Messages = new List<string>(),
            SMResults = new List<SMResult>(),
            SMEResults = new List<SMEResult>(),
            SQL = new List<string>()
        };

        var text = string.Empty;

        var watch = Stopwatch.StartNew();
        Console.WriteLine("\nSearchSMEs");

        watch.Restart();
        var query = GetSMEs(securityConfig.NoSecurity, securityCondition, qResult, watch, requested, db, false, withTotalCount,
            smSemanticId, smIdentifier, semanticId, diff, pageFrom, pageSize, contains, equal, lower, upper, expression);
        if (query == null)
        {
            text = "No query is generated.";
            Console.WriteLine(text);
            qResult.Messages.Add(text);
        }
        else
        {
            text = "Generate query in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            qResult.Messages.Add(text);

            watch.Restart();
            // var result = GetSMEResult(requested, (IQueryable<CombinedSMEResult>)query);
            int lastId = 0;
            var result = GetSMEResult(qResult, db, requested, query, withLastId, out lastId);
            qResult.LastID = lastId;
            qResult.Count = result.Count;
            text = "SMEs found ";
            if (withTotalCount)
            {
                text += qResult.TotalCount;
            }
            else
            {
                text += "totalCount";
            }
            text += "/" + db.SMESets.Count() + ": " + qResult.Count + " queried";
            Console.WriteLine(text);
            qResult.Messages.Add(text);
            text = "Collect results in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            qResult.Messages.Add(text);

            qResult.SMEResults = result;
        }

        return qResult;
    }

    public int CountSMEs(ISecurityConfig securityConfig, Dictionary<string, string>? securityCondition, AasContext db,
        string smSemanticId, string smIdentifier, string semanticId, string diff,
        string contains, string equal, string lower, string upper, int pageFrom, int pageSize, string expression)
    {
        var watch = Stopwatch.StartNew();
        Console.WriteLine("\nCountSMEs");

        watch.Restart();
        var query = GetSMEs(securityConfig.NoSecurity, securityCondition, new QResult(), watch, "", db, true, false, smSemanticId, smIdentifier, semanticId, diff, -1, -1, contains, equal, lower, upper, expression);
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

    internal SubmodelsQueryResult GetSubmodelList(bool noSecurity, AasContext db, Dictionary<string, string>? securityCondition, int pageFrom, int pageSize, string expression)
    {
        var qResult = new QResult()
        {
            Count = 0,
            TotalCount = 0,
            PageFrom = 0,
            PageSize = QResult.DefaultPageSize,
            LastID = 0,
            Messages = new List<string>(),
            SMResults = new List<SMResult>(),
            SMEResults = new List<SMEResult>(),
            SQL = new List<string>()
        };

        var text = string.Empty;

        var watch = Stopwatch.StartNew();
        Console.WriteLine("\nSearchSMs");

        watch.Restart();

        expression = "$JSONGRAMMAR " + expression;

        Dictionary<string, string>? condition;
        var query = GetSMs(noSecurity, securityCondition, out condition,
            qResult, watch, db, false, false, "", "", "", pageFrom, pageSize, expression);
        if (query == null)
        {
            text = "No query is generated.";
            Console.WriteLine(text);
            return null;
        }
        else
        {
            text = "Generate query in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);

            watch.Restart();
            int lastId = 0;
            var result = GetSMResult(qResult, (IQueryable<CombinedSMResult>)query, false, out lastId);
            text = "Collect results in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            text = "SMs found ";
            text += "/" + db.SMSets.Count() + ": " + qResult.Count + " queried";
            Console.WriteLine(text);

            /*
            var securityConditionSM = "";
            if (securityCondition != null && securityCondition["sm."] != null)
            {
                securityConditionSM = securityCondition["sm."];
            }

            if (securityConditionSM == "" || securityConditionSM == "*")
                securityConditionSM = "true";
            */

            var submodelsResult = new SubmodelsQueryResult();
            submodelsResult.SubmodelElements = [];

            if (!qResult.WithSelectId && !qResult.WithSelectMatch)
            {
                var timeStamp = DateTime.UtcNow;
                var submodels = new List<ISubmodel>();

                var smIdList = result.Select(sm => sm.smId).Distinct();
                var smList = db.SMSets.Where(sm => smIdList.Contains(sm.Id)).ToList();

                foreach (var sm in smList.Select(selector: submodelDB =>
                    CrudOperator.ReadSubmodel(db, smDB: submodelDB, "", securityCondition, condition)))
                {
                    if (sm.TimeStamp == DateTime.MinValue)
                    {
                        sm.SetAllParentsAndTimestamps(null, timeStamp, timeStamp, DateTime.MinValue);
                        sm.SetTimeStamp(timeStamp);
                    }
                    submodels.Add(sm);
                }
                submodelsResult.Submodels = submodels;
            }
            else
            {
                if (qResult.WithSelectId)
                {
                    submodelsResult.Ids = result.Select(sm => sm.smIdentifier).Distinct().ToList();
                }
                if (qResult.WithSelectMatch)
                {
                    var smIdList = result.Where(sm => sm.smId != null).Select(sm => sm.smId).Distinct().ToList();
                    var smMatchPathList = qResult.MatchPathList;
                    if (smIdList != null && smMatchPathList != null)
                    {
                        var smList = db.SMSets.Where(sm =>
                            smIdList.Contains(sm.Id));
                        var smeList = db.SMESets.Where(sme =>
                            smIdList.Contains(sme.SMId) && sme.IdShortPath != null && smMatchPathList.Contains(sme.IdShortPath)
                        );

                        foreach (var sme in smeList)
                        {
                            var sm = smList.FirstOrDefault(sm => sm.Id == sme.SMId);
                            if (sm != null)
                            {
                                var smeTree = CrudOperator.GetTree(db, sm, [sme]);
                                var smeMerged = CrudOperator.GetSmeMerged(db, null, smeTree, sm);
                                var readSme = CrudOperator.ReadSubmodelElement(sme, smeMerged);
                                if (readSme != null)
                                {
                                    readSme.Extensions = [
                                        new Extension("id", value: sm.Identifier),
                                        new Extension("idShort", value: sm.IdShort),
                                        new Extension("semanticId", value: sm.SemanticId),
                                        new Extension("matchIdShortPath", value: sme.IdShortPath)
                                    ];
                                    submodelsResult.SubmodelElements.Add(readSme);
                                }
                            }
                        }
                    }
                }
            }
            return submodelsResult;
        }
    }

    // --------------- SM Methods ---------------


    public class SmDto
    {
        public int SMId { get; set; }
        public List<bool> Conditions { get; set; } = new List<bool>();
    }

    private IQueryable? GetSMs(bool noSecurity, Dictionary<string, string>? securityCondition, out Dictionary<string, string>? condition,
        QResult qResult, Stopwatch watch, AasContext db, bool withCount = false, bool withTotalCount = false,
        string semanticId = "", string identifier = "", string diffString = "", int pageFrom = -1, int pageSize = -1, string expression = "")
    {
        condition = null;

        // parameter
        var messages = qResult.Messages ?? [];
        var rawSQL = qResult.SQL ?? [];

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

        var lastID = -1;
        var orderBy = false;
        if (expression.Contains("$LASTID="))
        {
            var index1 = expression.IndexOf("$LASTID");
            var index2 = expression.IndexOf("=", index1);
            var index3 = expression.IndexOf(";", index1);
            var s = expression.Substring(index2 + 1, index3 - index2 - 1);
            var s2 = expression.Substring(index1, index3 - index1 + 1);
            if (s.StartsWith("+"))
            {
                orderBy = true;
                s = s.Substring(1);
            }
            lastID = int.Parse(s);
            expression = expression.Replace(s2, "");
            messages.Add("$LASTID=" + pageSize);
        }

        // get condition out of expression
        var conditionsExpression = ConditionFromExpression(noSecurity, messages, expression, securityCondition);
        if (conditionsExpression == null || conditionsExpression.Count == 0)
        {
            return null;
        }
        condition = conditionsExpression;
        if (conditionsExpression.TryGetValue("select", out var sel))
        {
            qResult.WithSelectId = sel == "id";
            qResult.WithSelectMatch = sel == "match";
        }

        var withPathSme = false;
        var withMatch = false;
        var pathSME = "";
        var pathAllCondition = "";
        var pathAllConditionRaw = "";
        if (conditionsExpression.TryGetValue("path-sme", out pathSME))
        {
            withPathSme = true;
            if (!conditionsExpression.TryGetValue("path-all", out pathAllCondition))
            {
                withPathSme = false;
            }
            if (conditionsExpression.TryGetValue("path-raw", out pathAllConditionRaw))
            {
                if (pathAllConditionRaw.Contains("$$match"))
                {
                    withMatch = true;
                }
            }
        }

        // restrictions in which tables 
        var restrictSM = false;
        var restrictSME = false;
        var restrictSValue = false;
        var restrictNValue = false;
        var restrictValue = false;

        // restrict all tables seperate
        IQueryable<CombinedSMSMEV> comTable = null;
        IQueryable<SMSet> smTable;
        IQueryable<SMESet> smeTable;
        IQueryable<SValueSet>? sValueTable;
        IQueryable<IValueSet>? iValueTable;
        IQueryable<DValueSet>? dValueTable;

        // get data
        var skip = false;
        if (withExpression) // with expression
        {
            string ? rawSQLEx;
            // check restrictions
            restrictSM = conditionsExpression.TryGetValue("sm", out var value) && value != "" && value != "true";
            restrictSME = conditionsExpression.TryGetValue("sme", out value) && value != "" && value != "true";
            restrictSValue = conditionsExpression.TryGetValue("svalue", out value) && value != "" && value != "true";
            restrictNValue = conditionsExpression.TryGetValue("nvalue", out value) && value != "" && value != "true";
            restrictValue = restrictSValue || restrictNValue;
            object[]? param = null;

            if (withMatch)
            {
                var splitMatch = pathAllConditionRaw.Split("$$match$$");
                pathAllCondition = "";
                var iCondition = 0;
                var conditionCount = splitMatch.Where(s => s.Contains("$$tag$$path$$")).Count();
                var smContains = new IQueryable[conditionCount];
                List<string> matchPathList = [];
                for (var iMatch = 0; iMatch < splitMatch.Count(); iMatch++)
                {
                    var match = splitMatch[iMatch];
                    if (!match.Contains("$$tag$$path$$"))
                    {
                        pathAllCondition += match;
                    }
                    else
                    {
                        var matchCondition = "false";
                        List<string> idShortPath = [];
                        var idShortPathSplit = new List<List<string>>();
                        List<string> field = [];
                        List<string> exp = [];
                        var split = match.Split("$$tag$$path$$");
                        for (var i = 1; i < split.Length; i++)
                        {
                            var firstTag = split[i];
                            var split2 = firstTag.Split("$$");
                            idShortPath.Add(split2[0]);
                            field.Add(split2[1]);
                            exp.Add(split2[2]);
                        }
                        List<string> expSME = [];
                        var distinctPaths = idShortPath.Distinct().OrderByDescending(p => p.Count(c => c == '[')).ToList();
                        List<string> distinctExp = [];
                        for (var iDistinct = 0; iDistinct < distinctPaths.Count; iDistinct++)
                        {
                            idShortPathSplit.Add(distinctPaths[iDistinct].Split("[]").ToList());
                            var e = "";
                            for (var i = 0; i < idShortPath.Count; i++)
                            {
                                if (idShortPath[i] == distinctPaths[iDistinct])
                                {
                                    if (e != "")
                                    {
                                        e += " && ";
                                    }
                                    e += field[i] + exp[i];
                                }
                            }
                            distinctExp.Add(e);
                        }
                        foreach (var ids in idShortPathSplit)
                        {
                            string e = "";
                            for (var i = 0; i < ids.Count; i++)
                            {
                                if (i == 0)
                                {
                                    e = $"idShortPath.StartsWith(\"{ids[i]}[\")";
                                }
                                else if (i == ids.Count - 1)
                                {
                                    e += $" && idShortPath.EndsWith(\"]{ids[i]}\")";
                                }
                                else
                                {
                                    e += $" && idShortPath.Contains(\"]{ids[i]}[\")";
                                }
                            }
                            expSME.Add(e);
                        }

                        var smeList = new List<IQueryable>();
                        for (var iExp = 0; iExp < expSME.Count; iExp++)
                        {
                            var index = "Index1,";
                            var sme = db.SMESets.Where(expSME[iExp]);
                            var svalue = db.SValueSets.Where(distinctExp[iExp].Replace("svalue", "value"));
                            IQueryable joinSmeValuePath = sme.Join(
                                svalue,
                                "Id",
                                "SMEId",
                                "new (outer.SMId," +
                                    "outer.IdShortPath," +
                                    "outer.IdShortPath.IndexOf(\"]\") + 1 as Index1," +
                                    ")"
                                );
                            for (var i = 1; i < idShortPathSplit[iExp].Count - 1; i++)
                            {
                                joinSmeValuePath = joinSmeValuePath
                                    .Select("new (SMId," +
                                        "IdShortPath," +
                                        index +
                                        $"(IdShortPath.Substring(Index{i}).IndexOf(\"]\") + 1  + Index{i}) as Index{i + 1}," +
                                        ")"
                                    );
                                index += $" Index{i + 1},";
                            }
                            smeList.Add(joinSmeValuePath);
                        }
                        for (var iExp = 1; iExp < expSME.Count; iExp++)
                        {
                            var index = "";
                            var pathMax = 0;
                            while (pathMax < idShortPathSplit[iExp].Count &&
                                pathMax < idShortPathSplit[iExp - 1].Count &&
                                idShortPathSplit[iExp][pathMax] == idShortPathSplit[iExp - 1][pathMax])
                            {
                                pathMax++;
                                index += $"Index{pathMax}, ";
                            }

                            var smeListCompare = smeList[iExp - 1].Join(
                                smeList[iExp],
                                "SMId",
                                "SMId",
                                "new (outer.SMId as SMId," +
                                    "outer as O," +
                                    "inner as I" +
                                    ")"
                            ).Distinct();
                            index = $"Index{pathMax}";
                            smeListCompare = smeListCompare.Where(
                                    $"o.{index} == i.{index} && " +
                                    $"o.IdShortPath.Substring(0, o.{index}) == i.IdShortPath.Substring(0, i.{index})"
                                );
                            smeList[iExp - 1] = smeListCompare.Select("O").Distinct();
                            smeList[iExp] = smeListCompare.Select("I").Distinct();
                        }
                        var pm = smeList.Last().Select($"new (IdShortPath.Substring(0, Index1) as IdShortPath)").FirstOrDefault()?.IdShortPath;
                        if (pm != null)
                        {
                            matchPathList.Add(pm);
                        }
                        smContains[iCondition] = smeList.Last().Select("SMId");
                        pathAllCondition += $"@{iCondition}.Contains(Id)";
                        iCondition++;
                    }
                }

                qResult.MatchPathList = matchPathList;
                var parameters = new object[conditionCount];
                for (var i = 0; i < conditionCount; i++)
                {
                    parameters[i] = smContains[i];
                }
                smTable = db.SMSets.Where(pathAllCondition, parameters).Skip(pageFrom).Take(pageSize).Distinct();

                var resultSM = smTable.Select(sm => new CombinedSMResult
                {
                    SM_Id = sm.Id,
                    Identifier = sm.Identifier,
                    TimeStampTree = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree)
                })
                .Distinct()
                .Skip(pageFrom).Take(pageSize);
                return resultSM;
            }
            else
            {
                if (withPathSme)
                {
                    var text = "-- Start idShortPath at " + watch.ElapsedMilliseconds + " ms";
                    Console.WriteLine(text);
                    messages.Add(text);

                    pathSME = pathSME.Replace("sm.", "SM_").Replace("sme.", "SME_").Replace("svalue", "V_VALUE").Replace("mvalue", "V_D_VALUE");
                    var pathAllCondition1 = pathAllCondition.Replace("sm.", "SM_").Replace("sme.", "SME_").Replace("svalue", "V_VALUE").Replace("mvalue", "V_D_VALUE");
                    var split = pathSME.Split("$$");
                    var smContainsPathSme = new List<int?>[split.Length];
                    param = new object[split.Length];

                    var smeCondition = "";
                    var valueCondition = "";
                    foreach (var s in split)
                    {
                        var smeSplit = s.Split(" && ");
                        smeSplit[0] = smeSplit[0].Substring(1).Replace("V_", "");
                        smeSplit[1] = smeSplit[1].Replace("SME_", "");
                        smeSplit[2] = smeSplit[2].Substring(0, smeSplit[2].Length - 1).Replace("SME_", "");
                        if (smeCondition == "")
                        {
                            smeCondition = "(" + smeSplit[1] + " && " + smeSplit[2] + ")";
                        }
                        else
                        {
                            smeCondition += " || " + "(" + smeSplit[1] + " && " + smeSplit[2] + ")";
                        }
                        if (valueCondition == "")
                        {
                            valueCondition = smeSplit[0];
                        }
                        else
                        {
                            valueCondition += " || " + smeSplit[0];
                        }
                    }

                    // Pre-Search to check path conditions for submodels
                    // restrict sm only for path
                    smTable = restrictSM ? db.SMSets.Where(conditionsExpression["sm"]) : db.SMSets;
                    smeTable = db.SMESets;
                    if (smeCondition != "")
                    {
                        smeTable = db.SMESets.Where(smeCondition);
                    }
                    sValueTable = db.SValueSets;
                    if (valueCondition != "")
                    {
                        sValueTable = db.SValueSets.Where(valueCondition);
                    }
                    iValueTable = db.IValueSets;
                    dValueTable = db.DValueSets;

                    // combine tables to a raw sql 
                    rawSQLEx = CombineTablesToRawSQL(direction, smTable, smeTable, sValueTable, iValueTable, dValueTable, false);
                    // table name needed for EXISTS in path search
                    rawSQLEx = "WITH MergedTables AS (\r\n" + rawSQLEx + ")\r\nSELECT *\r\nFROM MergedTables\r\n";
                    comTable = db.Database.SqlQueryRaw<CombinedSMSMEV>(rawSQLEx).AsQueryable();

                    var withMerge = true;
                    if (withMerge)
                    {
                        try
                        {
                            var pathAllCondition2 = pathAllCondition.Replace("sm.", "t.SM_").Replace("sme.", "t.SME_").Replace("svalue", "t.V_VALUE").Replace("mvalue", "t.V_D_VALUE");
                            var orCondition = "";
                            var select = "";
                            var select2 = "";

                            for (var i = 0; i < split.Length; i++)
                            {
                                if (i == 0)
                                {
                                    orCondition = $"({split[0]})";
                                    // select = $"Any({split[i]}) as c{i}";
                                    select = $"({split[i]}) as c{i}";
                                    // select = $"({split[i]} ? 1 : 0)";
                                    select2 = $"(outer.c{i} || inner.c{i}) as c{i}";
                                }
                                else
                                {
                                    orCondition += $" || ({split[i]})";
                                    // select += $", Any({split[i]}) as c{i}";
                                    select += $", ({split[i]}) as c{i}";
                                    // select += $"+ ({split[i]} ? 1 : 0)";
                                    select2 += $", (outer.c{i} || inner.c{i}) as c{i}";
                                }
                                pathAllCondition2 = pathAllCondition2.Replace($"$$path{i}$$", $"c.c{i}");
                            }
                            orCondition = "(" + orCondition + ")";
                            // select = "new { Key, " + select + "}";
                            select = "new { SM_Id, " + select + "}";
                            select2 = "new (outer.SM_Id, " + select2 + ")";
                            // select = "new { SM_Id, " + select + " as count }";

                            var notAllFalse = "c0";
                            IQueryable join = comTable.Select($"new (SM_Id, ({split[0]}) as c0)").Distinct();
                            // var z1 = join.Take(100).ToDynamicList();
                            for (var i = 1; i < split.Length; i++)
                            {
                                notAllFalse += $" || c{i}";
                                var ci = comTable.Select($"new (SM_Id, ({split[i]}) as c{i})").Distinct();
                                // var z2 = ci.Take(100).ToDynamicList();
                                var select3 = "new (outer.SM_Id";
                                for (var j = 0; j < i; j++)
                                {
                                    select3 += $", outer.c{j} as c{j}";
                                }
                                select3 += $", inner.c{i} as c{i})";
                                join = join.Join(
                                    ci,
                                    "SM_Id",
                                    "SM_Id",
                                    select3
                                    ).Distinct();
                                // z1 = join.Take(100).ToDynamicList();
                            }
                            join = join.Where(notAllFalse);
                            var filterConditions = join;

                            var comTableFilter = comTable.Join(
                                filterConditions,
                                "SM_Id",
                                "SM_Id",
                                "new (outer as t, inner as c)"
                                ).Distinct();
                            // var y1 = comTableFilter.Take(100).ToDynamicList();
                            var comTableFilter2 = comTableFilter.Where(pathAllCondition2).Distinct();
                            // var y2 = comTableFilter2.Take(100).ToDynamicList();
                            comTable = comTableFilter2.Select<CombinedSMSMEV>("t");
                            // var y3 = comTable.Take(10000).ToDynamicList();
                            skip = true;
                        }
                        catch (Exception ex)
                        {
                            return null;
                        };
                    }
                    else
                    {
                        try
                        {
                            for (var i = 0; i < split.Length; i++)
                            {
                                var s = split[i];
                                var arg = s.Split(" ")[0].Replace("(", "");
                                if (arg.Contains("."))
                                {
                                    arg = arg.Split(".")[0];
                                }
                                var c1 = comTable.Select("new { SM_Id, SME_idShort, SME_idShortPath, " + arg + " }");
                                var c2 = c1.Where(split[i]);
                                param[i] = c2.Select("SM_Id").Distinct();
                                // param[i] = smContainsPathSme[i];
                                pathAllCondition1 = pathAllCondition1?.Replace($"$$path{i}$$", $"@{i}.Contains(SM_Id)");
                                pathAllCondition = pathAllCondition1;

                                text = $"-- idShortPath: {s} at " + watch.ElapsedMilliseconds + " ms";
                                Console.WriteLine(text);
                                messages.Add(text);
                            }
                        }
                        catch (Exception ex) { };
                    }
                }
            }
            // else
            if (!skip)
            {
                // restrict all tables seperate
                smTable = restrictSM ? db.SMSets.Where(conditionsExpression["sm"]) : db.SMSets;
                smeTable = restrictSME ? db.SMESets.Where(conditionsExpression["sme"]) : db.SMESets;
                sValueTable = restrictValue ? (restrictSValue ? db.SValueSets.Where(conditionsExpression["svalue"]) : null) : db.SValueSets;
                iValueTable = restrictValue ? (restrictNValue ? db.IValueSets.Where(conditionsExpression["nvalue"]) : null) : db.IValueSets;
                dValueTable = restrictValue ? (restrictNValue ? db.DValueSets.Where(conditionsExpression["nvalue"]) : null) : db.DValueSets;

                // combine tables to a raw sql 
                rawSQLEx = CombineTablesToRawSQL(direction, smTable, smeTable, sValueTable, iValueTable, dValueTable, false);
                // table name needed for EXISTS in path search
                rawSQLEx = "WITH MergedTables AS (\r\n" + rawSQLEx + ")\r\nSELECT *\r\nFROM MergedTables\r\n";
                comTable = db.Database.SqlQueryRaw<CombinedSMSMEV>(rawSQLEx).AsQueryable();

                if (withPathSme && pathAllCondition != null && param != null)
                {
                    comTable = comTable.Where(pathAllCondition, param);

                    var text = "-- End idShortPath at " + watch.ElapsedMilliseconds + " ms";
                    Console.WriteLine(text);
                    messages.Add(text);
                }

                var combi = conditionsExpression["all"].Replace("svalue", "V_Value").Replace("mvalue", "V_D_Value").Replace("sm.idShort", "SM_IdShort").Replace("sme.idShort", "SME_IdShort").Replace("sme.idShortPath", "SME_IdShortPath");
                comTable = comTable.Where(combi);
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

        if (comTable == null)
        {
            return null;
        }

        // modifiy raw sql
        var comTableQueryString = comTable.ToQueryString();
        comTableQueryString = ModifiyRawSQL(comTableQueryString);
        comTable = db.Database.SqlQueryRaw<CombinedSMSMEV>(comTableQueryString);

        var smRawSQL = comTable.ToQueryString();
        // check for WITH at the beginning
        if (withExpression)
        {
            var index = smRawSQL.IndexOf("WITH");
            if (index == 0)
            {
                smRawSQL = smRawSQL.Replace("SELECT *",
                    $"SELECT DISTINCT SM_Identifier AS Identifier, SM_Id as SM_Id, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', SM_TimeStampTree) AS TimeStampTree"
                    );
            }
            else
            {
                // change the first select
                index = smRawSQL.IndexOf("FROM");
                if (index == -1)
                    return null;
                smRawSQL = smRawSQL.Substring(index);
                var prefix = "";
                var split = smRawSQL.Split(" ");
                var count = split.Length;
                if (split[count - 2] == "AS")
                {
                    prefix = split.Last().Replace("\"", "").Replace("\n", "").Replace("\r", "") + ".";
                }
                smRawSQL = $"SELECT DISTINCT {prefix}SM_Identifier AS Identifier, SM_Id as SM_Id, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', {prefix}SM_TimeStampTree) AS TimeStampTree \n {smRawSQL}";
            }
        }
        else
        {
            var index = smRawSQL.IndexOf("FROM");
            if (index == -1)
                return null;
            smRawSQL = smRawSQL.Substring(index);
            smRawSQL = $"SELECT DISTINCT s.Identifier, SM_Id as SM_Id, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', s.TimeStampTree) AS TimeStampTree \n {smRawSQL}";
        }

        var result = db.Database.SqlQueryRaw<CombinedSMResult>(smRawSQL);

        // select for count
        if (withCount)
        {
            // var qCount = comTable.Select(sm => sm.SM_Identifier);
            // return qCount;
            return result;
        }

        if (withTotalCount)
        {
            var text = "-- Start totalCount at " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            messages.Add(text);
            qResult.TotalCount = result.Count();
            text = "-- End totalCount at " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            messages.Add(text);
        }

        // create queryable with pagenation
        qResult.PageFrom = pageFrom;
        qResult.PageSize = pageSize;
        if (pageFrom != -1)
        {
            if (orderBy)
            {
                Console.WriteLine("OrderBy");
                messages.Add("OrderBy");
                result = result.OrderBy(sm => sm.SM_Id).Skip(pageFrom).Take(pageSize);
            }
            else
            {
                result = result.Skip(pageFrom).Take(pageSize);
            }
            var text = "Using pageFrom and pageSize.";
            Console.WriteLine(text);
            messages.Add(text);
        }
        else
        {
            if (lastID != -1 && pageSize != -1)
            {
                if (orderBy)
                {
                    Console.WriteLine("OrderBy");
                    messages.Add("OrderBy");
                    result = result.OrderBy(sm => sm.SM_Id).Where(sm => sm.SM_Id > lastID).Take(pageSize);
                }
                else
                {
                    result = result.Where(sm => sm.SM_Id > lastID).Take(pageSize);
                }
                var text = "Using lastID and pageSize.";
                Console.WriteLine(text);
                messages.Add(text);
            }
        }
        // qResult.LastID = result.LastOrDefault().SM_Id;
        // smRawSQL = $".param set :pagesize {pageSize}\r\n" + $".param set :pagefrom {pageFrom}\r\n" + smRawSQL + "LIMIT :pagesize OFFSET :pagefrom\r\n";
        // var result = db.Database.SqlQueryRaw<CombinedSMResult>(smRawSQL);

        // return raw SQL
        smRawSQL = result.ToQueryString();
        var rawSqlSplit = smRawSQL.Replace("\r", "").Split("\n").Select(s => s.TrimStart()).ToList();
        rawSQL.AddRange(rawSqlSplit);

        return result;
    }

    private static List<SMResult> GetSMResult(QResult qResult, IQueryable<CombinedSMResult> query, bool withLastID, out int lastID)
    {
        var messages = qResult.Messages ?? [];

        lastID = 0;
        if (withLastID)
        {
            var resultWithSMID = query
                .Select(sm => new
                {
                    sm.SM_Id,
                    sm.Identifier,
                    sm.TimeStampTree,
                    lastID = sm.SM_Id
                })
                .ToList();
            var l = resultWithSMID.LastOrDefault();
            if (l != null && l.lastID != null)
            {
                lastID = (int)l.lastID;
            }

            var wrong = 0;
            var min = 0;
            var max = 0;
            var last = 0;
            foreach (var x in resultWithSMID)
            {
                var id = (int)x.lastID;
                if (last != 0)
                {
                    if (id <= last)
                    {
                        wrong++;
                    }
                }
                last = id;
                if (min == 0)
                {
                    min = id;
                }
                if (id < min)
                {
                    min = id;
                }
                if (id > max)
                {
                    max = id;
                }
            }
            messages.Add($"Wrong ID sequences: {wrong}");
            messages.Add($"Min ID: {min}");
            messages.Add($"Max ID: {max}");

            var result = resultWithSMID
                .Select(sm => new SMResult()
                {
                    smId = sm.SM_Id,
                    smIdentifier = sm.Identifier,
                    timeStampTree = sm.TimeStampTree,
                    url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm.Identifier ?? string.Empty)}",
                })
                .ToList();
            return result;
        }
        else
        {
            var result = query
                .Select(sm => new SMResult()
                {
                    smId = sm.SM_Id,
                    smIdentifier = sm.Identifier,
                    timeStampTree = sm.TimeStampTree,
                    url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm.Identifier ?? string.Empty)}",
                })
                .ToList();
            return result;
        }
    }

    // --------------- SME Methods ---------------
    private static IQueryable<SMESet> GetFilteredSMESets(DbSet<SMESet> smeSets, List<string> idShortPathList)
    {
        if (idShortPathList.Count == 0)
        {
            return smeSets;
        }

        // Provided list of idShortPath and their depths
        var depthList = idShortPathList.Select(path => path.Split('.').Length).ToList();

        // Prepare the list of valid entries for SQL
        var prefixEntriesSql = $@"(Depth < {depthList[0]} AND '{idShortPathList[0]}' LIKE BuiltIdShortPath || '%')";
        for (var i = 1; i < idShortPathList.Count; i++)
        {
            prefixEntriesSql += $@" OR (Depth < {depthList[i]} AND '{idShortPathList[i]}' LIKE BuiltIdShortPath || '%')";
        }

        var finalEntriesSql = $@"(Depth = {depthList[0]} AND '{idShortPathList[0]}' = BuiltIdShortPath)";
        for (var i = 1; i < idShortPathList.Count; i++)
        {
            finalEntriesSql += $@" OR (Depth = {depthList[i]} AND '{idShortPathList[i]}' = BuiltIdShortPath)";
        }

        // Recursive CTE to build the idShortPath and count the depth
        var sql = $@"
                --
                -- Find IdShortPath
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
                WHERE ({finalEntriesSql})
                --
            ";
        // Execute the query
        return smeSets.FromSqlRaw(sql);
    }

    private IQueryable? GetSMEs(bool noSecurity, Dictionary<string, string>? securityCondition, QResult qResult, Stopwatch watch, string requested, AasContext db, bool withCount = false, bool withTotalCount = false,
        string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diffString = "", int pageFrom = -1, int pageSize = -1,
        string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
    {
        // parameter
        var messages = qResult.Messages ?? [];
        var rawSQL = qResult.SQL ?? [];

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

        var orderBy = false;
        var lastID = -1;
        if (expression.Contains("$LASTID="))
        {
            var index1 = expression.IndexOf("$LASTID");
            var index2 = expression.IndexOf("=", index1);
            var index3 = expression.IndexOf(";", index1);
            var s = expression.Substring(index2 + 1, index3 - index2 - 1);
            var s2 = expression.Substring(index1, index3 - index1 + 1);
            if (s.StartsWith("+"))
            {
                orderBy = true;
                s = s.Substring(1);
            }
            lastID = int.Parse(s);
            expression = expression.Replace(s2, "");
            messages.Add("$LASTID=" + pageSize);
        }

        // get condition out of expression
        var conditionsExpression = ConditionFromExpression(noSecurity, messages, expression, securityCondition);

        if (conditionsExpression.ContainsKey("AccessRules"))
        {
            messages.Add(conditionsExpression["AccessRules"]);
            return null;
        }

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
            restrictSME = !conditionsExpression["sme"].IsNullOrEmpty() && !conditionsExpression["sme"].Equals("true");
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

        var smSelect = smTable.Select("new { Id, Identifier, IdShort }");
        var smeSelect = smeTable.Select("new { Id, SMId, IdShort, TimeStamp, IdShortPath }");
        var x1 = smeSelect.Take(100).ToDynamicList();

        var smSmeSelect = smSelect.AsQueryable().Join(
            smeSelect.AsQueryable(),
            "Id",
            "SMId",
            "new (outer as SM, inner as SME)"
        )
        .Select("new {" +
            "SM.Id as SM_Id, SM.Identifier as SM_Identifier, SM.IdShort as SM_IdShort, " +
            "SME.Id as SME_Id, SME.IdShort as SME_IdShort, SME.TimeStamp as SME_TimeStamp, SME.IdShortPath as SME_IdShortPath" +
            " }");
        var x2 = smSmeSelect.Take(100).ToDynamicList();

        // var count = smSmeSelect.Select("new { SME_Id }").Count();

        IQueryable<CombinedValue> combinedValues = null;
        if (sValueTable != null)
        {
            var sValueSelect = sValueTable.Select(sv => new CombinedValue
            {
                SMEId = sv.SMEId,
                SValue = sv.Value,
                MValue = null
            });
            combinedValues = sValueSelect;
        }
        if (iValueTable != null)
        {
            var iValueSelect = iValueTable.Select(iv => new CombinedValue
            {
                SMEId = iv.SMEId,
                SValue = null,
                MValue = iv.Value
            });

            if (combinedValues != null)
            {
                combinedValues = combinedValues
                    .Concat(iValueSelect);
            }
            else
            {
                combinedValues = iValueSelect;
            }
        }
        if (dValueTable != null)
        {
            var dValueSelect = dValueTable.Select(dv => new CombinedValue
            {
                SMEId = dv.SMEId,
                SValue = null,
                MValue = dv.Value
            });

            if (combinedValues != null)
            {
                combinedValues = combinedValues
                    .Concat(dValueSelect);
            }
            else
            {
                combinedValues = dValueSelect;
            }
        }

        var smSmeValue = smSmeSelect.AsQueryable().Join(
            combinedValues.AsQueryable(),
            "SME_Id",
            "SMEId",
            "new (outer as SMSME, inner as VALUE)"
        )
        .Select("new (" +
            "SMSME.SM_Id as SM_Id, SMSME.SM_Identifier as SM_Identifier, SMSME.SM_IdShort as SM_IdShort, " +
            "SMSME.SME_Id as SME_Id, SMSME.SME_IdShort as SME_IdShort, SMSME.SME_TimeStamp as SME_TimeStamp, SMSME.SME_IdShortPath as SME_IdShortPath, " +
            "VALUE.SValue as SValue, VALUE.MValue as MValue" +
            ")");
        var x3 = smSmeValue.Take(100).ToDynamicList();

        conditionAll = conditionAll.Replace("sm.idShort", "SM_IdShort").Replace("sme.idShort", "SME_IdShort").Replace("sme.idShortPath", "SME_IdShortPath");
        var smSmeValueWhere = smSmeValue.Where(conditionAll);
        var x4 = smSmeValueWhere.Take(100).ToDynamicList();

        // count = smSmeValue.Select("new { SME_Id }").Count();

        // combine tables to a raw sql 
        // var rawSQLEx = CombineTablesToRawSQL(direction, smTable, smeTable, sValueTable, iValueTable, dValueTable, withParaWithoutValue);

        // create queryable
        /*
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
        */
        var combineTableQuery = smSmeValueWhere;

        // select for count
        if (withCount)
        {
            // var qCount = combineTableQuery.Select(sme => sme.SME_Id);
            var qCount = combineTableQuery.Select("SME_Id");
            return qCount;
        }

        if (false)
        {
            // select for not count
            // var combineTableQueryS = combineTableQuery.Select(sme => new { sme.SME_Id, sme.SM_Identifier, sme.V_Value, sme.SME_TimeStamp });
            var combineTableQueryS = combineTableQuery.Select("new { SME_Id, SM_Identifier, SValue as V_Value, sme.SME_TimeStamp }");
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
        }

        // Create queryable
        // var qSearch = db.Database.SqlQueryRaw<CombinedSMEResult>(combineTableRawSQL);
        /*
        var qSearch = smSmeValueWhere.Select("new { SM_Identifier }");
        var q = qSearch.ToDynamicList().Select(o => new CombinedSMEResult { SM_Identifier = o.SM_Identifier });
        var raw = qSearch.ToQueryString();
        var qSearch2 = db.Database.SqlQueryRaw<CombinedSMEResult>(raw).Skip(pageFrom).Take(pageSize);
        */
        var qSearch = smSmeValueWhere;

        if (withTotalCount)
        {
            text = "-- Start totalCount at " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            messages.Add(text);
            qResult.TotalCount = qSearch.Count();
            text = "-- End totalCount at " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
        }
        messages.Add(text);

        // create queryable with pagenation
        qResult.PageFrom = pageFrom;
        qResult.PageSize = pageSize;
        if (pageFrom != -1)
        {
            if (orderBy)
            {
                Console.WriteLine("OrderBy");
                messages.Add("OrderBy");
                qSearch = qSearch.OrderBy("SME_Id").Skip(pageFrom).Take(pageSize);
            }
            else
            {
                qSearch = qSearch.Skip(pageFrom).Take(pageSize);
            }
            text = "Using pageFrom and pageSize.";
            Console.WriteLine(text);
            messages.Add(text);
        }
        else
        {
            if (lastID != -1 && pageSize != -1)
            {
                if (orderBy)
                {
                    Console.WriteLine("OrderBy");
                    messages.Add("OrderBy");
                    qSearch = qSearch.OrderBy("SME_Id").Where($"SME_Id > {lastID}").Take(pageSize);
                }
                else
                {
                    qSearch = qSearch.Where($"SME_Id > {lastID}").Take(pageSize);
                }
                text = "Using lastID and pageSize.";
                Console.WriteLine(text);
                messages.Add(text);
            }
        }

        // if needed create idShortPath
        if (false && (requested.Contains("idShortPath") || requested.Contains("url")))
        {
            var combineTableRawSQL = qSearch.ToQueryString();
            // Append the "get path" section
            combineTableRawSQL = $@"
                    WITH FilteredSMAndSMEAndValue AS (
                        {combineTableRawSQL}
                    ), 
                    RecursiveSME AS (
                        WITH RECURSIVE SME_CTE AS (
                            SELECT Id, IdShort, ParentSMEId, IdShort AS IdShortPath, Id AS StartId 
                            FROM SMESets 
                            WHERE Id IN (SELECT ""SME_Id"" FROM FilteredSMAndSMEAndValue) 
                            UNION ALL 
                            SELECT x.Id, x.IdShort, x.ParentSMEId, x.IdShort || '.' || c.IdShortPath, c.StartId 
                            FROM SMESets x 
                            INNER JOIN SME_CTE c ON x.Id = c.ParentSMEId 
                        ) 
                        SELECT StartId AS Id, IdShortPath 
                        FROM SME_CTE 
                        WHERE ParentSMEId IS NULL 
                    )
                    SELECT sme.SM_Identifier, r.IdShortPath, strftime('%Y-%m-%d %H:%M:%f', sme.TimeStamp) AS SME_TimeStamp, sme.SValue, sme.MValue
                    FROM FilteredSMAndSMEAndValue AS sme 
                    INNER JOIN RecursiveSME AS r ON sme.SME_Id = r.Id;
                    ";
            // qSearch = db.Set<SMEResultRaw>().FromSqlRaw(combineTableRawSQL).AsQueryable();
            qSearch = db.Database.SqlQueryRaw<CombinedSMEResult>(combineTableRawSQL);
        }

        // return raw SQL
        var smRawSQL = qSearch.ToQueryString();
        var rawSqlSplit = smRawSQL.Replace("\r", "").Split("\n").Select(s => s.TrimStart()).ToList();
        rawSQL.AddRange(rawSqlSplit);

        return qSearch;
    }

    private static List<SMEResult> GetSMEResult(QResult qResult, AasContext db, string requested, IQueryable query, bool withLastID, out int lastID)
    {
        var messages = qResult.Messages ?? [];

        var withSmId = requested.Contains("smId");
        var withTimeStamp = requested.Contains("timeStamp");
        var withValue = requested.Contains("value");
        var withIdShortPath = requested.Contains("idShortPath");
        var withUrl = requested.Contains("url");

        if (false) // withIdShortPath || withUrl)
        {
            // created idShortPath for result only
            var smeIdList = query.Select("SME_Id").ToDynamicList<int>();
            var smeSearch = db.SMESets.Where(sme => smeIdList.Contains(sme.Id)).Select(sme => new { sme.Id, IdShortPath = sme.IdShort, sme.ParentSMEId });
            var smeResult = smeSearch.Where(sme => sme.ParentSMEId == null);
            while (smeSearch != null && smeSearch.Any(sme => sme.ParentSMEId != null))
            {
                smeSearch = smeSearch.Where(sme => sme.ParentSMEId != null);
                var joinedResult = smeSearch
                    .Join(db.SMESets,
                          sme => sme.ParentSMEId,
                          parentSme => parentSme.Id,
                          (sme, parentSme) => new
                          {
                              sme.Id,
                              IdShortPath = parentSme.IdShort + "." + sme.IdShortPath,
                              parentSme.ParentSMEId
                          });
                if (joinedResult != null)
                {
                    smeResult = smeResult.Concat(joinedResult.Where(sme => sme.ParentSMEId == null));
                    smeSearch = joinedResult.Where(sme => sme.ParentSMEId != null);
                }
                else
                {
                    smeSearch = null;
                }
            };

            var queryJoin = query.Join(smeResult, "SME_Id", "Id", "new (outer as q, inner as s)")
                .Select("new { q.SME_Id as SME_Id, q.SM_Identifier as SM_Identifier, q.SME_TimeStamp as SME_TimeStamp, " +
                "q.SValue as SValue, q.MValue as MValue, s.IdShortPath as SME_IdShortPath }");
            query = queryJoin;
        }

        List<SMEResult> smeResults = null;
        lastID = 0;
        if (withLastID)
        {
            var resultWithSMEID = query
                .ToDynamicList();
            var l = resultWithSMEID.LastOrDefault();
            if (l != null && l.SME_Id != null)
            {
                lastID = (int)l.SME_Id;
            }

            var wrong = 0;
            var min = 0;
            var max = 0;
            var last = 0;
            foreach (var x in resultWithSMEID)
            {
                var id = x.SME_Id;
                if (last != 0)
                {
                    if (id <= last)
                    {
                        wrong++;
                    }
                }
                last = id;
                if (min == 0)
                {
                    min = id;
                }
                if (id < min)
                {
                    min = id;
                }
                if (id > max)
                {
                    max = id;
                }
            }
            messages.Add($"Wrong ID sequences: {wrong}");
            messages.Add($"Min ID: {min}");
            messages.Add($"Max ID: {max}");

            var result = resultWithSMEID
                .Select(sme => new SMEResult()
                {
                    smId = withSmId ? sme.SM_Identifier : null,
                    timeStamp = withTimeStamp ? sme.SME_TimeStamp.ToString() : null,
                    value = withValue ? (sme.SValue ?? sme.MValue.ToString()) : null,
                    idShortPath = withIdShortPath ? sme.SME_IdShortPath : null,
                    url = withUrl && sme.SM_Identifier != null && sme.SME_IdShortPath != null
                    ? $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sme.SM_Identifier)}/submodel-elements/{sme.SME_IdShortPath}"
                    : null,
                })
                .ToList();
            return result;
        }
        else
        {
            smeResults = query.ToDynamicList()
                .Select(sme => new SMEResult
                {
                    smId = withSmId ? sme.SM_Identifier : null,
                    timeStamp = withTimeStamp ? sme.SME_TimeStamp.ToString() : null,
                    value = withValue ? (sme.SValue ?? sme.MValue.ToString()) : null,
                    idShortPath = withIdShortPath ? sme.SME_IdShortPath : null,
                    url = withUrl && sme.SM_Identifier != null && sme.SME_IdShortPath != null
                        ? $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sme.SM_Identifier)}/submodel-elements/{sme.SME_IdShortPath}"
                        : null,
                }).ToList();
        }


        /*
        var q = x.Select(sm_sme_v => new SMEResult
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
        */

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
            var paramSNotEmpty = paramS.FirstOrDefault(s => s != "");
            if (!paramSNotEmpty.Contains("."))
            {
                paramSNotEmpty = "'" + paramSNotEmpty + "'";
            }
            i++;
            // result = result + $"{param[0]} LIKE \'%{paramS[1]}%\' {splitParam[i]}";
            result += $"{param[0]} LIKE '%' || {paramSNotEmpty} || '%' {splitParam[i]}";
        }
        return result;
    }

    private static string CombineTablesToRawSQL(int direction, IQueryable<SMSet> smTable, IQueryable<SMESet> smeTable, IQueryable<SValueSet>? sValueTable, IQueryable<IValueSet>? iValueTable, IQueryable<DValueSet>? dValueTable, bool withParaWithoutValue)
    {
        // set select
        var smSelect = smTable.Select(sm => new
        {
            SM_Id = sm.Id,
            SM_SemanticId = sm.SemanticId,
            SM_IdShort = sm.IdShort,
            SM_DisplayName = sm.DisplayName,
            SM_Description = sm.Description,
            SM_Identifier = sm.Identifier,
            SM_TimeStampTree = sm.TimeStampTree
        });
        var smeSelect = smeTable.Select(sme => new
        {
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
        var sValueSelect = sValueTable?.Select(sV => new
        {
            V_SMEId = sV.SMEId,
            V_Value = sV.Value,
            V_D_Value = sV.Value
        });
        var iValueSelect = iValueTable?.Select(iV => new
        {
            V_SMEId = iV.SMEId,
            V_Value = iV.Value,
            V_D_Value = iV.Value
        });
        var dValueSelect = dValueTable?.Select(dV => new
        {
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
                        $"sm.SM_Id, " +
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
                    $"sm_sme.SM_Id, " +
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
                            $"sm_sme.SM_Id, " +
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
                        $"sm.SM_Id, " +
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
                    $"sm_sme.SM_Id, " +
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
                            $"sm_sme.SM_Id, " +
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
                    $"sm.SM_Id, " +
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

    private Dictionary<string, string>? ConditionFromExpression(bool noSecurity, List<string> messages, string expression, Dictionary<string, string>? securityCondition)
    {
        var text = string.Empty;
        var condition = new Dictionary<string, string>();

        // no expression
        if (expression.IsNullOrEmpty())
            return condition;

        if (expression == "$all")
        {
            if (securityCondition != null && securityCondition.TryGetValue("all", out var value) && value != "")
            {
                condition["all"] = value;
            }

            if (securityCondition != null && securityCondition.TryGetValue("sm.", out value) && value != "")
            {
                condition["sm"] = value.Replace("sm.", "");
            }

            if (securityCondition != null && securityCondition.TryGetValue("sme.", out value) && value != "")
            {
                condition["sme"] = value.Replace("sme.", "");
            }

            if (securityCondition != null && securityCondition.TryGetValue("svalue", out value) && value != "")
            {
                condition["svalue"] = value.Replace("svalue", "value");
            }

            if (securityCondition != null && securityCondition.TryGetValue("mvalue", out value) && value != "")
            {
                condition["nvalue"] = value.Replace("mvalue", "value");
            }

            return condition;
        }

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
        if (withQueryLanguage == 3)
        {
            Root? deserializedData = null;

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

                var jsonSchema = "";
                if (System.IO.File.Exists("jsonschema-query.txt"))
                {
                    jsonSchema = System.IO.File.ReadAllText("jsonschema-query.txt");
                    string jsonData = expression;

                    /*
                    // NewtonSoft works, but is AGPL3
                    // Schema parsen
                    JSchema schema = JSchema.Parse(jsonSchema);

                    // JSON-Daten parsen
                    JObject jsonObject = JObject.Parse(jsonData);

                    // Validierung durchfÃ¼hren
                    IList<string> validationErrors = new List<string>();
                    bool isValid = jsonObject.IsValid(schema, out validationErrors);
                    */

                    var isValid = true;

                    if (isValid)
                    {
                        messages.Add("JSON is valid.");
                        try
                        {
                            deserializedData = JsonConvert.DeserializeObject<Root>(jsonData);
                            if (deserializedData != null)
                            {
                                messages.Add("Successfully deserialized.");

                                // mode: all, sm., sme., svalue, mvalue
                                List<LogicalExpression?> logicalExpressions = [];
                                List<Dictionary<string, string>> conditions = [];
                                if (deserializedData.Query != null && deserializedData.Query.Condition != null)
                                {
                                    logicalExpressions.Add(deserializedData.Query.Condition);
                                    conditions.Add(deserializedData.Query._query_conditions);
                                    if (deserializedData.Query.Filter != null)
                                    {
                                        logicalExpressions.Add(deserializedData.Query.Filter);
                                        conditions.Add(deserializedData.Query._filter_conditions);
                                    }
                                }
                                if (logicalExpressions.Count != 0)
                                {
                                    for (int i = 0; i < logicalExpressions.Count; i++)
                                    {
                                        var le = logicalExpressions[i];
                                        if (le != null)
                                        {
                                            QueryGrammarJSON.createExpression("all", le);
                                            conditions[i].Add("all", le._expression);
                                            QueryGrammarJSON.createExpression("sm.", le);
                                            conditions[i].Add("sm.", le._expression);
                                            QueryGrammarJSON.createExpression("sme.", le);
                                            conditions[i].Add("sme.", le._expression);
                                            QueryGrammarJSON.createExpression("svalue", le);
                                            conditions[i].Add("svalue", le._expression);
                                            QueryGrammarJSON.createExpression("mvalue", le);
                                            conditions[i].Add("mvalue", le._expression);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                isValid = false;
                            }
                        }
                        catch
                        {
                            isValid = false;
                        }
                    }
                    if (!isValid)
                    {
                        messages.Add("JSON not valid:");
                        /*
                        foreach (var error in validationErrors)
                        {
                            messages.Add($"- {error}");
                        }
                        */
                        return null;
                    }
                }
                else
                {
                    messages.Add("jsonschema-query.txt not found.");
                    return null;
                }

                var query = deserializedData.Query;
                if (deserializedData == null || query == null)
                {
                    return null;
                }

                if (query.Select != null)
                {
                    condition["select"] = query.Select;
                }

                var value = "";
                condition["all"] = query._query_conditions["all"];
                if (query._filter_conditions != null)
                {
                    condition["filter-all"] = query._filter_conditions["all"];
                }
                if (condition["all"].Contains("$$path$$"))
                {
                    messages.Add("PATH SEARCH");
                }
                else
                {
                    messages.Add("RECURSIVE SEARCH");
                    if (securityCondition != null && securityCondition.TryGetValue("all", out value) && value != "")
                    {
                        if (condition["all"] != "")
                        {
                            condition["all"] = value + " && " + condition["all"];
                        }
                        else
                        {
                            condition["all"] = value;
                        }
                    }
                }
                messages.Add("");

                text = "combinedCondition: " + condition["all"];
                Console.WriteLine(text);
                messages.Add(text);

                condition["sm"] = query._query_conditions["sm."];
                if (condition["sm"] == "$SKIP")
                {
                    condition["sm"] = "";
                }
                else
                {
                    condition["sm"] = condition["sm"].Replace("sm.", "");
                }
                if (securityCondition != null && securityCondition.TryGetValue("sm.", out value) && value != "")
                {
                    if (condition["sm"] != "")
                    {
                        condition["sm"] = value + " && " + condition["sm"];
                    }
                    else
                    {
                        condition["sm"] = value;
                    }
                }
                if (!condition["all"].Contains("$$path$$"))
                {
                    text = "conditionSM: " + condition["sm"];
                    Console.WriteLine(text);
                    messages.Add(text);
                }

                condition["sme"] = query._query_conditions["sme."];
                if (condition["sme"] == "$SKIP")
                {
                    condition["sme"] = "";
                }
                else
                {
                    condition["sme"] = condition["sme"].Replace("sme.", "");
                }
                if (securityCondition != null && securityCondition.TryGetValue("sme.", out value) && value != "")
                {
                    if (condition["sme"] != "")
                    {
                        condition["sme"] = value + " && " + condition["sme"];
                    }
                    else
                    {
                        condition["sme"] = value;
                    }
                }
                if (!condition["all"].Contains("$$path$$"))
                {
                    text = "conditionSME: " + condition["sme"];
                    Console.WriteLine(text);
                    messages.Add(text);
                }

                condition["svalue"] = query._query_conditions["svalue"];
                if (condition["svalue"] == "$SKIP")
                {
                    condition["svalue"] = "";
                }
                else
                {
                    condition["svalue"] = condition["svalue"].Replace("svalue", "value");
                }
                if (securityCondition != null && securityCondition.TryGetValue("svalue", out value) && value != "")
                {
                    if (condition["svalue"] != "")
                    {
                        condition["svalue"] = value + " && " + condition["svalue"];
                    }
                    else
                    {
                        condition["svalue"] = value;
                    }
                }
                if (!condition["all"].Contains("$$path$$"))
                {
                    text = "conditionSValue: " + condition["svalue"];
                    Console.WriteLine(text);
                    messages.Add(text);
                }

                condition["nvalue"] = query._query_conditions["mvalue"];
                if (condition["nvalue"] == "$SKIP")
                {
                    condition["nvalue"] = "";
                }
                else
                {
                    condition["nvalue"] = condition["nvalue"].Replace("nvalue", "value");
                }
                if (securityCondition != null && securityCondition.TryGetValue("mvalue", out value) && value != "")
                {
                    if (condition["nvalue"] != "")
                    {
                        condition["nvalue"] = value + " && " + condition["nvalue"];
                    }
                    else
                    {
                        condition["nvalue"] = value;
                    }
                }
                if (!condition["all"].Contains("$$path$$"))
                {
                    text = "conditionNValue: " + condition["nvalue"];
                    Console.WriteLine(text);
                    messages.Add(text);
                }
            }

            messages.Add("");
        }
        else
        {
            return null;
        }

        // handle tags in condition
        var allPathExpressions = "";
        var pathAllCondition = "";
        var pathAllConditionRaw = "";
        var countPathExpressions = 0;
        foreach (var c in condition)
        {
            var value = c.Value;
            if (c.Key == "all")
            {
                pathAllCondition = c.Value.Copy();
                pathAllConditionRaw = c.Value.Copy();
            }
            while (value.Contains("$$tag$$path$$"))
            {
                var split = value.Split("$$tag$$path$$");
                var firstTag = split[1];
                split = firstTag.Split("$$");
                var idShortPath = split[0];
                var field = split[1];
                var exp = split[2];

                var nextPathExpression = "";
                var idShort = idShortPath;
                if (idShort.Contains("."))
                {
                    var s = idShort.Split(".");
                    idShort = s.Last();
                }
                nextPathExpression = $"({field}{exp} && sme.idShort == \"{idShort}\" && sme.idShortPath == \"{idShortPath}\" )";
                if (c.Key == "all")
                {
                    if (allPathExpressions == "")
                    {
                        allPathExpressions = nextPathExpression;
                    }
                    else
                    {
                        allPathExpressions += "$$" + nextPathExpression;
                    }
                    pathAllCondition = pathAllCondition.Replace($"$$tag$$path$${idShortPath}$${field}$${exp}$$", $"$$path{countPathExpressions}$$");
                    countPathExpressions++;
                }
                value = value.Replace($"$$tag$$path$${idShortPath}$${field}$${exp}$$", "true");
            }
            condition[c.Key] = value;
        }
        if (allPathExpressions != "")
        {
            condition["path-sme"] = allPathExpressions;
            condition["path-all"] = pathAllCondition;
            condition["path-raw"] = pathAllConditionRaw;
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

