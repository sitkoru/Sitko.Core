﻿using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Serilog;
using Sitko.Blazor.ScriptInjector;
using Sitko.Core.App;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.Components;
using Sitko.Core.Blazor.Forms;

namespace Sitko.Core.Blazor.Wasm;

public interface ISitkoCoreBlazorWasmApplicationBuilder : ISitkoCoreBlazorApplicationBuilder
{
}

public class WasmApplicationEnvironment : IApplicationEnvironment
{
    private readonly IWebAssemblyHostEnvironment builderHostEnvironment;

    public WasmApplicationEnvironment(IWebAssemblyHostEnvironment builderHostEnvironment) =>
        this.builderHostEnvironment = builderHostEnvironment;

    public string EnvironmentName => builderHostEnvironment.Environment;
    public bool IsDevelopment() => builderHostEnvironment.IsDevelopment();

    public bool IsProduction() => builderHostEnvironment.IsProduction();
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
    }

    protected override LoggerConfiguration ConfigureDefautLogger(LoggerConfiguration loggerConfiguration)
    {
        base.ConfigureDefautLogger(loggerConfiguration);
        return loggerConfiguration.WriteTo.BrowserConsole(
            outputTemplate: BootApplicationContext.Options.ConsoleLogFormat,
            formatProvider: CultureInfo.InvariantCulture);
    }
}

public static class WebAssemblyHostBuilderExtensions
{
    public static ISitkoCoreBlazorApplicationBuilder AddSitkoCore(this WebAssemblyHostBuilder builder) =>
        builder.AddSitkoCore(Array.Empty<string>());

    public static ISitkoCoreBlazorApplicationBuilder AddSitkoCore(this WebAssemblyHostBuilder builder, string[] args) =>
        ApplicationBuilderFactory.GetOrCreateApplicationBuilder(builder,
            applicationBuilder => new SitkoCoreBlazorWasmApplicationBuilder(applicationBuilder, args));
}
