using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sitko.Core.Auth.OIDC.Tokens;
using System.Globalization;
using System.Text.Encodings.Web;

namespace Sitko.Core.Auth.OIDC
{
    public static class SessionRecoveryExtensions
    {
        private const string AttemptKey = ".sitko.session-recovery";
        private const string RetryKey = "__sitko_relogin";
        public static IApplicationBuilder UseOidcSessionRecovery(this IApplicationBuilder app) =>
            app.Use(async (context, next) =>
            {
                var options = context.RequestServices.GetRequiredService<IOptions<OidcAuthModuleOptions>>().Value;
                if (options.EnableSessionRecovery && HttpMethods.IsGet(context.Request.Method) &&
                    context.User.Identity?.IsAuthenticated == true &&
                    (context.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>() is not null ||
                     context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>() is not null) &&
                    context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is null &&
                    context.Request.GetTypedHeaders().Accept?.Any(x => x.MediaType.Value == "text/html") == true &&
                    await HasAuthorizationPolicyAsync(context))
                {
                    try
                    {
                        _ = await context.RequestServices.GetRequiredService<IUserTokenProvider>()
                            .GetUserTokenAsync(context, context.RequestAborted);
                    }
                    catch (InvalidUserSessionException)
                    {
                        var ticket = await context.AuthenticateAsync(options.SignInScheme);
                        var retried = ticket.Properties?.Items.TryGetValue(AttemptKey, out var attempted) == true &&
                                      DateTimeOffset.TryParse(attempted, CultureInfo.InvariantCulture,
                                          DateTimeStyles.RoundtripKind, out var attemptTime) &&
                                      attemptTime > DateTimeOffset.UtcNow.AddMinutes(-5);
                        var query = QueryString.Create(context.Request.Query.Where(pair => !string.Equals(pair.Key, RetryKey, StringComparison.OrdinalIgnoreCase))
                            .SelectMany(pair => pair.Value.Select(value => new KeyValuePair<string, string?>(pair.Key, value))));
                        var destination = context.Request.PathBase + context.Request.Path + query;
                        if (destination.StartsWith("//", StringComparison.Ordinal) || destination.StartsWith("/\\", StringComparison.Ordinal))
                        {
                            destination = "/";
                        }
                        if (retried && !context.Request.Query.ContainsKey(RetryKey))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "text/html; charset=utf-8";
                            var retry = QueryHelpers.AddQueryString(destination, RetryKey, "1");
                            await context.Response.WriteAsync($"Сеанс недоступен. <a href=\"{HtmlEncoder.Default.Encode(retry)}\">Войти повторно</a>", context.RequestAborted);
                            return;
                        }

                        var properties = new AuthenticationProperties
                        {
                            RedirectUri = destination
                        };
                        properties.Items[AttemptKey] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                        await context.ChallengeAsync(options.ChallengeScheme, properties);
                        return;
                    }
                }

                try
                {
                    await next(context);
                }
                catch (InvalidUserSessionException) when (options.EnableSessionRecovery && !context.Response.HasStarted &&
                                                           context.Response.StatusCode != StatusCodes.Status403Forbidden)
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                }
            });

        private static async Task<bool> HasAuthorizationPolicyAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint is null)
            {
                return false;
            }

            var provider = context.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>();
            var policy = await AuthorizationPolicy.CombineAsync(provider,
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(),
                endpoint.Metadata.GetOrderedMetadata<AuthorizationPolicy>());
            return policy is not null || endpoint.Metadata.GetOrderedMetadata<IAuthorizationRequirementData>()
                .Any(static data => data.GetRequirements().Any());
        }
    }
}
