using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Sitko.Core.HangFire.Components
{
    // // https://github.com/HangfireIO/Hangfire/issues/748
    public class StateHandler : IStateHandler
    {
        public void Apply(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (!(context.NewState is ScheduledState scheduledState)) return;
            var datetime = scheduledState.EnqueueAt;

            var timestamp = JobHelper.ToTimestamp(datetime);
            transaction.AddToSet("schedule", context.BackgroundJob.Id, timestamp);
        }

        public void Unapply(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            transaction.RemoveFromSet("schedule", context.BackgroundJob.Id);
        }

        public string StateName => ScheduledState.StateName;
    }
}
