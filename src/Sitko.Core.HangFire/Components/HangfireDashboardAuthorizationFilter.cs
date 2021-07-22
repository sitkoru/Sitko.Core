using System;
using Hangfire.Dashboard;

namespace Sitko.Core.HangFire.Components
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly Func<DashboardContext, bool> authorizedCheck;

        public HangfireDashboardAuthorizationFilter(Func<DashboardContext, bool> authorizedCheck) => this.authorizedCheck = authorizedCheck;

        public bool Authorize(DashboardContext context) => authorizedCheck(context);
    }
}
