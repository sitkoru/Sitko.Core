using System.Security.Claims;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;

namespace Sitko.Core.Auth.Basic;

public class BasicAuthModule : AuthModule<BasicAuthModuleOptions>
{
    public override string OptionsKey => "Auth:Basic";

    protected override void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
        BasicAuthModuleOptions startupOptions) =>
        authenticationBuilder.AddBasic(options =>
        {
            options.Realm = startupOptions.Realm;
            options.AllowInsecureProtocol = startupOptions.AllowInsecureProtocol;
            options.Events = new BasicAuthenticationEvents
            {
                OnValidateCredentials = validateContext =>
                {
                    var config = GetOptions(validateContext.HttpContext.RequestServices);
                    if (validateContext.Username == config.Username && validateContext.Password == config.Password)
                    {
                        var claims = new[]
                        {
                            new Claim(
                                ClaimTypes.NameIdentifier,
                                validateContext.Username,
                                ClaimValueTypes.String,
                                validateContext.Options.ClaimsIssuer),
                            new Claim(
                                ClaimTypes.Name,
                                validateContext.Username,
                                ClaimValueTypes.String,
                                validateContext.Options.ClaimsIssuer)
                        };

                        validateContext.Principal = new ClaimsPrincipal(
                            new ClaimsIdentity(claims, validateContext.Scheme.Name));
                        validateContext.Success();
                    }

                    return Task.CompletedTask;
                }
            };
        });
}
