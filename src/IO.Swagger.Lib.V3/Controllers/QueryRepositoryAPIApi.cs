namespace IO.Swagger.Controllers;

using AasxServer;
using System.Threading.Tasks;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using Contracts;
using IO.Swagger.Attributes;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Models;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers;
using IO.Swagger.Lib.V3.Services;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using IO.Swagger.Models;
using System.Collections.Generic;
using Contracts.DbRequests;
using Contracts.Exceptions;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Contracts.Pagination;
using System.Reflection.Emit;
using System.Xml.Linq;
using ScottPlot;
using Contracts.QueryResult;
using AdminShellNS.Extensions;

/// <summary>
/// 
/// </summary>
[Authorize(AuthenticationSchemes = "AasSecurityAuth")]
[ApiController]
public class QueryRepositoryAPIApiController : ControllerBase
{
    private readonly IAppLogger<QueryRepositoryAPIApiController> _logger;
    private readonly IDbRequestHandlerService _dbRequestHandlerService;
    private readonly ILevelExtentModifierService _levelExtentModifierService;
    private readonly IPaginationService _paginationService;
    private readonly QueryGrammarJSON _grammar;
    private readonly IValidateSerializationModifierService _validateModifierService;
    private readonly IAuthorizationService _authorizationService;

    public QueryRepositoryAPIApiController(IAppLogger<QueryRepositoryAPIApiController> logger, IDbRequestHandlerService dbRequestHandlerService,
        ILevelExtentModifierService levelExtentModifierService, IPaginationService paginationService,
        QueryGrammarJSON grammar, IValidateSerializationModifierService validateModifierService,
        IAuthorizationService authorizationService)
    {
        _logger = logger;
        _dbRequestHandlerService = dbRequestHandlerService;
        _levelExtentModifierService = levelExtentModifierService;
        _paginationService = paginationService;
        _grammar = grammar;
        _validateModifierService = validateModifierService;
        _authorizationService = authorizationService;
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
    [Route("query/submodels")]
    [ValidateModelState]
    [SwaggerOperation("PostSubmodels")]
    [SwaggerResponse(statusCode: 200, type: typeof(PagedResult), description: "Submodels created successfully")]
    [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
    [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
    [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
    [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
    [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
    public async virtual Task<IActionResult> PostSubmodels([FromQuery] int? limit, [FromQuery] string? cursor)
    {
        //Validate level and extent
        //var levelEnum = _validateModifierService.ValidateLevel(level);
        //var extentEnum = _validateModifierService.ValidateExtent(extent);

        _logger.LogInformation($"Received request to query submodels.");

        var securityConfig = new SecurityConfig(Program.noSecurity, this);
        var submodelList = new List<ISubmodel>();
        var paginationParameters = new PaginationParameters(cursor, limit);

        using (var reader = new StreamReader(HttpContext.Request.Body))
        {
            var expression = reader.ReadToEndAsync().Result;

            if (!Program.noSecurity)
            {
                var authResult = _authorizationService.AuthorizeAsync(User, "SecurityPolicy").Result;
                if (!authResult.Succeeded)
                {
                    throw new NotAllowed(authResult.Failure.FailureReasons.FirstOrDefault()?.Message ?? string.Empty);
                }
            }

            submodelList = await _dbRequestHandlerService.QueryGetSMs(securityConfig, paginationParameters, expression);

            if (submodelList.IsNullOrEmpty())
            {
                throw new NotFoundException("Queried submodels could not be found!");
            }
        }
        var submodelsPagedList = _paginationService.GetPaginatedResult(submodelList, paginationParameters);

        return new ObjectResult(submodelsPagedList);
    }
}