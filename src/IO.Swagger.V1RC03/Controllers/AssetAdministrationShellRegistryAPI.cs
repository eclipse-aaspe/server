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
using IO.Swagger.V1RC03.ApiModel;


namespace IO.Swagger.V1RC03.Controllers
{ 
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    public class AssetAdministrationShellRegistryApiController : ControllerBase, IAssetAdministrationShellRegistryApiController
    { 
        /// <summary>
        /// Deletes an Asset Administration Shell Descriptor, i.e. de-registers an AAS
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Asset Administration Shell Descriptor deleted successfully</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpDelete]
        [Route("/shell-descriptors/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("DeleteAssetAdministrationShellDescriptorById")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult DeleteAssetAdministrationShellDescriptorById([FromRoute][Required]string aasIdentifier)
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
        /// Deletes a Submodel Descriptor, i.e. de-registers a submodel
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Submodel Descriptor deleted successfully</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpDelete]
        [Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors/{submodelIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("DeleteSubmodelDescriptorByIdAASRegistry")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult DeleteSubmodelDescriptorByIdAASRegistry([FromRoute][Required]string aasIdentifier, [FromRoute][Required]string submodelIdentifier)
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
        /// Returns all Asset Administration Shell Descriptors
        /// </summary>
        /// <response code="200">Requested Asset Administration Shell Descriptors</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/shell-descriptors")]
        [ValidateModelState]
        [SwaggerOperation("GetAllAssetAdministrationShellDescriptors")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<AssetAdministrationShellDescriptor>), description: "Requested Asset Administration Shell Descriptors")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetAllAssetAdministrationShellDescriptors()
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(List<AssetAdministrationShellDescriptor>));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = null;
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<List<AssetAdministrationShellDescriptor>>(exampleJson)
                        : default(List<AssetAdministrationShellDescriptor>);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Returns all Submodel Descriptors
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Submodel Descriptors</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors")]
        [ValidateModelState]
        [SwaggerOperation("GetAllSubmodelDescriptorsAASRegistry")]
        [SwaggerResponse(statusCode: 200, type: typeof(List<SubmodelDescriptor>), description: "Requested Submodel Descriptors")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetAllSubmodelDescriptorsAASRegistry([FromRoute][Required]string aasIdentifier)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(List<SubmodelDescriptor>));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = null;
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<List<SubmodelDescriptor>>(exampleJson)
                        : default(List<SubmodelDescriptor>);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Returns a specific Asset Administration Shell Descriptor
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Asset Administration Shell Descriptor</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/shell-descriptors/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("GetAssetAdministrationShellDescriptorById")]
        [SwaggerResponse(statusCode: 200, type: typeof(AssetAdministrationShellDescriptor), description: "Requested Asset Administration Shell Descriptor")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetAssetAdministrationShellDescriptorById([FromRoute][Required]string aasIdentifier)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(AssetAdministrationShellDescriptor));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = null;
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<AssetAdministrationShellDescriptor>(exampleJson)
                        : default(AssetAdministrationShellDescriptor);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Returns a specific Submodel Descriptor
        /// </summary>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="200">Requested Submodel Descriptor</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpGet]
        [Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors/{submodelIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("GetSubmodelDescriptorByIdAASRegistry")]
        [SwaggerResponse(statusCode: 200, type: typeof(SubmodelDescriptor), description: "Requested Submodel Descriptor")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetSubmodelDescriptorByIdAASRegistry([FromRoute][Required]string aasIdentifier, [FromRoute][Required]string submodelIdentifier)
        { 
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(SubmodelDescriptor));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = null;
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<SubmodelDescriptor>(exampleJson)
                        : default(SubmodelDescriptor);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Creates a new Asset Administration Shell Descriptor, i.e. registers an AAS
        /// </summary>
        /// <param name="body">Asset Administration Shell Descriptor object</param>
        /// <response code="201">Asset Administration Shell Descriptor created successfully</response>
        /// <response code="400">Bad Request</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpPost]
        [Route("/shell-descriptors")]
        [ValidateModelState]
        [SwaggerOperation("PostAssetAdministrationShellDescriptor")]
        [SwaggerResponse(statusCode: 201, type: typeof(AssetAdministrationShellDescriptor), description: "Asset Administration Shell Descriptor created successfully")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult PostAssetAdministrationShellDescriptor([FromBody]AssetAdministrationShellDescriptor body)
        { 
            //TODO: Uncomment the next line to return response 201 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(201, default(AssetAdministrationShellDescriptor));

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = null;
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<AssetAdministrationShellDescriptor>(exampleJson)
                        : default(AssetAdministrationShellDescriptor);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Creates a new Submodel Descriptor, i.e. registers a submodel
        /// </summary>
        /// <param name="body">Submodel Descriptor object</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="201">Submodel Descriptor created successfully</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpPost]
        [Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors")]
        [ValidateModelState]
        [SwaggerOperation("PostSubmodelDescriptorAASRegistry")]
        [SwaggerResponse(statusCode: 201, type: typeof(SubmodelDescriptor), description: "Submodel Descriptor created successfully")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult PostSubmodelDescriptorAASRegistry([FromBody]SubmodelDescriptor body, [FromRoute][Required]byte[] aasIdentifier)
        { 
            //TODO: Uncomment the next line to return response 201 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(201, default(SubmodelDescriptor));

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));
            string exampleJson = null;
            exampleJson = null;
                        var example = exampleJson != null
                        ? JsonConvert.DeserializeObject<SubmodelDescriptor>(exampleJson)
                        : default(SubmodelDescriptor);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Updates an existing Asset Administration Shell Descriptor
        /// </summary>
        /// <param name="body">Asset Administration Shell Descriptor object</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Asset Administration Shell Descriptor updated successfully</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpPut]
        [Route("/shell-descriptors/{aasIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("PutAssetAdministrationShellDescriptorById")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult PutAssetAdministrationShellDescriptorById([FromBody]AssetAdministrationShellDescriptor body, [FromRoute][Required]byte[] aasIdentifier)
        { 
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates an existing Submodel Descriptor
        /// </summary>
        /// <param name="body">Submodel Descriptor object</param>
        /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
        /// <response code="204">Submodel Descriptor updated successfully</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        /// <response code="0">Default error handling for unmentioned status codes</response>
        [HttpPut]
        [Route("/shell-descriptors/{aasIdentifier}/submodel-descriptors/{submodelIdentifier}")]
        [ValidateModelState]
        [SwaggerOperation("PutSubmodelDescriptorByIdAASRegistry")]
        [SwaggerResponse(statusCode: 400, type: typeof(Result), description: "Bad Request")]
        [SwaggerResponse(statusCode: 404, type: typeof(Result), description: "Not Found")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult PutSubmodelDescriptorByIdAASRegistry([FromBody]SubmodelDescriptor body, [FromRoute][Required]string aasIdentifier, [FromRoute][Required]string submodelIdentifier)
        { 
            //TODO: Uncomment the next line to return response 204 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(204);

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(Result));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(Result));

            //TODO: Uncomment the next line to return response 0 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(0, default(Result));

            throw new NotImplementedException();
        }
    }
}
