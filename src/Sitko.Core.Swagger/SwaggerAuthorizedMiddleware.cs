using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.Swagger
{
    public class SwaggerAuthorizedMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IPolicyEvaluator policyEvaluator;

        public SwaggerAuthorizedMiddleware(RequestDelegate next, IPolicyEvaluator policyEvaluator)
        {
            this.next = next;
            this.policyEvaluator = policyEvaluator;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                //   await context.ChallengeAsync("Cookies");
                var policy = new AuthorizationPolicy(
                    new List<IAuthorizationRequirement>
                    {
                        new ClaimsAuthorizationRequirement("userFlag", new[] {"isAdmin"})
                    }, new[] {"Cookies", "oidc"});
                var authenticateResult =
                    await policyEvaluator.AuthenticateAsync(policy, context);
                var authorizationResult =
                    await policyEvaluator.AuthorizeAsync(policy, authenticateResult, context, null);
                if (!authorizationResult.Succeeded)
                {
                    await context.ChallengeAsync();
                    return;
                }
            }

            await next.Invoke(context);
        }
    }

    public static class SwaggerAuthorizeExtensions
    {
        public static IApplicationBuilder UseSwaggerAuthorized(this IApplicationBuilder builder, string name,
            string endPoint = "/swagger/v1/swagger.json")
        {
            builder.UseMiddleware<SwaggerAuthorizedMiddleware>();
            builder.UseSwagger();
            builder.UseSwaggerUI(c => { c.SwaggerEndpoint(endPoint, name); });
            return builder;
        }
    }
}
