using IO.Swagger.V1RC03.ApiModel;
using IO.Swagger.V1RC03.Attributes;
using IO.Swagger.V1RC03.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Controllers
{
    public class AASQueryAPIController : ControllerBase, IAASQueryAPIController
    {
        private readonly IBase64UrlDecoderService _decoderService;

        public AASQueryAPIController(IBase64UrlDecoderService decoderService)
        {
            _decoderService = decoderService;
        }


        [HttpGet]
        [Route("/query/{searchQuery}")]
        [ValidateModelState]
        [SwaggerOperation("GetQuery")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "Query Result")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetQuery([FromRoute][Required] string searchQuery)
        {
            var decodedQuery = _decoderService.Decode("searchQuery", searchQuery);
            return NoContent();
        }

        [HttpPost]
        [Route("/query")]
        [ValidateModelState]
        [SwaggerOperation("PostQuery")]
        [SwaggerResponse(statusCode: 200, type: typeof(OperationResult), description: "Query created successfully.")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 405, type: typeof(Result), description: "Method not allowed - Invoke only valid for Operation submodel element")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult PostQuery([FromBody][Required] string searchQuery)
        {
            return NoContent();
        }

        [HttpGet]
        [Route("/queryregistry/{searchQuery}")]
        [ValidateModelState]
        [SwaggerOperation("GetQueryRegistry")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "Query Result")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetQueryRegistry([FromRoute][Required] string searchQuery)
        {
            var decodedQuery = _decoderService.Decode("searchQuery", searchQuery);
            return NoContent();
        }

        [HttpPost]
        [Route("/queryregistry")]
        [ValidateModelState]
        [SwaggerOperation("PostQueryRegistry")]
        [SwaggerResponse(statusCode: 200, type: typeof(OperationResult), description: "Query Registry created successfully.")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 405, type: typeof(Result), description: "Method not allowed - Invoke only valid for Operation submodel element")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult PostQueryRegistry([FromBody][Required] string queryRegistry)
        {
            return NoContent();
        }
    }
}
