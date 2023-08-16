using AasSecurity.Models;
using AasxServerStandardBib.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AasSecurity
{
    public class AasSecurityAuthorizationHandler : AuthorizationHandler<SecurityRequirement, object>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityService _securityService;
        private static ILogger _logger = ApplicationLogging.CreateLogger("SecurityHandler");

        public AasSecurityAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ISecurityService securityService)
        {
            _httpContextAccessor = httpContextAccessor;
            _securityService = securityService;
        }


        //protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SecurityRequirement requirement)
        //{
        //    Console.WriteLine($"Authorizing the request.");
        //    //var httpRequest = _httpContextAccessor.HttpContext!.Request;
        //    //if (httpRequest != null)
        //    //{
        //    //    var httpMethod = httpRequest.Method;
        //    //    var httpRoute = httpRequest.Path.Value;
        //    //    if (httpMethod.Equals("delete", StringComparison.OrdinalIgnoreCase) ||
        //    //        httpMethod.Equals("post", StringComparison.OrdinalIgnoreCase) ||
        //    //        httpMethod.Equals("put", StringComparison.OrdinalIgnoreCase))
        //    //    {
        //    //        context.Succeed(requirement);
        //    //        return Task.CompletedTask;
        //    //    }

        //    //    if (httpMethod.Equals("get", StringComparison.OrdinalIgnoreCase) && httpRoute.Equals("/shells"))
        //    //    {
        //    //        context.Succeed(requirement);
        //    //        return Task.CompletedTask;
        //    //    }

        //    //    _securityService.SecurityCheckInit(_httpContextAccessor.HttpContext, httpRoute, httpMethod);
        //    //    //_securityService.SecurityCheck();
        //    //}

        //    context.Succeed(requirement);
        //    return Task.CompletedTask;
        //}

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SecurityRequirement requirement, object resource)
        {
            _logger.LogDebug("Authorizing the request");
            if (!GlobalSecurityVariables.WithAuthentication)
            {
                _logger.LogDebug("Server is configured without security. Therefore, skipping authorization.");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            //Get User Claims
            var httpRequest = _httpContextAccessor.HttpContext!.Request;
            string httpRoute = httpRequest.Path.Value!;
            bool isAuthorized = false;
            string accessRole = ""; string idShortPath = null; string error = null;
            AccessRights neededRights = AccessRights.READ;
            var claims = context.User;
            if (claims != null)
            {
                accessRole = claims.FindFirst(ClaimTypes.Role)!.Value;
                string right = claims.FindFirst("NeededRights")!.Value;
                if (claims.HasClaim(c => c.Type.Equals("IdShortPath")))
                {
                    idShortPath = claims.FindFirst("IdShortPath")!.Value;
                }
                Enum.TryParse(right, out neededRights);
            }
            if (!string.IsNullOrEmpty(idShortPath))
            {
                var parentSubmodel = resource as ISubmodel;
                isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, idShortPath, null, parentSubmodel);
            }
            else if (resource is ISubmodel submodel)
            {
                isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, submodel.IdShort!, "submodel", submodel);
            }
            else if (resource is List<IConceptDescription> || resource is IConceptDescription)
            {
                isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _);
            }

            if (isAuthorized)
            {
                _logger.LogInformation("Request authorized successfully.");
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation("Request could not be authorized successfully.");
                context.Fail(new AuthorizationFailureReason(this, error));
            }

            return Task.CompletedTask;
        }

    }
}
