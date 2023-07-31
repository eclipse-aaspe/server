using Microsoft.AspNetCore.Http;

namespace AasSecurity
{
    public interface ISecurityService
    {
        void SecurityCheck(HttpContext httpContext, string objPath = "", string aasResourceType = null, IClass aasResource = null);
        void SecurityCheckInit(HttpContext context, string route, string httpOperation);
    }
}
