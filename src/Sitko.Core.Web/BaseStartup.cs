using System;
using System.Globalization;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Web.Components;
using StackExchange.Redis;

namespace Sitko.Core.Web
{
    public abstract class BaseStartup
    {
        protected IConfiguration Configuration { get; }
        protected IHostEnvironment Environment { get; }

        protected BaseStartup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureMvc(services.AddControllersWithViews())
                .SetCompatibilityVersion(CompatibilityVersion.Latest);
            services.AddHttpContextAccessor();
            services.AddScoped<Flasher>();
            services.AddSession();
            ConfigureAppServices(services);
        }

        public void AddRedisCache(IServiceCollection services, string redisConnectionsString)
        {
            services.AddStackExchangeRedisCache(
                options => { options.Configuration = redisConnectionsString; });
        }

        public void AddRedisDataProtection(IServiceCollection services, string redisConnectionsString)
        {
            services.AddDataProtection().PersistKeysToStackExchangeRedis(() =>
                {
                    var redis = ConnectionMultiplexer.Connect(redisConnectionsString);
                    return redis.GetDatabase();
                }, $"{Environment.ApplicationName}-DP").SetApplicationName(Environment.ApplicationName)
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        }

        public void AddMemoryCache(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
        }

        public virtual void AddDataProtection(IServiceCollection services)
        {
            services.AddDataProtection()
                .SetApplicationName(Environment.ApplicationName)
                .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        }

        protected virtual void ConfigureAppServices(IServiceCollection services)
        {
        }

        protected virtual IMvcBuilder ConfigureMvc(IMvcBuilder mvcBuilder)
        {
            if (Environment.IsDevelopment())
            {
                mvcBuilder.AddRazorRuntimeCompilation();
            }

            mvcBuilder.AddMvcOptions(options =>
            {
                options.Filters.Add<ExceptionsFilter>();
            });
            return mvcBuilder;
        }

        protected virtual IHealthChecksBuilder ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            return healthChecksBuilder;
        }

        protected virtual void ConfigureBeforeRouting(IApplicationBuilder app)
        {
        }

        protected virtual void ConfigureAfterRouting(IApplicationBuilder app)
        {
        }

        protected virtual void ConfigureEndpoints(IApplicationBuilder app,
            IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health",
                new HealthCheckOptions
                {
                    Predicate = _ => true, ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
        }

        public void Configure(IApplicationBuilder appBuilder, WebApplication application,
            IHostApplicationLifetime applicationLifetime)
        {
            if (Environment.IsDevelopment())
            {
                appBuilder.UseDeveloperExceptionPage();
            }

            applicationLifetime.ApplicationStarted.Register(() => application.ApplicationStartedHook(appBuilder));
            applicationLifetime.ApplicationStopping.Register(() => application.ApplicationStartedHook(appBuilder));
            applicationLifetime.ApplicationStopped.Register(() => application.ApplicationStartedHook(appBuilder));
            appBuilder.UseMiddleware<RequestIdMiddleware>();
            if (Environment.IsProduction())
            {
                var options = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                };
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();
                options.RequireHeaderSymmetry = false;
                appBuilder.UseForwardedHeaders(options);
                appBuilder.UseExceptionHandler("/Error");
            }

            var cultureInfo = new CultureInfo("ru-RU");

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            appBuilder.UseStaticFiles();
            appBuilder.UseSession();

            application.BeforeRoutingHook(Configuration, Environment, appBuilder);
            ConfigureBeforeRouting(appBuilder);
            appBuilder.UseRouting();
            application.AfterRoutingHook(Configuration, Environment, appBuilder);
            ConfigureAfterRouting(appBuilder);

            appBuilder.UseEndpoints(endpoints =>
            {
                application.EndpointsHook(Configuration, Environment, appBuilder, endpoints);
                ConfigureEndpoints(appBuilder, endpoints);
            });
        }
    }
}
