using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Controllers
{
    public interface IAASQueryAPIController
    {
        IActionResult GetQuery([FromRoute, Required] string searchQuery);
        IActionResult GetQueryRegistryOnly([FromRoute, Required] string searchQuery); 
        IActionResult GetQueryRegistry([FromRoute, Required] string searchQuery);
        // IActionResult PostQuery([FromBody, Required] string searchQuery);
        Task<IActionResult> PostQueryAsync();
        Task<IActionResult> PostQueryRegistryAsync();
        // IActionResult PostQueryRegistry([FromBody, Required] string queryRegistry);
    }
}