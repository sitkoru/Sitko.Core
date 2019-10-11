using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.Auth
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthOptions _options;
        private readonly IPolicyEvaluator _policyEvaluator;

        public AuthorizationMiddleware(RequestDelegate next, AuthOptions options, IPolicyEvaluator policyEvaluator)
        {
            _next = next;
            _options = options;
            _policyEvaluator = policyEvaluator;
        }

        public async Task Invoke(HttpContext httpContext, IAuthorizationService authorizationService,
            IAuthenticationService authenticationService)
        {
            if (!string.IsNullOrEmpty(_options.ForcePolicy))
            {
                var policy = _options.Policies[_options.ForcePolicy];
                var authenticateResult =
                    await _policyEvaluator.AuthenticateAsync(policy, httpContext);
                var authorizationResult =
                    await _policyEvaluator.AuthorizeAsync(policy, authenticateResult, httpContext, null);
                if (!authorizationResult.Succeeded)
                {
                    await httpContext.ChallengeAsync();
                    return;
                }
            }

            await _next(httpContext);
        }
    }
}
