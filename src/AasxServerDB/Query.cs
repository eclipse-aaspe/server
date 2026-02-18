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
}

public class QueryResult
{
    public List<IAssetAdministrationShell> Shells { get; set; }
    public List<ISubmodel> Submodels { get; set; }
    public List<ISubmodelElement> SubmodelElements { get; set; }
    public List<string> Ids { get; set; }
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

    public List<int> SearchSMs(Dictionary<string, string>? securityCondition, AasContext db, int pageFrom, int pageSize, string expression)
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

        var query = GetSMs(false, securityCondition, out condition, ResultType.Submodel, qResult, watch, db, false, false, "", "", "", pageFrom, pageSize, expression);

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

    internal QueryResult GetQueryData(bool noSecurity, AasContext db, Dictionary<string, string>? securityCondition,
        int pageFrom, int pageSize, ResultType resultType, string expression)
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
        var result = GetSMs(noSecurity, securityCondition, out condition, resultType,
            qResult, watch, db, false, false, "", "", "", pageFrom, pageSize, expression);
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
                SubmodelElements = []
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
                            if (condition != null && condition.TryGetValue("filter-aas", out var filterAas))
                            {
                                if (filterAas != null && filterAas != "" && filterAas != "$SKIP")
                                {
                                    if (filterAas.Contains("globalAssetId"))
                                    {
                                        aas.Submodels = null;
                                        aas.Description = null;
                                        aas.DisplayName = null;
                                    }
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
                if (!qResult.WithSelectId && !qResult.WithSelectMatch)
                {
                    var timeStamp = DateTime.UtcNow;
                    var submodels = new List<ISubmodel>();

                    var smIdList = result;
                    var smList = db.SMSets.Where(sm => smIdList.Contains(sm.Id)).ToList();

                    foreach (var sm in smList.Select(selector: submodelDB =>
                        ReadSubmodel(db, smDB: submodelDB, "", securityCondition, condition)))
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
                                    var smeMerged = CrudOperator.GetSmeMerged(db, null, smeTree, sm);
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

    private List<int>? GetSMs(bool noSecurity, Dictionary<string, string>? securityCondition, out Dictionary<string, string>? condition,
        ResultType resultType,
        QResult qResult, Stopwatch watch, AasContext db, bool withCount = false, bool withTotalCount = false,
        string semanticId = "", string identifier = "", string diffString = "", int pageFrom = -1, int pageSize = -1, string expression = "")
    {
        List<int>? result = null;
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

        var consolidate = false;
        if (expression.Contains("$CONSOLIDATE"))
        {
            messages.Add("$CONSOLIDATE");
            consolidate = true;
            expression = expression.Replace("$CONSOLIDATE", string.Empty);
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

        List<int> comTable = null;

        // get data
        if (withExpression) // with expression
        {
            if (!consolidate)
            {
                comTable = CombineTablesCASE(db, conditionsExpression, pageFrom, pageSize, resultType, consolidate);
            }
            else
            {
                comTable = CombineTablesLEFT(db, conditionsExpression, pageFrom, pageSize, resultType, consolidate);
            }
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
        IQueryable<ValueSet>? valueTable;
        //IQueryable<IValueSet>? iValueTable;
        //IQueryable<DValueSet>? dValueTable;

        // get data
        if (withExpression) // with expression
        {
            // check restrictions
            restrictSM = !conditionsExpression["sm"].IsNullOrEmpty() && !conditionsExpression["sm"].Equals("true");
            restrictSME = !conditionsExpression["sme"].IsNullOrEmpty() && !conditionsExpression["sme"].Equals("true");
            /*
            restrictSValue = !conditionsExpression["svalue"].IsNullOrEmpty() && !conditionsExpression["svalue"].Equals("true");
            restrictNValue = !conditionsExpression["nvalue"].IsNullOrEmpty() && !conditionsExpression["nvalue"].Equals("true");
            restrictValue = restrictSValue || restrictNValue;
            */
            restrictValue = !conditionsExpression["value"].IsNullOrEmpty() && !conditionsExpression["value"].Equals("true");

            // restrict all tables seperate
            smTable = restrictSM ? db.SMSets.Where(conditionsExpression["sm"]) : db.SMSets;
            smeTable = restrictSME ? db.SMESets.Where(conditionsExpression["sme"]) : db.SMESets;
            valueTable = restrictValue ? (restrictSValue ? db.ValueSets.Where(conditionsExpression["value"]) : null) : db.ValueSets;
            //iValueTable = restrictValue ? (restrictNValue ? db.IValueSets.Where(conditionsExpression["nvalue"]) : null) : db.IValueSets;
            //dValueTable = restrictValue ? (restrictNValue ? db.DValueSets.Where(conditionsExpression["nvalue"]) : null) : db.DValueSets;
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
            valueTable = restrictValue ? (restrictSValue ? db.ValueSets.Where(v => restrictSValue && v.SValue != null && (!withContains || v.SValue.Contains(contains)) && (!withEqualString || v.SValue.Equals(equal))) : null) : db.ValueSets;
            //iValueTable = restrictValue ? (restrictNValue ? db.IValueSets.Where(v => restrictNValue && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))) : null) : db.IValueSets;
            //dValueTable = restrictValue ? (restrictNValue ? db.DValueSets.Where(v => restrictNValue && v.Value != null && (!withEqualNum || v.Value == equalNum) && (!withCompare || (v.Value >= lowerNum && v.Value <= upperNum))) : null) : db.DValueSets;
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
        if (valueTable != null)
        {
            var sValueSelect = valueTable.Select(sv => new CombinedValue
            {
                SMEId = sv.SMEId,
                SValue = sv.SValue,
                MValue = sv.NValue
            });
            combinedValues = sValueSelect;
        }
        //if (iValueTable != null)
        //{
        //    var iValueSelect = iValueTable.Select(iv => new CombinedValue
        //    {
        //        SMEId = iv.SMEId,
        //        SValue = null,
        //        MValue = iv.Value
        //    });

        //    if (combinedValues != null)
        //    {
        //        combinedValues = combinedValues
        //            .Concat(iValueSelect);
        //    }
        //    else
        //    {
        //        combinedValues = iValueSelect;
        //    }
        //}
        //if (dValueTable != null)
        //{
        //    var dValueSelect = dValueTable.Select(dv => new CombinedValue
        //    {
        //        SMEId = dv.SMEId,
        //        SValue = null,
        //        MValue = dv.Value
        //    });

        //    if (combinedValues != null)
        //    {
        //        combinedValues = combinedValues
        //            .Concat(dValueSelect);
        //    }
        //    else
        //    {
        //        combinedValues = dValueSelect;
        //    }
        //}

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

        //// if needed create idShortPath
        //if (false && (requested.Contains("idShortPath") || requested.Contains("url")))
        //{
        //    var combineTableRawSQL = qSearch.ToQueryString();
        //    // Append the "get path" section
        //    combineTableRawSQL = $@"
        //            WITH FilteredSMAndSMEAndValue AS (
        //                {combineTableRawSQL}
        //            ), 
        //            RecursiveSME AS (
        //                WITH RECURSIVE SME_CTE AS (
        //                    SELECT Id, IdShort, ParentSMEId, IdShort AS IdShortPath, Id AS StartId 
        //                    FROM SMESets 
        //                    WHERE Id IN (SELECT ""SME_Id"" FROM FilteredSMAndSMEAndValue) 
        //                    UNION ALL 
        //                    SELECT x.Id, x.IdShort, x.ParentSMEId, x.IdShort || '.' || c.IdShortPath, c.StartId 
        //                    FROM SMESets x 
        //                    INNER JOIN SME_CTE c ON x.Id = c.ParentSMEId 
        //                ) 
        //                SELECT StartId AS Id, IdShortPath 
        //                FROM SME_CTE 
        //                WHERE ParentSMEId IS NULL 
        //            )
        //            SELECT sme.SM_Identifier, r.IdShortPath, strftime('%Y-%m-%d %H:%M:%f', sme.TimeStamp) AS SME_TimeStamp, sme.SValue, sme.MValue
        //            FROM FilteredSMAndSMEAndValue AS sme 
        //            INNER JOIN RecursiveSME AS r ON sme.SME_Id = r.Id;
        //            ";
        //    // qSearch = db.Set<SMEResultRaw>().FromSqlRaw(combineTableRawSQL).AsQueryable();
        //    qSearch = db.Database.SqlQueryRaw<CombinedSMEResult>(combineTableRawSQL);
        //}

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

    private static List<string> GetFields(string prefix, string? expression)
    {
        var fields = new List<string>();

        if (expression != null)
        {
            var split1 = expression.Split(prefix);
            foreach (var s1 in split1)
            {
                var field = s1.Split([' ', ')']).First();
                field = char.ToUpper(field[0]) + field.Substring(1);
                if (!field.StartsWith('(') && !fields.Contains(field))
                {
                    fields.Add(field);
                }
            }
        }

        return fields;
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
    }

    private static List<int> CombineTablesCASE(
        AasContext db,
        Dictionary<string, string>? conditionsExpression,
        int pageFrom,
        int pageSize,
        ResultType resultType,
        bool consolidaté
        )
    {
        IQueryable<AASSet>? aasTable = null;
        IQueryable<SMSet>? smTable = null;
        IQueryable<SMESet>? smeTable = null;
        IQueryable<ValueSet>? valueTable = null;

        var restrictAAS = conditionsExpression.TryGetValue("aas", out var value) && value != "" && value != "true";
        var restrictSM = conditionsExpression.TryGetValue("sm", out value) && value != "" && value != "true";
        var restrictSME = conditionsExpression.TryGetValue("sme", out value) && value != "" && value != "true";
        var restrictValue = conditionsExpression.TryGetValue("value", out value) && value != "" && value != "true";

        var aasFields = new List<string>();
        var smFields = new List<string>();
        var smeFields = new List<string>();

        if (conditionsExpression.TryGetValue("all", out value) && value != "" && value != "true")
        {
            aasFields = GetFields("aas.", value);
            smFields = GetFields("sm.", value);
            smeFields = GetFields("sme.", value);
        }

        var withPathSme = false;
        var withMatch = false;
        var withRecursive = false;
        var pathSME = "";
        var pathAllCondition = "";
        var pathAllConditionRaw = "";

        var aasExistInCondition = conditionsExpression.TryGetValue("all", out value) && value.Contains("aas.");

        bool isWithAASTable = restrictAAS || aasExistInCondition || resultType == ResultType.AssetAdministrationShell;

        if (conditionsExpression.TryGetValue("path-sme", out pathSME))
        {
            withPathSme = true;
            if (!conditionsExpression.TryGetValue("path-all", out pathAllCondition))
            {
                withPathSme = false;
            }
            else
            {
                withRecursive = pathAllCondition.Contains("sme.") || pathAllCondition.Contains("svalue")
                    || pathAllCondition.Contains("mvalue") || pathAllCondition.Contains("dtvalue");
            }
            if (conditionsExpression.TryGetValue("path-raw", out pathAllConditionRaw))
            {
                if (pathAllConditionRaw.Contains("$$match"))
                {
                    withMatch = true;
                }
            }
        }

        if (!conditionsExpression.TryGetValue("path-raw", out var pathAllAas))
        {
            pathAllAas = "";
        }
        List<string> pathConditions = [];

        var selectMatch = "";
        List<string> whereMatch = [];
        if (withMatch)
        {
            var splitMatch = pathAllConditionRaw.Split("$$match$$");
            var matchCount = 0;
            var matchOffset = 0;
            pathAllCondition = "";
            List<string> matchPathList = [];
            for (var iMatch = 0; iMatch < splitMatch.Count(); iMatch++)
            {
                var match = splitMatch[iMatch];

                if (!match.Contains("$$$$tag$$path$$"))
                {
                    pathAllCondition += match;
                }
                else
                {
                    pathAllCondition += $"Math.Abs(SmId) == 10{matchCount}";
                    matchCount++;
                    if (selectMatch != "")
                    {
                        selectMatch += ",";
                    }

                    List<int> order = [];
                    List<int> count = [];
                    List<string> idShortPath = [];
                    List<string> idShortPathLast = [];
                    List<string> idShort = [];
                    List<string> field = [];
                    var split = match.Split("$$tag$$path$$");
                    var with = "";
                    for (var i = 1; i < split.Length; i++)
                    {
                        var firstTag = split[i];
                        var split2 = firstTag.Split("$$");
                        order.Add(i - 1);
                        idShortPath.Add(split2[0]);
                        with = "";
                        if (split2[0].Contains("[]"))
                        {
                            with = "[]";
                        }
                        if (split2[0].Contains("%"))
                        {
                            with = "%";
                        }
                        idShortPathLast.Add(split2[0].Split(with).Last());
                        count.Add(split2[0].Count(c => c == (with == "[]" ? '[' : '%')));
                        idShort.Add(split2[0].Split(".").Last());

                        var c = split2[1] + split2[2];
                        var v = db.ValueSets.Where(c).ToQueryString();
                        var sql = v.Split("WHERE ").Last();
                        field.Add(sql);
                    }
                    order = order.OrderBy(x => count[x]).ToList();

                    with = "";
                    var where = "";
                    if (idShortPath[0].Contains("[]"))
                    {
                        with = "[]";
                        for (var i = 0; i < order.Count - 1; i++)
                        {
                            var firstSplit = idShortPath[order[i]].Split("[]").ToList();
                            var secondSplit = idShortPath[order[i + 1]].Split("[]").ToList();
                            var c = count[order[i]];
                            var firstStartSegment = firstSplit[c - 1];
                            var firstEndSegment = firstSplit[c];
                            var firstIndex = $"Part{matchCount}_{i + 1}_1";
                            selectMatch += $"substr(smePath{order[i] + 1}.IdShortPath, instr(smePath{order[i] + 1}.IdShortPath, '{firstStartSegment}') + length('{firstStartSegment}'),\r\n" +
                               $"instr(smePath{order[i] + 1}.IdShortPath, '{firstEndSegment}') - (instr(smePath{order[i] + 1}.IdShortPath, '{firstStartSegment}') + length('{firstStartSegment}'))) AS {firstIndex},\r\n";
                            var secondStartSegment = secondSplit[c - 1];
                            var secondEndSegment = secondSplit[c];
                            var secondIndex = $"Part{matchCount}_{i + 1}_2";
                            selectMatch += $"substr(smePath{order[i + 1] + 1}.IdShortPath, instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondStartSegment}') + length('{secondStartSegment}'),\r\n" +
                               $"instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondEndSegment}') - (instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondStartSegment}') + length('{secondStartSegment}'))) AS {secondIndex}";
                            if (where != "")
                            {
                                where += " AND ";
                            }
                            where += $"{firstIndex} = {secondIndex}";
                            if (i != order.Count - 2)
                            {
                                selectMatch += ",\r\n";
                            }
                        }
                        selectMatch += "\r\n";
                    }
                    if (idShortPath[0].Contains("%"))
                    {
                        with = "%";
                        var shortestPath = idShortPath[order[0]];
                        var lastDotIndex = shortestPath.LastIndexOf('.');
                        shortestPath = (lastDotIndex >= 0) ? shortestPath.Substring(0, lastDotIndex + 1) : shortestPath;
                        var shortestPathSplit = shortestPath.Split(with).ToList();
                        var segments = shortestPathSplit;

                        for (var p = 0; p < idShortPath.Count; p++)
                        {
                            for (var s = 0; s < segments.Count - 1; s++)
                            {
                                var startSegment = segments[s];
                                var endSegment = segments[s + 1];
                                var alias = $"Part{matchCount}_{p + 1}_{s + 1}";
                                var sql = $"substr(smePath{p + 1}.IdShortPath, instr(smePath{p + 1}.IdShortPath, '{startSegment}') + length('{startSegment}'),\r\n" +
                                   $"instr(smePath{p + 1}.IdShortPath, '{endSegment}') - (instr(smePath{p + 1}.IdShortPath, '{startSegment}') + length('{startSegment}'))) AS {alias}";
                                selectMatch += sql;
                                if (s != segments.Count - 2 || p != idShortPath.Count - 1)
                                {
                                    selectMatch += ",";
                                }
                                selectMatch += "\r\n";
                            }
                        }
                        for (var p = 0; p < idShortPath.Count - 1; p++)
                        {
                            for (var s = 0; s < segments.Count - 1; s++)
                            {
                                if (where != "")
                                {
                                    where += " AND ";
                                }
                                where += $"Part{matchCount}_{p + 1}_{s + 1} = Part{matchCount}_{p + 2}_{s + 1}";
                            }
                        }
                    }
                    for (var i = 0; i < order.Count; i++)
                    {
                        if (where != "")
                        {
                            where += " AND ";
                        }
                        where += $"path{matchOffset + order[i] + 1} = 1";
                    }
                    whereMatch.Add($"({where})");
                    matchOffset += idShortPath.Count;
                }
            }

            pathAllAas = pathAllAas.Replace("$$match$$", "");
        }

        if (withPathSme)
        {
            var split = pathAllAas.Split("$$");
            List<string> pathList = [];
            List<string> fieldList = [];
            List<string> opList = [];
            var path = false;
            var field = false;
            var op = false;
            foreach (var s in split)
            {
                if (op)
                {
                    opList.Add(s);
                    op = false;
                }
                if (field)
                {
                    fieldList.Add(s);
                    field = false;
                    op = true;
                }
                if (path)
                {
                    pathList.Add(s);
                    path = false;
                    field = true;
                }
                if (s == "path")
                {
                    path = true;
                }
            }
            var pathAll = pathAllCondition.Copy();
            for (var i = 0; i < pathList.Count; i++)
            {
                var splitField = fieldList[i].Split("#");
                var idShort = pathList[i].Split(".").Last();
                var allCondition = $"sme.idShort == \"{idShort}\" && sme.idShortPath == \"{pathList[i]}\"";
                if (splitField.Length > 1 && splitField[1] != "value")
                {
                    allCondition += $" && sme.{splitField[1]}{opList[i]}";
                }
                else
                {
                    allCondition = $"{allCondition} && {splitField[0]}{opList[i]}";
                }
                var replace = $"$$tag$$path$${pathList[i]}$${fieldList[i]}$${opList[i]}$$";
                pathAllCondition = pathAllCondition.Replace(replace, $"$$path{i}$$");
                pathConditions.Add(allCondition);
            }
        }

        aasTable = db.AASSets;
        aasTable = restrictAAS ? aasTable.Where(conditionsExpression["aas"]) : aasTable;

        smTable = db.SMSets;
        smTable = restrictSM ? smTable.Where(conditionsExpression["sm"]) : smTable;

        smeTable = db.SMESets;
        if (pathConditions.Count == 0)
        {
            smeTable = restrictSME ? smeTable.Where(conditionsExpression["sme"]) : smeTable;
        }
        valueTable = db.ValueSets;
        if (pathConditions.Count == 0)
        {
            valueTable = restrictValue ? valueTable.Where(conditionsExpression["value"].Replace("DValue", "NValue")) : null;

            if (valueTable == null
                && restrictSME)
            {
                smeTable = null;
            }
        }

        var conditionAll = "true";
        if (!withMatch && conditionsExpression.TryGetValue("all-aas", out _))
        {
            conditionAll = conditionsExpression["all-aas"];
        }

        var rawAas = aasTable?.ToQueryString();
        var whereAas = rawAas?.Split("WHERE ").Last();
        var rawSm = smTable?.ToQueryString();
        var whereSm = rawSm?.Split("WHERE ").Last();
        //var rawSme = smeTable?.ToQueryString();
        //var whereSme = rawSme?.Split("WHERE").Last();
        //var rawValue = valueTable?.ToQueryString();
        //var whereValue = rawValue?.Split("WHERE").Last();

        IQueryable<joinAll>? result = null;

        if (isWithAASTable)
        {
            if (resultType == ResultType.AssetAdministrationShell
                && smTable == null
                && smeTable == null
                && valueTable == null)
            {
                result = aasTable.Select(x => new joinAll
                {
                    aas = x,
                    AASId = x.Id
                });
            }
            else
            {
                result = aasTable
                    .Join(db.SMRefSets,
                        aas => aas.Id,
                        r => r.AASId,
                        (aas, r) => new { aas, r })
                        .Join(smTable,
                              x => x.r.Identifier,
                              sm => sm.Identifier,
                              (x, sm) => new { x.aas, sm })
                        .Select(x => new joinAll
                        {
                            aas = x.aas,
                            sm = x.sm,
                            SMId = x.sm.Id,
                            AASId = x.aas.Id
                        });
            }
        }
        else
        {
            result = smTable.Select(sm => new joinAll
            {
                sm = sm,
                SMId = sm.Id
            });
        }

        if (valueTable != null
            || smeTable != null)
        {
            result = result.Join(smeTable,
            r => r.sm.Id,
            sme => sme.SMId,
            (r, sme) => new joinAll
            {
                aas = r.aas,
                sm = r.sm,
                sme = sme,
                SMId = r.sm.Id,
                AASId = r.aas.Id
            });
        }

        if (valueTable != null)
        {
            result = result.Join(valueTable,
            r => r.sme.Id,
            v => v.SMEId,
            (r, v) => new joinAll
            {
                SMId = r.SMId,
                AASId = r.aas.Id,
                aas = r.aas,
                sm = r.sm,
                sme = r.sme,
                svalue = v.SValue,
                mvalue = v.NValue,
                dtvalue = v.DTValue,
            });
        }

        var raw = "";
        var rawBase = "";
        if (pathConditions.Count != 0)
        {
            var selectAas = "(\r\n  SELECT Id";
            foreach (var aasField in aasFields)
            {
                selectAas += $", {aasField}";
            }
            selectAas += "\r\n  FROM AASSets\r\n";
            if (whereAas != null && !whereAas.StartsWith("SELECT"))
            {
                var whereAas2 = whereAas.Replace("\"a\".", "");
                selectAas += $"WHERE {whereAas2}\r\n";
            }
            selectAas += ")";

            var selectSm = "(\r\n  SELECT Id, Identifier";
            foreach (var smField in smFields)
            {
                if (smField != "Identifier")
                {
                    selectSm += $", {smField}";
                }
            }
            selectSm += "\r\n  FROM SMSets\r\n";
            if (whereSm != null && !whereSm.StartsWith("SELECT"))
            {
                var whereSm2 = whereSm.Replace("\"s\".", "");
                whereSm2 = whereSm2.Replace("\"s0\".", "");

                selectSm += $"WHERE {whereSm2}\r\n";
            }
            selectSm += ")";

            if (!withRecursive && !withMatch)
            {
                rawBase = "SELECT ";

                if (resultType == ResultType.AssetAdministrationShell)
                {
                    rawBase += "a.Id\r\n";
                }
                else
                {
                    rawBase += "t.Id\r\n";
                }

                if (isWithAASTable)
                {
                    rawBase += $"FROM {selectAas} AS a\r\n";
                    rawBase += "INNER JOIN SMRefSets AS sx ON a.Id = sx.AASId\r\n";
                    rawBase += $"INNER JOIN {selectSm} AS t ON sx.Identifier = t.Identifier\r\n";
                }
                else
                {
                    rawBase += $"FROM {selectSm} AS t\r\n";
                }

                var placeholderSQL = new string[pathConditions.Count];
                var pathAllExists = pathAllCondition.Copy();
                var smIdVariable = result.Select(s => s.SMId).ToQueryString()
                    .Replace("\r", "").Split("\n").First()
                    .Split("SELECT ").Last();

                for (var i = 0; i < pathConditions.Count; i++)
                {
                    var join = "";
                    join += $"LEFT JOIN(\r\nSELECT DISTINCT sme.SMId AS SMId\r\n";
                    join += $"FROM ValueSets AS v\r\n";
                    join += $"JOIN SMESets AS sme ON sme.Id = v.SMEId\r\n";

                    var where = "";

                    if (isWithAASTable)
                    {
                        var convertPathCondition = result.Where(pathConditions[i]).Select(r => r.AASId);
                        var convertPathConditionSQL = convertPathCondition.ToQueryString();
                        where = convertPathConditionSQL.Split("WHERE ").Last();
                        where = where.Replace("\"s1\".", "sme.").Replace("\"v\".", "v.");
                    }
                    else
                    {
                        var convertPathCondition = result.Where(pathConditions[i]).Select(r => r.SMId);
                        var convertPathConditionSQL = convertPathCondition.ToQueryString();
                        where = convertPathConditionSQL.Split("WHERE ").Last();
                        where = where.Replace("\"s0\".", "sme.").Replace("\"v\".", "v.");
                    }

                    var split = where.Split(" AND ");

                    var valueSQL = split[split.Length - 1];

                    join += $"WHERE {valueSQL} ";

                    var smeSQLPath = split[split.Length - 2];

                    if (smeSQLPath.Contains("[]"))
                    {
                        smeSQLPath = smeSQLPath.Replace("[]", "[%]");
                    }
                    if (smeSQLPath.Contains("%"))
                    {
                        smeSQLPath = smeSQLPath.Replace("\"IdShortPath\" = ", "\"IdShortPath\" LIKE ");
                    }

                    var smeSQLIdShort = split[split.Length - 3];
                    if (!smeSQLIdShort.Contains("[]") && !smeSQLIdShort.Contains("%"))
                    {
                        join += $"AND {smeSQLIdShort} ";
                    }

                    join += $"AND {smeSQLPath}\r\n";
                    join += $") AS p{i + 1} ON p{i + 1}.SMId = t.Id\r\n";

                    pathAllExists = pathAllExists.Replace($"$$path{i}$$", $"Math.Abs(SMId) == 20{i}");
                    placeholderSQL[i] = $"abs({smIdVariable}) = 20{i}";

                    rawBase += join;
                }

                rawBase += "WHERE ";

                // convert complete condition with placeholders
                var convertCondition = result.Where(pathAllExists);
                var convertConditionSQL = convertCondition.ToQueryString();
                convertConditionSQL = convertConditionSQL.Split("WHERE ").Last();

                if (!convertConditionSQL.StartsWith("SELECT"))
                {
                    for (var i = 0; i < pathConditions.Count; i++)
                    {
                        convertConditionSQL = convertConditionSQL.Copy().Replace(placeholderSQL[i], $"p{i + 1}.SMId IS NOT NULL");
                    }

                    convertConditionSQL = convertConditionSQL.Replace("\"s\".", "\"t\".");
                    convertConditionSQL = convertConditionSQL.Replace("\"s0\".", "\"t\".");
                    rawBase += convertConditionSQL;
                }
            }
            else
            {
                var placeholderSQL = new string[pathConditions.Count];
                var exists = new string[pathConditions.Count];
                var pathConditionsSQL = new string[pathConditions.Count];
                var pathAllConditionsSQL = new string[pathConditions.Count];
                var smeSQLPathArray = new string[pathConditions.Count];
                var smeSQLIdShortArray = new string[pathConditions.Count];
                var valueSQL = new string[pathConditions.Count];
                var pathAllExists = pathAllCondition.Copy();
                var smIdVariable = result.Select(s => s.SMId).ToQueryString()
                    .Replace("\r", "").Split("\n").First()
                    .Split("SELECT ").Last();

                for (var i = 0; i < pathConditions.Count; i++)
                {
                    var ii = i + 1;

                    var where = "";

                    if (isWithAASTable)
                    {
                        var convertPathCondition = result.Where(pathConditions[i]).Select(r => r.AASId);
                        var convertPathConditionSQL = convertPathCondition.ToQueryString();
                        where = convertPathConditionSQL.Split("WHERE ").Last();
                        where = where.Replace("\"s1\".", $"smePath{ii}.").Replace("\"v\".", $"vPath{ii}.");
                    }
                    else
                    {
                        var convertPathCondition = result.Where(pathConditions[i]).Select(r => r.SMId);
                        var convertPathConditionSQL = convertPathCondition.ToQueryString();
                        where = convertPathConditionSQL.Split("WHERE ").Last();
                        where = where.Replace("\"s0\".", $"smePath{ii}.").Replace("\"v\".", $"vPath{ii}.");
                    }

                    pathConditionsSQL[i] = where;
                    var split = where.Split(" AND ");
                    smeSQLPathArray[i] = split[split.Length - 2];
                    smeSQLIdShortArray[i] = split[split.Length - 3];
                    if (smeSQLPathArray[i].Contains("[]"))
                    {
                        smeSQLPathArray[i] = smeSQLPathArray[i].Replace("[]", "[%]");
                    }
                    if (smeSQLPathArray[i].Contains("%"))
                    {
                        smeSQLPathArray[i] = smeSQLPathArray[i].Replace("\"IdShortPath\" = ", "\"IdShortPath\" LIKE ");
                    }
                    valueSQL[i] = split[split.Length - 1];

                    pathAllExists = pathAllExists.Replace($"$$path{i}$$", $"Math.Abs(SMId) == 20{i}");
                    placeholderSQL[i] = $"abs({smIdVariable}) = 20{i}";
                }

                // convert complete condition with placeholders
                var convertCondition = result.Where(pathAllExists);
                var convertConditionSQL = convertCondition.ToQueryString();
                convertConditionSQL = convertConditionSQL.Split("WHERE ").Last();
                if (convertConditionSQL.StartsWith("SELECT"))
                {
                    convertConditionSQL = "";
                }

                for (var i = 0; i < pathConditions.Count; i++)
                {
                    pathAllConditionsSQL[i] = convertConditionSQL.Copy();
                    for (var j = 0; j < pathConditions.Count; j++)
                    {
                        var replace = "false";
                        if (j == i)
                        {
                            replace = "true";
                        }
                        pathAllConditionsSQL[i] = pathAllConditionsSQL[i].Replace(placeholderSQL[j], replace);
                    }
                }

                var caseWhen = "";
                var join = "";
                var wherePath = convertConditionSQL.Copy();
                // var wherePath = "";

                for (var i = 0; i < pathConditions.Count; i++)
                {
                    var ii = i + 1;

                    caseWhen += $"CASE WHEN {valueSQL[i]} THEN 1 ELSE 0 END AS path{ii}";
                    if (i != pathConditions.Count - 1)
                    {
                        caseWhen += ",";
                    }
                    caseWhen += "\r\n";

                    join += $"LEFT JOIN SMESets AS smePath{ii} ON aasSm.SmId = smePath{ii}.SMId ";

                    if (!smeSQLIdShortArray[i].Contains("[]") && !smeSQLIdShortArray[i].Contains("%"))
                    {
                        join += $"AND {smeSQLIdShortArray[i]}";
                    }

                    join += $"AND {smeSQLPathArray[i]}\r\n";
                    join += $"LEFT JOIN ValueSets AS vPath{ii} ON smePath{ii}.Id = vPath{ii}.SMEId AND {valueSQL[i]}\r\n";

                    wherePath = wherePath.Replace(placeholderSQL[i], $"path{ii} = 1");
                }

                var smeSelect = "";
                var smePrefix = "";
                if (wherePath.Contains("\"s0\"."))
                {
                    smePrefix = "\"s0\".";
                }
                if (wherePath.Contains("\"s1\"."))
                {
                    smePrefix = "\"s1\".";
                }

                if (smePrefix != "")
                {
                    var ii = 1;
                    var split1 = wherePath.Split(smePrefix);
                    foreach (var s1 in split1)
                    {
                        var split2 = s1.Split(" ");

                        // if (split2[0] == "\"IdShort\"" || split2[0] == "\"SemanticId\"")
                        if (smeFields.Contains($"{split2[0].Replace("\"", "")}"))
                        {
                            var s = split2[0] + " " + split2[1] + " " + split2[2];
                            s = s.Replace(")", "");

                            if (split2.Length >= 7 && split2[3] == "AND")
                            {
                                var v = split2[4] + " " + split2[5] + " " + split2[6];
                                v = v.Replace(")", "").Replace($"\"v\".", "");

                                caseWhen += $", CASE WHEN sme{ii}.{s} THEN 1 ELSE 0 END AS sme{ii}\r\n";
                                join += $"LEFT JOIN SMESets AS sme{ii} ON aasSm.SmId = sme{ii}.SMId AND sme{ii}.{s}\r\n";

                                wherePath = wherePath.Replace($"{smePrefix}{s} AND", "");

                                caseWhen += $", CASE WHEN valueSme{ii}.{v} THEN 1 ELSE 0 END AS valueSme{ii}\r\n";
                                join += $"LEFT JOIN ValueSets AS valueSme{ii} ON sme{ii}.Id = valueSme{ii}.SMEId AND valueSme{ii}.{v}\r\n";

                                wherePath = wherePath.Replace($"\"v\".{v}", $"valueSme{ii} = 1");

                                ii++;
                            }
                            else
                            {
                                caseWhen += $", CASE WHEN sme{ii}.{s} THEN 1 ELSE 0 END AS sme{ii}\r\n";
                                join += $"LEFT JOIN SMESets AS sme{ii} ON aasSm.SmId = sme{ii}.SMId AND sme{ii}.{s}\r\n";

                                wherePath = wherePath.Replace($"{smePrefix}{s}", $"sme{ii} = 1");

                                ii++;
                            }
                        }
                    }
                }

                if (wherePath.Contains("\"v\"."))
                {
                    join += $"LEFT JOIN SMESets AS sme ON aasSM.SmId = sme.SMId\r\n";

                    var ii = 1;
                    var split1 = wherePath.Split("\"v\".");
                    foreach (var s1 in split1)
                    {
                        var split2 = s1.Split(" ");
                        if (split2[0] == "\"SValue\"" || split2[0] == "\"NValue\"" || split2[0] == "\"DTValue\"")
                        {
                            var v = split2[0] + " " + split2[1] + " " + split2[2];
                            v = v.Replace(")", "");

                            caseWhen += $",CASE WHEN value{ii}.{v} THEN 1 ELSE 0 END AS value{ii}\r\n";
                            join += $"LEFT JOIN ValueSets AS value{ii} ON sme.Id = value{ii}.SMEId AND value{ii}.{v}\r\n";

                            wherePath = wherePath.Replace($"\"v\".{v}", $"value{ii} = 1");

                            ii++;
                        }
                    }
                }

                if (selectMatch != "")
                {
                    caseWhen += "," + selectMatch;
                }

                raw = "SELECT DISTINCT ";

                if (resultType == ResultType.AssetAdministrationShell)
                {
                    raw += "AasId AS Id\r\n";
                }
                else
                {
                    raw += "SmId AS Id\r\n";
                }

                raw += $"FROM (\r\nSELECT aasSm.SmId, ";
                foreach (var smField in smFields)
                {
                    raw += $"aasSm.Sm{smField}, ";
                }

                if (isWithAASTable)
                {
                    raw += "aasSm.AasId, ";
                    foreach (var aasField in aasFields)
                    {
                        raw += $"aasSm.Aas{aasField}, ";
                    }
                }
                raw += $"{smeSelect}\r\n";
                raw += caseWhen;

                if (isWithAASTable)
                {
                    raw += "FROM(\r\nSELECT s0.Id AS SmId, a.Id AS AasId ";
                    foreach (var smField in smFields)
                    {
                        raw += $", s0.{smField} AS Sm{smField}";
                    }
                    foreach (var aasField in aasFields)
                    {
                        raw += $", a.{aasField} AS Aas{aasField}";
                    }
                    raw += "\r\n";

                    /*
                    if (whereAas != null && !whereAas.StartsWith("SELECT"))
                    {
                        whereAas = whereAas.Replace("\"a\".", "");

                        raw += "FROM (\r\n  SELECT Id";
                        foreach (var aasField in aasFields)
                        {
                            raw += $", {aasField}";
                        }
                        raw += "\r\n  FROM AASSets\r\n";
                        raw += $"WHERE {whereAas}\r\n";
                        raw += ") AS a\r\n";
                    }
                    else
                    {
                        raw += "FROM AASSets AS a\r\n";
                    }
                    */
                    raw += $"FROM {selectAas} as a\r\n";

                    raw += "INNER JOIN SMRefSets AS sx ON a.Id = sx.AASId\r\n";
                    raw += $"INNER JOIN {selectSm} AS s0 ON sx.Identifier = s0.Identifier\r\n";
                }
                else
                {
                    raw += "FROM(\r\nSELECT Id AS SmId";
                    foreach (var smField in smFields)
                    {
                        raw += $", {smField}";
                    }
                    raw += "\r\nFROM SMSets\r\n";
                    // raw += $"FROM {selectSm} AS s0\r\n";
                }

                /*
                if (!isWithAASTable)
                {
                    if (whereSm != null && !whereSm.StartsWith("SELECT"))
                    {
                        whereSm = whereSm.Replace("\"s\".", "");
                    }
                    else
                    {
                        whereSm = "";
                    }

                    if (whereAas != null && !whereAas.StartsWith("SELECT"))
                    {
                        whereAas = whereAas.Replace("\"s\".", "");
                    }
                    else
                    {
                        whereAas = "";
                    }

                    var whereAasSm = "";
                    if (whereAas != "" || whereSm != "")
                    {
                        if (whereAas != "")
                        {
                            whereAasSm = $"({whereAas})";
                        }
                        if (whereSm != "")
                        {
                            if (whereAasSm == "")
                            {
                                whereAasSm = $"({whereSm})";
                            }
                            else
                            {
                                whereAasSm += $" AND ({whereSm})";
                            }
                        }

                        raw += $"WHERE ({whereAasSm})\r\n";
                    }
                }
                else if (whereSm != null && !whereSm.StartsWith("SELECT"))
                {
                    whereSm = whereSm.Replace("\"s\".", "\"s0\".");

                    raw += $"AND ({whereSm})\r\n";
                }
                */

                raw += ") AS aasSm\r\n";
                raw += join;
                raw += ") AS aasAll\r\n";
                rawBase = raw;
                if (whereMatch.Count != 0)
                {
                    for (var i = 0; i < whereMatch.Count; i++)
                    {
                        wherePath = wherePath.Replace($"abs({smIdVariable}) = 10{i}", whereMatch[i]);
                    }
                }

                var smIdVariableShortened = smIdVariable.Split(".").First();

                wherePath = wherePath.Replace($"{smIdVariableShortened}.", "\"aasAll\".");

                foreach (var smField in smFields)
                {
                    wherePath = wherePath.Replace($"\"aasAll\".\"{smField}\"", $"\"aasAll\".\"Sm{smField}\"");
                }
                foreach (var aasField in aasFields)
                {
                    wherePath = wherePath.Replace($"\"a\".\"{aasField}\"", $"\"aasAll\".\"Aas{aasField}\"");
                }

                if (wherePath != "")
                {
                    rawBase += $"WHERE {wherePath}\r\n";
                }
            }
        }
        else
        {
            switch (resultType)
            {
                case ResultType.AssetAdministrationShell:
                    rawBase = result.Where(conditionAll).Select(r => r.AASId).Distinct().ToQueryString();
                    break;
                case ResultType.Submodel:
                    rawBase = result.Where(conditionAll).Select(r => r.SMId).Distinct().ToQueryString();
                    break;
                case ResultType.SubmodelValue:
                case ResultType.SubmodelElement:
                default:
                    throw new NotImplementedException();
            }
        }

        raw = rawBase;
        if (raw.Contains(" LIKE "))
        {
            raw = raw.Replace(" LIKE ", " GLOB ");
            raw = raw.Replace("%", "*");
        }

        var qpRaw = GetQueryPlan(db, raw);

        IQueryable<SMSetIdResult> resultSMId = null;
        resultSMId = db.Set<SMSetIdResult>()
               .FromSqlRaw(raw)
               .AsQueryable();

        var smRawSQL = resultSMId.ToQueryString();
        var qp = GetQueryPlan(db, smRawSQL);

        return resultSMId.Select(r => r.Id).Skip(pageFrom).Take(pageSize).ToList();
    }

    private static List<int> CombineTablesLEFT1(
        AasContext db,
        Dictionary<string, string>? conditionsExpression,
        int pageFrom,
        int pageSize,
        ResultType resultType,
        bool consolidate
        )
    {
        IQueryable<AASSet>? aasTable = null;
        IQueryable<SMSet>? smTable = null;
        IQueryable<SMESet>? smeTable = null;
        IQueryable<ValueSet>? valueTable = null;

        var restrictAAS = conditionsExpression.TryGetValue("aas", out var value) && value != "" && value.ToLower() != "true";
        var restrictSM = conditionsExpression.TryGetValue("sm", out value) && value != "" && value.ToLower() != "true";
        var restrictSME = conditionsExpression.TryGetValue("sme", out value) && value != "" && value.ToLower() != "true";
        var restrictValue = conditionsExpression.TryGetValue("value", out value) && value != "" && value.ToLower() != "true";

        var aasFields = new List<string>();
        var smFields = new List<string>();
        var smeFields = new List<string>();

        if (conditionsExpression.TryGetValue("all", out value) && value != "" && value.ToLower() != "true")
        {
            aasFields = GetFields("aas.", value);
            smFields = GetFields("sm.", value);
            smeFields = GetFields("sme.", value);
        }

        var withPathSme = false;
        var withMatch = false;
        var withRecursive = false;
        var pathSME = "";
        var pathAllCondition = "";
        var pathAllConditionRaw = "";

        var aasExistInCondition = conditionsExpression.TryGetValue("all", out value) && value.Contains("aas.");

        bool isWithAASTable = restrictAAS || aasExistInCondition || resultType == ResultType.AssetAdministrationShell;

        if (conditionsExpression.TryGetValue("path-sme", out pathSME))
        {
            withPathSme = true;
            if (!conditionsExpression.TryGetValue("path-all", out pathAllCondition))
            {
                withPathSme = false;
            }
            else
            {
                withRecursive = pathAllCondition.Contains("sme.") || pathAllCondition.Contains("svalue")
                    || pathAllCondition.Contains("mvalue") || pathAllCondition.Contains("dtvalue");
            }
            if (conditionsExpression.TryGetValue("path-raw", out pathAllConditionRaw))
            {
                if (pathAllConditionRaw.Contains("$$match"))
                {
                    withMatch = true;
                }
            }
        }

        if (!conditionsExpression.TryGetValue("path-raw", out var pathAllAas))
        {
            pathAllAas = "";
        }
        List<string> pathConditions = [];

        var selectMatch = "";
        List<string> whereMatch = [];
        List<string> conditionMatch = [];
        if (withMatch)
        {
            var splitMatch = pathAllConditionRaw.Split("$$match$$");
            var matchCount = 0;
            var matchOffset = 0;
            pathAllCondition = "";
            List<string> matchPathList = [];
            for (var iMatch = 0; iMatch < splitMatch.Count(); iMatch++)
            {
                var match = splitMatch[iMatch];

                if (!match.Contains("$$$$tag$$path$$"))
                {
                    pathAllCondition += match;
                }
                else
                {
                    pathAllCondition += $"Math.Abs(SmId) == 10{matchCount}";
                    matchCount++;
                    if (selectMatch != "")
                    {
                        selectMatch += ",";
                    }

                    List<int> order = [];
                    List<int> count = [];
                    List<string> idShortPath = [];
                    List<string> idShortPathLast = [];
                    List<string> idShort = [];
                    List<string> field = [];
                    var split = match.Split("$$tag$$path$$");
                    var with = "";
                    for (var i = 1; i < split.Length; i++)
                    {
                        var firstTag = split[i];
                        var split2 = firstTag.Split("$$");
                        order.Add(i - 1);
                        idShortPath.Add(split2[0]);
                        with = "";
                        if (split2[0].Contains("[]"))
                        {
                            with = "[]";
                        }
                        if (split2[0].Contains("%"))
                        {
                            with = "%";
                        }
                        idShortPathLast.Add(split2[0].Split(with).Last());
                        count.Add(split2[0].Count(c => c == (with == "[]" ? '[' : '%')));
                        idShort.Add(split2[0].Split(".").Last());

                        var c = split2[1] + split2[2];
                        var v = db.ValueSets.Where(c).ToQueryString();
                        var sql = v.Split("WHERE ").Last();
                        field.Add(sql);
                    }
                    order = order.OrderBy(x => count[x]).ToList();

                    with = "";
                    var where = "";
                    if (idShortPath[0].Contains("[]"))
                    {
                        with = "[]";
                        var selectSegment = "";
                        for (var i = 0; i < order.Count - 1; i++)
                        {
                            var firstSplit = idShortPath[order[i]].Split("[]").ToList();
                            var secondSplit = idShortPath[order[i + 1]].Split("[]").ToList();
                            var c = count[order[i]];
                            var firstStartSegment = firstSplit[c - 1];
                            var firstEndSegment = firstSplit[c];
                            var firstIndex = $"Part{matchCount}_{i + 1}_1";
                            selectSegment += $"substr(smePath{order[i] + 1}.IdShortPath, instr(smePath{order[i] + 1}.IdShortPath, '{firstStartSegment}') + length('{firstStartSegment}'),\r\n" +
                               $"instr(smePath{order[i] + 1}.IdShortPath, '{firstEndSegment}') - (instr(smePath{order[i] + 1}.IdShortPath, '{firstStartSegment}') + length('{firstStartSegment}'))) AS {firstIndex},\r\n";
                            var secondStartSegment = secondSplit[c - 1];
                            var secondEndSegment = secondSplit[c];
                            var secondIndex = $"Part{matchCount}_{i + 1}_2";
                            selectSegment += $"substr(smePath{order[i + 1] + 1}.IdShortPath, instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondStartSegment}') + length('{secondStartSegment}'),\r\n" +
                               $"instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondEndSegment}') - (instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondStartSegment}') + length('{secondStartSegment}'))) AS {secondIndex}";
                            if (where != "")
                            {
                                where += " AND ";
                            }
                            where += $"{firstIndex} = {secondIndex}";
                            if (i != order.Count - 2)
                            {
                                selectSegment += ",\r\n";
                            }
                        }
                        selectMatch += $"{selectSegment}\r\n";
                        conditionMatch.Add(selectSegment.TrimEnd(','));
                    }
                    if (idShortPath[0].Contains('%'))
                    {
                        with = "%";
                        var shortestPath = idShortPath[order[0]];
                        var lastDotIndex = shortestPath.LastIndexOf('.');
                        shortestPath = (lastDotIndex >= 0) ? shortestPath.Substring(0, lastDotIndex + 1) : shortestPath;
                        var shortestPathSplit = shortestPath.Split(with).ToList();
                        var segments = shortestPathSplit;

                        for (var p = 0; p < idShortPath.Count; p++)
                        {
                            var selectSegment = "";
                            for (var s = 0; s < segments.Count - 1; s++)
                            {
                                var startSegment = segments[s];
                                var endSegment = segments[s + 1];
                                var alias = $"Part{matchCount}_{p + 1}_{s + 1}";
                                var sql = $"substr(smePath{p + 1}.IdShortPath, instr(smePath{p + 1}.IdShortPath, '{startSegment}') + length('{startSegment}'),\r\n" +
                                   $"instr(smePath{p + 1}.IdShortPath, '{endSegment}') - (instr(smePath{p + 1}.IdShortPath, '{startSegment}') + length('{startSegment}'))) AS {alias}";
                                selectSegment += sql;
                                if (s != segments.Count - 2 || p != idShortPath.Count - 1)
                                {
                                    selectSegment += ",";
                                }
                            }
                            selectMatch += $"{selectSegment}\r\n";
                            conditionMatch.Add(selectSegment.TrimEnd(','));
                        }
                        for (var p = 0; p < idShortPath.Count - 1; p++)
                        {
                            for (var s = 0; s < segments.Count - 1; s++)
                            {
                                if (where != "")
                                {
                                    where += " AND ";
                                }
                                where += $"Part{matchCount}_{p + 1}_{s + 1} = Part{matchCount}_{p + 2}_{s + 1}";
                            }
                        }
                    }
                    for (var i = 0; i < order.Count; i++)
                    {
                        if (where != "")
                        {
                            where = " AND " + where;
                        }
                        where = $"path{matchOffset + order[i] + 1} = 1" + where;
                    }
                    whereMatch.Add($"({where})");
                    matchOffset += idShortPath.Count;
                }
            }

            pathAllAas = pathAllAas.Replace("$$match$$", "");
        }

        if (withPathSme)
        {
            var split = pathAllAas.Split("$$");
            List<string> pathList = [];
            List<string> fieldList = [];
            List<string> opList = [];
            var path = false;
            var field = false;
            var op = false;
            foreach (var s in split)
            {
                if (op)
                {
                    opList.Add(s);
                    op = false;
                }
                if (field)
                {
                    fieldList.Add(s);
                    field = false;
                    op = true;
                }
                if (path)
                {
                    pathList.Add(s);
                    path = false;
                    field = true;
                }
                if (s == "path")
                {
                    path = true;
                }
            }
            var pathAll = pathAllCondition.Copy();
            for (var i = 0; i < pathList.Count; i++)
            {
                var splitField = fieldList[i].Split("#");
                var idShort = pathList[i].Split(".").Last();
                var allCondition = $"sme.idShort == \"{idShort}\" && sme.idShortPath == \"{pathList[i]}\"";
                if (splitField.Length > 1 && splitField[1] != "value")
                {
                    allCondition += $" && sme.{splitField[1]}{opList[i]}";
                }
                else
                {
                    allCondition = $"{allCondition} && {splitField[0]}{opList[i]}";
                }
                var replace = $"$$tag$$path$${pathList[i]}$${fieldList[i]}$${opList[i]}$$";
                pathAllCondition = pathAllCondition.Replace(replace, $"$$path{i}$$");
                pathConditions.Add(allCondition);
            }
        }

        aasTable = db.AASSets;
        aasTable = restrictAAS ? aasTable.Where(conditionsExpression["aas"]) : aasTable;

        smTable = db.SMSets;
        smTable = restrictSM ? smTable.Where(conditionsExpression["sm"]) : smTable;

        smeTable = db.SMESets;
        if (pathConditions.Count == 0)
        {
            smeTable = restrictSME ? smeTable.Where(conditionsExpression["sme"]) : smeTable;
        }
        valueTable = db.ValueSets;
        if (pathConditions.Count == 0)
        {
            valueTable = restrictValue ? valueTable.Where(conditionsExpression["value"].Replace("DValue", "NValue")) : null;

            if (valueTable == null
                && restrictSME)
            {
                smeTable = null;
            }
        }

        var conditionAll = "true";
        if (!withMatch && conditionsExpression.TryGetValue("all-aas", out _))
        {
            conditionAll = conditionsExpression["all-aas"];
        }
        if (pathAllCondition == "")
        {
            pathAllCondition = conditionAll;
        }

        var rawAas = aasTable?.ToQueryString();
        var whereAas = rawAas?.Split("WHERE ").Last();
        var rawSm = smTable?.ToQueryString();
        var whereSm = rawSm?.Split("WHERE ").Last();

        IQueryable<joinAll>? convert = null;

        convert = db.AASSets
            .Join(db.SMRefSets,
                aas => aas.Id,
                r => r.AASId,
                (aas, r) => new { aas, r })
            .Join(db.SMSets,
                x => x.r.Identifier,
                sm => sm.Identifier,
                (x, sm) => new { x.aas, sm })
            .Join(db.SMESets,
                x => x.sm.Id,
                sme => sme.SMId,
                (x, sme) => new { x.aas, x.sm, sme })
            .Join(db.ValueSets,
                x => x.sme.Id,
                v => v.SMEId,
                (x, v) => new joinAll
                {
                    aas = x.aas,
                    sm = x.sm,
                    SMId = x.sm.Id,
                    AASId = x.aas.Id,
                    sme = x.sme,
                    svalue = v.SValue,
                    mvalue = v.NValue,
                    dtvalue = v.DTValue
                });

        var convertSQL = convert.ToQueryString();
        var regex = new Regex("\"(?<table>[^\"]+)\"\\s+AS\\s+\"(?<sql>[^\"]+)\"", RegexOptions.IgnoreCase);
        var convertMap = new Dictionary<string, string>();
        foreach (Match match in regex.Matches(convertSQL))
        {
            var table = match.Groups["table"].Value;
            var sql = match.Groups["sql"].Value;

            convertMap[table] = sql;
        }
        var aasPrefix = convertMap["AASSets"];
        var smPrefix = convertMap["SMSets"];
        var smePrefix = convertMap["SMESets"];
        var valuePrefix = convertMap["ValueSets"];

        var raw = "";
        var rawBase = "";
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
        if (whereAas != null && !whereAas.StartsWith("SELECT"))
        {
            var whereAas2 = whereAas.Replace("\"a\".", "");
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
        if (whereSm != null && !whereSm.StartsWith("SELECT"))
        {
            var whereSm2 = whereSm.Replace("\"s\".", "");
            whereSm2 = whereSm2.Replace("\"s0\".", "");

            selectSm += $"WHERE {whereSm2}\r\n";
        }
        selectSm += ")";

        rawBase = "SELECT DISTINCT ";

        if (resultType == ResultType.AssetAdministrationShell)
        {
            rawBase += "a.Id\r\n";
        }
        else
        {
            rawBase += "t.Id\r\n";
        }

        if (isWithAASTable)
        {
            rawBase += $"FROM {selectAas} AS a\r\n";
            rawBase += "INNER JOIN SMRefSets AS sx ON a.Id = sx.AASId\r\n";
            rawBase += $"INNER JOIN {selectSm} AS t ON sx.Identifier = t.Identifier\r\n";
        }
        else
        {
            rawBase += $"FROM {selectSm} AS t\r\n";
        }

        // rawBase += "INNER JOIN \"SMESets\" AS \"s0\" ON \"t\".\"Id\" = \"s0\".\"SMId\"\r\n";
        // rawBase += "INNER JOIN \"ValueSets\" AS \"v\" ON \"s0\".\"Id\" = \"v\".\"SMEId\"\r\n";

        var placeholderSQL = new string[pathConditions.Count];
        var pathAllExists = pathAllCondition.Copy();
        //var smIdVariable = convert.Select(s => s.SMId).ToQueryString()
        //    .Replace("\r", "").Split("\n").First()
        //    .Split("SELECT ").Last();
        var smIdVariable = $"\"{convertMap["SMSets"]}\".\"Id\"";

        for (var i = 0; i < pathConditions.Count; i++)
        {
            var join = "";
            join += $"LEFT JOIN(\r\n";
            join += $"SELECT sme.SMId AS SMId";
            if (i < conditionMatch.Count)
            {
                var c = conditionMatch[i];
                c = c.Replace($"smePath{i + 1}", "sme");
                join += ",\r\n" + c;
            }
            join += "\r\n";
            // join += $"FROM ValueSets AS v\r\n";
            // join += $"JOIN SMESets AS sme ON sme.Id = v.SMEId\r\n";
            join += $"FROM SMESets sme\r\n";
            join += $"LEFT JOIN ValueSets v ON v.SMEId = sme.Id ";

            var where = "";

            if (isWithAASTable)
            {
                var convertPathCondition = convert.Where(pathConditions[i]).Select(r => r.AASId);
                var convertPathConditionSQL = convertPathCondition.ToQueryString();
                where = convertPathConditionSQL.Split("WHERE ").Last();
                where = where.Replace($"\"{smePrefix}\".", "sme.").Replace("\"v\".", "v.");
            }
            else
            {
                var convertPathCondition = convert.Where(pathConditions[i]).Select(r => r.SMId);
                var convertPathConditionSQL = convertPathCondition.ToQueryString();
                where = convertPathConditionSQL.Split("WHERE ").Last();
                where = where.Replace($"\"{smePrefix}\".", "sme.").Replace("\"v\".", "v.");
            }

            var split = where.Split(" AND ");

            var valueSQL = split[split.Length - 1];

            join += $"AND {valueSQL}\r\n";
            join += $"WHERE {valueSQL} ";
            // join += "WHERE True ";

            var smeSQLPath = split[split.Length - 2];
            smeSQLPath = smeSQLPath.Replace($"\"{smePrefix}\".", "sme.");
            if (smeSQLPath.Contains("[]"))
            {
                smeSQLPath = smeSQLPath.Replace("[]", "[%]");
            }
            if (smeSQLPath.Contains("%"))
            {
                // smeSQLPath = smeSQLPath.Replace("\"IdShortPath\" = ", "\"IdShortPath\" LIKE ");
                smeSQLPath = smeSQLPath.Replace("\"IdShortPath\" = ", "\"IdShortPath\" GLOB ");
                smeSQLPath = smeSQLPath.Replace("%", "*");
            }

            var smeSQLIdShort = split[split.Length - 3];
            smeSQLIdShort = smeSQLIdShort.Replace($"\"{smePrefix}\".", "sme.");
            if (!smeSQLIdShort.Contains("[]") && !smeSQLIdShort.Contains("%"))
            {
                join += $"AND {smeSQLIdShort} ";
            }

            join += $"AND {smeSQLPath}\r\n";
            join += $") AS p{i + 1} ON p{i + 1}.SMId = t.Id\r\n";

            pathPrefix.Add($"p{i + 1}");

            pathAllExists = pathAllExists.Replace($"$$path{i}$$", $"Math.Abs(SMId) == 20{i}");
            placeholderSQL[i] = $"abs({smIdVariable}) = 20{i}";
            // placeholderSQL[i] = $"(p{i + 1} IS NOT NULL)";

            rawBase += join;
        }

        // convert complete condition with placeholders
        var convertCondition = convert.Where(pathAllExists);
        var convertConditionSQL = convertCondition.ToQueryString();
        convertConditionSQL = convertConditionSQL.Split("WHERE ").Last();

        if (!convertConditionSQL.StartsWith("SELECT"))
        {
            for (var i = 0; i < pathConditions.Count; i++)
            {
                convertConditionSQL = convertConditionSQL.Copy().Replace(placeholderSQL[i], $"(p{i + 1}.SMId IS NOT NULL)");
            }

            if (whereMatch.Count != 0)
            {
                for (var i = 0; i < whereMatch.Count; i++)
                {
                    convertConditionSQL = convertConditionSQL.Replace($"abs({smIdVariable}) = 10{i}", whereMatch[i]);
                }

                for (var i = 0; i < conditionMatch.Count; i++)
                {
                    convertConditionSQL = convertConditionSQL.Replace($"path{i + 1} = 1", $"(p{i + 1}.SMId IS NOT NULL)");
                }
            }

            convertConditionSQL = convertConditionSQL.Replace($"\"{smPrefix}\".", "\"t\".");

            if (convertConditionSQL.Contains($"\"{smePrefix}\"."))
            {
                var ii = 1;
                var split1 = convertConditionSQL.Split($"\"{smePrefix}\".");
                foreach (var s1 in split1)
                {
                    var split2 = s1.Split(" ");

                    if (smeFields.Contains($"{split2[0].Replace("\"", "")}"))
                    {
                        var s = split2[0] + " " + split2[1] + " " + split2[2];
                        s = s.Replace(")", "");

                        if (split2.Length >= 7 && split2[3] == "AND")
                        {
                            var v = split2[4] + " " + split2[5] + " " + split2[6];
                            if (v.StartsWith('('))
                            {
                                var j = 6;
                                do
                                {
                                    j++;
                                    v += " " + split2[j];
                                }
                                while (!split2[j].EndsWith(')'));
                            }
                            v = v.Replace("(", "").Replace(")", "");
                            var vReplace = v;
                            v = v.Replace($"\"v\".", "");

                            rawBase += "LEFT JOIN(\r\n";
                            rawBase += "SELECT sme.SMId\r\n";
                            rawBase += "FROM ValueSets v\r\n";
                            rawBase += $"LEFT JOIN SMESets sme ON sme.Id = v.SMEId AND sme.{s}\r\n";
                            rawBase += $"WHERE \"v\".{v}\r\n";
                            rawBase += $") AS sme{ii} ON sme{ii}.SMId = t.Id\r\n";

                            convertConditionSQL = convertConditionSQL.Replace($"\"{smePrefix}\".{s} AND {vReplace}", $"(sme{ii}.SMId IS NOT NULL)");

                            pathPrefix.Add($"sme{ii}");

                            ii++;
                        }
                        else
                        {
                            rawBase += "LEFT JOIN(\r\n";
                            rawBase += "SELECT sme.SMId\r\n";
                            rawBase += "FROM SMESets sme\r\n";
                            rawBase += "LEFT JOIN ValueSets v ON sme.Id = v.SMEId\r\n";
                            rawBase += $"WHERE \"sme\".{s}\r\n";
                            rawBase += $") AS sme{ii} ON sme{ii}.SMId = t.Id\r\n";

                            convertConditionSQL = convertConditionSQL.Replace($"\"{smePrefix}\".{s}", $"(sme{ii}.SMId IS NOT NULL)");

                            pathPrefix.Add($"sme{ii}");

                            ii++;
                        }
                    }
                }
            }

            if (convertConditionSQL.Contains($"\"{valuePrefix}\"."))
            {
                var ii = 1;
                var split1 = convertConditionSQL.Split($"\"{valuePrefix}\".");

                for (var i = 0; i < split1.Length; i++)
                {
                    var s1 = split1[i];
                    var split2 = s1.Split(" ");
                    if (split2[0] == "\"SValue\"" || split2[0] == "\"NValue\"" || split2[0] == "\"DTValue\"")
                    {
                        var v = "\"v\"." + split2[0] + " " + split2[1] + " " + split2[2];
                        var vReplace = $"\"{valuePrefix}\"." + split2[0] + " " + split2[1] + " " + split2[2];
                        if (split2[0] == "\"DTValue\"")
                        {
                            v += " " + split2[3];
                            vReplace += " " + split2[3];
                        }
                        v = v.Replace(")", "");
                        vReplace = vReplace.Replace(")", "");

                        if (split2.Length >= 4 && split2[4] == "AND")
                        {
                            var split3 = split1[i + 1].Split(')');
                            v = "(" + $"\"{valuePrefix}\"." + split1[i] + $"\"{valuePrefix}\".";
                            vReplace = "(" + $"\"{valuePrefix}\"." + split1[i] + $"\"{valuePrefix}\".";
                            for (var j = 0; j < split3.Length - 1; j++)
                            {
                                v += split3[j] + ")";
                                vReplace += split3[j] + ")";
                            }
                            i++;
                        }

                        rawBase += "LEFT JOIN(\r\n";
                        rawBase += "SELECT sme.SMId\r\n";
                        rawBase += "FROM ValueSets v\r\n";
                        rawBase += "LEFT JOIN SMESets sme ON sme.Id = v.SMEId\r\n";
                        rawBase += $"WHERE {v}\r\n";
                        rawBase += $") AS value{ii} ON value{ii}.SMId = t.Id\r\n";

                        pathPrefix.Add($"value{ii}");

                        convertConditionSQL = convertConditionSQL.Replace(vReplace, $"(value{ii}.SMId IS NOT NULL)");

                        ii++;
                    }
                }
            }

            var splitConvertConditionSQL = SplitTopLevelOr(convertConditionSQL);

            if (false)
            {
                // rawBase = rawBase.Replace("SELECT DISTINCT ", "SELECT ");
                var smBase = "SELECT DISTINCT t.Id\r\n" + $"FROM {selectSm} AS t\r\n";

                var splitLeftJoin = rawBase.Split("LEFT JOIN(\r\n");

                // var rawBaseTopLevel = "SELECT DISTINCT Id\r\nFROM(\r\n\r\n";
                var rawBaseTopLevel = "";
                for (var s = 0; s < splitConvertConditionSQL.Count; s++)
                {
                    var rawBaseUnionStart = splitLeftJoin[0];
                    if (!splitConvertConditionSQL[s].Contains($"\"{aasPrefix}\"."))
                    {
                        rawBaseUnionStart = smBase;
                    }
                    var rawBaseUnion = "";

                    var substrList = "";
                    for (var i = 1; i < splitLeftJoin.Length; i++)
                    {
                        if (splitConvertConditionSQL[s].Contains($"({pathPrefix[i - 1]}.SMId IS NOT NULL)"))
                        {
                            rawBaseUnion += "LEFT JOIN(\r\n";
                            if (!splitLeftJoin[i].Contains("substr(sme.IdShortPath, "))
                            {
                                rawBaseUnion += splitLeftJoin[i];
                            }
                            else
                            {
                                var posSubstr = splitLeftJoin[i].IndexOf("substr(sme.IdShortPath, ");
                                var posFrom = splitLeftJoin[i].IndexOf("FROM SMESets sme");
                                rawBaseUnion += "SELECT sme.SMId AS SMId, sme.IdShortPath AS IdShortPath\r\n";
                                rawBaseUnion += splitLeftJoin[i].Substring(posFrom);
                                var substr = splitLeftJoin[i].Substring(posSubstr, posFrom - posSubstr);
                                substr = substr.Replace("sme.IdShortPath,", $"{pathPrefix[i - 1]}.IdShortPath,");
                                if (substrList != "")
                                {
                                    substrList += ",";
                                }
                                substrList += substr;
                            }
                        }
                    }
                    if (substrList != "")
                    {
                        var partList = "";
                        while (splitConvertConditionSQL[s].Contains(" AND Part"))
                        {
                            var part = splitConvertConditionSQL[s].Split(" AND Part")[1];
                            var index = part.IndexOf(')');
                            part = "Part" + part.Substring(0, index);
                            if (partList == "")
                            {
                                partList = part;
                            }
                            else
                            {
                                partList += " AND part";
                            }
                            splitConvertConditionSQL[s] = splitConvertConditionSQL[s].Replace(" AND " + part, "");
                        }

                        rawBaseUnionStart = rawBaseUnionStart.Replace("SELECT DISTINCT t.Id\r\n", "");
                        rawBaseUnionStart = "SELECT DISTINCT tt.Id\r\n" + "FROM (\r\n" + "SELECT t.Id,\r\n" + substrList + rawBaseUnionStart;
                        // var first = rawBaseUnionStart.Split("FROM (").First();
                        // rawBaseUnionStart = rawBaseUnionStart.Replace(first, first.Replace("\r\n", ",\r\n") + substrList);

                        partList = partList.Replace("Part", "tt.Part");
                        rawBaseTopLevel += rawBaseUnionStart + rawBaseUnion
                            + "WHERE " + splitConvertConditionSQL[s] + "\r\n"
                            + ") AS tt\r\n" + $"WHERE ({partList})\r\n";
                    }
                    else
                    {
                        rawBaseTopLevel += rawBaseUnionStart + rawBaseUnion + "WHERE " + splitConvertConditionSQL[s] + "\r\n";
                    }
                    if (s < splitConvertConditionSQL.Count - 1)
                    {
                        // rawBaseTopLevel += "\r\nUNION ALL\r\n\r\n";
                        rawBaseTopLevel += "\r\nUNION\r\n\r\n";
                    }
                }
                // rawBaseTopLevel += "\r\n)\r\n";
                rawBase = rawBaseTopLevel;

                // add dummy query, so that optimizer will create covering index
                /*
                rawBase += @"
                UNION
                SELECT DISTINCT t.Id
                FROM(
                SELECT Id, IdShort, Identifier
                FROM SMSets
                WHERE ""IdShort"" = 'dummy'
                ) AS t
                LEFT JOIN(
                SELECT sme.SMId
                FROM ValueSets v
                LEFT JOIN SMESets sme ON sme.Id = v.SMEId
                WHERE ""v"".""SValue"" = 'dummy'
                ) AS value1 ON value1.SMId = t.Id
                WHERE(""t"".""IdShort"" = 'dummy' AND(value1.SMId IS NOT NULL))
                ";
                */
            }
            if (true)
            {
                raw += "DROP TABLE IF EXISTS union_ids;\r\n";
                raw += "CREATE TEMP TABLE union_ids (\r\n";
                raw += "Id INTEGER PRIMARY KEY\r\n";
                raw += ") WITHOUT ROWID;\r\n";
                raw += "\r\n";

                var smBase = "SELECT DISTINCT t.Id\r\n" + $"FROM {selectSm} AS t\r\n";

                var splitLeftJoin = rawBase.Split("LEFT JOIN(\r\n");

                var rawBaseTopLevel = "";
                for (var s = 0; s < splitConvertConditionSQL.Count; s++)
                {
                    var rawBaseUnionStart = splitLeftJoin[0];
                    if (false && !splitConvertConditionSQL[s].Contains($"\"{aasPrefix}\"."))
                    {
                        rawBaseUnionStart = smBase;
                    }
                    var rawBaseUnion = "";

                    var substrList = "";
                    for (var i = 1; i < splitLeftJoin.Length; i++)
                    {
                        if (splitConvertConditionSQL[s].Contains($"({pathPrefix[i - 1]}.SMId IS NOT NULL)"))
                        {
                            rawBaseUnion += "LEFT JOIN(\r\n";
                            if (!splitLeftJoin[i].Contains("substr(sme.IdShortPath, "))
                            {
                                rawBaseUnion += splitLeftJoin[i];
                            }
                            else
                            {
                                var posSubstr = splitLeftJoin[i].IndexOf("substr(sme.IdShortPath, ");
                                var posFrom = splitLeftJoin[i].IndexOf("FROM SMESets sme");
                                rawBaseUnion += "SELECT sme.SMId AS SMId, sme.IdShortPath AS IdShortPath\r\n";
                                rawBaseUnion += splitLeftJoin[i].Substring(posFrom);
                                var substr = splitLeftJoin[i].Substring(posSubstr, posFrom - posSubstr);
                                substr = substr.Replace("sme.IdShortPath,", $"{pathPrefix[i - 1]}.IdShortPath,");
                                if (substrList != "")
                                {
                                    substrList += ",";
                                }
                                substrList += substr;
                            }
                        }
                    }
                    if (substrList != "")
                    {
                        var partList = "";
                        while (splitConvertConditionSQL[s].Contains(" AND Part"))
                        {
                            var part = splitConvertConditionSQL[s].Split(" AND Part")[1];
                            var index = part.IndexOf(')');
                            part = "Part" + part.Substring(0, index);
                            if (partList == "")
                            {
                                partList = part;
                            }
                            else
                            {
                                partList += " AND part";
                            }
                            splitConvertConditionSQL[s] = splitConvertConditionSQL[s].Replace(" AND " + part, "");
                        }

                        var select = "t.Id";
                        if (rawBaseUnionStart.Contains("SELECT DISTINCT a.Id\r\n"))
                        {
                            select = "a.Id";
                        }
                        rawBaseUnionStart = rawBaseUnionStart.Replace("SELECT DISTINCT t.Id\r\n", "");
                        rawBaseUnionStart = rawBaseUnionStart.Replace("SELECT DISTINCT a.Id\r\n", "");
                        rawBaseUnionStart = "SELECT DISTINCT q.Id\r\n" + "FROM (\r\n" + $"SELECT {select},\r\n" + substrList + rawBaseUnionStart;

                        partList = partList.Replace("Part", "q.Part");
                        rawBaseTopLevel += rawBaseUnionStart + rawBaseUnion
                            + "WHERE " + splitConvertConditionSQL[s] + "\r\n"
                            + ") AS q\r\n" + $"WHERE ({partList})\r\n";
                    }
                    else
                    {
                        rawBaseTopLevel += rawBaseUnionStart + rawBaseUnion + "WHERE " + splitConvertConditionSQL[s] + "\r\n";
                    }
                    if (s < splitConvertConditionSQL.Count - 1)
                    {
                        // rawBaseTopLevel += "\r\nUNION\r\n\r\n";
                    }
                    // rawBaseTopLevel = rawBaseTopLevel.Replace("SELECT DISTINCT t.Id", "SELECT t.Id");
                    raw += "INSERT OR IGNORE INTO union_ids(Id)\r\n" + rawBaseTopLevel + ";\r\n\r\n";
                    rawBaseTopLevel = "";
                }

                rawBase = raw;

                raw = rawBase;
                if (raw.Contains(" LIKE "))
                {
                    raw = raw.Replace(" LIKE ", " GLOB ");
                    raw = raw.Replace("%", "*");
                }

                using var tx = db.Database.BeginTransaction();

                db.Database.ExecuteSqlRaw(raw);

                var page = db.Set<SMSetIdResult>()
                    .FromSqlRaw(@"
                        SELECT Id
                        FROM union_ids
                        ORDER BY Id
                        LIMIT {0} OFFSET {1}", pageSize, pageFrom)
                    .AsNoTracking()
                    .Select(x => x.Id)
                    .ToList();

                tx.Commit();

                return page;
            }
        }

        raw = rawBase;
        if (raw.Contains(" LIKE "))
        {
            raw = raw.Replace(" LIKE ", " GLOB ");
            raw = raw.Replace("%", "*");
        }

        var qpRaw = GetQueryPlan(db, raw);

        IQueryable<SMSetIdResult> resultSMId = null;
        resultSMId = db.Set<SMSetIdResult>()
               .FromSqlRaw(raw)
               .AsQueryable();

        var smRawSQL = resultSMId.ToQueryString();
        var qp = GetQueryPlan(db, smRawSQL);

        return resultSMId.Select(r => r.Id).Skip(pageFrom).Take(pageSize).ToList();
    }

    private static List<int> CombineTablesLEFT(
        AasContext db,
        Dictionary<string, string>? conditionsExpression,
        int pageFrom,
        int pageSize,
        ResultType resultType,
        bool consolidate
        )
    {
        IQueryable<AASSet>? aasTable = null;
        IQueryable<SMSet>? smTable = null;
        IQueryable<SMESet>? smeTable = null;
        IQueryable<ValueSet>? valueTable = null;

        var restrictAAS = conditionsExpression.TryGetValue("aas", out var value) && value != "" && value.ToLower() != "true";
        var restrictSM = conditionsExpression.TryGetValue("sm", out value) && value != "" && value.ToLower() != "true";
        var restrictSME = conditionsExpression.TryGetValue("sme", out value) && value != "" && value.ToLower() != "true";
        var restrictValue = conditionsExpression.TryGetValue("value", out value) && value != "" && value.ToLower() != "true";

        var aasFields = new List<string>();
        var smFields = new List<string>();
        var smeFields = new List<string>();

        if (conditionsExpression.TryGetValue("all", out value) && value != "" && value.ToLower() != "true")
        {
            aasFields = GetFields("aas.", value);
            smFields = GetFields("sm.", value);
            smeFields = GetFields("sme.", value);
        }

        var withPathSme = false;
        var withMatch = false;
        var withRecursive = false;
        var pathSME = "";
        var pathAllCondition = "";
        var pathAllConditionRaw = "";

        var aasExistInCondition = conditionsExpression.TryGetValue("all", out value) && value.Contains("aas.");

        bool isWithAASTable = restrictAAS || aasExistInCondition || resultType == ResultType.AssetAdministrationShell;

        if (conditionsExpression.TryGetValue("path-sme", out pathSME))
        {
            withPathSme = true;
            if (!conditionsExpression.TryGetValue("path-all", out pathAllCondition))
            {
                withPathSme = false;
            }
            else
            {
                withRecursive = pathAllCondition.Contains("sme.") || pathAllCondition.Contains("svalue")
                    || pathAllCondition.Contains("mvalue") || pathAllCondition.Contains("dtvalue");
            }
            if (conditionsExpression.TryGetValue("path-raw", out pathAllConditionRaw))
            {
                if (pathAllConditionRaw.Contains("$$match"))
                {
                    withMatch = true;
                }
            }
        }

        if (!conditionsExpression.TryGetValue("path-raw", out var pathAllAas))
        {
            pathAllAas = "";
        }
        List<string> pathConditions = [];

        var selectMatch = "";
        List<string> whereMatch = [];
        List<string> conditionMatch = [];
        if (withMatch)
        {
            var splitMatch = pathAllConditionRaw.Split("$$match$$");
            var matchCount = 0;
            var matchOffset = 0;
            pathAllCondition = "";
            List<string> matchPathList = [];
            for (var iMatch = 0; iMatch < splitMatch.Count(); iMatch++)
            {
                var match = splitMatch[iMatch];

                if (!match.Contains("$$$$tag$$path$$"))
                {
                    pathAllCondition += match;
                }
                else
                {
                    pathAllCondition += $"Math.Abs(SmId) == 10{matchCount}";
                    matchCount++;
                    if (selectMatch != "")
                    {
                        selectMatch += ",";
                    }

                    List<int> order = [];
                    List<int> count = [];
                    List<string> idShortPath = [];
                    List<string> idShortPathLast = [];
                    List<string> idShort = [];
                    List<string> field = [];
                    var split = match.Split("$$tag$$path$$");
                    var with = "";
                    for (var i = 1; i < split.Length; i++)
                    {
                        var firstTag = split[i];
                        var split2 = firstTag.Split("$$");
                        order.Add(i - 1);
                        idShortPath.Add(split2[0]);
                        with = "";
                        if (split2[0].Contains("[]"))
                        {
                            with = "[]";
                        }
                        if (split2[0].Contains("%"))
                        {
                            with = "%";
                        }
                        idShortPathLast.Add(split2[0].Split(with).Last());
                        count.Add(split2[0].Count(c => c == (with == "[]" ? '[' : '%')));
                        idShort.Add(split2[0].Split(".").Last());

                        var c = split2[1] + split2[2];
                        var v = db.ValueSets.Where(c).ToQueryString();
                        var sql = v.Split("WHERE ").Last();
                        field.Add(sql);
                    }
                    order = order.OrderBy(x => count[x]).ToList();

                    with = "";
                    var where = "";
                    if (idShortPath[0].Contains("[]"))
                    {
                        with = "[]";
                        var selectSegment = "";
                        for (var i = 0; i < order.Count - 1; i++)
                        {
                            var firstSplit = idShortPath[order[i]].Split("[]").ToList();
                            var secondSplit = idShortPath[order[i + 1]].Split("[]").ToList();
                            var c = count[order[i]];
                            var firstStartSegment = firstSplit[c - 1];
                            var firstEndSegment = firstSplit[c];
                            var firstIndex = $"Part{matchCount}_{i + 1}_1";
                            selectSegment += $"substr(smePath{order[i] + 1}.IdShortPath, instr(smePath{order[i] + 1}.IdShortPath, '{firstStartSegment}') + length('{firstStartSegment}'),\r\n" +
                               $"instr(smePath{order[i] + 1}.IdShortPath, '{firstEndSegment}') - (instr(smePath{order[i] + 1}.IdShortPath, '{firstStartSegment}') + length('{firstStartSegment}'))) AS {firstIndex},\r\n";
                            var secondStartSegment = secondSplit[c - 1];
                            var secondEndSegment = secondSplit[c];
                            var secondIndex = $"Part{matchCount}_{i + 1}_2";
                            selectSegment += $"substr(smePath{order[i + 1] + 1}.IdShortPath, instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondStartSegment}') + length('{secondStartSegment}'),\r\n" +
                               $"instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondEndSegment}') - (instr(smePath{order[i + 1] + 1}.IdShortPath, '{secondStartSegment}') + length('{secondStartSegment}'))) AS {secondIndex}";
                            if (where != "")
                            {
                                where += " AND ";
                            }
                            where += $"{firstIndex} = {secondIndex}";
                            if (i != order.Count - 2)
                            {
                                selectSegment += ",\r\n";
                            }
                        }
                        selectMatch += $"{selectSegment}\r\n";
                        conditionMatch.Add(selectSegment.TrimEnd(','));
                    }
                    if (idShortPath[0].Contains('%'))
                    {
                        with = "%";
                        var shortestPath = idShortPath[order[0]];
                        var lastDotIndex = shortestPath.LastIndexOf('.');
                        shortestPath = (lastDotIndex >= 0) ? shortestPath.Substring(0, lastDotIndex + 1) : shortestPath;
                        var shortestPathSplit = shortestPath.Split(with).ToList();
                        var segments = shortestPathSplit;

                        for (var p = 0; p < idShortPath.Count; p++)
                        {
                            var selectSegment = "";
                            for (var s = 0; s < segments.Count - 1; s++)
                            {
                                var startSegment = segments[s];
                                var endSegment = segments[s + 1];
                                var alias = $"Part{matchCount}_{p + 1}_{s + 1}";
                                var sql = $"substr(smePath{p + 1}.IdShortPath, instr(smePath{p + 1}.IdShortPath, '{startSegment}') + length('{startSegment}'),\r\n" +
                                   $"instr(smePath{p + 1}.IdShortPath, '{endSegment}') - (instr(smePath{p + 1}.IdShortPath, '{startSegment}') + length('{startSegment}'))) AS {alias}";
                                selectSegment += sql;
                                if (s != segments.Count - 2 || p != idShortPath.Count - 1)
                                {
                                    selectSegment += ",";
                                }
                            }
                            selectMatch += $"{selectSegment}\r\n";
                            conditionMatch.Add(selectSegment.TrimEnd(','));
                        }
                        for (var p = 0; p < idShortPath.Count - 1; p++)
                        {
                            for (var s = 0; s < segments.Count - 1; s++)
                            {
                                if (where != "")
                                {
                                    where += " AND ";
                                }
                                where += $"Part{matchCount}_{p + 1}_{s + 1} = Part{matchCount}_{p + 2}_{s + 1}";
                            }
                        }
                    }
                    for (var i = 0; i < order.Count; i++)
                    {
                        if (where != "")
                        {
                            where = " AND " + where;
                        }
                        where = $"path{matchOffset + order[i] + 1} = 1" + where;
                    }
                    whereMatch.Add($"({where})");
                    matchOffset += idShortPath.Count;
                }
            }

            pathAllAas = pathAllAas.Replace("$$match$$", "");
        }

        if (withPathSme)
        {
            var split = pathAllAas.Split("$$");
            List<string> pathList = [];
            List<string> fieldList = [];
            List<string> opList = [];
            var path = false;
            var field = false;
            var op = false;
            foreach (var s in split)
            {
                if (op)
                {
                    opList.Add(s);
                    op = false;
                }
                if (field)
                {
                    fieldList.Add(s);
                    field = false;
                    op = true;
                }
                if (path)
                {
                    pathList.Add(s);
                    path = false;
                    field = true;
                }
                if (s == "path")
                {
                    path = true;
                }
            }
            var pathAll = pathAllCondition.Copy();
            for (var i = 0; i < pathList.Count; i++)
            {
                var splitField = fieldList[i].Split("#");
                var idShort = pathList[i].Split(".").Last();
                var allCondition = $"sme.idShort == \"{idShort}\" && sme.idShortPath == \"{pathList[i]}\"";
                if (splitField.Length > 1 && splitField[1] != "value")
                {
                    allCondition += $" && sme.{splitField[1]}{opList[i]}";
                }
                else
                {
                    allCondition = $"{allCondition} && {splitField[0]}{opList[i]}";
                }
                var replace = $"$$tag$$path$${pathList[i]}$${fieldList[i]}$${opList[i]}$$";
                pathAllCondition = pathAllCondition.Replace(replace, $"$$path{i}$$");
                pathConditions.Add(allCondition);
            }
        }

        aasTable = db.AASSets;
        aasTable = restrictAAS ? aasTable.Where(conditionsExpression["aas"]) : aasTable;

        smTable = db.SMSets;
        smTable = restrictSM ? smTable.Where(conditionsExpression["sm"]) : smTable;

        smeTable = db.SMESets;
        if (pathConditions.Count == 0)
        {
            smeTable = restrictSME ? smeTable.Where(conditionsExpression["sme"]) : smeTable;
        }
        valueTable = db.ValueSets;
        if (pathConditions.Count == 0)
        {
            valueTable = restrictValue ? valueTable.Where(conditionsExpression["value"].Replace("DValue", "NValue")) : null;

            if (valueTable == null
                && restrictSME)
            {
                smeTable = null;
            }
        }

        var conditionAll = "true";
        if (!withMatch && conditionsExpression.TryGetValue("all-aas", out _))
        {
            conditionAll = conditionsExpression["all-aas"];
        }
        if (pathAllCondition == "")
        {
            pathAllCondition = conditionAll;
        }

        var rawAas = aasTable?.ToQueryString();
        var whereAas = rawAas?.Split("WHERE ").Last();
        var rawSm = smTable?.ToQueryString();
        var whereSm = rawSm?.Split("WHERE ").Last();

        IQueryable<joinAll>? convert = null;

        convert = db.AASSets
            .Join(db.SMRefSets,
                aas => aas.Id,
                r => r.AASId,
                (aas, r) => new { aas, r })
            .Join(db.SMSets,
                x => x.r.Identifier,
                sm => sm.Identifier,
                (x, sm) => new { x.aas, sm })
            .Join(db.SMESets,
                x => x.sm.Id,
                sme => sme.SMId,
                (x, sme) => new { x.aas, x.sm, sme })
            .Join(db.ValueSets,
                x => x.sme.Id,
                v => v.SMEId,
                (x, v) => new joinAll
                {
                    aas = x.aas,
                    sm = x.sm,
                    SMId = x.sm.Id,
                    AASId = x.aas.Id,
                    sme = x.sme,
                    svalue = v.SValue,
                    mvalue = v.NValue,
                    dtvalue = v.DTValue
                });

        var convertSQL = convert.ToQueryString();
        var regex = new Regex("\"(?<table>[^\"]+)\"\\s+AS\\s+\"(?<sql>[^\"]+)\"", RegexOptions.IgnoreCase);
        var convertMap = new Dictionary<string, string>();
        foreach (Match match in regex.Matches(convertSQL))
        {
            var table = match.Groups["table"].Value;
            var sql = match.Groups["sql"].Value;

            convertMap[table] = sql;
        }
        var aasPrefix = convertMap["AASSets"];
        var smPrefix = convertMap["SMSets"];
        var smePrefix = convertMap["SMESets"];
        var valuePrefix = convertMap["ValueSets"];

        var raw = "";
        var rawBase = "";
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
        if (whereAas != null && !whereAas.StartsWith("SELECT"))
        {
            var whereAas2 = whereAas.Replace("\"a\".", "");
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
        if (whereSm != null && !whereSm.StartsWith("SELECT"))
        {
            var whereSm2 = whereSm.Replace("\"s\".", "");
            whereSm2 = whereSm2.Replace("\"s0\".", "");

            selectSm += $"WHERE {whereSm2}\r\n";
        }
        selectSm += ")";

        rawBase = "SELECT DISTINCT ";

        if (resultType == ResultType.AssetAdministrationShell)
        {
            rawBase += "a.Id\r\n";
        }
        else
        {
            rawBase += "t.Id\r\n";
        }

        if (isWithAASTable)
        {
            rawBase += $"FROM {selectAas} AS a\r\n";
            rawBase += "INNER JOIN SMRefSets AS sx ON a.Id = sx.AASId\r\n";
            rawBase += $"INNER JOIN {selectSm} AS t ON sx.Identifier = t.Identifier\r\n";
        }
        else
        {
            rawBase += $"FROM {selectSm} AS t\r\n";
        }

        // rawBase += "INNER JOIN \"SMESets\" AS \"s0\" ON \"t\".\"Id\" = \"s0\".\"SMId\"\r\n";
        // rawBase += "INNER JOIN \"ValueSets\" AS \"v\" ON \"s0\".\"Id\" = \"v\".\"SMEId\"\r\n";

        var placeholderSQL = new string[pathConditions.Count];
        var pathAllExists = pathAllCondition.Copy();
        //var smIdVariable = convert.Select(s => s.SMId).ToQueryString()
        //    .Replace("\r", "").Split("\n").First()
        //    .Split("SELECT ").Last();
        var smIdVariable = $"\"{convertMap["SMSets"]}\".\"Id\"";

        for (var i = 0; i < pathConditions.Count; i++)
        {
            var j = i;
            var join = "";

            if (i < conditionMatch.Count)
            {
                var pi = 0;

                join += $"LEFT JOIN(\r\n";
                join += $"SELECT Path1.SMId AS SMId\r\n";

                while (pi < 2)
                {
                    var c = conditionMatch[i];
                    c = c.Replace($"smePath{i + 1}", "sme");
                    var split = c.Split(" AS ");
                    c = split[0] + " AS Path";

                    if (pi == 0)
                    {
                        join += "FROM (\r\n";
                    }
                    else
                    {
                        join += "JOIN (\r\n";
                    }

                    join += "SELECT sme.SMId AS SMId,\r\n";
                    join += c;
                    join += "\r\n";

                    join += $"FROM SMESets sme\r\n";
                    join += $"JOIN ValueSets v ON v.SMEId = sme.Id ";

                    var where = "";

                    var convertPathCondition = convert.Where(pathConditions[i]);
                    var convertPathConditionSQL = convertPathCondition.ToQueryString();
                    where = convertPathConditionSQL.Split("WHERE ").Last();
                    where = where.Replace($"\"{smePrefix}\".", "sme.").Replace("\"v\".", "v.");

                    split = where.Split(" AND ");

                    var valueSQL = split[split.Length - 1];

                    join += $"AND {valueSQL}\r\n";
                    join += $"WHERE {valueSQL} ";

                    var smeSQLPath = split[split.Length - 2];
                    smeSQLPath = smeSQLPath.Replace($"\"{smePrefix}\".", "sme.");
                    if (smeSQLPath.Contains("[]"))
                    {
                        smeSQLPath = smeSQLPath.Replace("[]", "[%]");
                    }
                    if (smeSQLPath.Contains("%"))
                    {
                        // smeSQLPath = smeSQLPath.Replace("\"IdShortPath\" = ", "\"IdShortPath\" LIKE ");
                        smeSQLPath = smeSQLPath.Replace("\"IdShortPath\" = ", "\"IdShortPath\" GLOB ");
                        smeSQLPath = smeSQLPath.Replace("%", "*");
                    }

                    var smeSQLIdShort = split[split.Length - 3];
                    smeSQLIdShort = smeSQLIdShort.Replace($"\"{smePrefix}\".", "sme.");
                    if (!smeSQLIdShort.Contains("[]") && !smeSQLIdShort.Contains("%"))
                    {
                        join += $"AND {smeSQLIdShort} ";
                    }

                    join += $"AND {smeSQLPath}\r\n";
                    join += $") AS Path{pi + 1}\r\n";
                    if (pi == 0)
                    {
                        i++;
                    }
                    pi++;
                }
                join += "ON Path1.SMId = Path2.SMId AND Path1.Path = Path2.Path\r\n";
            }
            else
            {
                join += $"LEFT JOIN(\r\n";
                join += $"SELECT sme.SMId AS SMId";
                join += "\r\n";
                // join += $"FROM ValueSets AS v\r\n";
                // join += $"JOIN SMESets AS sme ON sme.Id = v.SMEId\r\n";
                join += $"FROM SMESets sme\r\n";
                join += $"LEFT JOIN ValueSets v ON v.SMEId = sme.Id ";

                var where = "";

                if (isWithAASTable)
                {
                    var convertPathCondition = convert.Where(pathConditions[i]).Select(r => r.AASId);
                    var convertPathConditionSQL = convertPathCondition.ToQueryString();
                    where = convertPathConditionSQL.Split("WHERE ").Last();
                    where = where.Replace($"\"{smePrefix}\".", "sme.").Replace("\"v\".", "v.");
                }
                else
                {
                    var convertPathCondition = convert.Where(pathConditions[i]).Select(r => r.SMId);
                    var convertPathConditionSQL = convertPathCondition.ToQueryString();
                    where = convertPathConditionSQL.Split("WHERE ").Last();
                    where = where.Replace($"\"{smePrefix}\".", "sme.").Replace("\"v\".", "v.");
                }

                var split = where.Split(" AND ");

                var valueSQL = split[split.Length - 1];

                join += $"AND {valueSQL}\r\n";
                join += $"WHERE {valueSQL} ";
                // join += "WHERE True ";

                var smeSQLPath = split[split.Length - 2];
                smeSQLPath = smeSQLPath.Replace($"\"{smePrefix}\".", "sme.");
                if (smeSQLPath.Contains("[]"))
                {
                    smeSQLPath = smeSQLPath.Replace("[]", "[%]");
                }
                if (smeSQLPath.Contains("%"))
                {
                    // smeSQLPath = smeSQLPath.Replace("\"IdShortPath\" = ", "\"IdShortPath\" LIKE ");
                    smeSQLPath = smeSQLPath.Replace("\"IdShortPath\" = ", "\"IdShortPath\" GLOB ");
                    smeSQLPath = smeSQLPath.Replace("%", "*");
                }

                var smeSQLIdShort = split[split.Length - 3];
                smeSQLIdShort = smeSQLIdShort.Replace($"\"{smePrefix}\".", "sme.");
                if (!smeSQLIdShort.Contains("[]") && !smeSQLIdShort.Contains("%"))
                {
                    join += $"AND {smeSQLIdShort} ";
                }

                join += $"AND {smeSQLPath}\r\n";
            }

            join += $") AS p{j + 1} ON p{j + 1}.SMId = t.Id\r\n";

            pathPrefix.Add($"p{j + 1}");

            pathAllExists = pathAllExists.Replace($"$$path{j}$$", $"Math.Abs(SMId) == 20{j}");
            placeholderSQL[j] = $"abs({smIdVariable}) = 20{j}";

            if (j < conditionMatch.Count)
            {
                whereMatch[j] = $"(p{j + 1}.SMId IS NOT NULL)";
            }
            // placeholderSQL[i] = $"(p{i + 1} IS NOT NULL)";

            rawBase += join;
        }

        // convert complete condition with placeholders
        var convertCondition = convert.Where(pathAllExists);
        var convertConditionSQL = convertCondition.ToQueryString();
        convertConditionSQL = convertConditionSQL.Split("WHERE ").Last();

        if (!convertConditionSQL.StartsWith("SELECT"))
        {
            for (var i = 0; i < pathConditions.Count; i++)
            {
                if (placeholderSQL[i] != null)
                {
                    convertConditionSQL = convertConditionSQL.Copy().Replace(placeholderSQL[i], $"(p{i + 1}.SMId IS NOT NULL)");
                }
            }

            if (whereMatch.Count != 0)
            {
                for (var i = 0; i < whereMatch.Count; i++)
                {
                    convertConditionSQL = convertConditionSQL.Replace($"abs({smIdVariable}) = 10{i}", whereMatch[i]);
                }

                for (var i = 0; i < conditionMatch.Count; i++)
                {
                    convertConditionSQL = convertConditionSQL.Replace($"path{i + 1} = 1", $"(p{i + 1}.SMId IS NOT NULL)");
                }
            }

            convertConditionSQL = convertConditionSQL.Replace($"\"{smPrefix}\".", "\"t\".");

            if (convertConditionSQL.Contains($"\"{smePrefix}\"."))
            {
                var ii = 1;
                var split1 = convertConditionSQL.Split($"\"{smePrefix}\".");
                foreach (var s1 in split1)
                {
                    var split2 = s1.Split(" ");

                    if (smeFields.Contains($"{split2[0].Replace("\"", "")}"))
                    {
                        var s = split2[0] + " " + split2[1] + " " + split2[2];
                        s = s.Replace(")", "");

                        if (split2.Length >= 7 && split2[3] == "AND")
                        {
                            var v = split2[4] + " " + split2[5] + " " + split2[6];
                            if (v.StartsWith('('))
                            {
                                var j = 6;
                                do
                                {
                                    j++;
                                    v += " " + split2[j];
                                }
                                while (!split2[j].EndsWith(')'));
                            }
                            v = v.Replace("(", "").Replace(")", "");
                            var vReplace = v;
                            v = v.Replace($"\"v\".", "");

                            rawBase += "LEFT JOIN(\r\n";
                            rawBase += "SELECT sme.SMId\r\n";
                            rawBase += "FROM ValueSets v\r\n";
                            rawBase += $"LEFT JOIN SMESets sme ON sme.Id = v.SMEId AND sme.{s}\r\n";
                            rawBase += $"WHERE \"v\".{v}\r\n";
                            rawBase += $") AS sme{ii} ON sme{ii}.SMId = t.Id\r\n";

                            convertConditionSQL = convertConditionSQL.Replace($"\"{smePrefix}\".{s} AND {vReplace}", $"(sme{ii}.SMId IS NOT NULL)");

                            pathPrefix.Add($"sme{ii}");

                            ii++;
                        }
                        else
                        {
                            rawBase += "LEFT JOIN(\r\n";
                            rawBase += "SELECT sme.SMId\r\n";
                            rawBase += "FROM SMESets sme\r\n";
                            rawBase += "LEFT JOIN ValueSets v ON sme.Id = v.SMEId\r\n";
                            rawBase += $"WHERE \"sme\".{s}\r\n";
                            rawBase += $") AS sme{ii} ON sme{ii}.SMId = t.Id\r\n";

                            convertConditionSQL = convertConditionSQL.Replace($"\"{smePrefix}\".{s}", $"(sme{ii}.SMId IS NOT NULL)");

                            pathPrefix.Add($"sme{ii}");

                            ii++;
                        }
                    }
                }
            }

            if (convertConditionSQL.Contains($"\"{valuePrefix}\"."))
            {
                var ii = 1;
                var split1 = convertConditionSQL.Split($"\"{valuePrefix}\".");

                for (var i = 0; i < split1.Length; i++)
                {
                    var s1 = split1[i];
                    var split2 = s1.Split(" ");
                    if (split2[0] == "\"SValue\"" || split2[0] == "\"NValue\"" || split2[0] == "\"DTValue\"")
                    {
                        var v = "\"v\"." + split2[0] + " " + split2[1] + " " + split2[2];
                        var vReplace = $"\"{valuePrefix}\"." + split2[0] + " " + split2[1] + " " + split2[2];
                        if (split2[0] == "\"DTValue\"")
                        {
                            v += " " + split2[3];
                            vReplace += " " + split2[3];
                        }
                        v = v.Replace(")", "");
                        vReplace = vReplace.Replace(")", "");

                        if (split2.Length >= 4 && split2[4] == "AND")
                        {
                            var split3 = split1[i + 1].Split(')');
                            v = "(" + $"\"{valuePrefix}\"." + split1[i] + $"\"{valuePrefix}\".";
                            vReplace = "(" + $"\"{valuePrefix}\"." + split1[i] + $"\"{valuePrefix}\".";
                            for (var j = 0; j < split3.Length - 1; j++)
                            {
                                v += split3[j] + ")";
                                vReplace += split3[j] + ")";
                            }
                            i++;
                        }

                        rawBase += "LEFT JOIN(\r\n";
                        rawBase += "SELECT sme.SMId\r\n";
                        rawBase += "FROM ValueSets v\r\n";
                        rawBase += "LEFT JOIN SMESets sme ON sme.Id = v.SMEId\r\n";
                        rawBase += $"WHERE {v}\r\n";
                        rawBase += $") AS value{ii} ON value{ii}.SMId = t.Id\r\n";

                        pathPrefix.Add($"value{ii}");

                        convertConditionSQL = convertConditionSQL.Replace(vReplace, $"(value{ii}.SMId IS NOT NULL)");

                        ii++;
                    }
                }
            }

            var splitConvertConditionSQL = SplitTopLevelOr(convertConditionSQL);

            if (true)
            {
                raw += "DROP TABLE IF EXISTS union_ids;\r\n";
                raw += "CREATE TEMP TABLE union_ids (\r\n";
                raw += "Id INTEGER PRIMARY KEY\r\n";
                raw += ") WITHOUT ROWID;\r\n";
                raw += "\r\n";

                var smBase = "SELECT DISTINCT t.Id\r\n" + $"FROM {selectSm} AS t\r\n";

                var splitLeftJoin = rawBase.Split("LEFT JOIN(\r\n");

                var rawBaseTopLevel = "";
                for (var s = 0; s < splitConvertConditionSQL.Count; s++)
                {
                    var rawBaseUnionStart = splitLeftJoin[0];
                    if (false && !splitConvertConditionSQL[s].Contains($"\"{aasPrefix}\"."))
                    {
                        rawBaseUnionStart = smBase;
                    }
                    var rawBaseUnion = "";

                    var substrList = "";
                    for (var i = 1; i < splitLeftJoin.Length; i++)
                    {
                        if (splitConvertConditionSQL[s].Contains($"({pathPrefix[i - 1]}.SMId IS NOT NULL)"))
                        {
                            rawBaseUnion += "LEFT JOIN(\r\n" + splitLeftJoin[i];
                            /*
                            rawBaseUnion += "LEFT JOIN(\r\n";
                            if (!splitLeftJoin[i].Contains("substr(sme.IdShortPath, "))
                            {
                                rawBaseUnion += splitLeftJoin[i];
                            }
                            else
                            {
                                var posSubstr = splitLeftJoin[i].IndexOf("substr(sme.IdShortPath, ");
                                var posFrom = splitLeftJoin[i].IndexOf("FROM SMESets sme");
                                rawBaseUnion += "SELECT sme.SMId AS SMId, sme.IdShortPath AS IdShortPath\r\n";
                                rawBaseUnion += splitLeftJoin[i].Substring(posFrom);
                                var substr = splitLeftJoin[i].Substring(posSubstr, posFrom - posSubstr);
                                substr = substr.Replace("sme.IdShortPath,", $"{pathPrefix[i - 1]}.IdShortPath,");
                                if (substrList != "")
                                {
                                    substrList += ",";
                                }
                                substrList += substr;
                            }
                            */
                        }
                    }
                    if (false && substrList != "")
                    {
                        var partList = "";
                        while (splitConvertConditionSQL[s].Contains(" AND Part"))
                        {
                            var part = splitConvertConditionSQL[s].Split(" AND Part")[1];
                            var index = part.IndexOf(')');
                            part = "Part" + part.Substring(0, index);
                            if (partList == "")
                            {
                                partList = part;
                            }
                            else
                            {
                                partList += " AND part";
                            }
                            splitConvertConditionSQL[s] = splitConvertConditionSQL[s].Replace(" AND " + part, "");
                        }

                        var select = "t.Id";
                        if (rawBaseUnionStart.Contains("SELECT DISTINCT a.Id\r\n"))
                        {
                            select = "a.Id";
                        }
                        rawBaseUnionStart = rawBaseUnionStart.Replace("SELECT DISTINCT t.Id\r\n", "");
                        rawBaseUnionStart = rawBaseUnionStart.Replace("SELECT DISTINCT a.Id\r\n", "");
                        rawBaseUnionStart = "SELECT DISTINCT q.Id\r\n" + "FROM (\r\n" + $"SELECT {select},\r\n" + substrList + rawBaseUnionStart;

                        partList = partList.Replace("Part", "q.Part");
                        rawBaseTopLevel += rawBaseUnionStart + rawBaseUnion
                            + "WHERE " + splitConvertConditionSQL[s] + "\r\n"
                            + ") AS q\r\n" + $"WHERE ({partList})\r\n";
                    }
                    else
                    {
                        rawBaseTopLevel += rawBaseUnionStart + rawBaseUnion + "WHERE " + splitConvertConditionSQL[s] + "\r\n";
                    }
                    if (s < splitConvertConditionSQL.Count - 1)
                    {
                        // rawBaseTopLevel += "\r\nUNION\r\n\r\n";
                    }
                    // rawBaseTopLevel = rawBaseTopLevel.Replace("SELECT DISTINCT t.Id", "SELECT t.Id");
                    raw += "INSERT OR IGNORE INTO union_ids(Id)\r\n" + rawBaseTopLevel + ";\r\n\r\n";
                    rawBaseTopLevel = "";
                }

                rawBase = raw;

                raw = rawBase;
                if (raw.Contains(" LIKE "))
                {
                    raw = raw.Replace(" LIKE ", " GLOB ");
                    raw = raw.Replace("%", "*");
                }

                using var tx = db.Database.BeginTransaction();

                db.Database.ExecuteSqlRaw(raw);

                var page = db.Set<SMSetIdResult>()
                    .FromSqlRaw(@"
                        SELECT Id
                        FROM union_ids
                        ORDER BY Id
                        LIMIT {0} OFFSET {1}", pageSize, pageFrom)
                    .AsNoTracking()
                    .Select(x => x.Id)
                    .ToList();

                tx.Commit();

                return page;
            }
        }

        raw = rawBase;
        if (raw.Contains(" LIKE "))
        {
            raw = raw.Replace(" LIKE ", " GLOB ");
            raw = raw.Replace("%", "*");
        }

        var qpRaw = GetQueryPlan(db, raw);

        IQueryable<SMSetIdResult> resultSMId = null;
        resultSMId = db.Set<SMSetIdResult>()
               .FromSqlRaw(raw)
               .AsQueryable();

        var smRawSQL = resultSMId.ToQueryString();
        var qp = GetQueryPlan(db, smRawSQL);

        return resultSMId.Select(r => r.Id).Skip(pageFrom).Take(pageSize).ToList();
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

            /*
            if (securityCondition != null && securityCondition.TryGetValue("svalue", out value) && value != "")
            {
                condition["svalue"] = value.Replace("svalue", "value");
            }

            if (securityCondition != null && securityCondition.TryGetValue("mvalue", out value) && value != "")
            {
                condition["nvalue"] = value.Replace("mvalue", "value");
            }
            */

            if (securityCondition != null && securityCondition.TryGetValue("value", out value) && value != "")
            {
                condition["value"] = value.Replace("svalue", "SValue").Replace("mvalue", "DValue").Replace("dtvalue", "DTValue");
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
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("all", le._expression);
                                            QueryGrammarJSON.createExpression("all-aas", le);
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("all-aas", le._expression);
                                            QueryGrammarJSON.createExpression("aas.", le);
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("aas.", le._expression);
                                            QueryGrammarJSON.createExpression("sm.", le);
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("sm.", le._expression);
                                            QueryGrammarJSON.createExpression("sme.", le);
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("sme.", le._expression);
                                            /*
                                            QueryGrammarJSON.createExpression("svalue", le);
                                            conditions[i].Add("svalue", le._expression);
                                            QueryGrammarJSON.createExpression("mvalue", le);
                                            conditions[i].Add("mvalue", le._expression);
                                            */
                                            QueryGrammarJSON.createExpression("value", le);
                                            le._expression = QueryGrammarJSON.optimizeTrueFalse(le._expression);
                                            conditions[i].Add("value", le._expression);
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
                condition["all-aas"] = query._query_conditions["all-aas"];
                if (query._filter_conditions != null && query._filter_conditions.Count != 0)
                {
                    condition["filter-all"] = query._filter_conditions["all"];
                    condition["filter-aas"] = query._filter_conditions["aas."];
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

                condition["aas"] = query._query_conditions["aas."];
                if (condition["aas"] == "$SKIP")
                {
                    condition["aas"] = "";
                }
                else
                {
                    condition["aas"] = condition["aas"].Replace("aas.", "");
                }
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

                condition["value"] = query._query_conditions["value"];
                if (condition["value"] == "$SKIP")
                {
                    condition["value"] = "";
                }
                else
                {
                    condition["value"] = condition["value"].Replace("svalue", "SValue").Replace("mvalue", "DValue").Replace("dtvalue", "DTValue");
                }
                if (securityCondition != null && securityCondition.TryGetValue("value", out value) && value != "")
                {
                    if (condition["value"] != "")
                    {
                        condition["value"] = value + " && " + condition["value"];
                    }
                    else
                    {
                        condition["value"] = value;
                    }
                }
                if (!condition["all"].Contains("$$path$$"))
                {
                    text = "conditionValue: " + condition["value"];
                    Console.WriteLine(text);
                    messages.Add(text);
                }

                /*
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
                */
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
                // nextPathExpression = $"({field}{exp} && sme.idShort == \"{idShort}\" && sme.idShortPath == \"{idShortPath}\" )";
                nextPathExpression = $"({field}{exp} && sme.idShortPath == \"{idShortPath}\")";
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

