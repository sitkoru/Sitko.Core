using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.PostgreSql;
using HealthChecks.Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.HangFire;

public class HangfireModule<THangfireConfig> : BaseApplicationModule<THangfireConfig>, IWebApplicationModule
    where THangfireConfig : HangfireModuleOptions, new()
{
    public override string OptionsKey => "Hangfire";

    public virtual void ConfigureAfterUseRouting(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
        var config = GetOptions(appBuilder.ApplicationServices);
        if (config.IsDashboardEnabled)
        {
            var options = new DashboardOptions();
            config.DashboardConfigure?.Invoke(options);

            appBuilder.UseHangfireDashboard(options: options);
        }
    }

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        THangfireConfig startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddHangfire(config =>
        {
            startupOptions.ConfigureHangfire?.Invoke(config);
        });
        if (startupOptions.IsHealthChecksEnabled)
        {
            services.AddHealthChecks().AddHangfire(options =>
            {
                startupOptions.ConfigureHealthChecks?.Invoke(options);
            });
        }

        if (startupOptions.Workers.Any())
        {
            foreach (var (workersCount, queues) in startupOptions.Workers)
            {
                services.AddHangfireServer(options =>
                {
                    options.WorkerCount = workersCount;
                    options.Queues = queues;
                });
            }
        }
    }
}

public abstract class HangfireModuleOptions : BaseModuleOptions
{
    [JsonIgnore] public Action<IGlobalConfiguration>? ConfigureHangfire { get; set; }

    public List<(int workersCount, string[] queues)> Workers { get; private set; } = new();

    [JsonIgnore] public bool IsDashboardEnabled { get; private set; }

    [JsonIgnore] public bool IsHealthChecksEnabled { get; private set; }
    [JsonIgnore] public Action<HangfireOptions>? ConfigureHealthChecks { get; private set; }

    public Action<DashboardOptions>? DashboardConfigure { get; set; }

    public void EnableWorker(int workersCount = 10, string[]? queues = null) =>
        AddWorker(workersCount, queues ?? new[] { "default" });

    public void AddWorker(int workersCount, string[] queues) => Workers.Add((workersCount, queues));

    public void EnableDashboard(Action<DashboardOptions>? configure = null)
    {
        IsDashboardEnabled = true;
        DashboardConfigure = configure;
    }

    public void EnableHealthChecks(Action<HangfireOptions>? configure = null)
    {
        IsHealthChecksEnabled = true;
        ConfigureHealthChecks = options =>
        {
            configure?.Invoke(options);
        };
    }
}

public class HangfirePostgresModuleOptions : HangfireModuleOptions
{
    public HangfirePostgresModuleOptions() =>
        ConfigureHangfire = configuration =>
        {
            configuration.UsePostgreSqlStorage(ConnectionString,
                new PostgreSqlStorageOptions
                {
                    InvisibilityTimeout = TimeSpan.FromMinutes(InvisibilityTimeoutInMinutes),
                    DistributedLockTimeout = TimeSpan.FromMinutes(DistributedLockTimeoutInMinutes)
                });
        };

    public string ConnectionString { get; set; } = string.Empty;
    public int InvisibilityTimeoutInMinutes { get; set; } = 300;
    public int DistributedLockTimeoutInMinutes { get; set; } = 300;
}
