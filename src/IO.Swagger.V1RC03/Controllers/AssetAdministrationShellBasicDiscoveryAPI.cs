/*
 * DotAAS Part 2 | HTTP/REST | Entire API Collection
 *
 * The entire API collection as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: V1.0RC03
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using IO.Swagger.V1RC03.Attributes;

using Microsoft.AspNetCore.Authorization;
using IO.Swagger.V1RC03.Models;
using IO.Swagger.V1RC03.ApiModel;


namespace IO.Swagger.V1RC03.Controllers
{ 
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    public class AssetAdministrationShellBasicDiscoveryApiController : ControllerBase, IAssetAdministrationShellBasicDiscoveryApiController
    { 
        /// <summary>
        /// Deletes all specific Asset identifiers linked to an Asset Administration Shell to edit discoverable content
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Specific Asset identifiers deleted successfully</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpDelete]
        [Route("/lookup/shells/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("DeleteAllAssetLinksById")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult DeleteAllAssetLinksById([FromRoute][Required]byte[] aasIdentifier)
        { 
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a list of Asset Administration Shell ids linked to specific Asset identifiers
        /// </summary>
        /// <param name="assetIds">A list of specific Asset identifiers</param>
        /// <response code="200">Requested Asset Administration Shell ids</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/lookup/shells")]
        [ValidateModelState]
        [SwaggerOperation("GetAllAssetAdministrationShellIdsByAssetLink")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<string>), description: "Requested Asset Administration Shell ids")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetAllAssetAdministrationShellIdsByAssetLink([FromQuery]List<AasCore.Aas3_0_RC02.SpecificAssetId> assetIds)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(List<string>));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = "[ \"\", \"\" ]";
            
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<List<string>>(exampleJson)
                        : default(List<string>);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Returns a list of specific Asset identifiers based on an Asset Administration Shell id to edit discoverable content
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested specific Asset identifiers</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/lookup/shells/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("GetAllAssetLinksById")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<AasCore.Aas3_0_RC02.SpecificAssetId>), description: "Requested specific Asset identifiers")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetAllAssetLinksById([FromRoute][Required]byte[] aasIdentifier)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(List<AasCore.Aas3_0_RC02.SpecificAssetId>));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = "[ \"\", \"\" ]";
            
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<List<AasCore.Aas3_0_RC02.SpecificAssetId>>(exampleJson)
                        : default(List<AasCore.Aas3_0_RC02.SpecificAssetId>);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Creates specific Asset identifiers linked to an Asset Administration Shell to edit discoverable content
        /// </summary>
        /// <param name="body">A list of specific Asset identifiers</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="201">Specific Asset identifiers created successfully</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpPost]
        [Route("/lookup/shells/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("PostAllAssetLinksById")]
        [SwaggerResponse(statusCode: 201, type: typeof(List<AasCore.Aas3_0_RC02.SpecificAssetId>), description: "Specific Asset identifiers created successfully")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult PostAllAssetLinksById([FromBody]List<AasCore.Aas3_0_RC02.SpecificAssetId> body, [FromRoute][Required]string aasIdentifier)
        { 
            //TODO: Uncomment the next line to return response 201 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(201, default(List<AasCore.Aas3_0_RC02.SpecificAssetId>));

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = "[ \"\", \"\" ]";
            
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<List<AasCore.Aas3_0_RC02.SpecificAssetId>>(exampleJson)
                        : default(List<AasCore.Aas3_0_RC02.SpecificAssetId>);            //TODO: Change the data returned
            return new ObjectResult(example);
        }
    }
}
