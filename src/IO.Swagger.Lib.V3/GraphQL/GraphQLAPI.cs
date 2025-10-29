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

namespace IO.Swagger.Lib.V3.GraphQL;
using HotChocolate.Resolvers;
using HotChocolate.Language;
using System.Collections.Generic;
using Contracts.QueryResult;
using HotChocolate;
using Contracts;
using System.Threading.Tasks;
using Contracts.Pagination;
using IO.Swagger.Lib.V3.Models;
using AasxServer;


public class GraphQLAPI
{
    public GraphQLAPI([Service] IDbRequestHandlerService dbRequestHandlerService)
    {
        _dbRequestHandlerService = dbRequestHandlerService;
    }

    private readonly IDbRequestHandlerService _dbRequestHandlerService;

    //ToDo: Agree on max value, also in pagination parameters
    private const int MAX_PAGE_SIZE = 500;

    // --------------- API ---------------
    public async Task<QResult> SearchSMs(IResolverContext context, string semanticId = "", string identifier = "", string diff = "",
        string pageFrom = "", string pageSize = "", string expression = "")
    {
        var parameterNames = context.ContextData.ContainsKey("ParameterNames")
            ? (List<string>)context.ContextData["ParameterNames"]
            : new List<string>();
        var withTotalCount = parameterNames.Contains("totalCount");

        var withLastId = parameterNames.Contains("lastID");

        var pageSizeParam = string.IsNullOrEmpty(pageSize)
                || !int.TryParse(pageSize, out var parsedPageSize) ? MAX_PAGE_SIZE : parsedPageSize;

        var paginationParameters = new PaginationParameters(pageFrom, pageSizeParam);
        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var qresult = await _dbRequestHandlerService.QuerySearchSMs(securityConfig, withTotalCount, withLastId, semanticId, identifier, diff,
            paginationParameters, expression);

        return qresult;
    }

    public async Task<int> CountSMs(string semanticId = "", string identifier = "", string diff = "", string pageFrom = "", string expression = "")
    {
        var paginationParameters = new PaginationParameters(pageFrom, MAX_PAGE_SIZE);
        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var result = await _dbRequestHandlerService.QueryCountSMs(securityConfig, semanticId, identifier, diff, paginationParameters, expression);
        return result;
    }

    public async Task<QResult> SearchSMEs(
        IResolverContext context,
        string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
        string pageFrom = "", string pageSize = "", string contains = "", string equal = "", string lower = "",
        string upper = "", string expression = "")
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

        var pageSizeParam = string.IsNullOrEmpty(pageSize)
                || !int.TryParse(pageSize, out var parsedPageSize) ? MAX_PAGE_SIZE : parsedPageSize;

        var paginationParameters = new PaginationParameters(pageFrom, pageSizeParam);
        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var qresult = await _dbRequestHandlerService.QuerySearchSMEs(securityConfig, requested, withTotalCount, withLastId,
        smSemanticId, smIdentifier, semanticId, diff,
        contains, equal, lower, upper, paginationParameters, expression);

        return qresult;
    }

    public async Task<int> CountSMEs(
        string smSemanticId = "", string smIdentifier = "", string semanticId = "", string diff = "",
        string pageFrom = "", string contains = "", string equal = "",
        string lower = "", string upper = "", string expression = "")
    {
        var paginationParameters = new PaginationParameters(pageFrom, MAX_PAGE_SIZE);

        var securityConfig = new SecurityConfig(Program.noSecurity, null);
        var result = await _dbRequestHandlerService.QueryCountSMEs(securityConfig, smSemanticId, smIdentifier, semanticId,
            diff, contains, equal, lower, upper, paginationParameters, expression);
        return result;
    }
}
