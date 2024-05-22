using AasSecurity.Models;
using AasxServer;
using AasxServerStandardBib.Logging;
using AdminShellNS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AasSecurity;

public class AasSecurityAuthorizationHandler : AuthorizationHandler<SecurityRequirement, object>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISecurityService _securityService;
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger("SecurityHandler");

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

        var httpRequest = _httpContextAccessor.HttpContext?.Request;
        var httpRoute = httpRequest?.Path.Value;
        var claims = context.User;
        var accessRole = claims.FindFirst(ClaimTypes.Role)?.Value;
        var neededRights = Enum.TryParse(claims.FindFirst("NeededRights")?.Value, out AccessRights rights) ? rights : AccessRights.READ;
        var idShortPath = claims.FindFirst("IdShortPath")?.Value;
        var policy = claims.FindFirst("Policy")?.Value;

        bool isAuthorized;
        string getPolicy;
        string error;

        if (idShortPath != null)
        {
            isAuthorized = AuthorizeWithIdShortPath(resource, accessRole, httpRoute, neededRights, idShortPath, out getPolicy, out error);
        }
        else
        {
            isAuthorized = AuthorizeWithoutIdShortPath(resource, accessRole, httpRoute, neededRights, policy, out getPolicy, out error);
        }

        if (isAuthorized)
        {
            HandleSuccess(context, requirement, getPolicy, httpRequest.Path.Value);
        }
        else
        {
            HandleFailure(context, httpRoute, accessRole, error);
        }

        return Task.CompletedTask;
    }

    private bool AuthorizeWithIdShortPath(object resource, string accessRole, string httpRoute, AccessRights neededRights, string idShortPath, out string getPolicy,
        out string error)
    {
        var parentSubmodel = resource as ISubmodel;
        return _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy, idShortPath, null, parentSubmodel);
    }

    private bool AuthorizeWithoutIdShortPath(object resource, string accessRole, string httpRoute, AccessRights neededRights, string? policy, out string getPolicy,
        out string error)
    {
        var httpRequest = _httpContextAccessor.HttpContext?.Request;

        switch (resource)
        {
            case ISubmodel submodel:
                if (httpRequest != null && httpRequest.Method.Equals("head", StringComparison.OrdinalIgnoreCase))
                {
                    policy = null;
                }

                return _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy, submodel.IdShort, "submodel", submodel, policy);

            case IAssetAdministrationShell aas:
                if (httpRequest != null && httpRequest.Headers.TryGetValue("IsGetAllPackagesApi", out var header) &&
                    bool.TryParse(header.FirstOrDefault(), out var isGetAllPackagesApi) && isGetAllPackagesApi)
                {
                    return _securityService.AuthorizeRequest(accessRole, "/packages", neededRights, out error, out _, out getPolicy);
                }

                if (httpRoute.Contains("/packages/"))
                {
                    return _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy) &&
                           _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy, string.Empty, "aas", aas);
                }

                return _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy, string.Empty, "aas", aas);

            case List<IConceptDescription>:
            case IConceptDescription:
            case List<PackageDescription>:
            case string resourceString when string.IsNullOrEmpty(resourceString):
                return _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out error, out _, out getPolicy);

            default:
                getPolicy = null;
                error = "Unsupported resource type";
                return false;
        }
    }

    private void HandleSuccess(AuthorizationHandlerContext context, SecurityRequirement requirement, string getPolicy, string requestedResource)
    {
        _logger.LogInformation("Request authorized successfully.");

        if (!string.IsNullOrEmpty(getPolicy))
        {
            var response = _httpContextAccessor.HttpContext?.Response;
            if (response != null)
            {
                response.Headers.Append("policy", getPolicy);
                response.Headers.Append("policyRequestedResource", requestedResource);
            }
        }

        context.Succeed(requirement);
    }

    private void HandleFailure(AuthorizationHandlerContext context, string httpRoute, string accessRole, string error)
    {
        _logger.LogInformation("Request could not be authorized successfully.");

        if (httpRoute.Contains("/packages/") && !string.IsNullOrEmpty(Program.redirectServer) && (accessRole is "isNotAuthenticated" or null))
        {
            _logger.LogDebug("Request can be redirected.");
            CreateRedirectResponse();
            context.Fail();
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this, error));
        }
    }

    private void CreateRedirectResponse()
    {
        var context = _httpContextAccessor.HttpContext;
        var originalRequest = context?.Request.GetDisplayUrl();
        var response = $"{Program.redirectServer}?authType={Program.authType}&OriginalRequest={originalRequest}";

        _logger.LogDebug($"Redirect OriginalRequest: {originalRequest}");
        _logger.LogDebug($"Redirect Response: {response}");

        AllowCors();

        if (context == null)
        {
            return;
        }

        context.Response.Headers.Append("redirectInfo", response);
        context.Response.Redirect(response);
        context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
    }

    private void AllowCors()
    {
        var response = _httpContextAccessor.HttpContext?.Response;
        if (response == null)
        {
            return;
        }

        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Credentials", "true");
        response.Headers.Add("Access-Control-Allow-Headers", "origin, content-type, accept, authorization");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, HEAD");
    }
}