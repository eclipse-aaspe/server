
namespace UA_CloudLibrary.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Swashbuckle.AspNetCore.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Net;

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AASXRESTController : ControllerBase
    {
        private readonly ILogger<AASXRESTController> _logger;

        public AASXRESTController(ILogger<AASXRESTController> logger)
        {
            _logger = logger;
        }

        [HttpGet("listaas")]
        [SwaggerResponse((int) HttpStatusCode.OK, Type = typeof(List<string>))]
        public IActionResult ListAAS()
        {
            return new OkObjectResult(new List<string>());
        }

        [HttpGet("download/{aasName}")]
        [SwaggerResponse((int) HttpStatusCode.OK, Type = typeof(AssetAdminShell))]
        [SwaggerResponse((int) HttpStatusCode.InternalServerError, Type = typeof(string))]
        public IActionResult Download(string aasName)
        {
            try
            {
                // TODO: retrieve AAS
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex.Message) { StatusCode = (int) HttpStatusCode.InternalServerError };
            }

            return new ObjectResult(new AssetAdminShell());
        }

        [HttpPut("upload/{base64EncodedAAS}")]
        [SwaggerResponse((int) HttpStatusCode.NotImplemented)]
        public IActionResult Upload(string base64EncodedAAS)
        {
            return new StatusCodeResult((int) HttpStatusCode.NotImplemented);
        }
    }
}
