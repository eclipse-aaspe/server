using AasxServerStandardBib.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AasSecurity;

public class AasSecurityAuthenticationHandler : AuthenticationHandler<AasSecurityAuthenticationOptions>
{
    private static readonly ILogger Logger = ApplicationLogging.CreateLogger("AasSecurityAuthenticationHandler");
    private readonly ISecurityService _securityService;

    public AasSecurityAuthenticationHandler(
        IOptionsMonitor<AasSecurityAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ISecurityService securityService)
        : base(options, logger, encoder, clock)
    {
        _securityService = securityService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Logger.LogDebug("Authenticating the request.");

        if (GlobalSecurityVariables.WithAuthentication)
        {
            return AuthenticateRequestAsync();
        }

        Logger.LogDebug("Server is configured without security. Therefore, skipping authentication.");
        return Task.FromResult(CreateAuthenticationTicket());
    }

    private Task<AuthenticateResult> AuthenticateRequestAsync()
    {
        var httpMethod = Request.Method;
        var httpRoute = Request.Path.Value;
        var context = Request.HttpContext;

        if (httpRoute == null)
        {
            Logger.LogError("Request route is null. Unable to authenticate.");
            return Task.FromResult(AuthenticateResult.Fail("Request route is null. Unable to authenticate."));
        }

        var ticket = _securityService.AuthenticateRequest(context, httpRoute, httpMethod, Scheme.Name);

        Logger.LogInformation("Request is successfully authenticated.");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private AuthenticateResult CreateAuthenticationTicket()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(Enumerable.Empty<Claim>(), Scheme.Name));
        var authenticationTicket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
        return AuthenticateResult.Success(authenticationTicket);
    }
}