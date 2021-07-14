using System.Security.Claims;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Auth.Basic
{
    public class BasicAuthModule : AuthModule<BasicAuthModuleOptions>
    {
        public override string OptionsKey => "Auth:Basic";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            BasicAuthModuleOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme).AddBasic(options =>
            {
                options.Realm = startupOptions.Realm;
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
    }
}
