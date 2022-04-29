using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IO.Swagger.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IO.Swagger.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUserService _userService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="encoder"></param>
        /// <param name="clock"></param>
        /// <param name="userService"></param>
        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IUserService userService)
            : base(options, logger, encoder, clock)
        {
            _userService = userService;
        }

#pragma warning disable CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
#pragma warning restore CS1591 // Fehledes XML-Kommentar für öffentlich sichtbaren Typ oder Element
        {
            Response.Headers["WWW-Authenticate"] = "Basic";
            return base.HandleChallengeAsync(properties);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // skip authentication if endpoint has [AllowAnonymous] attribute
            var endpoint = Context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
                return Task.FromResult(AuthenticateResult.NoResult());

            if (!Request.Headers.ContainsKey("Authorization"))
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
            bool isAuthenticated;

            string username;
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
                username = credentials[0];
                var password = credentials[1];
                isAuthenticated = _userService.Authenticate(username, password);
            }
            catch
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
            }

            if (!isAuthenticated)
                return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

            var claims = new[] {
                new Claim(ClaimTypes.Name, username),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
