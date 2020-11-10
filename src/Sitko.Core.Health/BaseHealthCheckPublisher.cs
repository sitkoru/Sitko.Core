using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Health
{
    public abstract class BaseHealthCheckPublisher<TOptions> : IHealthCheckPublisher
    {
        protected readonly TOptions Options;
        protected readonly ILogger<BaseHealthCheckPublisher<TOptions>> Logger;
        protected readonly IHostEnvironment HostingEnvironment;

        private readonly ConcurrentDictionary<string, HealthStatus> _entries =
            new ConcurrentDictionary<string, HealthStatus>();

        public BaseHealthCheckPublisher(TOptions options,
            ILogger<BaseHealthCheckPublisher<TOptions>> logger,
            IHostEnvironment hostingEnvironment)
        {
            Options = options;
            Logger = logger;
            HostingEnvironment = hostingEnvironment;
        }

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            foreach (var entry in report.Entries)
            {
                bool isNew = true;
                bool isChanged = false;
                _entries.AddOrUpdate(entry.Key, s => entry.Value.Status, (s, reportEntry) =>
                {
                    isNew = false;
                    isChanged = reportEntry != entry.Value.Status;
                    return entry.Value.Status;
                });

                if (isNew && entry.Value.Status != HealthStatus.Healthy || isChanged)
                {
                    try
                    {
                        await DoSendAsync(entry.Key, entry.Value, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, ex.ToString());
                    }
                }

                _entries[entry.Key] = entry.Value.Status;
            }
        }

        protected abstract Task DoSendAsync(string checkName, HealthReportEntry entry,
            CancellationToken cancellationToken);
    }
}
