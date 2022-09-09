using AasxRestServerLibrary;
using Grapevine.Interfaces.Server;
using IO.Swagger.V1RC03.ApiModel;
using IO.Swagger.V1RC03.Attributes;
using IO.Swagger.V1RC03.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
            // var decodedQuery = _decoderService.Decode("searchQuery", searchQuery);

            string result = AasxRestServer.TestResource.runQuery(searchQuery, "");

            return new ObjectResult(result);
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

        // public virtual IActionResult PostQuery([FromBody][Required] string searchQuery)
        public virtual async Task<IActionResult> PostQueryAsync()
        {
            string content = await new StreamReader(Request.Body).ReadToEndAsync();
            string result = AasxRestServer.TestResource.runQuery("", content);

            return new ObjectResult(result);
        }

        [HttpGet]
        [Route("/queryregistry/{searchQuery}")]
        [ValidateModelState]
        [SwaggerOperation("GetQueryRegistry")]
        [SwaggerResponse(statusCode: 200, type: typeof(string), description: "Query Result")]
        [SwaggerResponse(statusCode: 0, type: typeof(Result), description: "Default error handling for unmentioned status codes")]
        public virtual IActionResult GetQueryRegistry([FromRoute][Required] string searchQuery)
        {
            // var decodedQuery = _decoderService.Decode("searchQuery", searchQuery);

            string result = "";
            string query = searchQuery;

            var handler = new HttpClientHandler();
            var proxy = AasxServer.AasxTask.proxy;
            if (proxy != null)
                handler.Proxy = proxy;
            else
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);

            AasxRestServer.TestResource.initListOfRepositories();
            bool error = false;
            Task task = null;
            HttpResponseMessage response = null;
            foreach (var r in AasxRestServer.TestResource.listofRepositories)
            {
                string requestPath = r + "/query/";
                requestPath += query;
                try
                {
                    task = Task.Run(async () =>
                            {
                                response = await client.GetAsync(requestPath, HttpCompletionOption.ResponseHeadersRead);
                            }
                        );
                    task.Wait();
                    if (response.IsSuccessStatusCode)
                    {
                        result += response.Content.ReadAsStringAsync().Result;
                    }
                    else
                        error = true;
                }
                catch
                {
                    error = true;
                }
                if (error)
                    result += "\nerror " + requestPath + "\n\n";
            }

            return new ObjectResult(result);
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
        public virtual async Task<IActionResult> PostQueryRegistryAsync()
        {
            string content = await new StreamReader(Request.Body).ReadToEndAsync();
            string result = "";

            var handler = new HttpClientHandler();
            var proxy = AasxServer.AasxTask.proxy;
            if (proxy != null)
                handler.Proxy = proxy;
            else
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);

            AasxRestServer.TestResource.initListOfRepositories();
            bool error = false;
            Task task = null;
            HttpResponseMessage response = null;
            foreach (var r in AasxRestServer.TestResource.listofRepositories)
            {
                string requestPath = r + "/query/";
                try
                {
                    task = Task.Run(async () =>
                            {
                                response = await client.PostAsync(requestPath, new StringContent(content));
                            }
                        );
                    task.Wait();
                    if (response.IsSuccessStatusCode)
                    {
                        result += response.Content.ReadAsStringAsync().Result;
                    }
                    else
                        error = true;
                }
                catch
                {
                    error = true;
                }
                if (error)
                    result += "\nerror " + requestPath + "\n\n";
            }

            return new ObjectResult(result);

            // return NoContent();
        }
    }
}
