using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App.Health;

namespace Sitko.Core.App.Web;

public static class WebApplicationBuilderExtensions
{
    public static ISitkoCoreWebApplicationBuilder AddSitkoCoreWeb(this WebApplicationBuilder builder) =>
        builder.AddSitkoCoreWeb(Array.Empty<string>());

    public static ISitkoCoreWebApplicationBuilder AddSitkoCoreWeb(this WebApplicationBuilder builder, string[] args)
    {
        builder.Services.TryAddTransient<IStartupFilter, SitkoCoreWebStartupFilter>();
        return ApplicationBuilderFactory.GetOrCreateApplicationBuilder(builder,
            applicationBuilder => new SitkoCoreWebApplicationBuilder(applicationBuilder, args));
    }

    public static TBuilder ConfigureWeb<TBuilder>(this TBuilder builder, Action<SitkoCoreWebOptions> configure)
        where TBuilder : ISitkoCoreWebApplicationBuilder
    {
        configure(builder.WebOptions);
        return builder;
    }

    public static WebApplication MapSitkoCore(this WebApplication webApplication)
    {
        var applicationContext = webApplication.Services.GetRequiredService<IApplicationContext>();
        var applicationModuleRegistrations = webApplication.Services.GetServices<ApplicationModuleRegistration>();
        var webOptions = webApplication.Services.GetRequiredService<SitkoCoreWebOptions>();

        if (webOptions.AllowAllForwardedHeaders)
        {
            webApplication.UseForwardedHeaders();
        }

        if (applicationContext.IsDevelopment())
        {
            webApplication.UseDeveloperExceptionPage();
        }
        else
        {
            webApplication.UseExceptionHandler("/Error");
        }

        if (webOptions.EnableSameSiteCookiePolicy)
        {
            webApplication.UseCookiePolicy();
        }

        if (webOptions.EnableStaticFiles)
        {
            webApplication.UseStaticFiles();
        }

        webApplication.UseAntiforgery();

        if (webOptions.CorsPolicies.Count != 0)
        {
            var defaultPolicy = webOptions.CorsPolicies.Where(item => item.Value.isDefault).Select(item => item.Key)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(defaultPolicy))
            {
                webApplication.UseCors(defaultPolicy);
            }
        }

        webApplication.MapHealthChecks("/health/all",
            new HealthCheckOptions
            {
                Predicate = _ => true, ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        webApplication.MapHealthChecks("/health/startup",
            new HealthCheckOptions
            {
                Predicate = registration => ShouldRunHealthCheck(registration, HealthCheckStages.Startup)
            });
        webApplication.MapHealthChecks("/healthz",
            new HealthCheckOptions
            {
                Predicate = registration => ShouldRunHealthCheck(registration, HealthCheckStages.Startup)
            });
        webApplication.MapHealthChecks("/ready",
            new HealthCheckOptions
            {
                Predicate = registration => ShouldRunHealthCheck(registration, HealthCheckStages.Startup)
            });

        var webModules =
            ModulesHelper.GetEnabledModuleRegistrations(applicationContext, applicationModuleRegistrations)
                .Select(r => r.GetInstance())
                .OfType<IWebApplicationModule>()
                .ToList();

        var authModules = webModules.OfType<IAuthApplicationModule>().ToList();
        if (authModules.Count != 0)
        {
            webApplication.UseAuthentication();
            webApplication.UseAuthorization();
        }

        foreach (var webModule in webModules)
        {
            webModule.ConfigureBeforeUseRouting(applicationContext, webApplication);
            webModule.ConfigureAfterUseRouting(applicationContext, webApplication);
            webModule.ConfigureEndpoints(applicationContext, webApplication, webApplication);
        }

        if (webOptions.EnableMvc)
        {
            webApplication.MapControllers();
        }

        return webApplication;
    }

    private static bool ShouldRunHealthCheck(HealthCheckRegistration registration, string stage) =>
        !registration.Tags.Contains(HealthCheckStages.GetSkipTag(stage)) &&
        !registration.Tags.Contains(HealthCheckStages.GetSkipAllTag());
}
