using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using HealthChecks.Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.HangFire.Components;
using Sitko.Core.App.Web;

namespace Sitko.Core.HangFire
{
    public class HangfireModule<T> : BaseApplicationModule<T>, IWebApplicationModule
        where T : HangfireModuleConfig, new()
    {
        public HangfireModule(T config, Application application) : base(config, application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHangfire(config =>
            {
                Config.Configure?.Invoke(config);
                config.UseFilter(new UseQueueFromScheduledFilter());
                config.UseFilter(new PreserveOriginalQueueAttribute());
            });
            if (Config.IsHealthChecksEnabled)
            {
                services.AddHealthChecks().AddHangfire(options =>
                {
                    Config.ConfigureHealthChecks?.Invoke(options);
                });
            }
        }

        public virtual void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            if (Config.IsDashboardEnabled)
            {
                var authFilters = new List<IDashboardAuthorizationFilter>();
                if (Config.DashboardAuthorizationCheck != null)
                {
                    authFilters.Add(new HangfireDashboardAuthorizationFilter(Config.DashboardAuthorizationCheck));
                }

                appBuilder.UseHangfireDashboard(options: new DashboardOptions {Authorization = authFilters});
            }
        }

        public virtual void ConfigureBeforeUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            if (Config.IsWorkersEnabled)
            {
                appBuilder.UseHangfireServer(new BackgroundJobServerOptions
                {
                    WorkerCount = Config.Workers, Queues = Config.Queues
                });
            }

            var currentHandler =
                GlobalStateHandlers.Handlers.FirstOrDefault(h => h.StateName == ScheduledState.StateName);
            if (currentHandler != null)
            {
                GlobalStateHandlers.Handlers.Remove(currentHandler);
            }

            GlobalStateHandlers.Handlers.Add(new StateHandler());
        }
    }

    public abstract class HangfireModuleConfig
    {
        public Action<IGlobalConfiguration>? Configure { get; set; }

        public bool IsWorkersEnabled { get; private set; }
        public int Workers { get; private set; }
        public string[] Queues { get; private set; } = {"default"};

        public void EnableWorker(int workersCount = 10, string[]? queues = null)
        {
            IsWorkersEnabled = true;
            Workers = workersCount;
            if (queues != null && queues.Any())
            {
                Queues = queues;
            }
        }

        public bool IsDashboardEnabled { get; private set; }
        public Func<DashboardContext, bool>? DashboardAuthorizationCheck { get; private set; }

        public void EnableDashboard(Func<DashboardContext, bool>? configureAuthorizationCheck = null)
        {
            IsDashboardEnabled = true;
            DashboardAuthorizationCheck = context =>
                configureAuthorizationCheck == null || configureAuthorizationCheck.Invoke(context);
        }

        public bool IsHealthChecksEnabled { get; private set; }
        public Action<HangfireOptions>? ConfigureHealthChecks { get; private set; }

        public void EnableHealthChecks(Action<HangfireOptions>? configure = null)
        {
            IsHealthChecksEnabled = true;
            ConfigureHealthChecks = options =>
            {
                configure?.Invoke(options);
            };
        }
    }

    public class HangfirePostgresModuleConfig : HangfireModuleConfig
    {
        public string ConnectionString { get; set; } = string.Empty;
        public TimeSpan InvisibilityTimeout { get; set; } = TimeSpan.FromHours(5);
        public TimeSpan DistributedLockTimeout { get; set; } = TimeSpan.FromHours(5);

        public HangfirePostgresModuleConfig()
        {
            Configure = configuration =>
            {
                configuration.UsePostgreSqlStorage(ConnectionString,
                    new PostgreSqlStorageOptions
                    {
                        InvisibilityTimeout = InvisibilityTimeout, DistributedLockTimeout = DistributedLockTimeout
                    });
            };
        }
    }
}
