using System;
using System.Collections.Generic;
using Hangfire.Common;
using Hangfire.States;
using Newtonsoft.Json;

namespace Sitko.Core.HangFire.Components
{
    // https://github.com/HangfireIO/Hangfire/issues/748
    public class ScheduledState : IState
    {
        public static readonly string StateName = "Scheduled";

        public ScheduledState(TimeSpan enqueueIn, string queue = "default")
            : this(DateTime.UtcNow.Add(enqueueIn), queue)
        {
        }

        [JsonConstructor]
        public ScheduledState(DateTime enqueueAt, string queue = "default")
        {
            EnqueueAt = enqueueAt;
            ScheduledAt = DateTime.UtcNow;
            Queue = queue;
        }

        public DateTime EnqueueAt { get; }
        private DateTime ScheduledAt { get; }
        private string Queue { get; }
        public string Name => StateName;
        public string? Reason { get; set; }
        public bool IsFinal => false;
        public bool IgnoreJobLoadException => false;

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                {"Queue", Queue},
                {"EnqueueAt", JobHelper.SerializeDateTime(EnqueueAt)},
                {"ScheduledAt", JobHelper.SerializeDateTime(ScheduledAt)}
            };
        }
    }
}
