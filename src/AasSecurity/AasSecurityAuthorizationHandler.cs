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
        private const string? AasResourceTypeAas = "aas";
        private const string? AasResourceTypeSubmodel = "submodel";
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
                _logger.LogDebug("Server is configured without security. Therefore, skipping authorization");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            //Get User Claims
            var          httpRequest  = _httpContextAccessor.HttpContext!.Request;
            var          httpRoute    = httpRequest.Path.Value!;
            var          isAuthorized = false;
            var          getPolicy    = string.Empty;
            var          policy       = string.Empty;
            var          accessRole   = string.Empty;
            string?      idShortPath  = null;
            var          error        = String.Empty;
            AccessRights neededRights = AccessRights.READ;
            var          claims       = context.User;
            accessRole = claims.FindFirst(ClaimTypes.Role)!.Value;
            var right = claims.FindFirst("NeededRights")!.Value;
            if (claims.HasClaim(c => c.Type.Equals("IdShortPath")))
            {
                idShortPath = claims.FindFirst("IdShortPath")!.Value;
            }

            Enum.TryParse(right, out neededRights);
            var policyClaim = claims.FindFirst("Policy");
            if (policyClaim != null)
            {
                policy = policyClaim.Value;
            }

            if (!string.IsNullOrEmpty(idShortPath))
            {
                var parentSubmodel = resource as ISubmodel;
                isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy, idShortPath, null, parentSubmodel);
            }
            else
                switch (resource)
                {
                    case ISubmodel submodel:
                    {
                        var httpOperation = httpRequest.Method;
                        if (httpOperation.ToLower().Equals("head"))
                        {
                            policy = null;
                        }

                        isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy, submodel.IdShort!, AasResourceTypeSubmodel,
                                                                         submodel, policy);
                        break;
                    }
                    case IAssetAdministrationShell aas:
                    {
                        var header = _httpContextAccessor.HttpContext.Request.Headers[ "IsGetAllPackagesApi" ];
                        if (!header.IsNullOrEmpty() && header.Any())
                        {
                            var isGetAllPackagesApi = bool.Parse(header.First());
                            if (isGetAllPackagesApi)
                            {
                                httpRoute    = "/packages";
                                isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy);
                            }
                        }
                        else if (httpRoute.Contains("/packages/"))
                        {
                            //This if AASX File Server IF call, hence check the security for API Operation
                            bool isAuthorisedApi = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy);
                            //Check the security for the resource aas
                            bool isAuthorisedAas = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy, string.Empty, AasResourceTypeAas, aas);
                            isAuthorized = isAuthorisedApi && isAuthorisedAas;
                        }
                        else
                        {
                            //The request is solely for AAS
                            isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy, string.Empty, AasResourceTypeAas, aas);
                        }

                        break;
                    }
                    case List<IConceptDescription>:
                    case IConceptDescription:
                    case List<PackageDescription> packages:
                    case string resourceString when resourceString.IsNullOrEmpty():
                        isAuthorized = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy);
                        break;
                }

            if (isAuthorized)
            {
                _logger.LogInformation("Request authorized successfully");

                if (!string.IsNullOrWhiteSpace(getPolicy))
                {
                    _httpContextAccessor.HttpContext.Response.Headers.Append("policy", getPolicy);
                    _httpContextAccessor.HttpContext.Response.Headers.Append("policyRequestedResource", httpRequest.Path.Value);
                }

                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation("Request could not be authorized successfully");

                //Checking the redirect configuration
                if (httpRoute.Contains("/packages/") && !string.IsNullOrEmpty(Program.redirectServer) && (accessRole == "isNotAuthenticated"))
                {
                    _logger.LogDebug("Request can be redirected");
                    System.Collections.Specialized.NameValueCollection queryString     = System.Web.HttpUtility.ParseQueryString(string.Empty);
                    var                                                originalRequest = _httpContextAccessor.HttpContext.Request.GetDisplayUrl();
                    queryString.Add("OriginalRequest", originalRequest);
                    _logger.LogDebug("Redirect OriginalRequest: {OriginalRequest}", originalRequest);
                    var response = $"{Program.redirectServer}?authType={Program.authType}&{queryString}";
                    _logger.LogDebug("Redirect Response: {Response}", response);

                    CreateRedirectResponse(response);
                    context.Fail();
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
