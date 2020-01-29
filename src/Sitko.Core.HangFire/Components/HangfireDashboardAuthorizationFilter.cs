using System;
using Hangfire.Dashboard;

namespace Sitko.Core.HangFire.Components
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly Func<DashboardContext, bool> _authorizedCheck;

        public HangfireDashboardAuthorizationFilter(Func<DashboardContext, bool> authorizedCheck)
        {
            _authorizedCheck = authorizedCheck;
        }

        public bool Authorize(DashboardContext context)
        {
            return _authorizedCheck(context);
        }
    }
}
