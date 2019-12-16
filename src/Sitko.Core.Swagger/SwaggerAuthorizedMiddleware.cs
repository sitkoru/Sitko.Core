using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.Swagger
{
    [UsedImplicitly]
    public class SwaggerAuthorizedMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IPolicyEvaluator _policyEvaluator;

        public SwaggerAuthorizedMiddleware(RequestDelegate next, IPolicyEvaluator policyEvaluator)
        {
            _next = next;
            _policyEvaluator = policyEvaluator;
        }

        [SuppressMessage("ReSharper", "UseAsyncSuffix")]
        [UsedImplicitly]
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
                    await _policyEvaluator.AuthenticateAsync(policy, context);
                var authorizationResult =
                    await _policyEvaluator.AuthorizeAsync(policy, authenticateResult, context, null);
                if (!authorizationResult.Succeeded)
                {
                    await context.ChallengeAsync();
                    return;
                }
            }

            await _next.Invoke(context);
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
