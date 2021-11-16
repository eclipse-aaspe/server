/*
 * DotAAS Part 2 | HTTP/REST | Entire Interface Collection
 *
 * The entire interface collection as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: Final-Draft
 *
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using IO.Swagger.Attributes;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IO.Swagger.Controllers
{
    [Authorize]
    [ApiController]
    public class AssetAdministrationShellRepositoryInterfaceApiController : ControllerBase
    {
        /// <summary>
        /// Deletes an Asset Administration Shell
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (BASE64-URL-encoded)</param>
        /// <response code="204">Asset Administration Shell deleted successfully</response>
        [HttpDelete]
        [Route("/shells/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("DeleteAssetAdministrationShellById")]
        public virtual IActionResult DeleteAssetAdministrationShellById([FromRoute][Required]string aasIdentifier)
        {
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all Asset Administration Shells
        /// </summary>
        /// <param name="assetIds">The key-value-pair of an Asset identifier</param>
        /// <param name="idShort">The Asset Administration Shell’s IdShort</param>
        /// <response code="200">Requested Asset Administration Shells</response>
        [HttpGet]
        [Route("/shells")]
        [ValidateModelState]
        [SwaggerOperation("GetAllAssetAdministrationShells")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<AssetAdministrationShell>), description: "Requested Asset Administration Shells")]
        public virtual IActionResult GetAllAssetAdministrationShells([FromQuery]List<IdentifierKeyValuePair> assetIds, [FromQuery]string idShort)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(List<AssetAdministrationShell>));
            string exampleJson = null;
            exampleJson = "[ \"\", \"\" ]";

                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<List<AssetAdministrationShell>>(exampleJson)
                        : default(List<AssetAdministrationShell>);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Returns a specific Asset Administration Shell
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (BASE64-URL-encoded)</param>
        /// <response code="200">Requested Asset Administration Shell</response>
        [HttpGet]
        [Route("/shells/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("GetAssetAdministrationShellById")]
        [SwaggerResponse(statusCode: 200, type: typeof(AssetAdministrationShell), description: "Requested Asset Administration Shell")]
        public virtual IActionResult GetAssetAdministrationShellById([FromRoute][Required]string aasIdentifier)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(AssetAdministrationShell));
            string exampleJson = null;
            exampleJson = "\"\"";

                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<AssetAdministrationShell>(exampleJson)
                        : default(AssetAdministrationShell);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Creates a new Asset Administration Shell
        /// </summary>
        /// <param name="body">Asset Administration Shell object</param>
        /// <response code="201">Asset Administration Shell created successfully</response>
        [HttpPost]
        [Route("/shells")]
        [ValidateModelState]
        [SwaggerOperation("PostAssetAdministrationShell")]
        [SwaggerResponse(statusCode: 201, type: typeof(AssetAdministrationShell), description: "Asset Administration Shell created successfully")]
        public virtual IActionResult PostAssetAdministrationShell([FromBody]AssetAdministrationShell body)
        {
            //TODO: Uncomment the next line to return response 201 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(201, default(AssetAdministrationShell));
            string exampleJson = null;
            exampleJson = "\"\"";

                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<AssetAdministrationShell>(exampleJson)
                        : default(AssetAdministrationShell);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Updates an existing Asset Administration Shell
        /// </summary>
        /// <param name="body">Asset Administration Shell object</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (BASE64-URL-encoded)</param>
        /// <response code="204">Asset Administration Shell updated successfully</response>
        [HttpPut]
        [Route("/shells/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("PutAssetAdministrationShellById")]
        public virtual IActionResult PutAssetAdministrationShellById([FromBody]AssetAdministrationShell body, [FromRoute][Required]string aasIdentifier)
        {
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            throw new NotImplementedException();
        }
    }
}
