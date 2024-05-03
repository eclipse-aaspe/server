
using AasSecurity.Exceptions;
using AasxServer;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using IO.Swagger.Attributes;
using IO.Swagger.Controllers;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.Services;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace IO.Swagger.Lib.V3.Controllers
{
    public class AasFragmentApiController : ControllerBase
    {
        private readonly IAppLogger<AasFragmentApiController> _logger;
        private readonly IBase64UrlDecoderService _decoderService;
        public AasFragmentApiController(IAppLogger<AasFragmentApiController> logger, IBase64UrlDecoderService decoderService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _decoderService = decoderService ?? throw new ArgumentNullException(nameof(decoderService)); ;
        }

        /// <summary>
        /// Returns a specific submodel element from the Submodel at a specified path
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="idShortPath">IdShort path to the submodel element (dot-separated)</param>
        /// <param name="fragmentType">Fragment Type</param>
        /// <param name="fragment">Fragment</param>
        /// <param name="level">Determines the structural depth of the respective resource content</param>
        /// <param name="extent">Determines to which extent the resource is being serialized</param>
        /// <response code="200">Requested submodel element</response>
        /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
        /// <response code="401">Unauthorized, e.g. the server refused the authorization attempt.</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/shells/{aasIdentifier}/submodels/{submodelIdentifier}/submodel-elements/{idShortPath}/fragmentTypes/{fragmentType}/fragments/{fragment}")]
        [ValidateModelState]
        [SwaggerOperation("GetSubmodelElementByPath")]
        [SwaggerResponse(statusCode: 200, type: typeof(ISubmodelElement), description: "Requested submodel element")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request, e.g. the request parameters of the format of the request body is wrong.")]
        [SwaggerResponse(statusCode: 401, type: typeof(Result), description: "Unauthorized, e.g. the server refused the authorization attempt.")]
        [SwaggerResponse(statusCode: 403, type: typeof(Result), description: "Forbidden")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 500, type: typeof(Result), description: "Internal Server Error")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetSubmodelElementByPath([FromRoute][Required] string aasIdentifier, [FromRoute][Required] string submodelIdentifier, [FromRoute][Required] string idShortPath, [FromRoute][Required] string fragmentType, [FromRoute][Required]string fragment, [FromQuery] LevelEnum level, [FromQuery] ExtentEnum extent)
        {
            var decodedAasIdentifier = _decoderService.Decode("aasIdentifier", aasIdentifier);
            var decodedSubmodelIdentifier = _decoderService.Decode("submodelIdentifier", submodelIdentifier);

            _logger.LogInformation($"Received request to get the submodel element at {idShortPath} from the submodel with id {submodelIdentifier} and the AAS with id {aasIdentifier}.");


            return new ObjectResult("success");
        }
    }
}
