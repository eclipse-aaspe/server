
namespace UA_CloudLibrary.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System.Net;
    using System.Threading.Tasks;

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
        public Task<HttpStatusCode> ListAAS()
        {
            return Task.FromResult(HttpStatusCode.NotImplemented);
        }

        [HttpGet("download")]
        public Task<HttpStatusCode> Download(string aasName)
        {
            return Task.FromResult(HttpStatusCode.NotImplemented);
        }

        [HttpPut("upload")]
        public Task<HttpStatusCode> Upload(string base64EncodedAAS)
        {
            return Task.FromResult(HttpStatusCode.NotImplemented);
        }
    }
}
