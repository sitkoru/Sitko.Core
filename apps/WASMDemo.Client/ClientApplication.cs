using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Sitko.Core.Blazor.Wasm;
using Sitko.Core.Repository.Remote;
using Sitko.Core.Repository.Remote.Wasm;
using WASMDemo.Client.RemoteRepositories;

namespace WASMDemo.Client;

public class ClientApplication : WasmApplication
{
    public ClientApplication(string[] args) : base(args) =>
        this
            .AddRemoteRepositories(options =>
            {
                options.AddRepository<TestEntityRemoteRepository>();
            })
            .AddWasmHttpRepositoryTransport();

    protected override void ConfigureHostBuilder(WebAssemblyHostBuilder builder)
    {
        builder.Services
            .AddHttpClient(nameof(HttpRepositoryTransport)).AddHttpMessageHandler<CookieHandler>();
        builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddMudServices();
    }
}

