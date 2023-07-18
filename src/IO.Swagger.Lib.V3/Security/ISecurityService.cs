using Microsoft.AspNetCore.Http;

namespace IO.Swagger.Lib.V3.Security
{
    public interface ISecurityService
    {
        void SecurityCheck(string objPath = "", string aasOrSubmodel = null, object objectAasOrSubmodel = null);
        void SecurityCheckInit(HttpContext context, string route, string httpOperation);
    }
}
