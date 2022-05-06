using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

public class AutomaticTokenManagementConfigureCookieOptions : IConfigureNamedOptions<CookieAuthenticationOptions>
{
    private readonly AuthenticationScheme? scheme;

    public AutomaticTokenManagementConfigureCookieOptions(IAuthenticationSchemeProvider provider) =>
        scheme = provider.GetDefaultSignInSchemeAsync().GetAwaiter().GetResult();

    public void Configure(CookieAuthenticationOptions options)
    {
    }

    public void Configure(string name, CookieAuthenticationOptions options)
    {
        if (name == scheme?.Name)
        {
            options.EventsType = typeof(AutomaticTokenManagementCookieEvents);
        }
    }
}
