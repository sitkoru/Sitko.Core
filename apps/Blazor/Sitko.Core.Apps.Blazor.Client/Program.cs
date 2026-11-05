using FluentValidation;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sitko.Core.App.Localization;
using Sitko.Core.Apps.Blazor.Client;
using Sitko.Core.Apps.Blazor.Data;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Wasm;
using Sitko.Core.Storage.Remote;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder
    .AddSitkoCoreBlazorWasm()
    .AddMudBlazor()
    .AddJsonLocalization(options => options.AddDefaultResource<Index>())
    .AddRemoteStorage<RemoteStorageOptions>(options =>
    {
        options.PublicUri = new Uri(baseAddress, "static/");
        options.RemoteUrl = new Uri(baseAddress, "Upload");
    });

builder.Services.AddValidatorsFromAssemblyContaining<DemoFormModelValidator>();
builder.Services.Configure<MudLayoutOptions>(builder.Configuration.GetSection("MudLayout"));

builder.ConfigureLocalization("ru-RU");
await builder.RunApplicationAsync();
