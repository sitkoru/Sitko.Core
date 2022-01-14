using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;

namespace Sitko.Core.Health;

public abstract class BaseHealthCheckPublisher<TOptions> : IHealthCheckPublisher
{
    private readonly ConcurrentDictionary<string, HealthStatus> entries = new();
    private readonly IOptionsMonitor<TOptions> optionsMonitor;

    public BaseHealthCheckPublisher(IOptionsMonitor<TOptions> options,
        ILogger<BaseHealthCheckPublisher<TOptions>> logger,
        IApplicationContext applicationContext)
    {
        optionsMonitor = options;
        Logger = logger;
        ApplicationContext = applicationContext;
    }

    protected TOptions Options => optionsMonitor.CurrentValue;
    protected ILogger<BaseHealthCheckPublisher<TOptions>> Logger { get; }
    protected IApplicationContext ApplicationContext { get; }

    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var entry in report.Entries)
        {
            var isNew = true;
            var isChanged = false;
            entries.AddOrUpdate(entry.Key, _ => entry.Value.Status, (_, reportEntry) =>
            {
                isNew = false;
                isChanged = reportEntry != entry.Value.Status;
                return entry.Value.Status;
            });

            if ((isNew && entry.Value.Status != HealthStatus.Healthy) || isChanged)
            {
                try
                {
                    await DoSendAsync(entry.Key, entry.Value, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error publishing status: {ErrorText}", ex.ToString());
                }
            }

            entries[entry.Key] = entry.Value.Status;
        }
    }

    protected abstract Task DoSendAsync(string checkName, HealthReportEntry entry,
        CancellationToken cancellationToken);
}
