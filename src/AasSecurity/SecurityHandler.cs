using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace AasSecurity
{
    public class SecurityHandler : AuthorizationHandler<SecurityRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityService _securityService;

        public SecurityHandler(IHttpContextAccessor httpContextAccessor, ISecurityService securityService)
        {
            _httpContextAccessor = httpContextAccessor;
            _securityService = securityService;
        }


        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SecurityRequirement requirement)
        {
            Console.WriteLine($"Authorizing the request.");
            var httpRequest = _httpContextAccessor.HttpContext!.Request;
            if (httpRequest != null)
            {
                var httpMethod = httpRequest.Method;
                var httpRoute = httpRequest.Path.Value;
                if (httpMethod.Equals("delete", StringComparison.OrdinalIgnoreCase) ||
                    httpMethod.Equals("post", StringComparison.OrdinalIgnoreCase) ||
                    httpMethod.Equals("put", StringComparison.OrdinalIgnoreCase))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                if (httpMethod.Equals("get", StringComparison.OrdinalIgnoreCase) && httpRoute.Equals("/shells"))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                _securityService.SecurityCheckInit(_httpContextAccessor.HttpContext, httpRoute, httpMethod);
                //_securityService.SecurityCheck();
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }


    }
}
