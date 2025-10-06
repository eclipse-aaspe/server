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

namespace IO.Swagger.Controllers;

using AasxServer;
using System.Threading.Tasks;
using AasxServerStandardBib.Logging;
using Contracts;
using IO.Swagger.Attributes;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Models;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using IO.Swagger.Models;
using Contracts.Exceptions;
using Contracts.Pagination;
using AdminShellNS.Extensions;
using Contracts.Security;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.Json;
using AasxServerStandardBib.Exceptions;

/// <summary>
/// 
/// </summary>
[Authorize(AuthenticationSchemes = "AasSecurityAuth")]
[ApiController]
public class QueryRepositoryAPIApiController : ControllerBase
{
    private readonly IAppLogger<QueryRepositoryAPIApiController> _logger;
    private readonly IDbRequestHandlerService _dbRequestHandlerService;
    private readonly IPaginationService _paginationService;
    private readonly IMappingService _mappingService;

    public QueryRepositoryAPIApiController(IAppLogger<QueryRepositoryAPIApiController> logger, IDbRequestHandlerService dbRequestHandlerService,
        IPaginationService paginationService, IMappingService mappingService)
    {
        _logger = logger;
        _dbRequestHandlerService = dbRequestHandlerService;
        _paginationService = paginationService;
        _mappingService = mappingService;
    }

    /// <summary>
    /// Query Submodels
    /// </summary>
    /// <response code="201">Query created successfully</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
    /// <response code="403">Forbidden</response>
    /// <response code="409">Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="0">Default error handling for unmentioned status codes</response>
    [HttpPost]
    [Route("query/shells")]
    [ValidateModelState]
    [SwaggerOperation("PostAssetAdminstrationShells")]
    [Consumes("text/plain", "application/json")]
    [SwaggerResponse(statusCode: 200, type: typeof(QueryResult), description: "AssetAdministrationShells created successfully")]
    [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
    [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
    [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
    [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
    [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
    public virtual async Task<IActionResult> PostAssetAdminstrationShells([FromQuery] int? limit, [FromQuery] string? cursor, [FromBody] string? expression)
        => await HandleSubmodelQueryAsync(limit, cursor, ResultType.AssetAdministrationShell, expression);

    /// <summary>
    /// Query Submodels
    /// </summary>
    /// <response code="201">Query created successfully</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
    /// <response code="403">Forbidden</response>
    /// <response code="409">Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="0">Default error handling for unmentioned status codes</response>
    [HttpPost]
    [Route("query/submodels")]
    [ValidateModelState]
    [SwaggerOperation("PostSubmodels")]
    [Consumes("text/plain", "application/json")]
    [SwaggerResponse(statusCode: 200, type: typeof(QueryResult), description: "Submodels created successfully")]
    [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
    [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
    [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
    [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
    [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
    public virtual async Task<IActionResult> PostSubmodels([FromQuery] int? limit, [FromQuery] string? cursor, [FromBody] string? expression)
        => await HandleSubmodelQueryAsync(limit, cursor, ResultType.Submodel, expression);

    /// <summary>
    /// Query Submodels and return value serialization
    /// </summary>
    /// <response code="201">Query created successfully</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
    /// <response code="403">Forbidden</response>
    /// <response code="409">Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="0">Default error handling for unmentioned status codes</response>
    [HttpPost]
    [Route("query/submodels/$value")]
    [ValidateModelState]
    [SwaggerOperation("PostSubmodelsValue")]
    [Consumes("text/plain", "application/json")]
    [SwaggerResponse(statusCode: 200, type: typeof(QueryResult), description: "Submodels created successfully")]
    [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
    [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
    [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
    [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
    [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
    public virtual async Task<IActionResult> PostSubmodelsValue([FromQuery] int? limit, [FromQuery] string? cursor, [FromBody] string? expression)
        => await HandleSubmodelQueryAsync(limit, cursor, ResultType.SubmodelValue, expression);

    /// <summary>
    /// Query Submodel Elements
    /// </summary>
    /// <response code="201">Query created successfully</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
    /// <response code="403">Forbidden</response>
    /// <response code="409">Conflict, a resource which shall be created exists already. Might be thrown if a Submodel or SubmodelElement with the same ShortId is contained in a POST request.</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="0">Default error handling for unmentioned status codes</response>
    [HttpPost]
    [Route("query/submodel-elements")]
    [ValidateModelState]
    [SwaggerOperation("PostSubmodelElements")]
    [Consumes("text/plain", "application/json")]
    [SwaggerResponse(statusCode: 200, type: typeof(QueryResult), description: "Submodels created successfully")]
    [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
    [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
    [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
    [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
    [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
    public virtual async Task<IActionResult> PostSubmodelElements([FromQuery] int? limit, [FromQuery] string? cursor, [FromBody] string? expression)
        => await HandleSubmodelQueryAsync(limit, cursor, ResultType.SubmodelElement, expression);

    private async Task<IActionResult> HandleSubmodelQueryAsync(int? limit, string? cursor, ResultType resultType, string? expression)
    {
        if (expression == null)
        {
            throw new OperationNotSupported($"Expression body is empty");
        }

        _logger.LogInformation("Received request to query submodels.");

        var securityConfig = new SecurityConfig(Program.noSecurity, this, NeededRights.Read);
        var paginationParameters = new PaginationParameters(cursor, limit);

        var list = await _dbRequestHandlerService.QueryGetSMs(securityConfig, paginationParameters, resultType.ToString(), expression);

        if (list.IsNullOrEmpty())
        {
            throw new NotFoundException("Queried submodels could not be found!");
        }

        if (resultType == ResultType.SubmodelValue)
        {
            try
            {
                var submodels = list.Cast<Submodel>().ToList();
                var valueList = new List<object>();

                foreach (var submodel in submodels)
                {
                    var submodelValue = _mappingService.Map(submodel, "value");
                    if (submodelValue != null)
                    {
                        valueList.Add(submodelValue);
                    }
                }

                var submodelsPagedList = _paginationService.GetPaginatedQueryResult(valueList, paginationParameters);
                return new ObjectResult(submodelsPagedList);
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException("List contains non-Submodel items.");
            }
        }
        else
        {
            var submodelsPagedList = _paginationService.GetPaginatedQueryResult(list, paginationParameters);
            return new ObjectResult(submodelsPagedList);
        }
    }
}
