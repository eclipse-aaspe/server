/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using AasxServerStandardBib.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AasSecurity
{
    public class AasSecurityAuthenticationHandler : AuthenticationHandler<AasSecurityAuthenticationOptions>
    {
        private static ILogger _logger = ApplicationLogging.CreateLogger("AasSecurityAuthenticationHandler");
        private readonly ISecurityService _securityService;

        public AasSecurityAuthenticationHandler(IOptionsMonitor<AasSecurityAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ISecurityService securityService) : base(options, logger, encoder, clock)
        {
            _securityService = securityService;
        }


        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            _logger.LogDebug("Authenticating the request.");
            if (!GlobalSecurityVariables.WithAuthentication)
            {
                _logger.LogDebug("Server is configured without security. Therefore, skipping authentication.");
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(Enumerable.Empty<Claim>(), Scheme.Name));
                var authenticationTicket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
            }

            var httpMethod = Request.Method;
            var httpRoute = Request.Path.Value;
            var context = Request.HttpContext;
            var ticket = _securityService.AuthenticateRequest(context, httpRoute ?? string.Empty, httpMethod, Scheme.Name);
            if (ticket == null)
            {
                return Task.FromResult(AuthenticateResult.Fail(new Exception($"Request cannot be authenticated.")));
            }

            _logger.LogInformation($"Request is successfully authenticated.");
            return Task.FromResult(AuthenticateResult.Success(ticket));

        }

    }
}
