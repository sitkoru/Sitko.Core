using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Xunit.Web;

public class WebTestScope : WebTestScope<WebTestApplication, TestStartup>
{
}

public class WebTestScope<TWebApplication, TWebStartup> : WebTestScope<TWebApplication, TWebStartup,
    TestApplication, BaseTestConfig> where TWebApplication : WebTestApplication<TWebStartup>
    where TWebStartup : TestStartup
{
}

public class WebTestScope<TWebApplication, TWebStartup, TApplication, TConfig> : BaseTestScope<TApplication, TConfig>
    where TWebApplication : WebTestApplication<TWebStartup>
    where TWebStartup : TestStartup
    where TApplication : HostedApplication
    where TConfig : BaseTestConfig, new()
{
    protected IHost? Host { get; private set; }
    protected TestServer? Server { get; private set; }
    protected virtual TWebApplication ConfigureWebApplication(TWebApplication application, string name) => application;

    public override async Task BeforeConfiguredAsync(string name)
    {
        if (Activator.CreateInstance(typeof(TWebApplication), new object[] { Array.Empty<string>() }) is TWebApplication
            application)
        {
            ConfigureWebApplication(application, name);
            application.ConfigureServices(services =>
            {
                services.AddMvc().AddApplicationPart(GetType().Assembly).AddControllersAsServices();
            });
            Host = await application.StartAsync();
            Server = Host.GetTestServer();
            await InitWebApplicationAsync(Host.Services);
        }
        else
        {
            throw new InvalidOperationException($"Can't create {typeof(TWebApplication)}");
        }
    }

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

