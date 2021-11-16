/*
 * DotAAS Part 2 | HTTP/REST | Entire Interface Collection
 *
 * The entire interface collection as part of Details of the Asset Administration Shell Part 2
 *
 * OpenAPI spec version: Final-Draft
 *
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using AasxRestServerLibrary;
using IO.Swagger.Attributes;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace IO.Swagger.Controllers
{
    [Authorize]
    [ApiController]
    public class AssetAdministrationShellInterfaceApiController : ControllerBase
    {
        private AasxHttpContextHelper _helper = new AasxHttpContextHelper();

        /// <summary>
        /// Deletes the submodel reference from the Asset Administration Shell
        /// </summary>
        /// <param name="submodelIdentifier">The Submodel’s unique id (BASE64-URL-encoded)</param>
        /// <response code="204">Submodel reference deleted successfully</response>
        [HttpDelete]
        [Route("/aas/submodels/{submodelIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("DeleteSubmodelReferenceById")]
        public virtual IActionResult DeleteSubmodelReferenceById([FromRoute][Required]string submodelIdentifier)
        {
            _helper.EvalDeleteAasAndAsset(submodelIdentifier, true);

            return new StatusCodeResult((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Returns all submodel references
        /// </summary>
        /// <response code="200">Requested submodel references</response>
        [HttpGet]
        [Route("/aas/submodels")]
        [ValidateModelState]
        [SwaggerOperation("GetAllSubmodelReferences")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<Reference>), description: "Requested submodel references")]
        public virtual IActionResult GetAllSubmodelReferences()
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(List<Reference>));
            string exampleJson = null;
            exampleJson = "[ \"\", \"\" ]";

                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<List<Reference>>(exampleJson)
                        : default(List<Reference>);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Returns the Asset Administration Shell
        /// </summary>
        /// <param name="content">Determines the request or response kind of the resource</param>
        /// <response code="200">Requested Asset Administration Shell</response>
        [HttpGet]
        [Route("/aas")]
        [ValidateModelState]
        [SwaggerOperation("GetAssetAdministrationShell")]
        [SwaggerResponse(statusCode: 200, type: typeof(AssetAdministrationShell), description: "Requested Asset Administration Shell")]
        public virtual IActionResult GetAssetAdministrationShell([FromQuery]string content)
        {
           return new ObjectResult(string.Empty/*_helper.EvalGetAasEnv(content)*/);
        }

        /// <summary>
        /// Returns the Asset Information
        /// </summary>
        /// <response code="200">Requested Asset Information</response>
        [HttpGet]
        [Route("/aas/asset-information")]
        [ValidateModelState]
        [SwaggerOperation("GetAssetInformation")]
        [SwaggerResponse(statusCode: 200, type: typeof(AssetInformation), description: "Requested Asset Information")]
        public virtual IActionResult GetAssetInformation()
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(AssetInformation));
            string exampleJson = null;
            exampleJson = "\"\"";

                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<AssetInformation>(exampleJson)
                        : default(AssetInformation);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Creates a submodel reference at the Asset Administration Shell
        /// </summary>
        /// <param name="body">Reference to the Submodel</param>
        /// <response code="201">Submodel reference created successfully</response>
        [HttpPost]
        [Route("/aas/submodels")]
        [ValidateModelState]
        [SwaggerOperation("PostSubmodelReference")]
        [SwaggerResponse(statusCode: 201, type: typeof(Reference), description: "Submodel reference created successfully")]
        public virtual IActionResult PostSubmodelReference([FromBody]Reference body)
        {
            //TODO: Uncomment the next line to return response 201 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(201, default(Reference));
            string exampleJson = null;
            exampleJson = "\"\"";

                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<Reference>(exampleJson)
                        : default(Reference);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Updates the Asset Administration Shell
        /// </summary>
        /// <param name="body">Asset Administration Shell object</param>
        /// <param name="content">Determines the request or response kind of the resource</param>
        /// <response code="204">Asset Administration Shell updated successfully</response>
        [HttpPut]
        [Route("/aas")]
        [ValidateModelState]
        [SwaggerOperation("PutAssetAdministrationShell")]
        public virtual IActionResult PutAssetAdministrationShell([FromBody]AssetAdministrationShell body, [FromQuery]string content)
        {
            //_helper.EvalPutAas(body);

            return new StatusCodeResult((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Updates the Asset Information
        /// </summary>
        /// <param name="body">Asset Information object</param>
        /// <response code="204">Asset Information updated successfully</response>
        [HttpPut]
        [Route("/aas/asset-information")]
        [ValidateModelState]
        [SwaggerOperation("PutAssetInformation")]
        public virtual IActionResult PutAssetInformation([FromBody]AssetInformation body)
        {
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            throw new NotImplementedException();
        }
    }
}
