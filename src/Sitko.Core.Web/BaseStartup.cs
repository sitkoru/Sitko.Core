using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.Web
{
    public abstract class BaseStartup<T> where T : WebApplication<T>
    {
        protected IConfiguration Configuration { get; }
        protected IHostEnvironment Environment { get; }

        protected virtual bool EnableMvc { get; } = true;
        protected virtual bool AddHttpContextAccessor { get; } = true;
        protected virtual bool EnableSameSiteCookiePolicy { get; } = true;
        protected virtual bool EnableStaticFiles { get; } = true;

        private readonly Dictionary<string, (CorsPolicy policy, bool isDefault)> _corsPolicies =
            new Dictionary<string, (CorsPolicy policy, bool isDefault)>();

        private string _defaultCulture;

        protected BaseStartup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            if (EnableMvc)
            {
                ConfigureMvc(services.AddControllersWithViews())
                    .SetCompatibilityVersion(CompatibilityVersion.Latest);
            }

            if (AddHttpContextAccessor)
            {
                services.AddHttpContextAccessor();
            }

            if (EnableSameSiteCookiePolicy)
            {
                services.Configure<CookiePolicyOptions>(options =>
                {
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                    options.OnAppendCookie = cookieContext =>
                        CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                    options.OnDeleteCookie = cookieContext =>
                        CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                });
            }

            if (_corsPolicies.Any())
            {
                services.AddCors(options =>
                {
                    foreach ((string name, (CorsPolicy policy, _)) in _corsPolicies)
                    {
                        options.AddPolicy(name, policy);
                    }
                });
            }

            WebApplication<T>.GetInstance().ConfigureStartupServices(services, Configuration, Environment);
            ConfigureAppServices(services);
        }

        public virtual void AddRedisCache(IServiceCollection services, string redisConnectionsString)
        {
            services.AddStackExchangeRedisCache(
                options => { options.Configuration = redisConnectionsString; });
        }

        public virtual void AddMemoryCache(IServiceCollection services)
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
            
            return mvcBuilder;
        }

        protected virtual IHealthChecksBuilder ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            return healthChecksBuilder;
        }

        protected virtual void ConfigureBeforeRoutingMiddleware(IApplicationBuilder app)
        {
        }

        protected virtual void ConfigureBeforeRoutingModulesHook(IApplicationBuilder app)
        {
        }

        protected virtual void ConfigureAfterRoutingMiddleware(IApplicationBuilder app)
        {
        }

        protected virtual void ConfigureAfterRoutingModulesHook(IApplicationBuilder app)
        {
        }

        protected virtual void ConfigureEndpoints(IApplicationBuilder app,
            IEndpointRouteBuilder endpoints)
        {
            if (EnableMvc)
            {
                endpoints.MapControllers();
            }

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

            if (EnableSameSiteCookiePolicy)
            {
                appBuilder.UseCookiePolicy();
            }

            if (EnableStaticFiles)
            {
                UseStaticFiles(appBuilder);
            }

            ConfigureBeforeRoutingModulesHook(appBuilder);
            application.BeforeRoutingHook(Configuration, Environment, appBuilder);
            ConfigureBeforeRoutingMiddleware(appBuilder);
            appBuilder.UseRouting();
            if (_corsPolicies.Any())
            {
                var defaultPolicy = _corsPolicies.Where(item => item.Value.isDefault).Select(item => item.Key)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(defaultPolicy))
                {
                    appBuilder.UseCors(defaultPolicy);
                }
            }

            ConfigureAfterRoutingMiddleware(appBuilder);
            application.AfterRoutingHook(Configuration, Environment, appBuilder);
            ConfigureAfterRoutingModulesHook(appBuilder);

            appBuilder.UseEndpoints(endpoints =>
            {
                application.EndpointsHook(Configuration, Environment, appBuilder, endpoints);
                ConfigureEndpoints(appBuilder, endpoints);
            });
        }

        protected virtual void UseStaticFiles(IApplicationBuilder appBuilder)
        {
            appBuilder.UseStaticFiles();
        }

        protected void AddCorsPolicy(string name, CorsPolicy policy, bool isDefault = false)
        {
            if (_corsPolicies.ContainsKey(name))
            {
                throw new ArgumentException($"Cors policy with name {name} already registered", nameof(name));
            }

            if (isDefault && _corsPolicies.Any(c => c.Value.isDefault))
            {
                throw new ArgumentException("Default policy already registered", nameof(isDefault));
            }

            _corsPolicies.Add(name, (policy, isDefault));
        }

        protected void AddCorsPolicy(string name, Action<CorsPolicyBuilder> buildPolicy, bool isDefault = false)
        {
            var builder = new CorsPolicyBuilder();
            buildPolicy(builder);
            AddCorsPolicy(name, builder.Build(), isDefault);
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
