using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace IO.Swagger.V1RC03.Controllers
{
    public interface IAASQueryAPIController
    {
        IActionResult GetQuery([FromRoute, Required] string searchQuery);
        IActionResult GetQueryRegistry([FromRoute, Required] string searchQuery);
        IActionResult PostQuery([FromBody, Required] string searchQuery);
        IActionResult PostQueryRegistry([FromBody, Required] string queryRegistry);
    }
}