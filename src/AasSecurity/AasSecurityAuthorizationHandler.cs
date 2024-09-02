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

using AasSecurity.Models;
using AasxServer;
using AdminShellNS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AasSecurity
{
    public class AasSecurityAuthorizationHandler : AuthorizationHandler<SecurityRequirement, object>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityService _securityService;
        private readonly ILogger<AasSecurityAuthorizationHandler> _logger;

        private const string AasResourceTypeAas = "aas";
        private const string AasResourceTypeSubmodel = "submodel";

        public AasSecurityAuthorizationHandler(IHttpContextAccessor httpContextAccessor, ISecurityService securityService, ILogger<AasSecurityAuthorizationHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _securityService     = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _logger              = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SecurityRequirement requirement, object resource)
        {
            _logger.LogDebug("Authorizing the request");

            if (!GlobalSecurityVariables.WithAuthentication)
            {
                _logger.LogDebug("Server is configured without security. Therefore, skipping authorization");
                context.Succeed(requirement);
                return;
            }

            var httpRequest = _httpContextAccessor.HttpContext!.Request;
            var httpRoute   = httpRequest.Path.Value!;

            var (accessRole, _, neededRights, policy, error) = GetUserClaims(context.User);

            var isAuthorized = await AuthorizeResource(resource, accessRole, httpRoute, neededRights, policy);

            if (isAuthorized)
            {
                _logger.LogInformation("Request authorized successfully");

                SetPolicyHeaders(policy, httpRoute);

                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation("Request could not be authorized successfully");

                HandleAuthorizationFailure(context, accessRole, httpRoute, error);
            }
        }

        private (string accessRole, string? idShortPath, AccessRights neededRights, string policy, string error) GetUserClaims(ClaimsPrincipal claims)
        {
            var accessRole = claims.FindFirst(ClaimTypes.Role)!.Value;

            var rightClaim = claims.FindFirst("NeededRights");
            if (rightClaim == null || !Enum.TryParse(rightClaim.Value, out AccessRights neededRights))
            {
                neededRights = AccessRights.READ;
            }

            var idShortPath = claims.HasClaim(c => c.Type.Equals("IdShortPath")) ? claims.FindFirst("IdShortPath")!.Value : null;

            var policyClaim = claims.FindFirst("Policy");
            var policy      = policyClaim?.Value ?? string.Empty;

            return (accessRole, idShortPath, neededRights, policy, string.Empty);
        }


        private async Task<bool> AuthorizeResource(object resource, string accessRole, string httpRoute, AccessRights neededRights, string? policy)
        {
            switch (resource)
            {
                case ISubmodel submodel:
                    return await AuthorizeSubmodel(submodel, accessRole, httpRoute, neededRights, policy);

                case IAssetAdministrationShell aas:
                    return await AuthorizeAas(aas, accessRole, httpRoute, neededRights);

                case List<IConceptDescription> _:
                case IConceptDescription _:
                case List<PackageDescription>:
                case string resourceString when string.IsNullOrEmpty(resourceString):
                    return _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out policy);

                default:
                    return false;
            }
        }

        private Task<bool> AuthorizeSubmodel(ISubmodel submodel, string accessRole, string httpRoute, AccessRights neededRights, string? policy)
        {
            var httpOperation = _httpContextAccessor.HttpContext!.Request.Method.ToLower();
            if (httpOperation == "head")
            {
                policy = null;
            }

            return Task.FromResult(_securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out policy, submodel.IdShort!, AasResourceTypeSubmodel,
                                                                     submodel,
                                                                     policy));
        }

        private async Task<bool> AuthorizeAas(IAssetAdministrationShell aas, string accessRole, string httpRoute, AccessRights neededRights)
        {
            var header = _httpContextAccessor.HttpContext!.Request.Headers["IsGetAllPackagesApi"];
            if (!string.IsNullOrEmpty(header) && bool.TryParse(header, out var isGetAllPackagesApi) && isGetAllPackagesApi)
            {
                httpRoute = "/packages";
                return await AuthorizePackagesApi(accessRole, httpRoute, neededRights);
            }

            if (!httpRoute.Contains("/packages/"))
            {
                return _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out _, string.Empty, AasResourceTypeAas, aas);
            }

            // For routes containing "/packages/", ensure both API and AAS authorization
            var isAuthorisedApi = await AuthorizePackagesApi(accessRole, httpRoute, neededRights);
            var isAuthorisedAas = _securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out _, string.Empty, AasResourceTypeAas, aas);

            return isAuthorisedApi && isAuthorisedAas;
        }

        private Task<bool> AuthorizePackagesApi(string accessRole, string httpRoute, AccessRights neededRights)
        {
            httpRoute = "/packages";
            return  Task.FromResult(_securityService.AuthorizeRequest(accessRole, httpRoute, neededRights, out _, out _, out _));
        }


        private void SetPolicyHeaders(string? policy, string httpRoute)
        {
            if (string.IsNullOrWhiteSpace(policy))
            {
                return;
            }

            _httpContextAccessor.HttpContext!.Response.Headers.Append("policy", policy);
            _httpContextAccessor.HttpContext!.Response.Headers.Append("policyRequestedResource", httpRoute);
        }

        private void HandleAuthorizationFailure(AuthorizationHandlerContext context, string accessRole, string httpRoute, string error)
        {
            if (httpRoute.Contains("/packages/") && !string.IsNullOrEmpty(Program.redirectServer) && accessRole == "isNotAuthenticated")
            {
                _logger.LogDebug("Request can be redirected");

                var originalRequest = _httpContextAccessor.HttpContext!.Request.GetDisplayUrl();
                var queryString     = new System.Collections.Specialized.NameValueCollection {{"OriginalRequest", originalRequest}};
                _logger.LogDebug("Redirect OriginalRequest: {OriginalRequest}", originalRequest);

                var response = $"{Program.redirectServer}?authType={Program.authType}&{queryString}";
                _logger.LogDebug("Redirect Response: {Response}", response);

                CreateRedirectResponse(response);
                context.Fail();
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, error));
            }
        }

        private void CreateRedirectResponse(string responseUrl)
        {
            AllowCORS();

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return;
            }

            context.Response.Headers.Append("redirectInfo", responseUrl);
            context.Response.Redirect(responseUrl);
            context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
        }

        private void AllowCORS()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return;
            }

            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "origin, content-type, accept, authorization");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, HEAD");
        }
    }
}