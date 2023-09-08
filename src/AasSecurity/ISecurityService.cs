using AasSecurity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace AasSecurity
{
    public interface ISecurityService
    {
        AuthenticationTicket AuthenticateRequest(HttpContext context, string route, string httpOperation, string authenticationSchemeName = null);
        bool AuthorizeRequest(string accessRole, string httpRoute, AccessRights neededRights, out string error, out bool withAllow, string objPath = null, string aasResourceType = null, IClass aasResource = null);

        string GetSecurityRules();
    }
}
