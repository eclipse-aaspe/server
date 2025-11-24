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
using System.Drawing;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AasCore.Aas3_0;
using AasxServerDB.Entities;
using Contracts.QueryResult;
using Contracts.Security;
using Extensions;
// using Newtonsoft.Json.Schema;
using HotChocolate.Types.Relay;
using Irony.Parsing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static AasxServerDB.CrudOperator;
using static AasxServerDB.Query;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public sealed class MyDynamicLinqTypeProvider : IDynamicLinqCustomTypeProvider
{
    // Registrierte Typen, die in Ausdrücken referenzierbar sein sollen
    private static readonly HashSet<Type> _types = new HashSet<Type>
    {
        typeof(Queryable),
        typeof(Enumerable), // optional: falls du "Enumerable.Method" in Strings nutzen willst
        typeof(string)      // optional: falls du "String.Method" in Strings nutzen willst
    };

    // Namens-Mapping für Typauflösung in String-Ausdrücken
    private static readonly Dictionary<string, Type> _typesDict =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase) // oder Ordinal, wenn du strikt sein willst
        {
            ["Queryable"] = typeof(Queryable),
            ["System.Linq.Queryable"] = typeof(Queryable),
            ["Enumerable"] = typeof(Enumerable),
            ["System.Linq.Enumerable"] = typeof(Enumerable),
            ["String"] = typeof(string),
            ["System.String"] = typeof(string)
        };

    public HashSet<Type> GetCustomTypes() => _types;

    public Dictionary<string, Type> GetCustomTypesDictionary() => _typesDict;

    // In 1.6.0.2 sollte die Signatur non-null sein (Type statt Type?)
    public Type ResolveType(string typeName)
    {
        // Zuerst SimpleName-Mapping
        if (_typesDict.TryGetValue(typeName, out var t))
            return t;

        // Dann vollqualifizierte Typauflösung
        var resolved = Type.GetType(typeName, throwOnError: false);
        if (resolved != null)
            return resolved;

        // Wenn nicht gefunden: konsistente Exception
        throw new KeyNotFoundException($"Type '{typeName}' was not found in the custom type provider.");
    }

    public Type ResolveTypeBySimpleName(string typeName)
    {
        if (_typesDict.TryGetValue(typeName, out var t))
            return t;

        // Fallback auf ResolveType (kann vollqualifizierte Namen bedienen)
        var resolved = Type.GetType(typeName, throwOnError: false);
        if (resolved != null)
            return resolved;

        throw new KeyNotFoundException($"Type '{typeName}' was not found in the custom type provider (simple name).");
    }

    // Richtige Signatur: Dictionary<Type, List<MethodInfo>>
    public Dictionary<Type, List<MethodInfo>> GetExtensionMethods()
    {
        var result = new Dictionary<Type, List<MethodInfo>>();

        // Quellen: Queryable, Enumerable & String – deckt die meisten Szenarien ab
        var methodSources = new[] { typeof(Queryable), typeof(Enumerable), typeof(string) };

        foreach (var t in methodSources)
        {
            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (methods.Length == 0)
                continue;

            if (!result.TryGetValue(t, out var list))
            {
                list = new List<MethodInfo>(methods.Length);
                result[t] = list;
            }
            list.AddRange(methods);
        }

        return result;
    }
}

public class CombinedValue
{
    public int SMEId { get; set; }
    public String? SValue { get; set; }
    public Double? MValue { get; set; }
    public DateTime? DTValue { get; set; }
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
        var query = GetSMs(noSecurity, securityCondition, out condition, "submodel", qResult, watch, db, false, withTotalCount, semanticId, identifier, diff, pageFrom, pageSize, expression);
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
            var result = GetSMResult(qResult, (IQueryable<CombinedSMResultWithAas>)query, "Submodel", withLastId, out lastId);
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
            text += "/" + db.SMSets.Count() + ": " + result.Count + " queried";
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
        var query = GetSMs(securityConfig.NoSecurity, securityCondition, out condition, "submodel",
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

    internal SubmodelsQueryResult GetSubmodelList(bool noSecurity, AasContext db, Dictionary<string, string>? securityCondition,
        int pageFrom, int pageSize, string resultType, string expression)
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
        var query = GetSMs(noSecurity, securityCondition, out condition, resultType,
            qResult, watch, db, false, false, "", "", "", pageFrom, pageSize, expression);
        if (query == null)
        {
            text = "No query is generated.";
            Console.WriteLine(text);
            return null;
        }
        else
        {
            var submodelsResult = new SubmodelsQueryResult
            {
                Ids = [],
                Shells = [],
                Submodels = [],
                SubmodelElements = []
            };

            text = "Generate query in " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);

            watch.Restart();
            var lastId = 0;

            if (resultType == "SubmodelElement")
            {
                var smIdList = query.Select("SM_Id").Distinct().ToDynamicList();
                var smeIdList = query.Select("SME_Id").Distinct().ToDynamicList();

                var smList = db.SMSets.Where(sm => smIdList.Contains(sm.Id)).ToList();
                var smeList = db.SMESets.Where(sme => smeIdList.Contains(sme.Id)).ToList();

                foreach (var sm in smList)
                {
                    var smeSmList = smeList.Where(sme => sme.SMId == sm.Id);
                    var smeTree = GetTree(db, sm, smeSmList.ToList());
                    var smeMerged = GetSmeMerged(db, null, smeTree, sm);

                    /*
                    var mergeForCondition = smeMerged.Select(sme => new
                    {
                        sm = sme.smSet,
                        sme = sme.smeSet,
                        svalue = (sme.smeSet.TValue == "S" && sme.sValueSet != null && sme.sValueSet.Value != null) ? sme.sValueSet.Value : "",
                        mvalue = (sme.smeSet.TValue == "I" && sme.iValueSet != null && sme.iValueSet.Value != null) ? sme.iValueSet.Value :
                            (sme.smeSet.TValue == "D" && sme.dValueSet != null && sme.dValueSet.Value != null) ? sme.dValueSet.Value : 0
                    }).Distinct();

                    var filter = "true";
                    if (condition != null && condition.TryGetValue("filter-all", out var filter2))
                    {
                        if (filter2 != null)
                        {
                            if (filter == "true")
                            {
                                filter = filter2;
                            }
                            else
                            {
                                filter = $"({filter} && {filter2})";
                            }
                        }
                    }

                    var resultCondition = mergeForCondition.AsQueryable().Where(filter);
                    var resultConditionIDs = resultCondition.Select(s => s.sme.Id).Distinct().ToList();
                    smeMerged = smeMerged.Where(m => m.smeSet != null && resultConditionIDs.Contains(m.smeSet.Id)).ToList();
                    var smeSmListMerged = smeMerged.Where(m => smeSmList.Contains(m.smeSet)).Select(m => m.smeSet).Distinct().ToList();
                    */
                    var smeSmListMerged = smeSmList;

                    foreach (var sme in smeSmListMerged)
                    {
                        var readSme = ReadSubmodelElement(sme, smeMerged);
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
                            submodelsResult.SubmodelElements.Add(readSme);
                        }
                    }
                }
            }
            else
            {
                var result = GetSMResult(qResult, (IQueryable<CombinedSMResultWithAas>)query, resultType, false, out lastId);
                text = "Collect results in " + watch.ElapsedMilliseconds + " ms";
                Console.WriteLine(text);
                text = "SMs found ";
                text += "/" + db.SMSets.Count() + ": " + result.Count + " queried";
                Console.WriteLine(text);

                if (resultType == "AssetAdministrationShell")
                {
                    var timeStamp = DateTime.UtcNow;
                    var shells = new List<IAssetAdministrationShell>();

                    var aasIdList = result.Where(r => r.aasId != null).Select(r => r.aasId).Distinct();

                    if (aasIdList.IsNullOrEmpty())
                    {
                        var smIdentifierList = result.Select(r => r.smIdentifier).Distinct();

                        aasIdList = db.SMRefSets.Where(sm => smIdentifierList.Contains(sm.Identifier)).
                            Select(s => s.AASId);
                    }
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
                    submodelsResult.Shells = shells;
                }
                else if (!qResult.WithSelectId && !qResult.WithSelectMatch)
                {
                    var timeStamp = DateTime.UtcNow;
                    var submodels = new List<ISubmodel>();

                    var smIdList = result.Select(sm => sm.smId).Distinct();
                    var smList = db.SMSets.Where(sm => smIdList.Contains(sm.Id)).ToList();

                    foreach (var sm in smList.Select(selector: submodelDB =>
                        ReadSubmodel(db, smDB: submodelDB, "", securityCondition, condition)))
                    {
                        if (sm != null)
                        {
                            var aasId = result.FirstOrDefault(r => r.smIdentifier == sm.Id)?.aasId;
                            if (aasId != null)
                            {
                                var aasSet = db.AASSets.FirstOrDefault(a => a.Id == aasId);
                                if (aasSet != null)
                                {
                                    sm.Extensions ??= [];
                                    sm.Extensions.Add(new Extension("$aas#id", value: aasSet.Identifier));
                                    if (aasSet.IdShort != null)
                                    {
                                        sm.Extensions.Add(new Extension("$aas#idShort", value: aasSet.IdShort));
                                    }
                                    if (aasSet.GlobalAssetId != null)
                                    {
                                        sm.Extensions.Add(new Extension("$aas#globalAssetId", value: aasSet.GlobalAssetId));
                                    }
                                }
                            }

                            if (sm.TimeStamp == DateTime.MinValue)
                            {
                                sm.SetAllParentsAndTimestamps(null, timeStamp, timeStamp, DateTime.MinValue);
                                sm.SetTimeStamp(timeStamp);
                            }
                            submodels.Add(sm);
                        }
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
                                        submodelsResult.SubmodelElements.Add(readSme);
                                    }
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
        string resultType,
        QResult qResult, Stopwatch watch, AasContext db, bool withCount = false, bool withTotalCount = false,
        string semanticId = "", string identifier = "", string diffString = "", int pageFrom = -1, int pageSize = -1, string expression = "")
    {
        IQueryable<CombinedSMResultWithAas>? result = null;
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

        conditionsExpression.TryGetValue("aas", out var conditionAas);
        IQueryable<SMRefSet> smRef = null;
        IQueryable<int?> smRefId = null;
        if (!string.IsNullOrEmpty(conditionAas))
        {
            var aas = db.AASSets.Where(conditionAas).Select(a => new { a.Id });

            smRef = db.SMRefSets.Join(
                aas,
                smRef => smRef.AASId,
                aas => aas.Id,
                (smRef, aas) => smRef
            );

            smRefId = db.SMSets.Join(
                smRef,
                sm => sm.Identifier,
                smRef => smRef.Identifier,
                (sm, smRef) => new { id = (int?)sm.Id }
            )
            .Select(s => s.id);
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

        if (!conditionsExpression.TryGetValue("path-raw", out var pathAllAas))
        {
            pathAllAas = "";
        }
        List<string> anyConditions = [];

        // restrictions in which tables 
        var restrictAAS = false;
        var restrictSM = false;
        var restrictSME = false;
        var restrictSValue = false;
        var restrictNValue = false;
        var restrictValue = false;

        // restrict all tables seperate
        IQueryable<CombinedSMSMEV> comTable = null;
        IQueryable<AASSet>? aasTable;
        IQueryable<SMSet> smTable;
        IQueryable<SMESet> smeTable;
        IQueryable<ValueSet>? valueTable;
        //IQueryable<IValueSet>? iValueTable;
        //IQueryable<DValueSet>? dValueTable;

        // get data
        var skip = false;
        if (withExpression) // with expression
        {
            string ? rawSQLEx;
            // check restrictions
            restrictAAS = conditionsExpression.TryGetValue("aas", out var value) && value != "" && value != "true";
            restrictSM = conditionsExpression.TryGetValue("sm", out value) && value != "" && value != "true";
            restrictSME = conditionsExpression.TryGetValue("sme", out value) && value != "" && value != "true";
            /*
            restrictSValue = conditionsExpression.TryGetValue("svalue", out value) && value != "" && value != "true";
            restrictNValue = conditionsExpression.TryGetValue("nvalue", out value) && value != "" && value != "true";
            restrictValue = restrictSValue || restrictNValue;
            */
            restrictValue = conditionsExpression.TryGetValue("value", out value) && value != "" && value != "true";
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
                            // var svalue = db.ValueSets.Where(distinctExp[iExp].Replace("svalue", "value"));
                            var svalue = db.ValueSets.Where(distinctExp[iExp]);
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
                smTable = db.SMSets;
                if (smRef != null)
                {
                    var smRefString = smRef.Select<string>("Identifier");
                    smTable = smTable.Where(s => smRefString.Contains(s.Identifier));
                }
                smTable = smTable.Where(pathAllCondition, parameters).Skip(pageFrom).Take(pageSize).Distinct();

                var resultSMPath = smTable.Select(sm => new CombinedSMResult
                {
                    SM_Id = sm.Id,
                    Identifier = sm.Identifier,
                    TimeStampTree = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree),
                    MatchPathList = null
                });

                // resultSMPath = resultSMPath.Skip(pageFrom).Take(pageSize);
                result = CombineSMWithAas(db, resultSMPath, smRef);
                result = result.Skip(pageFrom).Take(pageSize);

                return result;
            }
            else
            {
                if (withPathSme && !consolidate)
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
                    smTable = db.SMSets;
                    if (smRef != null)
                    {
                        var smRefString = smRef.Select<string>("Identifier");
                        smTable = smTable.Where(s => smRefString.Contains(s.Identifier));
                    }
                    smTable = restrictSM ? smTable.Where(conditionsExpression["sm"]) : smTable;
                    smeTable = db.SMESets;
                    if (smeCondition != "")
                    {
                        smeTable = db.SMESets.Where(smeCondition);
                    }
                    valueTable = db.ValueSets;
                    if (valueCondition != "")
                    {
                        valueTable = db.ValueSets.Where(valueCondition);
                    }
                    //iValueTable = db.IValueSets;
                    //dValueTable = db.DValueSets;

                    var t1 = smTable.ToQueryString();
                    var t2 = smeTable.ToQueryString();
                    var t3 = valueTable.ToQueryString();
                    //var t4 = iValueTable.ToQueryString();
                    //var t5 = dValueTable.ToQueryString();

                    // combine tables to a raw sql 
                    rawSQLEx = CombineTablesToRawSQL(direction, smTable, smeTable, valueTable, false);
                    // table name needed for EXISTS in path search
                    rawSQLEx = "WITH MergedTables AS (\r\n" + rawSQLEx + ")\r\nSELECT *\r\nFROM MergedTables\r\n";
                    comTable = db.Database.SqlQueryRaw<CombinedSMSMEV>(rawSQLEx).AsQueryable();
                    var qp = GetQueryPlan(db, rawSQLEx);

                    /*
                    var comTable2 = CombineTables(db, null, smTable, smeTable, sValueTable, iValueTable, dValueTable);
                    var rawSQLEx2 = comTable2.ToQueryString();
                    var qp2 = GetQueryPlan(db, rawSQLEx2);

                    var comTable4 = CombineTablesWithCTE(db, smTable, smeTable, sValueTable, iValueTable, dValueTable);
                    var rawSQLEx4 = comTable4.ToQueryString();
                    var qp4 = GetQueryPlan(db, rawSQLEx4);
                    */

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
                if (withPathSme && consolidate)
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
                    var pathAll2 = pathAllCondition.Copy();
                    for (var i = 0; i < pathList.Count; i++)
                    {
                        var splitField = fieldList[i].Split("#");
                        var idShort = pathList[i].Split(".").Last();
                        var smeCondition = $"idShort == \"{idShort}\" && idShortPath == \"{pathList[i]}\"";
                        var allCondition = $"sme.idShort == \"{idShort}\" && sme.idShortPath == \"{pathList[i]}\"";
                        var valueCondition = "";
                        if (splitField.Length > 1 && splitField[1] != "value")
                        {
                            smeCondition += $" && {splitField[1]}{opList[i]}";
                            smeCondition = $"SMESets.Any({smeCondition})";
                            allCondition += $" && sme.{splitField[1]}{opList[i]}";
                        }
                        else
                        {
                            smeCondition = $"SMESets.Any({smeCondition} && ValueSets.Any({splitField[0]}{opList[i]}))";
                            allCondition = $"{allCondition} && {splitField[0]}{opList[i]}";
                        }
                        pathAll = pathAll.Replace($"$$path{i}$$", "(" + smeCondition + ")");
                        var replace = $"$$tag$$path$${pathList[i]}$${fieldList[i]}$${opList[i]}$$";
                        // pathAllAas = pathAllAas.Replace(replace, $"Any({allCondition})");
                        anyConditions.Add(allCondition);
                        pathAll2 = pathAll2.Replace($"$$path{i}$$", "(" + allCondition + ")");
                    }
                    // anyConditions.Clear();
                    // anyConditions.Add(pathAll2);
                    if (conditionsExpression["sm"] == "")
                    {
                        conditionsExpression["sm"] = $"({pathAll})".Replace("sm.", "").Replace("mvalue", "MValue");
                    }
                    else
                    {
                        conditionsExpression["sm"] = $"({conditionsExpression["sm"]}) && ({pathAll})".Replace("sm.", "").Replace("mvalue", "MValue");
                    }
                    restrictSM = false;
                }
            }
            // else
            if (!skip)
            {
                if (consolidate)
                {
                    // restrict all tables seperate
                    aasTable = db.AASSets;
                    aasTable = restrictAAS ? aasTable.Where(conditionsExpression["aas"]) : null;
                    smTable = db.SMSets;
                    smTable = restrictSM ? smTable.Where(conditionsExpression["sm"]) : smTable;
                    smeTable = db.SMESets;
                    if (anyConditions.Count == 0)
                    {
                        smeTable = restrictSME ? smeTable.Where(conditionsExpression["sme"]) : smeTable;
                    }
                    valueTable = db.ValueSets;
                    if (anyConditions.Count == 0)
                    {
                        valueTable = restrictValue ? valueTable.Where(conditionsExpression["value"].Replace("DValue", "NValue")) : valueTable;
                    }

                    var conditionAll = "true";
                    if (conditionsExpression.TryGetValue("all-aas", out _))
                    {
                        conditionAll = conditionsExpression["all-aas"];
                        // conditionAll = conditionsExpression["all-aas"].Replace("svalue", "SValue").Replace("mvalue", "DValue").Replace("dtvalue", "DTValue");
                    }
                    comTable = CombineTablesEXISTS(db, aasTable, smTable, smeTable, valueTable, pathAllCondition, anyConditions, conditionAll);
                    // var x1 = comTable.Take(10).ToList();
                }
                else
                {
                    // restrict all tables seperate
                    aasTable = db.AASSets;
                    aasTable = restrictAAS ? aasTable.Where(conditionsExpression["aas"]) : aasTable;
                    smTable = db.SMSets;
                    smTable = restrictSM ? smTable.Where(conditionsExpression["sm"]) : smTable;
                    smeTable = restrictSME ? db.SMESets.Where(conditionsExpression["sme"]) : db.SMESets;
                    valueTable = restrictValue ? (restrictSValue ? db.ValueSets.Where(conditionsExpression["value"]) : null) : db.ValueSets;
                    //iValueTable = restrictValue ? (restrictNValue ? db.IValueSets.Where(conditionsExpression["nvalue"]) : null) : db.IValueSets;
                    //dValueTable = restrictValue ? (restrictNValue ? db.DValueSets.Where(conditionsExpression["nvalue"]) : null) : db.DValueSets;

                    if (smRefId != null)
                    {
                        var smRefIdList = smRefId.ToList();
                        smTable = smTable.Where(s => smRefIdList.Contains(s.Id));
                    }

                    // combine tables to a raw sql 
                    rawSQLEx = CombineTablesToRawSQL(direction, smTable, smeTable, valueTable, false);
                    // table name needed for EXISTS in path search
                    rawSQLEx = "WITH MergedTables AS (\r\n" + rawSQLEx + ")\r\nSELECT *\r\nFROM MergedTables\r\n";
                    comTable = db.Database.SqlQueryRaw<CombinedSMSMEV>(rawSQLEx).AsQueryable();
                }

                if (withPathSme && pathAllCondition != null && param != null)
                {
                    comTable = comTable.Where(pathAllCondition, param);

                    var text = "-- End idShortPath at " + watch.ElapsedMilliseconds + " ms";
                    Console.WriteLine(text);
                    messages.Add(text);
                }

                if (!consolidate)
                {
                    var combi = conditionsExpression["all"].Replace("svalue", "V_Value").Replace("mvalue", "V_D_Value").Replace("sm.Identifier", "SM_Identifier").Replace("sm.idShort", "SM_IdShort").Replace("sme.idShort", "SME_IdShort").Replace("sme.idShortPath", "SME_IdShortPath");
                    comTable = comTable.Where(combi);
                }
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

        if (!consolidate)
        {
            // modifiy raw sql
            var comTableQueryString = comTable.ToQueryString();
            comTableQueryString = ModifiyRawSQL(comTableQueryString);
            comTable = db.Database.SqlQueryRaw<CombinedSMSMEV>(comTableQueryString);
        }

        if (resultType == "SubmodelElement")
        {
            if (condition != null && condition.TryGetValue("filter-all", out var filter))
            {
                filter = filter.Replace("sme.", "SME_");
                comTable = comTable.Where(filter);
            }
            var resultSME = comTable.Select("new (SM_Id as SM_Id, SME_Id as SME_Id)").Distinct().Skip(pageFrom).Take(pageSize);
            return resultSME;
        }

        var smRawSQL = comTable.ToQueryString();
        IQueryable<CombinedSMResult> resultSM = null;

        if (!consolidate)
        {

            // check for WITH at the beginning
            if (withExpression)
            {
                var index = smRawSQL.IndexOf("WITH");
                if (index == 0)
                {
                    smRawSQL = smRawSQL.Replace("SELECT *",
                        $"SELECT DISTINCT SM_Identifier AS Identifier, SM_Id as SM_Id, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', SM_TimeStampTree) AS TimeStampTree, null AS MatchPathList"
                        );
                }
                else
                {
                    // change the first select
                    index = smRawSQL.IndexOf("FROM");
                    if (index == -1)
                        return null;
                    var prefix = "";
                    var split = smRawSQL.Substring(0, index).Split(" ");
                    smRawSQL = smRawSQL.Substring(index);
                    if (split[1].Contains("."))
                    {
                        split = split[1].Split(".");
                        prefix = split.First().Replace("\"", "").Replace("\n", "").Replace("\r", "") + ".";
                    }
                    /*
                    var split = smRawSQL.Split(" ");
                    var count = split.Length;
                    if (split[count - 2] == "AS")
                    {
                        prefix = split.Last().Replace("\"", "").Replace("\n", "").Replace("\r", "") + ".";
                    }
                    */
                    smRawSQL = $"SELECT DISTINCT {prefix}SM_Identifier AS Identifier, {prefix}SM_Id as SM_Id, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', {prefix}SM_TimeStampTree) AS TimeStampTree, null AS MatchPathList \n {smRawSQL}";
                }
            }
            else
            {
                var index = smRawSQL.IndexOf("FROM");
                if (index == -1)
                    return null;
                smRawSQL = smRawSQL.Substring(index);
                smRawSQL = $"SELECT DISTINCT s.Identifier, SM_Id as SM_Id, strftime('{TimeStamp.TimeStamp.GetFormatStringSQL()}', s.TimeStampTree) AS TimeStampTree, null AS MatchPathList \n {smRawSQL}";
            }

            var qp = GetQueryPlan(db, smRawSQL);

            resultSM = db.Database.SqlQueryRaw<CombinedSMResult>(smRawSQL);
        }
        else
        {
            resultSM = comTable.Select(c => new CombinedSMResult
            {
                SM_Id = c.SM_Id,
                Identifier = c.SM_Identifier,
                TimeStampTree = TimeStamp.TimeStamp.DateTimeToString(c.SM_TimeStampTree),
                MatchPathList = null
            }).Distinct();
            // var x2 = resultSM.Take(10).ToList();
        }

        // select for count
        if (withCount)
        {
            // var qCount = comTable.Select(sm => sm.SM_Identifier);
            // return qCount;
            result = CombineSMWithAas(db, resultSM, smRef);
            return result;
        }

        if (withTotalCount)
        {
            var text = "-- Start totalCount at " + watch.ElapsedMilliseconds + " ms";
            Console.WriteLine(text);
            messages.Add(text);
            result = CombineSMWithAas(db, resultSM, smRef);
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
                resultSM = resultSM.OrderBy(sm => sm.SM_Id);
                // resultSM = resultSM.Skip(pageFrom).Take(pageSize);
                result = CombineSMWithAas(db, resultSM, smRef);
                result = result.Skip(pageFrom).Take(pageSize);
            }
            else
            {
                // resultSM = resultSM.Skip(pageFrom).Take(pageSize);
                result = CombineSMWithAas(db, resultSM, smRef);
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
                    // resultSM = resultSM.OrderBy(sm => sm.SM_Id).Where(sm => sm.SM_Id > lastID).Take(pageSize);
                    result = CombineSMWithAas(db, resultSM, smRef);
                    result = result.OrderBy(sm => sm.SM_Id).Where(sm => sm.SM_Id > lastID).Take(pageSize);
                }
                else
                {
                    // resultSM = resultSM.Where(sm => sm.SM_Id > lastID).Take(pageSize);
                    result = CombineSMWithAas(db, resultSM, smRef);
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
        if (result != null)
        {
            smRawSQL = result.ToQueryString();
            var rawSqlSplit = smRawSQL.Replace("\r", "").Split("\n").Select(s => s.TrimStart()).ToList();
            rawSQL.AddRange(rawSqlSplit);
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

    private static IQueryable<CombinedSMResultWithAas> CombineSMWithAas(AasContext db, IQueryable<CombinedSMResult> smResult, IQueryable<SMRefSet>? smRef)
    {
        IQueryable<CombinedSMResultWithAas> smResultWithAas;
        if (smRef != null)
        {
            var smRefDict = smRef
                .Where(r => r.Identifier != null && r.AASId != null)
                .Take(10000)
                .ToDictionary(r => r.Identifier!, r => r.AASId);

            smResultWithAas = smResult
                .Select(s => new CombinedSMResultWithAas
                {
                    AAS_Id = s.Identifier != null ? smRefDict[s.Identifier] : null,
                    SM_Id = s.SM_Id,
                    Identifier = s.Identifier,
                    TimeStampTree = s.TimeStampTree,
                    MatchPathList = null
                });

            //var smRawSQL = smResultWithAas.ToQueryString();
            //var qp = GetQueryPlan(db, smRawSQL);
            return smResultWithAas;
        }
        else
        {
            smResultWithAas = smResult.Select(r1 => new CombinedSMResultWithAas
            {
                AAS_Id = null,
                SM_Id = r1.SM_Id,
                Identifier = r1.Identifier,
                TimeStampTree = r1.TimeStampTree,
                MatchPathList = null
            });
        }
        return smResultWithAas;
    }

    private static List<SMResult> GetSMResult(QResult qResult, IQueryable<CombinedSMResultWithAas> query, string resultType, bool withLastID, out int lastID)
    {
        var messages = qResult.Messages ?? [];
        lastID = 0;

        if (withLastID)
        {
            var resultWithSMID = query
                .Select(sm => new
                {
                    sm.AAS_Id,
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
                    aasId = sm.AAS_Id,
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
                    aasId = sm.AAS_Id,
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

    private static string CombineTablesToRawSQL(int direction, IQueryable<SMSet> smTable, IQueryable<SMESet> smeTable, IQueryable<ValueSet>? sValueTable, bool withParaWithoutValue)
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
            V_Value = sV.SValue,
            V_D_Value = sV.NValue
        });
        //var iValueSelect = iValueTable?.Select(iV => new
        //{
        //    V_SMEId = iV.SMEId,
        //    V_Value = iV.Value,
        //    V_D_Value = iV.Value
        //});
        //var dValueSelect = dValueTable?.Select(dV => new
        //{
        //    V_SMEId = dV.SMEId,
        //    V_Value = dV.Value,
        //    V_D_Value = dV.Value
        //});

        // to query string
        var smQueryString = smSelect.ToQueryString();
        var smeQueryString = smeSelect.ToQueryString();
        var sValueQueryString = sValueSelect?.ToQueryString();
        //var iValueQueryString = iValueSelect?.ToQueryString();
        //var dValueQueryString = dValueSelect?.ToQueryString();

        // modify the raw sql (example set parameters, INSTR)
        smQueryString = ModifiyRawSQL(smQueryString);
        smeQueryString = ModifiyRawSQL(smeQueryString);
        sValueQueryString = ModifiyRawSQL(sValueQueryString);
        //iValueQueryString = ModifiyRawSQL(iValueQueryString);
        //dValueQueryString = ModifiyRawSQL(dValueQueryString);

        // with querys for each table
        var rawSQL = $"WITH\n" +
            $"FilteredSM AS (\n{smQueryString}\n),\n" +
            $"FilteredSME AS (\n{smeQueryString}\n),\n" +
            (sValueQueryString != null ? $"FilteredSValue AS (\n{sValueQueryString}\n),\n" : string.Empty) /*+
            (iValueQueryString != null ? $"FilteredIValue AS (\n{iValueQueryString}\n),\n" : string.Empty) +
            (dValueQueryString != null ? $"FilteredDValue AS (\n{dValueQueryString}\n),\n" : string.Empty)*/;

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
                    //((!sValueQueryString.IsNullOrEmpty()
                    //&& !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                    //(!iValueQueryString.IsNullOrEmpty() ? $"{selectStart}v.V_Value{selectDValueFROM}FilteredIValue{selectEnd}" : string.Empty) +
                    //((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                    //(!dValueQueryString.IsNullOrEmpty() ? $"{selectStart}v.V_Value{selectDValueFROM}FilteredDValue{selectEnd}" : string.Empty) +
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
                    //((!sValueQueryString.IsNullOrEmpty()
                    //&& !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                    //(!iValueQueryString.IsNullOrEmpty() ? $"{selectStart}v.V_Value{selectDValueFROM}FilteredIValue{selectEnd}" : string.Empty) +
                    //((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                    //(!dValueQueryString.IsNullOrEmpty() ? $"{selectStart}v.V_Value{selectDValueFROM}FilteredDValue{selectEnd}" : string.Empty) +
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
                    //((!sValueQueryString.IsNullOrEmpty()
                    //&& !iValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                    //(!iValueQueryString.IsNullOrEmpty() ? $"{selectWithStart}v.V_Value{selectDValueFROM}FilteredIValue{selectWithEnd}" : string.Empty) +
                    //((!iValueQueryString.IsNullOrEmpty() && !dValueQueryString.IsNullOrEmpty()) ? "UNION ALL \n" : string.Empty) +
                    //(!dValueQueryString.IsNullOrEmpty() ? $"{selectWithStart}v.V_Value{selectDValueFROM}FilteredDValue{selectWithEnd}" : string.Empty) +
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

    public sealed class DbIndexInfo
    {
        public string Name { get; set; } = default!;
        public string Table { get; set; } = default!;
        public string? Sql { get; set; }
    }

    public class UnifiedValue
    {
        public int V_SMEId { get; set; }
        public string? V_Value { get; set; }
        public double? V_D_Value { get; set; }
    }

    private class aasAndSm1
    {
        // public AASSet? aas;
        public int? aas;
        public SMSet sm;
    }
    private class aasAndSm
    {
        public AASSet? aas;
        // public int? aas;
        public SMSet sm;
    }
    private class joinAll
    {
        public int SMId;
        public AASSet? aas;
        public SMSet? sm;
        public SMESet? sme;
        public string? svalue;
        public double? mvalue;
        public DateTime? dtvalue;
    }

    public class SMIdOnly
    {
        public int SMId;
    }

    private static IQueryable<CombinedSMSMEV> CombineTablesCTE(
        AasContext db,
        IQueryable<AASSet>? aasTable,
        IQueryable<SMSet> smTable,
        IQueryable<SMESet> smeTable,
        IQueryable<ValueSet>? valueTable,
        //IQueryable<IValueSet>? iValueTable,
        //IQueryable<DValueSet>? dValueTable,
        string pathAllCondition,
        List<string> pathConditions,
        string conditionAll = "true")
    {
        var q1 = aasTable?.ToQueryString();
        var q2 = smTable?.ToQueryString();
        var q3 = smeTable?.ToQueryString();
        var q4 = valueTable?.ToQueryString();

        IQueryable<joinAll>? result = null;

        if (aasTable != null)
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
                    SMId = x.sm.Id
                });
        }
        else
        {
            result = smTable.Select(sm => new joinAll
            {
                sm = sm,
                SMId = sm.Id
            });
        }

        result = result.Join(smeTable,
            r => r.sm.Id,
            sme => sme.SMId,
            (r, sme) => new joinAll
            {
                aas = r.aas,
                sm = r.sm,
                sme = sme,
                SMId = r.sm.Id
            });

        if (valueTable != null)
        {
            result = result.Join(valueTable,
            r => r.sme.Id,
            v => v.SMEId,
            (r, v) => new joinAll
            {
                SMId = r.SMId,
                aas = r.aas,
                sm = r.sm,
                sme = r.sme,
                svalue = v.SValue,
                mvalue = v.NValue,
                dtvalue = v.DTValue,
            });
        }

        var smRawSQL = result.ToQueryString();
        var qp = GetQueryPlan(db, smRawSQL);

        if (pathConditions.Count != 0)
        {
            // sme with idShortPath use CTE (common table expressions)
            // for each idShortPath there is 1 CTE: pathCTE#
            // Convert C# conditions to SQL conditions by EF
            // Use placeholder in condition conversion: "Math.Abs(SMId)"
            // Include SQL conditions into SQL
            // Convert result back to EF by SMId only

            var placeholderSQL = new string[pathConditions.Count];
            var pathConditionsSQL = new string[pathConditions.Count];
            var pathAllExists = pathAllCondition.Copy();

            var raw = "WITH\r\n";

            for (var i = 0; i < pathConditions.Count; i++)
            {
                // convert path condition to SQL syntax
                var convertPathCondition = result.Where(pathConditions[i]).Select(r => r.SMId);
                var convertPathConditionSQL = convertPathCondition.ToQueryString();
                var where = convertPathConditionSQL.Split("WHERE").Last();
                pathConditionsSQL[i] = where;

                if (i != 0)
                {
                    raw += ",\r\n";
                }

                // Define CTE for idShortPath with condition
                raw += $"pathCTE{i} AS (\r\nSELECT s.\"Id\"\r\nFROM \"SMSets\" AS s\r\nINNER JOIN \"SMESets\" AS s0 ON s.\"Id\" = s0.\"SMId\"\r\nINNER JOIN \"ValueSets\" AS v ON s0.\"Id\" = v.\"SMEId\"\r\nWHERE\r\n";
                raw += where + "\r\n)\r\n";

                // change placeholder in complete condition
                // pathAllExists = pathAllExists.Replace($"$$path{i}$$", $"Math.Abs(SMId) == {i}");
                pathAllExists = pathAllExists.Replace($"$$path{i}$$", $"Math.Abs(SMId) == {i}");
                // placeholder after conversion to SQL
                placeholderSQL[i] = $"abs(\"s\".\"Id\") = {i}";
            }

            // convert complete condition with placeholders
            var convertCondition = result.Where(pathAllExists);
            var convertConditionSQL = convertCondition.ToQueryString();
            convertConditionSQL = convertConditionSQL.Split("WHERE").Last();

            // replace placeholders by path conditions
            raw += "SELECT DISTINCT s.\"Id\"\r\nFROM \"SMSets\" AS s\r\nINNER JOIN \"SMESets\" AS \"s0\" ON \"s\".\"Id\" = \"s0\".\"SMId\"\r\nINNER JOIN \"ValueSets\" AS \"v\" ON \"s0\".\"Id\" = \"v\".\"SMEId\"\r\n";
            for (var i = 0; i < pathConditionsSQL.Length; i++)
            {
                var exists = $"path{i}.\"Id\" IS NOT NULL";
                convertConditionSQL = convertConditionSQL.Replace(placeholderSQL[i], exists);
                raw += $"LEFT JOIN pathCTE{i} path{i} ON s.\"Id\" = path{i}.\"Id\"\r\n";
            }
            raw += "WHERE\r\n";
            raw += convertConditionSQL;

            var resultSMId = db.Set<SMSetIdResult>()
                           .FromSqlRaw(raw)
                           .AsQueryable();

            var smRawSQL2 = resultSMId.ToQueryString();
            var qp2 = GetQueryPlan(db, smRawSQL2);

            // back to normal joinall result
            result = result.Join(resultSMId,
                r => r.SMId,
                r2 => r2.Id,
                (r, r2) => r);

            /*
            var smList = new IQueryable<int>[pathConditions.Count];
            var raw = new string[pathConditions.Count];
            // List <IQueryable> smList = new List<IQueryable>();
            for (var i = 0; i < pathConditions.Count; i++)
            {
                // var q = result.Where(anyConditions[i]).Select(r => new { r.SMId });
                var q = result.Where(pathConditions[i]).Select(r => r.SMId);
                // smList.Add(q);
                smList[i] = q;
            }

            // var parameters = smList.Cast<object>().ToArray();
            var parameters = smList as object[];
            var replaceSqlTo = new string[smList.Length];

            pathAll = pathAllCondition.Copy();
            for (var i = 0; i < smList.Length; i++)
            {
                // Change placeholder
                pathAll = pathAll.Replace($"$$path{i}$$", $"Math.Abs(SMId) == {i}");
                // pathAll = pathAll.Replace($"$$path{i}$$", $"@{i}.Contains(SMId)");
                // pathAll = pathAll.Replace($"$$path{i}$$", $"@{i}.Any(SMId == it.SMId)");
                // pathAll = pathAll.Replace($"$$path{i}$$", $"((IQueryable<int>)@{i}).Contains(sm.Id)");
                // pathAll = pathAll.Replace($"$$path{i}$$", $"Queryable.Contains(@{i}, SMId)");
                raw[i] = smList[i].ToQueryString();
                pathConditionsSQL[i] = raw[i].Split("WHERE").Last();
                raw[i] = raw[i]
                    .Replace("SELECT \"s\".\"Id\"", "SELECT 1")
                    // .Replace("FROM \"SMSets\" AS \"s\"", "")
                    // .Replace("INNER JOIN \"SMESets\" AS \"s0\" ON \"s\".\"Id\" = \"s0\".\"SMId\"", "FROM \"SMESets\" AS s0")
                    .Replace("\r\n\r\n", "\r\n");
                placeholderSQL[i] = $"abs(\"s\".\"Id\") = {i}";
                replaceSqlTo[i] = $"EXISTS ( {raw[i]} )\r\n";
            }
            // result = result.Where(config, pathAll, parameters);
            // result = result.Where(pathAll, parameters);
            var resultx = result.Where(pathAll);
            var raw2 = resultx.ToQueryString();
            var whereConditionsSQL = raw2.Split("WHERE").Last();
            for (var i = 0; i < smList.Length; i++)
            {
                raw2 = raw2.Replace(placeholderSQL[i], replaceSqlTo[i]);
            }
            var split = raw2.Split("FROM");
            // raw2 = raw2.Replace(split[0], "SELECT \"s\".\"Id\" AS SMId\r\n");
            raw2 = raw2.Replace(split[0], "SELECT DISTINCT \"s\".\"Id\"\r\n");

            raw2 = "SELECT DISTINCT s.\"Id\"\r\nFROM \"SMSets\" AS s\r\nINNER JOIN \"SMESets\" AS \"s0\" ON \"s\".\"Id\" = \"s0\".\"SMId\"\r\nINNER JOIN \"ValueSets\" AS \"v\" ON \"s0\".\"Id\" = \"v\".\"SMEId\"\r\nWHERE ";
            raw2 += whereConditionsSQL;
            for (var i = 0; i < pathConditionsSQL.Length; i++)
            {
                var c = $"EXISTS (\r\nSELECT 1\r\nWHERE s0.\"SMId\" = s.\"Id\"\r\nAND ({pathConditionsSQL[i]}))\r\n";
                raw2 = raw2.Replace(placeholderSQL[i], c);
            }

            var result2 = db.Set<SMSetIdResult>()
                           .FromSqlRaw(raw2)
                           .AsQueryable();

            var smRawSQL2 = result.ToQueryString();
            var qp2 = GetQueryPlan(db, smRawSQL);

            result = result.Join(result2,
                r => r.SMId,
                r2 => r2.Id,
                (r, r2) => r);
            */

            /* Keep old code for join, in case of performance problems with large data
            IQueryable join = result.Select($"new (sm.Id as Id, ({anyConditions[0]}) as c0)");
            // var x1 = join.Take(100).ToDynamicList();
            var selectOuter = "outer.c0 as c0";
            var whereOr = "c0";
            for (var i = 1; i < anyConditions.Count; i++)
            {
                var ji = result.Select($"new (sm.Id as Id, ({anyConditions[i]}) as c{i})");
                // var x2 = ji.Take(100).ToDynamicList();
                whereOr += $" | c{i}";
                join = join.Join(
                    ji,
                    "Id",
                    "Id",
                    $"new (outer.Id as Id, {selectOuter}, inner.c{i} as c{i})"
                    );
                selectOuter += $", outer.c{i} as c{i}";
            }
            join = join.Where(whereOr);
            var x3 = join.Take(10).ToDynamicList();

            var filteredResult = result.Join(
                join,
                "sm.Id",
                "Id",
                $"new (outer as j, inner as c)"
            );
            var x5 = filteredResult.Take(100).ToDynamicList();

            var pa = pathAllCondition.Copy();
            for (var i = 0; i < anyConditions.Count; i++)
            {
                pa = pa.Replace($"$$path{i}$$", $"c.c{i}");
            }
            pa = pa.Replace("aas.", "j.aas.").Replace("sm.", "j.sm.").Replace("sme.", "j.sme.").Replace("svalue", "j.svalue").Replace("mvalue", "j.mvalue").Replace("dtvalue", "j.dtvalue");
            // conditionAll = conditionAll.Replace("aas.", "j.aas.").Replace("sm.", "j.sm.").Replace("sme.", "j.sme.").Replace("svalue", "j.svalue.").Replace("nvalue", "j.nvalue.");

            filteredResult = filteredResult.Where(pa);
            var x6 = filteredResult.Take(100).ToDynamicList();
            // filteredResult = filteredResult.Where(conditionAll);

            result = filteredResult.Select("j") as IQueryable<joinAll>;
            */
        }
        else
        {
            if (conditionAll != "true")
            {
                result = result.Where(conditionAll);

                var smRawSQL2 = result.ToQueryString();
                var qp2 = GetQueryPlan(db, smRawSQL2);
            }
        }

        var combined = result.Select(r => new CombinedSMSMEV
        {
            SM_Id = r.sm.Id,
            SM_SemanticId = r.sm.SemanticId,
            SM_IdShort = r.sm.IdShort,
            SM_DisplayName = r.sm.DisplayName,
            SM_Description = r.sm.Description,
            SM_Identifier = r.sm.Identifier,
            SM_TimeStampTree = r.sm.TimeStampTree,

            SME_SemanticId = r.sme.SemanticId,
            SME_IdShort = r.sme.IdShort,
            SME_IdShortPath = r.sme.IdShortPath,
            SME_DisplayName = r.sme.DisplayName,
            SME_Description = r.sme.Description,
            SME_Id = r.sme.Id,
            SME_TimeStamp = r.sme.TimeStamp,

            V_Value = r.svalue,
            V_D_Value = r.mvalue
        });

        return combined;
    }

    private static IQueryable<CombinedSMSMEV> CombineTablesEXISTS(
    AasContext db,
    IQueryable<AASSet>? aasTable,
    IQueryable<SMSet> smTable,
    IQueryable<SMESet> smeTable,
    IQueryable<ValueSet>? valueTable,
    //IQueryable<IValueSet>? iValueTable,
    //IQueryable<DValueSet>? dValueTable,
    string pathAllCondition,
    List<string> pathConditions,
    string conditionAll = "true")
    {
        var q1 = aasTable?.ToQueryString();
        var q2 = smTable?.ToQueryString();
        var q3 = smeTable?.ToQueryString();
        var q4 = valueTable?.ToQueryString();

        IQueryable<joinAll>? result = null;

        if (aasTable != null)
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
                    SMId = x.sm.Id
                });
        }
        else
        {
            result = smTable.Select(sm => new joinAll
            {
                sm = sm,
                SMId = sm.Id
            });
        }

        result = result.Join(smeTable,
            r => r.sm.Id,
            sme => sme.SMId,
            (r, sme) => new joinAll
            {
                aas = r.aas,
                sm = r.sm,
                sme = sme,
                SMId = r.sm.Id
            });

        if (valueTable != null)
        {
            result = result.Join(valueTable,
            r => r.sme.Id,
            v => v.SMEId,
            (r, v) => new joinAll
            {
                SMId = r.SMId,
                aas = r.aas,
                sm = r.sm,
                sme = r.sme,
                svalue = v.SValue,
                mvalue = v.NValue,
                dtvalue = v.DTValue,
            });
        }

        var smRawSQL = result.ToQueryString();
        var qp = GetQueryPlan(db, smRawSQL);

        if (pathConditions.Count != 0)
        {
            // sme with idShortPath use CTE (common table expressions)
            // for each idShortPath there is 1 CTE: pathCTE#
            // Convert C# conditions to SQL conditions by EF
            // Use placeholder in condition conversion: "Math.Abs(SMId)"
            // Include SQL conditions into SQL
            // Convert result back to EF by SMId only

            var placeholderSQL = new string[pathConditions.Count];
            var exists = new string[pathConditions.Count];
            var pathConditionsSQL = new string[pathConditions.Count];
            var pathAllExists = pathAllCondition.Copy();

            // var raw = "WITH\r\n";

            for (var i = 0; i < pathConditions.Count; i++)
            {
                // convert path condition to SQL syntax
                var convertPathCondition = result.Where(pathConditions[i]).Select(r => r.SMId);
                var convertPathConditionSQL = convertPathCondition.ToQueryString();
                var where = convertPathConditionSQL.Split("WHERE").Last();
                pathConditionsSQL[i] = where;

                if (i != 0)
                {
                    // raw += ",\r\n";
                }

                // Define CTE for idShortPath with condition
                // raw += $"pathCTE{i} AS (\r\nSELECT s.\"Id\"\r\nFROM \"SMSets\" AS s\r\nINNER JOIN \"SMESets\" AS s0 ON s.\"Id\" = s0.\"SMId\"\r\nINNER JOIN \"ValueSets\" AS v ON s0.\"Id\" = v.\"SMEId\"\r\nWHERE\r\n";
                // raw += where + "\r\n)\r\n";
                var ii = i + 1;
                where = where.Replace("\"s0\".", $"s{ii}.").Replace("\"v\".", $"v{ii}.");
                exists[i] = $"EXISTS (\r\nSELECT 1\r\nFROM SMESets AS s{ii}\r\nJOIN ValueSets AS v{ii} ON s{ii}.Id = v{ii}.SMEId\r\nWHERE s{ii}.SMId = s.Id AND\r\n{where})\r\n";

                // change placeholder in complete condition
                // pathAllExists = pathAllExists.Replace($"$$path{i}$$", $"Math.Abs(SMId) == {i}");
                pathAllExists = pathAllExists.Replace($"$$path{i}$$", $"Math.Abs(SMId) == {i}");
                // placeholder after conversion to SQL
                placeholderSQL[i] = $"abs(\"s\".\"Id\") = {i}";
            }

            // convert complete condition with placeholders
            var convertCondition = result.Where(pathAllExists);
            var convertConditionSQL = convertCondition.ToQueryString();
            convertConditionSQL = convertConditionSQL.Split("WHERE").Last();

            // replace other conditions also by EXISTS
            var convertAll = convertConditionSQL.Copy();
            if (convertAll.Contains("\"v\"."))
            {
                var ii = pathConditions.Count + 1;
                var split1 = convertAll.Split("\"v\".");
                foreach (var s1 in split1)
                {
                    var split2 = s1.Split(" ");
                    if (split2[0] == "\"SValue\"" || split2[0] == "\"NValue\"")
                    {
                        var s = split2[0] + " " + split2[1] + " " + split2[2];
                        s = s.Replace(")", "");
                        var exists2 = $"EXISTS (\r\nSELECT 1\r\nFROM SMESets AS s{ii}\r\nJOIN ValueSets AS v{ii} ON s{ii}.Id = v{ii}.SMEId\r\nWHERE s{ii}.SMId = s.Id AND\r\nv{ii}.{s})\r\n";
                        convertAll = convertAll.Replace("\"v\"." + s, exists2);
                        ii++;
                    }
                }
            }

            // replace placeholders by path conditions
            // var raw = "SELECT DISTINCT s.\"Id\"\r\nFROM \"SMSets\" AS s\r\nINNER JOIN \"SMESets\" AS \"s0\" ON \"s\".\"Id\" = \"s0\".\"SMId\"\r\nINNER JOIN \"ValueSets\" AS \"v\" ON \"s0\".\"Id\" = \"v\".\"SMEId\"\r\n";
            var raw = "SELECT DISTINCT s.\"Id\"\r\nFROM \"SMSets\" AS s\r\n";
            for (var i = 0; i < pathConditionsSQL.Length; i++)
            {
                // var exists = $"path{i}.\"Id\" IS NOT NULL";
                convertAll = convertAll.Replace(placeholderSQL[i], exists[i]);
                // raw += $"LEFT JOIN pathCTE{i} path{i} ON s.\"Id\" = path{i}.\"Id\"\r\n";
            }
            raw += "WHERE\r\n";
            raw += convertAll;

            var resultSMId = db.Set<SMSetIdResult>()
                           .FromSqlRaw(raw)
                           .AsQueryable();

            var smRawSQL2 = resultSMId.ToQueryString();
            var qp2 = GetQueryPlan(db, smRawSQL2);

            // back to normal joinall result
            result = result.Join(resultSMId,
                r => r.SMId,
                r2 => r2.Id,
                (r, r2) => r);

            /*
            var smList = new IQueryable<int>[pathConditions.Count];
            var raw = new string[pathConditions.Count];
            // List <IQueryable> smList = new List<IQueryable>();
            for (var i = 0; i < pathConditions.Count; i++)
            {
                // var q = result.Where(anyConditions[i]).Select(r => new { r.SMId });
                var q = result.Where(pathConditions[i]).Select(r => r.SMId);
                // smList.Add(q);
                smList[i] = q;
            }

            // var parameters = smList.Cast<object>().ToArray();
            var parameters = smList as object[];
            var replaceSqlTo = new string[smList.Length];

            pathAll = pathAllCondition.Copy();
            for (var i = 0; i < smList.Length; i++)
            {
                // Change placeholder
                pathAll = pathAll.Replace($"$$path{i}$$", $"Math.Abs(SMId) == {i}");
                // pathAll = pathAll.Replace($"$$path{i}$$", $"@{i}.Contains(SMId)");
                // pathAll = pathAll.Replace($"$$path{i}$$", $"@{i}.Any(SMId == it.SMId)");
                // pathAll = pathAll.Replace($"$$path{i}$$", $"((IQueryable<int>)@{i}).Contains(sm.Id)");
                // pathAll = pathAll.Replace($"$$path{i}$$", $"Queryable.Contains(@{i}, SMId)");
                raw[i] = smList[i].ToQueryString();
                pathConditionsSQL[i] = raw[i].Split("WHERE").Last();
                raw[i] = raw[i]
                    .Replace("SELECT \"s\".\"Id\"", "SELECT 1")
                    // .Replace("FROM \"SMSets\" AS \"s\"", "")
                    // .Replace("INNER JOIN \"SMESets\" AS \"s0\" ON \"s\".\"Id\" = \"s0\".\"SMId\"", "FROM \"SMESets\" AS s0")
                    .Replace("\r\n\r\n", "\r\n");
                placeholderSQL[i] = $"abs(\"s\".\"Id\") = {i}";
                replaceSqlTo[i] = $"EXISTS ( {raw[i]} )\r\n";
            }
            // result = result.Where(config, pathAll, parameters);
            // result = result.Where(pathAll, parameters);
            var resultx = result.Where(pathAll);
            var raw2 = resultx.ToQueryString();
            var whereConditionsSQL = raw2.Split("WHERE").Last();
            for (var i = 0; i < smList.Length; i++)
            {
                raw2 = raw2.Replace(placeholderSQL[i], replaceSqlTo[i]);
            }
            var split = raw2.Split("FROM");
            // raw2 = raw2.Replace(split[0], "SELECT \"s\".\"Id\" AS SMId\r\n");
            raw2 = raw2.Replace(split[0], "SELECT DISTINCT \"s\".\"Id\"\r\n");

            raw2 = "SELECT DISTINCT s.\"Id\"\r\nFROM \"SMSets\" AS s\r\nINNER JOIN \"SMESets\" AS \"s0\" ON \"s\".\"Id\" = \"s0\".\"SMId\"\r\nINNER JOIN \"ValueSets\" AS \"v\" ON \"s0\".\"Id\" = \"v\".\"SMEId\"\r\nWHERE ";
            raw2 += whereConditionsSQL;
            for (var i = 0; i < pathConditionsSQL.Length; i++)
            {
                var c = $"EXISTS (\r\nSELECT 1\r\nWHERE s0.\"SMId\" = s.\"Id\"\r\nAND ({pathConditionsSQL[i]}))\r\n";
                raw2 = raw2.Replace(placeholderSQL[i], c);
            }

            var result2 = db.Set<SMSetIdResult>()
                           .FromSqlRaw(raw2)
                           .AsQueryable();

            var smRawSQL2 = result.ToQueryString();
            var qp2 = GetQueryPlan(db, smRawSQL);

            result = result.Join(result2,
                r => r.SMId,
                r2 => r2.Id,
                (r, r2) => r);
            */

            /* Keep old code for join, in case of performance problems with large data
            IQueryable join = result.Select($"new (sm.Id as Id, ({anyConditions[0]}) as c0)");
            // var x1 = join.Take(100).ToDynamicList();
            var selectOuter = "outer.c0 as c0";
            var whereOr = "c0";
            for (var i = 1; i < anyConditions.Count; i++)
            {
                var ji = result.Select($"new (sm.Id as Id, ({anyConditions[i]}) as c{i})");
                // var x2 = ji.Take(100).ToDynamicList();
                whereOr += $" | c{i}";
                join = join.Join(
                    ji,
                    "Id",
                    "Id",
                    $"new (outer.Id as Id, {selectOuter}, inner.c{i} as c{i})"
                    );
                selectOuter += $", outer.c{i} as c{i}";
            }
            join = join.Where(whereOr);
            var x3 = join.Take(10).ToDynamicList();

            var filteredResult = result.Join(
                join,
                "sm.Id",
                "Id",
                $"new (outer as j, inner as c)"
            );
            var x5 = filteredResult.Take(100).ToDynamicList();

            var pa = pathAllCondition.Copy();
            for (var i = 0; i < anyConditions.Count; i++)
            {
                pa = pa.Replace($"$$path{i}$$", $"c.c{i}");
            }
            pa = pa.Replace("aas.", "j.aas.").Replace("sm.", "j.sm.").Replace("sme.", "j.sme.").Replace("svalue", "j.svalue").Replace("mvalue", "j.mvalue").Replace("dtvalue", "j.dtvalue");
            // conditionAll = conditionAll.Replace("aas.", "j.aas.").Replace("sm.", "j.sm.").Replace("sme.", "j.sme.").Replace("svalue", "j.svalue.").Replace("nvalue", "j.nvalue.");

            filteredResult = filteredResult.Where(pa);
            var x6 = filteredResult.Take(100).ToDynamicList();
            // filteredResult = filteredResult.Where(conditionAll);

            result = filteredResult.Select("j") as IQueryable<joinAll>;
            */
        }
        else
        {
            if (conditionAll != "true")
            {
                result = result.Where(conditionAll);

                var smRawSQL2 = result.ToQueryString();
                var qp2 = GetQueryPlan(db, smRawSQL2);
            }
        }

        var combined = result.Select(r => new CombinedSMSMEV
        {
            SM_Id = r.sm.Id,
            SM_SemanticId = r.sm.SemanticId,
            SM_IdShort = r.sm.IdShort,
            SM_DisplayName = r.sm.DisplayName,
            SM_Description = r.sm.Description,
            SM_Identifier = r.sm.Identifier,
            SM_TimeStampTree = r.sm.TimeStampTree,

            SME_SemanticId = r.sme.SemanticId,
            SME_IdShort = r.sme.IdShort,
            SME_IdShortPath = r.sme.IdShortPath,
            SME_DisplayName = r.sme.DisplayName,
            SME_Description = r.sme.Description,
            SME_Id = r.sme.Id,
            SME_TimeStamp = r.sme.TimeStamp,

            V_Value = r.svalue,
            V_D_Value = r.mvalue
        });

        return combined;
    }
    private static IQueryable<CombinedSMSMEV> CombineTablesWithCTE(
        AasContext db,
        IQueryable<SMSet> smTable,
        IQueryable<SMESet> smeTable,
        IQueryable<ValueSet>? sValueTable,
        bool includeEmptyValues = false)
    {
        // Hole EF-SQL
        var smRaw = smTable.ToQueryString();
        var smeRaw = smeTable.ToQueryString();
        var sValueRaw = sValueTable?.ToQueryString();
        //var iValueRaw = iValueTable?.ToQueryString();
        //var dValueRaw = dValueTable?.ToQueryString();

        // Extrahiere FROM + WHERE aus EF-SQL
        string ExtractFromWhere(string raw)
        {
            var fromIndex = raw.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
            return fromIndex > 0 ? raw.Substring(fromIndex) : raw;
        }

        var smFromWhere = ExtractFromWhere(smRaw);
        var smeFromWhere = ExtractFromWhere(smeRaw);
        var sValueFromWhere = sValueRaw != null ? ExtractFromWhere(sValueRaw) : null;
        //var iValueFromWhere = iValueRaw != null ? ExtractFromWhere(iValueRaw) : null;
        //var dValueFromWhere = dValueRaw != null ? ExtractFromWhere(dValueRaw) : null;

        // Baue CTEs mit eigener SELECT-Liste
        var sb = new StringBuilder("WITH\n");
        sb.AppendLine($"FilteredSM AS (\nSELECT \"s\".\"Id\" AS SM_Id, \"s\".\"SemanticId\" AS SM_SemanticId, \"s\".\"IdShort\" AS SM_IdShort, \"s\".\"DisplayName\" AS SM_DisplayName, \"s\".\"Description\" AS SM_Description, \"s\".\"Identifier\" AS SM_Identifier, \"s\".\"TimeStampTree\" AS SM_TimeStampTree\n{smFromWhere}\n),");
        sb.AppendLine($"FilteredSME AS (\nSELECT \"s\".\"Id\" AS SME_Id, \"s\".\"SMId\" AS SME_SMId, \"s\".\"SemanticId\" AS SME_SemanticId, \"s\".\"IdShort\" AS SME_IdShort, \"s\".\"IdShortPath\" AS SME_IdShortPath, \"s\".\"DisplayName\" AS SME_DisplayName, \"s\".\"Description\" AS SME_Description, \"s\".\"TimeStamp\" AS SME_TimeStamp, \"s\".\"TValue\" AS SME_TValue\n{smeFromWhere}\n),");

        if (sValueFromWhere != null)
            sb.AppendLine($"FilteredSValue AS (\nSELECT \"s\".\"SMEId\" AS V_SMEId, \"s\".\"Value\" AS V_Value\n{sValueFromWhere}\n),");
        //if (iValueFromWhere != null)
        //    sb.AppendLine($"FilteredIValue AS (\nSELECT \"i\".\"SMEId\" AS V_SMEId, \"i\".\"Value\" AS V_Value\n{iValueFromWhere}\n),");
        //if (dValueFromWhere != null)
        //    sb.AppendLine($"FilteredDValue AS (\nSELECT \"d\".\"SMEId\" AS V_SMEId, \"d\".\"Value\" AS V_Value\n{dValueFromWhere}\n),");

        // Join SM und SME
        sb.AppendLine(@"
FilteredSMAndSME AS (
    SELECT sm.SM_Id, sm.SM_SemanticId, sm.SM_IdShort, sm.SM_DisplayName, sm.SM_Description,
           sm.SM_Identifier, sm.SM_TimeStampTree, sme.SME_SemanticId, sme.SME_IdShort,
           sme.SME_IdShortPath, sme.SME_DisplayName, sme.SME_Description, sme.SME_Id,
           sme.SME_TimeStamp, sme.SME_TValue
    FROM FilteredSM AS sm
    INNER JOIN FilteredSME AS sme ON sm.SM_Id = sme.SME_SMId
)
");

        // Haupt-SELECT mit UNION ALL
        sb.AppendLine("SELECT sm_sme.SM_Id, sm_sme.SM_SemanticId, sm_sme.SM_IdShort, sm_sme.SM_DisplayName, sm_sme.SM_Description,");
        sb.AppendLine("       sm_sme.SM_Identifier, sm_sme.SM_TimeStampTree, sm_sme.SME_SemanticId, sm_sme.SME_IdShort, sm_sme.SME_IdShortPath,");
        sb.AppendLine("       sm_sme.SME_DisplayName, sm_sme.SME_Description, sm_sme.SME_Id, sm_sme.SME_TimeStamp, v.V_Value, NULL AS V_D_Value");
        sb.AppendLine("FROM FilteredSMAndSME AS sm_sme INNER JOIN FilteredSValue AS v ON sm_sme.SME_Id = v.V_SMEId");

        //if (iValueFromWhere != null)
        //{
        //    sb.AppendLine("UNION ALL");
        //    sb.AppendLine("SELECT sm_sme.SM_Id, sm_sme.SM_SemanticId, sm_sme.SM_IdShort, sm_sme.SM_DisplayName, sm_sme.SM_Description,");
        //    sb.AppendLine("       sm_sme.SM_Identifier, sm_sme.SM_TimeStampTree, sm_sme.SME_SemanticId, sm_sme.SME_IdShort, sm_sme.SME_IdShortPath,");
        //    sb.AppendLine("       sm_sme.SME_DisplayName, sm_sme.SME_Description, sm_sme.SME_Id, sm_sme.SME_TimeStamp, v.V_Value, v.V_Value AS V_D_Value");
        //    sb.AppendLine("FROM FilteredSMAndSME AS sm_sme INNER JOIN FilteredIValue AS v ON sm_sme.SME_Id = v.V_SMEId");
        //}

        //if (dValueFromWhere != null)
        //{
        //    sb.AppendLine("UNION ALL");
        //    sb.AppendLine("SELECT sm_sme.SM_Id, sm_sme.SM_SemanticId, sm_sme.SM_IdShort, sm_sme.SM_DisplayName, sm_sme.SM_Description,");
        //    sb.AppendLine("       sm_sme.SM_Identifier, sm_sme.SM_TimeStampTree, sm_sme.SME_SemanticId, sm_sme.SME_IdShort, sm_sme.SME_IdShortPath,");
        //    sb.AppendLine("       sm_sme.SME_DisplayName, sm_sme.SME_Description, sm_sme.SME_Id, sm_sme.SME_TimeStamp, v.V_Value, v.V_Value AS V_D_Value");
        //    sb.AppendLine("FROM FilteredSMAndSME AS sm_sme INNER JOIN FilteredDValue AS v ON sm_sme.SME_Id = v.V_SMEId");
        //}

        if (includeEmptyValues)
        {
            sb.AppendLine("UNION ALL");
            sb.AppendLine("SELECT sm_sme.SM_Id, sm_sme.SM_SemanticId, sm_sme.SM_IdShort, sm_sme.SM_DisplayName, sm_sme.SM_Description,");
            sb.AppendLine("       sm_sme.SM_Identifier, sm_sme.SM_TimeStampTree, sm_sme.SME_SemanticId, sm_sme.SME_IdShort, sm_sme.SME_IdShortPath,");
            sb.AppendLine("       sm_sme.SME_DisplayName, sm_sme.SME_Description, sm_sme.SME_Id, sm_sme.SME_TimeStamp, NULL, NULL");
            sb.AppendLine("FROM FilteredSMAndSME AS sm_sme WHERE sm_sme.SME_TValue IS NULL OR sm_sme.SME_TValue = ''");
        }

        var finalSql = sb.ToString();

        // Ausführen und typisierte Ergebnisse zurückgeben
        return db.Database.SqlQueryRaw<CombinedSMSMEV>(finalSql).AsQueryable();
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
                                            conditions[i].Add("all", le._expression);
                                            QueryGrammarJSON.createExpression("all-aas", le);
                                            conditions[i].Add("all-aas", le._expression);
                                            QueryGrammarJSON.createExpression("aas.", le);
                                            conditions[i].Add("aas.", le._expression);
                                            QueryGrammarJSON.createExpression("sm.", le);
                                            conditions[i].Add("sm.", le._expression);
                                            QueryGrammarJSON.createExpression("sme.", le);
                                            conditions[i].Add("sme.", le._expression);
                                            /*
                                            QueryGrammarJSON.createExpression("svalue", le);
                                            conditions[i].Add("svalue", le._expression);
                                            QueryGrammarJSON.createExpression("mvalue", le);
                                            conditions[i].Add("mvalue", le._expression);
                                            */
                                            QueryGrammarJSON.createExpression("value", le);
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

