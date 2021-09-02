using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Auth.Google
{
    public class GoogleAuthModule : AuthModule<GoogleAuthModuleOptions>
    {
        public override string OptionsKey => "Auth:Google";

        protected override void ConfigureAuthentication(AuthenticationBuilder authenticationBuilder,
            GoogleAuthModuleOptions startupOptions) => authenticationBuilder.AddGoogle(options =>
        {
            options.ClientId = startupOptions.ClientId;
            options.ClientSecret = startupOptions.ClientSecret;
            options.SaveTokens = true;
            if (startupOptions.Users.Any())
            {
                options.Events = new OAuthEvents
                {
                    OnTicketReceived = receivedContext =>
                    {
                        var config = GetOptions(receivedContext.HttpContext.RequestServices);
                        var email = receivedContext.Principal?.Claims
                            .FirstOrDefault(c => c.Type == ClaimTypes.Email)
                            ?.Value;
                        if (email is null)
                        {
                            receivedContext.Fail($"Empty {email} is not allowed");
                        }
                        else if (!config.Users.Contains(email))
                        {
                            receivedContext.Fail($"User {email} is not allowed");
                        }
                        else
                        {
                            receivedContext.Success();
                        }

                        return Task.CompletedTask;
                    }
                };
            }
        });
    }
}
