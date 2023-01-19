using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

public static class AutomaticTokenManagementBuilderExtensions
{
    public static AuthenticationBuilder AddAutomaticTokenManagement(this AuthenticationBuilder builder,
        Action<AutomaticTokenManagementOptions> options)
    {
        builder.Services.Configure(options);
        return builder.AddAutomaticTokenManagement();
    }

    public static AuthenticationBuilder AddAutomaticTokenManagement(this AuthenticationBuilder builder)
    {
        builder.Services.AddHttpClient("tokenClient");
        builder.Services.AddTransient<TokenEndpointService>();

        builder.Services.AddTransient<AutomaticTokenManagementCookieEvents>();

        return builder;
    }
}
