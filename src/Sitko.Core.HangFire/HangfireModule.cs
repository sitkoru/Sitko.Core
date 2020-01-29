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
using Npgsql;
using Sitko.Core.App;
using Sitko.Core.HangFire.Components;
using Sitko.Core.Health;
using Sitko.Core.Web;

namespace Sitko.Core.HangFire
{
    public class HangfireModule<T> : BaseApplicationModule<T>, IWebApplicationModule where T : HangfireModuleConfig
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddHangfire(config =>
            {
                Config.Configure(config);
            });
            if (Config.ConfigureHealthChecks != null)
            {
                services.AddHealthChecks().AddHangfire(options =>
                {
                    Config.ConfigureHealthChecks(options);
                });
            }
        }

        public virtual void ConfigureAfterUseRouting(IConfiguration configuration, IHostEnvironment environment,
            IApplicationBuilder appBuilder)
        {
            if (Config.DashboardAuthorizationCheck != null)
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
            GlobalConfiguration.Configuration.UseActivator(new HangfireActivator(appBuilder.ApplicationServices))
                .UseFilter(new UseQueueFromScheduledFilter())
                .UseFilter(new PreserveOriginalQueueAttribute());

            if (Config.EnableWorker)
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

        public override List<Type> GetRequiredModules()
        {
            var list = new List<Type>();
            if (Config.ConfigureHealthChecks != null)
            {
                list.Add(typeof(HealthModule));
            }

            return list;
        }
    }

    public abstract class HangfireModuleConfig
    {
        protected HangfireModuleConfig(Action<IGlobalConfiguration> configure)
        {
            Configure = configure;
        }

        public Action<IGlobalConfiguration> Configure { get; }


        public int Workers { get; set; } = 10;
        public string[] Queues { get; set; } = {"default"};

        public bool EnableWorker { get; set; }

        public Func<DashboardContext, bool>? DashboardAuthorizationCheck { get; private set; }

        public void EnableDashboard(Func<DashboardContext, bool>? configureAuthorizationCheck = null)
        {
            DashboardAuthorizationCheck = context =>
                configureAuthorizationCheck == null || configureAuthorizationCheck.Invoke(context);
        }

        public Action<HangfireOptions>? ConfigureHealthChecks { get; private set; }

        public void EnableHealthChecks(Action<HangfireOptions>? configure = null)
        {
            ConfigureHealthChecks = options =>
            {
                configure?.Invoke(options);
            };
        }
    }

    public class HangfirePostgresModuleConfig : HangfireModuleConfig
    {
        public HangfirePostgresModuleConfig(string host, int port, string database, string username, string password,
            TimeSpan? invisibilityTimeout = null, TimeSpan? distributedLockTimeout = null) : base(configuration =>
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Database = database,
                Port = port,
                Username = username,
                Password = password
            };
            invisibilityTimeout ??= TimeSpan.FromHours(5);
            distributedLockTimeout ??= TimeSpan.FromHours(5);
            configuration.UsePostgreSqlStorage(builder.ConnectionString,
                new PostgreSqlStorageOptions
                {
                    InvisibilityTimeout = invisibilityTimeout.Value,
                    DistributedLockTimeout = distributedLockTimeout.Value
                });
        })
        {
        }
    }
}
