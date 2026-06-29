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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using AasCore.Aas3_1;
using AasxServerDB.Entities;
using Contracts;
using Contracts.QueryResult;
using Contracts.Security;
using Extensions;
// using Newtonsoft.Json.Schema;
using Irony.Parsing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;
using static AasxServerDB.CrudOperator;

public class CombinedValue
{
    public int SMEId { get; set; }
    public String? SValue { get; set; }
    public Double? MValue { get; set; }
    public DateTime? DTValue { get; set; }
    /// <summary>Serialized <see cref="AasCore.Aas3_1.DataTypeDefXsd"/> (property) or language (MLP), same as DB <c>ValueSet.Annotation</c>.</summary>
    public String? Annotation { get; set; }
}

public class QueryResult
{
    public List<IAssetAdministrationShell> Shells { get; set; } = [];
    public List<ISubmodel> Submodels { get; set; } = [];
    public List<ISubmodelElement> SubmodelElements { get; set; } = [];
    public List<string> Ids { get; set; } = [];
    public List<string> Sql { get; set; } = [];
}

public class SMSetIdResult
{
    public int Id { get; set; }
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

    public List<int> SearchSMs(AasContext db, int pageFrom, int pageSize, string expression, SqlConditions? securitySqlConditions = null)
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

        var query = GetSMs(out _, ResultType.Submodel, qResult, watch, db, false, false, "", "", "", pageFrom, pageSize, expression, securitySqlConditions: securitySqlConditions);

        return query;
    }

    //public QResult SearchSMs(Dictionary<string, string>? securityCondition, AasContext db, bool withTotalCount, bool withLastId, string semanticId,
    //    string identifier, string diff, int pageFrom, int pageSize, string expression)
    //{
    //    bool noSecurity = securityCondition == null;

    //    var qResult = new QResult()
    //    {
    //        Count = 0,
    //        TotalCount = 0,
    //        PageFrom = 0,
    //        PageSize = QResult.DefaultPageSize,
    //        LastID = 0,
    //        Messages = new List<string>(),
    //        SMResults = new List<SMResult>(),
    //        SMEResults = new List<SMEResult>(),
    //        SQL = new List<string>()
    //    };

    //    var text = string.Empty;

    //    var watch = Stopwatch.StartNew();
    //    Console.WriteLine("\nSearchSMs");

    //    watch.Restart();
    //    Dictionary<string, string>? condition;
    //    var query = GetSMs(noSecurity, securityCondition, out condition, "submodel", qResult, watch, db, false, withTotalCount, semanticId, identifier, diff, pageFrom, pageSize, expression);
        //if (query == null)
        //{
        //    text = "No query is generated.";
        //    Console.WriteLine(text);
        //    qResult.Messages.Add(text);
        //}
        //else
        //{
        //    text = "Generate query in " + watch.ElapsedMilliseconds + " ms";
        //    Console.WriteLine(text);
        //    qResult.Messages.Add(text);

        //    watch.Restart();
        //    int lastId = 0;
        //    var result = GetSMResult(qResult, query, "Submodel", withLastId, out lastId);
        //    qResult.LastID = lastId;
        //    qResult.Count = result.Count;
        //    text = "Collect results in " + watch.ElapsedMilliseconds + " ms";
        //    Console.WriteLine(text);
        //    qResult.Messages.Add(text);
        //    text = "SMs found ";
        //    if (withTotalCount)
        //    {
        //        text += qResult.TotalCount;
        //    }
        //    else
        //    {
        //        text += "totalCount";
        //    }
        //    text += "/" + db.SMSets.Count() + ": " + result.Count + " queried";
        //    Console.WriteLine(text);
        //    qResult.Messages.Add(text);

        //    qResult.SMResults = result;
        //}

    //    return qResult;
    //}

    //public int CountSMs(ISecurityConfig securityConfig, Dictionary<string, string>? securityCondition, AasContext db, string semanticId, string identifier, string diff, int pageFrom, int pageSize, string expression)
    //{
    //    var watch = Stopwatch.StartNew();
    //    Console.WriteLine("\nCountSMs");

    //    watch.Restart();
    //    Dictionary<string, string>? condition;
    //    var query = GetSMs(securityConfig.NoSecurity, securityCondition, out condition, "submodel",
    //        new QResult(), watch, db, true, false, semanticId, identifier, diff, pageFrom, pageSize, expression);
    //    if (query == null)
    //    {
    //        Console.WriteLine("No query is generated.");
    //        return 0;
    //    }
    //    Console.WriteLine("Generate query in " + watch.ElapsedMilliseconds + " ms");

    //    watch.Restart();
    //    var result = query.Count();
    //    Console.WriteLine("Collect results in " + watch.ElapsedMilliseconds + " ms\nSMs found\t" + result + "/" + db.SMSets.Count());

    //    return result;
    //}

    //public QResult SearchSMEs(ISecurityConfig securityConfig, Dictionary<string, string>? securityCondition,
    //    AasContext db, string requested, bool withTotalCount, bool withLastId,
    //    string smSemanticId, string smIdentifier, string semanticId, string diff, string contains,
    //    string equal, string lower, string upper, int pageFrom, int pageSize, string expression)
    //{

    //    var qResult = new QResult()
    //    {
    //        Count = 0,
    //        TotalCount = 0,
    //        PageFrom = 0,
    //        PageSize = QResult.DefaultPageSize,
    //        LastID = 0,
    //        Messages = new List<string>(),
    //        SMResults = new List<SMResult>(),
    //        SMEResults = new List<SMEResult>(),
    //        SQL = new List<string>()
    //    };

    //    var text = string.Empty;

    //    var watch = Stopwatch.StartNew();
    //    Console.WriteLine("\nSearchSMEs");

    //    watch.Restart();
    //    var query = GetSMEs(securityConfig.NoSecurity, securityCondition, qResult, watch, requested, db, false, withTotalCount,
    //        smSemanticId, smIdentifier, semanticId, diff, pageFrom, pageSize, contains, equal, lower, upper, expression);
    //    if (query == null)
    //    {
    //        text = "No query is generated.";
    //        Console.WriteLine(text);
    //        qResult.Messages.Add(text);
    //    }
    //    else
    //    {
    //        text = "Generate query in " + watch.ElapsedMilliseconds + " ms";
    //        Console.WriteLine(text);
    //        qResult.Messages.Add(text);

    //        watch.Restart();
    //        // var result = GetSMEResult(requested, (IQueryable<CombinedSMEResult>)query);
    //        int lastId = 0;
    //        var result = GetSMEResult(qResult, db, requested, query, withLastId, out lastId);
    //        qResult.LastID = lastId;
    //        qResult.Count = result.Count;
    //        text = "SMEs found ";
    //        if (withTotalCount)
    //        {
    //            text += qResult.TotalCount;
    //        }
    //        else
    //        {
    //            text += "totalCount";
    //        }
    //        text += "/" + db.SMESets.Count() + ": " + qResult.Count + " queried";
    //        Console.WriteLine(text);
    //        qResult.Messages.Add(text);
    //        text = "Collect results in " + watch.ElapsedMilliseconds + " ms";
    //        Console.WriteLine(text);
    //        qResult.Messages.Add(text);

    //        qResult.SMEResults = result;
    //    }

    //    return qResult;
    //}


    internal QueryResult GetQueryData(bool noSecurity, AasContext db,
        int pageFrom, int pageSize, ResultType resultType, string expression, bool includeDebugSql = false,
        bool sqlOnly = false, SqlConditions? securitySqlConditions = null)
    {
        // When true (--no-security), ignore any passed-in rule merge and in-memory filtering.
        var effectiveSecurity = noSecurity ? null : securitySqlConditions;

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

        // SQL-only debugging: build SQL + query plan, but skip executing the actual query.
        // Append (not prepend) so the "$JSONGRAMMAR" prefix stays at the start for grammar detection;
        // flags are stripped position-independently in GetSMs.
        if (sqlOnly)
            expression += " $SQLONLY";

        List<string>? generatedSql = (includeDebugSql || sqlOnly) ? new List<string>() : null;
        var result = GetSMs(out var effectiveSqlConditions, resultType,
            qResult, watch, db, false, false, "", "", "", pageFrom, pageSize, expression, generatedSql, effectiveSecurity);
        if (result == null)
        {
            text = "No query is generated.";
            Console.WriteLine(text);
            return null;
        }
        else
        {
            var queryDataResult = new QueryResult
            {
                Ids = [],
                Shells = [],
                Submodels = [],
                SubmodelElements = [],
                Sql = generatedSql ?? []
            };

            text = "Query data in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);

            watch.Restart();
            //var lastId = 0;

            //if (resultType == "SubmodelElement")
            //{
            //    var smIdList = query;
            //    //var smeIdList = query.Select("SME_Id").Distinct().ToDynamicList();

            //    var smList = db.SMSets.Where(sm => smIdList.Contains(sm.Id)).ToList();
            //    //var smeList = db.SMESets.Where(sme => smeIdList.Contains(sme.Id)).ToList();

            //    foreach (var sm in smList)
            //    {
            //        var smeSmList = smeList.Where(sme => sme.SMId == sm.Id);
            //        var smeTree = GetTree(db, sm, smeSmList.ToList());
            //        var smeMerged = GetSmeMerged(db, null, smeTree, sm);

            //        var smeSmListMerged = smeSmList;

            //        foreach (var sme in smeSmListMerged)
            //        {
            //            var readSme = ReadSubmodelElement(sme, smeMerged);
            //            if (readSme != null)
            //            {
            //                readSme.Extensions ??= [];
            //                readSme.Extensions.Add(new Extension("sm#id", value: sm.Identifier));
            //                if (sm.IdShort != null)
            //                {
            //                    readSme.Extensions.Add(new Extension("sm#idShort", value: sm.IdShort));
            //                }
            //                if (sm.SemanticId != null)
            //                {
            //                    readSme.Extensions.Add(new Extension("sm#semanticId", value: sm.SemanticId));
            //                }
            //                if (sme.IdShortPath != null)
            //                {
            //                    readSme.Extensions.Add(new Extension("sme#IdShortPath", value: sme.IdShortPath));
            //                }
            //                submodelsResult.SubmodelElements.Add(readSme);
            //            }
            //        }
            //    }
            //}
            //else
            //{
            //var result = GetSMResult(qResult, (IQueryable<CombinedSMResultWithAas>)query, resultType, false, out lastId);
            //text = "SMs found ";
            //text += "/" + db.SMSets.Count() + ": " + result.Count + " queried";
            //Console.WriteLine(text);

            if (resultType == ResultType.AssetAdministrationShell)
            {
                var aasIdList = result;

                if (!qResult.WithSelectId)
                {
                    var timeStamp = DateTime.UtcNow;
                    var shells = new List<IAssetAdministrationShell>();

                    if (!aasIdList.IsNullOrEmpty())
                    {
                        var aasList = db.AASSets.Where(aas => aasIdList.Contains(aas.Id)).ToList();

                        for (var i = 0; i < aasList.Count; i++)
                        {
                            var aasDB = aasList[i];
                            var aas = ReadAssetAdministrationShell(db, aasDB: ref aasDB);
                            if (aas != null)
                            {
                                shells.Add(aas);
                            }
                        }
                    }
                    queryDataResult.Shells = shells;
                }
                else
                {
                    var aasList = db.AASSets.Where(aas =>
                            aasIdList.Contains(aas.Id));
                    queryDataResult.Ids = [.. aasList.Select(aas => aas.Identifier)];
                }
            }
            else
            {
                if (!qResult.WithSelectId && !(qResult.WithSelectMatch && qResult.MatchPathList != null))
                {
                    var timeStamp = DateTime.UtcNow;
                    var submodels = new List<ISubmodel>();

                    var smIdList = result;
                    var smList = db.SMSets.Where(sm => smIdList.Contains(sm.Id)).ToList();

                    foreach (var sm in smList.Select(selector: submodelDB =>
                        ReadSubmodel(db, smDB: submodelDB, securitySqlConditions: effectiveSecurity, skipAllowCheck: true)))
                    {
                        if (sm != null)
                        {
                            //var aasId = result.FirstOrDefault(r => r.smIdentifier == sm.Id)?.aasId;
                            //if (aasId != null)
                            //{
                            //    var aasSet = db.AASSets.FirstOrDefault(a => a.Id == aasId);
                            //    if (aasSet != null)
                            //    {
                            //        sm.Extensions ??= [];
                            //        sm.Extensions.Add(new Extension("$aas#id", value: aasSet.Identifier));
                            //        if (aasSet.IdShort != null)
                            //        {
                            //            sm.Extensions.Add(new Extension("$aas#idShort", value: aasSet.IdShort));
                            //        }
                            //        if (aasSet.GlobalAssetId != null)
                            //        {
                            //            sm.Extensions.Add(new Extension("$aas#globalAssetId", value: aasSet.GlobalAssetId));
                            //        }
                            //    }
                            //}

                            if (sm.TimeStamp == DateTime.MinValue)
                            {
                                sm.SetAllParentsAndTimestamps(null, timeStamp, timeStamp, DateTime.MinValue);
                                sm.SetTimeStamp(timeStamp);
                            }
                            submodels.Add(sm);
                        }
                    }
                    queryDataResult.Submodels = submodels;
                }
                else
                {
                    var smIdList = result;

                    if (qResult.WithSelectId)
                    {
                        var smList = db.SMSets.Where(sm =>
                                smIdList.Contains(sm.Id));
                        queryDataResult.Ids = smList.Select(sm => sm.Identifier).ToList();
                    }
                    if (qResult.WithSelectMatch)
                    {
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
                                    var smeMerged = CrudOperator.GetSmeMerged(db, smeTree, sm);
                                    var readSme = CrudOperator.ReadSubmodelElement(sme, smeMerged);
                                    if (readSme != null)
                                    {
                                        readSme.Extensions ??= [];
                                        readSme.Extensions.Add(new Extension("sm#id", value: sm.Identifier));
                                        if (sm.IdShort != null)
                                        {
                                            readSme.Extensions.Add(new Extension("sm#idShort", value: sm.IdShort));
                                        }
                                        if (sm.SemanticId != null)
                                        {
                                            readSme.Extensions.Add(new Extension("sm#semanticId", value: sm.SemanticId));
                                        }
                                        if (sme.IdShortPath != null)
                                        {
                                            readSme.Extensions.Add(new Extension("sme#IdShortPath", value: sme.IdShortPath));
                                        }
                                        queryDataResult.SubmodelElements.Add(readSme);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var collectResultText = "Collect results in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(collectResultText);

            return queryDataResult;
        }
    }

    // --------------- SM Methods ---------------


    public class SmDto
    {
        public int SMId { get; set; }
        public List<bool> Conditions { get; set; } = new List<bool>();
    }

    private List<int>? GetSMs(out SqlConditions? effectiveSqlConditions,
        ResultType resultType,
        QResult qResult, Stopwatch watch, AasContext db, bool withCount = false, bool withTotalCount = false,
        string semanticId = "", string identifier = "", string diffString = "", int pageFrom = -1, int pageSize = -1, string expression = "",
        List<string>? generatedSql = null, SqlConditions? securitySqlConditions = null)
    {
        List<int>? result = null;
        effectiveSqlConditions = null;

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

        var consolidate = false;
        if (expression.Contains("$CONSOLIDATE"))
        {
            messages.Add("$CONSOLIDATE");
            consolidate = true;
            expression = expression.Replace("$CONSOLIDATE", string.Empty);
        }

        List<string> possibleFlags = ["$LEFTJOIN", "$UNION", "$TEMPTABLE", "$LEGACYSMEJOIN", "$SQLONLY"];
        List<string> flags = [];
        foreach (var flag in possibleFlags)
        {
            if (expression.Contains(flag))
            {
                flags.Add(flag);
                expression = expression.Replace(flag, string.Empty);
            }
        }

        if (flags.Contains("$LEGACYSMEJOIN"))
            messages.Add("$LEGACYSMEJOIN");

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
        if (!ConditionFromExpression(messages, expression, out var sqlConditions))
        {
            return null;
        }

        // merge query SqlConditions with security SqlConditions
        sqlConditions = SqlConditionsMerger.Merge(sqlConditions, securitySqlConditions);
        effectiveSqlConditions = sqlConditions;

        var sel = sqlConditions?.Select;
        if (!string.IsNullOrWhiteSpace(sel))
        {
            qResult.WithSelectId = sel == "id";
            qResult.WithSelectMatch = sel == "match";
        }

        List<int> comTable = null;

        // get data
        if (withExpression) // with expression
        {
            comTable = CombineTablesLEFT(db, sqlConditions, pageFrom, pageSize, resultType, flags, generatedSql);
        }

        if (comTable == null)
        {
            return null;
        }
        else
        {
            result = comTable;
        }

        return result;
    }

    private static string GetQueryPlan(AasContext db, string smRawSQL)
    {
        var qp = "";
        using var connection = new SqliteConnection(db.Database.GetDbConnection().ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN\n" + smRawSQL;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            qp += reader.GetString(3) + "\n";
        }

        return qp;
    }

    // Query plan for a multi-statement script (e.g. the $TEMPTABLE build SQL).
    // EXPLAIN QUERY PLAN only works per statement, so DDL (DROP/CREATE) is executed
    // on a private connection so the temp table exists, and the data-moving
    // statements (INSERT ... SELECT) are explained without actually inserting rows.
    private static string GetQueryPlanForScript(AasContext db, string script)
    {
        var sb = new StringBuilder();
        using var connection = new SqliteConnection(db.Database.GetDbConnection().ConnectionString);
        connection.Open();

        foreach (var raw in script.Split(';'))
        {
            var stmt = raw.Trim();
            if (stmt.Length == 0)
                continue;

            var upper = stmt.TrimStart().ToUpperInvariant();
            using var command = connection.CreateCommand();

            // DDL must run so later EXPLAINs can resolve the temp table; it has no meaningful plan.
            if (upper.StartsWith("DROP") || upper.StartsWith("CREATE"))
            {
                command.CommandText = stmt;
                command.ExecuteNonQuery();
                continue;
            }

            command.CommandText = "EXPLAIN QUERY PLAN\n" + stmt;
            using var reader = command.ExecuteReader();
            sb.Append("-- ").Append(stmt.Split('\n')[0].Trim()).Append('\n');
            while (reader.Read())
                sb.Append(reader.GetString(3)).Append('\n');
            sb.Append('\n');
        }

        return sb.ToString();
    }

    // Appends a labeled EXPLAIN QUERY PLAN block to the debug SQL output (no-op when not collecting).
    private static void AddQueryPlan(List<string>? generatedSql, string? queryPlan)
    {
        if (generatedSql == null || string.IsNullOrWhiteSpace(queryPlan))
            return;

        AddGeneratedSql(generatedSql, "-- EXPLAIN QUERY PLAN\n" + queryPlan.TrimEnd());
    }

    //private static IQueryable<CombinedSMResultWithAas> CombineSMWithAas(AasContext db, IQueryable<CombinedSMResult> smResult, IQueryable<SMRefSet>? smRef)
    //{
    //    IQueryable<CombinedSMResultWithAas> smResultWithAas;
    //    if (smRef != null)
    //    {
    //        var smRefDict = smRef
    //            .Where(r => r.Identifier != null && r.AASId != null)
    //            .Take(10000)
    //            .ToDictionary(r => r.Identifier!, r => r.AASId);

    //        smResultWithAas = smResult
    //            .Select(s => new CombinedSMResultWithAas
    //            {
    //                AAS_Id = s.Identifier != null ? smRefDict[s.Identifier] : null,
    //                SM_Id = s.SM_Id,
    //                Identifier = s.Identifier,
    //                TimeStampTree = s.TimeStampTree,
    //                MatchPathList = null
    //            });

    //        //var smRawSQL = smResultWithAas.ToQueryString();
    //        //var qp = GetQueryPlan(db, smRawSQL);
    //        return smResultWithAas;
    //    }
    //    else
    //    {
    //        smResultWithAas = smResult.Select(r1 => new CombinedSMResultWithAas
    //        {
    //            AAS_Id = null,
    //            SM_Id = r1.SM_Id,
    //            Identifier = r1.Identifier,
    //            TimeStampTree = r1.TimeStampTree,
    //            MatchPathList = null
    //        });
    //    }
    //    return smResultWithAas;
    //}

    //private static List<SMResult> GetSMResult(QResult qResult, IQueryable<CombinedSMResultWithAas> query, string resultType, bool withLastID, out int lastID)
    //{
    //    var messages = qResult.Messages ?? [];
    //    lastID = 0;

    //    if (withLastID)
    //    {
    //        var resultWithSMID = query
    //            .Select(sm => new
    //            {
    //                sm.AAS_Id,
    //                sm.SM_Id,
    //                sm.Identifier,
    //                sm.TimeStampTree,
    //                lastID = sm.SM_Id
    //            })
    //            .ToList();
    //        var l = resultWithSMID.LastOrDefault();
    //        if (l != null && l.lastID != null)
    //        {
    //            lastID = (int)l.lastID;
    //        }

    //        var wrong = 0;
    //        var min = 0;
    //        var max = 0;
    //        var last = 0;
    //        foreach (var x in resultWithSMID)
    //        {
    //            var id = (int)x.lastID;
    //            if (last != 0)
    //            {
    //                if (id <= last)
    //                {
    //                    wrong++;
    //                }
    //            }
    //            last = id;
    //            if (min == 0)
    //            {
    //                min = id;
    //            }
    //            if (id < min)
    //            {
    //                min = id;
    //            }
    //            if (id > max)
    //            {
    //                max = id;
    //            }
    //        }
    //        messages.Add($"Wrong ID sequences: {wrong}");
    //        messages.Add($"Min ID: {min}");
    //        messages.Add($"Max ID: {max}");

    //        var result = resultWithSMID
    //            .Select(sm => new SMResult()
    //            {
    //                aasId = sm.AAS_Id,
    //                smId = sm.SM_Id,
    //                smIdentifier = sm.Identifier,
    //                timeStampTree = sm.TimeStampTree,
    //                url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm.Identifier ?? string.Empty)}",
    //            })
    //            .ToList();
    //        return result;
    //    }
    //    else
    //    {
    //        var result = query
    //            .Select(sm => new SMResult()
    //            {
    //                aasId = sm.AAS_Id,
    //                smId = sm.SM_Id,
    //                smIdentifier = sm.Identifier,
    //                timeStampTree = sm.TimeStampTree,
    //                url = $"{ExternalBlazor}/submodels/{Base64UrlEncoder.Encode(sm.Identifier ?? string.Empty)}",
    //            })
    //            .ToList();
    //        return result;
    //    }
    //}

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
    private static List<string> GetSqlFields(string alias, string? expression)
    {
        var fields = new List<string>();
        var prefix = $"\"{alias}\".";

        if (expression != null)
        {
            var split1 = expression.Split(prefix);
            foreach (var s1 in split1)
            {
                if (string.IsNullOrWhiteSpace(s1) || !s1.StartsWith('\"'))
                {
                    continue;
                }

                var field = s1.Split([' ', ')', ',']).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(field))
                {
                    continue;
                }

                field = field.Trim('\"');
                if (!string.IsNullOrWhiteSpace(field) && !fields.Contains(field))
                {
                    fields.Add(field);
                }
            }
        }

        return fields;
    }

    private static string NormalizeSqlAliases(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return "";
        }

        return sql
            .Replace("\"a\".", "\"aas\".")
            .Replace("\"t\".", "\"sm\".")
            .Replace("\"s0\".", "\"sm\".")
            .Replace("\"s1\".", "\"sme\".")
            .Replace("\"v\".", "\"value\".");
    }

    /// <summary>
    /// Sucht im großen wherePath nach verbleibenden v.* Prädikaten (aus convertConditionSQL),
    /// baut dafür Flag-CTEs und ersetzt sie durch CASE WHEN.
    /// 
    /// Achtung: Das ist bewusst simpel (für SValue/NValue/DTValue in der Form: v."SValue" = ...).
    /// Wenn du komplexere Ausdrücke hast, erweitern wir das später.
    /// </summary>
    /// <summary>
    /// Splits a WHERE expression into top-level OR parts.
    /// It only splits on OR tokens when:
    /// - parentheses depth == 0
    /// - not inside single-quoted strings
    /// - not inside double-quoted identifiers
    /// - not inside -- line comments or /* block comments */
    /// </summary>
    public static List<string> SplitTopLevelOr(string whereExpression)
    {
        if (whereExpression == null)
            throw new ArgumentNullException(nameof(whereExpression));

        var parts = new List<string>();
        var sb = new StringBuilder(whereExpression.Length);

        int depth = 0;
        bool inSingle = false;   // '...'
        bool inDouble = false;   // "..."
        bool inLineComment = false;   // -- ...
        bool inBlockComment = false;  // /* ... */

        for (int i = 0; i < whereExpression.Length; i++)
        {
            char c = whereExpression[i];
            char next = i + 1 < whereExpression.Length ? whereExpression[i + 1] : '\0';

            // Handle exiting line comment
            if (inLineComment)
            {
                sb.Append(c);
                if (c == '\n')
                    inLineComment = false;
                continue;
            }

            // Handle exiting block comment
            if (inBlockComment)
            {
                sb.Append(c);
                if (c == '*' && next == '/')
                {
                    sb.Append(next);
                    i++;
                    inBlockComment = false;
                }
                continue;
            }

            // Enter comments (only when not in quotes)
            if (!inSingle && !inDouble)
            {
                if (c == '-' && next == '-')
                {
                    sb.Append(c).Append(next);
                    i++;
                    inLineComment = true;
                    continue;
                }
                if (c == '/' && next == '*')
                {
                    sb.Append(c).Append(next);
                    i++;
                    inBlockComment = true;
                    continue;
                }
            }

            // Handle quotes
            if (!inDouble && c == '\'')
            {
                sb.Append(c);
                if (inSingle)
                {
                    // SQL escape '' inside string
                    if (next == '\'')
                    {
                        sb.Append(next);
                        i++;
                    }
                    else
                    {
                        inSingle = false;
                    }
                }
                else
                {
                    inSingle = true;
                }
                continue;
            }

            if (!inSingle && c == '"')
            {
                sb.Append(c);
                // In SQLite/SQL, "" escapes a quote inside quoted identifier
                if (inDouble)
                {
                    if (next == '"')
                    {
                        sb.Append(next);
                        i++;
                    }
                    else
                    {
                        inDouble = false;
                    }
                }
                else
                {
                    inDouble = true;
                }
                continue;
            }

            // Parentheses depth (only when not in quotes)
            if (!inSingle && !inDouble)
            {
                if (c == '(')
                { depth++; sb.Append(c); continue; }
                if (c == ')')
                { depth = Math.Max(0, depth - 1); sb.Append(c); continue; }
            }

            // Check for top-level OR token
            if (!inSingle && !inDouble && depth == 0 && IsOrTokenAt(whereExpression, i))
            {
                // Flush current buffer as a part
                var part = sb.ToString().Trim();
                if (part.Length > 0)
                    parts.Add(part);
                sb.Clear();

                // Skip the OR token ("OR")
                i += 1; // because loop will i++ again, total skip 2 chars
                // Also skip following whitespace
                while (i + 1 < whereExpression.Length && char.IsWhiteSpace(whereExpression[i + 1]))
                    i++;

                continue;
            }

            sb.Append(c);
        }

        var last = sb.ToString().Trim();
        if (last.Length > 0)
            parts.Add(last);

        return parts;
    }

    /// <summary>
    /// Splits a WHERE expression into top-level AND parts (same token rules as <see cref="SplitTopLevelOr"/>).
    /// </summary>
    public static List<string> SplitTopLevelAnd(string whereExpression)
    {
        if (whereExpression == null)
            throw new ArgumentNullException(nameof(whereExpression));

        var parts = new List<string>();
        var sb = new StringBuilder(whereExpression.Length);

        int depth = 0;
        bool inSingle = false;
        bool inDouble = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = 0; i < whereExpression.Length; i++)
        {
            char c = whereExpression[i];
            char next = i + 1 < whereExpression.Length ? whereExpression[i + 1] : '\0';

            if (inLineComment)
            {
                sb.Append(c);
                if (c == '\n')
                    inLineComment = false;
                continue;
            }

            if (inBlockComment)
            {
                sb.Append(c);
                if (c == '*' && next == '/')
                {
                    sb.Append(next);
                    i++;
                    inBlockComment = false;
                }
                continue;
            }

            if (!inSingle && !inDouble)
            {
                if (c == '-' && next == '-')
                {
                    sb.Append(c).Append(next);
                    i++;
                    inLineComment = true;
                    continue;
                }
                if (c == '/' && next == '*')
                {
                    sb.Append(c).Append(next);
                    i++;
                    inBlockComment = true;
                    continue;
                }
            }

            if (!inDouble && c == '\'')
            {
                sb.Append(c);
                if (inSingle)
                {
                    if (next == '\'')
                    {
                        sb.Append(next);
                        i++;
                    }
                    else
                    {
                        inSingle = false;
                    }
                }
                else
                {
                    inSingle = true;
                }
                continue;
            }

            if (!inSingle && c == '"')
            {
                sb.Append(c);
                if (inDouble)
                {
                    if (next == '"')
                    {
                        sb.Append(next);
                        i++;
                    }
                    else
                    {
                        inDouble = false;
                    }
                }
                else
                {
                    inDouble = true;
                }
                continue;
            }

            if (!inSingle && !inDouble)
            {
                if (c == '(')
                {
                    depth++;
                    sb.Append(c);
                    continue;
                }

                if (c == ')')
                {
                    depth = Math.Max(0, depth - 1);
                    sb.Append(c);
                    continue;
                }
            }

            if (!inSingle && !inDouble && depth == 0 && IsAndTokenAt(whereExpression, i))
            {
                var part = sb.ToString().Trim();
                if (part.Length > 0)
                    parts.Add(part);
                sb.Clear();
                i += 2;
                while (i + 1 < whereExpression.Length && char.IsWhiteSpace(whereExpression[i + 1]))
                    i++;
                continue;
            }

            sb.Append(c);
        }

        var last = sb.ToString().Trim();
        if (last.Length > 0)
            parts.Add(last);

        return parts;
    }

    private static bool IsAndTokenAt(string s, int index)
    {
        if (index + 2 >= s.Length)
            return false;

        char c0 = s[index];
        char c1 = s[index + 1];
        char c2 = s[index + 2];
        if (!((c0 == 'A' || c0 == 'a') && (c1 == 'N' || c1 == 'n') && (c2 == 'D' || c2 == 'd')))
            return false;

        char before = index > 0 ? s[index - 1] : '\0';
        char after = index + 3 < s.Length ? s[index + 3] : '\0';
        bool beforeOk = index == 0 || !IsIdentChar(before);
        bool afterOk = index + 3 >= s.Length || !IsIdentChar(after);
        return beforeOk && afterOk;
    }

    private static bool IsOrTokenAt(string s, int index)
    {
        // Need "OR" case-insensitive and token-bounded
        if (index + 1 >= s.Length)
            return false;

        char c0 = s[index];
        char c1 = s[index + 1];

        if (!((c0 == 'O' || c0 == 'o') && (c1 == 'R' || c1 == 'r')))
            return false;

        // Token boundaries: before and after must not be identifier chars
        char before = index > 0 ? s[index - 1] : '\0';
        char after = index + 2 < s.Length ? s[index + 2] : '\0';

        bool beforeOk = index == 0 || !IsIdentChar(before);
        bool afterOk = (index + 2) >= s.Length || !IsIdentChar(after);

        // Also allow boundaries like ')' '(' whitespace operators etc. (covered by !IsIdentChar)
        return beforeOk && afterOk;
    }

    private static bool IsIdentChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_' || ch == '.';
    }

    /// <summary>
    /// Reads the first <c>smePathN</c> alias from generated match SQL. For <c>[]</c> paths, one logical path can
    /// produce multiple <c>substr</c> lines; <c>N</c> maps to <c>pathConditions[N - 1]</c> (1-based global SME alias).
    /// </summary>
    private static bool TryGetSmePathNumberFromMatchSql(string sql, out int smePathNumber)
    {
        smePathNumber = 0;
        var m = Regex.Match(sql, @"smePath(\d+)");
        if (!m.Success)
            return false;
        return int.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out smePathNumber);
    }

    /// <summary>
    /// True iff every <c>"valuePrefix".</c> table qualifier appears at parenthesis depth 0 (not nested).
    /// Used to gate the combined ValueSets LEFT JOIN optimization; complex predicates fall back to the legacy path.
    /// </summary>
    private static bool ValueSetAliasOnlyAtTopLevel(string sql, string valuePrefix)
    {
        var delimiter = $"\"{valuePrefix}\".";
        var delLen = delimiter.Length;
        var depth = 0;
        var inSingle = false;
        var inDouble = false;
        var inLineComment = false;
        var inBlockComment = false;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            var next = i + 1 < sql.Length ? sql[i + 1] : '\0';

            if (inLineComment)
            {
                if (c == '\n')
                    inLineComment = false;
                continue;
            }
            if (inBlockComment)
            {
                if (c == '*' && next == '/')
                {
                    i++;
                    inBlockComment = false;
                }
                continue;
            }
            if (!inSingle && !inDouble)
            {
                if (c == '-' && next == '-')
                {
                    i++;
                    inLineComment = true;
                    continue;
                }
                if (c == '/' && next == '*')
                {
                    i++;
                    inBlockComment = true;
                    continue;
                }
            }

            // Table alias must be matched as a whole ("alias".) before generic " handling: otherwise the first
            // `"` of `"v"."SValue"` toggles inDouble and the delimiter is never detected (false positives).
            if (!inSingle && !inDouble
                && i + delLen <= sql.Length
                && string.CompareOrdinal(sql, i, delimiter, 0, delLen) == 0)
            {
                if (depth != 0)
                    return false;
                i += delLen - 1;
                continue;
            }

            if (!inDouble && c == '\'')
            {
                if (inSingle)
                {
                    if (next == '\'')
                        i++;
                    else
                        inSingle = false;
                }
                else
                    inSingle = true;
                continue;
            }
            if (!inSingle && c == '"')
            {
                if (inDouble)
                {
                    if (next == '"')
                        i++;
                    else
                        inDouble = false;
                }
                else
                    inDouble = true;
                continue;
            }

            if (!inSingle && !inDouble)
            {
                if (c == '(')
                {
                    depth++;
                    continue;
                }
                if (c == ')')
                {
                    depth = Math.Max(0, depth - 1);
                    continue;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// True when <paramref name="sql"/> references outer-scope join aliases (SM subquery <c>t</c>, SME/path
    /// joins <c>smeN</c>, <c>pN</c>, <c>PathN</c>) that are not in scope inside the standalone ValueSets
    /// LEFT JOIN subquery. Mixed predicates must use the legacy value path.
    /// </summary>
    private static bool ValueSetFastPathHasForeignJoinRefs(string sql)
    {
        if (sql.Contains("\"t\".", StringComparison.Ordinal))
            return true;
        // Unquoted join result aliases emitted by CombineTablesLEFT (before value-block substitution).
        return Regex.IsMatch(sql, @"\bsme\d+\.|\bp\d+\.|\bPath\d+\.", RegexOptions.CultureInvariant);
    }

    private static string TrimLeadingGroupingParensForValueSql(string s)
    {
        var t = s;
        while (true)
        {
            var u = t.TrimStart();
            if (u.Length == 0)
                return t;
            if (u[0] == '(')
            {
                t = u.Substring(1);
                continue;
            }
            return u;
        }
    }

    /// <summary>
    /// Qualifies bare <c>"SValue"</c> / <c>"NValue"</c> / <c>"DTValue"</c> with <c>"alias".</c> only when not already qualified.
    /// </summary>
    private static string ReplaceBareValueFieldTokens(string sql, IReadOnlyList<string> quotedFields, string aliasName)
    {
        var tablePrefix = $"\"{aliasName}\".";
        foreach (var f in quotedFields)
        {
            var replacement = tablePrefix + f;
            var searchStart = 0;
            while (searchStart < sql.Length)
            {
                var idx = sql.IndexOf(f, searchStart, StringComparison.Ordinal);
                if (idx < 0)
                    break;
                if (idx >= tablePrefix.Length
                    && string.CompareOrdinal(sql, idx - tablePrefix.Length, tablePrefix, 0, tablePrefix.Length) == 0)
                {
                    searchStart = idx + f.Length;
                    continue;
                }
                sql = sql.Substring(0, idx) + replacement + sql.Substring(idx + f.Length);
                searchStart = idx + replacement.Length;
            }
        }
        return sql;
    }

    private class joinAll
    {
        public int SMId;
        public int AASId;
        public AASSet? aas;
        public SMSet? sm;
        public SMESet? sme;
        public string? svalue;
        public double? mvalue;
        public DateTime? dtvalue;
        /// <summary>Maps to <see cref="Entities.ValueSet.Annotation"/> for query predicates (e.g. value type).</summary>
        public string? valueAnnotation;
    }

    private static List<int> CombineTablesLEFT(
        AasContext db,
        SqlConditions? sqlConditions,
        int pageFrom,
        int pageSize,
        ResultType resultType,
        List<string> flags,
        List<string>? generatedSql = null
        )
    {
        if (sqlConditions == null)
            throw new InvalidOperationException("CombineTablesLEFT requires SqlConditions.");

        var sqlAasMerged = NormalizeSqlAliases(sqlConditions.FormulaConditions.GetValueOrDefault("aas", ""));
        var sqlOverallCondition = NormalizeSqlAliases(sqlConditions.FormulaConditions.GetValueOrDefault("all", ""));

        var overallFieldCondition = sqlOverallCondition;

        // Do not join AASSets when the AAS scope is only a tautology (e.g. (1=1) from folding) — same idea as "all" without "aas".
        var restrictAAS = !SqlConditionIsPureTautology(sqlAasMerged);
        var aasExistInCondition = !string.IsNullOrWhiteSpace(overallFieldCondition)
            && overallFieldCondition.Contains("\"aas\".");

        bool isWithAASTable = restrictAAS || aasExistInCondition || resultType == ResultType.AssetAdministrationShell;

        var swBuild = Stopwatch.StartNew();
        var rawSql = BuildRawSqlFromSqlConditions(sqlConditions, isWithAASTable, resultType, pageFrom, pageSize, flags);
        swBuild.Stop();
        Console.WriteLine($"[ReadDiag] CombineTablesLEFT.BuildRawSql: {swBuild.ElapsedMilliseconds} ms");
        if (rawSql == null)
            throw new InvalidOperationException("BuildRawSqlFromSqlConditions returned null.");

        // SQL-only debugging: emit SQL + query plan, but never execute the actual query.
        var sqlOnly = flags.Contains("$SQLONLY");

        if (flags.Contains("$TEMPTABLE"))
        {
            AddGeneratedSql(generatedSql, rawSql);

            if (sqlOnly)
            {
                var swPlanOnly = Stopwatch.StartNew();
                AddQueryPlan(generatedSql, GetQueryPlanForScript(db, rawSql));
                swPlanOnly.Stop();
                Console.WriteLine($"[ReadDiag] CombineTablesLEFT.GetQueryPlan (sql-only, temptable): {swPlanOnly.ElapsedMilliseconds} ms");
                return new List<int>();
            }

            using var tx = db.Database.BeginTransaction();

            db.Database.ExecuteSqlRaw(rawSql);

            var tempTableSelectRaw = $@"SELECT Id
                        FROM union_ids
                        ORDER BY 1
                        LIMIT {pageSize} OFFSET {pageFrom}";
            AddGeneratedSql(generatedSql, tempTableSelectRaw);
            var page = db.Set<SMSetIdResult>()
                .FromSqlRaw(@"
                        SELECT Id
                        FROM union_ids
                        ORDER BY 1
                        LIMIT {0} OFFSET {1}", pageSize, pageFrom)
                .AsNoTracking()
                .Select(x => x.Id)
                .ToList();

            tx.Commit();
            return page;
        }


        AddGeneratedSql(generatedSql, rawSql);

        var swPlan = Stopwatch.StartNew();
        var qpRaw = GetQueryPlan(db, rawSql);
        swPlan.Stop();
        Console.WriteLine($"[ReadDiag] CombineTablesLEFT.GetQueryPlan: {swPlan.ElapsedMilliseconds} ms");

        if (sqlOnly)
        {
            AddQueryPlan(generatedSql, qpRaw);
            return new List<int>();
        }

        var swExec = Stopwatch.StartNew();
        // Execute the finished SQL directly via ADO.NET (like the sqlite3 CLI).
        // EF's FromSqlRaw(...).Select(...) wraps rawSql in a subquery, which degrades the
        // plan for DISTINCT+ORDER BY+LIMIT queries (observed ~8x slower than native).
        var ids = new List<int>();
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = rawSql;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                ids.Add(reader.GetInt32(0));
        }
        swExec.Stop();
        Console.WriteLine($"[ReadDiag] CombineTablesLEFT.Execute   : {swExec.ElapsedMilliseconds} ms ({ids.Count} ids)");
        return ids;
    }

    // ------------------------------------------------------------------
    // SQL assembly from SqlConditions (single source of truth for SM list SQL)
    // ------------------------------------------------------------------
    internal static string? BuildRawSqlFromSqlConditions(
        SqlConditions sc,
        bool isWithAASTable,
        ResultType resultType,
        int pageFrom,
        int pageSize,
        IReadOnlyList<string>? queryFlags = null)
    {
        var whereAas = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("aas", ""));
        var whereSm = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("sm", ""));
        var whereSme = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("sme", ""));
        var whereVal = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("value", ""));

        var withUnion = queryFlags != null && queryFlags.Contains("$UNION");
        var withTemp  = queryFlags != null && queryFlags.Contains("$TEMPTABLE");
        var unionOrTemp = withUnion || withTemp;
        // Default: rewrite pure SME filters to correlated EXISTS. Opt out with $LEGACYSMEJOIN (legacy LEFT JOIN + DISTINCT sme_inner).
        var useLegacySmeJoin = queryFlags != null && queryFlags.Contains("$LEGACYSMEJOIN");

        // Resolve placeholder references
        var overall = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("all", ""));
        var pathNum  = 1;
        var matchNum = 1;
        foreach (var path  in sc.Paths)    overall = overall.Replace($"$${path.Placeholder}$$",  $"(p{pathNum++}.SMId IS NOT NULL)");
        foreach (var match in sc.Matches)  overall = overall.Replace($"$${match.Placeholder}$$", $"(m{matchNum++}.SMId IS NOT NULL)");
        foreach (var exists in sc.ExistsConditions)
            overall = overall.Replace($"$${exists.Placeholder}$$", BuildValueMatchSql(exists.PredicateSql, whereSme));

        // ----------------------------------------------------------------
        // Direct paths: no paths/matches, no AAS join, no SM filter, standalone SM list.
        // SME-only: drive from SMESets so OR SME predicates apply inside WHERE (not a scan + outer filter).
        // Value-only: drive from ValueSets (analogous).
        // ----------------------------------------------------------------
        bool hasPathsOrMatches = sc.Paths.Count > 0 || sc.Matches.Count > 0;
        bool overallHasSmeRef  = overall.Contains("\"sme\".");
        bool overallHasVRef    = overall.Contains("\"value\".");
        bool overallHasTRef    = overall.Contains("\"sm\".") || overall.Contains("\"aas\".");

        bool canDirectSme = !hasPathsOrMatches && !isWithAASTable
            && string.IsNullOrWhiteSpace(whereSm)
            && resultType != ResultType.AssetAdministrationShell
            && overallHasSmeRef && !overallHasVRef && !overallHasTRef;

        bool canDirectValue = !hasPathsOrMatches && !isWithAASTable
            && string.IsNullOrWhiteSpace(whereSm)
            && resultType != ResultType.AssetAdministrationShell
            && overallHasVRef && !overallHasTRef;

        if (canDirectSme)
        {
            if (unionOrTemp)
                return BuildDirectUnionOrTempSql(isSmeTable: true, overall, withUnion, withTemp, pageFrom, pageSize);
            var raw = "SELECT DISTINCT sm.Id\r\nFROM SMESets AS sme\r\nJOIN SMSets AS sm ON sm.Id = sme.SMId\r\n";
            raw += $"WHERE {overall}\r\n";
            raw += $"ORDER BY sm.Id\r\nLIMIT {pageSize} OFFSET {pageFrom}\r\n";
            return ApplyLikeToGlob(raw);
        }

        if (canDirectValue)
        {
            if (unionOrTemp)
                return BuildDirectUnionOrTempSql(isSmeTable: false, overall, withUnion, withTemp, pageFrom, pageSize);
            var raw = "SELECT DISTINCT sm.Id\r\nFROM ValueSets AS value\r\nJOIN SMESets AS sme ON sme.Id = value.SMEId\r\nJOIN SMSets AS sm ON sm.Id = sme.SMId\r\n";
            raw += $"WHERE {overall}\r\n";
            raw += $"ORDER BY sm.Id\r\nLIMIT {pageSize} OFFSET {pageFrom}\r\n";
            return ApplyLikeToGlob(raw);
        }

        // ----------------------------------------------------------------
        // Normal path: AAS/SM subqueries
        // ----------------------------------------------------------------
        var selectAas = "(\r\n  SELECT Id, Identifier\r\n  FROM AASSets\r\n";
        if (!string.IsNullOrWhiteSpace(whereAas))
            selectAas += $"  WHERE {whereAas.Replace("\"aas\".", "")}\r\n";
        selectAas += ")";

        var selectSm = "(\r\n  SELECT Id, IdShort, Identifier\r\n  FROM SMSets\r\n";
        if (!string.IsNullOrWhiteSpace(whereSm))
            selectSm += $"  WHERE {whereSm.Replace("\"sm\".", "")}\r\n";
        selectSm += ")";

        var rawBase = "SELECT DISTINCT ";
        rawBase += resultType == ResultType.AssetAdministrationShell ? "aas.Id\r\n" : "sm.Id\r\n";

        if (isWithAASTable)
        {
            rawBase += $"FROM {selectAas} AS aas\r\n";
            rawBase += "INNER JOIN SMRefSets AS sx ON aas.Id = sx.AASId\r\n";
            rawBase += $"INNER JOIN {selectSm} AS sm ON sx.Identifier = sm.Identifier\r\n";
        }
        else
        {
            rawBase += $"FROM {selectSm} AS sm\r\n";
        }

        var pathJoins = new List<(string alias, string sql)>();
        pathNum = 1;
        foreach (var path in sc.Paths)
        {
            var frag = $"LEFT JOIN(\r\nSELECT sme.SMId AS SMId\r\n{path.SubquerySql}\r\n) AS p{pathNum} ON p{pathNum}.SMId = sm.Id\r\n";
            pathJoins.Add(($"p{pathNum}", frag));
            pathNum++;
        }

        var matchJoins = new List<(string alias, string sql)>();
        matchNum = 1;
        foreach (var match in sc.Matches)
        {
            var matchSql = BuildMatchSubquerySql(match);
            var frag = $"LEFT JOIN(\r\n{matchSql}\r\n) AS m{matchNum} ON m{matchNum}.SMId = sm.Id\r\n";
            matchJoins.Add(($"m{matchNum}", frag));
            matchNum++;
        }

        string? smeJoin = null;
        if (overallHasSmeRef)
        {
            if (!useLegacySmeJoin && TryRewriteOverallWithSmeExists(overall, whereSme, out var overallSmeExists))
                overall = overallSmeExists;
            else
            {
                var smeFields = GetSqlFields("sme", overall);
                var smeWhere = whereSme.Replace("\"sme\".", "\"sme_inner\".");

                var sbSme = new StringBuilder();
                sbSme.Append("LEFT JOIN(\r\n  SELECT DISTINCT\r\n    sme_inner.SMId AS \"SMId\"");
                foreach (var field in smeFields)
                    sbSme.Append($",\r\n    sme_inner.\"{field}\" AS \"{field}\"");
                sbSme.Append("\r\n  FROM SMESets sme_inner\r\n");
                if (!string.IsNullOrWhiteSpace(smeWhere))
                    sbSme.Append($"  WHERE {smeWhere}\r\n");
                sbSme.Append(") AS sme ON sme.\"SMId\" = sm.Id\r\n");
                smeJoin = sbSme.ToString();
            }
        }

        string? valueJoin = null;
        if (overallHasVRef)
        {
            var valFields = new List<string>();
            foreach (var col in new[] { "SValue", "NValue", "DTValue", "Annotation" })
                if (overall.Contains($"\"value\".\"{col}\"")) valFields.Add(col);
            var valWhere  = string.IsNullOrWhiteSpace(whereVal) ? "1=1" : whereVal.Replace("\"value\".", "v.");
            // Apply the element-visibility FILTER inside the value join (alias sme = SMESets here), so a
            // value in a hidden element cannot satisfy the query — same correctness fix as the value EXISTS.
            if (!string.IsNullOrWhiteSpace(whereSme))
                valWhere = $"({valWhere}) AND ({whereSme})";

            var sbVal = new StringBuilder();
            sbVal.Append("LEFT JOIN(\r\n  SELECT DISTINCT\r\n    sme.SMId AS \"SMId\"");
            foreach (var f in valFields) sbVal.Append($",\r\n    v.\"{f}\" AS \"{f}\"");
            sbVal.Append("\r\n  FROM ValueSets v\r\n  JOIN SMESets sme ON sme.Id = v.SMEId\r\n");
            sbVal.Append($"  WHERE {valWhere}\r\n) AS value ON value.\"SMId\" = sm.Id\r\n");
            valueJoin = sbVal.ToString();

            foreach (var col in valFields)
                overall = overall.Replace($"\"v\".\"{col}\"", $"\"value\".\"{col}\"");
        }

        if (unionOrTemp)
        {
            var orParts = BuildUnionOrParts(sc, overall, whereSme, smeJoin, valueJoin);
            return BuildJoinedUnionOrTempSql(
                rawBase,
                pathJoins,
                matchJoins,
                smeJoin,
                valueJoin,
                overallHasSmeRef,
                overallHasVRef,
                orParts,
                withUnion,
                withTemp,
                pageFrom,
                pageSize);
        }

        foreach (var j in pathJoins)
            rawBase += j.sql;
        foreach (var j in matchJoins)
            rawBase += j.sql;
        if (smeJoin != null)
            rawBase += smeJoin;
        if (valueJoin != null)
            rawBase += valueJoin;

        if (!string.IsNullOrWhiteSpace(overall) && overall != "1=1")
            rawBase += $"WHERE {overall}\r\n";

        rawBase += $"ORDER BY sm.Id\r\nLIMIT {pageSize} OFFSET {pageFrom}\r\n";
        return ApplyLikeToGlob(rawBase);
    }

    private static string ApplyLikeToGlob(string sql)
    {
        if (!sql.Contains(" LIKE ", StringComparison.Ordinal))
            return sql;
        sql = sql.Replace(" LIKE ", " GLOB ", StringComparison.Ordinal);
        return sql.Replace("%", "*");
    }

    // Realizes a direct $sme#value condition. The element-visibility FILTER (smeFilter, e.g. the
    // access-rule "$sme#idShort starts-with General/Manufacturer") is applied INSIDE the value match,
    // so a value sitting in a hidden element cannot satisfy the query (otherwise id-only queries would
    // leak the existence of hidden-element values). Selective predicates (=, range, GLOB-prefix) become
    // a value-index-driven, non-correlated subquery (sm.Id IN (...)); non-selective ones (<>, negation,
    // OR of predicates) stay a correlated EXISTS that short-circuits per submodel.
    // NOTE: interim operator heuristic — to be replaced by a structure-based decision in the planned
    // condition-IR (where selectivity could come from ANALYZE statistics).
    private static string BuildValueMatchSql(string predicateSql, string? smeFilter)
    {
        var filter = string.IsNullOrWhiteSpace(smeFilter)
            ? string.Empty
            : $"\r\n    AND ({smeFilter!.Replace("\"sme\".", "sme_value.", StringComparison.Ordinal)})";

        if (PredicatePrefersJoin(predicateSql))
        {
            return $@"sm.Id IN (
  SELECT sme_value.SMId
  FROM ValueSets v
  JOIN SMESets sme_value ON sme_value.Id = v.SMEId
  WHERE ({predicateSql}){filter}
)";
        }

        return $@"EXISTS (
  SELECT 1
  FROM ValueSets v
  JOIN SMESets sme_value ON sme_value.Id = v.SMEId
  WHERE sme_value.SMId = sm.Id
    AND ({predicateSql}){filter}
)";
    }

    // Operator heuristic for BuildValueMatchSql: a value predicate is "join-worthy" when it is an
    // index-usable positive comparison (=, <, >, <=, >=, or a GLOB prefix without leading wildcard)
    // and carries no negation or top-level OR (one non-selective disjunct would force a scan).
    private static bool PredicatePrefersJoin(string predicateSql)
    {
        if (string.IsNullOrWhiteSpace(predicateSql))
            return false;
        if (predicateSql.Contains("<>", StringComparison.Ordinal) ||
            predicateSql.Contains("!=", StringComparison.Ordinal) ||
            Regex.IsMatch(predicateSql, @"\bNOT\b", RegexOptions.IgnoreCase))
            return false;
        if (Regex.IsMatch(predicateSql, @"\bOR\b", RegexOptions.IgnoreCase))
            return false;
        if (Regex.IsMatch(predicateSql, @"[=<>]", RegexOptions.CultureInvariant))
            return true;
        if (Regex.IsMatch(predicateSql, @"GLOB\s+'[^*]"))
            return true;
        return false;
    }

    /// <summary>
    /// True if this top-level AND conjunct references <c>"sme".</c> but no other table aliases that would
    /// require staying in the outer WHERE (sm/aas/value/path/match). Used to gate SME EXISTS rewrite (unless <c>$LEGACYSMEJOIN</c>).
    /// </summary>
    private static bool SmeExistsConjunctIsPure(string conjunct)
    {
        if (!conjunct.Contains("\"sme\".", StringComparison.Ordinal))
            return false;
        if (conjunct.Contains("\"sm\".", StringComparison.Ordinal)) return false;
        if (conjunct.Contains("\"aas\".", StringComparison.Ordinal)) return false;
        if (conjunct.Contains("\"value\".", StringComparison.Ordinal)) return false;
        if (Regex.IsMatch(conjunct, @"\bp\d+\.SMId\b")) return false;
        if (Regex.IsMatch(conjunct, @"\bm\d+\.SMId\b")) return false;
        return true;
    }

    /// <summary>
    /// Normalizes SME EXISTS inner fragments so duplicate predicates (Filter <c>whereSme</c> vs. merged
    /// <c>"sme".</c> conjuncts, or <c>e.</c> vs. unqualified columns inside <c>FROM SMESets e</c>) collapse to one key.
    /// </summary>
    private static string NormalizeSmeExistsChunkForDedup(string sql)
    {
        var s = sql.Trim();
        s = s.Replace("\"e\".", "", StringComparison.Ordinal);
        s = s.Replace("\"sme\".", "", StringComparison.Ordinal);
        return Regex.Replace(s, @"\s+", " ", RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// Moves pure top-level AND conjuncts that reference <c>"sme"</c> into a correlated
    /// <c>EXISTS (SELECT 1 FROM SMESets e WHERE e.SMId = sm.Id AND …)</c> and merges per-scope
    /// <paramref name="whereSme"/> (formula+filter) into the EXISTS body. Returns false if any
    /// <c>"sme"</c> conjunct also references other aliases (caller must use the DISTINCT LEFT JOIN path).
    /// </summary>
    private static bool TryRewriteOverallWithSmeExists(string overall, string whereSme, out string rewrittenOverall)
    {
        rewrittenOverall = overall;
        if (string.IsNullOrWhiteSpace(overall) || overall == "1=1")
            return false;
        if (!overall.Contains("\"sme\".", StringComparison.Ordinal))
            return false;

        var parts = SplitTopLevelAnd(overall);
        if (parts.Count == 0)
            return false;

        var smeParts = new List<string>();
        var restParts = new List<string>();

        foreach (var p in parts)
        {
            var trimmed = p.Trim();
            if (trimmed.Length == 0)
                continue;

            if (!trimmed.Contains("\"sme\".", StringComparison.Ordinal))
            {
                restParts.Add(trimmed);
                continue;
            }

            if (!SmeExistsConjunctIsPure(trimmed))
                return false;

            smeParts.Add(trimmed);
        }

        if (smeParts.Count == 0)
            return false;

        var smeWhereE = string.IsNullOrWhiteSpace(whereSme) ? "" : whereSme.Replace("\"sme\".", "\"e\".", StringComparison.Ordinal);

        var innerChunks = new List<string>();
        var innerSeen = new HashSet<string>(StringComparer.Ordinal);
        void AddInnerChunk(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            var t = raw.Trim();
            var key = NormalizeSmeExistsChunkForDedup(t);
            if (key.Length == 0 || !innerSeen.Add(key)) return;
            innerChunks.Add(t);
        }

        AddInnerChunk(smeWhereE);
        foreach (var sp in smeParts)
            AddInnerChunk(sp.Replace("\"sme\".", "\"e\".", StringComparison.Ordinal));

        if (innerChunks.Count == 0)
            return false;

        string? innerCombined = null;
        foreach (var ch in innerChunks)
            innerCombined = AppendAndCondition(innerCombined, ch);

        var existsClause =
            "EXISTS (\r\n" +
            "  SELECT 1\r\n" +
            "  FROM SMESets e\r\n" +
            "  WHERE e.SMId = sm.Id\r\n" +
            $"    AND ({innerCombined})\r\n" +
            ")";

        string? restChain = null;
        foreach (var rp in restParts)
            restChain = AppendAndCondition(restChain, rp);

        rewrittenOverall = AppendAndCondition(restChain, existsClause);
        return true;
    }

    private static string BuildDirectUnionOrTempSql(
        bool isSmeTable,
        string overall,
        bool withUnion,
        bool withTemp,
        int pageFrom,
        int pageSize)
    {
        var orParts = SplitTopLevelOr(overall);
        if (orParts.Count == 0)
            orParts = new List<string> { "1=1" };
        var fromSql = isSmeTable
            ? "SELECT DISTINCT sm.Id\r\nFROM SMESets AS sme\r\nJOIN SMSets AS sm ON sm.Id = sme.SMId\r\n"
            : "SELECT DISTINCT sm.Id\r\nFROM ValueSets AS value\r\nJOIN SMESets AS sme ON sme.Id = value.SMEId\r\nJOIN SMSets AS sm ON sm.Id = sme.SMId\r\n";

        if (withTemp)
        {
            var sb = new StringBuilder();
            sb.Append("DROP TABLE IF EXISTS union_ids;\r\n");
            sb.Append("CREATE TEMP TABLE union_ids (\r\n");
            sb.Append("Id INTEGER PRIMARY KEY\r\n");
            sb.Append(") WITHOUT ROWID;\r\n\r\n");
            foreach (var part in orParts)
            {
                var q = fromSql + $"WHERE {part}\r\nORDER BY 1\r\n";
                q = ApplyLikeToGlob(q);
                sb.Append("INSERT OR IGNORE INTO union_ids(Id)\r\n").Append(q).Append(";\r\n\r\n");
            }
            return sb.ToString();
        }

        if (withUnion)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < orParts.Count; i++)
            {
                var q = fromSql + $"WHERE {orParts[i]}\r\n";
                q = ApplyLikeToGlob(q);
                sb.Append(q);
                if (i < orParts.Count - 1)
                    sb.Append("\r\nUNION\r\n\r\n");
            }
            return "SELECT DISTINCT Id\r\nFROM(\r\n\r\n" + sb + "\r\n)\r\nORDER BY Id\r\n"
                + $"LIMIT {pageSize} OFFSET {pageFrom}\r\n";
        }

        throw new InvalidOperationException("BuildDirectUnionOrTempSql requires $UNION or $TEMPTABLE.");
    }

    // Decide the OR-branches for the UNION/TEMPTABLE builder.
    //
    // When security is merged in, FormulaConditions["all"] is "(userOR) AND (securityACL)" whose
    // top-level operator is AND — SplitTopLevelOr can't decompose it, so $UNION/$TEMPTABLE would
    // degenerate to a single branch. Instead we split the *user* OR alone and AND the security ACL
    // onto each branch (distribution: (A OR B OR C) AND S -> (A AND S), (B AND S), (C AND S)), so each
    // branch can be driven by its own index. Falls back to the old combined-split whenever distribution
    // isn't applicable or would reference a join that isn't built.
    private static List<string> BuildUnionOrParts(SqlConditions sc, string combinedOverall, string? smeFilter, string? smeJoin, string? valueJoin)
    {
        List<string> NonEmpty(List<string> parts) => parts.Count == 0 ? new List<string> { "1=1" } : parts;

        List<string> Fallback() =>
            NonEmpty(string.IsNullOrWhiteSpace(combinedOverall) || combinedOverall == "1=1"
                ? new List<string> { "1=1" }
                : SplitTopLevelOr(combinedOverall));

        var qRaw = sc.QueryOverallRaw;
        var secRaw = sc.SecurityOverallRaw;
        if (string.IsNullOrWhiteSpace(qRaw) || string.IsNullOrWhiteSpace(secRaw))
            return Fallback();

        string Resolve(string raw)
        {
            var s = NormalizeSqlAliases(raw ?? "");
            int pN = 1, mN = 1;
            foreach (var path in sc.Paths) s = s.Replace($"$${path.Placeholder}$$", $"(p{pN++}.SMId IS NOT NULL)");
            foreach (var match in sc.Matches) s = s.Replace($"$${match.Placeholder}$$", $"(m{mN++}.SMId IS NOT NULL)");
            foreach (var exists in sc.ExistsConditions) s = s.Replace($"$${exists.Placeholder}$$", BuildValueMatchSql(exists.PredicateSql, smeFilter));
            return s;
        }

        var resolvedSecurity = Resolve(secRaw!);
        var userBranches = SplitTopLevelOr(StripEnclosingParens(Resolve(qRaw!)));
        if (userBranches.Count <= 1)
            return Fallback(); // no user OR to distribute -> nothing gained

        var distributed = userBranches
            .Select(b => b.Trim() == "1=1" ? $"({resolvedSecurity})" : $"({b}) AND ({resolvedSecurity})")
            .ToList();

        // Safety: a branch may only reference the sme/value materialized joins if they were actually built;
        // otherwise the emitted SQL would name an undefined alias (e.g. after the sme-EXISTS rewrite).
        bool joinsConsistent = distributed.All(b =>
            (smeJoin != null || !b.Contains("\"sme\".", StringComparison.Ordinal)) &&
            (valueJoin != null || !b.Contains("\"value\".", StringComparison.Ordinal)));

        return joinsConsistent ? distributed : Fallback();
    }

    // Removes redundant outer parentheses that wrap the whole expression, so SplitTopLevelOr can see
    // a top-level OR in "(A OR B OR C)". Leaves "(A) OR (B)" untouched (not a single enclosing pair).
    private static string StripEnclosingParens(string s)
    {
        s = s.Trim();
        while (s.Length >= 2 && s[0] == '(' && s[^1] == ')')
        {
            int depth = 0;
            bool inSingle = false, inDouble = false, wrapsWhole = true;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!inDouble && c == '\'')
                {
                    if (inSingle && i + 1 < s.Length && s[i + 1] == '\'') i++;
                    else inSingle = !inSingle;
                    continue;
                }
                if (!inSingle && c == '"')
                {
                    if (inDouble && i + 1 < s.Length && s[i + 1] == '"') i++;
                    else inDouble = !inDouble;
                    continue;
                }
                if (inSingle || inDouble) continue;
                if (c == '(') depth++;
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0 && i != s.Length - 1) { wrapsWhole = false; break; }
                }
            }
            if (!wrapsWhole) break;
            s = s.Substring(1, s.Length - 2).Trim();
        }
        return s;
    }

    private static string BuildJoinedUnionOrTempSql(
        string selectFromPrefix,
        List<(string alias, string sql)> pathJoins,
        List<(string alias, string sql)> matchJoins,
        string? smeJoin,
        string? valueJoin,
        bool overallHasSmeRef,
        bool overallHasVRef,
        List<string> orParts,
        bool withUnion,
        bool withTemp,
        int pageFrom,
        int pageSize)
    {
        string OneBranch(string wherePart)
        {
            var b = selectFromPrefix;
            foreach (var j in pathJoins)
            {
                if (wherePart.Contains($"({j.alias}.SMId IS NOT NULL)", StringComparison.Ordinal))
                    b += j.sql;
            }
            foreach (var j in matchJoins)
            {
                if (wherePart.Contains($"({j.alias}.SMId IS NOT NULL)", StringComparison.Ordinal))
                    b += j.sql;
            }
            if (smeJoin != null && overallHasSmeRef && wherePart.Contains("\"sme\".", StringComparison.Ordinal))
                b += smeJoin;
            if (valueJoin != null && overallHasVRef && wherePart.Contains("\"value\".", StringComparison.Ordinal))
                b += valueJoin;
            if (!string.IsNullOrWhiteSpace(wherePart) && wherePart != "1=1")
                b += $"WHERE {wherePart}\r\n";
            return b;
        }

        if (withTemp)
        {
            var sb = new StringBuilder();
            sb.Append("DROP TABLE IF EXISTS union_ids;\r\n");
            sb.Append("CREATE TEMP TABLE union_ids (\r\n");
            sb.Append("Id INTEGER PRIMARY KEY\r\n");
            sb.Append(") WITHOUT ROWID;\r\n\r\n");
            foreach (var part in orParts)
            {
                var branch = OneBranch(part);
                branch = ApplyLikeToGlob(branch);
                sb.Append("INSERT OR IGNORE INTO union_ids(Id)\r\n")
                    .Append(branch)
                    .Append("ORDER BY 1\r\n;\r\n\r\n");
            }
            return sb.ToString();
        }

        if (withUnion)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < orParts.Count; i++)
            {
                var branch = OneBranch(orParts[i]);
                branch = ApplyLikeToGlob(branch);
                sb.Append(branch);
                if (i < orParts.Count - 1)
                    sb.Append("\r\nUNION\r\n\r\n");
            }
            return "SELECT DISTINCT Id\r\nFROM(\r\n\r\n" + sb + "\r\n)\r\nORDER BY Id\r\n"
                + $"LIMIT {pageSize} OFFSET {pageFrom}\r\n";
        }

        throw new InvalidOperationException("BuildJoinedUnionOrTempSql requires $UNION or $TEMPTABLE.");
    }

    private static string BuildMatchSubquerySql(MatchJoin match)
    {
        // Each PathJoin.SubquerySql is body-only (FROM SMESets sme … WHERE …) with alias "sme".
        var isArrayMatch   = match.Paths.Any(p => p.IdShortPath.Contains("[]"));
        var isPercentMatch = !isArrayMatch && match.Paths.Any(p => p.IdShortPath.Contains('%'));
        var substrParts    = new List<string>(); // [] mode: reference Part column names from Path1

        // % mode: compute shared segments from shortest path (up to last dot)
        List<string>? percentSegments = null;
        if (isPercentMatch)
        {
            var shortestPath = match.Paths.Select(p => p.IdShortPath).OrderBy(p => p.Length).First();
            var lastDot = shortestPath.LastIndexOf('.');
            if (lastDot >= 0) shortestPath = shortestPath.Substring(0, lastDot + 1);
            percentSegments = shortestPath.Split('%').ToList();
        }

        var sb = new StringBuilder();
        sb.AppendLine("SELECT Path1.SMId AS SMId");

        for (int k = 0; k < match.Paths.Count; k++)
        {
            var alias       = $"smePath{k + 1}";
            var idShortPath = match.Paths[k].IdShortPath;

            var body = match.Paths[k].SubquerySql
                .Replace("FROM SMESets sme\r\n", $"FROM SMESets {alias}\r\n")
                .Replace("FROM SMESets sme\n",   $"FROM SMESets {alias}\n")
                .Replace("\"sme\".", $"\"{alias}\".")
                .Replace("sme.Id",   $"{alias}.Id")
                .Replace("sme.SMId", $"{alias}.SMId");

            sb.AppendLine(k == 0 ? "FROM (" : "JOIN (");
            sb.Append($"SELECT {alias}.SMId AS SMId");

            if (isArrayMatch && idShortPath.Contains("[]"))
            {
                // [] paths: add substr() columns for each [] segment
                var arraySections = idShortPath.Split("[]");
                for (int s = 0; s < arraySections.Length - 1; s++)
                {
                    var start   = arraySections[s];
                    var end     = arraySections[s + 1];
                    var partCol = $"Part1_{s + 1}_{k + 1}";
                    sb.Append($",\r\nsubstr({alias}.IdShortPath, instr({alias}.IdShortPath, '{start}') + length('{start}'), instr({alias}.IdShortPath, '{end}') - (instr({alias}.IdShortPath, '{start}') + length('{start}'))) AS {partCol}");
                    if (k == 0) substrParts.Add(partCol);
                }
            }
            else if (isPercentMatch && percentSegments != null)
            {
                // % paths: add substr() columns for each % segment (Part1_{path+1}_{seg+1})
                for (int s = 0; s < percentSegments.Count - 1; s++)
                {
                    var start   = percentSegments[s];
                    var end     = percentSegments[s + 1];
                    var partCol = $"Part1_{k + 1}_{s + 1}";
                    if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end))
                        sb.Append($",\r\n{alias}.IdShortPath AS {partCol}");
                    else
                        sb.Append($",\r\nsubstr({alias}.IdShortPath, instr({alias}.IdShortPath, '{start}') + length('{start}'), instr({alias}.IdShortPath, '{end}') - (instr({alias}.IdShortPath, '{start}') + length('{start}'))) AS {partCol}");
                }
            }

            sb.AppendLine();
            sb.AppendLine(body);
            sb.AppendLine($") AS Path{k + 1}");
        }

        // Build WHERE: SMId equality (loop covers all pairs — JoinConditionSql is NOT added, it would duplicate)
        var whereParts = new List<string>();
        for (int k = 1; k < match.Paths.Count; k++)
            whereParts.Add($"Path1.SMId = Path{k + 1}.SMId");

        if (isArrayMatch && substrParts.Count > 0 && match.Paths.Count > 1)
        {
            // [] mode: Part1_g_1 = Part1_g_{k+1} for k > 0
            for (int k = 1; k < match.Paths.Count; k++)
            {
                var idShortPath2 = match.Paths[k].IdShortPath;
                if (!idShortPath2.Contains("[]")) continue;
                var arraySections2 = idShortPath2.Split("[]");
                for (int s = 0; s < arraySections2.Length - 1 && s < substrParts.Count; s++)
                {
                    var refCol = substrParts[s];
                    var cmpCol = $"Part1_{s + 1}_{k + 1}";
                    whereParts.Add($"{refCol} = {cmpCol}");
                }
            }
        }
        else if (isPercentMatch && percentSegments != null && match.Paths.Count > 1)
        {
            // % mode: Part1_{p+1}_{s+1} = Part1_{p+2}_{s+1} for each adjacent pair of paths
            for (int p = 0; p < match.Paths.Count - 1; p++)
                for (int s = 0; s < percentSegments.Count - 1; s++)
                    whereParts.Add($"Part1_{p + 1}_{s + 1} = Part1_{p + 2}_{s + 1}");
        }

        if (whereParts.Count > 0)
            sb.AppendLine($"WHERE {string.Join(" AND ", whereParts.Select(part => $"({part})"))}");

        return sb.ToString().TrimEnd();
    }

    private static string NormalizeSqlForComparison(string sql)
    {
        // Collapse whitespace for loose comparison
        return System.Text.RegularExpressions.Regex.Replace(sql.Trim(), @"\s+", " ");
    }

    private static void AddDistinct(List<string> list, string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !list.Contains(value))
        {
            list.Add(value);
        }
    }

    /// <summary>
    /// True if the fragment is empty or only <c>1=1</c>, optionally wrapped in balanced parentheses (e.g. <c>(1=1)</c>, <c>((1=1))</c>).
    /// </summary>
    private static bool SqlConditionIsPureTautology(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return true;
        var t = sql.Trim();
        while (true)
        {
            if (t == "1=1")
                return true;
            if (t.Length >= 2 && t[0] == '(' && t[^1] == ')')
            {
                if (!IsSingleBalancedParenthesisWrap(t))
                    return false;
                t = t.Substring(1, t.Length - 2).Trim();
                continue;
            }

            return false;
        }
    }

    /// <summary>Returns true if the string is <c>( ... )</c> and the first '(' matches the last ')' (whole expression wrapped).</summary>
    private static bool IsSingleBalancedParenthesisWrap(string t)
    {
        if (t.Length < 2 || t[0] != '(')
            return false;
        var depth = 0;
        for (var i = 0; i < t.Length; i++)
        {
            if (t[i] == '(') depth++;
            else if (t[i] == ')')
            {
                depth--;
                if (depth == 0)
                    return i == t.Length - 1;
            }
        }

        return false;
    }

    private static string AppendAndCondition(string? left, string? right)
    {
        var normalizedLeft = string.IsNullOrWhiteSpace(left) || SqlConditionIsPureTautology(left) ? "" : left.Trim();
        var normalizedRight = string.IsNullOrWhiteSpace(right) || SqlConditionIsPureTautology(right) ? "" : right.Trim();

        if (string.IsNullOrWhiteSpace(normalizedLeft))
        {
            return normalizedRight;
        }

        if (string.IsNullOrWhiteSpace(normalizedRight))
        {
            return normalizedLeft;
        }

        return $"({normalizedLeft}) AND ({normalizedRight})";
    }

    private static void AddGeneratedSql(List<string>? generatedSql, string? raw)
    {
        if (generatedSql == null || string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        generatedSql.Add(raw);
    }

    private static List<string>? SplitExpression(string? fieldExpression)
    {
        if (fieldExpression != null)
        {
            var splitExpression = fieldExpression.Split(" ").ToList();

            if (splitExpression.Count < 3)
            {
                return [];
            }

            if (splitExpression[1].StartsWith('\''))
            {
                fieldExpression = fieldExpression.Substring(splitExpression[0].Length + 1);

                var end = 1;
                while (fieldExpression[end] != '\'')
                {
                    end++;
                }

                splitExpression[1] = fieldExpression.Substring(0, end + 1);

                return splitExpression;
            }

            if (splitExpression[2].StartsWith('\''))
            {
                fieldExpression = fieldExpression.Substring(splitExpression[0].Length + splitExpression[1].Length + 2);

                var end = 1;
                while (end < fieldExpression.Length)
                {
                    while (fieldExpression[end] != '\'')
                    {
                        end++;
                    }
                    var concatString = "' || '";
                    if (end < fieldExpression.Length - concatString.Length)
                    {
                        if (fieldExpression.Substring(end, concatString.Length) == concatString)
                        {
                            end += concatString.Length;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                splitExpression[2] = fieldExpression.Substring(0, end + 1);
            }

            return splitExpression;
        }

        return [];
    }

    private bool ConditionFromExpression(List<string> messages, string expression, out SqlConditions? sqlConditionsOut)
    {
        sqlConditionsOut = null;
        var text = string.Empty;

        // no expression
        if (expression.IsNullOrEmpty())
        {
            return false;
        }

        if (expression == "$all")
        {
            sqlConditionsOut = new SqlConditions();
            sqlConditionsOut.FormulaConditions["all"] = "1=1";
            SqlConditions.RefreshFormulaConditionsCSharpFromFormulaSql(sqlConditionsOut);
            return true;
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

                                List<LogicalExpression?> logicalExpressions = [];
                                if (deserializedData.Query != null && deserializedData.Query.Condition != null)
                                {
                                    logicalExpressions.Add(deserializedData.Query.Condition);
                                    if (deserializedData.Query.Filter != null)
                                    {
                                        logicalExpressions.Add(deserializedData.Query.Filter);
                                    }
                                }

                                if (logicalExpressions.Count != 0)
                                {
                                    sqlConditionsOut = QueryGrammarJSON.CreateSqlConditions(logicalExpressions[0]!);
                                    if (logicalExpressions.Count > 1 && logicalExpressions[1] != null)
                                    {
                                        var filterSql = QueryGrammarJSON.CreateSqlConditions(logicalExpressions[1]!);
                                        sqlConditionsOut = SqlConditionsMerger.Merge(sqlConditionsOut, filterSql);
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
                        return false;
                    }
                }
                else
                {
                    messages.Add("jsonschema-query.txt not found.");
                    return false;
                }

                var query = deserializedData.Query;
                if (deserializedData == null || query == null)
                {
                    return false;
                }

                if (query.Select != null && sqlConditionsOut != null)
                {
                    sqlConditionsOut.Select = query.Select;
                }

                var overallCondition = sqlConditionsOut?.FormulaConditions.GetValueOrDefault("all", "") ?? "";
                if (sqlConditionsOut != null && sqlConditionsOut.Paths.Count != 0)
                {
                    messages.Add("PATH SEARCH");
                }
                else
                {
                    messages.Add("RECURSIVE SEARCH");
                }

                messages.Add("");

                text = "combinedCondition: " + overallCondition;
                Console.WriteLine(text);
                messages.Add(text);

            }

            messages.Add("");
        }
        else
        {
            return false;
        }

        return sqlConditionsOut != null;
    }

    /// <summary>
    /// Returns the predicate after the last "WHERE " in an EF-generated SQL string.
    /// If the query has no WHERE clause, splitting by "WHERE " would yield the full SELECT;
    /// in that case returns empty so callers do not append a full SELECT as a WHERE predicate.
    /// </summary>
    /// <summary>
    /// Translates a parsed path condition (idShortPath, LINQ field name, LINQ op fragment) to SQL parts
    /// without going through EF Core. Returns <c>null</c> for unsupported fields (vtvalue, langvalue, sme.*).
    /// </summary>
    private static (string IdShort, string IdShortPath, string Value)? TryBuildPathCondSql(
        string idShortPath, string field, string linqOp)
    {
        var valueSql = TryBuildFieldOpSql(field, linqOp);
        if (valueSql == null)
            return null;

        var idShort = idShortPath.Split(".").Last();
        return (
            IdShort: $"\"IdShort\" = '{EscapeSqlLiteral(idShort)}'",
            IdShortPath: $"\"IdShortPath\" = '{EscapeSqlLiteral(idShortPath)}'",
            Value: valueSql
        );
    }

    private static string? TryBuildFieldOpSql(string field, string linqOp)
    {
        var sqlCol = field switch
        {
            "svalue"  => "\"v\".\"SValue\"",
            "mvalue"  => "\"v\".\"NValue\"",
            "dtvalue" => "\"v\".\"DTValue\"",
            _ => null   // vtvalue, langvalue, sme.* → caller falls back to EF Core
        };
        if (sqlCol == null)
            return null;

        return LinqOpToSqlCondition(sqlCol, linqOp);
    }

    /// <summary>Translates a LINQ op fragment (e.g. <c> == "ZVEI"</c>, <c>.StartsWith("X")</c>) to a SQL predicate.</summary>
    private static string? LinqOpToSqlCondition(string sqlCol, string linqOp)
    {
        if (linqOp == " == null") return $"{sqlCol} IS NULL";
        if (linqOp == " != null") return $"{sqlCol} IS NOT NULL";

        if (TryParseLinqMethodCall(linqOp, out var method, out var argInner))
        {
            var esc = EscapeSqlLiteral(argInner);
            return method switch
            {
                "StartsWith" => $"{sqlCol} LIKE '{esc}%'",
                "EndsWith"   => $"{sqlCol} LIKE '%{esc}'",
                "Contains"   => $"{sqlCol} LIKE '%{esc}%'",
                _ => null
            };
        }

        // Check longest prefixes first to avoid ambiguity (>= before >)
        (string Pfx, string SqlOp)[] binaryOps =
        [
            (" == ", "="), (" != ", "<>"),
            (" >= ", ">="), (" <= ", "<="),
            (" > ",  ">"),  (" < ",  "<"),
        ];
        foreach (var (pfx, sqlOp) in binaryOps)
        {
            if (!linqOp.StartsWith(pfx)) continue;
            var rhs = linqOp[pfx.Length..];
            string sqlRhs;
            if (rhs.StartsWith("\"") && rhs.EndsWith("\""))
                sqlRhs = "'" + EscapeSqlLiteral(UnescapeLinqString(rhs[1..^1])) + "'";
            else
                sqlRhs = rhs;   // numeric literal
            return $"{sqlCol} {sqlOp} {sqlRhs}";
        }

        return null;
    }

    private static bool TryParseLinqMethodCall(string op, out string method, out string argInner)
    {
        method = "";
        argInner = "";
        if (!op.StartsWith(".") || !op.EndsWith(")")) return false;
        var paren = op.IndexOf('(');
        if (paren < 0) return false;
        method = op[1..paren];
        var inner = op[(paren + 1)..^1];
        if (inner.StartsWith("\"") && inner.EndsWith("\""))
        {
            argInner = UnescapeLinqString(inner[1..^1]);
            return true;
        }
        return false;
    }

    private static string UnescapeLinqString(string s) =>
        s.Replace("\\\"", "\"").Replace("\\\\", "\\");

    private static string EscapeSqlLiteral(string s) => s.Replace("'", "''");

}
