using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Auth;

public class AuthorizationMiddleware<TAuthOptions> where TAuthOptions : AuthOptions
{
    private readonly RequestDelegate next;
    private readonly IOptionsMonitor<TAuthOptions> options;
    private readonly IPolicyEvaluator policyEvaluator;

    public AuthorizationMiddleware(RequestDelegate next, IOptionsMonitor<TAuthOptions> options,
        IPolicyEvaluator policyEvaluator)
    {
        this.next = next;
        this.options = options;
        this.policyEvaluator = policyEvaluator;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (!options.CurrentValue.IgnoreUrls.Any() ||
            options.CurrentValue.IgnoreUrls.All(u => !httpContext.Request.Path.StartsWithSegments(u))
           )
        {
            if (!string.IsNullOrEmpty(options.CurrentValue.ForcePolicy))
            {
                var policy = options.CurrentValue.Policies[options.CurrentValue.ForcePolicy];
                var authenticateResult =
                    await policyEvaluator.AuthenticateAsync(policy, httpContext);
                var authorizationResult =
                    await policyEvaluator.AuthorizeAsync(policy, authenticateResult, httpContext, null);
                if (!authorizationResult.Succeeded)
                {
                    await httpContext.ChallengeAsync();
                    return;
                }
            }
        }

        await next(httpContext);
    }
}

