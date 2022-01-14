using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using HealthChecks.Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.App.Web;
using Sitko.Core.HangFire.Components;

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
            var authFilters = new List<IDashboardAuthorizationFilter>();
            if (config.DashboardAuthorizationCheck != null)
            {
                authFilters.Add(new HangfireDashboardAuthorizationFilter(config.DashboardAuthorizationCheck));
            }

            appBuilder.UseHangfireDashboard(options: new DashboardOptions { Authorization = authFilters });
        }
    }

    public virtual void ConfigureBeforeUseRouting(IApplicationContext applicationContext,
        IApplicationBuilder appBuilder)
    {
        var config = GetOptions(appBuilder.ApplicationServices);
        if (config.IsWorkersEnabled)
        {
            appBuilder.UseHangfireServer(new BackgroundJobServerOptions
            {
                WorkerCount = config.Workers, Queues = config.Queues
            });
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
    }
}

public abstract class HangfireModuleOptions : BaseModuleOptions
{
    [JsonIgnore] public Action<IGlobalConfiguration>? ConfigureHangfire { get; set; }

    public bool IsWorkersEnabled { get; private set; }
    public int Workers { get; private set; }
    public string[] Queues { get; private set; } = { "default" };

    [JsonIgnore] public bool IsDashboardEnabled { get; private set; }

    [JsonIgnore] public Func<DashboardContext, bool>? DashboardAuthorizationCheck { get; private set; }

    [JsonIgnore] public bool IsHealthChecksEnabled { get; private set; }
    [JsonIgnore] public Action<HangfireOptions>? ConfigureHealthChecks { get; private set; }

    public void EnableWorker(int workersCount = 10, string[]? queues = null)
    {
        IsWorkersEnabled = true;
        Workers = workersCount;
        if (queues != null && queues.Any())
        {
            Queues = queues;
        }
    }

    public void EnableDashboard(Func<DashboardContext, bool>? configureAuthorizationCheck = null)
    {
        IsDashboardEnabled = true;
        DashboardAuthorizationCheck = context =>
            configureAuthorizationCheck == null || configureAuthorizationCheck.Invoke(context);
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
