using System.Threading.Tasks;
using Hangfire.Dashboard;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.HangFire.Authorization;

[PublicAPI]
public class ClaimsHangfireDashboardAuthorizationFilter : IdentityHangfireDashboardAuthorizationFilter
{
    private readonly string claimType;
    private readonly string claimValue;

    public ClaimsHangfireDashboardAuthorizationFilter(string claimType, string claimValue)
    {
        this.claimType = claimType;
        this.claimValue = claimValue;
    }

    protected override async Task<bool> DoAuthorizeAsync(DashboardContext context, HttpContext httpContext)
    {
        if (await base.DoAuthorizeAsync(context, httpContext))
        {
            return httpContext.User.HasClaim(claimType, claimValue);
        }

        return false;
    }
}
