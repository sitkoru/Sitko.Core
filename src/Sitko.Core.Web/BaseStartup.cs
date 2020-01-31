using System;
using System.Globalization;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Web.Components;

namespace Sitko.Core.Web
{
    public abstract class BaseStartup<T> where T : WebApplication<T>
    {
        protected IConfiguration Configuration { get; }
        protected IHostEnvironment Environment { get; }

        private string _defaultCulture;

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
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });
            WebApplication<T>.GetInstance().ConfigureStartupServices(services, Configuration, Environment);
            ConfigureAppServices(services);
        }

        public void AddRedisCache(IServiceCollection services, string redisConnectionsString)
        {
            services.AddStackExchangeRedisCache(
                options => { options.Configuration = redisConnectionsString; });
        }

        public void AddMemoryCache(IServiceCollection services)
        {
            services.AddMemoryCache();
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

        public void Configure(IApplicationBuilder appBuilder, WebApplication<T> application)
        {
            if (Environment.IsDevelopment())
            {
                appBuilder.UseDeveloperExceptionPage();
            }

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

            if (!string.IsNullOrEmpty(_defaultCulture))
            {
                var cultureInfo = new CultureInfo(_defaultCulture);

                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            }

            appBuilder.UseCookiePolicy();
            appBuilder.UseStaticFiles();

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

        protected void SetDefaultCulture(string culture)
        {
            _defaultCulture = culture;
        }

        // https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/
        private void CheckSameSite(HttpContext httpContext, CookieOptions options)
        {
            if (options.SameSite > SameSiteMode.None)
            {
                var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                if (DisallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }

        public static bool DisallowsSameSiteNone(string userAgent)
        {
            // Cover all iOS based browsers here. This includes:
            // - Safari on iOS 12 for iPhone, iPod Touch, iPad
            // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
            // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
            // All of which are broken by SameSite=None, because they use the iOS networking stack
            if (userAgent.Contains("CPU iPhone OS 12") || userAgent.Contains("iPad; CPU OS 12"))
            {
                return true;
            }

            // Cover Mac OS X based browsers that use the Mac OS networking stack. This includes:
            // - Safari on Mac OS X.
            // This does not include:
            // - Chrome on Mac OS X
            // Because they do not use the Mac OS networking stack.
            if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") &&
                userAgent.Contains("Version/") && userAgent.Contains("Safari"))
            {
                return true;
            }

            // Cover Chrome 50-69, because some versions are broken by SameSite=None, 
            // and none in this range require it.
            // Note: this covers some pre-Chromium Edge versions, 
            // but pre-Chromium Edge does not require SameSite=None.
            if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
            {
                return true;
            }

            return false;
        }
    }
}
