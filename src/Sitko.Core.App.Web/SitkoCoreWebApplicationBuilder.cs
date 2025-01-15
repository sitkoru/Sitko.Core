using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Sitko.Core.App.Web;

public interface ISitkoCoreWebApplicationBuilder : ISitkoCoreApplicationBuilder
{
    internal SitkoCoreWebOptions WebOptions { get; }
}

public class SitkoCoreWebApplicationBuilder : SitkoCoreServerApplicationBuilder, ISitkoCoreWebApplicationBuilder
{
    private readonly WebApplicationBuilder webApplicationBuilder;
    private readonly SitkoCoreWebOptions webOptions = new();
    SitkoCoreWebOptions ISitkoCoreWebApplicationBuilder.WebOptions => webOptions;

    public SitkoCoreWebApplicationBuilder(WebApplicationBuilder builder, string[] args) : base(builder, args)
    {
        webApplicationBuilder = builder;
        webApplicationBuilder.Services.AddSingleton(webOptions);
        AddModule<AppWebConfigurationModule, AppWebConfigurationModuleOptions>();
        ConfigureOpenTelemetry((_, _, otelBuilder) =>
        {
            otelBuilder.WithTracing(providerBuilder => providerBuilder.AddAspNetCoreInstrumentation());
            otelBuilder.WithMetrics(providerBuilder => providerBuilder.AddAspNetCoreInstrumentation());
        });
    }

    protected override void ConfigureHostBuilder<TModule, TModuleOptions>(ApplicationModuleRegistration registration)
    {
        base.ConfigureHostBuilder<TModule, TModuleOptions>(registration);

        if (typeof(TModule).IsAssignableTo(typeof(IWebApplicationModule)))
        {
            var module = registration.GetInstance();
            var (_, options) = registration.GetOptions(Context);
            if (module is TModule and IWebApplicationModule<TModuleOptions> webModule &&
                options is TModuleOptions webModuleOptions)
            {
                webModule.ConfigureWebHost(Context, webApplicationBuilder.WebHost, webModuleOptions);
            }
        }
    }

    protected override void BeforeContainerBuild()
    {
        base.BeforeContainerBuild();
        if (webOptions.EnableMvc)
        {
            Services.AddControllersWithViews().AddControllersAsServices();
        }

        if (webOptions.AddHttpContextAccessor)
        {
            Services.AddHttpContextAccessor();
        }

        if (webOptions.EnableSameSiteCookiePolicy)
        {
            Services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.None;
                options.OnAppendCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext =>
                    CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });
        }

        if (webOptions.CorsPolicies.Count != 0)
        {
            Services.AddCors(options =>
            {
                foreach (var (name, (policy, _)) in webOptions.CorsPolicies)
                {
                    options.AddPolicy(name, policy);
                }
            });
        }

        if (webOptions.AllowAllForwardedHeaders)
        {
            Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto |
                                           ForwardedHeaders.XForwardedHost;
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();
            });
        }

        var dataProtectionBuilder = Services.AddDataProtection()
            .SetApplicationName(Context.Name)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        webOptions.ConfigureDataProtection?.Invoke(dataProtectionBuilder);

        ConfigureHealthChecks(Services.AddHealthChecks());
    }

    protected virtual IHealthChecksBuilder ConfigureHealthChecks(IHealthChecksBuilder healthChecksBuilder) =>
        healthChecksBuilder;

    // https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/
    private static void CheckSameSite(HttpContext httpContext, CookieOptions options)
    {
        if (options.SameSite > SameSiteMode.None)
        {
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            if (DisallowsSameSiteNone(userAgent))
            {
                options.SameSite = SameSiteMode.Unspecified;
            }
        }
    }

    private static bool DisallowsSameSiteNone(string userAgent)
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

[PublicAPI]
public class SitkoCoreWebOptions
{
    internal Dictionary<string, (CorsPolicy policy, bool isDefault)> CorsPolicies { get; } = new();
    public bool EnableMvc { get; set; } = true;
    public bool AddHttpContextAccessor { get; set; } = true;
    public bool EnableSameSiteCookiePolicy { get; set; } = true;
    public bool AllowAllForwardedHeaders { get; set; } = true;
    public bool EnableStaticFiles { get; set; } = true;

    public Action<IDataProtectionBuilder>? ConfigureDataProtection { get; set; }

    public SitkoCoreWebOptions AddCorsPolicy(string name, CorsPolicy policy, bool isDefault = false)
    {
        if (CorsPolicies.ContainsKey(name))
        {
            throw new ArgumentException($"Cors policy with name {name} already registered", nameof(name));
        }

        if (isDefault && CorsPolicies.Any(c => c.Value.isDefault))
        {
            throw new ArgumentException("Default policy already registered", nameof(isDefault));
        }

        CorsPolicies.Add(name, (policy, isDefault));

        return this;
    }

    public SitkoCoreWebOptions AddCorsPolicy(string name, Action<CorsPolicyBuilder> buildPolicy, bool isDefault = false)
    {
        var builder = new CorsPolicyBuilder();
        buildPolicy(builder);
        AddCorsPolicy(name, builder.Build(), isDefault);

        return this;
    }
}
