using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Blazor.Forms;

namespace Sitko.Core.Blazor.Wasm;

public interface ISitkoCoreBlazorWasmApplicationBuilder : ISitkoCoreBlazorApplicationBuilder;

public class WasmApplicationEnvironment : IApplicationEnvironment
{
    private readonly IWebAssemblyHostEnvironment builderHostEnvironment;

    public WasmApplicationEnvironment(IWebAssemblyHostEnvironment builderHostEnvironment) =>
        this.builderHostEnvironment = builderHostEnvironment;

    public string EnvironmentName => builderHostEnvironment.Environment;
    public bool IsDevelopment() => builderHostEnvironment.IsDevelopment();

    public bool IsProduction() => builderHostEnvironment.IsProduction();
    public bool IsStaging() => builderHostEnvironment.IsStaging();
    public bool IsEnvironment(string environmentName) => builderHostEnvironment.IsEnvironment(environmentName);
}

public class SitkoCoreBlazorWasmApplicationBuilder : SitkoCoreBaseApplicationBuilder,
    ISitkoCoreBlazorWasmApplicationBuilder
{
    public SitkoCoreBlazorWasmApplicationBuilder(WebAssemblyHostBuilder builder, string[] args) : base(args,
        builder.Services, builder.Configuration, new WasmApplicationEnvironment(builder.HostEnvironment),
        builder.Logging)
    {
        builder.Services.AddScriptInjector();
        builder.Services.AddScoped<CompressedPersistentComponentState>();
        builder.Services.Configure<JsonLocalizationModuleOptions>(options =>
        {
            options.AddDefaultResource(typeof(BaseForm));
        });
        builder.ConfigureContainer(new SitkoCoreServiceProviderBuilderFactory(), _ => BeforeContainerBuild());
    }

    protected override LoggerConfiguration ConfigureDefaultLogger(LoggerConfiguration loggerConfiguration)
    {
        base.ConfigureDefaultLogger(loggerConfiguration);
        return loggerConfiguration.WriteTo.BrowserConsole(
            outputTemplate: Context.Options.ConsoleLogFormat,
            formatProvider: CultureInfo.InvariantCulture);
    }
}

public static class WebAssemblyHostBuilderExtensions
{
    public static ISitkoCoreBlazorWasmApplicationBuilder AddSitkoCoreBlazorWasm(this WebAssemblyHostBuilder builder) =>
        builder.AddSitkoCoreBlazorWasm(Array.Empty<string>());

    public static ISitkoCoreBlazorWasmApplicationBuilder AddSitkoCoreBlazorWasm(this WebAssemblyHostBuilder builder,
        string[] args) =>
        ApplicationBuilderFactory.GetOrCreateApplicationBuilder(builder,
            applicationBuilder => new SitkoCoreBlazorWasmApplicationBuilder(applicationBuilder, args));

    public static async Task RunApplicationAsync(this WebAssemblyHostBuilder builder)
    {
        var host = builder.Build();
        var lifecycle = host.Services.GetRequiredService<IApplicationLifecycle>();
        await lifecycle.StartingAsync(CancellationToken.None);
        await lifecycle.StartedAsync(CancellationToken.None);
        await host.RunAsync();
        await lifecycle.StoppingAsync(CancellationToken.None);
        await lifecycle.StoppedAsync(CancellationToken.None);
    }

    public static WebAssemblyHostBuilder ConfigureLocalization(this WebAssemblyHostBuilder builder, string culture)
    {
        builder.Services.AddLocalization();
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(culture);
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(culture);


        return builder;
    }

    public static ISitkoCoreBlazorWasmApplicationBuilder AddWebAssemblyAuth<TAuthenticationStateProvider>(
        this ISitkoCoreBlazorWasmApplicationBuilder builder)
        where TAuthenticationStateProvider : AuthenticationStateProvider
    {
        builder.ConfigureServices(services =>
        {
            services.AddAuthorizationCore();
            services.AddCascadingAuthenticationState();
            services.AddSingleton<AuthenticationStateProvider, TAuthenticationStateProvider>();
        });
        return builder;
    }
}
