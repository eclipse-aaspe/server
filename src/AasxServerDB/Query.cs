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
using AasCore.Aas3_0;
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
    /// <summary>Serialized <see cref="AasCore.Aas3_0.DataTypeDefXsd"/> (property) or language (MLP), same as DB <c>ValueSet.Annotation</c>.</summary>
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
        SqlConditions? securitySqlConditions = null)
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

        List<string>? generatedSql = includeDebugSql ? new List<string>() : null;
        var result = GetSMs(out var effectiveSqlConditions, resultType,
            qResult, watch, db, false, false, "", "", "", pageFrom, pageSize, expression, generatedSql, securitySqlConditions);
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
                            var filterAas = effectiveSqlConditions?.FilterConditions.GetValueOrDefault("aas", "");
                            if (!string.IsNullOrWhiteSpace(filterAas) && filterAas != "$SKIP")
                            {
                                if (filterAas.Contains("globalAssetId"))
                                {
                                    aas.Submodels = null;
                                    aas.Description = null;
                                    aas.DisplayName = null;
                                }
                            }
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
                        ReadSubmodel(db, smDB: submodelDB, securitySqlConditions: securitySqlConditions)))
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

        List<string> possibleFlags = ["$LEFTJOIN", "$UNION", "$TEMPTABLE"];
        List<string> flags = [];
        foreach (var flag in possibleFlags)
        {
            if (expression.Contains(flag))
            {
                flags.Add(flag);
                expression = expression.Replace(flag, string.Empty);
            }
        }

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
            /*
            if (!paramSNotEmpty.Contains("."))
            {
                paramSNotEmpty = "'" + paramSNotEmpty + "'";
            }
            */
            i++;
            // result = result + $"{param[0]} LIKE \'%{paramS[1]}%\' {splitParam[i]}";
            result += $"{param[0]} LIKE '%' || '{paramSNotEmpty}' || '%' {splitParam[i]}";
        }
        return result;
    }

    /// <summary>
    /// EF Core SQLite translates <see cref="string.Contains"/> on joined <c>ValueSet.Annotation</c> to
    /// <c>instr("v"."Annotation", 'needle') &gt; 0</c>. <see cref="CombineTablesLEFT"/> post-processes SQL using
    /// naive space splits that corrupt <c>instr(...)</c> with quoted identifiers. Replacing those fragments with
    /// equivalent <c>LIKE</c> keeps semantics and avoids broken generated subqueries.
    /// </summary>
    private static string NormalizeValueAnnotationInstrForCombineSql(string? sql)
    {
        if (string.IsNullOrEmpty(sql) || !sql.Contains("instr(\"", StringComparison.Ordinal))
            return sql ?? "";

        // Contains → instr(..., 'x') > 0
        sql = Regex.Replace(
            sql,
            @"instr\(""([^""]+)""\.""Annotation"",\s*'([^']*)'\)\s*>\s*0",
            m =>
            {
                var alias = m.Groups[1].Value;
                var literal = m.Groups[2].Value.Replace("''", "'");
                var esc = literal.Replace("'", "''");
                return $"(\"{alias}\".\"Annotation\" LIKE '%' || '{esc}' || '%')";
            });

        // StartsWith → instr(..., 'x') = 1 (common SQLite translation)
        sql = Regex.Replace(
            sql,
            @"instr\(""([^""]+)""\.""Annotation"",\s*'([^']*)'\)\s*=\s*1",
            m =>
            {
                var alias = m.Groups[1].Value;
                var literal = m.Groups[2].Value.Replace("''", "'");
                var esc = literal.Replace("'", "''");
                return $"(\"{alias}\".\"Annotation\" LIKE '{esc}' || '%')";
            });

        return sql;
    }

    private static List<string> GetFields(string prefix, string? expression)
    {
        var fields = new List<string>();

        if (expression != null)
        {
            var split1 = expression.Split(prefix);
            foreach (var s1 in split1)
            {
                var field = s1.Split([' ', ')']).First();
                field = field.Split('.').First();
                field = char.ToUpper(field[0]) + field.Substring(1);
                if (!field.StartsWith('(') && !fields.Contains(field))
                {
                    fields.Add(field);
                }
            }
        }

        return fields;
    }

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

        var sqlAasCondition = NormalizeSqlAliases(sqlConditions.FormulaConditions.GetValueOrDefault("aas", ""));
        var sqlSmCondition = NormalizeSqlAliases(sqlConditions.FormulaConditions.GetValueOrDefault("sm", ""));
        var sqlSmeCondition = NormalizeSqlAliases(sqlConditions.FormulaConditions.GetValueOrDefault("sme", ""));
        var sqlValueCondition = NormalizeSqlAliases(sqlConditions.FormulaConditions.GetValueOrDefault("value", ""));
        var sqlOverallCondition = NormalizeSqlAliases(sqlConditions.FormulaConditions.GetValueOrDefault("all", ""));
        var sqlFilterAllCondition = NormalizeSqlAliases(sqlConditions.FilterConditions.GetValueOrDefault("all", ""));

        var restrictAAS = !string.IsNullOrWhiteSpace(sqlAasCondition);
        var restrictSM = !string.IsNullOrWhiteSpace(sqlSmCondition);
        var restrictSME = !string.IsNullOrWhiteSpace(sqlSmeCondition);
        var restrictValue = !string.IsNullOrWhiteSpace(sqlValueCondition);

        var aasFields = new List<string>();
        var smFields = new List<string>();
        var smeFields = new List<string>();
        var overallFieldCondition = AppendAndCondition(sqlOverallCondition, sqlFilterAllCondition);

            if (!string.IsNullOrWhiteSpace(overallFieldCondition) && !string.Equals(overallFieldCondition, "true", StringComparison.OrdinalIgnoreCase))
            {
                aasFields = GetSqlFields("aas", overallFieldCondition);
                smFields = GetSqlFields("sm", overallFieldCondition);
                smeFields = GetSqlFields("sme", overallFieldCondition);
            }

        var aasExistInCondition = !string.IsNullOrWhiteSpace(overallFieldCondition)
            && overallFieldCondition.Contains("\"aas\".");

        bool isWithAASTable = restrictAAS || aasExistInCondition || resultType == ResultType.AssetAdministrationShell;
        var whereAas = sqlAasCondition;
        var whereSm = sqlSmCondition;
        var sqlPathByIndex = new Dictionary<int, PathJoin>();
        var sqlMatchByIndex = new Dictionary<int, MatchJoin>();
        var pathCount = 0;

        foreach (var p in sqlConditions.Paths)
        {
            if (p.Placeholder.StartsWith("path", StringComparison.Ordinal)
                && int.TryParse(p.Placeholder["path".Length..], out var idx))
            {
                sqlPathByIndex[idx] = p;
                pathCount = Math.Max(pathCount, idx + 1);
            }
        }

        foreach (var m in sqlConditions.Matches)
        {
            var matchPathIndices = new List<int>();
            foreach (var p in m.Paths)
            {
                if (p.Placeholder.StartsWith("path", StringComparison.Ordinal)
                    && int.TryParse(p.Placeholder["path".Length..], out var idx))
                {
                    sqlPathByIndex[idx] = p;
                    matchPathIndices.Add(idx);
                    pathCount = Math.Max(pathCount, idx + 1);
                }
            }

            if (matchPathIndices.Count > 0)
            {
                sqlMatchByIndex[matchPathIndices.Min()] = m;
            }
        }

        

        // EF Core 8 / SQLite generates deterministic single-letter aliases for the
        // 4-table join above (first letter of entity type, incremented on collision):
        //   AASSets   → "a"
        //   SMRefSets → "s"   (consumed by the first join)
        //   SMSets    → "s0"
        //   SMESets   → "s1"
        //   ValueSets → "v"
        // These constants eliminate the fragile regex extraction of ToQueryString() output.
        const string aasPrefix = "aas";
        const string smPrefix = "sm";
        const string smePrefix = "sme";
        const string valuePrefix = "value";

        var raw = "";
        var rawBase = "";
        var useValueOnlyDirectPath = false;
        var pathPrefix = new List<string>();

        var selectAas = "(\r\n  SELECT Id, Identifier";
        foreach (var aasField in aasFields)
        {
            if (aasField != "Identifier")
            {
                selectAas += $", {aasField}";
            }
        }
        selectAas += "\r\n  FROM AASSets\r\n";
        if (!string.IsNullOrWhiteSpace(whereAas))
        {
            var whereAas2 = whereAas.Replace($"\"{aasPrefix}\".", "");
            selectAas += $"WHERE {whereAas2}\r\n";
        }
        selectAas += ")";

        var selectSm = "(\r\n  SELECT Id, IdShort, Identifier";
        foreach (var smField in smFields)
        {
            if (smField != "IdShort" && smField != "Identifier")
            {
                selectSm += $", {smField}";
            }
        }
        selectSm += "\r\n  FROM SMSets\r\n";
        if (!string.IsNullOrWhiteSpace(whereSm))
        {
            var whereSm2 = whereSm.Replace("\"s\".", "");
            whereSm2 = whereSm2.Replace("\"s0\".", "");
            whereSm2 = whereSm2.Replace($"\"{smPrefix}\".", "");

            selectSm += $"WHERE {whereSm2}\r\n";
        }
        selectSm += ")";

        rawBase = "SELECT DISTINCT ";

        if (resultType == ResultType.AssetAdministrationShell)
        {
            rawBase += "aas.Id\r\n";
        }
        else
        {
            rawBase += "sm.Id\r\n";
        }

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

        var matchPlaceholderSql = new Dictionary<string, string>();
        for (var i = 0; i < pathCount;)
        {
            var j = i;
            var join = "";
            if (sqlMatchByIndex.TryGetValue(i, out var match))
            {
                join += $"LEFT JOIN(\r\n";
                join += BuildMatchSubquerySql(match);
                i += match.Paths.Count;
            }
            else
            {
                if (!sqlPathByIndex.TryGetValue(i, out var sqlPath))
                    throw new InvalidOperationException($"CombineTablesLEFT: missing SQL path definition for path{i}.");

                join += $"LEFT JOIN(\r\n";
                join += $"SELECT sme.SMId AS SMId";
                join += "\r\n";
                join += $"FROM SMESets sme\r\n";
                join += $"LEFT JOIN ValueSets v ON v.SMEId = sme.Id ";

                var pathSql = sqlPath.SubquerySql
                    .Replace("\"sme\".", "sme.")
                    .Replace("\"v\".", "v.");
                var splitSql = pathSql.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
                var valueSQL = splitSql.FirstOrDefault(l => l.TrimStart().StartsWith("WHERE ", StringComparison.Ordinal))?.TrimStart().Substring("WHERE ".Length) ?? "";
                var smeSQLIdShort = splitSql.FirstOrDefault(l => l.TrimStart().StartsWith("AND ", StringComparison.Ordinal) && l.Contains("\"IdShort\"", StringComparison.Ordinal) && !l.Contains("\"IdShortPath\"", StringComparison.Ordinal))?.TrimStart().Substring("AND ".Length) ?? "";
                var smeSQLPath = splitSql.LastOrDefault(l => l.TrimStart().StartsWith("AND ", StringComparison.Ordinal))?.TrimStart().Substring("AND ".Length) ?? "";

                join += $"AND {valueSQL}\r\n";
                join += $"WHERE {valueSQL} ";

                if (smeSQLPath.Contains("[]"))
                    smeSQLPath = smeSQLPath.Replace("[]", "[%]");
                if (smeSQLPath.Contains("%"))
                {
                    smeSQLPath = smeSQLPath.Replace("\"IdShortPath\" = ", "\"IdShortPath\" GLOB ");
                    smeSQLPath = smeSQLPath.Replace("%", "*");
                }

                if (!smeSQLIdShort.Contains("[]") && !smeSQLIdShort.Contains("%"))
                    join += $"AND {smeSQLIdShort} ";

                join += $"AND {smeSQLPath}\r\n";
                i++;
            }

            join += $"\r\n) AS p{j + 1} ON p{j + 1}.SMId = sm.Id\r\n";

            pathPrefix.Add($"p{j + 1}");
            matchPlaceholderSql[$"$$path{j}$$"] = $"(p{j + 1}.SMId IS NOT NULL)";
            if (sqlMatchByIndex.TryGetValue(j, out var currentMatch))
                matchPlaceholderSql[$"$${currentMatch.Placeholder}$$"] = $"(p{j + 1}.SMId IS NOT NULL)";

            rawBase += join;
        }

        string? convertConditionSQL = AppendAndCondition(
            sqlOverallCondition,
            sqlConditions?.FilterConditions.GetValueOrDefault("all", ""));

        if (!string.IsNullOrEmpty(convertConditionSQL))
        {
            for (var i = 0; i < pathCount; i++)
            {
                convertConditionSQL = convertConditionSQL.Replace($"$$path{i}$$", $"(p{i + 1}.SMId IS NOT NULL)");
            }
            foreach (var replacement in matchPlaceholderSql)
                convertConditionSQL = convertConditionSQL.Replace(replacement.Key, replacement.Value);

            convertConditionSQL = NormalizeSqlAliases(convertConditionSQL);

            List<string> combinedSelectFields = [];
            List<string> combinedWhereParts = [];

            // -----------------------------
            // VALUE-PREFIX einsammeln
            // -----------------------------
            if (convertConditionSQL.Contains($"\"{valuePrefix}\"."))
            {
                List<string> fields = ["\"SValue\"", "\"NValue\"", "\"DTValue\"", "\"Annotation\""];

                var splitByValue = convertConditionSQL.Split($"\"{valuePrefix}\".").ToList();

                var withInstr = false;
                for (var i = 0; i < splitByValue.Count; i++)
                {
                    if (splitByValue[i].EndsWith(" IS NOT NULL AND instr("))
                    {
                        withInstr = true;
                        continue;
                    }

                    var fieldExpression = splitByValue[i];
                    if (fieldExpression.IsNullOrEmpty())
                    {
                        continue;
                    }

                    var splitExpression = SplitExpression(fieldExpression);
                    if (splitExpression?.Count < 3)
                    {
                        continue;
                    }

                    var field = splitExpression[0].Replace(",", "");

                    if (fields.Contains(field))
                    {
                        var replace = "";
                        var replacement = "";

                        if (!withInstr)
                        {
                            replace = $"\"{valuePrefix}\"." + field + " " + splitExpression[1] + " " + splitExpression[2];
                            replace = replace.TrimEnd(')');
                            replacement = $"\"value\"." + field + " " + splitExpression[1] + " " + splitExpression[2];
                            replacement = replacement.TrimEnd(')');
                        }
                        else
                        {
                            withInstr = false;

                            replace = $"\"{valuePrefix}\".{field} IS NOT NULL AND instr(\"{valuePrefix}\".{field}, {splitExpression[1]}) > 0";
                            replacement = $"\"value\".{field} IS NOT NULL AND instr(\"value\".{field}, {splitExpression[1]}) > 0";
                        }

                        AddDistinct(combinedSelectFields, $"    v.{field} AS {field}");
                        AddDistinct(combinedWhereParts, $"({replace})");
                        convertConditionSQL = convertConditionSQL.Replace(replace, replacement);
                    }
                }
            }

            // -----------------------------
            // SME-PREFIX einsammeln
            // -----------------------------
            if (convertConditionSQL.Contains($"\"{smePrefix}\"."))
            {
                var splitBySme = convertConditionSQL.Split($"\"{smePrefix}\".").ToList();

                var withInstr = false;
                for (var i = 0; i < splitBySme.Count; i++)
                {
                    if (splitBySme[i].EndsWith(" IS NOT NULL AND instr("))
                    {
                        withInstr = true;
                        continue;
                    }

                    var fieldExpression = splitBySme[i];
                    if (fieldExpression.IsNullOrEmpty())
                    {
                        continue;
                    }

                    var splitExpression = SplitExpression(fieldExpression);
                    if (splitExpression?.Count < 3)
                    {
                        continue;
                    }

                    var field = splitExpression[0].Replace(",", "").Replace("\"", "");

                    if (smeFields.Contains(field))
                    {
                        var replace = "";
                        var replacement = "";
                        var where = "";

                        if (!withInstr)
                        {
                            replace = $"\"{smePrefix}\".\"{field}\" " + splitExpression[1] + " " + splitExpression[2];
                            replace = replace.TrimEnd(')');
                            replacement = $"\"value\".\"{field}\" " + splitExpression[1] + " " + splitExpression[2];
                            replacement = replacement.TrimEnd(')');
                            where = $"\"sme\".\"{field}\" " + splitExpression[1] + " " + splitExpression[2];
                            where = where.TrimEnd(')');
                        }
                        else
                        {
                            withInstr = false;

                            replace = $"\"{smePrefix}\".\"{field}\" IS NOT NULL AND instr(\"{smePrefix}\".\"{field}\", {splitExpression[1]}) > 0";
                            replacement = $"\"value\".\"{field}\" IS NOT NULL AND instr(\"value\".\"{field}\", {splitExpression[1]}) > 0";
                            where = $"\"sme\".\"{field}\" IS NOT NULL AND instr(\"sme\".\"{field}\", {splitExpression[1]}) > 0";
                        }

                        AddDistinct(combinedSelectFields, $"    sme.\"{field}\" AS \"{field}\"");
                        AddDistinct(combinedWhereParts, $"({where})");
                        convertConditionSQL = convertConditionSQL.Replace(replace, replacement);
                    }
                }
            }

            // -----------------------------
            // Gemeinsamen Vorfilter-Join bauen
            // -----------------------------
            if (combinedSelectFields.Count > 0 || combinedWhereParts.Count > 0)
            {
                var distinctWhereParts = combinedWhereParts.Distinct().ToList();
                var valueSetFirst = distinctWhereParts.Count > 0
                    && distinctWhereParts.TrueForAll(static p => !p.Contains("\"sme\".", StringComparison.Ordinal));
                var onlyValueColumnsInSelect = combinedSelectFields.Count == 0
                    || combinedSelectFields.TrueForAll(static line => !line.Contains("sme.", StringComparison.Ordinal));

                var canValueOnlyDirect = pathCount == 0
                    && !isWithAASTable
                    && string.IsNullOrWhiteSpace(whereSm)
                    && resultType != ResultType.AssetAdministrationShell
                    && distinctWhereParts.Count > 0
                    && valueSetFirst
                    && onlyValueColumnsInSelect
                    && !flags.Contains("$UNION")
                    && !flags.Contains("$TEMPTABLE");

                if (canValueOnlyDirect)
                {
                    // Drop t + LEFT JOIN value: only Value predicates, no SM filter, no path joins — drive from ValueSets.
                    useValueOnlyDirectPath = true;
                    rawBase = "SELECT DISTINCT sm.Id\r\nFROM ValueSets v\r\nJOIN SMESets sme ON sme.Id = v.SMEId\r\nJOIN SMSets sm ON sm.Id = sme.SMId\r\n";
                    rawBase += "WHERE " + string.Join("\r\n     OR ", distinctWhereParts) + "\r\n";
                    convertConditionSQL = "";
                }
                else
                {
                    rawBase += "LEFT JOIN(\r\n";
                    rawBase += "  SELECT DISTINCT\r\n";
                    rawBase += "    sme.SMId AS \"SMId\"";

                    foreach (var selectField in combinedSelectFields.Distinct())
                    {
                        rawBase += ",\r\n";
                        rawBase += selectField;
                    }

                    rawBase += "\r\n";
                    // ValueSets-first when the subquery only filters on v.* (sme.* in OR would need SME-first).
                    if (valueSetFirst)
                    {
                        rawBase += "  FROM ValueSets v\r\n";
                        rawBase += "  JOIN SMESets sme ON sme.Id = v.SMEId\r\n";
                    }
                    else
                    {
                        rawBase += "  FROM SMESets sme\r\n";
                        rawBase += "  LEFT JOIN ValueSets v ON v.SMEId = sme.Id\r\n";
                    }

                    if (distinctWhereParts.Count > 0)
                    {
                        rawBase += "  WHERE " + string.Join("\r\n     OR ", distinctWhereParts) + "\r\n";
                    }

                    rawBase += ") AS value ON value.\"SMId\" = sm.Id\r\n";
                }
            }

            var withUnion = false;
            var withTempTable = false;
            if (flags.Contains("$UNION"))
            {
                withUnion = true;
            }
            if (flags.Contains("$TEMPTABLE"))
            {
                withTempTable = true;
            }

            if (withUnion || withTempTable)
            {
                var splitConvertConditionSQL = SplitTopLevelOr(convertConditionSQL);

                if (withTempTable)
                {
                    raw += "DROP TABLE IF EXISTS union_ids;\r\n";
                    raw += "CREATE TEMP TABLE union_ids (\r\n";
                    raw += "Id INTEGER PRIMARY KEY\r\n";
                    raw += ") WITHOUT ROWID;\r\n";
                    raw += "\r\n";
                }

                var splitLeftJoin = rawBase.Split("LEFT JOIN(\r\n");

                for (var s = 0; s < splitConvertConditionSQL.Count; s++)
                {
                    var rawBaseUnionStart = splitLeftJoin[0];

                    var rawBaseUnion = "";
                    for (var i = 1; i < splitLeftJoin.Length; i++)
                    {
                        if (splitConvertConditionSQL[s].Contains($"({pathPrefix[i - 1]}.SMId IS NOT NULL)"))
                        {
                            rawBaseUnion += "LEFT JOIN(\r\n";
                            rawBaseUnion += splitLeftJoin[i];
                        }
                    }

                    var rawBaseTopLevel = rawBaseUnionStart + rawBaseUnion + "WHERE " + splitConvertConditionSQL[s] + "\r\n";
                    if (withTempTable)
                    {
                        rawBaseTopLevel += "ORDER BY 1\r\n";
                        raw += "INSERT OR IGNORE INTO union_ids(Id)\r\n" + rawBaseTopLevel + ";\r\n\r\n";
                    }
                    if (withUnion)
                    {
                        raw += rawBaseTopLevel;
                        if (s != splitConvertConditionSQL.Count -1)
                        {
                            raw += "\r\n" + "UNION\r\n\r\n";
                        }
                    }
                }

                rawBase = raw;

                raw = rawBase;
                if (raw.Contains(" LIKE "))
                {
                    raw = raw.Replace(" LIKE ", " GLOB ");
                    raw = raw.Replace("%", "*");
                }

                List<int> page = [];
                if (withUnion)
                {
                    raw = "SELECT DISTINCT Id\r\nFROM(\r\n\r\n" + raw;
                    raw += "\r\n)\r\nORDER BY Id\r\n";
                    raw += $"LIMIT {pageSize} OFFSET {pageFrom}\r\n";

                    AddGeneratedSql(generatedSql, raw);
                    page = db.Set<SMSetIdResult>()
                        .FromSqlRaw(raw)
                        .AsNoTracking()
                        .Select(x => x.Id)
                        .ToList();
                }
                if (withTempTable)
                {
                    using var tx = db.Database.BeginTransaction();

                    AddGeneratedSql(generatedSql, raw);
                    db.Database.ExecuteSqlRaw(raw);

                    var tempTableSelectRaw = $@"SELECT Id
                        FROM union_ids
                        ORDER BY 1
                        LIMIT {pageSize} OFFSET {pageFrom}";
                    AddGeneratedSql(generatedSql, tempTableSelectRaw);
                    page = db.Set<SMSetIdResult>()
                        .FromSqlRaw(@"
                        SELECT Id
                        FROM union_ids
                        ORDER BY 1
                        LIMIT {0} OFFSET {1}", pageSize, pageFrom)
                        .AsNoTracking()
                        .Select(x => x.Id)
                        .ToList();

                    tx.Commit();
                }

                return page;
            }
        }

        raw = string.IsNullOrWhiteSpace(convertConditionSQL)
            ? rawBase + "\r\n"
            : rawBase + $"WHERE {convertConditionSQL}\r\n";

        if (raw.Contains(" LIKE "))
        {
            raw = raw.Replace(" LIKE ", " GLOB ");
            raw = raw.Replace("%", "*");
        }

        raw += useValueOnlyDirectPath
            ? $"ORDER BY sm.Id\r\nLIMIT {pageSize} OFFSET {pageFrom}\r\n"
            : $"ORDER BY sm.Id\r\nLIMIT {pageSize} OFFSET {pageFrom}\r\n";

        var rawNew = BuildRawSqlFromSqlConditions(sqlConditions, isWithAASTable, resultType, pageFrom, pageSize);
        if (rawNew == null)
            throw new InvalidOperationException("BuildRawSqlFromSqlConditions returned null.");

        AddGeneratedSql(generatedSql, rawNew);
        var qpRaw = GetQueryPlan(db, rawNew);

        return db.Set<SMSetIdResult>()
            .FromSqlRaw(rawNew)
            .AsNoTracking()
            .Select(x => x.Id)
            .ToList();

        /*
        IQueryable<SMSetIdResult> resultSMId = null;
        resultSMId = db.Set<SMSetIdResult>()
               .FromSqlRaw(raw)
               .AsNoTracking()
               .AsQueryable();

        var smRawSQL = resultSMId.ToQueryString();
        var qp = GetQueryPlan(db, smRawSQL);

        // return resultSMId.OrderBy(r => r.Id).Select(r => r.Id).Skip(pageFrom).Take(pageSize).ToList();
        return resultSMId.Select(r => r.Id).ToList();
        */
    }

    // ------------------------------------------------------------------
    // SQL assembly from SqlConditions (new path)
    // ------------------------------------------------------------------
    internal static string? BuildRawSqlFromSqlConditions(
        SqlConditions sc, bool isWithAASTable, ResultType resultType, int pageFrom, int pageSize)
    {
        var whereAas = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("aas", ""));
        var whereSm  = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("sm",  ""));
        var whereSme = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("sme", ""));
        var whereVal = NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("value", ""));
        var filterAll = NormalizeSqlAliases(sc.FilterConditions.GetValueOrDefault("all", ""));

        // Resolve placeholder references
        var overall = AppendAndCondition(NormalizeSqlAliases(sc.FormulaConditions.GetValueOrDefault("all", "")), filterAll);
        var pathNum  = 1;
        var matchNum = 1;
        foreach (var path  in sc.Paths)    overall = overall.Replace($"$${path.Placeholder}$$",  $"(p{pathNum++}.SMId IS NOT NULL)");
        foreach (var match in sc.Matches)  overall = overall.Replace($"$${match.Placeholder}$$", $"(m{matchNum++}.SMId IS NOT NULL)");

        // ----------------------------------------------------------------
        // useValueOnlyDirect: no paths/matches, no AAS join, no SM filter,
        // overall only references value-table columns (v.)
        // ----------------------------------------------------------------
        bool hasPathsOrMatches = sc.Paths.Count > 0 || sc.Matches.Count > 0;
        bool overallHasSmeRef  = overall.Contains("\"sme\".");
        bool overallHasVRef    = overall.Contains("\"value\".");
        bool overallHasTRef    = overall.Contains("\"sm\".") || overall.Contains("\"aas\".");

        if (!hasPathsOrMatches && !isWithAASTable
            && string.IsNullOrWhiteSpace(whereSm)
            && resultType != ResultType.AssetAdministrationShell
            && overallHasVRef && !overallHasTRef)
        {
            var raw = "SELECT DISTINCT sm.Id\r\nFROM ValueSets AS value\r\nJOIN SMESets AS sme ON sme.Id = value.SMEId\r\nJOIN SMSets AS sm ON sm.Id = sme.SMId\r\n";
            raw += $"WHERE {overall}\r\n";
            raw += $"ORDER BY sm.Id\r\nLIMIT {pageSize} OFFSET {pageFrom}\r\n";
            return raw;
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

        // Standalone path LEFT JOINs
        pathNum = 1;
        foreach (var path in sc.Paths)
        {
            // SubquerySql is body only (FROM … WHERE …); add SELECT header with fixed alias "sme"
            rawBase += $"LEFT JOIN (\r\nSELECT sme.SMId AS SMId\r\n{path.SubquerySql}\r\n) AS p{pathNum} ON p{pathNum}.SMId = sm.Id\r\n";
            pathNum++;
        }

        // Match LEFT JOINs
        matchNum = 1;
        foreach (var match in sc.Matches)
        {
            var matchSql = BuildMatchSubquerySql(match);
            rawBase += $"LEFT JOIN (\r\n{matchSql}\r\n) AS m{matchNum} ON m{matchNum}.SMId = sm.Id\r\n";
            matchNum++;
        }

        if (overallHasSmeRef)
        {
            var smeFields = GetSqlFields("sme", overall);
            var smeWhere = whereSme.Replace("\"sme\".", "\"sme_inner\".");

            rawBase += "LEFT JOIN(\r\n  SELECT DISTINCT\r\n    sme_inner.SMId AS \"SMId\"";
            foreach (var field in smeFields)
                rawBase += $",\r\n    sme_inner.\"{field}\" AS \"{field}\"";
            rawBase += "\r\n  FROM SMESets sme_inner\r\n";
            if (!string.IsNullOrWhiteSpace(smeWhere))
                rawBase += $"  WHERE {smeWhere}\r\n";
            rawBase += ") AS sme ON sme.\"SMId\" = sm.Id\r\n";
        }

        // Value LEFT JOIN: when overall references v. columns outside path/match subqueries
        if (overallHasVRef)
        {
            // Collect which value columns are referenced
            var valFields = new List<string>();
            foreach (var col in new[] { "SValue", "NValue", "DTValue", "Annotation" })
                if (overall.Contains($"\"value\".\"{col}\"")) valFields.Add(col);
            var valWhere  = string.IsNullOrWhiteSpace(whereVal) ? "1=1" : whereVal.Replace("\"value\".", "v.");

            rawBase += $"LEFT JOIN(\r\n  SELECT DISTINCT\r\n    sme.SMId AS \"SMId\"";
            foreach (var f in valFields) rawBase += $",\r\n    v.\"{f}\" AS \"{f}\"";
            rawBase += "\r\n  FROM ValueSets v\r\n  JOIN SMESets sme ON sme.Id = v.SMEId\r\n";
            rawBase += $"  WHERE {valWhere}\r\n) AS value ON value.\"SMId\" = sm.Id\r\n";

            // Replace "v"."Col" → "value"."Col" in the outer WHERE
            foreach (var col in valFields)
                overall = overall.Replace($"\"v\".\"{col}\"", $"\"value\".\"{col}\"");
        }

        if (!string.IsNullOrWhiteSpace(overall) && overall != "1=1")
            rawBase += $"WHERE {overall}\r\n";

        rawBase += $"ORDER BY sm.Id\r\nLIMIT {pageSize} OFFSET {pageFrom}\r\n";
        return rawBase;
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

        var sb = new System.Text.StringBuilder();
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

    private static string AppendAndCondition(string? left, string? right)
    {
        var normalizedLeft = string.IsNullOrWhiteSpace(left) || left.Trim() == "1=1" ? "" : left.Trim();
        var normalizedRight = string.IsNullOrWhiteSpace(right) || right.Trim() == "1=1" ? "" : right.Trim();

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
            sqlConditionsOut.FormulaConditions["all"] = "true";
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
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("all", le._expression);
                                            QueryGrammarJSON.createExpression("all-aas", le);
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("all-aas", le._expression);
                                            QueryGrammarJSON.createExpression("aas.", le);
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("aas.", le._expression);
                                        }
                                    }
                                }

                                // New direct-SQL path: build SqlConditions alongside the LINQ path
                                sqlConditionsOut = QueryGrammarJSON.CreateSqlConditions(logicalExpressions[0]!);
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

    private static string SafeExtractWherePredicate(string? fullSql)
    {
        if (string.IsNullOrEmpty(fullSql))
            return "";
        var parts = fullSql.Split("WHERE ", StringSplitOptions.None);
        if (parts.Length < 2)
            return "";
        var predicate = parts[^1].Trim();
        if (predicate.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return "";
        return predicate;
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

