using System;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.HangFire.Components
{
    public class HangfireActivator : JobActivator
    {
        private readonly IServiceProvider _serviceProvider;

        public HangfireActivator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override object ActivateJob(Type type)
        {
            return _serviceProvider.CreateScope().ServiceProvider.GetService(type);
        }
    }
}
