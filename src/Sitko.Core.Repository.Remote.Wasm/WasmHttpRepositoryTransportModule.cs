using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote.Wasm;

public class WasmHttpRepositoryTransportModule : HttpRepositoryTransportModule
{
    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        HttpRepositoryTransportOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddTransient<CookieHandler>();
    }

    protected override IHttpClientBuilder RegisterHttpClient(IApplicationContext context, IServiceCollection services,
        HttpRepositoryTransportOptions startupOptions) =>
        base.RegisterHttpClient(context, services, startupOptions).AddHttpMessageHandler<CookieHandler>();
}

public class CookieHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return await base.SendAsync(request, cancellationToken);
    }
}
