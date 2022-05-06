using System.Threading.Tasks;
using Hangfire.Dashboard;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.HangFire.Authorization;

[PublicAPI]
public class IdentityHangfireDashboardAuthorizationFilter : IDashboardAsyncAuthorizationFilter
{
    public async Task<bool> AuthorizeAsync(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await httpContext.ChallengeAsync();
            return true;
        }

        return await DoAuthorizeAsync(context, httpContext);
    }

    protected virtual Task<bool> DoAuthorizeAsync(DashboardContext context, HttpContext httpContext) =>
        Task.FromResult(true);
}
