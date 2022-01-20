using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Storage.Remote.Tests.Server;
using Sitko.Core.Xunit;

namespace Sitko.Core.Storage.Remote.Tests;

public class BaseRemoteStorageTestScope : BaseTestScope
{
    private IHost? host;
    private TestServer? server;

    public override async Task BeforeConfiguredAsync()
    {
        var application = new RemoteStorageServerApplication(Array.Empty<string>());
        host = await application.StartAsync();
        server = host.GetTestServer();
    }

    protected override TestApplication ConfigureApplication(TestApplication application, string name)
    {
        base.ConfigureApplication(application, name);
        application.AddRemoteStorage<TestRemoteStorageSettings>(moduleOptions =>
        {
            moduleOptions.PublicUri = new Uri("https://localhost");
            if (server is not null)
            {
                moduleOptions.RemoteUrl = new Uri(server.BaseAddress, "Upload/");
            }
        });

        return application;
    }

    protected override IServiceCollection ConfigureServices(IApplicationContext applicationContext,
        IServiceCollection services, string name)
    {
        base.ConfigureServices(applicationContext, services, name);
        if (server is not null)
        {
            services.AddTransient<HttpClient>(_ => server.CreateClient());
        }

        return services;
    }


    public override async ValueTask DisposeAsync()
    {
        var storage = GetService<IStorage<TestRemoteStorageSettings>>();
        await storage.DeleteAllAsync();
        await base.DisposeAsync();
        if (host is not null)
        {
            await host.StopAsync();
            host.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
