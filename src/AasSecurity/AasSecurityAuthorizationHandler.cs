using AasSecurity.Models;
using AasxServer;
using AasxServerStandardBib.Logging;
using AdminShellNS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
            else if (resource is IAssetAdministrationShell aas)
            {
                var header = _httpContextAccessor.HttpContext.Request.Headers["IsGetAllPackagesApi"];
                if (!header.IsNullOrEmpty() && header.Any())
                {
                    bool isGetAllPackagesApi = bool.Parse(header.First());
                    if (isGetAllPackagesApi)
                    {
                        httpRoute = "/packages";
                        isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _);
                    }
                }
                else if (httpRoute.Contains("/packages/"))
                {
                    //This if AASX File Server IF call, hence check the security for API Operation
                    bool isAuthorisedApi = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _);
                    //Check the security for the resource aas
                    bool isAuthorisedAas = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, "", "aas", aas);
                    isAuthorized = isAuthorisedApi && isAuthorisedAas;
                }
                else
                {
                    //The request is solely for AAS
                    isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, "", "aas", aas);
                }

            }
            else if (resource is List<IConceptDescription> || resource is IConceptDescription)
            {
                isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _);
            }
            else if (resource is List<PackageDescription> packages)
            {
                isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _);
            }
            else if (resource is string resourceString && resourceString.IsNullOrEmpty())
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

                //Checking the redirect configuration
                if (httpRoute.Contains("/packages/"))
                {
                    if (!string.IsNullOrEmpty(Program.redirectServer) && (accessRole == "isNotAuthenticated" || accessRole == null))
                    {
                        _logger.LogDebug("Request can be redirected.");
                        System.Collections.Specialized.NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
                        string originalRequest = _httpContextAccessor.HttpContext.Request.GetDisplayUrl();
                        queryString.Add("OriginalRequest", originalRequest);
                        _logger.LogDebug("Redirect OriginalRequset: " + originalRequest);
                        string response = Program.redirectServer + "?" + "authType=" + Program.authType + "&" + queryString;
                        _logger.LogDebug("Redirect Response: " + response + "\n");

                        CreateRedirectResponse(response);
                        context.Fail();

                    }
                    else
                        context.Fail(new AuthorizationFailureReason(this, error));
                }
                else
                    context.Fail(new AuthorizationFailureReason(this, error));
            }

            return Task.CompletedTask;
        }

        private void CreateRedirectResponse(string responseUrl)
        {
            AllowCORS();

            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Headers.Append("redirectInfo", responseUrl);
                context.Response.Redirect(responseUrl);
                context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
            }
        }

        private void AllowCORS()
        {
            var context = _httpContextAccessor.HttpContext;
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "origin, content-type, accept, authorization");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, HEAD");
        }
    }
}
