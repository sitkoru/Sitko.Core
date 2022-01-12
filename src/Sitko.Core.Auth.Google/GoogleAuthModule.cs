using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Auth.Google;

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
                OnTicketReceived = async receivedContext =>
                {
                    var config = GetOptions(receivedContext.HttpContext.RequestServices);
                    var email = receivedContext.Principal?.Claims
                        .FirstOrDefault(c => c.Type == ClaimTypes.Email)
                        ?.Value;
                    string? error = null;
                    if (email is null)
                    {
                        error = $"Empty {email} is not allowed";
                    }
                    else if (!config.Users.Contains(email))
                    {
                        error = $"User {email} is not allowed";
                    }

                    if (error is not null)
                    {
                        var logger = receivedContext.HttpContext.RequestServices
                            .GetRequiredService<ILogger<GoogleAuthModule>>();
                        logger.LogError("Auth error: {ErrorText}", error);
                        receivedContext.Response.StatusCode = 403;
                        await receivedContext.Response.WriteAsync(error);
                        receivedContext.HandleResponse();
                        return;
                    }

                    receivedContext.Success();
                }
            };
        }
    });
}
