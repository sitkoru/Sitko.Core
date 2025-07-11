using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.App.Web;

namespace Sitko.Core.Xunit.Web;

public class WebTestScope : WebTestScope<HostApplicationBuilder, BaseTestConfig>
{
    protected override HostApplicationBuilder CreateHostBuilder()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.AddSitkoCore();
        return builder;
    }

    protected override IHost BuildApplication(HostApplicationBuilder builder) => builder.Build();
}

public abstract class WebTestScope<TApplicationBuilder, TConfig> : BaseTestScope<TApplicationBuilder, TConfig>
    where TConfig : BaseTestConfig, new()
    where TApplicationBuilder : IHostApplicationBuilder
{
    protected IHost? Host { get; private set; }
    protected TestServer? Server { get; private set; }

    protected virtual bool UseTestServer => true;
    protected virtual bool RunApplication => true;

    protected virtual WebApplicationBuilder ConfigureWebApplication(WebApplicationBuilder webApplicationBuilder,
        string name) => webApplicationBuilder;

    public override async Task BeforeConfiguredAsync(string name)
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddSitkoCoreWeb();
        ConfigureWebApplication(builder, name);
        builder.Services.AddMvc().AddApplicationPart(GetType().Assembly)
            .AddControllersAsServices();
        if (UseTestServer)
        {
            builder.WebHost.UseTestServer();
        }

        var host = builder.Build();
        host.MapControllers();
        host.MapSitkoCore();
        Host = host;
        if (RunApplication)
        {
            await StartWebApplicationAsync();
        }

        if (UseTestServer)
        {
            Server = host.GetTestServer();
        }

        await InitWebApplicationAsync(host.Services);
    }

    public Task StartWebApplicationAsync() => Host is not null
        ? Host.StartAsync()
        : throw new InvalidOperationException("Host is not configured");

    protected virtual Task InitWebApplicationAsync(IServiceProvider hostServices) => Task.CompletedTask;

    protected override async Task OnDisposeAsync()
    {
        await base.OnDisposeAsync();
        if (Host is not null)
        {
            await Host.StopAsync();
            Host.Dispose();
        }
    }
}
