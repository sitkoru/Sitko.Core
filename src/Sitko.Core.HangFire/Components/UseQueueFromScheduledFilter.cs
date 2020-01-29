using Hangfire.Common;
using Hangfire.States;

namespace Sitko.Core.HangFire.Components
{
    // // https://github.com/HangfireIO/Hangfire/issues/748
    public class UseQueueFromScheduledFilter : IJobFilter, IElectStateFilter
    {
        public void OnStateElection(ElectStateContext context)
        {
            var enqueuedState = context.CandidateState as EnqueuedState;

            if (enqueuedState != null)
            {
                var stateData = context.Connection.GetStateData(context.BackgroundJob.Id);
                if (stateData.Data.TryGetValue("Queue", out var queueName))
                {
                    enqueuedState.Queue = queueName;
                }
            }
        }

        public bool AllowMultiple { get; } = false;
        public int Order { get; } = 1;
    }
}
