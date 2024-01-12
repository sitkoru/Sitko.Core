using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sitko.Core.App.Localization;
using Sitko.Core.Apps.Blazor.Client;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Wasm;
using Sitko.Core.Repository.Remote;
using Sitko.Core.Repository.Remote.Wasm;
using Sitko.Core.Storage.Remote;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder
    .AddSitkoCoreBlazorWasm()
    .AddMudBlazor()
    .AddJsonLocalization(options => options.AddDefaultResource<Index>())
    .AddRemoteStorage<RemoteStorageOptions>()
    .AddRemoteRepositories(options => options.AddRepositoriesFromAssemblyOf<Program>())
    .AddWasmHttpRepositoryTransport();

builder.Services
    .AddHttpClient(nameof(HttpRepositoryTransport)).AddHttpMessageHandler<CookieHandler>();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.Configure<MudLayoutOptions>(builder.Configuration.GetSection("MudLayout"));

builder.ConfigureLocalization("ru-RU");
await builder.RunApplicationAsync();
