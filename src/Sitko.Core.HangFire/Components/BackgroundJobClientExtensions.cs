using System;
using System.Linq.Expressions;
using Hangfire;
using Hangfire.Common;

namespace Sitko.Core.HangFire.Components
{
    public static class BackgroundJobClientExtensions
    {
        public static string Create<T>(
            this IBackgroundJobClient client,
            Expression<Action<T>> methodCall,
            string queueName)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            var scheduledState = new ScheduledState(TimeSpan.FromSeconds(1), queueName);
            return client.Create(Job.FromExpression(methodCall), scheduledState);
        }
    }
}
