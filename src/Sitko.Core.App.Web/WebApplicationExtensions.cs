using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.App.Web;

public class SitkoCoreWebApplicationBuilder : SitkoCoreApplicationBuilder
{
    private readonly WebApplicationBuilder webApplicationBuilder;

    public SitkoCoreWebApplicationBuilder(WebApplicationBuilder builder, string[] args) : base(builder, args) =>
        webApplicationBuilder = builder;

    protected override void ConfigureHostBuilder<TModule, TModuleOptions>(ApplicationModuleRegistration registration)
    {
        base.ConfigureHostBuilder<TModule, TModuleOptions>(registration);

        if (typeof(TModule).IsAssignableTo(typeof(IWebApplicationModule)))
        {
            var module = registration.GetInstance();
            var (_, options) = registration.GetOptions(BootApplicationContext);
            if (module is TModule and IWebApplicationModule<TModuleOptions> webModule &&
                options is TModuleOptions webModuleOptions)
            {
                webModule?.ConfigureWebHost(BootApplicationContext, webApplicationBuilder.WebHost, webModuleOptions);
            }
        }
    }
}

public static class WebApplicationExtensions
{
    public static SitkoCoreApplicationBuilder AddSitkoCore(this WebApplicationBuilder builder, string[] args)
    {
        builder.Services.AddTransient<IStartupFilter, SitkoCoreWebStartupFilter>();
        // if (builder.Builder is WebApplicationBuilder webApplicationBuilder)
        // {
        //     webApplicationBuilder.WebHost.
        // }
        return new SitkoCoreWebApplicationBuilder(builder, args);
    }

    public static WebApplication MapSitkoCore(this WebApplication webApplication)
    {
        var applicationContext = webApplication.Services.GetRequiredService<IApplicationContext>();
        var applicationModuleRegistrations = webApplication.Services.GetServices<ApplicationModuleRegistration>();
        var webModules =
            ModulesHelper.GetEnabledModuleRegistrations(applicationContext, applicationModuleRegistrations)
                .Select(r => r.GetInstance())
                .OfType<IWebApplicationModule>()
                .ToList();
        foreach (var webModule in webModules)
        {
            webModule.ConfigureEndpoints(applicationContext, webApplication, webApplication);
        }

        return webApplication;
    }
}

public class SitkoCoreWebStartupFilter : IStartupFilter
{
    private readonly IApplicationContext applicationContext;
    private readonly IReadOnlyList<IWebApplicationModule> webModules;

    public SitkoCoreWebStartupFilter(IApplicationContext applicationContext,
        IEnumerable<ApplicationModuleRegistration> applicationModuleRegistrations)
    {
        this.applicationContext = applicationContext;
        webModules =
            ModulesHelper.GetEnabledModuleRegistrations(applicationContext, applicationModuleRegistrations)
                .Select(r => r.GetInstance())
                .OfType<IWebApplicationModule>()
                .ToList();
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        appBuilder =>
        {
            // Configure the HTTP request pipeline.
            if (!applicationContext.IsDevelopment())
            {
                appBuilder.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                appBuilder.UseHsts();
            }

            appBuilder.UseHttpsRedirection();

            appBuilder.UseStaticFiles();

            foreach (var webModule in webModules)
            {
                webModule.ConfigureAppBuilder(applicationContext, appBuilder);
            }

            next(appBuilder);

            // foreach (var webModule in webModules)
            // {
            //     appBuilder.UseEndpoints(endpointRouteBuilder =>
            //     {
            //         webModule.ConfigureEndpoints(applicationContext, appBuilder, endpointRouteBuilder);
            //     });
            // }
        };
}
