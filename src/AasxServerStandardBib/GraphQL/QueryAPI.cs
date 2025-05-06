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

namespace AasxServerStandardBib.GraphQL;

using System.Diagnostics;
using Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Dynamic.Core;
using System.Linq;
using Irony.Parsing;
using HotChocolate.Resolvers;
using HotChocolate.Language;
using System;
using System.Collections.Generic;
using Contracts.QueryResult;
using HotChocolate;
using Contracts;
using System.Threading.Tasks;

//public class SMEResultRaw
//{
//    public string? SM_Identifier { get; set; }
//    public string? IdShortPath { get; set; }
//    public DateTime? SME_TimeStamp { get; set; }
//    public string? SValue { get; set; }
//    public double? MValue { get; set; }
//}

public class QueryAPI
{
    /*
    public Query(QueryGrammar queryGrammar)
    {
        grammar = queryGrammar;
    }
    private readonly QueryGrammar grammar;
    */

    public QueryAPI([Service] IDbRequestHandlerService dbRequestHandlerService)
    {
        _dbRequestHandlerService = dbRequestHandlerService;
    }

    private readonly IDbRequestHandlerService _dbRequestHandlerService;

    // --------------- API ---------------
    public async Task<QResult> SearchSMs(IResolverContext context, string semanticId = "", string identifier = "", string diff = "", string expression = "")
    {
        var parameterNames = context.ContextData.ContainsKey("ParameterNames")
            ? (List<string>)context.ContextData["ParameterNames"]
            : new List<string>();
        var withTotalCount = parameterNames.Contains("totalCount");

        var withLastId = parameterNames.Contains("lastID");

        var qresult = await _dbRequestHandlerService.QuerySearchSMs(withTotalCount, withLastId, semanticId, identifier, diff, expression);

        return qresult;
    }

    public async Task<int> CountSMs(string semanticId = "", string identifier = "", string diff = "", string expression = "")
    {
        var result = await _dbRequestHandlerService.QueryCountSMs(semanticId, identifier, diff, expression);
        return result;
    }

    public async Task<QResult> SearchSMEs(
        IResolverContext context,
        string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
        string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
    {
        // Get parameter names
        var parameterNames = context.ContextData.ContainsKey("ParameterNames")
            ? (List<string>)context.ContextData["ParameterNames"]
            : new List<string>();
        var withTotalCount = parameterNames.Contains("totalCount");

        var withLastId = parameterNames.Contains("lastID");

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
        var qresult = await _dbRequestHandlerService.QuerySearchSMEs(requested, withTotalCount, withLastId,
        smSemanticId, smIdentifier, semanticId, diff,
        contains, equal, lower, upper, expression);

        return qresult;
    }

    public async Task<int> CountSMEs(
        string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
        string contains = "", string equal = "", string lower = "", string upper = "", string expression = "")
    {
        var result = await _dbRequestHandlerService.QueryCountSMEs(smSemanticId, smIdentifier, semanticId,
            diff, contains, equal, lower, upper, expression);
        return result;
    }
}