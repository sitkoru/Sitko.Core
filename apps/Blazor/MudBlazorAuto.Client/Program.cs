using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sitko.Core.App.Localization;
using Sitko.Core.Blazor.MudBlazorComponents;
using Sitko.Core.Blazor.Wasm;
using Sitko.Core.Repository.Remote;
using Sitko.Core.Repository.Remote.Wasm;
using Sitko.Core.Storage;
using Sitko.Core.Storage.Remote;
using Index = MudBlazorAuto.Client.Pages.Index;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder
    .AddSitkoCoreBlazorWasm()
    .AddMudBlazor()
    .AddJsonLocalization(options => options.AddDefaultResource<Index>())
    .AddRemoteRepositories(options => options.AddRepositoriesFromAssemblyOf<Program>())
    .AddWasmHttpRepositoryTransport(options =>
    {
        options.RepositoryControllerApiRoute = new Uri("https://localhost:7163/api");
    });

builder.Services
    .AddHttpClient(nameof(HttpRepositoryTransport)).AddHttpMessageHandler<CookieHandler>();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();

public class TestRemoteStorageOptions : StorageOptions, IRemoteStorageOptions
{
    public Uri RemoteUrl { get; set; } = new("https://localhost");
    public Func<HttpClient>? HttpClientFactory { get; set; }
}
