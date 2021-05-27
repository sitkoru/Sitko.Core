using System.Security.Claims;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Auth.Basic
{
    public class BasicAuthModule : AuthModule<BasicAuthOptions>
    {
        public override string GetConfigKey()
        {
            return "Auth:Basic";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            BasicAuthOptions startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme).AddBasic(options =>
            {
                options.Realm = startupConfig.Realm;
                options.Events = new BasicAuthenticationEvents
                {
                    OnValidateCredentials = validateContext =>
                    {
                        var config = GetConfig(validateContext.HttpContext.RequestServices);
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
